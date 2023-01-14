using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Data
{
    public class InvoiceServiceContext : DbContext
    {
        //public ShopServiceContext (DbContextOptions<ShopServiceContext> options)
        //    : base(options)
        //{
        //}
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(databaseName: "ShopDB");
        }

        public DbSet<Product> Product { get; set; } = default!;
        public DbSet<Material> Material { get; set; } = default!;

        public DbSet<Invoice> Invoice { get; set; } = default!;
    }
}
