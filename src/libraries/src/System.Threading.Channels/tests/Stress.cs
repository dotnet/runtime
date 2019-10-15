// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Xunit;

namespace System.Threading.Channels.Tests
{
    public class StressTests
    {
        public static IEnumerable<object[]> TestData()
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
            while (!reader.TryRead(out int value))
            {
                if (!await reader.WaitToReadAsync())
                {
                    return false;
                }
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
        [MemberData(nameof(TestData))]
        public void RunInStressMode(
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
    }
}
