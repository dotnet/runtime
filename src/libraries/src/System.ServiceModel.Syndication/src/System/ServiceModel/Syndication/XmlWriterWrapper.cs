// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Xml;

namespace System.ServiceModel.Syndication
{
    internal class XmlWriterWrapper : XmlWriter
    {
        private XmlWriter _writer;

        public static XmlWriter CreateFromWriter(XmlWriter writer)
        {
            if (writer is XmlWriterWrapper || (writer.Settings != null && writer.Settings.Async))
            {
                return writer;
            }

            return new XmlWriterWrapper(writer);
        }

        private XmlWriterWrapper(XmlWriter writer)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));

            if (writer.Settings != null && writer.Settings.Async)
            {
                throw new ArgumentException("Async XmlWriter should not be wrapped", nameof(writer));
            }
        }

        #region AsyncImplementations

        public override Task WriteNodeAsync(XmlReader reader, bool defattr)
        {
            _writer.WriteNode(reader, defattr);
            return Task.CompletedTask;
        }

        public override Task WriteStringAsync(string text)
        {
            _writer.WriteString(text);
            return Task.CompletedTask;
        }

        public override Task WriteStartElementAsync(string prefix, string localName, string ns)
        {
            _writer.WriteStartElement(prefix, localName, ns);
            return Task.CompletedTask;
        }

        public override Task WriteEndElementAsync()
        {
            _writer.WriteEndElement();
            return Task.CompletedTask;
        }

        public override Task WriteAttributesAsync(XmlReader reader, bool defattr)
        {
            _writer.WriteAttributes(reader, defattr);
            return Task.CompletedTask;
        }

        protected override Task WriteStartAttributeAsync(string prefix, string localName, string ns)
        {
            _writer.WriteStartAttribute(prefix, localName, ns);
            return Task.CompletedTask;
        }

        protected override Task WriteEndAttributeAsync()
        {
            _writer.WriteEndAttribute();
            return Task.CompletedTask;
        }
        #endregion

        #region DelegatingOverrides
        public override WriteState WriteState => _writer.WriteState;

        public override void Flush() => _writer.Flush();

        public override string LookupPrefix(string ns) => _writer.LookupPrefix(ns);

        public override void WriteBase64(byte[] buffer, int index, int count) => _writer.WriteBase64(buffer, index, count);

        public override void WriteCData(string text) => _writer.WriteCData(text);

        public override void WriteCharEntity(char ch) => _writer.WriteCharEntity(ch);

        public override void WriteChars(char[] buffer, int index, int count) => _writer.WriteChars(buffer, index, count);

        public override void WriteComment(string text) => _writer.WriteComment(text);

        public override void WriteDocType(string name, string pubid, string sysid, string subset) => _writer.WriteDocType(name, pubid, sysid, subset);

        public override void WriteEndAttribute() => _writer.WriteEndAttribute();

        public override void WriteEndDocument() => _writer.WriteEndDocument();

        public override void WriteEndElement() => _writer.WriteEndElement();

        public override void WriteEntityRef(string name) => _writer.WriteEntityRef(name);

        public override void WriteFullEndElement() => _writer.WriteFullEndElement();

        public override void WriteProcessingInstruction(string name, string text) => _writer.WriteProcessingInstruction(name, text);

        public override void WriteRaw(char[] buffer, int index, int count) => _writer.WriteRaw(buffer, index, count);

        public override void WriteRaw(string data) => _writer.WriteRaw(data);

        public override void WriteStartAttribute(string prefix, string localName, string ns) => _writer.WriteStartAttribute(prefix, localName, ns);

        public override void WriteStartDocument() => _writer.WriteStartDocument();

        public override void WriteStartDocument(bool standalone) => _writer.WriteStartDocument(standalone);

        public override void WriteStartElement(string prefix, string localName, string ns) =>_writer.WriteStartElement(prefix, localName, ns);

        public override void WriteString(string text) => _writer.WriteString(text);

        public override void WriteSurrogateCharEntity(char lowChar, char highChar) => _writer.WriteSurrogateCharEntity(lowChar, highChar);

        public override void WriteWhitespace(string ws) => _writer.WriteWhitespace(ws);
