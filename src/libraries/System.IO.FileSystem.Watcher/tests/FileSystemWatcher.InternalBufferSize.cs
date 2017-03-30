// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public class InternalBufferSizeTests : FileSystemWatcherTest
    {
        // FSW works by calling ReadDirectoryChanges asynchronously, processing the changes
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
        // FILE_NOTIFY_INFORMATION.  This works out to 16 + 2 * (Path.GetFileName(file.Path).Length + 1) bytes, where filePath
        // is the path to changed file relative to the path passed into ReadDirectoryChanges.

        // At some point we might decide to improve how FSW handles this at which point we'll need
        // a better test for Error (perhaps just a mock), but for now there is some value in forcing this limit.
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes
        public void FileSystemWatcher_InternalBufferSize(bool setToHigherCapacity)
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var file = new TempFile(Path.Combine(testDirectory.Path, "file")))
            using (var watcher = new FileSystemWatcher(testDirectory.Path, Path.GetFileName(file.Path)))
            {
                int internalBufferOperationCapacity = watcher.InternalBufferSize / (17 + Path.GetFileName(file.Path).Length);

                // Set the capacity high to ensure no error events arise.
                if (setToHigherCapacity)
                    watcher.InternalBufferSize = watcher.InternalBufferSize * 12;

                // block the handling thread
                ManualResetEvent unblockHandler = new ManualResetEvent(false);
                watcher.Changed += (o, e) =>
                {
                    unblockHandler.WaitOne();
                };

                // generate enough file change events to overflow the default buffer
                Action action = () =>
                {
                    for (int i = 1; i < internalBufferOperationCapacity * 10; i++)
                    {
                        File.SetLastWriteTime(file.Path, DateTime.Now + TimeSpan.FromSeconds(i));
                    }

                    unblockHandler.Set();
                };
                Action cleanup = () => unblockHandler.Reset();

                if (setToHigherCapacity)
                    ExpectNoError(watcher, action, cleanup);
                else
                    ExpectError(watcher, action, cleanup);
            }
        }
    }
}