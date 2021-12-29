// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class FileSystemEventArgsTests
    {
        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(WatcherChangeTypes.Changed, "C:", "foo.txt", "C:\\foo.txt")]
        [InlineData(WatcherChangeTypes.Changed, "D:\\", "foo.txt", "D:\\foo.txt")]
        [InlineData(WatcherChangeTypes.Changed, "E:\\bar", "foo.txt", "E:\\bar\\foo.txt")]
        [InlineData(WatcherChangeTypes.All, "C:", "foo.txt", "C:\\foo.txt")]
        [InlineData(WatcherChangeTypes.All, "D:\\", "foo.txt", "D:\\foo.txt")]
        [InlineData(WatcherChangeTypes.All, "E:\\bar", "foo.txt", "E:\\bar\\foo.txt")]
        public static void FileSystemEventArgs_ctor(WatcherChangeTypes changeType, string directory, string name, string expectedFullPath)
        {
            FileSystemEventArgs args = new FileSystemEventArgs(changeType, directory, name);

            Assert.Equal(changeType, args.ChangeType);
            Assert.Equal(expectedFullPath, args.FullPath);
            Assert.Equal(name, args.Name);
        }

        [Theory]
        [InlineData(WatcherChangeTypes.Changed, "bar", "foo.txt")]
        [InlineData(WatcherChangeTypes.Changed, "bar\\baz", "foo.txt")]
        [InlineData(WatcherChangeTypes.All, "bar", "foo.txt")]
        [InlineData(WatcherChangeTypes.All, "bar\\baz", "foo.txt")]
        public static void FileSystemEventArgs_ctor_DirectoryIsRelativePath(WatcherChangeTypes changeType, string directory, string name)
        {
            FileSystemEventArgs args = new FileSystemEventArgs(changeType, directory, name);

            directory = AppendDirectorySeparator(directory);

            Assert.Equal(changeType, args.ChangeType);
            Assert.Equal(AppendDirectorySeparator(Directory.GetCurrentDirectory()) + directory + name, args.FullPath);
            Assert.Equal(name, args.Name);
        }

        [Theory]
        [InlineData((WatcherChangeTypes)0, "", "")]
        [InlineData((WatcherChangeTypes)0, "", null)]
        public static void FileSystemEventArgs_ctor_When_EmptyDirectoryOrFileName_Then_FullPathReturnsAnEmptyString(WatcherChangeTypes changeType, string directory, string name)
        {
            FileSystemEventArgs args = new FileSystemEventArgs(changeType, directory, name);

            Assert.Equal(changeType, args.ChangeType);
            Assert.Equal(string.Empty, args.FullPath);
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
