// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;

namespace DotnetFuzzing.Fuzzers;

internal sealed class Deflate64Fuzzer : IFuzzer
{
    public string[] TargetAssemblies { get; } = ["System.IO.Compression"];
    public string[] TargetCoreLibPrefixes => [];
    public string Corpus => "deflate64";

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return;
        }

        TestArchive(CopyToRentedArray(bytes), bytes.Length, async: false).GetAwaiter().GetResult();
        TestArchive(CopyToRentedArray(bytes), bytes.Length, async: true).GetAwaiter().GetResult();
    }

#pragma warning disable IL2026 // RequiresUnreferencedCode
    private static readonly Type _deflateStreamType = typeof(ZipArchive).Assembly.GetType("System.IO.Compression.DeflateManagedStream", throwOnError: true)!;
#pragma warning restore IL2026

    private static Stream CreateStream(byte[] bytes, int length)
    {
#pragma warning disable IL2077 // dynamic access to non-public ctors
        return (Stream)Activator.CreateInstance(
            _deflateStreamType,
            bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            args: new object[] { new MemoryStream(bytes, 0, length), ZipCompressionMethod.Deflate64, -1L },
            culture: null)!;
#pragma warning restore IL2077
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

    private async Task TestArchive(byte[] buffer, int length, bool async)
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
            // ignore, this exception is expected to be thrown for invalid/corrupted archives.
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
