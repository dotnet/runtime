// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.IO.Tests
{
    public partial class StreamReaderTests
    {
        private const string LowerAlpha = "abcdefghijklmnopqrstuvwxyz";

        protected virtual Stream CreateStream()
        {
            return new MemoryStream();
        }

        protected virtual Stream GetSmallStream()
        {
            byte[] testData = new byte[] { 72, 69, 76, 76, 79 };
            return new MemoryStream(testData);
        }

        protected virtual Stream GetLargeStream()
        {
            byte[] testData = new byte[] { 72, 69, 76, 76, 79 };
            // System.Collections.Generic.

            List<byte> data = new List<byte>();
            for (int i = 0; i < 1000; i++)
            {
                data.AddRange(testData);
            }

            return new MemoryStream(data.ToArray());
        }

        protected Tuple<char[], StreamReader> GetCharArrayStream()
        {
            var chArr = TestDataProvider.CharData;
            var ms = CreateStream();
            var sw = new StreamWriter(ms);

            for (int i = 0; i < chArr.Length; i++)
                sw.Write(chArr[i]);
            sw.Flush();
            ms.Position = 0;

            return new Tuple<char[], StreamReader>(chArr, new StreamReader(ms));
        }

        [Fact]
        public void EndOfStream()
        {
            var sw = new StreamReader(GetSmallStream());

            var result = sw.ReadToEnd();

            Assert.Equal("HELLO", result);

            Assert.True(sw.EndOfStream, "End of Stream was not true after ReadToEnd");
        }

        [Fact]
        public void EndOfStreamSmallDataLargeBuffer()
        {
            var sw = new StreamReader(GetSmallStream(), Encoding.UTF8, true, 1024);

            var result = sw.ReadToEnd();

            Assert.Equal("HELLO", result);

            Assert.True(sw.EndOfStream, "End of Stream was not true after ReadToEnd");
        }

        [Fact]
        public void EndOfStreamLargeDataSmallBuffer()
        {
            var sw = new StreamReader(GetLargeStream(), Encoding.UTF8, true, 1);

            var result = sw.ReadToEnd();

            Assert.Equal(5000, result.Length);

            Assert.True(sw.EndOfStream, "End of Stream was not true after ReadToEnd");
        }

        [Fact]
        public void EndOfStreamLargeDataLargeBuffer()
        {
            var sw = new StreamReader(GetLargeStream(), Encoding.UTF8, true, 1 << 16);

            var result = sw.ReadToEnd();

            Assert.Equal(5000, result.Length);

            Assert.True(sw.EndOfStream, "End of Stream was not true after ReadToEnd");
        }

        [Fact]
        public async Task ReadToEndAsync()
        {
            var sw = new StreamReader(GetLargeStream());
            var result = await sw.ReadToEndAsync();

            Assert.Equal(5000, result.Length);
        }

        [Fact]
        public async Task ReadToEndAsync_WithCancellationToken()
        {
            using var sw = new StreamReader(GetLargeStream());
            var result = await sw.ReadToEndAsync(default);

            Assert.Equal(5000, result.Length);
        }

        [Fact]
        public async Task ReadToEndAsync_WithCanceledCancellationToken()
        {
            using var sw = new StreamReader(GetLargeStream());
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var token = cts.Token;

            var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await sw.ReadToEndAsync(token));
            Assert.Equal(token, ex.CancellationToken);
        }

        [Fact]
        public void GetBaseStream()
        {
            var ms = GetSmallStream();
            var sw = new StreamReader(ms);

            Assert.Same(sw.BaseStream, ms);
        }

        [Fact]
        public void TestRead()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;


            for (int i = 0; i < baseInfo.Item1.Length; i++)
            {
                int tmp = sr.Read();
                Assert.Equal((int)baseInfo.Item1[i], tmp);
            }

            sr.Dispose();
        }

        [Fact]
        public void TestPeek()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;

            for (int i = 0; i < baseInfo.Item1.Length; i++)
            {
                var peek = sr.Peek();
                Assert.Equal((int)baseInfo.Item1[i], peek);

                sr.Read();
            }
        }

        [Fact]
        public void TestPeekReadOneByteAtATime()
        {
            byte[] testData = new byte[] { 72, 69, 76, 76, 79 };
            using var ms = new MemoryStream(testData);

            // DelegateStream to read one at a time.
            using var stream = new DelegateStream(
                positionGetFunc: () => ms.Position,
                lengthFunc: () => ms.Length,
                canReadFunc: () => true,
                readFunc: (buffer, offset, count) =>
                {
                    if (count == 0 || ms.Position == ms.Length)
                    {
                        return 0;
                    }

                    ms.ReadExactly(buffer, offset, 1);
                    return 1;
                });

            using var sr = new StreamReader(stream);

            for (int i = 0; i < testData.Length; i++)
            {
                Assert.Equal(i, stream.Position);

                int tmp = sr.Peek();
                Assert.Equal(testData[i], tmp);

                tmp = sr.Read(); 
                Assert.Equal(testData[i], tmp);
            }

            Assert.Equal(stream.Position, stream.Length);
            Assert.Equal(-1, sr.Peek());
            Assert.Equal(-1, sr.Read());
        }

        [Fact]
        public void ArgumentNullOnNullArray()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;

            Assert.Throws<ArgumentNullException>(() => sr.Read(null, 0, 0));
        }

        [Fact]
        public void ArgumentOutOfRangeOnInvalidOffset()
        {
            var sr = GetCharArrayStream().Item2;
            Assert.Throws<ArgumentOutOfRangeException>(() => sr.Read(new char[0], -1, 0));
        }

        [Fact]
        public void ArgumentOutOfRangeOnNegativeCount()
        {
            var sr = GetCharArrayStream().Item2;
            AssertExtensions.Throws<ArgumentException>(null, () => sr.Read(new char[0], 0, 1));
        }

        [Fact]
        public void ArgumentExceptionOffsetAndCount()
        {
            var sr = GetCharArrayStream().Item2;
            AssertExtensions.Throws<ArgumentException>(null, () => sr.Read(new char[0], 2, 0));
        }

        [Fact]
        public void ObjectDisposedExceptionDisposedStream()
        {
            var sr = GetCharArrayStream().Item2;
            sr.Dispose();

            Assert.Throws<ObjectDisposedException>(() => sr.Read(new char[1], 0, 1));
        }

        [Fact]
        public void ObjectDisposedExceptionDisposedBaseStream()
        {
            var ms = GetSmallStream();
            var sr = new StreamReader(ms);
            ms.Dispose();

            Assert.Throws<ObjectDisposedException>(() => sr.Read(new char[1], 0, 1));
        }

        [Fact]
        public void EmptyStream()
        {
            var ms = CreateStream();
            var sr = new StreamReader(ms);

            var buffer = new char[10];
            int read = sr.Read(buffer, 0, 1);
            Assert.Equal(0, read);
        }

        [Fact]
        public void VanillaReads1()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;

            var chArr = new char[baseInfo.Item1.Length];

            var read = sr.Read(chArr, 0, chArr.Length);

            Assert.Equal(chArr.Length, read);
            for (int i = 0; i < baseInfo.Item1.Length; i++)
            {
                Assert.Equal(baseInfo.Item1[i], chArr[i]);
            }
        }

        [Fact]
        public async Task VanillaReads2WithAsync()
        {
            var baseInfo = GetCharArrayStream();

            var sr = baseInfo.Item2;

            var chArr = new char[baseInfo.Item1.Length];

            var read = await sr.ReadAsync(chArr, 4, 3);

            Assert.Equal(3, read);
            for (int i = 0; i < 3; i++)
            {
                Assert.Equal(baseInfo.Item1[i], chArr[i + 4]);
            }
        }

        [Fact]
        public void ObjectDisposedReadLine()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;

            sr.Dispose();
            Assert.Throws<ObjectDisposedException>(() => sr.ReadLine());
        }

        [Fact]
        public void ObjectDisposedReadLineBaseStream()
        {
            var ms = GetLargeStream();
            var sr = new StreamReader(ms);

            ms.Dispose();
            Assert.Throws<ObjectDisposedException>(() => sr.ReadLine());
        }

        [Fact]
        public void VanillaReadLines()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;

            string valueString = new string(baseInfo.Item1);


            var data = sr.ReadLine();
            Assert.Equal(valueString.Substring(0, valueString.IndexOf('\r')), data);

            data = sr.ReadLine();
            Assert.Equal(valueString.Substring(valueString.IndexOf('\r') + 1, 3), data);

            data = sr.ReadLine();
            Assert.Equal(valueString.Substring(valueString.IndexOf('\n') + 1, 2), data);

            data = sr.ReadLine();
            Assert.Equal((valueString.Substring(valueString.LastIndexOf('\n') + 1)), data);
        }

        [Fact]
        public void VanillaReadLines2()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;

            string valueString = new string(baseInfo.Item1);

            var temp = new char[10];
            sr.Read(temp, 0, 1);
            var data = sr.ReadLine();
            Assert.Equal(valueString.Substring(1, valueString.IndexOf('\r') - 1), data);
        }

        [Fact]
        public async Task VanillaReadLineAsync()
        {
            var baseInfo = GetCharArrayStream();
            var sr = baseInfo.Item2;

            string valueString = new string(baseInfo.Item1);

            var data = await sr.ReadLineAsync();
            Assert.Equal(valueString.Substring(0, valueString.IndexOf('\r')), data);

            data = await sr.ReadLineAsync(default);
            Assert.Equal(valueString.Substring(valueString.IndexOf('\r') + 1, 3), data);

            data = await sr.ReadLineAsync();
            Assert.Equal(valueString.Substring(valueString.IndexOf('\n') + 1, 2), data);

            data = await sr.ReadLineAsync(default);
            Assert.Equal((valueString.Substring(valueString.LastIndexOf('\n') + 1)), data);
        }

        [Fact]
        public async Task ContinuousNewLinesAndTabsAsync()
        {
            var ms = CreateStream();
            var sw = new StreamWriter(ms);
            sw.Write("\n\n\r\r\n");
            sw.Flush();

            ms.Position = 0;

            var sr = new StreamReader(ms);

            for (int i = 0; i < 4; i++)
            {
                var data = await sr.ReadLineAsync();
                Assert.Equal(string.Empty, data);
            }

            var eol = await sr.ReadLineAsync();
            Assert.Null(eol);
        }

        [Fact]
        public void CurrentEncoding()
        {
            var ms = CreateStream();

            var sr = new StreamReader(ms);
            Assert.Equal(Encoding.UTF8, sr.CurrentEncoding);

            sr = new StreamReader(ms, Encoding.Unicode);
            Assert.Equal(Encoding.Unicode, sr.CurrentEncoding);

        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public async Task Read_EmptySpan_ReadsNothing(int length)
        {
            using (var r = new StreamReader(new MemoryStream(Enumerable.Repeat((byte)'s', length).ToArray())))
            {
                Assert.Equal(0, r.Read(Span<char>.Empty));
                Assert.Equal(0, r.ReadBlock(Span<char>.Empty));
                Assert.Equal(0, await r.ReadAsync(Memory<char>.Empty));
                Assert.Equal(0, await r.ReadBlockAsync(Memory<char>.Empty));
            }
        }

        [Theory]
        [InlineData(1, 100, 1)]
        [InlineData(1, 100, 101)]
        [InlineData(100, 50, 1)]
        [InlineData(100, 50, 101)]
        public void Read_ReadsExpectedData(int readLength, int totalLength, int bufferSize)
        {
            var r = new Random(42);
            char[] data = r.GetItems<char>(LowerAlpha, totalLength);

            var result = new char[data.Length];
            Span<char> dst = result;

            using (var sr = new StreamReader(new MemoryStream(data.Select(i => (byte)i).ToArray()), Encoding.ASCII, false, bufferSize))
            {
                while (dst.Length > 0)
                {
                    int toRead = Math.Min(readLength, dst.Length);
                    int read = sr.Read(dst.Slice(0, toRead));
                    Assert.InRange(read, 1, dst.Length);
                    dst = dst.Slice(read);
                }
            }

            Assert.Equal<char>(data, result);
        }

        [Theory]
        [InlineData(1, 100, 1)]
        [InlineData(1, 100, 101)]
        [InlineData(100, 50, 1)]
        [InlineData(100, 50, 101)]
        public void ReadBlock_ReadsExpectedData(int readLength, int totalLength, int bufferSize)
        {
            var r = new Random(42);
            char[] data = r.GetItems<char>(LowerAlpha, totalLength);

            var result = new char[data.Length];
            Span<char> dst = result;

            using (var sr = new StreamReader(new MemoryStream(data.Select(i => (byte)i).ToArray()), Encoding.ASCII, false, bufferSize))
            {
                while (dst.Length > 0)
                {
                    int toRead = Math.Min(readLength, dst.Length);
                    int read = sr.ReadBlock(dst.Slice(0, toRead));
                    Assert.InRange(read, 1, dst.Length);
                    dst = dst.Slice(read);
                }
            }

            Assert.Equal<char>(data, result);
        }

        [Theory]
        [InlineData(1, 100, 1)]
        [InlineData(1, 100, 101)]
        [InlineData(100, 50, 1)]
        [InlineData(100, 50, 101)]
        public async Task ReadAsync_ReadsExpectedData(int readLength, int totalLength, int bufferSize)
        {
            var r = new Random(42);
            char[] data = r.GetItems<char>(LowerAlpha, totalLength);

            var result = new char[data.Length];
            Memory<char> dst = result;

            using (var sr = new StreamReader(new MemoryStream(data.Select(i => (byte)i).ToArray()), Encoding.ASCII, false, bufferSize))
            {
                while (dst.Length > 0)
                {
                    int toRead = Math.Min(readLength, dst.Length);
                    int read = await sr.ReadAsync(dst.Slice(0, toRead));
                    Assert.InRange(read, 1, dst.Length);
                    dst = dst.Slice(read);
                }
            }

            Assert.Equal<char>(data, result);
        }

        [Theory]
        [InlineData(1, 100, 1)]
        [InlineData(1, 100, 101)]
        [InlineData(100, 50, 1)]
        [InlineData(100, 50, 101)]
        public async Task ReadBlockAsync_ReadsExpectedData(int readLength, int totalLength, int bufferSize)
        {
            var r = new Random(42);
            char[] data = r.GetItems<char>(LowerAlpha, totalLength);


            var result = new char[data.Length];
            Memory<char> dst = result;

            using (var sr = new StreamReader(new MemoryStream(data.Select(i => (byte)i).ToArray()), Encoding.ASCII, false, bufferSize))
            {
                while (dst.Length > 0)
                {
                    int toRead = Math.Min(readLength, dst.Length);
                    int read = await sr.ReadBlockAsync(dst.Slice(0, toRead));
                    Assert.InRange(read, 1, dst.Length);
                    dst = dst.Slice(read);
                }
            }

            Assert.Equal<char>(data, result);
        }

        [Fact]
        public void ReadBlock_RepeatsReadsUntilReadDesiredAmount()
        {
            char[] data = "hello world".ToCharArray();
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var s = new DelegateStream(
                canReadFunc: () => true,
                readFunc: (buffer, offset, count) => ms.Read(buffer, offset, 1)); // do actual reads a byte at a time
            using (var r = new StreamReader(s, Encoding.UTF8, false, 2))
            {
                var result = new char[data.Length];
                Assert.Equal(data.Length, r.ReadBlock((Span<char>)result));
                Assert.Equal<char>(data, result);
            }
        }

        [Fact]
        public async Task ReadBlockAsync_RepeatsReadsUntilReadDesiredAmount()
        {
            char[] data = "hello world".ToCharArray();
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var s = new DelegateStream(
                canReadFunc: () => true,
                readAsyncFunc: (buffer, offset, count, cancellationToken) => ms.ReadAsync(buffer, offset, 1)); // do actual reads a byte at a time
            using (var r = new StreamReader(s, Encoding.UTF8, false, 2))
            {
                var result = new char[data.Length];
                Assert.Equal(data.Length, await r.ReadBlockAsync((Memory<char>)result));
                Assert.Equal<char>(data, result);
            }
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser.")]
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "SElinux blocks UNIX sockets in our CI environment")]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "iOS/tvOS blocks binding to UNIX sockets")]
        public async Task ReadAsync_Canceled_ThrowsException(int method, bool precanceled)
        {
            Func<StreamReader, CancellationToken, Task<int>> func = method switch
            {
                0 => (sr, ct) => sr.ReadAsync(new char[1], ct).AsTask(),
                1 => (sr, ct) => sr.ReadBlockAsync(new char[1], ct).AsTask(),
                _ => throw new Exception("unknown mode")
            };

            string pipeName = Guid.NewGuid().ToString("N");
            using (var serverStream = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (var clientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.In, PipeOptions.Asynchronous))
            {
                await Task.WhenAll(
                    serverStream.WaitForConnectionAsync(),
                    clientStream.ConnectAsync());

                using (var sr = new StreamReader(clientStream))
                {
                    var cts = new CancellationTokenSource();

                    if (precanceled)
                    {
                        cts.Cancel();
                    }

                    Task<int> t = func(sr, cts.Token);

                    if (!precanceled)
                    {
                        cts.Cancel();
                    }

                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
                }
            }
        }

        [Fact]
        public async Task Read_SpanMemory_DisposedStream_ThrowsException()
        {
            var sr = new StreamReader(new MemoryStream());
            sr.Dispose();

            Assert.Throws<ObjectDisposedException>(() => sr.Read(Span<char>.Empty));
            Assert.Throws<ObjectDisposedException>(() => sr.ReadBlock(Span<char>.Empty));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => sr.ReadAsync(Memory<char>.Empty).AsTask());
            await Assert.ThrowsAsync<ObjectDisposedException>(() => sr.ReadBlockAsync(Memory<char>.Empty).AsTask());
        }

        [Fact]
        public void StreamReader_WithOptionalArguments()
        {
            byte[] ByteOrderMaskUtf7 = new byte[] { 0x2B, 0x2F, 0x76, 0x38 };
            byte[] ByteOrderMaskUtf8 = new byte[] { 0xEF, 0xBB, 0xBF };
            byte[] ByteOrderMaskUtf16_BE = new byte[] { 0xFE, 0xFF, 0x20, 0x20 };
            byte[] ByteOrderMaskUtf16_LE = new byte[] { 0xFF, 0xFE, 0x20, 0x20 };
            byte[] ByteOrderMaskUtf32 = new byte[] { 0x00, 0x00, 0xFE, 0xFF };

            // check enabled leaveOpen and default encoding
            using (var tempStream = new MemoryStream())
            {
                using (var sr = new StreamReader(tempStream, leaveOpen: true))
                {
                    Assert.Equal(Encoding.UTF8, sr.CurrentEncoding);
                }
                Assert.True(tempStream.CanRead);
            }

            // check null encoding, default encoding, default leaveOpen
            using (var tempStream = new MemoryStream())
            {
                using (var sr = new StreamReader(tempStream, encoding: null))
                {
                    Assert.Equal(Encoding.UTF8, sr.CurrentEncoding);
                }
                Assert.False(tempStream.CanRead);
            }

            // check bufferSize, default BOM and default leaveOpen
            using (var tempStream = new MemoryStream(ByteOrderMaskUtf16_BE))
            {
                using (var sr = new StreamReader(tempStream, bufferSize: -1))
                {
                    sr.Read();
                    Assert.Equal(Encoding.BigEndianUnicode, sr.CurrentEncoding);
                }
                Assert.False(tempStream.CanRead);
            }

            // check BOM enabled/disabled encoding, enabled/disabled leaveOpen
            using (var tempStream = new MemoryStream(ByteOrderMaskUtf16_BE))
            {
                // check disabled BOM, default encoding
                using (var sr = new StreamReader(new MemoryStream(ByteOrderMaskUtf7), detectEncodingFromByteOrderMarks: false))
                {
                    sr.Read();
                    Assert.Equal(Encoding.UTF8, sr.CurrentEncoding);
                }

                // check disabled BOM, default encoding and leaveOpen
                tempStream.Seek(0, SeekOrigin.Begin);
                using (var sr = new StreamReader(tempStream, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
                {
                    sr.Read();
                    Assert.Equal(Encoding.UTF8, sr.CurrentEncoding);
                }
                Assert.True(tempStream.CanRead);

                // check enabled BOM and leaveOpen
                tempStream.Seek(0, SeekOrigin.Begin);
                using (var sr = new StreamReader(tempStream, detectEncodingFromByteOrderMarks: true))
                {
                    sr.Read();
                    Assert.Equal(Encoding.BigEndianUnicode, sr.CurrentEncoding);
                }
                Assert.False(tempStream.CanRead);
            }
        }

        [Fact]
        public void Read_ShortStream_PerformsFinalFlushCorrectly()
        {
            MemoryStream memStream = new MemoryStream(new byte[] { 0x61 /* 'a' */, 0xF0 });

            // First, use ReadToEnd API.

            memStream.Position = 0;
            StreamReader reader = new StreamReader(memStream, Encoding.UTF8);
            Assert.Equal("a\uFFFD", reader.ReadToEnd());

            // Next, use Read() API.

            memStream.Position = 0;
            reader = new StreamReader(memStream, Encoding.UTF8);
            Assert.Equal('a', reader.Read());
            Assert.Equal('\uFFFD', reader.Read());
            Assert.Equal(-1, reader.Read());

            // Next, use Read(Span<char>) API.

            StringBuilder builder = new StringBuilder();
            Span<char> destBuffer = new char[1024];
            memStream.Position = 0;
            reader = new StreamReader(memStream, Encoding.UTF8);
            int charsRead;
            while ((charsRead = reader.Read(destBuffer)) > 0)
            {
                builder.Append(destBuffer.Slice(0, charsRead));
            }
            Assert.Equal("a\uFFFD", builder.ToString());

            // Finally, use ReadLine API.

            memStream.Position = 0;
            reader = new StreamReader(memStream, Encoding.UTF8);
            Assert.Equal("a\uFFFD", reader.ReadLine());
            Assert.Null(reader.ReadLine());
        }

        [Fact]
        public void Read_LongStreamIntoShortBuffer_PerformsFinalFlushCorrectly()
        {
            MemoryStream memStream = new MemoryStream();
            memStream.Write(Enumerable.Repeat((byte)'a', 32 * 1024).ToArray());
            memStream.WriteByte(0xF0);
            string expected = new string('a', 32 * 1024) + "\uFFFD";

            // First, use ReadToEnd API.

            memStream.Position = 0;
            StreamReader reader = new StreamReader(memStream, encoding: Encoding.UTF8, bufferSize: 32);
            Assert.Equal(expected, reader.ReadToEnd());

            // Next, use Read() API.

            memStream.Position = 0;
            reader = new StreamReader(memStream, encoding: Encoding.UTF8, bufferSize: 32);
            for (int i = 0; i < 32 * 1024; i++)
            {
                Assert.Equal('a', reader.Read());
            }
            Assert.Equal('\uFFFD', reader.Read());
            Assert.Equal(-1, reader.Read());

            // Next, use Read(Span<char>) API.

            StringBuilder builder = new StringBuilder();
            Span<char> destBuffer = new char[47]; // prime number, because why not
            memStream.Position = 0;
            reader = new StreamReader(memStream, encoding: Encoding.UTF8, bufferSize: 32);
            int charsRead;
            while ((charsRead = reader.Read(destBuffer)) > 0)
            {
                builder.Append(destBuffer.Slice(0, charsRead));
            }
            Assert.Equal(expected, builder.ToString());

            // Finally, use ReadLine API.

            memStream.Position = 0;
            reader = new StreamReader(memStream, encoding: Encoding.UTF8, bufferSize: 32);
            Assert.Equal(expected, reader.ReadLine());
            Assert.Null(reader.ReadLine());
        }

        [Fact]
        public async Task ReadAsync_ShortStream_PerformsFinalFlushCorrectly()
        {
            MemoryStream memStream = new MemoryStream(new byte[] { 0x61 /* 'a' */, 0xF0 });

            // First, use ReadToEndAsync API.

            memStream.Position = 0;
            StreamReader reader = new StreamReader(memStream, Encoding.UTF8);
            Assert.Equal("a\uFFFD", await reader.ReadToEndAsync());

            // Next, use ReadAsync(Memory<char>) API.

            StringBuilder builder = new StringBuilder();
            Memory<char> destBuffer = new char[1024];
            memStream.Position = 0;
            reader = new StreamReader(memStream, Encoding.UTF8);
            int charsRead;
            while ((charsRead = await reader.ReadAsync(destBuffer)) > 0)
            {
                builder.Append(destBuffer.Slice(0, charsRead));
            }
            Assert.Equal("a\uFFFD", builder.ToString());

            // Finally, use ReadLineAsync API.

            memStream.Position = 0;
            reader = new StreamReader(memStream, Encoding.UTF8);
            Assert.Equal("a\uFFFD", await reader.ReadLineAsync());
            Assert.Null(await reader.ReadLineAsync());
        }

        [Fact]
        public async Task ReadAsync_LongStreamIntoShortBuffer_PerformsFinalFlushCorrectly()
        {
            MemoryStream memStream = new MemoryStream();
            memStream.Write(Enumerable.Repeat((byte)'a', 32 * 1024).ToArray());
            memStream.WriteByte(0xF0);
            string expected = new string('a', 32 * 1024) + "\uFFFD";

            // First, use ReadToEndAsync API.

            memStream.Position = 0;
            StreamReader reader = new StreamReader(memStream, encoding: Encoding.UTF8, bufferSize: 32);
            Assert.Equal(expected, await reader.ReadToEndAsync());

            // Next, use Read(Memory<char>) API.

            StringBuilder builder = new StringBuilder();
            Memory<char> destBuffer = new char[47]; // prime number, because why not
            memStream.Position = 0;
            reader = new StreamReader(memStream, encoding: Encoding.UTF8, bufferSize: 32);
            int charsRead;
            while ((charsRead = await reader.ReadAsync(destBuffer)) > 0)
            {
                builder.Append(destBuffer.Slice(0, charsRead));
            }
            Assert.Equal(expected, builder.ToString());

            // Finally, use ReadLineAsync API.

            memStream.Position = 0;
            reader = new StreamReader(memStream, encoding: Encoding.UTF8, bufferSize: 32);
            Assert.Equal(expected, await reader.ReadLineAsync());
            Assert.Null(await reader.ReadLineAsync());
        }
    }
}
