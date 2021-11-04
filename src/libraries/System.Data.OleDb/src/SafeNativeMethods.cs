// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Threading;

namespace System.Data.Common
{
    [SuppressUnmanagedCodeSecurity]
    internal static partial class SafeNativeMethods
    {
        [GeneratedDllImport(Interop.Libraries.Kernel32, CharSet = CharSet.Unicode, PreserveSig = true)]
        internal static partial int GetUserDefaultLCID();

        internal static void ZeroMemory(IntPtr ptr, int length)
        {
            var zeroes = new byte[length];
            Marshal.Copy(zeroes, 0, ptr, length);
        }

        internal static unsafe IntPtr InterlockedExchangePointer(
                IntPtr lpAddress,
                IntPtr lpValue)
        {
            IntPtr previousPtr;
            IntPtr actualPtr = *(IntPtr*)lpAddress.ToPointer();

            do
            {
                previousPtr = actualPtr;
                actualPtr = Interlocked.CompareExchange(ref *(IntPtr*)lpAddress.ToPointer(), lpValue, previousPtr);
            }
            while (actualPtr != previousPtr);

            return actualPtr;
        }

        [GeneratedDllImport(Interop.Libraries.Kernel32, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static partial int GetCurrentProcessId();

        [GeneratedDllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static partial IntPtr LocalAlloc(int flags, IntPtr countOfBytes);

        [GeneratedDllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static partial IntPtr LocalFree(IntPtr handle);

        [GeneratedDllImport(Interop.Libraries.OleAut32, CharSet = CharSet.Unicode)]
        internal static partial IntPtr SysAllocStringLen(string src, int len);  // BSTR

        [GeneratedDllImport(Interop.Libraries.OleAut32)]
        internal static partial void SysFreeString(IntPtr bstr);

        // only using this to clear existing error info with null
        [GeneratedDllImport(Interop.Libraries.OleAut32, CharSet = CharSet.Unicode, PreserveSig = false)]
        // TLS values are preserved between threads, need to check that we use this API to clear the error state only.
        private static partial void SetErrorInfo(int dwReserved, IntPtr pIErrorInfo);

        [GeneratedDllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static partial int ReleaseSemaphore(IntPtr handle, int releaseCount, IntPtr previousCount);

        [GeneratedDllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static partial int WaitForMultipleObjectsEx(uint nCount, IntPtr lpHandles, bool bWaitAll, uint dwMilliseconds, bool bAlertable);

        [GeneratedDllImport(Interop.Libraries.Kernel32/*, SetLastError=true*/)]
        internal static partial int WaitForSingleObjectEx(IntPtr lpHandles, uint dwMilliseconds, bool bAlertable);

        [GeneratedDllImport(Interop.Libraries.Ole32, PreserveSig = false)]
        internal static partial void PropVariantClear(IntPtr pObject);

        [GeneratedDllImport(Interop.Libraries.OleAut32, PreserveSig = false)]
        internal static partial void VariantClear(IntPtr pObject);

        internal sealed class Wrapper
        {
            private Wrapper() { }

            // SxS: clearing error information is considered safe
            internal static void ClearErrorInfo()
            {
                SafeNativeMethods.SetErrorInfo(0, ADP.PtrZero);
            }
        }
    }
}
