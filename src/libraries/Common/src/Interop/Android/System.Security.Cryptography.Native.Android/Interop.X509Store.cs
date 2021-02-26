// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509StoreEnumerateTrustedCertificates")]
        internal static extern unsafe int X509StoreEnumerateTrustedCertificates(
            byte systemOnly,
            delegate* unmanaged<void*, void*, void> callback,
            void *callbackContext);
    }
}
