// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http.Headers
{
    internal sealed class CookieHeaderParser : HttpHeaderParser
    {
        internal static readonly CookieHeaderParser Parser = new CookieHeaderParser();

        // According to RFC 6265 Section 4.2 multiple cookies have
        // to be concatenated using "; " as the separator.
        private CookieHeaderParser()
            : base(true, "; ")
        {
        }

        public override bool TryParseValue(string? value, object? storeValue, ref int index, [NotNullWhen(true)] out object? parsedValue)
        {
            Debug.Assert(index == 0);

            // Some headers support empty/null values. This one doesn't.
            if (string.IsNullOrEmpty(value) || HttpRuleParser.ContainsNewLine(value))
            {
                parsedValue = null;
                return false;
            }

            parsedValue = value;
            index = value.Length;

            return true;
        }
    }
}
