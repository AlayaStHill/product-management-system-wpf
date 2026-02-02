using Domain.Results;
using Infrastructure.Repositories;
using System.Text.Json;

namespace Assignment2.Tests.Infrastructure.Repositories;

public class JsonRepository_Tests : IDisposable 
{
    private readonly string _testDirectory;
    private readonly string _testFilePath;

    public JsonRepository_Tests() 
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "JsonRepoTests", Guid.NewGuid().ToString("N")); 
        _testFilePath = Path.Combine(_testDirectory, "test.json");
    }

    private class TestEntity
    {
        public string Id { get; set; } = ""; 
        public string Name { get; set; } = ""; 
    }

    public void Dispose()
    {
        try 
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, recursive: true); 
        }
        catch {}
    }

    // Happy path
    [Fact]
    public void EnsureInitialized_ShouldCreateDirectoryAndFile_WhenTheyDoNotExist()
    {
        // ARRANGE 
        Assert.False(Directory.Exists(_testDirectory));
        Assert.False(File.Exists(_testFilePath));

        // ACT 
        JsonRepository<TestEntity>.EnsureInitialized(_testDirectory, _testFilePath);

        // ASSERT 
        Assert.True(Directory.Exists(_testDirectory));
        Assert.True(File.Exists(_testFilePath));
        Assert.Equal("[]", File.ReadAllText(_testFilePath)); 
    }

    // Happy path
    [Fact]
    public void EnsureInitialized_ShouldOverwriteWithEmptyArray_WhenFileContainsOnlyWhitespace()
    {
        // ARRANGE: 
        Directory.CreateDirectory(_testDirectory);
        File.WriteAllText(_testFilePath, "   \r\n\t  ");

        // ACT: 
        JsonRepository<TestEntity>.EnsureInitialized(_testDirectory, _testFilePath);

        // ASSERT
        Assert.Equal("[]", File.ReadAllText(_testFilePath));
    }

    // Happy path
    [Fact]
    public void EnsureInitialized_ShouldOverwriteWithEmptyArray_WhenFileIsEmpty()
    {
        // ARRANGE:
        Directory.CreateDirectory(_testDirectory);
        File.WriteAllText(_testFilePath, string.Empty);
        Assert.True(File.Exists(_testFilePath));

        // ACT: 
        JsonRepository<TestEntity>.EnsureInitialized(_testDirectory, _testFilePath);

        // ASSERT:
        Assert.Equal("[]", File.ReadAllText(_testFilePath));
    }


    // Happy-path
    [Fact]
    public async Task WriteAsync_ShouldWriteJsonAndReturnSucceeded_WhenEntitiesProvided()
    {
        // ARRANGE: 
        JsonRepository<TestEntity> repo = new JsonRepository<TestEntity>(_testDirectory, "test.json");
        List<TestEntity> entities = new()
        {
            new() { Id = "1", Name = "Banan" },
            new() { Id = "2", Name = "Äpple" }
        };

        // ACT: 
        RepositoryResult result = await repo.WriteAsync(entities, CancellationToken.None);

        // ASSERT: 
        Assert.True(result.Succeeded);
        Assert.True(File.Exists(_testFilePath));

        string json = File.ReadAllText(_testFilePath);
        List<TestEntity>? roundtrip = JsonSerializer.Deserialize<List<TestEntity>>(json);
        Assert.NotNull(roundtrip);
        Assert.Equal(2, roundtrip!.Count);
        Assert.Equal("1", roundtrip[0].Id);
        Assert.Equal("Banan", roundtrip[0].Name);
        Assert.Equal("2", roundtrip[1].Id);
        Assert.Equal("Äpple", roundtrip[1].Name);
    }


    // Negative case test. 
    [Fact]
    public async Task WriteAsync_ShouldReturnInternalServerError_WhenFileIsLocked()
    {
        // ARRANGE:
        Directory.CreateDirectory(_testDirectory);
        File.WriteAllText(_testFilePath, "[]");

        JsonRepository<TestEntity> repo = new JsonRepository<TestEntity>(_testDirectory, "test.json");

        using FileStream lockStream = new(
            _testFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.None);

        List<TestEntity> entities = new() { new() { Id = "1", Name = "Banan" } };

        // ACT: 
        RepositoryResult result = await repo.WriteAsync(entities, CancellationToken.None);

        // ASSERT: 
        Assert.False(result.Succeeded);
        Assert.Equal(500, result.StatusCode);
        Assert.Contains("Kunde inte spara till fil:", result.ErrorMessage);
    }
}

