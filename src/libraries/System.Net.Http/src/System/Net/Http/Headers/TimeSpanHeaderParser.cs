// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Net.Http.Headers
{
    internal sealed class TimeSpanHeaderParser : HttpHeaderParser
    {
        internal static readonly TimeSpanHeaderParser Parser = new TimeSpanHeaderParser();

        private TimeSpanHeaderParser() : base(supportsMultipleValues: false)
        {
        }

        public override string ToString(object value)
        {
            Debug.Assert(value is TimeSpan);
            return ((int)((TimeSpan)value).TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
        }

        public override bool TryParseValue(string? value, object? storeValue, ref int index, [NotNullWhen(true)] out object? parsedValue)
        {
            if (Int32NumberHeaderParser.TryParseInt32Value(value, ref index, out int result))
            {
                parsedValue = new TimeSpan(0, 0, result);
                return true;
            }

            parsedValue = null;
            return false;
        }
    }
}
