// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace System.Runtime.Serialization.Xml.Tests
{
    public static class XmlDictionaryReaderTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1025)]
        [InlineData(4096)]
        public static async Task Reader_Utf8StringDecoding_PreservesValues(int length)
        {
            foreach (string format in new[] { "xml", "binary" })
            {
                foreach (string suffix in new[] { "", "\u00e9\u4e2d\ud83d\ude42" })
                {
                    string value = new string('x', length) + suffix;
                    byte[] document = CreateReaderDocument(format, $"<root value=\"{value}\">{value}</root>", string.Empty, Encoding.UTF8);
                    using XmlDictionaryReader reader = format == "binary"
                        ? XmlDictionaryReader.CreateBinaryReader(document, XmlDictionaryReaderQuotas.Max)
                        : XmlDictionaryReader.CreateTextReader(document, XmlDictionaryReaderQuotas.Max);
                    reader.MoveToContent();
                    Assert.Equal(value, reader.GetAttribute("value"));
                    Assert.Equal(value, reader.ReadElementContentAsString());
                    using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document, fragmentSize: 17, blocked: false);
                    XmlDictionaryReader asynchronous = await CreateAsyncReader(format, source, Encoding.UTF8, XmlDictionaryReaderQuotas.Max);
                    try
                    {
                        Assert.True(await asynchronous.ReadAsync());
                        Assert.Equal(value, asynchronous.GetAttribute("value"));
                        Assert.Equal(value, await asynchronous.ReadElementContentAsStringAsync());
                    }
                    finally
                    {
                        await CloseReaderAsync(asynchronous);
                    }
                }
            }
        }

        private delegate int ByteScanner(ReadOnlySpan<byte> value);

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(15)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(31)]
        [InlineData(32)]
        [InlineData(33)]
        [InlineData(63)]
        [InlineData(64)]
        [InlineData(65)]
        [InlineData(127)]
        [DynamicDependency("ReadText", "System.Xml.XmlUTF8TextReader", "System.Private.DataContractSerialization")]
        [DynamicDependency("ReadAttributeText", "System.Xml.XmlUTF8TextReader", "System.Private.DataContractSerialization")]
        public static void TextScanners_AllBytesAndBounds(int length)
        {
            Type type = typeof(XmlDictionaryReader).Assembly.GetType("System.Xml.XmlUTF8TextReader", throwOnError: true);
            foreach (bool attribute in new[] { false, true })
            {
                ByteScanner scan = type.GetMethod(attribute ? "ReadAttributeText" : "ReadText",
                    BindingFlags.Static | BindingFlags.NonPublic, binder: null, types: new[] { typeof(ReadOnlySpan<byte>) }, modifiers: null)
                    .CreateDelegate<ByteScanner>();
                foreach (PoisonPagePlacement placement in new[] { PoisonPagePlacement.Before, PoisonPagePlacement.After })
                {
                    using BoundedMemory<byte> memory = BoundedMemory.Allocate<byte>(length, placement);
                    Span<byte> value = memory.Span;
                    value.Fill((byte)'x');
                    Assert.Equal(length, scan(value));
                    for (int offset = 0; offset < length; offset++)
                    {
                        for (int b = 0; b <= byte.MaxValue; b++)
                        {
                            value[offset] = (byte)b;
                            bool allowed = b >= 0x20 && b is not (0x26 or 0x3c or 0xef);
                            allowed = attribute ? allowed && b is not (0x22 or 0x27) : (allowed && b != 0x5d) || b is 0x9 or 0xa;
                            Assert.Equal(allowed ? length : offset, scan(value));
                        }
                        value[offset] = (byte)'x';
                    }
                }
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(15)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(31)]
        [InlineData(32)]
        [InlineData(33)]
        [InlineData(63)]
        [InlineData(64)]
        [InlineData(65)]
        [InlineData(2047)]
        [InlineData(2048)]
        [InlineData(2049)]
        public static async Task TextScanners_UnicodeEntitiesAndFragmentedInput(int length)
        {
            string prefix = new string('x', length);
            string raw = prefix + "\u00e9\u4e2d\ud83d\ude42&#x9;&lt;&amp;" + prefix;
            string expected = prefix + "\u00e9\u4e2d\ud83d\ude42\t<&" + prefix;
            byte[] document = Encoding.UTF8.GetBytes($"<root value=\"{raw}\">{raw}</root>");
            using XmlDictionaryReader buffered = XmlDictionaryReader.CreateTextReader(document, XmlDictionaryReaderQuotas.Max);
            buffered.MoveToContent();
            Assert.Equal(expected, buffered.GetAttribute("value"));
            Assert.Equal(expected, buffered.ReadElementContentAsString());
            using MemoryStream synchronousSource = new(document);
            using XmlDictionaryReader synchronous = XmlDictionaryReader.CreateTextReader(synchronousSource, XmlDictionaryReaderQuotas.Max);
            synchronous.MoveToContent();
            Assert.Equal(expected, synchronous.GetAttribute("value"));
            Assert.Equal(expected, synchronous.ReadElementContentAsString());

            using AsyncOnlyReadStream source = new(document, fragmentSize: 17, blocked: false);
            XmlDictionaryReader asynchronous = await CreateAsyncReader("xml", source, Encoding.UTF8, XmlDictionaryReaderQuotas.Max);
            try
            {
                Assert.True(await asynchronous.ReadAsync());
                Assert.Equal(expected, asynchronous.GetAttribute("value"));
                Assert.Equal(expected, await asynchronous.ReadElementContentAsStringAsync());
                Assert.False(await asynchronous.ReadAsync());
            }
            finally
            {
                await CloseReaderAsync(asynchronous);
            }
        }

        [Theory]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(33)]
        [InlineData(2047)]
        [InlineData(2048)]
        [InlineData(2049)]
        public static async Task TextScanners_InvalidUtf8AndXml(int length)
        {
            foreach (byte[] invalid in new[]
            {
                new byte[] { 0 }, new byte[] { 1 }, new byte[] { 0x80 }, new byte[] { 0xc0, 0xaf },
                new byte[] { 0xe2, 0x82 }, new byte[] { 0xed, 0xa0, 0x80 }, new byte[] { 0xf4, 0x90, 0x80, 0x80 },
                new byte[] { 0xef, 0xbf, 0xbe }, new byte[] { 0xef, 0xbf, 0xbf }, Encoding.UTF8.GetBytes("]]>")
            })
            {
                byte[] document = Encoding.UTF8.GetBytes("<root>" + new string('x', length))
                    .Concat(invalid).Concat(Encoding.UTF8.GetBytes("</root>")).ToArray();
                using XmlDictionaryReader buffered = XmlDictionaryReader.CreateTextReader(document, XmlDictionaryReaderQuotas.Max);
                Assert.Throws<XmlException>(() => { while (buffered.Read()) { _ = buffered.Value; } });
                using MemoryStream synchronousSource = new(document);
                using XmlDictionaryReader synchronous = XmlDictionaryReader.CreateTextReader(synchronousSource, XmlDictionaryReaderQuotas.Max);
                Assert.Throws<XmlException>(() => { while (synchronous.Read()) { _ = synchronous.Value; } });
                using AsyncOnlyReadStream source = new(document, fragmentSize: 17, blocked: false);
                XmlDictionaryReader asynchronous = await CreateAsyncReader("xml", source, Encoding.UTF8, XmlDictionaryReaderQuotas.Max);
                try
                {
                    await Assert.ThrowsAsync<XmlException>(async () => { while (await asynchronous.ReadAsync()) { _ = asynchronous.Value; } });
                }
                finally
                {
                    await CloseReaderAsync(asynchronous);
                }
            }
        }

        [Theory]
        [InlineData(false, "utf-8")]
        [InlineData(false, "utf-16")]
        [InlineData(false, "utf-16BE")]
        [InlineData(true, "utf-8")]
        [InlineData(true, "utf-16")]
        [InlineData(true, "utf-16BE")]
        public static async Task EncodingStream_AsyncInitializationAndReads(bool json, string encodingName)
        {
            Encoding encoding = Encoding.GetEncoding(encodingName);
            string value = new string('\u00e9', 600) + "\ud83d\ude42";
            string text = json ? $"\"{value}\"" : $"<?xml version=\"1.0\" encoding=\"{encodingName}\"?><value>{value}</value>";
            byte[] input = encoding.GetPreamble().Concat(encoding.GetBytes(text)).ToArray();
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(input);
            string typeName = json ? "System.Runtime.Serialization.Json.JsonEncodingStreamWrapper" : "System.Xml.EncodingStreamWrapper";
            Type type = typeof(XmlDictionaryReader).Assembly.GetType(typeName, throwOnError: true);
            MethodInfo factory = type.GetMethod("CreateReaderAsync", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(factory);
            ValueTask<Stream> operation = (ValueTask<Stream>)factory.Invoke(null, new object[] { source, null, CancellationToken.None });
            Assert.False(operation.IsCompleted);
            source.Release();
            await using Stream wrapper = await operation;
            using MemoryStream output = new MemoryStream();
            byte[] buffer = new byte[17];
            int read;
            while ((read = await wrapper.ReadAsync(buffer.AsMemory())) != 0)
            {
                output.Write(buffer, 0, read);
            }
            Assert.Equal(text, Encoding.UTF8.GetString(output.ToArray()));
            Assert.True(source.AsyncReads > 1);
        }

        [Theory]
        [InlineData("xml", "utf-16", false)]
        [InlineData("xml", "utf-16", true)]
        [InlineData("xml", "utf-16BE", false)]
        [InlineData("xml", "utf-16BE", true)]
        [InlineData("json", "utf-16", false)]
        [InlineData("json", "utf-16", true)]
        [InlineData("json", "utf-16BE", false)]
        [InlineData("json", "utf-16BE", true)]
        public static async Task Reader_Async_BomlessUtf16OneByteFragments(string format, string encodingName, bool matchingHint)
        {
            Encoding encoding = Encoding.GetEncoding(encodingName);
            string text = format == "json" ? "{\"value\":\"\u00e9\ud83d\ude42\"}" :
                $"<?xml version=\"1.0\" encoding=\"{encodingName}\"?><root>\u00e9\ud83d\ude42</root>";
            byte[] document = encoding.GetBytes(text);
            using XmlDictionaryReader synchronous = CreateSynchronousReader(format, document, encoding);
            List<string> expected = new List<string>();
            while (synchronous.Read())
                expected.Add(ReaderSnapshot(synchronous));
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document, fragmentSize: 1, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader(format, source, matchingHint ? encoding : null);
            try
            {
                List<string> actual = new List<string>();
                while (await reader.ReadAsync())
                    actual.Add(ReaderSnapshot(reader));
                Assert.Equal(expected, actual);
                Assert.True(source.AsyncReads > 1);
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [InlineData("0", false)]
        [InlineData("0", true)]
        [InlineData("1", false)]
        [InlineData("1", true)]
        [InlineData("\"\"", false)]
        [InlineData("\"\"", true)]
        [InlineData("{}", false)]
        [InlineData("{}", true)]
        public static async Task Reader_Async_ShortUtf8RootsAtEof(string text, bool matchingHint)
        {
            byte[] document = Encoding.UTF8.GetBytes(text);
            using XmlDictionaryReader synchronous = CreateSynchronousReader("json", document, Encoding.UTF8);
            List<string> expected = new List<string>();
            while (synchronous.Read())
                expected.Add(ReaderSnapshot(synchronous));
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document, fragmentSize: 1, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader("json", source, matchingHint ? Encoding.UTF8 : null);
            try
            {
                List<string> actual = new List<string>();
                while (await reader.ReadAsync())
                    actual.Add(ReaderSnapshot(reader));
                Assert.Equal(expected, actual);
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [InlineData("xml", "utf-8", 1)]
        [InlineData("xml", "utf-16", 1)]
        [InlineData("xml", "utf-16BE", 3)]
        [InlineData("json", "utf-8", 1)]
        [InlineData("json", "utf-16", 1)]
        [InlineData("json", "utf-16BE", 3)]
        [InlineData("binary", "utf-8", 1)]
        [InlineData("xml", "utf-8", 128)]
        [InlineData("xml", "utf-8", 4096)]
        [InlineData("json", "utf-8", 128)]
        [InlineData("json", "utf-8", 4096)]
        [InlineData("binary", "utf-8", 128)]
        [InlineData("binary", "utf-8", 4096)]
        public static async Task Reader_ReadAsync_PreservesNodes(string format, string encodingName, int fragmentSize)
        {
            Encoding encoding = Encoding.GetEncoding(encodingName);
            string name = new string('n', 150);
            string xml = $"<?xml version=\"1.0\" encoding=\"{encodingName}\"?><p:{name} xmlns:p=\"urn:p\" xmlns=\"urn:default\" xml:space=\"preserve\" xml:lang=\"en\" a=\"one&#xD;&#10;&quot;\"><!--comment--><![CDATA[value<&]]>A&amp;\u00e9\ud83d\ude42<child xmlns=\"urn:child\"/><p:x/></p:{name}>";
            string json = "{\"__type\":\"Type:#ns\",\"first name\":\"a\\u00e9\\ud83d\\ude42\\t<&\",\"empty\":null,\"array\":[true,false,3.5e1,{\"x\":7}],\"quote\\\"name\":\"\ufffd\"}";
            byte[] document = CreateReaderDocument(format, xml, json, encoding);
            using XmlDictionaryReader synchronous = CreateSynchronousReader(format, document, encoding);
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document, fragmentSize);
            ValueTask<XmlDictionaryReader> initialization = CreateAsyncReader(format, source, encoding);
            if (format == "binary")
            {
                Assert.True(initialization.IsCompletedSuccessfully);
                Assert.Equal(0, source.AsyncReads);
            }
            else
            {
                Assert.False(initialization.IsCompleted);
            }
            source.Release();
            XmlDictionaryReader reader = await initialization;
            try
            {
                while (synchronous.Read())
                {
                    Assert.True(await reader.ReadAsync());
                    Assert.Equal(ReaderSnapshot(synchronous), ReaderSnapshot(reader));
                    Assert.Equal(reader.Value, await reader.GetValueAsync());
                }
                Assert.False(await reader.ReadAsync());
                Assert.True(reader.EOF);
                Assert.True(source.AsyncReads > 1);
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [InlineData("xml")]
        [InlineData("json")]
        [InlineData("binary")]
        public static async Task Reader_ReadAsync_ContentAndSkipAreStreaming(string format)
        {
            string value = string.Concat(Enumerable.Repeat("a\u00e9\ud83d\ude42", 1100));
            string xml = $"<root><value>{value}</value><skip><nested>ignore</nested></skip><tail>42</tail></root>";
            string json = $"{{\"value\":\"{value}\",\"skip\":{{\"nested\":\"ignore\"}},\"tail\":42}}";
            byte[] document = CreateReaderDocument(format, xml, json, Encoding.UTF8);
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document, fragmentSize: 3, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader(format, source, Encoding.UTF8, new XmlDictionaryReaderQuotas { MaxBytesPerRead = 64 });
            try
            {
                Assert.Equal(XmlNodeType.Element, await reader.MoveToContentAsync());
                Assert.True(await reader.ReadAsync());
                Assert.Equal("value", reader.LocalName);
                Assert.True(await reader.ReadAsync());
                Assert.Equal(XmlNodeType.Text, reader.NodeType);
                Assert.True(source.BytesRead < document.Length);
                Assert.True(GetBinaryReaderBuffer(reader).Length <= 256);
                string firstChunk = await reader.GetValueAsync();
                char[] characters = new char[firstChunk.Length];
                Assert.Equal(characters.Length, await reader.ReadValueChunkAsync(characters, 0, characters.Length));
                Assert.Equal(firstChunk, new string(characters));
                string remaining = await reader.ReadContentAsStringAsync();
                Assert.Equal(value, firstChunk + remaining);
                Assert.Equal(XmlNodeType.EndElement, reader.NodeType);
                Assert.True(await reader.ReadAsync());
                Assert.Equal("skip", reader.LocalName);
                await reader.SkipAsync();
                Assert.Equal("tail", reader.LocalName);
                Assert.Equal(42, await reader.ReadElementContentAsAsync(typeof(int), null));
                Assert.Equal(XmlNodeType.EndElement, reader.NodeType);
                Assert.False(await reader.ReadAsync());
                Assert.True(reader.EOF);
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [InlineData(false, false, 64)]
        [InlineData(false, true, 64)]
        [InlineData(true, false, 64)]
        [InlineData(true, true, 64)]
        [InlineData(false, false, 125)]
        [InlineData(false, true, 125)]
        [InlineData(true, false, 125)]
        [InlineData(true, true, 125)]
        public static async Task BinaryXml_ReadAsync_PartialText(bool unicode, bool withEndElement, int maxBytesPerRead)
        {
            string expected = string.Concat(Enumerable.Repeat("a\u00e9\ud83d\ude42", 129));
            Encoding encoding = unicode ? Encoding.Unicode : Encoding.UTF8;
            XmlBinaryNodeType nodeType = unicode ? XmlBinaryNodeType.UnicodeChars32Text : XmlBinaryNodeType.Chars32Text;
            byte[] document = CreateBinaryTextDocument(nodeType, encoding.GetBytes(expected), withEndElement);
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document, fragmentSize: 1, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader("binary", source, null, new XmlDictionaryReaderQuotas { MaxBytesPerRead = maxBytesPerRead });
            try
            {
                Assert.True(await reader.ReadAsync());
                Assert.Equal("ab", reader.LocalName);
                Assert.Equal(expected, await reader.ReadElementContentAsStringAsync());
                Assert.True(reader.EOF);
                Assert.Equal(document.Length, source.BytesRead);
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [InlineData("xml", false, 1)]
        [InlineData("xml", false, 7)]
        [InlineData("xml", true, 1)]
        [InlineData("xml", true, 7)]
        [InlineData("json", false, 1)]
        [InlineData("json", false, 7)]
        [InlineData("json", true, 1)]
        [InlineData("json", true, 7)]
        [InlineData("binary", false, 1)]
        [InlineData("binary", false, 7)]
        [InlineData("binary", true, 1)]
        [InlineData("binary", true, 7)]
        public static async Task Reader_ReadAsync_BinaryContent(string format, bool binHex, int chunkSize)
        {
            byte[] expected = Enumerable.Range(0, 513).Select(i => unchecked((byte)i)).ToArray();
            string text = binHex ? Convert.ToHexString(expected) : Convert.ToBase64String(expected, Base64FormattingOptions.InsertLineBreaks);
            byte[] document;
            if (format == "binary" && !binHex)
            {
                document = CreateBinaryTextDocument(XmlBinaryNodeType.Bytes32Text, expected, withEndElement: true);
            }
            else
            {
                document = CreateReaderDocument(format, $"<value>{text}</value>", $"\"{text.Replace("\r", "\\r").Replace("\n", "\\n")}\"", Encoding.UTF8);
            }
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document, fragmentSize: 1, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader(format, source, Encoding.UTF8, new XmlDictionaryReaderQuotas { MaxBytesPerRead = 64 });
            try
            {
                Assert.Equal(XmlNodeType.Element, await reader.MoveToContentAsync());
                using MemoryStream output = new MemoryStream();
                byte[] chunk = new byte[chunkSize];
                int read;
                do
                {
                    read = binHex ? await reader.ReadElementContentAsBinHexAsync(chunk, 0, chunk.Length) :
                        await reader.ReadElementContentAsBase64Async(chunk, 0, chunk.Length);
                    output.Write(chunk, 0, read);
                } while (read != 0);
                Assert.Equal(expected, output.ToArray());
                Assert.True(reader.EOF);
                Assert.True(GetBinaryReaderBuffer(reader).Length <= 256);
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [InlineData("xml")]
        [InlineData("json")]
        [InlineData("binary")]
        public static async Task Reader_ReadAsync_Cancellation(string format)
        {
            string value = new string('a', 9000);
            byte[] document = CreateReaderDocument(format, $"<value>{value}</value>", $"\"{value}\"", Encoding.UTF8);
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document, fragmentSize: 3, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader(format, source, Encoding.UTF8);
            try
            {
                Assert.True(await reader.ReadAsync());
                source.Block();
                using CancellationTokenSource cancellation = new CancellationTokenSource();
                ValueTask<bool> read = ReadWithCancellationAsync(reader, cancellation.Token);
                Assert.False(read.IsCompleted);
                cancellation.Cancel();
                OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read.AsTask());
                Assert.Equal(cancellation.Token, exception.CancellationToken);
                Assert.Equal(cancellation.Token, source.LastCancellationToken);
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [InlineData("xml")]
        [InlineData("json")]
        [InlineData("binary")]
        public static async Task Reader_ReadAsync_CanceledBeforeReadAndAsyncClose(string format)
        {
            byte[] document = CreateReaderDocument(format, "<value>42</value>", "42", Encoding.UTF8);
            await using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document, fragmentSize: 1, blocked: false, asyncDisposeOnly: true);
            int closed = 0;
            XmlDictionaryReader reader = await CreateAsyncReader(format, source, Encoding.UTF8, onClose: _ => closed++);
            int reads = source.AsyncReads;
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ReadWithCancellationAsync(reader, cancellation.Token).AsTask());
            Assert.Equal(reads, source.AsyncReads);
            await CloseReaderAsync(reader);
            Assert.True(source.AsyncDisposed);
            Assert.Equal(ReadState.Closed, reader.ReadState);
            Assert.Equal(1, closed);
            await CloseReaderAsync(reader);
            Assert.Equal(1, closed);
        }

        public static IEnumerable<object[]> BinaryTypedValues()
        {
            yield return new object[] { true };
            yield return new object[] { false };
            yield return new object[] { -100 };
            yield return new object[] { 1000 };
            yield return new object[] { 65537 };
            yield return new object[] { long.MinValue };
            yield return new object[] { ulong.MaxValue };
            yield return new object[] { 1.25f };
            yield return new object[] { 1.25d };
            yield return new object[] { 123.45m };
            yield return new object[] { new DateTime(637000000000123456, DateTimeKind.Utc) };
            yield return new object[] { TimeSpan.FromTicks(-1234567) };
            yield return new object[] { new Guid("01234567-89ab-cdef-0123-456789abcdef") };
            yield return new object[] { new UniqueId(new Guid("01234567-89ab-cdef-0123-456789abcdef")) };
        }

        [Theory]
        [MemberData(nameof(BinaryTypedValues))]
        public static async Task BinaryXml_ReadAsync_TypedValues(object value)
        {
            using MemoryStream document = new MemoryStream();
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(document))
            {
                writer.WriteStartElement("value");
                writer.WriteValue(value);
                writer.WriteEndElement();
            }
            byte[] bytes = document.ToArray();
            using XmlDictionaryReader synchronous = XmlDictionaryReader.CreateBinaryReader(bytes, new XmlDictionaryReaderQuotas());
            Assert.True(synchronous.Read());
            Assert.True(synchronous.Read());
            Type valueType = synchronous.ValueType;
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(bytes, fragmentSize: 1, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader("binary", source, null);
            try
            {
                Assert.True(await reader.ReadAsync());
                Assert.True(await reader.ReadAsync());
                Assert.Equal(valueType, reader.ValueType);
                Assert.Equal(value, await reader.ReadContentAsAsync(value.GetType(), null));
                Assert.Equal(XmlNodeType.EndElement, reader.NodeType);
                Assert.False(await reader.ReadAsync());
                Assert.True(reader.EOF);
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        public static IEnumerable<object[]> BinaryElementTypedValues()
        {
            foreach (object[] value in BinaryTypedValues())
            {
                yield return new object[] { value[0], false };
                yield return new object[] { value[0], true };
            }
        }

        [Theory]
        [MemberData(nameof(BinaryElementTypedValues))]
        public static async Task BinaryXml_ReadAsync_ElementTypedValuesPreserveFollowingNode(object value, bool canonicalize)
        {
            using MemoryStream document = new MemoryStream();
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(document))
            {
                writer.WriteStartElement("root");
                writer.WriteStartElement("value");
                writer.WriteValue(value);
                writer.WriteEndElement();
                writer.WriteElementString("tail", new string('a', 1024));
                writer.WriteEndElement();
            }
            byte[] bytes = document.ToArray();
            using MemoryStream expectedCanonical = new MemoryStream();
            if (canonicalize)
            {
                using XmlDictionaryReader control = XmlDictionaryReader.CreateBinaryReader(bytes, XmlDictionaryReaderQuotas.Max);
                control.StartCanonicalization(expectedCanonical, includeComments: false, inclusivePrefixes: null);
                while (control.Read()) { }
                control.EndCanonicalization();
            }
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(bytes, fragmentSize: 1, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader("binary", source, null);
            using MemoryStream canonical = new MemoryStream();
            try
            {
                if (canonicalize)
                    reader.StartCanonicalization(canonical, includeComments: false, inclusivePrefixes: null);
                Assert.True(await reader.ReadAsync());
                Assert.True(await reader.ReadAsync());
                Assert.Equal(value, await reader.ReadElementContentAsAsync(value.GetType(), null));
                Assert.Equal(XmlNodeType.Element, reader.NodeType);
                Assert.Equal("tail", reader.LocalName);
                await reader.SkipAsync();
                Assert.Equal(XmlNodeType.EndElement, reader.NodeType);
                Assert.Equal("root", reader.LocalName);
                Assert.False(await reader.ReadAsync());
                if (canonicalize)
                {
                    await EndReaderCanonicalizationAsync(reader, CancellationToken.None);
                    Assert.Equal(expectedCanonical.ToArray(), canonical.ToArray());
                }
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [InlineData(8, 1)]
        [InlineData(8, int.MaxValue)]
        [InlineData(9, 1)]
        [InlineData(9, int.MaxValue)]
        public static async Task BinaryXml_ReadAsync_TypedElementStartsNewQuotaWindow(int maxBytesPerRead, int fragmentSize)
        {
            using MemoryStream document = new MemoryStream();
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(document))
            {
                writer.WriteStartElement("root");
                writer.WriteStartElement("value");
                writer.WriteValue(1000);
                writer.WriteEndElement();
                writer.WriteStartElement("tail");
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas { MaxBytesPerRead = maxBytesPerRead };
            byte[] bytes = document.ToArray();
            using (XmlDictionaryReader control = XmlDictionaryReader.CreateBinaryReader(new MemoryStream(bytes), quotas))
            {
                Assert.True(control.Read());
                Assert.True(control.Read());
                Assert.Equal(1000, control.ReadElementContentAsInt());
                Assert.Equal("tail", control.LocalName);
            }
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(bytes, fragmentSize, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader("binary", source, null, quotas);
            try
            {
                Assert.True(await reader.ReadAsync());
                Assert.True(await reader.ReadAsync());
                Assert.Equal(1000, await reader.ReadElementContentAsAsync(typeof(int), null));
                Assert.Equal(XmlNodeType.Element, reader.NodeType);
                Assert.Equal("tail", reader.LocalName);
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        public static IEnumerable<object[]> BinaryPackedArrays()
        {
            yield return new object[] { Enumerable.Range(0, 25).Select(i => i % 2 == 0).ToArray() };
            yield return new object[] { Enumerable.Range(0, 25).Select(i => (short)(i - 13)).ToArray() };
            yield return new object[] { Enumerable.Range(0, 25).Select(i => i * 100000).ToArray() };
            yield return new object[] { Enumerable.Range(0, 25).Select(i => i * 10000000000L).ToArray() };
            yield return new object[] { Enumerable.Range(0, 25).Select(i => i + 0.25f).ToArray() };
            yield return new object[] { Enumerable.Range(0, 25).Select(i => i + 0.25d).ToArray() };
            yield return new object[] { Enumerable.Range(0, 25).Select(i => i + 0.25m).ToArray() };
            yield return new object[] { Enumerable.Range(0, 25).Select(i => new DateTime(637000000000000000 + i, DateTimeKind.Utc)).ToArray() };
            yield return new object[] { Enumerable.Range(0, 25).Select(i => TimeSpan.FromTicks(i - 13)).ToArray() };
            yield return new object[] { Enumerable.Range(0, 25).Select(i => new Guid(i, 0, 0, new byte[8])).ToArray() };
        }

        [Theory]
        [InlineData(64)]
        [InlineData(256)]
        public static async Task BinaryXml_ReadAsync_CoalescesRefillsWithinQuotaWindow(int maxBytesPerRead)
        {
            using MemoryStream document = new MemoryStream();
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(document, null, null, ownsStream: false))
            {
                writer.WriteStartElement("root");
                for (int i = 0; i < 128; i++)
                {
                    writer.WriteStartElement("item");
                    writer.WriteValue(i);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document.ToArray(), fragmentSize: int.MaxValue, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader("binary", source, null,
                new XmlDictionaryReaderQuotas { MaxBytesPerRead = maxBytesPerRead });
            try
            {
                int elements = 0;
                while (await reader.ReadAsync())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                        elements++;
                }
                Assert.Equal(129, elements);
                Assert.True(source.AsyncReads < 100, $"Read {source.AsyncReads} times for {document.Length} bytes.");
                Assert.InRange(source.MaxRequestedCount, 1, maxBytesPerRead);
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [MemberData(nameof(BinaryPackedArrays))]
        public static async Task BinaryXml_ReadAsync_PackedArrays(Array values)
        {
            using MemoryStream document = new MemoryStream();
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(document))
            {
                writer.WriteStartElement("root");
                MethodInfo write = typeof(XmlDictionaryWriter).GetMethod(nameof(XmlDictionaryWriter.WriteArray),
                    new[] { typeof(string), typeof(string), typeof(string), values.GetType(), typeof(int), typeof(int) });
                Assert.NotNull(write);
                write.Invoke(writer, new object[] { "p", "item", "urn:items", values, 0, values.Length });
                writer.WriteEndElement();
            }
            byte[] bytes = document.ToArray();
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(bytes, fragmentSize: 1, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader("binary", source, null, new XmlDictionaryReaderQuotas { MaxBytesPerRead = 64 });
            try
            {
                Assert.True(await reader.ReadAsync());
                Assert.True(await reader.ReadAsync());
                Assert.True(reader.IsStartArray(out Type valueType));
                foreach (object expected in values)
                {
                    Assert.Equal("item", reader.LocalName);
                    Assert.Equal("urn:items", reader.NamespaceURI);
                    Assert.Equal(expected, await reader.ReadElementContentAsAsync(valueType, null));
                }
                Assert.Equal(XmlNodeType.EndElement, reader.NodeType);
                Assert.Equal("root", reader.LocalName);
                Assert.False(await reader.ReadAsync());
                Assert.True(GetBinaryReaderBuffer(reader).Length <= 256);
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Fact]
        public static async Task BinaryXml_ReadAsync_DictionariesListsAndQualifiedNames()
        {
            XmlDictionary dictionary = new XmlDictionary();
            XmlDictionaryString root = dictionary.Add("root");
            XmlDictionaryString child = dictionary.Add("child");
            XmlDictionaryString ns = dictionary.Add("urn:items");
            XmlDictionaryString localName = dictionary.Add("name");
            object[] list = new object[] { 65537, true, 1.25m, new Guid("01234567-89ab-cdef-0123-456789abcdef") };
            using MemoryStream document = new MemoryStream();
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(document, dictionary))
            {
                writer.WriteStartElement("a", root, ns);
                writer.WriteStartAttribute("list");
                writer.WriteValue(list);
                writer.WriteEndAttribute();
                writer.WriteStartElement("a", child, ns);
                writer.WriteQualifiedName(localName, ns);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document.ToArray(), fragmentSize: 1, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader("binary", source, null, dictionary: dictionary);
            try
            {
                Assert.True(await reader.ReadAsync());
                Assert.Equal("urn:items", reader.LookupNamespace("a"));
                Assert.True(reader.MoveToAttribute("list"));
                Assert.Equal(typeof(object[]), reader.ValueType);
                Assert.Equal(list, Assert.IsType<object[]>(await reader.ReadContentAsObjectAsync()));
                Assert.True(reader.MoveToElement());
                Assert.True(await reader.ReadAsync());
                Assert.Equal("child", reader.LocalName);
                Assert.True(await reader.ReadAsync());
                Assert.Equal("a:name", await reader.ReadContentAsStringAsync());
                Assert.Equal(XmlNodeType.EndElement, reader.NodeType);
                Assert.True(await reader.ReadAsync());
                Assert.False(await reader.ReadAsync());
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [InlineData(64, 64)]
        [InlineData(64, 3000)]
        [InlineData(2048, 2048)]
        [InlineData(2048, 3000)]
        public static async Task Json_ReadAsync_NumbersAcrossChunks(int maxBytesPerRead, int digits)
        {
            string number = "1" + new string('0', digits - 1);
            byte[] document = Encoding.UTF8.GetBytes(number);
            XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas { MaxBytesPerRead = maxBytesPerRead };
            using XmlDictionaryReader synchronous = JsonReaderWriterFactory.CreateJsonReader(new MemoryStream(document), quotas);
            Assert.Equal(number, synchronous.ReadElementContentAsString());
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document, fragmentSize: 1, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader("json", source, Encoding.UTF8, quotas);
            try
            {
                Assert.Equal(number, await reader.ReadElementContentAsStringAsync());
                Assert.True(reader.EOF);
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        public static IEnumerable<object[]> JsonTypedNumbers()
        {
            yield return new object[] { "1.2e1", typeof(int), 12 };
            yield return new object[] { "1.2e1", typeof(long), 12L };
            yield return new object[] { "1.2e1", typeof(decimal), 12m };
        }

        [Theory]
        [MemberData(nameof(JsonTypedNumbers))]
        public static async Task Json_ReadAsync_TypedNumbers(string number, Type valueType, object expected)
        {
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(Encoding.UTF8.GetBytes(number), fragmentSize: 1, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader("json", source, Encoding.UTF8);
            try
            {
                Assert.Equal(expected, await reader.ReadElementContentAsAsync(valueType, null));
                Assert.True(reader.EOF);
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [InlineData("xml", "depth")]
        [InlineData("xml", "bytes")]
        [InlineData("xml", "name")]
        [InlineData("xml", "string")]
        [InlineData("xml", "array")]
        [InlineData("json", "depth")]
        [InlineData("json", "bytes")]
        [InlineData("json", "name")]
        [InlineData("json", "string")]
        [InlineData("json", "array")]
        [InlineData("binary", "depth")]
        [InlineData("binary", "bytes")]
        [InlineData("binary", "name")]
        [InlineData("binary", "string")]
        [InlineData("binary", "array")]
        public static async Task Reader_ReadAsync_Quotas(string format, string quota)
        {
            XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas();
            string name = new string('n', 150);
            string value = new string('a', 200);
            string xml = $"<root>{value}</root>";
            string json = $"\"{value}\"";
            switch (quota)
            {
                case "depth":
                    quotas.MaxDepth = 2;
                    xml = "<root><a><b/></a></root>";
                    json = "[[[1]]]";
                    break;
                case "bytes":
                    quotas.MaxBytesPerRead = 64;
                    xml = $"<{name}/>";
                    json = $"{{\"{name}\":1}}";
                    break;
                case "name":
                    quotas.MaxNameTableCharCount = 1;
                    break;
                case "string":
                    quotas.MaxStringContentLength = 32;
                    break;
                case "array":
                    quotas.MaxArrayLength = 8;
                    value = Convert.ToBase64String(new byte[100]);
                    xml = $"<root>{value}</root>";
                    json = $"\"{value}\"";
                    break;
            }
            byte[] document = CreateReaderDocument(format, xml, json, Encoding.UTF8);
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document, fragmentSize: 1, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader(format, source, Encoding.UTF8, quotas);
            try
            {
                await Assert.ThrowsAsync<XmlException>(async () =>
                {
                    if (quota == "string")
                    {
                        await reader.ReadElementContentAsStringAsync();
                    }
                    else if (quota == "array")
                    {
                        Assert.True(await reader.ReadAsync());
                        Assert.True(await reader.ReadAsync());
                        await ReadByteArrayAsync(reader, CancellationToken.None);
                    }
                    else
                    {
                        while (await reader.ReadAsync())
                        {
                            if (quota == "name")
                                _ = reader.Name;
                        }
                    }
                });
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [InlineData("</valueSuffix>")]
        [InlineData("</valueX>")]
        [InlineData("</value X>")]
        [InlineData("</value")]
        public static async Task Reader_AsyncBufferedPrimitive_RejectsMalformedClosingTag(string closingTag)
        {
            byte[] document = Encoding.UTF8.GetBytes("<value>42" + closingTag);
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document, fragmentSize: 4096, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader("xml", source, Encoding.UTF8);
            try
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(int), "value", string.Empty);
                SerializationException exception = await Assert.ThrowsAsync<SerializationException>(() => serializer.ReadObjectAsync(reader));
                Assert.IsType<XmlException>(exception.InnerException);
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [InlineData("xml", "<root a='1'b='2'/>")]
        [InlineData("xml", "<root>&missing;</root>")]
        [InlineData("xml", "<root><a></b></root>")]
        [InlineData("xml", "<root><a>42</aSuffix></root>")]
        [InlineData("xml", "<root><a>42</a")]
        [InlineData("xml", "<root><a>42</a X></root>")]
        [InlineData("xml", "<root><!--unfinished")]
        [InlineData("xml", "<![CDATA[value]]>")]
        [InlineData("xml", "<root>\ufffe</root>")]
        [InlineData("json", "\"\\q\"")]
        [InlineData("json", "\"\\ud83d\\u0061\"")]
        [InlineData("json", "{\"n\":falsee}")]
        [InlineData("json", "{\"n\":nullx}")]
        [InlineData("binary", "invalid")]
        [InlineData("binary", "truncated")]
        [InlineData("binary", "continuations")]
        public static async Task Reader_ReadAsync_RejectsMalformedInput(string format, string text)
        {
            byte[] document = format != "binary" ? Encoding.UTF8.GetBytes(text) :
                text == "invalid" ? new byte[] { (byte)XmlBinaryNodeType.ShortElement, 1, (byte)'r', 255 } :
                text == "continuations" ? CreateBinaryTextDocument(XmlBinaryNodeType.Chars32Text, Enumerable.Repeat((byte)128, 513).ToArray(), withEndElement: true) :
                CreateBinaryTextDocument(XmlBinaryNodeType.Chars32Text, "value"u8, withEndElement: true).SkipLast(2).ToArray();
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document, fragmentSize: 1, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader(format, source, Encoding.UTF8,
                new XmlDictionaryReaderQuotas { MaxBytesPerRead = 64 });
            try
            {
                await Assert.ThrowsAsync<XmlException>(async () =>
                {
                    while (await reader.ReadAsync())
                        _ = await reader.GetValueAsync();
                });
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [InlineData("xml")]
        [InlineData("binary")]
        public static async Task Reader_ReadAsync_InnerAndOuterXml(string format)
        {
            string xml = "<root><value><child a=\"v\">text</child></value><tail/></root>";
            byte[] document = CreateReaderDocument(format, xml, string.Empty, Encoding.UTF8);
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document, fragmentSize: 1, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader(format, source, Encoding.UTF8);
            try
            {
                Assert.True(await reader.ReadAsync());
                Assert.True(await reader.ReadAsync());
                Assert.Equal("<value><child a=\"v\">text</child></value>", await reader.ReadOuterXmlAsync());
                Assert.Equal("tail", reader.LocalName);
                Assert.Equal(string.Empty, await reader.ReadInnerXmlAsync());
                Assert.Equal(XmlNodeType.EndElement, reader.NodeType);
                Assert.False(await reader.ReadAsync());
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [InlineData("xml", false, false)]
        [InlineData("xml", true, false)]
        [InlineData("binary", false, false)]
        [InlineData("binary", true, false)]
        [InlineData("binaryTyped", false, false)]
        [InlineData("binaryTyped", true, false)]
        [InlineData("xml", false, true)]
        [InlineData("binaryTyped", false, true)]
        public static async Task Reader_ReadAsync_CanonicalizationUsesAsyncSink(string format, bool includeComments, bool finalizeByClosing)
        {
            string value = string.Concat(Enumerable.Repeat("a<&\u00e9\ud83d\ude42", 200));
            string xml = $"<root xmlns=\"urn:root\" xmlns:p=\"urn:p\" xml:lang=\"en\" z=\"last\" a=\"&quot;&#xD;\"><!--comment--><p:child><![CDATA[{value}]]></p:child><empty/></root>";
            byte[] document = format == "binaryTyped" ? CreateTypedCanonicalDocument() : CreateReaderDocument(format, xml, string.Empty, Encoding.UTF8);
            if (format == "binaryTyped")
                format = "binary";
            using MemoryStream expected = new MemoryStream();
            using (XmlDictionaryReader synchronous = CreateSynchronousReader(format, document, Encoding.UTF8))
            {
                synchronous.StartCanonicalization(expected, includeComments, ["p"]);
                while (synchronous.Read()) { }
                synchronous.EndCanonicalization();
            }

            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document, fragmentSize: 1, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader(format, source, Encoding.UTF8);
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            using AsyncOnlyCanonicalStream canonical = new AsyncOnlyCanonicalStream(cancellation.Token);
            try
            {
                reader.StartCanonicalization(canonical, includeComments, ["p"]);
                Task reading = DrainCanonicalReader(reader, cancellation.Token);
                Task first = await Task.WhenAny(reading, canonical.WriteStarted).WaitAsync(TimeSpan.FromSeconds(30));
                if (first == reading)
                    await reading;
                Assert.False(reading.IsCompleted);
                canonical.ReleaseWrites();
                await reading;

                if (finalizeByClosing)
                    canonical.ExpectCleanup();
                ValueTask finalization = finalizeByClosing ? CloseReaderAsync(reader) : EndReaderCanonicalizationAsync(reader, cancellation.Token);
                await canonical.FlushStarted.WaitAsync(TimeSpan.FromSeconds(30));
                Assert.False(finalization.IsCompleted);
                canonical.ReleaseFlush();
                await finalization;
                Assert.Equal(expected.ToArray(), canonical.ToArray());
                Assert.True(canonical.AsyncWrites > 1);
                Assert.Equal(1, canonical.AsyncFlushes);
            }
            finally
            {
                canonical.ReleaseWrites();
                canonical.ReleaseFlush();
                canonical.ExpectCleanup();
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [InlineData("xml")]
        [InlineData("binary")]
        public static async Task Reader_CloseAsync_CanonicalFlushFailureStillClosesInput(string format)
        {
            byte[] document = CreateReaderDocument(format, "<root/>", string.Empty, Encoding.UTF8);
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document, blocked: false);
            int closed = 0;
            XmlDictionaryReader reader = await CreateAsyncReader(format, source, Encoding.UTF8, onClose: _ => closed++);
            using AsyncOnlyCanonicalStream canonical = new AsyncOnlyCanonicalStream(CancellationToken.None)
            {
                FlushFailure = new IOException("Canonical flush failed.")
            };
            canonical.ReleaseWrites();
            canonical.ReleaseFlush();
            reader.StartCanonicalization(canonical, includeComments: false, inclusivePrefixes: null);
            Assert.True(await reader.ReadAsync());
            IOException exception = await Assert.ThrowsAsync<IOException>(() => CloseReaderAsync(reader).AsTask());
            Assert.Same(canonical.FlushFailure, exception);
            Assert.Equal(ReadState.Closed, reader.ReadState);
            Assert.True(source.AsyncDisposed);
            Assert.False(canonical.Disposed);
            Assert.Equal(1, closed);
            await CloseReaderAsync(reader);
            Assert.Equal(1, closed);
        }

        private static byte[] CreateTypedCanonicalDocument()
        {
            using MemoryStream output = new MemoryStream();
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(output))
            {
                writer.WriteStartElement("root");
                writer.WriteStartAttribute("number");
                writer.WriteValue(int.MinValue);
                writer.WriteEndAttribute();
                object[] values = [int.MaxValue, long.MinValue, ulong.MaxValue, 3.5f, double.NegativeInfinity, 12.5m,
                    new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc), new UniqueId("urn:example:id"),
                    new Guid("00112233-4455-6677-8899-aabbccddeeff"), TimeSpan.FromTicks(123456789), true];
                foreach (object value in values)
                {
                    writer.WriteStartElement("value");
                    writer.WriteValue(value);
                    writer.WriteEndElement();
                }
                byte[] bytes = Enumerable.Range(0, 1200).Select(index => (byte)index).ToArray();
                writer.WriteStartElement("bytes");
                writer.WriteBase64(bytes, 0, bytes.Length);
                writer.WriteEndElement();
                int[] array = Enumerable.Range(0, 25).ToArray();
                writer.WriteArray(null, "item", null, array, 0, array.Length);
                writer.WriteEndElement();
            }
            return output.ToArray();
        }

        private static async Task DrainCanonicalReader(XmlDictionaryReader reader, CancellationToken cancellationToken)
        {
            while (await ReadWithCancellationAsync(reader, cancellationToken)) { }
        }

        private static ValueTask EndReaderCanonicalizationAsync(XmlDictionaryReader reader, CancellationToken cancellationToken)
        {
            MethodInfo method = reader.GetType().GetMethod("EndCanonicalizationAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            return (ValueTask)method.Invoke(reader, [cancellationToken]);
        }

        private sealed class AsyncOnlyCanonicalStream : Stream
        {
            private readonly MemoryStream _output = new MemoryStream();
            private CancellationToken _expectedToken;
            private readonly TaskCompletionSource<bool> _writeStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource<bool> _flushStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource<bool> _writeGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource<bool> _flushGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public AsyncOnlyCanonicalStream(CancellationToken expectedToken) => _expectedToken = expectedToken;
            public Task WriteStarted => _writeStarted.Task;
            public Task FlushStarted => _flushStarted.Task;
            public int AsyncWrites { get; private set; }
            public int AsyncFlushes { get; private set; }
            public Exception? FlushFailure { get; init; }
            public bool Disposed { get; private set; }
            public void ReleaseWrites() => _writeGate.TrySetResult(true);
            public void ReleaseFlush() => _flushGate.TrySetResult(true);
            public void ExpectCleanup() => _expectedToken = CancellationToken.None;
            public byte[] ToArray() => _output.ToArray();
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() => throw new InvalidOperationException("Synchronous canonical flush.");
            public override void Write(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Synchronous canonical write.");
            public override void Write(ReadOnlySpan<byte> buffer) => throw new InvalidOperationException("Synchronous canonical write.");
            public override void WriteByte(byte value) => throw new InvalidOperationException("Synchronous canonical write.");
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                Assert.Equal(_expectedToken, cancellationToken);
                AsyncWrites++;
                _writeStarted.TrySetResult(true);
                await _writeGate.Task.WaitAsync(cancellationToken);
                _output.Write(buffer.Span);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

            public override async Task FlushAsync(CancellationToken cancellationToken)
            {
                Assert.Equal(_expectedToken, cancellationToken);
                AsyncFlushes++;
                _flushStarted.TrySetResult(true);
                await _flushGate.Task.WaitAsync(cancellationToken);
                if (FlushFailure is Exception failure)
                {
                    throw failure;
                }
            }

            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                if (disposing)
                {
                    _output.Dispose();
                }
                base.Dispose(disposing);
            }

        }

        [Fact]
        public static async Task Reader_ReadAsync_WrappedReader()
        {
            string value = new string('a', 20000);
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(Encoding.UTF8.GetBytes($"<root>{value}</root>"), fragmentSize: 3, blocked: false);
            using XmlReader underlying = XmlReader.Create(source, new XmlReaderSettings { Async = true });
            using XmlDictionaryReader reader = XmlDictionaryReader.CreateDictionaryReader(underlying);
            Assert.Equal(XmlNodeType.Element, await reader.MoveToContentAsync());
            Assert.True(await reader.ReadAsync());
            Assert.Equal(value, await reader.ReadContentAsStringAsync());
            Assert.Equal(XmlNodeType.EndElement, reader.NodeType);
            Assert.False(await reader.ReadAsync());
        }

        [Theory]
        [InlineData("xml")]
        [InlineData("json")]
        [InlineData("binary")]
        public static async Task Reader_ReadAsync_CachedStringQuota(string format)
        {
            string value = new string('a', 200);
            byte[] document = CreateReaderDocument(format, $"<root>{value}</root>", $"\"{value}\"", Encoding.UTF8);
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document, fragmentSize: 1, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader(format, source, Encoding.UTF8,
                new XmlDictionaryReaderQuotas { MaxStringContentLength = 32 });
            try
            {
                Assert.True(await reader.ReadAsync());
                Assert.True(await reader.ReadAsync());
                Assert.Equal(value, await reader.GetValueAsync());
                await Assert.ThrowsAsync<XmlException>(() => reader.ReadContentAsStringAsync());
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [InlineData(false, false, 10)]
        [InlineData(false, false, 13)]
        [InlineData(true, false, 11)]
        [InlineData(true, false, 12)]
        [InlineData(false, true, 5)]
        public static async Task BinaryXml_ReadAsync_QuotaMustAllowChunkProgress(bool unicode, bool binary, int maxBytesPerRead)
        {
            Encoding encoding = unicode ? Encoding.Unicode : Encoding.UTF8;
            byte[] payload = encoding.GetBytes(string.Concat(Enumerable.Repeat("\ud83d\ude42", 25)));
            XmlBinaryNodeType nodeType = binary ? XmlBinaryNodeType.Bytes32Text :
                unicode ? XmlBinaryNodeType.UnicodeChars32Text : XmlBinaryNodeType.Chars32Text;
            byte[] document = CreateBinaryTextDocument(nodeType, payload, withEndElement: true);
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document, fragmentSize: 1, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader("binary", source, null,
                new XmlDictionaryReaderQuotas { MaxBytesPerRead = maxBytesPerRead });
            try
            {
                Assert.True(await reader.ReadAsync());
                await Assert.ThrowsAsync<XmlException>(() => reader.ReadAsync());
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [InlineData("xml")]
        [InlineData("json")]
        [InlineData("binary")]
        [DynamicDependency("SetInputAsync", "System.Xml.XmlUTF8TextReader", "System.Private.DataContractSerialization")]
        [DynamicDependency("SetInputAsync", "System.Runtime.Serialization.Json.XmlJsonReader", "System.Private.DataContractSerialization")]
        public static async Task Reader_ReadAsync_ReinitializationResetsBinaryChunks(string format)
        {
            byte[] first = new byte[] { 1, 2, 3, 4, 5, 6 };
            byte[] second = new byte[] { 9, 8, 7, 6 };
            string firstValue = Convert.ToBase64String(first);
            string secondValue = Convert.ToBase64String(second);
            byte[] firstDocument = CreateReaderDocument(format, $"<root>{firstValue}</root>", $"\"{firstValue}\"", Encoding.UTF8);
            byte[] secondDocument = CreateReaderDocument(format, $"<root>{secondValue}</root>", $"\"{secondValue}\"", Encoding.UTF8);
            using AsyncOnlyReadStream firstSource = new AsyncOnlyReadStream(firstDocument, fragmentSize: 1, blocked: false);
            using AsyncOnlyReadStream secondSource = new AsyncOnlyReadStream(secondDocument, fragmentSize: 1, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader(format, firstSource, Encoding.UTF8);
            try
            {
                Assert.True(await reader.ReadAsync());
                Assert.True(await reader.ReadAsync());
                byte[] chunk = new byte[1];
                Assert.Equal(1, await reader.ReadContentAsBase64Async(chunk, 0, chunk.Length));
                Assert.Equal(1, chunk[0]);
                await CloseReaderAsync(reader);
                XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas();
                if (format == "binary")
                {
                    ((IXmlBinaryReaderInitializer)reader).SetInput(secondSource, null, quotas, null, null);
                }
                else
                {
                    MethodInfo initialize = reader.GetType().GetMethod("SetInputAsync", BindingFlags.Instance | BindingFlags.NonPublic);
                    Assert.NotNull(initialize);
                    await (ValueTask)initialize.Invoke(reader, new object[] { secondSource, Encoding.UTF8, quotas, null, CancellationToken.None });
                }
                Assert.True(await reader.ReadAsync());
                Assert.True(await reader.ReadAsync());
                Assert.Equal(second, await ReadByteArrayAsync(reader, CancellationToken.None));
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [InlineData("xml")]
        [InlineData("binary")]
        public static async Task Reader_ReadAsync_QualifiedNameConversion(string format)
        {
            byte[] document = CreateReaderDocument(format, "<root xmlns:p=\"urn:name\">p:value</root>", string.Empty, Encoding.UTF8);
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(document, fragmentSize: 1, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader(format, source, Encoding.UTF8);
            XmlNamespaceManager namespaces = new XmlNamespaceManager(new NameTable());
            namespaces.AddNamespace("p", "urn:name");
            try
            {
                Assert.Equal(new XmlQualifiedName("value", "urn:name"), await reader.ReadElementContentAsAsync(typeof(XmlQualifiedName), namespaces));
                Assert.True(reader.EOF);
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        [Theory]
        [InlineData("true", 4, "true")]
        [InlineData("false", 5, "false")]
        [InlineData("null", 4, "")]
        [InlineData("\"\"", 2, "")]
        public static async Task Json_ReadAsync_LiteralFitsReadQuota(string text, int maxBytesPerRead, string expected)
        {
            using AsyncOnlyReadStream source = new AsyncOnlyReadStream(Encoding.UTF8.GetBytes(text), fragmentSize: 1, blocked: false);
            XmlDictionaryReader reader = await CreateAsyncReader("json", source, Encoding.UTF8,
                new XmlDictionaryReaderQuotas { MaxBytesPerRead = maxBytesPerRead });
            try
            {
                Assert.Equal(expected, await reader.ReadElementContentAsStringAsync());
                Assert.True(reader.EOF);
            }
            finally
            {
                await CloseReaderAsync(reader);
            }
        }

        private static ValueTask<byte[]> ReadByteArrayAsync(XmlDictionaryReader reader, CancellationToken cancellationToken)
        {
            MethodInfo read = reader.GetType().GetMethod(nameof(XmlReader.ReadContentAsBase64Async), BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null, types: new[] { typeof(CancellationToken) }, modifiers: null);
            Assert.NotNull(read);
            return (ValueTask<byte[]>)read.Invoke(reader, new object[] { cancellationToken });
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, "System.Xml.XmlDictionaryReader", "System.Private.DataContractSerialization")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, "System.Runtime.Serialization.Json.JsonReaderWriterFactory", "System.Private.DataContractSerialization")]
        private static ValueTask<XmlDictionaryReader> CreateAsyncReader(string format, Stream source, Encoding encoding,
            XmlDictionaryReaderQuotas quotas = null, OnXmlDictionaryReaderClose onClose = null, CancellationToken cancellationToken = default,
            IXmlDictionary dictionary = null, XmlBinaryReaderSession session = null)
        {
            quotas ??= new XmlDictionaryReaderQuotas();
            Type factoryType = format == "json" ? typeof(JsonReaderWriterFactory) : typeof(XmlDictionaryReader);
            string methodName = format == "json" ? "CreateJsonReaderAsync" : format == "binary" ? "CreateBinaryReaderAsync" : "CreateTextReaderAsync";
            MethodInfo factory = factoryType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(factory);
            object[] arguments = format == "binary" ? new object[] { source, dictionary, quotas, session, onClose, cancellationToken } :
                new object[] { source, encoding, quotas, onClose, cancellationToken };
            return (ValueTask<XmlDictionaryReader>)factory.Invoke(null, arguments);
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, "System.Xml.XmlBaseReader", "System.Private.DataContractSerialization")]
        private static ValueTask<bool> ReadWithCancellationAsync(XmlDictionaryReader reader, CancellationToken cancellationToken)
        {
            MethodInfo read = reader.GetType().GetMethod(nameof(XmlReader.ReadAsync), BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null, types: new[] { typeof(CancellationToken) }, modifiers: null);
            Assert.NotNull(read);
            return (ValueTask<bool>)read.Invoke(reader, new object[] { cancellationToken });
        }

        private static ValueTask CloseReaderAsync(XmlDictionaryReader reader)
        {
            MethodInfo close = reader.GetType().GetMethod("CloseAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(close);
            return (ValueTask)close.Invoke(reader, null);
        }

        private static XmlDictionaryReader CreateSynchronousReader(string format, byte[] document, Encoding encoding) =>
            format switch
            {
                "json" => JsonReaderWriterFactory.CreateJsonReader(document, 0, document.Length, encoding, new XmlDictionaryReaderQuotas(), null),
                "binary" => XmlDictionaryReader.CreateBinaryReader(document, new XmlDictionaryReaderQuotas()),
                _ => XmlDictionaryReader.CreateTextReader(document, 0, document.Length, encoding, new XmlDictionaryReaderQuotas(), null)
            };

        private static byte[] CreateReaderDocument(string format, string xml, string json, Encoding encoding)
        {
            if (format != "binary")
            {
                return encoding.GetPreamble().Concat(encoding.GetBytes(format == "json" ? json : xml)).ToArray();
            }

            using MemoryStream document = new MemoryStream();
            using (XmlReader reader = XmlReader.Create(new StringReader(xml)))
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(document))
            {
                writer.WriteNode(reader, defattr: false);
            }
            return document.ToArray();
        }

        private static string ReaderSnapshot(XmlDictionaryReader reader)
        {
            string[] attributes = new string[reader.AttributeCount];
            for (int i = 0; i < attributes.Length; i++)
            {
                reader.MoveToAttribute(i);
                attributes[i] = $"{reader.Name}|{reader.NamespaceURI}|{reader.Value}";
            }
            if (attributes.Length != 0)
                reader.MoveToElement();
            return $"{reader.NodeType}|{reader.Depth}|{reader.Name}|{reader.NamespaceURI}|{reader.IsEmptyElement}|{reader.ValueType}|{reader.Value}|{string.Join(";", attributes)}";
        }

        private sealed class AsyncOnlyReadStream : Stream
        {
            private TaskCompletionSource _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly MemoryStream _input;
            private readonly int _fragmentSize;
            private readonly bool _asyncDisposeOnly;

            public AsyncOnlyReadStream(byte[] input, int fragmentSize = 3, bool blocked = true, bool asyncDisposeOnly = false)
            {
                _input = new MemoryStream(input, writable: false);
                _fragmentSize = fragmentSize;
                _asyncDisposeOnly = asyncDisposeOnly;
                if (!blocked)
                    Release();
            }

            public int AsyncReads { get; private set; }
            public int MaxRequestedCount { get; private set; }
            public int BytesRead => (int)_input.Position;
            public CancellationToken LastCancellationToken { get; private set; }
            public bool AsyncDisposed { get; private set; }
            public override bool CanRead => true;
            public override bool CanWrite => false;
            public override bool CanSeek => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Synchronous write.");

            public void Release() => _gate.TrySetResult();
            public void Block() => _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Synchronous read.");
            public override int Read(Span<byte> buffer) => throw new InvalidOperationException("Synchronous read.");
            public override int ReadByte() => throw new InvalidOperationException("Synchronous read.");
            public override void Flush() => throw new InvalidOperationException("Synchronous flush.");

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                LastCancellationToken = cancellationToken;
                MaxRequestedCount = Math.Max(MaxRequestedCount, buffer.Length);
                await _gate.Task.WaitAsync(cancellationToken);
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                AsyncReads++;
                return _input.Read(buffer.Span.Slice(0, Math.Min(buffer.Length, _fragmentSize)));
            }

            public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

            protected override void Dispose(bool disposing)
            {
                if (_asyncDisposeOnly)
                    throw new InvalidOperationException("Synchronous dispose.");
                if (disposing)
                {
                    _input.Dispose();
                }
                base.Dispose(disposing);
            }

            public override ValueTask DisposeAsync()
            {
                AsyncDisposed = true;
                _input.Dispose();
                GC.SuppressFinalize(this);
                return ValueTask.CompletedTask;
            }
        }

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

        [Theory]
        [InlineData(64, 128, false)]
        [InlineData(64, 128, true)]
        [InlineData(119, 128, false)]
        [InlineData(119, 128, true)]
        [InlineData(122, 131, false)]
        [InlineData(122, 131, true)]
        [InlineData(125, 256, false)]
        [InlineData(125, 256, true)]
        public static void BinaryXml_ReadPartialBinaryText_BufferCapacity(int maxBytesPerRead, int expectedCapacity, bool withEndElement)
        {
            byte[] payload = Enumerable.Range(0, 513).Select(i => unchecked((byte)i)).ToArray();
            byte[] document = CreateBinaryTextDocument(XmlBinaryNodeType.Bytes32Text, payload, withEndElement);
            using var stream = new MemoryStream(document);
            var quotas = new XmlDictionaryReaderQuotas { MaxBytesPerRead = maxBytesPerRead };
            using XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(stream, quotas);

            Assert.True(reader.Read());
            Assert.Equal(XmlNodeType.Element, reader.NodeType);
            Assert.Equal("ab", reader.LocalName);
            byte[] initialBuffer = GetBinaryReaderBuffer(reader);
            Assert.Equal(128, initialBuffer.Length);

            Assert.True(reader.Read());
            Assert.Equal(XmlNodeType.Text, reader.NodeType);
            byte[] buffer = GetBinaryReaderBuffer(reader);
            Assert.Equal(expectedCapacity, buffer.Length);
            if (expectedCapacity == initialBuffer.Length)
            {
                Assert.Same(initialBuffer, buffer);
            }
            else
            {
                Assert.NotSame(initialBuffer, buffer);
            }

            int chunkLength = maxBytesPerRead - 5;
            chunkLength -= chunkLength % 3;
            Assert.Equal(Convert.ToBase64String(payload, 0, chunkLength), reader.Value);
            // Four bytes for the element header and five for the original text header.
            ReadOnlySpan<byte> insertedHeader = buffer.AsSpan(9 + chunkLength, 5);
            Assert.Equal((byte)(withEndElement ? XmlBinaryNodeType.Bytes32TextWithEndElement : XmlBinaryNodeType.Bytes32Text), insertedHeader[0]);
            Assert.Equal(payload.Length - chunkLength, BinaryPrimitives.ReadInt32LittleEndian(insertedHeader.Slice(1)));

            Assert.Equal(payload, reader.ReadContentAsBase64());
            Assert.Same(buffer, GetBinaryReaderBuffer(reader));
            Assert.Equal(XmlNodeType.EndElement, reader.NodeType);
            Assert.Equal("ab", reader.LocalName);
            Assert.False(reader.Read());
            Assert.Equal(stream.Length, stream.Position);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(59, false)]
        [InlineData(59, true)]
        [InlineData(60, false)]
        [InlineData(60, true)]
        [InlineData(513, false)]
        [InlineData(513, true)]
        public static void BinaryXml_ReadPartialBinaryText(int payloadLength, bool withEndElement)
        {
            byte[] expected = Enumerable.Range(0, payloadLength).Select(i => unchecked((byte)i)).ToArray();
            byte[] document = CreateBinaryTextDocument(XmlBinaryNodeType.Bytes32Text, expected, withEndElement);
            using var stream = new MemoryStream(document);
            var quotas = new XmlDictionaryReaderQuotas { MaxBytesPerRead = 64 };
            using XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(stream, quotas);

            Assert.True(reader.Read());
            Assert.Equal("ab", reader.LocalName);
            Assert.Equal(expected, reader.ReadElementContentAsBase64());
            Assert.Equal(XmlNodeType.None, reader.NodeType);
            Assert.True(reader.EOF);
            Assert.Equal(stream.Length, stream.Position);
        }

        [Theory]
        [InlineData(false, false, 64)]
        [InlineData(false, true, 64)]
        [InlineData(true, false, 64)]
        [InlineData(true, true, 64)]
        [InlineData(false, false, 125)]
        [InlineData(false, true, 125)]
        [InlineData(true, false, 125)]
        [InlineData(true, true, 125)]
        public static void BinaryXml_ReadPartialText(bool unicode, bool withEndElement, int maxBytesPerRead)
        {
            string expected = string.Concat(Enumerable.Repeat("a\u00E9\uD83D\uDE00", 129));
            Encoding encoding = unicode ? Encoding.Unicode : Encoding.UTF8;
            XmlBinaryNodeType nodeType = unicode ? XmlBinaryNodeType.UnicodeChars32Text : XmlBinaryNodeType.Chars32Text;
            byte[] document = CreateBinaryTextDocument(nodeType, encoding.GetBytes(expected), withEndElement);
            using var stream = new MemoryStream(document);
            var quotas = new XmlDictionaryReaderQuotas { MaxBytesPerRead = maxBytesPerRead };
            using XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(stream, quotas);

            Assert.True(reader.Read());
            Assert.Equal("ab", reader.LocalName);
            Assert.Equal(expected, reader.ReadElementContentAsString());
            Assert.Equal(XmlNodeType.None, reader.NodeType);
            Assert.True(reader.EOF);
            Assert.Equal(stream.Length, stream.Position);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void BinaryXml_ReadPartialBinaryText_Truncated(bool withEndElement)
        {
            byte[] document = CreateBinaryTextDocument(XmlBinaryNodeType.Bytes32Text, new byte[513], withEndElement);
            Array.Resize(ref document, document.Length - (withEndElement ? 1 : 2));
            using var stream = new MemoryStream(document);
            var quotas = new XmlDictionaryReaderQuotas { MaxBytesPerRead = 64 };
            using XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(stream, quotas);

            reader.ReadStartElement("ab");
            Assert.Throws<XmlException>(() => reader.ReadContentAsBase64());
        }

        [DynamicDependency("_bufferReader", "System.Xml.XmlBaseReader", "System.Private.DataContractSerialization")]
        [DynamicDependency("get_Buffer", "System.Xml.XmlBufferReader", "System.Private.DataContractSerialization")]
        private static byte[] GetBinaryReaderBuffer(XmlDictionaryReader reader)
        {
            object bufferReader = reader.GetType().BaseType.GetField("_bufferReader", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(reader);
            return (byte[])bufferReader.GetType().GetProperty("Buffer").GetValue(bufferReader);
        }

        private static byte[] CreateBinaryTextDocument(XmlBinaryNodeType nodeType, ReadOnlySpan<byte> payload, bool withEndElement)
        {
            using var stream = new MemoryStream();
            stream.WriteByte((byte)XmlBinaryNodeType.ShortElement);
            stream.WriteByte(2);
            stream.Write("ab"u8);
            stream.WriteByte((byte)(withEndElement ? nodeType | XmlBinaryNodeType.EndElement : nodeType));
            Span<byte> length = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(length, payload.Length);
            stream.Write(length);
            stream.Write(payload);
            if (!withEndElement)
            {
                stream.WriteByte((byte)XmlBinaryNodeType.EndElement);
            }
            return stream.ToArray();
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
