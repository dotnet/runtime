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

        protected void AddWithoutValidate(string name, string value)
        {
            name = HttpValidationHelpers.CheckBadHeaderNameChars(name);
            value = HttpValidationHelpers.CheckBadHeaderValueChars(value);
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"calling InnerCollection.Add() key:[{name}], value:[{value}]");
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

    internal class CaseInsensitiveAscii : IEqualityComparer, IComparer
    {
        // ASCII char ToLower table
        internal static readonly CaseInsensitiveAscii StaticInstance = new CaseInsensitiveAscii();
        internal static readonly byte[] AsciiToLower = new byte[] {
              0,  1,  2,  3,  4,  5,  6,  7,  8,  9,
             10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
             20, 21, 22, 23, 24, 25, 26, 27, 28, 29,
             30, 31, 32, 33, 34, 35, 36, 37, 38, 39,
             40, 41, 42, 43, 44, 45, 46, 47, 48, 49,
             50, 51, 52, 53, 54, 55, 56, 57, 58, 59,
             60, 61, 62, 63, 64, 97, 98, 99,100,101, //  60, 61, 62, 63, 64, 65, 66, 67, 68, 69,
            102,103,104,105,106,107,108,109,110,111, //  70, 71, 72, 73, 74, 75, 76, 77, 78, 79,
            112,113,114,115,116,117,118,119,120,121, //  80, 81, 82, 83, 84, 85, 86, 87, 88, 89,
            122, 91, 92, 93, 94, 95, 96, 97, 98, 99, //  90, 91, 92, 93, 94, 95, 96, 97, 98, 99,
            100,101,102,103,104,105,106,107,108,109,
            110,111,112,113,114,115,116,117,118,119,
            120,121,122,123,124,125,126,127,128,129,
            130,131,132,133,134,135,136,137,138,139,
            140,141,142,143,144,145,146,147,148,149,
            150,151,152,153,154,155,156,157,158,159,
            160,161,162,163,164,165,166,167,168,169,
            170,171,172,173,174,175,176,177,178,179,
            180,181,182,183,184,185,186,187,188,189,
            190,191,192,193,194,195,196,197,198,199,
            200,201,202,203,204,205,206,207,208,209,
            210,211,212,213,214,215,216,217,218,219,
            220,221,222,223,224,225,226,227,228,229,
            230,231,232,233,234,235,236,237,238,239,
            240,241,242,243,244,245,246,247,248,249,
            250,251,252,253,254,255
        };

        // ASCII string case insensitive hash function
        public int GetHashCode(object myObject)
        {
            string myString = myObject as string;
            if (myObject == null)
            {
                return 0;
            }
            int myHashCode = myString.Length;
            if (myHashCode == 0)
            {
                return 0;
            }
            myHashCode ^= AsciiToLower[(byte)myString[0]] << 24 ^ AsciiToLower[(byte)myString[myHashCode - 1]] << 16;
            return myHashCode;
        }

        // ASCII string case insensitive comparer
        public int Compare(object firstObject, object secondObject)
        {
            string firstString = firstObject as string;
            string secondString = secondObject as string;
            if (firstString == null)
            {
                return secondString == null ? 0 : -1;
            }
            if (secondString == null)
            {
                return 1;
            }
            int result = firstString.Length - secondString.Length;
            int comparisons = result > 0 ? secondString.Length : firstString.Length;
            int difference, index = 0;
            while (index < comparisons)
            {
                difference = (int)(AsciiToLower[firstString[index]] - AsciiToLower[secondString[index]]);
                if (difference != 0)
                {
                    result = difference;
                    break;
                }
                index++;
            }
            return result;
        }

        // ASCII string case insensitive hash function
        int FastGetHashCode(string myString)
        {
            int myHashCode = myString.Length;
            if (myHashCode != 0)
            {
                myHashCode ^= AsciiToLower[(byte)myString[0]] << 24 ^ AsciiToLower[(byte)myString[myHashCode - 1]] << 16;
            }
            return myHashCode;
        }

        // ASCII string case insensitive comparer
        public new bool Equals(object firstObject, object secondObject)
        {
            string firstString = firstObject as string;
            string secondString = secondObject as string;
            if (firstString == null)
            {
                return secondString == null;
            }
            if (secondString != null)
            {
                int index = firstString.Length;
                if (index == secondString.Length)
                {
                    if (FastGetHashCode(firstString) == FastGetHashCode(secondString))
                    {
                        int comparisons = firstString.Length;
                        while (index > 0)
                        {
                            index--;
                            if (AsciiToLower[firstString[index]] != AsciiToLower[secondString[index]])
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
