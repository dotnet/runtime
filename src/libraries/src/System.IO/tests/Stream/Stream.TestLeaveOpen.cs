// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public class TestLeaveOpen
    {
        protected virtual Stream CreateStream()
        {
            return new MemoryStream();
        }

        [Fact]
        public void StreamReaderTest()
        {
            Stream ms = CreateStream();
            ms.WriteByte((byte)'a');
            ms.Position = 0;
            Assert.True(ms.CanRead, "ERROR: Before testing, MemoryStream's CanRead property was false!  What?");

            // Test leaveOpen with calling Dispose
            StreamReader sr = new StreamReader(ms, System.Text.Encoding.UTF8, true, 1000, true);
            sr.Dispose();
            Assert.True(ms.CanRead, "ERROR: After disposing a StreamReader with leaveOpen bool set, MemoryStream's CanRead property was false!");

            // Test leaveOpen with calling Close
            sr = new StreamReader(ms, System.Text.Encoding.UTF8, true, 1000, true);
            sr.Close();
            Assert.True(ms.CanRead, "ERROR: After closing a StreamReader with leaveOpen bool set, MemoryStream's CanRead property was false!");

            // Test not leaving open
            sr = new StreamReader(ms, System.Text.Encoding.UTF8, true, 1000, false);
            sr.Dispose();
            Assert.False(ms.CanRead, "ERROR: After closing a StreamReader with leaveOpen bool not set, MemoryStream's CanRead property was true!");

            // Test not leaving open
            ms = CreateStream();
            sr = new StreamReader(ms, System.Text.Encoding.UTF8, true, 1000, false);
            sr.Close();
            Assert.False(ms.CanRead, "ERROR: After closing a StreamReader with leaveOpen bool not set, MemoryStream's CanRead property was true!");

            // Test default
            ms = CreateStream();
            sr = new StreamReader(ms);
            sr.Dispose();
            Assert.False(ms.CanRead, "ERROR: After disposing a StreamReader with the default value for leaveOpen, MemoryStream's CanRead property was true!");

            // Test default
            ms = CreateStream();
            sr = new StreamReader(ms);
            sr.Close();
            Assert.False(ms.CanRead, "ERROR: After closing a StreamReader with the default value for leaveOpen, MemoryStream's CanRead property was true!");
        }

        [Fact]
        public void BinaryReaderTest()
        {
            Stream ms = CreateStream();
            ms.WriteByte((byte)'a');
            ms.Position = 0;
            Assert.True(ms.CanRead, "ERROR: Before testing, MemoryStream's CanRead property was false!  What?");

            // Test leaveOpen with calling Dispose
            BinaryReader br = new BinaryReader(ms, System.Text.Encoding.UTF8, true);
            br.Dispose();
            Assert.True(ms.CanRead, "ERROR: After disposing a BinaryReader with leaveOpen bool set, MemoryStream's CanRead property was false!");

            // Test leaveOpen with calling Close
            br = new BinaryReader(ms, System.Text.Encoding.UTF8, true);
            br.Close();
            Assert.True(ms.CanRead, "ERROR: After closing a BinaryReader with leaveOpen bool set, MemoryStream's CanRead property was false!");

            // Test not leaving open with calling Dispose
            br = new BinaryReader(ms, System.Text.Encoding.UTF8, false);
            br.Dispose();
            Assert.False(ms.CanRead, "ERROR: After disposing a BinaryReader with leaveOpen bool not set, MemoryStream's CanRead property was true!");

            // Test not leaving open with calling Close
            ms = CreateStream();
            br = new BinaryReader(ms, System.Text.Encoding.UTF8, false);
            br.Close();
            Assert.False(ms.CanRead, "ERROR: After closing a BinaryReader with leaveOpen bool not set, MemoryStream's CanRead property was true!");

            // Test default with calling Dispose
            ms = CreateStream();
            br = new BinaryReader(ms);
            br.Dispose();
            Assert.False(ms.CanRead, "ERROR: After disposing a BinaryReader with the default value for leaveOpen, MemoryStream's CanRead property was true!");

            // Test default with calling Close
            ms = CreateStream();
            br = new BinaryReader(ms);
            br.Close();
            Assert.False(ms.CanRead, "ERROR: After closing a BinaryReader with the default value for leaveOpen, MemoryStream's CanRead property was true!");
        }

        [Fact]
        public void StreamWriterTest()
        {
            Stream ms = CreateStream();
            Assert.True(ms.CanWrite, "ERROR: Before testing, MemoryStream's CanWrite property was false!  What?");

            // Test leaveOpen with calling Dispose
            StreamWriter sw = new StreamWriter(ms, System.Text.Encoding.UTF8, 1000, true);
            sw.Dispose();
            Assert.True(ms.CanWrite, "ERROR: After disposing a StreamWriter with leaveOpen bool set, MemoryStream's CanWrite property was false!");

            // Test leaveOpen with calling Close
            sw = new StreamWriter(ms, System.Text.Encoding.UTF8, 1000, true);
            sw.Close();
            Assert.True(ms.CanWrite, "ERROR: After closing a StreamWriter with leaveOpen bool set, MemoryStream's CanWrite property was false!");

            // Test not leaving open with calling Dispose
            sw = new StreamWriter(ms, System.Text.Encoding.UTF8, 1000, false);
            sw.Dispose();
            Assert.False(ms.CanWrite, "ERROR: After disposing a StreamWriter with leaveOpen bool not set, MemoryStream's CanWrite property was true!");

            // Test not leaving open with calling Close
            ms = CreateStream();
            sw = new StreamWriter(ms, System.Text.Encoding.UTF8, 1000, false);
            sw.Close();
            Assert.False(ms.CanWrite, "ERROR: After closing a StreamWriter with leaveOpen bool not set, MemoryStream's CanWrite property was true!");

            // Test default with calling Dispose
            ms = CreateStream();
            sw = new StreamWriter(ms);
            sw.Dispose();
            Assert.False( ms.CanWrite, "ERROR: After disposing a StreamWriter with the default value for leaveOpen, MemoryStream's CanWrite property was true!");

            // Test default with calling Close
            ms = CreateStream();
            sw = new StreamWriter(ms);
            sw.Close();
            Assert.False(ms.CanWrite, "ERROR: After closing a StreamWriter with the default value for leaveOpen, MemoryStream's CanWrite property was true!");
        }

        [Fact]
        public void BinaryWriterTest()
        {

            Stream ms = CreateStream();
            Assert.True(ms.CanWrite, "ERROR: Before testing, MemoryStream's CanWrite property was false!  What?");

            // Test leaveOpen with calling Dispose
            BinaryWriter bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, true);
            bw.Dispose();
            Assert.True(ms.CanWrite, "ERROR: After disposing a BinaryWriterwith leaveOpen bool set, MemoryStream's CanWrite property was false!");

            // Test leaveOpen with calling Close
            bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, true);
            bw.Close();
            Assert.True(ms.CanWrite, "ERROR: After closing a BinaryWriterwith leaveOpen bool set, MemoryStream's CanWrite property was false!");

            // Test not leaving open with calling Dispose
            bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, false);
            bw.Dispose();
            Assert.False(ms.CanWrite, "ERROR: After disposing a BinaryWriterwith leaveOpen bool not set, MemoryStream's CanWrite property was true!");

            // Test not leaving open with calling Close
            ms = CreateStream();
            bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, false);
            bw.Close();
            Assert.False(ms.CanWrite, "ERROR: After closing a BinaryWriterwith leaveOpen bool not set, MemoryStream's CanWrite property was true!");

            // Test default with calling Dispose
            ms = CreateStream();
            bw = new BinaryWriter(ms);
            bw.Dispose();
            Assert.False(ms.CanWrite, "ERROR: After disposing a BinaryWriterwith the default value for leaveOpen, MemoryStream's CanWrite property was true!");

            // Test default with calling Close
            ms = CreateStream();
            bw = new BinaryWriter(ms);
            bw.Close();
            Assert.False(ms.CanWrite, "ERROR: After closing a BinaryWriterwith the default value for leaveOpen, MemoryStream's CanWrite property was true!");
        }
    }
}
