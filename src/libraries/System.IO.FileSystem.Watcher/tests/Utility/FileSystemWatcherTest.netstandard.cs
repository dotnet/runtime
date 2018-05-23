// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Tests
{
    public abstract partial class FileSystemWatcherTest : FileCleanupTestBase
    {
        private static FileSystemWatcher RecreateWatcher(FileSystemWatcher watcher)
        {
            var newWatcher = new FileSystemWatcher()
            {
                IncludeSubdirectories = watcher.IncludeSubdirectories,
                NotifyFilter = watcher.NotifyFilter,
                Filter = watcher.Filter,
                Path = watcher.Path,
                InternalBufferSize = watcher.InternalBufferSize
            };

            return newWatcher;
        }
    }
}
