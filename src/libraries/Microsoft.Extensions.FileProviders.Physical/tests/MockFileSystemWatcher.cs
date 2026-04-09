// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.Extensions.FileProviders
{
    public class MockFileSystemWatcher : FileSystemWatcher
    {
        public MockFileSystemWatcher(string root)
            : base(root)
        {
        }

        public void CallOnChanged(FileSystemEventArgs e)
        {
            OnChanged(e);
        }

        public void CallOnCreated(FileSystemEventArgs e)
        {
            OnCreated(e);
        }

        public void CallOnDeleted(FileSystemEventArgs e)
        {
            OnDeleted(e);
        }

        public void CallOnError(ErrorEventArgs e)
        {
            OnError(e);
        }

        public void CallOnRenamed(RenamedEventArgs e)
        {
            OnRenamed(e);
        }
    }
}
