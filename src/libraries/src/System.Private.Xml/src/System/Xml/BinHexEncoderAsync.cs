// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace System.Xml
{
    internal static partial class BinHexEncoder
    {
        internal static async Task EncodeAsync(byte[] buffer, int index, int count, XmlWriter writer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (count > buffer.Length - index)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            char[] chars = new char[(count * 2) < CharsChunkSize ? (count * 2) : CharsChunkSize];
            int endIndex = index + count;
            while (index < endIndex)
            {
                int cnt = (count < CharsChunkSize / 2) ? count : CharsChunkSize / 2;
                int charCount = Encode(buffer, index, cnt, chars);
                await writer.WriteRawAsync(chars, 0, charCount).ConfigureAwait(false);
                index += cnt;
                count -= cnt;
            }
        }
    } // class
} // namespace
