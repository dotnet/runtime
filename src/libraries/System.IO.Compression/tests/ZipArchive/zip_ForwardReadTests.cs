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
    [MemberData(nameof(ZipFileTestBase.Get_Booleans_Data), MemberType = typeof(ZipFileTestBase))]
    public async Task StoredEntries_ReadDataAndLocalMetadata(bool useAsync)
    {
        using MemoryStream bytes = new(CreateFixture());
        await using Stream source = CreateSource(bytes, useAsync);
        await using ZipArchive archive = new(source, ZipArchiveMode.ForwardRead, leaveOpen: true);
        Assert.Equal(0, bytes.Position);
        Assert.Equal(3, (int)archive.Mode);
        for (int i = 0; i < s_names.Length; i++)
        {
            ZipArchiveEntry entry = await Next(archive, useAsync);
            Assert.Equal(s_names[i], entry.FullName);
            Assert.Equal(Path.GetFileName(s_names[i]), entry.Name);
            Assert.Equal(s_payloads[i].Length, entry.Length);
            Assert.Equal(entry.Length, entry.CompressedLength);
            Assert.Equal((uint)CRC.CalculateCRC(s_payloads[i]), entry.Crc32);
            Assert.Equal(s_timestamp.DateTime, entry.LastWriteTime.DateTime);
            await using Stream data = await Open(entry, useAsync);
            Assert.False(data.CanSeek);
            Assert.False(data.CanWrite);
            using MemoryStream output = new();
            if (useAsync)
            {
                await data.CopyToAsync(output);
            }
            else
            {
                data.CopyTo(output);
            }
            Assert.Equal(s_payloads[i], output.ToArray());
        }
        Assert.Null(await Next(archive, useAsync));
        Assert.Null(await Next(archive, useAsync));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Advance_DrainsPartialEntryAndInvalidatesHandle(bool useAsync, bool disposeHandle)
    {
        using MemoryStream bytes = new(CreateFixture());
        await using Stream source = CreateSource(bytes, useAsync);
        await using ZipArchive archive = new(source, ZipArchiveMode.ForwardRead, leaveOpen: true);
        ZipArchiveEntry first = await Next(archive, useAsync);
        await using Stream data = await Open(first, useAsync);
        byte[] buffer = new byte[1];
        Assert.Equal(1, useAsync ? await data.ReadAsync(buffer) : data.Read(buffer));
        Assert.Equal(s_payloads[0][0], buffer[0]);
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

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    public async Task CorruptPayload_OnlyOpenedEntriesValidateCrc(bool useAsync, int consumption)
    {
        byte[] fixture = CreateFixture();
        fixture[DataOffset(fixture, 0)] ^= 0xff;
        using MemoryStream bytes = new(fixture);
        await using Stream source = CreateSource(bytes, useAsync);
        await using ZipArchive archive = new(source, ZipArchiveMode.ForwardRead, leaveOpen: true);
        ZipArchiveEntry first = await Next(archive, useAsync);
        if (consumption == 0)
        {
            Assert.Equal(s_names[1], (await Next(archive, useAsync)).FullName);
            Assert.Null(await Next(archive, useAsync));
            return;
        }
        await using Stream data = await Open(first, useAsync);
        if (consumption == 1)
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

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Dispose_DoesNotDrainAndHonorsLeaveOpen(bool useAsync, bool leaveOpen)
    {
        using MemoryStream bytes = new(CreateFixture());
        await using Stream source = CreateSource(bytes, useAsync);
        ZipArchive archive = new(source, ZipArchiveMode.ForwardRead, leaveOpen);
        await using Stream data = await Open(await Next(archive, useAsync), useAsync);
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
    [InlineData(false, 8, 8u)] // Deflate method.
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

    [Fact]
    public async Task ForwardRead_RejectsRandomAccessAndRepeatedOpen()
    {
        using MemoryStream bytes = new(CreateFixture());
        using ZipArchive archive = new(bytes, ZipArchiveMode.ForwardRead);
        Assert.Throws<NotSupportedException>(() => archive.Entries);
        Assert.Throws<NotSupportedException>(() => archive.GetEntry(s_names[0]));
        Assert.Throws<NotSupportedException>(() => archive.CreateEntry("new"));
        Assert.Throws<NotSupportedException>(() => archive.Comment);
        ZipArchiveEntry entry = archive.GetNextEntry();
        Assert.Throws<NotSupportedException>(() => entry.Comment);
        Assert.Throws<NotSupportedException>(() => entry.ExternalAttributes);
        Assert.Throws<NotSupportedException>(() => entry.Delete());
        Assert.Throws<InvalidOperationException>(() => entry.Open(FileAccess.Write));
        using Stream data = entry.Open();
        Assert.Throws<InvalidOperationException>(() => entry.Open());
        await Assert.ThrowsAsync<InvalidOperationException>(() => entry.OpenAsync());
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

    private static byte[] CreateFixture()
    {
        using MemoryStream bytes = new();
        using (ZipArchive archive = new(bytes, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (int i = 0; i < s_names.Length; i++)
            {
                ZipArchiveEntry entry = archive.CreateEntry(s_names[i], CompressionLevel.NoCompression);
                entry.LastWriteTime = s_timestamp;
                using Stream data = entry.Open();
                data.Write(s_payloads[i]);
            }
        }
        byte[] fixture = bytes.ToArray();
        int offset = 0;
        foreach (byte[] payload in s_payloads)
        {
            Assert.Equal(0x04034b50u, BinaryPrimitives.ReadUInt32LittleEndian(fixture.AsSpan(offset)));
            Assert.Equal(0, BinaryPrimitives.ReadUInt16LittleEndian(fixture.AsSpan(offset + 6)) & 9);
            Assert.Equal(0, BinaryPrimitives.ReadUInt16LittleEndian(fixture.AsSpan(offset + 8)));
            Assert.Equal((uint)payload.Length, BinaryPrimitives.ReadUInt32LittleEndian(fixture.AsSpan(offset + 18)));
            Assert.Equal((uint)payload.Length, BinaryPrimitives.ReadUInt32LittleEndian(fixture.AsSpan(offset + 22)));
            offset = DataOffset(fixture, offset) + payload.Length;
        }
        Assert.Equal(0x02014b50u, BinaryPrimitives.ReadUInt32LittleEndian(fixture.AsSpan(offset)));
        return fixture;
    }

    private static int DataOffset(byte[] fixture, int header) =>
        header + 30 + BinaryPrimitives.ReadUInt16LittleEndian(fixture.AsSpan(header + 26)) +
        BinaryPrimitives.ReadUInt16LittleEndian(fixture.AsSpan(header + 28));

    private static Stream CreateSource(MemoryStream bytes, bool useAsync)
    {
        Stream source = new ClampedReadStream(new WrappedStream(bytes, canRead: true, canWrite: false, canSeek: false), 1);
        return useAsync ? new NoSyncCallsStream(source) : source;
    }

    private static ValueTask<ZipArchiveEntry> Next(ZipArchive archive, bool useAsync) =>
        useAsync ? archive.GetNextEntryAsync() : ValueTask.FromResult(archive.GetNextEntry());

    private static Task<Stream> Open(ZipArchiveEntry entry, bool useAsync) =>
        useAsync ? entry.OpenAsync() : Task.FromResult(entry.Open());

}
