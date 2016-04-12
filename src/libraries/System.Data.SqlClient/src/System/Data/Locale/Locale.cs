// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Data.Common;
using System.Globalization;
using System.Text;

namespace System.Data
{
    internal static class Locale
    {
        // Maps between LCIDs and LocaleName+AnsiCodePages+Encodings
        private static readonly ConcurrentDictionary<int, Tuple<string, int, Encoding>> s_cachedEncodings = new ConcurrentDictionary<int, Tuple<string, int, Encoding>>();

        static Locale()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);            
        }

        private static Tuple<string, int, Encoding> GetDetailsInternal(int lcid)
        {
            string localeName = LocaleMapper.LcidToLocaleNameInternal(lcid);
            int ansiCodePage = LocaleMapper.LocaleNameToAnsiCodePage(localeName);
            return Tuple.Create(localeName, ansiCodePage, Encoding.GetEncoding(ansiCodePage));
        }

        private static Tuple<string, int, Encoding> GetDetailsForLcid(int lcid)
        {
            if (lcid < 0)
            {
                throw ADP.ArgumentOutOfRange(nameof(lcid));
            }
            return s_cachedEncodings.GetOrAdd(lcid, id => GetDetailsInternal(id));
        }

        internal static Encoding GetEncodingForLcid(int lcid)
        {
            return GetDetailsForLcid(lcid).Item3;
        }

        internal static int GetCodePageForLcid(int lcid)
        {
            return GetDetailsForLcid(lcid).Item2;
        }

        internal static string GetLocaleNameForLcid(int lcid)
        {
            return GetDetailsForLcid(lcid).Item1;
        }

        internal static int GetCurrentCultureLcid()
        {
            return LocaleMapper.GetLcidForLocaleName(CultureInfo.CurrentCulture.ToString());
        }
    }
}
