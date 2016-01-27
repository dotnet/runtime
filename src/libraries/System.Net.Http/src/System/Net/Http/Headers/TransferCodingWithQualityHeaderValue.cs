// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Net.Http.Headers
{
    public sealed class TransferCodingWithQualityHeaderValue : TransferCodingHeaderValue, ICloneable
    {
        public double? Quality
        {
            get { return HeaderUtilities.GetQuality((ObjectCollection<NameValueHeaderValue>)Parameters); }
            set { HeaderUtilities.SetQuality((ObjectCollection<NameValueHeaderValue>)Parameters, value); }
        }

        internal TransferCodingWithQualityHeaderValue()
        {
            // Used by the parser to create a new instance of this type.
        }

        public TransferCodingWithQualityHeaderValue(string value)
            : base(value)
        {
        }

        public TransferCodingWithQualityHeaderValue(string value, double quality)
            : base(value)
        {
            Quality = quality;
        }

        private TransferCodingWithQualityHeaderValue(TransferCodingWithQualityHeaderValue source)
            : base(source)
        {
            // No additional members to initialize here. This constructor is used by Clone().
        }

        object ICloneable.Clone()
        {
            return new TransferCodingWithQualityHeaderValue(this);
        }

        public static new TransferCodingWithQualityHeaderValue Parse(string input)
        {
            int index = 0;
            return (TransferCodingWithQualityHeaderValue)TransferCodingHeaderParser.SingleValueWithQualityParser
                .ParseValue(input, null, ref index);
        }

        public static bool TryParse(string input, out TransferCodingWithQualityHeaderValue parsedValue)
        {
            int index = 0;
            object output;
            parsedValue = null;

            if (TransferCodingHeaderParser.SingleValueWithQualityParser.TryParseValue(
                input, null, ref index, out output))
            {
                parsedValue = (TransferCodingWithQualityHeaderValue)output;
                return true;
            }
            return false;
        }
    }
}
