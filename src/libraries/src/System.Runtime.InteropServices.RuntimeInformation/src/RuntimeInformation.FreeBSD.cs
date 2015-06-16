// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.InteropServices
{
    public static class RuntimeInformation
    {
        private static OSPlatform s_freeBSD = OSPlatform.Create("FREEBSD");

        public static bool IsOSPlatform(OSPlatform osPlatform)
        {
            return s_freeBSD == osPlatform;
        }
    }
}
