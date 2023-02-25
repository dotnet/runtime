// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static partial class SP800108HmacCounterKdfTests
    {
        [Fact]
        public static void DeriveBytes_Allocating_ArrayBytes_ArgValidation()
        {
            Assert.Throws<ArgumentNullException>("key", () =>
                SP800108HmacCounterKdf.DeriveBytes(key: (byte[])null, HashAlgorithmName.SHA256, s_labelBytes, s_contextBytes, 42));

            Assert.Throws<ArgumentNullException>("label", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, HashAlgorithmName.SHA256, label: (byte[])null, s_contextBytes, 42));

            Assert.Throws<ArgumentNullException>("context", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, HashAlgorithmName.SHA256, s_labelBytes, context: (byte[])null, 42));

            Assert.Throws<ArgumentNullException>("hashAlgorithm", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, s_nullHash, s_labelBytes, s_contextBytes, 42));

            Assert.Throws<ArgumentException>("hashAlgorithm", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, s_emptyHash, s_labelBytes, s_contextBytes, 42));

            CryptographicException ex = Assert.Throws<CryptographicException>(() =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, s_unknownHash, s_labelBytes, s_contextBytes, 42));
            Assert.Contains(s_unknownHash.Name, ex.Message);

            Assert.Throws<ArgumentOutOfRangeException>("derivedKeyLengthInBytes", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, HashAlgorithmName.SHA256, s_labelBytes, s_contextBytes, -1));

            Assert.Throws<ArgumentOutOfRangeException>("derivedKeyLengthInBytes", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, HashAlgorithmName.SHA256, s_labelBytes, s_contextBytes, 0x20000000));
        }

        [Fact]
        public static void DeriveBytes_Allocating_String_ArgValidation()
        {
            Assert.Throws<ArgumentNullException>("key", () =>
                SP800108HmacCounterKdf.DeriveBytes(key: (byte[])null, HashAlgorithmName.SHA256, Label, Context, 42));

            Assert.Throws<ArgumentNullException>("label", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, HashAlgorithmName.SHA256, label: (string)null, Context, 42));

            Assert.Throws<ArgumentNullException>("context", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, HashAlgorithmName.SHA256, Label, context: (string)null, 42));

            Assert.Throws<ArgumentNullException>("hashAlgorithm", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, s_nullHash, Label, Context, 42));

            Assert.Throws<ArgumentException>("hashAlgorithm", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, s_emptyHash, Label, Context, 42));

            CryptographicException ex = Assert.Throws<CryptographicException>(() =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, s_unknownHash, Label, Context, 42));
            Assert.Contains(s_unknownHash.Name, ex.Message);

            Assert.Throws<ArgumentOutOfRangeException>("derivedKeyLengthInBytes", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, HashAlgorithmName.SHA256, Label, Context, -1));

            Assert.Throws<ArgumentOutOfRangeException>("derivedKeyLengthInBytes", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, HashAlgorithmName.SHA256, Label, Context, 0x20000000));
        }

        [Fact]
        public static void DeriveBytes_Allocating_SpanBytes_ArgValidation()
        {
            Assert.Throws<ArgumentNullException>("hashAlgorithm", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk.AsSpan(), s_nullHash, s_labelBytes.AsSpan(), s_contextBytes.AsSpan(), 42));

            Assert.Throws<ArgumentException>("hashAlgorithm", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk.AsSpan(), s_emptyHash, s_labelBytes.AsSpan(), s_contextBytes.AsSpan(), 42));

            CryptographicException ex = Assert.Throws<CryptographicException>(() =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk.AsSpan(), s_unknownHash, s_labelBytes.AsSpan(), s_contextBytes.AsSpan(), 42));
            Assert.Contains(s_unknownHash.Name, ex.Message);

            Assert.Throws<ArgumentOutOfRangeException>("derivedKeyLengthInBytes", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk.AsSpan(), HashAlgorithmName.SHA256, s_labelBytes.AsSpan(), s_contextBytes.AsSpan(), -1));

            Assert.Throws<ArgumentOutOfRangeException>("derivedKeyLengthInBytes", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk.AsSpan(), HashAlgorithmName.SHA256, s_labelBytes.AsSpan(), s_contextBytes.AsSpan(), 0x20000000));
        }

        [Fact]
        public static void DeriveBytes_BufferFill_SpanBytes_ArgValidation()
        {
            byte[] destination = new byte[42];

            Assert.Throws<ArgumentNullException>("hashAlgorithm", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, s_nullHash, s_labelBytes, s_contextBytes, destination));

            Assert.Throws<ArgumentException>("hashAlgorithm", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, s_emptyHash, s_labelBytes, s_contextBytes, destination));

            CryptographicException ex = Assert.Throws<CryptographicException>(() =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, s_unknownHash, s_labelBytes, s_contextBytes, destination));
            Assert.Contains(s_unknownHash.Name, ex.Message);

            Assert.Throws<ArgumentOutOfRangeException>("destination", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, HashAlgorithmName.SHA256, s_labelBytes, s_contextBytes, GetOversizedSpan()));
        }

        [Fact]
        public static void DeriveBytes_Allocating_SpanChars_ArgValidation()
        {
            Assert.Throws<ArgumentNullException>("hashAlgorithm", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, s_nullHash, Label.AsSpan(), Context.AsSpan(), 42));

            Assert.Throws<ArgumentException>("hashAlgorithm", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, s_emptyHash, Label.AsSpan(), Context.AsSpan(), 42));

            CryptographicException ex = Assert.Throws<CryptographicException>(() =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, s_unknownHash, Label.AsSpan(), Context.AsSpan(), 42));
            Assert.Contains(s_unknownHash.Name, ex.Message);

            Assert.Throws<ArgumentOutOfRangeException>("derivedKeyLengthInBytes", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, HashAlgorithmName.SHA256, Label.AsSpan(), Context.AsSpan(), -1));

            Assert.Throws<ArgumentOutOfRangeException>("derivedKeyLengthInBytes", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, HashAlgorithmName.SHA256, Label.AsSpan(), Context.AsSpan(), 0x20000000));
        }

        [Fact]
        public static void DeriveBytes_BufferFill_SpanChars_ArgValidation()
        {
            byte[] destination = new byte[42];

            Assert.Throws<ArgumentNullException>("hashAlgorithm", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, s_nullHash, Label.AsSpan(), Context.AsSpan(), destination));

            Assert.Throws<ArgumentException>("hashAlgorithm", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, s_emptyHash, Label.AsSpan(), Context.AsSpan(), destination));

            CryptographicException ex = Assert.Throws<CryptographicException>(() =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, s_unknownHash, Label.AsSpan(), Context.AsSpan(), destination));
            Assert.Contains(s_unknownHash.Name, ex.Message);

            Assert.Throws<ArgumentOutOfRangeException>("destination", () =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, HashAlgorithmName.SHA256, Label.AsSpan(), Context.AsSpan(), GetOversizedSpan()));
        }

        [Fact]
        public static void Ctor_KeyArray_ArgValidation()
        {
            Assert.Throws<ArgumentNullException>("key", () =>
                new SP800108HmacCounterKdf((byte[])null, HashAlgorithmName.SHA256));

            Assert.Throws<ArgumentNullException>("hashAlgorithm", () =>
                new SP800108HmacCounterKdf(s_kdk, s_nullHash));

            Assert.Throws<ArgumentException>("hashAlgorithm", () =>
                new SP800108HmacCounterKdf(s_kdk, s_emptyHash));

            CryptographicException ex = Assert.Throws<CryptographicException>(() =>
                new SP800108HmacCounterKdf(s_kdk, s_unknownHash));
            Assert.Contains(s_unknownHash.Name, ex.Message);
        }

        [Fact]
        public static void Ctor_KeySpan_ArgValidation()
        {
            Assert.Throws<ArgumentNullException>("hashAlgorithm", () =>
                new SP800108HmacCounterKdf(s_kdk.AsSpan(), s_nullHash));

            Assert.Throws<ArgumentException>("hashAlgorithm", () =>
                new SP800108HmacCounterKdf(s_kdk.AsSpan(), s_emptyHash));

            CryptographicException ex = Assert.Throws<CryptographicException>(() =>
                new SP800108HmacCounterKdf(s_kdk.AsSpan(), s_unknownHash));
            Assert.Contains(s_unknownHash.Name, ex.Message);
        }

        [Fact]
        public static void DeriveKey_Allocating_ArrayBytes_ArgValidation()
        {
            using SP800108HmacCounterKdf kdf = new SP800108HmacCounterKdf(s_kdk, HashAlgorithmName.SHA256);

            Assert.Throws<ArgumentNullException>("label", () =>
                kdf.DeriveKey((byte[])null, s_contextBytes, 42));

            Assert.Throws<ArgumentNullException>("context", () =>
                kdf.DeriveKey(s_labelBytes, (byte[])null, 42));

            Assert.Throws<ArgumentOutOfRangeException>("derivedKeyLengthInBytes", () =>
                kdf.DeriveKey(s_labelBytes, s_contextBytes, -1));

            Assert.Throws<ArgumentOutOfRangeException>("derivedKeyLengthInBytes", () =>
                kdf.DeriveKey(s_labelBytes, s_contextBytes, 0x20000000));
        }

        [Fact]
        public static void DeriveKey_Allocating_SpanBytes_ArgValidation()
        {
            using SP800108HmacCounterKdf kdf = new SP800108HmacCounterKdf(s_kdk, HashAlgorithmName.SHA256);

            Assert.Throws<ArgumentOutOfRangeException>("derivedKeyLengthInBytes", () =>
                kdf.DeriveKey(s_labelBytes.AsSpan(), s_contextBytes.AsSpan(), -1));

            Assert.Throws<ArgumentOutOfRangeException>("derivedKeyLengthInBytes", () =>
                kdf.DeriveKey(s_labelBytes.AsSpan(), s_contextBytes.AsSpan(), 0x20000000));
        }

        [Fact]
        public static void DeriveKey_BufferFill_SpanBytes_ArgValidation()
        {
            using SP800108HmacCounterKdf kdf = new SP800108HmacCounterKdf(s_kdk, HashAlgorithmName.SHA256);

            Assert.Throws<ArgumentOutOfRangeException>("destination", () =>
                kdf.DeriveKey(s_labelBytes.AsSpan(), s_contextBytes.AsSpan(), GetOversizedSpan()));
        }

        [Fact]
        public static void DeriveKey_Allocating_SpanChars_ArgValidation()
        {
            using SP800108HmacCounterKdf kdf = new SP800108HmacCounterKdf(s_kdk, HashAlgorithmName.SHA256);

            Assert.Throws<ArgumentOutOfRangeException>("derivedKeyLengthInBytes", () =>
                kdf.DeriveKey(Label.AsSpan(), Context.AsSpan(), -1));

            Assert.Throws<ArgumentOutOfRangeException>("derivedKeyLengthInBytes", () =>
                kdf.DeriveKey(Label.AsSpan(), Context.AsSpan(), 0x20000000));
        }

        [Fact]
        public static void DeriveKey_BufferFill_SpanChars_ArgValidation()
        {
            using SP800108HmacCounterKdf kdf = new SP800108HmacCounterKdf(s_kdk, HashAlgorithmName.SHA256);

            Assert.Throws<ArgumentOutOfRangeException>("destination", () =>
                kdf.DeriveKey(Label.AsSpan(), Context.AsSpan(), GetOversizedSpan()));
        }

        [Fact]
        public static void DeriveKey_Allocating_String_ArgValidation()
        {
            using SP800108HmacCounterKdf kdf = new SP800108HmacCounterKdf(s_kdk, HashAlgorithmName.SHA256);

            Assert.Throws<ArgumentOutOfRangeException>("derivedKeyLengthInBytes", () =>
                kdf.DeriveKey(Label, Context, -1));

            Assert.Throws<ArgumentOutOfRangeException>("derivedKeyLengthInBytes", () =>
                kdf.DeriveKey(Label, Context, 0x20000000));
        }

        [Fact]
        public static void DeriveKey_Allocating_String_InvalidUTF8()
        {
            using SP800108HmacCounterKdf kdf = new SP800108HmacCounterKdf(s_kdk, HashAlgorithmName.SHA256);

            Assert.Throws<EncoderFallbackException>(() =>
                kdf.DeriveKey("\uD800", Context, 42));

            Assert.Throws<EncoderFallbackException>(() =>
                kdf.DeriveKey(Label, "\uD800", 42));
        }

        [Fact]
        public static void DeriveKey_BufferFill_SpanChars_InvalidUTF8()
        {
            using SP800108HmacCounterKdf kdf = new SP800108HmacCounterKdf(s_kdk, HashAlgorithmName.SHA256);
            byte[] derivedKey = new byte[42];

            Assert.Throws<EncoderFallbackException>(() =>
                kdf.DeriveKey("\uD800".AsSpan(), Context.AsSpan(), derivedKey));

            Assert.Throws<EncoderFallbackException>(() =>
                kdf.DeriveKey(Label.AsSpan(), "\uD800".AsSpan(), derivedKey));
        }

        [Fact]
        public static void DeriveKey_Allocating_SpanChars_InvalidUTF8()
        {
            using SP800108HmacCounterKdf kdf = new SP800108HmacCounterKdf(s_kdk, HashAlgorithmName.SHA256);

            Assert.Throws<EncoderFallbackException>(() =>
                kdf.DeriveKey("\uD800".AsSpan(), Context.AsSpan(), 42));

            Assert.Throws<EncoderFallbackException>(() =>
                kdf.DeriveKey(Label.AsSpan(), "\uD800".AsSpan(), 42));
        }

        [Fact]
        public static void DeriveBytes_BufferFill_SpanChars_InvalidUTF8()
        {
            byte[] destination = new byte[42];

            Assert.Throws<EncoderFallbackException>(() =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, HashAlgorithmName.SHA256, Label.AsSpan(), "\uD800".AsSpan(), destination));

            Assert.Throws<EncoderFallbackException>(() =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, HashAlgorithmName.SHA256, "\uD800".AsSpan(), Context.AsSpan(), destination));
        }

        [Fact]
        public static void DeriveBytes_Allocating_SpanChars_InvalidUTF8()
        {
            Assert.Throws<EncoderFallbackException>(() =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, HashAlgorithmName.SHA256, Label.AsSpan(), "\uD800".AsSpan(), 42));

            Assert.Throws<EncoderFallbackException>(() =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, HashAlgorithmName.SHA256, "\uD800".AsSpan(), Context.AsSpan(), 42));
        }

        [Fact]
        public static void DeriveBytes_Allocating_String_InvalidUTF8()
        {
            Assert.Throws<EncoderFallbackException>(() =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, HashAlgorithmName.SHA256, Label, "\uD800", 42));

            Assert.Throws<EncoderFallbackException>(() =>
                SP800108HmacCounterKdf.DeriveBytes(s_kdk, HashAlgorithmName.SHA256, "\uD800", Context, 42));
        }

        private unsafe static Span<byte> GetOversizedSpan()
        {
            // This creates an very large span over some address space. The memory in this span should never be read
            // or written to; it should only be used for length checking of the span for argument validation.
            return new Span<byte>((void*)0, 0x20000000);
        }
    }
}
