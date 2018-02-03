using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Sql2Json.Benchmark.Domain
{
    [Table("customer")]
    public class Customer
    {
        public int Id { get; set; }
        public string SName { get; set; }
        public string FName { get; set; }
        public string ADR_STREET { get; set; }
        public string ADR_CITY { get; set; }
        [ForeignKey("customer_id")]
        public List<Invoice> Invoices { get; set; }
        [ForeignKey("customer_id")]
        public List<Order> Orders { get; set; }
        public DateTime Bday { get; set; }
       // public DateTime LastUpdate { get; set; }
    }
}

