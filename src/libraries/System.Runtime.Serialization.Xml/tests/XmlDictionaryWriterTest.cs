// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

public static class XmlDictionaryWriterTest
{
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
    public static void XmlBaseWriter_WriteBase64Async()
    {
        string actual;
        int byteSize = 1024;
        byte[] bytes = GetByteArray(byteSize);
        string expect = GetExpectString(bytes, byteSize);
        using (var ms = new AsyncMemoryStream())
        {
            var writer = XmlDictionaryWriter.CreateTextWriter(ms);
            writer.WriteStartDocument();
            writer.WriteStartElement("data");
            var task = writer.WriteBase64Async(bytes, 0, byteSize);
            task.Wait();
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            ms.Position = 0;
            var sr = new StreamReader(ms);
            actual = sr.ReadToEnd();
        }

        Assert.Equal(expect, actual);
    }

    [Fact]
    public static void XmlBaseWriter_WriteBinHex()
    {
        var str = "The quick brown fox jumps over the lazy dog.";
        var bytes = Encoding.Unicode.GetBytes(str);
        string expect = @"<data>540068006500200071007500690063006B002000620072006F0077006E00200066006F00780020006A0075006D007000730020006F00760065007200200074006800650020006C0061007A007900200064006F0067002E00</data>";
        string actual;
        using (var ms = new MemoryStream())
        {
            var writer = XmlDictionaryWriter.CreateTextWriter(ms);
            writer.WriteStartElement("data");
            writer.WriteBinHex(bytes, 0, bytes.Length);
            writer.WriteEndElement();
            writer.Flush();
            ms.Position = 0;
            var sr = new StreamReader(ms);
            actual = sr.ReadToEnd();
        }

        Assert.Equal(expect, actual);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
    public static void XmlBaseWriter_FlushAsync()
    {
        string actual = null;
        int byteSize = 1024;
        byte[] bytes = GetByteArray(byteSize);
        string expect = GetExpectString(bytes, byteSize);
        string lastCompletedOperation = null;
        try
        {
            using (var ms = new AsyncMemoryStream())
            {
                var writer = XmlDictionaryWriter.CreateTextWriter(ms);
                lastCompletedOperation = "XmlDictionaryWriter.CreateTextWriter()";

                writer.WriteStartDocument();
                lastCompletedOperation = "writer.WriteStartDocument()";

                writer.WriteStartElement("data");
                lastCompletedOperation = "writer.WriteStartElement()";

                writer.WriteBase64(bytes, 0, byteSize);
                lastCompletedOperation = "writer.WriteBase64()";

                writer.WriteEndElement();
                lastCompletedOperation = "writer.WriteEndElement()";

                writer.WriteEndDocument();
                lastCompletedOperation = "writer.WriteEndDocument()";

                var task = writer.FlushAsync();
                lastCompletedOperation = "writer.FlushAsync()";

                task.Wait();
                ms.Position = 0;
                var sr = new StreamReader(ms);
                actual = sr.ReadToEnd();
            }
        }
        catch (Exception e)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"An error occurred: {e.Message}");
            sb.AppendLine(e.StackTrace);
            sb.AppendLine();
            sb.AppendLine($"The last completed operation before the exception was: {lastCompletedOperation}");
            Assert.True(false, sb.ToString());
        }

