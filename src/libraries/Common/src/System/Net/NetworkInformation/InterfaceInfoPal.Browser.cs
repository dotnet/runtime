// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System.Net.NetworkInformation
{
    internal static class InterfaceInfoPal
    {
        public static uint InterfaceNameToIndex<TChar>(ReadOnlySpan<TChar> _/*interfaceName*/)
            where TChar : unmanaged, IBinaryNumber<TChar>
        {
            // zero means "unknown"
            return 0;
        }
    }
}
