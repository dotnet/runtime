// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class FileSystemEventArgsTests
    {
        [Theory]
        [InlineData(WatcherChangeTypes.Changed, "C:", "foo.txt")]
        [InlineData(WatcherChangeTypes.Changed, "D:\\", "foo.txt")]
        [InlineData(WatcherChangeTypes.Changed, "E:\\bar", "foo.txt")]
        [InlineData(WatcherChangeTypes.All, "C:", "foo.txt")]
        [InlineData(WatcherChangeTypes.All, "D:\\", "foo.txt")]
        [InlineData(WatcherChangeTypes.All, "E:\\bar", "foo.txt")]
        public static void FileSystemEventArgs_ctor(WatcherChangeTypes changeType, string directory, string name)
        {
            FileSystemEventArgs args = new FileSystemEventArgs(changeType, directory, name);

            directory = AppendDirectorySeparator(directory);

            Assert.Equal(changeType, args.ChangeType);
            Assert.Equal(directory + name, args.FullPath);
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
        public static void FileSystemEventArgs_ctor_When_EmptyDirectory_Then_FullPathReturnsTheCurrentVolume(WatcherChangeTypes changeType, string directory, string name)
        {
            FileSystemEventArgs args = new FileSystemEventArgs(changeType, directory, name);

            directory = AppendDirectorySeparator(directory);

            Assert.Equal(changeType, args.ChangeType);
            Assert.Equal(Directory.GetDirectoryRoot(directory + name), args.FullPath);
            Assert.Equal(name, args.Name);
        }

        [Fact]
        public static void FileSystemEventArgs_ctor_Invalid()
        {
            Assert.Throws<NullReferenceException>(() => new FileSystemEventArgs((WatcherChangeTypes)0, null, string.Empty));
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
