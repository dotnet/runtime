// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public partial class WriteTests
    {
        [Fact]
        public void Synchronized_NewObject()
        {
            using (Stream str = CreateStream())
            {
                StreamWriter writer = new StreamWriter(str);
                TextWriter synced = TextWriter.Synchronized(writer);
                Assert.NotEqual(writer, synced);
                writer.Write("app");
                synced.Write("pad");

                writer.Flush();
                synced.Flush();

                str.Position = 0;
                StreamReader reader = new StreamReader(str);
                Assert.Equal("apppad", reader.ReadLine());
            }
        }

        private class TestFormatWriter : StreamWriter
        {
            public int WriteCalls { get; private set; }
            public int WriteLineCalls { get; private set; }

            public TestFormatWriter(Stream stream) : base(stream)
            { }

            public override void Write(string value)
            {
                WriteCalls++;
                base.Write(value);
            }

            public override void WriteLine(string value)
            {
                WriteLineCalls++;
                base.WriteLine(value);
            }
        }

        [Fact]
        public void FormatOverloadsCallWrite()
        {
            TestFormatWriter writer = new TestFormatWriter(new MemoryStream());
            writer.Write("{0}", "Zero");
            Assert.Equal(1, writer.WriteCalls);
            writer.Write("{0}{1}", "Zero", "One");
            Assert.Equal(2, writer.WriteCalls);
            writer.Write("{0}{1}{2}", "Zero", "One", "Two");
            Assert.Equal(3, writer.WriteCalls);
            writer.Write("{0}{1}{2}{3}", "Zero", "One", "Two", "Three");
            Assert.Equal(4, writer.WriteCalls);
            writer.Write("{0}{1}{2}{3}{4}", "Zero", "One", "Two", "Three", "Four");
            Assert.Equal(5, writer.WriteCalls);
            writer.WriteLine("{0}", "Zero");
            Assert.Equal(1, writer.WriteLineCalls);
            writer.WriteLine("{0}{1}", "Zero", "One");
            Assert.Equal(2, writer.WriteLineCalls);
            writer.WriteLine("{0}{1}{2}", "Zero", "One", "Two");
            Assert.Equal(3, writer.WriteLineCalls);
            writer.WriteLine("{0}{1}{2}{3}", "Zero", "One", "Two", "Three");
            Assert.Equal(4, writer.WriteLineCalls);
            writer.WriteLine("{0}{1}{2}{3}{4}", "Zero", "One", "Two", "Three", "Four");
            Assert.Equal(5, writer.WriteLineCalls);
        }
    }
}
