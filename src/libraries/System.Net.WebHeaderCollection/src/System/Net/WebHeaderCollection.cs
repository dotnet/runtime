// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace System.Net
{
    internal enum WebHeaderCollectionType : ushort
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

    /// <devdoc>
    ///    <para>
    ///       Contains protocol headers associated with a
    ///       request or response.
    ///    </para>
    /// </devdoc>
    public class WebHeaderCollection : NameValueCollection, IEnumerable
    {
        // Data and Constants
        private const int ApproxAveHeaderLineSize = 30;
        private const int ApproxHighAvgNumHeaders = 16;

        private int _numCommonHeaders;

        // Grouped by first character, so lookup is faster.  The table s_commonHeaderHints maps first letters to indexes in this array.
        // After first character, sort by decreasing length.  It's ok if two headers have the same first character and length.
        private static readonly string[] s_commonHeaderNames = {
            HttpKnownHeaderNames.AcceptRanges,      // "Accept-Ranges"       13
            HttpKnownHeaderNames.ContentLength,     // "Content-Length"      14
            HttpKnownHeaderNames.CacheControl,      // "Cache-Control"       13
            HttpKnownHeaderNames.ContentType,       // "Content-Type"        12
            HttpKnownHeaderNames.Date,              // "Date"                 4 
            HttpKnownHeaderNames.Expires,           // "Expires"              7
            HttpKnownHeaderNames.ETag,              // "ETag"                 4
            HttpKnownHeaderNames.LastModified,      // "Last-Modified"       13
            HttpKnownHeaderNames.Location,          // "Location"             8
            HttpKnownHeaderNames.ProxyAuthenticate, // "Proxy-Authenticate"  18
            HttpKnownHeaderNames.P3P,               // "P3P"                  3
            HttpKnownHeaderNames.SetCookie2,        // "Set-Cookie2"         11
            HttpKnownHeaderNames.SetCookie,         // "Set-Cookie"          10
            HttpKnownHeaderNames.Server,            // "Server"               6
            HttpKnownHeaderNames.Via,               // "Via"                  3
            HttpKnownHeaderNames.WWWAuthenticate,   // "WWW-Authenticate"    16
            HttpKnownHeaderNames.XAspNetVersion,    // "X-AspNet-Version"    16
            HttpKnownHeaderNames.XPoweredBy,        // "X-Powered-By"        12
            "["                                     // This sentinel will never match.  (This character isn't in the hint table.)
        };

        // Mask off all but the bottom five bits, and look up in this array.
        private static readonly sbyte[] s_commonHeaderHints = new sbyte[] {
            -1,  0, -1,  1,  4,  5, -1, -1,   // - a b c d e f g
            -1, -1, -1, -1,  7, -1, -1, -1,   // h i j k l m n o
             9, -1, -1, 11, -1, -1, 14, 15,   // p q r s t u v w
            16, -1, -1, -1, -1, -1, -1, -1 }; // x y z [ - - - -

        // To ensure C++ and IL callers can't pollute the underlying collection by calling overridden base members directly, we
        // will use a member collection instead.
        private NameValueCollection _innerCollection;

        private NameValueCollection InnerCollection
        {
            get
            {
                if (_innerCollection == null)
                {
                    _innerCollection = new NameValueCollection(ApproxHighAvgNumHeaders, StringComparer.OrdinalIgnoreCase);
                }
                return _innerCollection;
            }
        }

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
                return _type == WebHeaderCollectionType.WebRequest || _type == WebHeaderCollectionType.HttpWebRequest || _type == WebHeaderCollectionType.HttpListenerRequest;
            }
        }

        internal bool AllowHttpResponseHeader
        {
            get
            {
                if (_type == WebHeaderCollectionType.Unknown)
                {
                    _type = WebHeaderCollectionType.WebResponse;
                }
                return _type == WebHeaderCollectionType.WebResponse || _type == WebHeaderCollectionType.HttpWebResponse || _type == WebHeaderCollectionType.HttpListenerResponse;
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
                if (_type == WebHeaderCollectionType.HttpListenerResponse)
                {
                    if (value != null && value.Length > ushort.MaxValue)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), value, SR.Format(SR.net_headers_toolong, ushort.MaxValue));
                    }
                }
                this[header.GetName()] = value;
            }
        }

        private static readonly char[] s_httpTrimCharacters = new char[] { (char)0x09, (char)0xA, (char)0xB, (char)0xC, (char)0xD, (char)0x20 };

        private static readonly char[] s_invalidParamChars = new char[] { '(', ')', '<', '>', '@', ',', ';', ':', '\\', '"', '\'', '/', '[', ']', '?', '=', '{', '}', ' ', '\t', '\r', '\n' };

        // CheckBadChars - throws on invalid chars to be not found in header name/value
        internal static string CheckBadChars(string name, bool isHeaderValue)
        {
            if (name == null || name.Length == 0)
            {
                // empty name is invalid
                if (!isHeaderValue)
                {
                    throw name == null ? new ArgumentNullException(nameof(name)) :
                        new ArgumentException(SR.Format(SR.net_emptystringcall, "name"), nameof(name));
                }
                // empty value is OK
                return string.Empty;
            }

            if (isHeaderValue)
            {
                // VALUE check
                // Trim spaces from both ends
                name = name.Trim(s_httpTrimCharacters);

                // First, check for correctly formed multi-line value
                // Second, check for absence of CTL characters
                int crlf = 0;
                for (int i = 0; i < name.Length; ++i)
                {
                    char c = (char)(0x000000ff & (uint)name[i]);
                    switch (crlf)
                    {
                        case 0:
                            if (c == '\r')
                            {
                                crlf = 1;
                            }
                            else if (c == '\n')
                            {
                                // Technically this is bad HTTP, but we want to be permissive in what we accept.
                                // It is important to note that it would be a breaking change to reject this.
                                crlf = 2;
                            }
                            else if (c == 127 || (c < ' ' && c != '\t'))
                            {
                                throw new ArgumentException(SR.Format(SR.net_WebHeaderInvalidControlChars, "value"));
                            }
                            break;

                        case 1:
                            if (c == '\n')
                            {
                                crlf = 2;
                                break;
                            }
                            throw new ArgumentException(SR.Format(SR.net_WebHeaderInvalidCRLFChars, "value"));

                        case 2:
                            if (c == ' ' || c == '\t')
                            {
                                crlf = 0;
                                break;
                            }
                            throw new ArgumentException(SR.Format(SR.net_WebHeaderInvalidCRLFChars, "value"));
                    }
                }
                if (crlf != 0)
                {
                    throw new ArgumentException(SR.Format(SR.net_WebHeaderInvalidCRLFChars, "value"));
                }
            }
            else
            {
                // NAME check
                // First, check for absence of separators and spaces
                if (HttpValidationHelpers.IsInvalidMethodOrHeaderString(name))
                {
                    throw new ArgumentException(SR.Format(SR.net_WebHeaderInvalidHeaderChars, "name"));
                }

                // Second, check for non CTL ASCII-7 characters (32-126)
                if (ContainsNonAsciiChars(name))
                {
                    throw new ArgumentException(SR.Format(SR.net_WebHeaderInvalidNonAsciiChars, "name"));
                }
            }
            return name;
        }

        internal static bool ContainsNonAsciiChars(string token)
        {
            for (int i = 0; i < token.Length; ++i)
            {
                if ((token[i] < 0x20) || (token[i] > 0x7e))
                {
                    return true;
                }
            }
            return false;
        }

        // ThrowOnRestrictedHeader - generates an error if the user,
        // passed in a reserved string as the header name
        internal void ThrowOnRestrictedHeader(string headerName)
        {
            if (_type == WebHeaderCollectionType.HttpWebRequest)
            {
                if (HeaderInfoTable.GetKnownHeaderInfo(headerName).IsRequestRestricted)
                {
                    throw new ArgumentException(SR.Format(SR.net_headerrestrict, headerName), "name");
                }
            }
            else if (_type == WebHeaderCollectionType.HttpListenerResponse)
            {
                if (HeaderInfoTable.GetKnownHeaderInfo(headerName).IsResponseRestricted)
                {
                    throw new ArgumentException(SR.Format(SR.net_headerrestrict, headerName), "name");
                }
            }
        }

        // Our public METHOD set, most are inherited from NameValueCollection,
        // not all methods from NameValueCollection are listed, even though usable -
        //
        // This includes:
        // - Add(name, value)
        // - Add(header)
        // - this[name] {set, get}
        // - Remove(name), returns bool
        // - Remove(name), returns void
        // - Set(name, value)
        // - ToString()

        // Add -
        //  Routine Description:
        //      Adds headers with validation to see if they are "proper" headers.
        //      Will cause header to be concatenated to existing if already found.
        //      If the header is a special header, listed in RestrictedHeaders object,
        //      then this call will cause an exception indicating as such.
        //  Arguments:
        //      name - header-name to add
        //      value - header-value to add; if a header already exists, this value will be concatenated
        //  Return Value:
        //      None

        /// <devdoc>
        ///    <para>
        ///       Adds a new header with the indicated name and value.
        ///    </para>
        /// </devdoc>
        public override void Add(string name, string value)
        {
            name = CheckBadChars(name, false);
            ThrowOnRestrictedHeader(name);
            value = CheckBadChars(value, true);
            if (_type == WebHeaderCollectionType.HttpListenerResponse)
            {
                if (value != null && value.Length > ushort.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.Format(SR.net_headers_toolong, ushort.MaxValue));
                }
            }
            InvalidateCachedArrays();
            InnerCollection.Add(name, value);
        }

        // Set -
        // Routine Description:
        //     Sets headers with validation to see if they are "proper" headers.
        //     If the header is a special header, listed in RestrictedHeaders object,
        //     then this call will cause an exception indicating as such.
        // Arguments:
        //     name - header-name to set
        //     value - header-value to set
        // Return Value:
        //     None

        /// <devdoc>
        ///    <para>
        ///       Sets the specified header to the specified value.
        ///    </para>
        /// </devdoc>
        public override void Set(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            name = CheckBadChars(name, false);
            ThrowOnRestrictedHeader(name);
            value = CheckBadChars(value, true);
            if (_type == WebHeaderCollectionType.HttpListenerResponse)
            {
                if (value != null && value.Length > ushort.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.Format(SR.net_headers_toolong, ushort.MaxValue));
                }
            }
            InvalidateCachedArrays();
            InnerCollection.Set(name, value);
        }

        // Remove -
        // Routine Description:
        //     Removes give header with validation to see if they are "proper" headers.
        //     If the header is a speical header, listed in RestrictedHeaders object,
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
            name = CheckBadChars(name, false);
            if (_innerCollection != null)
            {
                InvalidateCachedArrays();
                _innerCollection.Remove(name);
            }
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

        /// <devdoc>
        ///    <para>
        ///       Gets an array of header values stored in a
        ///       header.
        ///    </para>
        /// </devdoc>
        public override string[] GetValues(string header)
        {
            // First get the information about the header and the values for
            // the header.
            HeaderInfo info = HeaderInfoTable.GetKnownHeaderInfo(header);
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
                    // See if it has multiple values.
                    if (tempValues.Length > 1)
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

        /// <internalonly/>
        /// <devdoc>
        ///    <para>
        ///       Obsolete.
        ///    </para>
        /// </devdoc>
        public override string ToString()
        {
            string result = GetAsString(this);
            return result;
        }

        internal static string GetAsString(NameValueCollection cc)
        {
            if (cc == null || cc.Count == 0)
            {
                return "\r\n";
            }
            StringBuilder sb = new StringBuilder(ApproxAveHeaderLineSize * cc.Count);
            string statusLine;
            statusLine = cc[string.Empty];
            if (statusLine != null)
            {
                sb.Append(statusLine).Append("\r\n");
            }
            for (int i = 0; i < cc.Count; i++)
            {
                string key = cc.GetKey(i) as string;
                string val = cc.Get(i) as string;
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }
                sb.Append(key)
                    .Append(": ")
                    .Append(val)
                    .Append("\r\n");
            }
            sb.Append("\r\n");
            return sb.ToString();
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Net.WebHeaderCollection'/>
        ///       class.
        ///    </para>
        /// </devdoc>
        public WebHeaderCollection() : base()
        {
        }

        // Override Get() to check the common headers.
        public override string Get(string name)
        {
            // Fall back to normal lookup.
            if (_innerCollection == null)
            {
                return null;
            }
            return _innerCollection.Get(name);
        }

        public override int Count
        {
            get
            {
                return (_innerCollection == null ? 0 : _innerCollection.Count) + _numCommonHeaders;
            }
        }

        public override KeysCollection Keys
        {
            get
            {
                return InnerCollection.Keys;
            }
        }

        public override string Get(int index)
        {
            return InnerCollection.Get(index);
        }

        public override string[] GetValues(int index)
        {
            return InnerCollection.GetValues(index);
        }

        public override string GetKey(int index)
        {
            return InnerCollection.GetKey(index);
        }

        public override string[] AllKeys
        {
            get
            {
                return InnerCollection.AllKeys;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override IEnumerator GetEnumerator()
        {
            return InnerCollection.Keys.GetEnumerator();
        }

        public override void Clear()
        {
            _numCommonHeaders = 0;
            InvalidateCachedArrays();
            if (_innerCollection != null)
            {
                _innerCollection.Clear();
            }
        }
    }
}
