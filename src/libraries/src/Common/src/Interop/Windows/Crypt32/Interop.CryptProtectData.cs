// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;

internal partial class Interop
{
    internal partial class Crypt32
    {
        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptProtectData(
                  [In] IntPtr pDataIn,
                  [In] string szDataDescr,
                  [In] IntPtr pOptionalEntropy,
                  [In] IntPtr pvReserved,
                  [In] IntPtr pPromptStruct,
                  [In] uint dwFlags,
                  [In, Out] IntPtr pDataOut);

        private const uint CRYPTPROTECTMEMORY_SAME_PROCESS = 0x00;

        internal static unsafe bool CryptProtectData(SafeBSTRHandle uncryptedBuffer, out SafeBSTRHandle cryptedBuffer)
        {
            byte* uncryptedBufferPtr = null;
            DATA_BLOB pDataOut = default(DATA_BLOB);
            try
            {
                uncryptedBuffer.AcquirePointer(ref uncryptedBufferPtr);
                DATA_BLOB pDataIn = new DATA_BLOB((IntPtr)uncryptedBufferPtr, uncryptedBuffer.Length * 2);
                if (CryptProtectData(new IntPtr(&pDataIn), String.Empty, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, CRYPTPROTECTMEMORY_SAME_PROCESS, new IntPtr(&pDataOut)))
                {
                    SafeBSTRHandle newHandle = SafeBSTRHandle.Allocate(pDataOut.pbData, pDataOut.cbData);
                    cryptedBuffer = newHandle;
                    return true;
                }
                else
                {
                    cryptedBuffer = SafeBSTRHandle.Allocate(null, 0);
                    return false;
                }
            }
            finally
            {
                if (uncryptedBufferPtr != null)
                    uncryptedBuffer.ReleasePointer();

                if (pDataOut.pbData != IntPtr.Zero)
                {
                    NtDll.ZeroMemory(pDataOut.pbData, (UIntPtr)pDataOut.cbData);
                    Marshal.FreeHGlobal(pDataOut.pbData);
                }
            }
        }
    }
}
