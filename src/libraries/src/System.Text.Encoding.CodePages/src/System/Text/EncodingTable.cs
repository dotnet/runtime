// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;

namespace System.Text
{
    internal static partial class EncodingTable
    {
        private static readonly Dictionary<string, int> s_nameToCodePageCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<int, string> s_codePageToWebNameCache = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> s_codePageToEnglishNameCache = new Dictionary<int, string>();
        private static readonly ReaderWriterLockSlim s_cacheLock = new ReaderWriterLockSlim();

        internal static int GetCodePageFromName(string name)
        {
            if (name == null)
                return 0;

            int codePage;

            s_cacheLock.EnterUpgradeableReadLock();
            try
            {
                if (s_nameToCodePageCache.TryGetValue(name, out codePage))
                {
                    return codePage;
                }
                else
                {
                    // Okay, we didn't find it in the hash table, try looking it up in the unmanaged data.
                    codePage = InternalGetCodePageFromName(name);
                    if (codePage == 0)
                        return 0;

                    s_cacheLock.EnterWriteLock();
                    try
                    {
                        int cachedCodePage;
                        if (s_nameToCodePageCache.TryGetValue(name, out cachedCodePage))
                        {
                            return cachedCodePage;
                        }
                        s_nameToCodePageCache.Add(name, codePage);
                    }
                    finally
                    {
                        s_cacheLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                s_cacheLock.ExitUpgradeableReadLock();
            }

            return codePage;
        }

        unsafe private static int InternalGetCodePageFromName(string name)
        {
            int left = 0;
            int right = s_encodingNameIndices.Length - 2;
            int index;
            int result;

            Contract.Assert(s_encodingNameIndices.Length == s_codePagesByName.Length + 1);
            Contract.Assert(s_encodingNameIndices[s_encodingNameIndices.Length - 1] == s_encodingNames.Length);

            name = name.ToLowerInvariant();

            //Binary search the array until we have only a couple of elements left and then
            //just walk those elements.
            while ((right - left) > 3)
            {
                index = ((right - left) / 2) + left;

                Contract.Assert(index < s_encodingNameIndices.Length - 1);
                result = CompareOrdinal(name, s_encodingNames, s_encodingNameIndices[index], s_encodingNameIndices[index + 1] - s_encodingNameIndices[index]);
                if (result == 0)
                {
                    //We found the item, return the associated codePage.
                    return (s_codePagesByName[index]);
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
                Contract.Assert(left < s_encodingNameIndices.Length - 1);
                if (CompareOrdinal(name, s_encodingNames, s_encodingNameIndices[left], s_encodingNameIndices[left + 1] - s_encodingNameIndices[left]) == 0)
                {
                    return (s_codePagesByName[left]);
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

        unsafe internal static string GetWebNameFromCodePage(int codePage)
        {
            return GetNameFromCodePage(codePage, s_webNames, s_webNameIndices, s_codePageToWebNameCache);
        }

        unsafe internal static string GetEnglishNameFromCodePage(int codePage)
        {
            return GetNameFromCodePage(codePage, s_englishNames, s_englishNameIndices, s_codePageToEnglishNameCache);
        }

        unsafe private static string GetNameFromCodePage(int codePage, string names, int[] indices, Dictionary<int, string> cache)
        {
            string name;

            Contract.Assert(s_mappedCodePages.Length + 1 == indices.Length);
            Contract.Assert(indices[indices.Length - 1] == names.Length);

            //This is a linear search, but we probably won't be doing it very often.
            for (int i = 0; i < s_mappedCodePages.Length; i++)
            {
                if (s_mappedCodePages[i] == codePage)
                {
                    Contract.Assert(i < indices.Length - 1);

                    s_cacheLock.EnterUpgradeableReadLock();
                    try
                    {
                        if (cache.TryGetValue(codePage, out name))
                        {
                            return name;
                        }
                        else
                        {
                            fixed (char* pChar = names)
                            {
                                name = new String(pChar, indices[i], indices[i + 1] - indices[i]);
                            }

                            s_cacheLock.EnterWriteLock();
                            try
                            {
                                string cachedName;
                                if (cache.TryGetValue(codePage, out cachedName))
                                {
                                    return cachedName;
                                }

                                cache.Add(codePage, name);
                            }
                            finally
                            {
                                s_cacheLock.ExitWriteLock();
                            }
                        }
                    }
                    finally
                    {
                        s_cacheLock.ExitUpgradeableReadLock();
                    }

                    return name;
                }
            }

            //Nope, we didn't find it.
            return null;
        }
    }
}