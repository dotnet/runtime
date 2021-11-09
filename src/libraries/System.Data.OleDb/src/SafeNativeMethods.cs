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

        [GeneratedDllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static partial int ReleaseSemaphore(IntPtr handle, int releaseCount, IntPtr previousCount);

        [GeneratedDllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static partial int WaitForMultipleObjectsEx(uint nCount, IntPtr lpHandles, bool bWaitAll, uint dwMilliseconds, bool bAlertable);

        [GeneratedDllImport(Interop.Libraries.Kernel32/*, SetLastError=true*/)]
        internal static partial int WaitForSingleObjectEx(IntPtr lpHandles, uint dwMilliseconds, bool bAlertable);

        internal sealed class Wrapper
        {
            private Wrapper() { }

            // SxS: clearing error information is considered safe
            internal static void ClearErrorInfo()
            {
                Interop.OleAut32.SetErrorInfo(0, ADP.PtrZero);
            }
        }
    }
}
