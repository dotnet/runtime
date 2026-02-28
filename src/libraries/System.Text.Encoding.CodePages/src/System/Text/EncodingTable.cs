// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;

namespace System.Text
{
    internal static partial class EncodingTable
    {
        private static readonly ConcurrentDictionary<string, int> s_nameToCodePageCache = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<int, string> s_codePageToWebNameCache = new ConcurrentDictionary<int, string>();
        private static readonly ConcurrentDictionary<int, string> s_codePageToEnglishNameCache = new ConcurrentDictionary<int, string>();
        private static readonly ConcurrentDictionary<int, (ushort FamilyCodePage, byte CodePageFlags)> s_codePageToItemCache = new ConcurrentDictionary<int, (ushort FamilyCodePage, byte CodePageFlags)>();

        internal static (ushort FamilyCodePage, byte CodePageFlags) GetCodePageItem(int codePage)
        {
            if (s_codePageToItemCache.TryGetValue(codePage, out (ushort FamilyCodePage, byte CodePageFlags) item))
            {
                return item;
            }

            int i = MappedCodePages.IndexOf((ushort)codePage);
            if (i < 0)
            {
                return ((ushort)codePage, (byte)0);
            }

            item = (MappedFamilyCodePage[i], MappedFlags[i]);
            s_codePageToItemCache.TryAdd(codePage, item);

            return item;
        }

        internal static int GetCodePageFromName(string name)
        {
            if (name is null)
                return 0;

            if (s_nameToCodePageCache.TryGetValue(name, out int codePage))
                return codePage;

            codePage = InternalGetCodePageFromName(name);
            if (codePage != 0)
                s_nameToCodePageCache.TryAdd(name, codePage);

            return codePage;
        }

        private static int InternalGetCodePageFromName(string name)
        {
            ReadOnlySpan<int> encodingNameIndices = EncodingNameIndices;

            int left = 0;
            int right = encodingNameIndices.Length - 2;
            int index;
            int result;

            Debug.Assert(encodingNameIndices.Length == CodePagesByName.Length + 1);
            Debug.Assert(encodingNameIndices[encodingNameIndices.Length - 1] == EncodingNames.Length);

            name = name.ToLowerInvariant();

            // Binary search the array until we have only a couple of elements left and then
            // just walk those elements.
            while ((right - left) > 3)
            {
                index = ((right - left) / 2) + left;

                Debug.Assert(index < encodingNameIndices.Length - 1);
                result = CompareOrdinal(name, EncodingNames, encodingNameIndices[index], encodingNameIndices[index + 1] - encodingNameIndices[index]);
                if (result == 0)
                {
                    //We found the item, return the associated codePage.
                    return CodePagesByName[index];
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

            // Walk the remaining elements (it'll be 3 or fewer).
            for (; left <= right; left++)
            {
                Debug.Assert(left < encodingNameIndices.Length - 1);
                if (CompareOrdinal(name, EncodingNames, encodingNameIndices[left], encodingNameIndices[left + 1] - encodingNameIndices[left]) == 0)
                {
                    return CodePagesByName[left];
                }
            }

            // The encoding name is not valid.
            return 0;
        }

        private static int CompareOrdinal(string s1, string s2, int index, int length)
        {
            int count = s1.Length;
            if (count > length)
                count = length;

            int i = 0;
            while (i < count && s1[i] == s2[index + i])
                i++;

            if (i < count)
                return (int)(s1[i] - s2[index + i]);

            return s1.Length - length;
        }

        internal static string? GetWebNameFromCodePage(int codePage)
        {
            return GetNameFromCodePage(codePage, WebNames, WebNameIndices, s_codePageToWebNameCache);
        }

        internal static string? GetEnglishNameFromCodePage(int codePage)
        {
            return GetNameFromCodePage(codePage, EnglishNames, EnglishNameIndices, s_codePageToEnglishNameCache);
        }

        private static string? GetNameFromCodePage(int codePage, string names, ReadOnlySpan<int> indices, ConcurrentDictionary<int, string> cache)
        {
            Debug.Assert(MappedCodePages.Length + 1 == indices.Length);
            Debug.Assert(indices[indices.Length - 1] == names.Length);

            if ((uint)codePage > ushort.MaxValue)
                return null;

            if (cache.TryGetValue(codePage, out string? cachedName))
                return cachedName;

            int i = MappedCodePages.IndexOf((ushort)codePage);
            if (i < 0)
                return null;

            Debug.Assert(i < indices.Length - 1);

            string name = names.Substring(indices[i], indices[i + 1] - indices[i]);
            cache.TryAdd(codePage, name);

            return name;
        }
    }
}
