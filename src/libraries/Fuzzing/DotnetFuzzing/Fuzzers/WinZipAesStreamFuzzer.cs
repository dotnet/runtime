// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DotnetFuzzing.Fuzzers;

[UnsupportedOSPlatform("browser")]
internal sealed class WinZipAesStreamFuzzer : IFuzzer
{
    public string[] TargetAssemblies { get; } = ["System.IO.Compression"];
    public string[] TargetCoreLibPrefixes => [];
    public string Corpus => "winzipaesstream";

    // AES-256 key size in bits; salt size = keySizeBits / 16 = 16 bytes.
    private const int KeySizeBits = 256;

#pragma warning disable IL2026 // RequiresUnreferencedCode
    private static readonly Type _winZipAesStreamType = typeof(ZipArchive).Assembly.GetType("System.IO.Compression.WinZipAesStream", throwOnError: true)!;
    private static readonly Type _winZipAesKeyMaterialType = typeof(ZipArchive).Assembly.GetType("System.IO.Compression.WinZipAesKeyMaterial", throwOnError: true)!;
#pragma warning restore IL2026

    // ReadOnlySpan<char> is a ref struct and cannot be boxed for MethodInfo.Invoke,
    // so we use a strongly-typed delegate instead.
    private delegate object CreateKeyDelegate(ReadOnlySpan<char> password, byte[]? salt, int keySizeBits);

#pragma warning disable IL2077 // dynamic access to non-public members
    private static readonly CreateKeyDelegate _createKey = _winZipAesKeyMaterialType.GetMethod(
        "Create",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
        binder: null,
        types: [typeof(ReadOnlySpan<char>), typeof(byte[]), typeof(int)],
        modifiers: null)!.CreateDelegate<CreateKeyDelegate>();  

    private static readonly MethodInfo _createMethod = _winZipAesStreamType.GetMethod(
        "Create",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
        binder: null,
        types: [typeof(Stream), _winZipAesKeyMaterialType, typeof(long), typeof(bool), typeof(bool)],
        modifiers: null)!;

    // The salt and password verifier properties are needed to prepend a valid header
    // so the stream's ReadAndValidateHeaderCore succeeds and decryption logic is reached.
    private static readonly PropertyInfo _saltProp = _winZipAesKeyMaterialType.GetProperty(
        "Salt",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly PropertyInfo _verifierProp = _winZipAesKeyMaterialType.GetProperty(
        "PasswordVerifier",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
#pragma warning restore IL2077

    // Pre-derive key material once with a fixed password and no salt so the fuzzer focuses
    // on the stream's decryption/HMAC logic rather than key derivation.
    private static readonly object s_keyMaterial = _createKey("fuzz", null, KeySizeBits);

    // Cache the salt and password verifier bytes for prepending to the fuzz input.
    private static readonly byte[] s_salt = (byte[])_saltProp.GetValue(s_keyMaterial)!;
    private static readonly byte[] s_verifier = (byte[])_verifierProp.GetValue(s_keyMaterial)!;

    // Minimum fuzz input: at least 1 byte of encrypted data beyond the header.
    // The header (salt + verifier) is prepended by CreateStream, so the fuzz input
    // only needs to supply encrypted data + the 10-byte auth code.
    private const int MinInputLength = 11; // 1 byte data + 10 bytes HMAC

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < MinInputLength)
        {
            return;
        }

        TestStream(CopyToRentedArray(bytes), bytes.Length, async: false).GetAwaiter().GetResult();
        TestStream(CopyToRentedArray(bytes), bytes.Length, async: true).GetAwaiter().GetResult();
    }

    private static Stream CreateStream(byte[] bytes, int length)
    {
        // Prepend the valid salt + password verifier so ReadAndValidateHeaderCore passes,
        // allowing the fuzzer to exercise the CTR decryption and HMAC validation paths.
        int headerSize = s_salt.Length + s_verifier.Length;
        int totalSize = headerSize + length;
        byte[] combined = new byte[totalSize];
        s_salt.CopyTo(combined, 0);
        s_verifier.CopyTo(combined, s_salt.Length);
        Buffer.BlockCopy(bytes, 0, combined, headerSize, length);

#pragma warning disable IL2072 // dynamic invocation
        return (Stream)_createMethod.Invoke(
            obj: null,
            parameters: [new MemoryStream(combined), s_keyMaterial, (long)totalSize, /*encrypting*/ false, /*leaveOpen*/ false])!;
#pragma warning restore IL2072
    }

    private byte[] CopyToRentedArray(ReadOnlySpan<byte> bytes)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(bytes.Length);
        try
        {
            bytes.CopyTo(buffer);
            return buffer;
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(buffer);
            throw;
        }
    }

    private async Task TestStream(byte[] buffer, int length, bool async)
    {
        try
        {
            using var stream = CreateStream(buffer, length);
            if (async)
            {
                await stream.CopyToAsync(Stream.Null);
            }
            else
            {
                stream.CopyTo(Stream.Null);
            }
        }
        catch (InvalidDataException)
        {
            // ignore, this exception is expected for invalid/corrupted data.
        }
        catch (CryptographicException)
        {
            // ignore, crypto failures are expected for random fuzz input.
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
