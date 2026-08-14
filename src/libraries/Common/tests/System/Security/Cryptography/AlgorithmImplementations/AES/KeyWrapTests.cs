// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    using Aes = System.Security.Cryptography.Aes;

    // [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    // public sealed class KeyWrapTests_AesCreate_KeyProp : KeyWrapTests
    // {
    //     protected override Aes CreateKey(byte[] key)
    //     {
    //         Aes aes = Aes.Create();
    //         aes.Key = key;
    //         return aes;
    //     }
    // }

    // [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    // public sealed class KeyWrapTests_AesCreate_SetKey : KeyWrapTests
    // {
    //     protected override Aes CreateKey(byte[] key)
    //     {
    //         Aes aes = Aes.Create();
    //         aes.SetKey(key);
    //         return aes;
    //     }
    // }

    // [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    // public static class KeyWrapTests_AesCryptoServiceProvider
    // {
    //     [Fact]
    //     public static void NotValidForAesCsp()
    //     {
    //         byte[] input = new byte[24];

    //         using (Aes aes = new AesCryptoServiceProvider())
    //         {
    //             Assert.Throws<NotSupportedException>(() => aes.EncryptKeyWrap(input));
    //             Assert.Throws<NotSupportedException>(() => aes.DecryptKeyWrap(input));
    //         }
    //     }
    // }

    // [PlatformSpecific(TestPlatforms.Windows)]
    // public sealed class KeyWrapTests_AesCng : KeyWrapTests
    // {
    //     protected override Aes CreateKey(byte[] key)
    //     {
    //         Aes aes = new AesCng();
    //         aes.Key = key;
    //         return aes;
    //     }
    // }

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
                    () => key.DecryptKeyWrap(ReadOnlySpan<byte>.Empty, output));

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

    // public abstract class KeyWrapPaddedTests
    // {
    //     protected abstract Aes CreateKey(byte[] key);

    //     [Theory]
    //     [MemberData(nameof(KnownAnswerTests))]
    //     public void VerifyKnownAnswer(KnownAnswerTest kat)
    //     {
    //         using (Aes key = CreateKey(kat.Key))
    //         {
    //             VerifyWrap(key, kat.Plaintext, kat.Ciphertext);
    //             VerifyUnwrap(key, kat.Ciphertext, kat.Plaintext);
    //         }
    //     }

    //     [Theory]
    //     [InlineData(128, 1, 16)]
    //     [InlineData(128, 96, 103)]
    //     [InlineData(192, 1, 16)]
    //     [InlineData(192, 96, 103)]
    //     [InlineData(256, 1, 16)]
    //     [InlineData(256, 96, 103)]
    //     public void VerifyRoundtrip(int kekSize, int ptMin, int ptMax)
    //     {
    //         byte[] kek = new byte[kekSize / 8];
    //         RandomNumberGenerator.Fill(kek);

    //         using (Aes key = CreateKey(kek))
    //         {
    //             for (int i = ptMin; i <= ptMax; i++)
    //             {
    //                 // Round plaintext up to the nearest multiple of 8,
    //                 // and add the 8 bytes for the IV semi-block.
    //                 int expectedSize = (i + 7) / 8 * 8 + 8;

    //                 byte[] plaintext = new byte[i];
    //                 RandomNumberGenerator.Fill(plaintext);
    //                 byte[] ciphertext = key.EncryptKeyWrapPadded(plaintext);
    //                 Assert.Equal(expectedSize, ciphertext.Length);

    //                 VerifyUnwrap(key, ciphertext, plaintext);
    //                 VerifyWrap(key, plaintext, ciphertext);
    //             }
    //         }
    //     }

    //     [Fact]
    //     public void UnwrapBadIV_SingleBlock()
    //     {
    //         // At the end of unwrap, the header block will have the incorrect value A65959A7.
    //         byte[] kek = "9FC9E4BA68CA3EC8BAC82B02223EADDAAA1A67350E12510D0016083095B32BBC".HexToByteArray();
    //         byte[] ciphertext = "1B2DE25B6990AA8B74087499294ECB39".HexToByteArray();

    //         VerifyUnwrapFails(kek, ciphertext);
    //     }

    //     [Fact]
    //     public void UnwrapBadIV_MultiBlock()
    //     {
    //         // At the end of unwrap, the header block will have the incorrect value A65959A7.
    //         byte[] kek = "B1D18A0296DF025443EE1677ED783FB6C137A98814E09FE1".HexToByteArray();
    //         byte[] ciphertext = (
    //             "67A3F00F801" +
    //             "A1CDAFF2D324C7AC393EB97938556FA8D54C5DB303F9EBB6321B84BCED6DD3A80EC98B3047110" +
    //             "89A8EF9ADADA14A3ADD324E55BEFB6A5598ABB90A40CA8F36CB175498FAA3BDC11FDAC1113042" +
    //             "E3229B790FA4BD0240830933FA9D0C8255CD271D5B7C301DDF85F098C62").HexToByteArray();

    //         VerifyUnwrapFails(kek, ciphertext);
    //     }

    //     [Fact]
    //     public void UnwrapLengthTooBig_Single()
    //     {
    //         // At the end of unwrap, the length segment will report 8 bytes more than the original input was,
    //         // which requires reading beyond the end of the processed buffer.
    //         byte[] kek = "3D7C64D35E1CEC5BBEA04867073F5E9F6DB671B28EA325215FA6DA3B1B561F48".HexToByteArray();
    //         byte[] ciphertext = "9EDBCAFD999E7A1CEEC4529DC192797E".HexToByteArray();

    //         VerifyUnwrapFails(kek, ciphertext);
    //     }

    //     [Fact]
    //     public void UnwrapLengthTooBig_MultiBlock()
    //     {
    //         // At the end of unwrap, the length segment will report 8 bytes more than the original input was,
    //         // which requires reading beyond the end of the processed buffer.
    //         byte[] kek = "3D7C64D35E1CEC5BBEA04867073F5E9F6DB671B28EA325215FA6DA3B1B561F48".HexToByteArray();
    //         byte[] ciphertext = "1A91401A927296BEF253F857C6124B20A2FEFB580FF472F5".HexToByteArray();

    //         VerifyUnwrapFails(kek, ciphertext);
    //     }

    //     [Fact]
    //     public void UnwrapLengthZero_Single()
    //     {
    //         // At the end of unwrap, the length segment will report zero, which is always invalid.
    //         byte[] kek = "B870467A475D675AEE893430A09FD77F".HexToByteArray();
    //         byte[] ciphertext = "024D1848259597D20FFDCE39BC3E461D".HexToByteArray();

    //         VerifyUnwrapFails(kek, ciphertext);
    //     }

    //     [Fact]
    //     public void UnwrapLengthZero_MultiBlock()
    //     {
    //         // At the end of unwrap, the length segment will report zero, which is always invalid.
    //         byte[] kek = "DA536B4D274173D0DAD5DBB8FF21F6E27AC8BB6F9E12F51A".HexToByteArray();
    //         byte[] ciphertext = "29288FFE637F4CCBF3D44FEAB22300C67796C14AAFB682E3".HexToByteArray();

    //         VerifyUnwrapFails(kek, ciphertext);
    //     }

    //     [Fact]
    //     public void UnwrapBadPadding_Single()
    //     {
    //         // At the end of unwrap, some of the "padding" bytes will be non-zero.
    //         byte[] kek = "838E662F79DC11058ED1EC27928DE835119BAC751B689A1DFC09011BD634842E".HexToByteArray();
    //         byte[] ciphertext = "E9D57F431E9A9A2878E6629A890E4C3E".HexToByteArray();

    //         VerifyUnwrapFails(kek, ciphertext);
    //     }

    //     [Fact]
    //     public void UnwrapBadPadding_MultiBlock()
    //     {
    //         // At the end of unwrap, some of the "padding" bytes will be non-zero.
    //         byte[] kek = "6BA88BFEA55ECE448898BFEE524244B965C5EB3CADA463E0".HexToByteArray();
    //         byte[] ciphertext = "852CA39B8A1DE2FD2EF10DA6F01AF860F1DF6E16F0593E85".HexToByteArray();

    //         VerifyUnwrapFails(kek, ciphertext);
    //     }

    //     [Fact]
    //     public void UnwrapLengthTooShort()
    //     {
    //         // At the end of unwrap, this length will report 8 less than the original input was,
    //         // which means the ciphertext should have had one block fewer than it does.
    //         //
    //         // Subtracting 8 from the length of a single-block ciphertext is either zero (already covered),
    //         // or "extremely large" (already covered), so there is not a single-block variant of this.
    //         byte[] kek = "7F0B9A269A182935200F4D92FFE291F94D132D9FBCB8982F".HexToByteArray();
    //         byte[] ciphertext = "9C21E8325D1D7406DE94B2009D3E67152EE6C7DBC0E5B911".HexToByteArray();

    //         VerifyUnwrapFails(kek, ciphertext);
    //     }

    //     private static void VerifyWrap(Aes key, byte[] plaintext, byte[] ciphertext)
    //     {
    //         // EncryptKeyWrapPadded(byte[])
    //         byte[] wrapped = key.EncryptKeyWrapPadded(plaintext);
    //         AssertExtensions.SequenceEqual(ciphertext, wrapped);

    //         // EncryptKeyWrapPadded(ReadOnlySpan<byte>)
    //         wrapped = key.EncryptKeyWrapPadded(new ReadOnlySpan<byte>(plaintext));
    //         AssertExtensions.SequenceEqual(ciphertext, wrapped);

    //         // void EncryptKeyWrapPadded(ReadOnlySpan<byte>, Span<byte>)
    //         Array.Clear(wrapped);
    //         key.EncryptKeyWrapPadded(plaintext, wrapped.AsSpan());
    //         AssertExtensions.SequenceEqual(ciphertext, wrapped);
    //     }

    //     private static void VerifyUnwrap(Aes key, byte[] ciphertext, byte[] plaintext)
    //     {
    //         // DecryptKeyWrapPadded(byte[])
    //         byte[] unwrapped = key.DecryptKeyWrapPadded(ciphertext);
    //         AssertExtensions.SequenceEqual(plaintext, unwrapped);

    //         // DecryptKeyWrapPadded(ReadOnlySpan<byte>)
    //         unwrapped = key.DecryptKeyWrapPadded(new ReadOnlySpan<byte>(ciphertext));
    //         AssertExtensions.SequenceEqual(plaintext, unwrapped);

    //         byte[] tooBig = new byte[ciphertext.Length];
    //         tooBig.AsSpan().Fill(0xFF);
    //         int maxOutput = ciphertext.Length - 8;
    //         int minOutput = maxOutput - 7;
    //         int expectedPadding = maxOutput - plaintext.Length;
    //         ReadOnlySpan<byte> paddingSlice = tooBig.AsSpan(plaintext.Length, expectedPadding);
    //         ReadOnlySpan<byte> untouchedSlice = tooBig.AsSpan(maxOutput);

    //         // The tooBig buffer will be composed of [the right answer][padding][bytes that are not touched]
    //         // Our slice point in this loop is always within [bytes that are not touched], so we expect all
    //         // padding bytes to be written (as 0) and all untouched bytes to remain 0xFF.
    //         for (int i = tooBig.Length; i >= maxOutput; i--)
    //         {
    //             Span<byte> targetSlice = tooBig.AsSpan(0, i);
    //             int written = key.DecryptKeyWrapPadded(ciphertext, targetSlice);
    //             ReadOnlySpan<byte> answerSlice = targetSlice.Slice(0, written);
    //             // SequenceEqual will also check that `written` is correct
    //             AssertExtensions.SequenceEqual(plaintext, answerSlice);

    //             // Since `written` is correct, paddingSlice and untouchedSlice are sliced correctly.
    //             AssertExtensions.TrueExpression(paddingSlice.IndexOfAnyExcept((byte)0) == -1);
    //             AssertExtensions.TrueExpression(untouchedSlice.IndexOfAnyExcept((byte)0xFF) == -1);

    //             // Repeat with TryDecryptKeyWrapPadded
    //             tooBig.AsSpan().Fill(0xFF);
    //             AssertExtensions.TrueExpression(key.TryDecryptKeyWrapPadded(ciphertext, targetSlice, out written));
    //             answerSlice = targetSlice.Slice(0, written);
    //             // SequenceEqual will also check that `written` is correct
    //             AssertExtensions.SequenceEqual(plaintext, answerSlice);

    //             // Since `written` is correct, paddingSlice and untouchedSlice are sliced correctly.
    //             AssertExtensions.TrueExpression(paddingSlice.IndexOfAnyExcept((byte)0) == -1);
    //             AssertExtensions.TrueExpression(untouchedSlice.IndexOfAnyExcept((byte)0xFF) == -1);
    //         }

    //         // In this loop, the input buffer is plausibly big enough, but not guaranteed big enough,
    //         // so the implementation is going to use rented space to compute the unwrap.
    //         //
    //         // Any surplus bytes should be set to 0 (as the padding), and we can still assert that
    //         // the untouched range is 0xFF, but this loop never even sees it.
    //         for (int i = maxOutput - 1; i >= plaintext.Length; i--)
    //         {
    //             Span<byte> targetSlice = tooBig.AsSpan(0, i);
    //             untouchedSlice = targetSlice.Slice(i);

    //             int written = key.DecryptKeyWrapPadded(ciphertext, targetSlice);
    //             ReadOnlySpan<byte> answerSlice = targetSlice.Slice(0, written);
    //             // SequenceEqual will also check that `written` is correct
    //             AssertExtensions.SequenceEqual(plaintext, answerSlice);

    //             paddingSlice = targetSlice.Slice(written);
    //             AssertExtensions.TrueExpression(paddingSlice.IndexOfAnyExcept((byte)0) == -1);
    //             AssertExtensions.TrueExpression(untouchedSlice.IndexOfAnyExcept((byte)0xFF) == -1);

    //             // Repeat with TryDecryptKeyWrapPadded
    //             tooBig.AsSpan().Fill(0xFF);
    //             AssertExtensions.TrueExpression(key.TryDecryptKeyWrapPadded(ciphertext, targetSlice, out written));
    //             answerSlice = targetSlice.Slice(0, written);
    //             // SequenceEqual will also check that `written` is correct
    //             AssertExtensions.SequenceEqual(plaintext, answerSlice);

    //             // Since `written` is correct, paddingSlice and untouchedSlice are still sliced correctly.
    //             AssertExtensions.TrueExpression(paddingSlice.IndexOfAnyExcept((byte)0) == -1);
    //             AssertExtensions.TrueExpression(untouchedSlice.IndexOfAnyExcept((byte)0xFF) == -1);
    //         }

    //         tooBig.AsSpan().Fill(0xFF);

    //         // targetSlice is now too small to hold the plaintext, but that can only be determined after
    //         // running the algorithm.
    //         // In this case, we should never touch the destination buffer.
    //         for (int i = plaintext.Length - 1; i >= minOutput; i--)
    //         {
    //             AssertExtensions.Throws<ArgumentException>(
    //                 "destination",
    //                 () => key.DecryptKeyWrapPadded(ciphertext, tooBig.AsSpan(0, i)));

    //             AssertExtensions.TrueExpression(tooBig.IndexOfAnyExcept((byte)0xFF) == -1);

    //             AssertExtensions.FalseExpression(
    //                 key.TryDecryptKeyWrapPadded(ciphertext, tooBig.AsSpan(0, i), out int written));
    //             Assert.Equal(0, written);

    //             AssertExtensions.TrueExpression(tooBig.IndexOfAnyExcept((byte)0xFF) == -1);
    //         }
    //     }

    //     private void VerifyUnwrapFails(byte[] kek, byte[] ciphertext)
    //     {
    //         using (Aes key = CreateKey(kek))
    //         {
    //             byte[] dest = new byte[ciphertext.Length];

    //             Assert.ThrowsAny<CryptographicException>(() => key.DecryptKeyWrapPadded(ciphertext));
    //             Assert.ThrowsAny<CryptographicException>(() => key.DecryptKeyWrapPadded(new ReadOnlySpan<byte>(ciphertext)));
    //             Assert.ThrowsAny<CryptographicException>(() => key.DecryptKeyWrapPadded(ciphertext, dest));
    //             Assert.ThrowsAny<CryptographicException>(() => key.TryDecryptKeyWrapPadded(ciphertext, dest, out _));
    //         }
    //     }

    //     public static IEnumerable<object[]> KnownAnswerTests { get; } =
    //         [
    //             new object[]
    //             {
    //                 new KnownAnswerTest(
    //                     "RFC 5649 Example 1",
    //                     "5840df6e29b02af1ab493b705bf16ea1ae8338f4dcc176a8".HexToByteArray(),
    //                     "c37b7e6492584340bed12207808941155068f738".HexToByteArray(),
    //                     "138bdeaa9b8fa7fc61f97742e72248ee5ae6ae5360d1ae6a5f54f373fa543b6a".HexToByteArray())
    //             },

    //             new object[]
    //             {
    //                 new KnownAnswerTest(
    //                     "RFC 5649 Example 2",
    //                     "5840df6e29b02af1ab493b705bf16ea1ae8338f4dcc176a8".HexToByteArray(),
    //                     "466f7250617369".HexToByteArray(),
    //                     "afbeb0f07dfbf5419200f2ccb50bb24f".HexToByteArray())
    //             },
    //         ];

    //     public struct KnownAnswerTest
    //     {
    //         public string Name { get; }
    //         public byte[] Key { get; }
    //         public byte[] Plaintext { get; }
    //         public byte[] Ciphertext { get; }

    //         public KnownAnswerTest(string name, byte[] key, byte[] plaintext, byte[] ciphertext)
    //         {
    //             Name = name;
    //             Key = key;
    //             Plaintext = plaintext;
    //             Ciphertext = ciphertext;
    //         }

    //         public override string ToString()
    //         {
    //             return Name;
    //         }
    //     }
    // }
}
