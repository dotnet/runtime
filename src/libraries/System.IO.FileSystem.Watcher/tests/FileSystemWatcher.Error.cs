// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using Xunit;

public partial class ErrorTests
{
    [Fact]
    [PlatformSpecific(PlatformID.Windows)]
    public static void FileSystemWatcher_Error_File()
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

            // FSW works by calling ReadDirectoryChanges aynchronously, processing the changes
            // in the callback and invoking event handlers from the callback serially.
            // After it processes all events for a particular callback it calls ReadDirectoryChanges
            // to queue another overlapped operation.
            // PER MSDN:
            //   When you first call ReadDirectoryChangesW, the system allocates a buffer to store change
            //   information. This buffer is associated with the directory handle until it is closed and 
            //   its size does not change during its lifetime. Directory changes that occur between calls
            //   to this function are added to the buffer and then returned with the next call. If the 
            //   buffer overflows, the entire contents of the buffer are discarded, the lpBytesReturned 
            //   parameter contains zero, and the ReadDirectoryChangesW function fails with the error 
            //   code ERROR_NOTIFY_ENUM_DIR.
            // We can force the error by increasing the amount of time between calls to ReadDirectoryChangesW
            // By blocking in an event handler, we allow the main thread of our test to generate a ton
            // of change events and overflow the OS's buffer.

            // We could copy the result of the operation and immediately call ReadDirectoryChangesW to 
            // limit the amount of time where we rely on the OS's buffer.  The downside of doing so 
            // is it can allow memory to grow out of control.

            // FSW tries to mitigate this by exposing InternalBufferSize.  The OS uses this when allocating
            // it's internal buffer (up to some limit).  Our docs say that limit is 64KB but testing on Win8.1 
            // indicates that it is much higher than this: I could grow the buffer up to 128 MB and still see
            // that it had an effect.  The size needed per operation is determined by the struct layout of 
            // FILE_NOTIFY_INFORMATION.  This works out to 16 + 2 * (filePath.Length + 1) bytes, where filePath
            // is the path to changed file relative to the path passed into ReadDirectoryChanges.

            // At some point we might decide to improve how FSW handles this at which point we'll need
            // a better test for Error (perhaps just a mock), but for now there is some value in forcing this limit.

            int internalBufferOperationCapacity = watcher.InternalBufferSize / (12 + 2 * (file.Path.Length + 1));

            // generate enough file change events to overflow the buffer
            // 4x is an approximation that should force the overflow.
            for (int i = 1; i < internalBufferOperationCapacity * 4; i++)
            {
                File.SetLastWriteTime(file.Path, DateTime.Now + TimeSpan.FromSeconds(i));
            }

            unblockHandler.Set();

            Utility.ExpectEvent(eventOccured, "error");
        }
    }
}
