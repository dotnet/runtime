// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Threading.Tasks.Tests.Status
{
    public sealed class TaskCompletionSourceTests
    {
        [Fact]
        public void Ctor_ArgumentsRoundtrip()
        {
            TaskCompletionSource tcs;
            object stateObj = new object();

            tcs = new TaskCompletionSource();
            Assert.NotNull(tcs.Task);
            Assert.Same(tcs.Task, tcs.Task);
            Assert.Equal(TaskStatus.WaitingForActivation, tcs.Task.Status);
            Assert.Null(tcs.Task.AsyncState);

            tcs = new TaskCompletionSource(stateObj);
            Assert.NotNull(tcs.Task);
            Assert.Same(tcs.Task, tcs.Task);
            Assert.Equal(TaskStatus.WaitingForActivation, tcs.Task.Status);
            Assert.Same(stateObj, tcs.Task.AsyncState);

            tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Assert.NotNull(tcs.Task);
            Assert.Same(tcs.Task, tcs.Task);
            Assert.Equal(TaskStatus.WaitingForActivation, tcs.Task.Status);
            Assert.Equal(TaskCreationOptions.RunContinuationsAsynchronously, tcs.Task.CreationOptions);
            Assert.Null(tcs.Task.AsyncState);

            tcs = new TaskCompletionSource(stateObj, TaskCreationOptions.RunContinuationsAsynchronously);
            Assert.NotNull(tcs.Task);
            Assert.Same(tcs.Task, tcs.Task);
            Assert.Equal(TaskStatus.WaitingForActivation, tcs.Task.Status);
            Assert.Equal(TaskCreationOptions.RunContinuationsAsynchronously, tcs.Task.CreationOptions);
            Assert.Same(stateObj, tcs.Task.AsyncState);
        }

        [Fact]
        public void Ctor_InvalidArguments_Throws()
        {
            // These shouldn't throw.
            new TaskCompletionSource(TaskCreationOptions.AttachedToParent);
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            new TaskCompletionSource(TaskCreationOptions.AttachedToParent | TaskCreationOptions.RunContinuationsAsynchronously);
            new TaskCompletionSource(new object(), TaskCreationOptions.AttachedToParent);
            new TaskCompletionSource(new object(), TaskCreationOptions.RunContinuationsAsynchronously);
            new TaskCompletionSource(new object(), TaskCreationOptions.AttachedToParent | TaskCreationOptions.RunContinuationsAsynchronously);

            // These should throw.
            foreach (TaskCreationOptions options in Enum.GetValues(typeof(TaskCreationOptions)))
            {
                if ((options & TaskCreationOptions.AttachedToParent) != 0 &&
                    (options & TaskCreationOptions.RunContinuationsAsynchronously) != 0)
                {
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("creationOptions", () => new TaskCompletionSource(options));
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("creationOptions", () => new TaskCompletionSource(options | TaskCreationOptions.RunContinuationsAsynchronously));
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("creationOptions", () => new TaskCompletionSource(new object(), options));
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("creationOptions", () => new TaskCompletionSource(new object(), options | TaskCreationOptions.RunContinuationsAsynchronously));
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SetResult_CompletesSuccessfully(bool tryMethod)
        {
            var tcs = new TaskCompletionSource();
            Assert.Equal(TaskStatus.WaitingForActivation, tcs.Task.Status);

            if (tryMethod)
            {
                Assert.True(tcs.TrySetResult());
            }
            else
            {
                tcs.SetResult();
            }
            Assert.Equal(TaskStatus.RanToCompletion, tcs.Task.Status);

            AssertCompletedTcsFailsToCompleteAgain(tcs);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SetCanceled_CompletesSuccessfully(bool tryMethod)
        {
            var tcs = new TaskCompletionSource();
            Assert.Equal(TaskStatus.WaitingForActivation, tcs.Task.Status);

            if (tryMethod)
            {
                Assert.True(tcs.TrySetCanceled());
            }
            else
            {
                tcs.SetCanceled();
            }
            Assert.Equal(TaskStatus.Canceled, tcs.Task.Status);
            Assert.Null(tcs.Task.Exception);
            TaskCanceledException tce = Assert.Throws<TaskCanceledException>(() => tcs.Task.GetAwaiter().GetResult());
            Assert.Equal(default, tce.CancellationToken);

            AssertCompletedTcsFailsToCompleteAgain(tcs);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SetCanceled_Token_CompletesSuccessfully(bool tryMethod)
        {
            var tcs = new TaskCompletionSource();
            var cts = new CancellationTokenSource();
            Assert.Equal(TaskStatus.WaitingForActivation, tcs.Task.Status);

            if (tryMethod)
            {
                Assert.True(tcs.TrySetCanceled(cts.Token));
            }
            else
            {
                tcs.SetCanceled(cts.Token);
            }
            Assert.Equal(TaskStatus.Canceled, tcs.Task.Status);
            Assert.Null(tcs.Task.Exception);
            TaskCanceledException tce = Assert.Throws<TaskCanceledException>(() => tcs.Task.GetAwaiter().GetResult());
            Assert.Equal(cts.Token, tce.CancellationToken);

            AssertCompletedTcsFailsToCompleteAgain(tcs);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SetException_Exception_CompletesSuccessfully(bool tryMethod)
        {
            var e = new Exception();
            var tcs = new TaskCompletionSource();
            Assert.Equal(TaskStatus.WaitingForActivation, tcs.Task.Status);

            if (tryMethod)
            {
                Assert.True(tcs.TrySetException(e));
            }
            else
            {
                tcs.SetException(e);
            }
            Assert.Equal(TaskStatus.Faulted, tcs.Task.Status);
            Assert.NotNull(tcs.Task.Exception);
            Assert.Same(e, tcs.Task.Exception.InnerException);
            Assert.Equal(1, tcs.Task.Exception.InnerExceptions.Count);

            AssertCompletedTcsFailsToCompleteAgain(tcs);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SetException_Enumerable_CompletesSuccessfully(bool tryMethod)
        {
            var e = new Exception[] { new FormatException(), new InvalidOperationException(), new ArgumentException() };
            var tcs = new TaskCompletionSource();
            Assert.Equal(TaskStatus.WaitingForActivation, tcs.Task.Status);

            if (tryMethod)
            {
                Assert.True(tcs.TrySetException(e));
            }
            else
            {
                tcs.SetException(e);
            }
            Assert.Equal(TaskStatus.Faulted, tcs.Task.Status);
            Assert.NotNull(tcs.Task.Exception);
            Assert.Equal(e, tcs.Task.Exception.InnerExceptions);

            AssertCompletedTcsFailsToCompleteAgain(tcs);
        }

        private static void AssertCompletedTcsFailsToCompleteAgain(TaskCompletionSource tcs)
        {
            Assert.Throws<InvalidOperationException>(() => tcs.SetResult());
            Assert.False(tcs.TrySetResult());

            Assert.Throws<InvalidOperationException>(() => tcs.SetException(new Exception()));
            Assert.Throws<InvalidOperationException>(() => tcs.SetException(Enumerable.Repeat(new Exception(), 1)));
            Assert.False(tcs.TrySetException(new Exception()));
            Assert.False(tcs.TrySetException(Enumerable.Repeat(new Exception(), 1)));

            Assert.Throws<InvalidOperationException>(() => tcs.SetCanceled());
            Assert.Throws<InvalidOperationException>(() => tcs.SetCanceled(default));
            Assert.False(tcs.TrySetCanceled());
            Assert.False(tcs.TrySetCanceled(default));
        }
    }
}
