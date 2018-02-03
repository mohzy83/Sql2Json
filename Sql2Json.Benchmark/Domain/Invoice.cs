using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Sql2Json.Benchmark.Domain
{
    public class Invoice
    {
        public int Id { get; set; }
        public double Amount { get; set; }
        public DateTime Inv_date { get; set; }
        [NotMapped]
        public int customer_id { get; set; }
    }
}
