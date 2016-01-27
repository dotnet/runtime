// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;

namespace XmlCoreTest.Common
{
    public class CustomWriter : XmlWriter
    {
        private XmlWriter _writer = null;

        public CustomWriter(Stream strm, XmlWriterSettings xws)
        {
            _writer = XmlWriter.Create(strm, xws);
        }

        public CustomWriter(String filename, XmlWriterSettings xws)
        {
            _writer = XmlWriter.Create(FilePathUtil.getStream(filename), xws);
        }

        public CustomWriter(StringWriter sw, XmlWriterSettings xws)
        {
            _writer = XmlWriter.Create(sw, xws);
        }

        public override void WriteStartDocument()
        {
            _writer.WriteStartDocument();
        }

        public override void WriteStartDocument(bool standalone)
        {
            _writer.WriteStartDocument(standalone);
        }

        public override void WriteEndDocument()
        {
            _writer.WriteEndDocument();
        }

        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            _writer.WriteDocType(name, pubid, sysid, subset);
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            _writer.WriteStartElement(prefix, localName, ns);
        }

        public override void WriteEndElement()
        {
            _writer.WriteEndElement();
        }

        public override void WriteFullEndElement()
        {
            _writer.WriteFullEndElement();
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            _writer.WriteStartAttribute(prefix, localName, ns);
        }

        public override void WriteEndAttribute()
        {
            _writer.WriteEndAttribute();
        }

        public override void WriteCData(string text)
        {
            _writer.WriteCData(text);
        }

        public override void WriteComment(string text)
        {
            _writer.WriteComment(text);
        }

        public override void WriteProcessingInstruction(string name, string text)
        {
            _writer.WriteProcessingInstruction(name, text);
        }

        public override void WriteEntityRef(string name)
        {
            _writer.WriteEntityRef(name);
        }

        public override void WriteCharEntity(char ch)
        {
            _writer.WriteCharEntity(ch);
        }

        public override void WriteWhitespace(string ws)
        {
            _writer.WriteWhitespace(ws);
        }

        public override void WriteString(string text)
        {
            _writer.WriteString(text);
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            _writer.WriteSurrogateCharEntity(lowChar, highChar);
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            _writer.WriteChars(buffer, index, count);
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            _writer.WriteRaw(buffer, index, count);
        }

        public override void WriteRaw(string data)
        {
            _writer.WriteRaw(data);
        }

        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            _writer.WriteBase64(buffer, index, count);
        }

        public override WriteState WriteState
        {
            get
            {
                return _writer.WriteState;
            }
        }

        // XmlSpace and XmlLang are virtual, so custom writer does not need to override them.
        // But the virtual impln returns Default and empty string respectively, which breaks many writer API tests, hence they are overridden here.
        public override XmlSpace XmlSpace
        {
            get
            {
                return _writer.XmlSpace;
            }
        }

        public override string XmlLang
        {
            get
            {
                return _writer.XmlLang;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _writer.Dispose();
            }
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            _writer.Flush();
        }

        public override string LookupPrefix(string ns)
        {
            return _writer.LookupPrefix(ns);
        }
    }
}

