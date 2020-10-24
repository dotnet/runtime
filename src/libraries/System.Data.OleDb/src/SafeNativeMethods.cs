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
    internal static class SafeNativeMethods
    {
        [DllImport(Interop.Libraries.Ole32, SetLastError = false)]
        internal static extern IntPtr CoTaskMemAlloc(IntPtr cb);

        [DllImport(Interop.Libraries.Ole32, SetLastError = false)]
        internal static extern void CoTaskMemFree(IntPtr handle);

        [DllImport(Interop.Libraries.Kernel32, CharSet = CharSet.Unicode, PreserveSig = true)]
        internal static extern int GetUserDefaultLCID();

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

        [DllImport(Interop.Libraries.Kernel32, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern int GetCurrentProcessId();

        [DllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static extern IntPtr LocalAlloc(int flags, IntPtr countOfBytes);

        [DllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static extern IntPtr LocalFree(IntPtr handle);

        [DllImport(Interop.Libraries.OleAut32, CharSet = CharSet.Unicode)]
        internal static extern IntPtr SysAllocStringLen(string src, int len);  // BSTR

        [DllImport(Interop.Libraries.OleAut32)]
        internal static extern void SysFreeString(IntPtr bstr);

        // only using this to clear existing error info with null
        [DllImport(Interop.Libraries.OleAut32, CharSet = CharSet.Unicode, PreserveSig = false)]
        // TLS values are preserved between threads, need to check that we use this API to clear the error state only.
        private static extern void SetErrorInfo(int dwReserved, IntPtr pIErrorInfo);

        [DllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static extern int ReleaseSemaphore(IntPtr handle, int releaseCount, IntPtr previousCount);

        [DllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static extern int WaitForMultipleObjectsEx(uint nCount, IntPtr lpHandles, bool bWaitAll, uint dwMilliseconds, bool bAlertable);

        [DllImport(Interop.Libraries.Kernel32/*, SetLastError=true*/)]
        internal static extern int WaitForSingleObjectEx(IntPtr lpHandles, uint dwMilliseconds, bool bAlertable);

        [DllImport(Interop.Libraries.Ole32, PreserveSig = false)]
        internal static extern void PropVariantClear(IntPtr pObject);

        [DllImport(Interop.Libraries.OleAut32, PreserveSig = false)]
        internal static extern void VariantClear(IntPtr pObject);

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
