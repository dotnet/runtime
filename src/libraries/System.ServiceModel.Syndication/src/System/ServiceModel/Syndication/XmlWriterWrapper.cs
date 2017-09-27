using System;
using System.Threading.Tasks;
using System.Xml;

namespace System.ServiceModel.Syndication
{
    internal class XmlWriterWrapper : XmlWriter
    {

        private XmlWriter writer;

        private Func<XmlWriterWrapper, string, Task> writeStringFunc;
        private Func<XmlWriterWrapper, string, string, Task> writeStartElementFunc2;
        private Func<XmlWriterWrapper, Task> writeEndElementFunc;
        private Func<XmlWriterWrapper, string, string, Task> writeAttributeStringFunc2;
        private Func<XmlWriterWrapper, string, string, string, Task> writeAttributeStringFunc3;
        private Func<XmlWriterWrapper, string, string, string, string, Task> writeAttributeStringFunc4;
        private Func<XmlWriterWrapper, XmlReader, bool, Task> WriteNodeFunc;


        private void InitAsync()
        {
            this.writeStringFunc = new Func<XmlWriterWrapper, string, Task>((thisPtr, text) => { return thisPtr.writer.WriteStringAsync(text); });
            this.writeStartElementFunc2 = new Func<XmlWriterWrapper, string, string, Task>((thisPtr, localName, ns) => { return this.writer.WriteStartElementAsync("", localName, ns); });
            this.writeEndElementFunc = new Func<XmlWriterWrapper, Task>((thisPtr) => { return thisPtr.writer.WriteEndElementAsync(); });
            this.writeAttributeStringFunc2 = new Func<XmlWriterWrapper, string, string, Task>((thisPtr, localname, value) => { return thisPtr.writer.WriteAttributeStringAsync("", localname, "", value); });
            this.writeAttributeStringFunc3 = new Func<XmlWriterWrapper, string, string, string, Task>((thisPtr, localName, ns, value) => { return thisPtr.writer.WriteAttributeStringAsync("", localName, ns, value); });
            this.writeAttributeStringFunc4 = new Func<XmlWriterWrapper, string, string, string, string, Task>((thisPtr, prefix, localName, ns, value) => { return thisPtr.writer.WriteAttributeStringAsync(prefix, localName, ns, value); });
            this.WriteNodeFunc = new Func<XmlWriterWrapper, XmlReader, bool, Task>((thisPtr, reader, defattr) => { return thisPtr.writer.WriteNodeAsync(reader, defattr); });
        }

        private void Init()
        {
            this.writeStringFunc = new Func<XmlWriterWrapper, string, Task>((thisPtr, text) => { thisPtr.writer.WriteString(text); return Task.CompletedTask; });
            this.writeStartElementFunc2 = new Func<XmlWriterWrapper, string, string, Task>((thisPtr, localName, ns) => { this.writer.WriteStartElement(localName, ns); return Task.CompletedTask; });
            this.writeEndElementFunc = new Func<XmlWriterWrapper, Task>((thisPtr) => { thisPtr.writer.WriteEndElement(); return Task.CompletedTask; });
            this.writeAttributeStringFunc2 = new Func<XmlWriterWrapper, string, string, Task>((thisPtr, localname, value) => { thisPtr.writer.WriteAttributeString("", localname, "", value); return Task.CompletedTask; });
            this.writeAttributeStringFunc3 = new Func<XmlWriterWrapper, string, string, string, Task>((thisPtr, localName, ns, value) => { thisPtr.writer.WriteAttributeString(localName, ns, value); return Task.CompletedTask; });
            this.writeAttributeStringFunc4 = new Func<XmlWriterWrapper, string, string, string, string, Task>((thisPtr, prefix, localName, ns, value) => { thisPtr.writer.WriteAttributeString(prefix, localName, ns, value); return Task.CompletedTask; });
            this.WriteNodeFunc = new Func<XmlWriterWrapper, XmlReader, bool, Task>((thisPtr, reader, defattr) => { thisPtr.writer.WriteNode(reader, defattr); return Task.CompletedTask; });
        }

        public static XmlWriter CreateFromWriter(XmlWriter writer)
        {
            if (writer is XmlWriterWrapper || writer.Settings.Async)
            {
                return writer;
            }

            return new XmlWriterWrapper(writer);
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
            return WriteNodeFunc(this, reader, defattr);
        }

        public override Task WriteStringAsync(string text)
        {
            return writeStringFunc(this, text);
        }

        public override Task WriteStartElementAsync(string prefix, string localName, string ns)
        {
            writer.WriteStartElement(prefix, localName, ns);
            return Task.CompletedTask;
        }

        public override Task WriteEndElementAsync()
        {
            return writeEndElementFunc(this);
        }

        public override Task WriteAttributesAsync(XmlReader reader, bool defattr)
        {
            writer.WriteAttributes(reader, defattr);
            return Task.CompletedTask;
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
            this.writer.WriteChars(buffer, index, count);
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
            this.writer.WriteRaw(buffer, index, count);
        }

        public override void WriteRaw(string data)
        {
            this.writer.WriteRaw(data);
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            this.writer.WriteStartAttribute(prefix, localName, ns);
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
            this.writer.WriteStartElement(prefix, localName, ns);
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

    internal static class XmlWriterExtensions
    {
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
            return writer.InternalWriteAttributeStringAsync(null, localName, ns, value);
        }

        public static Task WriteAttributeStringAsync(this XmlWriter writer, string localName, string value)
        {
            return writer.InternalWriteAttributeStringAsync(null, localName, null, value);
        }

        public static Task InternalWriteAttributeStringAsync(this XmlWriter writer, string prefix, string localName, string ns, string value)
        {
            if (writer is XmlWriterWrapper)
            {
                writer.WriteAttributeString(prefix, localName, ns, value);
                return Task.CompletedTask;
            }

            return writer.WriteAttributeStringAsync(prefix, localName, ns, value);
        }
    }
}
