// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace System.Diagnostics.TextWriterTraceListenerTests
{
    public class TextWriterTraceListener_WriteTestsCtorFileName : TextWriterTraceListener_WriteTestsBase
    {        
        public TextWriterTraceListener_WriteTestsCtorFileName()
        {
            CommonUtilities.DeleteFile(_fileName);            
        }

        public override TextWriterTraceListener GetListener()
        {
            return new TextWriterTraceListener(_fileName);
        }

        protected override void Dispose(bool disposing)
        {            
            base.Dispose(disposing);
        }
    }

    public class TextWriterTraceListener_WriteTestsCtorStream : TextWriterTraceListener_WriteTestsBase
    {
        private readonly Stream _stream;
        public TextWriterTraceListener_WriteTestsCtorStream()
        {
            CommonUtilities.DeleteFile(_fileName);
            _stream = new FileStream(_fileName, FileMode.OpenOrCreate, FileAccess.Write);
        }

        public override TextWriterTraceListener GetListener()
        {
            return new TextWriterTraceListener(_stream);
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

        protected override void Dispose(bool disposing)
        {
            _stream.Dispose();
            base.Dispose(disposing);
        }
    }

    public abstract class TextWriterTraceListener_WriteTestsBase : FileCleanupTestBase
    {
        protected readonly string _fileName;
        private const string TestMessage = "HelloWorld";

        public TextWriterTraceListener_WriteTestsBase()
        {
            _fileName = $"{GetTestFilePath()}.xml";
        }

        public abstract TextWriterTraceListener GetListener();

        [Fact]
        public void TestWrite()
        {
            using (var target = GetListener())
            {
                target.Write(TestMessage);
            }

            Assert.Contains(TestMessage, File.ReadAllText(_fileName));
        }

        [Fact]
        public void TestWriteLine()
        {
            using (var target = GetListener())
            {
                target.WriteLine(TestMessage);
            }

            string expected = TestMessage + Environment.NewLine;
            Assert.Contains(expected, File.ReadAllText(_fileName));
        }

        [Fact]
        public void TestFlush()
        {
            using (var target = GetListener())
            {
                target.Write(TestMessage);
                target.Flush();
            }
        }

        [Fact]
        public void TestWriteAfterDisposeShouldNotThrow()
        {
            var target = GetListener();
            target.Dispose();

            target.WriteLine(TestMessage);
            target.Write(TestMessage);
            target.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
