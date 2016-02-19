// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    internal abstract partial class Base64Encoder
    {
        byte[] leftOverBytes;
        int leftOverBytesCount;
        char[] charsLine;

        internal const int Base64LineSize = 76;
        internal const int LineSizeInBytes = Base64LineSize / 4 * 3;

        internal Base64Encoder()
        {
            charsLine = new char[Base64LineSize];
        }

        internal abstract void WriteChars(char[] chars, int index, int count);

        internal void Encode(byte[] buffer, int index, int count)
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

            // encode left-over buffer
            if (leftOverBytesCount > 0)
            {
                int i = leftOverBytesCount;
                while (i < 3 && count > 0)
                {
                    leftOverBytes[i++] = buffer[index++];
                    count--;
                }

                // the total number of buffer we have is less than 3 -> return
                if (count == 0 && i < 3)
                {
                    leftOverBytesCount = i;
                    return;
                }

                // encode the left-over buffer and write out
                int leftOverChars = Convert.ToBase64CharArray(leftOverBytes, 0, 3, charsLine, 0);
                WriteChars(charsLine, 0, leftOverChars);
            }

            // store new left-over buffer
            leftOverBytesCount = count % 3;
            if (leftOverBytesCount > 0)
            {
                count -= leftOverBytesCount;
                if (leftOverBytes == null)
                {
                    leftOverBytes = new byte[3];
                }
                for (int i = 0; i < leftOverBytesCount; i++)
                {
                    leftOverBytes[i] = buffer[index + count + i];
                }
            }

            // encode buffer in 76 character long chunks
            int endIndex = index + count;
            int chunkSize = LineSizeInBytes;
            while (index < endIndex)
            {
                if (index + chunkSize > endIndex)
                {
                    chunkSize = endIndex - index;
                }
                int charCount = Convert.ToBase64CharArray(buffer, index, chunkSize, charsLine, 0);
                WriteChars(charsLine, 0, charCount);

                index += chunkSize;
            }
        }

        internal void Flush()
        {
            if (leftOverBytesCount > 0)
            {
                int leftOverChars = Convert.ToBase64CharArray(leftOverBytes, 0, leftOverBytesCount, charsLine, 0);
                WriteChars(charsLine, 0, leftOverChars);
                leftOverBytesCount = 0;
            }
        }
    }
}
