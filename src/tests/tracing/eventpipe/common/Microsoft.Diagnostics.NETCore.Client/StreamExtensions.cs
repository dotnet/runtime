// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.NETCore.Client
{
    internal static class StreamExtensions
    {
        public static async Task<byte[]> ReadBytesAsync(this Stream stream, int length, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[length];

            int totalRead = 0;
            int remaining = length;
            while (remaining > 0)
            {
                int read = await stream.ReadAsync(buffer, totalRead, remaining, cancellationToken);
                if (0 == read)
                {
                    throw new EndOfStreamException();
                }

                remaining -= read;
                totalRead += read;
            }

            return buffer;
        }
    }
}
