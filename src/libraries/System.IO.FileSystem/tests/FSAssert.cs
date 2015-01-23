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

        public static void CompletesSynchronously(Task task, bool checkCompletedSynchronously = true)
        {
            IAsyncResult result = (IAsyncResult)task;

            // some IAsyncResult implementations don't set CompletedSynchronously
            // if that is expected, relax the check to just expect completion
            // this does allow for a race condition (task completes asynchronously
            // before we are able to check) but that is all that we can do.
            bool completed = checkCompletedSynchronously ? !result.CompletedSynchronously : !result.IsCompleted;

            if (!completed)
            {
                // wait on the task before asserting so that we don't leak it
                result.AsyncWaitHandle.WaitOne();
                Assert.True(false, "Should complete synchronously");
            }

            return;
        }

        public static T CompletesSynchronously<T>(Task<T> task, bool checkCompletedSynchronously = true)
        {
            CompletesSynchronously((Task)task, checkCompletedSynchronously);

            // this should not throw or wait since we waited above and threw
            // when it didn't complete synchronously.
            return task.Result;
        }

        public static void IsCancelled(Task task, CancellationToken ct)
        {
            Assert.True(task.IsCanceled);
            TaskCanceledException tcs = Assert.Throws<TaskCanceledException>(() => task.GetAwaiter().GetResult());
            Assert.NotNull(tcs);
            Assert.Equal(ct, tcs.CancellationToken);
        }
    }
}
