// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System.Net
{
    internal static class IPv4AddressHelper
    {
        internal const int Invalid = -1;
        internal static unsafe long ParseNonCanonical<TChar>(in ReadOnlySpan<TChar> name, out int bytesConsumed, bool notImplicitFile)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            bytesConsumed = 0;
            return 0;
        }
    }
}
