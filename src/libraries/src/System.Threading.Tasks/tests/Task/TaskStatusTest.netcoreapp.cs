// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public class TaskStatusProperties
    {
        public static IEnumerable<object[]> Status_IsProperties_Match_MemberData()
        {
            yield return new object[] { new StrongBox<Task>(Task.CompletedTask) };

            yield return new object[] { new StrongBox<Task>(new Task(() => { })) };

            yield return new object[] { new StrongBox<Task>(new TaskCompletionSource<int>().Task) };

            {
                var tcs = new TaskCompletionSource<int>();
                tcs.SetResult(42);
                yield return new object[] { new StrongBox<Task>(tcs.Task) };
            }

            {
                var tcs = new TaskCompletionSource<int>();
                tcs.SetException(new Exception());
                yield return new object[] { new StrongBox<Task>(tcs.Task) };
            }

            {
                var tcs = new TaskCompletionSource<int>();
                tcs.SetCanceled();
                yield return new object[] { new StrongBox<Task>(tcs.Task) };
            }

            {
                var t = Task.Run(() => { });
                t.Wait();
                yield return new object[] { new StrongBox<Task>(t) };
            }

            {
                var atmb = new AsyncTaskMethodBuilder<bool>();
                atmb.SetResult(true);
                yield return new object[] { new StrongBox<Task>(atmb.Task) };
            }
        }

        [Theory]
        [MemberData(nameof(Status_IsProperties_Match_MemberData))]
        public void Status_IsProperties_Match(StrongBox<Task> taskBox)
        {
            // The StrongBox<Task> is a workaround for xunit trying to automatically
            // Dispose of any IDisposable passed into a theory, but Task doesn't like
            // being Dispose'd when it's not in a final state.
            Task task = taskBox.Value;

            if (task.IsCompletedSuccessfully)
            {
                Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            }
            else if (task.IsFaulted)
            {
                Assert.Equal(TaskStatus.Faulted, task.Status);
            }
            else if (task.IsCanceled)
            {
                Assert.Equal(TaskStatus.Canceled, task.Status);
            }

            switch (task.Status)
            {
                case TaskStatus.RanToCompletion:
                    Assert.True(task.IsCompleted, "Expected IsCompleted to be true");
                    Assert.True(task.IsCompletedSuccessfully, "Expected IsCompletedSuccessfully to be true");
                    Assert.False(task.IsFaulted, "Expected IsFaulted to be false");
                    Assert.False(task.IsCanceled, "Expected IsCanceled to be false");
                    break;

                case TaskStatus.Faulted:
                    Assert.True(task.IsCompleted, "Expected IsCompleted to be true");
                    Assert.False(task.IsCompletedSuccessfully, "Expected IsCompletedSuccessfully to be false");
                    Assert.True(task.IsFaulted, "Expected IsFaulted to be true");
                    Assert.False(task.IsCanceled, "Expected IsCanceled to be false");
                    break;

                case TaskStatus.Canceled:
                    Assert.True(task.IsCompleted, "Expected IsCompleted to be true");
                    Assert.False(task.IsCompletedSuccessfully, "Expected IsCompletedSuccessfully to be false");
                    Assert.False(task.IsFaulted, "Expected IsFaulted to be false");
                    Assert.True(task.IsCanceled, "Expected IsCanceled to be true");
                    break;

                default:
                    Assert.False(task.IsCompleted, "Expected IsCompleted to be false");
                    Assert.False(task.IsCompletedSuccessfully, "Expected IsCompletedSuccessfully to be false");
                    Assert.False(task.IsFaulted, "Expected IsFaulted to be false");
                    Assert.False(task.IsCanceled, "Expected IsCanceled to be false");
                    break;
            }
        }
    }
}
