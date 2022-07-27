// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace System.Net.Http.Headers
{
    public class TransferCodingHeaderValue : ICloneable
    {
        // Use UnvalidatedObjectCollection<T> since we may have multiple parameters with the same name.
        private UnvalidatedObjectCollection<NameValueHeaderValue>? _parameters;
        private string _value = null!; // empty constructor only used internally and value set with non null

        public string Value => _value;

        public ICollection<NameValueHeaderValue> Parameters => _parameters ??= new UnvalidatedObjectCollection<NameValueHeaderValue>();

        internal TransferCodingHeaderValue()
        {
        }

        protected TransferCodingHeaderValue(TransferCodingHeaderValue source)
        {
            Debug.Assert(source != null);

            _value = source._value;
            _parameters = source._parameters.Clone();
        }

        public TransferCodingHeaderValue(string value)
        {
            HeaderUtilities.CheckValidToken(value, nameof(value));
            _value = value;
        }

        public static TransferCodingHeaderValue Parse(string? input)
        {
            int index = 0;
            return (TransferCodingHeaderValue)TransferCodingHeaderParser.SingleValueParser.ParseValue(
                input, null, ref index);
        }

        public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out TransferCodingHeaderValue? parsedValue)
        {
            int index = 0;
            parsedValue = null;

            if (TransferCodingHeaderParser.SingleValueParser.TryParseValue(input, null, ref index, out object? output))
            {
                parsedValue = (TransferCodingHeaderValue)output!;
                return true;
            }
            return false;
        }

        internal static int GetTransferCodingLength(string input, int startIndex,
            Func<TransferCodingHeaderValue> transferCodingCreator, out TransferCodingHeaderValue? parsedValue)
        {
            Debug.Assert(transferCodingCreator != null);
            Debug.Assert(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Caller must remove leading whitespace. If not, we'll return 0.
            int valueLength = HttpRuleParser.GetTokenLength(input, startIndex);

            if (valueLength == 0)
            {
                return 0;
            }

            string value = input.Substring(startIndex, valueLength);
            int current = startIndex + valueLength;
            current += HttpRuleParser.GetWhitespaceLength(input, current);
            TransferCodingHeaderValue transferCodingHeader;

            // If we're not done and we have a parameter delimiter, then we have a list of parameters.
            if ((current < input.Length) && (input[current] == ';'))
            {
                transferCodingHeader = transferCodingCreator();
                transferCodingHeader._value = value;

                current++; // skip delimiter.
                int parameterLength = NameValueHeaderValue.GetNameValueListLength(input, current, ';',
                    (UnvalidatedObjectCollection<NameValueHeaderValue>)transferCodingHeader.Parameters);

                if (parameterLength == 0)
                {
                    return 0;
                }

                parsedValue = transferCodingHeader;
                return current + parameterLength - startIndex;
            }

            // We have a transfer coding without parameters.
            transferCodingHeader = transferCodingCreator();
            transferCodingHeader._value = value;
            parsedValue = transferCodingHeader;
            return current - startIndex;
        }

        public override string ToString()
        {
            StringBuilder sb = StringBuilderCache.Acquire();
            sb.Append(_value);
            NameValueHeaderValue.ToString(_parameters, ';', true, sb);
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            TransferCodingHeaderValue? other = obj as TransferCodingHeaderValue;

            if (other == null)
            {
                return false;
            }

            return string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase) &&
                HeaderUtilities.AreEqualCollections(_parameters, other._parameters);
        }

        public override int GetHashCode()
        {
            // The value string is case-insensitive.
            return StringComparer.OrdinalIgnoreCase.GetHashCode(_value) ^ NameValueHeaderValue.GetHashCode(_parameters);
        }

        // Implement ICloneable explicitly to allow derived types to "override" the implementation.
        object ICloneable.Clone()
        {
            return new TransferCodingHeaderValue(this);
        }
    }
}
