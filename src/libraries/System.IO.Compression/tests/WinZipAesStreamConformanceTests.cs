// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.IO.Tests;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    /// <summary>
    /// Conformance tests for WinZipAesStream (AES-128).
    /// </summary>
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNativeAot))]
    [UnsupportedOSPlatform("browser")]
    public sealed class WinZipAes128StreamConformanceTests : WinZipAesStreamConformanceTests
    {
        protected override int KeySizeBits => 128;
    }

    /// <summary>
    /// Conformance tests for WinZipAesStream (AES-256).
    /// </summary>
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNativeAot))]
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
        private const string TestPassword = "test-password";

        private static readonly Type s_winZipAesStreamType;
        private static readonly MethodInfo s_createKeyMethod;
        private static readonly MethodInfo s_createMethod;

        protected abstract int KeySizeBits { get; }
        protected int SaltSize => KeySizeBits / 16;

        static WinZipAesStreamConformanceTests()
        {
            var assembly = typeof(ZipArchive).Assembly;
            s_winZipAesStreamType = assembly.GetType("System.IO.Compression.WinZipAesStream", throwOnError: true)!;

            s_createKeyMethod = s_winZipAesStreamType.GetMethod("CreateKey",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(ReadOnlyMemory<char>), typeof(byte[]), typeof(int) },
                null)!;

            s_createMethod = s_winZipAesStreamType.GetMethod("Create",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Stream), typeof(byte[]), typeof(int), typeof(long), typeof(bool), typeof(bool) },
                null)!;
        }

        protected override bool CanSeek => false;

        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData) =>
            Task.FromResult<Stream?>(null); // WinZipAesStream is either read-only or write-only

        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData)
        {
            var ms = new MemoryStream();
            byte[] keyMaterial = (byte[])s_createKeyMethod.Invoke(null, new object?[]
            {
                TestPassword.AsMemory(), null, KeySizeBits
            })!;

            var encryptStream = (Stream)s_createMethod.Invoke(null, new object[]
            {
                ms, keyMaterial, KeySizeBits, 0L, true, false
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

            // Create key material for encryption (generates random salt)
            byte[] encryptKeyMaterial = (byte[])s_createKeyMethod.Invoke(null, new object?[]
            {
                TestPassword.AsMemory(), null, KeySizeBits
            })!;

            // Encrypt data first
            using var encryptedMs = new MemoryStream();
            using (var encryptStream = (Stream)s_createMethod.Invoke(null, new object[]
            {
                encryptedMs, encryptKeyMaterial, KeySizeBits, 0L, true, true
            })!)
            {
                encryptStream.Write(plaintext);
            }

            // Extract salt from encrypted data to create matching key material for decryption
            byte[] encryptedData = encryptedMs.ToArray();
            byte[] salt = new byte[SaltSize];
            Array.Copy(encryptedData, 0, salt, 0, SaltSize);

            byte[] decryptKeyMaterial = (byte[])s_createKeyMethod.Invoke(null, new object?[]
            {
                TestPassword.AsMemory(), salt, KeySizeBits
            })!;

            // Create decryption stream over the encrypted data
            var ms = new MemoryStream(encryptedData);
            var decryptStream = (Stream)s_createMethod.Invoke(null, new object[]
            {
                ms, decryptKeyMaterial, KeySizeBits, (long)encryptedData.Length, false, false
            })!;

            return Task.FromResult<Stream?>(decryptStream);
        }
    }
}
