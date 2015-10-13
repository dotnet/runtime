// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative)]
        internal static extern SafeBasicConstraintsHandle DecodeBasicConstraints(byte[] buf, int len);

        [DllImport(Libraries.CryptoNative)]
        internal static extern SafeEkuExtensionHandle DecodeExtendedKeyUsage(byte[] buf, int len);
    }
}
