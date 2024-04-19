// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
            // Use every method to make sure they don't throw

            output.Close();
            output.Dispose();
            Assert.True(output.DisposeAsync().IsCompletedSuccessfully);

            Assert.NotNull(output.Encoding);
            Assert.Same(CultureInfo.InvariantCulture, output.FormatProvider);
            Assert.Equal(Environment.NewLine, output.NewLine);
            output.NewLine = "hello";
            Assert.Equal(Environment.NewLine, output.NewLine);

            output.Flush();
            Assert.True(output.FlushAsync().IsCompletedSuccessfully);
            Assert.True(output.FlushAsync(CancellationToken.None).IsCompletedSuccessfully);
            Assert.True(output.FlushAsync(new CancellationToken(true)).IsCompletedSuccessfully);

            output.Write('a');
            output.Write((char[])null);
            output.Write(new char[] { 'b', 'c' });
            output.Write(42m);
            output.Write(43d);
            output.Write(44f);
            output.Write(45);
            output.Write(46L);
            output.Write(DayOfWeek.Monday);
            output.Write((string)null);
            output.Write("Tuesday");
            output.Write((StringBuilder)null);
            output.Write(new StringBuilder("Wednesday"));
            output.Write(47u);
            output.Write(48ul);
            output.Write("Thursday".AsSpan());
            output.Write(" {0} ", "Friday");
            output.Write(" {0}{1} ", "Saturday", "Sunday");
            output.Write(" {0} {1}  {2}", TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2), TimeSpan.FromDays(3));
            output.Write(" {0} {1}  {2}    {3}", (Int128)4, (UInt128)5, (nint)6, (nuint)7);
            output.WriteLine();
            output.WriteLine(true);
            output.WriteLine('a');
            output.WriteLine((char[])null);
            output.WriteLine(new char[] { 'b', 'c' });
            output.WriteLine(42m);
            output.WriteLine(43d);
            output.WriteLine(44f);
            output.WriteLine(45);
            output.WriteLine(46L);
            output.WriteLine(DayOfWeek.Monday);
            output.WriteLine((string)null);
            output.WriteLine("Tuesday");
            output.WriteLine((StringBuilder)null);
            output.WriteLine(new StringBuilder("Wednesday"));
            output.WriteLine(47u);
            output.WriteLine(48ul);
            output.WriteLine("Thursday".AsSpan());
            output.WriteLine(" {0} ", "Friday");
            output.WriteLine(" {0}{1} ", "Saturday", "Sunday");
            output.WriteLine(" {0} {1}  {2}", TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2), TimeSpan.FromDays(3));
            output.WriteLine(" {0} {1}  {2}    {3}", (Int128)4, (UInt128)5, (nint)6, (nuint)7);
            Assert.True(output.WriteAsync('a').IsCompletedSuccessfully);
            Assert.True(output.WriteAsync((char[])null).IsCompletedSuccessfully);
            Assert.True(output.WriteAsync(new char[] { 'b', 'c' }).IsCompletedSuccessfully);
            Assert.True(output.WriteAsync((string)null).IsCompletedSuccessfully);
            Assert.True(output.WriteAsync("Tuesday").IsCompletedSuccessfully);
            Assert.True(output.WriteAsync((StringBuilder)null).IsCompletedSuccessfully);
            Assert.True(output.WriteAsync(new StringBuilder("Wednesday")).IsCompletedSuccessfully);
            Assert.True(output.WriteLineAsync().IsCompletedSuccessfully);
            Assert.True(output.WriteLineAsync('a').IsCompletedSuccessfully);
            Assert.True(output.WriteLineAsync((char[])null).IsCompletedSuccessfully);
            Assert.True(output.WriteLineAsync(new char[] { 'b', 'c' }).IsCompletedSuccessfully);
            Assert.True(output.WriteLineAsync((string)null).IsCompletedSuccessfully);
            Assert.True(output.WriteLineAsync("Tuesday").IsCompletedSuccessfully);
            Assert.True(output.WriteLineAsync((StringBuilder)null).IsCompletedSuccessfully);
            Assert.True(output.WriteLineAsync(new StringBuilder("Wednesday")).IsCompletedSuccessfully);

            if (output is StreamWriter sw)
            {
                Assert.False(sw.AutoFlush);
                sw.AutoFlush = true;
                Assert.False(sw.AutoFlush);

                Assert.Same(Stream.Null, sw.BaseStream);
            }

            // Use some parallelism in an attempt to validate statelessness
            string longLine = new string('#', 100_000);
            Parallel.For(0, 25, i => output.WriteLine(longLine));
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
            }
        }

        public static IEnumerable<object[]> NullWriters
        {
            get
            {
                yield return new object[] { TextWriter.Null };
                yield return new object[] { StreamWriter.Null };
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
