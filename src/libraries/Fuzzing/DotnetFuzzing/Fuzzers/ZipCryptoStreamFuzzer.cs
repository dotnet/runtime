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

    // ReadOnlySpan<char> is a ref struct and cannot be boxed for MethodInfo.Invoke,
    // and CreateDelegate cannot handle struct-to-object return covariance.
    // Use DynamicMethod to emit a wrapper that boxes the struct return value.
    private delegate object CreateKeyDelegate(ReadOnlySpan<char> password);

    private static readonly CreateKeyDelegate _createKey;
    private static readonly MethodInfo _createMethod;
    private static readonly object s_keys;

    static ZipCryptoStreamFuzzer()
    {
        Type zipCryptoStreamType = Type.GetType("System.IO.Compression.ZipCryptoStream, System.IO.Compression")!;
        Type zipCryptoKeysType = Type.GetType("System.IO.Compression.ZipCryptoKeys, System.IO.Compression")!;

#pragma warning disable IL3050 // RequiresDynamicCode: DynamicMethod is not AOT-compatible; fuzzers run under CoreCLR only.
        _createKey = CreateBoxingDelegate(zipCryptoStreamType, zipCryptoKeysType);
#pragma warning restore IL3050

        _createMethod = zipCryptoStreamType.GetMethod(
            "Create",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [typeof(Stream), zipCryptoKeysType, typeof(byte), typeof(bool), typeof(bool)],
            modifiers: null)!;

        s_keys = _createKey("fuzz");
    }

    private static CreateKeyDelegate CreateBoxingDelegate(Type zipCryptoStreamType, Type zipCryptoKeysType)
    {
        MethodInfo createKeyMethod = zipCryptoStreamType.GetMethod(
            "CreateKey",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;

        var dm = new System.Reflection.Emit.DynamicMethod(
            "CreateKeyWrapper",
            typeof(object),
            [typeof(ReadOnlySpan<char>)],
            typeof(ZipCryptoStreamFuzzer).Module,
            skipVisibility: true);
        var il = dm.GetILGenerator();
        il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
        il.Emit(System.Reflection.Emit.OpCodes.Call, createKeyMethod);
        il.Emit(System.Reflection.Emit.OpCodes.Box, zipCryptoKeysType);
        il.Emit(System.Reflection.Emit.OpCodes.Ret);
        return dm.CreateDelegate<CreateKeyDelegate>();
    }

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

    private static byte[] CopyToRentedArray(ReadOnlySpan<byte> bytes)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(bytes.Length);
        bytes.CopyTo(buffer);
        return buffer;
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
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidDataException)
        {
            // The reflected ZipCryptoStream.Create call wraps InvalidDataException
            // (e.g. password mismatch, truncated header) in TargetInvocationException.
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
