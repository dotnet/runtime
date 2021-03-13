// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace System.Net.Http.Headers
{
    public sealed class HttpContentHeaders : HttpHeaders
    {
        private readonly HttpContent _parent;
        private bool _contentLengthSet;

        private HttpHeaderValueCollection<string>? _allow;
        private HttpHeaderValueCollection<string>? _contentEncoding;
        private HttpHeaderValueCollection<string>? _contentLanguage;

        public ICollection<string> Allow
        {
            get
            {
                if (_allow == null)
                {
                    _allow = new HttpHeaderValueCollection<string>(KnownHeaders.Allow.Descriptor,
                        this, HeaderUtilities.TokenValidator);
                }
                return _allow;
            }
        }

        public ContentDispositionHeaderValue? ContentDisposition
        {
            get { return (ContentDispositionHeaderValue?)GetParsedValues(KnownHeaders.ContentDisposition.Descriptor); }
            set { SetOrRemoveParsedValue(KnownHeaders.ContentDisposition.Descriptor, value); }
        }

        // Must be a collection (and not provide properties like "GZip", "Deflate", etc.) since the
        // order matters!
        public ICollection<string> ContentEncoding
        {
            get
            {
                if (_contentEncoding == null)
                {
                    _contentEncoding = new HttpHeaderValueCollection<string>(KnownHeaders.ContentEncoding.Descriptor,
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
                    _contentLanguage = new HttpHeaderValueCollection<string>(KnownHeaders.ContentLanguage.Descriptor,
                        this, HeaderUtilities.TokenValidator);
                }
                return _contentLanguage;
            }
        }

        public long? ContentLength
        {
            get
            {
                // This could use GetParsedValues; however, for responses we typically access ContentLength
                // only once or twice: once as part of the handler, and potentially once as part of the
                // consumer, e.g. HttpClient.GetStringAsync.  In such cases, we're better off just parsing
                // the raw response content-length twice and not storing anything back, as today storing it
                // back incurs multiple allocations, on top of which if the response headers are enumerated,
                // we then need to format the boxed length back into a string.  Given typical usage patterns,
                // it's better to just parse each time and pay the few additional nanoseconds to re-parse.
                // If parsing multiple times ever becomes a perf issue, we could cache the Content-Length value
                // on HttpContentHeaders as a `long`. This would require additional coordination with the underlying
                // HttpHeaders header store (in case someone then called something like Add or Remove on
                // Content-Length at the HttpHeaders level), e.g. a callback to HttpContentHeaders that
                // Content-Length has been modified in the store and any cached value should be discarded.
                if (TryGetHeaderValue(KnownHeaders.ContentLength.Descriptor, out object? storedValue))
                {
                    // storedValue could be a raw string, a boxed long, or a HeaderStoreItemInfo containing
                    // one of those (RawValue as a string, or ParsedValue as a boxed long).
                    if (storedValue is string storedValueString)
                    {
                        if (long.TryParse(storedValueString, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, out long result))
                        {
                            return result;
                        }
                    }
                    else if (storedValue is long contentLengthFromBox)
                    {
                        return contentLengthFromBox;
                    }
                    else
                    {
                        Debug.Assert(storedValue is HeaderStoreItemInfo, $"Expected {nameof(HeaderStoreItemInfo)}, got {storedValue}");
                        var hsii = (HeaderStoreItemInfo)storedValue;
                        if (hsii.RawValue != null)
                        {
                            if (long.TryParse((string)hsii.RawValue, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, out long result))
                            {
                                return result;
                            }
                        }
                        else if (hsii.ParsedValue != null)
                        {
                            return (long)hsii.ParsedValue;
                        }
                    }
                }
                else if (!_contentLengthSet)
                {
                    // If we don't have a value for Content-Length in the store, try to let the content calculate its
                    // length. If the content object is able to calculate the length, we'll store it in the store.
                    // Only try to calculate the length if the user didn't set the value explicitly using the setter.
                    long? calculatedLength = _parent.GetComputedOrBufferLength();

                    if (calculatedLength != null)
                    {
                        SetParsedValue(KnownHeaders.ContentLength.Descriptor, calculatedLength.GetValueOrDefault());
                    }

                    return calculatedLength;
                }

                return null;
            }
            set
            {
                SetOrRemoveParsedValue(KnownHeaders.ContentLength.Descriptor, value); // box long value
                _contentLengthSet = true;
            }
        }

        public Uri? ContentLocation
        {
            get { return (Uri?)GetParsedValues(KnownHeaders.ContentLocation.Descriptor); }
            set { SetOrRemoveParsedValue(KnownHeaders.ContentLocation.Descriptor, value); }
        }

        public byte[]? ContentMD5
        {
            get { return (byte[]?)GetParsedValues(KnownHeaders.ContentMD5.Descriptor); }
            set { SetOrRemoveParsedValue(KnownHeaders.ContentMD5.Descriptor, value); }
        }

        public ContentRangeHeaderValue? ContentRange
        {
            get { return (ContentRangeHeaderValue?)GetParsedValues(KnownHeaders.ContentRange.Descriptor); }
            set { SetOrRemoveParsedValue(KnownHeaders.ContentRange.Descriptor, value); }
        }

        public MediaTypeHeaderValue? ContentType
        {
            get { return (MediaTypeHeaderValue?)GetParsedValues(KnownHeaders.ContentType.Descriptor); }
            set { SetOrRemoveParsedValue(KnownHeaders.ContentType.Descriptor, value); }
        }

        public DateTimeOffset? Expires
        {
            get { return HeaderUtilities.GetDateTimeOffsetValue(KnownHeaders.Expires.Descriptor, this, DateTimeOffset.MinValue); }
            set { SetOrRemoveParsedValue(KnownHeaders.Expires.Descriptor, value); }
        }

        public DateTimeOffset? LastModified
        {
            get { return HeaderUtilities.GetDateTimeOffsetValue(KnownHeaders.LastModified.Descriptor, this); }
            set { SetOrRemoveParsedValue(KnownHeaders.LastModified.Descriptor, value); }
        }

        internal HttpContentHeaders(HttpContent parent)
            : base(HttpHeaderType.Content | HttpHeaderType.Custom, HttpHeaderType.None)
        {
            _parent = parent;
        }
    }
}
