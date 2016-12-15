// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class NCrypt
    {

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern unsafe ErrorCode NCryptEncrypt(SafeNCryptKeyHandle hKey, [In] byte[] pbInput, int cbInput, void* pPaddingInfo, [Out] byte[] pbOutput, int cbOutput, out int pcbResult, AsymmetricPaddingMode dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern unsafe ErrorCode NCryptEncrypt(SafeNCryptKeyHandle hKey, byte* pbInput, int cbInput, void* pPaddingInfo, byte* pbOutput, int cbOutput, out int pcbResult, AsymmetricPaddingMode dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern unsafe ErrorCode NCryptDecrypt(SafeNCryptKeyHandle hKey, [In] byte[] pbInput, int cbInput, void* pPaddingInfo, [Out] byte[] pbOutput, int cbOutput, out int pcbResult, AsymmetricPaddingMode dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern unsafe ErrorCode NCryptDecrypt(SafeNCryptKeyHandle hKey, byte* pbInput, int cbInput, void* pPaddingInfo, byte* pbOutput, int cbOutput, out int pcbResult, AsymmetricPaddingMode dwFlags);
    }
}
