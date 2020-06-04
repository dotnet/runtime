// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal partial class RandomNumberGeneratorImplementation
    {
        private static readonly FileStream s_randomStream = new FileStream("/dev/random", FileMode.Open, FileAccess.Read);

        private static unsafe void GetBytes(byte* pbBuffer, int count)
        {
            Debug.Assert(count > 0);

            int pos = 0;
            while (pos < count)
            {
                var span = new Span<byte>(pbBuffer + pos, count - pos);
                int res = s_randomStream.Read(span);
                if (res == 0)
                {
                    throw new CryptographicException();
                }
                pos += res;
            }
        }
    }
}
