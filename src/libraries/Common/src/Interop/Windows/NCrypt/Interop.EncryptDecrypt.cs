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
        internal static unsafe ErrorCode NCryptEncrypt(SafeNCryptKeyHandle hKey, ReadOnlySpan<byte> pbInput, int cbInput, void* pPaddingInfo, Span<byte> pbOutput, int cbOutput, out int pcbResult, AsymmetricPaddingMode dwFlags)
        {
            fixed (byte* pbInputPtr = &pbInput.DangerousGetPinnableReference())
            fixed (byte* pbOutputPtr = &pbOutput.DangerousGetPinnableReference())
            {
                return NCryptEncrypt(hKey, pbInputPtr, cbInput, pPaddingInfo, pbOutputPtr, cbOutput, out pcbResult, dwFlags);
            }
        }

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        private static extern unsafe ErrorCode NCryptEncrypt(SafeNCryptKeyHandle hKey, byte* pbInput, int cbInput, void* pPaddingInfo, byte* pbOutput, int cbOutput, out int pcbResult, AsymmetricPaddingMode dwFlags);

        internal static unsafe ErrorCode NCryptDecrypt(SafeNCryptKeyHandle hKey, ReadOnlySpan<byte> pbInput, int cbInput, void* pPaddingInfo, Span<byte> pbOutput, int cbOutput, out int pcbResult, AsymmetricPaddingMode dwFlags)
        {
            fixed (byte* pbInputPtr = &pbInput.DangerousGetPinnableReference())
            fixed (byte* pbOutputPtr = &pbOutput.DangerousGetPinnableReference())
            {
                return NCryptDecrypt(hKey, pbInputPtr, cbInput, pPaddingInfo, pbOutputPtr, cbOutput, out pcbResult, dwFlags);
            }
        }

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        private static extern unsafe ErrorCode NCryptDecrypt(SafeNCryptKeyHandle hKey, byte* pbInput, int cbInput, void* pPaddingInfo, byte* pbOutput, int cbOutput, out int pcbResult, AsymmetricPaddingMode dwFlags);
    }
}
