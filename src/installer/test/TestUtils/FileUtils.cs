// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public static class FileUtils
    {
        // See: https://en.wikipedia.org/wiki/Knuth%E2%80%93Morris%E2%80%93Pratt_algorithm
        static int[] ComputeKMP_FailureFunction(byte[] pattern)
        {
            int[] table = new int[pattern.Length];
            if (pattern.Length >= 1)
            {
                table[0] = -1;
            }
            if (pattern.Length >= 2)
            {
                table[1] = 0;
            }

            int pos = 2;
            int cnd = 0;
            while (pos < pattern.Length)
            {
                if (pattern[pos - 1] == pattern[cnd])
                {
                    table[pos] = cnd + 1;
                    cnd++;
                    pos++;
                }
                else if (cnd > 0)
                {
                    cnd = table[cnd];
                }
                else
                {
                    table[pos] = 0;
                    pos++;
                }
            }
            return table;
        }

        // See: https://en.wikipedia.org/wiki/Knuth%E2%80%93Morris%E2%80%93Pratt_algorithm
        static unsafe int KMP_Search(byte[] pattern, byte* bytes, int length)
        {
            int m = 0;
            int i = 0;
            int[] table = ComputeKMP_FailureFunction(pattern);

            while (m + i < length)
            {
                if (pattern[i] == bytes[m + i])
                {
                    if (i == pattern.Length - 1)
                    {
                        return m;
                    }
                    i++;
                }
                else
                {
                    if (table[i] > -1)
                    {
                        m = m + i - table[i];
                        i = table[i];
                    }
                    else
                    {
                        m++;
                        i = 0;
                    }
                }
            }

            return -1;
        }

        public static unsafe void SearchAndReplace(string filePath, byte[] search, byte[] replace, bool terminateWithNul)
        {
            // Create stream because CreateFromFile(string, ...) uses FileShare.None which is too strict
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, 4096, false))
            {
                using (var mappedFile = MemoryMappedFile.CreateFromFile(
                    fileStream, null, fileStream.Length, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true))
                {
                    using (var accessor = mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite))
                    {
                        var safeBuffer = accessor.SafeMemoryMappedViewHandle;
                        int offset = KMP_Search(search, (byte*)safeBuffer.DangerousGetHandle(), (int)safeBuffer.ByteLength);
                        if (offset < 0)
                        {
                            throw new Exception("The search pattern was not found in the file.");
                        }
                        foreach (var b in replace)
                        {
                            accessor.Write(offset++, b);
                        }
                        // Terminate with '\0'
                        if (terminateWithNul)
                        {
                            accessor.Write(offset++, (byte)0);
                        }
                    }
                }
            }
        }

        public static unsafe int SearchInFile(string filePath, byte[] search)
        {
            // Create stream because CreateFromFile(string, ...) uses FileShare.None which is too strict
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, false))
            {
                using (var mappedFile = MemoryMappedFile.CreateFromFile(
                    fileStream, null, fileStream.Length, MemoryMappedFileAccess.Read, HandleInheritability.None, true))
                {
                    using (var accessor = mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
                    {
                        var safeBuffer = accessor.SafeMemoryMappedViewHandle;
                        return KMP_Search(search, (byte*)safeBuffer.DangerousGetHandle(), (int)safeBuffer.ByteLength);
                    }
                }
            }
        }
    }
}
