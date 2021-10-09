// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace Microsoft.NET.HostModel.AppHost
{
    public static class BinaryUtils
    {
        internal static unsafe void SearchAndReplace(
            MemoryMappedViewAccessor accessor,
            byte[] searchPattern,
            byte[] patternToReplace,
            bool pad0s = true)
        {
            byte* pointer = null;

            try
            {
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
                byte* bytes = pointer + accessor.PointerOffset;

                int position = KMPSearch(searchPattern, bytes, accessor.Capacity);
                if (position < 0)
                {
                    throw new PlaceHolderNotFoundInAppHostException(searchPattern);
                }

                accessor.WriteArray(
                    position: position,
                    array: patternToReplace,
                    offset: 0,
                    count: patternToReplace.Length);

                if (pad0s)
                {
                    Pad0(searchPattern, patternToReplace, bytes, position);
                }
            }
            finally
            {
                if (pointer != null)
                {
                    accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                }
            }
        }

        private static unsafe void Pad0(byte[] searchPattern, byte[] patternToReplace, byte* bytes, int offset)
        {
            if (patternToReplace.Length < searchPattern.Length)
            {
                for (int i = patternToReplace.Length; i < searchPattern.Length; i++)
                {
                    bytes[i + offset] = 0x0;
                }
            }
        }

        public static unsafe void SearchAndReplace(
            string filePath,
            byte[] searchPattern,
            byte[] patternToReplace,
            bool pad0s = true)
        {
            using (var mappedFile = MemoryMappedFile.CreateFromFile(filePath))
            {
                using (var accessor = mappedFile.CreateViewAccessor())
                {
                    SearchAndReplace(accessor, searchPattern, patternToReplace, pad0s);
                }
            }
        }

        internal static unsafe int SearchInFile(MemoryMappedViewAccessor accessor, byte[] searchPattern)
        {
            var safeBuffer = accessor.SafeMemoryMappedViewHandle;
            return KMPSearch(searchPattern, (byte*)safeBuffer.DangerousGetHandle(), (int)safeBuffer.ByteLength);
        }

        public static unsafe int SearchInFile(string filePath, byte[] searchPattern)
        {
            using (var mappedFile = MemoryMappedFile.CreateFromFile(filePath))
            {
                using (var accessor = mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
                {
                    return SearchInFile(accessor, searchPattern);
                }
            }
        }

        // See: https://en.wikipedia.org/wiki/Knuth%E2%80%93Morris%E2%80%93Pratt_algorithm
        private static int[] ComputeKMPFailureFunction(byte[] pattern)
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
        private static unsafe int KMPSearch(byte[] pattern, byte* bytes, long bytesLength)
        {
            int m = 0;
            int i = 0;
            int[] table = ComputeKMPFailureFunction(pattern);

            while (m + i < bytesLength)
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

        public static void CopyFile(string sourcePath, string destinationPath)
        {
            var destinationDirectory = new FileInfo(destinationPath).Directory.FullName;
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            // Copy file to destination path so it inherits the same attributes/permissions.
            File.Copy(sourcePath, destinationPath, overwrite: true);
        }

        internal static void WriteToStream(MemoryMappedViewAccessor sourceViewAccessor, FileStream fileStream, long length)
        {
            int pos = 0;
            int bufSize = 16384; //16K

            byte[] buf = new byte[bufSize];
            length = Math.Min(length, sourceViewAccessor.Capacity);
            do
            {
                int bytesRequested = Math.Min((int)length - pos, bufSize);
                if (bytesRequested <= 0)
                {
                    break;
                }

                int bytesRead = sourceViewAccessor.ReadArray(pos, buf, 0, bytesRequested);
                if (bytesRead > 0)
                {
                    fileStream.Write(buf, 0, bytesRead);
                    pos += bytesRead;
                }
            }
            while (true);
        }
    }
}
