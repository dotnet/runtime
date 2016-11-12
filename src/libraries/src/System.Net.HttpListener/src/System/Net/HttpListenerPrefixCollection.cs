// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace System.Net
{
    internal sealed class ListenerPrefixEnumerator : IEnumerator<string>
    {
        private readonly IEnumerator _enumerator;

        internal ListenerPrefixEnumerator(IEnumerator enumerator)
        {
            _enumerator = enumerator;
        }

        public string Current
        {
            get
            {
                return (string)_enumerator.Current;
            }
        }
        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Dispose()
        {
        }

        void System.Collections.IEnumerator.Reset()
        {
            _enumerator.Reset();
        }

        object System.Collections.IEnumerator.Current
        {
            get
            {
                return _enumerator.Current;
            }
        }
    }


    public class HttpListenerPrefixCollection : ICollection<string>
    {
        private HttpListener _httpListener;

        internal HttpListenerPrefixCollection(HttpListener listener)
        {
            _httpListener = listener;
        }

        public void CopyTo(Array array, int offset)
        {
            _httpListener.CheckDisposed();
            if (Count > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(array), SR.net_array_too_small);
            }
            if (offset + Count > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            int index = 0;
            foreach (string uriPrefix in _httpListener.PrefixCollection)
            {
                array.SetValue(uriPrefix, offset + index++);
            }
        }

        public void CopyTo(string[] array, int offset)
        {
            _httpListener.CheckDisposed();
            if (Count > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(array), SR.net_array_too_small);
            }
            if (offset + Count > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            int index = 0;
            foreach (string uriPrefix in _httpListener.PrefixCollection)
            {
                array[offset + index++] = uriPrefix;
            }
        }

        public int Count
        {
            get
            {
                return _httpListener.PrefixCollection.Count;
            }
        }
        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public void Add(string uriPrefix)
        {
            _httpListener.AddPrefix(uriPrefix);
        }

        public bool Contains(string uriPrefix)
        {
            return _httpListener.ContainsPrefix(uriPrefix);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerator<string> GetEnumerator()
        {
            return new ListenerPrefixEnumerator(_httpListener.PrefixCollection.GetEnumerator());
        }

        public bool Remove(string uriPrefix)
        {
            return _httpListener.RemovePrefix(uriPrefix);
        }

        public void Clear()
        {
            _httpListener.RemoveAll(true);
        }
    }
}

