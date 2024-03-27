// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace System.Xml
{
    internal abstract class XmlStreamNodeWriter : XmlNodeWriter
    {
        private readonly byte[] _buffer;
        private int _offset;
        private bool _ownsStream;
        private const int bufferLength = 512;
        private const int maxBytesPerChar = 3;
        private Encoding? _encoding;

        protected XmlStreamNodeWriter()
        {
            _buffer = new byte[bufferLength];
            OutputStream = null!; // Always initialized by SetOutput()
        }

        protected void SetOutput(Stream stream, bool ownsStream, Encoding? encoding)
        {
            OutputStream = stream;
            _ownsStream = ownsStream;
            _offset = 0;

            if (encoding != null)
                _encoding = encoding;
        }

        // Getting/Setting the Stream exists for fragmenting
        public Stream OutputStream { get; set; }

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
                return (int)OutputStream.Position + _offset;
            }
        }

        protected byte[] GetBuffer(int count, out int offset)
        {
            Debug.Assert(count >= 0 && count <= bufferLength);
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
            Debug.Assert(offset + count <= bufferLength);
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
            Debug.Assert(count >= 0 && count <= bufferLength);
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
            Debug.Assert(offset + count <= bufferLength);
            for (int i = 0; i < count; i++)
            {
                _buffer[offset + i] = (byte)'<';
            }
#endif
            return new BytesWithOffset(_buffer, offset);
        }

        protected void Advance(int count)
        {
            Debug.Assert(_offset + count <= bufferLength);
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
            Debug.Assert(ch < 0x80);
            WriteByte((byte)ch);
        }

        protected Task WriteByteAsync(char ch)
        {
            Debug.Assert(ch < 0x80);
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
            Debug.Assert(ch1 < 0x80 && ch2 < 0x80);
            WriteBytes((byte)ch1, (byte)ch2);
        }

        protected Task WriteBytesAsync(char ch1, char ch2)
        {
            Debug.Assert(ch1 < 0x80 && ch2 < 0x80);
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
                OutputStream.Write(byteBuffer, byteOffset, byteCount);
            }
        }

        protected void WriteBytes(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < bufferLength)
            {
                var buffer = GetBuffer(bytes.Length, out int offset).AsSpan(offset, bytes.Length);
                bytes.CopyTo(buffer);
                Advance(bytes.Length);
            }
            else
            {
                FlushBuffer();
                OutputStream.Write(bytes);
            }
        }

        protected unsafe void WriteUTF8Char(int ch)
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

        protected unsafe void WriteUTF8Chars(string value)
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

        protected void WriteUTF8Bytes(ReadOnlySpan<byte> value)
        {
            if (value.Length < bufferLength)
            {
                byte[] buffer = GetBuffer(value.Length, out int offset);
                value.CopyTo(buffer.AsSpan(offset));
                Advance(value.Length);
            }
            else
            {
                FlushBuffer();
                OutputStream.Write(value);
            }
        }

        protected unsafe void UnsafeWriteUTF8Chars(char* chars, int charCount)
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

        protected unsafe void UnsafeWriteUnicodeChars(char* chars, int charCount)
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

        protected static unsafe int UnsafeGetUnicodeChars(char* chars, int charCount, byte[] buffer, int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                new ReadOnlySpan<char>(chars, charCount)
                    .CopyTo(MemoryMarshal.Cast<byte, char>(buffer.AsSpan(offset)));
            }
            else
            {
                BinaryPrimitives.ReverseEndianness(new ReadOnlySpan<short>(chars, charCount),
                    MemoryMarshal.Cast<byte, short>(buffer.AsSpan(offset)));
            }

            return charCount * 2;
        }

        protected unsafe int UnsafeGetUTF8Length(char* chars, int charCount)
        {
            // Length will always be at least ( 128 / maxBytesPerChar) = 42
            return (_encoding ?? DataContractSerializer.ValidatingUTF8).GetByteCount(chars, charCount);
        }

        protected unsafe int UnsafeGetUTF8Chars(char* chars, int charCount, byte[] buffer, int offset)
        {
            if (charCount > 0)
            {
                fixed (byte* _bytes = &buffer[offset])
                {
                    // Fast path for small strings, use Encoding.GetBytes for larger strings since it is faster when vectorization is possible
                    if (!Vector128.IsHardwareAccelerated || (uint)charCount < 32)
                    {
                        byte* bytes = _bytes;
                        char* charsMax = &chars[charCount];

                        while (chars < charsMax)
                        {
                            char t = *chars;
                            if (t >= 0x80)
                                goto NonAscii;

                            *bytes = (byte)t;
                            bytes++;
                            chars++;
                        }
                        return charCount;

                    NonAscii:
                        byte* bytesMax = _bytes + buffer.Length - offset;
                        return (int)(bytes - _bytes) + (_encoding ?? DataContractSerializer.ValidatingUTF8).GetBytes(chars, (int)(charsMax - chars), bytes, (int)(bytesMax - bytes));
                    }
                    else
                    {
                        return (_encoding ?? DataContractSerializer.ValidatingUTF8).GetBytes(chars, charCount, _bytes, buffer.Length - offset);
                    }
                }
            }
            return 0;
        }

        protected virtual void FlushBuffer()
        {
            if (_offset != 0)
            {
                OutputStream.Write(_buffer, 0, _offset);
                _offset = 0;
            }
        }

        protected virtual Task FlushBufferAsync()
        {
            if (_offset != 0)
            {
                var task = OutputStream.WriteAsync(_buffer, 0, _offset);
                _offset = 0;
                return task;
            }

            return Task.CompletedTask;
        }

        public override void Flush()
        {
            FlushBuffer();
            OutputStream.Flush();
        }

        public override async Task FlushAsync()
        {
            await FlushBufferAsync().ConfigureAwait(false);
            await OutputStream.FlushAsync().ConfigureAwait(false);
        }

        public override void Close()
        {
            if (OutputStream != null)
            {
                if (_ownsStream)
                {
                    OutputStream.Dispose();
                }
                OutputStream = null!;
            }
        }
    }
}
