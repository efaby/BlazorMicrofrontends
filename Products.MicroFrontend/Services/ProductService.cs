using Shared.Contracts.Http;
using Shared.Contracts.Authentication;
using Products.MicroFrontend.Models;

namespace Products.MicroFrontend.Services;

public interface IProductService
{
    Task<List<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(string id);
    Task<Product> CreateProductAsync(Product product);
    Task UpdateProductAsync(string id, Product product);
    Task DeleteProductAsync(string id);
}

public class ProductService : IProductService
{
    private readonly AuthenticatedHttpClient _http;
    private readonly SharedAuthenticationStateProvider _authStateProvider;
    private readonly ILogger<ProductService> _logger;

    // ✅ Inyectar AMBOS: HttpClient Y AuthStateProvider
    public ProductService(
        AuthenticatedHttpClient http,
        SharedAuthenticationStateProvider authStateProvider,
        ILogger<ProductService> logger)
    {
        _http = http;
        _authStateProvider = authStateProvider;
        _logger = logger;
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
        try
        {
            // ✅ OPCIÓN 1: Obtener token explícitamente si lo necesitas
            var token = await _authStateProvider.GetTokenAsync();
            _logger.LogInformation("Getting products with token: {Token}", token?[..20] + "...");

            // ✅ OPCIÓN 2: Usar AuthenticatedHttpClient (token automático)
            // El token se agrega automáticamente al header Authorization
            var products = await _http.GetAsync<List<Product>>("products");

            _logger.LogInformation("Retrieved {Count} products", products?.Count ?? 0);
            return products ?? new List<Product>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error getting products from API");
            throw;
        }
    }

    public async Task<Product?> GetProductByIdAsync(string id)
    {
        try
        {
            // ✅ El token se incluye automáticamente
            return await _http.GetAsync<Product>($"products/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Product {Id} not found", id);
            return null;
        }
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        try
        {
            // ✅ Verificar que el usuario esté autenticado antes de crear
            var isAuthenticated = await _authStateProvider.IsAuthenticatedAsync();
            if (!isAuthenticated)
            {
                throw new UnauthorizedAccessException("User must be authenticated to create products");
            }

            // ✅ Obtener info del usuario para logging
            var userInfo = await _authStateProvider.GetUserInfoAsync();
            _logger.LogInformation("User {Username} creating product: {ProductName}",
                userInfo?.Username, product.Name);

            // POST con token automático
            var created = await _http.PostAsync<Product, Product>("products", product);

            _logger.LogInformation("Created product {Id}: {Name}", created?.Id, created?.Name);
            return created!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            throw;
        }
    }

    public async Task UpdateProductAsync(string id, Product product)
    {
        try
        {
            // ✅ Verificar permisos/roles si es necesario
            var userInfo = await _authStateProvider.GetUserInfoAsync();
            if (userInfo?.Roles?.Contains("Admin") != true)
            {
                throw new UnauthorizedAccessException("Only admins can update products");
            }

            // PUT con token automático
            await _http.PutAsync<Product, Product>($"products/{id}", product);

            _logger.LogInformation("Updated product {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {Id}", id);
            throw;
        }
    }

    public async Task DeleteProductAsync(string id)
    {
        try
        {
            // DELETE con token automático
            await _http.DeleteAsync($"products/{id}");

            _logger.LogInformation("Deleted product {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {Id}", id);
            throw;
        }
    }
}
