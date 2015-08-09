// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Xunit;

namespace StreamTests
{
    public class TestLeaveOpen
    {
        [Fact]
        public static void StreamReaderTest()
        {
            MemoryStream ms = new MemoryStream();
            ms.WriteByte((byte)'a');
            ms.Position = 0;
            Assert.True(ms.CanRead, "ERROR: Before testing, MemoryStream's CanRead property was false!  What?");

            // Test leaveOpen.
            StreamReader sr = new StreamReader(ms, System.Text.Encoding.UTF8, true, 1000, true);
            sr.Dispose();
            Assert.True(ms.CanRead, "ERROR: After closing a StreamReader with leaveOpen bool set, MemoryStream's CanRead property was false!");

            // Test not leaving open
            sr = new StreamReader(ms, System.Text.Encoding.UTF8, true, 1000, false);
            sr.Dispose();
            Assert.False(ms.CanRead, "ERROR: After closing a StreamReader with leaveOpen bool not set, MemoryStream's CanRead property was true!");

            // Test default
            ms = new MemoryStream();
            sr = new StreamReader(ms);
            sr.Dispose();
            Assert.False(ms.CanRead, "ERROR: After closing a StreamReader with the default value for leaveOpen, MemoryStream's CanRead property was true!");
        }

        [Fact]
        public static void BinaryReaderTest()
        {
            MemoryStream ms = new MemoryStream();
            ms.WriteByte((byte)'a');
            ms.Position = 0;
            Assert.True(ms.CanRead, "ERROR: Before testing, MemoryStream's CanRead property was false!  What?");

            // Test leaveOpen.
            BinaryReader br = new BinaryReader(ms, System.Text.Encoding.UTF8, true);
            br.Dispose();
            Assert.True(ms.CanRead, "ERROR: After closing a BinaryReader with leaveOpen bool set, MemoryStream's CanRead property was false!");

            // Test not leaving open
            br = new BinaryReader(ms, System.Text.Encoding.UTF8, false);
            br.Dispose();
            Assert.False(ms.CanRead, "ERROR: After closing a BinaryReader with leaveOpen bool not set, MemoryStream's CanRead property was true!");

            // Test default
            ms = new MemoryStream();
            br = new BinaryReader(ms);
            br.Dispose();
            Assert.False(ms.CanRead, "ERROR: After closing a BinaryReader with the default value for leaveOpen, MemoryStream's CanRead property was true!");
        }

        [Fact]
        public static void StreamWriterTest()
        {
            MemoryStream ms = new MemoryStream();
            Assert.True(ms.CanWrite, "ERROR: Before testing, MemoryStream's CanWrite property was false!  What?");

            // Test leaveOpen.
            StreamWriter sw = new StreamWriter(ms, System.Text.Encoding.UTF8, 1000, true);
            sw.Dispose();
            Assert.True(ms.CanWrite, "ERROR: After closing a StreamWriter with leaveOpen bool set, MemoryStream's CanWrite property was false!");

            // Test not leaving open
            sw = new StreamWriter(ms, System.Text.Encoding.UTF8, 1000, false);
            sw.Dispose();
            Assert.False(ms.CanWrite, "ERROR: After closing a StreamWriter with leaveOpen bool not set, MemoryStream's CanWrite property was true!");

            // Test default
            ms = new MemoryStream();
            sw = new StreamWriter(ms);
            sw.Dispose();
            Assert.False( ms.CanWrite, "ERROR: After closing a StreamWriter with the default value for leaveOpen, MemoryStream's CanWrite property was true!");
        }

        [Fact]
        public static void BinaryWriterTest()
        {

            MemoryStream ms = new MemoryStream();
            Assert.True(ms.CanWrite, "ERROR: Before testing, MemoryStream's CanWrite property was false!  What?");

            // Test leaveOpen.
            BinaryWriter bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, true);
            bw.Dispose();
            Assert.True(ms.CanWrite, "ERROR: After closing a BinaryWriterwith leaveOpen bool set, MemoryStream's CanWrite property was false!");

            // Test not leaving open
            bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, false);
            bw.Dispose();
            Assert.False(ms.CanWrite, "ERROR: After closing a BinaryWriterwith leaveOpen bool not set, MemoryStream's CanWrite property was true!");

            // Test default
            ms = new MemoryStream();
            bw = new BinaryWriter(ms);
            bw.Dispose();
            Assert.False(ms.CanWrite, "ERROR: After closing a BinaryWriterwith the default value for leaveOpen, MemoryStream's CanWrite property was true!");
        }
    }
}
