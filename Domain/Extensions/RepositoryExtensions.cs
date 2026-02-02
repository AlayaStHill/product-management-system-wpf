using Domain.Interfaces;
using Domain.Results;

namespace Domain.Extensions;
// Försöker hämta befintlig entitet eller skapar och sparar en ny om ingen matchar.
public static class RepositoryExtensions
{
    public static async Task<RepositoryResult<T>> GetOrCreateAsync<T>(
        this IRepository<T> repository,
        Func<T, bool> isMatch,
        Func<T> createEntity, 
        CancellationToken cancellationToken)
        where T : class
    {
        RepositoryResult<IEnumerable<T>> readResult = await repository.ReadAsync(cancellationToken);
        if (!readResult.Succeeded)
            return new RepositoryResult<T> { Succeeded = false, StatusCode = readResult.StatusCode, ErrorMessage = readResult.ErrorMessage, Data = null };


        T? entity = readResult.Data!.FirstOrDefault(isMatch);
        if (entity != null)
            return RepositoryResult<T>.OK(entity);

        entity = createEntity(); 

        List<T> list = readResult.Data!.ToList();
        list.Add(entity);

        RepositoryResult writeResult = await repository.WriteAsync(list, cancellationToken);
        if (!writeResult.Succeeded)
        {
            return new RepositoryResult<T>
            {
                Succeeded = false,
                StatusCode = writeResult.StatusCode,
                ErrorMessage = writeResult.ErrorMessage,
                Data = null
            };
        }
        
        return RepositoryResult<T>.Created(entity);
    }
}
