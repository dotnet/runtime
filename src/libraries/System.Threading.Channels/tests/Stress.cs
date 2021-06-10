// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Channels.Tests
{
    public class StressTests
    {
        public static IEnumerable<object[]> ReadWriteVariations_TestData()
        {
            foreach (var readDelegate in new Func<ChannelReader<int>, Task<bool>>[] { ReadSynchronous, ReadAsynchronous, ReadSyncAndAsync} )
            foreach (var writeDelegate in new Func<ChannelWriter<int>, int, Task>[] { WriteSynchronous, WriteAsynchronous, WriteSyncAndAsync} )
            foreach (bool singleReader in new [] {false, true})
            foreach (bool singleWriter in new [] {false, true})
            foreach (bool allowSynchronousContinuations in new [] {false, true})
            {
                Func<ChannelOptions, Channel<int>> unbounded = o => Channel.CreateUnbounded<int>((UnboundedChannelOptions)o);
                yield return new object[] { unbounded, new UnboundedChannelOptions
                                                            {
                                                                SingleReader = singleReader,
                                                                SingleWriter = singleWriter,
                                                                AllowSynchronousContinuations = allowSynchronousContinuations
                                                            }, readDelegate, writeDelegate
                };
            }

            foreach (var readDelegate in new Func<ChannelReader<int>, Task<bool>>[] { ReadSynchronous, ReadAsynchronous, ReadSyncAndAsync} )
            foreach (var writeDelegate in new Func<ChannelWriter<int>, int, Task>[] { WriteSynchronous, WriteAsynchronous, WriteSyncAndAsync} )
            foreach (BoundedChannelFullMode bco in Enum.GetValues(typeof(BoundedChannelFullMode)))
            foreach (int capacity in new [] { 1, 1000 })
            foreach (bool singleReader in new [] {false, true})
            foreach (bool singleWriter in new [] {false, true})
            foreach (bool allowSynchronousContinuations in new [] {false, true})
            {
                Func<ChannelOptions, Channel<int>> bounded = o => Channel.CreateBounded<int>((BoundedChannelOptions)o);
                yield return new object[] { bounded, new BoundedChannelOptions(capacity)
                                                        {
                                                            SingleReader = singleReader,
                                                            SingleWriter = singleWriter,
                                                            AllowSynchronousContinuations = allowSynchronousContinuations,
                                                            FullMode = bco
                                                            }, readDelegate, writeDelegate
                };
            }
        }

        private static async Task<bool> ReadSynchronous(ChannelReader<int> reader)
        {
            while (!reader.TryRead(out _))
            {
                if (!await reader.WaitToReadAsync())
                {
                    return false;
                }

                reader.TryPeek(out _);
            }

            return true;
        }

        private static async Task<bool> ReadAsynchronous(ChannelReader<int> reader)
        {
            if (await reader.WaitToReadAsync())
            {
                await reader.ReadAsync();
                return true;
            }

            return false;
        }

        private static async Task<bool> ReadSyncAndAsync(ChannelReader<int> reader)
        {
            if (!reader.TryRead(out int value))
            {
                if (await reader.WaitToReadAsync())
                {
                    await reader.ReadAsync();
                    return true;
                }
                return false;
            }

            return true;
        }

        private static async Task WriteSynchronous(ChannelWriter<int> writer, int value)
        {
            while (!writer.TryWrite(value))
            {
                if (!await writer.WaitToWriteAsync())
                {
                    break;
                }
            }
        }

        private static async Task WriteAsynchronous(ChannelWriter<int> writer, int value)
        {
            if (await writer.WaitToWriteAsync())
            {
                await writer.WriteAsync(value);
            }
        }

        private static async Task WriteSyncAndAsync(ChannelWriter<int> writer, int value)
        {
            if (!writer.TryWrite(value))
            {
                if (await writer.WaitToWriteAsync())
                {
                    await writer.WriteAsync(value);
                }
            }
        }

        const int MaxNumberToWriteToChannel = 400_000;
        private static readonly int MaxTaskCounts = Math.Max(2, Environment.ProcessorCount);

        [ConditionalTheory(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        [MemberData(nameof(ReadWriteVariations_TestData))]
        public void ReadWriteVariations(
            Func<ChannelOptions, Channel<int>> channelCreator,
            ChannelOptions options,
            Func<ChannelReader<int>, Task<bool>> readDelegate,
            Func<ChannelWriter<int>, int, Task> writeDelegate)
        {
            Channel<int> channel = channelCreator(options);
            ChannelReader<int> reader = channel.Reader;
            ChannelWriter<int> writer = channel.Writer;
            BoundedChannelOptions boundedOptions = options as BoundedChannelOptions;
            bool shouldReadAllWrittenValues = boundedOptions == null || boundedOptions.FullMode == BoundedChannelFullMode.Wait;

            List<Task> taskList = new List<Task>();

            int readerTasksCount;
            int writerTasksCount;

            if (options.SingleReader)
            {
                readerTasksCount = 1;
                writerTasksCount = options.SingleWriter ? 1 : MaxTaskCounts - 1;
            }
            else if (options.SingleWriter)
            {
                writerTasksCount = 1;
                readerTasksCount = MaxTaskCounts - 1;
            }
            else
            {
                readerTasksCount = MaxTaskCounts / 2;
                writerTasksCount = MaxTaskCounts - readerTasksCount;
            }

            int readCount = 0;

            for (int i=0; i < readerTasksCount; i++)
            {
                taskList.Add(Task.Run(async delegate
                {
                    try
                    {
                        while (true)
                        {
                            if (!await readDelegate(reader))
                                break;
                            Interlocked.Increment(ref readCount);
                        }
                    }
                    catch (ChannelClosedException)
                    {
                    }
                }));
            }

            int numberToWriteToQueue = -1;
            int remainingWriters = writerTasksCount;

            for (int i=0; i < writerTasksCount; i++)
            {
                taskList.Add(Task.Run(async delegate
                {
                    int num = Interlocked.Increment(ref numberToWriteToQueue);
                    while (num < MaxNumberToWriteToChannel)
                    {
                        await writeDelegate(writer, num);
                        num = Interlocked.Increment(ref numberToWriteToQueue);
                    }

                    if (Interlocked.Decrement(ref remainingWriters) == 0)
                        writer.Complete();
                }));
            }

            Task.WaitAll(taskList.ToArray());

            if (shouldReadAllWrittenValues)
            {
                Assert.Equal(MaxNumberToWriteToChannel, readCount);
            }
            else
            {
                Assert.InRange(readCount, 0, MaxNumberToWriteToChannel);
            }
        }

        public static IEnumerable<object[]> CanceledReads_TestData()
        {
            yield return new object[] { new Func<Channel<int>>(() => Channel.CreateUnbounded<int>()) };
            yield return new object[] { new Func<Channel<int>>(() => Channel.CreateUnbounded<int>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true })) };
            yield return new object[] { new Func<Channel<int>>(() => Channel.CreateBounded<int>(int.MaxValue)) };
        }

        [ConditionalTheory(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        [MemberData(nameof(CanceledReads_TestData))]
        public async Task CanceledReads(Func<Channel<int>> channelFactory)
        {
            const int Attempts = 100;
            const int Writes = 1_000;
            const int WaitTimeoutMs = 100_000;

            for (int i = 0; i < Attempts; i++)
            {
                var cts = new CancellationTokenSource();
                Channel<int> channel = channelFactory();

                // Create a bunch of reads, half of which are cancelable
                Task<int>[] reads = Enumerable.Range(0, Writes).Select(i => channel.Reader.ReadAsync(i % 2 == 0 ? cts.Token : default).AsTask()).ToArray();

                // Queue cancellation
                _ = Task.Run(() => cts.Cancel());

                // Write to complete the rest of the tasks
                for (int item = 0; item < Writes; item++)
                {
                    Assert.True(channel.Writer.TryWrite(item));
                }
                channel.Writer.Complete();

                // Wait for all the reads to complete
                try
                {
                    Assert.True(Task.WaitAll(reads, WaitTimeoutMs));
                }
                catch (AggregateException ae)
                {
                    Assert.All(ae.InnerExceptions, e => Assert.IsAssignableFrom<OperationCanceledException>(e));
                }

                // Validate all write data showed up
                int expected = 0;
                int actual = 0;
                for (int write = 0; write < Writes; write++)
                {
                    expected += write;
                    if (reads[write].Status == TaskStatus.RanToCompletion)
                    {
                        actual += reads[write].Result;
                    }
                }
                await foreach (int remaining in channel.Reader.ReadAllAsync())
                {
                    actual += remaining;
                }
                Assert.Equal(expected, actual);
            }
        }
    }
}
