// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Taken from https://github.com/dotnet/aspnetcore/blob/master/src/Mvc/Mvc.Core/test/Formatters/TranscodingWriteStreamTest.cs

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Json.Functional.Tests
{
    public class TranscodingWriteStreamTest
    {
        public static TheoryData WriteAsyncInputLatin =>
            TranscodingReadStreamTest.GetLatinTextInput(TranscodingWriteStream.MaxCharBufferSize, TranscodingWriteStream.MaxByteBufferSize);

        public static TheoryData WriteAsyncInputUnicode =>
            TranscodingReadStreamTest.GetUnicodeText(TranscodingWriteStream.MaxCharBufferSize);

        [Theory]
        [MemberData(nameof(WriteAsyncInputLatin))]
        [MemberData(nameof(WriteAsyncInputUnicode))]
        public Task WriteAsync_Works_WhenOutputIs_UTF32(string message)
        {
            Encoding targetEncoding = Encoding.UTF32;
            return WriteAsyncTest(targetEncoding, message);
        }

        [Theory]
        [MemberData(nameof(WriteAsyncInputLatin))]
        [MemberData(nameof(WriteAsyncInputUnicode))]
        public Task WriteAsync_Works_WhenOutputIs_Unicode(string message)
        {
            Encoding targetEncoding = Encoding.Unicode;
            return WriteAsyncTest(targetEncoding, message);
        }

        [Theory]
        [MemberData(nameof(WriteAsyncInputLatin))]
        public Task WriteAsync_Works_WhenOutputIs_WesternEuropeanEncoding(string message)
        {
            // Arrange
            Encoding targetEncoding = Encoding.GetEncoding(28591);
            return WriteAsyncTest(targetEncoding, message);
        }


        [Theory]
        [MemberData(nameof(WriteAsyncInputLatin))]
        public Task WriteAsync_Works_WhenOutputIs_ASCII(string message)
        {
            // Arrange
            Encoding targetEncoding = Encoding.ASCII;
            return WriteAsyncTest(targetEncoding, message);
        }

        private static async Task WriteAsyncTest(Encoding targetEncoding, string message)
        {
            string expected = $"{{\"Message\":\"{JavaScriptEncoder.Default.Encode(message)}\"}}";

            var model = new TestModel { Message = message };
            var stream = new MemoryStream();

            var transcodingStream = new TranscodingWriteStream(stream, targetEncoding);
            await JsonSerializer.SerializeAsync(transcodingStream, model, model.GetType());
            // The transcoding streams use Encoders and Decoders that have internal buffers. We need to flush these
            // when there is no more data to be written. Stream.FlushAsync isn't suitable since it's
            // acceptable to Flush a Stream (multiple times) prior to completion.
            await transcodingStream.FinalWriteAsync(default);
            await transcodingStream.FlushAsync();

            string actual = targetEncoding.GetString(stream.ToArray());
            Assert.Equal(expected, actual, StringComparer.OrdinalIgnoreCase);
        }

        private class TestModel
        {
            public string Message { get; set; }
        }
    }
}
