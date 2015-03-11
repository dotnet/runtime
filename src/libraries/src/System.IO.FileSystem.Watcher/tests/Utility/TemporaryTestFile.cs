// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
