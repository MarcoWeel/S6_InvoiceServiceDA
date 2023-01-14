using InvoiceService.Models;

namespace InvoiceService.Services.Interfaces
{
    public interface IInvoiceService
    {
        void SubscribeToGlobal();
        Task<List<Invoice>> GetInvoicesAsync();
        Task<Invoice> GetInvoiceAsync(Guid id);
        Task<Invoice> UpdateInvoiceAsync(Invoice updated);
        Task<Invoice> SaveInvoiceAsync(Invoice material);
        Task DeleteInvoiceAsync(Guid id);
    }
}
