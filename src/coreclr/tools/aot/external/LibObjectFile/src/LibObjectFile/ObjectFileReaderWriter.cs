// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using LibObjectFile.Utils;

namespace LibObjectFile
{
    /// <summary>
    /// Base class used for reading / writing an object file to/from a stream.
    /// </summary>
    public abstract class ObjectFileReaderWriter
    {
        private Stream _stream;

        protected ObjectFileReaderWriter(Stream stream) : this(stream, new DiagnosticBag())
        {
        }

        protected ObjectFileReaderWriter(Stream stream, DiagnosticBag diagnostics)
        {
            Stream = stream;
            Diagnostics = diagnostics;
            IsLittleEndian = true;
        }

        /// <summary>
        /// Gets or sets stream of the object file.
        /// </summary>
        public Stream Stream
        {
            get => _stream;
            set => _stream = value;
        }

        public ulong Offset
        {
            get => (ulong) Stream.Position;
            set => Stream.Position = (long) value;
        }

        public ulong Length
        {
            get => (ulong) Stream.Length;
        }

        /// <summary>
        /// The diagnostics while read/writing this object file.
        /// </summary>
        public DiagnosticBag Diagnostics { get; protected set; }

        /// <summary>
        /// Gets a boolean indicating if this reader is operating in read-only mode.
        /// </summary>
        public abstract bool IsReadOnly { get; }

        public bool IsLittleEndian { get; protected set; }

        public TextWriter Log { get; set; }

        /// <summary>
        /// Reads from the <see cref="Stream"/> and current position to the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer to receive the content of the read.</param>
        /// <param name="offset">The offset into the buffer.</param>
        /// <param name="count">The number of bytes to write from the buffer.</param>
        public int Read(byte[] buffer, int offset, int count)
        {
            return Stream.Read(buffer, offset, count);
        }
        
        /// <summary>
        /// Reads a null terminated UTF8 string from the stream.
        /// </summary>
        /// <returns><c>true</c> if the string was successfully read from the stream, false otherwise</returns>
        public string ReadStringUTF8NullTerminated()
        {
            return Stream.ReadStringUTF8NullTerminated();
        }
     
        /// <summary>
        /// Reads a null terminated UTF8 string from the stream.
        /// </summary>
        /// <param name="byteLength">The number of bytes to read including the null</param>
        /// <returns>A string</returns>
        public string ReadStringUTF8NullTerminated(uint byteLength)
        {
            return Stream.ReadStringUTF8NullTerminated(byteLength);
        }

        public byte ReadU8()
        {
            return Stream.ReadU8();
        }

        public sbyte ReadI8()
        {
            return Stream.ReadI8();
        }

        public short ReadI16()
        {
            return Stream.ReadI16(IsLittleEndian);
        }

        public ushort ReadU16()
        {
            return Stream.ReadU16(IsLittleEndian);
        }

        public int ReadI32()
        {
            return Stream.ReadI32(IsLittleEndian);
        }

        public uint ReadU32()
        {
            return Stream.ReadU32(IsLittleEndian);
        }
        
        public long ReadI64()
        {
            return Stream.ReadI64(IsLittleEndian);
        }

        public ulong ReadU64()
        {
            return Stream.ReadU64(IsLittleEndian);
        }

        public void WriteI8(sbyte value)
        {
            Stream.WriteI8(value);
        }

        public void WriteU8(byte value)
        {
            Stream.WriteU8(value);
        }
        
        public void WriteU16(ushort value)
        {
            Stream.WriteU16(IsLittleEndian, value);
        }

        public void WriteU32(uint value)
        {
            Stream.WriteU32(IsLittleEndian, value);
        }

        public void WriteU64(ulong value)
        {
            Stream.WriteU64(IsLittleEndian, value);
        }

        /// <summary>
        /// Writes a null terminated UTF8 string to the stream.
        /// </summary>
        public void WriteStringUTF8NullTerminated(string text)
        {
            Stream.WriteStringUTF8NullTerminated(text);
        }

        /// <summary>
        /// Tries to read an element of type <paramref name="{T}"/> with a specified size.
        /// </summary>
        /// <typeparam name="T">Type of the element to read.</typeparam>
        /// <param name="sizeToRead">Size of the element to read (might be smaller or bigger).</param>
        /// <param name="data">The data read.</param>
        /// <returns><c>true</c> if reading was successful. <c>false</c> otherwise.</returns>
        public unsafe bool TryReadData<T>(int sizeToRead, out T data) where T : unmanaged
        {
            if (sizeToRead <= 0) throw new ArgumentOutOfRangeException(nameof(sizeToRead));

            int dataByteCount = sizeof(T);
            int byteRead;

            // If we are requested to read more data than the sizeof(T)
            // we need to read it to an intermediate buffer before transferring it to T data
            if (sizeToRead > dataByteCount)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(sizeToRead);
                var span = new Span<byte>(buffer, 0, sizeToRead);
                byteRead = Stream.Read(span);
                data = MemoryMarshal.Cast<byte, T>(span)[0];
                ArrayPool<byte>.Shared.Return(buffer);
            }
            else
            {
                // Clear the data if the size requested is less than the expected struct to read
                if (sizeToRead < dataByteCount)
                {
                    data = default;
                }

                fixed (void* pData = &data)
                {
                    var span = new Span<byte>(pData, sizeToRead);
                    byteRead = Stream.Read(span);
                }
            }
            return byteRead == sizeToRead;
        }

