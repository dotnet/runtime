// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    internal class SocketProtocolSupportPal
    {
        public static bool OSSupportsIPv6 { get; private set; }

        internal static void FakesDisableIPv6()
        {
            OSSupportsIPv6 = false;
        }

        internal static void FakesReset()
        {
            OSSupportsIPv6 = true;
        }
    }
}
