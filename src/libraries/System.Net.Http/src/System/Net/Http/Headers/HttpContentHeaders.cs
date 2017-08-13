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
                    _allow = new HttpHeaderValueCollection<string>(KnownHeaders.Allow.Descriptor,
                        this, HeaderUtilities.TokenValidator);
                }
                return _allow;
            }
        }

        public ContentDispositionHeaderValue ContentDisposition
        {
            get { return (ContentDispositionHeaderValue)GetParsedValues(KnownHeaders.ContentDisposition.Descriptor); }
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
                // 'Content-Length' can only hold one value. So either we get 'null' back or a boxed long value.
                object storedValue = GetParsedValues(KnownHeaders.ContentLength.Descriptor);

                // Only try to calculate the length if the user didn't set the value explicitly using the setter.
                if (!_contentLengthSet && (storedValue == null))
                {
                    // If we don't have a value for Content-Length in the store, try to let the content calculate
                    // it's length. If the content object is able to calculate the length, we'll store it in the
                    // store.
                    long? calculatedLength = _parent.GetComputedOrBufferLength();

                    if (calculatedLength != null)
                    {
                        SetParsedValue(KnownHeaders.ContentLength.Descriptor, (object)calculatedLength.Value);
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
                SetOrRemoveParsedValue(KnownHeaders.ContentLength.Descriptor, value); // box long value
                _contentLengthSet = true;
            }
        }

        public Uri ContentLocation
        {
            get { return (Uri)GetParsedValues(KnownHeaders.ContentLocation.Descriptor); }
            set { SetOrRemoveParsedValue(KnownHeaders.ContentLocation.Descriptor, value); }
        }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "In this case the 'value' is the byte array. I.e. the array is treated as a value.")]
        public byte[] ContentMD5
        {
            get { return (byte[])GetParsedValues(KnownHeaders.ContentMD5.Descriptor); }
            set { SetOrRemoveParsedValue(KnownHeaders.ContentMD5.Descriptor, value); }
        }

        public ContentRangeHeaderValue ContentRange
        {
            get { return (ContentRangeHeaderValue)GetParsedValues(KnownHeaders.ContentRange.Descriptor); }
            set { SetOrRemoveParsedValue(KnownHeaders.ContentRange.Descriptor, value); }
        }

        public MediaTypeHeaderValue ContentType
        {
            get { return (MediaTypeHeaderValue)GetParsedValues(KnownHeaders.ContentType.Descriptor); }
            set { SetOrRemoveParsedValue(KnownHeaders.ContentType.Descriptor, value); }
        }

        public DateTimeOffset? Expires
        {
            get { return HeaderUtilities.GetDateTimeOffsetValue(KnownHeaders.Expires.Descriptor, this); }
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
