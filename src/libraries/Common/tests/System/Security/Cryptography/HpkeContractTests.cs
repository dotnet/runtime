// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.Tests
{
    public static class HpkeContractTests
    {
        private static readonly HpkeSuite s_suite = new(HpkeKem.MLKEM_768, HpkeKdf.SHAKE256, HpkeAead.AES_128_GCM);

        public static IEnumerable<object[]> Suites()
        {
            foreach (HpkeKem kem in Enum.GetValues(typeof(HpkeKem)))
            foreach (HpkeKdf kdf in Enum.GetValues(typeof(HpkeKdf)))
            foreach (HpkeAead aead in Enum.GetValues(typeof(HpkeAead)))
            {
                yield return new object[] { kem, kdf, aead };
            }
        }

        public static IEnumerable<object[]> KemAlgorithms()
        {
            foreach (HpkeKem kem in Enum.GetValues(typeof(HpkeKem)))
            {
                yield return new object[] { kem };
            }
        }

        [Fact]
        public static void Constructor_NullSuite()
        {
            AssertExtensions.Throws<ArgumentNullException>("suite", () => new HpkeContract(null));
        }

        [Theory]
        [MemberData(nameof(Suites))]
        public static void Constructor_SetsSuite(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            using (HpkeContract hpke = new(suite))
            {
                Assert.Same(suite, hpke.Suite);
            }
        }

        [Fact]
        public static void StaticMethods_NullArguments()
        {
            AssertExtensions.Throws<ArgumentNullException>("suite", () => Hpke.IsSupported(null));
            AssertExtensions.Throws<ArgumentNullException>("suite", () => Hpke.GenerateKey(null));
            AssertExtensions.Throws<ArgumentNullException>("suite", () => Hpke.DeriveKey(null, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentNullException>("suite",
                () => Hpke.DeriveKey(null, ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<ArgumentNullException>("ikm", () => Hpke.DeriveKey(s_suite, (byte[])null));
            AssertExtensions.Throws<ArgumentNullException>("suite",
                () => Hpke.ImportDecapsulationKey(null, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentNullException>("suite",
                () => Hpke.ImportDecapsulationKey(null, ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<ArgumentNullException>("source",
                () => Hpke.ImportDecapsulationKey(s_suite, (byte[])null));
            AssertExtensions.Throws<ArgumentNullException>("suite",
                () => Hpke.ImportEncapsulationKey(null, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentNullException>("suite",
                () => Hpke.ImportEncapsulationKey(null, ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<ArgumentNullException>("source",
                () => Hpke.ImportEncapsulationKey(s_suite, (byte[])null));
        }

        [Theory]
        [MemberData(nameof(KemAlgorithms))]
        public static void ImportKeys_InvalidSize(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.SHAKE256, HpkeAead.AES_128_GCM);

            foreach (int length in new[]
            {
                0,
                suite.DecapsulationKeySizeInBytes - 1,
                suite.DecapsulationKeySizeInBytes + 1
            })
            {
                byte[] source = new byte[length];
                AssertExtensions.Throws<ArgumentException>("source", () => Hpke.ImportDecapsulationKey(suite, source));
                AssertExtensions.Throws<ArgumentException>("source",
                    () => Hpke.ImportDecapsulationKey(suite, source.AsSpan()));
            }

            foreach (int length in new[]
            {
                0,
                suite.EncapsulationKeySizeInBytes - 1,
                suite.EncapsulationKeySizeInBytes + 1
            })
            {
                byte[] source = new byte[length];
                AssertExtensions.Throws<ArgumentException>("source", () => Hpke.ImportEncapsulationKey(suite, source));
                AssertExtensions.Throws<ArgumentException>("source",
                    () => Hpke.ImportEncapsulationKey(suite, source.AsSpan()));
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        public static void Dispose_CallsCoreOnce(int disposeCalls)
        {
            int calls = 0;
            HpkeContract hpke = new(s_suite)
            {
                OnDispose = disposing =>
                {
                    Assert.True(disposing);
                    calls++;
                },
            };

            for (int i = 0; i < disposeCalls; i++)
            {
                hpke.Dispose();
            }

            Assert.Equal(1, calls);
        }

        [Fact]
        public static void Dispose_FailurePropagatesAndDoesNotRepeat()
        {
            InvalidOperationException exception = new();
            int calls = 0;
            HpkeContract hpke = new(s_suite)
            {
                OnDispose = disposing =>
                {
                    Assert.True(disposing);
                    calls++;
                    throw exception;
                },
            };

            Assert.Same(exception, Assert.Throws<InvalidOperationException>(() => hpke.Dispose()));
            hpke.Dispose();
            Assert.Equal(1, calls);

            foreach (Action operation in InstanceOperations(hpke))
            {
                Assert.Throws<ObjectDisposedException>(operation);
            }
        }

        [Fact]
        public static void Disposed_InstanceOperationsDoNotCallCore()
        {
            using (HpkeContract hpke = new(s_suite))
            {
                hpke.Dispose();

                foreach (Action operation in InstanceOperations(hpke))
                {
                    Assert.Throws<ObjectDisposedException>(operation);
                }
            }
        }

        [Theory]
        [MemberData(nameof(KemAlgorithms))]
        public static void ExportKeys_Allocated(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.SHAKE256, HpkeAead.AES_128_GCM);

            using (HpkeContract hpke = new(suite)
            {
                OnExportDecapsulationKeyCore = destination => destination.Fill(0x42),
                OnExportEncapsulationKeyCore = destination => destination.Fill(0xE7),
            })
            {
                byte[] privateKey = hpke.ExportDecapsulationKey();
                byte[] publicKey = hpke.ExportEncapsulationKey();
                Assert.Equal(suite.DecapsulationKeySizeInBytes, privateKey.Length);
                Assert.Equal(suite.EncapsulationKeySizeInBytes, publicKey.Length);
                AssertExtensions.FilledWith<byte>(0x42, privateKey);
                AssertExtensions.FilledWith<byte>(0xE7, publicKey);
                Assert.Equal(1, hpke.ExportDecapsulationKeyCoreCount);
                Assert.Equal(1, hpke.ExportEncapsulationKeyCoreCount);
            }
        }

        [Theory]
        [MemberData(nameof(KemAlgorithms))]
        public static void ExportKeys_Exact(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.SHAKE256, HpkeAead.AES_128_GCM);
            byte[] privateBuffer = Filled(suite.DecapsulationKeySizeInBytes + 2, 0xA5);
            byte[] publicBuffer = Filled(suite.EncapsulationKeySizeInBytes + 2, 0xA5);
            Memory<byte> privateKey = privateBuffer.AsMemory(1, suite.DecapsulationKeySizeInBytes);
            Memory<byte> publicKey = publicBuffer.AsMemory(1, suite.EncapsulationKeySizeInBytes);

            using (HpkeContract hpke = new(suite)
            {
                OnExportDecapsulationKeyCore = destination =>
                {
                    AssertExtensions.Same(privateKey.Span, destination);
                    destination.Fill(0x42);
                },
                OnExportEncapsulationKeyCore = destination =>
                {
                    AssertExtensions.Same(publicKey.Span, destination);
                    destination.Fill(0xE7);
                },
            })
            {
                hpke.ExportDecapsulationKey(privateKey.Span);
                hpke.ExportEncapsulationKey(publicKey.Span);
                AssertGuardedOutput(privateBuffer, 0x42);
                AssertGuardedOutput(publicBuffer, 0xE7);
                Assert.Equal(1, hpke.ExportDecapsulationKeyCoreCount);
                Assert.Equal(1, hpke.ExportEncapsulationKeyCoreCount);
            }
        }

        [Theory]
        [MemberData(nameof(KemAlgorithms))]
        public static void ExportKeys_InvalidSizeBeforeDisposal(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.SHAKE256, HpkeAead.AES_128_GCM);

            foreach (bool disposed in new[] { false, true })
            {
                using (HpkeContract hpke = new(suite))
                {
                    if (disposed)
                    {
                        hpke.Dispose();
                    }

                    foreach (int length in new[]
                    {
                        0,
                        suite.DecapsulationKeySizeInBytes - 1,
                        suite.DecapsulationKeySizeInBytes + 1
                    })
                    {
                        AssertExtensions.Throws<ArgumentException>("destination",
                            () => hpke.ExportDecapsulationKey(new byte[length]));
                    }

                    foreach (int length in new[]
                    {
                        0,
                        suite.EncapsulationKeySizeInBytes - 1,
                        suite.EncapsulationKeySizeInBytes + 1
                    })
                    {
                        AssertExtensions.Throws<ArgumentException>("destination",
                            () => hpke.ExportEncapsulationKey(new byte[length]));
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(Suites))]
        public static void Seal_Allocated(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            foreach (int length in new[] { 0, 1, 32 })
            foreach (bool useSpan in new[] { false, true })
            {
                byte[] plaintext = Filled(length, 0x31);
                byte[] associatedData = [0x51, 0x52, 0x53];
                byte[] info = [0x71, 0x72];

                using (HpkeContract hpke = new(suite)
                {
                    OnSealCore = (p, enc, ct, aad, context) =>
                    {
                        AssertSameBuffer(plaintext, p);
                        AssertSameBuffer(associatedData, aad);
                        AssertSameBuffer(info, context);
                        enc.Fill(0x42);
                        ct.Fill(0xE7);
                    },
                })
                {
                    byte[] encapsulatedSecret;
                    byte[] ciphertext;

                    if (useSpan)
                    {
                        hpke.Seal(
                            plaintext.AsSpan(),
                            out encapsulatedSecret,
                            out ciphertext,
                            associatedData.AsSpan(),
                            info.AsSpan());
                    }
                    else
                    {
                        hpke.Seal(plaintext, out encapsulatedSecret, out ciphertext, associatedData, info);
                    }

                    Assert.Equal(suite.EncapsulatedSecretSizeInBytes, encapsulatedSecret.Length);
                    Assert.Equal(suite.GetCiphertextLength(length), ciphertext.Length);
                    AssertExtensions.FilledWith<byte>(0x42, encapsulatedSecret);
                    AssertExtensions.FilledWith<byte>(0xE7, ciphertext);
                    AssertExtensions.FilledWith<byte>(0x31, plaintext);
                    Assert.Equal(new byte[] { 0x51, 0x52, 0x53 }, associatedData);
                    Assert.Equal(new byte[] { 0x71, 0x72 }, info);
                    Assert.Equal(1, hpke.SealCoreCount);
                }
            }
        }

        [Theory]
        [MemberData(nameof(Suites))]
        public static void Seal_Exact(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            foreach (int length in new[] { 0, 1, 32 })
            {
                byte[] plaintext = Filled(length, 0x31);
                byte[] associatedData = [0x51, 0x52, 0x53];
                byte[] info = [0x71, 0x72];
                byte[] encBuffer = Filled(suite.EncapsulatedSecretSizeInBytes + 2, 0xA5);
                byte[] ctBuffer = Filled(suite.GetCiphertextLength(length) + 2, 0xA5);
                Memory<byte> encapsulatedSecret = encBuffer.AsMemory(1, encBuffer.Length - 2);
                Memory<byte> ciphertext = ctBuffer.AsMemory(1, ctBuffer.Length - 2);

                using (HpkeContract hpke = new(suite)
                {
                    OnSealCore = (p, enc, ct, aad, context) =>
                    {
                        AssertSameBuffer(plaintext, p);
                        AssertSameBuffer(associatedData, aad);
                        AssertSameBuffer(info, context);
                        AssertExtensions.Same(encapsulatedSecret.Span, enc);
                        AssertExtensions.Same(ciphertext.Span, ct);
                        enc.Fill(0x42);
                        ct.Fill(0xE7);
                    },
                })
                {
                    hpke.Seal(plaintext, encapsulatedSecret.Span, ciphertext.Span, associatedData, info);
                    AssertGuardedOutput(encBuffer, 0x42);
                    AssertGuardedOutput(ctBuffer, 0xE7);
                    AssertExtensions.FilledWith<byte>(0x31, plaintext);
                    Assert.Equal(1, hpke.SealCoreCount);
                }
            }
        }

        [Theory]
        [MemberData(nameof(KemAlgorithms))]
        public static void Seal_InvalidOutputSizesBeforeDisposal(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.SHAKE256, HpkeAead.AES_128_GCM);
            byte[] plaintext = new byte[32];
            byte[] ciphertext = new byte[suite.GetCiphertextLength(plaintext.Length)];
            byte[] encapsulatedSecret = new byte[suite.EncapsulatedSecretSizeInBytes];

            foreach (bool disposed in new[] { false, true })
            {
                using (HpkeContract hpke = new(suite))
                {
                    if (disposed)
                    {
                        hpke.Dispose();
                    }

                    foreach (int length in new[] { 0, encapsulatedSecret.Length - 1, encapsulatedSecret.Length + 1 })
                    {
                        AssertExtensions.Throws<ArgumentException>("encapsulatedSecret",
                            () => hpke.Seal(plaintext, new byte[length], ciphertext));
                    }

                    foreach (int length in new[] { 0, ciphertext.Length - 1, ciphertext.Length + 1 })
                    {
                        AssertExtensions.Throws<ArgumentException>("ciphertext",
                            () => hpke.Seal(plaintext, encapsulatedSecret, new byte[length]));
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(Suites))]
        public static void Open_Allocated(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            foreach (int length in new[] { 0, 1, 32 })
            foreach (bool useSpan in new[] { false, true })
            {
                byte[] encapsulatedSecret = Filled(suite.EncapsulatedSecretSizeInBytes, 0x31);
                byte[] ciphertext = Filled(suite.GetCiphertextLength(length), 0x41);
                byte[] associatedData = [0x51, 0x52, 0x53];
                byte[] info = [0x71, 0x72];

                using (HpkeContract hpke = new(suite)
                {
                    OnOpenCore = (enc, ct, p, aad, context) =>
                    {
                        AssertSameBuffer(encapsulatedSecret, enc);
                        AssertSameBuffer(ciphertext, ct);
                        AssertSameBuffer(associatedData, aad);
                        AssertSameBuffer(info, context);
                        p.Fill(0xE7);
                    },
                })
                {
                    byte[] plaintext = useSpan
                        ? hpke.Open(
                            encapsulatedSecret.AsSpan(),
                            ciphertext.AsSpan(),
                            associatedData: associatedData.AsSpan(),
                            info: info.AsSpan())
                        : hpke.Open(encapsulatedSecret, ciphertext, associatedData: associatedData, info: info);
                    Assert.Equal(length, plaintext.Length);
                    AssertExtensions.FilledWith<byte>(0xE7, plaintext);
                    AssertExtensions.FilledWith<byte>(0x31, encapsulatedSecret);
                    AssertExtensions.FilledWith<byte>(0x41, ciphertext);
                    Assert.Equal(new byte[] { 0x51, 0x52, 0x53 }, associatedData);
                    Assert.Equal(new byte[] { 0x71, 0x72 }, info);
                    Assert.Equal(1, hpke.OpenCoreCount);
                }
            }
        }

        [Theory]
        [MemberData(nameof(Suites))]
        public static void Open_Exact(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            foreach (int length in new[] { 0, 1, 32 })
            {
                byte[] encapsulatedSecret = Filled(suite.EncapsulatedSecretSizeInBytes, 0x31);
                byte[] ciphertext = Filled(suite.GetCiphertextLength(length), 0x41);
                byte[] associatedData = [0x51, 0x52, 0x53];
                byte[] info = [0x71, 0x72];
                byte[] buffer = Filled(length + 2, 0xA5);
                Memory<byte> plaintext = buffer.AsMemory(1, length);

                using (HpkeContract hpke = new(suite)
                {
                    OnOpenCore = (enc, ct, p, aad, context) =>
                    {
                        AssertSameBuffer(encapsulatedSecret, enc);
                        AssertSameBuffer(ciphertext, ct);
                        AssertSameBuffer(associatedData, aad);
                        AssertSameBuffer(info, context);
                        AssertSameBuffer(plaintext.Span, p);
                        p.Fill(0xE7);
                    },
                })
                {
                    hpke.Open(encapsulatedSecret, ciphertext, plaintext.Span, associatedData, info);
                    AssertGuardedOutput(buffer, 0xE7);
                    Assert.Equal(1, hpke.OpenCoreCount);
                }
            }
        }

        [Theory]
        [MemberData(nameof(KemAlgorithms))]
        public static void Open_InvalidSizesBeforeDisposal(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.SHAKE256, HpkeAead.AES_128_GCM);
            byte[] encapsulatedSecret = new byte[suite.EncapsulatedSecretSizeInBytes];
            byte[] ciphertext = new byte[suite.GetCiphertextLength(32)];
            byte[] plaintext = new byte[32];

            foreach (bool disposed in new[] { false, true })
            {
                using (HpkeContract hpke = new(suite))
                {
                    if (disposed)
                    {
                        hpke.Dispose();
                    }

                    foreach (int length in new[] { 0, encapsulatedSecret.Length - 1, encapsulatedSecret.Length + 1 })
                    {
                        byte[] invalid = new byte[length];
                        AssertExtensions.Throws<ArgumentException>("encapsulatedSecret",
                            () => hpke.Open(invalid, ciphertext));
                        AssertExtensions.Throws<ArgumentException>("encapsulatedSecret",
                            () => hpke.Open(invalid.AsSpan(), ciphertext.AsSpan()));
                        AssertExtensions.Throws<ArgumentException>("encapsulatedSecret",
                            () => hpke.Open(invalid, ciphertext, plaintext.AsSpan()));
                    }

                    foreach (int length in new[] { 0, suite.AeadTagSizeInBytes - 1 })
                    {
                        byte[] invalid = new byte[length];
                        AssertExtensions.Throws<ArgumentException>("ciphertext",
                            () => hpke.Open(encapsulatedSecret, invalid));
                        AssertExtensions.Throws<ArgumentException>("ciphertext",
                            () => hpke.Open(encapsulatedSecret.AsSpan(), invalid.AsSpan()));
                        AssertExtensions.Throws<ArgumentException>("ciphertext",
                            () => hpke.Open(encapsulatedSecret, invalid, plaintext.AsSpan()));
                    }

                    foreach (int length in new[] { 0, plaintext.Length - 1, plaintext.Length + 1 })
                    {
                        AssertExtensions.Throws<ArgumentException>("plaintext",
                            () => hpke.Open(encapsulatedSecret, ciphertext, new byte[length].AsSpan()));
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(Suites))]
        public static void CreateSender_AllocatedAndExact(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);
            byte[] info = [0x71, 0x72];
            byte[] buffer = Filled(suite.EncapsulatedSecretSizeInBytes + 2, 0xA5);
            Memory<byte> destination = buffer.AsMemory(1, buffer.Length - 2);

            using (ReturnedSender expected = new(suite))
            using (HpkeContract hpke = new(suite))
            {
                hpke.OnCreateSenderCore = (enc, context) =>
                {
                    AssertSameBuffer(info, context);

                    if (hpke.CreateSenderCoreCount == 2)
                    {
                        AssertExtensions.Same(destination.Span, enc);
                    }

                    enc.Fill(0x42);
                    return expected;
                };

                Assert.Same(expected, hpke.CreateSender(out byte[] encapsulatedSecret, info));
                Assert.Equal(suite.EncapsulatedSecretSizeInBytes, encapsulatedSecret.Length);
                AssertExtensions.FilledWith<byte>(0x42, encapsulatedSecret);
                Assert.Same(expected, hpke.CreateSender(destination.Span, info));
                AssertGuardedOutput(buffer, 0x42);
                Assert.Equal(2, hpke.CreateSenderCoreCount);
                hpke.Dispose();
                Assert.False(expected.Disposed);
            }
        }

        [Theory]
        [MemberData(nameof(Suites))]
        public static void CreateRecipient_ArrayAndSpan(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);
            byte[] encapsulatedSecret = Filled(suite.EncapsulatedSecretSizeInBytes, 0x31);
            byte[] info = [0x71, 0x72];

            using (ReturnedRecipient expected = new(suite))
            using (HpkeContract hpke = new(suite)
            {
                OnCreateRecipientCore = (enc, context) =>
                {
                    AssertSameBuffer(encapsulatedSecret, enc);
                    AssertSameBuffer(info, context);
                    return expected;
                },
            })
            {
                Assert.Same(expected, hpke.CreateRecipient(encapsulatedSecret, info));
                Assert.Same(expected, hpke.CreateRecipient(encapsulatedSecret.AsSpan(), info.AsSpan()));
                AssertExtensions.FilledWith<byte>(0x31, encapsulatedSecret);
                Assert.Equal(2, hpke.CreateRecipientCoreCount);
                hpke.Dispose();
                Assert.False(expected.Disposed);
            }
        }

        [Theory]
        [MemberData(nameof(Suites))]
        public static void CreatePskSender_AllocatedAndExact(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);
            byte[] psk = Filled(32, 0x31);
            byte[] pskId = [0x51, 0x52, 0x53];
            byte[] info = [0x71, 0x72];
            byte[] buffer = Filled(suite.EncapsulatedSecretSizeInBytes + 2, 0xA5);
            Memory<byte> destination = buffer.AsMemory(1, buffer.Length - 2);

            using (ReturnedSender expected = new(suite))
            using (HpkeContract hpke = new(suite))
            {
                hpke.OnCreatePskSenderCore = (enc, context, key, id) =>
                {
                    AssertSameBuffer(psk, key);
                    AssertSameBuffer(pskId, id);
                    AssertSameBuffer(info, context);

                    if (hpke.CreatePskSenderCoreCount == 3)
                    {
                        AssertExtensions.Same(destination.Span, enc);
                    }

                    enc.Fill(0x42);
                    return expected;
                };

                Assert.Same(expected, hpke.CreatePskSender(psk, pskId, out byte[] arrayEnc, info));
                Assert.Same(expected, hpke.CreatePskSender(
                    psk.AsSpan(),
                    pskId.AsSpan(),
                    out byte[] spanEnc,
                    info.AsSpan()));
                Assert.Equal(suite.EncapsulatedSecretSizeInBytes, arrayEnc.Length);
                Assert.Equal(suite.EncapsulatedSecretSizeInBytes, spanEnc.Length);
                AssertExtensions.FilledWith<byte>(0x42, arrayEnc);
                AssertExtensions.FilledWith<byte>(0x42, spanEnc);
                Assert.Same(expected, hpke.CreatePskSender(psk, pskId, destination.Span, info));
                AssertGuardedOutput(buffer, 0x42);
                AssertExtensions.FilledWith<byte>(0x31, psk);
                Assert.Equal(3, hpke.CreatePskSenderCoreCount);
                hpke.Dispose();
                Assert.False(expected.Disposed);
            }
        }

        [Theory]
        [MemberData(nameof(Suites))]
        public static void CreatePskRecipient_ArrayAndSpan(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);
            byte[] encapsulatedSecret = Filled(suite.EncapsulatedSecretSizeInBytes, 0x21);
            byte[] psk = Filled(32, 0x31);
            byte[] pskId = [0x51, 0x52, 0x53];
            byte[] info = [0x71, 0x72];

            using (ReturnedRecipient expected = new(suite))
            using (HpkeContract hpke = new(suite)
            {
                OnCreatePskRecipientCore = (enc, context, key, id) =>
                {
                    AssertSameBuffer(encapsulatedSecret, enc);
                    AssertSameBuffer(psk, key);
                    AssertSameBuffer(pskId, id);
                    AssertSameBuffer(info, context);
                    return expected;
                },
            })
            {
                Assert.Same(expected, hpke.CreatePskRecipient(encapsulatedSecret, psk, pskId, info));
                Assert.Same(expected, hpke.CreatePskRecipient(
                    encapsulatedSecret.AsSpan(),
                    psk.AsSpan(),
                    pskId.AsSpan(),
                    info.AsSpan()));
                AssertExtensions.FilledWith<byte>(0x21, encapsulatedSecret);
                AssertExtensions.FilledWith<byte>(0x31, psk);
                Assert.Equal(2, hpke.CreatePskRecipientCoreCount);
                hpke.Dispose();
                Assert.False(expected.Disposed);
            }
        }

        [Theory]
        [MemberData(nameof(KemAlgorithms))]
        public static void ContextFactories_InvalidEncapsulationSizeBeforeDisposal(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.SHAKE256, HpkeAead.AES_128_GCM);
            byte[] psk = new byte[32];
            byte[] pskId = [1];

            foreach (bool disposed in new[] { false, true })
            {
                using (HpkeContract hpke = new(suite))
                {
                    if (disposed)
                    {
                        hpke.Dispose();
                    }

                    foreach (int length in new[]
                    {
                        0,
                        suite.EncapsulatedSecretSizeInBytes - 1,
                        suite.EncapsulatedSecretSizeInBytes + 1
                    })
                    {
                        byte[] invalid = new byte[length];
                        AssertExtensions.Throws<ArgumentException>("encapsulatedSecret",
                            () => hpke.CreateSender(invalid.AsSpan()));
                        AssertExtensions.Throws<ArgumentException>("encapsulatedSecret",
                            () => hpke.CreateRecipient(invalid));
                        AssertExtensions.Throws<ArgumentException>("encapsulatedSecret",
                            () => hpke.CreateRecipient(invalid.AsSpan()));
                        AssertExtensions.Throws<ArgumentException>("encapsulatedSecret",
                            () => hpke.CreatePskSender(psk, pskId, invalid.AsSpan()));
                        AssertExtensions.Throws<ArgumentException>("encapsulatedSecret",
                            () => hpke.CreatePskRecipient(invalid, psk, pskId));
                        AssertExtensions.Throws<ArgumentException>("encapsulatedSecret",
                            () => hpke.CreatePskRecipient(invalid.AsSpan(), psk.AsSpan(), pskId.AsSpan()));
                    }
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void NullArgumentsBeforeDisposal(bool disposed)
        {
            byte[] encapsulatedSecret = new byte[s_suite.EncapsulatedSecretSizeInBytes];
            byte[] ciphertext = new byte[s_suite.AeadTagSizeInBytes];
            byte[] psk = new byte[32];
            byte[] pskId = [1];

            using (HpkeContract hpke = new(s_suite))
            {
                if (disposed)
                {
                    hpke.Dispose();
                }

                AssertExtensions.Throws<ArgumentNullException>("plaintext",
                    () => hpke.Seal((byte[])null, out _, out _));
                AssertExtensions.Throws<ArgumentNullException>("encapsulatedSecret",
                    () => hpke.Open((byte[])null, ciphertext));
                AssertExtensions.Throws<ArgumentNullException>("ciphertext",
                    () => hpke.Open(encapsulatedSecret, (byte[])null));
                AssertExtensions.Throws<ArgumentNullException>("encapsulatedSecret",
                    () => hpke.CreateRecipient((byte[])null));
                AssertExtensions.Throws<ArgumentNullException>("psk",
                    () => hpke.CreatePskSender((byte[])null, pskId, out _));
                AssertExtensions.Throws<ArgumentNullException>("pskId",
                    () => hpke.CreatePskSender(psk, (byte[])null, out _));
                AssertExtensions.Throws<ArgumentNullException>("encapsulatedSecret",
                    () => hpke.CreatePskRecipient((byte[])null, psk, pskId));
                AssertExtensions.Throws<ArgumentNullException>("psk",
                    () => hpke.CreatePskRecipient(encapsulatedSecret, (byte[])null, pskId));
                AssertExtensions.Throws<ArgumentNullException>("pskId",
                    () => hpke.CreatePskRecipient(encapsulatedSecret, psk, (byte[])null));
            }
        }

        [Theory]
        [InlineData(HpkeKdf.HKDF_SHA256)]
        [InlineData(HpkeKdf.HKDF_SHA384)]
        [InlineData(HpkeKdf.HKDF_SHA512)]
        [InlineData(HpkeKdf.SHAKE128)]
        [InlineData(HpkeKdf.SHAKE256)]
        public static void InfoLength_Boundaries(HpkeKdf kdf)
        {
            HpkeSuite suite = new(HpkeKem.MLKEM_768, kdf, HpkeAead.AES_128_GCM);

            using (HpkeContract hpke = CreateContextContract(suite))
            {
                foreach (int length in new[] { 0, 64, ushort.MaxValue })
                {
                    foreach (Action operation in ContextOperations(hpke, new byte[length]))
                    {
                        operation();
                    }
                }

                if (!HpkeContract.HasInputLengthLimit(kdf))
                {
                    foreach (Action operation in ContextOperations(hpke, new byte[ushort.MaxValue + 1]))
                    {
                        operation();
                    }
                }
            }
        }

        [Theory]
        [InlineData(HpkeKdf.SHAKE128, false)]
        [InlineData(HpkeKdf.SHAKE128, true)]
        [InlineData(HpkeKdf.SHAKE256, false)]
        [InlineData(HpkeKdf.SHAKE256, true)]
        public static void InfoLength_InvalidBeforeDisposal(HpkeKdf kdf, bool disposed)
        {
            using (HpkeContract hpke = new(new HpkeSuite(HpkeKem.MLKEM_768, kdf, HpkeAead.AES_128_GCM)))
            {
                if (disposed)
                {
                    hpke.Dispose();
                }

                foreach (Action operation in ContextOperations(hpke, new byte[ushort.MaxValue + 1]))
                {
                    AssertExtensions.Throws<ArgumentException>("info", operation);
                }
            }
        }

        [Theory]
        [InlineData(HpkeKdf.HKDF_SHA256)]
        [InlineData(HpkeKdf.HKDF_SHA384)]
        [InlineData(HpkeKdf.HKDF_SHA512)]
        [InlineData(HpkeKdf.SHAKE128)]
        [InlineData(HpkeKdf.SHAKE256)]
        public static void PskInputs_Boundaries(HpkeKdf kdf)
        {
            HpkeSuite suite = new(HpkeKem.MLKEM_768, kdf, HpkeAead.AES_128_GCM);

            using (HpkeContract hpke = new(suite)
            {
                OnCreatePskSenderCore = (enc, info, psk, id) => new ReturnedSender(suite),
                OnCreatePskRecipientCore = (enc, info, psk, id) => new ReturnedRecipient(suite),
            })
            {
                foreach ((int keyLength, int idLength) in new[]
                {
                    (32, 1),
                    (33, 2),
                    (ushort.MaxValue, ushort.MaxValue)
                })
                {
                    foreach (Action operation in PskOperations(
                        hpke, new byte[keyLength], new byte[idLength], Array.Empty<byte>()))
                    {
                        operation();
                    }
                }

                if (!HpkeContract.HasInputLengthLimit(kdf))
                {
                    foreach (Action operation in PskOperations(
                        hpke,
                        new byte[ushort.MaxValue + 1],
                        new byte[ushort.MaxValue + 1],
                        Array.Empty<byte>()))
                    {
                        operation();
                    }
                }
            }
        }

        public static IEnumerable<object[]> InvalidPskInputs()
        {
            foreach (HpkeKdf kdf in Enum.GetValues(typeof(HpkeKdf)))
            {
                yield return new object[] { kdf, 0, 1, "psk" };
                yield return new object[] { kdf, 31, 1, "psk" };
                yield return new object[] { kdf, 32, 0, "pskId" };

                if (HpkeContract.HasInputLengthLimit(kdf))
                {
                    yield return new object[] { kdf, 65536, 1, "psk" };
                    yield return new object[] { kdf, 32, 65536, "pskId" };
                }
            }
        }

        [Theory]
        [MemberData(nameof(InvalidPskInputs))]
        public static void PskInputs_InvalidBeforeDisposal(
            HpkeKdf kdf,
            int keyLength,
            int idLength,
            string parameterName)
        {
            foreach (bool disposed in new[] { false, true })
            {
                using (HpkeContract hpke = new(new HpkeSuite(HpkeKem.MLKEM_768, kdf, HpkeAead.AES_128_GCM)))
                {
                    if (disposed)
                    {
                        hpke.Dispose();
                    }

                    foreach (Action operation in PskOperations(
                        hpke, new byte[keyLength], new byte[idLength], Array.Empty<byte>()))
                    {
                        AssertExtensions.Throws<ArgumentException>(parameterName, operation);
                    }
                }
            }
        }

        [Fact]
        public static void OptionalArguments_AreEmpty()
        {
            byte[] enc = new byte[s_suite.EncapsulatedSecretSizeInBytes];
            byte[] ct = new byte[s_suite.AeadTagSizeInBytes];
            byte[] psk = new byte[32];
            byte[] pskId = [1];

            using (HpkeContract hpke = new(s_suite)
            {
                OnSealCore = (p, e, c, aad, info) =>
                {
                    Assert.True(p.IsEmpty);
                    Assert.True(aad.IsEmpty);
                    Assert.True(info.IsEmpty);
                },
                OnOpenCore = (e, c, p, aad, info) =>
                {
                    Assert.True(p.IsEmpty);
                    Assert.True(aad.IsEmpty);
                    Assert.True(info.IsEmpty);
                },
                OnCreateSenderCore = (e, info) =>
                {
                    Assert.True(info.IsEmpty);
                    return new ReturnedSender(s_suite);
                },
                OnCreateRecipientCore = (e, info) =>
                {
                    Assert.True(info.IsEmpty);
                    return new ReturnedRecipient(s_suite);
                },
                OnCreatePskSenderCore = (e, info, key, id) =>
                {
                    Assert.True(info.IsEmpty);
                    return new ReturnedSender(s_suite);
                },
                OnCreatePskRecipientCore = (e, info, key, id) =>
                {
                    Assert.True(info.IsEmpty);
                    return new ReturnedRecipient(s_suite);
                },
            })
            {
                hpke.Seal(Array.Empty<byte>(), out _, out _);
                hpke.Seal(ReadOnlySpan<byte>.Empty, out _, out _);
                hpke.Seal(ReadOnlySpan<byte>.Empty, enc, ct);
                hpke.Open(enc, ct);
                hpke.Open(enc.AsSpan(), ct.AsSpan());
                hpke.Open(enc, ct, Span<byte>.Empty);
                hpke.CreateSender(out _).Dispose();
                hpke.CreateSender(enc.AsSpan()).Dispose();
                hpke.CreateRecipient(enc).Dispose();
                hpke.CreateRecipient(enc.AsSpan()).Dispose();
                hpke.CreatePskSender(psk, pskId, out _).Dispose();
                hpke.CreatePskSender(psk.AsSpan(), pskId.AsSpan(), out _).Dispose();
                hpke.CreatePskSender(psk, pskId, enc.AsSpan()).Dispose();
                hpke.CreatePskRecipient(enc, psk, pskId).Dispose();
                hpke.CreatePskRecipient(enc.AsSpan(), psk.AsSpan(), pskId.AsSpan()).Dispose();
                Assert.Equal(3, hpke.SealCoreCount);
                Assert.Equal(3, hpke.OpenCoreCount);
                Assert.Equal(2, hpke.CreateSenderCoreCount);
                Assert.Equal(2, hpke.CreateRecipientCoreCount);
                Assert.Equal(3, hpke.CreatePskSenderCoreCount);
                Assert.Equal(2, hpke.CreatePskRecipientCoreCount);
            }
        }

        public static IEnumerable<object[]> SealOverlaps()
        {
            // Slots: plaintext, encapsulatedSecret, ciphertext, associatedData, info.
            int[] lengths = [32, s_suite.EncapsulatedSecretSizeInBytes, s_suite.GetCiphertextLength(32), 16, 16];

            // Include one-byte overlaps at both ends of each pair.
            foreach ((int first, int second) in new[] { (0, 1), (0, 2), (1, 2), (3, 1), (3, 2), (4, 1), (4, 2) })
            foreach (int offset in new[] { -1, 0, 1, lengths[first] - 1, 1 - lengths[second] })
            {
                yield return new object[] { first, second, offset };
            }
        }

        [Theory]
        [MemberData(nameof(SealOverlaps))]
        public static void Seal_OverlapsRejectedBeforeDisposal(int first, int second, int offset)
        {
            foreach (bool disposed in new[] { false, true })
            {
                byte[][] buffers = new byte[5][];

                for (int i = 0; i < buffers.Length; i++)
                {
                    buffers[i] = Filled(2 * s_suite.EncapsulatedSecretSizeInBytes + 96, 0xA5);
                }

                buffers[second] = buffers[first];
                int start = s_suite.EncapsulatedSecretSizeInBytes + 8;
                int[] starts = [start, start, start, start, start];
                starts[second] += offset;

                using (HpkeContract hpke = new(s_suite))
                {
                    if (disposed)
                    {
                        hpke.Dispose();
                    }

                    Assert.Throws<CryptographicException>(() => hpke.Seal(
                        buffers[0].AsSpan(starts[0], 32),
                        buffers[1].AsSpan(starts[1], s_suite.EncapsulatedSecretSizeInBytes),
                        buffers[2].AsSpan(starts[2], s_suite.GetCiphertextLength(32)),
                        buffers[3].AsSpan(starts[3], 16),
                        buffers[4].AsSpan(starts[4], 16)));

                    foreach (byte[] buffer in buffers)
                    {
                        AssertExtensions.FilledWith<byte>(0xA5, buffer);
                    }
                }
            }
        }

        public static IEnumerable<object[]> OpenOverlaps()
        {
            // Input slots: encapsulatedSecret, ciphertext, associatedData, info.
            int[] lengths = [s_suite.EncapsulatedSecretSizeInBytes, s_suite.GetCiphertextLength(32), 16, 16];

            for (int input = 0; input < 4; input++)
            foreach (int offset in new[] { -1, 0, 1, lengths[input] - 1, 1 - 32 })
            {
                yield return new object[] { input, offset };
            }
        }

        [Theory]
        [MemberData(nameof(OpenOverlaps))]
        public static void Open_OverlapsRejectedBeforeDisposal(int input, int offset)
        {
            foreach (bool disposed in new[] { false, true })
            {
                byte[][] buffers = new byte[5][];

                for (int i = 0; i < buffers.Length; i++)
                {
                    buffers[i] = Filled(2 * s_suite.EncapsulatedSecretSizeInBytes + 96, 0xA5);
                }

                buffers[4] = buffers[input];
                int start = s_suite.EncapsulatedSecretSizeInBytes + 8;

                using (HpkeContract hpke = new(s_suite))
                {
                    if (disposed)
                    {
                        hpke.Dispose();
                    }

                    Assert.Throws<CryptographicException>(() => hpke.Open(
                        buffers[0].AsSpan(start, s_suite.EncapsulatedSecretSizeInBytes),
                        buffers[1].AsSpan(start, s_suite.GetCiphertextLength(32)),
                        buffers[4].AsSpan(start + offset, 32),
                        buffers[2].AsSpan(start, 16),
                        buffers[3].AsSpan(start, 16)));
                    AssertExtensions.FilledWith<byte>(0xA5, buffers[4]);
                }
            }
        }

        public static IEnumerable<object[]> SenderOverlaps()
        {
            foreach (int offset in new[] { -1, 0, 1, 1 - 16, s_suite.EncapsulatedSecretSizeInBytes - 1 })
            {
                yield return new object[] { offset };
            }
        }

        [Theory]
        [MemberData(nameof(SenderOverlaps))]
        public static void CreateSender_OverlapsRejectedBeforeDisposal(int offset)
        {
            foreach (bool disposed in new[] { false, true })
            {
                byte[] buffer = Filled(2 * s_suite.EncapsulatedSecretSizeInBytes + 32, 0xA5);
                int start = s_suite.EncapsulatedSecretSizeInBytes + 8;

                using (HpkeContract hpke = new(s_suite))
                {
                    if (disposed)
                    {
                        hpke.Dispose();
                    }

                    Assert.Throws<CryptographicException>(() =>
                        hpke.CreateSender(
                            buffer.AsSpan(start, s_suite.EncapsulatedSecretSizeInBytes),
                            buffer.AsSpan(start + offset, 16)));
                    AssertExtensions.FilledWith<byte>(0xA5, buffer);
                }
            }
        }

        public static IEnumerable<object[]> PskSenderOverlaps()
        {
            // Input slots: psk, pskId, info.
            int[] lengths = [32, 16, 16];

            for (int input = 0; input < 3; input++)
            foreach (int offset in new[] { -1, 0, 1, lengths[input] - 1, 1 - s_suite.EncapsulatedSecretSizeInBytes })
            {
                yield return new object[] { input, offset };
            }
        }

        [Theory]
        [MemberData(nameof(PskSenderOverlaps))]
        public static void CreatePskSender_OverlapsRejectedBeforeDisposal(int input, int offset)
        {
            foreach (bool disposed in new[] { false, true })
            {
                byte[][] buffers = new byte[4][];

                for (int i = 0; i < buffers.Length; i++)
                {
                    buffers[i] = Filled(2 * s_suite.EncapsulatedSecretSizeInBytes + 96, 0xA5);
                }

                buffers[3] = buffers[input];
                int start = s_suite.EncapsulatedSecretSizeInBytes + 8;

                using (HpkeContract hpke = new(s_suite))
                {
                    if (disposed)
                    {
                        hpke.Dispose();
                    }

                    Assert.Throws<CryptographicException>(() => hpke.CreatePskSender(
                        buffers[0].AsSpan(start, 32),
                        buffers[1].AsSpan(start, 16),
                        buffers[3].AsSpan(start + offset, s_suite.EncapsulatedSecretSizeInBytes),
                        buffers[2].AsSpan(start, 16)));
                    AssertExtensions.FilledWith<byte>(0xA5, buffers[3]);
                }
            }
        }

        [Fact]
        public static void Seal_ReadOnlyOverlapAndAdjacentOutputs()
        {
            byte[] buffer = Filled(32 + s_suite.EncapsulatedSecretSizeInBytes + s_suite.GetCiphertextLength(32), 0xA5);

            using (HpkeContract hpke = new(s_suite)
            {
                OnSealCore = (p, enc, ct, aad, info) =>
                {
                    AssertSameBuffer(buffer.AsSpan(0, 32), p);
                    AssertSameBuffer(buffer.AsSpan(0, 16), aad);
                    AssertSameBuffer(buffer.AsSpan(0, 16), info);
                    enc.Fill(0x42);
                    ct.Fill(0xE7);
                },
            })
            {
                hpke.Seal(
                    buffer.AsSpan(0, 32),
                    buffer.AsSpan(32, s_suite.EncapsulatedSecretSizeInBytes),
                    buffer.AsSpan(32 + s_suite.EncapsulatedSecretSizeInBytes),
                    buffer.AsSpan(0, 16),
                    buffer.AsSpan(0, 16));
                AssertExtensions.FilledWith<byte>(0xA5, buffer.AsSpan(0, 32));
                AssertExtensions.FilledWith<byte>(0x42, buffer.AsSpan(32, s_suite.EncapsulatedSecretSizeInBytes));
                AssertExtensions.FilledWith<byte>(0xE7, buffer.AsSpan(32 + s_suite.EncapsulatedSecretSizeInBytes));
                Assert.Equal(1, hpke.SealCoreCount);
            }
        }

        [Fact]
        public static void Open_ReadOnlyOverlapAndAdjacentOutput()
        {
            byte[] buffer = Filled(s_suite.EncapsulatedSecretSizeInBytes + 32, 0xA5);

            using (HpkeContract hpke = new(s_suite)
            {
                OnOpenCore = (enc, ct, p, aad, info) => p.Fill(0xE7),
            })
            {
                hpke.Open(
                    buffer.AsSpan(0, s_suite.EncapsulatedSecretSizeInBytes),
                    buffer.AsSpan(0, s_suite.GetCiphertextLength(32)),
                    buffer.AsSpan(s_suite.EncapsulatedSecretSizeInBytes),
                    buffer.AsSpan(0, 16),
                    buffer.AsSpan(0, 16));
                AssertExtensions.FilledWith<byte>(0xA5, buffer.AsSpan(0, s_suite.EncapsulatedSecretSizeInBytes));
                AssertExtensions.FilledWith<byte>(0xE7, buffer.AsSpan(s_suite.EncapsulatedSecretSizeInBytes));
                Assert.Equal(1, hpke.OpenCoreCount);
            }
        }

        [Fact]
        public static void ContextFactories_ReadOnlyOverlapAndAdjacentOutput()
        {
            byte[] buffer = Filled(s_suite.EncapsulatedSecretSizeInBytes + 32, 0xA5);

            using (HpkeContract hpke = new(s_suite)
            {
                OnCreateSenderCore = (enc, info) => new ReturnedSender(s_suite),
                OnCreateRecipientCore = (enc, info) => new ReturnedRecipient(s_suite),
                OnCreatePskSenderCore = (enc, info, psk, id) => new ReturnedSender(s_suite),
                OnCreatePskRecipientCore = (enc, info, psk, id) => new ReturnedRecipient(s_suite),
            })
            {
                hpke.CreateSender(buffer.AsSpan(32), buffer.AsSpan(0, 32)).Dispose();
                hpke.CreateRecipient(
                    buffer.AsSpan(0, s_suite.EncapsulatedSecretSizeInBytes),
                    buffer.AsSpan(0, 32)).Dispose();
                hpke.CreatePskSender(
                    buffer.AsSpan(0, 32),
                    buffer.AsSpan(0, 32),
                    buffer.AsSpan(32),
                    buffer.AsSpan(0, 32)).Dispose();
                hpke.CreatePskRecipient(buffer.AsSpan(0, s_suite.EncapsulatedSecretSizeInBytes),
                    buffer.AsSpan(0, 32), buffer.AsSpan(0, 32), buffer.AsSpan(0, 32)).Dispose();
                Assert.Equal(1, hpke.CreateSenderCoreCount);
                Assert.Equal(1, hpke.CreateRecipientCoreCount);
                Assert.Equal(1, hpke.CreatePskSenderCoreCount);
                Assert.Equal(1, hpke.CreatePskRecipientCoreCount);
            }
        }

        [Fact]
        public static void EmptySpans_DoNotOverlap()
        {
            byte[] buffer = new byte[s_suite.EncapsulatedSecretSizeInBytes + s_suite.AeadTagSizeInBytes];

            using (HpkeContract hpke = new(s_suite)
            {
                OnSealCore = (p, enc, ct, aad, info) => { },
                OnOpenCore = (enc, ct, p, aad, info) => { },
                OnCreateSenderCore = (enc, info) => new ReturnedSender(s_suite),
            })
            {
                hpke.Seal(buffer.AsSpan(0, 0), buffer.AsSpan(0, s_suite.EncapsulatedSecretSizeInBytes),
                    buffer.AsSpan(s_suite.EncapsulatedSecretSizeInBytes), buffer.AsSpan(1, 0), buffer.AsSpan(2, 0));
                hpke.Open(buffer.AsSpan(0, s_suite.EncapsulatedSecretSizeInBytes),
                    buffer.AsSpan(0, s_suite.AeadTagSizeInBytes), buffer.AsSpan(1, 0),
                    buffer.AsSpan(2, 0), buffer.AsSpan(3, 0));
                hpke.CreateSender(
                    buffer.AsSpan(0, s_suite.EncapsulatedSecretSizeInBytes),
                    buffer.AsSpan(1, 0)).Dispose();
                Assert.Equal(1, hpke.SealCoreCount);
                Assert.Equal(1, hpke.OpenCoreCount);
                Assert.Equal(1, hpke.CreateSenderCoreCount);
            }
        }

        [Fact]
        public static void CoreFailures_PropagateUnchanged()
        {
            CryptographicException exception = new();

            using (HpkeContract hpke = new(s_suite)
            {
                OnExportDecapsulationKeyCore = destination => throw exception,
                OnExportEncapsulationKeyCore = destination => throw exception,
                OnSealCore = (p, enc, ct, aad, info) => throw exception,
                OnOpenCore = (enc, ct, p, aad, info) => throw exception,
                OnCreateSenderCore = (enc, info) => throw exception,
                OnCreateRecipientCore = (enc, info) => throw exception,
                OnCreatePskSenderCore = (enc, info, psk, id) => throw exception,
                OnCreatePskRecipientCore = (enc, info, psk, id) => throw exception,
            })
            {
                foreach (Action operation in InstanceOperations(hpke))
                {
                    Assert.Same(exception, Assert.Throws<CryptographicException>(operation));
                }

                Assert.Equal(2, hpke.ExportDecapsulationKeyCoreCount);
                Assert.Equal(2, hpke.ExportEncapsulationKeyCoreCount);
                Assert.Equal(3, hpke.SealCoreCount);
                Assert.Equal(3, hpke.OpenCoreCount);
                Assert.Equal(2, hpke.CreateSenderCoreCount);
                Assert.Equal(2, hpke.CreateRecipientCoreCount);
                Assert.Equal(3, hpke.CreatePskSenderCoreCount);
                Assert.Equal(2, hpke.CreatePskRecipientCoreCount);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Seal_CoreFailureDoesNotPublishOutputs(bool useSpan)
        {
            CryptographicException exception = new();
            byte[] originalEnc = [0x31];
            byte[] originalCiphertext = [0x41];
            byte[] enc = originalEnc;
            byte[] ciphertext = originalCiphertext;

            using (HpkeContract hpke = new(s_suite)
            {
                OnSealCore = (p, e, ct, aad, info) =>
                {
                    e.Fill(0x42);
                    ct.Fill(0xE7);
                    throw exception;
                },
            })
            {
                if (useSpan)
                {
                    Assert.Same(exception, Assert.Throws<CryptographicException>(() =>
                        hpke.Seal(ReadOnlySpan<byte>.Empty, out enc, out ciphertext)));
                }
                else
                {
                    Assert.Same(exception, Assert.Throws<CryptographicException>(() =>
                        hpke.Seal(Array.Empty<byte>(), out enc, out ciphertext)));
                }

                Assert.Same(originalEnc, enc);
                Assert.Same(originalCiphertext, ciphertext);
                Assert.Equal(1, hpke.SealCoreCount);
            }
        }

        [Theory]
        [InlineData(SenderFactory.Base)]
        [InlineData(SenderFactory.PskArray)]
        [InlineData(SenderFactory.PskSpan)]
        public static void CreateSender_CoreFailureDoesNotPublishOutput(SenderFactory factory)
        {
            CryptographicException exception = new();
            byte[] original = [0x31];
            byte[] encapsulatedSecret = original;

            using (HpkeContract hpke = new(s_suite))
            {
                if (factory == SenderFactory.Base)
                {
                    hpke.OnCreateSenderCore = (enc, info) => throw exception;
                    Assert.Same(exception, Assert.Throws<CryptographicException>(
                        () => hpke.CreateSender(out encapsulatedSecret)));
                }
                else
                {
                    byte[] psk = new byte[32];
                    byte[] pskId = [1];
                    hpke.OnCreatePskSenderCore = (enc, info, key, id) => throw exception;

                    if (factory == SenderFactory.PskArray)
                    {
                        Assert.Same(exception, Assert.Throws<CryptographicException>(() =>
                            hpke.CreatePskSender(psk, pskId, out encapsulatedSecret)));
                    }
                    else
                    {
                        Assert.Same(exception, Assert.Throws<CryptographicException>(() =>
                            hpke.CreatePskSender(psk.AsSpan(), pskId.AsSpan(), out encapsulatedSecret)));
                    }
                }

                Assert.Same(original, encapsulatedSecret);
                Assert.Equal(1, hpke.CreateSenderCoreCount + hpke.CreatePskSenderCoreCount);
            }
        }

        public enum SenderFactory
        {
            Base,
            PskArray,
            PskSpan,
        }

        private static HpkeContract CreateContextContract(HpkeSuite suite) => new(suite)
        {
            OnSealCore = (p, enc, ct, aad, info) => { },
            OnOpenCore = (enc, ct, p, aad, info) => { },
            OnCreateSenderCore = (enc, info) => new ReturnedSender(suite),
            OnCreateRecipientCore = (enc, info) => new ReturnedRecipient(suite),
            OnCreatePskSenderCore = (enc, info, psk, id) => new ReturnedSender(suite),
            OnCreatePskRecipientCore = (enc, info, psk, id) => new ReturnedRecipient(suite),
        };

        private static IEnumerable<Action> InstanceOperations(HpkeContract hpke)
        {
            yield return () => hpke.ExportDecapsulationKey();
            yield return () => hpke.ExportDecapsulationKey(new byte[hpke.Suite.DecapsulationKeySizeInBytes]);
            yield return () => hpke.ExportEncapsulationKey();
            yield return () => hpke.ExportEncapsulationKey(new byte[hpke.Suite.EncapsulationKeySizeInBytes]);

            foreach (Action operation in ContextOperations(hpke, Array.Empty<byte>()))
            {
                yield return operation;
            }
        }

        private static IEnumerable<Action> ContextOperations(HpkeContract hpke, byte[] info)
        {
            byte[] plaintext = new byte[32];
            byte[] ciphertext = new byte[hpke.Suite.GetCiphertextLength(plaintext.Length)];
            byte[] encapsulatedSecret = new byte[hpke.Suite.EncapsulatedSecretSizeInBytes];
            byte[] associatedData = [1, 2, 3];
            yield return () => hpke.Seal(plaintext, out _, out _, associatedData, info);
            yield return () => hpke.Seal(plaintext.AsSpan(), out _, out _, associatedData.AsSpan(), info.AsSpan());
            yield return () => hpke.Seal(plaintext, encapsulatedSecret, ciphertext, associatedData, info);
            yield return () => hpke.Open(encapsulatedSecret, ciphertext, associatedData: associatedData, info: info);
            yield return () => hpke.Open(
                encapsulatedSecret.AsSpan(),
                ciphertext.AsSpan(),
                associatedData: associatedData.AsSpan(),
                info: info.AsSpan());
            yield return () => hpke.Open(encapsulatedSecret, ciphertext, plaintext.AsSpan(), associatedData, info);
            yield return () => hpke.CreateSender(out _, info).Dispose();
            yield return () => hpke.CreateSender(encapsulatedSecret.AsSpan(), info).Dispose();
            yield return () => hpke.CreateRecipient(encapsulatedSecret, info).Dispose();
            yield return () => hpke.CreateRecipient(encapsulatedSecret.AsSpan(), info.AsSpan()).Dispose();

            foreach (Action operation in PskOperations(hpke, new byte[32], new byte[] { 1 }, info))
            {
                yield return operation;
            }
        }

        private static IEnumerable<Action> PskOperations(HpkeContract hpke, byte[] psk, byte[] pskId, byte[] info)
        {
            byte[] encapsulatedSecret = new byte[hpke.Suite.EncapsulatedSecretSizeInBytes];
            yield return () => hpke.CreatePskSender(psk, pskId, out _, info).Dispose();
            yield return () => hpke.CreatePskSender(psk.AsSpan(), pskId.AsSpan(), out _, info.AsSpan()).Dispose();
            yield return () => hpke.CreatePskSender(psk, pskId, encapsulatedSecret.AsSpan(), info).Dispose();
            yield return () => hpke.CreatePskRecipient(encapsulatedSecret, psk, pskId, info).Dispose();
            yield return () => hpke.CreatePskRecipient(
                encapsulatedSecret.AsSpan(),
                psk.AsSpan(),
                pskId.AsSpan(),
                info.AsSpan()).Dispose();
        }

        private static byte[] Filled(int length, byte value)
        {
            byte[] buffer = new byte[length];
            buffer.AsSpan().Fill(value);
            return buffer;
        }

        private static void AssertGuardedOutput(byte[] buffer, byte value)
        {
            Assert.Equal(0xA5, buffer[0]);
            Assert.Equal(0xA5, buffer[buffer.Length - 1]);
            AssertExtensions.FilledWith(value, buffer.AsSpan(1, buffer.Length - 2));
        }

        private static void AssertSameBuffer(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual)
        {
            Assert.Equal(expected.Length, actual.Length);

            if (!expected.IsEmpty)
            {
                AssertExtensions.Same(expected, actual);
            }
        }

        private sealed class ReturnedSender : HpkeSender
        {
            internal bool Disposed { get; private set; }
            internal ReturnedSender(HpkeSuite suite) : base(suite) { }
            protected override void SealCore(
                ReadOnlySpan<byte> plaintext,
                Span<byte> ciphertext,
                ReadOnlySpan<byte> associatedData) =>
                throw new XunitException("Unexpected sender operation.");
            protected override void ExportCore(ReadOnlySpan<byte> exporterContext, Span<byte> destination) =>
                throw new XunitException("Unexpected sender export.");
            protected override void Dispose(bool disposing) => Disposed = true;
        }

        private sealed class ReturnedRecipient : HpkeRecipient
        {
            internal bool Disposed { get; private set; }
            internal ReturnedRecipient(HpkeSuite suite) : base(suite) { }
            protected override void OpenCore(
                ReadOnlySpan<byte> ciphertext,
                Span<byte> plaintext,
                ReadOnlySpan<byte> associatedData) =>
                throw new XunitException("Unexpected recipient operation.");
            protected override void ExportCore(ReadOnlySpan<byte> exporterContext, Span<byte> destination) =>
                throw new XunitException("Unexpected recipient export.");
            protected override void Dispose(bool disposing) => Disposed = true;
        }
    }

    internal sealed class HpkeContract : Hpke
    {
        private bool _disposed;

        internal ExportKeyCoreCallback OnExportDecapsulationKeyCore { get; set; }
        internal ExportKeyCoreCallback OnExportEncapsulationKeyCore { get; set; }
        internal SealCoreCallback OnSealCore { get; set; }
        internal OpenCoreCallback OnOpenCore { get; set; }
        internal CreateSenderCoreCallback OnCreateSenderCore { get; set; }
        internal CreateRecipientCoreCallback OnCreateRecipientCore { get; set; }
        internal CreatePskSenderCoreCallback OnCreatePskSenderCore { get; set; }
        internal CreatePskRecipientCoreCallback OnCreatePskRecipientCore { get; set; }
        internal Action<bool> OnDispose { get; set; } = static disposing => { };

        internal int ExportDecapsulationKeyCoreCount { get; private set; }
        internal int ExportEncapsulationKeyCoreCount { get; private set; }
        internal int SealCoreCount { get; private set; }
        internal int OpenCoreCount { get; private set; }
        internal int CreateSenderCoreCount { get; private set; }
        internal int CreateRecipientCoreCount { get; private set; }
        internal int CreatePskSenderCoreCount { get; private set; }
        internal int CreatePskRecipientCoreCount { get; private set; }

        internal HpkeContract(HpkeSuite suite) : base(suite)
        {
        }

        protected override void ExportDecapsulationKeyCore(Span<byte> destination)
        {
            ExportDecapsulationKeyCoreCount++;
            Assert.Equal(Suite.DecapsulationKeySizeInBytes, destination.Length);
            GetCallback(OnExportDecapsulationKeyCore)(destination);
        }

        protected override void ExportEncapsulationKeyCore(Span<byte> destination)
        {
            ExportEncapsulationKeyCoreCount++;
            Assert.Equal(Suite.EncapsulationKeySizeInBytes, destination.Length);
            GetCallback(OnExportEncapsulationKeyCore)(destination);
        }

        protected override void SealCore(
            ReadOnlySpan<byte> plaintext, Span<byte> encapsulatedSecret, Span<byte> ciphertext,
            ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> info)
        {
            SealCoreCount++;
            Assert.Equal(Suite.EncapsulatedSecretSizeInBytes, encapsulatedSecret.Length);
            Assert.Equal(Suite.GetCiphertextLength(plaintext.Length), ciphertext.Length);
            AssertInfo(info);
            GetCallback(OnSealCore)(plaintext, encapsulatedSecret, ciphertext, associatedData, info);
        }

        protected override void OpenCore(
            ReadOnlySpan<byte> encapsulatedSecret, ReadOnlySpan<byte> ciphertext, Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> info)
        {
            OpenCoreCount++;
            Assert.Equal(Suite.EncapsulatedSecretSizeInBytes, encapsulatedSecret.Length);
            Assert.InRange(ciphertext.Length, Suite.AeadTagSizeInBytes, int.MaxValue);
            Assert.Equal(ciphertext.Length - Suite.AeadTagSizeInBytes, plaintext.Length);
            AssertInfo(info);
            GetCallback(OnOpenCore)(encapsulatedSecret, ciphertext, plaintext, associatedData, info);
        }

        protected override HpkeSender CreateSenderCore(Span<byte> encapsulatedSecret, ReadOnlySpan<byte> info)
        {
            CreateSenderCoreCount++;
            Assert.Equal(Suite.EncapsulatedSecretSizeInBytes, encapsulatedSecret.Length);
            AssertInfo(info);
            return GetCallback(OnCreateSenderCore)(encapsulatedSecret, info);
        }

        protected override HpkeRecipient CreateRecipientCore(
            ReadOnlySpan<byte> encapsulatedSecret,
            ReadOnlySpan<byte> info)
        {
            CreateRecipientCoreCount++;
            Assert.Equal(Suite.EncapsulatedSecretSizeInBytes, encapsulatedSecret.Length);
            AssertInfo(info);
            return GetCallback(OnCreateRecipientCore)(encapsulatedSecret, info);
        }

        protected override HpkeSender CreatePskSenderCore(
            Span<byte> encapsulatedSecret, ReadOnlySpan<byte> info, ReadOnlySpan<byte> psk, ReadOnlySpan<byte> pskId)
        {
            CreatePskSenderCoreCount++;
            Assert.Equal(Suite.EncapsulatedSecretSizeInBytes, encapsulatedSecret.Length);
            AssertInfo(info);
            AssertPskInputs(psk, pskId);
            return GetCallback(OnCreatePskSenderCore)(encapsulatedSecret, info, psk, pskId);
        }

        protected override HpkeRecipient CreatePskRecipientCore(
            ReadOnlySpan<byte> encapsulatedSecret,
            ReadOnlySpan<byte> info,
            ReadOnlySpan<byte> psk,
            ReadOnlySpan<byte> pskId)
        {
            CreatePskRecipientCoreCount++;
            Assert.Equal(Suite.EncapsulatedSecretSizeInBytes, encapsulatedSecret.Length);
            AssertInfo(info);
            AssertPskInputs(psk, pskId);
            return GetCallback(OnCreatePskRecipientCore)(encapsulatedSecret, info, psk, pskId);
        }

        protected override void Dispose(bool disposing)
        {
            GetCallback(OnDispose)(disposing);
            VerifyCalled(
                OnExportDecapsulationKeyCore, ExportDecapsulationKeyCoreCount, nameof(ExportDecapsulationKeyCore));
            VerifyCalled(
                OnExportEncapsulationKeyCore, ExportEncapsulationKeyCoreCount, nameof(ExportEncapsulationKeyCore));
            VerifyCalled(OnSealCore, SealCoreCount, nameof(SealCore));
            VerifyCalled(OnOpenCore, OpenCoreCount, nameof(OpenCore));
            VerifyCalled(OnCreateSenderCore, CreateSenderCoreCount, nameof(CreateSenderCore));
            VerifyCalled(OnCreateRecipientCore, CreateRecipientCoreCount, nameof(CreateRecipientCore));
            VerifyCalled(OnCreatePskSenderCore, CreatePskSenderCoreCount, nameof(CreatePskSenderCore));
            VerifyCalled(OnCreatePskRecipientCore, CreatePskRecipientCoreCount, nameof(CreatePskRecipientCore));
            _disposed = true;
        }

        internal static bool HasInputLengthLimit(HpkeKdf kdf) => kdf switch
        {
            HpkeKdf.HKDF_SHA256 or HpkeKdf.HKDF_SHA384 or HpkeKdf.HKDF_SHA512 => false,
            HpkeKdf.SHAKE128 or HpkeKdf.SHAKE256 => true,
            _ => throw new XunitException($"Unknown KDF {kdf}."),
        };

        private void AssertInfo(ReadOnlySpan<byte> info)
        {
            if (HasInputLengthLimit(Suite.KdfAlgorithm))
            {
                Assert.InRange(info.Length, 0, ushort.MaxValue);
            }
        }

        private void AssertPskInputs(ReadOnlySpan<byte> psk, ReadOnlySpan<byte> pskId)
        {
            int maximumLength = HasInputLengthLimit(Suite.KdfAlgorithm) ? ushort.MaxValue : int.MaxValue;
            Assert.InRange(psk.Length, 32, maximumLength);
            Assert.InRange(pskId.Length, 1, maximumLength);
        }

        private T GetCallback<T>(T callback, [CallerMemberName] string caller = null) where T : Delegate
        {
            if (_disposed)
            {
                Assert.Fail($"Unexpected call to {caller} after Dispose.");
            }

            return callback ?? throw new XunitException($"Unexpected call to {caller}.");
        }

        private static void VerifyCalled(Delegate callback, int count, string name)
        {
            if (callback is not null && count == 0)
            {
                Assert.Fail($"Expected call to {name}.");
            }
        }

        internal delegate void ExportKeyCoreCallback(Span<byte> destination);
        internal delegate void SealCoreCallback(
            ReadOnlySpan<byte> plaintext,
            Span<byte> encapsulatedSecret,
            Span<byte> ciphertext,
            ReadOnlySpan<byte> associatedData,
            ReadOnlySpan<byte> info);
        internal delegate void OpenCoreCallback(
            ReadOnlySpan<byte> encapsulatedSecret,
            ReadOnlySpan<byte> ciphertext,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData,
            ReadOnlySpan<byte> info);
        internal delegate HpkeSender CreateSenderCoreCallback(Span<byte> encapsulatedSecret, ReadOnlySpan<byte> info);
        internal delegate HpkeRecipient CreateRecipientCoreCallback(
            ReadOnlySpan<byte> encapsulatedSecret,
            ReadOnlySpan<byte> info);
        internal delegate HpkeSender CreatePskSenderCoreCallback(Span<byte> encapsulatedSecret, ReadOnlySpan<byte> info,
            ReadOnlySpan<byte> psk, ReadOnlySpan<byte> pskId);
        internal delegate HpkeRecipient CreatePskRecipientCoreCallback(
            ReadOnlySpan<byte> encapsulatedSecret,
            ReadOnlySpan<byte> info,
            ReadOnlySpan<byte> psk,
            ReadOnlySpan<byte> pskId);
    }
}
