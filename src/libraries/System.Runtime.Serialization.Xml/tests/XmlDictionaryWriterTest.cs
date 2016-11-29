// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    [Fact]
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

        Assert.StrictEqual(expect, actual);
    }

    [Fact]
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
        catch(Exception e)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"An error occured: {e.Message}");
            sb.AppendLine(e.StackTrace);
            sb.AppendLine();
            sb.AppendLine($"The last completed operation before the exception was: {lastCompletedOperation}");
            Assert.True(false, sb.ToString());
        }

        Assert.StrictEqual(expect, actual);

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

        Assert.StrictEqual(expect, actual);
    }

    [Fact]
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

    [ActiveIssue(13375)]
    [Fact]
    public static void FragmentTest()
    {
        string rwTypeStr = "Text";
        ReaderWriterFactory.ReaderWriterType rwType = (ReaderWriterFactory.ReaderWriterType)
            Enum.Parse(typeof(ReaderWriterFactory.ReaderWriterType), rwTypeStr, true);
        Encoding encoding = Encoding.GetEncoding("utf-8");
        int numberOfNestedFragments = 1;
        MemoryStream ms1 = new MemoryStream();
        MemoryStream ms2 = new MemoryStream();
        XmlDictionaryWriter writer1 = (XmlDictionaryWriter)ReaderWriterFactory.CreateXmlWriter(rwType, ms1, encoding);
        XmlDictionaryWriter writer2 = (XmlDictionaryWriter)ReaderWriterFactory.CreateXmlWriter(rwType, ms2, encoding);
        Assert.True(FragmentHelper.CanFragment(writer1));
        Assert.True(FragmentHelper.CanFragment(writer2));
        writer1.WriteStartDocument(); writer2.WriteStartDocument();
        writer1.WriteStartElement(ReaderWriterConstants.RootElementName); writer2.WriteStartElement(ReaderWriterConstants.RootElementName);
        SimulateWriteFragment(writer1, true, numberOfNestedFragments);
        SimulateWriteFragment(writer2, false, numberOfNestedFragments);
        writer1.WriteEndElement(); writer2.WriteEndElement();
        writer1.WriteEndDocument(); writer2.WriteEndDocument();
        writer1.Flush();
        writer2.Flush();

        byte[] doc1 = ms1.ToArray();
        byte[] doc2 = ms2.ToArray();
        CompareArrays(doc1, 0, doc2, 0, doc1.Length);
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

    private static void CompareArrays(byte[] array1, int offset1, byte[] array2, int offset2, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Assert.Equal(array1[i + offset1], array2[i + offset2]);
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
            writer.WriteStartElement(String.Format("Element{0}_{1}", nestedLevelsLeft, i));
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
