// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Tests;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    /// <summary>
    /// Conformance tests for ZipCryptoStream.
    /// </summary>
    [ConditionalClass(typeof(ZipCryptoStreamTestsSupport), nameof(ZipCryptoStreamTestsSupport.IsSupported))]
    public class ZipCryptoStreamConformanceTests : StandaloneStreamConformanceTests
    {
        private const string TestPassword = "PLACEHOLDER";
        private const ushort PasswordVerifier = 0x1234;

        private delegate object CreateKeyDelegate(ReadOnlySpan<char> password);
        private static readonly CreateKeyDelegate s_createKey;
        private static readonly MethodInfo s_createEncryptionMethod;
        private static readonly MethodInfo s_createDecryptionMethod;

        static ZipCryptoStreamConformanceTests()
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
                typeof(ZipCryptoStreamConformanceTests).Module,
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

        }

        protected override bool CanSeek => false;

        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData) =>
            Task.FromResult<Stream?>(null); // ZipCryptoStream is either read-only or write-only

        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData)
        {
            var ms = new MemoryStream();
            object keys = s_createKey(TestPassword);

            var encryptStream = (Stream)s_createEncryptionMethod.Invoke(null, new object?[]
            {
                ms, keys, PasswordVerifier, true /* encrypting */, null /* crc32 */, false /* leaveOpen */
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
            object keys = s_createKey(TestPassword);

            // The check byte is the HIGH byte of the password verifier (little-endian format)
            byte expectedCheckByte = (byte)(PasswordVerifier >> 8);

            // Encrypt data first
            using var encryptedMs = new MemoryStream();
            using (var encryptStream = (Stream)s_createEncryptionMethod.Invoke(null, new object?[]
            {
                encryptedMs, keys, PasswordVerifier, true /* encrypting */, null /* crc32 */, true /* leaveOpen */
            })!)
            {
                encryptStream.Write(plaintext);
            }

            byte[] encryptedData = encryptedMs.ToArray();

            // Create decryption stream over the encrypted data
            var ms = new MemoryStream(encryptedData);
            var decryptStream = (Stream)s_createDecryptionMethod.Invoke(null, new object[]
            {
                ms, keys, expectedCheckByte, false /* encrypting */, false /* leaveOpen */
            })!;

            return Task.FromResult<Stream?>(decryptStream);
        }
    }

    // The ZipCryptoStream conformance test classes have a static constructor that uses DynamicMethod to
    // invoke internal members. Accessing any static member of those classes (including an IsSupported gate
    // property) would trigger that static constructor, which throws PlatformNotSupportedException on
    // platforms without dynamic code generation (NativeAOT, Mono AOT). Hosting the gate on this separate
    // type ensures the ConditionalClass check can be evaluated without initializing the test classes, so
    // the tests are correctly skipped instead of failing during type initialization.
    internal static class ZipCryptoStreamTestsSupport
    {
        public static bool IsSupported => PlatformDetection.IsReflectionEmitSupported && PlatformDetection.IsMultithreadingSupported;
    }
}
