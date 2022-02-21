// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net.Http.Headers
{
    // The purpose of this type is to extract the handling of general headers in one place rather than duplicating
    // functionality in both HttpRequestHeaders and HttpResponseHeaders.
    internal sealed class HttpGeneralHeaders
    {
        private HttpHeaderValueCollection<string>? _connection;
        private HttpHeaderValueCollection<string>? _trailer;
        private HttpHeaderValueCollection<TransferCodingHeaderValue>? _transferEncoding;
        private HttpHeaderValueCollection<ProductHeaderValue>? _upgrade;
        private HttpHeaderValueCollection<ViaHeaderValue>? _via;
        private HttpHeaderValueCollection<WarningHeaderValue>? _warning;
        private HttpHeaderValueCollection<NameValueHeaderValue>? _pragma;
        private readonly HttpHeaders _parent;
        private bool _transferEncodingChunkedSet;
        private bool _connectionCloseSet;

        public CacheControlHeaderValue? CacheControl
        {
            get { return (CacheControlHeaderValue?)_parent.GetParsedValues(KnownHeaders.CacheControl.Descriptor); }
            set { _parent.SetOrRemoveParsedValue(KnownHeaders.CacheControl.Descriptor, value); }
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
                    if (!_parent.ContainsParsedValue(KnownHeaders.Connection.Descriptor, HeaderUtilities.ConnectionClose))
                    {
                        _parent.AddParsedValue(KnownHeaders.Connection.Descriptor, HeaderUtilities.ConnectionClose);
                    }
                }
                else
                {
                    _connectionCloseSet = value != null;
                    // We intentionally ignore the return value. It's OK if "close" wasn't in the store.
                    _parent.RemoveParsedValue(KnownHeaders.Connection.Descriptor, HeaderUtilities.ConnectionClose);
                }
            }
        }

        internal static bool? GetConnectionClose(HttpHeaders parent, HttpGeneralHeaders? headers)
        {
            if (parent.ContainsParsedValue(KnownHeaders.Connection.Descriptor, HeaderUtilities.ConnectionClose))
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

        public HttpHeaderValueCollection<NameValueHeaderValue> Pragma =>
            _pragma ??= new HttpHeaderValueCollection<NameValueHeaderValue>(KnownHeaders.Pragma.Descriptor, _parent);

        public HttpHeaderValueCollection<string> Trailer =>
            _trailer ??= new HttpHeaderValueCollection<string>(KnownHeaders.Trailer.Descriptor, _parent);

        internal static bool? GetTransferEncodingChunked(HttpHeaders parent, HttpGeneralHeaders? headers)
        {
            if (parent.ContainsParsedValue(KnownHeaders.TransferEncoding.Descriptor, HeaderUtilities.TransferEncodingChunked))
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
                    if (!_parent.ContainsParsedValue(KnownHeaders.TransferEncoding.Descriptor, HeaderUtilities.TransferEncodingChunked))
                    {
                        _parent.AddParsedValue(KnownHeaders.TransferEncoding.Descriptor, HeaderUtilities.TransferEncodingChunked);
                    }
                }
                else
                {
                    _transferEncodingChunkedSet = value != null;
                    // We intentionally ignore the return value. It's OK if "chunked" wasn't in the store.
                    _parent.RemoveParsedValue(KnownHeaders.TransferEncoding.Descriptor, HeaderUtilities.TransferEncodingChunked);
                }
            }
        }

        public HttpHeaderValueCollection<ProductHeaderValue> Upgrade =>
            _upgrade ??= new HttpHeaderValueCollection<ProductHeaderValue>(KnownHeaders.Upgrade.Descriptor, _parent);

        public HttpHeaderValueCollection<ViaHeaderValue> Via =>
            _via ??= new HttpHeaderValueCollection<ViaHeaderValue>(KnownHeaders.Via.Descriptor, _parent);

        public HttpHeaderValueCollection<WarningHeaderValue> Warning =>
            _warning ??= new HttpHeaderValueCollection<WarningHeaderValue>(KnownHeaders.Warning.Descriptor, _parent);

        public HttpHeaderValueCollection<string> Connection =>
            _connection ??= new HttpHeaderValueCollection<string>(KnownHeaders.Connection.Descriptor, _parent);

        public HttpHeaderValueCollection<TransferCodingHeaderValue> TransferEncoding =>
            _transferEncoding ??= new HttpHeaderValueCollection<TransferCodingHeaderValue>(KnownHeaders.TransferEncoding.Descriptor, _parent);

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
