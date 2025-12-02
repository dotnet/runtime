// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests;

[OuterLoop]
[Collection(nameof(DisableParallelization))] // don't create multiple large files at the same time
public class ManualTests : TarTestsBase
{
    public static bool ManualTestsEnabled => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MANUAL_TESTS"));

    public static IEnumerable<object[]> WriteEntry_LongFileSize_TheoryData()
    {
        foreach (bool unseekableStream in new[] { false, true })
        {
            foreach (TarEntryFormat entryFormat in new[] { TarEntryFormat.V7, TarEntryFormat.Ustar, TarEntryFormat.Gnu, TarEntryFormat.Pax })
            {
                yield return new object[] { entryFormat, LegacyMaxFileSize, unseekableStream };
            }

            // Pax and Gnu supports unlimited size files.
            yield return new object[] { TarEntryFormat.Pax, LegacyMaxFileSize + 1, unseekableStream };
            yield return new object[] { TarEntryFormat.Gnu, LegacyMaxFileSize + 1, unseekableStream };
        }
    }

    [ConditionalTheory(nameof(ManualTestsEnabled))]
    [MemberData(nameof(WriteEntry_LongFileSize_TheoryData))]
    [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.Android | TestPlatforms.Browser, "Needs too much disk space.")]
    public void WriteEntry_LongFileSize(TarEntryFormat entryFormat, long size, bool unseekableStream)
    {
        // Write archive with a 8 Gb long entry.
        using FileStream tarFile = File.Open(GetTestFilePath(), new FileStreamOptions { Access = FileAccess.ReadWrite, Mode = FileMode.Create, Options = FileOptions.DeleteOnClose });
        Stream s = unseekableStream ? new WrappedStream(tarFile, tarFile.CanRead, tarFile.CanWrite, canSeek: false) : tarFile;

        using (TarWriter writer = new(s, leaveOpen: true))
        {
            TarEntry writeEntry = InvokeTarEntryCreationConstructor(entryFormat, entryFormat is TarEntryFormat.V7 ? TarEntryType.V7RegularFile : TarEntryType.RegularFile, "foo");
            writeEntry.DataStream = new SimulatedDataStream(size);
            writer.WriteEntry(writeEntry);
        }

        tarFile.Position = 0;

        // Read archive back.
        using TarReader reader = new TarReader(s);
        TarEntry entry = reader.GetNextEntry();
        Assert.Equal(size, entry.Length);

        Stream dataStream = entry.DataStream;
        Assert.Equal(size, dataStream.Length);
        Assert.Equal(0, dataStream.Position);

        ReadOnlySpan<byte> dummyData = SimulatedDataStream.DummyData.Span;

        // Read the first bytes.
        Span<byte> buffer = new byte[dummyData.Length];
        Assert.Equal(buffer.Length, dataStream.Read(buffer));
        AssertExtensions.SequenceEqual(dummyData, buffer);
        Assert.Equal(0, dataStream.ReadByte()); // check next byte is correct.
        buffer.Clear();

        // Read the last bytes.
        long dummyDataOffset = size - dummyData.Length - 1;
        if (dataStream.CanSeek)
        {
            Assert.False(unseekableStream);
            dataStream.Seek(dummyDataOffset, SeekOrigin.Begin);
        }
        else
        {
            Assert.True(unseekableStream);
            Span<byte> seekBuffer = new byte[4_096];

            while (dataStream.Position < dummyDataOffset)
            {
                int bufSize = (int)Math.Min(seekBuffer.Length, dummyDataOffset - dataStream.Position);
                int res = dataStream.Read(seekBuffer.Slice(0, bufSize));
                Assert.True(res > 0, "Unseekable stream finished before expected - Something went very wrong");
            }
        }

        Assert.Equal(0, dataStream.ReadByte()); // check previous byte is correct.
        Assert.Equal(buffer.Length, dataStream.Read(buffer));
        AssertExtensions.SequenceEqual(dummyData, buffer);
        Assert.Equal(size, dataStream.Position);

        Assert.Null(reader.GetNextEntry());
    }
}
