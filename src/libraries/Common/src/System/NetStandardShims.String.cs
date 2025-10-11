// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    internal static partial class NetStandardShims
    {
        extension(string @this)
        {
            public bool Contains(char value)
            {
#pragma warning disable CA2249 // Consider using 'string.Contains' instead of 'string.IndexOf'
                return @this.IndexOf(value) >= 0;
#pragma warning restore CA2249
            }

            public bool Contains(string value, StringComparison comparisonType)
            {
#pragma warning disable CA2249 // Consider using 'string.Contains' instead of 'string.IndexOf'
                return @this.IndexOf(value, comparisonType) >= 0;
#pragma warning restore CA2249
            }
        }
    }
}
