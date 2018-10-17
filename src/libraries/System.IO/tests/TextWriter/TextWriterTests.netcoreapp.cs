// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class TextWriterTests
    {
        [Fact]
        public void WriteCharSpanTest()
        {
            using (CharArrayTextWriter tw = NewTextWriter)
            {
                var rs = new ReadOnlySpan<char>(TestDataProvider.CharData, 4, 6);
                tw.Write(rs);
                Assert.Equal(new string(rs), tw.Text);
            }
        }

        [Fact]
        public void WriteLineCharSpanTest()
        {
            using (CharArrayTextWriter tw = NewTextWriter)
            {
                var rs = new ReadOnlySpan<char>(TestDataProvider.CharData, 4, 6);
                tw.WriteLine(rs);
                Assert.Equal(new string(rs) + tw.NewLine, tw.Text);
            }
        }

        [Fact]
        public async Task WriteCharMemoryTest()
        {
            using (CharArrayTextWriter tw = NewTextWriter)
            {
                var rs = new Memory<char>(TestDataProvider.CharData, 4, 6);
                await tw.WriteAsync(rs);
                Assert.Equal(new string(rs.Span), tw.Text);
            }
        }

        [Fact]
        public async Task WriteLineCharMemoryTest()
        {
            using (CharArrayTextWriter tw = NewTextWriter)
            {
                var rs = new Memory<char>(TestDataProvider.CharData, 4, 6);
                await tw.WriteLineAsync(rs);
                Assert.Equal(new string(rs.Span) + tw.NewLine, tw.Text);
            }
        }

        [Theory]
        [MemberData(nameof(GetStringBuilderTestData))]
        public void WriteStringBuilderTest(bool isSynchronized, StringBuilder testData)
        {
            using (CharArrayTextWriter ctw = NewTextWriter)
            {
                TextWriter tw = isSynchronized ? TextWriter.Synchronized(ctw) : ctw;
                tw.Write(testData);
                tw.Flush();
                Assert.Equal(testData.ToString(), ctw.Text);
            }
        }

        [Theory]
        [MemberData(nameof(GetStringBuilderTestData))]
        public void WriteLineStringBuilderTest(bool isSynchronized, StringBuilder testData)
        {
            using (CharArrayTextWriter ctw = NewTextWriter)
            {
                TextWriter tw = isSynchronized ? TextWriter.Synchronized(ctw) : ctw;
                tw.WriteLine(testData);
                tw.Flush();
                Assert.Equal(testData.ToString() + tw.NewLine, ctw.Text);
            }
        }

        [Theory]
        [MemberData(nameof(GetStringBuilderTestData))]
        public async void WriteAsyncStringBuilderTest(bool isSynchronized, StringBuilder testData)
        {
            using (CharArrayTextWriter ctw = NewTextWriter)
            {
                TextWriter tw = isSynchronized ? TextWriter.Synchronized(ctw) : ctw;
                await tw.WriteAsync(testData);
                tw.Flush();
                Assert.Equal(testData.ToString(), ctw.Text);
            }
        }

        [Theory]
        [MemberData(nameof(GetStringBuilderTestData))]
        public async void WriteLineAsyncStringBuilderTest(bool isSynchronized, StringBuilder testData)
        {
            using (CharArrayTextWriter ctw = NewTextWriter)
            {
                TextWriter tw = isSynchronized ? TextWriter.Synchronized(ctw) : ctw;
                await tw.WriteLineAsync(testData);
                tw.Flush();
                Assert.Equal(testData + tw.NewLine, ctw.Text);
            }
        }

        [Fact]
        public void DisposeAsync_InvokesDisposeSynchronously()
        {
            bool disposeInvoked = false;
            var tw = new InvokeActionOnDisposeTextWriter() { DisposeAction = () => disposeInvoked = true };
            Assert.False(disposeInvoked);
            Assert.True(tw.DisposeAsync().IsCompletedSuccessfully);
            Assert.True(disposeInvoked);
        }

        [Fact]
        public void DisposeAsync_ExceptionReturnedInTask()
        {
            Exception e = new FormatException();
            var tw = new InvokeActionOnDisposeTextWriter() { DisposeAction = () => { throw e; } };
            ValueTask vt = tw.DisposeAsync();
            Assert.True(vt.IsFaulted);
            Assert.Same(e, vt.AsTask().Exception.InnerException);
        }

        private sealed class InvokeActionOnDisposeTextWriter : TextWriter
        {
            public Action DisposeAction;
            public override Encoding Encoding => Encoding.UTF8;
            protected override void Dispose(bool disposing) => DisposeAction?.Invoke();
        }

        // Generate data for TextWriter.Write* methods that take a stringBuilder.  
        // We test both the synchronized and unsynchronized variation, on strinbuilder swith 0, small and large values.    
        public static IEnumerable<object[]> GetStringBuilderTestData()
        {
            // Make a string that has 10 or so 8K chunks (probably).  
            StringBuilder complexStringBuilder = new StringBuilder();
            for (int i = 0; i < 4000; i++)
                complexStringBuilder.Append(TestDataProvider.CharData); // CharData ~ 25 chars

            foreach (StringBuilder testData in new StringBuilder[] { new StringBuilder(""), new StringBuilder(new string(TestDataProvider.CharData)), complexStringBuilder })
            {
                foreach (bool isSynchronized in new bool[] { true, false })
                {
                    yield return new object[] { isSynchronized, testData };
                }
            }
        }
    }
}
