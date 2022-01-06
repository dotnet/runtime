// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class RenamedEventArgsTests
    {
        [Theory]
        [InlineData(WatcherChangeTypes.Changed, "C:\\bar", "foo.txt", "bar.txt")]
        [InlineData(WatcherChangeTypes.All, "C:\\bar", "foo.txt", "bar.txt")]
        [InlineData((WatcherChangeTypes)0, "C:\\bar", "", "")]
        [InlineData((WatcherChangeTypes)0, "C:\\bar", null, null)]
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
        [InlineData(WatcherChangeTypes.Changed, "D:\\", "foo.txt", "bar.txt", "D:\\bar.txt")]
        [InlineData(WatcherChangeTypes.Changed, "E:\\bar", "foo.txt", "bar.txt", "E:\\bar\\bar.txt")]
        [InlineData(WatcherChangeTypes.All, "D:\\", "foo.txt", "bar.txt", "D:\\bar.txt")]
        [InlineData(WatcherChangeTypes.All, "E:\\bar", "foo.txt", "bar.txt", "E:\\bar\\bar.txt")]
        public static void RenamedEventArgs_ctor_OldFullPath_DirectoryIsAnAbsolutePath(WatcherChangeTypes changeType, string directory, string name, string oldName, string expectedOldFullPath)
        {
            RenamedEventArgs args = new RenamedEventArgs(changeType, directory, name, oldName);

            Assert.Equal(changeType, args.ChangeType);
            Assert.Equal(expectedOldFullPath, args.OldFullPath);
            Assert.Equal(name, args.Name);
            Assert.Equal(oldName, args.OldName);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(WatcherChangeTypes.Changed, "C:", "foo.txt", "bar.txt")]
        [InlineData(WatcherChangeTypes.All, "C:", "foo.txt", "bar.txt")]
        public static void RenamedEventArgs_ctor_OldFullPath_DirectoryIsRelativePathFromCurrentDirectoryInGivenDrive(WatcherChangeTypes changeType, string directory, string name, string oldName)
        {
            RenamedEventArgs args = new RenamedEventArgs(changeType, directory, name, oldName);

            Assert.Equal(changeType, args.ChangeType);
            Assert.Equal(AppendDirectorySeparator(Directory.GetCurrentDirectory()) + oldName, args.OldFullPath);
            Assert.Equal(name, args.Name);
        }

        [Theory]
        [InlineData(WatcherChangeTypes.Changed, "bar", "foo.txt", "bar.txt")]
        [InlineData(WatcherChangeTypes.Changed, "bar\\baz", "foo.txt", "bar.txt")]
        [InlineData(WatcherChangeTypes.All, "bar", "foo.txt", "bar.txt")]
        [InlineData(WatcherChangeTypes.All, "bar\\baz", "foo.txt", "bar.txt")]
        public static void RenamedEventArgs_ctor_OldFullPath_DirectoryIsRelativePath(WatcherChangeTypes changeType, string directory, string name, string oldName)
        {
            RenamedEventArgs args = new RenamedEventArgs(changeType, directory, name, oldName);

            directory = AppendDirectorySeparator(directory);

            Assert.Equal(changeType, args.ChangeType);
            Assert.Equal(AppendDirectorySeparator(Directory.GetCurrentDirectory()) + directory + oldName, args.OldFullPath);
            Assert.Equal(name, args.Name);
            Assert.Equal(oldName, args.OldName);
        }

        [Fact]
        public static void RenamedEventArgs_ctor_Invalid_EmptyDirectory()
        {
            Assert.Throws<ArgumentException>(() => new RenamedEventArgs((WatcherChangeTypes)0, "", "foo.txt", "bar.txt"));
        }

        [Fact]
        public static void RenamedEventArgs_ctor_Invalid_NullDirectory()
        {
            Assert.Throws<ArgumentNullException>(() => new RenamedEventArgs((WatcherChangeTypes)0, null, "foo.txt", "bar.txt"));
        }

        [Theory]
        [InlineData(WatcherChangeTypes.All, "bar", "", "")]
        [InlineData(WatcherChangeTypes.All, "bar", null, null)]
        [InlineData(WatcherChangeTypes.Changed, "bar", "", "")]
        [InlineData(WatcherChangeTypes.Changed, "bar", null, null)]
        [InlineData(WatcherChangeTypes.All, "bar", "foo.txt", "")]
        [InlineData(WatcherChangeTypes.Changed, "bar", "foo.txt", null)]
        public static void RenamedEventArgs_ctor_When_EmptyOldFileName_Then_OldFullPathReturnsTheDirectoryFullPath(WatcherChangeTypes changeType, string directory, string name, string oldName)
        {
            RenamedEventArgs args = new RenamedEventArgs(changeType, directory, name, oldName);

            Assert.Equal(changeType, args.ChangeType);
            Assert.Equal(AppendDirectorySeparator(Directory.GetCurrentDirectory()) + directory, args.OldFullPath);
            Assert.Equal(name, args.Name);
        }

        #region Test Helpers

        private static string AppendDirectorySeparator(string directory)
        {
            if (!directory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                directory += Path.DirectorySeparatorChar;
            }
            return directory;
        }

        #endregion
    }
}
