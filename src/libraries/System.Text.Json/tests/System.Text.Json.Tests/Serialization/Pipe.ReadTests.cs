// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public partial class PipeTests
    {
        [Fact]
        public async Task ReadNullArgumentFail()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await JsonSerializer.DeserializeAsync<string>((PipeReader)null));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await JsonSerializer.DeserializeAsync((PipeReader)null, (Type)null));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await JsonSerializer.DeserializeAsync((PipeReader)null, typeof(string)));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await JsonSerializer.DeserializeAsync(new Pipe().Reader, (Type)null));
        }

        [Fact]
        public async Task CompleteWriterThrowsJsonExceptionDuringDeserialize()
        {
            Pipe pipe = new Pipe();
            PipeWriter writer = pipe.Writer;
            PipeReader reader = pipe.Reader;

            // Write incomplete JSON and complete the writer (simulate disconnection)
            await writer.WriteAsync(Encoding.UTF8.GetBytes("{\"Value\": 42, \"Text\": \"Hello"));

            Task<SimpleTestClass> readTask = Serializer.DeserializeWrapper<SimpleTestClass>(reader);

            writer.Complete(); // Complete without closing the JSON

            // Attempt to deserialize should fail with JsonException due to incomplete JSON
            await Assert.ThrowsAsync<JsonException>(() => readTask);

            reader.Complete();
        }

        [Fact]
        public async Task CompleteWriterWithExceptionThrowsExceptionDuringDeserialize()
        {
            Pipe pipe = new Pipe();
            PipeWriter writer = pipe.Writer;
            PipeReader reader = pipe.Reader;

            // Write incomplete JSON and complete the writer (simulate disconnection)
            await writer.WriteAsync(Encoding.UTF8.GetBytes("{\"Value\": 42, \"Text\": \"Hello"));

            Task<SimpleTestClass> readTask = Serializer.DeserializeWrapper<SimpleTestClass>(reader);

            writer.Complete(new Exception());

            // Attempt to deserialize should fail with Exception due to Pipe completed with exception
            await Assert.ThrowsAsync<Exception>(() => readTask);

            reader.Complete();
        }

        [Fact]
        public async Task CancellationTokenPassedToDeserializeAsyncWorks()
        {
            // Setup pipe
            Pipe pipe = new Pipe();
            PipeWriter writer = pipe.Writer;
            PipeReader reader = pipe.Reader;

            // Write partial JSON data
            await writer.WriteAsync(Encoding.UTF8.GetBytes("{\"Value\":"));

            // Create a cancellation token source and cancel after starting deserialization
            using CancellationTokenSource cts = new CancellationTokenSource();

            // Start the deserializer task with the cancellation token
            Task deserializeTask = JsonSerializer.DeserializeAsync<SimpleTestClass>(reader, cancellationToken: cts.Token).AsTask();

            // Cancel the token immediately
            cts.Cancel();

            // Verify that the operation was canceled
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => deserializeTask);

            // Clean up
            writer.Complete();
            reader.Complete();
        }

        [Fact]
        public async Task CancelPendingReadThrows()
        {
            Pipe pipe = new Pipe();
            Task readTask = Serializer.DeserializeWrapper<int>(pipe.Reader);
            pipe.Reader.CancelPendingRead();

            OperationCanceledException ex = await Assert.ThrowsAsync<OperationCanceledException>(async () => await readTask);
            Assert.Equal("PipeReader.ReadAsync was canceled.", ex.Message);

            // Clean up
            pipe.Writer.Complete();
            pipe.Reader.Complete();
        }

        [Fact]
        public async Task DeserializeRelievesBackpressureWhileProcessing()
        {
            Pipe pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 10, resumeWriterThreshold: 5));

            // First write will experience backpressure
            ValueTask<FlushResult> writeTask = pipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("\"123456789"));
            Assert.False(writeTask.IsCompleted);

            Task<string> deserializeTask = Serializer.DeserializeWrapper<string>(pipe.Reader);

            // DeserializeAsync should start processing the data and relieve backpressure
            await writeTask;

            // Technically these writes could experience backpressure, but since the deserializer is reading in a loop
            // it'd be difficult to reliably observe the backpressure.
            // Do a couple writes that "would" experience backpressure, if there wasn't a reader, to verify that the deserializer
            // is processing the data.
            await pipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("1234567890"));

            await pipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("123456789\""));

            pipe.Writer.Complete();

            Assert.Equal("1234567891234567890123456789", await deserializeTask);

            pipe.Reader.Complete();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task MultiSegmentBOMIsHandled(int segmentSize)
        {
            byte[] data = [..Encoding.UTF8.GetPreamble(), ..Encoding.UTF8.GetBytes("123456789")];

            PipeReader reader = PipeReader.Create(JsonTestHelper.GetSequence(data, segmentSize));
            int result = await Serializer.DeserializeWrapper<int>(reader);
            Assert.Equal(123456789, result);
        }

        [Theory]
        [MemberData(nameof(StreamTests.NullableStructWithNullMemberTestData), MemberType = typeof(StreamTests))]
        public async Task ReadNullableStructWithNullMember_MultiSegment(Type type, bool useSourceGeneration, string expectedJson)
        {
            JsonSerializerOptions options = StreamTests.CreateNullableStructOptions(useSourceGeneration);
            byte[] data = Encoding.UTF8.GetBytes(expectedJson.Replace("}", ",\"Ignored\":null}"));

            foreach (int segmentSize in new[] { 1, 2, 3, 16 })
            {
                ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(data, segmentSize);
                Assert.False(sequence.IsSingleSegment);
                PipeReader reader = PipeReader.Create(sequence);
                try
                {
                    object result = await Serializer.DeserializeWrapper(reader, type, options);
                    Assert.Equal(expectedJson, JsonSerializer.Serialize(result, type, options));
                }
                finally
                {
                    await reader.CompleteAsync();
                }
            }
        }

        [Theory]
        [MemberData(nameof(StreamTests.NullableStructWithNullMemberTestData), MemberType = typeof(StreamTests))]
        public async Task ReadNullableStructWithNullMember_PartialBuffers(Type type, bool useSourceGeneration, string expectedJson)
        {
            JsonSerializerOptions options = StreamTests.CreateNullableStructOptions(useSourceGeneration);
            byte[] data = Encoding.UTF8.GetBytes(expectedJson.Replace("}", ",\"Ignored\":null}"));

            for (int split = 1; split < data.Length; split++)
            {
                foreach (bool completeJson in new[] { false, true })
                {
                    // Inline scheduling makes the flush/resume interleaving deterministic.
                    Pipe pipe = new(new PipeOptions(
                        readerScheduler: PipeScheduler.Inline,
                        writerScheduler: PipeScheduler.Inline,
                        pauseWriterThreshold: 0,
                        useSynchronizationContext: false));
                    try
                    {
                        Task<object> readTask = Serializer.DeserializeWrapper(pipe.Reader, type, options);
                        await pipe.Writer.WriteAsync(data.AsMemory(0, split));
                        Assert.False(readTask.IsCompleted);

                        if (completeJson)
                        {
                            for (int offset = split; offset < data.Length; offset++)
                            {
                                await pipe.Writer.WriteAsync(data.AsMemory(offset, 1));
                            }
                        }

                        await pipe.Writer.CompleteAsync();
                        if (completeJson)
                        {
                            object result = await readTask;
                            Assert.Equal(expectedJson, JsonSerializer.Serialize(result, type, options));
                        }
                        else
                        {
                            await Assert.ThrowsAsync<JsonException>(() => readTask);
                        }
                    }
                    finally
                    {
                        await pipe.Writer.CompleteAsync();
                        await pipe.Reader.CompleteAsync();
                    }
                }
            }
        }
    }
}
