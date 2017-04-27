using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public static class AppHostExtensions
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

        public static unsafe int SearchAndReplace(string filePath, byte[] search, byte[] replace, bool terminateWithNul)
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
                            Console.WriteLine("The search pattern was not found in the file.");
                            return -1;
                        }
                        foreach (var b in replace)
                        {
                            accessor.Write(offset++, b);
                        }
                        // Terminate with '\0'
                        if (terminateWithNul)
                        {
                            accessor.Write(offset++, (byte) 0);
                        }
                        return 0;
                    }
                }
            }
        }
    }
}

