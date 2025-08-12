// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Compression;
using System.Threading.Tasks;

namespace DotnetFuzzing.Fuzzers;

internal sealed class ZipArchiveFuzzer : IFuzzer
{
    public string[] TargetAssemblies { get; } = ["System.IO.Compression"];
    public string[] TargetCoreLibPrefixes => [];
    public string Dictionary => "ziparchive.dict";

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return;
        }

        TestArchive(CopyToRentedArray(bytes), bytes.Length, async: false).GetAwaiter().GetResult();
        TestArchive(CopyToRentedArray(bytes), bytes.Length, async: true).GetAwaiter().GetResult();
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
            using var stream = new MemoryStream(buffer, 0, length);

            ZipArchive archive;

            if (async)
            {
                archive = await ZipArchive.CreateAsync(stream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null);
            }
            else
            {
                archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null);
            }

            foreach (var entry in archive.Entries)
            {
                // Access entry properties to simulate usage
                _ = entry.FullName;
                _ = entry.Length;
                _ = entry.Comment;
                _ = entry.LastWriteTime;
                _ = entry.Crc32;
            }

            if (async)
            {
                await archive.DisposeAsync();
            }
            else
            {
                archive.Dispose();
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
