// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Concurrent;
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

            public override void Schedule(Action action)
            {
                Schedule(o => ((Action)o)(), action);
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
        public async Task DefaultReaderSchedulerRunsInline()
        {
            using (var pool = new TestMemoryPool())
            {
                var pipe = new Pipe(new PipeOptions(pool));

                var id = 0;

                Func<Task> doRead = async () => {
                    ReadResult result = await pipe.Reader.ReadAsync();

                    Assert.Equal(Thread.CurrentThread.ManagedThreadId, id);

                    pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);

                    pipe.Reader.Complete();
                };

                Task reading = doRead();

                id = Thread.CurrentThread.ManagedThreadId;

                PipeWriter buffer = pipe.Writer;
                buffer.Write(Encoding.UTF8.GetBytes("Hello World"));
                await buffer.FlushAsync();

                pipe.Writer.Complete();

                await reading;
            }
        }

        [Fact]
        public async Task DefaultWriterSchedulerRunsInline()
        {
            using (var pool = new TestMemoryPool())
            {
                var pipe = new Pipe(
                    new PipeOptions(
                        pool,
                        resumeWriterThreshold: 32,
                        pauseWriterThreshold: 64
                    ));

                PipeWriter writableBuffer = pipe.Writer.WriteEmpty(64);
                PipeAwaiter<FlushResult> flushAsync = writableBuffer.FlushAsync();

                Assert.False(flushAsync.IsCompleted);

                var id = 0;

                Func<Task> doWrite = async () => {
                    await flushAsync;

                    pipe.Writer.Complete();

                    Assert.Equal(Thread.CurrentThread.ManagedThreadId, id);
                };

                Task writing = doWrite();

                ReadResult result = await pipe.Reader.ReadAsync();

                id = Thread.CurrentThread.ManagedThreadId;

                pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);

                pipe.Reader.Complete();

                await writing;
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
                            writerScheduler: scheduler));

                    PipeWriter writableBuffer = pipe.Writer.WriteEmpty(64);
                    PipeAwaiter<FlushResult> flushAsync = writableBuffer.FlushAsync();

                    Assert.False(flushAsync.IsCompleted);

                    Func<Task> doWrite = async () => {
                        int oid = Thread.CurrentThread.ManagedThreadId;

                        await flushAsync;

                        Assert.NotEqual(oid, Thread.CurrentThread.ManagedThreadId);

                        pipe.Writer.Complete();

                        Assert.Equal(Thread.CurrentThread.ManagedThreadId, scheduler.Thread.ManagedThreadId);
                    };

                    Task writing = doWrite();

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
                    var pipe = new Pipe(new PipeOptions(pool, scheduler));

                    Func<Task> doRead = async () => {
                        int oid = Thread.CurrentThread.ManagedThreadId;

                        ReadResult result = await pipe.Reader.ReadAsync();

                        Assert.NotEqual(oid, Thread.CurrentThread.ManagedThreadId);

                        Assert.Equal(Thread.CurrentThread.ManagedThreadId, scheduler.Thread.ManagedThreadId);

                        pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);

                        pipe.Reader.Complete();
                    };

                    Task reading = doRead();

                    PipeWriter buffer = pipe.Writer;
                    buffer.Write(Encoding.UTF8.GetBytes("Hello World"));
                    await buffer.FlushAsync();

                    await reading;
                }
            }
        }
    }
}
