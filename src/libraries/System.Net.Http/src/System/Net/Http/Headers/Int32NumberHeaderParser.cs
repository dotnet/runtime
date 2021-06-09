// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Net.Http.Headers
{
    internal sealed class Int32NumberHeaderParser : HttpHeaderParser
    {
        // Note that we don't need a custom comparer even though we have a value type that gets boxed (comparing two
        // equal boxed value types returns 'false' since the object instances used for boxing the two values are
        // different). The reason is that the comparer is only used by HttpHeaders when comparing values in a collection.
        // Value types are never used in collections (in fact HttpHeaderValueCollection expects T to be a reference type).

        internal static readonly Int32NumberHeaderParser Parser = new Int32NumberHeaderParser();

        private Int32NumberHeaderParser() : base(false)
        {
        }

        public override string ToString(object value)
        {
            Debug.Assert(value is int);
            return ((int)value).ToString(NumberFormatInfo.InvariantInfo);
        }

        public override bool TryParseValue(string? value, object? storeValue, ref int index, [NotNullWhen(true)] out object? parsedValue)
        {
            if (TryParseInt32Value(value, ref index, out int result))
            {
                parsedValue = result;
                return true;
            }

            parsedValue = null;
            return false;
        }

        internal static bool TryParseInt32Value(string? value, ref int index, out int parsedValue)
        {
            ReadOnlySpan<char> span = value.AsSpan(index);

            // Skip past valid starting whitespace
            int startingWhitespaceLength = HttpRuleParser.GetWhitespaceLength(span);
            if (startingWhitespaceLength != 0)
            {
                span = span.Slice(startingWhitespaceLength);
            }

            // Parse everything until the ending whitespace
            ReadOnlySpan<char> spanTrimmed = span.TrimEnd();
            if (HeaderUtilities.TryParseInt32(spanTrimmed, out int result))
            {
                // If there wasn't ending whitespace or it was valid, give back the successfully parsed value.
                ReadOnlySpan<char> remainingSpace = span.Slice(spanTrimmed.Length);
                if (remainingSpace.IsEmpty || HttpRuleParser.GetWhitespaceLength(remainingSpace) == remainingSpace.Length)
                {
                    parsedValue = result;
                    index = value!.Length;
                    return true;
                }
            }

            // Invalid.
            parsedValue = 0;
            return false;
        }
    }
}