        /// <summary>
        /// Reads from the current <see cref="Stream"/> <see cref="size"/> bytes and return the data as
        /// a <see cref="SliceStream"/> if <see cref="IsReadOnly"/> is <c>false</c> otherwise as a 
        /// <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="size">Size of the data to read.</param>
        /// <returns>A <see cref="SliceStream"/> if <see cref="IsReadOnly"/> is <c>false</c> otherwise as a 
        /// <see cref="MemoryStream"/>.</returns>
        public Stream ReadAsStream(ulong size)
        {
            if (IsReadOnly)
            {
                var stream = ReadAsSliceStream(size);
                Stream.Position += stream.Length;
                return stream;
            }

            return ReadAsMemoryStream(size);
        }

        /// <summary>
        /// Writes to the <see cref="Stream"/> and current position from the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write the content from.</param>
        /// <param name="offset">The offset into the buffer.</param>
        /// <param name="count">The number of bytes to read from the buffer and write to the stream.</param>
        public void Write(byte[] buffer, int offset, int count)
        {
            Stream.Write(buffer, offset, count);
        }

        /// <summary>
        /// Writes an element of type <paramref name="{T}"/> to the stream.
        /// </summary>
        /// <typeparam name="T">Type of the element to read.</typeparam>
        /// <param name="data">The data to write.</param>
        public unsafe void Write<T>(in T data) where T : unmanaged
        {
            fixed (void* pData = &data)
            {
                var span = new ReadOnlySpan<byte>(pData, sizeof(T));
                Stream.Write(span);
            }
        }

        /// <summary>
        /// Writes from the specified stream to the current <see cref="Stream"/> of this instance.
        /// The position of the input stream is set to 0 before writing and reset back to 0 after writing.
        /// </summary>
        /// <param name="inputStream">The input stream to read from and write to <see cref="Stream"/></param>
        /// <param name="size">The amount of data to read from the input stream (if == 0, by default, it will read the entire input stream)</param>
        /// <param name="bufferSize">The size of the intermediate buffer used to transfer the data.</param>
        public void Write(Stream inputStream, ulong size = 0, int bufferSize = 4096)
        {
            if (inputStream == null) throw new ArgumentNullException(nameof(inputStream));
            if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));

            inputStream.Position = 0;
            size = size == 0 ? (ulong)inputStream.Length : size;
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            while (size != 0)
            {
                var sizeToRead = size >= (ulong)buffer.Length ? buffer.Length : (int)size;
                var sizeRead = inputStream.Read(buffer, 0, sizeToRead);
                if (sizeRead <= 0) break;

                Stream.Write(buffer, 0, sizeRead);
                size -= (ulong)sizeRead;
            }

            inputStream.Position = 0;
            if (size != 0)
            {
                throw new InvalidOperationException("Unable to write stream entirely");
            }
        }
        
        private SliceStream ReadAsSliceStream(ulong size)
        {
            var position = Stream.Position;
            if (position + (long)size > Stream.Length)
            {
                if (position < Stream.Length)
                {
                    size = Stream.Position < Stream.Length ? (ulong)(Stream.Length - Stream.Position) : 0;
                    Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unexpected end of file. Expecting to slice {size} bytes at offset {position} while remaining length is {size}");
                }
                else
                {
                    position = Stream.Length;
                    size = 0;
                    Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unexpected end of file. Position of slice {position} is outside of the stream length {Stream.Length} in bytes");
                }
            }

            return new SliceStream(Stream, position, (long)size);
        }

        private MemoryStream ReadAsMemoryStream(ulong size)
        {
            var memoryStream = new MemoryStream((int)size);
            if (size == 0) return memoryStream;

            memoryStream.SetLength((long)size);

            var buffer = memoryStream.GetBuffer();
            while (size != 0)
            {
                var lengthToRead = size >= int.MaxValue ? int.MaxValue : (int)size;
                var lengthRead = Stream.Read(buffer, 0, lengthToRead);
                if (lengthRead < 0) break;
                if ((uint)lengthRead >= size)
                {
                    size -= (uint)lengthRead;
                }
                else
                {
                    break;
                }
            }

            if (size != 0)
            {
                Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unexpected end of file. Expecting to read {size} bytes at offset {Stream.Position}");
            }
            
            return memoryStream;
        }
    }
}