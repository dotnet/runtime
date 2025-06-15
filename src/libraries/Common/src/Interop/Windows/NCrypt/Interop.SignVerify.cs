// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class NCrypt
    {
        [LibraryImport(Libraries.NCrypt, StringMarshalling = StringMarshalling.Utf16)]
        private static unsafe partial ErrorCode NCryptSignHash(SafeNCryptKeyHandle hKey, void* pPaddingInfo, byte* pbHashValue, int cbHashValue, byte* pbSignature, int cbSignature, out int pcbResult, AsymmetricPaddingMode dwFlags);

        internal static unsafe ErrorCode NCryptSignHash(SafeNCryptKeyHandle hKey, void* pPaddingInfo, ReadOnlySpan<byte> pbHashValue, Span<byte> pbSignature, out int pcbResult, AsymmetricPaddingMode dwFlags)
        {
            fixed (byte* pHash = &Helpers.GetNonNullPinnableReference(pbHashValue))
            fixed (byte* pSignature = &Helpers.GetNonNullPinnableReference(pbSignature))
            {
                return NCryptSignHash(hKey, pPaddingInfo, pHash, pbHashValue.Length, pSignature, pbSignature.Length, out pcbResult, dwFlags);
            }
        }

        [LibraryImport(Libraries.NCrypt, StringMarshalling = StringMarshalling.Utf16)]
        private static unsafe partial ErrorCode NCryptVerifySignature(SafeNCryptKeyHandle hKey, void* pPaddingInfo, byte* pbHashValue, int cbHashValue, byte* pbSignature, int cbSignature, AsymmetricPaddingMode dwFlags);

        internal static unsafe ErrorCode NCryptVerifySignature(SafeNCryptKeyHandle hKey, void* pPaddingInfo, ReadOnlySpan<byte> pbHashValue, int cbHashValue, ReadOnlySpan<byte> pbSignature, int cbSignature, AsymmetricPaddingMode dwFlags)
        {
            fixed (byte* pHash = &Helpers.GetNonNullPinnableReference(pbHashValue))
            fixed (byte* pSignature = &Helpers.GetNonNullPinnableReference(pbSignature))
            {
                return NCryptVerifySignature(hKey, pPaddingInfo, pHash, cbHashValue, pSignature, cbSignature, dwFlags);
            }
        }
    }
}
