// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarWriter_WriteEntry_Roundtrip_Tests : TarTestsBase
    {
        public static IEnumerable<object[]> NameRoundtripsTheoryData()
        {
            foreach (bool unseekableStream in new[] { false, true })
            {
                foreach (TarEntryType entryType in new[] { TarEntryType.RegularFile, TarEntryType.Directory })
                {
                    foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.Name).Concat(GetNamesPrefixedTestData(NameCapabilities.Name)))
                    {
                        TarEntryType v7EntryType = entryType is TarEntryType.RegularFile ? TarEntryType.V7RegularFile : entryType;
                        yield return new object[] { TarEntryFormat.V7, v7EntryType, unseekableStream, name };
                    }

                    foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.NameAndPrefix).Concat(GetNamesPrefixedTestData(NameCapabilities.NameAndPrefix)))
                    {
                        yield return new object[] { TarEntryFormat.Ustar, entryType, unseekableStream, name };
                    }

                    foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.Unlimited).Concat(GetNamesPrefixedTestData(NameCapabilities.Unlimited)))
                    {
                        yield return new object[] { TarEntryFormat.Pax, entryType, unseekableStream, name };
                        yield return new object[] { TarEntryFormat.Gnu, entryType, unseekableStream, name };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(NameRoundtripsTheoryData))]
        public async Task NameRoundtrips(TarEntryFormat entryFormat, TarEntryType entryType, bool unseekableStream, string name)
        {
            foreach (bool async in Booleans)
            {
                TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, name);
                entry.Name = name;

                using MemoryStream ms = new();
                Stream s = unseekableStream ? new WrappedStream(ms, ms.CanRead, ms.CanWrite, canSeek: false) : ms;

                TarWriter writer = await CreateTarWriter(s, async, TarEntryFormat.Pax, leaveOpen: true);
                try
                {
                    await WriteEntry(writer, entry, async);
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }

                ms.Position = 0;
                TarReader reader = await CreateTarReader(s, async);
                try
                {
                    entry = await GetNextEntry(reader, async);
                    Assert.Null(await GetNextEntry(reader, async));
                    Assert.Equal(name, entry.Name);
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }

        public static IEnumerable<object[]> LinkNameRoundtripsTheoryData()
        {
            foreach (bool unseekableStream in new[] { false, true })
            {
                foreach (TarEntryType entryType in new[] { TarEntryType.SymbolicLink, TarEntryType.HardLink })
                {
                    foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.Name).Concat(GetNamesPrefixedTestData(NameCapabilities.Name)))
                    {
                        yield return new object[] { TarEntryFormat.V7, entryType, unseekableStream, name };
                        yield return new object[] { TarEntryFormat.Ustar, entryType, unseekableStream, name };
                    }

                    foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.Unlimited).Concat(GetNamesPrefixedTestData(NameCapabilities.Unlimited)))
                    {
                        yield return new object[] { TarEntryFormat.Pax, entryType, unseekableStream, name };
                        yield return new object[] { TarEntryFormat.Gnu, entryType, unseekableStream, name };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(LinkNameRoundtripsTheoryData))]
        public async Task LinkNameRoundtrips(TarEntryFormat entryFormat, TarEntryType entryType, bool unseekableStream, string linkName)
        {
            foreach (bool async in Booleans)
            {
                string name = "foo";
                TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, name);
                entry.LinkName = linkName;

                using MemoryStream ms = new();
                Stream s = unseekableStream ? new WrappedStream(ms, ms.CanRead, ms.CanWrite, canSeek: false) : ms;

                TarWriter writer = await CreateTarWriter(s, async, TarEntryFormat.Pax, leaveOpen: true);
                try
                {
                    await WriteEntry(writer, entry, async);
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }

                ms.Position = 0;
                TarReader reader = await CreateTarReader(s, async);
                try
                {
                    entry = await GetNextEntry(reader, async);
                    Assert.Null(await GetNextEntry(reader, async));
                    Assert.Equal(name, entry.Name);
                    Assert.Equal(linkName, entry.LinkName);
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }

        public static IEnumerable<object[]> UserNameGroupNameRoundtripsTheoryData()
        {
            foreach (bool unseekableStream in new[] { false, true })
            {
                foreach (TarEntryFormat entryFormat in new[] { TarEntryFormat.Ustar, TarEntryFormat.Pax, TarEntryFormat.Gnu })
                {
                    yield return new object[] { entryFormat, unseekableStream, Repeat(OneByteCharacter, 32) };
                    yield return new object[] { entryFormat, unseekableStream, Repeat(TwoBytesCharacter, 32 / 2) };
                    yield return new object[] { entryFormat, unseekableStream, Repeat(FourBytesCharacter, 32 / 4) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(UserNameGroupNameRoundtripsTheoryData))]
        public async Task UserNameGroupNameRoundtrips(TarEntryFormat entryFormat, bool unseekableStream, string userGroupName)
        {
            foreach (bool async in Booleans)
            {
                string name = "foo";
                TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, TarEntryType.RegularFile, name);
                PosixTarEntry posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
                posixEntry.UserName = userGroupName;
                posixEntry.GroupName = userGroupName;

                using MemoryStream ms = new();
                Stream s = unseekableStream ? new WrappedStream(ms, ms.CanRead, ms.CanWrite, canSeek: false) : ms;

                TarWriter writer = await CreateTarWriter(s, async, TarEntryFormat.Pax, leaveOpen: true);
                try
                {
                    await WriteEntry(writer, posixEntry, async);
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }

                ms.Position = 0;
                TarReader reader = await CreateTarReader(s, async);
                try
                {
                    entry = await GetNextEntry(reader, async);
                    posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
                    Assert.Null(await GetNextEntry(reader, async));

                    Assert.Equal(name, posixEntry.Name);
                    Assert.Equal(userGroupName, posixEntry.UserName);
                    Assert.Equal(userGroupName, posixEntry.GroupName);
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }

        [Theory]
        [InlineData(TarEntryType.RegularFile)]
        [InlineData(TarEntryType.Directory)]
        [InlineData(TarEntryType.HardLink)]
        [InlineData(TarEntryType.SymbolicLink)]
        public async Task PaxExtendedAttributes_DoNotOverwritePublicProperties_WhenTheyFitOnLegacyFields(TarEntryType entryType)
        {
            foreach (bool async in Booleans)
            {
                Dictionary<string, string> extendedAttributes = new();
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
                writeEntry.GroupName = new string('b', 32);
                writeEntry.UserName = new string('c', 32);
                writeEntry.ModificationTime = TestModificationTime.AddDays(1);

                if (entryType is TarEntryType.HardLink or TarEntryType.SymbolicLink)
                {
                    writeEntry.LinkName = new string('d', 100);
                }

                using MemoryStream ms = new();
                TarWriter writer = await CreateTarWriter(ms, async, TarEntryFormat.Pax, leaveOpen: true);
                try
                {
                    await WriteEntry(writer, writeEntry, async);
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }
                ms.Position = 0;

                TarReader reader = await CreateTarReader(ms, async);
                try
                {
                    PaxTarEntry readEntry = Assert.IsType<PaxTarEntry>(await GetNextEntry(reader, async));
                    Assert.Null(await GetNextEntry(reader, async));

                    Assert.Equal(writeEntry.Name, readEntry.Name);
                    Assert.Equal(writeEntry.GroupName, readEntry.GroupName);
                    Assert.Equal(writeEntry.UserName, readEntry.UserName);
                    Assert.Equal(writeEntry.ModificationTime, readEntry.ModificationTime);
                    Assert.Equal(writeEntry.LinkName, readEntry.LinkName);

                    Assert.Equal(0, writeEntry.Length);
                    Assert.Equal(0, readEntry.Length);
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }

        [Theory]
        [InlineData(TarEntryType.RegularFile)]
        [InlineData(TarEntryType.Directory)]
        [InlineData(TarEntryType.HardLink)]
        [InlineData(TarEntryType.SymbolicLink)]
        public async Task PaxExtendedAttributes_DoNotOverwritePublicProperties_WhenLargerThanLegacyFields(TarEntryType entryType)
        {
            foreach (bool async in Booleans)
            {
                Dictionary<string, string> extendedAttributes = new();
                extendedAttributes[PaxEaGName] = "ea_gname";
                extendedAttributes[PaxEaUName] = "ea_uname";
                extendedAttributes[PaxEaMTime] = GetTimestampStringFromDateTimeOffset(TestModificationTime);

                if (entryType is TarEntryType.HardLink or TarEntryType.SymbolicLink)
                {
                    extendedAttributes[PaxEaLinkName] = "ea_linkname";
                }

                PaxTarEntry writeEntry = new PaxTarEntry(entryType, "name", extendedAttributes);
                writeEntry.Name = new string('a', MaxPathComponent);
                writeEntry.GroupName = new string('b', 32 + 1);
                writeEntry.UserName = new string('c', 32 + 1);
                writeEntry.ModificationTime = TestModificationTime.AddDays(1);

                if (entryType is TarEntryType.HardLink or TarEntryType.SymbolicLink)
                {
                    writeEntry.LinkName = new string('d', 100 + 1);
                }

                using MemoryStream ms = new();
                TarWriter writer = await CreateTarWriter(ms, async, TarEntryFormat.Pax, leaveOpen: true);
                try
                {
                    await WriteEntry(writer, writeEntry, async);
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }
                ms.Position = 0;

                TarReader reader = await CreateTarReader(ms, async);
                try
                {
                    PaxTarEntry readEntry = Assert.IsType<PaxTarEntry>(await GetNextEntry(reader, async));
                    Assert.Null(await GetNextEntry(reader, async));

                    Assert.Equal(writeEntry.Name, readEntry.Name);
                    Assert.Equal(writeEntry.GroupName, readEntry.GroupName);
                    Assert.Equal(writeEntry.UserName, readEntry.UserName);
                    Assert.Equal(writeEntry.ModificationTime, readEntry.ModificationTime);
                    Assert.Equal(writeEntry.LinkName, readEntry.LinkName);
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }
    }
}
