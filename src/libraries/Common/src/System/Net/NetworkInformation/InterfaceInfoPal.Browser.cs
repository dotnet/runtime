// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.NetworkInformation
{
    internal static class InterfaceInfoPal
    {
        public static uint InterfaceNameToIndex(string _/*interfaceName*/)
        {
            // zero means "unknown"
            return 0;
        }

        public static uint InterfaceNameToIndex(ReadOnlySpan<char> _/*interfaceName*/)
        {
            // zero means "unknown"
            return 0;
        }

        public static uint InterfaceNameToIndex(ReadOnlySpan<byte> _/*interfaceName*/)
        {
            // zero means "unknown"
            return 0;
        }
    }
}
