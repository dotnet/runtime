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
        internal static unsafe extern ErrorCode NCryptSignHash(SafeNCryptKeyHandle hKey, void* pPaddingInfo, [In] byte[] pbHashValue, int cbHashValue, [Out] byte[] pbSignature, int cbSignature, out int pcbResult, AsymmetricPaddingMode dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static unsafe extern ErrorCode NCryptVerifySignature(SafeNCryptKeyHandle hKey, void *pPaddingInfo, [In] byte[] pbHashValue, int cbHashValue, [In] byte[] pbSignature, int cbSignature, AsymmetricPaddingMode dwFlags);
    }
}
