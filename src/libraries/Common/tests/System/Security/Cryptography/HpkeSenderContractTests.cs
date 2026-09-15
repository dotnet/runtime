// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.Tests
{
    public static class HpkeSenderContractTests
    {
        private static readonly HpkeSuite s_suite = new(
            HpkeKem.MLKEM_768, HpkeKdf.SHAKE256, HpkeAead.AES_128_GCM);

        [Fact]
        public static void Constructor_NullSuite()
        {
            AssertExtensions.Throws<ArgumentNullException>("suite", () => new HpkeSenderContract(null));
        }

        [Theory]
        [MemberData(nameof(HpkeTestData.RepresentativeSuites), MemberType = typeof(HpkeTestData))]
        public static void Constructor_SetsSuite(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            using (HpkeSenderContract sender = new(suite))
            {
                Assert.Equal(suite, sender.Suite);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        public static void Dispose_CallsCoreOnce(int disposeCalls)
        {
            int calls = 0;
            HpkeSenderContract sender = new(s_suite)
            {
                OnDispose = disposing =>
                {
                    Assert.True(disposing);
                    calls++;
                },
            };

            for (int i = 0; i < disposeCalls; i++)
            {
                sender.Dispose();
            }

            Assert.Equal(1, calls);
        }

        [Fact]
        public static void Disposed_OperationsDoNotCallCore()
        {
            using (HpkeSenderContract sender = new(s_suite))
            {
                sender.Dispose();

                foreach (Action operation in Operations(sender))
                {
                    Assert.Throws<ObjectDisposedException>(operation);
                }
            }
        }

        [Fact]
        public static void Dispose_FailurePropagatesAndDoesNotRepeat()
        {
            InvalidOperationException exception = new();
            int calls = 0;
            HpkeSenderContract sender = new(s_suite)
            {
                OnDispose = disposing =>
                {
                    Assert.True(disposing);
                    calls++;
                    throw exception;
                },
            };

            Assert.Same(exception, Assert.Throws<InvalidOperationException>(() => sender.Dispose()));
            sender.Dispose();
            Assert.Equal(1, calls);

            foreach (Action operation in Operations(sender))
            {
                Assert.Throws<ObjectDisposedException>(operation);
            }
        }

        [Theory]
        [MemberData(nameof(HpkeTestData.RepresentativeSuites), MemberType = typeof(HpkeTestData))]
        public static void Seal_Allocated(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            foreach (int length in new[] { 0, 1, 16, 17, 32 })
            foreach (bool useSpan in new[] { false, true })
            {
                byte[] plaintext = Data(length);
                byte[] associatedData = [0x71, 0x72, 0x73];

                using (HpkeSenderContract sender = new(suite)
                {
                    OnSealCore = (p, ct, aad) =>
                    {
                        AssertExtensions.SequenceEqual(plaintext.AsSpan(), p);
                        AssertExtensions.SequenceEqual(associatedData.AsSpan(), aad);
                        ct.Fill(0xE7);
                    },
                })
                {
                    byte[] ciphertext = useSpan
                        ? sender.Seal(plaintext.AsSpan(), associatedData: associatedData.AsSpan())
                        : sender.Seal(plaintext, associatedData: associatedData);
                    Assert.Equal(suite.GetCiphertextLength(length), ciphertext.Length);
                    AssertExtensions.FilledWith<byte>(0xE7, ciphertext);
                    Assert.Equal(Data(length), plaintext);
                    Assert.Equal(new byte[] { 0x71, 0x72, 0x73 }, associatedData);
                    Assert.Equal(1, sender.SealCoreCount);
                    Assert.Equal(0, sender.ExportCoreCount);
                }
            }
        }

        [Theory]
        [MemberData(nameof(HpkeTestData.RepresentativeSuites), MemberType = typeof(HpkeTestData))]
        public static void Seal_Exact(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            foreach (int length in new[] { 0, 1, 32 })
            {
                byte[] input = Data(length + 2);
                Memory<byte> plaintext = input.AsMemory(1, length);
                byte[] expected = plaintext.ToArray();
                byte[] aadBuffer = [0xA5, 0x71, 0x72, 0x73, 0xA5];
                Memory<byte> associatedData = aadBuffer.AsMemory(1, 3);
                byte[] output = new byte[suite.GetCiphertextLength(length) + 2];
                output.AsSpan().Fill(0xA5);

                using (HpkeSenderContract sender = new(suite)
                {
                    OnSealCore = (p, ct, aad) =>
                    {
                        AssertExtensions.SequenceEqual(expected.AsSpan(), p);
                        AssertExtensions.SequenceEqual(associatedData.Span, aad);
                        ct.Fill(0xE7);
                    },
                })
                {
                    sender.Seal(plaintext.Span, output.AsSpan(1, output.Length - 2), associatedData.Span);
                    AssertGuardedOutput(output);
                    Assert.Equal(Data(length + 2), input);
                    Assert.Equal(new byte[] { 0xA5, 0x71, 0x72, 0x73, 0xA5 }, aadBuffer);
                    Assert.Equal(1, sender.SealCoreCount);
                    Assert.Equal(0, sender.ExportCoreCount);
                }
            }
        }

        [Fact]
        public static void Seal_OptionalAssociatedDataIsEmpty()
        {
            using (HpkeSenderContract sender = new(s_suite)
            {
                OnSealCore = (p, ct, aad) =>
                {
                    Assert.True(p.IsEmpty);
                    Assert.True(aad.IsEmpty);
                    ct.Fill(0xE7);
                },
            })
            {
                byte[] first = sender.Seal(Array.Empty<byte>());
                byte[] second = sender.Seal(ReadOnlySpan<byte>.Empty);
                byte[] third = sender.Seal(Array.Empty<byte>(), associatedData: null);
                byte[] destination = new byte[s_suite.AeadTagSizeInBytes];
                sender.Seal(ReadOnlySpan<byte>.Empty, destination.AsSpan());
                Assert.Equal(first, second);
                Assert.Equal(first, third);
                Assert.Equal(first, destination);
                AssertExtensions.FilledWith<byte>(0xE7, destination);
                Assert.Equal(4, sender.SealCoreCount);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void NullArgumentsBeforeDisposal(bool disposed)
        {
            using (HpkeSenderContract sender = new(s_suite))
            {
                if (disposed)
                {
                    sender.Dispose();
                }

                AssertExtensions.Throws<ArgumentNullException>("plaintext", () => sender.Seal((byte[])null));
                AssertExtensions.Throws<ArgumentNullException>("exporterContext",
                    () => sender.Export((byte[])null, 0));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Seal_InvalidDestinationBeforeDisposal(bool disposed)
        {
            byte[] plaintext = Data(32);

            using (HpkeSenderContract sender = new(s_suite))
            {
                if (disposed)
                {
                    sender.Dispose();
                }

                int size = s_suite.GetCiphertextLength(plaintext.Length);

                foreach (int length in new[] { 0, size - 1, size + 1 })
                {
                    byte[] destination = new byte[length];
                    destination.AsSpan().Fill(0xA5);
                    AssertExtensions.Throws<ArgumentException>("ciphertext",
                        () => sender.Seal(plaintext, destination.AsSpan()));
                    AssertExtensions.FilledWith<byte>(0xA5, destination);
                }
            }
        }

        public static IEnumerable<object[]> SealOverlaps()
        {
            foreach (bool overlapPlaintext in new[] { false, true })
            {
                int inputLength = overlapPlaintext ? 32 : 16;
                int outputLength = s_suite.GetCiphertextLength(32);

                foreach (int offset in new[] { -1, 0, 1, inputLength - 1, 1 - outputLength })
                {
                    yield return new object[] { overlapPlaintext, offset };
                }
            }
        }

        [Theory]
        [MemberData(nameof(SealOverlaps))]
        public static void Seal_OverlapsRejectedBeforeDisposal(bool overlapPlaintext, int offset)
        {
            foreach (bool disposed in new[] { false, true })
            {
                byte[] plaintext = new byte[256];
                byte[] aad = new byte[256];
                byte[] output = overlapPlaintext ? plaintext : aad;
                output.AsSpan().Fill(0xA5);

                using (HpkeSenderContract sender = new(s_suite))
                {
                    if (disposed)
                    {
                        sender.Dispose();
                    }

                    Assert.Throws<CryptographicException>(() => sender.Seal(
                        plaintext.AsSpan(80, 32),
                        output.AsSpan(80 + offset, s_suite.GetCiphertextLength(32)),
                        aad.AsSpan(80, 16)));
                    AssertExtensions.FilledWith<byte>(0xA5, output);
                }
            }
        }

        [Fact]
        public static void Seal_ReadOnlyOverlapAndAdjacentOutput()
        {
            byte[] buffer = Data(32 + s_suite.GetCiphertextLength(32));
            byte[] expected = buffer.AsSpan(0, 32).ToArray();

            using (HpkeSenderContract sender = new(s_suite)
            {
                OnSealCore = (p, ct, aad) =>
                {
                    AssertExtensions.SequenceEqual(expected.AsSpan(), p);
                    AssertExtensions.SequenceEqual(expected.AsSpan(0, 16), aad);
                    ct.Fill(0xE7);
                },
            })
            {
                sender.Seal(buffer.AsSpan(0, 32), buffer.AsSpan(32), buffer.AsSpan(0, 16));
                AssertExtensions.SequenceEqual(expected.AsSpan(), buffer.AsSpan(0, 32));
                AssertExtensions.FilledWith<byte>(0xE7, buffer.AsSpan(32));
                Assert.Equal(1, sender.SealCoreCount);
            }
        }

        [Fact]
        public static void Seal_EmptyInputsMayShareOutputBuffer()
        {
            byte[] buffer = new byte[s_suite.AeadTagSizeInBytes];

            using (HpkeSenderContract sender = new(s_suite)
            {
                OnSealCore = (p, ct, aad) =>
                {
                    Assert.True(p.IsEmpty);
                    Assert.True(aad.IsEmpty);
                    ct.Fill(0xE7);
                },
            })
            {
                sender.Seal(buffer.AsSpan(0, 0), buffer.AsSpan(), buffer.AsSpan(1, 0));
                AssertExtensions.FilledWith<byte>(0xE7, buffer);
                Assert.Equal(1, sender.SealCoreCount);
            }
        }

        [Theory]
        [MemberData(nameof(HpkeTestData.ExportLimits), MemberType = typeof(HpkeTestData))]
        public static void Export_AllocatedAndExact(HpkeKdf kdf, int maximumLength)
        {
            HpkeSuite suite = new(HpkeKem.MLKEM_768, kdf, HpkeAead.AES_128_GCM);

            foreach (int contextLength in new[] { 0, 1, HpkeTestData.MaxExporterContextLength })
            foreach (int length in new[] { 0, 1, maximumLength })
            {
                byte[] context = Data(contextLength);
                byte[] expectedContext = (byte[])context.Clone();
                byte[] output = new byte[length + 2];
                output.AsSpan().Fill(0xA5);

                using (HpkeSenderContract sender = new(suite)
                {
                    OnExportCore = (c, destination) =>
                    {
                        AssertExtensions.SequenceEqual(expectedContext.AsSpan(), c);
                        Assert.Equal(length, destination.Length);
                        destination.Fill(0xE7);
                    },
                })
                {
                    byte[] first = sender.Export(context, length);
                    byte[] second = sender.Export(context.AsSpan(), length);
                    sender.Export(context, output.AsSpan(1, length));
                    Assert.Equal(length, first.Length);
                    AssertExtensions.FilledWith<byte>(0xE7, first);
                    Assert.Equal(first, second);
                    AssertGuardedOutput(output);
                    Assert.Equal(expectedContext, context);
                    Assert.Equal(3, sender.ExportCoreCount);
                    Assert.Equal(0, sender.SealCoreCount);
                }
            }
        }

        [Theory]
        [MemberData(nameof(HpkeTestData.ExportLimits), MemberType = typeof(HpkeTestData))]
        public static void Export_InvalidLengthsBeforeDisposal(HpkeKdf kdf, int maximumLength)
        {
            HpkeSuite suite = new(HpkeKem.MLKEM_768, kdf, HpkeAead.AES_128_GCM);

            foreach (bool disposed in new[] { false, true })
            {
                using (HpkeSenderContract sender = new(suite))
                {
                    if (disposed)
                    {
                        sender.Dispose();
                    }

                    foreach (int length in new[] { -1, int.MinValue, maximumLength + 1, int.MaxValue })
                    {
                        AssertExtensions.Throws<ArgumentOutOfRangeException>("length",
                            () => sender.Export(Array.Empty<byte>(), length));
                        AssertExtensions.Throws<ArgumentOutOfRangeException>("length",
                            () => sender.Export(ReadOnlySpan<byte>.Empty, length));
                    }

                    byte[] destination = new byte[maximumLength + 1];
                    destination.AsSpan().Fill(0xA5);
                    AssertExtensions.Throws<ArgumentException>("destination",
                        () => sender.Export(ReadOnlySpan<byte>.Empty, destination.AsSpan()));
                    AssertExtensions.FilledWith<byte>(0xA5, destination);
                }
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(15)]
        [InlineData(-31)]
        public static void Export_OverlapsRejectedBeforeDisposal(int offset)
        {
            foreach (bool disposed in new[] { false, true })
            {
                byte[] buffer = new byte[128];
                buffer.AsSpan().Fill(0xA5);

                using (HpkeSenderContract sender = new(s_suite))
                {
                    if (disposed)
                    {
                        sender.Dispose();
                    }

                    Assert.Throws<CryptographicException>(() =>
                        sender.Export(buffer.AsSpan(48, 16), buffer.AsSpan(48 + offset, 32)));
                    AssertExtensions.FilledWith<byte>(0xA5, buffer);
                }
            }
        }

        [Theory]
        [InlineData(0, 32)]
        [InlineData(32, 0)]
        [InlineData(32, 32)]
        public static void Export_EmptyAndAdjacentBuffers(int contextLength, int outputLength)
        {
            byte[] buffer = Data(contextLength + outputLength + 1);
            byte[] original = (byte[])buffer.Clone();

            using (HpkeSenderContract sender = new(s_suite)
            {
                OnExportCore = (context, destination) =>
                {
                    AssertExtensions.SequenceEqual(original.AsSpan(0, contextLength), context);
                    Assert.Equal(outputLength, destination.Length);
                    destination.Fill(0xE7);
                },
            })
            {
                sender.Export(buffer.AsSpan(0, contextLength), buffer.AsSpan(contextLength, outputLength));
                AssertExtensions.SequenceEqual(original.AsSpan(0, contextLength), buffer.AsSpan(0, contextLength));
                AssertExtensions.FilledWith<byte>(0xE7, buffer.AsSpan(contextLength, outputLength));
                Assert.Equal(original[original.Length - 1], buffer[buffer.Length - 1]);
                Assert.Equal(1, sender.ExportCoreCount);
            }
        }

        [Fact]
        public static void CoreFailures_PropagateUnchanged()
        {
            CryptographicException exception = new();

            using (HpkeSenderContract sender = new(s_suite)
            {
                OnSealCore = (p, ct, aad) => throw exception,
                OnExportCore = (context, destination) => throw exception,
            })
            {
                foreach (Action operation in Operations(sender))
                {
                    Assert.Same(exception, Assert.Throws<CryptographicException>(operation));
                }

                Assert.Equal(3, sender.SealCoreCount);
                Assert.Equal(3, sender.ExportCoreCount);
            }
        }

        private static IEnumerable<Action> Operations(HpkeSender sender)
        {
            yield return () => sender.Seal(Array.Empty<byte>());
            yield return () => sender.Seal(ReadOnlySpan<byte>.Empty);
            yield return () => sender.Seal(
                ReadOnlySpan<byte>.Empty, new byte[sender.Suite.AeadTagSizeInBytes].AsSpan());
            yield return () => sender.Export(Array.Empty<byte>(), 0);
            yield return () => sender.Export(ReadOnlySpan<byte>.Empty, 1);
            yield return () => sender.Export(ReadOnlySpan<byte>.Empty, new byte[1].AsSpan());
        }

        private static byte[] Data(int length)
        {
            byte[] data = new byte[length];

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i * 17 + 3);
            }

            return data;
        }

        private static void AssertGuardedOutput(byte[] buffer)
        {
            Assert.Equal(0xA5, buffer[0]);
            Assert.Equal(0xA5, buffer[buffer.Length - 1]);
            AssertExtensions.FilledWith<byte>(0xE7, buffer.AsSpan(1, buffer.Length - 2));
        }
    }

    internal sealed class HpkeSenderContract : HpkeSender
    {
        private bool _disposed;

        internal SealCoreCallback OnSealCore { get; set; }
        internal ExportCoreCallback OnExportCore { get; set; }
        internal Action<bool> OnDispose { get; set; } = static disposing => { };
        internal int SealCoreCount { get; private set; }
        internal int ExportCoreCount { get; private set; }

        internal HpkeSenderContract(HpkeSuite suite) : base(suite)
        {
        }

        protected override void SealCore(
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            ReadOnlySpan<byte> associatedData)
        {
            SealCoreCount++;
            Assert.Equal(Suite.GetCiphertextLength(plaintext.Length), ciphertext.Length);
            GetCallback(OnSealCore)(plaintext, ciphertext, associatedData);
        }

        protected override void ExportCore(ReadOnlySpan<byte> exporterContext, Span<byte> destination)
        {
            ExportCoreCount++;
            GetCallback(OnExportCore)(exporterContext, destination);
        }

        protected override void Dispose(bool disposing)
        {
            GetCallback(OnDispose)(disposing);

            if (OnSealCore is not null && SealCoreCount == 0)
            {
                Assert.Fail($"Expected call to {nameof(SealCore)}.");
            }

            if (OnExportCore is not null && ExportCoreCount == 0)
            {
                Assert.Fail($"Expected call to {nameof(ExportCore)}.");
            }

            _disposed = true;
        }

        private T GetCallback<T>(T callback, [CallerMemberName] string caller = null) where T : Delegate
        {
            if (_disposed)
            {
                Assert.Fail($"Unexpected call to {caller} after Dispose.");
            }

            return callback ?? throw new XunitException($"Unexpected call to {caller}.");
        }

        internal delegate void SealCoreCallback(
            ReadOnlySpan<byte> plaintext, Span<byte> ciphertext, ReadOnlySpan<byte> associatedData);
        internal delegate void ExportCoreCallback(ReadOnlySpan<byte> exporterContext, Span<byte> destination);
    }
}
