// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

namespace System.Net
{
    // Writes DNS query messages into a caller-provided buffer.
    // Only supports writing request messages (header + questions).
    internal ref struct DnsMessageWriter
    {
        private readonly Span<byte> _destination;
        private int _bytesWritten;

        public DnsMessageWriter(Span<byte> destination)
        {
            _destination = destination;
            _bytesWritten = 0;
        }

        public readonly int BytesWritten => _bytesWritten;

        // Writes the 12-byte message header at the current position.
        public bool TryWriteHeader(in DnsMessageHeader header)
        {
            if (!header.TryWrite(_destination[_bytesWritten..]))
            {
                return false;
            }
            _bytesWritten += DnsMessageHeader.Size;
            return true;
        }

        // Writes a question entry: encoded domain name + type + class.
        // Expands compression pointers if present (safe for names from responses).
        public bool TryWriteQuestion(
            scoped DnsEncodedName name,
            DnsRecordType type,
            DnsRecordClass @class = DnsRecordClass.Internet)
        {
            if (!name.TryCopyEncodedTo(_destination[_bytesWritten..], out int nameWritten))
            {
                return false;
            }

            // type (2) + class (2)
            if (_bytesWritten + nameWritten + 4 > _destination.Length)
            {
                return false;
            }
            _bytesWritten += nameWritten;

            BinaryPrimitives.WriteUInt16BigEndian(_destination[_bytesWritten..], (ushort)type);
            _bytesWritten += 2;

            BinaryPrimitives.WriteUInt16BigEndian(_destination[_bytesWritten..], (ushort)@class);
            _bytesWritten += 2;

            return true;
        }
    }
}
