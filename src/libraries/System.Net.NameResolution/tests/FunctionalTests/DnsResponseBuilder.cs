// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace System.Net.NameResolution.Tests
{
    // DNS record types used by the loopback test server. Values are the RFC-assigned TYPE codes.
    internal enum DnsRecordType : ushort
    {
        A = 1,
        NS = 2,
        CNAME = 5,
        SOA = 6,
        PTR = 12,
        MX = 15,
        TXT = 16,
        AAAA = 28,
        SRV = 33,
    }

    [Flags]
    internal enum DnsHeaderFlags : ushort
    {
        None = 0,
        Truncation = 0x0200,
        RecursionDesired = 0x0100,
        RecursionAvailable = 0x0080,
    }

    /// <summary>
    /// Fluent builder for constructing raw DNS response byte arrays in tests.
    /// Self-contained: does not depend on any production DNS message types.
    /// </summary>
    internal sealed class DnsResponseBuilder
    {
        private readonly ushort _queryId;
        private readonly byte[] _questionName; // wire-encoded question name (may be empty)
        private readonly DnsRecordType _questionType;

        private DnsResponseCode _rcode;
        private DnsHeaderFlags _extraFlags;

        private List<(byte[]? OwnerName, DnsRecordType Type, uint Ttl, byte[] Rdata)>? _answers;
        private List<(byte[]? OwnerName, DnsRecordType Type, uint Ttl, byte[] Rdata)>? _authority;
        private List<(byte[]? OwnerName, DnsRecordType Type, uint Ttl, byte[] Rdata)>? _additional;

        private int _questionCountOverride = -1;
        private int _answerCountOverride = -1;
        private int _authorityCountOverride = -1;
        private int _additionalCountOverride = -1;
        private bool _skipQuestion;

        private DnsResponseBuilder(ushort queryId, byte[] questionName, DnsRecordType questionType)
        {
            _queryId = queryId;
            _questionName = questionName;
            _questionType = questionType;
        }

        public static DnsResponseBuilder For(ushort queryId, byte[] questionName, DnsRecordType questionType)
            => new DnsResponseBuilder(queryId, questionName, questionType);

        public DnsResponseBuilder ResponseCode(DnsResponseCode rcode)
        {
            _rcode = rcode;
            return this;
        }

        public DnsResponseBuilder Truncated()
        {
            _extraFlags |= DnsHeaderFlags.Truncation;
            return this;
        }

        public DnsResponseBuilder Answer(byte[] rdata, uint ttl = 300)
            => Answer(_questionType, rdata, ttl);

        public DnsResponseBuilder Answer(DnsRecordType type, byte[] rdata, uint ttl = 300)
        {
            _answers ??= new();
            _answers.Add((null, type, ttl, rdata));
            return this;
        }

        public DnsResponseBuilder Answer(string ownerName, DnsRecordType type, byte[] rdata, uint ttl = 300)
        {
            _answers ??= new();
            _answers.Add((EncodeName(ownerName), type, ttl, rdata));
            return this;
        }

        public DnsResponseBuilder Authority(string ownerName, DnsRecordType type, byte[] rdata, uint ttl = 60)
        {
            _authority ??= new();
            _authority.Add((EncodeName(ownerName), type, ttl, rdata));
            return this;
        }

        public DnsResponseBuilder Additional(string ownerName, DnsRecordType type, byte[] rdata, uint ttl = 300)
        {
            _additional ??= new();
            _additional.Add((EncodeName(ownerName), type, ttl, rdata));
            return this;
        }

        public DnsResponseBuilder OverrideQuestionCount(ushort count) { _questionCountOverride = count; return this; }
        public DnsResponseBuilder OverrideAnswerCount(ushort count) { _answerCountOverride = count; return this; }
        public DnsResponseBuilder OverrideAuthorityCount(ushort count) { _authorityCountOverride = count; return this; }
        public DnsResponseBuilder OverrideAdditionalCount(ushort count) { _additionalCountOverride = count; return this; }
        public DnsResponseBuilder SkipQuestion() { _skipQuestion = true; return this; }

        public byte[] Build()
        {
            int answerCount = _answers?.Count ?? 0;
            int authorityCount = _authority?.Count ?? 0;
            int additionalCount = _additional?.Count ?? 0;
            bool writeQuestion = !_skipQuestion && _questionName.Length > 0;

            byte[] buf = new byte[4096];
            int offset = 0;

            // Header
            BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(offset), _queryId);
            ushort flags = (ushort)(0x8000 // QR (response)
                | (ushort)(DnsHeaderFlags.RecursionDesired | DnsHeaderFlags.RecursionAvailable | _extraFlags)
                | ((ushort)_rcode & 0xF));
            BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(offset + 2), flags);
            BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(offset + 4), (ushort)(_questionCountOverride >= 0 ? _questionCountOverride : (writeQuestion ? 1 : 0)));
            BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(offset + 6), (ushort)(_answerCountOverride >= 0 ? _answerCountOverride : answerCount));
            BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(offset + 8), (ushort)(_authorityCountOverride >= 0 ? _authorityCountOverride : authorityCount));
            BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(offset + 10), (ushort)(_additionalCountOverride >= 0 ? _additionalCountOverride : additionalCount));
            offset += 12;

            if (writeQuestion)
            {
                _questionName.CopyTo(buf.AsSpan(offset));
                offset += _questionName.Length;
                BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(offset), (ushort)_questionType);
                BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(offset + 2), 1); // class IN
                offset += 4;
            }

            WriteSection(buf, ref offset, _answers);
            WriteSection(buf, ref offset, _authority);
            WriteSection(buf, ref offset, _additional);

            return buf[..offset];
        }

        private void WriteSection(byte[] buf, ref int offset,
            List<(byte[]? OwnerName, DnsRecordType Type, uint Ttl, byte[] Rdata)>? records)
        {
            if (records is null)
            {
                return;
            }

            foreach ((byte[]? ownerName, DnsRecordType type, uint ttl, byte[] rdata) in records)
            {
                byte[] name = ownerName ?? _questionName;
                name.CopyTo(buf.AsSpan(offset));
                offset += name.Length;
                BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(offset), (ushort)type);
                BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(offset + 2), 1); // class IN
                BinaryPrimitives.WriteUInt32BigEndian(buf.AsSpan(offset + 4), ttl);
                BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(offset + 8), (ushort)rdata.Length);
                offset += 10;
                rdata.CopyTo(buf.AsSpan(offset));
                offset += rdata.Length;
            }
        }

        internal static byte[] EncodeName(string name)
        {
            if (string.IsNullOrEmpty(name) || name == ".")
            {
                return new byte[] { 0 };
            }

            List<byte> bytes = new();
            foreach (string label in name.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                byte[] labelBytes = Encoding.ASCII.GetBytes(label);
                bytes.Add((byte)labelBytes.Length);
                bytes.AddRange(labelBytes);
            }
            bytes.Add(0);
            return bytes.ToArray();
        }

        internal static byte[] BuildSoaRdata(string soaName, uint minTtl)
        {
            byte[] mname = EncodeName("ns." + soaName);
            byte[] rname = EncodeName("admin." + soaName);
            byte[] rdata = new byte[mname.Length + rname.Length + 20];
            mname.CopyTo(rdata, 0);
            rname.CopyTo(rdata, mname.Length);
            int fixedStart = mname.Length + rname.Length;
            BinaryPrimitives.WriteUInt32BigEndian(rdata.AsSpan(fixedStart), 2024010101);     // serial
            BinaryPrimitives.WriteUInt32BigEndian(rdata.AsSpan(fixedStart + 4), 3600);        // refresh
            BinaryPrimitives.WriteUInt32BigEndian(rdata.AsSpan(fixedStart + 8), 900);         // retry
            BinaryPrimitives.WriteUInt32BigEndian(rdata.AsSpan(fixedStart + 12), 604800);     // expire
            BinaryPrimitives.WriteUInt32BigEndian(rdata.AsSpan(fixedStart + 16), minTtl);     // minimum
            return rdata;
        }

        internal static byte[] BuildSrvRdata(ushort priority, ushort weight, ushort port, string target)
        {
            byte[] targetBytes = EncodeName(target);
            byte[] rdata = new byte[6 + targetBytes.Length];
            BinaryPrimitives.WriteUInt16BigEndian(rdata, priority);
            BinaryPrimitives.WriteUInt16BigEndian(rdata.AsSpan(2), weight);
            BinaryPrimitives.WriteUInt16BigEndian(rdata.AsSpan(4), port);
            targetBytes.CopyTo(rdata, 6);
            return rdata;
        }

        internal static byte[] BuildMxRdata(ushort preference, string exchange)
        {
            byte[] exchangeBytes = EncodeName(exchange);
            byte[] rdata = new byte[2 + exchangeBytes.Length];
            BinaryPrimitives.WriteUInt16BigEndian(rdata, preference);
            exchangeBytes.CopyTo(rdata, 2);
            return rdata;
        }

        // TXT RDATA containing one or more character-strings (each length-prefixed).
        internal static byte[] BuildTxtRdata(params string[] values)
        {
            List<byte> rdata = new();
            foreach (string value in values)
            {
                byte[] valueBytes = Encoding.ASCII.GetBytes(value);
                rdata.Add((byte)valueBytes.Length);
                rdata.AddRange(valueBytes);
            }
            return rdata.ToArray();
        }
    }
}
