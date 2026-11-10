// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

namespace System.Net
{
    // A parsed question entry from the question section.
    internal readonly ref struct DnsQuestion
    {
        public DnsEncodedName Name { get; }
        public DnsRecordType Type { get; }
        public DnsRecordClass Class { get; }

        internal DnsQuestion(DnsEncodedName name, DnsRecordType type, DnsRecordClass @class)
        {
            Name = name;
            Type = type;
            Class = @class;
        }
    }

    // A parsed resource record from any section (answer, authority, additional).
    internal readonly ref struct DnsRecord
    {
        public DnsEncodedName Name { get; }
        public DnsRecordType Type { get; }
        public DnsRecordClass Class { get; }
        public uint TimeToLive { get; }

        // Raw RDATA bytes.
        public ReadOnlySpan<byte> Data { get; }

        // The full DNS message buffer, for resolving compression pointers in RDATA.
        public ReadOnlySpan<byte> Message { get; }

        // Offset of Data within Message.
        public int DataOffset { get; }

        internal DnsRecord(DnsEncodedName name, DnsRecordType type, DnsRecordClass @class,
            uint ttl, ReadOnlySpan<byte> data, ReadOnlySpan<byte> message, int dataOffset)
        {
            Name = name;
            Type = type;
            Class = @class;
            TimeToLive = ttl;
            Data = data;
            Message = message;
            DataOffset = dataOffset;
        }
    }

    // Reads DNS messages from a buffer. Parses sequentially: header, questions, resource records.
    internal ref struct DnsMessageReader
    {
        private readonly ReadOnlySpan<byte> _message;
        private int _pos;

        public DnsMessageHeader Header { get; }

        private DnsMessageReader(ReadOnlySpan<byte> message, DnsMessageHeader header)
        {
            _message = message;
            _pos = DnsMessageHeader.Size;
            Header = header;
        }

        // Attempts to create a reader over a DNS message. Parses the header eagerly.
        // Returns false if the buffer is too small for a valid header.
        public static bool TryCreate(ReadOnlySpan<byte> message, out DnsMessageReader reader)
        {
            reader = default;

            if (!DnsMessageHeader.TryRead(message, out DnsMessageHeader header))
            {
                return false;
            }

            reader = new DnsMessageReader(message, header);
            return true;
        }

        // Reads the next question from the message.
        public bool TryReadQuestion(out DnsQuestion question)
        {
            question = default;

            if (_pos >= _message.Length)
            {
                return false;
            }

            if (!DnsEncodedName.TryParse(_message, _pos, out DnsEncodedName name, out int nameWireLen))
            {
                return false;
            }
            _pos += nameWireLen;

            // QTYPE (2) + QCLASS (2) = 4 bytes
            if (_pos + 4 > _message.Length)
            {
                return false;
            }

            DnsRecordType type = (DnsRecordType)BinaryPrimitives.ReadUInt16BigEndian(_message[_pos..]);
            _pos += 2;
            DnsRecordClass @class = (DnsRecordClass)BinaryPrimitives.ReadUInt16BigEndian(_message[_pos..]);
            _pos += 2;

            question = new DnsQuestion(name, type, @class);
            return true;
        }

        // Reads the next resource record from the message.
        public bool TryReadRecord(out DnsRecord record)
        {
            record = default;

            if (_pos >= _message.Length)
            {
                return false;
            }

            if (!DnsEncodedName.TryParse(_message, _pos, out DnsEncodedName name, out int nameWireLen))
            {
                return false;
            }
            _pos += nameWireLen;

            // TYPE(2) + CLASS(2) + TTL(4) + RDLENGTH(2) = 10 bytes
            if (_pos + 10 > _message.Length)
            {
                return false;
            }

            DnsRecordType type = (DnsRecordType)BinaryPrimitives.ReadUInt16BigEndian(_message[_pos..]);
            _pos += 2;
            DnsRecordClass @class = (DnsRecordClass)BinaryPrimitives.ReadUInt16BigEndian(_message[_pos..]);
            _pos += 2;
            uint ttl = BinaryPrimitives.ReadUInt32BigEndian(_message[_pos..]);
            _pos += 4;
            ushort rdLength = BinaryPrimitives.ReadUInt16BigEndian(_message[_pos..]);
            _pos += 2;

            int dataOffset = _pos;
            if (dataOffset + rdLength > _message.Length)
            {
                return false;
            }

            ReadOnlySpan<byte> data = _message.Slice(dataOffset, rdLength);
            _pos += rdLength;

            record = new DnsRecord(name, type, @class, ttl, data, _message, dataOffset);
            return true;
        }
    }
}
