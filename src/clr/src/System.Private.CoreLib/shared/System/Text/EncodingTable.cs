// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections;
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
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            object codePageObj;
            codePageObj = s_nameToCodePage[name];

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
            int right = s_encodingNameIndices.Length - 2;
            int index;
            int result;

            Debug.Assert(s_encodingNameIndices.Length == s_codePagesByName.Length + 1);
            Debug.Assert(s_encodingNameIndices[s_encodingNameIndices.Length - 1] == s_encodingNames.Length);

            ReadOnlySpan<char> invariantName = name.ToLowerInvariant().AsSpan();

            //Binary search the array until we have only a couple of elements left and then
            //just walk those elements.
            while ((right - left) > 3)
            {
                index = ((right - left) / 2) + left;

                Debug.Assert(index < s_encodingNameIndices.Length - 1);
                result = string.CompareOrdinal(invariantName, s_encodingNames.AsSpan(s_encodingNameIndices[index], s_encodingNameIndices[index + 1] - s_encodingNameIndices[index]));

                if (result == 0)
                {
                    //We found the item, return the associated codePage.
                    return s_codePagesByName[index];
                }
                else if (result < 0)
                {
                    //The name that we're looking for is less than our current index.
                    right = index;
                }
                else
                {
                    //The name that we're looking for is greater than our current index
                    left = index;
                }
            }

            //Walk the remaining elements (it'll be 3 or fewer).
            for (; left <= right; left++)
            {
                Debug.Assert(left < s_encodingNameIndices.Length - 1);
                if (string.CompareOrdinal(invariantName, s_encodingNames.AsSpan(s_encodingNameIndices[left], s_encodingNameIndices[left + 1] - s_encodingNameIndices[left])) == 0)
                {
                    return s_codePagesByName[left];
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
            EncodingInfo[] arrayEncodingInfo = new EncodingInfo[s_mappedCodePages.Length];

            for (int i = 0; i < s_mappedCodePages.Length; i++)
            {
                arrayEncodingInfo[i] = new EncodingInfo(
                    s_mappedCodePages[i],
                    s_webNames.Substring(s_webNameIndices[i], s_webNameIndices[i + 1] - s_webNameIndices[i]),
                    GetDisplayName(s_mappedCodePages[i], i)
                    );
            }

            return arrayEncodingInfo;
        }

        internal static CodePageDataItem? GetCodePageDataItem(int codePage)
        {
            if (s_codePageToCodePageData == null)
            {
                Interlocked.CompareExchange<CodePageDataItem?[]?>(ref s_codePageToCodePageData, new CodePageDataItem[s_mappedCodePages.Length], null);
            }

            // Keep in sync with s_mappedCodePages
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

            CodePageDataItem? data = s_codePageToCodePageData![index]; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
            if (data == null)
            {
                Interlocked.CompareExchange<CodePageDataItem?>(ref s_codePageToCodePageData[index], InternalGetCodePageDataItem(codePage, index), null);
                data = s_codePageToCodePageData[index];
            }

            return data;
        }

        private static CodePageDataItem InternalGetCodePageDataItem(int codePage, int index)
        {
            int uiFamilyCodePage = s_uiFamilyCodePages[index];
            string webName = s_webNames.Substring(s_webNameIndices[index], s_webNameIndices[index + 1] - s_webNameIndices[index]);
            // All supported code pages have identical header names, and body names.
            string headerName = webName;
            string bodyName = webName;
            string displayName = GetDisplayName(codePage, index);
            uint flags = s_flags[index];

            return new CodePageDataItem(codePage, uiFamilyCodePage, webName, headerName, bodyName, displayName, flags);
        }

        private static string GetDisplayName(int codePage, int englishNameIndex)
        {
            string? displayName = SR.GetResourceString("Globalization_cp_" + codePage.ToString());
            if (string.IsNullOrEmpty(displayName))
                displayName = s_englishNames.Substring(s_englishNameIndices[englishNameIndex], s_englishNameIndices[englishNameIndex + 1] - s_englishNameIndices[englishNameIndex]);

            return displayName;
        }
    }
}
