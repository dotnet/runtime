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
                new[] { typeof(Stream), typeof(Stream), typeof(long) },
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
                decompressedStream, decryptStream, (long)plaintext.Length
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

                byte[] buffer = new byte[1];
                if (async)
                {
                    await Assert.ThrowsAsync<InvalidDataException>(async () => await authenticatedStream.ReadAsync(buffer));
                }
                else
                {
                    Assert.Throws<InvalidDataException>(() => authenticatedStream.Read(buffer));
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

        public static IEnumerable<object[]> PayloadLongerThanDeclaredLengthCases()
        {
            // bulkOvershoot: a single large read delivers more than the declared length at once, vs. byte-at-a-time
            // reads that land exactly on the declared length first, so the 1-byte boundary probe finds the excess.
            // simulateAe1WithCrcValidation: AE-1 entries validate CRC (CrcValidatingReadStream) below the
            // authentication guard and can throw their own length-mismatch error before AuthenticatedReadStream's
            // own bookkeeping gets a turn - authentication must still run first.
            foreach (bool async in new[] { false, true })
            {
                foreach (bool corruptAuthenticationCode in new[] { false, true })
                {
                    foreach (bool bulkOvershoot in new[] { false, true })
                    {
                        foreach (bool simulateAe1WithCrcValidation in new[] { false, true })
                        {
                            yield return new object[] { async, corruptAuthenticationCode, bulkOvershoot, simulateAe1WithCrcValidation };
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(PayloadLongerThanDeclaredLengthCases))]
        public async Task PayloadStreamProducesMoreThanDeclaredLength_AuthenticationIsCompleted(
            bool async, bool corruptAuthenticationCode, bool bulkOvershoot, bool simulateAe1WithCrcValidation)
        {
            byte[] plaintext = new byte[4096];
            new Random(42).NextBytes(plaintext);

            // Declare a length shorter than the actual plaintext so the payload stream legitimately produces more
            // data than AuthenticatedReadStream expects, exercising the overshoot-detection paths rather than the
            // truncation path covered above.
            long declaredLength = plaintext.Length - 16;

            (byte[] encryptedData, object decryptKeyMaterial) = CreateEncryptedData(plaintext);
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

            // Force single-byte reads so the exact-length boundary probe - rather than a single bulk read - is the
            // one that discovers the excess byte, when bulkOvershoot is false.
            Stream decryptedPayloadStream = bulkOvershoot ? decryptStream : new ClampedReadStream(decryptStream, readSizeLimit: 1);

            // CRC computed over just the declared-length prefix, so CrcValidatingReadStream validates cleanly at
            // the boundary and only raises its own UnexpectedStreamLength once the excess byte arrives - exactly
            // like a real AE-1 entry whose declared length disagrees with its actual (tampered) content.
            Stream payloadStream = simulateAe1WithCrcValidation
                ? (Stream)s_crcValidatingReadStreamConstructor.Invoke(new object[]
                    { decryptedPayloadStream, ComputeCrc32(plaintext.AsSpan(0, (int)declaredLength)), declaredLength })
                : decryptedPayloadStream;

            using var authenticatedStream = (Stream)s_createAuthenticatedStreamMethod.Invoke(null, new object[]
            {
                payloadStream, decryptStream, declaredLength
            })!;

            using var output = new MemoryStream();
            InvalidDataException ex = async
                ? await Assert.ThrowsAsync<InvalidDataException>(() => authenticatedStream.CopyToAsync(output, CancellationToken.None))
                : Assert.Throws<InvalidDataException>(() => authenticatedStream.CopyTo(output));

            if (corruptAuthenticationCode)
            {
                // A bad HMAC must still be detected and surface even though the entry is also the wrong length -
                // authentication cannot be silently skipped just because the overshoot error (whether raised by
                // AuthenticatedReadStream itself or by a CRC-validating payload stream below it) fires first.
                Assert.Contains("authentication code", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                Assert.Contains("decompressed data length", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        public static IEnumerable<object[]> DecompressorEndsBeforeDeclaredLengthCases()
        {
            foreach (bool async in new[] { false, true })
            {
                foreach (bool corruptAuthenticationCode in new[] { false, true })
                {
                    yield return new object[] { async, corruptAuthenticationCode };
                }
            }
        }

        [Theory]
        [MemberData(nameof(DecompressorEndsBeforeDeclaredLengthCases))]
        public async Task DecompressorProducesLessThanDeclaredLength_AuthenticationIsCompleted(
            bool async, bool corruptAuthenticationCode)
        {
            byte[] plaintext = new byte[4096];
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

            // Pad with trailing ciphertext the decompressor never needs to reach, so authentication still has
            // unread encrypted data to drain through CompleteAuthenticationCoreAsync's drain loop.
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

            using Stream decompressedStream = new DeflateStream(decryptStream, CompressionMode.Decompress, leaveOpen: true);

            // Declare more plaintext than the decompressor will actually produce, so the decompressor reports its
            // own EOF (bytesRead == 0) before AuthenticatedReadStream reaches the declared length - the truncation
            // branch, as opposed to the overshoot branch covered above.
            long declaredLength = plaintext.Length + 16;

            using var authenticatedStream = (Stream)s_createAuthenticatedStreamMethod.Invoke(null, new object[]
            {
                decompressedStream, decryptStream, declaredLength
            })!;

            using var output = new MemoryStream();
            InvalidDataException ex = async
                ? await Assert.ThrowsAsync<InvalidDataException>(() => authenticatedStream.CopyToAsync(output, CancellationToken.None))
                : Assert.Throws<InvalidDataException>(() => authenticatedStream.CopyTo(output));

            if (corruptAuthenticationCode)
            {
                // A bad HMAC must still be detected and surface even though the entry is also truncated -
                // authentication cannot be silently skipped just because the decompressor hit EOF first.
                Assert.Contains("authentication code", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                Assert.Contains("decompressed data length", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task CompleteAuthenticationCoreAsync_ResumesAfterCancellationMidTrailerRead()
        {
            byte[] plaintext = "Hello, authenticated world!"u8.ToArray();
            (byte[] encryptedData, object decryptKeyMaterial) = CreateEncryptedData(plaintext);

            // Delivers at most one byte per read, so the resumable 10-byte trailer read in
            // CompleteAuthenticationCoreAsync requires multiple underlying reads and can be cancelled mid-way.
            using var cancelableStream = new CancelAfterReadsStream(encryptedData);
            using Stream decryptStream = (Stream)s_createMethod.Invoke(null, new object[]
            {
                cancelableStream, decryptKeyMaterial, (long)encryptedData.Length, false, false
            })!;

            byte[] buffer = new byte[plaintext.Length];
            int totalRead = 0;
            while (totalRead < plaintext.Length)
            {
                totalRead += await decryptStream.ReadAsync(buffer.AsMemory(totalRead));
            }
            Assert.Equal(plaintext, buffer);

            // Cancel partway (after 3 of the 10 trailer bytes) through the resumable authentication-code read.
            using var cts = new CancellationTokenSource();
            cancelableStream.CancelAfter(cts, readCount: 3);
            byte[] probe = new byte[1];
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => decryptStream.ReadAsync(probe, cts.Token).AsTask());

            // A retry with a fresh token must resume from the 4th trailer byte - not lose or re-read the first
            // 3 - and still successfully complete authentication.
            int bytesRead = await decryptStream.ReadAsync(probe);
            Assert.Equal(0, bytesRead);
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
                payloadStream, decryptStream, (long)plaintext.Length
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
