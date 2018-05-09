// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace System.Text
{
    internal static class EncodingTable
    {
        // Return a list of all EncodingInfo objects describing all of our encodings
        internal static EncodingInfo[] GetEncodings()
        {
            EncodingInfo[] arrayEncodingInfo = new EncodingInfo[s_encodingDataTableItems.Length];

            for (int i = 0; i < s_encodingDataTableItems.Length; i++)
            {
                CodePageDataItem dataItem = s_encodingDataTableItems[i];

                arrayEncodingInfo[i] = new EncodingInfo(dataItem.CodePage, dataItem.WebName,
                    SR.GetResourceString(dataItem.DisplayNameResourceKey));
            }

            return arrayEncodingInfo;
        }

        internal static int GetCodePageFromName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            ushort codePage;
            if (s_encodingDataTable.TryGetValue(name, out codePage))
            {
                return codePage;
            }

            // The encoding name is not valid.
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    SR.Argument_EncodingNotSupported, name), nameof(name));
        }

        internal static CodePageDataItem GetCodePageDataItem(int codepage)
        {
            CodePageDataItem item;

            switch (codepage)
            {
                case 1200:
                    item = s_encodingDataTableItems[0];
                    break;
                case 1201:
                    item = s_encodingDataTableItems[1];
                    break;
                case 12000:
                    item = s_encodingDataTableItems[2];
                    break;
                case 12001:
                    item = s_encodingDataTableItems[3];
                    break;
                case 20127:
                    item = s_encodingDataTableItems[4];
                    break;
                case 28591:
                    item = s_encodingDataTableItems[5];
                    break;
                case 65000:
                    item = s_encodingDataTableItems[6];
                    break;
                case 65001:
                    item = s_encodingDataTableItems[7];
                    break;
                default:
                    item = null;
                    break;
            }

            Debug.Assert(item == null || item.CodePage == codepage, "item.CodePage needs to equal the specified codepage");
            return item;
        }

        // PAL ends here.

#if DEBUG
        static EncodingTable()
        {
            Debug.Assert(
                s_encodingDataTable.Count == EncodingTableCapacity,
                string.Format(CultureInfo.InvariantCulture,
                    "EncodingTable s_encodingDataTable's initial capacity (EncodingTableCapacity) is incorrect.{0}Expected (s_encodingDataTable.Count): {1}, Actual (EncodingTableCapacity): {2}",
                    Environment.NewLine,
                    s_encodingDataTable.Count,
                    EncodingTableCapacity));
        }
#endif

        // NOTE: the following two lists were taken from ~\src\classlibnative\nls\encodingdata.cpp
        // and should be kept in sync with those lists

        private const int EncodingTableCapacity = 42;
        private readonly static Dictionary<string, ushort> s_encodingDataTable =
            new Dictionary<string, ushort>(EncodingTableCapacity, StringComparer.OrdinalIgnoreCase)
        {
            { "ANSI_X3.4-1968", 20127 },
            { "ANSI_X3.4-1986", 20127 },
            { "ascii", 20127 },
            { "cp367", 20127 },
            { "cp819", 28591 },
            { "csASCII", 20127 },
            { "csISOLatin1", 28591 },
            { "csUnicode11UTF7", 65000 },
            { "IBM367", 20127 },
            { "ibm819", 28591 },
            { "ISO-10646-UCS-2", 1200 },
            { "iso-8859-1", 28591 },
            { "iso-ir-100", 28591 },
            { "iso-ir-6", 20127 },
            { "ISO646-US", 20127 },
            { "iso8859-1", 28591 },
            { "ISO_646.irv:1991", 20127 },
            { "iso_8859-1", 28591 },
            { "iso_8859-1:1987", 28591 },
            { "l1", 28591 },
            { "latin1", 28591 },
            { "ucs-2", 1200 },
            { "unicode", 1200},
            { "unicode-1-1-utf-7", 65000 },
            { "unicode-1-1-utf-8", 65001 },
            { "unicode-2-0-utf-7", 65000 },
            { "unicode-2-0-utf-8", 65001 },
            // People get confused about the FFFE here.  We can't change this because it'd break existing apps.
            // This has been this way for a long time, including in Mlang.
            // Big Endian, BOM seems backwards, think of the BOM in little endian order.
            { "unicodeFFFE", 1201},
            { "us", 20127 },
            { "us-ascii", 20127 },
            { "utf-16", 1200 },
            { "UTF-16BE", 1201},
            { "UTF-16LE", 1200},
            { "utf-32", 12000 },
            { "UTF-32BE", 12001 },
            { "UTF-32LE", 12000 },
            { "utf-7", 65000 },
            { "utf-8", 65001 },
            { "x-unicode-1-1-utf-7", 65000 },
            { "x-unicode-1-1-utf-8", 65001 },
            { "x-unicode-2-0-utf-7", 65000 },
            { "x-unicode-2-0-utf-8", 65001 },
        };

        // redeclaring these constants here for readability below
        private const uint MIMECONTF_MAILNEWS = Encoding.MIMECONTF_MAILNEWS;
        private const uint MIMECONTF_BROWSER = Encoding.MIMECONTF_BROWSER;
        private const uint MIMECONTF_SAVABLE_MAILNEWS = Encoding.MIMECONTF_SAVABLE_MAILNEWS;
        private const uint MIMECONTF_SAVABLE_BROWSER = Encoding.MIMECONTF_SAVABLE_BROWSER;

        // keep this array sorted by code page, so the order is consistent for GetEncodings()
        // Remember to update GetCodePageDataItem() if this list is updated
        private readonly static CodePageDataItem[] s_encodingDataTableItems = new[]
        {
            new CodePageDataItem(1200, 1200, "utf-16", MIMECONTF_SAVABLE_BROWSER), // "Unicode"
            new CodePageDataItem(1201, 1200, "utf-16BE", 0), // Big Endian, old FFFE BOM seems backwards, think of the BOM in little endian order.
            new CodePageDataItem(12000, 1200, "utf-32", 0), // "Unicode (UTF-32)"
            new CodePageDataItem(12001, 1200, "utf-32BE", 0), // "Unicode (UTF-32 Big Endian)"
            new CodePageDataItem(20127, 1252, "us-ascii", MIMECONTF_MAILNEWS | MIMECONTF_SAVABLE_MAILNEWS), // "US-ASCII"
            new CodePageDataItem(28591, 1252, "iso-8859-1", MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Western European (ISO)"
            new CodePageDataItem(65000, 1200, "utf-7", MIMECONTF_MAILNEWS | MIMECONTF_SAVABLE_MAILNEWS), // "Unicode (UTF-7)"
            new CodePageDataItem(65001, 1200, "utf-8", MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Unicode (UTF-8)"
        };
    }
}
