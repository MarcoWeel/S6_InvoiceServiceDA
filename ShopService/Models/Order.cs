using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InvoiceService.Models;

namespace InvoiceDAService.Models
{
    public class Order
    {
        public Guid Id { get; set; }
        public Guid UserGuid { get; set; }
        public double TotalPrice { get; set; }
        public List<Product> Products { get; set; }
    }
}
