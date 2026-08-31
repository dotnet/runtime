// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

namespace System.Net
{
    // The fixed 12-byte DNS message header (RFC 1035 §4.1.1).
    internal struct DnsMessageHeader
    {
        public ushort Id { get; set; }
        public bool IsResponse { get; set; }
        public DnsOpCode OpCode { get; set; }
        public DnsHeaderFlags Flags { get; set; }
        public DnsResponseCode ResponseCode { get; set; }
        public ushort QuestionCount { get; set; }
        public ushort AnswerCount { get; set; }
        public ushort AuthorityCount { get; set; }
        public ushort AdditionalCount { get; set; }

        internal const int Size = 12;

        internal bool TryWrite(Span<byte> destination)
        {
            if (destination.Length < Size)
            {
                return false;
            }

            BinaryPrimitives.WriteUInt16BigEndian(destination, Id);
            BinaryPrimitives.WriteUInt16BigEndian(destination[2..], EncodeFlagsWord());
            BinaryPrimitives.WriteUInt16BigEndian(destination[4..], QuestionCount);
            BinaryPrimitives.WriteUInt16BigEndian(destination[6..], AnswerCount);
            BinaryPrimitives.WriteUInt16BigEndian(destination[8..], AuthorityCount);
            BinaryPrimitives.WriteUInt16BigEndian(destination[10..], AdditionalCount);
            return true;
        }

        internal static bool TryRead(ReadOnlySpan<byte> source, out DnsMessageHeader header)
        {
            header = default;
            if (source.Length < Size)
            {
                return false;
            }

            ushort id = BinaryPrimitives.ReadUInt16BigEndian(source);
            ushort flagsWord = BinaryPrimitives.ReadUInt16BigEndian(source[2..]);
            ushort qdCount = BinaryPrimitives.ReadUInt16BigEndian(source[4..]);
            ushort anCount = BinaryPrimitives.ReadUInt16BigEndian(source[6..]);
            ushort nsCount = BinaryPrimitives.ReadUInt16BigEndian(source[8..]);
            ushort arCount = BinaryPrimitives.ReadUInt16BigEndian(source[10..]);

            DecodeFlagsWord(flagsWord, out bool isResponse, out DnsOpCode opCode,
                out DnsHeaderFlags flags, out DnsResponseCode responseCode);

            header = new DnsMessageHeader
            {
                Id = id,
                IsResponse = isResponse,
                OpCode = opCode,
                Flags = flags,
                ResponseCode = responseCode,
                QuestionCount = qdCount,
                AnswerCount = anCount,
                AuthorityCount = nsCount,
                AdditionalCount = arCount,
            };
            return true;
        }

        // RFC 1035 §4.1.1 wire format of the flags word (bytes 2-3):
        //
        //   Bit:  15 14 13 12 11 10  9  8  7  6  5  4  3  2  1  0
        //         QR |  OpCode | AA TC RD RA  Z AD CD |   RCODE   |
        //
        // DnsHeaderFlags enum values are the wire bit positions shifted right by 4,
        // so the enum fits in a byte. Encoding shifts left by 4 to restore wire positions,
        // decoding shifts right by 4. The Z bit (wire bit 6) gap is preserved by the shift.
        // Wire flag bits: AA(10) TC(9) RD(8) RA(7) AD(5) CD(4)
        // Enum bits:      AA(6)  TC(5) RD(4) RA(3) AD(1) CD(0)
        private const int FlagsShift = 4;
        private const ushort WireFlagsMask = 0x07F0; // wire bits 10-7 and 5-4

        private readonly ushort EncodeFlagsWord()
        {
            ushort word = 0;

            if (IsResponse)
            {
                word |= 1 << 15;
            }

            word |= (ushort)(((int)OpCode & 0xF) << 11);
            word |= (ushort)((int)Flags << FlagsShift);
            word |= (ushort)((int)ResponseCode & 0xF);

            return word;
        }

        private static void DecodeFlagsWord(ushort word,
            out bool isResponse, out DnsOpCode opCode,
            out DnsHeaderFlags flags, out DnsResponseCode responseCode)
        {
            isResponse = (word & (1 << 15)) != 0;
            opCode = (DnsOpCode)((word >> 11) & 0xF);
            responseCode = (DnsResponseCode)(word & 0xF);
            flags = (DnsHeaderFlags)((word & WireFlagsMask) >> FlagsShift);
        }
    }
}
