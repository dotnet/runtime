// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    [ConditionalClass(typeof(ZipCryptoStreamTestsSupport), nameof(ZipCryptoStreamTestsSupport.IsSupported))]
    public class ZipCryptoStreamWrappedConformanceTests : WrappingConnectedStreamConformanceTests
    {
        private const string TestPassword = "PLACEHOLDER";
        private const ushort PasswordVerifier = 0x1234;

        private delegate object CreateKeyDelegate(ReadOnlySpan<char> password);
        private static readonly CreateKeyDelegate s_createKey;
        private static readonly MethodInfo s_createEncryptionMethod;
        private static readonly MethodInfo s_createDecryptionMethod;
        private static readonly MethodInfo s_createAsyncMethod;

        static ZipCryptoStreamWrappedConformanceTests()
        {
            Type zipCryptoStreamType = Type.GetType("System.IO.Compression.ZipCryptoStream, System.IO.Compression")!;
            Type zipCryptoKeysType = Type.GetType("System.IO.Compression.ZipCryptoKeys, System.IO.Compression")!;

            MethodInfo createKeyMethod = zipCryptoStreamType.GetMethod("CreateKey",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(ReadOnlySpan<char>) },
                null)!;

            // Use DynamicMethod to box the struct return value.
#pragma warning disable IL3050 // RequiresDynamicCode: DynamicMethod is not supported in AOT; these tests are skipped under NativeAOT.
            var dm = new System.Reflection.Emit.DynamicMethod(
                "CreateKeyWrapper",
                typeof(object),
                new[] { typeof(ReadOnlySpan<char>) },
                typeof(ZipCryptoStreamWrappedConformanceTests).Module,
                skipVisibility: true);
            var il = dm.GetILGenerator();
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            il.Emit(System.Reflection.Emit.OpCodes.Call, createKeyMethod);
            il.Emit(System.Reflection.Emit.OpCodes.Box, zipCryptoKeysType);
            il.Emit(System.Reflection.Emit.OpCodes.Ret);
            s_createKey = dm.CreateDelegate<CreateKeyDelegate>();
#pragma warning restore IL3050

            s_createEncryptionMethod = zipCryptoStreamType.GetMethod("Create",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Stream), zipCryptoKeysType, typeof(ushort), typeof(bool), typeof(uint?), typeof(bool) },
                null)!;

            s_createDecryptionMethod = zipCryptoStreamType.GetMethod("Create",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Stream), zipCryptoKeysType, typeof(byte), typeof(bool), typeof(bool) },
                null)!;

            s_createAsyncMethod = zipCryptoStreamType.GetMethod("CreateAsync",
               BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
               null,
               new[] { typeof(Stream), zipCryptoKeysType, typeof(byte), typeof(bool), typeof(CancellationToken), typeof(bool) },
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
            object keys = s_createKey(TestPassword);

            // The check byte is the HIGH byte of the password verifier (little-endian format)
            byte expectedCheckByte = (byte)(PasswordVerifier >> 8); // 0x12

            // Create the encryption stream (write-only) - wraps stream1
            var encryptStream = (Stream)s_createEncryptionMethod.Invoke(null, new object?[]
            {
                wrapped.Stream1, keys, PasswordVerifier, true /* encrypting */, null /* crc32 */, leaveOpen
            })!;

            // Write and flush the header so the decryption stream can read it
            // ZipCrypto header is written lazily on first write, so we need to trigger it
            // Use async operations to support AsyncOnlyStream wrappers used by conformance tests
            await encryptStream.WriteAsync(new byte[] { 0 }, 0, 1).ConfigureAwait(false); // Trigger header write
            await encryptStream.FlushAsync().ConfigureAwait(false);

            // Now create the decryption stream (read-only) - wraps stream2
            // This will read and validate the 12-byte header
            // Use async factory method to support AsyncOnlyStream wrappers
            var decryptStream = await CreateDecryptStreamAsync(wrapped.Stream2, keys, expectedCheckByte, leaveOpen).ConfigureAwait(false);

            // Read the byte we wrote to trigger the header
            byte[] readBuffer = new byte[1];
            await decryptStream.ReadAsync(readBuffer, 0, 1).ConfigureAwait(false);

            return (encryptStream, decryptStream);
        }

        private static async Task<Stream> CreateDecryptStreamAsync(Stream baseStream, object keys, byte expectedCheckByte, bool leaveOpen)
        {
            // CreateAsync returns Task<ZipCryptoStream>, await it and get the result
            var task = (Task)s_createAsyncMethod.Invoke(null, new object[]
            {
                    baseStream, keys, expectedCheckByte, false /* encrypting */, CancellationToken.None, leaveOpen
            })!;
            await task.ConfigureAwait(false);

            // Get the Result property from the completed task
#pragma warning disable IL2075 // CreateAsync returns Task<ZipCryptoStream>; this test is skipped under NativeAOT.
            var resultProperty = task.GetType().GetProperty(nameof(Task<object>.Result))!;
#pragma warning restore IL2075
            return (Stream)resultProperty.GetValue(task)!;

        }
    }
}
