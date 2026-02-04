// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class PaxTarEntry_ExtendedAttributes_Tests : TarTestsBase
    {
        public static IEnumerable<object[]> StringPropertySyncTestData()
        {
            yield return new object[] { "path", "Name", "oldname.txt", "newname.txt", TarEntryType.RegularFile };
            yield return new object[] { "uname", "UserName", "olduser", "newuser", TarEntryType.RegularFile };
            yield return new object[] { "gname", "GroupName", "oldgroup", "newgroup", TarEntryType.RegularFile };
            yield return new object[] { "linkpath", "LinkName", "oldlink", "newlink", TarEntryType.SymbolicLink };
        }

        [Theory]
        [MemberData(nameof(StringPropertySyncTestData))]
        public void StringProperty_Setter_ShouldUpdateExtendedAttributes(string extAttrKey, string propertyName, string oldValue, string newValue, TarEntryType entryType)
        {
            // Create an entry with extended attributes
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { extAttrKey, oldValue }
            };

            PaxTarEntry entry = new PaxTarEntry(entryType, "test.txt", extendedAttributes);
            
            // Set LinkName initial value if needed
            if (propertyName == "LinkName")
            {
                entry.LinkName = oldValue;
            }

            // Verify initial state
            Assert.True(entry.ExtendedAttributes.ContainsKey(extAttrKey));
            Assert.Equal(oldValue, entry.ExtendedAttributes[extAttrKey]);

            // Set property value
            var property = typeof(PaxTarEntry).GetProperty(propertyName);
            Assert.NotNull(property);
            property.SetValue(entry, newValue);

            // Verify ExtendedAttributes is synchronized immediately
            Assert.True(entry.ExtendedAttributes.ContainsKey(extAttrKey));
            Assert.Equal(newValue, entry.ExtendedAttributes[extAttrKey]);
        }

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

        [Theory]
        [InlineData(3000000, true)]  // Large value should add to ExtendedAttributes
        [InlineData(1000, false)]     // Small value should not be in ExtendedAttributes
        public void Uid_Setter_ShouldSyncWithExtendedAttributes(int uidValue, bool shouldBeInExtendedAttributes)
        {
            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt");

            // Access ExtendedAttributes to initialize it
            _ = entry.ExtendedAttributes.Count;

            // Set Uid
            entry.Uid = uidValue;

            // Verify ExtendedAttributes state
            if (shouldBeInExtendedAttributes)
            {
                Assert.True(entry.ExtendedAttributes.ContainsKey("uid"));
                Assert.Equal(uidValue.ToString(), entry.ExtendedAttributes["uid"]);
            }
            else
            {
                Assert.False(entry.ExtendedAttributes.ContainsKey("uid"));
            }
        }

        [Theory]
        [InlineData(3000000, true)]  // Large value should add to ExtendedAttributes
        [InlineData(1000, false)]     // Small value should not be in ExtendedAttributes
        public void Gid_Setter_ShouldSyncWithExtendedAttributes(int gidValue, bool shouldBeInExtendedAttributes)
        {
            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt");

            // Access ExtendedAttributes to initialize it
            _ = entry.ExtendedAttributes.Count;

            // Set Gid
            entry.Gid = gidValue;

            // Verify ExtendedAttributes state
            if (shouldBeInExtendedAttributes)
            {
                Assert.True(entry.ExtendedAttributes.ContainsKey("gid"));
                Assert.Equal(gidValue.ToString(), entry.ExtendedAttributes["gid"]);
            }
            else
            {
                Assert.False(entry.ExtendedAttributes.ContainsKey("gid"));
            }
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

        [Fact]
        public void ModificationTime_ShouldSyncWithExtendedAttributes_Roundtrip()
        {
            // Create an entry with extended attributes including mtime
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "mtime", "1234567890.0" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes);

            // Set ModificationTime property
            DateTimeOffset newModificationTime = DateTimeOffset.FromUnixTimeSeconds(9876543210);
            entry.ModificationTime = newModificationTime;

            // Write the entry and verify the mtime is from the property, not the old extended attributes
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: true))
            {
                writer.WriteEntry(entry);
            }

            // Read back and verify
            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry readEntry = reader.GetNextEntry() as PaxTarEntry;
                Assert.NotNull(readEntry);
                Assert.Equal(newModificationTime, readEntry.ModificationTime);
            }
        }

        [Fact]
        public void CopyConstructor_ModificationTime_ShouldOverrideExtendedAttributes()
        {
            // This is the scenario from the GitHub issue
            // Create an entry with extended attributes
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "mtime", "1234567890.0" }
            };

            PaxTarEntry originalEntry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes);

            // Use copy constructor
            PaxTarEntry newEntry = new PaxTarEntry(originalEntry);

            // Set ModificationTime on the new entry
            DateTimeOffset newModificationTime = DateTimeOffset.FromUnixTimeSeconds(9876543210);
            newEntry.ModificationTime = newModificationTime;

            // Write the entry and verify ModificationTime is used, not the old mtime from ExtendedAttributes
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: true))
            {
                writer.WriteEntry(newEntry);
            }

            // Read back and verify
            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry readEntry = reader.GetNextEntry() as PaxTarEntry;
                Assert.NotNull(readEntry);
                Assert.Equal(newModificationTime, readEntry.ModificationTime);
            }
        }

        [Fact]
        public void Constructor_WithConflictingPathExtendedAttribute_ShouldThrow()
        {
            // Extended attribute path conflicts with entryName parameter
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "path", "different.txt" }
            };

            ArgumentException ex = Assert.Throws<ArgumentException>(() =>
                new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes));
            
            Assert.Contains("path", ex.Message);
            Assert.Contains("different.txt", ex.Message);
            Assert.Contains("test.txt", ex.Message);
        }

        [Fact]
        public void Constructor_WithMatchingExtendedAttributes_ShouldSucceed()
        {
            // Extended attributes that match entryName should not throw
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "path", "test.txt" }
            };

            // This should not throw because path matches entryName
            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes);
            
            Assert.True(entry.ExtendedAttributes.ContainsKey("path"));
            Assert.Equal("test.txt", entry.ExtendedAttributes["path"]);
        }
    }
}
