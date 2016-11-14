// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;

namespace System.Net
{
    /// <devdoc>
    ///    <para>
    ///    An HTTP-specific implementation of the
    ///    <see cref='System.Net.WebResponse'/> class.
    /// </para>
    /// </devdoc>
    [Serializable]
    public class HttpWebResponse : WebResponse, ISerializable
    {
        private HttpResponseMessage _httpResponseMessage;
        private Uri _requestUri;
        private CookieCollection _cookies;
        private WebHeaderCollection _webHeaderCollection = null;
        private string _characterSet = null;
        private bool _isVersionHttp11 = true;

        public HttpWebResponse() { }

        protected HttpWebResponse(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
            _webHeaderCollection = (WebHeaderCollection)serializationInfo.GetValue("_HttpResponseHeaders", typeof(WebHeaderCollection));
            _requestUri = (Uri)serializationInfo.GetValue("_Uri", typeof(Uri));
            Version version = (Version)serializationInfo.GetValue("_Version", typeof(Version));
            _isVersionHttp11 = version.Equals(HttpVersion.Version11);            
            ContentLength = serializationInfo.GetInt64("_ContentLength");                        
        }
     
        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            GetObjectData(serializationInfo, streamingContext);
        }

        protected override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {           
            serializationInfo.AddValue("_HttpResponseHeaders", _webHeaderCollection, typeof(WebHeaderCollection));
            serializationInfo.AddValue("_Uri", _requestUri, typeof(Uri));
            serializationInfo.AddValue("_Version", ProtocolVersion, typeof(Version));
            serializationInfo.AddValue("_StatusCode", StatusCode);
            serializationInfo.AddValue("_ContentLength", ContentLength);
            serializationInfo.AddValue("_Verb", Method);
            serializationInfo.AddValue("_StatusDescription", StatusDescription);            
            base.GetObjectData(serializationInfo, streamingContext);
        }


        internal HttpWebResponse(HttpResponseMessage _message, Uri requestUri, CookieContainer cookieContainer)
        {
            _httpResponseMessage = _message;
            _requestUri = requestUri;

            // Match Desktop behavior. If the request didn't set a CookieContainer, we don't populate the response's CookieCollection.
            if (cookieContainer != null)
            {
                _cookies = cookieContainer.GetCookies(requestUri);
            }
            else
            {
                _cookies = new CookieCollection();
            }
        }
        public override bool IsMutuallyAuthenticated
        {
            get
            {
                return base.IsMutuallyAuthenticated;
            }
        }

        public override long ContentLength
        {
            get
            {
                CheckDisposed();
                long? length = _httpResponseMessage.Content.Headers.ContentLength;
                return length.HasValue ? length.Value : -1;
            }
        }

