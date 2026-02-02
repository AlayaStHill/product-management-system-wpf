using ApplicationLayer.Interfaces;

namespace ApplicationLayer.DTOs;
public class ProductUpdateRequest : IProductRequest
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal? Price { get; set; }
    public string? CategoryName { get; set; } 
    public string? ManufacturerName { get; set; } 
}