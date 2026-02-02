using ApplicationLayer.DTOs;
using ApplicationLayer.Results;
using ApplicationLayer.Services;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Results;
using Moq;

namespace Assignment2.Tests.ApplicationLayer.Services;

public class ProductService_Tests
{
    private readonly Mock<IRepository<Product>> _productRepoMock;
    private readonly Mock<IRepository<Category>> _categoryRepoMock;
    private readonly Mock<IRepository<Manufacturer>> _manufacturerRepoMock;
    private readonly ProductService _productService; 

    public ProductService_Tests()
    {
        _productRepoMock = new Mock<IRepository<Product>>();
        _categoryRepoMock = new Mock<IRepository<Category>>();
        _manufacturerRepoMock = new Mock<IRepository<Manufacturer>>();

        _productService = new(
            _productRepoMock.Object,
            _categoryRepoMock.Object,
            _manufacturerRepoMock.Object
        );
    }

    // Happy path
    [Fact]
    public async Task EnsureLoadedAsync_ShouldLoadProducts_WhenRepositoryReturnsData()
    {
        // ARRANGE: 
        List<Product> productList = new() { new Product { Id = "1", Name = "Banan", Price = 6m } };

        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.OK(productList));

        // ACT: 
        ServiceResult result = await _productService.EnsureLoadedAsync(CancellationToken.None); 

        // ASSERT: 
        Assert.True(result.Succeeded);
        Assert.Equal(200, result.StatusCode);

        // kontrollerar att ReadAsync anropades exakt en gång
        _productRepoMock.Verify(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    // Negative case: EnsureLoaded ska returnera ett ServiceResult med felmeddelande
    [Fact]
    public async Task EnsureLoadedAsync_ShouldReturnError_WhenReadAsyncFails()
    {
        // ARRANGE: 
        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.InternalServerError("Läsfel"));

        // ACT: 
        ServiceResult result = await _productService.EnsureLoadedAsync(CancellationToken.None);

        // ASSERT: 
        Assert.False(result.Succeeded);
        Assert.Equal(500, result.StatusCode);
        Assert.NotNull(result.ErrorMessage);               
        Assert.Equal("Ett okänt fel uppstod vid filhämtning", result.ErrorMessage);
    }



    // Happy path
    [Fact]
    public async Task GetProductsAsync_ShouldReturnProducts_WhenEnsureLoadedSucceeds()
    {
        // ARRANGE: 
        List<Product> productList = new()
        {
            new Product { Id = "1", Name = "Banan", Price = 6m },
            new Product { Id = "2", Name = "Äpple", Price = 8m }
        };

        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.OK(productList));

        // ACT: 
        ServiceResult<IEnumerable<Product>> result = await _productService.GetProductsAsync(CancellationToken.None);

