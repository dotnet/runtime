// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets
{
    // Class that wraps the semantics of a Winsock TRANSMIT_PACKETS_ELEMENTS struct.
    public class SendPacketsElement
    {
        internal string _filePath;
        internal byte[] _buffer;
        internal int _offset;
        internal int _count;
        internal SendPacketsElementFlags _flags;

        // Constructors for file elements.
        public SendPacketsElement(string filepath) :
            this(filepath, 0, 0, false)
        { }

        public SendPacketsElement(string filepath, int offset, int count) :
            this(filepath, offset, count, false)
        { }

        public SendPacketsElement(string filepath, int offset, int count, bool endOfPacket)
        {
            // We will validate if the file exists on send.
            if (filepath == null)
            {
                throw new ArgumentNullException(nameof(filepath));
            }
            // The native API will validate the file length on send.
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            Initialize(filepath, null, offset, count, SendPacketsElementFlags.File, endOfPacket);
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
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (count < 0 || count > (buffer.Length - offset))
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            Initialize(null, buffer, offset, count, SendPacketsElementFlags.Memory, endOfPacket);
        }

        private void Initialize(string filePath, byte[] buffer, int offset, int count,
            SendPacketsElementFlags flags, bool endOfPacket)
        {
            _filePath = filePath;
            _buffer = buffer;
            _offset = offset;
            _count = count;
            _flags = flags;
            if (endOfPacket)
            {
                _flags |= SendPacketsElementFlags.EndOfPacket;
            }
        }

        public string FilePath
        {
            get { return _filePath; }
        }

        public byte[] Buffer
        {
            get { return _buffer; }
        }

        public int Count
        {
            get { return _count; }
        }

        public int Offset
        {
            get { return _offset; }
        }

        public bool EndOfPacket
        {
            get { return (_flags & SendPacketsElementFlags.EndOfPacket) != 0; }
        }
    }
}
