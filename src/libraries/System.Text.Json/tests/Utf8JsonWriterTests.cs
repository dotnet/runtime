// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Buffers;
using System.IO;
using Newtonsoft.Json;
using System.Globalization;
using System.Threading.Tasks;
using System.IO.Pipelines;

namespace System.Text.Json.Tests
{
    public class Utf8JsonWriterTests
    {
        public static bool IsX64 { get; } = IntPtr.Size >= 8;

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void NullCtor(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            Assert.Throws<ArgumentNullException>(() => new Utf8JsonWriter((Stream)null));
            Assert.Throws<ArgumentNullException>(() => new Utf8JsonWriter((IBufferWriter<byte>)null));
            Assert.Throws<ArgumentNullException>(() => new Utf8JsonWriter((Stream)null, options));
            Assert.Throws<ArgumentNullException>(() => new Utf8JsonWriter((IBufferWriter<byte>)null, options));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void CantWriteToNonWritableStream(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var stream = new MemoryStream();
            stream.Dispose();

            Assert.Throws<ArgumentException>(() => new Utf8JsonWriter(stream));
            Assert.Throws<ArgumentException>(() => new Utf8JsonWriter(stream, options));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void InitialState(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            var stream = new MemoryStream();
            var writer = new Utf8JsonWriter(stream, options);
            Assert.Equal(0, writer.BytesCommitted);
            Assert.Equal(0, writer.BytesPending);
            Assert.Equal(0, writer.CurrentDepth);
            Assert.Equal(formatted, writer.Options.Indented);
            Assert.Equal(skipValidation, writer.Options.SkipValidation);
            Assert.Equal(0, stream.Position);

            var output = new FixedSizedBufferWriter(0);
            writer = new Utf8JsonWriter(output, options);
            Assert.Equal(0, writer.BytesCommitted);
            Assert.Equal(0, writer.BytesPending);
            Assert.Equal(0, writer.CurrentDepth);
            Assert.Equal(formatted, writer.Options.Indented);
            Assert.Equal(skipValidation, writer.Options.SkipValidation);
            Assert.Equal(0, output.FormattedCount);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void Reset(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            var stream = new MemoryStream();
            var writeToStream = new Utf8JsonWriter(stream, options);
            writeToStream.WriteNumberValue(1);
            writeToStream.Flush();

            Assert.True(writeToStream.BytesCommitted != 0);

            writeToStream.Reset();
            Assert.Equal(0, writeToStream.BytesCommitted);
            Assert.Equal(0, writeToStream.BytesPending);
            Assert.Equal(0, writeToStream.CurrentDepth);
            Assert.Equal(formatted, writeToStream.Options.Indented);
            Assert.Equal(skipValidation, writeToStream.Options.SkipValidation);
            Assert.True(stream.Position != 0);

            long previousWritten = stream.Position;
            writeToStream.Flush();
            Assert.Equal(previousWritten, stream.Position);

            var output = new FixedSizedBufferWriter(256);
            var writeToIBW = new Utf8JsonWriter(output, options);
            writeToIBW.WriteNumberValue(1);
            writeToIBW.Flush();

            Assert.True(writeToIBW.BytesCommitted != 0);

            writeToIBW.Reset();
            Assert.Equal(0, writeToIBW.BytesCommitted);
            Assert.Equal(0, writeToIBW.BytesPending);
            Assert.Equal(0, writeToIBW.CurrentDepth);
            Assert.Equal(formatted, writeToIBW.Options.Indented);
            Assert.Equal(skipValidation, writeToIBW.Options.SkipValidation);
            Assert.True(output.FormattedCount != 0);

            previousWritten = output.FormattedCount;
            writeToIBW.Flush();
            Assert.Equal(previousWritten, output.FormattedCount);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void ResetWithSameOutput(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            var stream = new MemoryStream();
            var writeToStream = new Utf8JsonWriter(stream, options);
            writeToStream.WriteNumberValue(1);
            writeToStream.Flush();

            Assert.True(writeToStream.BytesCommitted != 0);

            writeToStream.Reset(stream);
            Assert.Equal(0, writeToStream.BytesCommitted);
            Assert.Equal(0, writeToStream.BytesPending);
            Assert.Equal(0, writeToStream.CurrentDepth);
            Assert.Equal(formatted, writeToStream.Options.Indented);
            Assert.Equal(skipValidation, writeToStream.Options.SkipValidation);
            Assert.True(stream.Position != 0);

            long previousWritten = stream.Position;
            writeToStream.Flush();
            Assert.Equal(previousWritten, stream.Position);

            writeToStream.WriteNumberValue(1);
            writeToStream.Flush();

            Assert.NotEqual(previousWritten, stream.Position);
            Assert.Equal("11", Encoding.UTF8.GetString(stream.ToArray()));

            var output = new FixedSizedBufferWriter(257);
            var writeToIBW = new Utf8JsonWriter(output, options);
            writeToIBW.WriteNumberValue(1);
            writeToIBW.Flush();

            Assert.True(writeToIBW.BytesCommitted != 0);

            writeToIBW.Reset(output);
            Assert.Equal(0, writeToIBW.BytesCommitted);
            Assert.Equal(0, writeToIBW.BytesPending);
            Assert.Equal(0, writeToIBW.CurrentDepth);
            Assert.Equal(formatted, writeToIBW.Options.Indented);
            Assert.Equal(skipValidation, writeToIBW.Options.SkipValidation);
            Assert.True(output.FormattedCount != 0);

            previousWritten = output.FormattedCount;
            writeToIBW.Flush();
            Assert.Equal(previousWritten, output.FormattedCount);

            writeToIBW.WriteNumberValue(1);
            writeToIBW.Flush();

            Assert.NotEqual(previousWritten, output.FormattedCount);
            Assert.Equal("11", Encoding.UTF8.GetString(output.Formatted));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void ResetChangeOutputMode(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            var stream = new MemoryStream();
            var writeToStream = new Utf8JsonWriter(stream, options);
            writeToStream.WriteNumberValue(1);
            writeToStream.Flush();

            Assert.True(writeToStream.BytesCommitted != 0);

            var output = new FixedSizedBufferWriter(256);
            writeToStream.Reset(output);
            Assert.Equal(0, writeToStream.BytesCommitted);
            Assert.Equal(0, writeToStream.BytesPending);
            Assert.Equal(0, writeToStream.CurrentDepth);
            Assert.Equal(formatted, writeToStream.Options.Indented);
            Assert.Equal(skipValidation, writeToStream.Options.SkipValidation);
            Assert.True(stream.Position != 0);

            long previousWrittenStream = stream.Position;
            long previousWrittenIBW = output.FormattedCount;
            Assert.Equal(0, previousWrittenIBW);
            writeToStream.Flush();
            Assert.Equal(previousWrittenStream, stream.Position);
            Assert.Equal(previousWrittenIBW, output.FormattedCount);

            writeToStream.WriteNumberValue(1);
            writeToStream.Flush();

            Assert.True(writeToStream.BytesCommitted != 0);
            Assert.Equal(previousWrittenStream, stream.Position);
            Assert.True(output.FormattedCount != 0);

            output = new FixedSizedBufferWriter(256);
            var writeToIBW = new Utf8JsonWriter(output, options);
            writeToIBW.WriteNumberValue(1);
            writeToIBW.Flush();

            Assert.True(writeToIBW.BytesCommitted != 0);

            stream = new MemoryStream();
            writeToIBW.Reset(stream);
            Assert.Equal(0, writeToIBW.BytesCommitted);
            Assert.Equal(0, writeToIBW.BytesPending);
            Assert.Equal(0, writeToIBW.CurrentDepth);
            Assert.Equal(formatted, writeToIBW.Options.Indented);
            Assert.Equal(skipValidation, writeToIBW.Options.SkipValidation);
            Assert.True(output.FormattedCount != 0);

            previousWrittenStream = stream.Position;
            previousWrittenIBW = output.FormattedCount;
            Assert.Equal(0, previousWrittenStream);
            writeToIBW.Flush();
            Assert.Equal(previousWrittenStream, stream.Position);
            Assert.Equal(previousWrittenIBW, output.FormattedCount);

            writeToIBW.WriteNumberValue(1);
            writeToIBW.Flush();

            Assert.True(writeToIBW.BytesCommitted != 0);
            Assert.Equal(previousWrittenIBW, output.FormattedCount);
            Assert.True(stream.Position != 0);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void InvalidReset(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            var stream = new MemoryStream();
            var writeToStream = new Utf8JsonWriter(stream, options);

            Assert.Throws<ArgumentNullException>(() => writeToStream.Reset((Stream)null));
            Assert.Throws<ArgumentNullException>(() => writeToStream.Reset((IBufferWriter<byte>)null));

            stream.Dispose();

            Assert.Throws<ArgumentException>(() => writeToStream.Reset(stream));

            var output = new FixedSizedBufferWriter(256);
            var writeToIBW = new Utf8JsonWriter(output, options);

            Assert.Throws<ArgumentNullException>(() => writeToIBW.Reset((Stream)null));
            Assert.Throws<ArgumentNullException>(() => writeToIBW.Reset((IBufferWriter<byte>)null));

            Assert.Throws<ArgumentException>(() => writeToIBW.Reset(stream));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void FlushEmpty(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new FixedSizedBufferWriter(0);

            var jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.Flush();
            Assert.Equal(0, jsonUtf8.BytesCommitted);
            Assert.Equal(0, output.FormattedCount);

            var stream = new MemoryStream();
            var writeToStream = new Utf8JsonWriter(stream, options);
            writeToStream.Flush();
            Assert.Equal(0, jsonUtf8.BytesCommitted);
            Assert.Equal(0, stream.Position);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task FlushEmptyAsync(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new FixedSizedBufferWriter(0);

            var jsonUtf8 = new Utf8JsonWriter(output, options);
            await jsonUtf8.FlushAsync();
            Assert.Equal(0, jsonUtf8.BytesCommitted);
            Assert.Equal(0, output.FormattedCount);

            var stream = new MemoryStream();
            var writeToStream = new Utf8JsonWriter(stream, options);
            await writeToStream.FlushAsync();
            Assert.Equal(0, jsonUtf8.BytesCommitted);
            Assert.Equal(0, stream.Position);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void FlushMultipleTimes(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new FixedSizedBufferWriter(256);

            var jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            jsonUtf8.WriteEndObject();
            Assert.Equal(0, jsonUtf8.BytesCommitted);
            Assert.Equal(2, jsonUtf8.BytesPending);
            Assert.Equal(0, output.FormattedCount);
            jsonUtf8.Flush();
            Assert.Equal(2, jsonUtf8.BytesCommitted);
            Assert.Equal(0, jsonUtf8.BytesPending);
            Assert.Equal(2, output.FormattedCount);
            jsonUtf8.Flush();
            Assert.Equal(2, jsonUtf8.BytesCommitted);
            Assert.Equal(0, jsonUtf8.BytesPending);
            Assert.Equal(2, output.FormattedCount);

            var stream = new MemoryStream();
            var writeToStream = new Utf8JsonWriter(stream, options);
            writeToStream.WriteStartObject();
            writeToStream.WriteEndObject();
            Assert.Equal(0, writeToStream.BytesCommitted);
            Assert.Equal(2, writeToStream.BytesPending);
            Assert.Equal(0, stream.Position);
            writeToStream.Flush();
            Assert.Equal(2, writeToStream.BytesCommitted);
            Assert.Equal(0, writeToStream.BytesPending);
            Assert.Equal(2, stream.Position);
            writeToStream.Flush();
            Assert.Equal(2, writeToStream.BytesCommitted);
            Assert.Equal(0, writeToStream.BytesPending);
            Assert.Equal(2, stream.Position);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task FlushMultipleTimesAsync(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new FixedSizedBufferWriter(256);

            var jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            jsonUtf8.WriteEndObject();
            Assert.Equal(0, jsonUtf8.BytesCommitted);
            Assert.Equal(2, jsonUtf8.BytesPending);
            Assert.Equal(0, output.FormattedCount);
            await jsonUtf8.FlushAsync();
            Assert.Equal(2, jsonUtf8.BytesCommitted);
            Assert.Equal(0, jsonUtf8.BytesPending);
            Assert.Equal(2, output.FormattedCount);
            await jsonUtf8.FlushAsync();
            Assert.Equal(2, jsonUtf8.BytesCommitted);
            Assert.Equal(0, jsonUtf8.BytesPending);
            Assert.Equal(2, output.FormattedCount);

            var stream = new MemoryStream();
            var writeToStream = new Utf8JsonWriter(stream, options);
            writeToStream.WriteStartObject();
            writeToStream.WriteEndObject();
            Assert.Equal(0, writeToStream.BytesCommitted);
            Assert.Equal(2, writeToStream.BytesPending);
            Assert.Equal(0, stream.Position);
            await writeToStream.FlushAsync();
            Assert.Equal(2, writeToStream.BytesCommitted);
            Assert.Equal(0, writeToStream.BytesPending);
            Assert.Equal(2, stream.Position);
            await writeToStream.FlushAsync();
            Assert.Equal(2, writeToStream.BytesCommitted);
            Assert.Equal(0, writeToStream.BytesPending);
            Assert.Equal(2, stream.Position);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void DisposeAutoFlushes(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new FixedSizedBufferWriter(256);

            var jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            jsonUtf8.WriteEndObject();
            Assert.Equal(0, jsonUtf8.BytesCommitted);
            Assert.Equal(0, output.FormattedCount);
            jsonUtf8.Dispose();
            Assert.Equal(0, jsonUtf8.BytesCommitted);
            Assert.Equal(2, output.FormattedCount);

            var stream = new MemoryStream();
            var writeToStream = new Utf8JsonWriter(stream, options);
            writeToStream.WriteStartObject();
            writeToStream.WriteEndObject();
            Assert.Equal(0, writeToStream.BytesCommitted);
            Assert.Equal(0, stream.Position);
            writeToStream.Dispose();
            Assert.Equal(0, writeToStream.BytesCommitted);
            Assert.Equal(2, stream.Position);
        }

#if !netstandard
        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task DisposeAutoFlushesAsync(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new FixedSizedBufferWriter(256);

            var jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            jsonUtf8.WriteEndObject();
            Assert.Equal(0, jsonUtf8.BytesCommitted);
            Assert.Equal(0, output.FormattedCount);
            await jsonUtf8.DisposeAsync();
            Assert.Equal(0, jsonUtf8.BytesCommitted);
            Assert.Equal(2, output.FormattedCount);

            var stream = new MemoryStream();
            var writeToStream = new Utf8JsonWriter(stream, options);
            writeToStream.WriteStartObject();
            writeToStream.WriteEndObject();
            Assert.Equal(0, writeToStream.BytesCommitted);
            Assert.Equal(0, stream.Position);
            await writeToStream.DisposeAsync();
            Assert.Equal(0, writeToStream.BytesCommitted);
            Assert.Equal(2, stream.Position);
        }
#endif

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void UseAfterDisposeInvalid(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new FixedSizedBufferWriter(256);

            var jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            Assert.Equal(0, jsonUtf8.BytesCommitted);
            Assert.Equal(1, jsonUtf8.BytesPending);
            Assert.Equal(0, output.FormattedCount);
            jsonUtf8.Dispose();
            Assert.Equal(0, jsonUtf8.BytesCommitted);
            Assert.Equal(0, jsonUtf8.BytesPending);
            Assert.Equal(1, output.FormattedCount);
            Assert.Throws<ObjectDisposedException>(() => jsonUtf8.Flush());
            jsonUtf8.Dispose();
            Assert.Throws<ObjectDisposedException>(() => jsonUtf8.Flush());

            Assert.Throws<ObjectDisposedException>(() => jsonUtf8.Reset());

            var stream = new MemoryStream();
            Assert.Throws<ObjectDisposedException>(() => jsonUtf8.Reset(stream));

            var writeToStream = new Utf8JsonWriter(stream, options);
            writeToStream.WriteStartObject();
            Assert.Equal(0, writeToStream.BytesCommitted);
            Assert.Equal(1, writeToStream.BytesPending);
            Assert.Equal(0, stream.Position);
            writeToStream.Dispose();
            Assert.Equal(0, writeToStream.BytesCommitted);
            Assert.Equal(0, writeToStream.BytesPending);
            Assert.Equal(1, stream.Position);
            Assert.Throws<ObjectDisposedException>(() => writeToStream.Flush());
            writeToStream.Dispose();
            Assert.Throws<ObjectDisposedException>(() => writeToStream.Flush());

            Assert.Throws<ObjectDisposedException>(() => writeToStream.Reset());

            Assert.Throws<ObjectDisposedException>(() => jsonUtf8.Reset(output));
        }

#if !netstandard
        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task UseAfterDisposeInvalidAsync(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new FixedSizedBufferWriter(256);

            var jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            Assert.Equal(0, jsonUtf8.BytesCommitted);
            Assert.Equal(1, jsonUtf8.BytesPending);
            Assert.Equal(0, output.FormattedCount);
            await jsonUtf8.DisposeAsync();
            Assert.Equal(0, jsonUtf8.BytesCommitted);
            Assert.Equal(0, jsonUtf8.BytesPending);
            Assert.Equal(1, output.FormattedCount);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => jsonUtf8.FlushAsync());
            await jsonUtf8.DisposeAsync();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => jsonUtf8.FlushAsync());

            Assert.Throws<ObjectDisposedException>(() => jsonUtf8.Reset());

            var stream = new MemoryStream();
            Assert.Throws<ObjectDisposedException>(() => jsonUtf8.Reset(stream));

            var writeToStream = new Utf8JsonWriter(stream, options);
            writeToStream.WriteStartObject();
            Assert.Equal(0, writeToStream.BytesCommitted);
            Assert.Equal(1, writeToStream.BytesPending);
            Assert.Equal(0, stream.Position);
            await writeToStream.DisposeAsync();
            Assert.Equal(0, writeToStream.BytesCommitted);
            Assert.Equal(0, writeToStream.BytesPending);
            Assert.Equal(1, stream.Position);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => writeToStream.FlushAsync());
            await writeToStream.DisposeAsync();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => writeToStream.FlushAsync());

            Assert.Throws<ObjectDisposedException>(() => writeToStream.Reset());

            Assert.Throws<ObjectDisposedException>(() => jsonUtf8.Reset(output));
        }
#endif

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void InvalidBufferWriter(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new InvalidBufferWriter();

            var jsonUtf8 = new Utf8JsonWriter(output, options);

            Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteNumberValue((ulong)12345678901));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task WriteLargeToStream(bool formatted, bool skipValidation)
        {
            var stream = new MemoryStream();
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            await WriteLargeToStreamHelper(stream, options);

            string expectedString = GetExpectedLargeString(formatted);
            string actualString = Encoding.UTF8.GetString(stream.ToArray());

            Assert.Equal(expectedString, actualString);
        }

        private static async Task WriteLargeToStreamHelper(Stream stream, JsonWriterOptions options)
        {
            const int SyncWriteThreshold = 25_000;

#if !netstandard
            await
#endif
                using var jsonUtf8 = new Utf8JsonWriter(stream, options);

            byte[] utf8String = Encoding.UTF8.GetBytes("some string 1234");

            jsonUtf8.WriteStartArray();
            for (int i = 0; i < 10_000; i++)
            {
                jsonUtf8.WriteStringValue(utf8String);
                if (jsonUtf8.BytesPending > SyncWriteThreshold)
                {
                    await jsonUtf8.FlushAsync();
                }
            }
            jsonUtf8.WriteEndArray();
        }

        private static string GetExpectedLargeString(bool prettyPrint)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None
            };

            json.WriteStartArray();
            for (int i = 0; i < 10_000; i++)
            {
                json.WriteValue("some string 1234");
            }
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void FixedSizeBufferWriter_Guid(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            int sizeTooSmall = 256;
            var output = new FixedSizedBufferWriter(sizeTooSmall);

            byte[] utf8String = Encoding.UTF8.GetBytes(new string('a', 215));

            var jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartArray();
            jsonUtf8.WriteStringValue(utf8String);
            Guid guid = Guid.NewGuid();

            Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStringValue(guid));

            sizeTooSmall += formatted ? 9 : 1;
            output = new FixedSizedBufferWriter(sizeTooSmall);
            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartArray();
            jsonUtf8.WriteStringValue(utf8String);
            jsonUtf8.WriteStringValue(guid);
            jsonUtf8.Flush();
            string actualStr = Encoding.UTF8.GetString(output.Formatted);

            if (!formatted)
            {
                Assert.Equal(257, output.Formatted.Length);
            }
            Assert.Equal($"\"{guid.ToString()}\"", actualStr.Substring(actualStr.Length - 38));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void FixedSizeBufferWriter_DateTime(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            int sizeTooSmall = 256;
            var output = new FixedSizedBufferWriter(sizeTooSmall);

            byte[] utf8String = Encoding.UTF8.GetBytes(new string('a', 232));

            var jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartArray();
            jsonUtf8.WriteStringValue(utf8String);
            var date = new DateTime(2019, 1, 1);

            Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStringValue(date));

            sizeTooSmall += formatted ? 23 : 15;
            output = new FixedSizedBufferWriter(sizeTooSmall);
            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartArray();
            jsonUtf8.WriteStringValue(utf8String);
            jsonUtf8.WriteStringValue(date);
            jsonUtf8.Flush();
            string actualStr = Encoding.UTF8.GetString(output.Formatted);

            if (!formatted)
            {
                Assert.Equal(257, output.Formatted.Length);
            }
            Assert.Equal($"\"{date.ToString("yyyy-MM-ddTHH:mm:ss")}\"", actualStr.Substring(actualStr.Length - 21));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void FixedSizeBufferWriter_DateTimeOffset(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            int sizeTooSmall = 256;
            var output = new FixedSizedBufferWriter(sizeTooSmall);

            byte[] utf8String = Encoding.UTF8.GetBytes(new string('a', 226));

            var jsonUtf8 = new Utf8JsonWriter(output, options);
            DateTimeOffset date = new DateTime(2019, 1, 1);
            jsonUtf8.WriteStartArray();
            jsonUtf8.WriteStringValue(utf8String);

            Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStringValue(date));

            sizeTooSmall += formatted ? 23 : 15;
            output = new FixedSizedBufferWriter(sizeTooSmall);
            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartArray();
            jsonUtf8.WriteStringValue(utf8String);
            jsonUtf8.WriteStringValue(date);
            jsonUtf8.Flush();
            string actualStr = Encoding.UTF8.GetString(output.Formatted);

            if (!formatted)
            {
                Assert.Equal(257, output.Formatted.Length);
            }
            Assert.Equal($"\"{date.ToString("yyyy-MM-ddTHH:mm:ssK")}\"", actualStr.Substring(actualStr.Length - 27));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void FixedSizeBufferWriter_Decimal(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var random = new Random(42);

            for (int i = 0; i < 1_000; i++)
            {
                var output = new FixedSizedBufferWriter(256);
                decimal value = JsonTestHelper.NextDecimal(random, 78E14, -78E14);

                var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteNumberValue(value);

                jsonUtf8.Flush();
                string actualStr = Encoding.UTF8.GetString(output.Formatted);

                Assert.True(output.Formatted.Length <= 31);
                Assert.Equal(decimal.Parse(actualStr, CultureInfo.InvariantCulture), value);
            }

            for (int i = 0; i < 1_000; i++)
            {
                var output = new FixedSizedBufferWriter(256);
                decimal value = JsonTestHelper.NextDecimal(random, 1_000_000, -1_000_000);
                var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteNumberValue(value);

                jsonUtf8.Flush();
                string actualStr = Encoding.UTF8.GetString(output.Formatted);

                Assert.True(output.Formatted.Length <= 31);
                Assert.Equal(decimal.Parse(actualStr, CultureInfo.InvariantCulture), value);
            }

            {
                var output = new FixedSizedBufferWriter(256);
                decimal value = 9999999999999999999999999999m;
                var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteNumberValue(value);

                jsonUtf8.Flush();
                string actualStr = Encoding.UTF8.GetString(output.Formatted);

                Assert.Equal(value.ToString().Length, output.Formatted.Length);
                Assert.Equal(decimal.Parse(actualStr, CultureInfo.InvariantCulture), value);
            }

            {
                var output = new FixedSizedBufferWriter(256);
                decimal value = -9999999999999999999999999999m;
                var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteNumberValue(value);

                jsonUtf8.Flush();
                string actualStr = Encoding.UTF8.GetString(output.Formatted);

                Assert.Equal(value.ToString().Length, output.Formatted.Length);
                Assert.Equal(decimal.Parse(actualStr, CultureInfo.InvariantCulture), value);
            }

            {

                int sizeTooSmall = 256;
                var output = new FixedSizedBufferWriter(sizeTooSmall);

                byte[] utf8String = Encoding.UTF8.GetBytes(new string('a', 222));

                decimal value = -0.9999999999999999999999999999m;
                var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteStringValue(utf8String);
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteNumberValue(value));

                sizeTooSmall += formatted ? 9 : 1;
                output = new FixedSizedBufferWriter(sizeTooSmall);
                jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteStringValue(utf8String);
                jsonUtf8.WriteNumberValue(value);

                jsonUtf8.Flush();
                string actualStr = Encoding.UTF8.GetString(output.Formatted);

                if (!formatted)
                {
                    Assert.Equal(257, output.Formatted.Length);
                }
                Assert.Equal(decimal.Parse(actualStr.Substring(actualStr.Length - 31), CultureInfo.InvariantCulture), value);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void InvalidJsonMismatch(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new ArrayBufferWriter<byte>(1024);

            var jsonUtf8 = new Utf8JsonWriter(output, options);
            if (skipValidation)
            {
                jsonUtf8.WriteEndArray();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteEndArray());
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            if (skipValidation)
            {
                jsonUtf8.WriteEndObject();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteEndObject());
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            if (skipValidation)
            {
                jsonUtf8.WriteStartArray("property at start");
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStartArray("property at start"));
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            if (skipValidation)
            {
                jsonUtf8.WriteStartObject("property at start");
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStartObject("property at start"));
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartArray();
            if (skipValidation)
            {
                jsonUtf8.WriteStartArray("property inside array");
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStartArray("property inside array"));
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            if (skipValidation)
            {
                jsonUtf8.WriteStartObject();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStartObject());
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartArray();
            if (skipValidation)
            {
                jsonUtf8.WriteEndObject();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteEndObject());
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            if (skipValidation)
            {
                jsonUtf8.WriteStringValue("key");
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStringValue("key"));
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartArray();
            if (skipValidation)
            {
                jsonUtf8.WriteString("key", "value");
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteString("key", "value"));
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            if (skipValidation)
            {
                jsonUtf8.WriteEndArray();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteEndArray());
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartArray();
            jsonUtf8.WriteStartArray();
            jsonUtf8.WriteEndArray();
            if (skipValidation)
            {
                jsonUtf8.WriteEndObject();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteEndObject());
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            jsonUtf8.WriteStartObject("some object");
            jsonUtf8.WriteEndObject();
            if (skipValidation)
            {
                jsonUtf8.WriteEndArray();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteEndArray());
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartArray();
            if (skipValidation)
            {
                jsonUtf8.WriteStartObject("some object");
                jsonUtf8.WriteEndObject();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStartObject("some object"));
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteEndObject());
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            jsonUtf8.WriteStartArray("test array");
            jsonUtf8.WriteEndArray();
            if (skipValidation)
            {
                jsonUtf8.WriteEndArray();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteEndArray());
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartArray();
            jsonUtf8.WriteEndArray();
            if (skipValidation)
            {
                jsonUtf8.WriteEndArray();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteEndArray());
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            jsonUtf8.WriteEndObject();
            if (skipValidation)
            {
                jsonUtf8.WriteEndObject();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteEndObject());
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartArray();
            jsonUtf8.WriteStartArray();
            if (skipValidation)
            {
                jsonUtf8.WriteEndObject();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteEndObject());
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            jsonUtf8.WriteStartObject("test object");
            if (skipValidation)
            {
                jsonUtf8.WriteEndArray();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteEndArray());
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void InvalidJsonIncomplete(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new ArrayBufferWriter<byte>(1024);

            var jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartArray();
            Assert.True(jsonUtf8.CurrentDepth != 0);

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            Assert.True(jsonUtf8.CurrentDepth != 0);

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartArray();
            jsonUtf8.WriteStartArray();
            jsonUtf8.WriteEndArray();
            Assert.True(jsonUtf8.CurrentDepth != 0);

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            jsonUtf8.WriteStartObject("some object");
            Assert.True(jsonUtf8.CurrentDepth != 0);

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartArray();
            jsonUtf8.WriteStartObject();
            jsonUtf8.WriteEndObject();
            Assert.True(jsonUtf8.CurrentDepth != 0);

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            jsonUtf8.WriteStartArray("test array");
            jsonUtf8.WriteEndArray();
            Assert.True(jsonUtf8.CurrentDepth != 0);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void InvalidJsonPrimitive(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new ArrayBufferWriter<byte>(1024);

            var jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteNumberValue(12345);
            if (skipValidation)
            {
                jsonUtf8.WriteNumberValue(12345);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteNumberValue(12345));
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteNumberValue(12345);
            if (skipValidation)
            {
                jsonUtf8.WriteStartArray();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStartArray());
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteNumberValue(12345);
            if (skipValidation)
            {
                jsonUtf8.WriteStartObject();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStartObject());
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteNumberValue(12345);
            if (skipValidation)
            {
                jsonUtf8.WriteStartArray("property name");
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStartArray("property name"));
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteNumberValue(12345);
            if (skipValidation)
            {
                jsonUtf8.WriteStartObject("property name");
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStartObject("property name"));
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteNumberValue(12345);
            if (skipValidation)
            {
                jsonUtf8.WriteString("property name", "value");
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteString("property name", "value"));
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteNumberValue(12345);
            if (skipValidation)
            {
                jsonUtf8.WriteEndArray();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteEndArray());
            }

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteNumberValue(12345);
            if (skipValidation)
            {
                jsonUtf8.WriteEndObject();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteEndObject());
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void InvalidNumbersJson(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new ArrayBufferWriter<byte>(1024);

            var jsonUtf8 = new Utf8JsonWriter(output, options);
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumberValue(double.NegativeInfinity));

            jsonUtf8 = new Utf8JsonWriter(output, options);
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumberValue(double.PositiveInfinity));

            jsonUtf8 = new Utf8JsonWriter(output, options);
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumberValue(double.NaN));

            jsonUtf8 = new Utf8JsonWriter(output, options);
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumberValue(float.PositiveInfinity));

            jsonUtf8 = new Utf8JsonWriter(output, options);
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumberValue(float.NegativeInfinity));

            jsonUtf8 = new Utf8JsonWriter(output, options);
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumberValue(float.NaN));

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumber("name", double.NegativeInfinity));

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumber("name", double.PositiveInfinity));

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumber("name", double.NaN));

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumber("name", float.PositiveInfinity));

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumber("name", float.NegativeInfinity));

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumber("name", float.NaN));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void InvalidJsonContinueShouldSucceed(bool formatted)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = true };
            var output = new ArrayBufferWriter<byte>(1024);

            var jsonUtf8 = new Utf8JsonWriter(output, options);

            for (int i = 0; i < 100; i++)
            {
                jsonUtf8.WriteEndArray();
            }
            jsonUtf8.WriteStartArray();
            jsonUtf8.WriteEndArray();
            jsonUtf8.Flush();

            var sb = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                if (formatted)
                    sb.Append(Environment.NewLine);
                sb.Append("]");
            }
            sb.Append(",");
            if (formatted)
                sb.Append(Environment.NewLine);
            sb.Append("[]");

            AssertContents(sb.ToString(), output);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WritingTooDeep(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new ArrayBufferWriter<byte>(1024);

            var jsonUtf8 = new Utf8JsonWriter(output, options);

            for (int i = 0; i < 1000; i++)
            {
                jsonUtf8.WriteStartArray();
            }
            Assert.Equal(1000, jsonUtf8.CurrentDepth);
            Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStartArray());
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WritingTooDeepProperty(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new ArrayBufferWriter<byte>(1024);

            var jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            for (int i = 0; i < 999; i++)
            {
                jsonUtf8.WriteStartObject("name");
            }
            Assert.Equal(1000, jsonUtf8.CurrentDepth);
            Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStartArray("name"));

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            for (int i = 0; i < 999; i++)
            {
                jsonUtf8.WriteStartObject(Encoding.UTF8.GetBytes("name"));
            }
            Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStartArray(Encoding.UTF8.GetBytes("name")));
        }

        [ConditionalTheory(nameof(IsX64))]
        [OuterLoop]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WritingTooLargeProperty(bool formatted, bool skipValidation)
        {
            byte[] key;
            char[] keyChars;

            try
            {
                key = new byte[1_000_000_000];
                keyChars = new char[1_000_000_000];
            }
            catch (OutOfMemoryException)
            {
                return;
            }

            key.AsSpan().Fill((byte)'a');
            keyChars.AsSpan().Fill('a');

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new ArrayBufferWriter<byte>(1024);

            var jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteStartArray(keyChars));

            jsonUtf8 = new Utf8JsonWriter(output, options);
            jsonUtf8.WriteStartObject();
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteStartArray(key));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteSingleValue(bool formatted, bool skipValidation)
        {
            string expectedStr = "123456789012345";

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            var output = new ArrayBufferWriter<byte>(1024);
            var jsonUtf8 = new Utf8JsonWriter(output, options);

            jsonUtf8.WriteNumberValue(123456789012345);

            jsonUtf8.Flush();

            AssertContents(expectedStr, output);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteHelloWorld(bool formatted, bool skipValidation)
        {
            string propertyName = "message";
            string value = "Hello, World!";
            string expectedStr = GetHelloWorldExpectedString(prettyPrint: formatted, propertyName, value);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            for (int i = 0; i < 9; i++)
            {
                var output = new ArrayBufferWriter<byte>(32);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteString("message", "Hello, World!");
                        jsonUtf8.WriteString("message", "Hello, World!");
                        break;
                    case 1:
                        jsonUtf8.WriteString("message", "Hello, World!".AsSpan());
                        jsonUtf8.WriteString("message", "Hello, World!".AsSpan());
                        break;
                    case 2:
                        jsonUtf8.WriteString("message", Encoding.UTF8.GetBytes("Hello, World!"));
                        jsonUtf8.WriteString("message", Encoding.UTF8.GetBytes("Hello, World!"));
                        break;
                    case 3:
                        jsonUtf8.WriteString("message".AsSpan(), "Hello, World!");
                        jsonUtf8.WriteString("message".AsSpan(), "Hello, World!");
                        break;
                    case 4:
                        jsonUtf8.WriteString("message".AsSpan(), "Hello, World!".AsSpan());
                        jsonUtf8.WriteString("message".AsSpan(), "Hello, World!".AsSpan());
                        break;
                    case 5:
                        jsonUtf8.WriteString("message".AsSpan(), Encoding.UTF8.GetBytes("Hello, World!"));
                        jsonUtf8.WriteString("message".AsSpan(), Encoding.UTF8.GetBytes("Hello, World!"));
                        break;
                    case 6:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes("message"), "Hello, World!");
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes("message"), "Hello, World!");
                        break;
                    case 7:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes("message"), "Hello, World!".AsSpan());
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes("message"), "Hello, World!".AsSpan());
                        break;
                    case 8:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes("message"), Encoding.UTF8.GetBytes("Hello, World!"));
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes("message"), Encoding.UTF8.GetBytes("Hello, World!"));
                        break;
                }

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteHelloWorldEscaped(bool formatted, bool skipValidation)
        {
            string propertyName = "mess><age";
            string value = "Hello,>< World!";
            string expectedStr = GetHelloWorldExpectedString(prettyPrint: formatted, propertyName, value);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            ReadOnlySpan<char> propertyNameSpan = propertyName.AsSpan();
            ReadOnlySpan<char> valueSpan = value.AsSpan();
            ReadOnlySpan<byte> propertyNameSpanUtf8 = Encoding.UTF8.GetBytes(propertyName);
            ReadOnlySpan<byte> valueSpanUtf8 = Encoding.UTF8.GetBytes(value);

            for (int i = 0; i < 9; i++)
            {
                var output = new ArrayBufferWriter<byte>(32);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteString(propertyName, value);
                        jsonUtf8.WriteString(propertyName, value);
                        break;
                    case 1:
                        jsonUtf8.WriteString(propertyName, valueSpan);
                        jsonUtf8.WriteString(propertyName, valueSpan);
                        break;
                    case 2:
                        jsonUtf8.WriteString(propertyName, valueSpanUtf8);
                        jsonUtf8.WriteString(propertyName, valueSpanUtf8);
                        break;
                    case 3:
                        jsonUtf8.WriteString(propertyNameSpan, value);
                        jsonUtf8.WriteString(propertyNameSpan, value);
                        break;
                    case 4:
                        jsonUtf8.WriteString(propertyNameSpan, valueSpan);
                        jsonUtf8.WriteString(propertyNameSpan, valueSpan);
                        break;
                    case 5:
                        jsonUtf8.WriteString(propertyNameSpan, valueSpanUtf8);
                        jsonUtf8.WriteString(propertyNameSpan, valueSpanUtf8);
                        break;
                    case 6:
                        jsonUtf8.WriteString(propertyNameSpanUtf8, value);
                        jsonUtf8.WriteString(propertyNameSpanUtf8, value);
                        break;
                    case 7:
                        jsonUtf8.WriteString(propertyNameSpanUtf8, valueSpan);
                        jsonUtf8.WriteString(propertyNameSpanUtf8, valueSpan);
                        break;
                    case 8:
                        jsonUtf8.WriteString(propertyNameSpanUtf8, valueSpanUtf8);
                        jsonUtf8.WriteString(propertyNameSpanUtf8, valueSpanUtf8);
                        break;
                }

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }

            // Verify that escaping does not change the input strings/spans.
            Assert.Equal("mess><age", propertyName);
            Assert.Equal("Hello,>< World!", value);
            Assert.True(propertyName.AsSpan().SequenceEqual(propertyNameSpan));
            Assert.True(value.AsSpan().SequenceEqual(valueSpan));
            Assert.True(Encoding.UTF8.GetBytes(propertyName).AsSpan().SequenceEqual(propertyNameSpanUtf8));
            Assert.True(Encoding.UTF8.GetBytes(value).AsSpan().SequenceEqual(valueSpanUtf8));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WritePartialHelloWorld(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            var output = new ArrayBufferWriter<byte>(10);
            var jsonUtf8 = new Utf8JsonWriter(output, options);

            jsonUtf8.WriteStartObject();

            Assert.Equal(0, jsonUtf8.BytesCommitted);
            Assert.Equal(1, jsonUtf8.BytesPending);

            jsonUtf8.WriteString("message", "Hello, World!");

            Assert.Equal(0, jsonUtf8.BytesCommitted);
            if (formatted)
                Assert.Equal(26 + 2 + Environment.NewLine.Length + 1, jsonUtf8.BytesPending); // new lines, indentation, white space
            else
                Assert.Equal(26, jsonUtf8.BytesPending);

            jsonUtf8.Flush();

            if (formatted)
                Assert.Equal(26 + 2 + Environment.NewLine.Length + 1, jsonUtf8.BytesCommitted); // new lines, indentation, white space
            else
                Assert.Equal(26, jsonUtf8.BytesCommitted);

            Assert.Equal(0, jsonUtf8.BytesPending);

            jsonUtf8.WriteString("message", "Hello, World!");
            jsonUtf8.WriteEndObject();

            if (formatted)
                Assert.Equal(26 + 2 + Environment.NewLine.Length + 1, jsonUtf8.BytesCommitted);
            else
                Assert.Equal(26, jsonUtf8.BytesCommitted);

            if (formatted)
                Assert.Equal(27 + 2 + (2 * Environment.NewLine.Length) + 1, jsonUtf8.BytesPending); // new lines, indentation, white space
            else
                Assert.Equal(27, jsonUtf8.BytesPending);

            jsonUtf8.Flush();

            if (formatted)
                Assert.Equal(53 + (2 * 2) + (3 * Environment.NewLine.Length) + (1 * 2), jsonUtf8.BytesCommitted); // new lines, indentation, white space
            else
                Assert.Equal(53, jsonUtf8.BytesCommitted);

            Assert.Equal(0, jsonUtf8.BytesPending);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteInvalidPartialJson(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new ArrayBufferWriter<byte>(10);
            var jsonUtf8 = new Utf8JsonWriter(output, options);

            jsonUtf8.WriteStartObject();

            Assert.Equal(0, jsonUtf8.BytesCommitted);
            Assert.Equal(1, jsonUtf8.BytesPending);

            jsonUtf8.Flush();

            Assert.Equal(1, jsonUtf8.BytesCommitted);
            Assert.Equal(0, jsonUtf8.BytesPending);

            if (skipValidation)
            {
                jsonUtf8.WriteStringValue("Hello, World!");
                jsonUtf8.WriteEndArray();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStringValue("Hello, World!"));
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteEndArray());
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteInvalidDepthPartial(bool formatted, bool skipValidation)
        {
            {
                var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
                var output = new ArrayBufferWriter<byte>(10);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                Assert.Equal(0, jsonUtf8.CurrentDepth);

                if (skipValidation)
                {
                    jsonUtf8.WriteStartObject();
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStartObject());
                }
            }

            {
                var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
                var output = new ArrayBufferWriter<byte>(10);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                if (skipValidation)
                {
                    jsonUtf8.WriteStartObject("name");
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStartObject("name"));
                }
            }
        }

        [Theory]
        [InlineData(true, true, "comment")]
        [InlineData(true, false, "comment")]
        [InlineData(false, true, "comment")]
        [InlineData(false, false, "comment")]
        [InlineData(true, true, "comm><ent")]
        [InlineData(true, false, "comm><ent")]
        [InlineData(false, true, "comm><ent")]
        [InlineData(false, false, "comm><ent")]
        [InlineData(true, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(true, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        public void WriteComments(bool formatted, bool skipValidation, string comment)
        {
            string expectedStr = GetCommentExpectedString(prettyPrint: formatted, comment);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            for (int i = 0; i < 3; i++)
            {
                var output = new ArrayBufferWriter<byte>(32);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartArray();

                for (int j = 0; j < 10; j++)
                {
                    WriteCommentValue(jsonUtf8, i, comment);
                }

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteStringValue(comment);
                        break;
                    case 1:
                        jsonUtf8.WriteStringValue(comment.AsSpan());
                        break;
                    case 2:
                        jsonUtf8.WriteStringValue(Encoding.UTF8.GetBytes(comment));
                        break;
                }

                WriteCommentValue(jsonUtf8, i, comment);

                jsonUtf8.WriteEndArray();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }
        }

        private static void WriteCommentValue(Utf8JsonWriter jsonUtf8, int i, string comment)
        {
            switch (i)
            {
                case 0:
                    jsonUtf8.WriteCommentValue(comment);
                    break;
                case 1:
                    jsonUtf8.WriteCommentValue(comment.AsSpan());
                    break;
                case 2:
                    jsonUtf8.WriteCommentValue(Encoding.UTF8.GetBytes(comment));
                    break;
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteInvalidComment(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            var output = new ArrayBufferWriter<byte>(32);
            var jsonUtf8 = new Utf8JsonWriter(output, options);

            string comment = "comment is */ invalid";

            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteCommentValue(comment));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteCommentValue(comment.AsSpan()));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteCommentValue(Encoding.UTF8.GetBytes(comment)));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteCommentsInvalidTextAllowed(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            var output = new ArrayBufferWriter<byte>(32);
            var jsonUtf8 = new Utf8JsonWriter(output, options);

            string comment = "comment is * / valid";
            jsonUtf8.WriteCommentValue(comment);
            jsonUtf8.WriteCommentValue(comment.AsSpan());
            jsonUtf8.WriteCommentValue(Encoding.UTF8.GetBytes(comment));

            comment = "comment is /* valid";
            jsonUtf8.WriteCommentValue(comment);
            jsonUtf8.WriteCommentValue(comment.AsSpan());
            jsonUtf8.WriteCommentValue(Encoding.UTF8.GetBytes(comment));

            comment = "comment is / * valid even with unpaired surrogate \udc00 this part no longer visible";
            jsonUtf8.WriteCommentValue(comment);
            jsonUtf8.WriteCommentValue(comment.AsSpan());

            jsonUtf8.Flush();

            // Explicitly skipping flushing here
            var invalidUtf8 = new byte[2] { 0xc3, 0x28 };
            jsonUtf8.WriteCommentValue(invalidUtf8);

            string expectedStr = GetCommentExpectedString(prettyPrint: formatted);
            AssertContents(expectedStr, output);
        }

        private static string GetCommentExpectedString(bool prettyPrint)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            };

            string comment = "comment is * / valid";
            json.WriteComment(comment);
            json.WriteComment(comment);
            json.WriteComment(comment);

            comment = "comment is /* valid";
            json.WriteComment(comment);
            json.WriteComment(comment);
            json.WriteComment(comment);

            comment = "comment is / * valid even with unpaired surrogate ";
            json.WriteComment(comment);
            json.WriteComment(comment);

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteStrings(bool formatted, bool skipValidation)
        {
            string value = "temp";
            string expectedStr = GetStringsExpectedString(prettyPrint: formatted, value);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            for (int i = 0; i < 3; i++)
            {
                var output = new ArrayBufferWriter<byte>(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartArray();

                for (int j = 0; j < 10; j++)
                {
                    switch (i)
                    {
                        case 0:
                            jsonUtf8.WriteStringValue(value);
                            break;
                        case 1:
                            jsonUtf8.WriteStringValue(value.AsSpan());
                            break;
                        case 2:
                            jsonUtf8.WriteStringValue(Encoding.UTF8.GetBytes(value));
                            break;
                    }
                }

                jsonUtf8.WriteEndArray();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }
        }

        [Theory]
        [InlineData(true, true, "mess\nage", "Hello, \nWorld!")]
        [InlineData(true, false, "mess\nage", "Hello, \nWorld!")]
        [InlineData(false, true, "mess\nage", "Hello, \nWorld!")]
        [InlineData(false, false, "mess\nage", "Hello, \nWorld!")]
        [InlineData(true, true, "message", "Hello, \nWorld!")]
        [InlineData(true, false, "message", "Hello, \nWorld!")]
        [InlineData(false, true, "message", "Hello, \nWorld!")]
        [InlineData(false, false, "message", "Hello, \nWorld!")]
        [InlineData(true, true, "mess\nage", "Hello, World!")]
        [InlineData(true, false, "mess\nage", "Hello, World!")]
        [InlineData(false, true, "mess\nage", "Hello, World!")]
        [InlineData(false, false, "mess\nage", "Hello, World!")]
        [InlineData(true, true, "message", "Hello, World!")]
        [InlineData(true, false, "message", "Hello, World!")]
        [InlineData(false, true, "message", "Hello, World!")]
        [InlineData(false, false, "message", "Hello, World!")]
        [InlineData(true, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>mess\nage", ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>Hello, \nWorld!")]
        [InlineData(true, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>mess\nage", ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>Hello, \nWorld!")]
        [InlineData(false, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>mess\nage", ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>Hello, \nWorld!")]
        [InlineData(false, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>mess\nage", ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>Hello, \nWorld!")]
        public void WriteHelloWorldEscaped(bool formatted, bool skipValidation, string key, string value)
        {
            string expectedStr = GetEscapedExpectedString(prettyPrint: formatted, key, value, StringEscapeHandling.EscapeHtml);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            for (int i = 0; i < 9; i++)
            {
                var output = new ArrayBufferWriter<byte>(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteString(key, value);
                        break;
                    case 1:
                        jsonUtf8.WriteString(key.AsSpan(), value.AsSpan());
                        break;
                    case 2:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value));
                        break;
                    case 3:
                        jsonUtf8.WriteString(key, value.AsSpan());
                        break;
                    case 4:
                        jsonUtf8.WriteString(key, Encoding.UTF8.GetBytes(value));
                        break;
                    case 5:
                        jsonUtf8.WriteString(key.AsSpan(), value);
                        break;
                    case 6:
                        jsonUtf8.WriteString(key.AsSpan(), Encoding.UTF8.GetBytes(value));
                        break;
                    case 7:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes(key), value);
                        break;
                    case 8:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes(key), value.AsSpan());
                        break;
                }

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void EscapeAsciiCharacters(bool formatted, bool skipValidation)
        {
            var propertyArray = new char[128];

            char[] specialCases = { '+', '`', (char)0x7F };
            for (int i = 0; i < propertyArray.Length; i++)
            {
                if (Array.IndexOf(specialCases, (char)i) != -1)
                {
                    propertyArray[i] = (char)0;
                }
                else
                {
                    propertyArray[i] = (char)i;
                }
            }

            string propertyName = new string(propertyArray);
            string value = new string(propertyArray);

            string expectedStr = GetEscapedExpectedString(prettyPrint: formatted, propertyName, value, StringEscapeHandling.EscapeHtml);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            for (int i = 0; i < 2; i++)
            {
                var output = new ArrayBufferWriter<byte>(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteString(propertyName, value);
                        break;
                    case 1:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes(propertyName), Encoding.UTF8.GetBytes(value));
                        break;
                }

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void EscapeCharacters(bool formatted, bool skipValidation)
        {
            // Do not include surrogate pairs.
            var propertyArray = new char[0xD800 + (0xFFFF - 0xE000) + 1];

            for (int i = 128; i < propertyArray.Length; i++)
            {
                if (i < 0xD800 || i > 0xDFFF)
                {
                    propertyArray[i] = (char)i;
                }
            }

            string propertyName = new string(propertyArray);
            string value = new string(propertyArray);

            string expectedStr = GetEscapedExpectedString(prettyPrint: formatted, propertyName, value, StringEscapeHandling.EscapeNonAscii);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            for (int i = 0; i < 2; i++)
            {
                var output = new ArrayBufferWriter<byte>(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteString(propertyName, value);
                        break;
                    case 1:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes(propertyName), Encoding.UTF8.GetBytes(value));
                        break;
                }

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void EscapeSurrogatePairs(bool formatted, bool skipValidation)
        {
            var propertyArray = new char[10] { 'a', (char)0xD800, (char)0xDC00, (char)0xD803, (char)0xDE6D, (char)0xD834, (char)0xDD1E, (char)0xDBFF, (char)0xDFFF, 'a' };

            string propertyName = new string(propertyArray);
            string value = new string(propertyArray);

            string expectedStr = GetEscapedExpectedString(prettyPrint: formatted, propertyName, value, StringEscapeHandling.EscapeNonAscii);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            for (int i = 0; i < 2; i++)
            {
                var output = new ArrayBufferWriter<byte>(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteString(propertyName, value);
                        break;
                    case 1:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes(propertyName), Encoding.UTF8.GetBytes(value));
                        break;
                }

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void InvalidUTF8(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            var output = new ArrayBufferWriter<byte>(1024);
            var jsonUtf8 = new Utf8JsonWriter(output, options);

            var validUtf8 = new byte[2] { 0xc3, 0xb1 }; // 0xF1
            var invalidUtf8 = new byte[2] { 0xc3, 0x28 };

            jsonUtf8.WriteStartObject();
            for (int i = 0; i < 4; i++)
            {
                switch (i)
                {
                    case 0:
                        Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(invalidUtf8, invalidUtf8));
                        break;
                    case 1:
                        Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(invalidUtf8, validUtf8));
                        break;
                    case 2:
                        Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(validUtf8, invalidUtf8));
                        break;
                    case 3:
                        jsonUtf8.WriteString(validUtf8, validUtf8);
                        break;
                }
            }
            jsonUtf8.WriteEndObject();
            jsonUtf8.Flush();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void InvalidUTF16(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            var output = new ArrayBufferWriter<byte>(1024);
            var jsonUtf8 = new Utf8JsonWriter(output, options);

            var validUtf16 = new char[2] { (char)0xD801, (char)0xDC37 }; // 0x10437
            var invalidUtf16 = new char[2] { (char)0xD801, 'a' };

            jsonUtf8.WriteStartObject();
            for (int i = 0; i < 4; i++)
            {
                switch (i)
                {
                    case 0:
                        Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(invalidUtf16, invalidUtf16));
                        break;
                    case 1:
                        Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(invalidUtf16, validUtf16));
                        break;
                    case 2:
                        Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(validUtf16, invalidUtf16));
                        break;
                    case 3:
                        jsonUtf8.WriteString(validUtf16, validUtf16);
                        break;
                }
            }
            jsonUtf8.WriteEndObject();
            jsonUtf8.Flush();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteCustomStrings(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new ArrayBufferWriter<byte>(10);
            var jsonUtf8 = new Utf8JsonWriter(output, options);

            jsonUtf8.WriteStartObject();

            for (int i = 0; i < 1_000; i++)
            {
                jsonUtf8.WriteString("message", "Hello, World!");
            }

            jsonUtf8.WriteEndObject();
            jsonUtf8.Flush();

            AssertContents(GetCustomExpectedString(formatted), output);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteStartEnd(bool formatted, bool skipValidation)
        {
            string expectedStr = GetStartEndExpectedString(prettyPrint: formatted);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new ArrayBufferWriter<byte>(1024);

            var jsonUtf8 = new Utf8JsonWriter(output, options);

            jsonUtf8.WriteStartArray();
            jsonUtf8.WriteStartObject();
            jsonUtf8.WriteEndObject();
            jsonUtf8.WriteEndArray();
            jsonUtf8.Flush();

            AssertContents(expectedStr, output);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WriteStartEndInvalid(bool formatted)
        {
            {
                string expectedStr = "[}";

                var options = new JsonWriterOptions { Indented = formatted, SkipValidation = true };
                var output = new ArrayBufferWriter<byte>(1024);

                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }

            {
                string expectedStr = "{]";

                var options = new JsonWriterOptions { Indented = formatted, SkipValidation = true };
                var output = new ArrayBufferWriter<byte>(1024);

                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteEndArray();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteStartEndWithPropertyNameArray(bool formatted, bool skipValidation)
        {
            string expectedStr = GetStartEndWithPropertyArrayExpectedString(prettyPrint: formatted);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            for (int i = 0; i < 3; i++)
            {
                var output = new ArrayBufferWriter<byte>(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteStartArray("property name");
                        break;
                    case 1:
                        jsonUtf8.WriteStartArray("property name".AsSpan());
                        break;
                    case 2:
                        jsonUtf8.WriteStartArray(Encoding.UTF8.GetBytes("property name"));
                        break;
                }

                jsonUtf8.WriteEndArray();
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }
        }

        [Theory]
        [InlineData(true, true, 10)]
        [InlineData(true, false, 10)]
        [InlineData(false, true, 10)]
        [InlineData(false, false, 10)]
        [InlineData(true, true, 100)]
        [InlineData(true, false, 100)]
        [InlineData(false, true, 100)]
        [InlineData(false, false, 100)]
        public void WriteStartEndWithPropertyNameArray(bool formatted, bool skipValidation, int keyLength)
        {
            var keyChars = new char[keyLength];
            for (int i = 0; i < keyChars.Length; i++)
            {
                keyChars[i] = '<';
            }
            var key = new string(keyChars);

            string expectedStr = GetStartEndWithPropertyArrayExpectedString(key, prettyPrint: formatted, escape: true);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            for (int i = 0; i < 3; i++)
            {
                var output = new ArrayBufferWriter<byte>(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteStartArray(key);
                        break;
                    case 1:
                        jsonUtf8.WriteStartArray(key.AsSpan());
                        break;
                    case 2:
                        jsonUtf8.WriteStartArray(Encoding.UTF8.GetBytes(key));
                        break;
                }

                jsonUtf8.WriteEndArray();
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteStartEndWithPropertyNameObject(bool formatted, bool skipValidation)
        {
            string expectedStr = GetStartEndWithPropertyObjectExpectedString(prettyPrint: formatted);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            for (int i = 0; i < 3; i++)
            {
                var output = new ArrayBufferWriter<byte>(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteStartObject("property name");
                        break;
                    case 1:
                        jsonUtf8.WriteStartObject("property name".AsSpan());
                        break;
                    case 2:
                        jsonUtf8.WriteStartObject(Encoding.UTF8.GetBytes("property name"));
                        break;
                }

                jsonUtf8.WriteEndObject();
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }
        }

        [Theory]
        [InlineData(true, true, 10)]
        [InlineData(true, false, 10)]
        [InlineData(false, true, 10)]
        [InlineData(false, false, 10)]
        [InlineData(true, true, 100)]
        [InlineData(true, false, 100)]
        [InlineData(false, true, 100)]
        [InlineData(false, false, 100)]
        public void WriteStartEndWithPropertyNameObject(bool formatted, bool skipValidation, int keyLength)
        {
            var keyChars = new char[keyLength];
            for (int i = 0; i < keyChars.Length; i++)
            {
                keyChars[i] = '<';
            }
            var key = new string(keyChars);

            string expectedStr = GetStartEndWithPropertyObjectExpectedString(key, prettyPrint: formatted, escape: true);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            for (int i = 0; i < 3; i++)
            {
                var output = new ArrayBufferWriter<byte>(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteStartObject(key);
                        break;
                    case 1:
                        jsonUtf8.WriteStartObject(key.AsSpan());
                        break;
                    case 2:
                        jsonUtf8.WriteStartObject(Encoding.UTF8.GetBytes(key));
                        break;
                }

                jsonUtf8.WriteEndObject();
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteArrayWithProperty(bool formatted, bool skipValidation)
        {
            string expectedStr = GetArrayWithPropertyExpectedString(prettyPrint: formatted);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            for (int i = 0; i < 3; i++)
            {
                var output = new ArrayBufferWriter<byte>(1024);

                var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteStartArray("message");
                        break;
                    case 1:
                        jsonUtf8.WriteStartArray("message".AsSpan());
                        break;
                    case 2:
                        jsonUtf8.WriteStartArray(Encoding.UTF8.GetBytes("message"));
                        break;
                }

                jsonUtf8.WriteEndArray();
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }
        }

        [Theory]
        [InlineData(true, true, true, "message")]
        [InlineData(true, false, true, "message")]
        [InlineData(false, true, true, "message")]
        [InlineData(false, false, true, "message")]
        [InlineData(true, true, true, "mess><age")]
        [InlineData(true, false, true, "mess><age")]
        [InlineData(false, true, true, "mess><age")]
        [InlineData(false, false, true, "mess><age")]
        [InlineData(true, true, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(true, false, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, true, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, false, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(true, true, false, "message")]
        [InlineData(true, false, false, "message")]
        [InlineData(false, true, false, "message")]
        [InlineData(false, false, false, "message")]
        [InlineData(true, true, false, "mess><age")]
        [InlineData(true, false, false, "mess><age")]
        [InlineData(false, true, false, "mess><age")]
        [InlineData(false, false, false, "mess><age")]
        [InlineData(true, true, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(true, false, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, true, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, false, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        public void WriteBooleanValue(bool formatted, bool skipValidation, bool value, string keyString)
        {
            string expectedStr = GetBooleanExpectedString(prettyPrint: formatted, keyString, value, escape: true);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            for (int i = 0; i < 3; i++)
            {
                var output = new ArrayBufferWriter<byte>(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteBoolean(keyString, value);
                        break;
                    case 1:
                        jsonUtf8.WriteBoolean(keyString.AsSpan(), value);
                        break;
                    case 2:
                        jsonUtf8.WriteBoolean(Encoding.UTF8.GetBytes(keyString), value);
                        break;
                }

                jsonUtf8.WriteStartArray("temp");
                jsonUtf8.WriteBooleanValue(true);
                jsonUtf8.WriteBooleanValue(true);
                jsonUtf8.WriteBooleanValue(false);
                jsonUtf8.WriteBooleanValue(false);
                jsonUtf8.WriteEndArray();

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }
        }

        [Theory]
        [InlineData(true, true, "message")]
        [InlineData(true, false, "message")]
        [InlineData(false, true, "message")]
        [InlineData(false, false, "message")]
        [InlineData(true, true, "mess><age")]
        [InlineData(true, false, "mess><age")]
        [InlineData(false, true, "mess><age")]
        [InlineData(false, false, "mess><age")]
        [InlineData(true, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(true, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        public void WriteNullValue(bool formatted, bool skipValidation, string keyString)
        {
            string expectedStr = GetNullExpectedString(prettyPrint: formatted, keyString, escape: true);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            for (int i = 0; i < 3; i++)
            {
                var output = new ArrayBufferWriter<byte>(16);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteNull(keyString);
                        jsonUtf8.WriteNull(keyString);
                        break;
                    case 1:
                        jsonUtf8.WriteNull(keyString.AsSpan());
                        jsonUtf8.WriteNull(keyString.AsSpan());
                        break;
                    case 2:
                        jsonUtf8.WriteNull(Encoding.UTF8.GetBytes(keyString));
                        jsonUtf8.WriteNull(Encoding.UTF8.GetBytes(keyString));
                        break;
                }

                jsonUtf8.WriteStartArray("temp");
                jsonUtf8.WriteNullValue();
                jsonUtf8.WriteNullValue();
                jsonUtf8.WriteEndArray();

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }
        }

        [Theory]
        [InlineData(true, true, 0)]
        [InlineData(true, false, 0)]
        [InlineData(false, true, 0)]
        [InlineData(false, false, 0)]
        [InlineData(true, true, -1)]
        [InlineData(true, false, -1)]
        [InlineData(false, true, -1)]
        [InlineData(false, false, -1)]
        [InlineData(true, true, 1)]
        [InlineData(true, false, 1)]
        [InlineData(false, true, 1)]
        [InlineData(false, false, 1)]
        [InlineData(true, true, int.MaxValue)]
        [InlineData(true, false, int.MaxValue)]
        [InlineData(false, true, int.MaxValue)]
        [InlineData(false, false, int.MaxValue)]
        [InlineData(true, true, int.MinValue)]
        [InlineData(true, false, int.MinValue)]
        [InlineData(false, true, int.MinValue)]
        [InlineData(false, false, int.MinValue)]
        [InlineData(true, true, 12345)]
        [InlineData(true, false, 12345)]
        [InlineData(false, true, 12345)]
        [InlineData(false, false, 12345)]
        public void WriteIntegerValue(bool formatted, bool skipValidation, int value)
        {
            string expectedStr = GetIntegerExpectedString(prettyPrint: formatted, value);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            for (int i = 0; i < 3; i++)
            {
                var output = new ArrayBufferWriter<byte>(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteNumber("message", value);
                        break;
                    case 1:
                        jsonUtf8.WriteNumber("message".AsSpan(), value);
                        break;
                    case 2:
                        jsonUtf8.WriteNumber(Encoding.UTF8.GetBytes("message"), value);
                        break;
                }

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }
        }

        [Theory]
        [InlineData(true, true, "message")]
        [InlineData(true, false, "message")]
        [InlineData(false, true, "message")]
        [InlineData(false, false, "message")]
        [InlineData(true, true, "mess><age")]
        [InlineData(true, false, "mess><age")]
        [InlineData(false, true, "mess><age")]
        [InlineData(false, false, "mess><age")]
        [InlineData(true, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(true, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        public void WriteNumbers(bool formatted, bool skipValidation, string keyString)
        {
            var random = new Random(42);
            const int numberOfItems = 1_000;

            var ints = new int[numberOfItems];
            ints[0] = 0;
            ints[1] = int.MaxValue;
            ints[2] = int.MinValue;
            ints[3] = 12345;
            ints[4] = -12345;
            for (int i = 5; i < numberOfItems; i++)
            {
                ints[i] = random.Next(int.MinValue, int.MaxValue);
            }

            var uints = new uint[numberOfItems];
            uints[0] = uint.MaxValue;
            uints[1] = uint.MinValue;
            uints[2] = 3294967295;
            for (int i = 3; i < numberOfItems; i++)
            {
                uint thirtyBits = (uint)random.Next(1 << 30);
                uint twoBits = (uint)random.Next(1 << 2);
                uint fullRange = (thirtyBits << 2) | twoBits;
                uints[i] = fullRange;
            }

            var longs = new long[numberOfItems];
            longs[0] = 0;
            longs[1] = long.MaxValue;
            longs[2] = long.MinValue;
            longs[3] = 12345678901;
            longs[4] = -12345678901;
            for (int i = 5; i < numberOfItems; i++)
            {
                long value = random.Next(int.MinValue, int.MaxValue);
                value += value < 0 ? int.MinValue : int.MaxValue;
                longs[i] = value;
            }

            var ulongs = new ulong[numberOfItems];
            ulongs[0] = ulong.MaxValue;
            ulongs[1] = ulong.MinValue;
            ulongs[2] = 10446744073709551615;
            for (int i = 3; i < numberOfItems; i++)
            {

            }

            var doubles = new double[numberOfItems * 2];
            doubles[0] = 0.00;
            doubles[1] = double.MaxValue;
            doubles[2] = double.MinValue;
            doubles[3] = 12.345e1;
            doubles[4] = -123.45e1;
            for (int i = 5; i < numberOfItems; i++)
            {
                var value = random.NextDouble();
                if (value < 0.5)
                {
                    doubles[i] = random.NextDouble() * double.MinValue;
                }
                else
                {
                    doubles[i] = random.NextDouble() * double.MaxValue;
                }
            }

            for (int i = numberOfItems; i < numberOfItems * 2; i++)
            {
                var value = random.NextDouble();
                if (value < 0.5)
                {
                    doubles[i] = random.NextDouble() * -1_000_000;
                }
                else
                {
                    doubles[i] = random.NextDouble() * 1_000_000;
                }
            }

            var floats = new float[numberOfItems];
            floats[0] = 0.00f;
            floats[1] = float.MaxValue;
            floats[2] = float.MinValue;
            floats[3] = 12.345e1f;
            floats[4] = -123.45e1f;
            for (int i = 5; i < numberOfItems; i++)
            {
                double mantissa = (random.NextDouble() * 2.0) - 1.0;
                double exponent = Math.Pow(2.0, random.Next(-126, 128));
                floats[i] = (float)(mantissa * exponent);
            }

            var decimals = new decimal[numberOfItems * 2];
            decimals[0] = (decimal)0.00;
            decimals[1] = decimal.MaxValue;
            decimals[2] = decimal.MinValue;
            decimals[3] = (decimal)12.345e1;
            decimals[4] = (decimal)-123.45e1;
            for (int i = 5; i < numberOfItems; i++)
            {
                var value = random.NextDouble();
                if (value < 0.5)
                {
                    decimals[i] = (decimal)(random.NextDouble() * -78E14);
                }
                else
                {
                    decimals[i] = (decimal)(random.NextDouble() * 78E14);
                }
            }

            for (int i = numberOfItems; i < numberOfItems * 2; i++)
            {
                var value = random.NextDouble();
                if (value < 0.5)
                {
                    decimals[i] = (decimal)(random.NextDouble() * -1_000_000);
                }
                else
                {
                    decimals[i] = (decimal)(random.NextDouble() * 1_000_000);
                }
            }

            string expectedStr = GetNumbersExpectedString(prettyPrint: formatted, keyString, ints, uints, longs, ulongs, floats, doubles, decimals, escape: false);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            for (int j = 0; j < 3; j++)
            {
                var output = new ArrayBufferWriter<byte>(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                ReadOnlySpan<char> keyUtf16 = keyString.AsSpan();
                ReadOnlySpan<byte> keyUtf8 = Encoding.UTF8.GetBytes(keyString);

                jsonUtf8.WriteStartObject();

                switch (j)
                {
                    case 0:
                        for (int i = 0; i < floats.Length; i++)
                            jsonUtf8.WriteNumber(keyString, floats[i]);
                        for (int i = 0; i < ints.Length; i++)
                            jsonUtf8.WriteNumber(keyString, ints[i]);
                        for (int i = 0; i < uints.Length; i++)
                            jsonUtf8.WriteNumber(keyString, uints[i]);
                        for (int i = 0; i < doubles.Length; i++)
                            jsonUtf8.WriteNumber(keyString, doubles[i]);
                        for (int i = 0; i < longs.Length; i++)
                            jsonUtf8.WriteNumber(keyString, longs[i]);
                        for (int i = 0; i < ulongs.Length; i++)
                            jsonUtf8.WriteNumber(keyString, ulongs[i]);
                        for (int i = 0; i < decimals.Length; i++)
                            jsonUtf8.WriteNumber(keyString, decimals[i]);
                        jsonUtf8.WriteStartArray(keyString);
                        break;
                    case 1:
                        for (int i = 0; i < floats.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, floats[i]);
                        for (int i = 0; i < ints.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, ints[i]);
                        for (int i = 0; i < uints.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, uints[i]);
                        for (int i = 0; i < doubles.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, doubles[i]);
                        for (int i = 0; i < longs.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, longs[i]);
                        for (int i = 0; i < ulongs.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, ulongs[i]);
                        for (int i = 0; i < decimals.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, decimals[i]);
                        jsonUtf8.WriteStartArray(keyUtf16);
                        break;
                    case 2:
                        for (int i = 0; i < floats.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, floats[i]);
                        for (int i = 0; i < ints.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, ints[i]);
                        for (int i = 0; i < uints.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, uints[i]);
                        for (int i = 0; i < doubles.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, doubles[i]);
                        for (int i = 0; i < longs.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, longs[i]);
                        for (int i = 0; i < ulongs.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, ulongs[i]);
                        for (int i = 0; i < decimals.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, decimals[i]);
                        jsonUtf8.WriteStartArray(keyUtf8);
                        break;
                }

                jsonUtf8.WriteNumberValue(floats[0]);
                jsonUtf8.WriteNumberValue(ints[0]);
                jsonUtf8.WriteNumberValue(uints[0]);
                jsonUtf8.WriteNumberValue(doubles[0]);
                jsonUtf8.WriteNumberValue(longs[0]);
                jsonUtf8.WriteNumberValue(ulongs[0]);
                jsonUtf8.WriteNumberValue(decimals[0]);
                jsonUtf8.WriteEndArray();

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                // TODO: The output doesn't match what JSON.NET does (different rounding/e-notation).
                // AssertContents(expectedStr, output);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteNumberValueInt32(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            var output = new ArrayBufferWriter<byte>();
            var jsonUtf8 = new Utf8JsonWriter(output, options);

            int numberOfElements = 0;
            jsonUtf8.WriteStartArray();
            int currentCapactiy = output.Capacity;
            while (currentCapactiy == output.Capacity)
            {
                jsonUtf8.WriteNumberValue(1234567);
                numberOfElements++;
            }
            Assert.Equal(currentCapactiy + 4096, output.Capacity);
            jsonUtf8.WriteEndArray();
            jsonUtf8.Flush();

            string expectedStr = GetNumbersExpectedString(formatted, numberOfElements);
            AssertContents(expectedStr, output);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteNumberValueInt64(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            var output = new ArrayBufferWriter<byte>();
            var jsonUtf8 = new Utf8JsonWriter(output, options);

            int numberOfElements = 0;
            jsonUtf8.WriteStartArray();
            int currentCapactiy = output.Capacity;
            while (currentCapactiy == output.Capacity)
            {
                jsonUtf8.WriteNumberValue((long)1234567);
                numberOfElements++;
            }
            Assert.Equal(currentCapactiy + 4096, output.Capacity);
            jsonUtf8.WriteEndArray();
            jsonUtf8.Flush();

            string expectedStr = GetNumbersExpectedString(formatted, numberOfElements);
            AssertContents(expectedStr, output);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteNumberValueUInt32(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            var output = new ArrayBufferWriter<byte>();
            var jsonUtf8 = new Utf8JsonWriter(output, options);

            int numberOfElements = 0;
            jsonUtf8.WriteStartArray();
            int currentCapactiy = output.Capacity;
            while (currentCapactiy == output.Capacity)
            {
                jsonUtf8.WriteNumberValue((uint)1234567);
                numberOfElements++;
            }
            Assert.Equal(currentCapactiy + 4096, output.Capacity);
            jsonUtf8.WriteEndArray();
            jsonUtf8.Flush();

            string expectedStr = GetNumbersExpectedString(formatted, numberOfElements);
            AssertContents(expectedStr, output);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteNumberValueUInt64(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            var output = new ArrayBufferWriter<byte>();
            var jsonUtf8 = new Utf8JsonWriter(output, options);

            int numberOfElements = 0;
            jsonUtf8.WriteStartArray();
            int currentCapactiy = output.Capacity;
            while (currentCapactiy == output.Capacity)
            {
                jsonUtf8.WriteNumberValue((ulong)1234567);
                numberOfElements++;
            }
            Assert.Equal(currentCapactiy + 4096, output.Capacity);
            jsonUtf8.WriteEndArray();
            jsonUtf8.Flush();

            string expectedStr = GetNumbersExpectedString(formatted, numberOfElements);
            AssertContents(expectedStr, output);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteNumberValueSingle(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            var output = new ArrayBufferWriter<byte>();
            var jsonUtf8 = new Utf8JsonWriter(output, options);

            int numberOfElements = 0;
            jsonUtf8.WriteStartArray();
            int currentCapactiy = output.Capacity;
            while (currentCapactiy == output.Capacity)
            {
                jsonUtf8.WriteNumberValue((float)1234567);
                numberOfElements++;
            }
            Assert.Equal(currentCapactiy + 4096, output.Capacity);
            jsonUtf8.WriteEndArray();
            jsonUtf8.Flush();

            string expectedStr = GetNumbersExpectedString(formatted, numberOfElements);
            AssertContents(expectedStr, output);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteNumberValueDouble(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            var output = new ArrayBufferWriter<byte>();
            var jsonUtf8 = new Utf8JsonWriter(output, options);

            int numberOfElements = 0;
            jsonUtf8.WriteStartArray();
            int currentCapactiy = output.Capacity;
            while (currentCapactiy == output.Capacity)
            {
                jsonUtf8.WriteNumberValue((double)1234567);
                numberOfElements++;
            }
            Assert.Equal(currentCapactiy + 4096, output.Capacity);
            jsonUtf8.WriteEndArray();
            jsonUtf8.Flush();

            string expectedStr = GetNumbersExpectedString(formatted, numberOfElements);
            AssertContents(expectedStr, output);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteNumberValueDecimal(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            var output = new ArrayBufferWriter<byte>();
            var jsonUtf8 = new Utf8JsonWriter(output, options);

            int numberOfElements = 0;
            jsonUtf8.WriteStartArray();
            int currentCapactiy = output.Capacity;
            while (currentCapactiy == output.Capacity)
            {
                jsonUtf8.WriteNumberValue((decimal)1234567);
                numberOfElements++;
            }
            Assert.Equal(currentCapactiy + 4096, output.Capacity);
            jsonUtf8.WriteEndArray();
            jsonUtf8.Flush();

            string expectedStr = GetNumbersExpectedString(formatted, numberOfElements);
            AssertContents(expectedStr, output);
        }

        [Theory]
        [InlineData(true, true, "message")]
        [InlineData(true, false, "message")]
        [InlineData(false, true, "message")]
        [InlineData(false, false, "message")]
        [InlineData(true, true, "mess><age")]
        [InlineData(true, false, "mess><age")]
        [InlineData(false, true, "mess><age")]
        [InlineData(false, false, "mess><age")]
        [InlineData(true, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(true, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        public void WriteGuidsValue(bool formatted, bool skipValidation, string keyString)
        {
            const int numberOfItems = 1_000;

            var guids = new Guid[numberOfItems];
            for (int i = 0; i < numberOfItems; i++)
            {
                guids[i] = Guid.NewGuid();
            }

            string expectedStr = GetGuidsExpectedString(prettyPrint: formatted, keyString, guids, escape: true);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            ReadOnlySpan<char> keyUtf16 = keyString.AsSpan();
            ReadOnlySpan<byte> keyUtf8 = Encoding.UTF8.GetBytes(keyString);

            for (int i = 0; i < 3; i++)
            {
                var output = new ArrayBufferWriter<byte>(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyString, guids[j]);
                        jsonUtf8.WriteStartArray(keyString);
                        break;
                    case 1:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyUtf16, guids[j]);
                        jsonUtf8.WriteStartArray(keyUtf16);
                        break;
                    case 2:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyUtf8, guids[j]);
                        jsonUtf8.WriteStartArray(keyUtf8);
                        break;
                }

                jsonUtf8.WriteStringValue(guids[0]);
                jsonUtf8.WriteStringValue(guids[1]);
                jsonUtf8.WriteEndArray();

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }
        }

        [Theory]
        [InlineData(true, true, "message")]
        [InlineData(true, false, "message")]
        [InlineData(false, true, "message")]
        [InlineData(false, false, "message")]
        [InlineData(true, true, "mess><age")]
        [InlineData(true, false, "mess><age")]
        [InlineData(false, true, "mess><age")]
        [InlineData(false, false, "mess><age")]
        [InlineData(true, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(true, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        public void WriteDateTimesValue(bool formatted, bool skipValidation, string keyString)
        {
            var random = new Random(42);
            const int numberOfItems = 1_000;

            var start = new DateTime(1995, 1, 1);
            int range = (DateTime.Today - start).Days;

            var dates = new DateTime[numberOfItems];
            for (int i = 0; i < numberOfItems; i++)
                dates[i] = start.AddDays(random.Next(range));

            string expectedStr = GetDatesExpectedString(prettyPrint: formatted, keyString, dates, escape: true);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            ReadOnlySpan<char> keyUtf16 = keyString.AsSpan();
            ReadOnlySpan<byte> keyUtf8 = Encoding.UTF8.GetBytes(keyString);

            for (int i = 0; i < 3; i++)
            {
                var output = new ArrayBufferWriter<byte>(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyString, dates[j]);
                        jsonUtf8.WriteStartArray(keyString);
                        break;
                    case 1:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyUtf16, dates[j]);
                        jsonUtf8.WriteStartArray(keyUtf16);
                        break;
                    case 2:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyUtf8, dates[j]);
                        jsonUtf8.WriteStartArray(keyUtf8);
                        break;
                }

                jsonUtf8.WriteStringValue(dates[0]);
                jsonUtf8.WriteStringValue(dates[1]);
                jsonUtf8.WriteEndArray();

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }
        }

        [Theory]
        [InlineData(true, true, "message")]
        [InlineData(true, false, "message")]
        [InlineData(false, true, "message")]
        [InlineData(false, false, "message")]
        [InlineData(true, true, "mess><age")]
        [InlineData(true, false, "mess><age")]
        [InlineData(false, true, "mess><age")]
        [InlineData(false, false, "mess><age")]
        [InlineData(true, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(true, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        public void WriteDateTimeOffsetsValue(bool formatted, bool skipValidation, string keyString)
        {
            var random = new Random(42);
            const int numberOfItems = 1_000;

            var start = new DateTime(1995, 1, 1);
            int range = (DateTime.Today - start).Days;

            var dates = new DateTimeOffset[numberOfItems];
            for (int i = 0; i < numberOfItems; i++)
                dates[i] = new DateTimeOffset(start.AddDays(random.Next(range)));

            string expectedStr = GetDatesExpectedString(prettyPrint: formatted, keyString, dates, escape: true);

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            ReadOnlySpan<char> keyUtf16 = keyString.AsSpan();
            ReadOnlySpan<byte> keyUtf8 = Encoding.UTF8.GetBytes(keyString);

            for (int i = 0; i < 3; i++)
            {
                var output = new ArrayBufferWriter<byte>(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, options);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyString, dates[j]);
                        jsonUtf8.WriteStartArray(keyString);
                        break;
                    case 1:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyUtf16, dates[j]);
                        jsonUtf8.WriteStartArray(keyUtf16);
                        break;
                    case 2:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyUtf8, dates[j]);
                        jsonUtf8.WriteStartArray(keyUtf8);
                        break;
                }

                jsonUtf8.WriteStringValue(dates[0]);
                jsonUtf8.WriteStringValue(dates[1]);
                jsonUtf8.WriteEndArray();

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                AssertContents(expectedStr, output);
            }
        }

        [ConditionalTheory(nameof(IsX64))]
        [OuterLoop]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteLargeKeyOrValue(bool formatted, bool skipValidation)
        {
            byte[] key;
            byte[] value;

            try
            {
                key = new byte[1_000_000_001];
                value = new byte[1_000_000_001];
            }
            catch (OutOfMemoryException)
            {
                return;
            }

            key.AsSpan().Fill((byte)'a');
            value.AsSpan().Fill((byte)'b');

            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };
            var output = new ArrayBufferWriter<byte>(1024);

            {
                var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartObject();
                Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(key, DateTime.Now));
                Assert.Equal(0, output.WrittenCount);
            }

            {
                var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartArray();
                Assert.Throws<ArgumentException>(() => jsonUtf8.WriteStringValue(value));
                Assert.Equal(0, output.WrittenCount);
            }
        }

        [ConditionalTheory(nameof(IsX64))]
        [OuterLoop]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteLargeKeyValue(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            Span<byte> key;
            Span<byte> value;

            try
            {
                key = new byte[1_000_000_001];
                value = new byte[1_000_000_001];
            }
            catch (OutOfMemoryException)
            {
                return;
            }

            WriteTooLargeHelper(options, key, value);
            WriteTooLargeHelper(options, key.Slice(0, 1_000_000_000), value);
            WriteTooLargeHelper(options, key, value.Slice(0, 1_000_000_000));
            WriteTooLargeHelper(options, key.Slice(0, 10_000_000 / 3), value.Slice(0, 10_000_000 / 3), noThrow: true);
        }

        [Theory]
        [MemberData(nameof(JsonDateTimeTestData.DateTimeFractionTrimBaseTests), MemberType = typeof(JsonDateTimeTestData))]
        [MemberData(nameof(JsonDateTimeTestData.DateTimeFractionTrimUtcOffsetTests), MemberType = typeof(JsonDateTimeTestData))]
        public void WriteDateTime_TrimsFractionCorrectly(string testStr, string expectedStr)
        {
            var output = new ArrayBufferWriter<byte>(1024);
            using var jsonUtf8 = new Utf8JsonWriter(output);

            jsonUtf8.WriteStringValue(DateTime.ParseExact(testStr, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
            jsonUtf8.Flush();

            AssertContents($"\"{expectedStr}\"", output);
        }

        [Theory]
        [MemberData(nameof(JsonDateTimeTestData.DateTimeOffsetFractionTrimTests), MemberType = typeof(JsonDateTimeTestData))]
        public void WriteDateTimeOffset_TrimsFractionCorrectly(string testStr, string expectedStr)
        {
            var output = new ArrayBufferWriter<byte>(1024);
            using var jsonUtf8 = new Utf8JsonWriter(output);

            jsonUtf8.WriteStringValue(DateTimeOffset.ParseExact(testStr, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
            jsonUtf8.Flush();

            AssertContents($"\"{expectedStr}\"", output);
        }

        [Fact]
        public void WriteDateTime_TrimsFractionCorrectly_SerializerRoundtrip()
        {
            DateTime utcNow = DateTime.UtcNow;
            Assert.Equal(utcNow, Serialization.JsonSerializer.Parse(Serialization.JsonSerializer.ToBytes(utcNow), typeof(DateTime)));
        }

        private static void WriteTooLargeHelper(JsonWriterOptions options, ReadOnlySpan<byte> key, ReadOnlySpan<byte> value, bool noThrow = false)
        {
            // Resizing is too slow, even for outerloop tests, so initialize to a large output size up front.
            var output = new ArrayBufferWriter<byte>(noThrow ? 40_000_000 : 1024);
            var jsonUtf8 = new Utf8JsonWriter(output, options);

            jsonUtf8.WriteStartObject();

            try
            {
                jsonUtf8.WriteString(key, value);

                if (!noThrow)
                {
                    Assert.True(false, $"Expected ArgumentException for data too large wasn't thrown. KeyLength: {key.Length} | ValueLength: {value.Length}");
                }
            }
            catch (ArgumentException)
            {
                if (noThrow)
                {
                    Assert.True(false, $"Expected writing large key/value to succeed. KeyLength: {key.Length} | ValueLength: {value.Length}");
                }
            }

            jsonUtf8.WriteEndObject();
            jsonUtf8.Flush();
        }

        [ConditionalTheory(nameof(IsX64))]
        [OuterLoop]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        public void WriteTooLargeArguments(bool formatted, bool skipValidation)
        {
            var options = new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation };

            byte[] bytesTooLarge;
            char[] charsTooLarge;
            var bytes = new byte[5];
            var chars = new char[5];

            try
            {
                bytesTooLarge = new byte[400_000_000];
                charsTooLarge = new char[400_000_000];
            }
            catch (OutOfMemoryException)
            {
                return;
            }

            bytesTooLarge.AsSpan().Fill((byte)'a');
            charsTooLarge.AsSpan().Fill('a');
            bytes.AsSpan().Fill((byte)'a');
            chars.AsSpan().Fill('a');

            var pipe = new Pipe();
            var output = pipe.Writer;
            using var jsonUtf8 = new Utf8JsonWriter(output, options);

            jsonUtf8.WriteStartArray();

            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteStartObject(bytesTooLarge));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(bytesTooLarge, bytes));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(bytes, bytesTooLarge));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(bytesTooLarge, chars));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(chars, bytesTooLarge));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(bytesTooLarge, new DateTime(2015, 11, 9)));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(bytesTooLarge, new DateTimeOffset(new DateTime(2015, 11, 9))));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(bytesTooLarge, Guid.NewGuid()));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteStringValue(bytesTooLarge));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteCommentValue(bytesTooLarge));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumber(bytesTooLarge, 10m));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumber(bytesTooLarge, 10.1));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumber(bytesTooLarge, 10.1f));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumber(bytesTooLarge, 12345678901));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumber(bytesTooLarge, (ulong)12345678901));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteBoolean(bytesTooLarge, true));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNull(bytesTooLarge));

            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteStartObject(charsTooLarge));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(charsTooLarge, chars));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(chars, charsTooLarge));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(charsTooLarge, bytes));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(bytes, charsTooLarge));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(charsTooLarge, new DateTime(2015, 11, 9)));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(charsTooLarge, new DateTimeOffset(new DateTime(2015, 11, 9))));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteString(charsTooLarge, Guid.NewGuid()));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteStringValue(charsTooLarge));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteCommentValue(charsTooLarge));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumber(charsTooLarge, 10m));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumber(charsTooLarge, 10.1));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumber(charsTooLarge, 10.1f));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumber(charsTooLarge, 12345678901));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNumber(charsTooLarge, (ulong)12345678901));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteBoolean(charsTooLarge, true));
            Assert.Throws<ArgumentException>(() => jsonUtf8.WriteNull(charsTooLarge));

            jsonUtf8.Flush();
            Assert.Equal(1, jsonUtf8.BytesCommitted);
        }

        private static string GetHelloWorldExpectedString(bool prettyPrint, string propertyName, string value)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml
            };

            json.WriteStartObject();
            json.WritePropertyName(propertyName);
            json.WriteValue(value);
            json.WritePropertyName(propertyName);
            json.WriteValue(value);
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetCommentExpectedString(bool prettyPrint, string comment)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            };

            json.WriteStartArray();
            for (int j = 0; j < 10; j++)
                json.WriteComment(comment);
            json.WriteValue(comment);
            json.WriteComment(comment);
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetStringsExpectedString(bool prettyPrint, string value)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None
            };

            json.WriteStartArray();
            for (int j = 0; j < 10; j++)
                json.WriteValue(value);
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetEscapedExpectedString(bool prettyPrint, string propertyName, string value, StringEscapeHandling escaping, bool escape = true)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = escaping
            };

            json.WriteStartObject();
            json.WritePropertyName(propertyName, escape);
            json.WriteValue(value);
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetCustomExpectedString(bool prettyPrint)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None
            };

            json.WriteStartObject();
            for (int i = 0; i < 1_000; i++)
            {
                json.WritePropertyName("message");
                json.WriteValue("Hello, World!");
            }
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetStartEndExpectedString(bool prettyPrint)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None
            };

            json.WriteStartArray();
            json.WriteStartObject();
            json.WriteEnd();
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetStartEndWithPropertyArrayExpectedString(bool prettyPrint)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None
            };

            json.WriteStartObject();
            json.WritePropertyName("property name");
            json.WriteStartArray();
            json.WriteEnd();
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetStartEndWithPropertyArrayExpectedString(string key, bool prettyPrint, bool escape = false)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml
            };

            json.WriteStartObject();
            json.WritePropertyName(key, escape);
            json.WriteStartArray();
            json.WriteEnd();
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetStartEndWithPropertyObjectExpectedString(bool prettyPrint)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None
            };

            json.WriteStartObject();
            json.WritePropertyName("property name");
            json.WriteStartObject();
            json.WriteEnd();
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetStartEndWithPropertyObjectExpectedString(string key, bool prettyPrint, bool escape = false)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml
            };

            json.WriteStartObject();
            json.WritePropertyName(key, escape);
            json.WriteStartObject();
            json.WriteEnd();
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetArrayWithPropertyExpectedString(bool prettyPrint)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None
            };

            json.WriteStartObject();
            json.WritePropertyName("message");
            json.WriteStartArray();
            json.WriteEndArray();
            json.WriteEndObject();
            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetBooleanExpectedString(bool prettyPrint, string keyString, bool value, bool escape = false)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            };

            json.WriteStartObject();
            json.WritePropertyName(keyString, escape);
            json.WriteValue(value);

            json.WritePropertyName("temp");
            json.WriteStartArray();
            json.WriteValue(true);
            json.WriteValue(true);
            json.WriteValue(false);
            json.WriteValue(false);
            json.WriteEnd();

            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetNullExpectedString(bool prettyPrint, string keyString, bool escape = false)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            };

            json.WriteStartObject();
            json.WritePropertyName(keyString, escape);
            json.WriteNull();
            json.WritePropertyName(keyString, escape);
            json.WriteNull();

            json.WritePropertyName("temp");
            json.WriteStartArray();
            json.WriteValue((string)null);
            json.WriteValue((string)null);
            json.WriteEnd();

            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetIntegerExpectedString(bool prettyPrint, int value)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None
            };

            json.WriteStartObject();
            json.WritePropertyName("message");
            json.WriteValue(value);
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetNumbersExpectedString(bool prettyPrint, string keyString, int[] ints, uint[] uints, long[] longs, ulong[] ulongs, float[] floats, double[] doubles, decimal[] decimals, bool escape = false)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None
            };

            json.WriteStartObject();

            for (int i = 0; i < floats.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(floats[i]);
            }
            for (int i = 0; i < ints.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(ints[i]);
            }
            for (int i = 0; i < uints.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(uints[i]);
            }
            for (int i = 0; i < doubles.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(doubles[i]);
            }
            for (int i = 0; i < longs.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(longs[i]);
            }
            for (int i = 0; i < ulongs.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(ulongs[i]);
            }
            for (int i = 0; i < decimals.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(decimals[i]);
            }

            json.WritePropertyName(keyString, escape);
            json.WriteStartArray();
            json.WriteValue(floats[0]);
            json.WriteValue(ints[0]);
            json.WriteValue(uints[0]);
            json.WriteValue(doubles[0]);
            json.WriteValue(longs[0]);
            json.WriteValue(ulongs[0]);
            json.WriteValue(decimals[0]);
            json.WriteEndArray();

            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetGuidsExpectedString(bool prettyPrint, string keyString, Guid[] guids, bool escape = false)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml
            };

            json.WriteStartObject();

            for (int i = 0; i < guids.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(guids[i]);
            }

            json.WritePropertyName(keyString, escape);
            json.WriteStartArray();
            json.WriteValue(guids[0]);
            json.WriteValue(guids[1]);
            json.WriteEnd();

            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetNumbersExpectedString(bool prettyPrint, int numberOfElements)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
            };

            json.WriteStartArray();
            for (int i = 0; i < numberOfElements; i++)
            {
                json.WriteValue(1234567);
            }
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetDatesExpectedString(bool prettyPrint, string keyString, DateTime[] dates, bool escape = false)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            };

            json.WriteStartObject();

            for (int i = 0; i < dates.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(dates[i]);
            }

            json.WritePropertyName(keyString, escape);
            json.WriteStartArray();
            json.WriteValue(dates[0]);
            json.WriteValue(dates[1]);
            json.WriteEnd();

            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetDatesExpectedString(bool prettyPrint, string keyString, DateTimeOffset[] dates, bool escape = false)
        {
            var ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            };

            json.WriteStartObject();

            for (int i = 0; i < dates.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(dates[i]);
            }

            json.WritePropertyName(keyString, escape);
            json.WriteStartArray();
            json.WriteValue(dates[0]);
            json.WriteValue(dates[1]);
            json.WriteEnd();

            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static void AssertContents(string expectedValue, ArrayBufferWriter<byte> buffer)
        {
            Assert.Equal(
                expectedValue,
                Encoding.UTF8.GetString(
                    buffer.WrittenSpan
#if netstandard
                        .ToArray()
#endif
                    ));
        }
    }
}
