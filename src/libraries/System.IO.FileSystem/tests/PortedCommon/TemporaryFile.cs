// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
