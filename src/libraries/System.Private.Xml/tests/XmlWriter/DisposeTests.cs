// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
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
                                        Assert.True(output.Contains("<test>abc"));
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
    }
}
