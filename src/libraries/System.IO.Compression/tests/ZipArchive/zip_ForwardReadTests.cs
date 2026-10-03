// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.IO.Compression.Tests.Utilities;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests;

public class zip_ForwardReadTests
{
    private static readonly byte[][] s_payloads = ["first payload"u8.ToArray(), "second payload"u8.ToArray()];
    private static readonly string[] s_names = ["first.txt", "folder/second.txt"];
    private static readonly DateTimeOffset s_timestamp = new(2025, 1, 2, 3, 4, 6, TimeSpan.Zero);

    [Theory]
    [InlineData(false, CompressionLevel.NoCompression, 13)]
    [InlineData(true, CompressionLevel.NoCompression, 13)]
    [InlineData(false, CompressionLevel.Optimal, 13)]
    [InlineData(false, CompressionLevel.SmallestSize, 13)]
    [InlineData(true, CompressionLevel.Fastest, 13)]
    [InlineData(false, CompressionLevel.NoCompression, 0)]
    [InlineData(true, CompressionLevel.NoCompression, 0)]
    [InlineData(false, CompressionLevel.Optimal, 0)]
    [InlineData(true, CompressionLevel.Optimal, 0)]
    [InlineData(false, CompressionLevel.Optimal, 200_000)]
    [InlineData(true, CompressionLevel.Optimal, 200_000)]
    public async Task Entries_ReadDataAndLocalMetadata(bool useAsync, CompressionLevel compressionLevel, int payloadLength)
    {
        byte[] payload = new byte[payloadLength];
        new Random(42).NextBytes(payload);
        byte[][] payloads = [payload, s_payloads[1]];
        // ZipArchive writes empty entries as Stored; construct an empty Deflate stream explicitly.
        byte[] fixture = payloadLength == 0 && compressionLevel != CompressionLevel.NoCompression
            ? ReplaceFirstDeflatePayload([0x03, 0x00], payload)
            : CreateFixture(compressionLevel, payloads);
        using MemoryStream bytes = new(fixture);
        await using Stream source = CreateSource(bytes, useAsync, maxRead: payloadLength > 65_536 ? 4093 : 1);
        await using ZipArchive archive = new(source, ZipArchiveMode.ForwardRead, leaveOpen: true);
        Assert.Equal(0, bytes.Position);
        int header = 0;
        for (int i = 0; i < s_names.Length; i++)
        {
            ZipArchiveEntry entry = await Next(archive, useAsync);
            Assert.Equal(s_names[i], entry.FullName);
            Assert.Equal(Path.GetFileName(s_names[i]), entry.Name);
            Assert.Equal(payloads[i].Length, entry.Length);
            Assert.Equal(BinaryPrimitives.ReadUInt32LittleEndian(fixture.AsSpan(header + 18)), entry.CompressedLength);
            if (payloads[i].Length > 65_536)
            {
                Assert.True(entry.CompressedLength > 65_536);
            }
            Assert.Equal((uint)CRC.CalculateCRC(payloads[i]), entry.Crc32);
            Assert.Equal(s_timestamp.DateTime, entry.LastWriteTime.DateTime);
            long beforeOpen = bytes.Position;
            await using Stream data = await Open(entry, useAsync);
            Assert.Equal(beforeOpen, bytes.Position);
            Assert.True(data.CanRead);
            Assert.False(data.CanSeek);
            Assert.False(data.CanWrite);
            using MemoryStream output = new();
            if (useAsync)
            {
                await data.CopyToAsync(output, 257);
            }
            else
            {
                data.CopyTo(output, 257);
            }
            Assert.Equal(payloads[i], output.ToArray());
            header = DataOffset(fixture, header) + (int)entry.CompressedLength;
            Assert.Equal(header, bytes.Position);
        }
        Assert.Null(await Next(archive, useAsync));
        Assert.Null(await Next(archive, useAsync));
    }

