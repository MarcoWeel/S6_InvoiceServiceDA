﻿using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using InvoiceService.Data;
using InvoiceService.Models;
using InvoiceService.Services.Interfaces;

namespace InvoiceService.dataaccess.Services;

public interface IDataAccessService
{
    void SubscribeToPersistence();
}

public class DataAccessService : IDataAccessService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessagingService _messagingService;

    public DataAccessService(IServiceProvider serviceProvider, IMessagingService messagingService)
    {
        _serviceProvider = serviceProvider;
        _messagingService = messagingService;

    }

    public void SubscribeToPersistence()
    {
        _messagingService.Subscribe("invoice-data",
            (BasicDeliverEventArgs ea, string queue, string request) => RouteCallback(ea, queue, request),
            ExchangeType.Topic, "*.*.request");
    }

    private async void RouteCallback(BasicDeliverEventArgs ea, string queue, string request)
    {
        using InvoiceServiceContext context =
            _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<InvoiceServiceContext>();

        string route = ea.RoutingKey.Replace("request", "response");

        string data = Encoding.UTF8.GetString(ea.Body.ToArray());
        string exchange = ea.Exchange;

        switch (request)
        {
            case "getAllInvoices":
                {
                    var invoices = await context.Invoice.ToListAsync();
                    var json = JsonConvert.SerializeObject(invoices);
                    byte[] message = Encoding.UTF8.GetBytes(json);

                    _messagingService.Publish(exchange, queue, route, request, message);

                    break;
                }
            case "getInvoiceById":
                {
                    Guid id = Guid.Parse(data);
                    var invoice = await context.Invoice.SingleOrDefaultAsync(m => m.Id == id);
                    var json = JsonConvert.SerializeObject(invoice);
                    byte[] message = Encoding.UTF8.GetBytes(json);

                    _messagingService.Publish(exchange, queue, route, request, message);

                    break;
                }
            case "addInvoice":
                {
                    var invoice = JsonConvert.DeserializeObject<Invoice>(data);
                    if (invoice == null)
                        break;

                    context.Add(invoice);
                    await context.SaveChangesAsync();

                    var newInvoice =
                        await context.Invoice.SingleOrDefaultAsync(m => m.Id == invoice.Id);
                    if (newInvoice == null)
                        break;
                    var json = JsonConvert.SerializeObject(newInvoice);
                    byte[] message = Encoding.UTF8.GetBytes(json);
                    _messagingService.Publish(exchange, queue, route, request, message);

                    break;
                }
            case "deleteInvoice":
                {
                    Guid id = Guid.Parse(data);

                    var invoice = await context.Invoice.SingleOrDefaultAsync(m => m.Id == id);
                    if (invoice == null)
                        return;

                    context.Invoice.Remove(invoice);
                    await context.SaveChangesAsync();
                    var json = JsonConvert.SerializeObject(invoice);
                    byte[] message = Encoding.UTF8.GetBytes(json);
                    _messagingService.Publish(exchange, queue, route, request, message);

                    break;
                }
            case "updateInvoice":
                {
                    var updatedInvoice = JsonConvert.DeserializeObject<Invoice>(data);
                    if (updatedInvoice == null)
                        break;

                    var oldInvoice = await context.Invoice.SingleOrDefaultAsync(m => m.Id == updatedInvoice.Id);
                    if (oldInvoice == null)
                        break;

                    oldInvoice.TotalPrice = updatedInvoice.TotalPrice;
                    oldInvoice.Products = updatedInvoice.Products;

                    await context.SaveChangesAsync();

                    var json = JsonConvert.SerializeObject(updatedInvoice);
                    byte[] message = Encoding.UTF8.GetBytes(json);
                    _messagingService.Publish(exchange, queue, route, request, message);

                    break;
                }
            default:
                Console.WriteLine($"Request {request} Not Found");
                break;

        }
    }
}
