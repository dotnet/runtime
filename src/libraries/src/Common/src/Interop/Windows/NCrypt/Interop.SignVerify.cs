// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class NCrypt
    {
        internal static unsafe ErrorCode NCryptSignHash(SafeNCryptKeyHandle hKey, void* pPaddingInfo, ReadOnlySpan<byte> pbHashValue, int cbHashValue, Span<byte> pbSignature, int cbSignature, out int pcbResult, AsymmetricPaddingMode dwFlags)
        {
            fixed (byte* pbHashValuePtr = &pbHashValue.DangerousGetPinnableReference())
            fixed (byte* pbSignaturePtr = &pbSignature.DangerousGetPinnableReference())
            {
                return NCryptSignHash(hKey, pPaddingInfo, pbHashValuePtr, cbHashValue, pbSignaturePtr, cbSignature, out pcbResult, dwFlags);
            }
        }

        [DllImport(Libraries.NCrypt, CharSet = CharSet.Unicode)]
        private static extern unsafe ErrorCode NCryptSignHash(SafeNCryptKeyHandle hKey, void* pPaddingInfo, byte* pbHashValue, int cbHashValue, byte* pbSignature, int cbSignature, out int pcbResult, AsymmetricPaddingMode dwFlags);

        internal static unsafe ErrorCode NCryptVerifySignature(SafeNCryptKeyHandle hKey, void* pPaddingInfo, ReadOnlySpan<byte> pbHashValue, int cbHashValue, ReadOnlySpan<byte> pbSignature, int cbSignature, AsymmetricPaddingMode dwFlags)
        {
            fixed (byte* pbHashValuePtr = &pbHashValue.DangerousGetPinnableReference())
            fixed (byte* pbSignaturePtr = &pbSignature.DangerousGetPinnableReference())
            {
                return NCryptVerifySignature(hKey, pPaddingInfo, pbHashValuePtr, cbHashValue, pbSignaturePtr, cbSignature, dwFlags);
            }
        }

        [DllImport(Libraries.NCrypt, CharSet = CharSet.Unicode)]
        private static extern unsafe ErrorCode NCryptVerifySignature(SafeNCryptKeyHandle hKey, void* pPaddingInfo, byte* pbHashValue, int cbHashValue, byte* pbSignature, int cbSignature, AsymmetricPaddingMode dwFlags);
    }
}
