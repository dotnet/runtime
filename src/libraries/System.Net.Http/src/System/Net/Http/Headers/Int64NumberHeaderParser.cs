// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;

namespace System.Net.Http.Headers
{
    internal sealed class Int64NumberHeaderParser : BaseHeaderParser
    {
        // Note that we don't need a custom comparer even though we have a value type that gets boxed (comparing two
        // equal boxed value types returns 'false' since the object instances used for boxing the two values are
        // different). The reason is that the comparer is only used by HttpHeaders when comparing values in a collection.
        // Value types are never used in collections (in fact HttpHeaderValueCollection expects T to be a reference
        // type).

        internal static readonly Int64NumberHeaderParser Parser = new Int64NumberHeaderParser();

        private Int64NumberHeaderParser()
            : base(false)
        {
        }

        public override string ToString(object value)
        {
            Debug.Assert(value is long);

            return ((long)value).ToString(NumberFormatInfo.InvariantInfo);
        }

        protected override int GetParsedValueLength(string value, int startIndex, object? storeValue,
            out object? parsedValue)
        {
            ReadOnlySpan<char> span = value.AsSpan(startIndex).Trim();
            if (HeaderUtilities.TryParseInt64(span, out long result))
            {
                parsedValue = result;
                return span.Length;
            }

            parsedValue = null;
            return 0;
        }
    }
}
