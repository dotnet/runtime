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
    /// Conformance tests for ZipCryptoStream.
    /// </summary>
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNativeAot))]
    public class ZipCryptoStreamConformanceTests : StandaloneStreamConformanceTests
    {
        private const string TestPassword = "test-password";
        private const ushort PasswordVerifier = 0x1234;

        private static readonly Type s_zipCryptoStreamType;
        private static readonly MethodInfo s_createKeyMethod;
        private static readonly MethodInfo s_createEncryptionMethod;
        private static readonly MethodInfo s_createDecryptionMethod;

        static ZipCryptoStreamConformanceTests()
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

        }

        protected override bool CanSeek => false;

        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData) =>
            Task.FromResult<Stream?>(null); // ZipCryptoStream is either read-only or write-only

        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData)
        {
            var ms = new MemoryStream();
            byte[] keyBytes = (byte[])s_createKeyMethod.Invoke(null, new object[] { TestPassword.AsMemory() })!;

            var encryptStream = (Stream)s_createEncryptionMethod.Invoke(null, new object?[]
            {
                ms, keyBytes, PasswordVerifier, true /* encrypting */, null /* crc32 */, false /* leaveOpen */
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
            byte[] keyBytes = (byte[])s_createKeyMethod.Invoke(null, new object[] { TestPassword.AsMemory() })!;

            // The check byte is the HIGH byte of the password verifier (little-endian format)
            byte expectedCheckByte = (byte)(PasswordVerifier >> 8);

            // Encrypt data first
            using var encryptedMs = new MemoryStream();
            using (var encryptStream = (Stream)s_createEncryptionMethod.Invoke(null, new object?[]
            {
                encryptedMs, keyBytes, PasswordVerifier, true /* encrypting */, null /* crc32 */, true /* leaveOpen */
            })!)
            {
                encryptStream.Write(plaintext);
            }

            byte[] encryptedData = encryptedMs.ToArray();

            // Create decryption stream over the encrypted data
            var ms = new MemoryStream(encryptedData);
            var decryptStream = (Stream)s_createDecryptionMethod.Invoke(null, new object[]
            {
                ms, keyBytes, expectedCheckByte, false /* encrypting */, false /* leaveOpen */
            })!;

            return Task.FromResult<Stream?>(decryptStream);
        }
    }
}
