using Sql2Json.Core.Compiled;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sql2Json.Core.MappingBuilder
{
    /// <summary>
    /// Builder to create a mapping for the mapping engine
    /// </summary>
    public class JsonMappingBuilder : IJsonMappingBuilder
    {
        /// <summary>
        /// Returns the complete mapping configuration
        /// </summary>
        public MappedObject Result { get; private set; }
        private JsonMappingBuilder parent;

        private JsonMappingBuilder(JsonMappingBuilder parent)
        {
            this.parent = parent;
            Result = new MappedObject();
        }

        /// <summary>
        /// Creates an emtpy mapping configuration.
        /// This is the point to start.
        /// </summary>
        /// <returns></returns>
        public static IJsonMappingBuilder Root()
        {
            return new JsonMappingBuilder(null);
        }

        /// <summary>
        /// Adds a property with a static value
        /// </summary>
        /// <param name="name">Name of the property in resulting json document</param>
        /// <param name="value">static value</param>
        /// <returns></returns>
        public IJsonMappingBuilder Property(string name, object value)
        {
            if (name == null) throw new ArgumentNullException("name cant be null");
            if (value == null) throw new ArgumentNullException("value cant be null");
            Result.MappedPropertyList.Add(new MappedProperty(name, value));
            return this;
        }

        public IJsonMappingBuilder Query(string propertyName, string query, Action<IJsonMappingBuilder> config)
        {
            if (propertyName == null) throw new ArgumentNullException("propertyName cant be null");
            if (query == null) throw new ArgumentNullException("query cant be null");
            if (config == null) throw new ArgumentNullException("config cant be null");
            var builder = new JsonMappingBuilder(this);
            var mappedQuery = new MappedQuery(query);
            builder.Result = mappedQuery;
            Result.MappedPropertyList.Add(new MappedProperty(propertyName, mappedQuery));
            config(builder);
            return this;
        }

        public IJsonMappingBuilder QueryWithNesting(string propertyName, string query, string idColumn, Action<IJsonMappingBuilder> config)
        {
            if (propertyName == null) throw new ArgumentNullException("propertyName cant be null");
            if (query == null) throw new ArgumentNullException("query cant be null");
            if (idColumn == null) throw new ArgumentNullException("idColumn cant be null");
            if (config == null) throw new ArgumentNullException("config cant be null");
            var builder = new JsonMappingBuilder(this);
            var mappedQuery = new MappedQueryWithNestedResults(idColumn, query);
            builder.Result = mappedQuery;
            Result.MappedPropertyList.Add(new MappedProperty(propertyName, mappedQuery));
            config(builder);
            return this;
        }

        public IJsonMappingBuilder Column(string propertyName, string columnName = null)
        {
            AssertParentMappingHasType<MappedQuery>(this, propertyName, "Column");
            if (propertyName == null) throw new ArgumentNullException("propertyName cant be null");
            Result.MappedPropertyList.Add(new MappedProperty(propertyName, string.IsNullOrEmpty(columnName) ? propertyName : columnName));
            return this;
        }

        public IJsonMappingBuilder Object(string propertyName, Action<IJsonMappingBuilder> config)
        {
            if (propertyName == null) throw new ArgumentNullException("propertyName cant be null");
            if (config == null) throw new ArgumentNullException("config cant be null");
            var builder = new JsonMappingBuilder(this);
            Result.MappedPropertyList.Add(new MappedProperty(propertyName, builder.Result));
            config(builder);
            return this;
        }

        public IJsonMappingBuilder NestedResults(string propertyName, string idColumn, Action<IJsonMappingBuilder> config)
        {
            if (propertyName == null) throw new ArgumentNullException("propertyName cant be null");
            if (idColumn == null) throw new ArgumentNullException("idColumn cant be null");
            if (config == null) throw new ArgumentNullException("config cant be null");
            AssertParentMappingHasType<MappedQueryWithNestedResults>(this, propertyName, "NestedResults");
            var builder = new JsonMappingBuilder(this);
            var mappedNestedResults = new MappedNestedResults(idColumn);
            builder.Result = mappedNestedResults;
            Result.MappedPropertyList.Add(new MappedProperty(propertyName, mappedNestedResults));
            config(builder);
            return this;
        }

        private void AssertParentMappingHasType<TMappingType>(JsonMappingBuilder start, string property, string methodName) where TMappingType : MappedObject
        {
            JsonMappingBuilder current = start;
            bool found = false;
            while (current != null)
            {
                if (current.Result is TMappingType)
                {
                    found = true;
                    break;
                }
                current = current.parent;
            }
            if (!found) throw new InvalidOperationException(string.Format("{0} with property name [{1}] is not allowed at this location. Parent of type [{2}] is missing.", methodName, property, typeof(TMappingType).Name));
        }
    }

}
