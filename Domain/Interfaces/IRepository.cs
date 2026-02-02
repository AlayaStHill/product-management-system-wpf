using Domain.Results;

namespace Domain.Interfaces;
// Gemensamt interface för att undvika duplicerad read/write-logik (DRY).
public interface IRepository<T>
{
    Task<RepositoryResult<IEnumerable<T>>> ReadAsync(CancellationToken ct);
    Task<RepositoryResult> WriteAsync(IEnumerable<T> entities, CancellationToken ct);
}

