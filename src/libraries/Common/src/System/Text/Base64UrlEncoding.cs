// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace System.Text
{
    internal static class Base64UrlEncoding
    {
        internal static ArraySegment<char> RentEncode(ReadOnlySpan<byte> input)
        {
            // Every 3 bytes turns into 4 chars for the Base64 operation
            int base64Len = ((input.Length + 2) / 3) * 4;
            char[] base64 = ArrayPool<char>.Shared.Rent(base64Len);

            if (!Convert.TryToBase64Chars(input, base64, out int charsWritten))
            {
                Debug.Fail($"Convert.TryToBase64 failed with {input.Length} bytes to a {base64.Length} buffer");
                throw new UnreachableException();
            }

            Debug.Assert(charsWritten == base64Len);

            // In the degenerate case every char will turn into 3 chars.
            int urlEncodedLen = charsWritten * 3;
            char[] urlEncoded = ArrayPool<char>.Shared.Rent(urlEncodedLen);
            int writeIdx = 0;

            for (int readIdx = 0; readIdx < charsWritten; readIdx++)
            {
                char cur = base64[readIdx];

                if (char.IsAsciiLetterOrDigit(cur))
                {
                    urlEncoded[writeIdx++] = cur;
                }
                else if (cur == '+')
                {
                    urlEncoded[writeIdx++] = '%';
                    urlEncoded[writeIdx++] = '2';
                    urlEncoded[writeIdx++] = 'B';
                }
                else if (cur == '/')
                {
                    urlEncoded[writeIdx++] = '%';
                    urlEncoded[writeIdx++] = '2';
                    urlEncoded[writeIdx++] = 'F';
                }
                else if (cur == '=')
                {
                    urlEncoded[writeIdx++] = '%';
                    urlEncoded[writeIdx++] = '3';
                    urlEncoded[writeIdx++] = 'D';
                }
                else
                {
                    Debug.Fail($"'{cur}' is not a valid Base64 character");
                    throw new UnreachableException();
                }
            }

            ArrayPool<char>.Shared.Return(base64);
            return new ArraySegment<char>(urlEncoded, 0, writeIdx);
        }
    }
}
