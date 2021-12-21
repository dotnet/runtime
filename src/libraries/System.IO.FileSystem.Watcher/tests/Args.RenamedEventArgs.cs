// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class RenamedEventArgsTests
    {
        [Theory]
        [InlineData(WatcherChangeTypes.Changed, "C:", "foo.txt", "bar.txt")]
        [InlineData(WatcherChangeTypes.All, "C:", "foo.txt", "bar.txt")]
        [InlineData((WatcherChangeTypes)0, "", "", "")]
        [InlineData((WatcherChangeTypes)0, "", null, null)]
        public static void RenamedEventArgs_ctor(WatcherChangeTypes changeType, string directory, string name, string oldName)
        {
            RenamedEventArgs args = new RenamedEventArgs(changeType, directory, name, oldName);
            Assert.Equal(changeType, args.ChangeType);
            Assert.Equal(name, args.Name);
            Assert.Equal(oldName, args.OldName);
            // FullPath is tested as part of the base class FileSystemEventArgs tests
        }

        [Theory]
        [InlineData(WatcherChangeTypes.Changed, "C:", "foo.txt", "bar.txt")]
        [InlineData(WatcherChangeTypes.Changed, "D:\\", "foo.txt", "bar.txt")]
        [InlineData(WatcherChangeTypes.Changed, "E:\\bar", "foo.txt", "bar.txt")]
        [InlineData(WatcherChangeTypes.All, "C:", "foo.txt", "bar.txt")]
        [InlineData(WatcherChangeTypes.All, "D:\\", "foo.txt", "bar.txt")]
        [InlineData(WatcherChangeTypes.All, "E:\\bar", "foo.txt", "bar.txt")]
        public static void RenamedEventArgs_ctor_OldFullPath(WatcherChangeTypes changeType, string directory, string name, string oldName)
        {
            RenamedEventArgs args = new RenamedEventArgs(changeType, directory, name, oldName);

            directory = AppendDirectorySeparator(directory);

            Assert.Equal(changeType, args.ChangeType);
            Assert.Equal(directory + oldName, args.OldFullPath);
            Assert.Equal(name, args.Name);
            Assert.Equal(oldName, args.OldName);
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

        [Theory]
        [InlineData((WatcherChangeTypes)0, "", "", "")]
        [InlineData((WatcherChangeTypes)0, "", null, null)]
        public static void RenamedEventArgs_ctor_OldFullPath_When_EmptyDirectory_Then_FullPathReturnsTheCurrentVolume(WatcherChangeTypes changeType, string directory, string name, string oldName)
        {
            RenamedEventArgs args = new RenamedEventArgs(changeType, directory, name, oldName);

            directory = AppendDirectorySeparator(directory);

            Assert.Equal(changeType, args.ChangeType);
            Assert.Equal(Directory.GetDirectoryRoot(directory + oldName), args.OldFullPath);
            Assert.Equal(name, args.Name);
            Assert.Equal(oldName, args.OldName);
        }

        [Fact]
        public static void RenamedEventArgs_ctor_Invalid()
        {
            Assert.Throws<NullReferenceException>(() => new RenamedEventArgs((WatcherChangeTypes)0, null, string.Empty, string.Empty));
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
