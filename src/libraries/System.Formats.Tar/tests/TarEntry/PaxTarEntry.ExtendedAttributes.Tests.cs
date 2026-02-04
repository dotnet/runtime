// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class PaxTarEntry_ExtendedAttributes_Tests : TarTestsBase
    {
        [Fact]
        public void ModificationTime_ShouldSyncWithExtendedAttributes()
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
        public void Name_ShouldSyncWithExtendedAttributes()
        {
            // Create an entry with extended attributes including path
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "path", "oldname.txt" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "oldname.txt", extendedAttributes);

            // Set Name property
            entry.Name = "newname.txt";

            // Write the entry and verify the path is from the property
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
                Assert.Equal("newname.txt", readEntry.Name);
            }
        }

        [Fact]
        public void UserName_ShouldSyncWithExtendedAttributes()
        {
            // Create an entry with extended attributes including uname
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "uname", "olduser" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes);

            // Set UserName property
            entry.UserName = "newuser";

            // Write the entry and verify the uname is from the property
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
                Assert.Equal("newuser", readEntry.UserName);
            }
        }

        [Fact]
        public void GroupName_ShouldSyncWithExtendedAttributes()
        {
            // Create an entry with extended attributes including gname
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "gname", "oldgroup" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes);

            // Set GroupName property
            entry.GroupName = "newgroup";

            // Write the entry and verify the gname is from the property
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
                Assert.Equal("newgroup", readEntry.GroupName);
            }
        }

        [Fact]
        public void LinkName_ShouldSyncWithExtendedAttributes()
        {
            // Create an entry with extended attributes including linkpath
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "linkpath", "oldlink" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.SymbolicLink, "test.txt", extendedAttributes);
            entry.LinkName = "oldlink";

            // Set LinkName property
            entry.LinkName = "newlink";

            // Write the entry and verify the linkpath is from the property
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
                Assert.Equal("newlink", readEntry.LinkName);
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
        public void Uid_ShouldSyncWithExtendedAttributes()
        {
            // Create an entry with extended attributes including uid
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "uid", "1000" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes);

            // Set Uid property
            entry.Uid = 2000;

            // Write the entry and verify the uid is from the property
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
                Assert.Equal(2000, readEntry.Uid);
            }
        }

        [Fact]
        public void Gid_ShouldSyncWithExtendedAttributes()
        {
            // Create an entry with extended attributes including gid
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "gid", "1000" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes);

            // Set Gid property
            entry.Gid = 2000;

            // Write the entry and verify the gid is from the property
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
                Assert.Equal(2000, readEntry.Gid);
            }
        }

        [Fact]
        public void DeviceMajor_ShouldSyncWithExtendedAttributes()
        {
            // Create an entry with extended attributes including devmajor
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "devmajor", "10" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.BlockDevice, "test", extendedAttributes);

            // Set DeviceMajor property
            entry.DeviceMajor = 20;

            // Write the entry and verify the devmajor is from the property
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
                Assert.Equal(20, readEntry.DeviceMajor);
            }
        }

        [Fact]
        public void DeviceMinor_ShouldSyncWithExtendedAttributes()
        {
            // Create an entry with extended attributes including devminor
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "devminor", "10" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.BlockDevice, "test", extendedAttributes);

            // Set DeviceMinor property
            entry.DeviceMinor = 20;

            // Write the entry and verify the devminor is from the property
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
                Assert.Equal(20, readEntry.DeviceMinor);
            }
        }
    }
}
