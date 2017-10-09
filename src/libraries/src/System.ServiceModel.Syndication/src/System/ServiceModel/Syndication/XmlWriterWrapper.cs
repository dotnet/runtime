// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using System.Xml;

namespace System.ServiceModel.Syndication
{
    internal class XmlWriterWrapper : XmlWriter
    {
        private XmlWriter _writer;

        private Func<XmlWriterWrapper, string, Task> _writeStringFunc;
        private Func<XmlWriterWrapper, string, string, Task> _writeStartElementFunc2;
        private Func<XmlWriterWrapper, Task> _writeEndElementFunc;
        private Func<XmlWriterWrapper, string, string, Task> _writeAttributeStringFunc2;
        private Func<XmlWriterWrapper, string, string, string, Task> _writeAttributeStringFunc3;
        private Func<XmlWriterWrapper, string, string, string, string, Task> _writeAttributeStringFunc4;
        private Func<XmlWriterWrapper, XmlReader, bool, Task> _writeNodeFunc;


        private void InitAsync()
        {
            _writeStringFunc = new Func<XmlWriterWrapper, string, Task>((thisPtr, text) => { return thisPtr._writer.WriteStringAsync(text); });
            _writeStartElementFunc2 = new Func<XmlWriterWrapper, string, string, Task>((thisPtr, localName, ns) => { return _writer.WriteStartElementAsync("", localName, ns); });
            _writeEndElementFunc = new Func<XmlWriterWrapper, Task>((thisPtr) => { return thisPtr._writer.WriteEndElementAsync(); });
            _writeAttributeStringFunc2 = new Func<XmlWriterWrapper, string, string, Task>((thisPtr, localname, value) => { return thisPtr._writer.WriteAttributeStringAsync("", localname, "", value); });
            _writeAttributeStringFunc3 = new Func<XmlWriterWrapper, string, string, string, Task>((thisPtr, localName, ns, value) => { return thisPtr._writer.WriteAttributeStringAsync("", localName, ns, value); });
            _writeAttributeStringFunc4 = new Func<XmlWriterWrapper, string, string, string, string, Task>((thisPtr, prefix, localName, ns, value) => { return thisPtr._writer.WriteAttributeStringAsync(prefix, localName, ns, value); });
            _writeNodeFunc = new Func<XmlWriterWrapper, XmlReader, bool, Task>((thisPtr, reader, defattr) => { return thisPtr._writer.WriteNodeAsync(reader, defattr); });
        }

        private void Init()
        {
            _writeStringFunc = new Func<XmlWriterWrapper, string, Task>((thisPtr, text) => { thisPtr._writer.WriteString(text); return Task.CompletedTask; });
            _writeStartElementFunc2 = new Func<XmlWriterWrapper, string, string, Task>((thisPtr, localName, ns) => { _writer.WriteStartElement(localName, ns); return Task.CompletedTask; });
            _writeEndElementFunc = new Func<XmlWriterWrapper, Task>((thisPtr) => { thisPtr._writer.WriteEndElement(); return Task.CompletedTask; });
            _writeAttributeStringFunc2 = new Func<XmlWriterWrapper, string, string, Task>((thisPtr, localname, value) => { thisPtr._writer.WriteAttributeString("", localname, "", value); return Task.CompletedTask; });
            _writeAttributeStringFunc3 = new Func<XmlWriterWrapper, string, string, string, Task>((thisPtr, localName, ns, value) => { thisPtr._writer.WriteAttributeString(localName, ns, value); return Task.CompletedTask; });
            _writeAttributeStringFunc4 = new Func<XmlWriterWrapper, string, string, string, string, Task>((thisPtr, prefix, localName, ns, value) => { thisPtr._writer.WriteAttributeString(prefix, localName, ns, value); return Task.CompletedTask; });
            _writeNodeFunc = new Func<XmlWriterWrapper, XmlReader, bool, Task>((thisPtr, reader, defattr) => { thisPtr._writer.WriteNode(reader, defattr); return Task.CompletedTask; });
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

            _writer = writer;

            if (_writer.Settings.Async)
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
            return _writeNodeFunc(this, reader, defattr);
        }

        public override Task WriteStringAsync(string text)
        {
            return _writeStringFunc(this, text);
        }

        public override Task WriteStartElementAsync(string prefix, string localName, string ns)
        {
            _writer.WriteStartElement(prefix, localName, ns);
            return Task.CompletedTask;
        }

        public override Task WriteEndElementAsync()
        {
            return _writeEndElementFunc(this);
        }

        public override Task WriteAttributesAsync(XmlReader reader, bool defattr)
        {
            _writer.WriteAttributes(reader, defattr);
            return Task.CompletedTask;
        }

        // inherited methods
        public override WriteState WriteState
        {
            get
            {
                return _writer.WriteState;
            }
        }

        public override void Flush()
        {
            _writer.Flush();
        }

        public override string LookupPrefix(string ns)
        {
            return _writer.LookupPrefix(ns);
        }

        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            _writer.WriteBase64(buffer, index, count);
        }

        public override void WriteCData(string text)
        {
            _writer.WriteCData(text);
        }

        public override void WriteCharEntity(char ch)
        {
            _writer.WriteCharEntity(ch);
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            _writer.WriteChars(buffer, index, count);
        }

        public override void WriteComment(string text)
        {
            _writer.WriteComment(text);
        }

        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            _writer.WriteDocType(name, pubid, sysid, subset);
        }

        public override void WriteEndAttribute()
        {
            _writer.WriteEndAttribute();
        }

        public override void WriteEndDocument()
        {
            _writer.WriteEndDocument();
        }

        public override void WriteEndElement()
        {
            _writer.WriteEndElement();
        }

        public override void WriteEntityRef(string name)
        {
            _writer.WriteEntityRef(name);
        }

        public override void WriteFullEndElement()
        {
            _writer.WriteFullEndElement();
        }

        public override void WriteProcessingInstruction(string name, string text)
        {
            _writer.WriteProcessingInstruction(name, text);
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            _writer.WriteRaw(buffer, index, count);
        }

        public override void WriteRaw(string data)
        {
            _writer.WriteRaw(data);
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            _writer.WriteStartAttribute(prefix, localName, ns);
        }

        public override void WriteStartDocument()
        {
            _writer.WriteStartDocument();
        }

        public override void WriteStartDocument(bool standalone)
        {
            _writer.WriteStartDocument(standalone);
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            _writer.WriteStartElement(prefix, localName, ns);
        }

        public override void WriteString(string text)
        {
            _writer.WriteString(text);
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            _writer.WriteSurrogateCharEntity(lowChar, highChar);
        }

        public override void WriteWhitespace(string ws)
        {
            _writer.WriteWhitespace(ws);
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
