// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http.Headers
{
    // Don't derive from BaseHeaderParser since parsing is delegated to DateTimeOffset.TryParseExact()
    // which will remove leading, trailing, and whitespace in the middle of the string.
    internal sealed class DateHeaderParser : HttpHeaderParser
    {
        internal static readonly DateHeaderParser Parser = new DateHeaderParser();

        private DateHeaderParser()
            : base(false)
        {
        }

        public override string ToString(object value)
        {
            Debug.Assert(value is DateTimeOffset);

            return ((DateTimeOffset)value).ToString("r");
        }

        public override bool TryParseValue([NotNullWhen(true)] string? value, object? storeValue, ref int index, [NotNullWhen(true)] out object? parsedValue)
        {
            parsedValue = null;

            // Some headers support empty/null values. This one doesn't.
            if (string.IsNullOrEmpty(value) || (index == value.Length))
            {
                return false;
            }

            ReadOnlySpan<char> dateString = value;
            if (index > 0)
            {
                dateString = value.AsSpan(index);
            }

            DateTimeOffset date;
            if (!HttpDateParser.TryParse(dateString, out date))
            {
                return false;
            }

            index = value.Length;
            parsedValue = date;
            return true;
        }
    }
}
