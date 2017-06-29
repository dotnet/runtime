// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http.Headers
{
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Justification = "This is not a collection")]
    public sealed class HttpResponseHeaders : HttpHeaders
    {
        private static readonly Dictionary<string, HttpHeaderParser> s_parserStore = CreateParserStore();
        private static readonly HashSet<string> s_invalidHeaders = CreateInvalidHeaders();

        private const int AcceptRangesSlot = 0;
        private const int ProxyAuthenticateSlot = 1;
        private const int ServerSlot = 2;
        private const int VarySlot = 3;
        private const int WwwAuthenticateSlot = 4;
        private const int NumCollectionsSlots = 5;

        private object[] _specialCollectionsSlots;
        private HttpGeneralHeaders _generalHeaders;

        #region Response Headers

        private T GetSpecializedCollection<T>(int slot, Func<HttpResponseHeaders, T> creationFunc)
        {
            // 5 properties each lazily allocate a collection to store the value(s) for that property.
            // Rather than having a field for each of these, store them untyped in an array that's lazily
            // allocated.  Then we only pay for the 45 bytes for those fields when any is actually accessed.
            object[] collections = _specialCollectionsSlots ?? (_specialCollectionsSlots = new object[NumCollectionsSlots]);
            object result = collections[slot];
            if (result == null)
            {
                collections[slot] = result = creationFunc(this);
            }
            return (T)result;
        }

        public HttpHeaderValueCollection<string> AcceptRanges =>
            GetSpecializedCollection(AcceptRangesSlot, thisRef => new HttpHeaderValueCollection<string>(HttpKnownHeaderNames.AcceptRanges, thisRef, HeaderUtilities.TokenValidator));

        public TimeSpan? Age
        {
            get { return HeaderUtilities.GetTimeSpanValue(HttpKnownHeaderNames.Age, this); }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.Age, value); }
        }

        public EntityTagHeaderValue ETag
        {
            get { return (EntityTagHeaderValue)GetParsedValues(HttpKnownHeaderNames.ETag); }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.ETag, value); }
        }

        public Uri Location
        {
            get { return (Uri)GetParsedValues(HttpKnownHeaderNames.Location); }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.Location, value); }
        }

        public HttpHeaderValueCollection<AuthenticationHeaderValue> ProxyAuthenticate =>
            GetSpecializedCollection(ProxyAuthenticateSlot, thisRef => new HttpHeaderValueCollection<AuthenticationHeaderValue>(HttpKnownHeaderNames.ProxyAuthenticate, thisRef));

        public RetryConditionHeaderValue RetryAfter
        {
            get { return (RetryConditionHeaderValue)GetParsedValues(HttpKnownHeaderNames.RetryAfter); }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.RetryAfter, value); }
        }

        public HttpHeaderValueCollection<ProductInfoHeaderValue> Server =>
            GetSpecializedCollection(ServerSlot, thisRef => new HttpHeaderValueCollection<ProductInfoHeaderValue>(HttpKnownHeaderNames.Server, thisRef));

        public HttpHeaderValueCollection<string> Vary =>
            GetSpecializedCollection(VarySlot, thisRef => new HttpHeaderValueCollection<string>(HttpKnownHeaderNames.Vary, thisRef, HeaderUtilities.TokenValidator));

        public HttpHeaderValueCollection<AuthenticationHeaderValue> WwwAuthenticate =>
            GetSpecializedCollection(WwwAuthenticateSlot, thisRef => new HttpHeaderValueCollection<AuthenticationHeaderValue>(HttpKnownHeaderNames.WWWAuthenticate, thisRef));

        #endregion

        #region General Headers

        public CacheControlHeaderValue CacheControl
        {
            get { return GeneralHeaders.CacheControl; }
            set { GeneralHeaders.CacheControl = value; }
        }

        public HttpHeaderValueCollection<string> Connection
        {
            get { return GeneralHeaders.Connection; }
        }

        public bool? ConnectionClose
        {
            get { return GeneralHeaders.ConnectionClose; }
            set { GeneralHeaders.ConnectionClose = value; }
        }

        public DateTimeOffset? Date
        {
            get { return GeneralHeaders.Date; }
            set { GeneralHeaders.Date = value; }
        }

        public HttpHeaderValueCollection<NameValueHeaderValue> Pragma
        {
            get { return GeneralHeaders.Pragma; }
        }

        public HttpHeaderValueCollection<string> Trailer
        {
            get { return GeneralHeaders.Trailer; }
        }

        public HttpHeaderValueCollection<TransferCodingHeaderValue> TransferEncoding
        {
            get { return GeneralHeaders.TransferEncoding; }
        }

        public bool? TransferEncodingChunked
        {
            get { return HttpGeneralHeaders.GetTransferEncodingChunked(this, _generalHeaders); } // special-cased to avoid forcing _generalHeaders initialization
            set { GeneralHeaders.TransferEncodingChunked = value; }
        }

        public HttpHeaderValueCollection<ProductHeaderValue> Upgrade
        {
            get { return GeneralHeaders.Upgrade; }
        }

        public HttpHeaderValueCollection<ViaHeaderValue> Via
        {
            get { return GeneralHeaders.Via; }
        }

        public HttpHeaderValueCollection<WarningHeaderValue> Warning
        {
            get { return GeneralHeaders.Warning; }
        }

        #endregion

        internal HttpResponseHeaders()
        {
            base.SetConfiguration(s_parserStore, s_invalidHeaders);
        }

        private static Dictionary<string, HttpHeaderParser> CreateParserStore()
        {
            var parserStore = new Dictionary<string, HttpHeaderParser>(StringComparer.OrdinalIgnoreCase);

            parserStore.Add(HttpKnownHeaderNames.AcceptRanges, GenericHeaderParser.TokenListParser);
            parserStore.Add(HttpKnownHeaderNames.Age, TimeSpanHeaderParser.Parser);
            parserStore.Add(HttpKnownHeaderNames.ETag, GenericHeaderParser.SingleValueEntityTagParser);
            parserStore.Add(HttpKnownHeaderNames.Location, UriHeaderParser.RelativeOrAbsoluteUriParser);
            parserStore.Add(HttpKnownHeaderNames.ProxyAuthenticate, GenericHeaderParser.MultipleValueAuthenticationParser);
            parserStore.Add(HttpKnownHeaderNames.RetryAfter, GenericHeaderParser.RetryConditionParser);
            parserStore.Add(HttpKnownHeaderNames.Server, ProductInfoHeaderParser.MultipleValueParser);
            parserStore.Add(HttpKnownHeaderNames.Vary, GenericHeaderParser.TokenListParser);
            parserStore.Add(HttpKnownHeaderNames.WWWAuthenticate, GenericHeaderParser.MultipleValueAuthenticationParser);

            HttpGeneralHeaders.AddParsers(parserStore);

            return parserStore;
        }

        private static HashSet<string> CreateInvalidHeaders()
        {
            var invalidHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HttpContentHeaders.AddKnownHeaders(invalidHeaders);
            return invalidHeaders;

            // Note: Reserved request header names are allowed as custom response header names.  Reserved request
            // headers have no defined meaning or format when used on a response. This enables a client to accept
            // any headers sent from the server as either content headers or response headers.
        }

        internal static void AddKnownHeaders(HashSet<string> headerSet)
        {
            Debug.Assert(headerSet != null);

            headerSet.Add(HttpKnownHeaderNames.AcceptRanges);
            headerSet.Add(HttpKnownHeaderNames.Age);
            headerSet.Add(HttpKnownHeaderNames.ETag);
            headerSet.Add(HttpKnownHeaderNames.Location);
            headerSet.Add(HttpKnownHeaderNames.ProxyAuthenticate);
            headerSet.Add(HttpKnownHeaderNames.RetryAfter);
            headerSet.Add(HttpKnownHeaderNames.Server);
            headerSet.Add(HttpKnownHeaderNames.Vary);
            headerSet.Add(HttpKnownHeaderNames.WWWAuthenticate);
        }

        internal override void AddHeaders(HttpHeaders sourceHeaders)
        {
            base.AddHeaders(sourceHeaders);
            HttpResponseHeaders sourceResponseHeaders = sourceHeaders as HttpResponseHeaders;
            Debug.Assert(sourceResponseHeaders != null);

            // Copy special values, but do not overwrite
            if (sourceResponseHeaders._generalHeaders != null)
            {
                GeneralHeaders.AddSpecialsFrom(sourceResponseHeaders._generalHeaders);
            }
        }

        private HttpGeneralHeaders GeneralHeaders => _generalHeaders ?? (_generalHeaders = new HttpGeneralHeaders(this));
    }
}
