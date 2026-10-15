// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression.Tests.Utilities;
using System.IO.Tests;
using System.Reflection;
using System.Runtime.Versioning;
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
        private const string TestPassword = "PLACEHOLDER";

        private delegate object CreateKeyDelegate(ReadOnlySpan<char> password, byte[]? salt, int keySizeBits);
        private static readonly CreateKeyDelegate s_createKey;
        private static readonly MethodInfo s_createAsyncMethod;
        private static readonly MethodInfo s_createMethod;
        private static readonly FieldInfo s_hmacField;

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

            s_createAsyncMethod = winZipAesStreamType.GetMethod("CreateAsync",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Stream), winZipAesKeyMaterialType, typeof(long), typeof(bool), typeof(bool), typeof(CancellationToken) },
                null)!;

            s_hmacField = winZipAesStreamType.GetField("_hmac", BindingFlags.NonPublic | BindingFlags.Instance)!;
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

            // Create decryption stream over the encrypted data
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
        public async Task DecryptingStream_VerifiesAndRewindsBeforeReading(bool async)
        {
            const int PasswordVerifierSize = 2;
            const int AuthenticationCodeSize = 10;

            byte[] plaintext = new byte[4097];
            new Random(42).NextBytes(plaintext);
            (byte[] encryptedData, object decryptKeyMaterial) = CreateEncryptedData(plaintext);

            using var encryptedDataStream = new MemoryStream(encryptedData);
            using var encryptedStream = new ClampedReadStream(encryptedDataStream, readSizeLimit: 1);
            using Stream decryptStream = await CreateDecryptingStream(encryptedStream, decryptKeyMaterial, encryptedData.Length, async);

            Assert.Equal(SaltSize + PasswordVerifierSize, encryptedStream.Position);
            Assert.Null(s_hmacField.GetValue(decryptStream));

            using var decryptedStream = new MemoryStream();
            if (async)
            {
                await decryptStream.CopyToAsync(decryptedStream);
            }
            else
            {
                decryptStream.CopyTo(decryptedStream);
            }

            Assert.Equal(encryptedData.Length - AuthenticationCodeSize, encryptedStream.Position);
            Assert.Equal(plaintext, decryptedStream.ToArray());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DecryptingStream_InvalidSizeOrTruncatedStream_ThrowsInvalidDataException(bool async)
        {
            const int PasswordVerifierSize = 2;
            const int AuthenticationCodeSize = 10;
            int headerSize = SaltSize + PasswordVerifierSize;

            (byte[] encryptedData, object decryptKeyMaterial) = CreateEncryptedData("payload"u8.ToArray());

            // Declared totalStreamSize smaller than header + auth code
            await AssertCreateThrowsInvalidDataException(encryptedData, decryptKeyMaterial, headerSize + AuthenticationCodeSize - 1, async);

            // Stream truncated inside the salt/verifier header (while totalStreamSize claims to be valid)
            await AssertCreateThrowsInvalidDataException(encryptedData[..(headerSize - 1)], decryptKeyMaterial, encryptedData.Length, async);

            // Stream truncated inside the trailing 10-byte authentication code
            await AssertCreateThrowsInvalidDataException(encryptedData[..^1], decryptKeyMaterial, encryptedData.Length, async);
        }

        private static async Task AssertCreateThrowsInvalidDataException(byte[] streamBytes, object keyMaterial, int declaredTotalStreamSize, bool async)
        {
            using var encryptedStream = new MemoryStream(streamBytes);
            if (async)
            {
                await Assert.ThrowsAsync<InvalidDataException>(() => CreateDecryptingStream(encryptedStream, keyMaterial, declaredTotalStreamSize, async: true));
            }
            else
            {
                TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => CreateDecryptingStream(encryptedStream, keyMaterial, declaredTotalStreamSize, async: false).GetAwaiter().GetResult());
                Assert.IsType<InvalidDataException>(ex.InnerException);
            }
        }

        private static async Task<Stream> CreateDecryptingStream(Stream encryptedStream, object keyMaterial, int encryptedDataLength, bool async)
        {
            object[] arguments =
            {
                encryptedStream, keyMaterial, (long)encryptedDataLength, false, true
            };

            if (!async)
            {
                return (Stream)s_createMethod.Invoke(null, arguments)!;
            }

            object[] asyncArguments =
            {
                encryptedStream, keyMaterial, (long)encryptedDataLength, false, true, CancellationToken.None
            };
            var createTask = (Task)s_createAsyncMethod.Invoke(null, asyncArguments)!;
            await createTask;

            return (Stream)createTask.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(createTask)!;
        }

        private (byte[] EncryptedData, object DecryptKeyMaterial) CreateEncryptedData(byte[] plaintext)
        {
            object encryptKeyMaterial = s_createKey(TestPassword, null, KeySizeBits);

            using var encryptedStream = new MemoryStream();
            using (var encryptStream = (Stream)s_createMethod.Invoke(null, new object[]
            {
                encryptedStream, encryptKeyMaterial, -1L, true, true
            })!)
            {
                encryptStream.Write(plaintext);
            }

            byte[] encryptedData = encryptedStream.ToArray();
            byte[] salt = encryptedData.AsSpan(0, SaltSize).ToArray();
            object decryptKeyMaterial = s_createKey(TestPassword, salt, KeySizeBits);

            return (encryptedData, decryptKeyMaterial);
        }
    }
}
