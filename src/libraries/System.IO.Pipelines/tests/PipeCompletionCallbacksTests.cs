// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CS0618 // Type or member is obsolete

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class PipeCompletionCallbacksTests : IDisposable
    {
        public PipeCompletionCallbacksTests()
        {
            _pool = new TestMemoryPool();
        }

        public void Dispose()
        {
            _pool.Dispose();
        }

        private readonly TestMemoryPool _pool;

        private class TestScheduler : PipeScheduler
        {
            public int CallCount { get; set; }

            public Exception LastException { get; set; }

            public override void Schedule(Action<object> action, object state)
            {
                CallCount++;
                try
                {
                    action(state);
                }
                catch (Exception e)
                {
                    LastException = e;
                }
            }
        }

        [Fact]
        public void CompletingReaderFromWriterCallbackWorks()
        {
            var callbackRan = false;
            var pipe = new Pipe(new PipeOptions(_pool, pauseWriterThreshold: 5, resumeWriterThreshold: 5, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));

            pipe.Writer.OnReaderCompleted((exception, state) => { pipe.Writer.Complete(); }, null);

            pipe.Reader.OnWriterCompleted((exception, state) => { callbackRan = true; }, null);

            pipe.Reader.Complete();
            Assert.True(callbackRan);
        }

        [Fact]
        public void CompletingWriterFromReaderCallbackWorks()
        {
            var callbackRan = false;
            var pipe = new Pipe(new PipeOptions(_pool, pauseWriterThreshold: 5, resumeWriterThreshold: 5, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));

            pipe.Reader.OnWriterCompleted((exception, state) => { pipe.Reader.Complete(); }, null);

            pipe.Writer.OnReaderCompleted((exception, state) => { callbackRan = true; }, null);

            pipe.Writer.Complete();
            Assert.True(callbackRan);
        }

        [Fact]
        public void OnReaderCompletedContinuesOnException()
        {
            var callbackState1 = new object();
            var callbackState2 = new object();
            var exception1 = new Exception();
            var exception2 = new Exception();

            var counter = 0;

            var pipe = new Pipe(new PipeOptions(_pool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
            pipe.Writer.OnReaderCompleted(
                (exception, state) =>
                {
                    Assert.Equal(callbackState1, state);
                    Assert.Equal(0, counter);
                    counter++;
                    throw exception1;
                }, callbackState1);

            pipe.Writer.OnReaderCompleted(
                (exception, state) =>
                {
                    Assert.Equal(callbackState2, state);
                    Assert.Equal(1, counter);
                    counter++;
                    throw exception2;
                }, callbackState2);

            var aggregateException = Assert.Throws<AggregateException>(() => pipe.Reader.Complete());
            Assert.Equal(exception1, aggregateException.InnerExceptions[0]);
            Assert.Equal(exception2, aggregateException.InnerExceptions[1]);
            Assert.Equal(2, counter);
        }

        [Fact]
        public void OnReaderCompletedExceptionSurfacesToWriterScheduler()
        {
            var exception = new Exception();
            var scheduler = new TestScheduler();
            var pipe = new Pipe(new PipeOptions(_pool, writerScheduler: scheduler, readerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
            pipe.Writer.OnReaderCompleted((e, state) => throw exception, null);
            pipe.Reader.Complete();

            Assert.Equal(1, scheduler.CallCount);
            var aggregateException = Assert.IsType<AggregateException>(scheduler.LastException);
            Assert.Equal(aggregateException.InnerExceptions[0], exception);
        }

        [Fact]
        public void OnReaderCompletedExecutesOnSchedulerIfCompleted()
        {
            var callbackRan = false;
            var scheduler = new TestScheduler();
            var pipe = new Pipe(new PipeOptions(_pool, writerScheduler: scheduler, readerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
            pipe.Reader.Complete();

            pipe.Writer.OnReaderCompleted(
                (exception, state) =>
                {
                    Assert.Null(exception);
                    callbackRan = true;
                }, null);

            Assert.True(callbackRan);
            Assert.Equal(1, scheduler.CallCount);
        }

        [Fact]
        public void OnReaderCompletedIsDetachedDuringReset()
        {
            var callbackRan = false;
            var scheduler = new TestScheduler();
            var pipe = new Pipe(new PipeOptions(_pool, writerScheduler: scheduler, readerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
            pipe.Writer.OnReaderCompleted((exception, state) => { callbackRan = true; }, null);

            pipe.Reader.Complete();
            pipe.Writer.Complete();
            pipe.Reset();

            Assert.True(callbackRan);
            callbackRan = false;

            pipe.Reader.Complete();
            pipe.Writer.Complete();

            Assert.Equal(1, scheduler.CallCount);
            Assert.False(callbackRan);
        }

        [Fact]
        public void OnReaderCompletedPassesException()
        {
            var callbackRan = false;
            var pipe = new Pipe(new PipeOptions(_pool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
            var readerException = new Exception();

            pipe.Writer.OnReaderCompleted(
                (exception, state) =>
                {
                    callbackRan = true;
                    Assert.Same(readerException, exception);
                }, null);
            pipe.Reader.Complete(readerException);

            Assert.True(callbackRan);
        }

        [Fact]
        public void OnReaderCompletedPassesState()
        {
            var callbackRan = false;
            var callbackState = new object();
            var pipe = new Pipe(new PipeOptions(_pool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
            pipe.Writer.OnReaderCompleted(
                (exception, state) =>
                {
                    Assert.Equal(callbackState, state);
                    callbackRan = true;
                }, callbackState);
            pipe.Reader.Complete();

            Assert.True(callbackRan);
        }

        [Fact]
        public void OnReaderCompletedRanBeforeFlushContinuation()
        {
            var callbackRan = false;
            var continuationRan = false;
            var pipe = new Pipe(new PipeOptions(_pool, pauseWriterThreshold: 5, resumeWriterThreshold: 5, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));

            pipe.Writer.OnReaderCompleted(
                (exception, state) =>
                {
                    Assert.False(continuationRan);
                    callbackRan = true;
                }, null);

            PipeWriter buffer = pipe.Writer.WriteEmpty(10);
            ValueTaskAwaiter<FlushResult> awaiter = buffer.FlushAsync().GetAwaiter();

            Assert.False(awaiter.IsCompleted);
            awaiter.OnCompleted(() => { continuationRan = true; });
            pipe.Reader.Complete();

            Assert.True(callbackRan);
            pipe.Writer.Complete();
        }

        [Fact]
        public void OnReaderCompletedRunsInRegistrationOrder()
        {
            var callbackState1 = new object();
            var callbackState2 = new object();
            var callbackState3 = new object();
            var counter = 0;

            var pipe = new Pipe(new PipeOptions(_pool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
            pipe.Writer.OnReaderCompleted(
                (exception, state) =>
                {
                    Assert.Equal(callbackState1, state);
                    Assert.Equal(0, counter);
                    counter++;
                }, callbackState1);

            pipe.Writer.OnReaderCompleted(
                (exception, state) =>
                {
                    Assert.Equal(callbackState2, state);
                    Assert.Equal(1, counter);
                    counter++;
                }, callbackState2);

            pipe.Writer.OnReaderCompleted(
                (exception, state) =>
                {
                    Assert.Equal(callbackState3, state);
                    Assert.Equal(2, counter);
                    counter++;
                }, callbackState3);

            pipe.Reader.Complete();

            Assert.Equal(3, counter);
        }

        [Fact]
        public void OnReaderCompletedThrowsWithNullCallback()
        {
            var pipe = new Pipe(new PipeOptions(_pool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));

            Assert.Throws<ArgumentNullException>(() => pipe.Writer.OnReaderCompleted(null, null));
        }

        [Fact]
        public void OnReaderCompletedUsingWriterScheduler()
        {
            var callbackRan = false;
            var scheduler = new TestScheduler();
            var pipe = new Pipe(new PipeOptions(_pool, writerScheduler: scheduler, readerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
            pipe.Writer.OnReaderCompleted((exception, state) => { callbackRan = true; }, null);
            pipe.Reader.Complete();

            Assert.True(callbackRan);
            Assert.Equal(1, scheduler.CallCount);
        }

        [Fact]
        public void OnWriterCompletedContinuesOnException()
        {
            var callbackState1 = new object();
            var callbackState2 = new object();
            var exception1 = new Exception();
            var exception2 = new Exception();

            var counter = 0;

            var pipe = new Pipe(new PipeOptions(_pool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
            pipe.Reader.OnWriterCompleted(
                (exception, state) =>
                {
                    Assert.Equal(callbackState1, state);
                    Assert.Equal(0, counter);
                    counter++;
                    throw exception1;
                }, callbackState1);

            pipe.Reader.OnWriterCompleted(
                (exception, state) =>
                {
                    Assert.Equal(callbackState2, state);
                    Assert.Equal(1, counter);
                    counter++;
                    throw exception2;
                }, callbackState2);

            var aggregateException = Assert.Throws<AggregateException>(() => pipe.Writer.Complete());
            Assert.Equal(exception1, aggregateException.InnerExceptions[0]);
            Assert.Equal(exception2, aggregateException.InnerExceptions[1]);
            Assert.Equal(2, counter);
        }

        [Fact]
        public void OnWriterCompletedExceptionSurfacesToReaderScheduler()
        {
            var exception = new Exception();
            var scheduler = new TestScheduler();
            var pipe = new Pipe(new PipeOptions(_pool, scheduler, PipeScheduler.Inline, useSynchronizationContext: false));
            pipe.Reader.OnWriterCompleted((e, state) => throw exception, null);
            pipe.Writer.Complete();

            Assert.Equal(1, scheduler.CallCount);
            var aggregateException = Assert.IsType<AggregateException>(scheduler.LastException);
            Assert.Equal(aggregateException.InnerExceptions[0], exception);
        }

        [Fact]
        public void OnWriterCompletedExecutedSchedulerIfCompleted()
        {
            var callbackRan = false;
            var scheduler = new TestScheduler();
            var pipe = new Pipe(new PipeOptions(_pool, scheduler, PipeScheduler.Inline, useSynchronizationContext: false));
            pipe.Writer.Complete();

            pipe.Reader.OnWriterCompleted(
                (exception, state) =>
                {
                    Assert.Null(exception);
                    callbackRan = true;
                }, null);

            Assert.True(callbackRan);
            Assert.Equal(1, scheduler.CallCount);
        }

        [Fact]
        public void OnWriterCompletedIsDetachedDuringReset()
        {
            var callbackRan = false;
            var scheduler = new TestScheduler();
            var pipe = new Pipe(new PipeOptions(_pool, scheduler, PipeScheduler.Inline, useSynchronizationContext: false));
            pipe.Reader.OnWriterCompleted((exception, state) => { callbackRan = true; }, null);
            pipe.Reader.Complete();
            pipe.Writer.Complete();
            pipe.Reset();

            Assert.True(callbackRan);
            callbackRan = false;

            pipe.Reader.Complete();
            pipe.Writer.Complete();

            Assert.Equal(1, scheduler.CallCount);
            Assert.False(callbackRan);
        }

        [Fact]
        public void OnWriterCompletedPassesException()
        {
            var callbackRan = false;
            var pipe = new Pipe(new PipeOptions(_pool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
            var readerException = new Exception();

            pipe.Reader.OnWriterCompleted(
                (exception, state) =>
                {
                    callbackRan = true;
                    Assert.Same(readerException, exception);
                }, null);
            pipe.Writer.Complete(readerException);

            Assert.True(callbackRan);
        }

        [Fact]
        public void OnWriterCompletedPassesState()
        {
            var callbackRan = false;
            var callbackState = new object();
            var pipe = new Pipe(new PipeOptions(_pool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
            pipe.Reader.OnWriterCompleted(
                (exception, state) =>
                {
                    Assert.Equal(callbackState, state);
                    callbackRan = true;
                }, callbackState);
            pipe.Writer.Complete();

            Assert.True(callbackRan);
        }

        [Fact]
        public void OnWriterCompletedRanBeforeReadContinuation()
        {
            var callbackRan = false;
            var continuationRan = false;
            var pipe = new Pipe(new PipeOptions(_pool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));

            pipe.Reader.OnWriterCompleted(
                (exception, state) =>
                {
                    callbackRan = true;
                    Assert.False(continuationRan);
                }, null);

            ValueTask<ReadResult> awaiter = pipe.Reader.ReadAsync();
            Assert.False(awaiter.IsCompleted);
            awaiter.GetAwaiter().OnCompleted(() => { continuationRan = true; });
            pipe.Writer.Complete();

            Assert.True(callbackRan);
        }

        [Fact]
        public void OnWriterCompletedRunsInRegistrationOrder()
        {
            var callbackState1 = new object();
            var callbackState2 = new object();
            var callbackState3 = new object();
            var counter = 0;

            var pipe = new Pipe(new PipeOptions(_pool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
            pipe.Reader.OnWriterCompleted(
                (exception, state) =>
                {
                    Assert.Equal(callbackState1, state);
                    Assert.Equal(0, counter);
                    counter++;
                }, callbackState1);

            pipe.Reader.OnWriterCompleted(
                (exception, state) =>
                {
                    Assert.Equal(callbackState2, state);
                    Assert.Equal(1, counter);
                    counter++;
                }, callbackState2);

            pipe.Reader.OnWriterCompleted(
                (exception, state) =>
                {
                    Assert.Equal(callbackState3, state);
                    Assert.Equal(2, counter);
                    counter++;
                }, callbackState3);

            pipe.Writer.Complete();

            Assert.Equal(3, counter);
        }

        [Fact]
        public void OnWriterCompletedThrowsWithNullCallback()
        {
            var pipe = new Pipe(new PipeOptions(_pool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));

            Assert.Throws<ArgumentNullException>(() => pipe.Reader.OnWriterCompleted(null, null));
        }

        [Fact]
        public void OnWriterCompletedUsingReaderScheduler()
        {
            var callbackRan = false;
            var scheduler = new TestScheduler();
            var pipe = new Pipe(new PipeOptions(_pool, scheduler, PipeScheduler.Inline, useSynchronizationContext: false));
            pipe.Reader.OnWriterCompleted((exception, state) => { callbackRan = true; }, null);
            pipe.Writer.Complete();

            Assert.True(callbackRan);
            Assert.Equal(1, scheduler.CallCount);
        }
    }
}
