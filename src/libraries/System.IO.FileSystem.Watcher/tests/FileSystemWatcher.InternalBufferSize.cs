// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using Xunit;

public class InternalBufferSizeTests
{
    [Fact]
    [ActiveIssue(1165)]
    [PlatformSpecific(PlatformID.Windows)]
    public static void FileSystemWatcher_InternalBufferSize_File()
    {
        using (var file = Utility.CreateTestFile())
        using (var watcher = new FileSystemWatcher("."))
        {
            watcher.Filter = Path.GetFileName(file.Path);
            ManualResetEvent unblockHandler = new ManualResetEvent(false);
            watcher.Changed += (o, e) =>
            {
                // block the handling thread
                unblockHandler.WaitOne();
            };

            AutoResetEvent eventOccured = new AutoResetEvent(false);
            watcher.Error += (o, e) =>
            {
                eventOccured.Set();
            };
            watcher.EnableRaisingEvents = true;

            // See note in FileSystemWatcher_Error_File
            int originalInternalBufferOperationCapacity = watcher.InternalBufferSize / (16 + 2 * (file.Path.Length + 1));
            for (int i = 1; i < originalInternalBufferOperationCapacity * 2; i++)
            {
                File.SetLastWriteTime(file.Path, DateTime.Now + TimeSpan.FromSeconds(i));
            }

            unblockHandler.Set();
            Utility.ExpectEvent(eventOccured, "error");

            // Update InternalBufferSize to accomadate the data
            watcher.InternalBufferSize = watcher.InternalBufferSize * 2;
            unblockHandler.Reset();

            // Send the same number of events.
            for (int i = 1; i < originalInternalBufferOperationCapacity * 2; i++)
            {
                File.SetLastWriteTime(file.Path, DateTime.Now + TimeSpan.FromSeconds(i));
            }
            unblockHandler.Set();
            // This time we should not see an error
            Utility.ExpectNoEvent(eventOccured, "error");
        }
    }
}