        Assert.Equal(expect, actual);

    }

    [Fact]
    public static void XmlBaseWriter_WriteStartEndElementAsync()
    {
        string actual;
        int byteSize = 1024;
        byte[] bytes = GetByteArray(byteSize);
        string expect = GetExpectString(bytes, byteSize);
        using (var ms = new AsyncMemoryStream())
        {
            var writer = XmlDictionaryWriter.CreateTextWriter(ms);
            writer.WriteStartDocument();
            // NOTE: the async method has only one overload that takes 3 params
            var t1 = writer.WriteStartElementAsync(null, "data", null);
            t1.Wait();
            writer.WriteBase64(bytes, 0, byteSize);
            var t2 = writer.WriteEndElementAsync();
            t2.Wait();
            writer.WriteEndDocument();
            writer.Flush();
            ms.Position = 0;
            var sr = new StreamReader(ms);
            actual = sr.ReadToEnd();
        }

        Assert.Equal(expect, actual);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
    public static void XmlBaseWriter_CheckAsync_ThrowInvalidOperationException()
    {
        int byteSize = 1024;
        byte[] bytes = GetByteArray(byteSize);
        using (var ms = new MemoryStreamWithBlockAsync())
        {
            var writer = XmlDictionaryWriter.CreateTextWriter(ms);
            writer.WriteStartDocument();
            writer.WriteStartElement("data");

            ms.blockAsync(true);
            var t1 = writer.WriteBase64Async(bytes, 0, byteSize);
            var t2 = Assert.ThrowsAsync<InvalidOperationException>(() => writer.WriteBase64Async(bytes, 0, byteSize));

            InvalidOperationException e = t2.Result;
            bool isAsyncIsRunningException = e.Message.Contains("XmlAsyncIsRunningException") || e.Message.Contains("in progress");
            Assert.True(isAsyncIsRunningException, "The exception is not XmlAsyncIsRunningException.");

            // let the first task complete
            ms.blockAsync(false);
            t1.Wait();
        }
    }

    [Fact]
    public static void XmlDictionaryWriter_InvalidUnicodeChar()
    {
        using (var ms = new MemoryStream())
        {
            var writer = XmlDictionaryWriter.CreateTextWriter(ms);
            writer.WriteStartDocument();
            writer.WriteStartElement("data");

            // This is an invalid char. Writing this char shouldn't
            // throw exception.
            writer.WriteString("\uDB1B");

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            ms.Position = 0;
        }
    }

    [Fact]
    public static void CreateMtomReaderWriter_Throw_PNSE()
    {
        using (var stream = new MemoryStream())
        {
            string startInfo = "application/soap+xml";
            Assert.Throws<PlatformNotSupportedException>(() => XmlDictionaryWriter.CreateMtomWriter(stream, Encoding.UTF8, int.MaxValue, startInfo));
        }
    }

    [Fact]
    public static void CreateTextReaderWriterTest()
    {
        string expected = "<localName>the value</localName>";
        using (MemoryStream stream = new MemoryStream())
        {
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(stream, Encoding.UTF8, false))
            {
                writer.WriteElementString("localName", "the value");
                writer.Flush();
                byte[] bytes = stream.ToArray();
                StreamReader reader = new StreamReader(stream);
                stream.Position = 0;
                string content = reader.ReadToEnd();
                Assert.Equal(expected, content);
                reader.Close();

                using (XmlDictionaryReader xreader = XmlDictionaryReader.CreateTextReader(bytes, new XmlDictionaryReaderQuotas()))
                {
                    xreader.Read();
                    string xml = xreader.ReadOuterXml();
                    Assert.Equal(expected, xml);
                }
            }
        }
    }

    [Fact]
    public static void StreamProvoiderTest()
    {
        List<string> ReaderWriterType = new List<string>
            {
                "Binary",
                //"MTOM", //MTOM methods not supported now.
                //"MTOM",
                //"MTOM",
                "Text",
                "Text",
                "Text"
            };

        List<string> Encodings = new List<string>
            {
                "utf-8",
                "utf-8",
                "utf-16",
                "unicodeFFFE",
                "utf-8",
                "utf-16",
                "unicodeFFFE"
            };

        for (int i = 0; i < ReaderWriterType.Count; i++)
        {
            string rwTypeStr = ReaderWriterType[i];
            ReaderWriterFactory.ReaderWriterType rwType = (ReaderWriterFactory.ReaderWriterType)
            Enum.Parse(typeof(ReaderWriterFactory.ReaderWriterType), rwTypeStr, true);
            Encoding encoding = Encoding.GetEncoding(Encodings[i]);

            Random rndGen = new Random();
            int byteArrayLength = rndGen.Next(100, 2000);
            byte[] byteArray = new byte[byteArrayLength];
            rndGen.NextBytes(byteArray);
            MyStreamProvider myStreamProvider = new MyStreamProvider(new MemoryStream(byteArray));
            bool success = false;
            bool successBase64 = false;
            MemoryStream ms = new MemoryStream();
            success = WriteTest(ms, rwType, encoding, myStreamProvider);
            Assert.True(success);
            success = ReadTest(ms, encoding, rwType, byteArray);
            Assert.True(success);
            if (rwType == ReaderWriterFactory.ReaderWriterType.Text)
            {
                ms = new MemoryStream();
                myStreamProvider = new MyStreamProvider(new MemoryStream(byteArray));
                success = AsyncWriteTest(ms, encoding, myStreamProvider);
                Assert.True(success);
                successBase64 = AsyncWriteBase64Test(ms, byteArray, encoding, myStreamProvider);
                Assert.True(successBase64);
            }
        }
    }

    [Fact]
    public static void IXmlBinaryReaderWriterInitializerTest()
    {
        DataContractSerializer serializer = new DataContractSerializer(typeof(TestData));
        MemoryStream ms = new MemoryStream();
        TestData td = new TestData();
        XmlDictionaryWriter binaryWriter = XmlDictionaryWriter.CreateBinaryWriter(ms, null, null, false);
        IXmlBinaryWriterInitializer writerInitializer = (IXmlBinaryWriterInitializer)binaryWriter;
        writerInitializer.SetOutput(ms, null, null, false);
        serializer.WriteObject(ms, td);
        binaryWriter.Flush();
        byte[] xmlDoc = ms.ToArray();
        binaryWriter.Close();
        XmlDictionaryReader binaryReader = XmlDictionaryReader.CreateBinaryReader(xmlDoc, 0, xmlDoc.Length, null, XmlDictionaryReaderQuotas.Max, null, new OnXmlDictionaryReaderClose((XmlDictionaryReader reader) => { }));
        IXmlBinaryReaderInitializer readerInitializer = (IXmlBinaryReaderInitializer)binaryReader;
        readerInitializer.SetInput(xmlDoc, 0, xmlDoc.Length, null, XmlDictionaryReaderQuotas.Max, null, new OnXmlDictionaryReaderClose((XmlDictionaryReader reader) => { }));
        binaryReader.ReadContentAsObject();
        binaryReader.Close();
    }

    [Fact]
    public static void IXmlTextReaderInitializerTest()
    {
        var writer = new SampleTextWriter();
        var ms = new MemoryStream();
        var encoding = Encoding.UTF8;
        writer.SetOutput(ms, encoding, true);
    }

    [Fact]
    public static void FragmentTest()
    {
        string rwTypeStr = "Text";
        ReaderWriterFactory.ReaderWriterType rwType = (ReaderWriterFactory.ReaderWriterType)
            Enum.Parse(typeof(ReaderWriterFactory.ReaderWriterType), rwTypeStr, true);
        Encoding encoding = Encoding.GetEncoding("utf-8");
        MemoryStream ms = new MemoryStream();
        XmlDictionaryWriter writer = (XmlDictionaryWriter)ReaderWriterFactory.CreateXmlWriter(rwType, ms, encoding);
        Assert.False(FragmentHelper.CanFragment(writer));
    }

    [Fact]
    public static void BinaryWriter_PrimitiveTypes()
    {
        using MemoryStream ms = new();
        using XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(ms);
        writer.WriteStartElement("root");

        AssertBytesWritten(x => x.WriteValue((byte)0x78), XmlBinaryNodeType.Int8Text, new byte[] { 0x78 });
        AssertBytesWritten(x => x.WriteValue((short)0x1234), XmlBinaryNodeType.Int16Text, new byte[] { 0x34, 0x12 });
        AssertBytesWritten(x => x.WriteValue(unchecked((short)0xf234)), XmlBinaryNodeType.Int16Text, new byte[] { 0x34, 0xf2 });
        AssertBytesWritten(x => x.WriteValue((int)0x12345678), XmlBinaryNodeType.Int32Text, new byte[] { 0x78, 0x56, 0x34, 0x12 });
        AssertBytesWritten(x => x.WriteValue((long)0x0102030412345678), XmlBinaryNodeType.Int64Text, new byte[] { 0x78, 0x56, 0x34, 0x12, 04, 03, 02, 01 });

        // Integer values should be represented using smalles possible type
        AssertBytesWritten(x => x.WriteValue((long)0), XmlBinaryNodeType.ZeroText, Span<byte>.Empty);
        AssertBytesWritten(x => x.WriteValue((long)1), XmlBinaryNodeType.OneText, Span<byte>.Empty);
        AssertBytesWritten(x => x.WriteValue((int)0x00000078), XmlBinaryNodeType.Int8Text, new byte[] { 0x78 });
        AssertBytesWritten(x => x.WriteValue(unchecked((int)0xfffffff0)), XmlBinaryNodeType.Int8Text, new byte[] { 0xf0 });
        AssertBytesWritten(x => x.WriteValue((int)0x00001234), XmlBinaryNodeType.Int16Text, new byte[] { 0x34, 0x12 });
        AssertBytesWritten(x => x.WriteValue(unchecked((int)0xfffff234)), XmlBinaryNodeType.Int16Text, new byte[] { 0x34, 0xf2 });
        AssertBytesWritten(x => x.WriteValue((long)0x12345678), XmlBinaryNodeType.Int32Text, new byte[] { 0x78, 0x56, 0x34, 0x12 });
        AssertBytesWritten(x => x.WriteValue(unchecked((long)0xfffffffff2345678)), XmlBinaryNodeType.Int32Text, new byte[] { 0x78, 0x56, 0x34, 0xf2 });

        float f = 1.23456788f;
        ReadOnlySpan<byte> floatBytes = new byte[] { 0x52, 0x06, 0x9e, 0x3f };
        double d = 1.0 / 3.0;
        ReadOnlySpan<byte> doubleBytes = new byte[] { 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0xd5, 0x3f };
        Guid guid = new Guid(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });
        DateTime datetime = new DateTime(2022, 8, 26, 12, 34, 56, DateTimeKind.Utc);
        Span<byte> datetimeBytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(datetimeBytes, datetime.ToBinary());

        AssertBytesWritten(x => x.WriteValue(f), XmlBinaryNodeType.FloatText, floatBytes);
        AssertBytesWritten(x => x.WriteValue(new decimal(0x20212223, 0x10111213, 0x01020304, true, scale: 0x1b)), XmlBinaryNodeType.DecimalText,
                           new byte[] { 0x0, 0x0, 0x1b, 0x80, 0x4, 0x3, 0x2, 0x1, 0x23, 0x22, 0x21, 0x20, 0x13, 0x12, 0x11, 0x10 });
        AssertBytesWritten(x => x.WriteValue(guid), XmlBinaryNodeType.GuidText, guid.ToByteArray());
        AssertBytesWritten(x => x.WriteValue(new TimeSpan(0x0807060504030201)), XmlBinaryNodeType.TimeSpanText, new byte[] { 01, 02, 03, 04, 05, 06, 07, 08 });
        AssertBytesWritten(x => x.WriteValue(datetime), XmlBinaryNodeType.DateTimeText, datetimeBytes);

        // Double can be represented as float or int as long as no detail is lost
        AssertBytesWritten(x => x.WriteValue((double)f), XmlBinaryNodeType.FloatText, floatBytes);
        AssertBytesWritten(x => x.WriteValue((double)0x0100), XmlBinaryNodeType.Int16Text, new byte[] { 0x00, 0x01 });
        AssertBytesWritten(x => x.WriteValue(d), XmlBinaryNodeType.DoubleText, doubleBytes);


        void AssertBytesWritten(Action<XmlDictionaryWriter> action, XmlBinaryNodeType nodeType, ReadOnlySpan<byte> expected)
        {
            writer.WriteStartElement("a");

            // Reset stream so we only compare the actual value written (including end element)
            writer.Flush();
            ms.Position = 0;
            ms.SetLength(0);

            action(writer);

            writer.Flush();
            ms.TryGetBuffer(out ArraySegment<byte> segement);
            Assert.Equal(nodeType, (XmlBinaryNodeType)segement[0]);
            AssertExtensions.SequenceEqual(expected, segement.AsSpan(1));
            writer.WriteEndElement();
        }
    }


    [Fact]
    public static void BinaryWriter_Arrays()
    {
        using var ms = new MemoryStream();
        using var writer = XmlDictionaryWriter.CreateBinaryWriter(ms);
        writer.WriteStartElement("root");
        int offset = 1;
        int count = 2;

        bool[] bools = new bool[] { false, true, false, true };
        AssertBytesWritten(x => x.WriteArray(null, "a", null, bools, offset, count), XmlBinaryNodeType.BoolTextWithEndElement,
                           count, new byte[] { 1, 0 });

        short[] shorts = new short[] { -1, 0x0102, 0x1122, -1 };
        AssertBytesWritten(x => x.WriteArray(null, "a", null, shorts, offset, count), XmlBinaryNodeType.Int16TextWithEndElement,
                           count, new byte[] { 2, 1, 0x22, 0x11 });

        int[] ints = new int[] { -1, 0x01020304, 0x11223344, -1 };
        AssertBytesWritten(x => x.WriteArray(null, "a", null, ints, offset, count), XmlBinaryNodeType.Int32TextWithEndElement,
                           count, new byte[] { 4, 3, 2, 1, 0x44, 0x33, 0x22, 0x11 });

        long[] longs = new long[] { -1, 0x0102030405060708, 0x1122334455667788, -1 };
        AssertBytesWritten(x => x.WriteArray(null, "a", null, longs, offset, count), XmlBinaryNodeType.Int64TextWithEndElement,
                           count, new byte[] { 8, 7, 6, 5, 4, 3, 2, 1, 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11 });

        float[] floats = new float[] { -1.0f, 1.23456788f, 1.23456788f, -1.0f };
        AssertBytesWritten(x => x.WriteArray(null, "a", null, floats, offset, count), XmlBinaryNodeType.FloatTextWithEndElement,
                           count, new byte[] { 0x52, 0x06, 0x9e, 0x3f, 0x52, 0x06, 0x9e, 0x3f });

        double[] doubles = new double[] { -1.0, 1.0 / 3.0, 1.0 / 3.0, -1.0 };
        AssertBytesWritten(x => x.WriteArray(null, "a", null, doubles, offset, count), XmlBinaryNodeType.DoubleTextWithEndElement,
                           count, new byte[] { 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0xd5, 0x3f,
                                               0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0xd5, 0x3f });

        decimal[] decimals = new[] {
            new decimal(0x20212223, 0x10111213, 0x01020304, true, scale: 0x1b),
            new decimal(0x50515253, 0x40414243, 0x31323334, false, scale: 0x1c)
        };
        AssertBytesWritten(x => x.WriteArray(null, "a", null, decimals, 0, decimals.Length), XmlBinaryNodeType.DecimalTextWithEndElement,
                           decimals.Length, new byte[] { 0x0, 0x0, 0x1b, 0x80, 0x4, 0x3, 0x2, 0x1,
                                                         0x23, 0x22, 0x21, 0x20, 0x13, 0x12, 0x11, 0x10,
                                                         0x0, 0x0, 0x1c, 0x00, 0x34, 0x33, 0x32, 0x31,
                                                         0x53, 0x52, 0x51, 0x50, 0x43, 0x42, 0x41, 0x40 });

        DateTime[] datetimes = new[] {
            new DateTime(2022, 8, 26, 12, 34, 56, DateTimeKind.Utc),
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local)
        };
        Span<byte> datetimeBytes = stackalloc byte[8 * datetimes.Length];
        for (int i = 0; i < datetimes.Length; i++)
        {
            BinaryPrimitives.WriteInt64LittleEndian(datetimeBytes.Slice(8 * i), datetimes[i].ToBinary());
        }
        AssertBytesWritten(x => x.WriteArray(null, "a", null, datetimes, 0, datetimes.Length), XmlBinaryNodeType.DateTimeTextWithEndElement,
                           datetimes.Length, datetimeBytes);

        TimeSpan[] timespans = new[] { new TimeSpan(0x0807060504030201), new TimeSpan(0x1817161514131211) };
        AssertBytesWritten(x => x.WriteArray(null, "a", null, timespans, 0, timespans.Length), XmlBinaryNodeType.TimeSpanTextWithEndElement,
                           timespans.Length, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                                                          0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18 });

        Guid[] guids = new Guid[]
        {
            new Guid(new ReadOnlySpan<byte>(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 })),
            new Guid(new ReadOnlySpan<byte>(new byte[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160 }))
        };
        AssertBytesWritten(x => x.WriteArray(null, "a", null, guids, 0, guids.Length), XmlBinaryNodeType.GuidTextWithEndElement,
                           guids.Length, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
                                                      10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160 });

        // Write more than 512 bytes in a single call to trigger different writing logic in XmlStreamNodeWriter.WriteBytes
        long[] many_longs = Enumerable.Range(0x01020304, 127).Select(i => (long)i | (long)(~i << 32)).ToArray();
        Span<byte> many_longBytes = stackalloc byte[8 * many_longs.Length];
        for (int i = 0; i < many_longs.Length; i++)
        {
            BinaryPrimitives.WriteInt64LittleEndian(many_longBytes.Slice(8 * i), many_longs[i]);
        }
        AssertBytesWritten(x => x.WriteArray(null, "a", null, many_longs, 0, many_longs.Length), XmlBinaryNodeType.Int64TextWithEndElement,
                           many_longs.Length, many_longBytes);

        void AssertBytesWritten(Action<XmlDictionaryWriter> action, XmlBinaryNodeType nodeType, int count, ReadOnlySpan<byte> expected)
        {
            // Reset stream so we only compare the actual value written (including end element)
            writer.Flush();
            ms.Position = 0;
            ms.SetLength(0);

            action(writer);

            writer.Flush();
            ms.TryGetBuffer(out ArraySegment<byte> segement);

            var actual = segement.AsSpan();
            Assert.Equal(XmlBinaryNodeType.Array, (XmlBinaryNodeType)actual[0]);
            Assert.Equal(XmlBinaryNodeType.ShortElement, (XmlBinaryNodeType)actual[1]);
            int elementLength = actual[2];
            Assert.InRange(elementLength, 0, 0x8f); // verify count is single byte
            Assert.Equal(XmlBinaryNodeType.EndElement, (XmlBinaryNodeType)actual[3 + elementLength]);

            actual = actual.Slice(4 + elementLength);
            // nodetype and count
            Assert.Equal(nodeType, (XmlBinaryNodeType)actual[0]);
            Assert.Equal(checked((sbyte)count), (sbyte)actual[1]);

            AssertExtensions.SequenceEqual(expected, actual.Slice(2));
        }
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/85013", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
    public static void XmlBaseWriter_WriteString()
    {
        const byte Chars8Text = 152;
        const byte Chars16Text = 154;
        MemoryStream ms = new MemoryStream();
        XmlDictionaryWriter writer = (XmlDictionaryWriter)XmlDictionaryWriter.CreateBinaryWriter(ms);
        writer.WriteStartElement("root");

        int[] lengths = new[] { 7, 8, 9, 15, 16, 17, 31, 32, 36, 258 };
        byte[] buffer = new byte[lengths.Max() + 1];

        foreach (var length in lengths)
        {
            string allAscii = string.Create(length, null, (Span<char> chars, object _) =>
            {
                for (int i = 0; i < chars.Length; ++i)
                    chars[i] = (char)(i % 128);
            });
            string multiByteLast = string.Create(length, null, (Span<char> chars, object _) =>
            {
                for (int i = 0; i < chars.Length; ++i)
                    chars[i] = (char)(i % 128);
                chars[^1] = '\u00E4'; // 'ä' - Latin Small Letter a with Diaeresis. Latin-1 Supplement.
            });

            int numBytes = Encoding.UTF8.GetBytes(allAscii, buffer);
            Assert.True(numBytes == length, "Test setup wrong - allAscii");
            ValidateWriteText(ms, writer, allAscii, expected: buffer.AsSpan(0, numBytes));

            numBytes = Encoding.UTF8.GetBytes(multiByteLast, buffer);
            Assert.True(numBytes == length + 1, "Test setup wrong - multiByte");
            ValidateWriteText(ms, writer, multiByteLast, expected: buffer.AsSpan(0, numBytes));
        }

        static void ValidateWriteText(MemoryStream ms, XmlDictionaryWriter writer, string text, ReadOnlySpan<byte> expected)
        {
            writer.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            ms.SetLength(0);
            writer.WriteString(text);
            writer.Flush();

            ms.TryGetBuffer(out ArraySegment<byte> arraySegment);
            ReadOnlySpan<byte> buffer = arraySegment;

            if (expected.Length <= byte.MaxValue)
            {
                Assert.Equal(Chars8Text, buffer[0]);
                Assert.Equal(expected.Length, buffer[1]);
                buffer = buffer.Slice(2);
            }
            else if (expected.Length <= ushort.MaxValue)
            {
                Assert.Equal(Chars16Text, buffer[0]);
                Assert.Equal(expected.Length, (int)(buffer[1]) | ((int)buffer[2] << 8));
                buffer = buffer.Slice(3);
            }
            else
                Assert.Fail("test use to long length");

            AssertExtensions.SequenceEqual(expected, buffer);
        }
    }

    private static bool ReadTest(MemoryStream ms, Encoding encoding, ReaderWriterFactory.ReaderWriterType rwType, byte[] byteArray)
    {
        ms.Position = 0;
        XmlDictionaryReader reader = (XmlDictionaryReader)ReaderWriterFactory.CreateXmlReader(rwType, ms, encoding);
        reader.ReadToDescendant("Root");
        byte[] bytesFromReader = reader.ReadElementContentAsBase64();
        if (bytesFromReader.Length != byteArray.Length)
        {
            return false;
        }
        else
        {
            for (int i = 0; i < byteArray.Length; i++)
            {
                if (byteArray[i] != bytesFromReader[i])
                {
                    return false;
                }
            }
        }
        return true;
    }

    static bool WriteTest(MemoryStream ms, ReaderWriterFactory.ReaderWriterType rwType, Encoding encoding, MyStreamProvider myStreamProvider)
    {
        XmlWriter writer = ReaderWriterFactory.CreateXmlWriter(rwType, ms, encoding);
        XmlDictionaryWriter writeD = writer as XmlDictionaryWriter;
        writeD.WriteStartElement("Root");
        writeD.WriteValue(myStreamProvider);

        if (rwType != ReaderWriterFactory.ReaderWriterType.MTOM)
        {
            // stream should be released right after WriteValue
            Assert.True(myStreamProvider.StreamReleased, "Error, stream not released after WriteValue");
        }
        writer.WriteEndElement();

        // stream should be released now for MTOM
        if (rwType == ReaderWriterFactory.ReaderWriterType.MTOM)
        {
            Assert.True(myStreamProvider.StreamReleased, "Error, stream not released after WriteEndElement");
        }
        writer.Flush();
        return true;
    }

    static bool AsyncWriteTest(MemoryStream ms, Encoding encoding, MyStreamProvider myStreamProvider)
    {
        XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(ms);
        writer.WriteStartElement("Root");
        Task writeValueAsynctask = writer.WriteValueAsync(myStreamProvider);
        writeValueAsynctask.Wait();
        Assert.True(myStreamProvider.StreamReleased, "Error, stream not released.");
        writer.WriteEndElement();
        writer.Flush();
        return true;
    }

    static bool AsyncWriteBase64Test(MemoryStream ms, byte[] byteArray, Encoding encoding, MyStreamProvider myStreamProvider)
    {
        XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(ms);
        writer.WriteStartElement("Root");
        Task writeValueBase64Asynctask = writer.WriteBase64Async(byteArray, 0, byteArray.Length);
        writeValueBase64Asynctask.Wait();
        Assert.True(myStreamProvider.StreamReleased, "Error, stream not released.");
        writer.WriteEndElement();
        writer.Flush();
        return true;
    }


    private static byte[] GetByteArray(int byteSize)
    {
        var bytes = new byte[byteSize];
        for (int i = 0; i < byteSize; i++)
        {
            bytes[i] = 8;
        }

        return bytes;
    }

    private static string GetExpectString(byte[] bytes, int byteSize)
    {
        using (var ms = new MemoryStream())
        {
            var writer = XmlDictionaryWriter.CreateTextWriter(ms);
            writer.WriteStartDocument();
            writer.WriteStartElement("data");
            writer.WriteBase64(bytes, 0, byteSize);
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            ms.Position = 0;
            var sr = new StreamReader(ms);
            return sr.ReadToEnd();
        }

    }
    private static void SimulateWriteFragment(XmlDictionaryWriter writer, bool useFragmentAPI, int nestedLevelsLeft)
    {
        if (nestedLevelsLeft <= 0)
        {
            return;
        }

        Random rndGen = new Random(nestedLevelsLeft);
        int signatureLen = rndGen.Next(100, 200);
        byte[] signature = new byte[signatureLen];
        rndGen.NextBytes(signature);

        MemoryStream fragmentStream = new MemoryStream();

        if (!useFragmentAPI) // simulating in the writer itself
        {
            writer.WriteStartElement("SignatureValue_" + nestedLevelsLeft);
            writer.WriteBase64(signature, 0, signatureLen);
            writer.WriteEndElement();
        }

        if (useFragmentAPI)
        {
            FragmentHelper.Start(writer, fragmentStream);
        }

        writer.WriteStartElement("Fragment" + nestedLevelsLeft);
        for (int i = 0; i < 5; i++)
        {
            writer.WriteStartElement(string.Format("Element{0}_{1}", nestedLevelsLeft, i));
            writer.WriteAttributeString("attr1", "value1");
            writer.WriteAttributeString("attr2", "value2");
        }
        writer.WriteString("This is a text with unicode characters: <>&;\u0301\u2234");
        for (int i = 0; i < 5; i++)
        {
            writer.WriteEndElement();
        }

        // write other nested fragments...
        SimulateWriteFragment(writer, useFragmentAPI, nestedLevelsLeft - 1);

        writer.WriteEndElement(); // Fragment{nestedLevelsLeft}
        writer.Flush();

        if (useFragmentAPI)
        {
            FragmentHelper.End(writer);
            writer.WriteStartElement("SignatureValue_" + nestedLevelsLeft);
            writer.WriteBase64(signature, 0, signatureLen);
            writer.WriteEndElement();

            FragmentHelper.Write(writer, fragmentStream.GetBuffer(), 0, (int)fragmentStream.Length);

            writer.Flush();
        }
    }

    public class AsyncMemoryStream : MemoryStream
    {
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await Task.Delay(1).ConfigureAwait(false);
            await base.WriteAsync(buffer, offset, count, cancellationToken);
        }
    }

    public class MemoryStreamWithBlockAsync : MemoryStream
    {
        private bool _blockAsync;
        public void blockAsync(bool blockAsync)
        {
            _blockAsync = blockAsync;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            while (_blockAsync)
            {
                await Task.Delay(10).ConfigureAwait(false);
            }

            await base.WriteAsync(buffer, offset, count, cancellationToken);
        }
    }
}
