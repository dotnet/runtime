// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.Common
{
    public class DbConnectionStringBuilder : System.Collections.IDictionary
    {
        // keyword->value currently listed in the connection string
        private Dictionary<string, object> _currentValues;

        // cached connectionstring to avoid constant rebuilding
        // and to return a user's connectionstring as is until editing occurs
        private string _connectionString = "";


        public DbConnectionStringBuilder()
        {
        }


        private ICollection Collection
        {
            get { return (ICollection)CurrentValues; }
        }
        private IDictionary Dictionary
        {
            get { return (IDictionary)CurrentValues; }
        }
        private Dictionary<string, object> CurrentValues
        {
            get
            {
                Dictionary<string, object> values = _currentValues;
                if (null == values)
                {
                    values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    _currentValues = values;
                }
                return values;
            }
        }

        object System.Collections.IDictionary.this[object keyword]
        {
            // delegate to this[string keyword]
            get { return this[ObjectToString(keyword)]; }
            set { this[ObjectToString(keyword)] = value; }
        }

        public virtual object this[string keyword]
        {
            get
            {
                ADP.CheckArgumentNull(keyword, "keyword");
                object value;
                if (CurrentValues.TryGetValue(keyword, out value))
                {
                    return value;
                }
                throw ADP.KeywordNotSupported(keyword);
            }
            set
            {
                ADP.CheckArgumentNull(keyword, "keyword");
                if (null != value)
                {
                    string keyvalue = DbConnectionStringBuilderUtil.ConvertToString(value);
                    DbConnectionOptions.ValidateKeyValuePair(keyword, keyvalue);
                    // store keyword/value pair
                    CurrentValues[keyword] = keyvalue;
                }
                _connectionString = null;
            }
        }

        public string ConnectionString
        {
            get
            {
                string connectionString = _connectionString;
                if (null == connectionString)
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (string keyword in Keys)
                    {
                        object value;
                        if (ShouldSerialize(keyword) && TryGetValue(keyword, out value))
                        {
                            string keyvalue = (null != value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : (string)null;
                            AppendKeyValuePair(builder, keyword, keyvalue);
                        }
                    }
                    connectionString = builder.ToString();
                    _connectionString = connectionString;
                }
                return connectionString;
            }
            set
            {
                DbConnectionOptions constr = new DbConnectionOptions(value, null);
                string originalValue = ConnectionString;
                Clear();
                try
                {
                    for (NameValuePair pair = constr.KeyChain; null != pair; pair = pair.Next)
                    {
                        if (null != pair.Value)
                        {
                            this[pair.Name] = pair.Value;
                        }
                        else
                        {
                            Remove(pair.Name);
                        }
                    }
                    _connectionString = null;
                }
                catch (ArgumentException)
                { // restore original string
                    ConnectionString = originalValue;
                    _connectionString = originalValue;
                    throw;
                }
            }
        }

        public virtual int Count
        {
            get { return CurrentValues.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public virtual bool IsFixedSize
        {
            get { return false; }
        }
        bool System.Collections.ICollection.IsSynchronized
        {
            get { return Collection.IsSynchronized; }
        }

        public virtual ICollection Keys
        {
            get
            {
                return Dictionary.Keys;
            }
        }


        object System.Collections.ICollection.SyncRoot
        {
            get { return Collection.SyncRoot; }
        }

        public virtual ICollection Values
        {
            get
            {
                System.Collections.Generic.ICollection<string> keys = (System.Collections.Generic.ICollection<string>)Keys;
                System.Collections.Generic.IEnumerator<string> keylist = keys.GetEnumerator();
                object[] values = new object[keys.Count];
                for (int i = 0; i < values.Length; ++i)
                {
                    keylist.MoveNext();
                    values[i] = this[keylist.Current];
                    Debug.Assert(null != values[i], "null value " + keylist.Current);
                }
                return new System.Data.Common.ReadOnlyCollection<object>(values);
            }
        }

        void System.Collections.IDictionary.Add(object keyword, object value)
        {
            Add(ObjectToString(keyword), value);
        }
        public void Add(string keyword, object value)
        {
            this[keyword] = value;
        }

        public static void AppendKeyValuePair(StringBuilder builder, string keyword, string value)
        {
            DbConnectionOptions.AppendKeyValuePairBuilder(builder, keyword, value, false);
        }


        public virtual void Clear()
        {
            _connectionString = "";
            CurrentValues.Clear();
        }


        // does the keyword exist as a strongly typed keyword or as a stored value
        bool System.Collections.IDictionary.Contains(object keyword)
        {
            return ContainsKey(ObjectToString(keyword));
        }
        public virtual bool ContainsKey(string keyword)
        {
            ADP.CheckArgumentNull(keyword, "keyword");
            return CurrentValues.ContainsKey(keyword);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            Collection.CopyTo(array, index);
        }

        public virtual bool EquivalentTo(DbConnectionStringBuilder connectionStringBuilder)
        {
            ADP.CheckArgumentNull(connectionStringBuilder, "connectionStringBuilder");


            if ((GetType() != connectionStringBuilder.GetType()) || (CurrentValues.Count != connectionStringBuilder.CurrentValues.Count))
            {
                return false;
            }
            object value;
            foreach (KeyValuePair<string, object> entry in CurrentValues)
            {
                if (!connectionStringBuilder.CurrentValues.TryGetValue(entry.Key, out value) || !entry.Value.Equals(value))
                {
                    return false;
                }
            }
            return true;
        }

        IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Collection.GetEnumerator();
        }
        IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator()
        {
            return Dictionary.GetEnumerator();
        }


        private string ObjectToString(object keyword)
        {
            try
            {
                return (string)keyword;
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("not a string", nameof(keyword));
            }
        }

        void System.Collections.IDictionary.Remove(object keyword)
        {
            Remove(ObjectToString(keyword));
        }
        public virtual bool Remove(string keyword)
        {
            ADP.CheckArgumentNull(keyword, "keyword");
            if (CurrentValues.Remove(keyword))
            {
                _connectionString = null;
                return true;
            }
            return false;
        }

        // does the keyword exist as a stored value or something that should always be persisted
        public virtual bool ShouldSerialize(string keyword)
        {
            ADP.CheckArgumentNull(keyword, "keyword");
            return CurrentValues.ContainsKey(keyword);
        }

        public override string ToString()
        {
            return ConnectionString;
        }

        public virtual bool TryGetValue(string keyword, out object value)
        {
            ADP.CheckArgumentNull(keyword, "keyword");
            return CurrentValues.TryGetValue(keyword, out value);
        }
    }
}

