// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Storage.Streams;
using Xunit;

namespace System.IO
{
    public class AsWinRTStreamTests
    {
        [Fact]
        public static void AsInputStream_FromReadOnlyStream()
        {
            Stream managedStream = TestStreamProvider.CreateReadOnlyStream();
            using (IInputStream ins = managedStream.AsInputStream())
            {
                Assert.NotNull(ins);

                // Adapting a read-only managed Stream to IOutputStream must throw a NotSupportedException
                Assert.Throws<NotSupportedException>(() => { IOutputStream outs = managedStream.AsOutputStream(); });
            }
        }

        [Fact]
        public static void AsOutputStream_FromWriteOnlyStream()
        {
            Stream managedStream = TestStreamProvider.CreateWriteOnlyStream();
            using (IOutputStream outs = managedStream.AsOutputStream())
            {
                Assert.NotNull(outs);

                // Adapting a write-only managed Stream to IInputStream must throw a NotSupportedException
                Assert.Throws<NotSupportedException>(() => { IInputStream ins = managedStream.AsInputStream(); });
            }
        }

        [Fact]
        public static void AsInputStream_WrapsToSameInstance()
        {
            Stream managedStream = TestStreamProvider.CreateReadOnlyStream();
            using (IInputStream ins = managedStream.AsInputStream())
            {
                Assert.NotNull(ins);
                Assert.Same(ins, managedStream.AsInputStream());
            }
        }

        [Fact]
        public static void AsOutputStream_WrapsToSameInstance()
        {
            Stream managedStream = TestStreamProvider.CreateWriteOnlyStream();
            using (IOutputStream outs = managedStream.AsOutputStream())
            {
                Assert.NotNull(outs);
                Assert.Same(outs, managedStream.AsOutputStream());
            }
        }

        [Fact]
        public static void AsInputStream_RoundtripUnwrap()
        {
            // NetFx Stream -> IInputStream -> NetFx Stream -> roundtrip reference equality is preserved
            Stream managedStream = TestStreamProvider.CreateReadOnlyStream();
            using (IInputStream ins = managedStream.AsInputStream())
            {
                Assert.Same(managedStream, ins.AsStreamForRead());
            }
        }

        [Fact]
        public static void AsOutputStream_RoundtripUnwrap()
        {
            // NetFx Stream -> IOutputStream -> NetFx Stream -> roundtrip reference equality is preserved
            Stream managedStream = TestStreamProvider.CreateWriteOnlyStream();
            using (IOutputStream outs = managedStream.AsOutputStream())
            {
                Assert.Same(managedStream, outs.AsStreamForWrite());
            }
        }

        [Fact]
        public static void AsInputStream_Equal()
        {
            Stream stream = TestStreamProvider.CreateReadOnlyStream();

            using (IInputStream insOne = stream.AsInputStream())
            using (IInputStream insTwo = stream.AsInputStream())
            {
                Assert.Equal(insOne, insTwo);
            }
        }

        [Fact]
        public static void AsInputStream_NotEqual()
        {
            Stream streamOne = TestStreamProvider.CreateReadOnlyStream();
            Stream streamTwo = TestStreamProvider.CreateReadOnlyStream();

            Assert.NotEqual(streamOne, streamTwo);

            using (IInputStream insOne = streamOne.AsInputStream())
            using (IInputStream insTwo = streamTwo.AsInputStream())
            {
                Assert.NotEqual(insOne, insTwo);
            }
        }

        [Fact]
        public static void AsOutputStream_Equal()
        {
            Stream stream = TestStreamProvider.CreateWriteOnlyStream();

            using (IOutputStream outsOne = stream.AsOutputStream())
            using (IOutputStream outsTwo = stream.AsOutputStream())
            {
                Assert.Equal(outsOne, outsOne);
            }
        }

