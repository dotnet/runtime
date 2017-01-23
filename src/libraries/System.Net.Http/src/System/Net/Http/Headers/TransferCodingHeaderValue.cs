// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace System.Net.Http.Headers
{
    public class TransferCodingHeaderValue : ICloneable
    {
        // Use ObjectCollection<T> since we may have multiple parameters with the same name.
        private ObjectCollection<NameValueHeaderValue> _parameters;
        private string _value;

        public string Value
        {
            get { return _value; }
        }

        public ICollection<NameValueHeaderValue> Parameters
        {
            get
            {
                if (_parameters == null)
                {
                    _parameters = new ObjectCollection<NameValueHeaderValue>();
                }
                return _parameters;
            }
        }

        internal TransferCodingHeaderValue()
        {
        }

        protected TransferCodingHeaderValue(TransferCodingHeaderValue source)
        {
            Debug.Assert(source != null);

            _value = source._value;

            if (source._parameters != null)
            {
                foreach (var parameter in source._parameters)
                {
                    this.Parameters.Add((NameValueHeaderValue)((ICloneable)parameter).Clone());
                }
            }
        }

        public TransferCodingHeaderValue(string value)
        {
            HeaderUtilities.CheckValidToken(value, nameof(value));
            _value = value;
        }

        public static TransferCodingHeaderValue Parse(string input)
        {
            int index = 0;
            return (TransferCodingHeaderValue)TransferCodingHeaderParser.SingleValueParser.ParseValue(
                input, null, ref index);
        }

        public static bool TryParse(string input, out TransferCodingHeaderValue parsedValue)
        {
            int index = 0;
            object output;
            parsedValue = null;

            if (TransferCodingHeaderParser.SingleValueParser.TryParseValue(input, null, ref index, out output))
            {
                parsedValue = (TransferCodingHeaderValue)output;
                return true;
            }
            return false;
        }

        internal static int GetTransferCodingLength(string input, int startIndex,
            Func<TransferCodingHeaderValue> transferCodingCreator, out TransferCodingHeaderValue parsedValue)
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
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);
            TransferCodingHeaderValue transferCodingHeader = null;

            // If we're not done and we have a parameter delimiter, then we have a list of parameters.
            if ((current < input.Length) && (input[current] == ';'))
            {
                transferCodingHeader = transferCodingCreator();
                transferCodingHeader._value = value;

                current++; // skip delimiter.
                int parameterLength = NameValueHeaderValue.GetNameValueListLength(input, current, ';',
                    (ObjectCollection<NameValueHeaderValue>)transferCodingHeader.Parameters);

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

        public override bool Equals(object obj)
        {
            TransferCodingHeaderValue other = obj as TransferCodingHeaderValue;

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
