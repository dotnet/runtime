// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.Tests
{
    public static class HpkeRecipientContractTests
    {
        private static readonly HpkeSuite s_suite = new(
            HpkeKem.MLKEM_768, HpkeKdf.SHAKE256, HpkeAead.AES_128_GCM);

        [Fact]
        public static void Constructor_NullSuite()
        {
            AssertExtensions.Throws<ArgumentNullException>("suite", () => new HpkeRecipientContract(null));
        }

        [Theory]
        [MemberData(nameof(HpkeTestData.RepresentativeSuites), MemberType = typeof(HpkeTestData))]
        public static void Constructor_SetsSuite(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            using (HpkeRecipientContract recipient = new(suite))
            {
                Assert.Equal(suite, recipient.Suite);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        public static void Dispose_CallsCoreOnce(int disposeCalls)
        {
            int calls = 0;
            HpkeRecipientContract recipient = new(s_suite)
            {
                OnDispose = disposing =>
                {
                    Assert.True(disposing);
                    calls++;
                },
            };

            for (int i = 0; i < disposeCalls; i++)
            {
                recipient.Dispose();
            }

            Assert.Equal(1, calls);
        }

        [Fact]
        public static void Disposed_OperationsDoNotCallCore()
        {
            using (HpkeRecipientContract recipient = new(s_suite))
            {
                recipient.Dispose();

                foreach (Action operation in Operations(recipient))
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
            HpkeRecipientContract recipient = new(s_suite)
            {
                OnDispose = disposing =>
                {
                    Assert.True(disposing);
                    calls++;
                    throw exception;
                },
            };

            Assert.Same(exception, Assert.Throws<InvalidOperationException>(() => recipient.Dispose()));
            recipient.Dispose();
            Assert.Equal(1, calls);

            foreach (Action operation in Operations(recipient))
            {
                Assert.Throws<ObjectDisposedException>(operation);
            }
        }

        [Theory]
        [MemberData(nameof(HpkeTestData.RepresentativeSuites), MemberType = typeof(HpkeTestData))]
        public static void Open_Allocated(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            foreach (int length in new[] { 0, 1, 16, 17, 32 })
            foreach (bool useSpan in new[] { false, true })
            {
                byte[] ciphertext = Data(suite.GetCiphertextLength(length));
                byte[] expectedCiphertext = (byte[])ciphertext.Clone();
                byte[] associatedData = [0x71, 0x72, 0x73];

                using (HpkeRecipientContract recipient = new(suite)
                {
                    OnOpenCore = (ct, p, aad) =>
                    {
                        AssertExtensions.SequenceEqual(expectedCiphertext.AsSpan(), ct);
                        AssertExtensions.SequenceEqual(associatedData.AsSpan(), aad);
                        p.Fill(0xE7);
                    },
                })
                {
                    byte[] plaintext = useSpan
                        ? recipient.Open(ciphertext.AsSpan(), associatedData: associatedData.AsSpan())
                        : recipient.Open(ciphertext, associatedData: associatedData);
                    Assert.Equal(length, plaintext.Length);
                    AssertExtensions.FilledWith<byte>(0xE7, plaintext);
                    Assert.Equal(expectedCiphertext, ciphertext);
                    Assert.Equal(new byte[] { 0x71, 0x72, 0x73 }, associatedData);
                    Assert.Equal(1, recipient.OpenCoreCount);
                    Assert.Equal(0, recipient.ExportCoreCount);
                }
            }
        }

        [Theory]
        [MemberData(nameof(HpkeTestData.RepresentativeSuites), MemberType = typeof(HpkeTestData))]
        public static void Open_Exact(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            HpkeSuite suite = new(kem, kdf, aead);

            foreach (int length in new[] { 0, 1, 32 })
            {
                byte[] input = Data(suite.GetCiphertextLength(length) + 2);
                Memory<byte> ciphertext = input.AsMemory(1, input.Length - 2);
                byte[] expected = ciphertext.ToArray();
                byte[] aadBuffer = [0xA5, 0x71, 0x72, 0x73, 0xA5];
                Memory<byte> associatedData = aadBuffer.AsMemory(1, 3);
                byte[] output = new byte[length + 2];
                output.AsSpan().Fill(0xA5);

                using (HpkeRecipientContract recipient = new(suite)
                {
                    OnOpenCore = (ct, p, aad) =>
                    {
                        AssertExtensions.SequenceEqual(expected.AsSpan(), ct);
                        AssertExtensions.SequenceEqual(associatedData.Span, aad);
                        p.Fill(0xE7);
                    },
                })
                {
                    recipient.Open(ciphertext.Span, output.AsSpan(1, length), associatedData.Span);
                    AssertGuardedOutput(output);
                    Assert.Equal(Data(input.Length), input);
                    Assert.Equal(new byte[] { 0xA5, 0x71, 0x72, 0x73, 0xA5 }, aadBuffer);
                    Assert.Equal(1, recipient.OpenCoreCount);
                    Assert.Equal(0, recipient.ExportCoreCount);
                }
            }
        }

        [Fact]
        public static void Open_OptionalAssociatedDataIsEmpty()
        {
            byte[] ciphertext = Data(s_suite.GetCiphertextLength(1));

            using (HpkeRecipientContract recipient = new(s_suite)
            {
                OnOpenCore = (ct, p, aad) =>
                {
                    AssertExtensions.SequenceEqual(ciphertext.AsSpan(), ct);
                    Assert.True(aad.IsEmpty);
                    p.Fill(0xE7);
                },
            })
            {
                byte[] first = recipient.Open(ciphertext);
                byte[] second = recipient.Open(ciphertext.AsSpan());
                byte[] third = recipient.Open(ciphertext, associatedData: null);
                byte[] destination = new byte[1];
                recipient.Open(ciphertext, destination.AsSpan());
                Assert.Equal(first, second);
                Assert.Equal(first, third);
                Assert.Equal(first, destination);
                AssertExtensions.FilledWith<byte>(0xE7, destination);
                Assert.Equal(4, recipient.OpenCoreCount);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void NullArgumentsBeforeDisposal(bool disposed)
        {
            using (HpkeRecipientContract recipient = new(s_suite))
            {
                if (disposed)
                {
                    recipient.Dispose();
                }

                AssertExtensions.Throws<ArgumentNullException>("ciphertext", () => recipient.Open((byte[])null));
                AssertExtensions.Throws<ArgumentNullException>("exporterContext",
                    () => recipient.Export((byte[])null, 0));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Open_ShortCiphertextBeforeDisposal(bool disposed)
        {
            using (HpkeRecipientContract recipient = new(s_suite))
            {
                if (disposed)
                {
                    recipient.Dispose();
                }

                foreach (int length in new[] { 0, s_suite.AeadTagSizeInBytes - 1 })
                {
                    byte[] ciphertext = Data(length);
                    byte[] destination = Data(1);
                    AssertExtensions.Throws<ArgumentException>("ciphertext", () => recipient.Open(ciphertext));
                    AssertExtensions.Throws<ArgumentException>("ciphertext",
                        () => recipient.Open(ciphertext.AsSpan()));
                    AssertExtensions.Throws<ArgumentException>("ciphertext",
                        () => recipient.Open(ciphertext, destination.AsSpan()));
                    Assert.Equal(Data(1), destination);
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Open_InvalidDestinationBeforeDisposal(bool disposed)
        {
            byte[] ciphertext = Data(s_suite.GetCiphertextLength(32));

            using (HpkeRecipientContract recipient = new(s_suite))
            {
                if (disposed)
                {
                    recipient.Dispose();
                }

                foreach (int length in new[] { 0, 31, 33 })
                {
                    byte[] destination = new byte[length];
                    destination.AsSpan().Fill(0xA5);
                    AssertExtensions.Throws<ArgumentException>("plaintext",
                        () => recipient.Open(ciphertext, destination.AsSpan()));
                    AssertExtensions.FilledWith<byte>(0xA5, destination);
                }
            }
        }

        public static IEnumerable<object[]> OpenOverlaps()
        {
            foreach (bool overlapCiphertext in new[] { false, true })
            {
                int inputLength = overlapCiphertext ? s_suite.GetCiphertextLength(32) : 16;

                foreach (int offset in new[] { -1, 0, 1, inputLength - 1, 1 - 32 })
                {
                    yield return new object[] { overlapCiphertext, offset };
                }
            }
        }

        [Theory]
        [MemberData(nameof(OpenOverlaps))]
        public static void Open_OverlapsRejectedBeforeDisposal(bool overlapCiphertext, int offset)
        {
            foreach (bool disposed in new[] { false, true })
            {
                byte[] ciphertext = new byte[256];
                byte[] aad = new byte[256];
                byte[] output = overlapCiphertext ? ciphertext : aad;
                output.AsSpan().Fill(0xA5);

                using (HpkeRecipientContract recipient = new(s_suite))
                {
                    if (disposed)
                    {
                        recipient.Dispose();
                    }

                    Assert.Throws<CryptographicException>(() => recipient.Open(
                        ciphertext.AsSpan(80, s_suite.GetCiphertextLength(32)),
                        output.AsSpan(80 + offset, 32),
                        aad.AsSpan(80, 16)));
                    AssertExtensions.FilledWith<byte>(0xA5, output);
                }
            }
        }

        [Fact]
        public static void Open_ReadOnlyOverlapAndAdjacentOutput()
        {
            int ciphertextLength = s_suite.GetCiphertextLength(32);
            byte[] buffer = Data(ciphertextLength + 32);
            byte[] expected = buffer.AsSpan(0, ciphertextLength).ToArray();

            using (HpkeRecipientContract recipient = new(s_suite)
            {
                OnOpenCore = (ct, p, aad) =>
                {
                    AssertExtensions.SequenceEqual(expected.AsSpan(), ct);
                    AssertExtensions.SequenceEqual(expected.AsSpan(0, 16), aad);
                    p.Fill(0xE7);
                },
            })
            {
                recipient.Open(
                    buffer.AsSpan(0, ciphertextLength), buffer.AsSpan(ciphertextLength), buffer.AsSpan(0, 16));
                AssertExtensions.SequenceEqual(expected.AsSpan(), buffer.AsSpan(0, ciphertextLength));
                AssertExtensions.FilledWith<byte>(0xE7, buffer.AsSpan(ciphertextLength));
                Assert.Equal(1, recipient.OpenCoreCount);
            }
        }

        [Fact]
        public static void Open_EmptyOutputMayShareInputBuffer()
        {
            byte[] buffer = Data(s_suite.AeadTagSizeInBytes);
            byte[] original = (byte[])buffer.Clone();

            using (HpkeRecipientContract recipient = new(s_suite)
            {
                OnOpenCore = (ct, p, aad) =>
                {
                    AssertExtensions.SequenceEqual(original.AsSpan(), ct);
                    Assert.True(p.IsEmpty);
                    Assert.True(aad.IsEmpty);
                },
            })
            {
                recipient.Open(buffer.AsSpan(), buffer.AsSpan(1, 0), buffer.AsSpan(2, 0));
                Assert.Equal(original, buffer);
                Assert.Equal(1, recipient.OpenCoreCount);
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

                using (HpkeRecipientContract recipient = new(suite)
                {
                    OnExportCore = (c, destination) =>
                    {
                        AssertExtensions.SequenceEqual(expectedContext.AsSpan(), c);
                        Assert.Equal(length, destination.Length);
                        destination.Fill(0xE7);
                    },
                })
                {
                    byte[] first = recipient.Export(context, length);
                    byte[] second = recipient.Export(context.AsSpan(), length);
                    recipient.Export(context, output.AsSpan(1, length));
                    Assert.Equal(length, first.Length);
                    AssertExtensions.FilledWith<byte>(0xE7, first);
                    Assert.Equal(first, second);
                    AssertGuardedOutput(output);
                    Assert.Equal(expectedContext, context);
                    Assert.Equal(3, recipient.ExportCoreCount);
                    Assert.Equal(0, recipient.OpenCoreCount);
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
                using (HpkeRecipientContract recipient = new(suite))
                {
                    if (disposed)
                    {
                        recipient.Dispose();
                    }

                    foreach (int length in new[] { -1, int.MinValue, maximumLength + 1, int.MaxValue })
                    {
                        AssertExtensions.Throws<ArgumentOutOfRangeException>("length",
                            () => recipient.Export(Array.Empty<byte>(), length));
                        AssertExtensions.Throws<ArgumentOutOfRangeException>("length",
                            () => recipient.Export(ReadOnlySpan<byte>.Empty, length));
                    }

                    byte[] destination = new byte[maximumLength + 1];
                    destination.AsSpan().Fill(0xA5);
                    AssertExtensions.Throws<ArgumentException>("destination",
                        () => recipient.Export(ReadOnlySpan<byte>.Empty, destination.AsSpan()));
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

                using (HpkeRecipientContract recipient = new(s_suite))
                {
                    if (disposed)
                    {
                        recipient.Dispose();
                    }

                    Assert.Throws<CryptographicException>(() =>
                        recipient.Export(buffer.AsSpan(48, 16), buffer.AsSpan(48 + offset, 32)));
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

            using (HpkeRecipientContract recipient = new(s_suite)
            {
                OnExportCore = (context, destination) =>
                {
                    AssertExtensions.SequenceEqual(original.AsSpan(0, contextLength), context);
                    Assert.Equal(outputLength, destination.Length);
                    destination.Fill(0xE7);
                },
            })
            {
                recipient.Export(buffer.AsSpan(0, contextLength), buffer.AsSpan(contextLength, outputLength));
                AssertExtensions.SequenceEqual(original.AsSpan(0, contextLength), buffer.AsSpan(0, contextLength));
                AssertExtensions.FilledWith<byte>(0xE7, buffer.AsSpan(contextLength, outputLength));
                Assert.Equal(original[original.Length - 1], buffer[buffer.Length - 1]);
                Assert.Equal(1, recipient.ExportCoreCount);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Open_CoreFailurePropagatesUnchanged(bool authenticationFailure)
        {
            CryptographicException exception = authenticationFailure
                ? new AuthenticationTagMismatchException()
                : new CryptographicException();
            byte[] ciphertext = new byte[s_suite.AeadTagSizeInBytes];

            using (HpkeRecipientContract recipient = new(s_suite)
            {
                OnOpenCore = (ct, p, aad) => throw exception,
            })
            {
                Assert.Same(exception, Assert.Throws(exception.GetType(), () => recipient.Open(ciphertext)));
                Assert.Same(exception, Assert.Throws(exception.GetType(), () => recipient.Open(ciphertext.AsSpan())));
                Assert.Same(exception, Assert.Throws(exception.GetType(),
                    () => recipient.Open(ciphertext, Span<byte>.Empty)));
                Assert.Equal(3, recipient.OpenCoreCount);
                Assert.Equal(0, recipient.ExportCoreCount);
            }
        }

        [Fact]
        public static void Export_CoreFailurePropagatesUnchanged()
        {
            CryptographicException exception = new();

            using (HpkeRecipientContract recipient = new(s_suite)
            {
                OnExportCore = (context, destination) => throw exception,
            })
            {
                Assert.Same(exception, Assert.Throws<CryptographicException>(
                    () => recipient.Export(Array.Empty<byte>(), 0)));
                Assert.Same(exception, Assert.Throws<CryptographicException>(
                    () => recipient.Export(ReadOnlySpan<byte>.Empty, 1)));
                Assert.Same(exception, Assert.Throws<CryptographicException>(
                    () => recipient.Export(ReadOnlySpan<byte>.Empty, new byte[1].AsSpan())));
                Assert.Equal(3, recipient.ExportCoreCount);
                Assert.Equal(0, recipient.OpenCoreCount);
            }
        }

        private static IEnumerable<Action> Operations(HpkeRecipient recipient)
        {
            byte[] ciphertext = new byte[recipient.Suite.AeadTagSizeInBytes];
            yield return () => recipient.Open(ciphertext);
            yield return () => recipient.Open(ciphertext.AsSpan());
            yield return () => recipient.Open(ciphertext, Span<byte>.Empty);
            yield return () => recipient.Export(Array.Empty<byte>(), 0);
            yield return () => recipient.Export(ReadOnlySpan<byte>.Empty, 1);
            yield return () => recipient.Export(ReadOnlySpan<byte>.Empty, new byte[1].AsSpan());
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

    internal sealed class HpkeRecipientContract : HpkeRecipient
    {
        private bool _disposed;

        internal OpenCoreCallback OnOpenCore { get; set; }
        internal ExportCoreCallback OnExportCore { get; set; }
        internal Action<bool> OnDispose { get; set; } = static disposing => { };
        internal int OpenCoreCount { get; private set; }
        internal int ExportCoreCount { get; private set; }

        internal HpkeRecipientContract(HpkeSuite suite) : base(suite)
        {
        }

        protected override void OpenCore(
            ReadOnlySpan<byte> ciphertext,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData)
        {
            OpenCoreCount++;
            Assert.InRange(ciphertext.Length, Suite.AeadTagSizeInBytes, int.MaxValue);
            Assert.Equal(ciphertext.Length - Suite.AeadTagSizeInBytes, plaintext.Length);
            GetCallback(OnOpenCore)(ciphertext, plaintext, associatedData);
        }

        protected override void ExportCore(ReadOnlySpan<byte> exporterContext, Span<byte> destination)
        {
            ExportCoreCount++;
            GetCallback(OnExportCore)(exporterContext, destination);
        }

        protected override void Dispose(bool disposing)
        {
            GetCallback(OnDispose)(disposing);

            if (OnOpenCore is not null && OpenCoreCount == 0)
            {
                Assert.Fail($"Expected call to {nameof(OpenCore)}.");
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

        internal delegate void OpenCoreCallback(
            ReadOnlySpan<byte> ciphertext, Span<byte> plaintext, ReadOnlySpan<byte> associatedData);
        internal delegate void ExportCoreCallback(ReadOnlySpan<byte> exporterContext, Span<byte> destination);
    }
}
