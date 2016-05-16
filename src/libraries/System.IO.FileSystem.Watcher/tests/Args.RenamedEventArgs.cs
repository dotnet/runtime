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

            if (!directory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                directory += Path.DirectorySeparatorChar;
            }

            Assert.Equal(changeType, args.ChangeType);
            Assert.Equal(directory + name, args.FullPath);
            Assert.Equal(name, args.Name);

            Assert.Equal(directory + oldName, args.OldFullPath);
            Assert.Equal(oldName, args.OldName);
        }

        [Fact]
        public static void RenamedEventArgs_ctor_Invalid()
        {
            Assert.Throws<NullReferenceException>(() => new RenamedEventArgs((WatcherChangeTypes)0, null, String.Empty, String.Empty));
        }
    }
}