// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class RenamedEventArgsTests
    {
        [Theory]
        [InlineData(WatcherChangeTypes.Changed, "C:", "foo.txt", "bar.txt")]
        [InlineData(WatcherChangeTypes.All, "C:", "foo.txt", "bar.txt")]
        [InlineData(0, "", "", "")]
        [InlineData(0, "", null, null)]
        public static void RenamedEventArgs_ctor(WatcherChangeTypes changeType, string directory, string name, string oldName)
        {
            RenamedEventArgs args = new RenamedEventArgs(changeType, directory, name, oldName);
            Assert.Equal(changeType, args.ChangeType);
            Assert.Equal(directory + Path.DirectorySeparatorChar + name, args.FullPath);
            Assert.Equal(name, args.Name);
            Assert.Equal(oldName, args.OldName);
        }

        [Theory]
        [InlineData(WatcherChangeTypes.Changed, "C:", "foo.txt", "bar.txt")]
        [InlineData(WatcherChangeTypes.All, "C:", "foo.txt", "bar.txt")]
        [InlineData(0, "", "", "")]
        [InlineData(0, "", null, null)]
        public static void RenamedEventArgs_ctor_OldFullPath(WatcherChangeTypes changeType, string directory, string name, string oldName)
        {
            RenamedEventArgs args = new RenamedEventArgs(changeType, directory, name, oldName);
            Assert.Equal(directory + Path.DirectorySeparatorChar + oldName, args.OldFullPath);
        }

        [Fact]
        public static void RenamedEventArgs_ctor_Invalid()
        {
            Assert.Throws<NullReferenceException>(() => new RenamedEventArgs((WatcherChangeTypes)0, null, string.Empty, string.Empty));
        }
    }
}