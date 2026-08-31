// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.IO
{
    internal static partial class ArchivingUtils
    {
        public static void EnsureCapacity(ref char[] buffer, int min)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(min > 0);

            if (buffer.Length < min)
            {
                int newCapacity = buffer.Length * 2;
                if (newCapacity < min)
                    newCapacity = min;

                char[] oldBuffer = buffer;
                buffer = ArrayPool<char>.Shared.Rent(newCapacity);
                ArrayPool<char>.Shared.Return(oldBuffer);
            }
        }

        public static bool IsDirEmpty(string directoryFullName)
        {
            using (IEnumerator<string> enumerator = Directory.EnumerateFileSystemEntries(directoryFullName).GetEnumerator())
                return !enumerator.MoveNext();
        }

        public static void AttemptSetLastWriteTime(string destinationFileName, DateTimeOffset lastWriteTime)
        {
            try
            {
                File.SetLastWriteTime(destinationFileName, lastWriteTime.DateTime);
            }
            catch
            {
                // Some OSes like Android (#35374) might not support setting the last write time, the extraction should not fail because of that
            }
        }
    }
}
