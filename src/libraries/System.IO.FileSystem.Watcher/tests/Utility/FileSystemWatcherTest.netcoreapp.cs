// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.IO.Tests
{
    public abstract partial class FileSystemWatcherTest : FileCleanupTestBase
    {
        private static FileSystemWatcher RecreateWatcher(FileSystemWatcher watcher)
        {
            FileSystemWatcher newWatcher = new FileSystemWatcher()
            {
                IncludeSubdirectories = watcher.IncludeSubdirectories,
                NotifyFilter = watcher.NotifyFilter,
                Path = watcher.Path,
                InternalBufferSize = watcher.InternalBufferSize
            };

            foreach (string filter in watcher.Filters)
            {
                newWatcher.Filters.Add(filter);
            }

            return newWatcher;
        }
    }
}
