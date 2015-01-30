// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
#if WIN64
    [StructLayout(LayoutKind.Explicit, Size = 24)]
#else
    [StructLayout(LayoutKind.Explicit, Size = 20)]
#endif
    internal unsafe struct HSTRING_HEADER
    {
    }

    internal static class UnsafeNativeMethods
    {
        [DllImport("api-ms-win-core-winrt-error-l1-1-1.dll", PreserveSig = false)]
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        internal static extern IRestrictedErrorInfo GetRestrictedErrorInfo();

        [DllImport("api-ms-win-core-winrt-error-l1-1-1.dll")]
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RoOriginateLanguageException(int error, [MarshalAs(UnmanagedType.HString)]string message, IntPtr languageException);

        [DllImport("api-ms-win-core-winrt-error-l1-1-1.dll", PreserveSig = false)]
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        internal static extern void RoReportUnhandledError(IRestrictedErrorInfo error);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        internal static unsafe extern int WindowsCreateString([MarshalAs(UnmanagedType.LPWStr)] string sourceString,
                                                              int length,
                                                              [Out] IntPtr *hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        internal static unsafe extern int WindowsCreateStringReference(char *sourceString,
                                                                       int length,
                                                                       [Out] HSTRING_HEADER *hstringHeader,
                                                                       [Out] IntPtr *hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        internal static extern int WindowsDeleteString(IntPtr hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        internal static unsafe extern char* WindowsGetStringRawBuffer(IntPtr hstring, [Out] uint *length);
    }
}
