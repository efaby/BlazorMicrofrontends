namespace Products.MicroFrontend.Services;

public interface IProductService
{
    Task<List<ProductDto>> GetProductsAsync();
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<bool> CreateProductAsync(ProductDto product);
}

public class ProductService : IProductService
{
    private readonly List<ProductDto> _products = new()
    {
        new ProductDto
        {
            Id = 1,
            Name = "Gaming Laptop Pro",
            Description = "High-end laptop for gaming and design",
            Price = 1299.99m,
            Stock = 15,
            Category = "Electronics"
        },
        new ProductDto
        {
            Id = 2,
            Name = "Wireless Mouse",
            Description = "Ergonomic mouse with 6 programmable buttons",
            Price = 49.99m,
            Stock = 50,
            Category = "Accessories"
        },
        new ProductDto
        {
            Id = 3,
            Name = "Mechanical Keyboard RGB",
            Description = "Mechanical keyboard with Cherry MX switches",
            Price = 129.99m,
            Stock = 30,
            Category = "Accessories"
        },
        new ProductDto
        {
            Id = 4,
            Name = "Wireless Mouse",
            Description = "Ergonomic mouse with 6 programmable buttons",
            Price = 49.99m,
            Stock = 50,
            Category = "Accessories"
        },
        new ProductDto
        {
            Id = 5,
            Name = "Mechanical Keyboard RGB",
            Description = "Mechanical keyboard with Cherry MX switches",
            Price = 129.99m,
            Stock = 30,
            Category = "Accessories"
        }
    };

    public Task<List<ProductDto>> GetProductsAsync()
    {
        return Task.FromResult(_products);
    }

    public Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(product);
    }

    public Task<bool> CreateProductAsync(ProductDto product)
    {
        product.Id = _products.Max(p => p.Id) + 1;
        _products.Add(product);
        return Task.FromResult(true);
    }
}

public class ProductDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public required string Category { get; set; }
}