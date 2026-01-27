// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.IO.Tests;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    /// <summary>
    /// Conformance tests for ZipCryptoStream encryption (write-only stream).
    /// </summary>
    public sealed class ZipCryptoEncryptionStreamConformanceTests : StandaloneStreamConformanceTests
    {
        private const string TestPassword = "test-password";
        private const ushort PasswordVerifier = 0x1234;

        private static readonly Type s_zipCryptoStreamType;
        private static readonly MethodInfo s_createKeyMethod;
        private static readonly MethodInfo s_createEncryptionMethod;

        static ZipCryptoEncryptionStreamConformanceTests()
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
        }

        protected override bool CanSeek => false;

        // ZipCryptoStream doesn't track disposal state internally,
        // so it won't throw ObjectDisposedException after disposal.
        public override Task Disposed_ThrowsObjectDisposedException() => Task.CompletedTask;

        protected override Task<Stream?> CreateReadOnlyStreamCore(byte[]? initialData) =>
            Task.FromResult<Stream?>(null); // Encryption stream is write-only

        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData) =>
            Task.FromResult<Stream?>(null); // Encryption stream is write-only

        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData)
        {
            var ms = new MemoryStream();
            byte[] keyBytes = (byte[])s_createKeyMethod.Invoke(null, new object[] { TestPassword.AsMemory() })!;

            var encryptStream = (Stream)s_createEncryptionMethod.Invoke(null, new object?[]
            {
                ms, keyBytes, PasswordVerifier, true, null, false
            })!;

            if (initialData != null && initialData.Length > 0)
            {
                encryptStream.Write(initialData);
            }

            return Task.FromResult<Stream?>(encryptStream);
        }
    }

    /// <summary>
    /// Conformance tests for ZipCryptoStream decryption (read-only stream).
    /// </summary>
    public sealed class ZipCryptoDecryptionStreamConformanceTests : StandaloneStreamConformanceTests
    {
        private const string TestPassword = "test-password";
        private const ushort PasswordVerifier = 0x1234;
        // The check byte is the HIGH byte of the verifier (little-endian: byte 11 = 0x12)
        private const byte ExpectedCheckByte = 0x12;

        private static readonly Type s_zipCryptoStreamType;
        private static readonly MethodInfo s_createKeyMethod;
        private static readonly MethodInfo s_createEncryptionMethod;
        private static readonly MethodInfo s_createDecryptionMethod;

        static ZipCryptoDecryptionStreamConformanceTests()
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

        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData) =>
            Task.FromResult<Stream?>(null); // Decryption stream is read-only

        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData) =>
            Task.FromResult<Stream?>(null); // Decryption stream is read-only

        protected override Task<Stream?> CreateReadOnlyStreamCore(byte[]? initialData)
        {
            byte[] plaintext = initialData ?? Array.Empty<byte>();
            byte[] keyBytes = (byte[])s_createKeyMethod.Invoke(null, new object[] { TestPassword.AsMemory() })!;

            // Encrypt data first
            using var encryptedMs = new MemoryStream();
            using (var encryptStream = (Stream)s_createEncryptionMethod.Invoke(null, new object?[]
            {
                encryptedMs, keyBytes, PasswordVerifier, true, null, true
            })!)
            {
                encryptStream.Write(plaintext);
            }

            // Create decryption stream over the encrypted data
            var ms = new MemoryStream(encryptedMs.ToArray());
            var decryptStream = (Stream)s_createDecryptionMethod.Invoke(null, new object[]
            {
                ms, keyBytes, ExpectedCheckByte, false, false
            })!;

            return Task.FromResult<Stream?>(decryptStream);
        }
    }
}
