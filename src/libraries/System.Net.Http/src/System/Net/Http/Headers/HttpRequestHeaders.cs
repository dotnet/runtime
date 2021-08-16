// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net.Http.Headers
{
    public sealed class HttpRequestHeaders : HttpHeaders
    {
        private const int AcceptSlot = 0;
        private const int AcceptCharsetSlot = 1;
        private const int AcceptEncodingSlot = 2;
        private const int AcceptLanguageSlot = 3;
        private const int IfMatchSlot = 4;
        private const int IfNoneMatchSlot = 5;
        private const int TransferEncodingSlot = 6;
        private const int UserAgentSlot = 7;
        private const int NumCollectionsSlots = 8;

        private object[]? _specialCollectionsSlots;
        private HttpGeneralHeaders? _generalHeaders;
        private HttpHeaderValueCollection<NameValueWithParametersHeaderValue>? _expect;
        private bool _expectContinueSet;

        #region Request Headers

        private T GetSpecializedCollection<T>(int slot, Func<HttpRequestHeaders, T> creationFunc)
        {
            // 8 properties each lazily allocate a collection to store the value(s) for that property.
            // Rather than having a field for each of these, store them untyped in an array that's lazily
            // allocated.  Then we only pay for the 64 bytes for those fields when any is actually accessed.
            _specialCollectionsSlots ??= new object[NumCollectionsSlots];
            return (T)(_specialCollectionsSlots[slot] ??= creationFunc(this)!);
        }

        public HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue> Accept =>
            GetSpecializedCollection(AcceptSlot, static thisRef => new HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue>(KnownHeaders.Accept.Descriptor, thisRef));

        public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptCharset =>
            GetSpecializedCollection(AcceptCharsetSlot, static thisRef => new HttpHeaderValueCollection<StringWithQualityHeaderValue>(KnownHeaders.AcceptCharset.Descriptor, thisRef));

        public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptEncoding =>
            GetSpecializedCollection(AcceptEncodingSlot, static thisRef => new HttpHeaderValueCollection<StringWithQualityHeaderValue>(KnownHeaders.AcceptEncoding.Descriptor, thisRef));

        public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptLanguage =>
            GetSpecializedCollection(AcceptLanguageSlot, static thisRef => new HttpHeaderValueCollection<StringWithQualityHeaderValue>(KnownHeaders.AcceptLanguage.Descriptor, thisRef));

        public AuthenticationHeaderValue? Authorization
        {
            get { return (AuthenticationHeaderValue?)GetParsedValues(KnownHeaders.Authorization.Descriptor); }
            set { SetOrRemoveParsedValue(KnownHeaders.Authorization.Descriptor, value); }
        }

        public HttpHeaderValueCollection<NameValueWithParametersHeaderValue> Expect
        {
            get { return ExpectCore; }
        }

        public bool? ExpectContinue
        {
            get
            {
                // ExpectCore will force the collection into existence, so avoid accessing it if possible.
                if (_expectContinueSet || ContainsParsedValue(KnownHeaders.Expect.Descriptor, HeaderUtilities.ExpectContinue))
                {
                    if (ExpectCore.IsSpecialValueSet)
                    {
                        return true;
                    }
                    if (_expectContinueSet)
                    {
                        return false;
                    }
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

        public string? From
        {
            get { return (string?)GetParsedValues(KnownHeaders.From.Descriptor); }
            set
            {
                // Null and empty string are equivalent. In this case it means, remove the From header value (if any).
                if (value == string.Empty)
                {
                    value = null;
                }

                CheckContainsNewLine(value);

                SetOrRemoveParsedValue(KnownHeaders.From.Descriptor, value);
            }
        }

        public string? Host
        {
            get { return (string?)GetParsedValues(KnownHeaders.Host.Descriptor); }
            set
            {
                // Null and empty string are equivalent. In this case it means, remove the Host header value (if any).
                if (value == string.Empty)
                {
                    value = null;
                }

                if ((value != null) && (HttpRuleParser.GetHostLength(value, 0, false, out string? _) != value.Length))
                {
                    throw new FormatException(SR.net_http_headers_invalid_host_header);
                }
                SetOrRemoveParsedValue(KnownHeaders.Host.Descriptor, value);
            }
        }

        public HttpHeaderValueCollection<EntityTagHeaderValue> IfMatch =>
            GetSpecializedCollection(IfMatchSlot, static thisRef => new HttpHeaderValueCollection<EntityTagHeaderValue>(KnownHeaders.IfMatch.Descriptor, thisRef));

        public DateTimeOffset? IfModifiedSince
        {
            get { return HeaderUtilities.GetDateTimeOffsetValue(KnownHeaders.IfModifiedSince.Descriptor, this); }
            set { SetOrRemoveParsedValue(KnownHeaders.IfModifiedSince.Descriptor, value); }
        }

        public HttpHeaderValueCollection<EntityTagHeaderValue> IfNoneMatch =>
            GetSpecializedCollection(IfNoneMatchSlot, static thisRef => new HttpHeaderValueCollection<EntityTagHeaderValue>(KnownHeaders.IfNoneMatch.Descriptor, thisRef));

        public RangeConditionHeaderValue? IfRange
        {
            get { return (RangeConditionHeaderValue?)GetParsedValues(KnownHeaders.IfRange.Descriptor); }
            set { SetOrRemoveParsedValue(KnownHeaders.IfRange.Descriptor, value); }
        }

        public DateTimeOffset? IfUnmodifiedSince
        {
            get { return HeaderUtilities.GetDateTimeOffsetValue(KnownHeaders.IfUnmodifiedSince.Descriptor, this); }
            set { SetOrRemoveParsedValue(KnownHeaders.IfUnmodifiedSince.Descriptor, value); }
        }

        public int? MaxForwards
        {
            get
            {
                object? storedValue = GetParsedValues(KnownHeaders.MaxForwards.Descriptor);
                if (storedValue != null)
                {
                    return (int)storedValue;
                }
                return null;
            }
            set { SetOrRemoveParsedValue(KnownHeaders.MaxForwards.Descriptor, value); }
        }


        public AuthenticationHeaderValue? ProxyAuthorization
        {
            get { return (AuthenticationHeaderValue?)GetParsedValues(KnownHeaders.ProxyAuthorization.Descriptor); }
            set { SetOrRemoveParsedValue(KnownHeaders.ProxyAuthorization.Descriptor, value); }
        }

        public RangeHeaderValue? Range
        {
            get { return (RangeHeaderValue?)GetParsedValues(KnownHeaders.Range.Descriptor); }
            set { SetOrRemoveParsedValue(KnownHeaders.Range.Descriptor, value); }
        }

        public Uri? Referrer
        {
            get { return (Uri?)GetParsedValues(KnownHeaders.Referer.Descriptor); }
            set { SetOrRemoveParsedValue(KnownHeaders.Referer.Descriptor, value); }
        }

        public HttpHeaderValueCollection<TransferCodingWithQualityHeaderValue> TE =>
            GetSpecializedCollection(TransferEncodingSlot, static thisRef => new HttpHeaderValueCollection<TransferCodingWithQualityHeaderValue>(KnownHeaders.TE.Descriptor, thisRef));

        public HttpHeaderValueCollection<ProductInfoHeaderValue> UserAgent =>
            GetSpecializedCollection(UserAgentSlot, static thisRef => new HttpHeaderValueCollection<ProductInfoHeaderValue>(KnownHeaders.UserAgent.Descriptor, thisRef));

        private HttpHeaderValueCollection<NameValueWithParametersHeaderValue> ExpectCore =>
            _expect ??= new HttpHeaderValueCollection<NameValueWithParametersHeaderValue>(KnownHeaders.Expect.Descriptor, this, HeaderUtilities.ExpectContinue);

        #endregion

        #region General Headers

        public CacheControlHeaderValue? CacheControl
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
            : base(HttpHeaderType.General | HttpHeaderType.Request | HttpHeaderType.Custom, HttpHeaderType.Response)
        {
        }

        internal override void AddHeaders(HttpHeaders sourceHeaders)
        {
            base.AddHeaders(sourceHeaders);
            HttpRequestHeaders? sourceRequestHeaders = sourceHeaders as HttpRequestHeaders;
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
