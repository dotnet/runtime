// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Linq;
using Xunit;
using FsCheck.Xunit;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
#if DEBUG
    public partial class CborRoundtripTests
    {
        private const string ReplaySeed = "(0,0)"; // fix for determinism
        private const int MaxTests = 10_000;

        [Property(Replay = ReplaySeed, MaxTest = MaxTests)]
        public static void Int64RoundtripTests(long input)
        {
            using var writer = new CborWriter();
            writer.Write(input);
            byte[] encoding = writer.ToArray();

            var reader = new CborReader(encoding);
            long result = reader.ReadInt64();
            Assert.Equal(input, result);
        }

        [Property(Replay = ReplaySeed, MaxTest = MaxTests)]
        public static void UInt64RoundtripTests(ulong input)
        {
            using var writer = new CborWriter();
            writer.Write(input);
            byte[] encoding = writer.ToArray();

            var reader = new CborReader(encoding);
            ulong result = reader.ReadUInt64();
            Assert.Equal(input, result);
        }

        [Property(Replay = ReplaySeed, MaxTest = MaxTests)]
        public static void ByteStringRoundtripTests(byte[]? input)
        {
            using var writer = new CborWriter();
            writer.Write(input);
            byte[] encoding = writer.ToArray();

            var reader = new CborReader(encoding);
            byte[] result = reader.ReadByteString();
            Assert.Equal(input, result);
        }

        [Property(Replay = ReplaySeed, MaxTest = MaxTests)]
        public static void Utf8StringRoundtripTests(string? input)
        {
            using var writer = new CborWriter();
            writer.Write(input);
            byte[] encoding = writer.ToArray();

            var reader = new CborReader(encoding);
            string result = reader.ReadUtf8String();
            Assert.Equal(input ?? "", result);
        }

        [Property(Replay = ReplaySeed, MaxTest = MaxTests)]
        public static void ByteString_Encoding_ShouldContainInputBytes(byte[]? input)
        {
            using var writer = new CborWriter();
            writer.Write(input);
            byte[] encoding = writer.ToArray();

            int length = input?.Length ?? 0;
            int lengthEncodingLength = GetLengthEncodingLength(length);

            Assert.Equal(lengthEncodingLength + length, encoding.Length);
            Assert.Equal(input ?? Array.Empty<byte>(), encoding.Skip(lengthEncodingLength));

            static int GetLengthEncodingLength(int length)
            {
                return length switch
                {
                    _ when (length < 24) => 1,
                    _ when (length < byte.MaxValue) => 1 + sizeof(byte),
                    _ when (length < ushort.MaxValue) => 1 + sizeof(ushort),
                    _ => 1 + sizeof(uint)
                };
            }
        }
    }
#endif
}
