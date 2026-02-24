// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.IO.Tests;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    /// <summary>
    /// Wrapped connected stream conformance tests for ZipCryptoStream.
    /// Tests encryption → decryption data flow through connected streams.
    /// </summary>
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNativeAot))]
    public class ZipCryptoStreamWrappedConformanceTests : WrappingConnectedStreamConformanceTests
    {
        private const string TestPassword = "test-password";
        private const ushort PasswordVerifier = 0x1234;

        private static readonly Type s_zipCryptoStreamType;
        private static readonly MethodInfo s_createKeyMethod;
        private static readonly MethodInfo s_createEncryptionMethod;
        private static readonly MethodInfo s_createDecryptionMethod;
        private static readonly MethodInfo s_createAsyncMethod;

        static ZipCryptoStreamWrappedConformanceTests()
        {
            var assembly = typeof(ZipArchive).Assembly;
            s_zipCryptoStreamType = assembly.GetType("System.IO.Compression.ZipCryptoStream", throwOnError: true)!;

            s_createKeyMethod = s_zipCryptoStreamType.GetMethod("CreateKey",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(ReadOnlyMemory<char>) },
                null)!;

            s_createEncryptionMethod = s_zipCryptoStreamType.GetMethod("Create",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Stream), typeof(byte[]), typeof(ushort), typeof(bool), typeof(uint?), typeof(bool) },
                null)!;

            s_createDecryptionMethod = s_zipCryptoStreamType.GetMethod("Create",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Stream), typeof(byte[]), typeof(byte), typeof(bool), typeof(bool) },
                null)!;

            s_createAsyncMethod = s_zipCryptoStreamType.GetMethod("CreateAsync",
               BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
               null,
               new[] { typeof(Stream), typeof(byte[]), typeof(byte), typeof(bool), typeof(CancellationToken), typeof(bool) },
               null);
        }

        // ZipCryptoStream doesn't support seeking
        protected override bool CanSeek => false;

        protected override bool FlushGuaranteesAllDataWritten => true;

        // ZipCrypto uses streaming cipher - blocks on zero byte reads
        protected override bool BlocksOnZeroByteReads => true;

        // No concurrent exception type - single-threaded stream
        protected override Type UnsupportedConcurrentExceptionType => null!;

        protected override Task<StreamPair> CreateConnectedStreamsAsync()
        {
            // Create bidirectional connected streams with sufficient buffer for header
            (Stream stream1, Stream stream2) = ConnectedStreams.CreateBidirectional(4 * 1024, 16 * 1024);
            return CreateWrappedConnectedStreamsAsync((stream1, stream2), leaveOpen: false);
        }

        protected override async Task<StreamPair> CreateWrappedConnectedStreamsAsync(StreamPair wrapped, bool leaveOpen = false)
        {
            byte[] keyBytes = (byte[])s_createKeyMethod.Invoke(null, new object[] { TestPassword.AsMemory() })!;

            // The check byte is the HIGH byte of the password verifier (little-endian format)
            byte expectedCheckByte = (byte)(PasswordVerifier >> 8); // 0x12

            // Create the encryption stream (write-only) - wraps stream1
            var encryptStream = (Stream)s_createEncryptionMethod.Invoke(null, new object?[]
            {
                wrapped.Stream1, keyBytes, PasswordVerifier, true /* encrypting */, null /* crc32 */, leaveOpen
            })!;

            // Write and flush the header so the decryption stream can read it
            // ZipCrypto header is written lazily on first write, so we need to trigger it
            // Use async operations to support AsyncOnlyStream wrappers used by conformance tests
            await encryptStream.WriteAsync(new byte[] { 0 }, 0, 1).ConfigureAwait(false); // Trigger header write
            await encryptStream.FlushAsync().ConfigureAwait(false);

            // Now create the decryption stream (read-only) - wraps stream2
            // This will read and validate the 12-byte header
            // Use async factory method to support AsyncOnlyStream wrappers
            var decryptStream = await CreateDecryptStreamAsync(wrapped.Stream2, keyBytes, expectedCheckByte, leaveOpen).ConfigureAwait(false);

            // Read the byte we wrote to trigger the header
            byte[] readBuffer = new byte[1];
            await decryptStream.ReadAsync(readBuffer, 0, 1).ConfigureAwait(false);

            return (encryptStream, decryptStream);
        }

        private static async Task<Stream> CreateDecryptStreamAsync(Stream baseStream, byte[] keyBytes, byte expectedCheckByte, bool leaveOpen)
        {
            // CreateAsync returns Task<ZipCryptoStream>, await it and get the result
            var task = (Task)s_createAsyncMethod.Invoke(null, new object[]
            {
                    baseStream, keyBytes, expectedCheckByte, false /* encrypting */, CancellationToken.None, leaveOpen
            })!;
            await task.ConfigureAwait(false);

            // Get the Result property from the completed task
            var resultProperty = task.GetType().GetProperty("Result")!;
            return (Stream)resultProperty.GetValue(task)!;

        }
    }
}