        public override string ContentType
        {
            get
            {
                CheckDisposed();

                // We use TryGetValues() instead of the strongly type Headers.ContentType property so that
                // we return a string regardless of it being fully RFC conformant. This matches current
                // .NET Framework behavior.
                IEnumerable<string> values;
                if (_httpResponseMessage.Content.Headers.TryGetValues("Content-Type", out values))
                {
                    // In most cases, there is only one media type value as per RFC. But for completeness, we
                    // return all values in cases of overly malformed strings.
                    var builder = new StringBuilder();
                    int ndx = 0;
                    foreach (string value in values)
                    {
                        if (ndx > 0)
                        {
                            builder.Append(',');
                        }
                        
                        builder.Append(value);
                        ndx++;
                    }

                    return builder.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
        }

     
        public String ContentEncoding
        {
            get
            {
                CheckDisposed();
                return GetHeaderValueAsString(_httpResponseMessage.Content.Headers.ContentEncoding);
            }
        }

        public virtual CookieCollection Cookies
        {
            get
            {
                CheckDisposed();
                return _cookies;
            }

            set
            {
                CheckDisposed();
                _cookies = value;
            }
        }
      
        public DateTime LastModified
        {
            get
            {
                CheckDisposed();
                string lastmodHeaderValue = Headers["Last-Modified"];
                if (string.IsNullOrEmpty(lastmodHeaderValue))
                {
                    return DateTime.Now;
                }
                DateTime dtOut;
                HttpDateParse.ParseHttpDate(lastmodHeaderValue, out dtOut);
                return dtOut;
            }
        }


        /// <devdoc>
        ///    <para>
        ///       Gets the name of the server that sent the response.
        ///    </para>
        /// </devdoc>
        public string Server
        {
            get
            {
                CheckDisposed();                
                return string.IsNullOrEmpty( Headers["Server"])?  string.Empty : Headers["Server"];
            }
        }


        // HTTP Version
        /// <devdoc>
        ///    <para>
        ///       Gets
        ///       the version of the HTTP protocol used in the response.
        ///    </para>
        /// </devdoc>
        public Version ProtocolVersion
        {
            get
            {
                CheckDisposed();
                return _isVersionHttp11 ? HttpVersion.Version11 : HttpVersion.Version10;
            }
        }

        public override WebHeaderCollection Headers
        {
            get
            {
                CheckDisposed();
                if (_webHeaderCollection == null)
                {
                    _webHeaderCollection = new WebHeaderCollection();

                    foreach (var header in _httpResponseMessage.Headers)
                    {
                        _webHeaderCollection[header.Key] = GetHeaderValueAsString(header.Value);
                    }

                    if (_httpResponseMessage.Content != null)
                    {
                        foreach (var header in _httpResponseMessage.Content.Headers)
                        {
                            _webHeaderCollection[header.Key] = GetHeaderValueAsString(header.Value);
                        }
                    }
                }
                return _webHeaderCollection;
            }
        }

        public virtual string Method
        {
            get
            {
                CheckDisposed();
                return _httpResponseMessage.RequestMessage.Method.Method;
            }
        }

        public override Uri ResponseUri
        {
            get
            {
                CheckDisposed();

                // The underlying System.Net.Http API will automatically update
                // the .RequestUri property to be the final URI of the response.
                return _httpResponseMessage.RequestMessage.RequestUri;
            }
        }

        public virtual HttpStatusCode StatusCode
        {
            get
            {
                CheckDisposed();
                return _httpResponseMessage.StatusCode;
            }
        }

        public virtual string StatusDescription
        {
            get
            {
                CheckDisposed();
                return _httpResponseMessage.ReasonPhrase;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string CharacterSet
        {
            get
            {
                CheckDisposed();                                
                string contentType = Headers["Content-Type"];

                if (_characterSet == null && !string.IsNullOrWhiteSpace(contentType))
                {

                    //sets characterset so the branch is never executed again.
                    _characterSet = String.Empty;

                    //first string is the media type
                    string srchString = contentType.ToLower();

                    //media subtypes of text type has a default as specified by rfc 2616
                    if (srchString.Trim().StartsWith("text/"))
                    {
                        _characterSet = "ISO-8859-1";
                    }

                    //one of the parameters may be the character set
                    //there must be at least a mediatype for this to be valid
                    int i = srchString.IndexOf(";");
                    if (i > 0)
                    {

                        //search the parameters
                        while ((i = srchString.IndexOf("charset", i)) >= 0)
                        {

                            i += 7;

                            //make sure the word starts with charset
                            if (srchString[i - 8] == ';' || srchString[i - 8] == ' ')
                            {

                                //skip whitespace
                                while (i < srchString.Length && srchString[i] == ' ')
                                    i++;

                                //only process if next character is '='
                                //and there is a character after that
                                if (i < srchString.Length - 1 && srchString[i] == '=')
                                {
                                    i++;

                                    //get and trim character set substring
                                    int j = srchString.IndexOf(';', i);
                                    //In the past we used
                                    //Substring(i, j). J is the offset not the length
                                    //the qfe is to fix the second parameter so that this it is the
                                    //length. since j points to the next ; the operation j -i
                                    //gives the length of the charset
                                    if (j > i)
                                        _characterSet = contentType.Substring(i, j - i).Trim();
                                    else
                                        _characterSet = contentType.Substring(i).Trim();

                                    //done
                                    break;
                                }
                            }
                        }
                    }
                }
                return _characterSet;
            }
        }

        public override bool SupportsHeaders
        {
            get
            {
                return true;
            }
        }       

        public override Stream GetResponseStream()
        {
            CheckDisposed();
            return _httpResponseMessage.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        }

        public string GetResponseHeader(string headerName)
        {
            CheckDisposed();
            string headerValue = Headers[headerName];
            return ((headerValue == null) ? String.Empty : headerValue);
        }

        public override void Close()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            var httpResponseMessage = _httpResponseMessage;
            if (httpResponseMessage != null)
            {
                httpResponseMessage.Dispose();
                _httpResponseMessage = null;
            }
        }

        private void CheckDisposed()
        {
            if (_httpResponseMessage == null)
            {
                throw new ObjectDisposedException(this.GetType().ToString());
            }
        }

        private string GetHeaderValueAsString(IEnumerable<string> values)
        {
            // There is always at least one value even if it is an empty string.
            var enumerator = values.GetEnumerator();
            bool success = enumerator.MoveNext();
            Debug.Assert(success, "There should be at least one value");

            string headerValue = enumerator.Current;

            if (enumerator.MoveNext())
            {
                // Multi-valued header
                var buffer = new StringBuilder(headerValue);

                do
                {
                    buffer.Append(", ");
                    buffer.Append(enumerator.Current);
                } while (enumerator.MoveNext());

                return buffer.ToString();
            }

            return headerValue;
        }
    }
}
