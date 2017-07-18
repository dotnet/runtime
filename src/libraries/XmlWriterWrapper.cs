using System;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.ServiceModel.Syndication
{
    internal class XmlWriterWrapper : XmlWriter
    {

        private XmlWriter writer;

        private Func<XmlWriterWrapper, string, Task> writeStringFunc;
        private Func<XmlWriterWrapper, string, string, Task> writeStartElementFunc2;
        private Func<XmlWriterWrapper, string, Task> writeStartElementFunc1;
        private Func<XmlWriterWrapper, Task> writeEndElementFunc;
        private Func<XmlWriterWrapper, string, string, Task> writeElementStringFunc1;
        private Func<XmlWriterWrapper, string, string, string, Task> writeElementStringFunc2;
        private Func<XmlWriterWrapper, string, string, Task> writeAttributeStringFunc2;
        private Func<XmlWriterWrapper, string, string, string, Task> writeAttributeStringFunc3;
        private Func<XmlWriterWrapper, string, string, string, string, Task> writeAttributeStringFunc4;

        private Func<XmlWriterWrapper, XmlReader, bool, Task> WriteNodeFunc;


        private void InitAsync()
        {
            this.writeStringFunc = new Func<XmlWriterWrapper, string, Task>((thisPtr, text) => { return thisPtr.writer.WriteStringAsync(text); });
            this.writeStartElementFunc1 = new Func<XmlWriterWrapper, string, Task>( (thisPtr,localname) => { return this.writer.WriteStartElementAsync("",localname,""); });
            this.writeStartElementFunc2 = new Func<XmlWriterWrapper, string, string, Task>((thisPtr, localName, ns) => { return this.writer.WriteStartElementAsync("", localName, ns); });
            this.writeEndElementFunc = new Func<XmlWriterWrapper, Task>((thisPtr) => { return thisPtr.writer.WriteEndElementAsync(); });
            this.writeElementStringFunc1 = new Func<XmlWriterWrapper, string, string, Task>((thisPtr, localName, value) => { return thisPtr.writer.WriteElementStringAsync("", localName, "", value); });
            this.writeElementStringFunc2 = new Func<XmlWriterWrapper, string, string, string, Task>((thisPtr, localName, ns, value) => { return thisPtr.writer.WriteElementStringAsync("", localName, ns, value); });
            this.writeAttributeStringFunc2 = new Func<XmlWriterWrapper, string, string, Task>((thisPtr,localname,value) => { return thisPtr.writer.WriteAttributeStringAsync("", localname, "", value); });
            this.writeAttributeStringFunc3 = new Func<XmlWriterWrapper, string, string, string, Task>( (thisPtr,localName, ns, value) => { return thisPtr.writer.WriteAttributeStringAsync("", localName, ns, value); });
            this.writeAttributeStringFunc4 = new Func<XmlWriterWrapper, string, string, string, string, Task>((thisPtr, prefix, localName, ns, value) => { return thisPtr.writer.WriteAttributeStringAsync(prefix, localName, ns, value); });

            this.WriteNodeFunc = new Func<XmlWriterWrapper, XmlReader, bool, Task>((thisPtr,reader,defattr) => { return thisPtr.writer.WriteNodeAsync(reader, defattr);  });
        }

        private void Init()
        {
            this.writeStringFunc = new Func<XmlWriterWrapper, string, Task>((thisPtr, text) => { thisPtr.writer.WriteString(text); return Task.CompletedTask; });
            this.writeStartElementFunc1 = new Func<XmlWriterWrapper, string, Task>((thisPtr, localname) => { this.writer.WriteStartElement(localname); return Task.CompletedTask; });
            this.writeStartElementFunc2 = new Func<XmlWriterWrapper, string, string, Task>((thisPtr, localName, ns) => { this.writer.WriteStartElement(localName,ns); return Task.CompletedTask; });
            this.writeEndElementFunc = new Func<XmlWriterWrapper, Task>((thisPtr) => { thisPtr.writer.WriteEndElement(); return Task.CompletedTask; });
            this.writeElementStringFunc1 = new Func<XmlWriterWrapper, string, string, Task>((thisPtr, localName, value) => { thisPtr.writer.WriteElementString(localName, value); return Task.CompletedTask; });
            this.writeElementStringFunc2 = new Func<XmlWriterWrapper, string, string, string, Task>((thisPtr, localName, ns, value) => { thisPtr.writer.WriteElementString(localName,ns,value); return Task.CompletedTask; });
            this.writeAttributeStringFunc2 = new Func<XmlWriterWrapper, string, string, Task>((thisPtr, localname, value) => {  thisPtr.writer.WriteAttributeString("", localname, "", value); return Task.CompletedTask; });
            this.writeAttributeStringFunc3 = new Func<XmlWriterWrapper, string, string, string, Task>((thisPtr, localName, ns, value) => { thisPtr.writer.WriteAttributeString(localName,ns,value); return Task.CompletedTask; });
            this.writeAttributeStringFunc4 = new Func<XmlWriterWrapper, string, string, string, string, Task>((thisPtr, prefix, localName, ns, value) => { thisPtr.writer.WriteAttributeString(prefix, localName, ns, value); return Task.CompletedTask; });

            this.WriteNodeFunc = new Func<XmlWriterWrapper, XmlReader, bool, Task>((thisPtr,reader,defattr) => { thisPtr.writer.WriteNode(reader, defattr); return Task.CompletedTask; });
        }

        public static XmlWriterWrapper CreateFromWriter(XmlWriter writer)
        {
            XmlWriterWrapper wrappedWriter = writer as XmlWriterWrapper;

            return wrappedWriter != null ? wrappedWriter : new XmlWriterWrapper(writer);
        }


        public XmlWriterWrapper(XmlWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            this.writer = writer;

            if (this.writer.Settings.Async)
            {
                InitAsync();
            }
            else
            {
                Init();
            }
        }


        // wrapper methods

        public override Task WriteNodeAsync(XmlReader reader, bool defattr)
        {
            return WriteNodeFunc(this,reader,defattr);
        }

        public override Task WriteStringAsync(string text)
        {
            return writeStringFunc(this,text);
        }

        public Task WriteStartElementAsync(string localName)
        {
            return writeStartElementFunc1(this, localName);
        }

        public Task WriteStartElementAsync(string localName, string ns)
        {
            return writeStartElementFunc2(this,localName,ns);
        }

        public override Task WriteEndElementAsync()
        {
            return writeEndElementFunc(this);
        }

        public Task WriteElementStringAsync(string localName, string value)
        {
            return writeElementStringFunc1(this,localName,value);
        }

        public Task WriteElementStringAsync(string localName, string ns, string value)
        {
            return writeElementStringFunc2(this, localName, ns, value);
        }

        public new Task WriteAttributeStringAsync(string prefix, string localName, string ns, string value) // ????
        {
            return writeAttributeStringFunc4(this, prefix, localName, ns, value);
        }

        public Task WriteAttributeStringAsync(string localName, string ns, string value)
        {
            return writeAttributeStringFunc3(this,localName, ns, value);
        }

        public Task WriteAttributeStringAsync(string localName,string value)
        {
            return writeAttributeStringFunc3(this, localName, "", value);
        }

        // inherited methods
        public override WriteState WriteState
        {
            get
            {
                return this.writer.WriteState;
            }
        }
        

        public override void Flush()
        {
            this.writer.Flush();
        }

        public override string LookupPrefix(string ns)
        {
            return this.writer.LookupPrefix(ns);
        }

        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            this.writer.WriteBase64(buffer, index, count);
        }

        public override void WriteCData(string text)
        {
            this.writer.WriteCData(text);
        }

        public override void WriteCharEntity(char ch)
        {
            this.writer.WriteCharEntity(ch);
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            this.writer.WriteChars(buffer,index,count);
        }

        public override void WriteComment(string text)
        {
            this.writer.WriteComment(text);
        }

        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            this.writer.WriteDocType(name, pubid, sysid, subset);
        }

        public override void WriteEndAttribute()
        {
            this.writer.WriteEndAttribute();
        }

        public override void WriteEndDocument()
        {
            this.writer.WriteEndDocument();
        }

        public override void WriteEndElement()
        {
            this.writer.WriteEndElement();
        }

        public override void WriteEntityRef(string name)
        {
            this.writer.WriteEntityRef(name);
        }

        public override void WriteFullEndElement()
        {
            this.writer.WriteFullEndElement();
        }

        public override void WriteProcessingInstruction(string name, string text)
        {
            this.writer.WriteProcessingInstruction(name, text);
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            this.writer.WriteRaw(buffer,index,count);
        }

        public override void WriteRaw(string data)
        {
            this.writer.WriteRaw(data);
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            this.writer.WriteStartAttribute(prefix,localName,ns);
        }

        public override void WriteStartDocument()
        {
            this.writer.WriteStartDocument();
        }

        public override void WriteStartDocument(bool standalone)
        {
            this.writer.WriteStartDocument(standalone);
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            this.writer.WriteStartElement(prefix,localName,ns);
        }

        public override void WriteString(string text)
        {
            this.writer.WriteString(text);
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            this.writer.WriteSurrogateCharEntity(lowChar, highChar);
        }

        public override void WriteWhitespace(string ws)
        {
            this.writer.WriteWhitespace(ws);
        }
    }

    internal static class XmlWritterExtensions
    {
        //public static async Task WriteNodeAsync(this XmlWriter writer, XmlReader reader, bool defattr)
        //{
        //    if (null == reader)
        //    {
        //        throw new ArgumentNullException("reader");
        //    }

        //    bool canReadChunk = reader.CanReadValueChunk;
        //    int d = reader.NodeType == XmlNodeType.None ? -1 : reader.Depth;
        //    do
        //    {
        //        switch (reader.NodeType)
        //        {
        //            case XmlNodeType.Element:
        //                writer.WriteStartElementAsync(reader.Prefix, reader.LocalName, reader.NamespaceURI);
        //                writer.WriteAttributesAsync(reader, defattr);
        //                if (reader.IsEmptyElement)
        //                {
        //                    writer.WriteEndElementAsync();
        //                    break;
        //                }
        //                break;
        //            case XmlNodeType.Text:
        //                if (canReadChunk)
        //                {
        //                    if (writer.writeNodeBuffer == null)
        //                    {
        //                        writeNodeBuffer = new char[WriteNodeBufferSize];
        //                    }
        //                    int read;
        //                    while ((read = reader.ReadValueChunk(writeNodeBuffer, 0, WriteNodeBufferSize)) > 0)
        //                    {
        //                        this.WriteChars(writeNodeBuffer, 0, read);
        //                    }
        //                }
        //                else
        //                {

        //                    WriteString(reader.Value);

        //                }
        //                break;
        //            case XmlNodeType.Whitespace:
        //            case XmlNodeType.SignificantWhitespace:

        //                WriteWhitespace(reader.Value);

        //                break;
        //            case XmlNodeType.CDATA:
        //                WriteCData(reader.Value);
        //                break;
        //            case XmlNodeType.EntityReference:
        //                WriteEntityRef(reader.Name);
        //                break;
        //            case XmlNodeType.XmlDeclaration:
        //            case XmlNodeType.ProcessingInstruction:
        //                WriteProcessingInstruction(reader.Name, reader.Value);
        //                break;
        //            case XmlNodeType.DocumentType:
        //                WriteDocType(reader.Name, reader.GetAttribute("PUBLIC"), reader.GetAttribute("SYSTEM"), reader.Value);
        //                break;

        //            case XmlNodeType.Comment:
        //                WriteComment(reader.Value);
        //                break;
        //            case XmlNodeType.EndElement:
        //                WriteFullEndElement();
        //                break;
        //        }
        //    } while (reader.Read() && (d < reader.Depth || (d == reader.Depth && reader.NodeType == XmlNodeType.EndElement)));
        //}

    }
}
