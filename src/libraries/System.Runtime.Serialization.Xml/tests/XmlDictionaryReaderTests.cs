// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using Xunit;

namespace System.Runtime.Serialization.Xml.Tests
{
    public static class XmlDictionaryReaderTests
    {
        [Fact]
        public static void ReadValueChunkReadEncodedDoubleWideChars()
        {
            // The test is to verify the fix made for the following issue:
            // When reading value chunk from XmlReader where Encoding.UTF8 is used, and where the
            // encoded bytes contains 4-byte UTF-8 encoded characters: if the 4 byte character is decoded
            // into 2 chars and the char[] only has one space left, an ArgumentException will be thrown
            // stating that there is not enough space to decode the bytes.
            string xmlPayloadHolder = @"<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/""><s:Body><Response xmlns=""http://tempuri.org/""><Result>{0}</Result></Response></s:Body></s:Envelope>";
            int startWideChars = 0;
            int endWideChars = 128;
            int incrementWideChars = 1;

            for (int wideChars = startWideChars; wideChars < endWideChars; wideChars += incrementWideChars)
            {
                for (int singleByteChars = 0; singleByteChars < 4; singleByteChars++)
                {
                    string testString = GenerateDoubleWideTestString(wideChars, singleByteChars);
                    string returnedString;
                    string xmlContent = string.Format(xmlPayloadHolder, testString);
                    using (Stream stream = GenerateStreamFromString(xmlContent))
                    {
                        var encoding = Encoding.UTF8;
                        var quotas = new XmlDictionaryReaderQuotas();
                        XmlReader reader = XmlDictionaryReader.CreateTextReader(stream, encoding, quotas, null);

                        reader.ReadStartElement(); // <s:Envelope>
                        reader.ReadStartElement(); // <s:Body>
                        reader.ReadStartElement(); // <Response>
                        reader.ReadStartElement(); // <Result>

                        Assert.True(reader.CanReadValueChunk, "reader.CanReadValueChunk is expected to be true, but it returned false.");

                        var resultChars = new List<char>();
                        var buffer = new char[256];
                        int count = 0;
                        while ((count = reader.ReadValueChunk(buffer, 0, buffer.Length)) > 0)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                resultChars.Add(buffer[i]);
                            }
                        }

                        returnedString = new string(resultChars.ToArray());
                    }

                    Assert.Equal(testString, returnedString);
                }
            }
        }

        [Fact]
        public static void ReadElementContentAsStringDataExceedsMaxBytesPerReadQuota()
        {
            XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas();
            quotas.MaxBytesPerRead = 4096;
            int contentLength = 8176;

            string testString = new string('a', contentLength);
            string returnedString;
            XmlDictionary dict = new XmlDictionary();
            XmlDictionaryString dictEntry = dict.Add("Value");

            using (var ms = new MemoryStream())
            {
                XmlDictionaryWriter xmlWriter = XmlDictionaryWriter.CreateBinaryWriter(ms, dict);
                xmlWriter.WriteElementString(dictEntry, XmlDictionaryString.Empty, testString);
                xmlWriter.Flush();
                ms.Position = 0;
                XmlDictionaryReader xmlReader = XmlDictionaryReader.CreateBinaryReader(ms, dict, quotas);
                xmlReader.Read();
                returnedString = xmlReader.ReadElementContentAsString();
            }

            Assert.Equal(testString, returnedString);
        }

        [Fact]
        public static void ReadElementContentAsDateTimeTest()
        {
            string xmlFileContent = @"<root><date>2013-01-02T03:04:05.006Z</date></root>";
            Stream sm = GenerateStreamFromString(xmlFileContent);
            XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(sm, XmlDictionaryReaderQuotas.Max);
            reader.ReadToFollowing("date");
            DateTime dt = reader.ReadElementContentAsDateTime();
            DateTime expected = new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc);
            Assert.Equal(expected, dt);
        }

        [Fact]
        public static void ReadElementContentAsBinHexTest()
        {
            string xmlFileContent = @"<data>540068006500200071007500690063006B002000620072006F0077006E00200066006F00780020006A0075006D007000730020006F00760065007200200074006800650020006C0061007A007900200064006F0067002E00</data>";
            Stream sm = GenerateStreamFromString(xmlFileContent);
            XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(sm, XmlDictionaryReaderQuotas.Max);
            reader.ReadToFollowing("data");
            byte[] bytes = reader.ReadElementContentAsBinHex();
            byte[] expected = Encoding.Unicode.GetBytes("The quick brown fox jumps over the lazy dog.");
            Assert.Equal(expected, bytes);
        }

        [Fact]
        public static void GetNonAtomizedNamesTest()
        {
            string localNameTest = "localNameTest";
            string namespaceUriTest = "http://www.msn.com/";
            var encoding = Encoding.UTF8;
            var rndGen = new Random();
            int byteArrayLength = rndGen.Next(100, 2000);
            byte[] byteArray = new byte[byteArrayLength];
            rndGen.NextBytes(byteArray);
            MemoryStream ms = new MemoryStream();
            XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(ms, encoding);
            writer.WriteElementString(localNameTest, namespaceUriTest, "value");
            writer.Flush();
            ms.Position = 0;
            XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(ms, encoding, XmlDictionaryReaderQuotas.Max, null);
            bool success = reader.ReadToDescendant(localNameTest);
            Assert.True(success);
            string localName;
            string namespaceUriStr;
            reader.GetNonAtomizedNames(out localName, out namespaceUriStr);
            Assert.Equal(localNameTest, localName);
            Assert.Equal(namespaceUriTest, namespaceUriStr);
            writer.Close();
        }

        [Fact]
        public static void ReadStringTest()
        {
            MemoryStream stream = new MemoryStream();
            XmlDictionary dictionary = new XmlDictionary();
            List<XmlDictionaryString> stringList = new List<XmlDictionaryString>();
            stringList.Add(dictionary.Add("Name"));
            stringList.Add(dictionary.Add("urn:Test"));

            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(stream, dictionary, null))
            {
                // write using the dictionary - element name, namespace, value
                string value = "value";
                writer.WriteElementString(stringList[0], stringList[1], value);
                writer.Flush();
                stream.Position = 0;
                XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(stream, dictionary, new XmlDictionaryReaderQuotas());
                reader.Read();
                string s = reader.ReadString();
                Assert.Equal(value, s);
            }
        }

        [Fact]
        public static void BinaryXml_ReadPrimitiveTypes()
        {
            float f = 1.23456788f;
            ReadOnlySpan<byte> floatBytes = new byte[] { 0x52, 0x06, 0x9e, 0x3f };
            Guid guid = new Guid(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });

            AssertReadContentFromBinary<long>(long.MaxValue, XmlBinaryNodeType.Int64TextWithEndElement, new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x7f });

            AssertReadContentFromBinary((byte)0x78, XmlBinaryNodeType.Int8Text, new byte[] { 0x78 });
            AssertReadContentFromBinary((short)0x1234, XmlBinaryNodeType.Int16Text, new byte[] { 0x34, 0x12 });
            AssertReadContentFromBinary(unchecked((short)0xf234), XmlBinaryNodeType.Int16Text, new byte[] { 0x34, 0xf2 });
            AssertReadContentFromBinary((int)0x12345678, XmlBinaryNodeType.Int32Text, new byte[] { 0x78, 0x56, 0x34, 0x12 });
            AssertReadContentFromBinary((long)0x0102030412345678, XmlBinaryNodeType.Int64Text, new byte[] { 0x78, 0x56, 0x34, 0x12, 04, 03, 02, 01 });

            // Integer values should be represented using smalles possible type
            AssertReadContentFromBinary((long)0, XmlBinaryNodeType.ZeroText, ReadOnlySpan<byte>.Empty);
            AssertReadContentFromBinary((long)1, XmlBinaryNodeType.OneText, ReadOnlySpan<byte>.Empty);
            AssertReadContentFromBinary((int)0x00000078, XmlBinaryNodeType.Int8Text, new byte[] { 0x78 });
            AssertReadContentFromBinary(unchecked((int)0xfffffff0), XmlBinaryNodeType.Int8Text, new byte[] { 0xf0 });
            AssertReadContentFromBinary((int)0x00001234, XmlBinaryNodeType.Int16Text, new byte[] { 0x34, 0x12 });
            AssertReadContentFromBinary(unchecked((int)0xfffff234), XmlBinaryNodeType.Int16Text, new byte[] { 0x34, 0xf2 });
            AssertReadContentFromBinary((long)0x12345678, XmlBinaryNodeType.Int32Text, new byte[] { 0x78, 0x56, 0x34, 0x12 });
            AssertReadContentFromBinary(unchecked((long)0xfffffffff2345678), XmlBinaryNodeType.Int32Text, new byte[] { 0x78, 0x56, 0x34, 0xf2 });

            AssertReadContentFromBinary(f, XmlBinaryNodeType.FloatText, floatBytes);
            AssertReadContentFromBinary(8.20788039913184E-304, XmlBinaryNodeType.DoubleText, new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 });
            AssertReadContentFromBinary(guid, XmlBinaryNodeType.GuidText, guid.ToByteArray());
            AssertReadContentFromBinary(new TimeSpan(0x0807060504030201), XmlBinaryNodeType.TimeSpanText, new byte[] { 01, 02, 03, 04, 05, 06, 07, 08 });
            AssertReadContentFromBinary(new decimal(0x20212223, 0x10111213, 0x01020304, true, scale: 0x1b), XmlBinaryNodeType.DecimalText,
                new byte[] { 0x0, 0x0, 0x1b, 0x80, 0x4, 0x3, 0x2, 0x1, 0x23, 0x22, 0x21, 0x20, 0x13, 0x12, 0x11, 0x10 });
            AssertReadContentFromBinary(new DateTime(2022, 8, 26, 12, 34, 56, DateTimeKind.Utc), XmlBinaryNodeType.DateTimeText,
                new byte[] { 0x00, 0x18, 0xdf, 0x61, 0x5f, 0x87, 0xda, 0x48 });

            // Double can be represented as float or inte as long as no detail is lost
            AssertReadContentFromBinary((double)0x0100, XmlBinaryNodeType.Int16Text, new byte[] { 0x00, 0x01 });
            AssertReadContentFromBinary((double)f, XmlBinaryNodeType.FloatText, floatBytes);
        }

        [Fact]
        public static void BinaryXml_Array_RoundTrip()
        {
            int[] ints = new int[] { -1, 0x01020304, 0x11223344, -1 };
            float[] floats = new float[] { 1.2345f, 2.3456f };
            double[] doubles = new double[] { 1.2345678901, 2.3456789012 };
            decimal[] decimals = new[] {
                new decimal(0x20212223, 0x10111213, 0x01020304, true, scale: 0x1b),
                new decimal(0x50515253, 0x40414243, 0x31323334, false, scale: 0x1c)
            };
            DateTime[] datetimes = new[] {
                new DateTime(2022, 8, 26, 12, 34, 56, DateTimeKind.Utc),
                new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local)
            };
            TimeSpan[] timespans = new[] { TimeSpan.FromTicks(0x0102030405060708), TimeSpan.FromTicks(0x1011121314151617) };
            // Write more than 4 kb in a single call to ensure we hit path for reading (and writing happens on 512b) large arrays
            long[] longs = Enumerable.Range(0x01020304, 513).Select(i => (long)i | (long)(~i << 32)).ToArray();
            Guid[] guids = new[] {
                new Guid(new ReadOnlySpan<byte>(new byte[] {1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16 })),
                new Guid(new ReadOnlySpan<byte>(new byte[] {10,20,30,40,50,60,70,80,90,100,110,120,130,140,150,160 }))
            };

            using var ms = new MemoryStream();
            using var writer = XmlDictionaryWriter.CreateBinaryWriter(ms);
            writer.WriteStartElement("root");
            writer.WriteArray(null, "ints", null, ints, 1, 2);
            writer.WriteArray(null, "floats", null, floats, 0, floats.Length);
            writer.WriteArray(null, "doubles", null, doubles, 0, doubles.Length);
            writer.WriteArray(null, "decimals", null, decimals, 0, decimals.Length);
            writer.WriteArray(null, "datetimes", null, datetimes, 0, datetimes.Length);
            writer.WriteArray(null, "timespans", null, timespans, 0, timespans.Length);
            writer.WriteArray(null, "longs", null, longs, 0, longs.Length);
            writer.WriteArray(null, "guids", null, guids, 0, guids.Length);
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            ms.Seek(0, SeekOrigin.Begin);

            int[] actualInts = new int[] { -1, -1, -1, -1 };

            using var reader = XmlDictionaryReader.CreateBinaryReader(ms, XmlDictionaryReaderQuotas.Max);
            reader.ReadStartElement("root");
            int intsRead = reader.ReadArray("ints", string.Empty, actualInts, 1, 3);
            float[] actualFloats = reader.ReadSingleArray("floats", string.Empty);
            double[] actualDoubles = reader.ReadDoubleArray("doubles", string.Empty);
            decimal[] actualDecimals = reader.ReadDecimalArray("decimals", string.Empty);
            DateTime[] actualDateTimes = reader.ReadDateTimeArray("datetimes", string.Empty);
            TimeSpan[] actualTimeSpans = reader.ReadTimeSpanArray("timespans", string.Empty);
            long[] actualLongs = reader.ReadInt64Array("longs", string.Empty);
            Guid[] actualGuids = reader.ReadGuidArray("guids", string.Empty);
            reader.ReadEndElement();

            Assert.Equal(XmlNodeType.None, reader.NodeType); // Should be at end

            Assert.Equal(2, intsRead);
            AssertExtensions.SequenceEqual(ints, actualInts);
            AssertExtensions.SequenceEqual(actualLongs, longs);
            AssertExtensions.SequenceEqual(actualFloats, floats);
            AssertExtensions.SequenceEqual(actualDoubles, doubles);
            AssertExtensions.SequenceEqual(actualDecimals, decimals);
            AssertExtensions.SequenceEqual(actualDateTimes, datetimes);
            AssertExtensions.SequenceEqual(actualTimeSpans, timespans);
            AssertExtensions.SequenceEqual(actualGuids, guids);
        }

        private static void AssertReadContentFromBinary<T>(T expected, XmlBinaryNodeType nodeType, ReadOnlySpan<byte> bytes)
        {
            ReadOnlySpan<byte> documentStart = new byte[] { 0x40, 0x1, 0x61 };  // start node "a"
            MemoryStream ms = new MemoryStream(documentStart.Length + 1 + bytes.Length);
            ms.Write(documentStart);
            ms.WriteByte((byte)(nodeType | XmlBinaryNodeType.EndElement)); // With EndElement
            ms.Write(bytes);
            ms.Seek(0, SeekOrigin.Begin);
            XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(ms, XmlDictionaryReaderQuotas.Max);
            reader.ReadStartElement("a");
            T result = (T)reader.ReadContentAs(typeof(T), null);
            reader.ReadEndElement();

            Assert.True(ms.Position == ms.Length, "whole buffer should have been consumed");
            Assert.True(XmlNodeType.None == reader.NodeType, "XmlDictionaryReader should be at end of document");
            Assert.Equal(expected, result);
        }

        private static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private static string GenerateDoubleWideTestString(int charsToGenerate, int singleByteChars)
        {
            int count = 0;
            int startChar = 0x10000;

            var sb = new StringBuilder();

            while (count < singleByteChars)
            {
                sb.Append((char)('a' + count % 26));
                count++;
            }

            count = 0;

            while (count < charsToGenerate)
            {
                sb.Append(char.ConvertFromUtf32(startChar + count % 65535));
                count++;
            }

            return sb.ToString();
        }

        [Fact]
        public static void Close_DerivedReader_Success()
        {
            new NotImplementedXmlDictionaryReader().Close();
        }

        private sealed class NotImplementedXmlDictionaryReader : XmlDictionaryReader
        {
            public override ReadState ReadState => ReadState.Initial;

            public override int AttributeCount => throw new NotImplementedException();
            public override string BaseURI => throw new NotImplementedException();
            public override int Depth => throw new NotImplementedException();
            public override bool EOF => throw new NotImplementedException();
            public override bool IsEmptyElement => throw new NotImplementedException();
            public override string LocalName => throw new NotImplementedException();
            public override string NamespaceURI => throw new NotImplementedException();
            public override XmlNameTable NameTable => throw new NotImplementedException();
            public override XmlNodeType NodeType => throw new NotImplementedException();
            public override string Prefix => throw new NotImplementedException();
            public override string Value => throw new NotImplementedException();
            public override string GetAttribute(int i) => throw new NotImplementedException();
            public override string GetAttribute(string name) => throw new NotImplementedException();
            public override string GetAttribute(string name, string namespaceURI) => throw new NotImplementedException();
            public override string LookupNamespace(string prefix) => throw new NotImplementedException();
            public override bool MoveToAttribute(string name) => throw new NotImplementedException();
            public override bool MoveToAttribute(string name, string ns) => throw new NotImplementedException();
            public override bool MoveToElement() => throw new NotImplementedException();
            public override bool MoveToFirstAttribute() => throw new NotImplementedException();
            public override bool MoveToNextAttribute() => throw new NotImplementedException();
            public override bool Read() => throw new NotImplementedException();
            public override bool ReadAttributeValue() => throw new NotImplementedException();
            public override void ResolveEntity() => throw new NotImplementedException();
        }
    }
}
