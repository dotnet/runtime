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
    public sealed class HttpRequestHeaders : HttpHeaders
    {
        private static readonly Dictionary<string, HttpHeaderParser> s_parserStore = CreateParserStore();
        private static readonly HashSet<string> s_invalidHeaders = CreateInvalidHeaders();

        private const int AcceptSlot = 0;
        private const int AcceptCharsetSlot = 1;
        private const int AcceptEncodingSlot = 2;
        private const int AcceptLanguageSlot = 3;
        private const int ExpectSlot = 4;
        private const int IfMatchSlot = 5;
        private const int IfNoneMatchSlot = 6;
        private const int TransferEncodingSlot = 7;
        private const int UserAgentSlot = 8;
        private const int NumCollectionsSlots = 9;

        private object[] _specialCollectionsSlots;
        private HttpGeneralHeaders _generalHeaders;
        private bool _expectContinueSet;

        #region Request Headers

        private T GetSpecializedCollection<T>(int slot, Func<HttpRequestHeaders, T> creationFunc)
        {
            // 9 properties each lazily allocate a collection to store the value(s) for that property.
            // Rather than having a field for each of these, store them untyped in an array that's lazily
            // allocated.  Then we only pay for the 72 bytes for those fields when any is actually accessed.
            object[] collections = _specialCollectionsSlots ?? (_specialCollectionsSlots = new object[NumCollectionsSlots]);
            object result = collections[slot];
            if (result == null)
            {
                collections[slot] = result = creationFunc(this);
            }
            return (T)result;
        }

        public HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue> Accept =>
            GetSpecializedCollection(AcceptSlot, thisRef => new HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue>(HttpKnownHeaderNames.Accept, thisRef));

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Charset", Justification = "The HTTP header name is 'Accept-Charset'.")]
        public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptCharset =>
            GetSpecializedCollection(AcceptCharsetSlot, thisRef => new HttpHeaderValueCollection<StringWithQualityHeaderValue>(HttpKnownHeaderNames.AcceptCharset, thisRef));

        public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptEncoding =>
            GetSpecializedCollection(AcceptEncodingSlot, thisRef => new HttpHeaderValueCollection<StringWithQualityHeaderValue>(HttpKnownHeaderNames.AcceptEncoding, thisRef));

        public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptLanguage =>
            GetSpecializedCollection(AcceptLanguageSlot, thisRef => new HttpHeaderValueCollection<StringWithQualityHeaderValue>(HttpKnownHeaderNames.AcceptLanguage, thisRef));

        public AuthenticationHeaderValue Authorization
        {
            get { return (AuthenticationHeaderValue)GetParsedValues(HttpKnownHeaderNames.Authorization); }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.Authorization, value); }
        }

        public HttpHeaderValueCollection<NameValueWithParametersHeaderValue> Expect
        {
            get { return ExpectCore; }
        }

        public bool? ExpectContinue
        {
            get
            {
                if (ExpectCore.IsSpecialValueSet)
                {
                    return true;
                }
                if (_expectContinueSet)
                {
                    return false;
                }
                return null;
            }
            set
            {
                if (value == true)
                {
                    _expectContinueSet = true;
                    ExpectCore.SetSpecialValue();
                }
                else
                {
                    _expectContinueSet = value != null;
                    ExpectCore.RemoveSpecialValue();
                }
            }
        }

        public string From
        {
            get { return (string)GetParsedValues(HttpKnownHeaderNames.From); }
            set
            {
                // Null and empty string are equivalent. In this case it means, remove the From header value (if any).
                if (value == string.Empty)
                {
                    value = null;
                }

                if ((value != null) && !HeaderUtilities.IsValidEmailAddress(value))
                {
                    throw new FormatException(SR.net_http_headers_invalid_from_header);
                }
                SetOrRemoveParsedValue(HttpKnownHeaderNames.From, value);
            }
        }

        public string Host
        {
            get { return (string)GetParsedValues(HttpKnownHeaderNames.Host); }
            set
            {
                // Null and empty string are equivalent. In this case it means, remove the Host header value (if any).
                if (value == string.Empty)
                {
                    value = null;
                }

                string host = null;
                if ((value != null) && (HttpRuleParser.GetHostLength(value, 0, false, out host) != value.Length))
                {
                    throw new FormatException(SR.net_http_headers_invalid_host_header);
                }
                SetOrRemoveParsedValue(HttpKnownHeaderNames.Host, value);
            }
        }

        public HttpHeaderValueCollection<EntityTagHeaderValue> IfMatch =>
            GetSpecializedCollection(IfMatchSlot, thisRef => new HttpHeaderValueCollection<EntityTagHeaderValue>(HttpKnownHeaderNames.IfMatch, thisRef));

        public DateTimeOffset? IfModifiedSince
        {
            get { return HeaderUtilities.GetDateTimeOffsetValue(HttpKnownHeaderNames.IfModifiedSince, this); }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.IfModifiedSince, value); }
        }

        public HttpHeaderValueCollection<EntityTagHeaderValue> IfNoneMatch =>
            GetSpecializedCollection(IfNoneMatchSlot, thisRef => new HttpHeaderValueCollection<EntityTagHeaderValue>(HttpKnownHeaderNames.IfNoneMatch, thisRef));

        public RangeConditionHeaderValue IfRange
        {
            get { return (RangeConditionHeaderValue)GetParsedValues(HttpKnownHeaderNames.IfRange); }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.IfRange, value); }
        }

        public DateTimeOffset? IfUnmodifiedSince
        {
            get { return HeaderUtilities.GetDateTimeOffsetValue(HttpKnownHeaderNames.IfUnmodifiedSince, this); }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.IfUnmodifiedSince, value); }
        }

        public int? MaxForwards
        {
            get
            {
                object storedValue = GetParsedValues(HttpKnownHeaderNames.MaxForwards);
                if (storedValue != null)
                {
                    return (int)storedValue;
                }
                return null;
            }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.MaxForwards, value); }
        }


        public AuthenticationHeaderValue ProxyAuthorization
        {
            get { return (AuthenticationHeaderValue)GetParsedValues(HttpKnownHeaderNames.ProxyAuthorization); }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.ProxyAuthorization, value); }
        }

        public RangeHeaderValue Range
        {
            get { return (RangeHeaderValue)GetParsedValues(HttpKnownHeaderNames.Range); }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.Range, value); }
        }

        public Uri Referrer
        {
            get { return (Uri)GetParsedValues(HttpKnownHeaderNames.Referer); }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.Referer, value); }
        }

        public HttpHeaderValueCollection<TransferCodingWithQualityHeaderValue> TE =>
            GetSpecializedCollection(TransferEncodingSlot, thisRef => new HttpHeaderValueCollection<TransferCodingWithQualityHeaderValue>(HttpKnownHeaderNames.TE, thisRef));

        public HttpHeaderValueCollection<ProductInfoHeaderValue> UserAgent =>
            GetSpecializedCollection(UserAgentSlot, thisRef => new HttpHeaderValueCollection<ProductInfoHeaderValue>(HttpKnownHeaderNames.UserAgent, thisRef));

        private HttpHeaderValueCollection<NameValueWithParametersHeaderValue> ExpectCore =>
            GetSpecializedCollection(ExpectSlot, thisRef => new HttpHeaderValueCollection<NameValueWithParametersHeaderValue>(HttpKnownHeaderNames.Expect, thisRef, HeaderUtilities.ExpectContinue));

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
            get { return HttpGeneralHeaders.GetConnectionClose(this, _generalHeaders); } // special-cased to avoid forcing _generalHeaders initialization
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

        internal HttpRequestHeaders()
        {
            base.SetConfiguration(s_parserStore, s_invalidHeaders);
        }

        private static Dictionary<string, HttpHeaderParser> CreateParserStore()
        {
            var parserStore = new Dictionary<string, HttpHeaderParser>(StringComparer.OrdinalIgnoreCase);

            parserStore.Add(HttpKnownHeaderNames.Accept, MediaTypeHeaderParser.MultipleValuesParser);
            parserStore.Add(HttpKnownHeaderNames.AcceptCharset, GenericHeaderParser.MultipleValueStringWithQualityParser);
            parserStore.Add(HttpKnownHeaderNames.AcceptEncoding, GenericHeaderParser.MultipleValueStringWithQualityParser);
            parserStore.Add(HttpKnownHeaderNames.AcceptLanguage, GenericHeaderParser.MultipleValueStringWithQualityParser);
            parserStore.Add(HttpKnownHeaderNames.Authorization, GenericHeaderParser.SingleValueAuthenticationParser);
            parserStore.Add(HttpKnownHeaderNames.Expect, GenericHeaderParser.MultipleValueNameValueWithParametersParser);
            parserStore.Add(HttpKnownHeaderNames.From, GenericHeaderParser.MailAddressParser);
            parserStore.Add(HttpKnownHeaderNames.Host, GenericHeaderParser.HostParser);
            parserStore.Add(HttpKnownHeaderNames.IfMatch, GenericHeaderParser.MultipleValueEntityTagParser);
            parserStore.Add(HttpKnownHeaderNames.IfModifiedSince, DateHeaderParser.Parser);
            parserStore.Add(HttpKnownHeaderNames.IfNoneMatch, GenericHeaderParser.MultipleValueEntityTagParser);
            parserStore.Add(HttpKnownHeaderNames.IfRange, GenericHeaderParser.RangeConditionParser);
            parserStore.Add(HttpKnownHeaderNames.IfUnmodifiedSince, DateHeaderParser.Parser);
            parserStore.Add(HttpKnownHeaderNames.MaxForwards, Int32NumberHeaderParser.Parser);
            parserStore.Add(HttpKnownHeaderNames.ProxyAuthorization, GenericHeaderParser.SingleValueAuthenticationParser);
            parserStore.Add(HttpKnownHeaderNames.Range, GenericHeaderParser.RangeParser);
            parserStore.Add(HttpKnownHeaderNames.Referer, UriHeaderParser.RelativeOrAbsoluteUriParser);
            parserStore.Add(HttpKnownHeaderNames.TE, TransferCodingHeaderParser.MultipleValueWithQualityParser);
            parserStore.Add(HttpKnownHeaderNames.UserAgent, ProductInfoHeaderParser.MultipleValueParser);

            HttpGeneralHeaders.AddParsers(parserStore);

            return parserStore;
        }

        private static HashSet<string> CreateInvalidHeaders()
        {
            var invalidHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HttpContentHeaders.AddKnownHeaders(invalidHeaders);
            return invalidHeaders;

            // Note: Reserved response header names are allowed as custom request header names.  Reserved response
            // headers have no defined meaning or format when used on a request.  This enables a server to accept
            // any headers sent from the client as either content headers or request headers.
        }

        internal static void AddKnownHeaders(HashSet<string> headerSet)
        {
            Debug.Assert(headerSet != null);

            headerSet.Add(HttpKnownHeaderNames.Accept);
            headerSet.Add(HttpKnownHeaderNames.AcceptCharset);
            headerSet.Add(HttpKnownHeaderNames.AcceptEncoding);
            headerSet.Add(HttpKnownHeaderNames.AcceptLanguage);
            headerSet.Add(HttpKnownHeaderNames.Authorization);
            headerSet.Add(HttpKnownHeaderNames.Expect);
            headerSet.Add(HttpKnownHeaderNames.From);
            headerSet.Add(HttpKnownHeaderNames.Host);
            headerSet.Add(HttpKnownHeaderNames.IfMatch);
            headerSet.Add(HttpKnownHeaderNames.IfModifiedSince);
            headerSet.Add(HttpKnownHeaderNames.IfNoneMatch);
            headerSet.Add(HttpKnownHeaderNames.IfRange);
            headerSet.Add(HttpKnownHeaderNames.IfUnmodifiedSince);
            headerSet.Add(HttpKnownHeaderNames.MaxForwards);
            headerSet.Add(HttpKnownHeaderNames.ProxyAuthorization);
            headerSet.Add(HttpKnownHeaderNames.Range);
            headerSet.Add(HttpKnownHeaderNames.Referer);
            headerSet.Add(HttpKnownHeaderNames.TE);
            headerSet.Add(HttpKnownHeaderNames.UserAgent);
        }

        internal override void AddHeaders(HttpHeaders sourceHeaders)
        {
            base.AddHeaders(sourceHeaders);
            HttpRequestHeaders sourceRequestHeaders = sourceHeaders as HttpRequestHeaders;
            Debug.Assert(sourceRequestHeaders != null);

            // Copy special values but do not overwrite.
            if (sourceRequestHeaders._generalHeaders != null)
            {
                GeneralHeaders.AddSpecialsFrom(sourceRequestHeaders._generalHeaders);
            }

            bool? expectContinue = ExpectContinue;
            if (!expectContinue.HasValue)
            {
                ExpectContinue = sourceRequestHeaders.ExpectContinue;
            }
        }

        private HttpGeneralHeaders GeneralHeaders => _generalHeaders ?? (_generalHeaders = new HttpGeneralHeaders(this));
    }
}
