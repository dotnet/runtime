// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class PaxTarEntry_ExtendedAttributes_Sync_Tests : TarTestsBase
    {
        [Fact]
        public void ModificationTime_Setter_ShouldUpdateExtendedAttributes()
        {
            // Create an entry with extended attributes including mtime
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "mtime", "1234567890.0" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes);

            // Verify initial state
            Assert.True(entry.ExtendedAttributes.ContainsKey("mtime"));
            Assert.Equal("1234567890.0", entry.ExtendedAttributes["mtime"]);

            // Set ModificationTime property
            DateTimeOffset newModificationTime = DateTimeOffset.FromUnixTimeSeconds(9876543210);
            entry.ModificationTime = newModificationTime;

            // Verify ExtendedAttributes is synchronized immediately
            Assert.True(entry.ExtendedAttributes.ContainsKey("mtime"));
            string expectedMtimeString = "9876543210";
            Assert.Equal(expectedMtimeString, entry.ExtendedAttributes["mtime"]);
        }

        [Fact]
        public void Name_Setter_ShouldUpdateExtendedAttributes()
        {
            // Create an entry with extended attributes including path
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "path", "oldname.txt" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "oldname.txt", extendedAttributes);

            // Verify initial state
            Assert.True(entry.ExtendedAttributes.ContainsKey("path"));
            Assert.Equal("oldname.txt", entry.ExtendedAttributes["path"]);

            // Set Name property
            entry.Name = "newname.txt";

            // Verify ExtendedAttributes is synchronized immediately
            Assert.True(entry.ExtendedAttributes.ContainsKey("path"));
            Assert.Equal("newname.txt", entry.ExtendedAttributes["path"]);
        }

        [Fact]
        public void UserName_Setter_ShouldUpdateExtendedAttributes()
        {
            // Create an entry with extended attributes including uname
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "uname", "olduser" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes);

            // Verify initial state
            Assert.True(entry.ExtendedAttributes.ContainsKey("uname"));
            Assert.Equal("olduser", entry.ExtendedAttributes["uname"]);

            // Set UserName property
            entry.UserName = "newuser";

            // Verify ExtendedAttributes is synchronized immediately
            Assert.True(entry.ExtendedAttributes.ContainsKey("uname"));
            Assert.Equal("newuser", entry.ExtendedAttributes["uname"]);
        }

        [Fact]
        public void GroupName_Setter_ShouldUpdateExtendedAttributes()
        {
            // Create an entry with extended attributes including gname
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "gname", "oldgroup" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes);

            // Verify initial state
            Assert.True(entry.ExtendedAttributes.ContainsKey("gname"));
            Assert.Equal("oldgroup", entry.ExtendedAttributes["gname"]);

            // Set GroupName property
            entry.GroupName = "newgroup";

            // Verify ExtendedAttributes is synchronized immediately
            Assert.True(entry.ExtendedAttributes.ContainsKey("gname"));
            Assert.Equal("newgroup", entry.ExtendedAttributes["gname"]);
        }

        [Fact]
        public void LinkName_Setter_ShouldUpdateExtendedAttributes()
        {
            // Create an entry with extended attributes including linkpath
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "linkpath", "oldlink" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.SymbolicLink, "test.txt", extendedAttributes);
            entry.LinkName = "oldlink";

            // Verify initial state
            Assert.True(entry.ExtendedAttributes.ContainsKey("linkpath"));
            Assert.Equal("oldlink", entry.ExtendedAttributes["linkpath"]);

            // Set LinkName property
            entry.LinkName = "newlink";

            // Verify ExtendedAttributes is synchronized immediately
            Assert.True(entry.ExtendedAttributes.ContainsKey("linkpath"));
            Assert.Equal("newlink", entry.ExtendedAttributes["linkpath"]);
        }

        [Fact]
        public void Uid_Setter_WithLargeValue_ShouldAddToExtendedAttributes()
        {
            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt");

            // Access ExtendedAttributes to initialize it
            int initialCount = entry.ExtendedAttributes.Count;

            // Set Uid to a value larger than max octal (2097151)
            entry.Uid = 3000000;

            // Verify ExtendedAttributes contains uid
            Assert.True(entry.ExtendedAttributes.ContainsKey("uid"));
            Assert.Equal("3000000", entry.ExtendedAttributes["uid"]);
        }

        [Fact]
        public void Uid_Setter_WithSmallValue_ShouldRemoveFromExtendedAttributes()
        {
            // Create an entry with extended attributes including uid
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "uid", "3000000" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes);

            // Verify initial state
            Assert.True(entry.ExtendedAttributes.ContainsKey("uid"));

            // Set Uid to a value smaller than max octal (2097151)
            entry.Uid = 1000;

            // Verify ExtendedAttributes no longer contains uid (it fits in standard field)
            Assert.False(entry.ExtendedAttributes.ContainsKey("uid"));
        }

        [Fact]
        public void Gid_Setter_WithLargeValue_ShouldAddToExtendedAttributes()
        {
            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt");

            // Access ExtendedAttributes to initialize it
            int initialCount = entry.ExtendedAttributes.Count;

            // Set Gid to a value larger than max octal (2097151)
            entry.Gid = 3000000;

            // Verify ExtendedAttributes contains gid
            Assert.True(entry.ExtendedAttributes.ContainsKey("gid"));
            Assert.Equal("3000000", entry.ExtendedAttributes["gid"]);
        }

        [Fact]
        public void Gid_Setter_WithSmallValue_ShouldRemoveFromExtendedAttributes()
        {
            // Create an entry with extended attributes including gid
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "gid", "3000000" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes);

            // Verify initial state
            Assert.True(entry.ExtendedAttributes.ContainsKey("gid"));

            // Set Gid to a value smaller than max octal (2097151)
            entry.Gid = 1000;

            // Verify ExtendedAttributes no longer contains gid (it fits in standard field)
            Assert.False(entry.ExtendedAttributes.ContainsKey("gid"));
        }
    }
}
