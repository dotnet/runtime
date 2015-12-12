// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Data
{
    internal class LocaleWindowsApiHelper : ILocaleApiHelper
    {
        // Maps between LCIDs and LocaleName+AnsiCodePages+Encodings
        private static readonly ConcurrentDictionary<int, Tuple<string, int, Encoding>> s_cachedEncodings = new ConcurrentDictionary<int, Tuple<string, int, Encoding>>();

        private const string ApiWinCoreLocalization = "api-ms-win-core-localization-l1-2-0.dll";
        private const string ApiWinCoreLocalizationObsolete = "api-ms-win-core-localization-obsolete-l1-2-0.dll";

        private const int LOCALE_NAME_MAX_LENGTH = 85;
        private const int ERROR_INVALID_PARAMETER = 0x57;
        private const uint LOCALE_IDEFAULTANSICODEPAGE = 0x00001004;
        private const uint LOCALE_RETURN_NUMBER = 0x20000000;

        [DllImport(ApiWinCoreLocalization, SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int GetLocaleInfoEx(string lpLocaleName, uint LCType, [Out] out uint lpLCData, int cchData);

        [DllImport(ApiWinCoreLocalizationObsolete, SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int LCIDToLocaleName(uint Locale, StringBuilder lpName, int cchName, int dwFlags);

        [DllImport(ApiWinCoreLocalization, SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint LocaleNameToLCID(string lpName, uint dwFlags);


        public string LcidToLocaleNameInternal(int lcid)
        {
            StringBuilder localName = new StringBuilder(LOCALE_NAME_MAX_LENGTH);

            if (LCIDToLocaleName(unchecked((uint)lcid), localName, localName.Capacity, 0) != 0)
            {
                return localName.ToString();
            }
            else
            {
                // LCIDToLocaleName should not return any other errors if we have sufficient buffer space
                int win32Error = Marshal.GetLastWin32Error();
                Debug.Assert(win32Error == ERROR_INVALID_PARAMETER, string.Format("Unknown error returned by LCIDToLocaleName: {0}. Lcid: {1}", win32Error, lcid));

                throw new CultureNotFoundException("lcid", lcid.ToString(), message: null);
            }
        }

        public int LocaleNameToAnsiCodePage(string localeName)
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
                Debug.Assert(win32Error == ERROR_INVALID_PARAMETER, string.Format("Unknown error returned by GetLocaleInfoEx: {0}. LocaleName: {1}", win32Error, localeName));

                throw new CultureNotFoundException("localeName", localeName, message: null);
            }
        }

        public int GetLcidForLocaleName(string localeName)
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
                Debug.Assert(win32Error == ERROR_INVALID_PARAMETER, string.Format("Unknown error returned by LocaleNameToLCID: {0}. LocaleName: {1}", win32Error, localeName));

                throw new CultureNotFoundException("localeName", localeName, message: null);
            }
        }
        
    }
}