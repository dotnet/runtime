// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private const string TestPassword = "PLACEHOLDER";

        private delegate object CreateKeyDelegate(ReadOnlySpan<char> password, byte[]? salt, int keySizeBits);
        private static readonly CreateKeyDelegate s_createKey;
        private static readonly MethodInfo s_createMethod;

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

            // Create key material for encryption (generates random salt)
            object encryptKeyMaterial = s_createKey(TestPassword, null, KeySizeBits);

            // Encrypt data first
            using var encryptedMs = new MemoryStream();
            using (var encryptStream = (Stream)s_createMethod.Invoke(null, new object[]
            {
                encryptedMs, encryptKeyMaterial, -1L, true, true
            })!)
            {
                encryptStream.Write(plaintext);
            }

            // Extract salt from encrypted data to create matching key material for decryption
            byte[] encryptedData = encryptedMs.ToArray();
            byte[] salt = new byte[SaltSize];
            Array.Copy(encryptedData, 0, salt, 0, SaltSize);

            object decryptKeyMaterial = s_createKey(TestPassword, salt, KeySizeBits);

            // Create decryption stream over the encrypted data
            var ms = new MemoryStream(encryptedData);
            var decryptStream = (Stream)s_createMethod.Invoke(null, new object[]
            {
                ms, decryptKeyMaterial, (long)encryptedData.Length, false, false
            })!;

            return Task.FromResult<Stream?>(decryptStream);
        }
    }
}
