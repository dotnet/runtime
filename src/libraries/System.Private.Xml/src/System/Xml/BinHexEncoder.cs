// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml
{
    internal static partial class BinHexEncoder
    {
        private const int CharsChunkSize = 128;

        internal static void Encode(byte[] buffer, int index, int count, XmlWriter writer)
        {
            ArgumentNullException.ThrowIfNull(buffer);

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
                HexConverter.EncodeToUtf16(buffer.AsSpan(index, cnt), chars);
                writer.WriteRaw(chars, 0, cnt * 2);
                index += cnt;
                count -= cnt;
            }
        }

        internal static string Encode(byte[] inArray, int offsetIn, int count)
        {
            return Convert.ToHexString(inArray, offsetIn, count);
        }
    } // class
} // namespace
