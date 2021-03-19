// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static SafeCFDataHandle SecKeyExportData(
            SafeSecKeyRefHandle? key,
            bool exportPrivate,
            ReadOnlySpan<char> password)
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static byte[] SecKeyExport(
            SafeSecKeyRefHandle? key,
            bool exportPrivate,
            string password)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
