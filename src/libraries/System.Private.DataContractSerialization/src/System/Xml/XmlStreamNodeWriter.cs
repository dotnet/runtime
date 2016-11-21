// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace System.Xml
{
    internal abstract class XmlStreamNodeWriter : XmlNodeWriter
    {
        private Stream _stream;
        private byte[] _buffer;
        private int _offset;
        private bool _ownsStream;
        private const int bufferLength = 512;
        private const int maxBytesPerChar = 3;
        private Encoding _encoding;
        private static UTF8Encoding s_UTF8Encoding = new UTF8Encoding(false, true);

        protected XmlStreamNodeWriter()
        {
            _buffer = new byte[bufferLength];
        }

        protected void SetOutput(Stream stream, bool ownsStream, Encoding encoding)
        {
            _stream = stream;
            _ownsStream = ownsStream;
            _offset = 0;
            _encoding = encoding;
        }

        // Getting/Setting the Stream exists for fragmenting
        public Stream Stream
        {
            get
            {
                return _stream;
            }
            set
            {
                _stream = value;
            }
        }

        // StreamBuffer/BufferOffset exists only for the BinaryWriter to fix up nodes
        public byte[] StreamBuffer
        {
            get
            {
                return _buffer;
            }
        }
        public int BufferOffset
        {
            get
            {
                return _offset;
            }
        }

        public int Position
        {
            get
            {
                return (int)_stream.Position + _offset;
            }
        }

        private int GetByteCount(char[] chars)
        {
            if (_encoding == null)
            {
                return s_UTF8Encoding.GetByteCount(chars);
            }
            else
            {
                return _encoding.GetByteCount(chars);
            }
        }

        protected byte[] GetBuffer(int count, out int offset)
        {
            DiagnosticUtility.DebugAssert(count >= 0 && count <= bufferLength, "");
            int bufferOffset = _offset;
            if (bufferOffset + count <= bufferLength)
            {
                offset = bufferOffset;
            }
            else
            {
                FlushBuffer();
                offset = 0;
            }
#if DEBUG
            DiagnosticUtility.DebugAssert(offset + count <= bufferLength, "");
            for (int i = 0; i < count; i++)
            {
                _buffer[offset + i] = (byte)'<';
            }
#endif
            return _buffer;
        }

        protected async Task<BytesWithOffset> GetBufferAsync(int count)
        {
            int offset;
            DiagnosticUtility.DebugAssert(count >= 0 && count <= bufferLength, "");
            int bufferOffset = _offset;
            if (bufferOffset + count <= bufferLength)
            {
                offset = bufferOffset;
            }
            else
            {
                await FlushBufferAsync().ConfigureAwait(false);
                offset = 0;
            }
#if DEBUG
            DiagnosticUtility.DebugAssert(offset + count <= bufferLength, "");
            for (int i = 0; i < count; i++)
            {
                _buffer[offset + i] = (byte)'<';
            }
#endif
            return new BytesWithOffset(_buffer, offset);
        }

        protected void Advance(int count)
        {
            DiagnosticUtility.DebugAssert(_offset + count <= bufferLength, "");
            _offset += count;
        }

        private void EnsureByte()
        {
            if (_offset >= bufferLength)
            {
                FlushBuffer();
            }
        }

        protected void WriteByte(byte b)
        {
            EnsureByte();
            _buffer[_offset++] = b;
        }

        protected Task WriteByteAsync(byte b)
        {
            if (_offset >= bufferLength)
            {
                return FlushBufferAndWriteByteAsync(b);
            }
            else
            {
                _buffer[_offset++] = b;
                return Task.CompletedTask;
            }
        }

        private async Task FlushBufferAndWriteByteAsync(byte b)
        {
            await FlushBufferAsync().ConfigureAwait(false);
            _buffer[_offset++] = b;
        }

        protected void WriteByte(char ch)
        {
            DiagnosticUtility.DebugAssert(ch < 0x80, "");
            WriteByte((byte)ch);
        }

        protected Task WriteByteAsync(char ch)
        {
            DiagnosticUtility.DebugAssert(ch < 0x80, "");
            return WriteByteAsync((byte)ch);
        }

        protected void WriteBytes(byte b1, byte b2)
        {
            byte[] buffer = _buffer;
            int offset = _offset;
            if (offset + 1 >= bufferLength)
            {
                FlushBuffer();
                offset = 0;
            }
            buffer[offset + 0] = b1;
            buffer[offset + 1] = b2;
            _offset += 2;
        }

        protected Task WriteBytesAsync(byte b1, byte b2)
        {
            if (_offset + 1 >= bufferLength)
            {
                return FlushAndWriteBytesAsync(b1, b2);
            }
            else
            {
                _buffer[_offset++] = b1;
                _buffer[_offset++] = b2;
                return Task.CompletedTask;
            }
        }

        private async Task FlushAndWriteBytesAsync(byte b1, byte b2)
        {
            await FlushBufferAsync().ConfigureAwait(false);
            _buffer[_offset++] = b1;
            _buffer[_offset++] = b2;
        }

        protected void WriteBytes(char ch1, char ch2)
        {
            DiagnosticUtility.DebugAssert(ch1 < 0x80 && ch2 < 0x80, "");
            WriteBytes((byte)ch1, (byte)ch2);
        }

        protected Task WriteBytesAsync(char ch1, char ch2)
        {
            DiagnosticUtility.DebugAssert(ch1 < 0x80 && ch2 < 0x80, "");
            return WriteBytesAsync((byte)ch1, (byte)ch2);
        }

        public void WriteBytes(byte[] byteBuffer, int byteOffset, int byteCount)
        {
            if (byteCount < bufferLength)
            {
                int offset;
                byte[] buffer = GetBuffer(byteCount, out offset);
                Buffer.BlockCopy(byteBuffer, byteOffset, buffer, offset, byteCount);
                Advance(byteCount);
            }
            else
            {
                FlushBuffer();
                _stream.Write(byteBuffer, byteOffset, byteCount);
            }
        }

        unsafe protected void UnsafeWriteBytes(byte* bytes, int byteCount)
        {
            FlushBuffer();
            byte[] buffer = _buffer;
            while (byteCount >= bufferLength)
            {
                for (int i = 0; i < bufferLength; i++)
                    buffer[i] = bytes[i];
                _stream.Write(buffer, 0, bufferLength);
                bytes += bufferLength;
                byteCount -= bufferLength;
            }
            {
                for (int i = 0; i < byteCount; i++)
                    buffer[i] = bytes[i];
                _stream.Write(buffer, 0, byteCount);
            }
        }

        unsafe protected void WriteUTF8Char(int ch)
        {
            if (ch < 0x80)
            {
                WriteByte((byte)ch);
            }
            else if (ch <= char.MaxValue)
            {
                char* chars = stackalloc char[1];
                chars[0] = (char)ch;
                UnsafeWriteUTF8Chars(chars, 1);
            }
            else
            {
                SurrogateChar surrogateChar = new SurrogateChar(ch);
                char* chars = stackalloc char[2];
                chars[0] = surrogateChar.HighChar;
                chars[1] = surrogateChar.LowChar;
                UnsafeWriteUTF8Chars(chars, 2);
            }
        }

        protected void WriteUTF8Chars(byte[] chars, int charOffset, int charCount)
        {
            if (charCount < bufferLength)
            {
                int offset;
                byte[] buffer = GetBuffer(charCount, out offset);
                Buffer.BlockCopy(chars, charOffset, buffer, offset, charCount);
                Advance(charCount);
            }
            else
            {
                FlushBuffer();
                _stream.Write(chars, charOffset, charCount);
            }
        }

        unsafe protected void WriteUTF8Chars(string value)
        {
            int count = value.Length;
            if (count > 0)
            {
                fixed (char* chars = value)
                {
                    UnsafeWriteUTF8Chars(chars, count);
                }
            }
        }

        unsafe protected void UnsafeWriteUTF8Chars(char* chars, int charCount)
        {
            const int charChunkSize = bufferLength / maxBytesPerChar;
            while (charCount > charChunkSize)
            {
                int offset;
                int chunkSize = charChunkSize;
                if ((int)(chars[chunkSize - 1] & 0xFC00) == 0xD800) // This is a high surrogate
                    chunkSize--;
                byte[] buffer = GetBuffer(chunkSize * maxBytesPerChar, out offset);
                Advance(UnsafeGetUTF8Chars(chars, chunkSize, buffer, offset));
                charCount -= chunkSize;
                chars += chunkSize;
            }
            if (charCount > 0)
            {
                int offset;
                byte[] buffer = GetBuffer(charCount * maxBytesPerChar, out offset);
                Advance(UnsafeGetUTF8Chars(chars, charCount, buffer, offset));
            }
        }

        unsafe protected void UnsafeWriteUnicodeChars(char* chars, int charCount)
        {
            const int charChunkSize = bufferLength / 2;
            while (charCount > charChunkSize)
            {
                int offset;
                int chunkSize = charChunkSize;
                if ((int)(chars[chunkSize - 1] & 0xFC00) == 0xD800) // This is a high surrogate
                    chunkSize--;
                byte[] buffer = GetBuffer(chunkSize * 2, out offset);
                Advance(UnsafeGetUnicodeChars(chars, chunkSize, buffer, offset));
                charCount -= chunkSize;
                chars += chunkSize;
            }
            if (charCount > 0)
            {
                int offset;
                byte[] buffer = GetBuffer(charCount * 2, out offset);
                Advance(UnsafeGetUnicodeChars(chars, charCount, buffer, offset));
            }
        }

        unsafe protected int UnsafeGetUnicodeChars(char* chars, int charCount, byte[] buffer, int offset)
        {
            char* charsMax = chars + charCount;
            while (chars < charsMax)
            {
                char value = *chars++;
                buffer[offset++] = (byte)value;
                value >>= 8;
                buffer[offset++] = (byte)value;
            }
            return charCount * 2;
        }

        unsafe protected int UnsafeGetUTF8Length(char* chars, int charCount)
        {
            char* charsMax = chars + charCount;
            while (chars < charsMax)
            {
                if (*chars >= 0x80)
                    break;

                chars++;
            }

            if (chars == charsMax)
                return charCount;

            char[] chArray = new char[charsMax - chars];
            for (int i = 0; i < chArray.Length; i++)
            {
                chArray[i] = chars[i];
            }
            return (int)(chars - (charsMax - charCount)) + GetByteCount(chArray);
        }

        unsafe protected int UnsafeGetUTF8Chars(char* chars, int charCount, byte[] buffer, int offset)
        {
            if (charCount > 0)
            {
                fixed (byte* _bytes = &buffer[offset])
                {
                    byte* bytes = _bytes;
                    byte* bytesMax = &bytes[buffer.Length - offset];
                    char* charsMax = &chars[charCount];

                    while (true)
                    {
                        while (chars < charsMax)
                        {
                            char t = *chars;
                            if (t >= 0x80)
                                break;

                            *bytes = (byte)t;
                            bytes++;
                            chars++;
                        }

                        if (chars >= charsMax)
                            break;

                        char* charsStart = chars;
                        while (chars < charsMax && *chars >= 0x80)
                        {
                            chars++;
                        }

                        bytes += (_encoding ?? s_UTF8Encoding).GetBytes(charsStart, (int)(chars - charsStart), bytes, (int)(bytesMax - bytes));

                        if (chars >= charsMax)
                            break;
                    }

                    return (int)(bytes - _bytes);
                }
            }
            return 0;
        }

        protected virtual void FlushBuffer()
        {
            if (_offset != 0)
            {
                _stream.Write(_buffer, 0, _offset);
                _offset = 0;
            }
        }

        protected virtual Task FlushBufferAsync()
        {
            if (_offset != 0)
            {
                var task = _stream.WriteAsync(_buffer, 0, _offset);
                _offset = 0;
                return task;
            }

            return Task.CompletedTask;
        }

        public override void Flush()
        {
            FlushBuffer();
            _stream.Flush();
        }

        public override async Task FlushAsync()
        {
            await FlushBufferAsync().ConfigureAwait(false);
            await _stream.FlushAsync().ConfigureAwait(false);
        }

        public override void Close()
        {
            if (_stream != null)
            {
                if (_ownsStream)
                {
                    _stream.Dispose();
                }
                _stream = null;
            }
        }
    }
}