using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Sql2Json.Benchmark.Domain
{
    public class Order
    {
        public int Id { get; set; }
        public string Article { get; set; }
        [ForeignKey("order_id")]
        public List<OrderItem> Items { get; set; }
        [NotMapped]
        public int customer_id { get; set; }
    }
}
