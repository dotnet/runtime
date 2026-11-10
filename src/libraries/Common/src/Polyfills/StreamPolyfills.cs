// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

/// <summary>Provides downlevel polyfills for instance methods on <see cref="Stream"/>.</summary>
internal static class StreamPolyfills
{
    extension(Stream stream)
    {
        public void ReadExactly(byte[] buffer)
        {
            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int read = stream.Read(buffer, totalRead, buffer.Length - totalRead);
                if (read == 0)
                {
                    throw new EndOfStreamException();
                }

                totalRead += read;
            }
        }

        public Task CopyToAsync(Stream destination, CancellationToken cancellationToken) =>
            stream.CopyToAsync(destination, 81_920, cancellationToken); // 81_920 is the default buffer size used by Stream.CopyToAsync on .NET
    }
}
