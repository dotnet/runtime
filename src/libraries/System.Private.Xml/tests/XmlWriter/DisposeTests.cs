// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Xml.Tests
{
    public class MyXmlWriter : XmlWriter
    {
        public MyXmlWriter() { IsDisposed = false; }
        public bool IsDisposed { get; private set; }
        protected override void Dispose(bool disposing) { IsDisposed = true; }

        // Implementation of the abstract class
        public override void Flush() { }
        public override string LookupPrefix(string ns) { return default(string); }
        public override void WriteBase64(byte[] buffer, int index, int count) { }
        public override void WriteCData(string text) { }
        public override void WriteCharEntity(char ch) { }
        public override void WriteChars(char[] buffer, int index, int count) { }
        public override void WriteComment(string text) { }
        public override void WriteDocType(string name, string pubid, string sysid, string subset) { }
        public override void WriteEndAttribute() { }
        public override void WriteEndDocument() { }
        public override void WriteEndElement() { }
        public override void WriteEntityRef(string name) { }
        public override void WriteFullEndElement() { }
        public override void WriteProcessingInstruction(string name, string text) { }
        public override void WriteRaw(string data) { }
        public override void WriteRaw(char[] buffer, int index, int count) { }
        public override void WriteStartAttribute(string prefix, string localName, string ns) { }
        public override void WriteStartDocument(bool standalone) { }
        public override void WriteStartDocument() { }
        public override void WriteStartElement(string prefix, string localName, string ns) { }
        public override WriteState WriteState { get { return default(WriteState); } }
        public override void WriteString(string text) { }
        public override void WriteSurrogateCharEntity(char lowChar, char highChar) { }
        public override void WriteWhitespace(string ws) { }
    }

    public static class XmlWriterDisposeTests
    {
        public static string ReadAsString(MemoryStream ms)
        {
            byte[] buffer = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(buffer, 0, buffer.Length);
            return (new UTF8Encoding(false)).GetString(buffer, 0, buffer.Length);
        }

        [Fact]
        public static void DisposeFlushesAndDisposesOutputStream()
        {
            bool[] asyncValues = { false, true };
            bool[] closeOutputValues = { false, true };
            bool[] indentValues = { false, true };
            bool[] omitXmlDeclarationValues = { false, true };
            bool[] writeEndDocumentOnCloseValues = { false, true };
            foreach (var async in asyncValues)
                foreach (var closeOutput in closeOutputValues)
                    foreach (var indent in indentValues)
                        foreach (var omitXmlDeclaration in omitXmlDeclarationValues)
                            foreach (var writeEndDocumentOnClose in writeEndDocumentOnCloseValues)
                            {
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    XmlWriterSettings settings = new XmlWriterSettings();
                                    // UTF8 without BOM
                                    settings.Encoding = new UTF8Encoding(false);
                                    settings.Async = async;
                                    settings.CloseOutput = closeOutput;
                                    settings.Indent = indent;
                                    settings.OmitXmlDeclaration = omitXmlDeclaration;
                                    settings.WriteEndDocumentOnClose = writeEndDocumentOnClose;
                                    XmlWriter writer = XmlWriter.Create(ms, settings);
                                    writer.WriteStartDocument();
                                    writer.WriteStartElement("root");
                                    writer.WriteStartElement("test");
                                    writer.WriteString("abc");
                                    // !!! intentionally not closing both elements
                                    // !!! writer.WriteEndElement();
                                    // !!! writer.WriteEndElement();
                                    writer.Dispose();

                                    if (closeOutput)
                                    {
                                        bool failed = true;
                                        try
                                        {
                                            ms.WriteByte(123);
                                        }
                                        catch (ObjectDisposedException) { failed = false; }
                                        if (failed)
                                        {
                                            throw new Exception("Failed!");
                                        }
                                    }
                                    else
                                    {
                                        string output = ReadAsString(ms);
                                        Assert.Contains("<test>abc", output);
                                        Assert.NotEqual(output.Contains("<?xml version"), omitXmlDeclaration);
                                        Assert.Equal(output.Contains("  "), indent);
                                        Assert.Equal(output.Contains("</test>"), writeEndDocumentOnClose);
                                    }

                                    // should not throw
                                    writer.Dispose();
                                }
                            }
        }

        [Fact]
        public static void XmlWriterDisposeWorksWithDerivingClasses()
        {
            MyXmlWriter mywriter = new MyXmlWriter();
            Assert.False(mywriter.IsDisposed);
            mywriter.Dispose();
            Assert.True(mywriter.IsDisposed);
        }

        [Fact]
        public static async Task AsyncWriter_DisposeAsync_ShouldCall_FlushAsyncWriteAsyncOnly_StreamWriter()
        {
            using (var stream = new AsyncOnlyStream())
            await using (var writer = XmlWriter.Create(stream, new XmlWriterSettings() { Async = true }))
            {
                await writer.WriteStartDocumentAsync();
                await writer.WriteStartElementAsync(string.Empty, "root", null);
                await writer.WriteStartElementAsync(null, "test", null);
                await writer.WriteAttributeStringAsync(string.Empty, "abc", string.Empty, "1");
                await writer.WriteEndElementAsync();
                await writer.WriteEndElementAsync();
            }
        }

        [Fact]
        public static async Task XmlWriter_AsyncSyncResult_ShouldBeSame_AfterDispose_StreamWriter()
        {
            var settings = new XmlWriterSettings() { Async = true, Encoding = new UTF8Encoding(false) };
            using (var stream1 = new MemoryStream())
            using (var stream2 = new MemoryStream())
            using (var stream3 = new MemoryStream())
            {
                using (var asyncWriter = XmlWriter.Create(stream1, settings))
                {
                    await asyncWriter.WriteStartDocumentAsync();
                    await asyncWriter.WriteStartElementAsync(string.Empty, "root", null);
                    await asyncWriter.WriteStartElementAsync(null, "test", null);
                    await asyncWriter.WriteAttributeStringAsync(string.Empty, "abc", string.Empty, "1");
                    await asyncWriter.WriteEndElementAsync();
                    await asyncWriter.WriteEndElementAsync();
                }

                await using (var asyncWriter = XmlWriter.Create(stream2, settings))
                {
                    await asyncWriter.WriteStartDocumentAsync();
                    await asyncWriter.WriteStartElementAsync(string.Empty, "root", null);
                    await asyncWriter.WriteStartElementAsync(null, "test", null);
                    await asyncWriter.WriteAttributeStringAsync(string.Empty, "abc", string.Empty, "1");
                    await asyncWriter.WriteEndElementAsync();
                    await asyncWriter.WriteEndElementAsync();
                }

                settings.Async = false;
                using (var syncWriter = XmlWriter.Create(stream3, settings))
                {
                    syncWriter.WriteStartDocument();
                    syncWriter.WriteStartElement(string.Empty, "root", null);
                    syncWriter.WriteStartElement(null, "test", null);
                    syncWriter.WriteAttributeString(string.Empty, "abc", string.Empty, "1");
                    syncWriter.WriteEndElement();
                    syncWriter.WriteEndElement();
                }

                Assert.Equal(stream1.GetBuffer(), stream2.GetBuffer());
                Assert.Equal(stream2.GetBuffer(), stream3.GetBuffer());

                Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?><root><test abc=""1"" /></root>", ReadAsString(stream1.GetBuffer(), stream1.Length));
                Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?><root><test abc=""1"" /></root>", ReadAsString(stream2.GetBuffer(), stream2.Length));
                Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?><root><test abc=""1"" /></root>", ReadAsString(stream3.GetBuffer(), stream3.Length));
            }
        }

        private static string ReadAsString(byte[] bytes, long length) => Encoding.UTF8.GetString(bytes, 0, (int)length);

        [Fact]
        public static async Task AsyncWriterDispose_ShouldCall_FlushAsyncWriteAsyncOnly_TextWriter()
        {
            using (var sw = new AsyncOnlyWriter())
            await using (var writer = XmlWriter.Create(sw, new XmlWriterSettings() { Async = true }))
            {
                await writer.WriteStartElementAsync(null, "book", null);
                await writer.WriteElementStringAsync(null, "price", null, "19.95");
                await writer.WriteEndElementAsync();
            }
        }

        [Fact]
        public static async Task XmlWriter_AsyncSyncResult_ShouldBeSame_AfterDispose_TextWriter()
        {
            using (var sw1 = new StringWriter())
            using (var sw2 = new StringWriter())
            using (var sw3 = new StringWriter())
            {
                using (var asyncWriter = XmlWriter.Create(sw1, new XmlWriterSettings() { Async = true }))
                {
                    await asyncWriter.WriteStartElementAsync(null, "book", null);
                    await asyncWriter.WriteElementStringAsync(null, "price", null, "19.95");
                    await asyncWriter.WriteEndElementAsync();
                }

                await using (var asyncWriter = XmlWriter.Create(sw2, new XmlWriterSettings() { Async = true }))
                {
                    await asyncWriter.WriteStartElementAsync(null, "book", null);
                    await asyncWriter.WriteElementStringAsync(null, "price", null, "19.95");
                    await asyncWriter.WriteEndElementAsync();
                }

                using (var syncWriter = XmlWriter.Create(sw3, new XmlWriterSettings() { Async = false }))
                {
                    syncWriter.WriteStartElement(null, "book", null);
                    syncWriter.WriteElementString(null, "price", null, "19.95");
                    syncWriter.WriteEndElement();
                }

                Assert.Equal(sw1.ToString(), sw2.ToString());
                Assert.Equal(sw1.ToString(), sw3.ToString());
            }
        }

        internal class AsyncOnlyWriter : StringWriter
        {
            public override void Flush()
            {
                throw new InvalidOperationException("Sync operations are not allowed.");
            }

            public override Task FlushAsync()
            {
                return Task.CompletedTask;
            }

            public override void Write(char[] buffer, int offset, int count)
            {
                throw new InvalidOperationException("Sync operations are not allowed.");
            }

            public override Task WriteAsync(char[] buffer, int offset, int count)
            {
                return Task.CompletedTask;
            }

            public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }

        internal class AsyncOnlyStream : MemoryStream
        {
            public override void Flush()
            {
                throw new InvalidOperationException("Sync operations are not allowed.");
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new InvalidOperationException("Sync operations are not allowed.");
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
            {
                return default;
            }
        }
    }
}
