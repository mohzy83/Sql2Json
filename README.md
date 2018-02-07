# Sql2Json

## Current Version 1.0.0

for dotnet core 1.0, 1.1 and .NET Framework 4.0 and higher


## Nuget
[Nuget Package](https://www.nuget.org/packages/Sql2Json.Core/)
```
PM> Install-Package Sql2Json.Core
```


## Description
This library helps to convert sql query results (from a database) to JSON (including nested objects).

Some database engines lack the ability to query tables an return the results directly as JSON (e.g. Oracle). 
The mapping can be configured with an fluent interface or can be extracted out of an anonymous type defintion.
The primary goal of this library is ease of use. 
With this library it is possible to skip the step to add an OR-Mapping Layer just to generate JSON.
The Library supports all databases with an ado.net provider.


## Usage

### If you have a database with the following content

```sql
INSERT INTO customer (id, sname, fname, BDAY, ADR_STREET, ADR_CITY) VALUES(1, 'Meyers','Mike','1983-10-31 00:00:00.000', 'Street 1', 'Las Vegas');
INSERT INTO invoices (id, inv_date, amount, customer_id) VALUES(1, '2015-10-12 15:43:00.000', 500.2, 1);
INSERT INTO invoices (id, inv_date, amount, customer_id) VALUES(2, '2016-05-09 15:43:00.000', 155.2, 1);
INSERT INTO orders (id, ordernumber, articles, customer_id) VALUES (1,'O-1001','Book 1, Book 2',1);
INSERT INTO orders (id, ordernumber, articles, customer_id) VALUES (2,'O-1002','CD Burner, Usb stick 32Gb',1);
```

### Using the fluent interface to create the mapping
```csharp
var mapping = JsonMappingBuilder.Root()
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
	.Result;
```

### Lets generate the JSON using the mapping
```csharp
// Execute mapping
using (var engine = new MappingEngine(() => new SqliteConnection("Data Source=Test.db"), mappingDef.Compile()));
using (var filestream = new FileStream("output.json", FileMode.CreateNew)) {
    engine.ExecuteMapping(filestream, null);
}
```

### The output looks like this

```json
[{
    "Id": 1,
    "SName": "Meyers",
    "FirstName": "Mike",
    "LastUpdate": "2017-03-01",
    "Birthday": "1983-10-31 00:00:00.000",
    "Address": {
        "Street": "Street 1",
        "City": "Las Vegas"
    },
    "Invoices": [{
            "InvoiceId": 1,
            "Inv_Date": "2015-10-12 15:43:00.000",
            "TotalAmount": 500.2
        }, {
            "InvoiceId": 2,
            "Inv_Date": "2016-05-09 15:43:00.000",
            "TotalAmount": 155.2
        }
    ],
    "Orders": [{
            "OrderId": 1,
            "ArticleSummary": "Book 1, Book 2"
        }, {
            "OrderId": 2,
            "ArticleSummary": "CD Burner, Usb stick 32Gb"
        }
    ]
}]
```


### Using a anonymous type defintion as mapping
As an alternative for the fluent mapping you can use the anonymous type mapping API.
The following code creates the same JSON output.

```csharp
var mappingDef = Define.QueryWithNestedResults("cid",
@"select c.id as cid, c.sname, c.fname, c.BDAY, c.ADR_STREET, c.ADR_CITY, i.id as iid, i.inv_date, i.amount, o.articles, o.id as oid
from customer c
left join orders o on c.id = o.customer_id
left join invoices i on c.id = i.customer_id"
,
    new
    {
        Id = Define.Column("cid"),
        SurName = Define.Column("sname"),
        FirstName = Define.Column("fname"),
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
                ArticleSummary = Define.Column("articles"),
            }
        )
    }
);
```



## Save mapping

It is possible to save the mapping defintion as external json for later use.

```csharp
var mappingDef = Define.QueryWithNestedResults("id","select * from customer");
mappingDef.Compile().Save("mapping.json");
```

## Load mapping

Load a saved mapping from file and execute the mapping.

```csharp

// Load mapping
var loadedCompiledMapping = MappingFileManager.Load(@"mapping.json");
// execute it
var engine = new MappingEngine(connection, loadedCompiledMapping);
...
```

### License

__MIT License__