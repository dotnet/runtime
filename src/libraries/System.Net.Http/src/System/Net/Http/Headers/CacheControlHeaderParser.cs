// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net.Http.Headers
{
    internal sealed class CacheControlHeaderParser : BaseHeaderParser
    {
        internal static readonly CacheControlHeaderParser Parser = new CacheControlHeaderParser();

        // The Cache-Control header is special: It is a header supporting a list of values, but we represent the list
        // as _one_ instance of CacheControlHeaderValue. I.e we set 'SupportsMultipleValues' to 'true' since it is
        // OK to have multiple Cache-Control headers in a request/response message. However, after parsing all
        // Cache-Control headers, only one instance of CacheControlHeaderValue is created (if all headers contain valid
        // values, otherwise we may have multiple strings containing the invalid values).
        private CacheControlHeaderParser()
            : base(true)
        {
        }

        protected override int GetParsedValueLength(string value, int startIndex, object? storeValue,
            out object? parsedValue)
        {
            CacheControlHeaderValue? temp = storeValue as CacheControlHeaderValue;
            Debug.Assert(storeValue == null || temp != null, "'storeValue' is not of type CacheControlHeaderValue");

            int resultLength = CacheControlHeaderValue.GetCacheControlLength(value, startIndex, temp, out temp);

            parsedValue = temp;
            return resultLength;
        }
    }
}
