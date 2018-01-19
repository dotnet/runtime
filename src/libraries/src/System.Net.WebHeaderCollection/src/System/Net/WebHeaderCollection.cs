// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        HttpWebRequest,
        HttpWebResponse,
        HttpListenerRequest,
        HttpListenerResponse,
        FtpWebRequest,
        FtpWebResponse,
        FileWebRequest,
        FileWebResponse,
    }

    public class WebHeaderCollection : NameValueCollection, ISerializable
    {
        private const int ApproxAveHeaderLineSize = 30;
        private const int ApproxHighAvgNumHeaders = 16;
        private WebHeaderCollectionType _type;
        private NameValueCollection _innerCollection;

        private static HeaderInfoTable _headerInfo;

        protected WebHeaderCollection(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        private bool AllowHttpRequestHeader
        {
            get
            {
                if (_type == WebHeaderCollectionType.Unknown)
                {
                    _type = WebHeaderCollectionType.WebRequest;
                }
                return _type == WebHeaderCollectionType.WebRequest ||
                      _type == WebHeaderCollectionType.HttpWebRequest ||
                      _type == WebHeaderCollectionType.HttpListenerRequest;
            }
        }

        private static HeaderInfoTable HeaderInfo
        {
            get
            {
                if (_headerInfo == null)
                {
                    _headerInfo = new HeaderInfoTable();
                }
                return _headerInfo;
            }
        }

        private NameValueCollection InnerCollection
        {
            get
            {
                if (_innerCollection == null)
                    _innerCollection = new NameValueCollection(ApproxHighAvgNumHeaders, CaseInsensitiveAscii.StaticInstance);
                return _innerCollection;
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
                return _type == WebHeaderCollectionType.WebResponse ||
                       _type == WebHeaderCollectionType.HttpWebResponse ||
                       _type == WebHeaderCollectionType.HttpListenerResponse;
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

        public override void Set(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            name = HttpValidationHelpers.CheckBadHeaderNameChars(name);
            ThrowOnRestrictedHeader(name);
            value = HttpValidationHelpers.CheckBadHeaderValueChars(value);
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"calling InnerCollection.Set() key:[{name}], value:[{value}]");
            if (_type == WebHeaderCollectionType.WebResponse)
            {
                if (value != null && value.Length > ushort.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, string.Format(CultureInfo.InvariantCulture,SR.net_headers_toolong, ushort.MaxValue));
                }
            }
            InvalidateCachedArrays();
            InnerCollection.Set(name, value);
        }

        public void Set(HttpRequestHeader header, string value)
        {
            if (!AllowHttpRequestHeader)
            {
                throw new InvalidOperationException(SR.net_headers_req);
            }
            this.Set(header.GetName(), value);
        }

        public void Set(HttpResponseHeader header, string value)
        {
            if (!AllowHttpResponseHeader)
            {
                throw new InvalidOperationException(SR.net_headers_rsp);
            }
            if (_type == WebHeaderCollectionType.WebResponse)
            {
                if (value != null && value.Length > ushort.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, string.Format(CultureInfo.InvariantCulture, SR.net_headers_toolong, ushort.MaxValue));
                }
            }
            this.Set(header.GetName(), value);
        }

        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        public void Remove(HttpRequestHeader header)
        {
            if (!AllowHttpRequestHeader)
            {
                throw new InvalidOperationException(SR.net_headers_req);
            }
            this.Remove(header.GetName());
        }

        public void Remove(HttpResponseHeader header)
        {
            if (!AllowHttpResponseHeader)
            {
                throw new InvalidOperationException(SR.net_headers_rsp);
            }
            this.Remove(header.GetName());
        }

        public override void OnDeserialization(object sender)
        {
            // Nop in desktop 
        }
             
        public static bool IsRestricted(string headerName)
        {
            return IsRestricted(headerName, false);
        }

        public static bool IsRestricted(string headerName, bool response)
        {
            headerName =  HttpValidationHelpers.CheckBadHeaderNameChars(headerName);
            return response ? HeaderInfo[headerName].IsResponseRestricted : HeaderInfo[headerName].IsRequestRestricted;
        }

        public override string[] GetValues(int index)
        {
            return InnerCollection.GetValues(index);
        }

        // GetValues
        // Routine Description:
        //     This method takes a header name and returns a string array representing
        //     the individual values for that headers. For example, if the headers
        //     contained the line Accept: text/plain, text/html then
        //     GetValues("Accept") would return an array of two strings: "text/plain"
        //     and "text/html".
        // Arguments:
        //     header      - Name of the header.
        // Return Value:
        //     string[] - array of parsed string objects
        public override string[] GetValues(string header)
        {
            // First get the information about the header and the values for
            // the header.
            HeaderInfo info = HeaderInfo[header];
            string[] values = InnerCollection.GetValues(header);
            // If we have no information about the header or it doesn't allow
            // multiple values, just return the values.
            if (info == null || values == null || !info.AllowMultiValues)
            {
                return values;
            }
            // Here we have a multi value header. We need to go through
            // each entry in the multi values array, and if an entry itself
            // has multiple values we'll need to combine those in.
            //
            // We do some optimazation here, where we try not to copy the
            // values unless there really is one that have multiple values.
            string[] tempValues;
            List<string> valueList = null;
            for (int i = 0; i < values.Length; i++)
            {
                // Parse this value header.
                tempValues = info.Parser(values[i]);
                // If we don't have an array list yet, see if this
                // value has multiple values.
                if (valueList == null)
                {
                    // If it's not empty, replace valueList.
                    // Because for invalid WebRequest headers, we will return empty
                    // valueList instead of the default NameValueCollection.GetValues().
                    if (tempValues != null)
                    {
                        // It does, so we need to create an array list that
                        // represents the Values, then trim out this one and
                        // the ones after it that haven't been parsed yet.
                        valueList = new List<string>(values);
                        valueList.RemoveRange(i, values.Length - i);
                        valueList.AddRange(tempValues);
                    }
                }
                else
                {
                    // We already have an List, so just add the values.
                    valueList.AddRange(tempValues);
                }
            }
            // See if we have an List. If we don't, just return the values.
            // Otherwise convert the List to a string array and return that.
            if (valueList != null)
            {
                return valueList.ToArray();
            }
            return values;
        }

        public override string GetKey(int index)
        {
            return InnerCollection.GetKey(index);
        }

        public override void Clear()
        {
            InvalidateCachedArrays();
            if (_innerCollection != null)
            {
                _innerCollection.Clear();
            }
        }

        public override string Get(int index)
        {
            if (_innerCollection == null)
            {
                return null;
            }
            return _innerCollection.Get(index);
        }

        public override string Get(string name)
        {
            if (_innerCollection == null)
            {
                return null;
            }
            return _innerCollection.Get(name);
        }

        public void Add(HttpRequestHeader header, string value)
        {
            if (!AllowHttpRequestHeader)
            {
                throw new InvalidOperationException(SR.net_headers_req);
            }
            this.Add(header.GetName(), value);
        }

        public void Add(HttpResponseHeader header, string value)
        {
            if (!AllowHttpResponseHeader)
            {
                throw new InvalidOperationException(SR.net_headers_rsp);
            }
            if (_type == WebHeaderCollectionType.WebResponse)
            {
                if (value != null && value.Length > ushort.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, string.Format(CultureInfo.InvariantCulture, SR.net_headers_toolong, ushort.MaxValue));
                }
            }
            this.Add(header.GetName(), value);
        }

        public void Add(string header)
        {
            if (string.IsNullOrEmpty(header))
            {
                throw new ArgumentNullException(nameof(header));
            }
            int colpos = header.IndexOf(':');
            // check for badly formed header passed in
            if (colpos < 0)
            {
                throw new ArgumentException(SR.net_WebHeaderMissingColon, nameof(header));
            }
            string name = header.Substring(0, colpos);
            string value = header.Substring(colpos + 1);
            name = HttpValidationHelpers.CheckBadHeaderNameChars(name);
            ThrowOnRestrictedHeader(name);
            value = HttpValidationHelpers.CheckBadHeaderValueChars(value);
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"Add({header}) calling InnerCollection.Add() key:[{name}], value:[{value}]");
            if (_type == WebHeaderCollectionType.WebResponse)
            {
                if (value != null && value.Length > ushort.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, string.Format(CultureInfo.InvariantCulture, SR.net_headers_toolong, ushort.MaxValue));
                }
            }
            InvalidateCachedArrays();
            InnerCollection.Add(name, value);
        }

        public override void Add(string name, string value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.net_emptyStringCall, nameof(name)), nameof(name));
            }

            name = HttpValidationHelpers.CheckBadHeaderNameChars(name);
            ThrowOnRestrictedHeader(name);
            value = HttpValidationHelpers.CheckBadHeaderValueChars(value);
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"calling InnerCollection.Add() key:[{name}], value:[{value}]");
            if (_type == WebHeaderCollectionType.WebResponse)
            {
                if (value != null && value.Length > ushort.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,string.Format(CultureInfo.InvariantCulture, SR.net_headers_toolong, ushort.MaxValue));
                }
            }
            InvalidateCachedArrays();
            InnerCollection.Add(name, value);
        }

        protected void AddWithoutValidate(string headerName, string headerValue)
        {
            headerName = HttpValidationHelpers.CheckBadHeaderNameChars(headerName);
            headerValue = HttpValidationHelpers.CheckBadHeaderValueChars(headerValue);
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"calling InnerCollection.Add() key:[{headerName}], value:[{headerValue}]");
            if (_type == WebHeaderCollectionType.WebResponse)
            {
                if (headerValue != null && headerValue.Length > ushort.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(headerValue), headerValue, string.Format(CultureInfo.InvariantCulture, SR.net_headers_toolong, ushort.MaxValue));
                }
            }
            InvalidateCachedArrays();
            InnerCollection.Add(headerName, headerValue);
        }

        internal void ThrowOnRestrictedHeader(string headerName)
        {
            if (_type == WebHeaderCollectionType.HttpWebRequest)
            {
                if (HeaderInfo[headerName].IsRequestRestricted)
                {
                    throw new ArgumentException(string.Format(SR.net_headerrestrict, headerName), nameof(headerName));
                }
            }
            else if (_type == WebHeaderCollectionType.HttpListenerResponse)
            {
                if (HeaderInfo[headerName].IsResponseRestricted)
                {
                    throw new ArgumentException(string.Format(SR.net_headerrestrict, headerName), nameof(headerName));
                }
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
        public override void Remove(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            ThrowOnRestrictedHeader(name);
            name = HttpValidationHelpers.CheckBadHeaderNameChars(name);
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"calling InnerCollection.Remove() key:[{name}]");
            if (_innerCollection != null)
            {
                InvalidateCachedArrays();
                _innerCollection.Remove(name);
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

            var sb = new StringBuilder(ApproxAveHeaderLineSize * Count);

            foreach (string key in InnerCollection)
            {
                string val = InnerCollection.Get(key);
                sb.Append(key)
                    .Append(": ")
                    .Append(val)
                    .Append("\r\n");
            }

            sb.Append("\r\n");
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"ToString: {sb}");
            return sb.ToString();
        }

        public byte[] ToByteArray()
        {
            string tempString = this.ToString();
            return System.Text.Encoding.ASCII.GetBytes(tempString);
        }

        public WebHeaderCollection()
        {
        }

        public override int Count
        {
            get
            {
                return (_innerCollection == null ? 0 : _innerCollection.Count);
            }
        }

        public override KeysCollection Keys
        {
            get
            {
                return InnerCollection.Keys;
            }
        }

        public override string[] AllKeys
        {
            get
            {
                return InnerCollection.AllKeys;
            }
        }

        public override IEnumerator GetEnumerator()
        {
            return InnerCollection.Keys.GetEnumerator();
        }
    }
}
