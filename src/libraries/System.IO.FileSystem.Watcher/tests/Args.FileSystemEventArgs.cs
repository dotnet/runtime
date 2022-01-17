// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class FileSystemEventArgsTests
    {
        [Fact]
        public static void FileSystemEventArgs_ctor_ChangeType_IsSetCorrectly()
        {
            FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.Deleted, "bar", "foo.txt");
            Assert.Equal(WatcherChangeTypes.Deleted, args.ChangeType);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData("D:\\", "foo.txt", "D:\\foo.txt")]
        [InlineData("E:\\bar", "foo.txt", "E:\\bar\\foo.txt")]
        public static void FileSystemEventArgs_ctor_DirectoryIsAbsolutePath_Windows(string directory, string name, string expectedFullPath)
        {
            FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.All, directory, name);

            Assert.Equal(expectedFullPath, args.FullPath);
            Assert.Equal(name, args.Name);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData("/", "foo.txt", "/foo.txt")]
        [InlineData("/bar", "foo.txt", "/bar/foo.txt")]
        public static void FileSystemEventArgs_ctor_DirectoryIsAbsolutePath_Unix(string directory, string name, string expectedFullPath)
        {
            FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.All, directory, name);

            Assert.Equal(expectedFullPath, args.FullPath);
            Assert.Equal(name, args.Name);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData("bar", "foo.txt")]
        [InlineData("bar\\baz", "foo.txt")]
        public static void FileSystemEventArgs_ctor_DirectoryIsRelativePath_Windows(string directory, string name)
        {
            FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.All, directory, name);

            directory = AppendDirectorySeparator(directory);

            Assert.Equal(AppendDirectorySeparator(Directory.GetCurrentDirectory()) + directory + name, args.FullPath);
            Assert.Equal(name, args.Name);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData("bar", "foo.txt")]
        [InlineData("bar/baz", "foo.txt")]
        public static void FileSystemEventArgs_ctor_DirectoryIsRelativePath_Unix(string directory, string name)
        {
            FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.All, directory, name);

            directory = AppendDirectorySeparator(directory);

            Assert.Equal(AppendDirectorySeparator(Directory.GetCurrentDirectory()) + directory + name, args.FullPath);
            Assert.Equal(name, args.Name);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData("C:", "foo.txt")]
        public static void FileSystemEventArgs_ctor_RelativePathFromCurrentDirectoryInGivenDrive(string directory, string name)
        {
            FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.All, directory, name);

            Assert.Equal(AppendDirectorySeparator(Directory.GetCurrentDirectory()) + name, args.FullPath);
            Assert.Equal(name, args.Name);
        }

        [Theory]
        [InlineData("bar", "")]
        [InlineData("bar", null)]
        public static void FileSystemEventArgs_ctor_When_EmptyFileName_Then_FullPathReturnsTheDirectoryFullPath(string directory, string name)
        {
            FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.All, directory, name);

            Assert.Equal(AppendDirectorySeparator(Directory.GetCurrentDirectory()) + directory, args.FullPath);
            Assert.Equal(name, args.Name);
        }

        [Fact]
        public static void FileSystemEventArgs_ctor_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => new FileSystemEventArgs((WatcherChangeTypes)0, null, "foo.txt"));
            Assert.Throws<ArgumentException>(() => new FileSystemEventArgs((WatcherChangeTypes)0, "", "foo.txt"));
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
