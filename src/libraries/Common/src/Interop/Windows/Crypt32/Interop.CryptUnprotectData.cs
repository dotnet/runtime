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
        private static extern bool CryptUnprotectData(
                  [In] IntPtr pDataIn,
                  [In] IntPtr ppszDataDescr,
                  [In] IntPtr pOptionalEntropy,
                  [In] IntPtr pvReserved,
                  [In] IntPtr pPromptStruct,
                  [In] uint dwFlags,
                  [In, Out] IntPtr pDataOut);

        internal static unsafe bool CryptUnProtectData(SafeBSTRHandle cryptedBuffer, out SafeBSTRHandle uncryptedBuffer)
        {
            byte* cryptedBufferPtr = null;
            DATA_BLOB pDataOut = default(DATA_BLOB);
            try
            {
                cryptedBuffer.AcquirePointer(ref cryptedBufferPtr);
                DATA_BLOB pDataIn = new DATA_BLOB((IntPtr)cryptedBufferPtr, cryptedBuffer.Length * 2);
                if (CryptUnprotectData(new IntPtr(&pDataIn), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, CRYPTPROTECTMEMORY_SAME_PROCESS, new IntPtr(&pDataOut)))
                {
                    SafeBSTRHandle newHandle = SafeBSTRHandle.Allocate(pDataOut.pbData, pDataOut.cbData);
                    uncryptedBuffer = newHandle;
                    return true;
                }
                else
                {
                    uncryptedBuffer = SafeBSTRHandle.Allocate(null, 0);
                    return false;
                }
            }
            finally
            {
                if (cryptedBufferPtr != null)
                    cryptedBuffer.ReleasePointer();

                if (pDataOut.pbData != IntPtr.Zero)
                {
                    NtDll.ZeroMemory(pDataOut.pbData, (UIntPtr)pDataOut.cbData);
                    Marshal.FreeHGlobal(pDataOut.pbData);
                }
            }
        }
    }
}
