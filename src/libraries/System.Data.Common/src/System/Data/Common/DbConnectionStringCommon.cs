// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;


namespace System.Data.Common
{
    internal sealed class ReadOnlyCollection<T> : System.Collections.ICollection, ICollection<T>
    {
        private T[] _items;

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

        Object System.Collections.ICollection.SyncRoot
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
            private K[] _items;
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

            Object System.Collections.IEnumerator.Current
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
        internal static string ConvertToString(object value)
        {
            try
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException e)
            {
                throw ADP.ConvertFailed(value.GetType(), typeof(String), e);
            }
        }
    }

    internal static class DbConnectionStringDefaults
    {
        internal const int ConnectTimeout = 15;
    }
}
