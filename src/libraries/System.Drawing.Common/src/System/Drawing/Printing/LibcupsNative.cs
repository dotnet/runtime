// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text;

namespace System.Drawing.Printing
{
    internal static partial class LibcupsNative
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

        [GeneratedDllImport(LibraryName)]
        internal static partial int cupsGetDests(ref IntPtr dests);

        [GeneratedDllImport(LibraryName)]
        internal static partial void cupsFreeDests(int num_dests, IntPtr dests);

        [DllImport(LibraryName, CharSet = CharSet.Ansi)]
#pragma warning disable CA1838 // not hot-path enough to worry about the overheads of StringBuilder marshaling
        internal static extern IntPtr cupsTempFd([Out] StringBuilder sb, int len);
#pragma warning restore CA1838

        [GeneratedDllImport(LibraryName)]
        internal static partial IntPtr cupsGetDefault();

        [GeneratedDllImport(LibraryName, CharSet = CharSet.Ansi)]
        internal static partial int cupsPrintFile(string printer, string filename, string title, int num_options, IntPtr options);

        [GeneratedDllImport(LibraryName, CharSet = CharSet.Ansi)]
        internal static partial IntPtr cupsGetPPD(string printer);

        [GeneratedDllImport(LibraryName, CharSet = CharSet.Ansi)]
        internal static partial IntPtr ppdOpenFile(string filename);

        [GeneratedDllImport(LibraryName, CharSet = CharSet.Ansi)]
        internal static partial IntPtr ppdFindOption(IntPtr ppd_file, string keyword);

        [GeneratedDllImport(LibraryName, CharSet = CharSet.Ansi)]
        internal static partial void ppdClose(IntPtr ppd);

        [GeneratedDllImport(LibraryName, CharSet = CharSet.Ansi)]
        internal static partial int cupsParseOptions(string arg, int number_of_options, ref IntPtr options);

        [GeneratedDllImport(LibraryName)]
        internal static partial void cupsFreeOptions(int number_options, IntPtr options);
    }
}