#endregion
    }

    internal static class XmlWriterExtensions
    {
        // These are Async equivalents of sync methods on XmlWriter which can be rewritten to be async as they are
        // composed of methods which there are existing async versions
        public static Task WriteStartElementAsync(this XmlWriter writer, string localName)
        {
            return writer.WriteStartElementAsync(null, localName, (string)null);
        }

        public static Task WriteStartElementAsync(this XmlWriter writer, string localName, string ns)
        {
            return writer.WriteStartElementAsync(null, localName, ns);
        }

        public static Task WriteElementStringAsync(this XmlWriter writer, string localName, string value)
        {
            return writer.WriteElementStringAsync(null, localName, null, value);
        }

        public static Task WriteElementStringAsync(this XmlWriter writer, string localName, string ns, string value)
        {
            return writer.WriteElementStringAsync(null, localName, ns, value);
        }

        public static Task WriteAttributeStringAsync(this XmlWriter writer, string localName, string ns, string value)
        {
            return writer.WriteAttributeStringAsync(null, localName, ns, value);
        }

        public static Task WriteAttributeStringAsync(this XmlWriter writer, string localName, string value)
        {
            return writer.WriteAttributeStringAsync(null, localName, null, value);
        }

        // XmlDictionaryWriter.WriteAsync isn't supported for the type of writer returned by XmlDictionaryWriter.CreateBinaryWriter
        // as that writer isn't Async. We still want to read from the reader async so this is a hybrid implementation of WriteNodeAsync.
        // It uses synchronous api's when using the writer and async api's when using the reader.
        public static async Task InternalWriteNodeAsync(this XmlDictionaryWriter writer, XmlReader reader, bool defattr)
        {
            char[] writeNodeBuffer = null;
            const int WriteNodeBufferSize = 1024;

            if (null == reader)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            bool canReadChunk = reader.CanReadValueChunk;
            int d = reader.NodeType == XmlNodeType.None ? -1 : reader.Depth;
            do
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                        writer.WriteAttributes(reader, defattr);
                        if (reader.IsEmptyElement)
                        {
                            writer.WriteEndElement();
                        }
                        break;
                        
                    case XmlNodeType.Text:
                        if (canReadChunk)
                        {
                            if (writeNodeBuffer == null)
                            {
                                writeNodeBuffer = new char[WriteNodeBufferSize];
                            }
                            int read;
                            while ((read = await reader.ReadValueChunkAsync(writeNodeBuffer, 0, WriteNodeBufferSize)) > 0)
                            {
                                writer.WriteChars(writeNodeBuffer, 0, read);
                            }
                        }
                        else
                        {
                            writer.WriteString(await reader.GetValueAsync());
                        }
                        break;

                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        writer.WriteWhitespace(await reader.GetValueAsync());
                        break;

                    case XmlNodeType.CDATA:
                        writer.WriteCData(await reader.GetValueAsync());
                        break;

                    case XmlNodeType.EntityReference:
                        writer.WriteEntityRef(reader.Name);
                        break;

                    case XmlNodeType.XmlDeclaration:
                    case XmlNodeType.ProcessingInstruction:
                        writer.WriteProcessingInstruction(reader.Name, await reader.GetValueAsync());
                        break;

                    case XmlNodeType.DocumentType:
                        writer.WriteDocType(reader.Name, reader.GetAttribute("PUBLIC"), reader.GetAttribute("SYSTEM"), await reader.GetValueAsync());
                        break;

                    case XmlNodeType.Comment:
                        writer.WriteComment(await reader.GetValueAsync());
                        break;

                    case XmlNodeType.EndElement:
                        writer.WriteFullEndElement();
                        break;
                }
            } while (await reader.ReadAsync() && (d < reader.Depth || (d == reader.Depth && reader.NodeType == XmlNodeType.EndElement)));
        }
    }
}
