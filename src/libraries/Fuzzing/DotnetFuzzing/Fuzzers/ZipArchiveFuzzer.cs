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

        try
        {
            using var stream = new MemoryStream(bytes.ToArray());

            Task sync_test = TestArchive(stream, async: false);
            Task async_test = TestArchive(stream, async: true);

            Task.WaitAll(sync_test, async_test);
        }
        catch (Exception) { }
    }

    private async Task TestArchive(Stream stream, bool async)
    {
        stream.Position = 0;

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
}
