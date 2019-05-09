// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.WindowsRuntime
{
#if BIT64
    [StructLayout(LayoutKind.Explicit, Size = 24)]
#else
    [StructLayout(LayoutKind.Explicit, Size = 20)]
#endif
    internal unsafe struct HSTRING_HEADER
    {
    }

    internal static class UnsafeNativeMethods
    {
        [DllImport("api-ms-win-core-winrt-error-l1-1-0.dll", PreserveSig = false)]
        internal static extern IRestrictedErrorInfo GetRestrictedErrorInfo();

        [DllImport("api-ms-win-core-winrt-error-l1-1-1.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RoOriginateLanguageException(int error, [MarshalAs(UnmanagedType.HString)]string message, IntPtr languageException);

        [DllImport("api-ms-win-core-winrt-error-l1-1-1.dll", PreserveSig = false)]
        internal static extern void RoReportUnhandledError(IRestrictedErrorInfo error);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int WindowsCreateString([MarshalAs(UnmanagedType.LPWStr)] string sourceString,
                                                              int length,
                                                              [Out] IntPtr* hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int WindowsCreateStringReference(char* sourceString,
                                                                       int length,
                                                                       [Out] HSTRING_HEADER* hstringHeader,
                                                                       [Out] IntPtr* hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern int WindowsDeleteString(IntPtr hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe char* WindowsGetStringRawBuffer(IntPtr hstring, [Out] uint* length);
    }
}
