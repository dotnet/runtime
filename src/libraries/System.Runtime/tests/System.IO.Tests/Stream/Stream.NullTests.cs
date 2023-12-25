// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class NullTests
    {
        [Fact]
        public static async Task TestNullStream_Flush()
        {
            // Neither of these methods should have
            // side effects, so call them twice to
            // make sure they don't throw something
            // the second time around

            Stream.Null.Flush();
            await Stream.Null.FlushAsync();

            Stream.Null.Flush();
            await Stream.Null.FlushAsync();
        }

        [Fact]
        public static void TestNullStream_Dispose()
        {
            Stream.Null.Dispose();
            Stream.Null.Dispose(); // Dispose shouldn't have any side effects
        }

        [Fact]
        public static async Task TestNullStream_CopyTo()
        {
            Stream source = Stream.Null;

            int originalCapacity = 0;
            var destination = new MemoryStream(originalCapacity);

            source.CopyTo(destination, 12345);
            await source.CopyToAsync(destination, 67890, CancellationToken.None);

            Assert.Equal(0, source.Position);
            Assert.Equal(0, destination.Position);

            Assert.Equal(0, source.Length);
            Assert.Equal(0, destination.Length);

            Assert.Equal(originalCapacity, destination.Capacity);
        }

        [Fact]
        public static async Task TestNullStream_CopyToAsyncValidation()
        {
            var disposedStream = new MemoryStream();
            disposedStream.Dispose();

            // Stream.Null doesn't do any validation of state or arguments
            var readOnlyStream = new MemoryStream(new byte[1], writable: false);

            await Stream.Null.CopyToAsync(null);
            await Stream.Null.CopyToAsync(null, -123);
            await Stream.Null.CopyToAsync(Stream.Null, 0); // 0 shouldn't be a valid buffer size
            await Stream.Null.CopyToAsync(Stream.Null, -123);
            await Stream.Null.CopyToAsync(disposedStream);
            await Stream.Null.CopyToAsync(readOnlyStream);
        }

        [Theory]
        [MemberData(nameof(NullStream_ReadWriteData))]
        public static async Task TestNullStream_Read(byte[] buffer, int offset, int count)
        {
            byte[] copy = buffer?.ToArray();
            Stream source = Stream.Null;

            int read = source.Read(buffer, offset, count);
            Assert.Equal(0, read);
            Assert.Equal(copy, buffer); // Make sure Read doesn't modify the buffer
            Assert.Equal(0, source.Position);

            read = await source.ReadAsync(buffer, offset, count);
            Assert.Equal(0, read);
            Assert.Equal(copy, buffer);
            Assert.Equal(0, source.Position);
        }

        [Fact]
        public static void TestNullStream_ReadByte()
        {
            Stream source = Stream.Null;

            int data = source.ReadByte();
            Assert.Equal(-1, data);
            Assert.Equal(0, source.Position);
        }

        [Theory]
        [MemberData(nameof(NullStream_ReadWriteData))]
        public static async Task TestNullStream_Write(byte[] buffer, int offset, int count)
        {
            byte[] copy = buffer?.ToArray();
            Stream source = Stream.Null;

            source.Write(buffer, offset, count);
            Assert.Equal(copy, buffer); // Make sure Write doesn't modify the buffer
            Assert.Equal(0, source.Position);

            await source.WriteAsync(buffer, offset, count);
            Assert.Equal(copy, buffer);
            Assert.Equal(0, source.Position);
        }

        [Fact]
        public static void TestNullStream_WriteByte()
        {
            Stream source = Stream.Null;

            source.WriteByte(3);
            Assert.Equal(0, source.Position);
        }

        [Theory]
        [MemberData(nameof(NullReaders))]
        public static void TestNullTextReaderDispose(TextReader input)
        {
            // dispose should be a no-op
            input.Dispose();
            input.Dispose();
            Assert.Equal("", input.ReadToEnd());
        }

        [Theory]
        [MemberData(nameof(NullReaders))]
        public static void TestNullTextReader(TextReader input)
        {
            StreamReader sr = input as StreamReader;

            if (sr != null)
                Assert.True(sr.EndOfStream, "EndOfStream property didn't return true");
            Assert.Null(input.ReadLine());
            if (sr != null)
                Assert.True(sr.EndOfStream, "EndOfStream property didn't return true");

            Assert.Equal(-1, input.Read());
            Assert.Equal(-1, input.Peek());
            var chars = new char[2];
            Assert.Equal(0, input.Read(chars, 0, chars.Length));
            Assert.Equal(0, input.Read(chars.AsSpan()));
            Assert.Equal(0, input.ReadBlock(chars, 0, chars.Length));
            Assert.Equal(0, input.ReadBlock(chars.AsSpan()));
            Assert.Equal("", input.ReadToEnd());
            input.Dispose();
        }

        [Theory]
        [MemberData(nameof(NullReaders))]
        public static async Task TestNullTextReaderAsync(TextReader input)
        {
            var chars = new char[2];
            Assert.Equal(0, await input.ReadAsync(chars, 0, chars.Length));
            Assert.Equal(0, await input.ReadAsync(chars.AsMemory(), default));
            Assert.Equal(0, await input.ReadBlockAsync(chars, 0, chars.Length));
            Assert.Equal(0, await input.ReadBlockAsync(chars.AsMemory(), default));
            Assert.Null(await input.ReadLineAsync());
            Assert.Null(await input.ReadLineAsync(default));
            Assert.Equal("", await input.ReadToEndAsync());
            Assert.Equal("", await input.ReadToEndAsync(default));
            input.Dispose();
        }

        [Theory]
        [MemberData(nameof(NullReaders))]
        public static async Task TestCanceledNullTextReaderAsync(TextReader input)
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();
            var token = tokenSource.Token;
            var chars = new char[2];
            OperationCanceledException ex;
            ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await input.ReadAsync(chars.AsMemory(), token));
            Assert.Equal(token, ex.CancellationToken);
            ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await input.ReadBlockAsync(chars.AsMemory(), token));
            Assert.Equal(token, ex.CancellationToken);
            ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await input.ReadLineAsync(token));
            Assert.Equal(token, ex.CancellationToken);
            ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await input.ReadToEndAsync(token));
            Assert.Equal(token, ex.CancellationToken);
            input.Dispose();
        }

        [Theory]
        [MemberData(nameof(NullWriters))]
        public static void TextNullTextWriter(TextWriter output)
        {
            output.Flush();
            output.Dispose();

            output.WriteLine(decimal.MinValue);
            output.WriteLine(Math.PI);
            output.WriteLine();
            output.Flush();
            output.Dispose();
        }

        [Theory]
        [MemberData(nameof(NullStream_ReadWriteData))]
        public void TestNullStream_ReadSpan(byte[] buffer, int offset, int count)
        {
            if (buffer == null) return;

            byte[] copy = buffer.ToArray();
            Stream source = Stream.Null;

            int read = source.Read(new Span<byte>(buffer, offset, count));
            Assert.Equal(0, read);
            Assert.Equal(copy, buffer); // Make sure Read doesn't modify the buffer
            Assert.Equal(0, source.Position);
        }

        [Theory]
        [MemberData(nameof(NullStream_ReadWriteData))]
        public void TestNullStream_WriteSpan(byte[] buffer, int offset, int count)
        {
            if (buffer == null) return;

            byte[] copy = buffer.ToArray();
            Stream source = Stream.Null;

            source.Write(new Span<byte>(buffer, offset, count));
            Assert.Equal(copy, buffer); // Make sure Write doesn't modify the buffer
            Assert.Equal(0, source.Position);
        }

        [Fact]
        public void DisposeAsync_Nop()
        {
            Assert.True(Stream.Null.DisposeAsync().IsCompletedSuccessfully);
            Stream.Null.Write(new byte[42]); // still usable
        }

        public static IEnumerable<object[]> NullReaders
        {
            get
            {
                yield return new object[] { TextReader.Null };
                yield return new object[] { StreamReader.Null };
                yield return new object[] { StringReader.Null };
            }
        }

        public static IEnumerable<object[]> NullWriters
        {
            get
            {
                yield return new object[] { TextWriter.Null };
                yield return new object[] { StreamWriter.Null };
                yield return new object[] { StringWriter.Null };
            }
        }

        public static IEnumerable<object[]> NullStream_ReadWriteData
        {
            get
            {
                yield return new object[] { new byte[10], 0, 10 };
                yield return new object[] { null, -123, 456 }; // Stream.Null.Read/Write should not perform argument validation
            }
        }
    }
}
