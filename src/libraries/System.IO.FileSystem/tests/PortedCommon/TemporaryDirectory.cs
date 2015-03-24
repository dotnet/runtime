// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using IOPath = System.IO.Path;

internal class TemporaryDirectory : TemporaryFileSystemItem<DirectoryInfo>
{
    public TemporaryDirectory()
        : base(CreateTemporaryDirectoryInfo())
    {
    }

    protected override void Delete()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, true);
        }
    }

    private static DirectoryInfo CreateTemporaryDirectoryInfo()
    {
        string path = IOPath.Combine(TestInfo.CurrentDirectory, IOPath.GetRandomFileName());

        return Directory.CreateDirectory(path);
    }
}
