// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        internal enum CryptProvParam : int
        {
            PP_CLIENT_HWND = 1,
            PP_IMPTYPE = 3,
            PP_NAME = 4,
            PP_CONTAINER = 6,
            PP_PROVTYPE = 16,
            PP_KEYSET_TYPE = 27,
            PP_KEYEXCHANGE_PIN = 32,
            PP_SIGNATURE_PIN = 33,
            PP_UNIQUE_CONTAINER = 36
        }

        [DllImport(Libraries.Advapi32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptGetProvParam(
            SafeHandle safeProvHandle,
            CryptProvParam dwParam,
            IntPtr pbData,
            ref int dwDataLen,
            int dwFlags);

        [DllImport(Libraries.Advapi32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptSetProvParam(
            SafeHandle safeProvHandle,
            CryptProvParam dwParam,
            IntPtr pbData,
            int dwFlags);

        public static bool CryptGetProvParam(
            SafeHandle safeProvHandle,
            CryptProvParam dwParam,
            Span<byte> pbData,
            ref int dwDataLen)
        {
            if (pbData.IsEmpty)
            {
                return CryptGetProvParam(safeProvHandle, dwParam, IntPtr.Zero, ref dwDataLen, 0);
            }

            if (dwDataLen > pbData.Length)
            {
                throw new IndexOutOfRangeException();
            }

            unsafe
            {
                fixed (byte* bytePtr = &MemoryMarshal.GetReference(pbData))
                {
                    return CryptGetProvParam(safeProvHandle, dwParam, (IntPtr)bytePtr, ref dwDataLen, 0);
                }
            }
        }
    }
}
