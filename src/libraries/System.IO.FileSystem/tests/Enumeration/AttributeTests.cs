// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Enumeration;
using Xunit;

namespace System.IO.Tests.Enumeration
{
    public class AttributeTests : FileSystemTest
    {
        private class FileSystemEntryProperties
        {
            public string FileName { get; init; }
            public FileAttributes Attributes { get; init; }
            public DateTimeOffset CreationTimeUtc { get; init; }
            public bool IsDirectory { get; init; }
            public bool IsHidden { get; init; }
            public DateTimeOffset LastAccessTimeUtc { get; init; }
            public DateTimeOffset LastWriteTimeUtc { get; init; }
            public long Length { get; init; }
            public string Directory { get; init; }
            public string FullPath { get; init; }
            public string SpecifiedFullPath { get; init; }
        }

        private class GetPropertiesEnumerator : FileSystemEnumerator<FileSystemEntryProperties>
        {
            public GetPropertiesEnumerator(string directory, EnumerationOptions options)
                : base(directory, options)
            { }

            protected override bool ContinueOnError(int error)
            {
                Assert.False(true, $"Should not have errored {error}");
                return false;
            }

            protected override FileSystemEntryProperties TransformEntry(ref FileSystemEntry entry)
            {
                return new FileSystemEntryProperties
                {
                    FileName = new string(entry.FileName),
                    Attributes = entry.Attributes,
                    CreationTimeUtc = entry.CreationTimeUtc,
                    IsDirectory = entry.IsDirectory,
                    IsHidden = entry.IsHidden,
                    LastAccessTimeUtc = entry.LastAccessTimeUtc,
                    LastWriteTimeUtc = entry.LastWriteTimeUtc,
                    Length = entry.Length,
                    Directory = new string(entry.Directory),
                    FullPath = entry.ToFullPath(),
                    SpecifiedFullPath = entry.ToSpecifiedFullPath()
                };
            }
        }

        // The test is performed using two items with different properties (file/dir, file length)
        // to check cached values from the previous entry don't leak into the non-existing entry.
        [InlineData("dir1", "dir2")]
        [InlineData("file1", "file2")]
        [InlineData("dir1", "file1")]
        [InlineData("file1", "dir1")]
        [Theory]
        public void PropertiesWhenItemNoLongerExists(string item1, string item2)
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());

            FileSystemInfo item1Info = CreateItem(testDirectory, item1);
            FileSystemInfo item2Info = CreateItem(testDirectory, item2);

            using (var enumerator = new GetPropertiesEnumerator(testDirectory.FullName, new EnumerationOptions() { AttributesToSkip = 0 }))
            {
                // Move to the first item.
                Assert.True(enumerator.MoveNext(), "Move first");
                FileSystemEntryProperties entry = enumerator.Current;

                Assert.True(entry.FileName == item1 || entry.FileName == item2, "Unexpected item");

                // Delete both items.
                DeleteItem(testDirectory, item1);
                DeleteItem(testDirectory, item2);

                // Move to the second item.
                FileSystemInfo expected = entry.FileName == item1 ? item2Info : item1Info;
                Assert.True(enumerator.MoveNext(), "Move second");
                entry = enumerator.Current;

                // Names and paths.
                AssertExtensions.EqualTo(expected.Name, entry.FileName, "Name");
                AssertExtensions.EqualTo(testDirectory.FullName, entry.Directory, "Directory");
                AssertExtensions.EqualTo(expected.FullName, entry.FullPath, "FullPath");
                AssertExtensions.EqualTo(expected.FullName, entry.SpecifiedFullPath, "SpecifiedFullPath");

                // Values determined during enumeration.
                if (PlatformDetection.IsBrowser)
                {
                    // For Browser, all items are typed as DT_UNKNOWN.
                    AssertExtensions.EqualTo(false, entry.IsDirectory, "IsDirectory");
                    AssertExtensions.EqualTo(entry.FileName.StartsWith('.') ? FileAttributes.Hidden : FileAttributes.Normal, entry.Attributes, "Attributes");
                }
                else
                {
                    AssertExtensions.EqualTo(expected is DirectoryInfo, entry.IsDirectory, "IsDirectory");
                    AssertExtensions.EqualTo(expected.Attributes, entry.Attributes, "Attributes");
                }

                if (PlatformDetection.IsWindows)
                {
                    AssertExtensions.EqualTo((expected.Attributes & FileAttributes.Hidden) != 0, entry.IsHidden, "IsHidden");
                    AssertExtensions.EqualTo(expected.CreationTimeUtc, entry.CreationTimeUtc, "CreationTimeUtc");
                    AssertExtensions.EqualTo(expected.LastAccessTimeUtc, entry.LastAccessTimeUtc, "LastAccessTimeUtc");
                    AssertExtensions.EqualTo(expected.LastWriteTimeUtc, entry.LastWriteTimeUtc, "LastWriteTimeUtc");
                    if (expected is FileInfo fileInfo)
                    {
                        AssertExtensions.EqualTo(fileInfo.Length, entry.Length, "Length");
                    }
                }
                else
                {
                    // On Unix, these values were not determined during enumeration.
                    // Because the file was deleted, the values can no longer be retrieved and sensible defaults are returned.
                    AssertExtensions.EqualTo(entry.FileName.StartsWith('.'), entry.IsHidden, "IsHidden");
                    DateTimeOffset defaultTime = new DateTimeOffset(DateTime.FromFileTimeUtc(0));
                    AssertExtensions.EqualTo(defaultTime, entry.CreationTimeUtc, "CreationTimeUtc");
                    AssertExtensions.EqualTo(defaultTime, entry.LastAccessTimeUtc, "LastAccessTimeUtc");
                    AssertExtensions.EqualTo(defaultTime, entry.LastWriteTimeUtc, "LastWriteTimeUtc");
                    AssertExtensions.EqualTo(0, entry.Length, "Length");
                }

                Assert.False(enumerator.MoveNext(), "Move final");
            }

