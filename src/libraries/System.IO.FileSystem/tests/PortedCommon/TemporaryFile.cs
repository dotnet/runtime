// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using IOPath = System.IO.Path;

internal class TemporaryFile : TemporaryFileSystemItem<FileInfo>
{
    public TemporaryFile()
        : base(new FileInfo(IOPath.GetTempFileName()))
    {
    }

    protected override void Delete()
    {
        File.Delete(Path);
    }
}
