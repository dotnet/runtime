// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Enumeration;
using Xunit;

namespace System.IO.Tests.Enumeration
{
    public class AttributeTests : FileSystemTest
    {
        private class DefaultFileAttributes : FileSystemEnumerator<string>
        {
            public DefaultFileAttributes(string directory, EnumerationOptions options)
                : base(directory, options)
            {
            }

            protected override bool ContinueOnError(int error)
            {
                Assert.False(true, $"Should not have errored {error}");
                return false;
            }

            protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
                => !entry.IsDirectory;

            protected override string TransformEntry(ref FileSystemEntry entry)
            {
                string path = entry.ToFullPath();
                File.Delete(path);

                // Attributes require a stat call on Unix- ensure that we have the right attributes
                // even if the returned file is deleted.
                Assert.Equal(FileAttributes.Normal, entry.Attributes);
                Assert.Equal(path, entry.ToFullPath());
                return new string(entry.FileName);
            }
        }

        [Fact]
        public void FileAttributesAreExpected()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo fileOne = new FileInfo(Path.Combine(testDirectory.FullName, GetTestFileName()));

            fileOne.Create().Dispose();

            if (PlatformDetection.IsWindows)
            {
                // Archive should always be set on a new file. Clear it and other expected flags to
                // see that we get "Normal" as the default when enumerating.

                Assert.True((fileOne.Attributes & FileAttributes.Archive) != 0);
                fileOne.Attributes &= ~(FileAttributes.Archive | FileAttributes.NotContentIndexed);
            }

            using (var enumerator = new DefaultFileAttributes(testDirectory.FullName, new EnumerationOptions()))
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(fileOne.Name, enumerator.Current);
                Assert.False(enumerator.MoveNext());
            }
        }

        private class DefaultDirectoryAttributes : FileSystemEnumerator<string>
        {
            public DefaultDirectoryAttributes(string directory, EnumerationOptions options)
                : base(directory, options)
            {
            }

            protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
                => entry.IsDirectory;

            protected override bool ContinueOnError(int error)
            {
                Assert.False(true, $"Should not have errored {error}");
                return false;
            }

            protected override string TransformEntry(ref FileSystemEntry entry)
            {
                string path = entry.ToFullPath();
                Directory.Delete(path);

                // Attributes require a stat call on Unix- ensure that we have the right attributes
                // even if the returned directory is deleted.
                Assert.Equal(FileAttributes.Directory, entry.Attributes);
                Assert.Equal(path, entry.ToFullPath());
                return new string(entry.FileName);
            }
        }

        [Fact]
        public void DirectoryAttributesAreExpected()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            DirectoryInfo subDirectory = Directory.CreateDirectory(Path.Combine(testDirectory.FullName, GetTestFileName()));

            if (PlatformDetection.IsWindows)
            {
                // Clear possible extra flags to see that we get Directory
                subDirectory.Attributes &= ~FileAttributes.NotContentIndexed;
            }

            using (var enumerator = new DefaultDirectoryAttributes(testDirectory.FullName, new EnumerationOptions()))
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(subDirectory.Name, enumerator.Current);
                Assert.False(enumerator.MoveNext());
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
