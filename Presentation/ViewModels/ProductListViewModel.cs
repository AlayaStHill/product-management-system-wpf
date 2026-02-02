using ApplicationLayer.DTOs;
using ApplicationLayer.Interfaces;
using ApplicationLayer.Results;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Presentation.Interfaces;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels;

public partial class ProductListViewModel : StatusViewModelBase 
{
    private readonly IViewNavigationService _viewNavigationService;
    private readonly IProductService _productService;
    private bool _suppressCancelStatus;

    public IAsyncRelayCommand LoadCommand { get; } 
    public IAsyncRelayCommand RefreshCommand { get; } 

    public ProductListViewModel(IViewNavigationService navigationService, IProductService productService)
    {
        _viewNavigationService = navigationService;
        _productService = productService;

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);

        LoadCommand.Execute(null);
    }

    public ObservableCollection<Product> ProductList { get; } = new();

    [ObservableProperty]
    private string _title = "Produktlista";

    [ObservableProperty]
    private bool _isLoading;


    public async Task PopulateProductListAsync(CancellationToken ct = default)
    {
        ServiceResult<IEnumerable<Product>> loadResult = await _productService.GetProductsAsync(ct);

        if (!loadResult.Succeeded)
        {
            SetStatus(loadResult!.ErrorMessage ?? "Kunde inte hämta produkterna. Försök igen senare.", "red");
            return;
        }

        ProductList.Clear();

        await Task.Yield();

        foreach (Product product in loadResult.Data ?? [])
        {
            ct.ThrowIfCancellationRequested(); 
            ProductList.Add(product);
        }
    }

    private async Task LoadAsync(CancellationToken ct) 
    {
        try
        {
            IsLoading = true;
            await PopulateProductListAsync(ct);
        }
        catch (OperationCanceledException) 
        {
            if (!_suppressCancelStatus)
                SetStatus("Laddning avbröts.", "red");
        }
        catch (Exception ex) 
        {
            SetStatus($"Ett oväntat fel uppstod: {ex.Message}", "red");
        }
        finally {  IsLoading = false; }
    }

    private async Task RefreshAsync(CancellationToken ct) 
    {
        try
        {
            IsLoading = true;

            if (!_suppressCancelStatus)
                SetStatus("Laddar om...", "black");

            await Task.Delay(3000, ct);

            await PopulateProductListAsync(ct);

            int count = ProductList.Count;
            string plural = count == 1 ? "produkt" : "produkter";

            SetStatus($"Listan är uppdaterad. {count} {plural}.", "green");
        }
        catch (OperationCanceledException) 
        {
            if (!_suppressCancelStatus)
                SetStatus("Omladdning avbröts.", "red");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CancelRefresh()
    {
        if (RefreshCommand.IsRunning)
        {
            RefreshCommand.Cancel();
        }
    }

    private async Task CancelOngoingLoadsAsync(bool suppressStatus = true) 
    {
        if (!(RefreshCommand.IsRunning || LoadCommand.IsRunning))
            return;

        bool previous = _suppressCancelStatus;
        _suppressCancelStatus = suppressStatus;

        try
        {
            RefreshCommand.Cancel();
            LoadCommand.Cancel();

            Task waitRefresh = RefreshCommand.ExecutionTask ?? Task.CompletedTask;
            Task waitLoad = LoadCommand.ExecutionTask ?? Task.CompletedTask;

            try { await Task.WhenAll(waitRefresh, waitLoad); }
            catch (OperationCanceledException) { }
        }
        finally
        {
            _suppressCancelStatus = previous;
        }
    }

    [RelayCommand] 
    private async Task NavigateToProductAddView()
    {
        await CancelOngoingLoadsAsync(suppressStatus: true);
        await ClearStatusAfterAsync(0);
        IsLoading = false;
        _viewNavigationService.NavigateTo<ProductAddViewModel>();
    }

    [RelayCommand]
    private async Task Edit(Product? selectedProduct)
    {
        if (selectedProduct is null)
        {
            SetStatus("Välj en produkt att redigera.", "red");
            return;
        }

        await CancelOngoingLoadsAsync(suppressStatus: true);
        await ClearStatusAfterAsync(0);
        IsLoading = false;

        ProductUpdateRequest dto = new ProductUpdateRequest
        {
            Id = selectedProduct.Id,
            Name = selectedProduct.Name,
            Price = selectedProduct.Price,
            CategoryName = selectedProduct.Category?.Name,
            ManufacturerName = selectedProduct.Manufacturer?.Name
        };

        _viewNavigationService.NavigateTo<ProductEditViewModel>(viewmodel => viewmodel.SetProduct(dto));
    }

    [RelayCommand] 
    private async Task Delete(string productId, CancellationToken ct) 
    {
        try 
        {
            ServiceResult deleteResult = await _productService.DeleteProductAsync(productId, ct); 
            if (!deleteResult.Succeeded)
            {
                SetStatus(deleteResult.ErrorMessage ?? "Kunde inte ta bort produkten", "red");
                return; 
            }

            await PopulateProductListAsync(ct);

            SetStatus("Produkten har tagits bort", "green");
        }
        catch (Exception ex)
        {
            SetStatus($"Ett oväntat fel uppstod: {ex.Message}", "red");  
        }
    }
}

