// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http.Headers
{
    // Don't derive from BaseHeaderParser since parsing the Base64 string is delegated to Convert.FromBase64String()
    // which will remove leading, trailing, and whitespace in the middle of the string.
    internal sealed class ByteArrayHeaderParser : HttpHeaderParser
    {
        internal static readonly ByteArrayHeaderParser Parser = new ByteArrayHeaderParser();

        private ByteArrayHeaderParser()
            : base(false)
        {
        }

        public override string ToString(object value)
        {
            Debug.Assert(value is byte[]);

            return Convert.ToBase64String((byte[])value);
        }

        public override bool TryParseValue([NotNullWhen(true)] string? value, object? storeValue, ref int index, [NotNullWhen(true)] out object? parsedValue)
        {
            parsedValue = null;

            // Some headers support empty/null values. This one doesn't.
            if (string.IsNullOrEmpty(value) || (index == value.Length))
            {
                return false;
            }

            string base64String = value;
            if (index > 0)
            {
                base64String = value.Substring(index);
            }

            // Try convert the string (we assume it's a valid Base64 string) to byte[].
            try
            {
                parsedValue = Convert.FromBase64String(base64String);
                index = value.Length;
                return true;
            }
            catch (FormatException e)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, SR.Format(SR.net_http_parser_invalid_base64_string, base64String, e.Message));
            }

            return false;
        }
    }
}
