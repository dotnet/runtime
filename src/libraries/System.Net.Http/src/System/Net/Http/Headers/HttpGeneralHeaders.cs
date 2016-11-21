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
            get { return (CacheControlHeaderValue)_parent.GetParsedValues(HttpKnownHeaderNames.CacheControl); }
            set { _parent.SetOrRemoveParsedValue(HttpKnownHeaderNames.CacheControl, value); }
        }

        public HttpHeaderValueCollection<string> Connection
        {
            get { return ConnectionCore; }
        }

        public bool? ConnectionClose
        {
            get
            {
                // If we've already initialized the connection header value collection
                // and it contains the special value, or if we haven't and the headers contain
                // the parsed special value, return true.  We don't just access ConnectionCore,
                // as doing so will unnecessarily initialize the collection even if it's not needed.
                if (_connection != null)
                {
                    if (_connection.IsSpecialValueSet)
                    {
                        return true;
                    }
                }
                else if (_parent.ContainsParsedValue(HttpKnownHeaderNames.Connection, HeaderUtilities.ConnectionClose))
                {
                    return true;
                }
                if (_connectionCloseSet)
                {
                    return false;
                }
                return null;
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

        public DateTimeOffset? Date
        {
            get { return HeaderUtilities.GetDateTimeOffsetValue(HttpKnownHeaderNames.Date, _parent); }
            set { _parent.SetOrRemoveParsedValue(HttpKnownHeaderNames.Date, value); }
        }

        public HttpHeaderValueCollection<NameValueHeaderValue> Pragma
        {
            get
            {
                if (_pragma == null)
                {
                    _pragma = new HttpHeaderValueCollection<NameValueHeaderValue>(HttpKnownHeaderNames.Pragma, _parent);
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
                    _trailer = new HttpHeaderValueCollection<string>(HttpKnownHeaderNames.Trailer,
                        _parent, HeaderUtilities.TokenValidator);
                }
                return _trailer;
            }
        }

        public HttpHeaderValueCollection<TransferCodingHeaderValue> TransferEncoding
        {
            get { return TransferEncodingCore; }
        }

        public bool? TransferEncodingChunked
        {
            get
            {
                // If we've already initialized the transfer encoding header value collection
                // and it contains the special value, or if we haven't and the headers contain
                // the parsed special value, return true.  We don't just access TransferEncodingCore,
                // as doing so will unnecessarily initialize the collection even if it's not needed.
                if (_transferEncoding != null)
                {
                    if (_transferEncoding.IsSpecialValueSet)
                    {
                        return true;
                    }
                }
                else if (_parent.ContainsParsedValue(HttpKnownHeaderNames.TransferEncoding, HeaderUtilities.TransferEncodingChunked))
                {
                    return true;
                }
                if (_transferEncodingChunkedSet)
                {
                    return false;
                }
                return null;
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
                    _upgrade = new HttpHeaderValueCollection<ProductHeaderValue>(HttpKnownHeaderNames.Upgrade, _parent);
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
                    _via = new HttpHeaderValueCollection<ViaHeaderValue>(HttpKnownHeaderNames.Via, _parent);
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
                    _warning = new HttpHeaderValueCollection<WarningHeaderValue>(HttpKnownHeaderNames.Warning, _parent);
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
                    _connection = new HttpHeaderValueCollection<string>(HttpKnownHeaderNames.Connection,
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
                        HttpKnownHeaderNames.TransferEncoding, _parent, HeaderUtilities.TransferEncodingChunked);
                }
                return _transferEncoding;
            }
        }

        internal HttpGeneralHeaders(HttpHeaders parent)
        {
            Debug.Assert(parent != null);

            _parent = parent;
        }

        internal static void AddParsers(Dictionary<string, HttpHeaderParser> parserStore)
        {
            Debug.Assert(parserStore != null);

            parserStore.Add(HttpKnownHeaderNames.CacheControl, CacheControlHeaderParser.Parser);
            parserStore.Add(HttpKnownHeaderNames.Connection, GenericHeaderParser.TokenListParser);
            parserStore.Add(HttpKnownHeaderNames.Date, DateHeaderParser.Parser);
            parserStore.Add(HttpKnownHeaderNames.Pragma, GenericHeaderParser.MultipleValueNameValueParser);
            parserStore.Add(HttpKnownHeaderNames.Trailer, GenericHeaderParser.TokenListParser);
            parserStore.Add(HttpKnownHeaderNames.TransferEncoding, TransferCodingHeaderParser.MultipleValueParser);
            parserStore.Add(HttpKnownHeaderNames.Upgrade, GenericHeaderParser.MultipleValueProductParser);
            parserStore.Add(HttpKnownHeaderNames.Via, GenericHeaderParser.MultipleValueViaParser);
            parserStore.Add(HttpKnownHeaderNames.Warning, GenericHeaderParser.MultipleValueWarningParser);
        }

        internal static void AddKnownHeaders(HashSet<string> headerSet)
        {
            Debug.Assert(headerSet != null);

            headerSet.Add(HttpKnownHeaderNames.CacheControl);
            headerSet.Add(HttpKnownHeaderNames.Connection);
            headerSet.Add(HttpKnownHeaderNames.Date);
            headerSet.Add(HttpKnownHeaderNames.Pragma);
            headerSet.Add(HttpKnownHeaderNames.Trailer);
            headerSet.Add(HttpKnownHeaderNames.TransferEncoding);
            headerSet.Add(HttpKnownHeaderNames.Upgrade);
            headerSet.Add(HttpKnownHeaderNames.Via);
            headerSet.Add(HttpKnownHeaderNames.Warning);
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
