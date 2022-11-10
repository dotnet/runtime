// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarWriter_WriteEntryAsync_Roundtrip_Tests : TarTestsBase
    {
        public static IEnumerable<object[]> NameRoundtripsAsyncTheoryData()
            => TarWriter_WriteEntry_Roundtrip_Tests.NameRoundtripsTheoryData();

        [Theory]
        [MemberData(nameof(NameRoundtripsAsyncTheoryData))]
        public async Task NameRoundtripsAsync(TarEntryFormat entryFormat, TarEntryType entryType, bool unseekableStream, string name)
        {
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, name);
            entry.Name = name;

            MemoryStream ms = new();
            Stream s = unseekableStream ? new WrappedStream(ms, ms.CanRead, ms.CanWrite, canSeek: false) : ms;

            await using (TarWriter writer = new(s, leaveOpen: true))
            {
                await writer.WriteEntryAsync(entry);
            }

            ms.Position = 0;
            await using TarReader reader = new(s);

            entry = await reader.GetNextEntryAsync();
            Assert.Null(await reader.GetNextEntryAsync());
            Assert.Equal(name, entry.Name);
        }

        public static IEnumerable<object[]> LinkNameRoundtripsAsyncTheoryData()
            => TarWriter_WriteEntry_Roundtrip_Tests.LinkNameRoundtripsTheoryData();

        [Theory]
        [MemberData(nameof(LinkNameRoundtripsAsyncTheoryData))]
        public async Task LinkNameRoundtripsAsync(TarEntryFormat entryFormat, TarEntryType entryType, bool unseekableStream, string linkName)
        {
            string name = "foo";
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, name);
            entry.LinkName = linkName;

            MemoryStream ms = new();
            Stream s = unseekableStream ? new WrappedStream(ms, ms.CanRead, ms.CanWrite, canSeek: false) : ms;

            await using (TarWriter writer = new(s, leaveOpen: true))
            {
                await writer.WriteEntryAsync(entry);
            }

            ms.Position = 0;
            await using TarReader reader = new(s);

            entry = await reader.GetNextEntryAsync();
            Assert.Null(await reader.GetNextEntryAsync());
            Assert.Equal(name, entry.Name);
            Assert.Equal(linkName, entry.LinkName);
        }

        public static IEnumerable<object[]> UserNameGroupNameRoundtripsAsyncTheoryData()
            => TarWriter_WriteEntry_Roundtrip_Tests.UserNameGroupNameRoundtripsTheoryData();

        [Theory]
        [MemberData(nameof(UserNameGroupNameRoundtripsAsyncTheoryData))]
        public async Task UserNameGroupNameRoundtripsAsync(TarEntryFormat entryFormat, bool unseekableStream, string userGroupName)
        {
            string name = "foo";
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, TarEntryType.RegularFile, name);
            PosixTarEntry posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
            posixEntry.UserName = userGroupName;
            posixEntry.GroupName = userGroupName;

            MemoryStream ms = new();
            Stream s = unseekableStream ? new WrappedStream(ms, ms.CanRead, ms.CanWrite, canSeek: false) : ms;

            await using (TarWriter writer = new(s, leaveOpen: true))
            {
                await writer.WriteEntryAsync(posixEntry);
            }

            ms.Position = 0;
            await using TarReader reader = new(s);

            entry = await reader.GetNextEntryAsync();
            posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
            Assert.Null(await reader.GetNextEntryAsync());

            Assert.Equal(name, posixEntry.Name);
            Assert.Equal(userGroupName, posixEntry.UserName);
            Assert.Equal(userGroupName, posixEntry.GroupName);
        }

        [Theory]
        [InlineData(TarEntryType.RegularFile)]
        [InlineData(TarEntryType.Directory)]
        [InlineData(TarEntryType.HardLink)]
        [InlineData(TarEntryType.SymbolicLink)]
        public async Task PaxExtendedAttributes_DoNotOverwritePublicProperties_WhenTheyFitOnLegacyFieldsAsync(TarEntryType entryType)
        {
            Dictionary<string, string> extendedAttributes = new();
            extendedAttributes[PaxEaName] = "ea_name";
            extendedAttributes[PaxEaGName] = "ea_gname";
            extendedAttributes[PaxEaUName] = "ea_uname";
            extendedAttributes[PaxEaMTime] = GetTimestampStringFromDateTimeOffset(TestModificationTime);
            extendedAttributes[PaxEaSize] = 42.ToString();

            if (entryType is TarEntryType.HardLink or TarEntryType.SymbolicLink)
            {
                extendedAttributes[PaxEaLinkName] = "ea_linkname";
            }

            PaxTarEntry writeEntry = new PaxTarEntry(entryType, "name", extendedAttributes);
            writeEntry.Name = new string('a', 100);
            // GName and UName must be longer than 32 to be written as extended attribute.
            writeEntry.GroupName = new string('b', 32);
            writeEntry.UserName = new string('c', 32);
            // There's no limit on MTime, we just ensure it roundtrips.
            writeEntry.ModificationTime = TestModificationTime.AddDays(1);

            if (entryType is TarEntryType.HardLink or TarEntryType.SymbolicLink)
            {
                writeEntry.LinkName = new string('d', 100);
            }

            MemoryStream ms = new();
            await using (TarWriter w = new(ms, leaveOpen: true))
            {
                await w.WriteEntryAsync(writeEntry);
            }
            ms.Position = 0;

            await using TarReader r = new(ms);
            PaxTarEntry readEntry = Assert.IsType<PaxTarEntry>(await r.GetNextEntryAsync());
            Assert.Null(await r.GetNextEntryAsync());

            Assert.Equal(writeEntry.Name, readEntry.Name);
            Assert.Equal(writeEntry.GroupName, readEntry.GroupName);
            Assert.Equal(writeEntry.UserName, readEntry.UserName);
            Assert.Equal(writeEntry.ModificationTime, readEntry.ModificationTime);
            Assert.Equal(writeEntry.LinkName, readEntry.LinkName);

            Assert.Equal(0, writeEntry.Length);
            Assert.Equal(0, readEntry.Length);
        }

        [Theory]
        [InlineData(TarEntryType.RegularFile)]
        [InlineData(TarEntryType.Directory)]
        [InlineData(TarEntryType.HardLink)]
        [InlineData(TarEntryType.SymbolicLink)]
        public async Task PaxExtendedAttributes_DoNotOverwritePublicProperties_WhenLargerThanLegacyFieldsAsync(TarEntryType entryType)
        {
            Dictionary<string, string> extendedAttributes = new();
            extendedAttributes[PaxEaName] = "ea_name";
            extendedAttributes[PaxEaGName] = "ea_gname";
            extendedAttributes[PaxEaUName] = "ea_uname";
            extendedAttributes[PaxEaMTime] = GetTimestampStringFromDateTimeOffset(TestModificationTime);

            if (entryType is TarEntryType.HardLink or TarEntryType.SymbolicLink)
            {
                extendedAttributes[PaxEaLinkName] = "ea_linkname";
            }

            PaxTarEntry writeEntry = new PaxTarEntry(entryType, "name", extendedAttributes);
            writeEntry.Name = new string('a', MaxPathComponent);
            // GName and UName must be longer than 32 to be written as extended attribute.
            writeEntry.GroupName = new string('b', 32 + 1);
            writeEntry.UserName = new string('c', 32 + 1);
            // There's no limit on MTime, we just ensure it roundtrips.
            writeEntry.ModificationTime = TestModificationTime.AddDays(1);

            if (entryType is TarEntryType.HardLink or TarEntryType.SymbolicLink)
            {
                writeEntry.LinkName = new string('d', 100 + 1);
            }

            MemoryStream ms = new();
            await using (TarWriter w = new(ms, leaveOpen: true))
            {
                await w.WriteEntryAsync(writeEntry);
            }
            ms.Position = 0;

            await using TarReader r = new(ms);
            PaxTarEntry readEntry = Assert.IsType<PaxTarEntry>(await r.GetNextEntryAsync());
            Assert.Null(await r.GetNextEntryAsync());

            Assert.Equal(writeEntry.Name, readEntry.Name);
            Assert.Equal(writeEntry.GroupName, readEntry.GroupName);
            Assert.Equal(writeEntry.UserName, readEntry.UserName);
            Assert.Equal(writeEntry.ModificationTime, readEntry.ModificationTime);
            Assert.Equal(writeEntry.LinkName, readEntry.LinkName);
        }
    }
}
