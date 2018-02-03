using Sql2Json.Core.Compiled;
using Sql2Json.Core.MappingBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Sql2Json.Tests
{
    public class MappingBuilderTests
    {
        private IJsonMappingBuilder builder;

        public MappingBuilderTests()
        {
            builder = JsonMappingBuilder.Root();
        }

        [Fact]
        public void AddPropertyTest()
        {

            builder.Property("PROPERTY", 1000);
            var mapping = builder.Result;
            Assert.Single(mapping.MappedPropertyList);
            Assert.Equal("PROPERTY", mapping.MappedPropertyList.First().TargetPropertyName);
            Assert.Equal(1000, mapping.MappedPropertyList.First().StaticValue);
        }

        [Fact]
        public void AddPropertyExceptionsTest()
        {

            Assert.Throws<ArgumentNullException>(() => builder.Property(null, 1000));
            Assert.Throws<ArgumentNullException>(() => builder.Property("test", null));
        }

        [Fact]
        public void AddQueryWithColumnTest()
        {

            builder.Query("QUERYPROPERTY", "SELECT * FROM", cfg => cfg.Column("COLUMNPROPERTY", "COL"));
            var mapping = builder.Result;
            Assert.Single(mapping.MappedPropertyList);
            Assert.Equal("QUERYPROPERTY", mapping.MappedPropertyList.First().TargetPropertyName);
            Assert.Equal("SELECT * FROM", mapping.MappedPropertyList.First().MappedQuery.Query);
            Assert.Equal("COLUMNPROPERTY", mapping.MappedPropertyList.First().MappedQuery.MappedPropertyList.First().TargetPropertyName);
            Assert.Equal("COL", mapping.MappedPropertyList.First().MappedQuery.MappedPropertyList.First().ColumnName);
        }


        [Fact]
        public void AddColumnTest()
        {

            Assert.Throws<InvalidOperationException>(() => builder.Column("COLP", "COL"));
        }

        [Fact]
        public void AddNestedResultsTest()
        {

            Assert.Throws<InvalidOperationException>(() => builder.NestedResults("NR", "IDCOL", cfg => cfg.Property("", "")));
        }
    }
}
