// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Threading;

namespace System.Net
{
    /// <summary>
    /// The class is a simple wrapper on top of a read stream. It will read the exact number of bytes requested.
    /// It will throw if EOF is reached before the expected number of bytes is returned.
    /// </summary>
    internal static class FixedSizeReader
    {
        /// <summary>
        /// Returns 0 on legitimate EOF or if 0 bytes were requested, otherwise reads as directed or throws.
        /// Returns count on success.
        /// </summary>
        public static int ReadPacket(Stream transport, byte[] buffer, int offset, int count)
        {
            int remainingCount = count;
            do
            {
                int bytes = transport.Read(buffer, offset, remainingCount);
                if (bytes == 0)
                {
                    if (remainingCount != count)
                    {
                        throw new IOException(SR.net_io_eof);
                    }

                    return 0;
                }

                remainingCount -= bytes;
                offset += bytes;
            } while (remainingCount > 0);

            Debug.Assert(remainingCount == 0);
            return count;
        }

        /// <summary>
        /// Completes "request" with 0 if 0 bytes was requested or legitimate EOF received.
        /// Otherwise, reads as directed or completes "request" with an Exception.
        /// </summary>
        public static async void ReadPacketAsync(Stream transport, AsyncProtocolRequest request) // "async Task" might result in additional, unnecessary allocation
        {
            try
            {
                int remainingCount = request.Count, offset = request.Offset;
                do
                {
                    int bytes = await transport.ReadAsync(request.Buffer, offset, remainingCount, CancellationToken.None).ConfigureAwait(false);
                    if (bytes == 0)
                    {
                        if (remainingCount != request.Count)
                        {
                            throw new IOException(SR.net_io_eof);
                        }
                        request.CompleteRequest(0);
                        return;
                    }

                    offset += bytes;
                    remainingCount -= bytes;
                } while (remainingCount > 0);

                Debug.Assert(remainingCount == 0);
                request.CompleteRequest(request.Count);
            }
            catch (Exception e)
            {
                request.CompleteUserWithError(e);
            }
        }
    }
}
