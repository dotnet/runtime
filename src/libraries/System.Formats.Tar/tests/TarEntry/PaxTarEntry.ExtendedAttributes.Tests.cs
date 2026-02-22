// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
            // Create an entry with a small value that fits in the regular header field
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>()
            {
                [extAttrKey] = smallValue.ToString()
            };

            PaxTarEntry entry = new PaxTarEntry(entryType, "test.txt", extendedAttributes);

            // Verify initial state, since the value fits in the regular header field, it should not be present in ExtendedAttributes
            Assert.Equal(smallValue, accessors.getter(entry));
            Assert.False(entry.ExtendedAttributes.ContainsKey(extAttrKey));

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

            // Set property value
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
        public void Constructor_WithConflictingPathExtendedAttribute_ShouldThrowArgumentException()
        {
            // Extended attribute path differs from entryName parameter
            Dictionary<string, string> extendedAttributes = new Dictionary<string, string>
            {
                { "path", "different.txt" }
            };

            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.RegularFile, "test.txt", extendedAttributes));
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
