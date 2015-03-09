// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

public class TestFileSystemWatcher : FileSystemWatcher
{
    // Helper to expose protected members for testing

    public void CallOnChanged(FileSystemEventArgs e)
    {
        this.OnChanged(e);
    }

    public void CallOnCreated(FileSystemEventArgs e)
    {
        this.OnCreated(e);
    }

    public void CallOnDeleted(FileSystemEventArgs e)
    {
        this.OnDeleted(e);
    }

    public void CallOnError(ErrorEventArgs e)
    {
        this.OnError(e);
    }

    public void CallOnRenamed(RenamedEventArgs e)
    {
        this.OnRenamed(e);
    }
}
