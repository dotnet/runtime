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
            foreach (object[] asyncData in GetBooleanData())
            {
                bool async = (bool)asyncData[0];

                foreach (TarEntryType entryType in new[] { TarEntryType.RegularFile, TarEntryType.Directory })
                {
                    foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.Name).Concat(GetNamesPrefixedTestData(NameCapabilities.Name)))
                    {
                        TarEntryType v7EntryType = entryType is TarEntryType.RegularFile ? TarEntryType.V7RegularFile : entryType;
                        yield return new object[] { TarEntryFormat.V7, v7EntryType, false, name, async };
                        yield return new object[] { TarEntryFormat.V7, v7EntryType, true, name, async };
                    }

                    foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.NameAndPrefix).Concat(GetNamesPrefixedTestData(NameCapabilities.NameAndPrefix)))
                    {
                        yield return new object[] { TarEntryFormat.Ustar, entryType, false, name, async };
                        yield return new object[] { TarEntryFormat.Ustar, entryType, true, name, async };
                    }

                    foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.Unlimited).Concat(GetNamesPrefixedTestData(NameCapabilities.Unlimited)))
                    {
                        yield return new object[] { TarEntryFormat.Pax, entryType, false, name, async };
                        yield return new object[] { TarEntryFormat.Pax, entryType, true, name, async };
                        yield return new object[] { TarEntryFormat.Gnu, entryType, false, name, async };
                        yield return new object[] { TarEntryFormat.Gnu, entryType, true, name, async };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(NameRoundtripsTheoryData))]
        public async Task NameRoundtrips(TarEntryFormat entryFormat, TarEntryType entryType, bool unseekableStream, string name, bool async)
        {
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, name);
            entry.Name = name;

            using MemoryStream ms = new();
            Stream s = unseekableStream ? new WrappedStream(ms, ms.CanRead, ms.CanWrite, canSeek: false) : ms;

            {
                await using TarWriterHolder writerHolder = CreateTarWriter(s, async, TarEntryFormat.Pax, leaveOpen: true);
                TarWriter writer = writerHolder;

                await WriteEntry(writer, entry, async);
            }

            ms.Position = 0;
            {
                await using TarReaderHolder readerHolder = CreateTarReader(s, async, leaveOpen: false);
                TarReader reader = readerHolder;

                entry = await GetNextEntry(reader, async: async);
                Assert.Null(await GetNextEntry(reader, async: async));
                Assert.Equal(name, entry.Name);
            }
        }

        public static IEnumerable<object[]> LinkNameRoundtripsTheoryData()
        {
            foreach (object[] asyncData in GetBooleanData())
            {
                bool async = (bool)asyncData[0];

                foreach (TarEntryType entryType in new[] { TarEntryType.SymbolicLink, TarEntryType.HardLink })
                {
                    foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.Name).Concat(GetNamesPrefixedTestData(NameCapabilities.Name)))
                    {
                        yield return new object[] { TarEntryFormat.V7, entryType, false, name, async };
                        yield return new object[] { TarEntryFormat.V7, entryType, true, name, async };
                        yield return new object[] { TarEntryFormat.Ustar, entryType, false, name, async };
                        yield return new object[] { TarEntryFormat.Ustar, entryType, true, name, async };
                    }

                    foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.Unlimited).Concat(GetNamesPrefixedTestData(NameCapabilities.Unlimited)))
                    {
                        yield return new object[] { TarEntryFormat.Pax, entryType, false, name, async };
                        yield return new object[] { TarEntryFormat.Pax, entryType, true, name, async };
                        yield return new object[] { TarEntryFormat.Gnu, entryType, false, name, async };
                        yield return new object[] { TarEntryFormat.Gnu, entryType, true, name, async };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(LinkNameRoundtripsTheoryData))]
        public async Task LinkNameRoundtrips(TarEntryFormat entryFormat, TarEntryType entryType, bool unseekableStream, string linkName, bool async)
        {
            string name = "foo";
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, name);
            entry.LinkName = linkName;

            using MemoryStream ms = new();
            Stream s = unseekableStream ? new WrappedStream(ms, ms.CanRead, ms.CanWrite, canSeek: false) : ms;

            {
                await using TarWriterHolder writerHolder = CreateTarWriter(s, async, TarEntryFormat.Pax, leaveOpen: true);
                TarWriter writer = writerHolder;

                await WriteEntry(writer, entry, async);
            }

            ms.Position = 0;
            {
                await using TarReaderHolder readerHolder = CreateTarReader(s, async, leaveOpen: false);
                TarReader reader = readerHolder;

                entry = await GetNextEntry(reader, async: async);
                Assert.Null(await GetNextEntry(reader, async: async));
                Assert.Equal(name, entry.Name);
                Assert.Equal(linkName, entry.LinkName);
            }
        }

        public static IEnumerable<object[]> UserNameGroupNameRoundtripsTheoryData()
        {
            foreach (object[] asyncData in GetBooleanData())
            {
                bool async = (bool)asyncData[0];

                foreach (TarEntryFormat entryFormat in new[] { TarEntryFormat.Ustar, TarEntryFormat.Pax, TarEntryFormat.Gnu })
                {
                    yield return new object[] { entryFormat, false, Repeat(OneByteCharacter, 32), async };
                    yield return new object[] { entryFormat, true, Repeat(OneByteCharacter, 32), async };
                    yield return new object[] { entryFormat, false, Repeat(TwoBytesCharacter, 32 / 2), async };
                    yield return new object[] { entryFormat, true, Repeat(TwoBytesCharacter, 32 / 2), async };
                    yield return new object[] { entryFormat, false, Repeat(FourBytesCharacter, 32 / 4), async };
                    yield return new object[] { entryFormat, true, Repeat(FourBytesCharacter, 32 / 4), async };
                }
            }
        }

        [Theory]
        [MemberData(nameof(UserNameGroupNameRoundtripsTheoryData))]
        public async Task UserNameGroupNameRoundtrips(TarEntryFormat entryFormat, bool unseekableStream, string userGroupName, bool async)
        {
            string name = "foo";
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, TarEntryType.RegularFile, name);
            PosixTarEntry posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
            posixEntry.UserName = userGroupName;
            posixEntry.GroupName = userGroupName;

            using MemoryStream ms = new();
            Stream s = unseekableStream ? new WrappedStream(ms, ms.CanRead, ms.CanWrite, canSeek: false) : ms;

            {
                await using TarWriterHolder writerHolder = CreateTarWriter(s, async, TarEntryFormat.Pax, leaveOpen: true);
                TarWriter writer = writerHolder;

                await WriteEntry(writer, posixEntry, async);
            }

            ms.Position = 0;
            {
                await using TarReaderHolder readerHolder = CreateTarReader(s, async, leaveOpen: false);
                TarReader reader = readerHolder;

                entry = await GetNextEntry(reader, async: async);
                posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
                Assert.Null(await GetNextEntry(reader, async: async));

                Assert.Equal(name, posixEntry.Name);
                Assert.Equal(userGroupName, posixEntry.UserName);
                Assert.Equal(userGroupName, posixEntry.GroupName);
            }
        }

        public static IEnumerable<object[]> PaxExtendedAttributesEntryTypeAndBooleanData()
        {
            foreach (object[] data in GetDataAndBooleanData(new[]
            {
                new object[] { TarEntryType.RegularFile },
                new object[] { TarEntryType.Directory },
                new object[] { TarEntryType.HardLink },
                new object[] { TarEntryType.SymbolicLink }
            }))
            {
                yield return data;
            }
        }

        [Theory]
        [MemberData(nameof(PaxExtendedAttributesEntryTypeAndBooleanData))]
        public async Task PaxExtendedAttributes_DoNotOverwritePublicProperties_WhenTheyFitOnLegacyFields(TarEntryType entryType, bool async)
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
            // GName and UName must be longer than 32 to be written as extended attribute.
            writeEntry.GroupName = new string('b', 32);
            writeEntry.UserName = new string('c', 32);
            // There's no limit on MTime, we just ensure it roundtrips.
            writeEntry.ModificationTime = TestModificationTime.AddDays(1);

            if (entryType is TarEntryType.HardLink or TarEntryType.SymbolicLink)
            {
                writeEntry.LinkName = new string('d', 100);
            }

            using MemoryStream ms = new();
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(ms, async, TarEntryFormat.Pax, leaveOpen: true);
                TarWriter writer = writerHolder;

                await WriteEntry(writer, writeEntry, async);
            }
            ms.Position = 0;

            {
                await using TarReaderHolder readerHolder = CreateTarReader(ms, async, leaveOpen: false);
                TarReader reader = readerHolder;

                PaxTarEntry readEntry = Assert.IsType<PaxTarEntry>(await GetNextEntry(reader, async: async));
                Assert.Null(await GetNextEntry(reader, async: async));

                Assert.Equal(writeEntry.Name, readEntry.Name);
                Assert.Equal(writeEntry.GroupName, readEntry.GroupName);
                Assert.Equal(writeEntry.UserName, readEntry.UserName);
                Assert.Equal(writeEntry.ModificationTime, readEntry.ModificationTime);
                Assert.Equal(writeEntry.LinkName, readEntry.LinkName);

                Assert.Equal(0, writeEntry.Length);
                Assert.Equal(0, readEntry.Length);
            }
        }

        [Theory]
        [MemberData(nameof(PaxExtendedAttributesEntryTypeAndBooleanData))]
        public async Task PaxExtendedAttributes_DoNotOverwritePublicProperties_WhenLargerThanLegacyFields(TarEntryType entryType, bool async)
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
            {
                await using TarWriterHolder writerHolder = CreateTarWriter(ms, async, TarEntryFormat.Pax, leaveOpen: true);
                TarWriter writer = writerHolder;

                await WriteEntry(writer, writeEntry, async);
            }
            ms.Position = 0;

            {
                await using TarReaderHolder readerHolder = CreateTarReader(ms, async, leaveOpen: false);
                TarReader reader = readerHolder;

                PaxTarEntry readEntry = Assert.IsType<PaxTarEntry>(await GetNextEntry(reader, async: async));
                Assert.Null(await GetNextEntry(reader, async: async));

                Assert.Equal(writeEntry.Name, readEntry.Name);
                Assert.Equal(writeEntry.GroupName, readEntry.GroupName);
                Assert.Equal(writeEntry.UserName, readEntry.UserName);
                Assert.Equal(writeEntry.ModificationTime, readEntry.ModificationTime);
                Assert.Equal(writeEntry.LinkName, readEntry.LinkName);
            }
        }
    }
}
