// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace System.Reflection.Internal
{
    internal static class StreamExtensions
    {
        // From System.IO.Stream.CopyTo:
        // We pick a value that is the largest multiple of 4096 that is still smaller than the large object heap threshold (85K).
        // The CopyTo/CopyToAsync buffer is short-lived and is likely to be collected at Gen0, and it offers a significant
        // improvement in Copy performance.
        internal const int StreamCopyBufferSize = 81920;

        /// <summary>
        /// Copies specified amount of data from given stream to a target memory pointer.
        /// </summary>
        /// <exception cref="IOException">unexpected stream end.</exception>
        internal static unsafe void CopyTo(this Stream source, byte* destination, int size)
        {
            byte[] buffer = new byte[Math.Min(StreamCopyBufferSize, size)];
            while (size > 0)
            {
                int readSize = Math.Min(size, buffer.Length);
                int bytesRead = source.Read(buffer, 0, readSize);

                if (bytesRead <= 0 || bytesRead > readSize)
                {
                    throw new IOException(MetadataResources.UnexpectedStreamEnd);
                }

                Marshal.Copy(buffer, 0, (IntPtr)destination, bytesRead);

                destination += bytesRead;
                size -= bytesRead;
            }
        }
    }
}
