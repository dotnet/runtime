// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Net.Sockets
{
    // Class that wraps the semantics of a Winsock TRANSMIT_PACKETS_ELEMENTS struct.
    public class SendPacketsElement
    {
        // Constructors for file elements.
        public SendPacketsElement(string filepath) :
            this(filepath, 0L, 0, false)
        { }

        public SendPacketsElement(string filepath, int offset, int count) :
            this(filepath, (long)offset, count, false)
        { }

        public SendPacketsElement(string filepath, int offset, int count, bool endOfPacket) :
            this(filepath, (long)offset, count, endOfPacket)
        { }

        public SendPacketsElement(string filepath, long offset, int count) :
            this(filepath, offset, count, false)
        { }

        public SendPacketsElement(string filepath, long offset, int count, bool endOfPacket)
        {
            ArgumentNullException.ThrowIfNull(filepath);

            // The native API will validate the file length on send.
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            Initialize(filepath, null, null, null, offset, count, endOfPacket);
        }

        // Constructors for fileStream elements.
        public SendPacketsElement(FileStream fileStream) :
            this(fileStream, 0L, 0, false)
        { }

        public SendPacketsElement(FileStream fileStream, long offset, int count) :
            this(fileStream, offset, count, false)
        { }

        public SendPacketsElement(FileStream fileStream, long offset, int count, bool endOfPacket)
        {
            ArgumentNullException.ThrowIfNull(fileStream);

            if (!fileStream.IsAsync)
            {
                throw new ArgumentException(SR.net_sockets_sendpackelement_FileStreamMustBeAsync, nameof(fileStream));
            }
            // The native API will validate the file length on send.
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            Initialize(null, fileStream, null, null, offset, count, endOfPacket);
        }

        // Constructors for buffer elements.
        public SendPacketsElement(byte[] buffer) :
            this(buffer, 0, (buffer != null ? buffer.Length : 0), false)
        { }

        public SendPacketsElement(byte[] buffer, int offset, int count) :
            this(buffer, offset, count, false)
        { }

        public SendPacketsElement(byte[] buffer, int offset, int count, bool endOfPacket)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)offset, (uint)buffer.Length, nameof(offset));
            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)count, (uint)(buffer.Length - offset), nameof(count));

            Initialize(null, null, buffer, buffer.AsMemory(offset, count), offset, count, endOfPacket);
        }

        public SendPacketsElement(ReadOnlyMemory<byte> buffer) :
            this(buffer, endOfPacket: false)
        { }

        public SendPacketsElement(ReadOnlyMemory<byte> buffer, bool endOfPacket)
        {
            Initialize(null, null, null, buffer, 0, buffer.Length, endOfPacket);
        }

        private void Initialize(string? filePath, FileStream? fileStream, byte[]? buffer, ReadOnlyMemory<byte>? memoryBuffer, long offset, int count, bool endOfPacket)
        {
            FilePath = filePath;
            FileStream = fileStream;
            Buffer = buffer;
            MemoryBuffer = memoryBuffer;
            OffsetLong = offset;
            Count = count;
            EndOfPacket = endOfPacket;
        }

        public string? FilePath { get; private set; }

        public FileStream? FileStream { get; private set; }

        public byte[]? Buffer { get; private set; }

        public int Count { get; private set; }

        public ReadOnlyMemory<byte>? MemoryBuffer { get; private set; }

        public int Offset => checked((int)OffsetLong);

        public long OffsetLong { get; private set; }

        public bool EndOfPacket { get; private set; }
    }
}
