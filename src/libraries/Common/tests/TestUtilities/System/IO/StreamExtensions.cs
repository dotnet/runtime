// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    public static class StreamExtensions
    {
        public static async Task<int> ReadByteAsync(this Stream stream, CancellationToken cancellationToken = default)
        {
            byte[] buffer = new byte[1];

            int numBytesRead = await stream.ReadAsync(buffer, 0, 1, cancellationToken);
            if (numBytesRead == 0)
            {
                return -1; // EOF
            }

            return buffer[0];
        }
    }
}
