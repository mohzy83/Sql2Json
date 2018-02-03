using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace Sql2Json.Core.Engine
{
    /// <summary>
    /// Extension methods for <see cref="IDataReader"/>
    /// </summary>
    public static class DataReaderExtensions
    {
        /// <summary>
        /// Converts the current row to a dictonary where key is the column name and value is the column value
        /// </summary>
        /// <param name="reader">current <see cref="IDataReader"/></param>
        /// <returns>row as dictionary</returns>
        public static IDictionary<string, object> ToDictionary(this IDataReader reader, IDictionary<string,int> lookupDictionary)
        {
            //Dictionary<string, object> result = new Dictionary<string, object>();
            if (reader == null) throw new ArgumentNullException("reader cant be null");
            //for (int i = 0; i < reader.FieldCount; i++)
            //    result.Add(reader.GetName(i), reader.GetValue(i));
            return new DataReaderDictionary(reader, lookupDictionary);
        }
        /// <summary>
        /// Get all column names of the current <see cref="IDataReader"/>
        /// </summary>
        /// <param name="reader">current <see cref="IDataReader"/></param>
        /// <returns>List of column names</returns>
        public static List<string> GetFieldNameList(this IDataReader reader)
        {
            if (reader == null) throw new ArgumentNullException("reader cant be null");
            List<string> result = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
                result.Add(reader.GetName(i));
            return result;
        }

        /// <summary>
        /// Gets a lookup table for column index by name
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static IDictionary<string, int> GetFieldIndexLookupTable(this IDataReader reader)
        {
            var result = new Dictionary<string, int>();
            if (reader == null) throw new ArgumentNullException("reader cant be null");
            for (int i = 0; i < reader.FieldCount; i++)
                result.Add(reader.GetName(i),i);
            return result;
        }

    }


    public class DataReaderDictionary : IDictionary<string, object>
    {
        //   private IDataReader reader;
        object[] values;
        private IDictionary<string, int> lookupDictionary;


        public DataReaderDictionary(IDataReader reader, IDictionary<string, int> lookupDictionary)
        {
            values = new object[reader.FieldCount];
            this.lookupDictionary = lookupDictionary;
            for (int i = 0; i < reader.FieldCount; i++)
            {
                values[i] = reader.GetValue(i);
            }
        }


        public object this[string key]
        {
            get
            {
                return values[lookupDictionary[key]];
            }
            set => throw new NotImplementedException();
        }

        public ICollection<string> Keys => throw new NotImplementedException();

        public ICollection<object> Values => throw new NotImplementedException();

        public int Count => values.Length;

        public bool IsReadOnly => throw new NotImplementedException();

        public void Add(string key, object value)
        {
            throw new NotImplementedException();
        }

        public void Add(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(string key)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool Remove(string key)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(string key, out object value)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

}
