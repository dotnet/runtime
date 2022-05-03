// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class RenamedEventArgsTests
    {
        [Theory]
        [InlineData(WatcherChangeTypes.Changed, "bar", "foo.txt", "bar.txt")]
        [InlineData(WatcherChangeTypes.All, "bar", "foo.txt", "bar.txt")]
        [InlineData((WatcherChangeTypes)0, "bar", "", "")]
        [InlineData((WatcherChangeTypes)0, "bar", null, null)]
        public static void RenamedEventArgs_ctor_NonPathPropertiesAreSetCorrectly(WatcherChangeTypes changeType, string directory, string name, string oldName)
        {
            RenamedEventArgs args = new RenamedEventArgs(changeType, directory, name, oldName);

            Assert.Equal(changeType, args.ChangeType);
            Assert.Equal(name, args.Name);
            Assert.Equal(oldName, args.OldName);

            // FullPath is tested as part of the base class FileSystemEventArgs tests
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData("D:\\", "foo.txt", "bar.txt", "D:\\bar.txt")]
        [InlineData("E:\\bar", "foo.txt", "bar.txt", "E:\\bar\\bar.txt")]
        public static void RenamedEventArgs_ctor_OldFullPath_DirectoryIsAnAbsolutePath_Windows(string directory, string name, string oldName, string expectedOldFullPath)
        {
            RenamedEventArgs args = new RenamedEventArgs(WatcherChangeTypes.All, directory, name, oldName);

            Assert.Equal(expectedOldFullPath, args.OldFullPath);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData("/", "foo.txt", "bar.txt", "/bar.txt")]
        [InlineData("/bar", "foo.txt", "bar.txt", "/bar/bar.txt")]
        public static void RenamedEventArgs_ctor_OldFullPath_DirectoryIsAnAbsolutePath_Unix(string directory, string name, string oldName, string expectedOldFullPath)
        {
            RenamedEventArgs args = new RenamedEventArgs(WatcherChangeTypes.All, directory, name, oldName);

            Assert.Equal(expectedOldFullPath, args.OldFullPath);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData("", "", "")]
        [InlineData("", "foo.txt", "bar.txt")]
        [InlineData("bar", "foo.txt", "bar.txt")]
        [InlineData("bar\\baz", "foo.txt", "bar.txt")]
        public static void RenamedEventArgs_ctor_OldFullPath_DirectoryIsRelativePath_Windows(string directory, string name, string oldName)
        {
            RenamedEventArgs args = new RenamedEventArgs(WatcherChangeTypes.All, directory, name, oldName);

            var expectedDirectory = PathInternal.EnsureTrailingSeparator(Path.Combine(Directory.GetCurrentDirectory(), directory));
            Assert.Equal(Path.Combine(expectedDirectory, oldName), args.OldFullPath);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData("", "", "")]
        [InlineData("", "foo.txt", "bar.txt")]
        [InlineData("bar", "foo.txt", "bar.txt")]
        [InlineData("bar/baz", "foo.txt", "bar.txt")]
        public static void RenamedEventArgs_ctor_OldFullPath_DirectoryIsRelativePath_Unix(string directory, string name, string oldName)
        {
            RenamedEventArgs args = new RenamedEventArgs(WatcherChangeTypes.All, directory, name, oldName);

            var expectedDirectory = PathInternal.EnsureTrailingSeparator(Path.Combine(Directory.GetCurrentDirectory(), directory));
            Assert.Equal(Path.Combine(expectedDirectory, oldName), args.OldFullPath);
        }

        [Theory]
        [InlineData("bar", "", "")]
        [InlineData("bar", null, null)]
        [InlineData("bar", "foo.txt", null)]
        public static void RenamedEventArgs_ctor_When_EmptyOldFileName_Then_OldFullPathReturnsTheDirectoryFullPath_WithTrailingSeparator(string directory, string name, string oldName)
        {
            RenamedEventArgs args = new RenamedEventArgs(WatcherChangeTypes.All, directory, name, oldName);

            directory = PathInternal.EnsureTrailingSeparator(directory);

            Assert.Equal(PathInternal.EnsureTrailingSeparator(Directory.GetCurrentDirectory()) + directory, args.OldFullPath);
        }

        [Fact]
        public static void RenamedEventArgs_ctor_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => new RenamedEventArgs((WatcherChangeTypes)0, null, "foo.txt", "bar.txt"));
        }
    }
}
