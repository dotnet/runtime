// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public static class FSAssert
    {
        public static void ThrowsSharingViolation(Action testCode)
        {
            Exception e = Assert.ThrowsAny<Exception>(testCode);
            Assert.True(e is UnauthorizedAccessException || e is IOException, "Exception should be either UnauthorizedAccessException or IOException: " + e.ToString());
        }

        public static void CompletesSynchronously(Task task, bool permitFaultedTask = false)
        {
            // The purpose of this method is to try and ensure that a task does a particular
            // piece of work (like return a result or throw) synchronously.

            // We cannot use Task's IAsyncResult implementation since it doesn't set 
            // CompletedSynchronously to anything but false.

            TaskStatus status = task.Status;

            if (status == TaskStatus.Faulted)
            {
                // If an API is expected to return a faulted task instead of throwing synchronously
                // then permit this.
                if (permitFaultedTask)
                {
                    // Could have happened synchronously. Don't assert but trigger the exception
                    task.GetAwaiter().GetResult();
                }
                else
                {
                    Assert.True(false, "Should throw synchronously, but instead returned faulted task");
                }
            }
            else if (status == TaskStatus.Canceled)
            {
                // Could have happened synchronously. Don't assert but trigger the exception
                task.GetAwaiter().GetResult();
            }
            else if (status != TaskStatus.RanToCompletion)
            {
                // first wait for the task to complete without throwing
                ((IAsyncResult)task).AsyncWaitHandle.WaitOne();

                // now assert, we ignore the result of the task intentionally
                Assert.True(false, "Should complete synchronously");
            }

            return;
        }

        public static T CompletesSynchronously<T>(Task<T> task, bool permitFaultedTask = false)
        {
            CompletesSynchronously((Task)task, permitFaultedTask);

            // this should not throw or wait since we waited above and threw
            // when it didn't complete synchronously.
            return task.Result;
        }

        public static void IsCancelled(Task task, CancellationToken ct)
        {
            Assert.True(task.IsCanceled);
            OperationCanceledException tce = Assert.ThrowsAny<OperationCanceledException>(() => task.GetAwaiter().GetResult());
            Assert.NotNull(tce);
            Assert.Equal(ct, tce.CancellationToken);
        }
    }
}
