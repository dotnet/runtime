// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    internal static partial class BinHexEncoder
    {
        private const string s_hexDigits = "0123456789ABCDEF";
        private const int CharsChunkSize = 128;

        internal static void Encode(byte[] buffer, int index, int count, XmlWriter writer)
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
                writer.WriteRaw(chars, 0, charCount);
                index += cnt;
                count -= cnt;
            }
        }

        internal static string Encode(byte[] inArray, int offsetIn, int count)
        {
            if (null == inArray)
            {
                throw new ArgumentNullException(nameof(inArray));
            }
            if (0 > offsetIn)
            {
                throw new ArgumentOutOfRangeException(nameof(offsetIn));
            }
            if (0 > count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (count > inArray.Length - offsetIn)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            char[] outArray = new char[2 * count];
            int lenOut = Encode(inArray, offsetIn, count, outArray);
            return new String(outArray, 0, lenOut);
        }

        private static int Encode(byte[] inArray, int offsetIn, int count, char[] outArray)
        {
            int curOffsetOut = 0, offsetOut = 0;
            byte b;
            int lengthOut = outArray.Length;

            for (int j = 0; j < count; j++)
            {
                b = inArray[offsetIn++];
                outArray[curOffsetOut++] = s_hexDigits[b >> 4];
                if (curOffsetOut == lengthOut)
                {
                    break;
                }
                outArray[curOffsetOut++] = s_hexDigits[b & 0xF];
                if (curOffsetOut == lengthOut)
                {
                    break;
                }
            }
            return curOffsetOut - offsetOut;
        } // function
    } // class
} // namespace
