using ApplicationLayer.Results;
using Domain.Results;

namespace ApplicationLayer.Helpers;
// Översätter RepoResult till ServiceResult. Ger läsbarhet och mindre duplicering, men behåller specificitet i felmeddelanden. 
public static class ResultMappers
{
    // RepositoryResult -> ServiceResult (utan data)
    public static ServiceResult MapToServiceResult(
    this RepositoryResult repoResult, 
    string? customErrorMessage = null,
    int? overrideStatusCode = null)
    {
        if (!repoResult.Succeeded)
        {
            return new ServiceResult
            {
                Succeeded = false,
                StatusCode = overrideStatusCode ?? (repoResult.StatusCode == 0 ? 500 : repoResult.StatusCode),
                ErrorMessage = customErrorMessage ?? repoResult.ErrorMessage ?? "Ett okänt fel uppstod vid filhantering."
            };
        }

        return new ServiceResult
        {
            Succeeded = true,
            StatusCode = overrideStatusCode ?? (repoResult.StatusCode == 0 ? 200 : repoResult.StatusCode)
        };
    }


    // RepositoryResult<T> -> ServiceResult<T> (med data)
    public static ServiceResult<T> MapToServiceResult<T>(
        this RepositoryResult<T> repoResult,
        string? customErrorMessage = null,
        int? overrideStatusCode = null)
    {
        if (!repoResult.Succeeded || repoResult.Data is null)
        {
            return new ServiceResult<T>
            {
                Succeeded = false,
                StatusCode = overrideStatusCode ?? (repoResult.StatusCode == 0 ? 500 : repoResult.StatusCode),
                ErrorMessage = customErrorMessage ?? repoResult.ErrorMessage ?? "Ett okänt fel uppstod vid filhantering.",
                Data = default
            };
        }

        return new ServiceResult<T>
        {
            Succeeded = true,
            StatusCode = repoResult.StatusCode,
            Data = repoResult.Data
        };
    }

    // RepositoryResult (icke-generisk) -> ServiceResult<T> (utan data)
    public static ServiceResult<T> MapToServiceResultAs<T>(
    this RepositoryResult repoResult,
    string? customErrorMessage = null,
    int? overrideStatusCode = null)
    {
        if (!repoResult.Succeeded)
        {
            return new ServiceResult<T>
            {
                Succeeded = false,
                StatusCode = overrideStatusCode ?? (repoResult.StatusCode == 0 ? 500 : repoResult.StatusCode),
                ErrorMessage = customErrorMessage ?? repoResult.ErrorMessage ?? "Ett okänt fel uppstod vid filhantering.",
                Data = default
            };
        }

        return new ServiceResult<T>
        {
            Succeeded = true,
            StatusCode = overrideStatusCode ?? (repoResult.StatusCode == 0 ? 200 : repoResult.StatusCode),
            Data = default
        };
    }
}

