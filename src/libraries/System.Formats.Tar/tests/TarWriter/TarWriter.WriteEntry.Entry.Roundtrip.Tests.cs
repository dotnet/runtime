// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarWriter_WriteEntry_Roundtrip_Tests : TarTestsBase
    {
        public static IEnumerable<object[]> NameRoundtripsTheoryData()
        {
            foreach (TarEntryType entryType in new[] { TarEntryType.RegularFile, TarEntryType.Directory })
            {
                TarEntryType v7EntryType = entryType is TarEntryType.RegularFile ? TarEntryType.V7RegularFile : entryType;
                foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.Name).Concat(GetNamesPrefixedTestData(NameCapabilities.Name)))
                {
                    yield return new object[] { TarEntryFormat.V7, v7EntryType, name };
                }

                // TODO: Use NameCapabilities.NameAndPrefix once https://github.com/dotnet/runtime/issues/75360 is fixed.
                foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.Name).Concat(GetNamesPrefixedTestData(NameCapabilities.Name)))
                {
                    yield return new object[] { TarEntryFormat.Ustar, entryType, name };
                }

                foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.Unlimited).Concat(GetNamesPrefixedTestData(NameCapabilities.Unlimited)))
                {
                    yield return new object[] { TarEntryFormat.Pax, entryType, name };
                    yield return new object[] { TarEntryFormat.Gnu, entryType, name };
                }
            }
        }

        [Theory]
        [MemberData(nameof(NameRoundtripsTheoryData))]
        public void NameRoundtrips(TarEntryFormat entryFormat, TarEntryType entryType, string name)
        {
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, name);
            entry.Name = name;

            MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                writer.WriteEntry(entry);
            }

            ms.Position = 0;
            using TarReader reader = new(ms);

            entry = reader.GetNextEntry();
            Assert.Null(reader.GetNextEntry());
            Assert.Equal(name, entry.Name);
        }

        public static IEnumerable<object[]> LinkNameRoundtripsTheoryData()
        {
            foreach (TarEntryType entryType in new[] { TarEntryType.SymbolicLink, TarEntryType.HardLink })
            {
                foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.Name).Concat(GetNamesPrefixedTestData(NameCapabilities.Name)))
                {
                    yield return new object[] { TarEntryFormat.V7, entryType, name };
                    // TODO: Use NameCapabilities.NameAndPrefix once https://github.com/dotnet/runtime/issues/75360 is fixed.
                    yield return new object[] { TarEntryFormat.Ustar, entryType, name };
                }

                foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.Unlimited).Concat(GetNamesPrefixedTestData(NameCapabilities.Unlimited)))
                {
                    yield return new object[] { TarEntryFormat.Pax, entryType, name };
                    yield return new object[] { TarEntryFormat.Gnu, entryType, name };
                }
            }
        }

        [Theory]
        [MemberData(nameof(LinkNameRoundtripsTheoryData))]
        public void LinkNameRoundtrips(TarEntryFormat entryFormat, TarEntryType entryType, string linkName)
        {
            string name = "foo";
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, name);
            entry.LinkName = linkName;

            MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                writer.WriteEntry(entry);
            }

            ms.Position = 0;
            using TarReader reader = new(ms);

            entry = reader.GetNextEntry();
            Assert.Null(reader.GetNextEntry());
            Assert.Equal(name, entry.Name);
            Assert.Equal(linkName, entry.LinkName);
        }


        public static IEnumerable<object[]> UserNameGroupNameRoundtripsTheoryData()
        {
            foreach (TarEntryFormat entryFormat in new[] { TarEntryFormat.Ustar, TarEntryFormat.Pax, TarEntryFormat.Gnu })
            {
                yield return new object[] { entryFormat, Repeat(OneByteCharacter, 32) };
                yield return new object[] { entryFormat, Repeat(TwoBytesCharacter, 32 / 2) };
                yield return new object[] { entryFormat, Repeat(FourBytesCharacter, 32 / 4) };
            }
        }

        [Theory]
        [MemberData(nameof(UserNameGroupNameRoundtripsTheoryData))]
        public void UserNameGroupNameRoundtrips(TarEntryFormat entryFormat, string userGroupName)
        {
            string name = "foo";
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, TarEntryType.RegularFile, name);
            PosixTarEntry posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
            posixEntry.UserName = userGroupName;
            posixEntry.GroupName = userGroupName;

            MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                writer.WriteEntry(posixEntry);
            }

            ms.Position = 0;
            using TarReader reader = new(ms);

            entry = reader.GetNextEntry();
            posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
            Assert.Null(reader.GetNextEntry());

            Assert.Equal(name, posixEntry.Name);
            Assert.Equal(userGroupName, posixEntry.UserName);
            Assert.Equal(userGroupName, posixEntry.GroupName);
        }
    }
}
