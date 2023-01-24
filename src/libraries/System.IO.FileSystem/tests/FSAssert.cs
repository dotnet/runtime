// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public static class FSAssert
    {
        public static void ThrowsSharingViolation(Action testCode)
        {
            Exception e = Assert.ThrowsAny<Exception>(testCode);
            Assert.True(e is UnauthorizedAccessException || e is IOException, "Exception should be either UnauthorizedAccessException or IOException: " + e.ToString());
        }

        public static void CompletesSynchronously(Task task)
        {
            // The purpose of this method is to try and ensure that a task does a particular
            // piece of work (like return a result or throw) synchronously.

            // We cannot use Task's IAsyncResult implementation since it doesn't set
            // CompletedSynchronously to anything but false.

            TaskStatus status = task.Status;

            Assert.NotEqual(TaskStatus.Faulted, status);

            if (status == TaskStatus.Canceled)
            {
                // Could have happened synchronously. Don't assert but trigger the exception
                task.GetAwaiter().GetResult();
            }
            else
            {
                // first wait for the task to complete without throwing
                using WaitHandle wh = ((IAsyncResult)task).AsyncWaitHandle;
                wh.WaitOne();

                // now assert, we ignore the result of the task intentionally,
                // As it previously did not complete synchronously.
                Assert.Equal(TaskStatus.RanToCompletion, status);
            }

            return;
        }

        public static T CompletesSynchronously<T>(Task<T> task)
        {
            CompletesSynchronously((Task)task);

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

        public static void EqualWhenOrdered<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            Assert.Equal(expected.OrderBy(e => e).Select(o => o), actual.OrderBy(a => a).Select(o => o));
        }
    }
}
