using Sql2Json.Core;
using Sql2Json.Core.Compiled;
using System;
using System.Text;
using Xunit;

namespace Sql2Json.Tests
{
    public class DefinitionTemplateCompilerTests
    {
        [Fact]
        public void ArgumentNullTest()
        {
            Assert.Throws<ArgumentNullException>(() => DefinitionTemplateCompiler.CompileNestedResults(null, null));
        }


        [Fact]
        public void AnonymousTypeTest()
        {
            var mappedObject = new MappedObject();
            var anonObj = new { Field1 = "Test", Field2 = 2 };
            DefinitionTemplateCompiler.CompileProperties(anonObj, mappedObject, null);
            Assert.Equal("Field1", mappedObject.MappedPropertyList[0].TargetPropertyName);
            Assert.Equal(anonObj.Field1, mappedObject.MappedPropertyList[0].StaticValue);
            Assert.Equal("Field2", mappedObject.MappedPropertyList[1].TargetPropertyName);
            Assert.Equal(anonObj.Field2, mappedObject.MappedPropertyList[1].StaticValue);

            Assert.Throws<ArgumentException>(() => DefinitionTemplateCompiler.CompileProperties(new[] { "a", "b" }, mappedObject, null));
            Assert.Throws<ArgumentException>(() => DefinitionTemplateCompiler.CompileProperties(new StringBuilder(), mappedObject, null));

        }
    }
}
