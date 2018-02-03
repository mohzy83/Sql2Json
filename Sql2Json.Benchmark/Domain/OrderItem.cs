using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Sql2Json.Benchmark.Domain
{
    [Table("order_items")]
    public class OrderItem
    {
        public int Id { get; set; }
        public string Item_Name { get; set; }
        public double Price { get; set; }
        [NotMapped]
        public int order_id { get; set; }
    }
}