    [Theory]
    [InlineData(false, false, CompressionLevel.NoCompression)]
    [InlineData(true, false, CompressionLevel.NoCompression)]
    [InlineData(false, true, CompressionLevel.NoCompression)]
    [InlineData(true, true, CompressionLevel.NoCompression)]
    [InlineData(false, false, CompressionLevel.Optimal)]
    [InlineData(true, false, CompressionLevel.Optimal)]
    [InlineData(false, true, CompressionLevel.Optimal)]
    [InlineData(true, true, CompressionLevel.Optimal)]
    public async Task Advance_DrainsPartialEntryAndInvalidatesHandle(bool useAsync, bool disposeHandle, CompressionLevel compressionLevel)
    {
        using MemoryStream bytes = new(CreateFixture(compressionLevel));
        await using Stream source = CreateSource(bytes, useAsync);
        await using ZipArchive archive = new(source, ZipArchiveMode.ForwardRead, leaveOpen: true);
        ZipArchiveEntry first = await Next(archive, useAsync);
        await using Stream data = await Open(first, useAsync);
        byte[] buffer = new byte[1];
        Assert.Equal(1, useAsync ? await data.ReadAsync(buffer) : data.Read(buffer));
        if (disposeHandle)
        {
            long before = bytes.Position;
            if (useAsync)
            {
                await data.DisposeAsync();
            }
            else
            {
                data.Dispose();
            }
            Assert.Equal(before, bytes.Position);
        }
        Assert.Equal(s_names[1], (await Next(archive, useAsync)).FullName);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await Open(first, useAsync));
        if (disposeHandle)
        {
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => { _ = useAsync ? await data.ReadAsync(buffer) : data.Read(buffer); });
        }
        else
        {
            await Assert.ThrowsAsync<NotSupportedException>(async () => { _ = useAsync ? await data.ReadAsync(buffer) : data.Read(buffer); });
        }
        Assert.Null(await Next(archive, useAsync));
    }

    public enum EntryReadScenario
    {
        SkipUnopened,
        DisposeThenAdvance,
        ReadExactlyThenDisposeAndAdvance,
        ReadToEnd
    }

    [Theory]
    [InlineData(false, EntryReadScenario.SkipUnopened, CompressionLevel.NoCompression)]
    [InlineData(true, EntryReadScenario.SkipUnopened, CompressionLevel.NoCompression)]
    [InlineData(false, EntryReadScenario.DisposeThenAdvance, CompressionLevel.NoCompression)]
    [InlineData(true, EntryReadScenario.DisposeThenAdvance, CompressionLevel.NoCompression)]
    [InlineData(false, EntryReadScenario.ReadExactlyThenDisposeAndAdvance, CompressionLevel.NoCompression)]
    [InlineData(true, EntryReadScenario.ReadExactlyThenDisposeAndAdvance, CompressionLevel.NoCompression)]
    [InlineData(false, EntryReadScenario.ReadToEnd, CompressionLevel.NoCompression)]
    [InlineData(true, EntryReadScenario.ReadToEnd, CompressionLevel.NoCompression)]
    [InlineData(false, EntryReadScenario.SkipUnopened, CompressionLevel.Optimal)]
    [InlineData(true, EntryReadScenario.SkipUnopened, CompressionLevel.Optimal)]
    [InlineData(false, EntryReadScenario.DisposeThenAdvance, CompressionLevel.Optimal)]
    [InlineData(true, EntryReadScenario.DisposeThenAdvance, CompressionLevel.Optimal)]
    [InlineData(false, EntryReadScenario.ReadExactlyThenDisposeAndAdvance, CompressionLevel.Optimal)]
    [InlineData(true, EntryReadScenario.ReadExactlyThenDisposeAndAdvance, CompressionLevel.Optimal)]
    [InlineData(false, EntryReadScenario.ReadToEnd, CompressionLevel.Optimal)]
    [InlineData(true, EntryReadScenario.ReadToEnd, CompressionLevel.Optimal)]
    public async Task InvalidCrc_OnlyOpenedEntriesValidate(bool useAsync, EntryReadScenario scenario, CompressionLevel compressionLevel)
    {
        byte[] fixture = CreateFixture(compressionLevel);
        fixture[14] ^= 0xff;
        using MemoryStream bytes = new(fixture);
        await using Stream source = CreateSource(bytes, useAsync);
        await using ZipArchive archive = new(source, ZipArchiveMode.ForwardRead, leaveOpen: true);
        ZipArchiveEntry first = await Next(archive, useAsync);
        if (scenario == EntryReadScenario.SkipUnopened)
        {
            Assert.Equal(s_names[1], (await Next(archive, useAsync)).FullName);
            Assert.Null(await Next(archive, useAsync));
            return;
        }
        await using Stream data = await Open(first, useAsync);
        if (scenario is EntryReadScenario.DisposeThenAdvance or EntryReadScenario.ReadExactlyThenDisposeAndAdvance)
        {
            if (scenario == EntryReadScenario.ReadExactlyThenDisposeAndAdvance)
            {
                // ReadExactly stops at Length without the extra read that observes EOF.
                byte[] payload = new byte[(int)first.Length];
                if (useAsync)
                {
                    await data.ReadExactlyAsync(payload);
                }
                else
                {
                    data.ReadExactly(payload);
                }
                Assert.Equal(s_payloads[0], payload);
            }
            long before = bytes.Position;
            if (useAsync)
            {
                await data.DisposeAsync();
            }
            else
            {
                data.Dispose();
            }
            Assert.Equal(before, bytes.Position);
            await Assert.ThrowsAsync<InvalidDataException>(async () => await Next(archive, useAsync));
        }
        else
        {
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                if (useAsync)
                {
                    await data.CopyToAsync(Stream.Null);
                }
                else
                {
                    data.CopyTo(Stream.Null);
                }
            });
        }
    }

    [Theory]
    [InlineData(false, false, CompressionLevel.NoCompression)]
    [InlineData(true, false, CompressionLevel.NoCompression)]
    [InlineData(false, true, CompressionLevel.NoCompression)]
    [InlineData(true, true, CompressionLevel.NoCompression)]
    [InlineData(false, false, CompressionLevel.Optimal)]
    [InlineData(true, false, CompressionLevel.Optimal)]
    [InlineData(false, true, CompressionLevel.Optimal)]
    [InlineData(true, true, CompressionLevel.Optimal)]
    public async Task Dispose_DoesNotDrainAndHonorsLeaveOpen(bool useAsync, bool leaveOpen, CompressionLevel compressionLevel)
    {
        using MemoryStream bytes = new(CreateFixture(compressionLevel));
        await using Stream source = CreateSource(bytes, useAsync);
        ZipArchive archive = new(source, ZipArchiveMode.ForwardRead, leaveOpen);
        await using Stream data = await Open(await Next(archive, useAsync), useAsync);
        if (compressionLevel != CompressionLevel.NoCompression)
        {
            byte[] buffer = new byte[1];
            Assert.Equal(1, useAsync ? await data.ReadAsync(buffer) : data.Read(buffer));
        }
        long before = bytes.Position;
        if (useAsync)
        {
            await archive.DisposeAsync();
        }
        else
        {
            archive.Dispose();
        }
        Assert.Equal(before, bytes.Position);
        Assert.Equal(leaveOpen, source.CanRead);
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await Next(archive, useAsync));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task TruncatedHeaderOrPayload_ThrowsInvalidData(bool useAsync, bool payload)
    {
        byte[] fixture = CreateFixture();
        Array.Resize(ref fixture, payload ? DataOffset(fixture, 0) + 2 : 12);
        using MemoryStream bytes = new(fixture);
        await using Stream source = CreateSource(bytes, useAsync);
        await using ZipArchive archive = new(source, ZipArchiveMode.ForwardRead, leaveOpen: true);
        if (payload)
        {
            Assert.NotNull(await Next(archive, useAsync));
        }
        await Assert.ThrowsAsync<InvalidDataException>(async () => await Next(archive, useAsync));
    }

    [Theory]
    [InlineData(false, 8, 9u)] // Deflate64 method.
    [InlineData(true, 6, 8u)] // Data descriptor flag.
    [InlineData(false, 6, 1u)] // Encryption flag.
    [InlineData(true, 18, uint.MaxValue)] // ZIP64 size sentinel.
    public async Task UnsupportedLocalHeader_ThrowsNotSupported(bool useAsync, int offset, uint value)
    {
        byte[] fixture = CreateFixture();
        if (offset == 18)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(fixture.AsSpan(offset), value);
        }
        else
        {
            BinaryPrimitives.WriteUInt16LittleEndian(fixture.AsSpan(offset), (ushort)value);
        }
        using MemoryStream bytes = new(fixture);
        await using Stream source = CreateSource(bytes, useAsync);
        await using ZipArchive archive = new(source, ZipArchiveMode.ForwardRead, leaveOpen: true);
        await Assert.ThrowsAsync<NotSupportedException>(async () => await Next(archive, useAsync));
    }

    [Fact]
    public async Task PreCanceledOperations_DoNotConsumeAndPermitRetry()
    {
        using MemoryStream bytes = new(CreateFixture());
        await using Stream source = CreateSource(bytes, useAsync: true);
        await using ZipArchive archive = new(source, ZipArchiveMode.ForwardRead, leaveOpen: true);
        CancellationToken canceled = new(canceled: true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await archive.GetNextEntryAsync(canceled));
        Assert.Equal(0, bytes.Position);
        ZipArchiveEntry first = await archive.GetNextEntryAsync();
        long before = bytes.Position;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.OpenAsync(canceled));
        Assert.Equal(before, bytes.Position);
        await using Stream data = await first.OpenAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await data.ReadAsync(new byte[1], canceled));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await archive.GetNextEntryAsync(canceled));
        Assert.Equal(before, bytes.Position);
        Assert.Equal(s_names[1], (await archive.GetNextEntryAsync()).FullName);
        Assert.Null(await archive.GetNextEntryAsync());
    }

    [Theory]
    [MemberData(nameof(ZipFileTestBase.Get_Booleans_Data), MemberType = typeof(ZipFileTestBase))]
    public async Task ForwardRead_RejectsUnsupportedOperations(bool useAsync)
    {
        using MemoryStream bytes = new(CreateFixture());
        await using Stream source = CreateSource(bytes, useAsync);
        await using ZipArchive archive = new(source, ZipArchiveMode.ForwardRead, leaveOpen: true);
        Assert.Throws<NotSupportedException>(() => archive.Entries);
        Assert.Throws<NotSupportedException>(() => archive.GetEntry(s_names[0]));
        Assert.Throws<NotSupportedException>(() => archive.CreateEntry("new"));
        Assert.Throws<NotSupportedException>(() => archive.Comment);
        Assert.Throws<NotSupportedException>(() => archive.Comment = "new");
        ZipArchiveEntry entry = await Next(archive, useAsync);
        Assert.Throws<NotSupportedException>(() => entry.Comment);
        Assert.Throws<NotSupportedException>(() => entry.Comment = "new");
        Assert.Throws<NotSupportedException>(() => entry.ExternalAttributes);
        Assert.Throws<NotSupportedException>(() => entry.ExternalAttributes = 0);
        Assert.Throws<NotSupportedException>(() => entry.VersionMadeBy);
        Assert.Throws<NotSupportedException>(() => entry.LastWriteTime = s_timestamp);
        Assert.Throws<NotSupportedException>(() => entry.Delete());
        foreach (FileAccess access in new[] { FileAccess.Write, FileAccess.ReadWrite })
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await (useAsync ? entry.OpenAsync(access) : Task.FromResult(entry.Open(access))));
        }
        long before = bytes.Position;
        await using Stream data = await Open(entry, useAsync);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await Open(entry, useAsync));
        Assert.Throws<NotSupportedException>(() => data.Length);
        Assert.Throws<NotSupportedException>(() => data.Position);
        Assert.Throws<NotSupportedException>(() => data.Position = 0);
        Assert.Throws<NotSupportedException>(() => data.Seek(0, SeekOrigin.Begin));
        Assert.Throws<NotSupportedException>(() => data.SetLength(0));
        byte[] buffer = new byte[1];
        if (useAsync)
        {
            await Assert.ThrowsAsync<NotSupportedException>(async () => await data.WriteAsync(buffer));
            await Assert.ThrowsAsync<NotSupportedException>(() => data.WriteAsync(buffer, 0, buffer.Length));
            await Assert.ThrowsAsync<NotSupportedException>(() => data.FlushAsync());
        }
        else
        {
            Assert.Throws<NotSupportedException>(() => data.Write(buffer));
            Assert.Throws<NotSupportedException>(() => data.Write(buffer, 0, buffer.Length));
            Assert.Throws<NotSupportedException>(() => data.Flush());
        }
        Assert.Equal(before, bytes.Position);
    }

    [Theory]
    [InlineData(ZipArchiveMode.Read)]
    [InlineData(ZipArchiveMode.Create)]
    [InlineData(ZipArchiveMode.Update)]
    public async Task OtherModes_RejectForwardEnumeration(ZipArchiveMode mode)
    {
        using MemoryStream bytes = new();
        bytes.Write(CreateFixture());
        bytes.Position = 0;
        using ZipArchive archive = new(bytes, mode);
        Assert.Throws<NotSupportedException>(() => archive.GetNextEntry());
        await Assert.ThrowsAsync<NotSupportedException>(async () => await archive.GetNextEntryAsync());
    }

    [Theory]
    [MemberData(nameof(ZipFileTestBase.Get_Booleans_Data), MemberType = typeof(ZipFileTestBase))]
    public async Task Deflate_UnopenedInvalidDataIsSkipped(bool useAsync)
    {
        byte[] fixture = ReplaceFirstDeflatePayload([0x07], []);
        using MemoryStream bytes = new(fixture);
        await using Stream source = CreateSource(bytes, useAsync);
        await using ZipArchive archive = new(source, ZipArchiveMode.ForwardRead, leaveOpen: true);
        Assert.Equal(s_names[0], (await Next(archive, useAsync)).FullName);
        Assert.Equal(s_names[1], (await Next(archive, useAsync)).FullName);
        Assert.Null(await Next(archive, useAsync));
    }

    [Theory]
    [InlineData(false, "short", 1)]
    [InlineData(true, "short", 1)]
    [InlineData(false, "long", 1)]
    [InlineData(true, "long", 1)]
    [InlineData(false, "truncated", 1)]
    [InlineData(true, "truncated", 1)]
    [InlineData(false, "unfinished", 1)]
    [InlineData(true, "unfinished", 1)]
    [InlineData(false, "trailing", 1)]
    [InlineData(true, "trailing", 1)]
    // Fetch the final block and trailing junk together to exercise buffered leftovers.
    [InlineData(false, "trailing", 7)]
    [InlineData(true, "trailing", 7)]
    public async Task Deflate_InvalidPayloadThrowsOnReadOrDrain(bool useAsync, string corruption, int maxRead)
    {
        byte[] fixture;
        switch (corruption)
        {
            case "short":
            case "long":
            {
                fixture = CreateFixture(CompressionLevel.Optimal);
                int declaredLength = s_payloads[0].Length + (corruption == "short" ? -1 : 1);
                BinaryPrimitives.WriteUInt32LittleEndian(fixture.AsSpan(22), (uint)declaredLength);
                break;
            }
            case "truncated":
            {
                fixture = CreateFixture(CompressionLevel.Optimal);
                int compressedLength = (int)BinaryPrimitives.ReadUInt32LittleEndian(fixture.AsSpan(18));
                Array.Resize(ref fixture, DataOffset(fixture, 0) + compressedLength - 1);
                break;
            }
            case "unfinished":
            {
                // This non-final Deflate block emits the expected output but lacks a final block.
                fixture = ReplaceFirstDeflatePayload([0x00, 0x01, 0x00, 0xfe, 0xff, 0x42], [0x42]);
                break;
            }
            case "trailing":
            {
                fixture = ReplaceFirstDeflatePayload([0x01, 0x01, 0x00, 0xfe, 0xff, 0x42, 0xff], [0x42]);
                break;
            }
            default:
            {
                throw new ArgumentOutOfRangeException(nameof(corruption), corruption, "Unknown corruption scenario.");
            }
        }
        foreach (bool drain in new[] { false, true })
        {
            using MemoryStream bytes = new(fixture);
            await using Stream source = CreateSource(bytes, useAsync, maxRead);
            await using ZipArchive archive = new(source, ZipArchiveMode.ForwardRead, leaveOpen: true);
            await using Stream data = await Open(await Next(archive, useAsync), useAsync);
            if (drain)
            {
                await data.DisposeAsync();
                await Assert.ThrowsAsync<InvalidDataException>(async () => await Next(archive, useAsync));
            }
            else
            {
                await Assert.ThrowsAsync<InvalidDataException>(async () =>
                {
                    if (useAsync)
                    {
                        await data.CopyToAsync(Stream.Null);
                    }
                    else
                    {
                        data.CopyTo(Stream.Null);
                    }
                });
            }
        }
    }

    [Theory]
    [MemberData(nameof(ZipFileTestBase.Get_Booleans_Data), MemberType = typeof(ZipFileTestBase))]
    public async Task Deflate_HighExpansionWithSmallDestination(bool useAsync)
    {
        byte[] payload = new byte[200_000];
        Array.Fill(payload, (byte)0x42);
        byte[] fixture = CreateFixture(CompressionLevel.Optimal, [payload, s_payloads[1]]);
        using MemoryStream bytes = new(fixture);
        await using Stream source = CreateSource(bytes, useAsync);
        await using ZipArchive archive = new(source, ZipArchiveMode.ForwardRead, leaveOpen: true);
        ZipArchiveEntry first = await Next(archive, useAsync);
        Assert.True(first.CompressedLength < payload.Length / 100);
        await using Stream data = await Open(first, useAsync);
        using MemoryStream output = new();
        byte[] buffer = new byte[7];
        int read;
        while ((read = useAsync ? await data.ReadAsync(buffer) : data.Read(buffer)) != 0)
        {
            output.Write(buffer, 0, read);
        }
        Assert.Equal(payload, output.ToArray());
        Assert.Equal(DataOffset(fixture, 0) + first.CompressedLength, bytes.Position);
        Assert.Equal(s_names[1], (await Next(archive, useAsync)).FullName);
        Assert.Null(await Next(archive, useAsync));
    }

    private static byte[] ReplaceFirstDeflatePayload(byte[] compressed, byte[] payload)
    {
        byte[] fixture = CreateFixture(CompressionLevel.Optimal);
        int start = DataOffset(fixture, 0);
        int end = start + (int)BinaryPrimitives.ReadUInt32LittleEndian(fixture.AsSpan(18));
        byte[] result = new byte[fixture.Length - (end - start) + compressed.Length];
        fixture.AsSpan(0, start).CopyTo(result);
        compressed.CopyTo(result, start);
        fixture.AsSpan(end).CopyTo(result.AsSpan(start + compressed.Length));
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(14), (uint)CRC.CalculateCRC(payload));
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(18), (uint)compressed.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(22), (uint)payload.Length);
        return result;
    }

    private static byte[] CreateFixture(CompressionLevel compressionLevel = CompressionLevel.NoCompression, byte[][] payloads = null)
    {
        payloads ??= s_payloads;
        using MemoryStream bytes = new();
        using (ZipArchive archive = new(bytes, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (int i = 0; i < s_names.Length; i++)
            {
                ZipArchiveEntry entry = archive.CreateEntry(s_names[i], compressionLevel);
                entry.LastWriteTime = s_timestamp;
                using Stream data = entry.Open();
                data.Write(payloads[i]);
            }
        }
        byte[] fixture = bytes.ToArray();
        int offset = 0;
        foreach (byte[] payload in payloads)
        {
            Assert.Equal(0x04034b50u, BinaryPrimitives.ReadUInt32LittleEndian(fixture.AsSpan(offset)));
            Assert.Equal(0, BinaryPrimitives.ReadUInt16LittleEndian(fixture.AsSpan(offset + 6)) & 9);
            int options = compressionLevel switch
            {
                CompressionLevel.SmallestSize => 2,
                CompressionLevel.Fastest => 6,
                _ => 0
            };
            Assert.Equal(options, BinaryPrimitives.ReadUInt16LittleEndian(fixture.AsSpan(offset + 6)) & 6);
            Assert.Equal(compressionLevel == CompressionLevel.NoCompression ? 0 : 8, BinaryPrimitives.ReadUInt16LittleEndian(fixture.AsSpan(offset + 8)));
            uint compressedLength = BinaryPrimitives.ReadUInt32LittleEndian(fixture.AsSpan(offset + 18));
            if (compressionLevel == CompressionLevel.NoCompression)
            {
                Assert.Equal((uint)payload.Length, compressedLength);
            }
            Assert.Equal((uint)payload.Length, BinaryPrimitives.ReadUInt32LittleEndian(fixture.AsSpan(offset + 22)));
            offset = DataOffset(fixture, offset) + (int)compressedLength;
        }
        Assert.Equal(0x02014b50u, BinaryPrimitives.ReadUInt32LittleEndian(fixture.AsSpan(offset)));
        return fixture;
    }

    private static int DataOffset(byte[] fixture, int header) =>
        header + 30 + BinaryPrimitives.ReadUInt16LittleEndian(fixture.AsSpan(header + 26)) +
        BinaryPrimitives.ReadUInt16LittleEndian(fixture.AsSpan(header + 28));

    private static Stream CreateSource(MemoryStream bytes, bool useAsync, int maxRead = 1)
    {
        Stream source = new ClampedReadStream(new WrappedStream(bytes, canRead: true, canWrite: false, canSeek: false), maxRead);
        return useAsync ? new NoSyncCallsStream(source) : source;
    }

    private static ValueTask<ZipArchiveEntry> Next(ZipArchive archive, bool useAsync) =>
        useAsync ? archive.GetNextEntryAsync() : ValueTask.FromResult(archive.GetNextEntry());

    private static Task<Stream> Open(ZipArchiveEntry entry, bool useAsync) =>
        useAsync ? entry.OpenAsync() : Task.FromResult(entry.Open());

}
