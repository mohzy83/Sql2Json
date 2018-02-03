﻿using Newtonsoft.Json;
using Sql2Json.Core.Compiled;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Json.Core.Engine
{
    /// <summary>
    /// Interface for to process query result rows
    /// </summary>
    public interface IQueryResultProcessor
    {
        /// <summary>
        /// Processes all rows of a query result
        /// </summary>
        /// <param name="mappedProperty">the created child objects will be assigned to the property as array</param>
        /// <param name="mappedQuery">mapped query object</param>
        /// <param name="reader">current reader</param>
        /// <param name="jsonWriter">current json writer</param>
        /// <param name="rowFinishedAction">action will be fired when the row or rowset was read</param>
        void ProcessResults(MappedProperty mappedProperty, MappedQuery mappedQuery, IDataReader reader, JsonTextWriter jsonWriter, Action<IDictionary<string, object>, RowSet, List<string>> rowFinishedAction);
    }
}
