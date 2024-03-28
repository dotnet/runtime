// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

public class ObjectFileScraper
{
    public readonly ReadOnlyMemory<byte> MagicLE = new byte[4]{0x01, 0x02, 0x03, 0x4};
    public readonly ReadOnlyMemory<byte> MagicBE = new byte[4]{0x04, 0x03, 0x02, 0x1};

    public bool Verbose {get;}
    public ObjectFileScraper(bool verbose) {
        Verbose = verbose;
    }

    public async Task<bool> ScrapeInput(string inputPath, CancellationToken token)
    {
        using var file = File.Open(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var r = await FindMagic(file, token);

        if (!r.Found){
            return false;
        }
        if (Verbose) {
            Console.WriteLine ($"{inputPath}: magic at {r.Position}");
        }
        file.Seek(r.Position, SeekOrigin.Begin);
        var header = ReadHeader(file, r.LittleEndian);
        if (Verbose) {
            Console.WriteLine ($"{inputPath}: {header}");
        }
        return true;
    }

    struct MagicResult {
        public bool Found {get; init;}
        public long Position {get; init;}
        public bool LittleEndian {get; init;}
    }

    private async Task<MagicResult> FindMagic(Stream stream, CancellationToken token)
    {
        var buf = new byte[4096];
        long pos = stream.Position;
        while (true) {
            token.ThrowIfCancellationRequested();
            int bytesRead = await stream.ReadAsync(buf, 0, buf.Length, token);
            if (bytesRead == 0)
                return new (){Found = false, Position = 0, LittleEndian = true};
            // FIXME: what if magic spans a buffer boundary
            if (FindMagic(buf, out int offset, out bool isLittleEndian)) {
                pos += (long)offset;
                return new (){Found = true, Position = pos, LittleEndian = isLittleEndian};
            }
            pos += bytesRead;
        }
    }

    private bool FindMagic(ReadOnlySpan<byte> buffer, out int offset, out bool isLittleEndian)
    {
        int start = buffer.IndexOf(MagicLE.Span);
        if (start != -1)
        {
            offset = start;
            isLittleEndian = true;
            return true;
        }
        start = buffer.IndexOf(MagicBE.Span);
        if (start != -1)
        {
            offset = start;
            isLittleEndian = false;
            return true;
        }
        offset = 0;
        isLittleEndian = false;
        return false;
    }

    private string ReadHeader(Stream stream, bool isLittleEndian)
    {
        return "TODO: read a header";
    }

}
