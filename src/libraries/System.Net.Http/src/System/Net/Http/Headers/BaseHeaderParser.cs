// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http.Headers
{
    internal abstract class BaseHeaderParser : HttpHeaderParser
    {
        protected BaseHeaderParser(bool supportsMultipleValues)
            : base(supportsMultipleValues)
        {
        }

        /// <summary>
        /// Parses a full header or a segment of a multi-value header.
        /// </summary>
        /// <param name="value">The header value string to parse.</param>
        /// <param name="startIndex">The index to begin parsing at.</param>
        /// <param name="storeValue"></param>
        /// <param name="parsedValue">The resulting value parsed.</param>
        /// <returns>If a value could be parsed, the number of characters used to parse that value. Otherwise, 0.</returns>
        protected abstract int GetParsedValueLength(string value, int startIndex, object? storeValue,
            out object? parsedValue);

#pragma warning disable CS8765 // Doesn't match overridden member nullable attribute on out parameter
        public sealed override bool TryParseValue(string? value, object? storeValue, ref int index,
            out object? parsedValue)
#pragma warning restore CS8765
        {
            parsedValue = null;

            // If multiple values are supported (i.e. list of values), then accept an empty string: The header may
            // be added multiple times to the request/response message. E.g.
            //  Accept: text/xml; q=1
            //  Accept:
            //  Accept: text/plain; q=0.2
            if (string.IsNullOrEmpty(value) || (index == value.Length))
            {
                return SupportsMultipleValues;
            }

            bool separatorFound;
            int current = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(value, index, SupportsMultipleValues,
                out separatorFound);

            if (separatorFound && !SupportsMultipleValues)
            {
                return false; // leading separators not allowed if we don't support multiple values.
            }

            if (current == value.Length)
            {
                if (SupportsMultipleValues)
                {
                    index = current;
                }
                return SupportsMultipleValues;
            }

            int length = GetParsedValueLength(value, current, storeValue, out object? result);

            if (length == 0)
            {
                return false;
            }

            current += length;
            current = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(value, current, SupportsMultipleValues,
                out separatorFound);

            // If we support multiple values and we've not reached the end of the string, then we must have a separator.
            if ((separatorFound && !SupportsMultipleValues) || (!separatorFound && (current < value.Length)))
            {
                return false;
            }

            index = current;
            parsedValue = result!;
            return true;
        }
    }
}
