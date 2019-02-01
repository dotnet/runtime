// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class SchedulerFacts
    {
        private class ThreadScheduler : PipeScheduler, IDisposable
        {
            private readonly BlockingCollection<Action> _work = new BlockingCollection<Action>();

            public ThreadScheduler()
            {
                Thread = new Thread(Work) { IsBackground = true };
                Thread.Start();
            }

            public Thread Thread { get; }

            public void Dispose()
            {
                _work.CompleteAdding();
            }

            public override void Schedule(Action<object> action, object state)
            {
                _work.Add(() => action(state));
            }

            private void Work(object state)
            {
                foreach (Action callback in _work.GetConsumingEnumerable())
                {
                    callback();
                }
            }
        }

        [Fact]
        public async Task UseSynchronizationContextFalseIgnoresSyncContextForReaderScheduler()
        {
            SynchronizationContext previous = SynchronizationContext.Current;
            var sc = new CustomSynchronizationContext();
            try
            {
                SynchronizationContext.SetSynchronizationContext(sc);

                var pipe = new Pipe(new PipeOptions(useSynchronizationContext: false));

                Func<Task> doRead = async () =>
                {
                    ReadResult result = await pipe.Reader.ReadAsync();

                    pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);

                    pipe.Reader.Complete();
                };

                // This needs to run on the current SynchronizationContext
                Task reading = doRead();

                PipeWriter buffer = pipe.Writer;
                buffer.Write(Encoding.UTF8.GetBytes("Hello World"));

                // Don't run code on our sync context (we just want to make sure the callbacks)
                // are scheduled on the sync context
                await buffer.FlushAsync().ConfigureAwait(false);

                // Nothing posted to the sync context
                Assert.Equal(0, sc.Callbacks.Count);

                pipe.Writer.Complete();

                // Don't run code on our sync context
                await reading.ConfigureAwait(false);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        [Fact]
        public async Task DefaultReaderSchedulerRunsOnSynchronizationContext()
        {
            SynchronizationContext previous = SynchronizationContext.Current;
            var sc = new CustomSynchronizationContext();
            try
            {
                SynchronizationContext.SetSynchronizationContext(sc);

                var pipe = new Pipe();

                Func<Task> doRead = async () =>
                {
                    ReadResult result = await pipe.Reader.ReadAsync();

                    pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);

                    pipe.Reader.Complete();
                };

                // This needs to run on the current SynchronizationContext
                Task reading = doRead();

                PipeWriter buffer = pipe.Writer;
                buffer.Write(Encoding.UTF8.GetBytes("Hello World"));

                // Don't run code on our sync context (we just want to make sure the callbacks)
                // are scheduled on the sync context
                await buffer.FlushAsync().ConfigureAwait(false);

                Assert.Equal(1, sc.Callbacks.Count);
                sc.Callbacks[0].Item1(sc.Callbacks[0].Item2);

                pipe.Writer.Complete();

                // Don't run code on our sync context
                await reading.ConfigureAwait(false);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        [Fact]
        public async Task DefaultReaderSchedulerIgnoresSyncContextIfConfigureAwaitFalse()
        {
            // Get off the xunit sync context

            var previous = SynchronizationContext.Current;
            try
            {
                var sc = new CustomSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(sc);

                var pipe = new Pipe();

                Func<Task> doRead = async () =>
                {
                    ReadResult result = await pipe.Reader.ReadAsync().ConfigureAwait(false);

                    Assert.True(Thread.CurrentThread.IsThreadPoolThread);

                    pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);

                    pipe.Reader.Complete();
                };

                // This needs to run on the current SynchronizationContext
                Task reading = doRead();

                PipeWriter buffer = pipe.Writer;
                buffer.Write(Encoding.UTF8.GetBytes("Hello World"));

                // We don't want to run any code on our fake sync context
                await buffer.FlushAsync().ConfigureAwait(false);

                // Nothing posted to the sync context
                Assert.Equal(0, sc.Callbacks.Count);

                pipe.Writer.Complete();

                // We don't want to run any code on our fake sync context
                await reading.ConfigureAwait(false);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }

        }

        [Fact]
        public async Task DefaultReaderSchedulerRunsOnThreadPool()
        {
            var pipe = new Pipe(new PipeOptions(useSynchronizationContext: false));

            Func<Task> doRead = async () =>
            {
                Assert.False(Thread.CurrentThread.IsThreadPoolThread, "We started on the thread pool");

                ReadResult result = await pipe.Reader.ReadAsync();

                Assert.True(Thread.CurrentThread.IsThreadPoolThread);

                pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);

                pipe.Reader.Complete();
            };

            Task reading = ExecuteOnNonThreadPoolThread(doRead);

            PipeWriter buffer = pipe.Writer;
            buffer.Write(Encoding.UTF8.GetBytes("Hello World"));
            await buffer.FlushAsync();

            pipe.Writer.Complete();

            await reading;
        }

        [Fact]
        public async Task DefaultWriterSchedulerRunsOnThreadPool()
        {
            using (var pool = new TestMemoryPool())
            {
                var pipe = new Pipe(
                    new PipeOptions(
                        pool,
                        resumeWriterThreshold: 32,
                        pauseWriterThreshold: 64,
                        useSynchronizationContext: false
                    ));

                Func<Task> doWrite = async () =>
                {
                    Assert.False(Thread.CurrentThread.IsThreadPoolThread, "We started on the thread pool");

                    PipeWriter writableBuffer = pipe.Writer.WriteEmpty(64);
                    ValueTask<FlushResult> flushAsync = writableBuffer.FlushAsync();

                    Assert.False(flushAsync.IsCompleted);

                    await flushAsync;

                    pipe.Writer.Complete();

                    Assert.True(Thread.CurrentThread.IsThreadPoolThread);
                };

                Task writing = ExecuteOnNonThreadPoolThread(doWrite);

                ReadResult result = await pipe.Reader.ReadAsync();

                pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);

                pipe.Reader.Complete();

                await writing;
            }
        }

        [Fact]
        public async Task UseSynchronizationContextFalseIgnoresSyncContextForWriterScheduler()
        {
            SynchronizationContext previous = SynchronizationContext.Current;
            var sc = new CustomSynchronizationContext();
            try
            {
                SynchronizationContext.SetSynchronizationContext(sc);

                using (var pool = new TestMemoryPool())
                {
                    var pipe = new Pipe(
                        new PipeOptions(
                            pool,
                            resumeWriterThreshold: 32,
                            pauseWriterThreshold: 64,
                            useSynchronizationContext: false
                        ));

                    Func<Task> doWrite = async () =>
                    {
                        PipeWriter writableBuffer = pipe.Writer.WriteEmpty(64);
                        ValueTask<FlushResult> flushAsync = writableBuffer.FlushAsync();

                        Assert.False(flushAsync.IsCompleted);

                        await flushAsync;

                        pipe.Writer.Complete();
                    };

                    Task writing = doWrite();

                    // Don't run on our bogus sync context
                    ReadResult result = await pipe.Reader.ReadAsync().ConfigureAwait(false);

                    pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);

                    // Nothing scheduled on the sync context
                    Assert.Equal(0, sc.Callbacks.Count);

                    pipe.Reader.Complete();

                    // Don't run on our bogus sync context
                    await writing.ConfigureAwait(false);
                }
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        [Fact]
        public async Task DefaultWriterSchedulerRunsOnSynchronizationContext()
        {
            SynchronizationContext previous = SynchronizationContext.Current;
            var sc = new CustomSynchronizationContext();
            try
            {
                SynchronizationContext.SetSynchronizationContext(sc);

                using (var pool = new TestMemoryPool())
                {
                    var pipe = new Pipe(
                        new PipeOptions(
                            pool,
                            resumeWriterThreshold: 32,
                            pauseWriterThreshold: 64
                        ));

                    Func<Task> doWrite = async () =>
                    {
                        PipeWriter writableBuffer = pipe.Writer.WriteEmpty(64);
                        ValueTask<FlushResult> flushAsync = writableBuffer.FlushAsync();

                        Assert.False(flushAsync.IsCompleted);

                        await flushAsync;

                        pipe.Writer.Complete();

                        Assert.Same(SynchronizationContext.Current, sc);
                    };

                    Task writing = doWrite();

                    // Don't run on our bogus sync context
                    ReadResult result = await pipe.Reader.ReadAsync().ConfigureAwait(false);

                    pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);

                    Assert.Equal(1, sc.Callbacks.Count);
                    sc.Callbacks[0].Item1(sc.Callbacks[0].Item2);

                    pipe.Reader.Complete();

                    // Don't run on our bogus sync context
                    await writing.ConfigureAwait(false);
                }
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        [Fact]
        public async Task DefaultWriterSchedulerIgnoresSynchronizationContext()
        {
            SynchronizationContext previous = SynchronizationContext.Current;
            var sc = new CustomSynchronizationContext();
            try
            {
                SynchronizationContext.SetSynchronizationContext(sc);

                using (var pool = new TestMemoryPool())
                {
                    var pipe = new Pipe(
                        new PipeOptions(
                            pool,
                            resumeWriterThreshold: 32,
                            pauseWriterThreshold: 64
                        ));

                    PipeWriter writableBuffer = pipe.Writer.WriteEmpty(64);
                    ValueTask<FlushResult> flushAsync = writableBuffer.FlushAsync();

                    Assert.False(flushAsync.IsCompleted);

                    Func<Task> doWrite = async () =>
                    {
                        await flushAsync.ConfigureAwait(false);

                        pipe.Writer.Complete();

                        Assert.NotSame(SynchronizationContext.Current, sc);
                    };

                    Task writing = doWrite();

                    // Don't run on our bogus sync context
                    ReadResult result = await pipe.Reader.ReadAsync().ConfigureAwait(false);

                    pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);

                    Assert.Equal(0, sc.Callbacks.Count);

                    pipe.Reader.Complete();

                    // Don't run on our bogus sync context
                    await writing.ConfigureAwait(false);
                }
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        [Fact]
        public async Task FlushCallbackRunsOnWriterScheduler()
        {
            using (var pool = new TestMemoryPool())
            {
                using (var scheduler = new ThreadScheduler())
                {
                    var pipe = new Pipe(
                        new PipeOptions(
                            pool,
                            resumeWriterThreshold: 32,
                            pauseWriterThreshold: 64,
                            readerScheduler: PipeScheduler.Inline,
                            writerScheduler: scheduler,
                            useSynchronizationContext: false));

                    PipeWriter writableBuffer = pipe.Writer.WriteEmpty(64);
                    ValueTask<FlushResult> flushAsync = writableBuffer.FlushAsync();

                    Assert.False(flushAsync.IsCompleted);

                    Func<Task> doWrite = async () =>
                    {
                        Assert.False(Thread.CurrentThread.IsThreadPoolThread, "We started on the thread pool");

                        await flushAsync;

                        pipe.Writer.Complete();

                        Assert.Equal(Thread.CurrentThread.ManagedThreadId, scheduler.Thread.ManagedThreadId);
                    };

                    Task writing = ExecuteOnNonThreadPoolThread(doWrite);

                    ReadResult result = await pipe.Reader.ReadAsync();

                    pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);

                    pipe.Reader.Complete();

                    await writing;
                }
            }
        }

        [Fact]
        public async Task ReadAsyncCallbackRunsOnReaderScheduler()
        {
            using (var pool = new TestMemoryPool())
            {
                using (var scheduler = new ThreadScheduler())
                {
                    var pipe = new Pipe(new PipeOptions(pool, scheduler, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));

                    Func<Task> doRead = async () =>
                    {
                        Assert.False(Thread.CurrentThread.IsThreadPoolThread, "We started on the thread pool");

                        ReadResult result = await pipe.Reader.ReadAsync();

                        Assert.Equal(Thread.CurrentThread.ManagedThreadId, scheduler.Thread.ManagedThreadId);

                        pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);

                        pipe.Reader.Complete();
                    };

                    Task reading = ExecuteOnNonThreadPoolThread(doRead);

                    PipeWriter buffer = pipe.Writer;
                    buffer.Write(Encoding.UTF8.GetBytes("Hello World"));
                    await buffer.FlushAsync();

                    await reading;
                }
            }
        }

        [Fact]
        public async Task ThreadPoolScheduler_SchedulesOnThreadPool()
        {
            var pipe = new Pipe(new PipeOptions(readerScheduler: PipeScheduler.ThreadPool, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));

            async Task DoRead()
            {
                // Make sure we aren't on a thread pool thread
                Assert.False(Thread.CurrentThread.IsThreadPoolThread, "We started on the thread pool");

                ValueTask<ReadResult> task = pipe.Reader.ReadAsync();

                Assert.False(task.IsCompleted, "Task completed synchronously");

                ReadResult result = await task;

                Assert.True(Thread.CurrentThread.IsThreadPoolThread);

                pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);
                pipe.Reader.Complete();
            }

            bool callbackRan = false;

            // Wait start the thread and wait for it to finish
            Task reading = ExecuteOnNonThreadPoolThread(DoRead);

            PipeWriter buffer = pipe.Writer;
            pipe.Writer.OnReaderCompleted((state, exception) =>
            {
                callbackRan = true;
                Assert.True(Thread.CurrentThread.IsThreadPoolThread);
            },
            null);

            buffer.Write(Encoding.UTF8.GetBytes("Hello World"));
            await buffer.FlushAsync();

            await reading;

            Assert.True(callbackRan);
        }

        private Task ExecuteOnNonThreadPoolThread(Func<Task> func)
        {
            // Starts the execution of a task on a non thread pool thread
            Task task = null;
            var thread = new Thread(() =>
            {
                task = func();
            });
            thread.Start();
            thread.Join();
            return task;
        }

        private sealed class CustomSynchronizationContext : SynchronizationContext
        {
            public List<Tuple<SendOrPostCallback, object>> Callbacks = new List<Tuple<SendOrPostCallback, object>>();

            public override void Post(SendOrPostCallback d, object state)
            {
                Callbacks.Add(Tuple.Create(d, state));
            }
        }
    }
}
