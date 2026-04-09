// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Data.Common
{
    internal sealed class ReadOnlyCollection<T> : System.Collections.ICollection, ICollection<T>
    {
        private readonly T[] _items;

        internal ReadOnlyCollection(T[] items)
        {
            _items = items;
#if DEBUG
            for (int i = 0; i < items.Length; ++i)
            {
                Debug.Assert(null != items[i], "null item");
            }
#endif
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(_items, 0, array, arrayIndex, _items.Length);
        }

        void System.Collections.ICollection.CopyTo(Array array, int arrayIndex)
        {
            Array.Copy(_items, 0, array, arrayIndex, _items.Length);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator<T>(_items);
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            return new Enumerator<T>(_items);
        }

        bool System.Collections.ICollection.IsSynchronized
        {
            get { return false; }
        }

        object System.Collections.ICollection.SyncRoot
        {
            get { return _items; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return true; }
        }

        void ICollection<T>.Add(T value)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<T>.Contains(T value)
        {
            return Array.IndexOf(_items, value) >= 0;
        }

        bool ICollection<T>.Remove(T value)
        {
            throw new NotSupportedException();
        }

        public int Count
        {
            get { return _items.Length; }
        }

        internal struct Enumerator<K> : IEnumerator<K>, System.Collections.IEnumerator
        { // based on List<T>.Enumerator
            private readonly K[] _items;
            private int _index;

            internal Enumerator(K[] items)
            {
                _items = items;
                _index = -1;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                return (++_index < _items.Length);
            }

            public K Current
            {
                get
                {
                    return _items[_index];
                }
            }

            object? System.Collections.IEnumerator.Current
            {
                get
                {
                    return _items[_index];
                }
            }

            void System.Collections.IEnumerator.Reset()
            {
                _index = -1;
            }
        }
    }

    internal static class DbConnectionStringBuilderUtil
    {
        internal static bool ConvertToBoolean(object value)
        {
            Debug.Assert(null != value, "ConvertToBoolean(null)");
            string? svalue = (value as string);
            if (null != svalue)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(svalue, "true") || StringComparer.OrdinalIgnoreCase.Equals(svalue, "yes"))
                    return true;
                else if (StringComparer.OrdinalIgnoreCase.Equals(svalue, "false") || StringComparer.OrdinalIgnoreCase.Equals(svalue, "no"))
                    return false;
                else
                {
                    string tmp = svalue.Trim();  // Remove leading & trailing white space.
                    if (StringComparer.OrdinalIgnoreCase.Equals(tmp, "true") || StringComparer.OrdinalIgnoreCase.Equals(tmp, "yes"))
                        return true;
                    else if (StringComparer.OrdinalIgnoreCase.Equals(tmp, "false") || StringComparer.OrdinalIgnoreCase.Equals(tmp, "no"))
                        return false;
                }
                return bool.Parse(svalue);
            }
            try
            {
                return ((IConvertible)value).ToBoolean(CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException e)
            {
                throw ADP.ConvertFailed(value.GetType(), typeof(bool), e);
            }
        }

        internal static int ConvertToInt32(object value)
        {
            try
            {
                return ((IConvertible)value).ToInt32(CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException e)
            {
                throw ADP.ConvertFailed(value.GetType(), typeof(int), e);
            }
        }

        internal static string ConvertToString(object value)
        {
            try
            {
                return ((IConvertible)value).ToString(CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException e)
            {
                throw ADP.ConvertFailed(value.GetType(), typeof(string), e);
            }
        }
    }

    internal static class DbConnectionStringDefaults
    {
        // OleDb
        internal const string FileName = "";
        internal const int OleDbServices = ~(/*DBPROPVAL_OS_AGR_AFTERSESSION*/0x00000008 | /*DBPROPVAL_OS_CLIENTCURSOR*/0x00000004); // -13
        internal const string Provider = "";

        internal const int ConnectTimeout = 15;
        internal const bool PersistSecurityInfo = false;
        internal const string DataSource = "";
    }

    internal static class DbConnectionOptionKeywords
    {
        // Odbc
        internal const string Driver = "driver";
        internal const string Pwd = "pwd";
        internal const string UID = "uid";

        // OleDb
        internal const string DataProvider = "data provider";
        internal const string ExtendedProperties = "extended properties";
        internal const string FileName = "file name";
        internal const string Provider = "provider";
        internal const string RemoteProvider = "remote provider";

        // common keywords (OleDb, OracleClient, SqlClient)
        internal const string Password = "password";
        internal const string UserID = "user id";
    }

    internal static class DbConnectionStringKeywords
    {
        // Odbc
        internal const string Driver = "Driver";

        // OleDb
        internal const string FileName = "File Name";
        internal const string OleDbServices = "OLE DB Services";
        internal const string Provider = "Provider";

        internal const string DataSource = "Data Source";
        internal const string PersistSecurityInfo = "Persist Security Info";
    }
}
