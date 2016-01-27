// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Net.Http.Headers
{
    public sealed class MediaTypeWithQualityHeaderValue : MediaTypeHeaderValue, ICloneable
    {
        public double? Quality
        {
            get { return HeaderUtilities.GetQuality((ObjectCollection<NameValueHeaderValue>)Parameters); }
            set { HeaderUtilities.SetQuality((ObjectCollection<NameValueHeaderValue>)Parameters, value); }
        }

        internal MediaTypeWithQualityHeaderValue()
            : base()
        {
            // Used by the parser to create a new instance of this type.
        }

        public MediaTypeWithQualityHeaderValue(string mediaType)
            : base(mediaType)
        {
        }

        public MediaTypeWithQualityHeaderValue(string mediaType, double quality)
            : base(mediaType)
        {
            Quality = quality;
        }

        private MediaTypeWithQualityHeaderValue(MediaTypeWithQualityHeaderValue source)
            : base(source)
        {
            // No additional members to initialize here. This constructor is used by Clone().
        }

        object ICloneable.Clone()
        {
            return new MediaTypeWithQualityHeaderValue(this);
        }

        public static new MediaTypeWithQualityHeaderValue Parse(string input)
        {
            int index = 0;
            return (MediaTypeWithQualityHeaderValue)MediaTypeHeaderParser.SingleValueWithQualityParser.ParseValue(
                input, null, ref index);
        }

        public static bool TryParse(string input, out MediaTypeWithQualityHeaderValue parsedValue)
        {
            int index = 0;
            object output;
            parsedValue = null;

            if (MediaTypeHeaderParser.SingleValueWithQualityParser.TryParseValue(input, null, ref index, out output))
            {
                parsedValue = (MediaTypeWithQualityHeaderValue)output;
                return true;
            }
            return false;
        }
    }
}
