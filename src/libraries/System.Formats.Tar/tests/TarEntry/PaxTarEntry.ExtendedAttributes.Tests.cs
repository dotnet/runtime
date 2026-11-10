// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class PaxTarEntry_ExtendedAttributes_Tests : TarTestsBase
    {
        // helper to create getters and setters for test data
        static (Func<PaxTarEntry, object> getter, Action<PaxTarEntry, object> setter) Accessors<T>(Func<PaxTarEntry, T> getter, Action<PaxTarEntry, T> setter) =>
            (entry => getter(entry), (entry, value) => setter(entry, (T)value));

        public static IEnumerable<object[]> PropertyUpdateData()
        {
            yield return new object[] { "gname", "OldGroup", new string('a', 33), TarEntryType.RegularFile, Accessors(e => e.GroupName, (e, v) => e.GroupName = v) };
            yield return new object[] { "uname", "OldUser", new string('a', 33), TarEntryType.RegularFile, Accessors(e => e.UserName, (e, v) => e.UserName = v) };
            yield return new object[] { "uid", 10, 3000000, TarEntryType.RegularFile, Accessors(e => e.Uid, (e, v) => e.Uid = v) };
            yield return new object[] { "gid", 10, 3000000, TarEntryType.RegularFile, Accessors(e => e.Gid, (e, v) => e.Gid = v) };
            yield return new object[] { "devmajor", 10, 3000000, TarEntryType.BlockDevice, Accessors(e => e.DeviceMajor, (e, v) => e.DeviceMajor = v) };
            yield return new object[] { "devminor", 10, 3000000, TarEntryType.BlockDevice, Accessors(e => e.DeviceMinor, (e, v) => e.DeviceMinor = v) };
        }

        [Theory]
        [MemberData(nameof(PropertyUpdateData))]
        public void Property_Setter_ShouldUpdateExtendedAttributes(string extAttrKey, object smallValue, object largeValue, TarEntryType entryType, (Func<PaxTarEntry, object> getter, Action<PaxTarEntry, object> setter) accessors)
        {
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>()
            {
                [extAttrKey] = smallValue.ToString()
            };

            PaxTarEntry entry = new PaxTarEntry(entryType, "test.txt", extendedAttributes);

            // Small value fits in the regular header field, but EA is still preserved from construction
            Assert.Equal(smallValue, accessors.getter(entry));
            Assert.True(entry.ExtendedAttributes.ContainsKey(extAttrKey));
            Assert.Equal(smallValue.ToString(), entry.ExtendedAttributes[extAttrKey]);

            // Set property value to the larger value that requires using ExtendedAttributes
            accessors.setter(entry, largeValue);
            Assert.Equal(largeValue, accessors.getter(entry));
            Assert.True(entry.ExtendedAttributes.ContainsKey(extAttrKey));
            Assert.Equal(largeValue.ToString(), entry.ExtendedAttributes[extAttrKey]);

            // Set property value back to the smaller value that fits in the regular header field, which should remove it from ExtendedAttributes
            accessors.setter(entry, smallValue);
            Assert.Equal(smallValue, accessors.getter(entry));
            Assert.False(entry.ExtendedAttributes.ContainsKey(extAttrKey));
        }

        public static IEnumerable<object[]> PropertySetData()
        {
            yield return new object[] { "linkpath", "./newpath", "./newpath", TarEntryType.SymbolicLink, Accessors(e => e.LinkName, (e, v) => e.LinkName = v) };
            yield return new object[] { "mtime", DateTimeOffset.FromUnixTimeSeconds(9876543210), "9876543210", TarEntryType.RegularFile, Accessors(e => e.ModificationTime, (e, v) => e.ModificationTime = v) };
        }

        [Theory]
        [MemberData(nameof(PropertySetData))]
        public void PersistentProperty_ShouldUpdateWhenPropertyIsSet(string extAttrKey, object value, string valueAsString, TarEntryType entryType, (Func<PaxTarEntry, object> getter, Action<PaxTarEntry, object> setter) accessors)
        {
            PaxTarEntry entry = new PaxTarEntry(entryType, "./test.txt");

            accessors.setter(entry, value);
            Assert.Equal(value, accessors.getter(entry));

            Assert.True(entry.ExtendedAttributes.ContainsKey(extAttrKey));
            Assert.Equal(valueAsString, entry.ExtendedAttributes[extAttrKey]);

            entry = new PaxTarEntry(entryType, "./test.txt", new Dictionary<string, string>()
            {
                [extAttrKey] = valueAsString
            });

            Assert.True(entry.ExtendedAttributes.ContainsKey(extAttrKey));
            Assert.Equal(valueAsString, entry.ExtendedAttributes[extAttrKey]);
        }

        [Fact]
        public void Constructor_WithConflictingPathExtendedAttribute_ShouldUseEntryName()
        {
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "path", "different.txt" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes);

            Assert.Equal("test.txt", entry.Name);
            Assert.True(entry.ExtendedAttributes.ContainsKey("path"));
            Assert.Equal("test.txt", entry.ExtendedAttributes["path"]);
        }

        [Fact]
        public void Constructor_WithMatchingExtendedAttributes_ShouldSucceed()
        {
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "path", "test.txt" }
            };

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes);

            Assert.True(entry.ExtendedAttributes.ContainsKey("path"));
            Assert.Equal("test.txt", entry.ExtendedAttributes["path"]);
        }

        [Fact]
        public void SyncAfterRead_ChangeProperty_ExtendedAttributeReflectsNewValue()
        {
            MemoryStream ms = new();
            using (TarWriter writer = new(ms, TarEntryFormat.Pax, leaveOpen: true))
            {
                PaxTarEntry writeEntry = new PaxTarEntry(TarEntryType.RegularFile, "file.txt");
                writeEntry.Uid = 1000;
                writer.WriteEntry(writeEntry);
            }

            ms.Position = 0;
            using TarReader reader = new(ms);
            PaxTarEntry readEntry = Assert.IsType<PaxTarEntry>(reader.GetNextEntry());

            Assert.Equal(1000, readEntry.Uid);

            // Change property after reading
            readEntry.Uid = 5000000;
            Assert.Equal(5000000, readEntry.Uid);
            Assert.True(readEntry.ExtendedAttributes.ContainsKey(PaxEaUid));
            Assert.Equal("5000000", readEntry.ExtendedAttributes[PaxEaUid]);

            // Change name after reading
            readEntry.Name = "renamed.txt";
            Assert.Equal("renamed.txt", readEntry.Name);
            Assert.True(readEntry.ExtendedAttributes.ContainsKey(PaxEaName));
            Assert.Equal("renamed.txt", readEntry.ExtendedAttributes[PaxEaName]);
        }

        // Helper to build a raw PAX archive MemoryStream from EA data and header fields.
        // Provides a seekable MemoryStream positioned at 0, ready for TarReader.
        private static MemoryStream BuildRawPaxArchiveStream(
            string headerName,
            Dictionary<string, string> extraEAs = null,
            int uid = 0, int gid = 0,
            long mtime = 1700000000,
            bool includePathInEA = true)
        {
            var ms = new MemoryStream();
            byte[] eaData;
            if (includePathInEA)
            {
                eaData = BuildRawPaxExtendedAttributeData(headerName, extraEAs);
            }
            else
            {
                var sb = new System.Text.StringBuilder();
                if (extraEAs is not null)
                {
                    foreach (var kvp in extraEAs)
                    {
                        AppendRawPaxExtendedAttributeRecord(sb, kvp.Key, kvp.Value);
                    }
                }
                eaData = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            }

            WriteRawTarHeader(ms, "PaxHeaders.0/entry", 0, 0, 0, eaData.Length, 0, 'x', "");
            ms.Write(eaData);
            PadToTarBlockBoundary(ms);

            WriteRawTarHeader(ms, headerName, Convert.ToInt32("644", 8), uid, gid, 0, mtime, '0', "");
            PadToTarBlockBoundary(ms);

            ms.Write(new byte[1024]);
            ms.Position = 0;
            return ms;
        }

        [Fact]
        public void EAPreservationOnRead_StandardFieldEAs_StillVisibleInExtendedAttributes()
        {
            var extraEAs = new Dictionary<string, string>
            {
                [PaxEaUid] = "1000",
                [PaxEaGid] = "2000",
                [PaxEaUName] = "testuser",
                [PaxEaGName] = "testgroup",
            };
            using MemoryStream ms = BuildRawPaxArchiveStream("file.txt", extraEAs, uid: 1000, gid: 2000);
            using TarReader reader = new(ms);
            PaxTarEntry readEntry = Assert.IsType<PaxTarEntry>(reader.GetNextEntry());

            Assert.Equal(1000, readEntry.Uid);
            Assert.Equal(2000, readEntry.Gid);
            Assert.Equal("testuser", readEntry.UserName);
            Assert.Equal("testgroup", readEntry.GroupName);

            Assert.True(readEntry.ExtendedAttributes.ContainsKey(PaxEaUid));
            Assert.Equal("1000", readEntry.ExtendedAttributes[PaxEaUid]);
            Assert.True(readEntry.ExtendedAttributes.ContainsKey(PaxEaGid));
            Assert.Equal("2000", readEntry.ExtendedAttributes[PaxEaGid]);
            Assert.True(readEntry.ExtendedAttributes.ContainsKey(PaxEaUName));
            Assert.Equal("testuser", readEntry.ExtendedAttributes[PaxEaUName]);
            Assert.True(readEntry.ExtendedAttributes.ContainsKey(PaxEaGName));
            Assert.Equal("testgroup", readEntry.ExtendedAttributes[PaxEaGName]);
        }

        [Fact]
        public void CustomEA_Roundtrip_SurvivesWriteAndRead()
        {
            const string customKey = "MSWINDOWS.rawsd";
            const string customValue = "AQAAgBQAAAAkAAA";

            MemoryStream ms = new();
            using (TarWriter writer = new(ms, TarEntryFormat.Pax, leaveOpen: true))
            {
                PaxTarEntry writeEntry = new PaxTarEntry(TarEntryType.RegularFile, "file.txt",
                    new Dictionary<string, string>
                    {
                        { customKey, customValue }
                    });
                writer.WriteEntry(writeEntry);
            }

            ms.Position = 0;
            using TarReader reader = new(ms);
            PaxTarEntry readEntry = Assert.IsType<PaxTarEntry>(reader.GetNextEntry());

            Assert.True(readEntry.ExtendedAttributes.ContainsKey(customKey));
            Assert.Equal(customValue, readEntry.ExtendedAttributes[customKey]);
        }

        [Fact]
        public void BadArchive_MtimeDisagreement_EAWins()
        {
            long headerMtime = 1700000000;
            string eaMtime = "9876543210.123";

            using MemoryStream ms = BuildRawPaxArchiveStream("file.txt",
                extraEAs: new Dictionary<string, string> { ["mtime"] = eaMtime },
                mtime: headerMtime);
            using TarReader reader = new(ms);
            PaxTarEntry entry = Assert.IsType<PaxTarEntry>(reader.GetNextEntry());

            // EA mtime should take precedence over header mtime
            Assert.NotEqual(DateTimeOffset.FromUnixTimeSeconds(headerMtime), entry.ModificationTime);
            Assert.True(entry.ExtendedAttributes.ContainsKey(PaxEaMTime));
            Assert.Equal(eaMtime, entry.ExtendedAttributes[PaxEaMTime]);
        }

        [Fact]
        public void BadArchive_UidGidDisagreement_EAWins()
        {
            var extraEAs = new Dictionary<string, string>
            {
                ["uid"] = "1000",
                ["gid"] = "2000"
            };
            // Header has uid=500, gid=600 (different from EA values)
            using MemoryStream ms = BuildRawPaxArchiveStream("file.txt", extraEAs, uid: 500, gid: 600);
            using TarReader reader = new(ms);
            PaxTarEntry entry = Assert.IsType<PaxTarEntry>(reader.GetNextEntry());

            // EA values should take precedence over header values
            Assert.Equal(1000, entry.Uid);
            Assert.Equal(2000, entry.Gid);
            Assert.True(entry.ExtendedAttributes.ContainsKey(PaxEaUid));
            Assert.Equal("1000", entry.ExtendedAttributes[PaxEaUid]);
            Assert.True(entry.ExtendedAttributes.ContainsKey(PaxEaGid));
            Assert.Equal("2000", entry.ExtendedAttributes[PaxEaGid]);
        }

        [Fact]
        public void BadArchive_MissingEAPath_HeaderNameIsUsed()
        {
            using MemoryStream ms = BuildRawPaxArchiveStream("fallback-name.txt",
                extraEAs: new Dictionary<string, string> { ["mtime"] = "1700000000" },
                includePathInEA: false);
            using TarReader reader = new(ms);
            PaxTarEntry entry = Assert.IsType<PaxTarEntry>(reader.GetNextEntry());

            // Header name should be used when EA has no "path"
            Assert.Equal("fallback-name.txt", entry.Name);
        }

        [Fact]
        public void BadArchive_MalformedNumericEA_Throws()
        {
            using MemoryStream ms = BuildRawPaxArchiveStream("file.txt",
                extraEAs: new Dictionary<string, string> { ["uid"] = "notanumber" });
            using TarReader reader = new(ms);
            Assert.Throws<FormatException>(() => reader.GetNextEntry());
        }
    }
}
