// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http.Headers
{
    public sealed class TransferCodingWithQualityHeaderValue : TransferCodingHeaderValue, ICloneable
    {
        public double? Quality
        {
            get => HeaderUtilities.GetQuality((UnvalidatedObjectCollection<NameValueHeaderValue>)Parameters);
            set => HeaderUtilities.SetQuality((UnvalidatedObjectCollection<NameValueHeaderValue>)Parameters, value);
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

        public static new TransferCodingWithQualityHeaderValue Parse(string? input)
        {
            int index = 0;
            return (TransferCodingWithQualityHeaderValue)TransferCodingHeaderParser.SingleValueWithQualityParser
                .ParseValue(input, null, ref index);
        }

        public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out TransferCodingWithQualityHeaderValue? parsedValue)
        {
            int index = 0;
            parsedValue = null;

            if (TransferCodingHeaderParser.SingleValueWithQualityParser.TryParseValue(
                input, null, ref index, out object? output))
            {
                parsedValue = (TransferCodingWithQualityHeaderValue)output!;
                return true;
            }
            return false;
        }
    }
}
