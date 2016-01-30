// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyDestroy")]
        internal static extern void EcKeyDestroy(IntPtr a);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyCreateByCurveName")]
        internal static extern SafeEcKeyHandle EcKeyCreateByCurveName(int nid);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyGenerateKey")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EcKeyGenerateKey(SafeEcKeyHandle eckey);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyUpRef")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EcKeyUpRef(IntPtr r);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyGetCurveName")]
        internal static extern int EcKeyGetCurveName(SafeEcKeyHandle ecKey);
    }
}
