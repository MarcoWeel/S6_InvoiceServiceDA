using InvoiceService.Models;

namespace InvoiceService.Services.Interfaces;

using InvoiceService.Models;

public interface IProductService
{
    void SubscribeToGlobal();
    Task<List<Product>> GetProductsAsync();
    Task<Product> GetProductAsync(Guid id);
    Task<Product> UpdateProductAsync(Product updated);
    Task<Product> SaveProductAsync(Product product);
    Task DeleteProductAsync(Guid id);
}

