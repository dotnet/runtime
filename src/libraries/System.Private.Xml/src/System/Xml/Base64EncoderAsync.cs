// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Diagnostics;

using System.Threading.Tasks;

namespace System.Xml
{
    internal abstract partial class Base64Encoder
    {
        internal abstract Task WriteCharsAsync(char[] chars, int index, int count);

        internal Task EncodeAsync(byte[] buffer, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            if (index < 0 || (uint)count > buffer.Length - index)
            {
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count));
            }
            return Core(buffer, index, count);

            async Task Core(byte[] buffer, int index, int count)
            {
                // encode left-over buffer
                if (_leftOverBytesCount > 0)
                {
                    int i = _leftOverBytesCount;
                    while (i < 3 && count > 0)
                    {
                        _leftOverBytes![i++] = buffer[index++];
                        count--;
                    }

                    // the total number of buffer we have is less than 3 -> return
                    if (count == 0 && i < 3)
                    {
                        _leftOverBytesCount = i;
                        return;
                    }

                    // encode the left-over buffer and write out
                    int leftOverChars = Convert.ToBase64CharArray(_leftOverBytes!, 0, 3, _charsLine, 0);
                    await WriteCharsAsync(_charsLine, 0, leftOverChars).ConfigureAwait(false);
                }

                // store new left-over buffer
                _leftOverBytesCount = count % 3;
                if (_leftOverBytesCount > 0)
                {
                    count -= _leftOverBytesCount;
                    if (_leftOverBytes == null)
                    {
                        _leftOverBytes = new byte[3];
                    }
                    for (int i = 0; i < _leftOverBytesCount; i++)
                    {
                        _leftOverBytes[i] = buffer[index + count + i];
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
                    int charCount = Convert.ToBase64CharArray(buffer, index, chunkSize, _charsLine, 0);
                    await WriteCharsAsync(_charsLine, 0, charCount).ConfigureAwait(false);

                    index += chunkSize;
                }
            }
        }

        internal async Task FlushAsync()
        {
            if (_leftOverBytesCount > 0)
            {
                int leftOverChars = Convert.ToBase64CharArray(_leftOverBytes!, 0, _leftOverBytesCount, _charsLine, 0);
                await WriteCharsAsync(_charsLine, 0, leftOverChars).ConfigureAwait(false);
                _leftOverBytesCount = 0;
            }
        }
    }

    internal sealed partial class XmlTextWriterBase64Encoder : Base64Encoder
    {
        internal override Task WriteCharsAsync(char[] chars, int index, int count)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed partial class XmlRawWriterBase64Encoder : Base64Encoder
    {
        internal override Task WriteCharsAsync(char[] chars, int index, int count)
        {
            return _rawWriter.WriteRawAsync(chars, index, count);
        }
    }
}
