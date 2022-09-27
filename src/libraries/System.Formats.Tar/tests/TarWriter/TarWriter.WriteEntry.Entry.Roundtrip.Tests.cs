// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public void NameRoundtrips(TarEntryFormat entryFormat, TarEntryType entryType, bool unseekableStream, string name)
        {
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, name);
            entry.Name = name;

            MemoryStream ms = new();
            Stream s = unseekableStream ? new WrappedStream(ms, ms.CanRead, ms.CanWrite, canSeek: false) : ms;

            using (TarWriter writer = new(s, leaveOpen: true))
            {
                writer.WriteEntry(entry);
            }

            ms.Position = 0;
            using TarReader reader = new(s);

            entry = reader.GetNextEntry();
            Assert.Null(reader.GetNextEntry());
            Assert.Equal(name, entry.Name);
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
        public void LinkNameRoundtrips(TarEntryFormat entryFormat, TarEntryType entryType, bool unseekableStream, string linkName)
        {
            string name = "foo";
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, name);
            entry.LinkName = linkName;

            MemoryStream ms = new();
            Stream s = unseekableStream ? new WrappedStream(ms, ms.CanRead, ms.CanWrite, canSeek: false) : ms;

            using (TarWriter writer = new(s, leaveOpen: true))
            {
                writer.WriteEntry(entry);
            }

            ms.Position = 0;
            using TarReader reader = new(s);

            entry = reader.GetNextEntry();
            Assert.Null(reader.GetNextEntry());
            Assert.Equal(name, entry.Name);
            Assert.Equal(linkName, entry.LinkName);
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
        public void UserNameGroupNameRoundtrips(TarEntryFormat entryFormat, bool unseekableStream, string userGroupName)
        {
            string name = "foo";
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, TarEntryType.RegularFile, name);
            PosixTarEntry posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
            posixEntry.UserName = userGroupName;
            posixEntry.GroupName = userGroupName;

            MemoryStream ms = new();
            Stream s = unseekableStream ? new WrappedStream(ms, ms.CanRead, ms.CanWrite, canSeek: false) : ms;

            using (TarWriter writer = new(s, leaveOpen: true))
            {
                writer.WriteEntry(posixEntry);
            }

            ms.Position = 0;
            using TarReader reader = new(s);

            entry = reader.GetNextEntry();
            posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
            Assert.Null(reader.GetNextEntry());

            Assert.Equal(name, posixEntry.Name);
            Assert.Equal(userGroupName, posixEntry.UserName);
            Assert.Equal(userGroupName, posixEntry.GroupName);
        }
    }
}
