// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Text
{
    //
    // Data table for encoding classes.  Used by System.Text.Encoding.
    // This class contains two hashtables to allow System.Text.Encoding
    // to retrieve the data item either by codepage value or by webName.
    //
    internal static partial class EncodingTable
    {
        private static readonly Hashtable s_nameToCodePage = Hashtable.Synchronized(new Hashtable(StringComparer.OrdinalIgnoreCase));
        private static CodePageDataItem?[]? s_codePageToCodePageData;

        /*=================================GetCodePageFromName==========================
        **Action: Given a encoding name, return the correct code page number for this encoding.
        **Returns: The code page for the encoding.
        **Arguments:
        **  name    the name of the encoding
        **Exceptions:
        **  ArgumentNullException if name is null.
        **  internalGetCodePageFromName will throw ArgumentException if name is not a valid encoding name.
        ============================================================================*/

        internal static int GetCodePageFromName(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            object? codePageObj = s_nameToCodePage[name];

            if (codePageObj != null)
            {
                return (int)codePageObj;
            }

            int codePage = InternalGetCodePageFromName(name);

            s_nameToCodePage[name] = codePage;

            return codePage;
        }

        // Find the data item by binary searching the table.
        private static int InternalGetCodePageFromName(string name)
        {
            int left = 0;
            int right = EncodingNameIndices.Length - 2;
            int index;
            int result;

            Debug.Assert(EncodingNameIndices.Length == CodePagesByName.Length + 1);
            Debug.Assert(EncodingNameIndices[^1] == EncodingNames.Length);

            ReadOnlySpan<char> invariantName = name.ToLowerInvariant().AsSpan();

            // Binary search the array until we have only a couple of elements left and then
            // just walk those elements.
            while ((right - left) > 3)
            {
                index = ((right - left) / 2) + left;

                Debug.Assert(index < EncodingNameIndices.Length - 1);
                result = string.CompareOrdinal(invariantName, EncodingNames.AsSpan(EncodingNameIndices[index], EncodingNameIndices[index + 1] - EncodingNameIndices[index]));

                if (result == 0)
                {
                    // We found the item, return the associated codePage.
                    return CodePagesByName[index];
                }
                else if (result < 0)
                {
                    // The name that we're looking for is less than our current index.
                    right = index;
                }
                else
                {
                    // The name that we're looking for is greater than our current index
                    left = index;
                }
            }

            // Walk the remaining elements (it'll be 3 or fewer).
            for (; left <= right; left++)
            {
                Debug.Assert(left < EncodingNameIndices.Length - 1);
                if (invariantName.SequenceEqual(EncodingNames.AsSpan(EncodingNameIndices[left], EncodingNameIndices[left + 1] - EncodingNameIndices[left])))
                {
                    return CodePagesByName[left];
                }
            }

            // The encoding name is not valid.
            throw new ArgumentException(
                SR.Format(SR.Argument_EncodingNotSupported, name),
                nameof(name));
        }

        // Return a list of all EncodingInfo objects describing all of our encodings
        internal static EncodingInfo[] GetEncodings()
        {
            // If UTF-7 encoding is not enabled, we adjust the return array length by -1
            // to account for the skipped EncodingInfo element.

            ReadOnlySpan<ushort> mappedCodePages = MappedCodePages;
            EncodingInfo[] arrayEncodingInfo = new EncodingInfo[(LocalAppContextSwitches.EnableUnsafeUTF7Encoding) ? mappedCodePages.Length : (mappedCodePages.Length - 1)];
            string webNames = WebNames;
            ReadOnlySpan<int> webNameIndices = WebNameIndices;
            int arrayEncodingInfoIdx = 0;

            for (int i = 0; i < mappedCodePages.Length; i++)
            {
                int codePage = mappedCodePages[i];
                if (codePage == Encoding.CodePageUTF7 && !LocalAppContextSwitches.EnableUnsafeUTF7Encoding)
                {
                    continue; // skip this entry; UTF-7 is disabled
                }

                arrayEncodingInfo[arrayEncodingInfoIdx++] = new EncodingInfo(
                    codePage,
                    webNames[webNameIndices[i]..webNameIndices[i + 1]],
                    GetDisplayName(codePage)
                    );
            }

            Debug.Assert(arrayEncodingInfoIdx == arrayEncodingInfo.Length);
            return arrayEncodingInfo;
        }

        internal static EncodingInfo[] GetEncodings(Dictionary<int, EncodingInfo> encodingInfoList)
        {
            Debug.Assert(encodingInfoList != null);
            ReadOnlySpan<ushort> mappedCodePages = MappedCodePages;
            string webNames = WebNames;
            ReadOnlySpan<int> webNameIndices = WebNameIndices;

            for (int i = 0; i < mappedCodePages.Length; i++)
            {
                int codePage = mappedCodePages[i];
                if (!encodingInfoList.ContainsKey(codePage))
                {
                    // If UTF-7 encoding is not enabled, don't add it to the provided dictionary instance.
                    // Exception: If somebody already registered a custom UTF-7 provider, the dictionary
                    // will already contain an entry for the UTF-7 code page key, and we'll let it go through.

                    if (codePage != Encoding.CodePageUTF7 || LocalAppContextSwitches.EnableUnsafeUTF7Encoding)
                    {
                        encodingInfoList[codePage] = new EncodingInfo(codePage, webNames[webNameIndices[i]..webNameIndices[i + 1]],
                                                                                GetDisplayName(codePage));
                    }
                }
            }

            // Just in case a provider registered UTF-7 without the application's consent

            if (!LocalAppContextSwitches.EnableUnsafeUTF7Encoding)
            {
                encodingInfoList.Remove(Encoding.CodePageUTF7); // won't throw if doesn't exist
            }

            var result = new EncodingInfo[encodingInfoList.Count];
            int j = 0;
            foreach (KeyValuePair<int, EncodingInfo> pair in encodingInfoList)
            {
                result[j++] = pair.Value;
            }

            return result;
        }

        internal static CodePageDataItem? GetCodePageDataItem(int codePage)
        {
            if (s_codePageToCodePageData == null)
            {
                Interlocked.CompareExchange<CodePageDataItem?[]?>(ref s_codePageToCodePageData, new CodePageDataItem[MappedCodePages.Length], null);
            }

            // Keep in sync with MappedCodePages
            int index;
            switch (codePage)
            {
                case 1200: // utf-16
                    index = 0;
                    break;
                case 1201: // utf-16be
                    index = 1;
                    break;
                case 12000: // utf-32
                    index = 2;
                    break;
                case 12001: // utf-32be
                    index = 3;
                    break;
                case 20127: // us-ascii
                    index = 4;
                    break;
                case 28591: // iso-8859-1
                    index = 5;
                    break;
                case 65000: // utf-7
                    index = 6;
                    break;
                case 65001: // utf-8
                    index = 7;
                    break;
                default:
                    return null;
            }

            CodePageDataItem? data = s_codePageToCodePageData[index];
            if (data == null)
            {
                Interlocked.CompareExchange<CodePageDataItem?>(ref s_codePageToCodePageData[index], InternalGetCodePageDataItem(codePage, index), null);
                data = s_codePageToCodePageData[index];
            }

            return data;
        }

        private static CodePageDataItem InternalGetCodePageDataItem(int codePage, int index)
        {
            int uiFamilyCodePage = UiFamilyCodePages[index];
            string webName = WebNames[WebNameIndices[index]..WebNameIndices[index + 1]];
            // All supported code pages have identical header names, and body names.
            string headerName = webName;
            string bodyName = webName;
            string displayName = GetDisplayName(codePage);
            uint flags = Flags[index];

            return new CodePageDataItem(uiFamilyCodePage, webName, headerName, bodyName, displayName, flags);
        }

        private static string GetDisplayName(int codePage)
        {
            switch (codePage)
            {
                case 1200: return SR.Globalization_cp_1200;
                case 1201: return SR.Globalization_cp_1201;
                case 12000: return SR.Globalization_cp_12000;
                case 12001: return SR.Globalization_cp_12001;
                case 20127: return SR.Globalization_cp_20127;
                case 28591: return SR.Globalization_cp_28591;
                case 65000: return SR.Globalization_cp_65000;
                case 65001: return SR.Globalization_cp_65001;
            };

            Debug.Fail("Unexpected code page");
            return "";
        }
    }
}
