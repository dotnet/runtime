// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;

namespace System.Net.Http.Headers
{
    internal class MediaTypeHeaderParser : BaseHeaderParser
    {
        private bool _supportsMultipleValues;
        private Func<MediaTypeHeaderValue> _mediaTypeCreator;

        internal static readonly MediaTypeHeaderParser SingleValueParser = new MediaTypeHeaderParser(false,
            CreateMediaType);
        internal static readonly MediaTypeHeaderParser SingleValueWithQualityParser = new MediaTypeHeaderParser(false,
            CreateMediaTypeWithQuality);
        internal static readonly MediaTypeHeaderParser MultipleValuesParser = new MediaTypeHeaderParser(true,
            CreateMediaTypeWithQuality);

        private MediaTypeHeaderParser(bool supportsMultipleValues, Func<MediaTypeHeaderValue> mediaTypeCreator)
            : base(supportsMultipleValues)
        {
            Contract.Requires(mediaTypeCreator != null);

            _supportsMultipleValues = supportsMultipleValues;
            _mediaTypeCreator = mediaTypeCreator;
        }

        protected override int GetParsedValueLength(string value, int startIndex, object storeValue,
            out object parsedValue)
        {
            MediaTypeHeaderValue temp = null;
            int resultLength = MediaTypeHeaderValue.GetMediaTypeLength(value, startIndex, _mediaTypeCreator, out temp);

            parsedValue = temp;
            return resultLength;
        }

        private static MediaTypeHeaderValue CreateMediaType()
        {
            return new MediaTypeHeaderValue();
        }

        private static MediaTypeHeaderValue CreateMediaTypeWithQuality()
        {
            return new MediaTypeWithQualityHeaderValue();
        }
    }
}
