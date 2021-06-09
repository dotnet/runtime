// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;

namespace System.Net.Http.Headers
{
    internal sealed class TimeSpanHeaderParser : BaseHeaderParser
    {
        internal static readonly TimeSpanHeaderParser Parser = new TimeSpanHeaderParser();

        private TimeSpanHeaderParser()
            : base(false)
        {
        }

        public override string ToString(object value)
        {
            Debug.Assert(value is TimeSpan);

            return ((int)((TimeSpan)value).TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
        }

        protected override int GetParsedValueLength(string value, int startIndex, object? storeValue,
            out object? parsedValue)
        {
            ReadOnlySpan<char> span = value.AsSpan(startIndex).Trim();
            if (HeaderUtilities.TryParseInt32(span, out int result))
            {
                parsedValue = new TimeSpan(0, 0, result);
                return span.Length;
            }

            parsedValue = null;
            return 0;
        }
    }
}
