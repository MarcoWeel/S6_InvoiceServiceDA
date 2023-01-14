using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceService.Models
{
    public class Invoice
    {
        [Column(TypeName = "char(36)")]
        [Key]
        public Guid Id { get; set; }
        public Guid UserGuid { get; set; }
        public double TotalPrice { get; set; }
        public List<Product> Products { get; set; }
    }
}
