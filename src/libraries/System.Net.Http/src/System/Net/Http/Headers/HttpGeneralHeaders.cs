// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.Http.Headers
{
    // The purpose of this type is to extract the handling of general headers in one place rather than duplicating
    // functionality in both HttpRequestHeaders and HttpResponseHeaders.
    internal sealed class HttpGeneralHeaders
    {
        private HttpHeaderValueCollection<string> _connection;
        private HttpHeaderValueCollection<string> _trailer;
        private HttpHeaderValueCollection<TransferCodingHeaderValue> _transferEncoding;
        private HttpHeaderValueCollection<ProductHeaderValue> _upgrade;
        private HttpHeaderValueCollection<ViaHeaderValue> _via;
        private HttpHeaderValueCollection<WarningHeaderValue> _warning;
        private HttpHeaderValueCollection<NameValueHeaderValue> _pragma;
        private HttpHeaders _parent;
        private bool _transferEncodingChunkedSet;
        private bool _connectionCloseSet;

        public CacheControlHeaderValue CacheControl
        {
            get { return (CacheControlHeaderValue)_parent.GetParsedValues(KnownHeaders.CacheControl.Descriptor); }
            set { _parent.SetOrRemoveParsedValue(KnownHeaders.CacheControl.Descriptor, value); }
        }

        public HttpHeaderValueCollection<string> Connection
        {
            get { return ConnectionCore; }
        }

        public bool? ConnectionClose
        {
            get
            {
                // Separated out into a static to enable access to TransferEncodingChunked
                // without the caller needing to force the creation of HttpGeneralHeaders
                // if it wasn't created for other reasons.
                return GetConnectionClose(_parent, this);
            }
            set
            {
                if (value == true)
                {
                    _connectionCloseSet = true;
                    ConnectionCore.SetSpecialValue();
                }
                else
                {
                    _connectionCloseSet = value != null;
                    ConnectionCore.RemoveSpecialValue();
                }
            }
        }

        internal static bool? GetConnectionClose(HttpHeaders parent, HttpGeneralHeaders headers)
        {
            // If we've already initialized the connection header value collection
            // and it contains the special value, or if we haven't and the headers contain
            // the parsed special value, return true.  We don't just access ConnectionCore,
            // as doing so will unnecessarily initialize the collection even if it's not needed.
            if (headers?._connection != null)
            {
                if (headers._connection.IsSpecialValueSet)
                {
                    return true;
                }
            }
                else if (parent.ContainsParsedValue(KnownHeaders.Connection.Descriptor, HeaderUtilities.ConnectionClose))
            {
                return true;
            }
            if (headers != null && headers._connectionCloseSet)
            {
                return false;
            }
            return null;
        }

        public DateTimeOffset? Date
        {
            get { return HeaderUtilities.GetDateTimeOffsetValue(KnownHeaders.Date.Descriptor, _parent); }
            set { _parent.SetOrRemoveParsedValue(KnownHeaders.Date.Descriptor, value); }
        }

        public HttpHeaderValueCollection<NameValueHeaderValue> Pragma
        {
            get
            {
                if (_pragma == null)
                {
                    _pragma = new HttpHeaderValueCollection<NameValueHeaderValue>(KnownHeaders.Pragma.Descriptor, _parent);
                }
                return _pragma;
            }
        }

        public HttpHeaderValueCollection<string> Trailer
        {
            get
            {
                if (_trailer == null)
                {
                    _trailer = new HttpHeaderValueCollection<string>(KnownHeaders.Trailer.Descriptor,
                        _parent, HeaderUtilities.TokenValidator);
                }
                return _trailer;
            }
        }

        public HttpHeaderValueCollection<TransferCodingHeaderValue> TransferEncoding
        {
            get { return TransferEncodingCore; }
        }

        internal static bool? GetTransferEncodingChunked(HttpHeaders parent, HttpGeneralHeaders headers)
        {
            // If we've already initialized the transfer encoding header value collection
            // and it contains the special value, or if we haven't and the headers contain
            // the parsed special value, return true.  We don't just access TransferEncodingCore,
            // as doing so will unnecessarily initialize the collection even if it's not needed.
            if (headers?._transferEncoding != null)
            {
                if (headers._transferEncoding.IsSpecialValueSet)
                {
                    return true;
                }
            }
            else if (parent.ContainsParsedValue(KnownHeaders.TransferEncoding.Descriptor, HeaderUtilities.TransferEncodingChunked))
            {
                return true;
            }
            if (headers != null && headers._transferEncodingChunkedSet)
            {
                return false;
            }
            return null;
        }

        public bool? TransferEncodingChunked
        {
            get
            {
                // Separated out into a static to enable access to TransferEncodingChunked
                // without the caller needing to force the creation of HttpGeneralHeaders
                // if it wasn't created for other reasons.
                return GetTransferEncodingChunked(_parent, this);
            }
            set
            {
                if (value == true)
                {
                    _transferEncodingChunkedSet = true;
                    TransferEncodingCore.SetSpecialValue();
                }
                else
                {
                    _transferEncodingChunkedSet = value != null;
                    TransferEncodingCore.RemoveSpecialValue();
                }
            }
        }

        public HttpHeaderValueCollection<ProductHeaderValue> Upgrade
        {
            get
            {
                if (_upgrade == null)
                {
                    _upgrade = new HttpHeaderValueCollection<ProductHeaderValue>(KnownHeaders.Upgrade.Descriptor, _parent);
                }
                return _upgrade;
            }
        }

        public HttpHeaderValueCollection<ViaHeaderValue> Via
        {
            get
            {
                if (_via == null)
                {
                    _via = new HttpHeaderValueCollection<ViaHeaderValue>(KnownHeaders.Via.Descriptor, _parent);
                }
                return _via;
            }
        }

        public HttpHeaderValueCollection<WarningHeaderValue> Warning
        {
            get
            {
                if (_warning == null)
                {
                    _warning = new HttpHeaderValueCollection<WarningHeaderValue>(KnownHeaders.Warning.Descriptor, _parent);
                }
                return _warning;
            }
        }

        private HttpHeaderValueCollection<string> ConnectionCore
        {
            get
            {
                if (_connection == null)
                {
                    _connection = new HttpHeaderValueCollection<string>(KnownHeaders.Connection.Descriptor,
                        _parent, HeaderUtilities.ConnectionClose, HeaderUtilities.TokenValidator);
                }
                return _connection;
            }
        }

        private HttpHeaderValueCollection<TransferCodingHeaderValue> TransferEncodingCore
        {
            get
            {
                if (_transferEncoding == null)
                {
                    _transferEncoding = new HttpHeaderValueCollection<TransferCodingHeaderValue>(
                        KnownHeaders.TransferEncoding.Descriptor, _parent, HeaderUtilities.TransferEncodingChunked);
                }
                return _transferEncoding;
            }
        }

        internal HttpGeneralHeaders(HttpHeaders parent)
        {
            Debug.Assert(parent != null);

            _parent = parent;
        }

        internal void AddSpecialsFrom(HttpGeneralHeaders sourceHeaders)
        {
            // Copy special values, but do not overwrite
            bool? chunked = TransferEncodingChunked;
            if (!chunked.HasValue)
            {
                TransferEncodingChunked = sourceHeaders.TransferEncodingChunked;
            }

            bool? close = ConnectionClose;
            if (!close.HasValue)
            {
                ConnectionClose = sourceHeaders.ConnectionClose;
            }
        }
    }
}
