// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
        private static OSPlatform s_freeBSD = OSPlatform.Create("FREEBSD");

        public static bool IsOSPlatform(OSPlatform osPlatform)
        {
            return s_freeBSD == osPlatform;
        }
    }
}
