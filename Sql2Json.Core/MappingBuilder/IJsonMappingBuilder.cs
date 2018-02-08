using Sql2Json.Core.Compiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sql2Json.Core.MappingBuilder
{
    /// <summary>
    /// Mapping builder interface
    /// </summary>
    public interface IJsonMappingBuilder
    {
        IJsonMappingBuilder Query(string propertyName, string query, Action<IJsonMappingBuilder> config);
        IJsonMappingBuilder QueryWithNesting(string propertyName, string query, string idColumn, Action<IJsonMappingBuilder> config);
        IJsonMappingBuilder Column(string propertyName, string columnName = null);
        IJsonMappingBuilder PropertyResolver(string propertyName, Type valueResolverType);
        IJsonMappingBuilder Property(string propertyName, object value);
        IJsonMappingBuilder Object(string propertyName, Action<IJsonMappingBuilder> config);
        IJsonMappingBuilder NestedResults(string propertyName, string idColumn, Action<IJsonMappingBuilder> config);
        /// <summary>
        /// Returns the complete mapping configuration
        /// </summary>
        MappedObject Result { get; }
    }
}
