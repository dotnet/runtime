// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;

namespace System.Net
{
    internal enum WebHeaderCollectionType : byte
    {
        Unknown,
        WebRequest,
        WebResponse,
    }

    [Serializable]
    public class WebHeaderCollection : IEnumerable, ISerializable
    {
        private const int ApproxAveHeaderLineSize = 30;
        private const int ApproxHighAvgNumHeaders = 16;

        // Lazily initialized fields.
        private List<string> _entriesList;
        private Dictionary<string, string> _entriesDictionary;
        private string[] _allKeys;

        // This is the object that created the header collection.
        private WebHeaderCollectionType _type;

        private bool AllowHttpRequestHeader
        {
            get
            {
                if (_type == WebHeaderCollectionType.Unknown)
                {
                    _type = WebHeaderCollectionType.WebRequest;
                }
                return _type == WebHeaderCollectionType.WebRequest;
            }
        }

        private bool AllowHttpResponseHeader
        {
            get
            {
                if (_type == WebHeaderCollectionType.Unknown)
                {
                    _type = WebHeaderCollectionType.WebResponse;
                }
                return _type == WebHeaderCollectionType.WebResponse;
            }
        }

        public string this[HttpRequestHeader header]
        {
            get
            {
                if (!AllowHttpRequestHeader)
                {
                    throw new InvalidOperationException(SR.net_headers_req);
                }
                return this[header.GetName()];
            }
            set
            {
                if (!AllowHttpRequestHeader)
                {
                    throw new InvalidOperationException(SR.net_headers_req);
                }
                this[header.GetName()] = value;
            }
        }

        public string this[HttpResponseHeader header]
        {
            get
            {
                if (!AllowHttpResponseHeader)
                {
                    throw new InvalidOperationException(SR.net_headers_rsp);
                }
                return this[header.GetName()];
            }
            set
            {
                if (!AllowHttpResponseHeader)
                {
                    throw new InvalidOperationException(SR.net_headers_rsp);
                }
                this[header.GetName()] = value;
            }
        }

        public string this[string name]
        {
            get
            {
                string entry = null;
                _entriesDictionary?.TryGetValue(name, out entry);
                return entry;
            }
            set
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentNullException(nameof(name));
                }

                name = HttpValidationHelpers.CheckBadHeaderNameChars(name);
                value = HttpValidationHelpers.CheckBadHeaderValueChars(value);

                InvalidateCachedArray();
                EnsureInitialized();

                if (!_entriesDictionary.ContainsKey(name))
                {
                    Debug.Assert(_entriesList.FindIndex(s => StringComparer.OrdinalIgnoreCase.Equals(s, name)) == -1,
                        $"'{name}' must not be in {nameof(_entriesList)}.");

                    // Only add the name to the list if it isn't already in the dictionary.
                    _entriesList.Add(name);
                }

                _entriesDictionary[name] = value;

                Debug.Assert(_entriesList.FindIndex(s => StringComparer.OrdinalIgnoreCase.Equals(s, name)) != -1,
                    $"'{name}' must be in {nameof(_entriesList)}.");

                Debug.Assert(_entriesDictionary.Count == _entriesList.Count, "Counts must be equal.");
            }
        }       
        

        // Remove -
        // Routine Description:
        //     Removes give header with validation to see if they are "proper" headers.
        //     If the header is a special header, listed in RestrictedHeaders object,
        //     then this call will cause an exception indicating as such.
        // Arguments:
        //     name - header-name to remove
        // Return Value:
        //     None

        /// <devdoc>
        ///    <para>Removes the specified header.</para>
        /// </devdoc>
        public void Remove(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            name = HttpValidationHelpers.CheckBadHeaderNameChars(name);

            if (IsInitialized)
            {
                InvalidateCachedArray();

                _entriesDictionary.Remove(name);

                List<string> list = _entriesList;
                StringComparer comparer = StringComparer.OrdinalIgnoreCase;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (comparer.Equals(name, list[i]))
                    {
                        list.RemoveAt(i);
                        break;
                    }
                }

                Debug.Assert(_entriesList.FindIndex(s => StringComparer.OrdinalIgnoreCase.Equals(s, name)) == -1,
                    $"'{name}' must not be in {nameof(_entriesList)}.");

                Debug.Assert(_entriesDictionary.Count == _entriesList.Count, "Counts must be equal.");
            }
        }

        // ToString()  -
        // Routine Description:
        //     Generates a string representation of the headers, that is ready to be sent except for it being in string format:
        //     the format looks like:
        //
        //     Header-Name: Header-Value\r\n
        //     Header-Name2: Header-Value2\r\n
        //     ...
        //     Header-NameN: Header-ValueN\r\n
        //     \r\n
        //
        //     Uses the string builder class to Append the elements together.
        // Arguments:
        //     None.
        // Return Value:
        //     string
        public override string ToString()
        {
            if (Count == 0)
            {
                return "\r\n";
            }

            Debug.Assert(IsInitialized);

            var sb = new StringBuilder(ApproxAveHeaderLineSize * Count);

            foreach (string key in _entriesList)
            {
                string val = _entriesDictionary[key];
                sb.Append(key)
                    .Append(": ")
                    .Append(val)
                    .Append("\r\n");
            }

            sb.Append("\r\n");
            return sb.ToString();
        }

        public WebHeaderCollection()
        {
        }

        protected WebHeaderCollection(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            int count = serializationInfo.GetInt32("Count");
            for (int i = 0; i < count; i++)
            {
                string headerName = serializationInfo.GetString(i.ToString(NumberFormatInfo.InvariantInfo));
                string headerValue = serializationInfo.GetString((i + count).ToString(NumberFormatInfo.InvariantInfo));
                this[headerName] = headerValue;
            }
        }

        public virtual void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            serializationInfo.AddValue("Count", Count);
            if (_entriesList != null)
            {
                int i = 0;
                foreach (string key in _entriesList)
                {
                    string val = _entriesDictionary[key];
                    serializationInfo.AddValue(i.ToString(NumberFormatInfo.InvariantInfo), key);
                    serializationInfo.AddValue((i + Count).ToString(NumberFormatInfo.InvariantInfo), val);
                    i++;
                }
            }
        }

        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            GetObjectData(serializationInfo, streamingContext);
        }

        public int Count => _entriesList != null ? _entriesList.Count : 0;

        public string[] AllKeys => _allKeys ?? (_allKeys = ToArray(_entriesList));

        IEnumerator IEnumerable.GetEnumerator()
        {
            EnsureInitialized();
            return _entriesList.GetEnumerator();
        }

        private bool IsInitialized => _entriesList != null && _entriesDictionary != null;

        private void EnsureInitialized()
        {
            _entriesList = _entriesList ?? new List<string>(ApproxHighAvgNumHeaders);
            _entriesDictionary = _entriesDictionary ?? new Dictionary<string, string>(ApproxHighAvgNumHeaders, StringComparer.OrdinalIgnoreCase);
        }

        private void InvalidateCachedArray() => _allKeys = null;

        private static string[] ToArray(List<string> list) => list != null ? list.ToArray() : Array.Empty<string>();
    }
}
