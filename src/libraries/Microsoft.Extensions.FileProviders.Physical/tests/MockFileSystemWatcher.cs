// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
