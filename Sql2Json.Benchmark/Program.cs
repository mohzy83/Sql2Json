using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Newtonsoft.Json;
using Npgsql;
using Npgsql.Logging;
using Sql2Json.Benchmark.Domain;
using Sql2Json.Core.Engine;
using Sql2Json.Core.MappingBuilder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Sql2Json.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Running Benchmarks");
            //   NpgsqlLogManager.Provider = new ConsoleLoggingProvider(NpgsqlLogLevel.Debug, true, true);
            const int size = 500;
            CreateTables(size, size * 1);
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine("----------------------------------");
                Console.WriteLine("Round " + (i + 1));
                RunNativSqlBenchmark();
            //    RunDapperBenchmark();
                RunEfBenchmark();
                RunMappingEngineNestedResultsBenchmark();
                RunMappingEngineWithSubSelectsBenchmark();
            }
            Console.ReadKey();
        }

        private static void CreateTables(int entries, int idOffset)
        {
            NpgsqlConnection conn = new NpgsqlConnection("Server=localhost;User Id=postgres;" +
                               "Password=admin;Database=test;");
            conn.Open();

            var command = conn.CreateCommand();
            command.CommandText = "CREATE TABLE IF NOT EXISTS customer ( id INTEGER NOT NULL PRIMARY KEY, sname varchar(100) NOT NULL, fname varchar(100) NOT NULL, BDAY date, ADR_STREET varchar(100), ADR_CITY varchar(100));";
            command.ExecuteNonQuery();
            command.CommandText = "DELETE FROM customer";
            command.ExecuteNonQuery();

            command.CommandText = "CREATE TABLE IF NOT EXISTS invoices ( id INTEGER NOT NULL PRIMARY KEY, inv_date date, amount REAL, customer_id INTEGER);";
            command.ExecuteNonQuery();
            command.CommandText = "CREATE INDEX IF NOT EXISTS invoices_cust_id ON invoices (customer_id);";
            command.ExecuteNonQuery();

            command.CommandText = "DELETE FROM invoices";
            command.ExecuteNonQuery();

            command.CommandText = "CREATE TABLE IF NOT EXISTS orders ( id INTEGER NOT NULL PRIMARY KEY, ordernumber varchar(100), article varchar(100), customer_id INTEGER);";
            command.ExecuteNonQuery();
            command.CommandText = "CREATE INDEX IF NOT EXISTS orders_cust_id ON orders (customer_id);";
            command.ExecuteNonQuery();

            command.CommandText = "DELETE FROM orders";
            command.ExecuteNonQuery();

            command.CommandText = "CREATE TABLE IF NOT EXISTS order_items ( id INTEGER NOT NULL PRIMARY KEY, item_name varchar(100), price REAL, order_id INTEGER);";
            command.ExecuteNonQuery();
            command.CommandText = "CREATE INDEX IF NOT EXISTS order_items_o_id ON order_items (order_id);";
            command.ExecuteNonQuery();
            command.CommandText = "DELETE FROM order_items";
            command.ExecuteNonQuery();


            for (int i = idOffset; i < entries + idOffset; i++)
            {
                command.CommandText = string.Format("INSERT INTO customer (id, sname,fname,BDAY,ADR_STREET,ADR_CITY)  VALUES({0}, '{1}','{2}','{3}', '{4}', '{5}')", i + 1, "Jens", "Müller", "1988-10-31", "Street " + i.ToString(), "Los Angeles");
                command.ExecuteNonQuery();
                for (int k = 0; k < 4; k++)
                {
                    command.CommandText = string.Format("INSERT INTO invoices (id, inv_date,amount,customer_id) VALUES({0}, '{1}',{2},{3})", (i + 1).ToString() + (k + 1).ToString(), "2017-12-12", i*2, i + 1);
                    command.ExecuteNonQuery();
                }

                for (int j = 0; j < 8; j++)
                {
                    command.CommandText = string.Format("INSERT INTO orders (id, ordernumber, article, customer_id) VALUES({0}, '{1}','{2}',{3})", (i + 1).ToString() + (j + 1).ToString(), "O-155522-GGGG", "USB STICK", i + 1);
                    command.ExecuteNonQuery();

                    for (int l = 0; l < 4; l++)
                    {
                        var oiid = (i + 1).ToString() + (j + 1).ToString() + (l + 1).ToString();
                        command.CommandText = string.Format("INSERT INTO order_items (id, item_name, price, order_id) VALUES({0}, '{1}',{2},{3})", oiid, "Book abc - " + oiid, 4899, (i + 1).ToString() + (j + 1).ToString());
                        command.ExecuteNonQuery();
                    }

                }

            }
            command.Dispose();
            conn.Close();
        }

        private static void RunMappingEngineNestedResultsBenchmark()
        {
            var mapping = JsonMappingBuilder.Root()
                .Property("Version", "2.0")
                .QueryWithNesting("Results",
                        @"select c.id as cid, c.sname,'2017-03-01' as LastUpdate, c.fname, c.BDAY, c.ADR_STREET, c.ADR_CITY, i.id as iid, i.inv_date, i.amount, o.article, o.id as oid, oi.item_name,oi.price, oi.id as oiid
                                    from customer c
                                    left join invoices i on c.id = i.customer_id
                                    left join orders o on c.id = o.customer_id
                                    left join order_items oi on o.id = oi.order_id
                                    ", //where amount>${context:min_amount:Int32}
                                       // Id Column
                        "cid"
                , cfg1 => cfg1
                   .Column("Id", "cid")
                   .Column("SName")
                   .Column("LastUpdate")
                   .Column("Birthday", "BDAY")
                   .Object("Address", cfg2 => cfg2
                        .Column("Street", "ADR_STREET")
                        .Column("City", "ADR_CITY")
                    )
                   .NestedResults("Invoices", "iid", cfg2 => cfg2
                        .Column("InvoiceId", "iid")
                        .Column("Inv_Date")
                        .Column("TotalAmount", "amount")
                    )
                   .NestedResults("Orders", "oid", cfg2 => cfg2
                        .Column("OrderId", "oid")
                        .Column("ArticleName", "article")
                        .NestedResults("Items", "oiid", cfg3 => cfg3
                             .Column("Item_Name")
                             .Column("Price")
                        )
                    )
                 )
                //.Property("Last-Update", "2010-10-10")
                .Result;


            var sw = new Stopwatch();
            sw.Start();

            using (var engine = new MappingEngine(() => new NpgsqlConnection("Server=localhost;User Id=postgres;" +
                               "Password=admin;Database=test;"), mapping))
            {
                using (var ms = new FileStream("out1.json", FileMode.Create))
                {
                    var context = new Dictionary<string, object>();
              //      context.Add("min_amount", 10);
                    engine.ExecuteMapping(ms, context, false);
                }
            }

            sw.Stop();
            Console.WriteLine("RunMappingEngineNestedResultsBenchmark time elapsed: " + sw.ElapsedMilliseconds);
        }

        private static void RunMappingEngineWithSubSelectsBenchmark()
        {
            var mapping = JsonMappingBuilder.Root()
                .Property("Version", "2.0")
                .Query("Results",
                        @"select c.id as cid, c.sname,'2017-03-01' as LastUpdate, c.fname, c.BDAY, c.ADR_STREET, c.ADR_CITY
                                    from customer c"
                , cfg1 => cfg1
                   .Column("Id", "cid")
                   .Column("SName")
                   .Column("LastUpdate")
                   .Column("Birthday", "BDAY")
                   .Object("Address", cfg2 => cfg2
                        .Column("Street", "ADR_STREET")
                        .Column("City", "ADR_CITY")
                    )
                   .Query("Invoices", "select i.id as iid, i.inv_date, i.amount from invoices i where i.customer_id=${column:cid:Int32}", cfg2 => cfg2
                        .Column("InvoiceId", "iid")
                        .Column("Inv_Date")
                        .Column("TotalAmount", "amount")
                    )
                   .Query("Orders", "select o.article, o.id as oid from orders o where o.customer_id=${column:cid:Int32}", cfg2 => cfg2
                        .Column("OrderId", "oid")
                        .Column("ArticleName", "article")
                        .Query("Items", "select oi.item_name, oi.price from order_items oi where oi.order_id=${column:oid:Int32}", cfg3 => cfg3
                            .Column("item_name")
                            .Column("price")
                        )

                    )
                 )
                //.Property("Last-Update", "2010-10-10")
                .Result;


            var sw = new Stopwatch();
            sw.Start();

            using (var engine = new MappingEngine(() => new NpgsqlConnection("Server=localhost;User Id=postgres;" +
                               "Password=admin;Database=test;"), mapping))
            {
                using (var ms = new FileStream("out3.json", FileMode.Create))
                {
                    var context = new Dictionary<string, object>();
                    //  context.Add("min_amount", 100);
                    engine.ExecuteMapping(ms, context, false);
                }
            }

            sw.Stop();
            Console.WriteLine("RunMappingEngineWithSubSelectsBenchmark time elapsed: " + sw.ElapsedMilliseconds);
        }

        private static void RunNativSqlBenchmark()
        {
            var conn2 = new NpgsqlConnection("Server=localhost;User Id=postgres;" +
                   "Password=admin;Database=test;");
            conn2.Open();
            var sw = new Stopwatch();
            sw.Start();

            var command = conn2.CreateCommand();
            command.CommandText =
                @"select row_to_json(cu) 
	                    from (select c.id as cid, c.sname,'2017-03-01' as LastUpdate, c.fname, c.BDAY, c.ADR_STREET, c.ADR_CITY,
                          (select json_agg(inv) from (select i.id, i.inv_date, i.amount from invoices as i  where i.customer_id=c.id) as inv  ) as invoices,
                          (select json_agg(ord) from (select o.id, o.article ,      (select json_agg(itm) from (select oi.item_name, oi.price from order_items as oi  where oi.order_id=o.id) as itm  ) as items          from orders as o  where o.customer_id=c.id) as ord  ) as orders
                    from customer c) as cu";
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    sb.Append(reader[0] as string);
                    sb.Append(",");
                }
            }
            sb.Append("]");
            File.WriteAllText("ou2.json", sb.ToString());
            command.Dispose();
            sw.Stop();
            Console.WriteLine("RunNativSqlBenchmark time elapsed: " + sw.ElapsedMilliseconds);
            conn2.Close();
        }

        private static void RunDapperBenchmark()
        {
            var sw = new Stopwatch();
            sw.Start();

            NpgsqlConnection conn = new NpgsqlConnection("Server=localhost;User Id=postgres;" +
                   "Password=admin;Database=test;");
            conn.Open();

            var customers = conn.Query<Customer>(@"select c.id , c.sname,'2017-03-01' as LastUpdate, c.fname, c.BDAY, c.ADR_STREET, c.ADR_CITY
                                    from customer c order by c.id").ToList();
            var invoices = conn.Query<Invoice>("select i.id, i.amount, i.customer_id from invoices i Order by customer_id"/*, new { Id = customer.Id }*/).ToList();
            var orders = conn.Query<Order>("select o.id, o.article, o.customer_id from orders o Order by customer_id"/*, new { Id = customer.Id }*/).ToList();
            var orderItems = conn.Query<OrderItem>("select oi.item_name, oi.price, oi.order_id from order_items oi Order by oi.order_id" /*, new { Id = order.Id }*/).ToList();






            int index = 0;
            int key = invoices.ElementAt(index).customer_id;

            foreach (var cust in customers)
            {

                while(key < cust.Id)
                {
                    index++;
                    key = invoices.ElementAt(index).customer_id;
                }
            //    if(key== cust.Id) cust.Invoices = grouped.ElementAt(index).vale


            }


            conn.Close();
            conn.Dispose();


            File.WriteAllText("out4.json", JsonConvert.SerializeObject(customers));

            sw.Stop();
            Console.WriteLine("RunDapperBenchmark time elapsed: " + sw.ElapsedMilliseconds);
        }

        private static void RunEfBenchmark()
        {
            var sw = new Stopwatch();
            sw.Start();

            var total = 10000;
            var pageSize = 15;
            for (int i = 0; i < total / pageSize; i++)
            {
                using (var context = new EfContext())
                {
                    var customers = context.Customers
                    .Include(c => c.Invoices)
                    .Include(c => c.Orders)
                        .ThenInclude(order => order.Items)
                        .Skip(i * pageSize).Take(pageSize)
                    .ToList();
                    File.WriteAllText("out5" + i.ToString() + ".json", JsonConvert.SerializeObject(customers));
                }
            }

            sw.Stop();
            Console.WriteLine("RunEfBenchmark time elapsed: " + sw.ElapsedMilliseconds);
        }
    }

    public class EfContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Server=localhost;User Id=postgres;" +
                               "Password=admin;Database=test;").ReplaceService<ISqlGenerationHelper, SqlGenerationHelper>();
        }
    }

    public class SqlGenerationHelper : NpgsqlSqlGenerationHelper
    {
        public override string DelimitIdentifier(string identifier) => identifier.Contains(".") ? base.DelimitIdentifier(identifier) : identifier;
    }
}
