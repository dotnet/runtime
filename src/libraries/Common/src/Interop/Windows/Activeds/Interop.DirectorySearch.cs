// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Activeds
    {
        // IDirecorySearch return codes
        internal const int S_ADS_NOMORE_ROWS = 0x00005012;
        internal const int INVALID_FILTER = unchecked((int)0x8007203E);
        internal const int SIZE_LIMIT_EXCEEDED = unchecked((int)0x80072023);

        [ComImport, Guid("109BA8EC-92F0-11D0-A790-00C04FD8D5A8"), System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        public interface IDirectorySearch
        {
            void SetSearchPreference([In] IntPtr /*ads_searchpref_info * */pSearchPrefs, int dwNumPrefs);

            void ExecuteSearch(
                [In, MarshalAs(UnmanagedType.LPWStr)] string pszSearchFilter,
                [In, MarshalAs(UnmanagedType.LPArray)] string[] pAttributeNames,
                [In] int dwNumberAttributes,
                [Out] out IntPtr hSearchResult);

            void AbandonSearch([In] IntPtr hSearchResult);

            [return: MarshalAs(UnmanagedType.U4)]
            [PreserveSig]
            int GetFirstRow([In] IntPtr hSearchResult);

            [return: MarshalAs(UnmanagedType.U4)]
            [PreserveSig]
            int GetNextRow([In] IntPtr hSearchResult);

            [return: MarshalAs(UnmanagedType.U4)]
            [PreserveSig]
            int GetPreviousRow([In] IntPtr hSearchResult);

            [return: MarshalAs(UnmanagedType.U4)]
            [PreserveSig]
            int GetNextColumnName(
                [In] IntPtr hSearchResult,
                [Out] IntPtr ppszColumnName);

            void GetColumn(
                [In] IntPtr hSearchResult,
                [In] IntPtr /* char * */ szColumnName,
                [In] IntPtr pSearchColumn);

            void FreeColumn([In] IntPtr pSearchColumn);

            void CloseSearchHandle([In] IntPtr hSearchResult);
        }
    }
}
