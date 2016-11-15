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

    [Serializable]
    public class WebHeaderCollection : NameValueCollection, ISerializable
    {
        private const int ApproxAveHeaderLineSize = 30;
        private const int ApproxHighAvgNumHeaders = 16;
        private WebHeaderCollectionType _type;
        private NameValueCollection _innerCollection;

        private static HeaderInfoTable _headerInfo;

        protected WebHeaderCollection(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            int count = serializationInfo.GetInt32("Count");
            for (int i = 0; i < count; i++)
            {
                string headerName = serializationInfo.GetString(i.ToString(NumberFormatInfo.InvariantInfo));
                string headerValue = serializationInfo.GetString((i + count).ToString(NumberFormatInfo.InvariantInfo));
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"calling InnerCollection.Add() key:[{headerName}], value:[{headerValue}]");
                InnerCollection.Add(headerName, headerValue);
            }
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
            //
            // for now disregard streamingContext.
            //
            serializationInfo.AddValue("Count", Count);
            for (int i = 0; i < Count; i++)
            {
                serializationInfo.AddValue(i.ToString(NumberFormatInfo.InvariantInfo), GetKey(i));
                serializationInfo.AddValue((i + Count).ToString(NumberFormatInfo.InvariantInfo), Get(i));
            }
        }

        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            GetObjectData(serializationInfo, streamingContext);
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

        public override string[] GetValues(string header)
        {
            return InnerCollection.GetValues(header);
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
            if (string.IsNullOrWhiteSpace(header))
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
            if (string.IsNullOrWhiteSpace(name))
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
