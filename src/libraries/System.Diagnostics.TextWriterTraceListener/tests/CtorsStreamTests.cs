// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Diagnostics.TextWriterTraceListenerTests
{
    public class CtorsStreamTests
    {
        [Fact]
        public void NegativeTests()
        {
            Assert.Throws<ArgumentNullException>(() => new TextWriterTraceListener(stream: null));

            var closedStream = new MemoryStream();
            // We should call Close method here but for some reason we receive
            // error CS1061: 'System.IO.MemoryStream' does not contain a definition for 'Close'.
            // Calling Dispose should have same effect as calling Close.
            closedStream.Dispose();
            AssertExtensions.Throws<ArgumentException>(null, () => new TextWriterTraceListener(closedStream));

            var readOnlyStream = new MemoryStream(new byte[256], writable: false);
            AssertExtensions.Throws<ArgumentException>(null, () => new TextWriterTraceListener(readOnlyStream));
        }

        [Fact]
        public void TestDefaultConstructor()
        {
            string expectedName = string.Empty;
            using (var target = new TextWriterTraceListener())
            {
                Assert.Equal(expectedName, target.Name);
                Assert.Null(target.Writer);
            }
        }

        [Fact]
        public void TestConstructorWithStream()
        {
            string expectedName = string.Empty;
            using (var target = new TextWriterTraceListener(FileStream.Null))
            {
                Assert.Equal(expectedName, target.Name);
                Assert.NotNull(target.Writer);
            }
        }

        [Fact]
        public void TestConstructorWithTextWriter()
        {
            string expectedName = string.Empty;
            StreamWriter testWriter = StreamWriter.Null;
            using (var target = new TextWriterTraceListener(testWriter))
            {
                Assert.Equal(expectedName, target.Name);
                Assert.Same(testWriter, target.Writer);
            }
        }

        [Fact]
        public void TestConstructorWithNullName()
        {
            string expectedName = string.Empty;
            StreamWriter testWriter = StreamWriter.Null;

            using (var target = new TextWriterTraceListener(testWriter, name: null))
            {
                Assert.Equal(expectedName, target.Name);
                Assert.Same(testWriter, target.Writer);
            }

            using (var target = new TextWriterTraceListener(FileStream.Null, name: null))
            {
                Assert.Equal(expectedName, target.Name);
                Assert.NotNull(target.Writer);
            }
        }

        public static IEnumerable<object[]> TestNames
        {
            get
            {
                return new[]
                {
                    new object[] { "MyXMLTraceWriter" },
                    new object[] { string.Empty },
                    new object[] { new string('a', 100000) },
                    new object[] { "hell0<" },
                    new object[] { "><&" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TestNames))]
        public void TestConstructorWithStreamAndName(string testName)
        {
            StreamWriter testWriter = StreamWriter.Null;

            using (var target = new TextWriterTraceListener(testWriter, testName))
            {
                Assert.Equal(testName, target.Name);
                Assert.Same(testWriter, target.Writer);
            }

            using (var target = new TextWriterTraceListener(FileStream.Null, testName))
            {
                Assert.Equal(testName, target.Name);
                Assert.NotNull(target.Writer);
            }
        }
    }
}
