// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace System.Net.Http.Headers
{
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Justification = "This is not a collection")]
    public sealed class HttpRequestHeaders : HttpHeaders
    {
        private static readonly Dictionary<string, HttpHeaderParser> s_parserStore;
        private static readonly HashSet<string> s_invalidHeaders;

        private HttpGeneralHeaders _generalHeaders;
        private HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue> _accept;
        private HttpHeaderValueCollection<NameValueWithParametersHeaderValue> _expect;
        private bool _expectContinueSet;
        private HttpHeaderValueCollection<EntityTagHeaderValue> _ifMatch;
        private HttpHeaderValueCollection<EntityTagHeaderValue> _ifNoneMatch;
        private HttpHeaderValueCollection<TransferCodingWithQualityHeaderValue> _te;
        private HttpHeaderValueCollection<ProductInfoHeaderValue> _userAgent;
        private HttpHeaderValueCollection<StringWithQualityHeaderValue> _acceptCharset;
        private HttpHeaderValueCollection<StringWithQualityHeaderValue> _acceptEncoding;
        private HttpHeaderValueCollection<StringWithQualityHeaderValue> _acceptLanguage;

        #region Request Headers

        public HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue> Accept
        {
            get
            {
                if (_accept == null)
                {
                    _accept = new HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue>(
                        HttpKnownHeaderNames.Accept, this);
                }
                return _accept;
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Charset",
            Justification = "The HTTP header name is 'Accept-Charset'.")]
        public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptCharset
        {
            get
            {
                if (_acceptCharset == null)
                {
                    _acceptCharset = new HttpHeaderValueCollection<StringWithQualityHeaderValue>(
                        HttpKnownHeaderNames.AcceptCharset, this);
                }
                return _acceptCharset;
            }
        }

        public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptEncoding
        {
            get
            {
                if (_acceptEncoding == null)
                {
                    _acceptEncoding = new HttpHeaderValueCollection<StringWithQualityHeaderValue>(
                        HttpKnownHeaderNames.AcceptEncoding, this);
                }
                return _acceptEncoding;
            }
        }

        public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptLanguage
        {
            get
            {
                if (_acceptLanguage == null)
                {
                    _acceptLanguage = new HttpHeaderValueCollection<StringWithQualityHeaderValue>(
                        HttpKnownHeaderNames.AcceptLanguage, this);
                }
                return _acceptLanguage;
            }
        }

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

        public HttpHeaderValueCollection<EntityTagHeaderValue> IfMatch
        {
            get
            {
                if (_ifMatch == null)
                {
                    _ifMatch = new HttpHeaderValueCollection<EntityTagHeaderValue>(
                        HttpKnownHeaderNames.IfMatch, this);
                }
                return _ifMatch;
            }
        }

        public DateTimeOffset? IfModifiedSince
        {
            get { return HeaderUtilities.GetDateTimeOffsetValue(HttpKnownHeaderNames.IfModifiedSince, this); }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.IfModifiedSince, value); }
        }

        public HttpHeaderValueCollection<EntityTagHeaderValue> IfNoneMatch
        {
            get
            {
                if (_ifNoneMatch == null)
                {
                    _ifNoneMatch = new HttpHeaderValueCollection<EntityTagHeaderValue>(
                        HttpKnownHeaderNames.IfNoneMatch, this);
                }
                return _ifNoneMatch;
            }
        }

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

        public HttpHeaderValueCollection<TransferCodingWithQualityHeaderValue> TE
        {
            get
            {
                if (_te == null)
                {
                    _te = new HttpHeaderValueCollection<TransferCodingWithQualityHeaderValue>(
                        HttpKnownHeaderNames.TE, this);
                }
                return _te;
            }
        }

        public HttpHeaderValueCollection<ProductInfoHeaderValue> UserAgent
        {
            get
            {
                if (_userAgent == null)
                {
                    _userAgent = new HttpHeaderValueCollection<ProductInfoHeaderValue>(HttpKnownHeaderNames.UserAgent,
                        this);
                }
                return _userAgent;
            }
        }

        private HttpHeaderValueCollection<NameValueWithParametersHeaderValue> ExpectCore
        {
            get
            {
                if (_expect == null)
                {
                    _expect = new HttpHeaderValueCollection<NameValueWithParametersHeaderValue>(
                        HttpKnownHeaderNames.Expect, this, HeaderUtilities.ExpectContinue);
                }
                return _expect;
            }
        }

        #endregion

        #region General Headers

        public CacheControlHeaderValue CacheControl
        {
            get { return _generalHeaders.CacheControl; }
            set { _generalHeaders.CacheControl = value; }
        }

        public HttpHeaderValueCollection<string> Connection
        {
            get { return _generalHeaders.Connection; }
        }

        public bool? ConnectionClose
        {
            get { return _generalHeaders.ConnectionClose; }
            set { _generalHeaders.ConnectionClose = value; }
        }

        public DateTimeOffset? Date
        {
            get { return _generalHeaders.Date; }
            set { _generalHeaders.Date = value; }
        }

        public HttpHeaderValueCollection<NameValueHeaderValue> Pragma
        {
            get { return _generalHeaders.Pragma; }
        }

        public HttpHeaderValueCollection<string> Trailer
        {
            get { return _generalHeaders.Trailer; }
        }

        public HttpHeaderValueCollection<TransferCodingHeaderValue> TransferEncoding
        {
            get { return _generalHeaders.TransferEncoding; }
        }

        public bool? TransferEncodingChunked
        {
            get { return _generalHeaders.TransferEncodingChunked; }
            set { _generalHeaders.TransferEncodingChunked = value; }
        }

        public HttpHeaderValueCollection<ProductHeaderValue> Upgrade
        {
            get { return _generalHeaders.Upgrade; }
        }

        public HttpHeaderValueCollection<ViaHeaderValue> Via
        {
            get { return _generalHeaders.Via; }
        }

        public HttpHeaderValueCollection<WarningHeaderValue> Warning
        {
            get { return _generalHeaders.Warning; }
        }

        #endregion

        internal HttpRequestHeaders()
        {
            _generalHeaders = new HttpGeneralHeaders(this);

            base.SetConfiguration(s_parserStore, s_invalidHeaders);
        }

        static HttpRequestHeaders()
        {
            s_parserStore = new Dictionary<string, HttpHeaderParser>(StringComparer.OrdinalIgnoreCase);

            s_parserStore.Add(HttpKnownHeaderNames.Accept, MediaTypeHeaderParser.MultipleValuesParser);
            s_parserStore.Add(HttpKnownHeaderNames.AcceptCharset, GenericHeaderParser.MultipleValueStringWithQualityParser);
            s_parserStore.Add(HttpKnownHeaderNames.AcceptEncoding, GenericHeaderParser.MultipleValueStringWithQualityParser);
            s_parserStore.Add(HttpKnownHeaderNames.AcceptLanguage, GenericHeaderParser.MultipleValueStringWithQualityParser);
            s_parserStore.Add(HttpKnownHeaderNames.Authorization, GenericHeaderParser.SingleValueAuthenticationParser);
            s_parserStore.Add(HttpKnownHeaderNames.Expect, GenericHeaderParser.MultipleValueNameValueWithParametersParser);
            s_parserStore.Add(HttpKnownHeaderNames.From, GenericHeaderParser.MailAddressParser);
            s_parserStore.Add(HttpKnownHeaderNames.Host, GenericHeaderParser.HostParser);
            s_parserStore.Add(HttpKnownHeaderNames.IfMatch, GenericHeaderParser.MultipleValueEntityTagParser);
            s_parserStore.Add(HttpKnownHeaderNames.IfModifiedSince, DateHeaderParser.Parser);
            s_parserStore.Add(HttpKnownHeaderNames.IfNoneMatch, GenericHeaderParser.MultipleValueEntityTagParser);
            s_parserStore.Add(HttpKnownHeaderNames.IfRange, GenericHeaderParser.RangeConditionParser);
            s_parserStore.Add(HttpKnownHeaderNames.IfUnmodifiedSince, DateHeaderParser.Parser);
            s_parserStore.Add(HttpKnownHeaderNames.MaxForwards, Int32NumberHeaderParser.Parser);
            s_parserStore.Add(HttpKnownHeaderNames.ProxyAuthorization, GenericHeaderParser.SingleValueAuthenticationParser);
            s_parserStore.Add(HttpKnownHeaderNames.Range, GenericHeaderParser.RangeParser);
            s_parserStore.Add(HttpKnownHeaderNames.Referer, UriHeaderParser.RelativeOrAbsoluteUriParser);
            s_parserStore.Add(HttpKnownHeaderNames.TE, TransferCodingHeaderParser.MultipleValueWithQualityParser);
            s_parserStore.Add(HttpKnownHeaderNames.UserAgent, ProductInfoHeaderParser.MultipleValueParser);

            HttpGeneralHeaders.AddParsers(s_parserStore);

            s_invalidHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HttpContentHeaders.AddKnownHeaders(s_invalidHeaders);
            // Note: Reserved response header names are allowed as custom request header names.  Reserved response
            // headers have no defined meaning or format when used on a request.  This enables a server to accept
            // any headers sent from the client as either content headers or request headers.
        }

        internal static void AddKnownHeaders(HashSet<string> headerSet)
        {
            Contract.Requires(headerSet != null);

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
            _generalHeaders.AddSpecialsFrom(sourceRequestHeaders._generalHeaders);

            bool? expectContinue = ExpectContinue;
            if (!expectContinue.HasValue)
            {
                ExpectContinue = sourceRequestHeaders.ExpectContinue;
            }
        }
    }
}
