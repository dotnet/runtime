// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using System.Text.Tests;
using Xunit;

namespace System.Text.Encodings.Tests
{
    public class EncodingVirtualTests
    {
        [Theory]
        [MemberData(nameof(UTF8EncodingEncode.Encode_TestData), MemberType = typeof(UTF8EncodingEncode))]
        public void Encode(string chars, int index, int count, byte[] expected) =>
            EncodingHelpers.Encode(new CustomEncoding(), chars, index, count, expected);

        [Theory]
        [MemberData(nameof(UTF8EncodingDecode.Decode_TestData), MemberType = typeof(UTF8EncodingDecode))]
        public void Decode(byte[] bytes, int index, int count, string expected) =>
            EncodingHelpers.Decode(new CustomEncoding(), bytes, index, count, expected);

        // Explicitly not overriding virtual methods to test their base implementations
        private sealed class CustomEncoding : Encoding
        {
            private readonly Encoding _encoding = Encoding.UTF8;

            public override int GetByteCount(char[] chars, int index, int count) =>
                _encoding.GetByteCount(chars, index, count);

            public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) =>
                _encoding.GetBytes(chars, charIndex, charCount, bytes, byteIndex);

            public override int GetCharCount(byte[] bytes, int index, int count) =>
                _encoding.GetCharCount(bytes, index, count);

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) =>
                _encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);

            public override int GetMaxByteCount(int charCount) => throw new System.NotImplementedException();

            public override int GetMaxCharCount(int byteCount) => throw new System.NotImplementedException();
        }
    }
}
