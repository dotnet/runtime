// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class NullTests
    {
        [Fact]
        public async static Task TestNullStream_Flush()
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
        public async static Task TestNullStream_CopyTo()
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
        public async static Task TestNullStream_CopyToAsyncValidation()
        {
            // Since Stream.Null previously inherited its CopyToAsync
            // implementation from the base class, which did check its
            // arguments, we have to make sure it validates them as
            // well for compat.
            
            var disposedStream = new MemoryStream();
            disposedStream.Dispose();
            
            var readOnlyStream = new MemoryStream(new byte[1], writable: false);
            
            await AssertExtensions.ThrowsAsync<ArgumentNullException>("destination", () => Stream.Null.CopyToAsync(null));
            await AssertExtensions.ThrowsAsync<ArgumentNullException>("destination", () => Stream.Null.CopyToAsync(null, -123)); // Should check if destination == null first
            await AssertExtensions.ThrowsAsync<ArgumentOutOfRangeException>("bufferSize", () => Stream.Null.CopyToAsync(Stream.Null, 0)); // 0 shouldn't be a valid buffer size
            await AssertExtensions.ThrowsAsync<ArgumentOutOfRangeException>("bufferSize", () => Stream.Null.CopyToAsync(Stream.Null, -123));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => Stream.Null.CopyToAsync(disposedStream));
            await Assert.ThrowsAsync<NotSupportedException>(() => Stream.Null.CopyToAsync(readOnlyStream));
        }
        
        [Theory]
        [MemberData(nameof(NullStream_ReadWriteData))]
        public async static Task TestNullStream_Read(byte[] buffer, int offset, int count)
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
        public async static Task TestNullStream_Write(byte[] buffer, int offset, int count)
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
        public static void TestNullTextReader(TextReader input)
        {
            StreamReader sr = input as StreamReader;

            if (sr != null)
                Assert.True(sr.EndOfStream, "EndOfStream property didn't return true");
            input.ReadLine();
            input.Dispose();

            input.ReadLine();
            if (sr != null)
                Assert.True(sr.EndOfStream, "EndOfStream property didn't return true");
            input.Read();
            input.Peek();
            input.Read(new char[2], 0, 2);
            input.ReadToEnd();
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