        // ASSERT: 
        Assert.True(result.Succeeded);
        Assert.Equal(200, result.StatusCode);
        // Verifiera innehåll och ordning i resultatlistan
        Assert.Collection(result.Data!,
            product => Assert.Equal("Banan", product.Name),
            product => Assert.Equal("Äpple", product.Name));
    }

    // Negative case
    [Fact]
    public async Task GetProductsAsync_ShouldReturnError_WhenEnsureLoadedFails()
    {
        // ARRANGE: 
        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.InternalServerError("Läsfel"));

        // ACT: 
        ServiceResult<IEnumerable<Product>> result = await _productService.GetProductsAsync(CancellationToken.None);

        // ASSERT:
        Assert.False(result.Succeeded);
        Assert.Equal(500, result.StatusCode);
        Assert.NotNull(result.Data);                        
        Assert.Empty(result.Data);                          
        Assert.NotNull(result.ErrorMessage);                
        Assert.Equal("Ett okänt fel uppstod vid filhämtning", result.ErrorMessage);
    }


    // Happy path: 
    [Fact]
    public async Task SaveProductAsync_ShouldCreateAndSaveProduct_WhenValidRequest()
    {
        // ARRANGE: 
        ProductCreateRequest createRequest = new()
        {
            Name = "  Banan  ",    
            Price = 6m
        };


        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.OK(new List<Product>()));

        _productRepoMock
            .Setup(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult.NoContent());

        // ACT: 
        ServiceResult<Product> result = await _productService.SaveProductAsync(createRequest, CancellationToken.None);

        // ASSERT: 
        Assert.True(result.Succeeded);
        Assert.Equal(201, result.StatusCode);
        Assert.NotNull(result.Data); 
        Assert.Equal("Banan", result.Data.Name);     
        Assert.Equal(6m, result.Data.Price);

        _productRepoMock.Verify(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Negative case: 
    [Fact]
    public async Task SaveProductAsync_ShouldReturnError_WhenRequestIsInvalid()
    {
        // ARRANGE: 
        ProductCreateRequest createRequest = new()
        {
            Name = "",
            Price = null
        };

        // ACT: 
        ServiceResult<Product> result = await _productService.SaveProductAsync(createRequest, CancellationToken.None);

        // ASSERT: 
        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Null(result.Data);                         
        Assert.NotNull(result.ErrorMessage);              
        Assert.Contains("Namn måste anges.", result.ErrorMessage);
        Assert.Contains("Pris måste anges.", result.ErrorMessage);

        _productRepoMock.Verify(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Negative case: 
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task SaveProductAsync_ShouldReturnError_WhenPriceIsZeroOrNegative(decimal invalidPrice)
    {
        // ARRANGE:
        ProductCreateRequest createRequest = new()
        {
            Name = "Banan",
            Price = invalidPrice
        };

        // ACT: 
        ServiceResult<Product> result = await _productService.SaveProductAsync(createRequest, CancellationToken.None);

        // ASSERT:
        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Null(result.Data);                        
        Assert.NotNull(result.ErrorMessage);       
        Assert.Contains("Pris måste vara större än 0.", result.ErrorMessage);

        _productRepoMock.Verify(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()), Times.Never);
    }


    // Happy path: 
        [Fact]
    public async Task UpdateProductAsync_ShouldUpdateProduct_WhenRequestIsValid()
    {
        // ARRANGE: 
        Product existingProduct = new()
        {
            Id = "1",
            Name = "Banan",
            Price = 6m,
            Category = new Category { Id = "10", Name = "Grönsaker" },
            Manufacturer = new Manufacturer { Id = "20", Name = "Bananträd" }
        };

        List<Product> productList = new() { existingProduct };

        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.OK(productList));

        _productRepoMock
            .Setup(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult.NoContent());

        _categoryRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Category>>.OK(
                new List<Category> { new Category { Id = "11", Name = "Frukt" } }));

        _manufacturerRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Manufacturer>>.OK(
                new List<Manufacturer> { new Manufacturer { Id = "21", Name = "Äppelträd" } }));

        ProductUpdateRequest updateRequest = new()
        {
            Id = "1",
            Name = "  Äpple  ",   
            Price = 8m,
            CategoryName = "  Frukt  ",
            ManufacturerName = "  Äppelträd  "
        };

        // ACT: 
        ServiceResult result = await _productService.UpdateProductAsync(updateRequest, CancellationToken.None);

        // ASSERT:  
        Assert.True(result.Succeeded);
        Assert.Equal(204, result.StatusCode);

        Assert.Equal("Äpple", existingProduct.Name);  
        Assert.Equal(8m, existingProduct.Price);
        Assert.NotNull(existingProduct.Category);
        Assert.Equal("Frukt", existingProduct.Category.Name);
        Assert.NotNull(existingProduct.Manufacturer);
        Assert.Equal("Äppelträd", existingProduct.Manufacturer.Name);

        _productRepoMock.Verify(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Negative case: 
    [Fact]
    public async Task UpdateProductAsync_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // ARRANGE: 
        List<Product> productList = new()
        {
            new Product { Id = "1", Name = "Banan", Price = 6m }
        };

        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.OK(productList));

        ProductUpdateRequest updateRequest = new()
        {
            Id = "2",                
            Name = "Äpple",
            Price = 8m
        };

        // ACT:
        ServiceResult result = await _productService.UpdateProductAsync(updateRequest, CancellationToken.None);

        // ASSERT:
        Assert.False(result.Succeeded);            
        Assert.Equal(404, result.StatusCode);      
        Assert.Equal("Produkten med Id 2 kunde inte hittas", result.ErrorMessage);
    }

    // Negative case: 
    [Fact]
    public async Task UpdateProductAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // ARRANGE: 
        ProductUpdateRequest updateRequest = new()
        {
            Id = "1",
            Name = "",
            Price = null
        };

        // ACT
        ServiceResult result = await _productService.UpdateProductAsync(updateRequest, CancellationToken.None);

        // ASSERT: 
        Assert.False(result.Succeeded);                
        Assert.Equal(400, result.StatusCode);         
        Assert.NotNull(result.ErrorMessage);           
        Assert.Contains("Namn måste anges.", result.ErrorMessage);   
        Assert.Contains("Pris måste anges.", result.ErrorMessage);   
    }

    // Negative case: 
    [Fact]
    public async Task UpdateProductAsync_ShouldReturnConflict_WhenDuplicateNameExists()
    {
        // ARRANGE:  
        List<Product> productList = new()
        {
            new Product { Id = "1", Name = "Banan", Price = 6m },
            new Product { Id = "2", Name = "Äpple", Price = 8m }
        };

        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.OK(productList));

        ProductUpdateRequest request = new ProductUpdateRequest
        {
            Id = "1",
            Name = "Äpple",     
            Price = 8m
        };

        // ACT
        ServiceResult result = await _productService.UpdateProductAsync(request, CancellationToken.None);

        // ASSERT: 
        Assert.False(result.Succeeded);                
        Assert.Equal(409, result.StatusCode);          
        Assert.NotNull(result.ErrorMessage);           
        Assert.Contains("En produkt med namnet Äpple finns redan.", result.ErrorMessage); 
    }



    // Happy path: 
    [Fact]
    public async Task DeleteProductAsync_ShouldRemoveProduct_WhenProductExists()
    {
        // ARRANGE: 
        Product existingProduct = new() { Id = "1", Name = "Banan", Price = 6m };

        // Simulera att ReadAsync returnerar en lista med produkten
        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.OK(new List<Product> { existingProduct }));

        _productRepoMock
            .Setup(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult.NoContent());

        // ACT: 
        ServiceResult result = await _productService.DeleteProductAsync("1", CancellationToken.None);

        // ASSERT: 
        Assert.NotNull(result); 
        Assert.True(result.Succeeded);
        Assert.Equal(204, result.StatusCode); 
        Assert.Null(result.ErrorMessage);

        _productRepoMock.Verify(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Negative case: 
    [Fact]
    public async Task DeleteProductAsync_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // ARRANGE: 
        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.OK(new List<Product>()));

        // ACT
        ServiceResult result = await _productService.DeleteProductAsync("3", CancellationToken.None);

        // ASSERT: 
        Assert.NotNull(result); 
        Assert.False(result.Succeeded); 
        Assert.Equal(404, result.StatusCode); 
        Assert.Equal("Produkten med Id 3 kunde inte hittas", result.ErrorMessage); 
    }

    // Negative case: 
    [Fact]
    public async Task DeleteProductAsync_ShouldReturnError_WhenWriteAsyncFails()
    {
        // ARRANGE: 
        Product existingProduct = new Product { Id = "1", Name = "Banan", Price = 6m };

        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.OK(new List<Product> { existingProduct }));

        _productRepoMock
            .Setup(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult.InternalServerError("Ett okänt fel uppstod vid filsparning"));

        // ACT
        ServiceResult result = await _productService.DeleteProductAsync("1", CancellationToken.None);

        // ASSERT: 
        Assert.NotNull(result); 
        Assert.False(result.Succeeded); 
        Assert.Equal(500, result.StatusCode);
        Assert.Equal("Ett okänt fel uppstod vid filsparning", result.ErrorMessage);
    }
}
