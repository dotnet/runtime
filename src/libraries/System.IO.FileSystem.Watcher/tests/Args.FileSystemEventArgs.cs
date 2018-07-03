// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class FileSystemEventArgsTests
    {
        [Theory]
        [InlineData(WatcherChangeTypes.Changed, "C:", "foo.txt")]
        [InlineData(WatcherChangeTypes.All, "C:", "foo.txt")]
        [InlineData(0, "", "")]
        [InlineData(0, "", null)]
        public static void FileSystemEventArgs_ctor(WatcherChangeTypes changeType, string directory, string name)
        {
            FileSystemEventArgs args = new FileSystemEventArgs(changeType, directory, name);

            if (!directory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                directory += Path.DirectorySeparatorChar;
            }

            Assert.Equal(changeType, args.ChangeType);
            Assert.Equal(directory + name, args.FullPath);
            Assert.Equal(name, args.Name);
        }

        [Fact]
        public static void FileSystemEventArgs_ctor_Invalid()
        {
            Assert.Throws<NullReferenceException>(() => new FileSystemEventArgs((WatcherChangeTypes)0, null, string.Empty));
        }
    }
}