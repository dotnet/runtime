// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Tests
{
    /// <summary>
    /// Helper to expose protected members for testing
    /// </summary>
    public class TestFileSystemWatcher : FileSystemWatcher
    {
        public TestFileSystemWatcher() : base()
        {
        }

        public TestFileSystemWatcher(string path, string filter) : base(path, filter)
        {
        }

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
}
