using ApplicationLayer.Interfaces;

namespace ApplicationLayer.DTOs;
public class ProductCreateRequest : IProductRequest
{
    public string Name { get; set; } = null!;
    public decimal? Price { get; set; }
}

