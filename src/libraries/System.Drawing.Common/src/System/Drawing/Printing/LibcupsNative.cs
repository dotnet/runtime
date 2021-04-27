// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text;

namespace System.Drawing.Printing
{
    internal static class LibcupsNative
    {
        internal const string LibraryName = "libcups";

        static LibcupsNative()
        {
            LibraryResolver.EnsureRegistered();
        }

        internal static IntPtr LoadLibcups()
        {
            // We allow both "libcups.so" and "libcups.so.2" to be loaded.
            if (!NativeLibrary.TryLoad("libcups.so", out IntPtr lib))
            {
                NativeLibrary.TryLoad("libcups.so.2", out lib);
            }

            return lib;
        }

        [DllImport(LibraryName, ExactSpelling = true)]
        internal static extern int cupsGetDests(ref IntPtr dests);

        [DllImport(LibraryName, ExactSpelling = true)]
        internal static extern void cupsFreeDests(int num_dests, IntPtr dests);

        [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Ansi)]
#pragma warning disable CA1838 // not hot-path enough to worry about the overheads of StringBuilder marshaling
        internal static extern IntPtr cupsTempFd([Out] StringBuilder sb, int len);
#pragma warning restore CA1838

        [DllImport(LibraryName, ExactSpelling = true)]
        internal static extern IntPtr cupsGetDefault();

        [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Ansi)]
        internal static extern int cupsPrintFile(string printer, string filename, string title, int num_options, IntPtr options);

        [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Ansi)]
        internal static extern IntPtr cupsGetPPD(string printer);

        [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Ansi)]
        internal static extern IntPtr ppdOpenFile(string filename);

        [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Ansi)]
        internal static extern IntPtr ppdFindOption(IntPtr ppd_file, string keyword);

        [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Ansi)]
        internal static extern void ppdClose(IntPtr ppd);

        [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Ansi)]
        internal static extern int cupsParseOptions(string arg, int number_of_options, ref IntPtr options);

        [DllImport(LibraryName, ExactSpelling = true)]
        internal static extern void cupsFreeOptions(int number_options, IntPtr options);
    }
}
