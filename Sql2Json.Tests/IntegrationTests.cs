using Microsoft.Data.Sqlite;
using Sql2Json.Core;
using Sql2Json.Core.Definition;
using Sql2Json.Core.Engine;
using Sql2Json.Core.MappingBuilder;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Sql2Json.Tests
{
    public class IntegrationTests
    {

        public IntegrationTests()
        {
            CreateTestDataBase();
        }

        /// <summary>
        /// ZERO WIDTH NO-BREAK SPACE + JSON
        /// </summary>
        const string nestedResultsGeneratedJson = "\uFEFF" + "{\"Version\":\"2.0\",\"Results\":[{\"Id\":1,\"SName\":\"Meyers\",\"LastUpdate\":\"2017-03-01\",\"Birthday\":\"1983-10-31 00:00:00.000\",\"Address\":{\"Street\":\"Street 1\",\"City\":\"Las Vegas\"},\"Invoices\":[{\"InvoiceId\":1,\"Inv_Date\":\"2015-10-12 15:43:00.000\",\"TotalAmount\":500.2},{\"InvoiceId\":2,\"Inv_Date\":\"2016-05-09 15:43:00.000\",\"TotalAmount\":155.2}],\"Orders\":[{\"OrderId\":3,\"ArticleName\":\"Book X\"},{\"OrderId\":2,\"ArticleName\":\"CD Burner\"},{\"OrderId\":1,\"ArticleName\":\"USB Stick\"}]},{\"Id\":2,\"SName\":\"Carter\",\"LastUpdate\":\"2017-03-01\",\"Birthday\":\"1975-08-08 00:00:00.000\",\"Address\":{\"Street\":\"Dummy Road\",\"City\":\"Sao Paulo\"},\"Invoices\":[{\"InvoiceId\":4,\"Inv_Date\":\"2015-10-12 15:43:00.000\",\"TotalAmount\":1544.2}]}]}";
        const string nestedSelectGeneratedJson = "\uFEFF" + "[{\"Id\":1,\"SName\":\"Meyers\",\"Age\":22,\"LastUpdate\":\"2017-03-01\",\"Birthday\":\"1983-10-31 00:00:00.000\",\"Address\":{\"Street\":\"Street 1\",\"City\":\"Las Vegas\"},\"Invoices\":[{\"InvoiceId\":1,\"Inv_Date\":\"2015-10-12 15:43:00.000\",\"TotalAmount\":500.2},{\"InvoiceId\":2,\"Inv_Date\":\"2016-05-09 15:43:00.000\",\"TotalAmount\":155.2}],\"Orders\":[{\"OrderId\":1,\"ArticleName\":\"USB Stick\"},{\"OrderId\":2,\"ArticleName\":\"CD Burner\"},{\"OrderId\":3,\"ArticleName\":\"Book X\"}]},{\"Id\":2,\"SName\":\"Carter\",\"Age\":22,\"LastUpdate\":\"2017-03-01\",\"Birthday\":\"1975-08-08 00:00:00.000\",\"Address\":{\"Street\":\"Dummy Road\",\"City\":\"Sao Paulo\"},\"Invoices\":[{\"InvoiceId\":4,\"Inv_Date\":\"2015-10-12 15:43:00.000\",\"TotalAmount\":1544.2}]}]";
        private void CreateTestDataBase()
        {
            File.Delete("Test.db");
            using (var connection = new SqliteConnection("Data Source=Test.db"))
            {
                connection.Open();
                var command = connection.CreateCommand();

                #region customer table

                command.CommandText = "CREATE TABLE IF NOT EXISTS customer ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, sname VARCHAR(100) NOT NULL, fname VARCHAR(100) NOT NULL,BDAY text,ADR_STREET VARCHAR(100), ADR_CITY VARCHAR(100));";
                command.ExecuteNonQuery();

                // table customer
                command.CommandText = "INSERT INTO customer (id, sname,fname,BDAY,ADR_STREET,ADR_CITY) VALUES(NULL, 'Meyers','Mike','1983-10-31 00:00:00.000', 'Street 1', 'Las Vegas')";
                command.ExecuteNonQuery();
                command.CommandText = "INSERT INTO customer (id, sname,fname,BDAY,ADR_STREET,ADR_CITY) VALUES(NULL, 'Carter','Jonny','1975-08-08 00:00:00.000', 'Dummy Road', 'Sao Paulo')";
                command.ExecuteNonQuery();
                #endregion

                #region invoices table
                // table invoices
                command.CommandText = "CREATE TABLE IF NOT EXISTS invoices ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, inv_date text, amount REAL, customer_id INTEGER);";
                command.ExecuteNonQuery();

                // test data
                command.CommandText = "INSERT INTO invoices (id, inv_date,amount,customer_id) VALUES(NULL, '2015-10-12 15:43:00.000', 500.2, 1)";
                command.ExecuteNonQuery();
                command.CommandText = "INSERT INTO invoices (id, inv_date,amount,customer_id) VALUES(NULL, '2016-05-09 15:43:00.000', 155.2, 1)";
                command.ExecuteNonQuery();
                command.CommandText = "INSERT INTO invoices (id, inv_date,amount,customer_id) VALUES(NULL, '2015-05-09 15:43:00.000', 77, 1)";
                command.ExecuteNonQuery();

                command.CommandText = "INSERT INTO invoices (id, inv_date,amount,customer_id) VALUES(NULL, '2015-10-12 15:43:00.000', 1544.2, 2)";
                command.ExecuteNonQuery();
                command.CommandText = "INSERT INTO invoices (id, inv_date,amount,customer_id) VALUES(NULL, '2016-05-09 15:43:00.000', 75.2, 2)";
                command.ExecuteNonQuery();

                #endregion

                #region orders table
                // table orders
                command.CommandText = " CREATE TABLE [orders] (   [id] INTEGER NOT NULL , [ordernumber] TEXT , [article] TEXT , [customer_id] INTEGER , CONSTRAINT [PK_orders] PRIMARY KEY ([id]) ); ";
                command.ExecuteNonQuery();
                // test data
                command.CommandText = "INSERT INTO [orders] ([id],[ordernumber],[article],[customer_id]) VALUES (1,'1','USB Stick',1);";
                command.ExecuteNonQuery();
                command.CommandText = "INSERT INTO [orders] ([id],[ordernumber],[article],[customer_id]) VALUES (2,'1002','CD Burner',1);";
                command.ExecuteNonQuery();
                command.CommandText = "INSERT INTO [orders] ([id],[ordernumber],[article],[customer_id]) VALUES (3,'1003','Book X',1);";
                command.ExecuteNonQuery();
                #endregion

                // close command
                command.Dispose();


            }
        }

        [Fact]
        public void TestMappingBuilderObjectAsRoot()
        {
            var mapping = JsonMappingBuilder.Root()
                .Property("Version", "2.0")
                .QueryWithNesting("Results",
                        @"select c.id as cid, sname,'2017-03-01' as LastUpdate, fname, BDAY, ADR_STREET, ADR_CITY, i.id as iid, i.inv_date, i.amount, o.article, o.id as oid
                        from customer c
                        left join orders o on c.id = o.customer_id
                        left join invoices i on c.id = i.customer_id
                        where i.amount > ${context:min_amount}",
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
                    )
                 )
                //.Property("Last-Update", "2010-10-10")
                .Result;

            using (var engine = new MappingEngine(() => new SqliteConnection("Data Source=Test.db"), mapping))
            {
                using (var ms = new MemoryStream())
                {
                    var context = new Dictionary<string, object>();
                    context.Add("min_amount", 100);
                    engine.ExecuteMapping(ms, context, false);
                    var json = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                    Assert.Equal(nestedResultsGeneratedJson, json);
                }
            }
        }

        [Fact]
        public void TestMappingBuilderQueryAsRoot()
        {
            var mapping = JsonMappingBuilder.RootQueryWithNesting(
                        @"select c.id as cid, sname,'2017-03-01' as LastUpdate, fname, BDAY, ADR_STREET, ADR_CITY, i.id as iid, i.inv_date, i.amount, o.article, o.id as oid
                        from customer c
                        left join orders o on c.id = o.customer_id
                        left join invoices i on c.id = i.customer_id
                        where i.amount > ${context:min_amount}
                        order by c.id,o.id ",
                        // Id Column
                        "cid"
                , cfg1 => cfg1
                   .Column("Id", "cid")
                   .Column("SName")
                   .PropertyResolver("Age", typeof(AgeResolver))
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
                    )
                 ).Result
                ;

            using (var engine = new MappingEngine(() => new SqliteConnection("Data Source=Test.db"), mapping))
            {
                using (var ms = new MemoryStream())
                {
                    var context = new Dictionary<string, object>();
                    context.Add("min_amount", 100);
                    engine.ExecuteMapping(ms, context, false);
                    var json = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                    Assert.Equal(nestedSelectGeneratedJson, json);
                }
            }
        }

        [Fact]
        public void TestNestedResultsMappingAnonymousType()
        {
            TemplateDefinition nestedResultsMapping = Define.Template(
                            new
                            {
                                Version = "2.0",
                                Results =
                                    Define.QueryWithNestedResults("cid",
                                    @"select c.id as cid, sname,'2017-03-01' as LastUpdate, fname, BDAY, ADR_STREET, ADR_CITY, i.id as iid, i.inv_date, i.amount, o.article, o.id as oid
                                                    from customer c
                                                    left join orders o on c.id = o.customer_id
                                                    left join invoices i on c.id = i.customer_id
                                                    where i.amount > ${context:min_amount}"
                                    ,
                                        new
                                        {
                                            Id = Define.Column("cid"),
                                            SName = Define.Column(),
                                            LastUpdate = Define.Column(),
                                            Birthday = Define.Column("BDAY"),
                                            Address = new
                                            {
                                                Street = Define.Column("ADR_STREET"),
                                                City = Define.Column("ADR_CITY"),
                                            },
                                            Invoices = Define.NestedResults("iid",
                                                new
                                                {
                                                    InvoiceId = Define.Column("iid"),
                                                    Inv_Date = Define.Column(),
                                                    TotalAmount = Define.Column("amount"),
                                                }
                                            ),
                                            Orders = Define.NestedResults("oid",
                                                new
                                                {
                                                    OrderId = Define.Column("oid"),
                                                    ArticleName = Define.Column("article"),
                                                }
                                            )
                                        }
                                    )
                            });

            // Compile Mapping

            var compiledMapping = nestedResultsMapping.Compile();

            // Execute Mapping

            using (var engine = new MappingEngine(() => new SqliteConnection("Data Source=Test.db"), compiledMapping))

            using (var ms = new MemoryStream())
            {
                var context = new Dictionary<string, object>();
                context.Add("min_amount", 100);
                engine.ExecuteMapping(ms, context, false);
                var json = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                Assert.Equal(nestedResultsGeneratedJson, json);
            }


        }

        [Fact]
        public void TestNestedSelectMappingTestAnonymousType()
        {
            QueryDefinition nestedSelectMapping = Define.QueryResult("select id as cid, sname,'2017-03-01' as LastUpdate, fname, BDAY, ADR_STREET, ADR_CITY from customer",
                new
                {
                    Id = Define.Column("cid"),
                    SName = Define.Column(),
                    Age = 22,
                    LastUpdate = Define.Column(),
                    Birthday = Define.Column("BDAY"),
                    Address = new
                    {
                        Street = Define.Column("ADR_STREET"),
                        City = Define.Column("ADR_CITY"),
                    },
                    Invoices = Define.QueryResult("select id as iid, inv_date, amount from invoices where customer_id=${column:cid} and amount > ${context:min_amount}",
                        new
                        {
                            InvoiceId = Define.Column("iid"),
                            Inv_Date = Define.Column(),
                            TotalAmount = Define.Column("amount"),
                        }
                    ),
                    Orders = Define.QueryResult("select id as oid, article from orders where customer_id=${column:cid}",
                        new
                        {
                            OrderId = Define.Column("oid"),
                            ArticleName = Define.Column("article"),
                        }
                    )
                });

            var compiledMapping = nestedSelectMapping.Compile();

            // Execute Mapping

            using (var engine = new MappingEngine(() => new SqliteConnection("Data Source=Test.db"), compiledMapping))
            {
                using (var ms = new MemoryStream())
                {
                    var context = new Dictionary<string, object>();
                    context.Add("min_amount", 100);
                    engine.ExecuteMapping(ms, context, false);
                    var json = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                    Assert.Equal(nestedSelectGeneratedJson, json);
                }
            }

        }
    }


    internal class AgeResolver : ICustomValueResolver
    {
        public object ResolveValueFromRow(IDictionary<string, object> row, IDictionary<string, object> context)
        {
            return 22;
        }
    }
}
