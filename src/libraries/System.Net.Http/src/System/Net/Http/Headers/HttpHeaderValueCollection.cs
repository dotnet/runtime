// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.Http.Headers
{
    // This type is used for headers supporting a list of values. It essentially just forwards calls to
    // the actual header-store in HttpHeaders.
    //
    // This type can deal with a so called "special value": The RFC defines some headers which are collection of
    // values, but the RFC only defines 1 value, e.g. Transfer-Encoding: chunked, Connection: close,
    // Expect: 100-continue.
    // We expose strongly typed properties for these special values: TransferEncodingChunked, ConnectionClose,
    // ExpectContinue.
    // So we have 2 properties for each of these headers ('Transfer-Encoding' => TransferEncoding,
    // TransferEncodingChunked; 'Connection' => Connection, ConnectionClose; 'Expect' => Expect, ExpectContinue)
    //
    // The following solution was chosen:
    // - Keep HttpHeaders clean: HttpHeaders is unaware of these "special values"; it just stores the collection of
    //   headers.
    // - It is the responsibility of "higher level" components (HttpHeaderValueCollection, HttpRequestHeaders,
    //   HttpResponseHeaders) to deal with special values.
    // - HttpHeaderValueCollection can be configured with an IEqualityComparer and a "special value".
    //
    // Example: Server sends header "Transfer-Encoding: gzip, custom, chunked" to the client.
    // - HttpHeaders: HttpHeaders will have an entry in the header store for "Transfer-Encoding" with values
    //   "gzip", "custom", "chunked"
    // - HttpGeneralHeaders:
    //   - Property TransferEncoding: has three values "gzip", "custom", and "chunked"
    //   - Property TransferEncodingChunked: is set to "true".
    public sealed class HttpHeaderValueCollection<T> : ICollection<T> where T : class
    {
        private HeaderDescriptor _descriptor;
        private HttpHeaders _store;
        private T _specialValue;
        private Action<HttpHeaderValueCollection<T>, T> _validator;

        public int Count
        {
            get { return GetCount(); }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        internal bool IsSpecialValueSet
        {
            get
            {
                // If this collection instance has a "special value", then check whether that value was already set.
                if (_specialValue == null)
                {
                    return false;
                }
                return _store.ContainsParsedValue(_descriptor, _specialValue);
            }
        }

        internal HttpHeaderValueCollection(HeaderDescriptor descriptor, HttpHeaders store)
            : this(descriptor, store, null, null)
        {
        }

        internal HttpHeaderValueCollection(HeaderDescriptor descriptor, HttpHeaders store,
            Action<HttpHeaderValueCollection<T>, T> validator)
            : this(descriptor, store, null, validator)
        {
        }

        internal HttpHeaderValueCollection(HeaderDescriptor descriptor, HttpHeaders store, T specialValue)
            : this(descriptor, store, specialValue, null)
        {
        }

        internal HttpHeaderValueCollection(HeaderDescriptor descriptor, HttpHeaders store, T specialValue,
            Action<HttpHeaderValueCollection<T>, T> validator)
        {
            Debug.Assert(descriptor.Name != null);
            Debug.Assert(store != null);

            _store = store;
            _descriptor = descriptor;
            _specialValue = specialValue;
            _validator = validator;
        }

        public void Add(T item)
        {
            CheckValue(item);
            _store.AddParsedValue(_descriptor, item);
        }

        public void ParseAdd(string input)
        {
            _store.Add(_descriptor, input);
        }

        public bool TryParseAdd(string input)
        {
            return _store.TryParseAndAddValue(_descriptor, input);
        }

        public void Clear()
        {
            _store.Remove(_descriptor);
        }

        public bool Contains(T item)
        {
            CheckValue(item);
            return _store.ContainsParsedValue(_descriptor, item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            // Allow arrayIndex == array.Length in case our own collection is empty
            if ((arrayIndex < 0) || (arrayIndex > array.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            object storeValue = _store.GetParsedValues(_descriptor);

            if (storeValue == null)
            {
                return;
            }

            List<object> storeValues = storeValue as List<object>;

            if (storeValues == null)
            {
                // We only have 1 value: If it is the "special value" just return, otherwise add the value to the
                // array and return.
                Debug.Assert(storeValue is T);
                if (arrayIndex == array.Length)
                {
                    throw new ArgumentException(SR.net_http_copyto_array_too_small);
                }
                array[arrayIndex] = storeValue as T;
            }
            else
            {
                storeValues.CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(T item)
        {
            CheckValue(item);
            return _store.RemoveParsedValue(_descriptor, item);
        }

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            object storeValue = _store.GetParsedValues(_descriptor);

            if (storeValue == null)
            {
                yield break;
            }

            List<object> storeValues = storeValue as List<object>;

            if (storeValues == null)
            {
                Debug.Assert(storeValue is T);
                yield return storeValue as T;
            }
            else
            {
                // We have multiple values. Iterate through the values and return them.
                foreach (object item in storeValues)
                {
                    Debug.Assert(item is T);
                    yield return item as T;
                }
            }
        }

        #endregion

        #region IEnumerable Members

        Collections.IEnumerator Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        public override string ToString()
        {
            return _store.GetHeaderString(_descriptor);
        }

        internal void SetSpecialValue()
        {
            Debug.Assert(_specialValue != null,
                "This method can only be used if the collection has a 'special value' set.");

            if (!_store.ContainsParsedValue(_descriptor, _specialValue))
            {
                _store.AddParsedValue(_descriptor, _specialValue);
            }
        }

        internal void RemoveSpecialValue()
        {
            Debug.Assert(_specialValue != null,
                "This method can only be used if the collection has a 'special value' set.");

            // We're not interested in the return value. It's OK if the "special value" wasn't in the store
            // before calling RemoveParsedValue().
            _store.RemoveParsedValue(_descriptor, _specialValue);
        }

        private void CheckValue(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            // If this instance has a custom validator for validating arguments, call it now.
            if (_validator != null)
            {
                _validator(this, item);
            }
        }

        private int GetCount()
        {
            // This is an O(n) operation.

            object storeValue = _store.GetParsedValues(_descriptor);

            if (storeValue == null)
            {
                return 0;
            }

            List<object> storeValues = storeValue as List<object>;

            if (storeValues == null)
            {
                return 1;
            }
            else
            {
                return storeValues.Count;
            }
        }
    }
}