            static FileSystemInfo CreateItem(DirectoryInfo testDirectory, string item)
            {
                string fullPath = Path.Combine(testDirectory.FullName, item);
                if (item.StartsWith("dir"))
                {
                    Directory.CreateDirectory(fullPath);
                    var info = new DirectoryInfo(fullPath);
                    info.Refresh();
                    return info;
                }
                else
                {
                    // use the last char to have different lengths for different files.
                    Assert.True(item == "file1" || item == "file2", "File names");
                    int length = (int)item[item.Length - 1];
                    File.WriteAllBytes(fullPath, new byte[length]);
                    var info = new FileInfo(fullPath);
                    info.Refresh();
                    return info;
                }
            }

            static void DeleteItem(DirectoryInfo testDirectory, string item)
            {
                string fullPath = Path.Combine(testDirectory.FullName, item);
                if (item.StartsWith("dir"))
                {
                    Directory.Delete(fullPath);
                }
                else
                {
                    File.Delete(fullPath);
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)]
        public void IsHiddenAttribute_Windows_OSX()
        {
            // Windows and MacOS hide a file by setting the hidden attribute
            IsHiddenAttributeInternal(useDotPrefix: false, useHiddenFlag: true);
        }


        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void IsHiddenAttribute_Unix()
        {
            // Put a period in front to make it hidden on Unix
            IsHiddenAttributeInternal(useDotPrefix: true, useHiddenFlag: false);
        }

        private void IsHiddenAttributeInternal(bool useDotPrefix, bool useHiddenFlag)
        {
            string prefix = useDotPrefix ? "." : "";

            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());

            FileInfo fileOne = new FileInfo(Path.Combine(testDirectory.FullName, GetTestFileName()));
            FileInfo fileTwo = new FileInfo(Path.Combine(testDirectory.FullName, prefix + GetTestFileName()));

            fileOne.Create().Dispose();
            fileTwo.Create().Dispose();

            if (useHiddenFlag)
            {
                fileTwo.Attributes |= FileAttributes.Hidden;
            }

            FileInfo fileCheck = new FileInfo(fileTwo.FullName);
            Assert.Equal(fileTwo.Attributes, fileCheck.Attributes);

            IEnumerable<string> enumerable = new FileSystemEnumerable<string>(
                testDirectory.FullName,
                (ref FileSystemEntry entry) => entry.ToFullPath(),
                new EnumerationOptions() { AttributesToSkip = 0 })
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => entry.IsHidden
            };

            Assert.Equal(new string[] { fileTwo.FullName }, enumerable);
        }

        [Fact]
        public void IsReadOnlyAttribute()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());

            FileInfo fileOne = new FileInfo(Path.Combine(testDirectory.FullName, GetTestFileName()));
            FileInfo fileTwo = new FileInfo(Path.Combine(testDirectory.FullName, GetTestFileName()));

            fileOne.Create().Dispose();
            fileTwo.Create().Dispose();

            fileTwo.Attributes |= FileAttributes.ReadOnly;

            IEnumerable<string> enumerable = new FileSystemEnumerable<string>(
                 testDirectory.FullName,
                 (ref FileSystemEntry entry) => entry.ToFullPath(),
                 new EnumerationOptions() { AttributesToSkip = 0 })
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => (entry.Attributes & FileAttributes.ReadOnly) != 0
            };

            Assert.Equal(new string[] { fileTwo.FullName }, enumerable);
        }
    }
}
