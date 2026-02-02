using ApplicationLayer.DTOs;
using ApplicationLayer.Factories;
using ApplicationLayer.Helpers;
using ApplicationLayer.Interfaces;
using ApplicationLayer.Results;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Results;

namespace ApplicationLayer.Services;
public partial class ProductService(IRepository<Product> productRepository, IRepository<Category> categoryRepository, IRepository<Manufacturer> manufacturerRepository) : IProductService
{
    private readonly IRepository<Product> _productRepository = productRepository;
    private readonly IRepository<Category> _categoryRepository = categoryRepository;
    private readonly IRepository<Manufacturer> _manufacturerRepository = manufacturerRepository;
    private List<Product> _productList = [];
    private bool _isLoaded;


    public async Task<ServiceResult> EnsureLoadedAsync(CancellationToken ct)
    {
        if (_isLoaded)
            return new ServiceResult { Succeeded = true, StatusCode = 200 };

        try
        {
            RepositoryResult<IEnumerable<Product>>? loadResult = await _productRepository.ReadAsync(ct);
            if (!loadResult.Succeeded)
                return loadResult.MapToServiceResult("Ett okänt fel uppstod vid filhämtning");

            _productList = [.. (loadResult.Data ?? [])];
            _isLoaded = true;

            return new ServiceResult { Succeeded = true, StatusCode = 200 };
        }
        // Avbruten laddning (CancellationToken)
        catch (OperationCanceledException)
        {
            return new ServiceResult { Succeeded = false, StatusCode = 408, ErrorMessage = "Hämtning avbröts av användaren." };
        }
        catch (Exception ex)
        {
            return new ServiceResult { Succeeded = false, StatusCode = 500, ErrorMessage = $"Fel vid filhämtning: {ex.Message}" };
        }
    }


    public async Task<ServiceResult<Product>> SaveProductAsync(ProductCreateRequest createRequest, CancellationToken ct = default)  
    {
        try 
        {
            ServiceResult validationResult = ValidateRequest(createRequest);
            if (!validationResult.Succeeded)
                return new ServiceResult<Product>
                {
                    Succeeded = false,
                    StatusCode = 400,
                    ErrorMessage = validationResult.ErrorMessage
                };

            ServiceResult ensureResult = await EnsureLoadedAsync(ct);
            if (!ensureResult.Succeeded)
                return new ServiceResult<Product>
                {
                    Succeeded = false,
                    StatusCode = ensureResult.StatusCode,
                    ErrorMessage = ensureResult.ErrorMessage,
                    Data = null
                };

            string trimmedName = createRequest.Name.Trim();

            if (IsDuplicateName(trimmedName))
                return new ServiceResult<Product> { Succeeded = false, StatusCode = 409, ErrorMessage = $"En produkt med namnet {createRequest.Name} finns redan." };

            Product newProduct = ProductFactory.MapRequestToProduct(createRequest);
            newProduct.Name = trimmedName;
            _productList.Add(newProduct);

            RepositoryResult saveResult = await _productRepository.WriteAsync(_productList, ct);
            if (!saveResult.Succeeded)
                return saveResult.MapToServiceResultAs<Product>("Kunde inte spara till fil.");

            return new ServiceResult<Product> { Succeeded = true, StatusCode = 201, Data = newProduct };
        }
        catch (OperationCanceledException) // Avbruten av användaren
        {
            return new ServiceResult<Product> { Succeeded = false, StatusCode = 408, ErrorMessage = "Sparande avbröts av användaren" };
        }
        catch (Exception ex) 
        {
            return new ServiceResult<Product> { Succeeded = false, StatusCode = 500, ErrorMessage = $"Det gick inte att spara produkten: {ex.Message}", Data = null };
        }
    }



    public async Task<ServiceResult<IEnumerable<Product>>> GetProductsAsync(CancellationToken ct = default)
    {
        ServiceResult ensureResult = await EnsureLoadedAsync(ct);
        if (!ensureResult.Succeeded)
            return new ServiceResult<IEnumerable<Product>> {
                Succeeded = false,
                StatusCode = ensureResult.StatusCode,
                ErrorMessage = ensureResult.ErrorMessage,
                Data = []
            };

        return new ServiceResult<IEnumerable<Product>> { Succeeded = true, StatusCode = 200, Data = [.. _productList] };
    }


    public async Task<ServiceResult> UpdateProductAsync(ProductUpdateRequest updateRequest, CancellationToken ct = default)
    {
        try 
        {
            ServiceResult validationResult = ValidateRequest(updateRequest); 
            if (!validationResult.Succeeded)
                return validationResult;

            ServiceResult ensureResult = await EnsureLoadedAsync(ct);
            if (!ensureResult.Succeeded)
                return ensureResult;

            if (FindExistingProduct(updateRequest.Id) is not Product existingProduct)
                return new ServiceResult { Succeeded = false, StatusCode = 404, ErrorMessage = $"Produkten med Id {updateRequest.Id} kunde inte hittas" };

            string trimmedName = updateRequest.Name.Trim();

            if (IsDuplicateName(trimmedName, updateRequest.Id))
                return new ServiceResult { Succeeded = false, StatusCode = 409, ErrorMessage = $"En produkt med namnet {updateRequest.Name} finns redan." };

            ServiceResult categoryResult = await UpdateCategoryAsync(existingProduct, updateRequest.CategoryName);
            if (!categoryResult.Succeeded)
                return categoryResult;

            ServiceResult manufacturerResult = await UpdateManufacturerAsync(existingProduct, updateRequest.ManufacturerName);
            if (!manufacturerResult.Succeeded)
                return manufacturerResult;

            existingProduct.Name = trimmedName;
            existingProduct.Price = updateRequest.Price!.Value; 

            RepositoryResult saveResult = await _productRepository.WriteAsync(_productList, ct);
            if (!saveResult.Succeeded)
                return saveResult.MapToServiceResult("Ett okänt fel uppstod vid filsparning");

            return new ServiceResult { Succeeded = true, StatusCode = 204 };
        }
        catch (OperationCanceledException)
        {
            return new ServiceResult { Succeeded = false, StatusCode = 408, ErrorMessage = "Uppdatering avbröts av användaren" };
        }
        catch (Exception ex)
        {
            return new ServiceResult { Succeeded = false, StatusCode = 500, ErrorMessage = $"Det gick inte att uppdatera produkten: {ex.Message}" };
        }
    }



    public async Task<ServiceResult> DeleteProductAsync(string id, CancellationToken ct = default)
    {
        try 
        {
            ServiceResult ensureResult = await EnsureLoadedAsync(ct);
            if (!ensureResult.Succeeded)
                return ensureResult;

            Product? productToDelete = FindExistingProduct(id);
            if (productToDelete is null)
                return new ServiceResult { Succeeded = false, StatusCode = 404, ErrorMessage = $"Produkten med Id {id} kunde inte hittas" };

            _productList.Remove(productToDelete);

            RepositoryResult repoSaveResult = await _productRepository.WriteAsync(_productList, ct);
            if (!repoSaveResult.Succeeded)
                return repoSaveResult.MapToServiceResult("Ett okänt fel uppstod vid filsparning");

            return new ServiceResult { Succeeded = true, StatusCode = 204 };
        }
        catch (OperationCanceledException)
        {
            return new ServiceResult { Succeeded = false, StatusCode = 408, ErrorMessage = "Borttagning avbröts av användaren" };
        }
        catch (Exception ex)
        {
            return new ServiceResult { Succeeded = false, StatusCode = 500, ErrorMessage = $"Det gick inte att ta bort produkten: {ex.Message}" };
        }
    }
}

