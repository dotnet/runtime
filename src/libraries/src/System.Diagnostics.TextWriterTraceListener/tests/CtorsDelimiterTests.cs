// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Diagnostics.TextWriterTraceListenerTests
{
    public class CtorsDelimiterTests
    {
        private const string DefaultDelimiter = ";";

        [Fact]
        public void TestConstructorWithStream()
        {
            string expectedName = string.Empty;
            var target = new DelimitedListTraceListener(FileStream.Null);
            Assert.Equal(DefaultDelimiter, target.Delimiter);
            Assert.Equal(expectedName, target.Name);
            Assert.NotNull(target.Writer);
        }

        [Fact]
        public void TestConstructorWithNullName()
        {
            string expectedName = string.Empty;
            StreamWriter testWriter = StreamWriter.Null;

            var target = new DelimitedListTraceListener(testWriter, name: null);
            Assert.Equal(DefaultDelimiter, target.Delimiter);
            Assert.Equal(expectedName, target.Name);
            Assert.Same(testWriter, target.Writer);

            target = new DelimitedListTraceListener(FileStream.Null, name: null);
            Assert.Equal(DefaultDelimiter, target.Delimiter);
            Assert.Equal(expectedName, target.Name);
            Assert.NotNull(target.Writer);
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
            var target = new DelimitedListTraceListener(FileStream.Null, testName);
            Assert.Equal(DefaultDelimiter, target.Delimiter);
            Assert.Equal(testName, target.Name);
            Assert.NotNull(target.Writer);
        }

        [Theory]
        [MemberData(nameof(TestNames))]
        public void TestConstructorWithWriterAndName(string testName)
        {
            StreamWriter testWriter = StreamWriter.Null;
            var target = new DelimitedListTraceListener(testWriter, testName);
            Assert.Equal(DefaultDelimiter, target.Delimiter);
            Assert.Equal(testName, target.Name);
            Assert.Same(testWriter, target.Writer);
        }

        [Fact]
        public void TestConstructorWithTextWriter()
        {
            string expectedName = string.Empty;
            StreamWriter testWriter = StreamWriter.Null;
            var target = new DelimitedListTraceListener(testWriter);
            Assert.Equal(DefaultDelimiter, target.Delimiter);
            Assert.Equal(expectedName, target.Name);
            Assert.Same(testWriter, target.Writer);
        }

        [Fact]
        public static void TestDelimiterProperty()
        {
            var target = new DelimitedListTraceListener(FileStream.Null);
            Assert.Equal(DefaultDelimiter, target.Delimiter);
            Assert.Throws<ArgumentNullException>(() => target.Delimiter = null);
            AssertExtensions.Throws<ArgumentException>(null, () => target.Delimiter = string.Empty);
        }

        [Fact]
        public void TestConstructorWithEmptyFileName()
        {
            string expectedName = string.Empty;
            var target = new DelimitedListTraceListener(string.Empty);
            Assert.Throws<ArgumentException>(() => target.Writer);
            Assert.Equal(expectedName, target.Name);
        }

        [Fact]
        public void TestConstructorWithFileName()
        {
            string expectedName = string.Empty;
            var target = new DelimitedListTraceListener(Path.GetTempFileName());
            Assert.NotNull(target.Writer);
            Assert.Equal(expectedName, target.Name);
        }

        [Theory]
        [MemberData(nameof(TestNames))]
        public void TestConstructorWithFileNameAndName(string testName)
        {
            var target = new DelimitedListTraceListener(Path.GetTempFileName(), testName);
            Assert.NotNull(target.Writer);
            Assert.Equal(testName, target.Name);
        }
    }
}