        [Fact]
        public static void AsOutputStream_NotEqual()
        {
            Stream streamOne = TestStreamProvider.CreateWriteOnlyStream();
            Stream streamTwo = TestStreamProvider.CreateWriteOnlyStream();

            Assert.NotEqual(streamOne, streamTwo);

            using (IOutputStream outsOne = streamOne.AsOutputStream())
            using (IOutputStream outsTwo = streamTwo.AsOutputStream())
            {
                Assert.NotEqual(outsOne, outsTwo);
            }
        }

        [Fact]
        public static void TestRead_MemoryStream_None()
        {
            DoTestRead(TestStreamProvider.CreateMemoryStreamAsInputStream, InputStreamOptions.None, mustInvokeProgressHandler: false, completesSynchronously: true);
        }

        [Fact]
        public static void TestRead_MemoryStream_Partial()
        {
            DoTestRead(TestStreamProvider.CreateMemoryStreamAsInputStream, InputStreamOptions.Partial, mustInvokeProgressHandler: false, completesSynchronously: true);
        }

        [Fact]
        public static void TestWrite_MemoryStream()
        {
            DoTestWrite(TestStreamProvider.CreateMemoryStream, mustInvokeProgressHandler: false);
        }

        private static void DoTestRead(Func<IInputStream> createStreamFunc, InputStreamOptions inputStreamOptions, bool mustInvokeProgressHandler, bool completesSynchronously)
        {

            IInputStream stream = createStreamFunc();
            IBuffer buffer = WindowsRuntimeBuffer.Create(TestStreamProvider.ModelStreamLength);

            IAsyncOperationWithProgress<IBuffer, uint> readOp = stream.ReadAsync(buffer, (uint)TestStreamProvider.ModelStreamLength, inputStreamOptions);

            if (completesSynchronously)
            {
                // New readOp for a stream where we know that reading is sycnhronous must have Status = Completed
                Assert.Equal(AsyncStatus.Completed, readOp.Status);
            }
            else
            {
                // Note the race. By the tie we get here, the status of the op may be started or already completed.
                AsyncStatus readOpStatus = readOp.Status;
                Assert.True(readOpStatus == AsyncStatus.Completed || readOpStatus == AsyncStatus.Started, "New readOp must have Status = Started or Completed (race)");
            }

            bool progressCallbackInvoked = false;
            bool completedCallbackInvoked = false;

            uint readOpId = readOp.Id;
            EventWaitHandle waitHandle = new ManualResetEvent(false);

            readOp.Progress = (asyncReadOp, bytesCompleted) =>
            {
                progressCallbackInvoked = true;

                // asyncReadOp.Id in a progress callback must match the ID of the asyncReadOp to which the callback was assigned
                Assert.Equal(readOpId, asyncReadOp.Id);

                // asyncReadOp.Status must be 'Started' for an asyncReadOp in progress
                Assert.Equal(AsyncStatus.Started, asyncReadOp.Status);

                // bytesCompleted must be in range [0, maxBytesToRead] asyncReadOp in progress
                Assert.InRange(bytesCompleted, 0u, (uint)TestStreamProvider.ModelStreamLength);
            };

            readOp.Completed = (asyncReadOp, passedStatus) =>
            {
                try
                {
                    completedCallbackInvoked = true;

                    // asyncReadOp.Id in a completion callback must match the ID of the asyncReadOp to which the callback was assigned
                    Assert.Equal(readOpId, asyncReadOp.Id);

                    // asyncReadOp.Status must match passedStatus for a completed asyncReadOp
                    Assert.Equal(passedStatus, asyncReadOp.Status);

                    // asyncReadOp.Status must be 'Completed' for a completed asyncReadOp
                    Assert.Equal(AsyncStatus.Completed, asyncReadOp.Status);

                    IBuffer resultBuffer = asyncReadOp.GetResults();

                    // asyncReadOp.GetResults() must not return null for a completed asyncReadOp
                    Assert.NotNull(resultBuffer);

                    AssertExtensions.GreaterThan(resultBuffer.Capacity, 0u, "resultBuffer.Capacity should be more than zero in completed callback");
                    AssertExtensions.GreaterThan(resultBuffer.Length, 0u, "resultBuffer.Length should be more than zero in completed callback");
                    AssertExtensions.LessThanOrEqualTo(resultBuffer.Length, resultBuffer.Capacity, "resultBuffer.Length should be <= Capacity in completed callback");

                    if (inputStreamOptions == InputStreamOptions.None)
                    {
                        // resultBuffer.Length must be equal to requested number of bytes when an asyncReadOp with
                        // InputStreamOptions.None completes successfully
                        Assert.Equal(resultBuffer.Length, (uint)TestStreamProvider.ModelStreamLength);
                    }

                    if (inputStreamOptions == InputStreamOptions.Partial)
                    {
                        AssertExtensions.LessThanOrEqualTo(resultBuffer.Length, (uint)TestStreamProvider.ModelStreamLength,
                            "resultBuffer.Length must be <= requested number of bytes with InputStreamOptions.Partial in completed callback");
                    }
                    buffer = resultBuffer;
                }
                finally
                {
                    waitHandle.Set();
                }
            };

            // Now, let's block until the read op is complete.
            // We speculate that it will complete within 3500 msec, although under high load it may not be.
            // If the test fails we should use a better way to determine if callback is really not invoked, or if it's just too slow.
            waitHandle.WaitOne(500);
            waitHandle.WaitOne(1000);
            waitHandle.WaitOne(2000);

            if (mustInvokeProgressHandler)
            {
                Assert.True(progressCallbackInvoked,
                    "Progress callback specified to ReadAsync callback must be invoked when reading from this kind of stream");
            }

            Assert.True(completedCallbackInvoked,
                "Completion callback specified to ReadAsync callback must be invoked");

            // readOp.Status must be 'Completed' for a completed async readOp
            Assert.Equal(AsyncStatus.Completed, readOp.Status);

            AssertExtensions.GreaterThan(buffer.Capacity, 0u, "buffer.Capacity should be greater than zero bytes");
            AssertExtensions.GreaterThan(buffer.Length, 0u, "buffer.Length should be greater than zero bytes");
            AssertExtensions.LessThanOrEqualTo(buffer.Length, buffer.Capacity, "buffer.Length <= buffer.Capacity is required for a completed async readOp");

            if (inputStreamOptions == InputStreamOptions.None)
            {
                // buffer.Length must be equal to requested number of bytes when an async readOp with
                //  InputStreamOptions.None completes successfully
                Assert.Equal((uint)TestStreamProvider.ModelStreamLength, buffer.Length);
            }

            if (inputStreamOptions == InputStreamOptions.Partial)
            {
                AssertExtensions.LessThanOrEqualTo(buffer.Length, (uint)TestStreamProvider.ModelStreamLength,
                    "resultBuffer.Length must be <= requested number of bytes with InputStreamOptions.Partial");
            }

            byte[] results = new byte[buffer.Length];
            buffer.CopyTo(0, results, 0, (int)buffer.Length);

            Assert.True(TestStreamProvider.CheckContent(results, 0, (int)buffer.Length),
                "Result data returned from AsyncRead must be the same as expected from the test data source");
        }

