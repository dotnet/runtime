// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;

namespace System.Drawing.Printing
{
    internal static class LibcupsNative
    {
        private const string LibraryName = "libcups";
        private static IntPtr s_libcupsHandle = LoadLibcups();

        private static IntPtr LoadLibcups()
        {
            // We allow both "libcups.so" and "libcups.so.2" to be loaded.
            IntPtr lib = Interop.Libdl.dlopen("libcups.so", Interop.Libdl.RTLD_NOW);
            if (lib == IntPtr.Zero)
            {
                lib = Interop.Libdl.dlopen("libcups.so.2", Interop.Libdl.RTLD_NOW);
            }

            return lib;
        }

        private delegate int cupsGetDests_delegate(ref IntPtr dests);
        private static FunctionWrapper<cupsGetDests_delegate> cupsGetDests_ptr
            = FunctionWrapper.Load<cupsGetDests_delegate>(s_libcupsHandle, "cupsGetDests", LibraryName);
        internal static int cupsGetDests(ref IntPtr dests) => cupsGetDests_ptr.Delegate(ref dests);

        private delegate int cupsFreeDests_delegate(int num_dests, IntPtr dests);
        private static FunctionWrapper<cupsFreeDests_delegate> cupsFreeDests_ptr
            = FunctionWrapper.Load<cupsFreeDests_delegate>(s_libcupsHandle, "cupsFreeDests", LibraryName);
        internal static void cupsFreeDests(int num_dests, IntPtr dests) => cupsFreeDests_ptr.Delegate(num_dests, dests);

        private delegate IntPtr cupsTempFd_delegate([MarshalAs(UnmanagedType.LPStr)]StringBuilder sb, int len);
        private static FunctionWrapper<cupsTempFd_delegate> cupsTempFd_ptr
            = FunctionWrapper.Load<cupsTempFd_delegate>(s_libcupsHandle, "cupsTempFd", LibraryName);
        internal static IntPtr cupsTempFd(StringBuilder sb, int len) => cupsTempFd_ptr.Delegate(sb, len);

        private delegate IntPtr cupsGetDefault_delegate();
        private static FunctionWrapper<cupsGetDefault_delegate> cupsGetDefault_ptr
            = FunctionWrapper.Load<cupsGetDefault_delegate>(s_libcupsHandle, "cupsGetDefault", LibraryName);
        internal static IntPtr cupsGetDefault() => cupsGetDefault_ptr.Delegate();

        private delegate int cupsPrintFile_delegate(
            [MarshalAs(UnmanagedType.LPStr)]string printer,
            [MarshalAs(UnmanagedType.LPStr)]string filename,
            [MarshalAs(UnmanagedType.LPStr)]string title,
            int num_options,
            IntPtr options);
        private static FunctionWrapper<cupsPrintFile_delegate> cupsPrintFile_ptr
            = FunctionWrapper.Load<cupsPrintFile_delegate>(s_libcupsHandle, "cupsPrintFile", LibraryName);
        internal static int cupsPrintFile(string printer, string filename, string title, int num_options, IntPtr options)
            => cupsPrintFile_ptr.Delegate(printer, filename, title, num_options, options);

        private delegate IntPtr cupsGetPPD_delegate([MarshalAs(UnmanagedType.LPStr)]string printer);
        private static FunctionWrapper<cupsGetPPD_delegate> cupsGetPPD_ptr
            = FunctionWrapper.Load<cupsGetPPD_delegate>(s_libcupsHandle, "cupsGetPPD", LibraryName);
        internal static IntPtr cupsGetPPD(string printer) => cupsGetPPD_ptr.Delegate(printer);

        private delegate IntPtr ppdOpenFile_delegate([MarshalAs(UnmanagedType.LPStr)]string filename);
        private static FunctionWrapper<ppdOpenFile_delegate> ppdOpenFile_ptr
            = FunctionWrapper.Load<ppdOpenFile_delegate>(s_libcupsHandle, "ppdOpenFile", LibraryName);
        internal static IntPtr ppdOpenFile(string filename) => ppdOpenFile_ptr.Delegate(filename);

        private delegate IntPtr ppdFindOption_delegate(IntPtr ppd_file, [MarshalAs(UnmanagedType.LPStr)]string keyword);
        private static FunctionWrapper<ppdFindOption_delegate> ppdFindOption_ptr
            = FunctionWrapper.Load<ppdFindOption_delegate>(s_libcupsHandle, "ppdFindOption", LibraryName);
        internal static IntPtr ppdFindOption(IntPtr ppd_file, string keyword) => ppdFindOption_ptr.Delegate(ppd_file, keyword);

        private delegate void ppdClose_delegate(IntPtr ppd);
        private static FunctionWrapper<ppdClose_delegate> ppdClose_ptr
            = FunctionWrapper.Load<ppdClose_delegate>(s_libcupsHandle, "ppdClose", LibraryName);
        internal static void ppdClose(IntPtr ppd) => ppdClose_ptr.Delegate(ppd);

        private delegate int cupsParseOptions_delegate([MarshalAs(UnmanagedType.LPStr)]string arg, int number_of_options, ref IntPtr options);
        private static FunctionWrapper<cupsParseOptions_delegate> cupsParseOptions_ptr
            = FunctionWrapper.Load<cupsParseOptions_delegate>(s_libcupsHandle, "cupsParseOptions", LibraryName);
        internal static int cupsParseOptions(string arg, int number_of_options, ref IntPtr options)
            => cupsParseOptions_ptr.Delegate(arg, number_of_options, ref options);

        private delegate void cupsFreeOptions_delegate(int number_options, IntPtr options);
        private static FunctionWrapper<cupsFreeOptions_delegate> cupsFreeOptions_ptr
            = FunctionWrapper.Load<cupsFreeOptions_delegate>(s_libcupsHandle, "cupsFreeOptions", LibraryName);
        internal static void cupsFreeOptions(int number_options, IntPtr options) => cupsFreeOptions_ptr.Delegate(number_options, options);
    }
}
