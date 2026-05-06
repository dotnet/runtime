// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace System.Text
{
    /// <summary>
    /// This class provides URL-encoded-Base64, which is distinct from the base64url encoding.
    /// </summary>
    internal static class UrlBase64Encoding
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

            ReadOnlySpan<char> source = base64.AsSpan(0, base64Len);
            Span<char> dest = urlEncoded;
            int written = 0;

            while (!source.IsEmpty)
            {
                int pos = source.IndexOfAny('+', '/', '=');

                if (pos < 0)
                {
                    source.CopyTo(dest);
                    written += source.Length;
                    break;
                }

                source.Slice(0, pos).CopyTo(dest);
                source = source.Slice(pos);
                dest = dest.Slice(pos);
                written += pos;

                dest[0] = '%';

                switch (source[0])
                {
                    case '+':
                        dest[1] = '2';
                        dest[2] = 'B';
                        break;
                    case '/':
                        dest[1] = '2';
                        dest[2] = 'F';
                        break;
                    default:
                        Debug.Assert(source[0] == '=');
                        dest[1] = '3';
                        dest[2] = 'D';
                        break;
                }

                source = source.Slice(1);
                dest = dest.Slice(3);
                written += 3;
            }

            ArrayPool<char>.Shared.Return(base64);
            return new ArraySegment<char>(urlEncoded, 0, written);
        }
    }
}
