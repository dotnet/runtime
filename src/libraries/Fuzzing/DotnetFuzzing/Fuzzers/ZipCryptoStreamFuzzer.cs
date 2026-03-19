// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;

namespace DotnetFuzzing.Fuzzers;

internal sealed class ZipCryptoStreamFuzzer : IFuzzer
{
    public string[] TargetAssemblies { get; } = ["System.IO.Compression"];
    public string[] TargetCoreLibPrefixes => [];
    public string Corpus => "zipcryptostream";

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        // ZipCryptoStream.Create reads a 12-byte header from the stream and validates the
        // last decrypted byte against the expected check byte. Require at least 13 bytes
        // (1 check byte + 12 header bytes) so the fuzzer can reach past the header.
        if (bytes.Length < 13)
        {
            return;
        }

        TestStream(CopyToRentedArray(bytes), bytes.Length, async: false).GetAwaiter().GetResult();
        TestStream(CopyToRentedArray(bytes), bytes.Length, async: true).GetAwaiter().GetResult();
    }

#pragma warning disable IL2026 // RequiresUnreferencedCode
    private static readonly Type _zipCryptoStreamType = typeof(ZipArchive).Assembly.GetType("System.IO.Compression.ZipCryptoStream", throwOnError: true)!;
    private static readonly Type _zipCryptoKeysType = typeof(ZipArchive).Assembly.GetType("System.IO.Compression.ZipCryptoKeys", throwOnError: true)!;
#pragma warning restore IL2026

    // ReadOnlySpan<char> is a ref struct and cannot be boxed for MethodInfo.Invoke,
    // so we use a strongly-typed delegate instead.
    private delegate object CreateKeyDelegate(ReadOnlySpan<char> password);

#pragma warning disable IL2077 // dynamic access to non-public members
    private static readonly CreateKeyDelegate _createKey = _zipCryptoStreamType.GetMethod(
        "CreateKey",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.CreateDelegate<CreateKeyDelegate>();

    private static readonly MethodInfo _createMethod = _zipCryptoStreamType.GetMethod(
        "Create",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
        binder: null,
        types: [typeof(Stream), _zipCryptoKeysType, typeof(byte), typeof(bool), typeof(bool)],
        modifiers: null)!;
#pragma warning restore IL2077

    // Derive keys from a fixed password so the key state is realistic.
    private static readonly object s_keys = _createKey("fuzz");

    private static Stream CreateStream(byte[] bytes, int length)
    {
        // Use the first byte of the input as the "expected check byte" so that the
        // header validation path is exercised with varying values.
        byte expectedCheckByte = bytes[0];
        var baseStream = new MemoryStream(bytes, 1, length - 1);
#pragma warning disable IL2072 // dynamic invocation
        return (Stream)_createMethod.Invoke(
            obj: null,
            parameters: [baseStream, s_keys, expectedCheckByte, /*encrypting*/ false, /*leaveOpen*/ false])!;
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
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
