// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

public class FileSystemEventArgsTests
{
    private static void ValidateFileSystemEventArgs(WatcherChangeTypes changeType, string directory, string name)
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
    public static void FileSystemEventArgs_ctor()
    {
        ValidateFileSystemEventArgs(WatcherChangeTypes.Changed, "C:" + Path.DirectorySeparatorChar, "foo.txt");
        ValidateFileSystemEventArgs(WatcherChangeTypes.Changed, "C:", "foo.txt");
        ValidateFileSystemEventArgs(WatcherChangeTypes.All, "C:" + Path.DirectorySeparatorChar, "foo.txt");

        ValidateFileSystemEventArgs((WatcherChangeTypes)0, String.Empty, String.Empty);
        ValidateFileSystemEventArgs((WatcherChangeTypes)0, String.Empty, null);

        Assert.Throws<NullReferenceException>(() => new FileSystemEventArgs((WatcherChangeTypes)0, null, String.Empty));
    }
}