        private static void DoTestWrite(Func<Stream> createStreamFunc, bool mustInvokeProgressHandler)
        {
            Stream backingStream = createStreamFunc();
            using (IOutputStream stream = backingStream.AsOutputStream())
            {
                // Create test data
                Random rnd = new Random(20100720); //  Must be a different seed than used for TestStreamProvider.ModelStreamContents
                byte[] modelWriteData = new byte[0xA000];
                rnd.NextBytes(modelWriteData);

                // Start test

                IBuffer buffer = modelWriteData.AsBuffer();

                // ibuffer.Length for IBuffer created by Array.ToBuffer(void) must equal to array.Length
                Assert.Equal((uint)modelWriteData.Length, buffer.Length);

                // ibuffer.Capacity for IBuffer created by Array.ToBuffer(void) must equal to array.Length
                Assert.Equal((uint)modelWriteData.Length, buffer.Capacity);

                IAsyncOperationWithProgress<uint, uint> writeOp = stream.WriteAsync(buffer);

                // Note the race. By the tie we get here, the status of the op may be started or already completed.
                AsyncStatus writeOpStatus = writeOp.Status;
                Assert.True(writeOpStatus == AsyncStatus.Completed || writeOpStatus == AsyncStatus.Started, "New writeOp must have Status = Started or Completed (race)");

                uint writeOpId = writeOp.Id;
                bool progressCallbackInvoked = false;
                bool completedCallbackInvoked = false;
                uint resultBytesWritten = 0;

                EventWaitHandle waitHandle = new ManualResetEvent(false);

                writeOp.Progress = (asyncWriteOp, bytesCompleted) =>
                {
                    progressCallbackInvoked = true;

                    // asyncWriteOp.Id in a progress callback must match the ID of the asyncWriteOp to which the callback was assigned
                    Assert.Equal(writeOpId, asyncWriteOp.Id);

                    // asyncWriteOp.Status must be 'Started' for an asyncWriteOp in progress
                    Assert.Equal(AsyncStatus.Started, asyncWriteOp.Status);

                    // bytesCompleted must be in range [0, maxBytesToWrite] asyncWriteOp in progress
                    Assert.InRange(bytesCompleted, 0u, (uint)TestStreamProvider.ModelStreamLength);
                };

                writeOp.Completed = (asyncWriteOp, passedStatus) =>
                {
                    try
                    {
                        completedCallbackInvoked = true;

                        // asyncWriteOp.Id in a completion callback must match the ID of the asyncWriteOp to which the callback was assigned
                        Assert.Equal(writeOpId, asyncWriteOp.Id);

                        // asyncWriteOp.Status must match passedStatus for a completed asyncWriteOp
                        Assert.Equal(passedStatus, asyncWriteOp.Status);

                        // asyncWriteOp.Status must be 'Completed' for a completed asyncWriteOp
                        Assert.Equal(AsyncStatus.Completed, asyncWriteOp.Status);

                        uint bytesWritten = asyncWriteOp.GetResults();

                        // asyncWriteOp.GetResults() must return that all required bytes were written for a completed asyncWriteOp
                        Assert.Equal((uint)modelWriteData.Length, bytesWritten);

                        resultBytesWritten = bytesWritten;
                    }
                    finally
                    {
                        waitHandle.Set();
                    }
                };

                // Now, let's block until the write op is complete.
                // We speculate that it will complete within 3500 msec, although under high load it may not be.
                // If the test fails we should use a better way to determine if callback is really not invoked, or if it's just too slow.
                waitHandle.WaitOne(500);
                waitHandle.WaitOne(1000);
                waitHandle.WaitOne(2000);

                if (mustInvokeProgressHandler)
                {
                    Assert.True(progressCallbackInvoked,
                        "Progress callback specified to WriteAsync callback must be invoked when reading from this kind of stream");
                }

                Assert.True(completedCallbackInvoked, "Completion callback specified to WriteAsync callback must be invoked");

                // writeOp.Status must be 'Completed' for a completed async writeOp
                Assert.Equal(AsyncStatus.Completed, writeOp.Status);

                // writeOp.GetResults() must return that all required bytes were written for a completed async writeOp
                Assert.Equal((uint)modelWriteData.Length, resultBytesWritten);

                // Check contents

                backingStream.Seek(0, SeekOrigin.Begin);
                byte[] verifyBuff = new byte[modelWriteData.Length + 1024];

                int r = backingStream.Read(verifyBuff, 0, verifyBuff.Length);

                for (int i = 0; i < modelWriteData.Length; i++)
                {
                    Assert.Equal(modelWriteData[i], verifyBuff[i]);
                }
            }
        }
    }
}

