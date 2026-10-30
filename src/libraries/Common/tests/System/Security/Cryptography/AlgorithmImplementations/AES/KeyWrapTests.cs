// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    using Aes = System.Security.Cryptography.Aes;

    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public sealed class KeyWrapTests_AesCreate_KeyProp : KeyWrapTests
    {
        protected override Aes CreateKey(byte[] key)
        {
            Aes aes = Aes.Create();
            aes.Key = key;
            return aes;
        }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public sealed class KeyWrapTests_AesCreate_SetKey : KeyWrapTests
    {
        protected override Aes CreateKey(byte[] key)
        {
            Aes aes = Aes.Create();
            aes.SetKey(key);
            return aes;
        }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public static class KeyWrapTests_AesCryptoServiceProvider
    {
        [Fact]
        public static void NotValidForAesCsp()
        {
            byte[] plaintext = new byte[16];
            byte[] ciphertext = new byte[24];

            using (Aes aes = new AesCryptoServiceProvider())
            {
                Assert.Throws<NotSupportedException>(() => aes.EncryptKeyWrap(plaintext));
                Assert.Throws<NotSupportedException>(() => aes.EncryptKeyWrap(new ReadOnlySpan<byte>(plaintext)));
                Assert.Throws<NotSupportedException>(() => aes.EncryptKeyWrap(plaintext, ciphertext));

                Assert.Throws<NotSupportedException>(() => aes.DecryptKeyWrap(ciphertext));
                Assert.Throws<NotSupportedException>(() => aes.DecryptKeyWrap(new ReadOnlySpan<byte>(ciphertext)));
                Assert.Throws<NotSupportedException>(() => aes.DecryptKeyWrap(ciphertext, plaintext));
                Assert.Throws<NotSupportedException>(() => aes.TryDecryptKeyWrap(ciphertext, plaintext, out _));
            }
        }
    }

    [PlatformSpecific(TestPlatforms.Windows)]
    public sealed class KeyWrapTests_AesCng : KeyWrapTests
    {
        protected override Aes CreateKey(byte[] key)
        {
            Aes aes = new AesCng();
            aes.Key = key;
            return aes;
        }
    }

    public static class KeyWrapContractTests
    {
        [Theory]
        [InlineData(16, 24)]
        [InlineData(0x7FFF_FFF0, 0x7FFF_FFF8)]
        public static void VerifyGetLength(int inputLength, int expectedLength)
        {
            Assert.Equal(expectedLength, Aes.GetKeyWrapLength(inputLength));
        }

        [Fact]
        public static void VerifyGetLength_Random()
        {
            int value = Random.Shared.Next(16, 0x7FFF_FFF1) & ~0b111;
            int actual = Aes.GetKeyWrapLength(value);
            Assert.Equal(value + 8, actual);
        }

        [Fact]
        public static void GetLength_TooLarge()
        {
            int i = int.MaxValue;

            for (; i >= 0x7FFF_FFF1; i--)
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "plaintextLengthInBytes",
                    () => Aes.GetKeyWrapLength(i));
            }

            Assert.Equal(0x7FFF_FFF8, Aes.GetKeyWrapLength(i));
        }

        [Theory]
        [InlineData(15)]
        [InlineData(8)]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public static void GetLengthTooSmall(int len)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "plaintextLengthInBytes",
                () => Aes.GetKeyWrapLength(len));
        }

        [Theory]
        [InlineData(17)]
        [InlineData(19)]
        [InlineData(25)]
        public static void GetLengthNotAlignedToMultiple(int len)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "plaintextLengthInBytes",
                () => Aes.GetKeyWrapLength(len));
        }

        [Fact]
        public static void NeverCalledWithEmpty()
        {
            using (TestAes key = new TestAes())
            {
                byte[] output = new byte[24];

                AssertExtensions.Throws<ArgumentNullException>(
                    "plaintext",
                    () => key.EncryptKeyWrap(null));

                AssertExtensions.Throws<ArgumentException>(
                    "plaintext",
                    () => key.EncryptKeyWrap(Array.Empty<byte>()));

                AssertExtensions.Throws<ArgumentException>(
                    "plaintext",
                    () => key.EncryptKeyWrap(ReadOnlySpan<byte>.Empty));

                AssertExtensions.Throws<ArgumentException>(
                    "plaintext",
                    () => key.EncryptKeyWrap(ReadOnlySpan<byte>.Empty, output));

                AssertExtensions.Throws<ArgumentNullException>(
                    "ciphertext",
                    () => key.DecryptKeyWrap(null));

                AssertExtensions.Throws<ArgumentException>(
                    "ciphertext",
                    () => key.DecryptKeyWrap(Array.Empty<byte>()));

                AssertExtensions.Throws<ArgumentException>(
                    "ciphertext",
                    () => key.DecryptKeyWrap(ReadOnlySpan<byte>.Empty));

                AssertExtensions.Throws<ArgumentException>(
                    "ciphertext",
                    () => key.DecryptKeyWrap(ReadOnlySpan<byte>.Empty, output));

                AssertExtensions.Throws<ArgumentException>(
                    "ciphertext",
                    () => key.TryDecryptKeyWrap(ReadOnlySpan<byte>.Empty, output, out _));

                Assert.Equal(0, key.DecryptKeyWrapCallCount);
            }
        }

        [Fact]
        public static void DecryptNeverCalledWithPartialBlocks()
        {
            byte[] input = new byte[129];
            byte[] buffer = new byte[input.Length];

            using (TestAes key = new TestAes())
            {
                key.DecryptOverride = (source, destination) => source.Length - 8;

                Assert.ThrowsAny<Exception>(() => key.DecryptKeyWrap(Array.Empty<byte>()));
                Assert.Equal(0, key.DecryptKeyWrapCallCount);

                int expectedCallCount = 0;
                const int MinCiphertextLength = 24;

                for (int i = input.Length; i >= 0; i--)
                {
                    if (i % 8 == 0 && i >= MinCiphertextLength)
                    {
                        // Assert.NoThrow
                        key.DecryptKeyWrap(new ReadOnlySpan<byte>(input, 0, i));
                        expectedCallCount++;
                    }
                    else
                    {
                        AssertExtensions.Throws<ArgumentException>(
                            "ciphertext",
                            () => key.DecryptKeyWrap(new ReadOnlySpan<byte>(input, 0, i)));

                        AssertExtensions.Throws<ArgumentException>(
                            "ciphertext",
                            () => key.TryDecryptKeyWrap(new ReadOnlySpan<byte>(input, 0, i), buffer, out _));
                    }

                    Assert.Equal(expectedCallCount, key.DecryptKeyWrapCallCount);
                }
            }
        }

        [Fact]
        public static void EncryptNeverCalledWithPartialBlocks()
        {
            byte[] input = new byte[32];
            byte[] buffer = new byte[input.Length + 8];

            using (TestAes key = new TestAes())
            {
                key.EncryptOverride = (source, destination) => {};


                Assert.Throws<ArgumentException>(() => key.EncryptKeyWrap(new byte[15]));
                Assert.Throws<ArgumentException>(() => key.EncryptKeyWrap(new ReadOnlySpan<byte>(new byte[15])));
                Assert.Throws<ArgumentException>(() => key.EncryptKeyWrap(new ReadOnlySpan<byte>(new byte[15]), buffer));
                Assert.Equal(0, key.EncryptKeyWrapCallCount);

                int expectedCallCount = 0;
                const int MinPlaintextLength = 16;

                for (int i = input.Length; i >= 0; i--)
                {
                    if (i % 8 == 0 && i >= MinPlaintextLength)
                    {
                        // Assert.NoThrow
                        key.EncryptKeyWrap(new ReadOnlySpan<byte>(input, 0, i));
                        expectedCallCount++;
                    }
                    else
                    {
                        AssertExtensions.Throws<ArgumentException>(
                            "plaintext",
                            () => key.EncryptKeyWrap(new ReadOnlySpan<byte>(input, 0, i)));

                        AssertExtensions.Throws<ArgumentException>(
                            "plaintext",
                            () => key.EncryptKeyWrap(new ReadOnlySpan<byte>(input, 0, i), buffer));
                    }

                    Assert.Equal(expectedCallCount, key.EncryptKeyWrapCallCount);
                }
            }
        }

        [Fact]
        public static void DecryptMustReportCorrectLength()
        {
            using (TestAes key = new TestAes())
            {
                byte[] input = new byte[32];
                byte[] output = new byte[24];
                int expectedCallCount = 0;

                foreach (int reportedLength in new[] { 23, 25 })
                {
                    key.DecryptOverride =
                        (source, destination) =>
                        {
                            destination.Fill(0xDD);
                            return reportedLength;
                        };

                    Assert.Throws<CryptographicException>(() => key.DecryptKeyWrap(input));
                    Assert.Equal(++expectedCallCount, key.DecryptKeyWrapCallCount);

                    Assert.Throws<CryptographicException>(
                        () => key.DecryptKeyWrap(new ReadOnlySpan<byte>(input)));

                    Assert.Equal(++expectedCallCount, key.DecryptKeyWrapCallCount);

                    output.AsSpan().Fill(0xFF);
                    Assert.Throws<CryptographicException>(() => key.DecryptKeyWrap(input, output));
                    Assert.Equal(++expectedCallCount, key.DecryptKeyWrapCallCount);
                    AssertExtensions.TrueExpression(output.IndexOfAnyExcept((byte)0) == -1);

                    output.AsSpan().Fill(0xFF);
                    Assert.Throws<CryptographicException>(() => key.TryDecryptKeyWrap(input, output, out _));
                    Assert.Equal(++expectedCallCount, key.DecryptKeyWrapCallCount);
                    AssertExtensions.TrueExpression(output.IndexOfAnyExcept((byte)0) == -1);
                }

                key.DecryptOverride = (source, destination) => source.Length - 8;
                byte[] ret = key.DecryptKeyWrap(input);
                Assert.Equal(24, ret.Length);
                Assert.Equal(++expectedCallCount, key.DecryptKeyWrapCallCount);
            }
        }

        [Fact]
        public static void DecryptClearsDestinationWhenVirtualThrows()
        {
            byte[] input = new byte[24];
            byte[] output = new byte[24];
            const int OutputLength = 16;
            const byte PreFill = 0xB5;

            using (TestAes key = new TestAes())
            {
                key.DecryptOverride =
                    (source, destination) =>
                    {
                        destination.Fill(0xDD);
                        throw new CryptographicException();
                    };

                Array.Fill(output, PreFill);
                Assert.Throws<CryptographicException>(() => key.DecryptKeyWrap(input, output));
                Assert.Equal(1, key.DecryptKeyWrapCallCount);
                AssertExtensions.TrueExpression(output.AsSpan(0, OutputLength).IndexOfAnyExcept((byte)0) == -1);
                AssertExtensions.TrueExpression(output.AsSpan(OutputLength).IndexOfAnyExcept(PreFill) == -1);

                Array.Fill(output, PreFill);
                Assert.Throws<CryptographicException>(() => key.TryDecryptKeyWrap(input, output, out _));
                Assert.Equal(2, key.DecryptKeyWrapCallCount);
                AssertExtensions.TrueExpression(output.AsSpan(0, OutputLength).IndexOfAnyExcept((byte)0) == -1);
                AssertExtensions.TrueExpression(output.AsSpan(OutputLength).IndexOfAnyExcept(PreFill) == -1);
            }
        }

        [Fact]
        public static void EncryptAlwaysSeesSource()
        {
            using (TestAes key = new TestAes())
            {
                byte[] input = new byte[64];
                int callLen = input.Length;

                key.EncryptOverride =
                    (source, destination) =>
                    {
                        AssertExtensions.TrueExpression(source.Overlaps(input));
                    };

                for (; callLen >= 16; callLen -= 8)
                {
                    key.EncryptKeyWrap(input.AsSpan(0, callLen));
                }
            }
        }

        [Fact]
        public static void EncryptNeverSeesInexactDestination()
        {
            using (TestAes key = new TestAes())
            {
                byte[] input = new byte[16];
                byte[] output = new byte[32];
                int expectedCallCount = 0;

                key.EncryptOverride =
                    (source, destination) =>
                    {
                        Assert.Equal(Aes.GetKeyWrapLength(source.Length), destination.Length);
                        AssertExtensions.TrueExpression(destination.Overlaps(output, out int offset));
                        Assert.Equal(0, offset);
                    };

                int correctLength = Aes.GetKeyWrapLength(input.Length);

                for (int i = 0; i <= output.Length; i++)
                {
                    if (i == correctLength)
                    {
                        // Assert.NoThrow
                        key.EncryptKeyWrap(input, output.AsSpan(0, i));
                        Assert.Equal(++expectedCallCount, key.EncryptKeyWrapCallCount);
                    }
                    else
                    {
                        AssertExtensions.Throws<ArgumentException>(
                            "destination",
                            () => key.EncryptKeyWrap(input, output.AsSpan(0, i)));
                        Assert.Equal(expectedCallCount, key.EncryptKeyWrapCallCount);
                    }
                }

                Assert.Equal(1, key.EncryptKeyWrapCallCount);
            }
        }

        [Fact]
        public static void DecryptNeverSeesSmallDestination()
        {
            using (TestAes key = new TestAes())
            {
                byte[] input = new byte[64];
                byte[] output = new byte[64];
                int callLen = output.Length;
                int expectedCallCount = 0;

                key.DecryptOverride =
                    (source, destination) =>
                    {
                        // Since decrypt is unpadded we never need to rent a destination buffer, so we should always
                        // be decrypting into the caller supplied buffer.
                        int outputLength = source.Length - 8;
                        Assert.Equal(outputLength, destination.Length);
                        AssertExtensions.TrueExpression(destination.Overlaps(output, out int offset));
                        Assert.Equal(0, offset);
                        return source.Length - 8;
                    };

                for (; callLen >= input.Length - 8; callLen--)
                {
                    key.DecryptKeyWrap(input, output.AsSpan(0, callLen));
                    Assert.Equal(++expectedCallCount, key.DecryptKeyWrapCallCount);

                    AssertExtensions.TrueExpression(key.TryDecryptKeyWrap(input, output.AsSpan(0, callLen), out _));
                    Assert.Equal(++expectedCallCount, key.DecryptKeyWrapCallCount);
                }

                // Now that callLen is too short, we should get an ArgumentException with no increase in call count.
                AssertExtensions.Throws<ArgumentException>(
                    "destination",
                    () => key.DecryptKeyWrap(input, output.AsSpan(0, callLen)));

                Assert.Equal(expectedCallCount, key.DecryptKeyWrapCallCount);
                AssertExtensions.TrueExpression(expectedCallCount > 0);

                // TryDecrypt doesn't throw, but also doesn't call the virtual
                AssertExtensions.FalseExpression(key.TryDecryptKeyWrap(input, output.AsSpan(0, callLen), out _));
                Assert.Equal(expectedCallCount, key.DecryptKeyWrapCallCount);
            }
        }

        [Fact]
        public static void DecryptCallsVirtualWhenDestinationIsBigEnough()
        {
            using (TestAes key = new TestAes())
            {
                byte[] input = new byte[24];
                byte[] output = new byte[32];
                int retLen = input.Length - 8;
                int expectedCallCount = 0;

                const byte CallFill = 0xDD;
                const byte PreFill = 0xB5;

                key.DecryptOverride =
                    (source, destination) =>
                    {
                        destination.Fill(CallFill);
                        return destination.Length;
                    };

                for (int outputLen = output.Length; outputLen >= 0; outputLen--)
                {
                    int outputOffset = (output.Length - outputLen + 1) / 2;
                    int trimmedLen = int.Min(retLen, outputLen);
                    Span<byte> destination = output.AsSpan(outputOffset, outputLen);

                    ReadOnlySpan<byte> preDest = output.AsSpan(0, outputOffset);
                    ReadOnlySpan<byte> postDest = output.AsSpan(outputOffset + trimmedLen);

                    if (outputLen >= retLen)
                    {
                        Array.Fill(output, PreFill);
                        int ret = key.DecryptKeyWrap(input, destination);
                        Assert.Equal(++expectedCallCount, key.DecryptKeyWrapCallCount);

                        ReadOnlySpan<byte> answer = destination.Slice(0, retLen);

                        AssertExtensions.TrueExpression(answer.IndexOfAnyExcept(CallFill) == -1);
                        AssertExtensions.TrueExpression(preDest.IndexOfAnyExcept(PreFill) == -1);
                        AssertExtensions.TrueExpression(postDest.IndexOfAnyExcept(PreFill) == -1);

                        Array.Fill(output, PreFill);
                        AssertExtensions.TrueExpression(key.TryDecryptKeyWrap(input, destination, out ret));
                        Assert.Equal(++expectedCallCount, key.DecryptKeyWrapCallCount);

                        AssertExtensions.TrueExpression(answer.IndexOfAnyExcept(CallFill) == -1);
                        AssertExtensions.TrueExpression(preDest.IndexOfAnyExcept(PreFill) == -1);
                        AssertExtensions.TrueExpression(postDest.IndexOfAnyExcept(PreFill) == -1);
                    }
                    else
                    {
                        Array.Fill(output, PreFill);

                        AssertExtensions.Throws<ArgumentException>(
                            "destination",
                            () => key.DecryptKeyWrap(input, output.AsSpan(outputOffset, outputLen)));

                        Assert.Equal(expectedCallCount, key.DecryptKeyWrapCallCount);
                        AssertExtensions.TrueExpression(output.IndexOfAnyExcept(PreFill) == -1);

                        Array.Fill(output, PreFill);
                        AssertExtensions.FalseExpression(key.TryDecryptKeyWrap(input, destination, out int ret));
                        Assert.Equal(expectedCallCount, key.DecryptKeyWrapCallCount);
                        AssertExtensions.TrueExpression(output.IndexOfAnyExcept(PreFill) == -1);
                        Assert.Equal(0, ret);
                    }
                }
            }
        }

        [Fact]
        public static void NoOverlapForEncrypt()
        {
            byte[] buffer = new byte[40];

            using (TestAes key = new TestAes())
            {
                AssertExtensions.Throws<CryptographicException>(
                    () => key.EncryptKeyWrap(buffer.AsSpan(24, 16), buffer.AsSpan(1, 24)));

                Assert.Equal(0, key.EncryptKeyWrapCallCount);

                key.EncryptOverride = (source, destination) => { };

                // Adjacent is OK
                key.EncryptKeyWrap(buffer.AsSpan(24, 16), buffer.AsSpan(0, 24));
                Assert.Equal(1, key.EncryptKeyWrapCallCount);
            }
        }

        [Fact]
        public static void NoOverlapForDecrypt()
        {
            byte[] buffer = new byte[40];

            using (TestAes key = new TestAes())
            {
                AssertExtensions.Throws<CryptographicException>(
                    () => key.DecryptKeyWrap(buffer.AsSpan(0, 24), buffer.AsSpan(23, 16)));

                AssertExtensions.Throws<CryptographicException>(
                    () => key.TryDecryptKeyWrap(buffer.AsSpan(0, 24), buffer.AsSpan(23, 16), out _));

                Assert.Equal(0, key.DecryptKeyWrapCallCount);

                key.DecryptOverride = (source, destination) => source.Length - 8;

                // Adjacent is OK
                key.DecryptKeyWrap(buffer.AsSpan(0, 24), buffer.AsSpan(24, 16));
                Assert.Equal(1, key.DecryptKeyWrapCallCount);

                AssertExtensions.TrueExpression(key.TryDecryptKeyWrap(buffer.AsSpan(0, 24), buffer.AsSpan(24, 16), out _));
                Assert.Equal(2, key.DecryptKeyWrapCallCount);
            }
        }

        private class TestAes : Aes
        {
            public delegate void EncryptCallback(ReadOnlySpan<byte> source, Span<byte> destination);
            public delegate int DecryptCallback(ReadOnlySpan<byte> source, Span<byte> destination);

            public int EncryptKeyWrapCallCount { get; private set; }
            public int DecryptKeyWrapCallCount { get; private set; }
            public EncryptCallback EncryptOverride { get; set; }
            public DecryptCallback DecryptOverride { get; set; }

            public override void GenerateIV()
            {
            }

            public override void GenerateKey()
            {
            }

            public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
            {
                Assert.Fail("CreateDecryptor should never be called");
                return null;
            }

            public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
            {
                Assert.Fail("CreateEncryptor should never be called");
                return null;
            }

            protected override void EncryptKeyWrapCore(ReadOnlySpan<byte> source, Span<byte> destination)
            {
                EncryptKeyWrapCallCount++;

                if (EncryptOverride is not null)
                {
                    EncryptOverride(source, destination);
                }
                else
                {
                    Assert.Fail("Unexpected call to EncryptKeyWrapCore");
                }
            }

            protected override int DecryptKeyWrapCore(ReadOnlySpan<byte> source, Span<byte> destination)
            {
                DecryptKeyWrapCallCount++;

                if (DecryptOverride is not null)
                {
                    return DecryptOverride(source, destination);
                }

                Assert.Fail("Unexpected call to EncryptKeyWrapCore");
                return -1;
            }
        }
    }

    public abstract class KeyWrapTests
    {
        protected abstract Aes CreateKey(byte[] key);

        [Theory]
        [MemberData(nameof(KnownAnswerTests))]
        public void VerifyKnownAnswer(KnownAnswerTest kat)
        {
            using (Aes key = CreateKey(kat.Key))
            {
                VerifyWrap(key, kat.Plaintext, kat.Ciphertext);
                VerifyUnwrap(key, kat.Ciphertext, kat.Plaintext);
            }
        }

        [Theory]
        [InlineData(128, 16)]
        [InlineData(128, 96)]
        [InlineData(128, 128)]
        [InlineData(192, 16)]
        [InlineData(192, 96)]
        [InlineData(192, 128)]
        [InlineData(256, 16)]
        [InlineData(256, 96)]
        [InlineData(256, 128)]
        public void VerifyRoundtrip(int kekSize, int plaintextSize)
        {
            byte[] kek = new byte[kekSize / 8];
            RandomNumberGenerator.Fill(kek);

            using (Aes key = CreateKey(kek))
            {
                int expectedSize = plaintextSize + 8;

                byte[] plaintext = new byte[plaintextSize];
                RandomNumberGenerator.Fill(plaintext);
                byte[] ciphertext = key.EncryptKeyWrap(plaintext);
                Assert.Equal(expectedSize, ciphertext.Length);

                VerifyUnwrap(key, ciphertext, plaintext);
                VerifyWrap(key, plaintext, ciphertext);
            }
        }

        [Theory]
        [MemberData(nameof(KnownAnswerTests))]
        public void RejectsTamperedCiphertext(KnownAnswerTest kat)
        {
            byte[] tampered = (byte[])kat.Ciphertext.Clone();
            const byte TamperedBit = 1 << 2;

            for (int i = 0; i < tampered.Length; i++)
            {
                tampered[i] ^= TamperedBit;
                VerifyUnwrapFails(kat.Key, tampered);
                tampered[i] ^= TamperedBit; // Put the tampered bit back so only one bit is tampered at a time.
            }
        }

        [Theory]
        [InlineData("079E449C7E8504B8D559EDA0387724C78820C1E93F4F9716")]
        [InlineData("BAB95D7021F1196EE8BC5146D20167F58362B46EED49CB9E")]
        public void RejectsIncorrectInitialValue(string ciphertextHex)
        {
            // Each of these chosen ciphertexts produces a recovered IV that is off by a single bit, one in the top 32-bit
            // half and the other in the lower 32-bit half (A7A6A6A6A6A6A6A6 and A6A6A6A6A7A6A6A6, respectively).
            byte[] kek = "000102030405060708090A0B0C0D0E0F".HexToByteArray();
            byte[] ciphertext = ciphertextHex.HexToByteArray();

            VerifyUnwrapFails(kek, ciphertext);
        }

        private static void VerifyWrap(Aes key, byte[] plaintext, byte[] ciphertext)
        {
            // EncryptKeyWrap(byte[])
            byte[] wrapped = key.EncryptKeyWrap(plaintext);
            AssertExtensions.SequenceEqual(ciphertext, wrapped);

            // EncryptKeyWrap(ReadOnlySpan<byte>)
            wrapped = key.EncryptKeyWrap(new ReadOnlySpan<byte>(plaintext));
            AssertExtensions.SequenceEqual(ciphertext, wrapped);

            // void EncryptKeyWrap(ReadOnlySpan<byte>, Span<byte>)
            Array.Clear(wrapped);
            key.EncryptKeyWrap(plaintext, wrapped.AsSpan());
            AssertExtensions.SequenceEqual(ciphertext, wrapped);
        }

        private static void VerifyUnwrap(Aes key, byte[] ciphertext, byte[] plaintext)
        {
            // DecryptKeyWrap(byte[])
            byte[] unwrapped = key.DecryptKeyWrap(ciphertext);
            AssertExtensions.SequenceEqual(plaintext, unwrapped);

            // DecryptKeyWrap(ReadOnlySpan<byte>)
            unwrapped = key.DecryptKeyWrap(new ReadOnlySpan<byte>(ciphertext));
            AssertExtensions.SequenceEqual(plaintext, unwrapped);

            // DecryptKeyWrap(ReadOnlySpan<byte>, Span<byte>)
            Array.Clear(unwrapped);
            int written = key.DecryptKeyWrap(new ReadOnlySpan<byte>(ciphertext), unwrapped);
            Assert.Equal(unwrapped.Length, written);
            AssertExtensions.SequenceEqual(plaintext, unwrapped);

            // TryDecryptKeyWrap(ReadOnlySpan<byte>, Span<byte>, out int)
            Array.Clear(unwrapped);
            bool result = key.TryDecryptKeyWrap(new ReadOnlySpan<byte>(ciphertext), unwrapped, out written);
            AssertExtensions.TrueExpression(result);
            Assert.Equal(unwrapped.Length, written);
            AssertExtensions.SequenceEqual(plaintext, unwrapped);
        }

        private void VerifyUnwrapFails(byte[] kek, byte[] ciphertext)
        {
            using (Aes key = CreateKey(kek))
            {
                byte[] dest = new byte[ciphertext.Length];
                int plaintextLength = ciphertext.Length - 8;
                const byte PreFill = 0xB5;

                Assert.ThrowsAny<CryptographicException>(() => key.DecryptKeyWrap(ciphertext));
                Assert.ThrowsAny<CryptographicException>(() => key.DecryptKeyWrap(new ReadOnlySpan<byte>(ciphertext)));

                Array.Fill(dest, PreFill);
                Assert.ThrowsAny<CryptographicException>(() => key.DecryptKeyWrap(ciphertext, dest));
                AssertExtensions.TrueExpression(dest.AsSpan(0, plaintextLength).IndexOfAnyExcept((byte)0) == -1);
                AssertExtensions.TrueExpression(dest.AsSpan(plaintextLength).IndexOfAnyExcept(PreFill) == -1);

                Array.Fill(dest, PreFill);
                Assert.ThrowsAny<CryptographicException>(() => key.TryDecryptKeyWrap(ciphertext, dest, out _));
                AssertExtensions.TrueExpression(dest.AsSpan(0, plaintextLength).IndexOfAnyExcept((byte)0) == -1);
                AssertExtensions.TrueExpression(dest.AsSpan(plaintextLength).IndexOfAnyExcept(PreFill) == -1);
            }
        }

        public static IEnumerable<object[]> KnownAnswerTests { get; } =
            [
                new object[]
                {
                    new KnownAnswerTest(
                        "RFC 3394 4.1",
                        "000102030405060708090A0B0C0D0E0F".HexToByteArray(),
                        "00112233445566778899AABBCCDDEEFF".HexToByteArray(),
                        "1FA68B0A8112B447AEF34BD8FB5A7B829D3E862371D2CFE5".HexToByteArray())
                },

                new object[]
                {
                    new KnownAnswerTest(
                        "RFC 3394 4.2",
                        "000102030405060708090A0B0C0D0E0F1011121314151617".HexToByteArray(),
                        "00112233445566778899AABBCCDDEEFF".HexToByteArray(),
                        "96778B25AE6CA435F92B5B97C050AED2468AB8A17AD84E5D".HexToByteArray())
                },

                new object[]
                {
                    new KnownAnswerTest(
                        "RFC 3394 4.3",
                        "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F".HexToByteArray(),
                        "00112233445566778899AABBCCDDEEFF".HexToByteArray(),
                        "64E8C3F9CE0F5BA263E9777905818A2A93C8191E7D6E8AE7".HexToByteArray())
                },

                new object[]
                {
                    new KnownAnswerTest(
                        "RFC 3394 4.4",
                        "000102030405060708090A0B0C0D0E0F1011121314151617".HexToByteArray(),
                        "00112233445566778899AABBCCDDEEFF0001020304050607".HexToByteArray(),
                        "031D33264E15D33268F24EC260743EDCE1C6C7DDEE725A936BA814915C6762D2".HexToByteArray())
                },

                new object[]
                {
                    new KnownAnswerTest(
                        "RFC 3394 4.5",
                        "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F".HexToByteArray(),
                        "00112233445566778899AABBCCDDEEFF0001020304050607".HexToByteArray(),
                        "A8F9BC1612C68B3FF6E6F4FBE30E71E4769C8B80A32CB8958CD5D17D6B254DA1".HexToByteArray())
                },

                new object[]
                {
                    new KnownAnswerTest(
                        "RFC 3394 4.6",
                        "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F".HexToByteArray(),
                        "00112233445566778899AABBCCDDEEFF000102030405060708090A0B0C0D0E0F".HexToByteArray(),
                        "28C9F404C4B810F4CBCCB35CFB87F8263F5786E2D80ED326CBC7F0E71A99F43BFB988B9B7A02DD21".HexToByteArray())
                },
            ];

        public struct KnownAnswerTest
        {
            public string Name { get; }
            public byte[] Key { get; }
            public byte[] Plaintext { get; }
            public byte[] Ciphertext { get; }

            public KnownAnswerTest(string name, byte[] key, byte[] plaintext, byte[] ciphertext)
            {
                Name = name;
                Key = key;
                Plaintext = plaintext;
                Ciphertext = ciphertext;
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}
