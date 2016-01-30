// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

public class TemporaryTestFile : FileStream
{
    internal const int DefaultBufferSize = 4096;
    public TemporaryTestFile(string path) :
        base(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete, DefaultBufferSize, FileOptions.DeleteOnClose)
    {
        this.Path = path;
    }

    public string Path { get; private set; }

    public void Move(string targetPath)
    {
        File.Move(this.Path, targetPath);
        this.Path = targetPath;
    }
}
