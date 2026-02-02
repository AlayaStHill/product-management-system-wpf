using ApplicationLayer.DTOs;
using ApplicationLayer.Interfaces;
using ApplicationLayer.Results;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Presentation.Interfaces;


namespace Presentation.ViewModels;

public partial class ProductEditViewModel(IViewNavigationService viewNavigationService, IProductService productService) : StatusViewModelBase
{
    private readonly IViewNavigationService _viewNavigationService = viewNavigationService;
    private readonly IProductService _productService = productService;

    [ObservableProperty]
    private ProductUpdateRequest? _productData;

    [ObservableProperty]
    private string _title = "Uppdatera produkt";

    public void SetProduct(ProductUpdateRequest product)
    {
        ProductData = new ProductUpdateRequest
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            CategoryName = product.CategoryName,
            ManufacturerName = product.ManufacturerName,
        };
    }

    [RelayCommand]
    private async Task Save(CancellationToken ct)
    {
        try
        {
            if (ProductData is null) 
            {
                SetStatus("Inga produktuppgifter angivna.", "red");
                return;
            }

            string name = ProductData.Name?.Trim() ?? string.Empty;

            List<string> errors = [];

            if (string.IsNullOrWhiteSpace(ProductData.Name))
                errors.Add("Namn måste anges.");

            if (ProductData.Price is null)
                errors.Add("Pris måste anges.");
            else if (ProductData.Price <= 0)
                errors.Add("Pris måste vara större än 0.");

            if (errors.Count > 0)
            {
                SetStatus(string.Join("\n", errors), "red");
                return;
            }

            ProductData.Name = name;

            ServiceResult saveResult = await _productService.UpdateProductAsync(ProductData, ct);

            if (!saveResult.Succeeded)
            {
                SetStatus(saveResult.ErrorMessage ?? "Produkten kunde inte uppdateras.", "red");
                return;
            }

            SetStatus("Produkten har uppdaterats.", "green");

            await Task.Delay(1000, ct);

            await _viewNavigationService.NavigateToAsync<ProductListViewModel>(viewmodel => viewmodel.PopulateProductListAsync(ct));
        }
        catch (OperationCanceledException) 
        {
            return;
        }
        catch (Exception ex)
        {
            SetStatus($"Ett oväntat fel uppstod: {ex.Message}", "red");
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _viewNavigationService.NavigateTo<ProductListViewModel>();
    }
}
