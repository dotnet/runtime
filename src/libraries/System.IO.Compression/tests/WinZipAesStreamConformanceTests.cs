// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Compression.Tests.Utilities;
using System.IO.Tests;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    /// <summary>
    /// Conformance tests for WinZipAesStream (AES-128).
    /// </summary>
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNativeAot))]
    [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on browser.")]
    [UnsupportedOSPlatform("browser")]
    public sealed class WinZipAes128StreamConformanceTests : WinZipAesStreamConformanceTests
    {
        protected override int KeySizeBits => 128;
    }

    /// <summary>
    /// Conformance tests for WinZipAesStream (AES-256).
    /// </summary>
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNativeAot))]
    [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on browser.")]
    [UnsupportedOSPlatform("browser")]
    public sealed class WinZipAes256StreamConformanceTests : WinZipAesStreamConformanceTests
    {
        protected override int KeySizeBits => 256;
    }

    /// <summary>
    /// Base class for WinZipAesStream conformance tests.
    /// </summary>
    public abstract class WinZipAesStreamConformanceTests : StandaloneStreamConformanceTests
    {
        private const int AuthenticationCodeSize = 10;
        private const string TestPassword = "PLACEHOLDER";

        private delegate object CreateKeyDelegate(ReadOnlySpan<char> password, byte[]? salt, int keySizeBits);
        private static readonly CreateKeyDelegate s_createKey;
        private static readonly MethodInfo s_createAuthenticatedStreamMethod;
        private static readonly MethodInfo s_createMethod;
        private static readonly ConstructorInfo s_deflateManagedStreamConstructor;
        private static readonly ConstructorInfo s_crcValidatingReadStreamConstructor;

        protected abstract int KeySizeBits { get; }
        protected int SaltSize => KeySizeBits / 16;

        static WinZipAesStreamConformanceTests()
        {
            Type winZipAesStreamType = Type.GetType("System.IO.Compression.WinZipAesStream, System.IO.Compression")!;
            Type winZipAesKeyMaterialType = Type.GetType("System.IO.Compression.WinZipAesKeyMaterial, System.IO.Compression")!;

            MethodInfo createKeyMethod = winZipAesKeyMaterialType.GetMethod("Create",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(ReadOnlySpan<char>), typeof(byte[]), typeof(int) },
                null)!;

            // CreateDelegate can't handle value-type return covariance (struct → object boxing).
            // Use DynamicMethod to emit a wrapper that calls the target and boxes the result.
#pragma warning disable IL3050 // RequiresDynamicCode: DynamicMethod is not supported in AOT; these tests are skipped under NativeAOT.
            var dm = new System.Reflection.Emit.DynamicMethod(
                "CreateKeyWrapper",
                typeof(object),
                new[] { typeof(ReadOnlySpan<char>), typeof(byte[]), typeof(int) },
                typeof(WinZipAesStreamConformanceTests).Module,
                skipVisibility: true);
            var il = dm.GetILGenerator();
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_2);
            il.Emit(System.Reflection.Emit.OpCodes.Call, createKeyMethod);
            il.Emit(System.Reflection.Emit.OpCodes.Box, winZipAesKeyMaterialType);
            il.Emit(System.Reflection.Emit.OpCodes.Ret);
            s_createKey = dm.CreateDelegate<CreateKeyDelegate>();
#pragma warning restore IL3050

            s_createMethod = winZipAesStreamType.GetMethod("Create",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Stream), winZipAesKeyMaterialType, typeof(long), typeof(bool), typeof(bool) },
                null)!;

            s_createAuthenticatedStreamMethod = winZipAesStreamType.GetMethod("CreateAuthenticatedStream",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Stream), typeof(Stream) },
                null)!;

            Type deflateManagedStreamType = Type.GetType("System.IO.Compression.DeflateManagedStream, System.IO.Compression")!;
            s_deflateManagedStreamConstructor = deflateManagedStreamType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(Stream), typeof(ZipCompressionMethod), typeof(long) },
                null)!;

            Type crcValidatingReadStreamType = Type.GetType("System.IO.Compression.CrcValidatingReadStream, System.IO.Compression")!;
            s_crcValidatingReadStreamConstructor = crcValidatingReadStreamType.GetConstructor(
                new[] { typeof(Stream), typeof(uint), typeof(long) })!;
        }

        protected override bool CanSeek => false;

        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData) =>
            Task.FromResult<Stream?>(null); // WinZipAesStream is either read-only or write-only

        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData)
        {
            var ms = new MemoryStream();
            object keyMaterial = s_createKey(TestPassword, null, KeySizeBits);

            var encryptStream = (Stream)s_createMethod.Invoke(null, new object[]
            {
                ms, keyMaterial, -1L, true, false
            })!;

            if (initialData != null && initialData.Length > 0)
            {
                encryptStream.Write(initialData);
            }

            return Task.FromResult<Stream?>(encryptStream);
        }

        protected override Task<Stream?> CreateReadOnlyStreamCore(byte[]? initialData)
        {
            byte[] plaintext = initialData ?? Array.Empty<byte>();
            (byte[] encryptedData, object decryptKeyMaterial) = CreateEncryptedData(plaintext);

            var ms = new MemoryStream(encryptedData);
            var decryptStream = (Stream)s_createMethod.Invoke(null, new object[]
            {
                ms, decryptKeyMaterial, (long)encryptedData.Length, false, false
            })!;

            return Task.FromResult<Stream?>(decryptStream);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WinZipAesStream_AuthenticatesOnFinalPositiveCiphertextRead(bool async)
        {
            byte[] plaintext = "authenticated payload"u8.ToArray();
            (byte[] encryptedData, object decryptKeyMaterial) = CreateEncryptedData(plaintext);
            byte[] hmacKey = (byte[])decryptKeyMaterial.GetType().GetProperty("HmacKey")!.GetValue(decryptKeyMaterial)!;
            HMACSHA1.HashData(hmacKey, ReadOnlySpan<byte>.Empty)
                .AsSpan(0, AuthenticationCodeSize)
                .CopyTo(encryptedData.AsSpan(^AuthenticationCodeSize));

            using var encryptedStream = new CountingReadStream(encryptedData, maxReadSize: 1);
            using Stream decryptStream = (Stream)s_createMethod.Invoke(null, new object[]
            {
                encryptedStream, decryptKeyMaterial, (long)encryptedData.Length, false, false
            })!;

            byte[] buffer = new byte[1];
            for (int i = 0; i < plaintext.Length - 1; i++)
            {
                int bytesRead = async ? await decryptStream.ReadAsync(buffer) : decryptStream.Read(buffer);
                Assert.Equal(1, bytesRead);
                Assert.Equal(plaintext[i], buffer[0]);
            }

            InvalidDataException ex = async
                ? await Assert.ThrowsAsync<InvalidDataException>(() => decryptStream.ReadAsync(buffer).AsTask())
                : Assert.Throws<InvalidDataException>(() => decryptStream.Read(buffer));
            Assert.Contains("authentication code", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task WinZipAesStream_ZeroLengthReadAndEarlyDisposeDoNotAuthenticate(bool async, bool wrapAuthenticated)
        {
            (byte[] encryptedData, object decryptKeyMaterial) = CreateEncryptedData(new byte[64]);
            byte[] hmacKey = (byte[])decryptKeyMaterial.GetType().GetProperty("HmacKey")!.GetValue(decryptKeyMaterial)!;
            HMACSHA1.HashData(hmacKey, ReadOnlySpan<byte>.Empty)
                .AsSpan(0, AuthenticationCodeSize)
                .CopyTo(encryptedData.AsSpan(^AuthenticationCodeSize));

            using var encryptedStream = new CountingReadStream(encryptedData);
            using Stream decryptStream = (Stream)s_createMethod.Invoke(null, new object[]
            {
                encryptedStream, decryptKeyMaterial, (long)encryptedData.Length, false, false
            })!;
            using Stream readStream = wrapAuthenticated
                ? (Stream)s_createAuthenticatedStreamMethod.Invoke(null, new object[] { decryptStream, decryptStream })!
                : decryptStream;

            int readsAfterInitialization = encryptedStream.ReadCount;
            int bytesRead = async ? await readStream.ReadAsync(Memory<byte>.Empty) : readStream.Read(Span<byte>.Empty);
            Assert.Equal(0, bytesRead);
            if (async)
            {
                await readStream.DisposeAsync();
            }
            else
            {
                readStream.Dispose();
            }

            Assert.Equal(readsAfterInitialization, encryptedStream.ReadCount);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task AuthenticatedReadStream_PayloadFailureDoesNotDrain(bool async, bool failAtEnd)
        {
            byte[] ciphertextPlaintext = new byte[64];
            (byte[] encryptedData, object decryptKeyMaterial) = CreateEncryptedData(ciphertextPlaintext);
            byte[] hmacKey = (byte[])decryptKeyMaterial.GetType().GetProperty("HmacKey")!.GetValue(decryptKeyMaterial)!;
            HMACSHA1.HashData(hmacKey, ReadOnlySpan<byte>.Empty)
                .AsSpan(0, AuthenticationCodeSize)
                .CopyTo(encryptedData.AsSpan(^AuthenticationCodeSize));

            using var encryptedStream = new CountingReadStream(encryptedData);
            using Stream decryptStream = (Stream)s_createMethod.Invoke(null, new object[]
            {
                encryptedStream, decryptKeyMaterial, (long)encryptedData.Length, false, true
            })!;
            int readsAfterInitialization = encryptedStream.ReadCount;

            byte[] payload = failAtEnd ? new byte[16] : Array.Empty<byte>();
            using var payloadStream = new FailingReadStream(payload, failAtEnd);
            using Stream authenticatedStream = (Stream)s_createAuthenticatedStreamMethod.Invoke(null, new object[]
            {
                payloadStream, decryptStream
            })!;

            byte[] buffer = new byte[payload.Length + 1];
            async Task ReadPayload()
            {
                while ((async ? await authenticatedStream.ReadAsync(buffer) : authenticatedStream.Read(buffer)) != 0)
                {
                }
            }

            IOException failure = await Assert.ThrowsAsync<IOException>(ReadPayload);

            Assert.Same(payloadStream.Failure, failure);
            Assert.Equal(readsAfterInitialization, encryptedStream.ReadCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AuthenticatedReadStream_CrcFailureDoesNotDrain(bool async)
        {
            byte[] plaintext = new byte[64];
            (byte[] encryptedData, object decryptKeyMaterial) = CreateEncryptedData(plaintext);
            using var encryptedStream = new CountingReadStream(encryptedData);
            using Stream decryptStream = (Stream)s_createMethod.Invoke(null, new object[]
            {
                encryptedStream, decryptKeyMaterial, (long)encryptedData.Length, false, true
            })!;
            int readsAfterInitialization = encryptedStream.ReadCount;
            using var payloadStream = new MemoryStream(plaintext);
            using Stream crcStream = (Stream)s_crcValidatingReadStreamConstructor.Invoke(new object[]
            {
                payloadStream, ComputeCrc32(plaintext) ^ 1u, (long)plaintext.Length
            });
            using Stream authenticatedStream = (Stream)s_createAuthenticatedStreamMethod.Invoke(null, new object[]
            {
                crcStream, decryptStream
            })!;

            byte[] buffer = new byte[plaintext.Length];
            InvalidDataException failure = async
                ? await Assert.ThrowsAsync<InvalidDataException>(() => authenticatedStream.ReadAsync(buffer).AsTask())
                : Assert.Throws<InvalidDataException>(() => authenticatedStream.Read(buffer));

            Assert.Contains("CRC", failure.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(readsAfterInitialization, encryptedStream.ReadCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WinZipAesStream_PreCanceledReadDoesNotPoisonStream(bool wrapAuthenticated)
        {
            byte[] plaintext = "retry after a pre-canceled read"u8.ToArray();
            (byte[] encryptedData, object decryptKeyMaterial) = CreateEncryptedData(plaintext);
            using var encryptedStream = new CountingReadStream(encryptedData);
            using Stream decryptStream = (Stream)s_createMethod.Invoke(null, new object[]
            {
                encryptedStream, decryptKeyMaterial, (long)encryptedData.Length, false, false
            })!;
            using Stream readStream = wrapAuthenticated
                ? (Stream)s_createAuthenticatedStreamMethod.Invoke(null, new object[] { decryptStream, decryptStream })!
                : decryptStream;

            using var cancellationSource = new CancellationTokenSource();
            cancellationSource.Cancel();
            byte[] buffer = new byte[plaintext.Length];
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => readStream.ReadAsync(buffer, cancellationSource.Token).AsTask());

            int bytesRead = await readStream.ReadAsync(buffer);
            Assert.Equal(plaintext.Length, bytesRead);
            Assert.Equal(plaintext, buffer);
        }

        public static IEnumerable<object[]> DecompressorFinishesBeforeEncryptedDataCases()
        {
            // Cross product generated via loops (rather than spelling out every InlineData combination) so each
            // axis - async/sync, valid/corrupt trailer, and Deflate vs. Deflate64 - is exercised together.
            foreach (bool async in new[] { false, true })
            {
                foreach (bool corruptAuthenticationCode in new[] { false, true })
                {
                    foreach (bool useDeflate64 in new[] { false, true })
                    {
                        yield return new object[] { async, corruptAuthenticationCode, useDeflate64 };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(DecompressorFinishesBeforeEncryptedDataCases))]
        public async Task DecompressorFinishesBeforeEncryptedData_AuthenticationIsCompleted(
            bool async, bool corruptAuthenticationCode, bool useDeflate64)
        {
            byte[] plaintext = new byte[4097];
            new Random(42).NextBytes(plaintext);

            byte[] compressedData;
            using (var compressedStream = new MemoryStream())
            {
                using (var compressionStream = new DeflateStream(compressedStream, CompressionLevel.SmallestSize, leaveOpen: true))
                {
                    compressionStream.Write(plaintext);
                }

                compressedData = compressedStream.ToArray();
            }

            byte[] compressedDataWithTrailingBytes = new byte[compressedData.Length + 16 * 1024];
            compressedData.CopyTo(compressedDataWithTrailingBytes, 0);

            (byte[] encryptedData, object decryptKeyMaterial) = CreateEncryptedData(compressedDataWithTrailingBytes);
            if (corruptAuthenticationCode)
            {
                byte[] hmacKey = (byte[])decryptKeyMaterial.GetType().GetProperty("HmacKey")!.GetValue(decryptKeyMaterial)!;
                HMACSHA1.HashData(hmacKey, ReadOnlySpan<byte>.Empty)
                    .AsSpan(0, AuthenticationCodeSize)
                    .CopyTo(encryptedData.AsSpan(^AuthenticationCodeSize));
            }

            using var encryptedStream = new MemoryStream(encryptedData);
            using var decryptStream = (Stream)s_createMethod.Invoke(null, new object[]
            {
                encryptedStream, decryptKeyMaterial, (long)encryptedData.Length, false, true
            })!;

            // Deflate64 uses a different decompressor (DeflateManagedStream) than standard Deflate, but both
            // understand the same bitstream produced above, so this also exercises the Deflate64 decoding path.
            using Stream decompressedStream = useDeflate64
                ? (Stream)s_deflateManagedStreamConstructor.Invoke(new object[] { decryptStream, ZipCompressionMethod.Deflate64, (long)plaintext.Length })
                : new DeflateStream(decryptStream, CompressionMode.Decompress, leaveOpen: true);

            using var authenticatedStream = (Stream)s_createAuthenticatedStreamMethod.Invoke(null, new object[]
            {
                decompressedStream, decryptStream
            })!;

            using var output = new MemoryStream();
            if (corruptAuthenticationCode)
            {
                if (async)
                {
                    await Assert.ThrowsAsync<InvalidDataException>(() => authenticatedStream.CopyToAsync(output, CancellationToken.None));
                }
                else
                {
                    Assert.Throws<InvalidDataException>(() => authenticatedStream.CopyTo(output));
                }

            }
            else
            {
                if (async)
                {
                    await authenticatedStream.CopyToAsync(output, CancellationToken.None);
                }
                else
                {
                    authenticatedStream.CopyTo(output);
                }

                Assert.Equal(plaintext, output.ToArray());
            }
        }

        [Fact]
        public async Task WinZipAesStream_CancellationMidTrailerReadPropagates()
        {
            byte[] plaintext = "Hello, authenticated world!"u8.ToArray();
            (byte[] encryptedData, object decryptKeyMaterial) = CreateEncryptedData(plaintext);

            // Delivers at most one byte per read, so reading the 10-byte trailer requires multiple underlying reads.
            using var cancelableStream = new CancelAfterReadsStream(encryptedData);
            using Stream decryptStream = (Stream)s_createMethod.Invoke(null, new object[]
            {
                cancelableStream, decryptKeyMaterial, (long)encryptedData.Length, false, false
            })!;

            byte[] buffer = new byte[plaintext.Length];
            int totalRead = 0;
            while (totalRead < plaintext.Length - 1)
            {
                totalRead += await decryptStream.ReadAsync(buffer.AsMemory(totalRead, 1));
            }
            Assert.Equal(plaintext.AsSpan(0, plaintext.Length - 1).ToArray(), buffer.AsSpan(0, plaintext.Length - 1).ToArray());

            // The final ciphertext byte starts eager authentication, so arm cancellation immediately before it.
            using var cts = new CancellationTokenSource();
            cancelableStream.CancelAfter(cts, readCount: 3);
            Memory<byte> lastByte = buffer.AsMemory(totalRead, 1);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => decryptStream.ReadAsync(lastByte, cts.Token).AsTask());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task AuthenticatedReadStream_Flush_DelegatesToPayloadStream(bool async, bool useDeflate64)
        {
            byte[] plaintext = "Hello, authenticated world!"u8.ToArray();

            byte[] compressedData;
            using (var compressedStream = new MemoryStream())
            {
                using (var compressionStream = new DeflateStream(compressedStream, CompressionLevel.SmallestSize, leaveOpen: true))
                {
                    compressionStream.Write(plaintext);
                }

                compressedData = compressedStream.ToArray();
            }

            (byte[] encryptedData, object decryptKeyMaterial) = CreateEncryptedData(compressedData);
            using var encryptedStream = new MemoryStream(encryptedData);
            using var decryptStream = (Stream)s_createMethod.Invoke(null, new object[]
            {
                encryptedStream, decryptKeyMaterial, (long)encryptedData.Length, false, true
            })!;
            using Stream payloadStream = useDeflate64
                ? (Stream)s_deflateManagedStreamConstructor.Invoke(new object[] { decryptStream, ZipCompressionMethod.Deflate64, (long)plaintext.Length })
                : new DeflateStream(decryptStream, CompressionMode.Decompress, leaveOpen: true);
            using Stream authenticatedStream = (Stream)s_createAuthenticatedStreamMethod.Invoke(null, new object[]
            {
                payloadStream, decryptStream
            })!;

            if (async)
            {
                await authenticatedStream.FlushAsync();
            }
            else
            {
                authenticatedStream.Flush();
            }
        }

        private static uint ComputeCrc32(ReadOnlySpan<byte> data)
        {
            const uint Polynomial = 0xEDB88320;

            uint crc = 0xFFFFFFFF;
            foreach (byte b in data)
            {
                crc ^= b;
                for (int i = 0; i < 8; i++)
                {
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ Polynomial : crc >> 1;
                }
            }

            return ~crc;
        }

        private sealed class CancelAfterReadsStream(byte[] data) : MemoryStream(data)
        {
            private CancellationTokenSource? _cancellationSource;
            private int _readsUntilCancellation;

            public void CancelAfter(CancellationTokenSource cancellationSource, int readCount)
            {
                _cancellationSource = cancellationSource;
                _readsUntilCancellation = readCount;
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                int bytesRead = await base.ReadAsync(buffer[..Math.Min(1, buffer.Length)], cancellationToken).ConfigureAwait(false);

                if (_cancellationSource is not null && --_readsUntilCancellation == 0)
                {
                    _cancellationSource.Cancel();
                }

                return bytesRead;
            }
        }

        private sealed class CountingReadStream(byte[] data, int maxReadSize = int.MaxValue) : MemoryStream(data)
        {
            public int ReadCount { get; private set; }

            public override int Read(byte[] buffer, int offset, int count)
            {
                ReadCount++;
                return base.Read(buffer, offset, Math.Min(maxReadSize, count));
            }

            public override int Read(Span<byte> buffer)
            {
                ReadCount++;
                return base.Read(buffer[..Math.Min(maxReadSize, buffer.Length)]);
            }

            public override int ReadByte()
            {
                ReadCount++;
                return base.ReadByte();
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                ReadCount++;
                return base.ReadAsync(buffer[..Math.Min(maxReadSize, buffer.Length)], cancellationToken);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ReadCount++;
                return base.ReadAsync(buffer, offset, Math.Min(maxReadSize, count), cancellationToken);
            }
        }

        private sealed class FailingReadStream(byte[] data, bool failAtEnd) : MemoryStream(data)
        {
            public IOException Failure { get; } = new("Injected payload read failure.");

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (!failAtEnd || Position == Length)
                {
                    throw Failure;
                }

                return base.Read(buffer, offset, count);
            }

            public override int Read(Span<byte> buffer)
            {
                if (!failAtEnd || Position == Length)
                {
                    throw Failure;
                }

                return base.Read(buffer);
            }

            public override int ReadByte()
            {
                if (!failAtEnd || Position == Length)
                {
                    throw Failure;
                }

                return base.ReadByte();
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(Read(buffer.Span));
            }
        }

        private (byte[] EncryptedData, object DecryptKeyMaterial) CreateEncryptedData(byte[] plaintext)
        {
            object encryptKeyMaterial = s_createKey(TestPassword, null, KeySizeBits);

            using var encryptedStream = new MemoryStream();
            using (var encryptingStream = (Stream)s_createMethod.Invoke(null, new object[]
            {
                encryptedStream, encryptKeyMaterial, -1L, true, true
            })!)
            {
                encryptingStream.Write(plaintext);
            }

            byte[] encryptedData = encryptedStream.ToArray();
            byte[] salt = new byte[SaltSize];
            Array.Copy(encryptedData, 0, salt, 0, SaltSize);

            object decryptKeyMaterial = s_createKey(TestPassword, salt, KeySizeBits);

            return (encryptedData, decryptKeyMaterial);
        }
    }
}
