// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private readonly HeaderDescriptor _descriptor;
        private readonly HttpHeaders _store;

        public int Count
        {
            get { return GetCount(); }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        internal HttpHeaderValueCollection(HeaderDescriptor descriptor, HttpHeaders store)
        {
            _store = store;
            _descriptor = descriptor;
        }

        public void Add(T item)
        {
            CheckValue(item);
            _store.AddParsedValue(_descriptor, item);
        }

        public void ParseAdd(string? input)
        {
            _store.Add(_descriptor, input);
        }

        public bool TryParseAdd(string? input)
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

        public void CopyTo(T[] array!!, int arrayIndex)
        {
            // Allow arrayIndex == array.Length in case our own collection is empty
            if ((arrayIndex < 0) || (arrayIndex > array.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            object? storeValue = _store.GetParsedAndInvalidValues(_descriptor);

            if (storeValue == null)
            {
                return;
            }

            List<object>? storeValues = storeValue as List<object>;

            if (storeValues == null)
            {
                if (storeValue is not HttpHeaders.InvalidValue)
                {
                    Debug.Assert(storeValue is T);
                    if (arrayIndex == array.Length)
                    {
                        throw new ArgumentException(SR.net_http_copyto_array_too_small);
                    }
                    array[arrayIndex] = (T)storeValue;
                }
            }
            else
            {
                foreach (object item in storeValues)
                {
                    if (item is not HttpHeaders.InvalidValue)
                    {
                        Debug.Assert(item is T);
                        if (arrayIndex == array.Length)
                        {
                            throw new ArgumentException(SR.net_http_copyto_array_too_small);
                        }
                        array[arrayIndex++] = (T)item;
                    }
                }
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
            object? storeValue = _store.GetParsedAndInvalidValues(_descriptor);
            return storeValue is null || storeValue is HttpHeaders.InvalidValue ?
                ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator() : // use singleton empty array enumerator
                Iterate(storeValue);

            static IEnumerator<T> Iterate(object storeValue)
            {
                if (storeValue is List<object> storeValues)
                {
                    // We have multiple values. Iterate through the values and return them.
                    foreach (object item in storeValues)
                    {
                        if (item is HttpHeaders.InvalidValue)
                        {
                            continue;
                        }
                        Debug.Assert(item is T);
                        yield return (T)item;
                    }
                }
                else
                {
                    Debug.Assert(storeValue is T);
                    yield return (T)storeValue;
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

        private void CheckValue(T item!!)
        {
            if (_descriptor.Parser == GenericHeaderParser.TokenListParser)
            {
                // The collection expects valid HTTP tokens, which are typed as string.
                // Unlike other parsed values (which are always valid by construction),
                // we can't assume the provided string is a valid token. So validate it before we use it.
                Debug.Assert(typeof(T) == typeof(string));
                HeaderUtilities.CheckValidToken((string)(object)item, nameof(item));
            }
        }

        private int GetCount()
        {
            // This is an O(n) operation.

            object? storeValue = _store.GetParsedAndInvalidValues(_descriptor);

            if (storeValue == null)
            {
                return 0;
            }

            List<object>? storeValues = storeValue as List<object>;

            if (storeValues == null)
            {
                if (storeValue is not HttpHeaders.InvalidValue)
                {
                    return 1;
                }
                return 0;
            }
            else
            {
                int count = 0;
                foreach (object item in storeValues)
                {
                    if (item is not HttpHeaders.InvalidValue)
                    {
                        count++;
                    }
                }
                return count;
            }
        }
    }
}
