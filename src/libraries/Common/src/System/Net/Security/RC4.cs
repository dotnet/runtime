// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// ARC4Managed.cs: Alleged RC4(tm) compatible symmetric stream cipher
// RC4 is a trademark of RSA Security
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Buffers;
using System.Diagnostics;
using System.Security.Cryptography;

namespace System.Net.Security
{
    // References:
    // a. Usenet 1994 - RC4 Algorithm revealed
    // http://www.qrst.de/html/dsds/rc4.htm
    internal class RC4 : IDisposable
    {
        private byte[]? state;
        private byte x;
        private byte y;

        public RC4(ReadOnlySpan<byte> key)
        {
            state = ArrayPool<byte>.Shared.Rent(256);

            byte index1 = 0;
            byte index2 = 0;

            for (int counter = 0; counter < 256; counter++)
            {
                state[counter] = (byte)counter;
            }

            for (int counter = 0; counter < 256; counter++)
            {
                index2 = (byte)(key[index1] + state[counter] + index2);
                (state[counter], state[index2]) = (state[index2], state[counter]);
                index1 = (byte)((index1 + 1) % key.Length);
            }
        }

        public void Dispose()
        {
            if (state != null)
            {
                x = 0;
                y = 0;
                CryptographicOperations.ZeroMemory(state.AsSpan(0, 256));
                ArrayPool<byte>.Shared.Return(state);
                state = null;
            }
        }

        public void Transform(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Debug.Assert(input.Length == output.Length);
            Debug.Assert(state != null);

            for (int counter = 0; counter < input.Length; counter++)
            {
                x = (byte)(x + 1);
                y = (byte)(state[x] + y);
                (state[x], state[y]) = (state[y], state[x]);
                byte xorIndex = (byte)(state[x] + state[y]);
                output[counter] = (byte)(input[counter] ^ state[xorIndex]);
            }
        }
    }
}
