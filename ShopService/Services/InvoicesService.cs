using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using InvoiceService.Data;
using InvoiceService.Models;
using InvoiceService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Services
{
    public class InvoicesService : IInvoiceService
    {
        private readonly IMessagingService _messagingService;
        public InvoicesService(IMessagingService messagingService)
        {
            _messagingService = messagingService;
        }
        public void SubscribeToGlobal()
        {
            _messagingService.Subscribe("invoice", (BasicDeliverEventArgs ea, string queue, string request) => RouteCallback(ea, request), ExchangeType.Fanout, "*");
        }

        private static async void RouteCallback(BasicDeliverEventArgs ea, string request)
        {
            using InvoiceServiceContext context = new();

            string data = Encoding.UTF8.GetString(ea.Body.ToArray());

            switch (request)
            {
                case "addInvoice":
                    {
                        var invoice = JsonConvert.DeserializeObject<Invoice>(data);
                        if (invoice == null)
                            return;

                        var existing = await context.Invoice.SingleOrDefaultAsync(m => m.Id == invoice.Id);
                        if (existing != null)
                            return;

                        context.Add(invoice);
                        await context.SaveChangesAsync();

                        break;
                    }
                case "deleteInvoice":
                    {
                        Guid id = Guid.Parse(data);
                        Invoice invoice = await context.Invoice.SingleOrDefaultAsync(m => m.Id == id);
                        if (invoice == null)
                            return;
                        context.Invoice.Remove(invoice);
                        await context.SaveChangesAsync();
                        break;
                    };
                case "updateInvoice":
                    {
                        var invoice = JsonConvert.DeserializeObject<Invoice>(data);
                        if (invoice == null)
                            return;

                        var existing = await context.Invoice.SingleOrDefaultAsync(m => m.Id == invoice.Id);
                        if (existing == null) context.Add(invoice);
                        else
                        {
                            context.Invoice.Update(invoice);
                        }
                        await context.SaveChangesAsync();
                        break;
                    }
                default:
                    Console.WriteLine($"Request {request} Not Found");
                    break;
            }
        }
        public async Task<Invoice> GetInvoiceAsync(Guid id)
        {
            using InvoiceServiceContext context = new();

            var invoice = await context.Invoice.SingleOrDefaultAsync(m => m.Id == id);

            if (invoice != null)
                return invoice;

            string response = await _messagingService.PublishAndRetrieve("invoice-data", "getInvoiceById", Encoding.UTF8.GetBytes(id.ToString()));

            invoice = JsonConvert.DeserializeObject<Invoice>(response);
            if (invoice == null)
                return null;

            try
            {
                context.Add(invoice);
                await context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine("Already added");
            }


            return invoice;
        }

        public async Task<List<Invoice>> GetInvoicesAsync()
        {
            using InvoiceServiceContext context = new();

            if (!hasallInvoices)
                await RetrieveAllInvoices(context);

            return await context.Invoice.ToListAsync();
        }

        public async Task<Invoice> SaveInvoiceAsync(Invoice invoice)
        {
            invoice.Id = Guid.NewGuid();
            using InvoiceServiceContext context = new();


            var existing = await context.Invoice.SingleOrDefaultAsync(m => m.Id == invoice.Id);
            if (existing != null)
                return null;

            string response = await _messagingService.PublishAndRetrieve("invoice-data", "addInvoice", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(invoice)));

            invoice = JsonConvert.DeserializeObject<Invoice>(response);
            if (invoice == null)
                return null;

            _messagingService.Publish("invoice", "invoice-messaging", "addInvoice", "addInvoice", Encoding.UTF8.GetBytes(response));

            return invoice;
        }


        public async Task<Invoice> UpdateInvoiceAsync(Invoice updated)
        {
            var response = await _messagingService.PublishAndRetrieve("invoice-data", "updateInvoice", Encoding.UTF8.GetBytes(updated.Id.ToString()));
            if (response == null)
                return null;

            _messagingService.Publish("invoice", "invoice-messaging", "updateInvoice", "updateInvoice", Encoding.UTF8.GetBytes(updated.Id.ToString()));

            return updated;
        }

        private bool hasallInvoices = false;
        private Task gettingInvoices;
        private async Task RetrieveAllInvoices(InvoiceServiceContext context)
        {
            try
            {
                string response = await _messagingService.PublishAndRetrieve("invoice-data", "getAllInvoices");
                List<Invoice> invoices = JsonConvert.DeserializeObject<List<Invoice>>(response);
                foreach (Invoice invoice in invoices)
                {
                    bool existing = context.Invoice.FirstOrDefault(e => e.Id == invoice.Id) != null;
                    if (!existing)
                        context.Invoice.Add(invoice);
                }
                await context.SaveChangesAsync();
                await Task.Delay(1000);
                hasallInvoices = true;
            }
            catch (Exception ex)
            {
                gettingInvoices = null;
                throw new Exception(ex.Message);
            }
        }

        public async Task DeleteInvoiceAsync(Guid id)
        {
            var response = await _messagingService.PublishAndRetrieve("invoice-data", "deleteInvoice", Encoding.UTF8.GetBytes(id.ToString()));

            _messagingService.Publish("invoice", "invoice-messaging", "deleteInvoice", "deleteInvoice", Encoding.UTF8.GetBytes(id.ToString()));
        }
    }
}
