// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Xunit;

namespace System.Diagnostics.TextWriterTraceListenerTests
{
    public class TextWriterTraceListener_WriteTests : IDisposable
    {
        private readonly Stream _stream;
        private readonly string _fileName;
        private const string TestMessage = "HelloWorld";

        public TextWriterTraceListener_WriteTests()
        {
            _fileName = string.Format("{0}.xml", GetType().Name);
            CommonUtilities.DeleteFile(_fileName);
            _stream = new FileStream(_fileName, FileMode.OpenOrCreate, FileAccess.Write);
        }

        [Fact]
        public void TestWrite()
        {
            using (var target = new TextWriterTraceListener(_stream))
            {
                target.Write(TestMessage);
            }

            Assert.Contains(TestMessage, File.ReadAllText(_fileName));
        }

        [Fact]
        public void TestWriteLine()
        {
            using (var target = new TextWriterTraceListener(_stream))
            {
                target.WriteLine(TestMessage);
            }

            string expected = TestMessage + Environment.NewLine;
            Assert.Contains(expected, File.ReadAllText(_fileName));
        }

        [Fact]
        public void TestFlush()
        {
            using (var target = new TextWriterTraceListener(_stream))
            {
                target.Write(TestMessage);
                target.Flush();
            }
        }

        [Fact]
        public void TestWriteAfterDisposeShouldNotThrow()
        {
            var target = new TextWriterTraceListener(_stream);
            target.Dispose();

            target.WriteLine(TestMessage);
            target.Write(TestMessage);
            target.Flush();
        }

        [Fact]
        public void TestWriterPropery()
        {
            var testWriter = new StreamWriter(_stream);
            using (var target = new TextWriterTraceListener())
            {
                Assert.Null(target.Writer);
                target.Writer = testWriter;
                Assert.NotNull(target.Writer);
                Assert.Same(testWriter, target.Writer);
            }
        }

        public void Dispose()
        {
            _stream.Dispose();
            CommonUtilities.DeleteFile(_fileName);
        }
    }
}
