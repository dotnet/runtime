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
    public sealed class HttpContentHeaders : HttpHeaders
    {
        private static readonly Dictionary<string, HttpHeaderParser> s_parserStore = CreateParserStore();
        private static readonly HashSet<string> s_invalidHeaders = CreateInvalidHeaders();

        private readonly HttpContent _parent;
        private bool _contentLengthSet;

        private HttpHeaderValueCollection<string> _allow;
        private HttpHeaderValueCollection<string> _contentEncoding;
        private HttpHeaderValueCollection<string> _contentLanguage;

        public ICollection<string> Allow
        {
            get
            {
                if (_allow == null)
                {
                    _allow = new HttpHeaderValueCollection<string>(HttpKnownHeaderNames.Allow,
                        this, HeaderUtilities.TokenValidator);
                }
                return _allow;
            }
        }

        public ContentDispositionHeaderValue ContentDisposition
        {
            get { return (ContentDispositionHeaderValue)GetParsedValues(HttpKnownHeaderNames.ContentDisposition); }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.ContentDisposition, value); }
        }

        // Must be a collection (and not provide properties like "GZip", "Deflate", etc.) since the 
        // order matters!
        public ICollection<string> ContentEncoding
        {
            get
            {
                if (_contentEncoding == null)
                {
                    _contentEncoding = new HttpHeaderValueCollection<string>(HttpKnownHeaderNames.ContentEncoding,
                        this, HeaderUtilities.TokenValidator);
                }
                return _contentEncoding;
            }
        }

        public ICollection<string> ContentLanguage
        {
            get
            {
                if (_contentLanguage == null)
                {
                    _contentLanguage = new HttpHeaderValueCollection<string>(HttpKnownHeaderNames.ContentLanguage,
                        this, HeaderUtilities.TokenValidator);
                }
                return _contentLanguage;
            }
        }

        public long? ContentLength
        {
            get
            {
                // 'Content-Length' can only hold one value. So either we get 'null' back or a boxed long value.
                object storedValue = GetParsedValues(HttpKnownHeaderNames.ContentLength);

                // Only try to calculate the length if the user didn't set the value explicitly using the setter.
                if (!_contentLengthSet && (storedValue == null))
                {
                    // If we don't have a value for Content-Length in the store, try to let the content calculate
                    // it's length. If the content object is able to calculate the length, we'll store it in the
                    // store.
                    long? calculatedLength = _parent.GetComputedOrBufferLength();

                    if (calculatedLength != null)
                    {
                        SetParsedValue(HttpKnownHeaderNames.ContentLength, (object)calculatedLength.Value);
                    }

                    return calculatedLength;
                }

                if (storedValue == null)
                {
                    return null;
                }
                else
                {
                    return (long)storedValue;
                }
            }
            set
            {
                SetOrRemoveParsedValue(HttpKnownHeaderNames.ContentLength, value); // box long value
                _contentLengthSet = true;
            }
        }

        public Uri ContentLocation
        {
            get { return (Uri)GetParsedValues(HttpKnownHeaderNames.ContentLocation); }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.ContentLocation, value); }
        }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "In this case the 'value' is the byte array. I.e. the array is treated as a value.")]
        public byte[] ContentMD5
        {
            get { return (byte[])GetParsedValues(HttpKnownHeaderNames.ContentMD5); }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.ContentMD5, value); }
        }

        public ContentRangeHeaderValue ContentRange
        {
            get { return (ContentRangeHeaderValue)GetParsedValues(HttpKnownHeaderNames.ContentRange); }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.ContentRange, value); }
        }

        public MediaTypeHeaderValue ContentType
        {
            get { return (MediaTypeHeaderValue)GetParsedValues(HttpKnownHeaderNames.ContentType); }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.ContentType, value); }
        }

        public DateTimeOffset? Expires
        {
            get { return HeaderUtilities.GetDateTimeOffsetValue(HttpKnownHeaderNames.Expires, this); }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.Expires, value); }
        }

        public DateTimeOffset? LastModified
        {
            get { return HeaderUtilities.GetDateTimeOffsetValue(HttpKnownHeaderNames.LastModified, this); }
            set { SetOrRemoveParsedValue(HttpKnownHeaderNames.LastModified, value); }
        }

        internal HttpContentHeaders(HttpContent parent)
        {
            _parent = parent;

            SetConfiguration(s_parserStore, s_invalidHeaders);
        }

        private static Dictionary<string, HttpHeaderParser> CreateParserStore()
        {
            var parserStore = new Dictionary<string, HttpHeaderParser>(11, StringComparer.OrdinalIgnoreCase);

            parserStore.Add(HttpKnownHeaderNames.Allow, GenericHeaderParser.TokenListParser);
            parserStore.Add(HttpKnownHeaderNames.ContentDisposition, GenericHeaderParser.ContentDispositionParser);
            parserStore.Add(HttpKnownHeaderNames.ContentEncoding, GenericHeaderParser.TokenListParser);
            parserStore.Add(HttpKnownHeaderNames.ContentLanguage, GenericHeaderParser.TokenListParser);
            parserStore.Add(HttpKnownHeaderNames.ContentLength, Int64NumberHeaderParser.Parser);
            parserStore.Add(HttpKnownHeaderNames.ContentLocation, UriHeaderParser.RelativeOrAbsoluteUriParser);
            parserStore.Add(HttpKnownHeaderNames.ContentMD5, ByteArrayHeaderParser.Parser);
            parserStore.Add(HttpKnownHeaderNames.ContentRange, GenericHeaderParser.ContentRangeParser);
            parserStore.Add(HttpKnownHeaderNames.ContentType, MediaTypeHeaderParser.SingleValueParser);
            parserStore.Add(HttpKnownHeaderNames.Expires, DateHeaderParser.Parser);
            parserStore.Add(HttpKnownHeaderNames.LastModified, DateHeaderParser.Parser);

            return parserStore;
        }

        private static HashSet<string> CreateInvalidHeaders()
        {
            var invalidHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            HttpRequestHeaders.AddKnownHeaders(invalidHeaders);
            HttpResponseHeaders.AddKnownHeaders(invalidHeaders);
            HttpGeneralHeaders.AddKnownHeaders(invalidHeaders);

            return invalidHeaders;
        }

        internal static void AddKnownHeaders(HashSet<string> headerSet)
        {
            Debug.Assert(headerSet != null);

            headerSet.Add(HttpKnownHeaderNames.Allow);
            headerSet.Add(HttpKnownHeaderNames.ContentDisposition);
            headerSet.Add(HttpKnownHeaderNames.ContentEncoding);
            headerSet.Add(HttpKnownHeaderNames.ContentLanguage);
            headerSet.Add(HttpKnownHeaderNames.ContentLength);
            headerSet.Add(HttpKnownHeaderNames.ContentLocation);
            headerSet.Add(HttpKnownHeaderNames.ContentMD5);
            headerSet.Add(HttpKnownHeaderNames.ContentRange);
            headerSet.Add(HttpKnownHeaderNames.ContentType);
            headerSet.Add(HttpKnownHeaderNames.Expires);
            headerSet.Add(HttpKnownHeaderNames.LastModified);
        }
    }
}
