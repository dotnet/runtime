// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;

namespace System.Net.Http.Headers
{
    internal class TransferCodingHeaderParser : BaseHeaderParser
    {
        private Func<TransferCodingHeaderValue> _transferCodingCreator;

        internal static readonly TransferCodingHeaderParser SingleValueParser =
            new TransferCodingHeaderParser(false, CreateTransferCoding);
        internal static readonly TransferCodingHeaderParser MultipleValueParser =
            new TransferCodingHeaderParser(true, CreateTransferCoding);
        internal static readonly TransferCodingHeaderParser SingleValueWithQualityParser =
            new TransferCodingHeaderParser(false, CreateTransferCodingWithQuality);
        internal static readonly TransferCodingHeaderParser MultipleValueWithQualityParser =
            new TransferCodingHeaderParser(true, CreateTransferCodingWithQuality);

        private TransferCodingHeaderParser(bool supportsMultipleValues,
            Func<TransferCodingHeaderValue> transferCodingCreator)
            : base(supportsMultipleValues)
        {
            Contract.Requires(transferCodingCreator != null);

            _transferCodingCreator = transferCodingCreator;
        }

        protected override int GetParsedValueLength(string value, int startIndex, object storeValue,
            out object parsedValue)
        {
            TransferCodingHeaderValue temp = null;
            int resultLength = TransferCodingHeaderValue.GetTransferCodingLength(value, startIndex,
                _transferCodingCreator, out temp);

            parsedValue = temp;
            return resultLength;
        }

        private static TransferCodingHeaderValue CreateTransferCoding()
        {
            return new TransferCodingHeaderValue();
        }

        private static TransferCodingHeaderValue CreateTransferCodingWithQuality()
        {
            return new TransferCodingWithQualityHeaderValue();
        }
    }
}
