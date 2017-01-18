// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Data
{
    internal static class LocaleMapper
    {
        private const string ApiWinKernel32 = "kernel32.dll";

        private const int LOCALE_NAME_MAX_LENGTH = 85;
        private const int ERROR_INVALID_PARAMETER = 0x57;
        private const uint LOCALE_IDEFAULTANSICODEPAGE = 0x00001004;
        private const uint LOCALE_RETURN_NUMBER = 0x20000000;

        [DllImport(ApiWinKernel32, SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int GetLocaleInfoEx(string lpLocaleName, uint LCType, [Out] out uint lpLCData, int cchData);

        [DllImport(ApiWinKernel32, SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern unsafe int LCIDToLocaleName(uint Locale, char* lpName, int cchName, int dwFlags);

        [DllImport(ApiWinKernel32, SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint LocaleNameToLCID(string lpName, uint dwFlags);


        public static unsafe string LcidToLocaleNameInternal(int lcid)
        {
            char* localeName = stackalloc char[LOCALE_NAME_MAX_LENGTH];
            int length = LCIDToLocaleName(unchecked((uint)lcid), localeName, LOCALE_NAME_MAX_LENGTH, 0);

            if (length != 0)
            {
                return new string(localeName, 0, length - 1);
            }
            else
            {
                // LCIDToLocaleName should not return any other errors if we have sufficient buffer space
                int win32Error = Marshal.GetLastWin32Error();
                Debug.Assert(win32Error == ERROR_INVALID_PARAMETER, $"Unknown error returned by {nameof(LCIDToLocaleName)}: {win32Error}. Lcid: {lcid}");

                throw new CultureNotFoundException(nameof(lcid), lcid.ToString(), message: null);
            }
        }

        public static int LocaleNameToAnsiCodePage(string localeName)
        {
            uint ansiCodePage;
            if (GetLocaleInfoEx(localeName, LOCALE_RETURN_NUMBER | LOCALE_IDEFAULTANSICODEPAGE, out ansiCodePage, sizeof(uint)) != 0)
            {
                return unchecked((int)ansiCodePage);
            }
            else
            {
                // GetLocaleInfoEx should not return any other errors if we have sufficient buffer space
                int win32Error = Marshal.GetLastWin32Error();
                Debug.Assert(win32Error == ERROR_INVALID_PARAMETER, $"Unknown error returned by {nameof(GetLocaleInfoEx)}: {win32Error}. LocaleName: {localeName}");

                throw new CultureNotFoundException(nameof(localeName), localeName, message: null);
            }
        }

        public static int GetLcidForLocaleName(string localeName)
        {
            Debug.Assert(localeName != null, "Locale name should never be null");

            uint lcid = LocaleNameToLCID(localeName, 0);
            if (lcid != 0)
            {
                return unchecked((int)lcid);
            }
            else
            {
                // LocaleNameToLCID should not return any other errors
                int win32Error = Marshal.GetLastWin32Error();
                Debug.Assert(win32Error == ERROR_INVALID_PARAMETER, $"Unknown error returned by {nameof(LocaleNameToLCID)}: {win32Error}. LocaleName: {localeName}");

                throw new CultureNotFoundException(nameof(localeName), localeName, message: null);
            }
        }
    }
}
