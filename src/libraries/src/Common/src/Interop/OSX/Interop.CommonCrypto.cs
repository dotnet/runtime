// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    // Direct support on top of Apple CommonCrypto.
    // In general, this should not be used, the System.Security.Cryptography.Native.Apple shim
    // being preferred. But when there is a layering complication, or other compelling reason,
    // then this can be used directly.
    internal static partial class CommonCrypto
    {
        [DllImport(Libraries.LibSystemCommonCrypto)]
        internal static extern unsafe int CCRandomGenerateBytes(byte* bytes, int byteCount);
    }
}