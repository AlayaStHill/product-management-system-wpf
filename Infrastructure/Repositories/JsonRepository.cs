using Domain.Interfaces;
using Domain.Results;
using System.Text.Json;

namespace Infrastructure.Repositories;

// JSON-baserad repository-implementation.
// Ansvarar för att säkerställa att lagringsfil och katalog finns,
// samt läsa och skriva hela entitetslistan till fil.
public class JsonRepository<T> : IRepository<T> where T : class
{
    private readonly string _filePath;
    private readonly string _dataDirectory;
    private static JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public JsonRepository(string dataDirectory, string fileName)
    {
        _dataDirectory = dataDirectory;
        _filePath = Path.Combine(dataDirectory, fileName);

        // Säkerställer att katalog och lagringsfil finns innan repository används
        EnsureInitialized(_dataDirectory, _filePath);
    }



    public static void EnsureInitialized(string dataDirectory, string filePath)
    {
        // Säkerställer att datakatalogen finns
        if (!Directory.Exists(dataDirectory))
            Directory.CreateDirectory(dataDirectory);


        if (!File.Exists(filePath))
        {
            // Skapa fil med tom JSON-array
            File.WriteAllText(filePath, "[]");
            return;
        }
            

        // Om filen finns men är tom eller whitespace, initiera den
        string existing = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(existing))
            File.WriteAllText(filePath, "[]");
    }


    public async Task<RepositoryResult<IEnumerable<T>>> ReadAsync(CancellationToken ct)
    {
        try
        {
            EnsureInitialized(_dataDirectory, _filePath);

            string json = await File.ReadAllTextAsync(_filePath, ct);

            List<T>? entities = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);
            return RepositoryResult<IEnumerable<T>>.OK(entities ?? []);
        }
        catch (OperationCanceledException) { throw; } // bubbla vidare så att avbryt via cancellationtoken fungerar
        catch (JsonException ex)
        {
            await File.WriteAllTextAsync(_filePath, "[]", ct);
            return RepositoryResult<IEnumerable<T>>.InternalServerError($"Ogiltig JSON: {ex.Message}");
        }
        catch (IOException ex)
        {
            return RepositoryResult<IEnumerable<T>>.InternalServerError($"Filfel: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return RepositoryResult<IEnumerable<T>>.InternalServerError($"Behörighetsfel: {ex.Message}");
        }
        catch (Exception ex) // Sista fallback för oväntade fel som inte fångats
        {
            return RepositoryResult<IEnumerable<T>>.InternalServerError($"Oväntat fel vid läsning: {ex.Message}");
        }
    }

    public async Task<RepositoryResult> WriteAsync(IEnumerable<T> entities, CancellationToken ct)
    {
        try
        {
            string json = JsonSerializer.Serialize(entities, _jsonOptions);
            await File.WriteAllTextAsync(_filePath, json, ct);

            return RepositoryResult.NoContent();

        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            return RepositoryResult.InternalServerError($"Kunde inte spara till fil: {ex.Message}");
        }
    }
}
