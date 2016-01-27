// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

public class TemporaryTestDirectory : IDisposable
{
    public TemporaryTestDirectory(string path)
    {
        Directory.CreateDirectory(path);
        this.Path = path;
    }

    public string Path { get; private set; }

    public void Move(string targetPath)
    {
        Directory.Move(this.Path, targetPath);
        this.Path = targetPath;
    }

    // Use a finalizer to ensure we always clean up
    ~TemporaryTestDirectory()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        if (!string.IsNullOrEmpty(this.Path))
        {
            Utility.EnsureDelete(this.Path);
        }
    }
}
