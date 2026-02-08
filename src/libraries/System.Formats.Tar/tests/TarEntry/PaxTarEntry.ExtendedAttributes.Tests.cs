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
        [InlineData("uid", 3000000, true)]
        [InlineData("uid", 1000, false)]
        [InlineData("gid", 3000000, true)]
        [InlineData("gid", 1000, false)]
        public void NumericProperty_Setter_ShouldSyncWithExtendedAttributes(string extAttrKey, int numericValue, bool shouldBeInExtendedAttributes)
        {
            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt");

            // Access ExtendedAttributes to force initialization of the backing dictionary
            _ = entry.ExtendedAttributes.Count;

            if (extAttrKey == "uid")
                entry.Uid = numericValue;
            else
                entry.Gid = numericValue;

            if (shouldBeInExtendedAttributes)
            {
                Assert.True(entry.ExtendedAttributes.ContainsKey(extAttrKey));
                Assert.Equal(numericValue.ToString(), entry.ExtendedAttributes[extAttrKey]);
            }
            else
            {
                Assert.False(entry.ExtendedAttributes.ContainsKey(extAttrKey));
            }
        }

        [Theory]
        [InlineData("uid")]
        [InlineData("gid")]
        public void NumericProperty_Setter_WithSmallValue_ShouldRemoveFromExtendedAttributes(string extAttrKey)
        {
            var extendedAttributes = new Dictionary<string, string>
            {
                { extAttrKey, "3000000" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes);
            Assert.True(entry.ExtendedAttributes.ContainsKey(extAttrKey));

            if (extAttrKey == "uid")
                entry.Uid = 1000;
            else
                entry.Gid = 1000;

            Assert.False(entry.ExtendedAttributes.ContainsKey(extAttrKey));
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
        public void Constructor_WithConflictingPathExtendedAttribute_PropertyTakesPrecedence()
        {
            // Extended attribute path differs from entryName parameter
            // When written, the property value (entryName) should take precedence
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "path", "different.txt" }
            };

            // This should not throw - extended attributes can differ from properties
            // The synchronization mechanism ensures properties take precedence when writing
            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes);
            
            // The extended attribute was added as-is initially
            Assert.True(entry.ExtendedAttributes.ContainsKey("path"));
            Assert.Equal("different.txt", entry.ExtendedAttributes["path"]);
            
            // Write and read back to verify property takes precedence
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: true))
            {
                writer.WriteEntry(entry);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                PaxTarEntry readEntry = reader.GetNextEntry() as PaxTarEntry;
                Assert.NotNull(readEntry);
                // The name from the property (test.txt) should be used, not the extended attribute
                Assert.Equal("test.txt", readEntry.Name);
            }
        }

        [Fact]
        public void Constructor_WithMatchingExtendedAttributes_ShouldSucceed()
        {
            // Extended attributes that match entryName should work fine
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "path", "test.txt" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes);
            
            Assert.True(entry.ExtendedAttributes.ContainsKey("path"));
            Assert.Equal("test.txt", entry.ExtendedAttributes["path"]);
        }
    }
}
