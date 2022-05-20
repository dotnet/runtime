// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class FileSystemEventArgsTests
    {
        [Theory]
        [InlineData(WatcherChangeTypes.Changed, "C:", "foo.txt")]
        [InlineData(WatcherChangeTypes.All, "C:", "foo.txt")]
        [InlineData((WatcherChangeTypes)0, "", "")]
        [InlineData((WatcherChangeTypes)0, "", null)]
        public static void FileSystemEventArgs_ctor_NonPathPropertiesAreSetCorrectly(WatcherChangeTypes changeType, string directory, string name)
        {
            FileSystemEventArgs args = new FileSystemEventArgs(changeType, directory, name);

            Assert.Equal(changeType, args.ChangeType);
            Assert.Equal(name, args.Name);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData("D:\\", null, "D:\\")]
        [InlineData("D:\\", "", "D:\\")]
        [InlineData("D:\\", "foo.txt", "D:\\foo.txt")]
        [InlineData("E:\\bar", null, "E:\\bar\\")]
        [InlineData("E:\\bar", "", "E:\\bar\\")]
        [InlineData("E:\\bar", "foo.txt", "E:\\bar\\foo.txt")]
        [InlineData("E:\\bar\\", null, "E:\\bar\\")]
        [InlineData("E:\\bar\\", "", "E:\\bar\\")]
        [InlineData("E:\\bar\\", "foo.txt", "E:\\bar\\foo.txt")]
        public static void FileSystemEventArgs_ctor_DirectoryIsAbsolutePath_Windows(string directory, string name, string expectedFullPath)
        {
            FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.All, directory, name);

            Assert.Equal(expectedFullPath, args.FullPath);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData("/", null, "/")]
        [InlineData("/", "", "/")]
        [InlineData("/", "   ", "/   ")]
        [InlineData("/", "foo.txt", "/foo.txt")]
        [InlineData("/bar", null, "/bar/")]
        [InlineData("/bar", "", "/bar/")]
        [InlineData("/bar", "foo.txt", "/bar/foo.txt")]
        [InlineData("/bar/", null, "/bar/")]
        [InlineData("/bar/", "", "/bar/")]
        [InlineData("/bar/", "foo.txt", "/bar/foo.txt")]
        public static void FileSystemEventArgs_ctor_DirectoryIsAbsolutePath_Unix(string directory, string name, string expectedFullPath)
        {
            FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.All, directory, name);

            Assert.Equal(expectedFullPath, args.FullPath);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData("", "", "\\")]
        [InlineData("", "foo.txt", "\\foo.txt")]
        [InlineData("bar", "foo.txt", "bar\\foo.txt")]
        [InlineData("bar\\baz", "foo.txt", "bar\\baz\\foo.txt")]
        public static void FileSystemEventArgs_ctor_DirectoryIsRelativePath_Windows(string directory, string name, string expectedFullPath)
        {
            FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.All, directory, name);

            Assert.Equal(expectedFullPath, args.FullPath);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData("", "", "/")]
        [InlineData("", "foo.txt", "/foo.txt")]
        [InlineData("   ", "    ", "   /    ")]
        [InlineData("   ", "foo.txt", "   /foo.txt")]
        [InlineData("bar", "foo.txt", "bar/foo.txt")]
        [InlineData("bar/baz", "foo.txt", "bar/baz/foo.txt")]
        public static void FileSystemEventArgs_ctor_DirectoryIsRelativePath_Unix(string directory, string name, string expectedFullPath)
        {
            FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.All, directory, name);

            Assert.Equal(expectedFullPath, args.FullPath);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData("bar", "", "bar\\")]
        [InlineData("bar", null, "bar\\")]
        public static void FileSystemEventArgs_ctor_EmptyFileName_Windows(string directory, string name, string expectedFullPath)
        {
            FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.All, directory, name);

            Assert.Equal(expectedFullPath, args.FullPath);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData("bar", "", "bar/")]
        [InlineData("bar", null, "bar/")]
        public static void FileSystemEventArgs_ctor_EmptyFileName_Unix(string directory, string name, string expectedFullPath)
        {
            FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.All, directory, name);

            Assert.Equal(expectedFullPath, args.FullPath);
        }

        [Fact]
        public static void FileSystemEventArgs_ctor_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => new FileSystemEventArgs((WatcherChangeTypes)0, null, "foo.txt"));
        }
    }
}
