// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

public partial class RenamedEventArgsTests
{
    private static void ValidateRenamedEventArgs(WatcherChangeTypes changeType, string directory, string name, string oldName)
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
    public static void RenamedEventArgs_ctor()
    {
        ValidateRenamedEventArgs(WatcherChangeTypes.Changed, "C:" + Path.DirectorySeparatorChar, "foo.txt", "bar.txt");
        ValidateRenamedEventArgs(WatcherChangeTypes.Changed, "C:", "foo.txt", "bar.txt");
        ValidateRenamedEventArgs(WatcherChangeTypes.All, "C:" + Path.DirectorySeparatorChar, "foo.txt", "bar.txt");

        ValidateRenamedEventArgs((WatcherChangeTypes)0, String.Empty, String.Empty, String.Empty);
        ValidateRenamedEventArgs((WatcherChangeTypes)0, String.Empty, null, null);

        Assert.Throws<NullReferenceException>(() => new RenamedEventArgs((WatcherChangeTypes)0, null, String.Empty, String.Empty));
    }
}
