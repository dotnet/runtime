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
        public async Task NameRoundtripsAsync(TarEntryFormat entryFormat, TarEntryType entryType, string name)
        {
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, name);
            entry.Name = name;

            MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                await writer.WriteEntryAsync(entry);
            }

            ms.Position = 0;
            using TarReader reader = new(ms);

            entry = await reader.GetNextEntryAsync();
            Assert.Null(await reader.GetNextEntryAsync());
            Assert.Equal(name, entry.Name);
        }

        public static IEnumerable<object[]> LinkNameRoundtripsAsyncTheoryData()
            => TarWriter_WriteEntry_Roundtrip_Tests.LinkNameRoundtripsTheoryData();

        [Theory]
        [MemberData(nameof(LinkNameRoundtripsAsyncTheoryData))]
        public async Task LinkNameRoundtrips(TarEntryFormat entryFormat, TarEntryType entryType, string linkName)
        {
            string name = "foo";
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, name);
            entry.LinkName = linkName;

            MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                await writer.WriteEntryAsync(entry);
            }

            ms.Position = 0;
            using TarReader reader = new(ms);

            entry = await reader.GetNextEntryAsync();
            Assert.Null(await reader.GetNextEntryAsync());
            Assert.Equal(name, entry.Name);
            Assert.Equal(linkName, entry.LinkName);
        }

        public static IEnumerable<object[]> UserNameGroupNameRoundtripsAsyncTheoryData()
            => TarWriter_WriteEntry_Roundtrip_Tests.UserNameGroupNameRoundtripsTheoryData();

        [Theory]
        [MemberData(nameof(UserNameGroupNameRoundtripsAsyncTheoryData))]
        public async Task UserNameGroupNameRoundtrips(TarEntryFormat entryFormat, string userGroupName)
        {
            string name = "foo";
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, TarEntryType.RegularFile, name);
            PosixTarEntry posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
            posixEntry.UserName = userGroupName;
            posixEntry.GroupName = userGroupName;

            MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                await writer.WriteEntryAsync(posixEntry);
            }

            ms.Position = 0;
            using TarReader reader = new(ms);

            entry = await reader.GetNextEntryAsync();
            posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
            Assert.Null(await reader.GetNextEntryAsync());

            Assert.Equal(name, posixEntry.Name);
            Assert.Equal(userGroupName, posixEntry.UserName);
            Assert.Equal(userGroupName, posixEntry.GroupName);
        }
    }
}
