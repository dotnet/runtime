// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Xml.Linq
{
    internal sealed class XNodeBuilder : XmlWriter
    {
        private List<object>? _content;
        private XContainer? _parent;
        private XName? _attrName;
        private string? _attrValue;
        private readonly XContainer _root;

        public XNodeBuilder(XContainer container)
        {
            _root = container;
        }

        public override XmlWriterSettings Settings
        {
            get
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.ConformanceLevel = ConformanceLevel.Auto;
                return settings;
            }
        }

        public override WriteState WriteState
        {
            get { throw new NotSupportedException(); } // nop
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }
        }

        public override void Close()
        {
            _root.Add(_content);
        }

        public override void Flush()
        {
        }

        public override string LookupPrefix(string namespaceName)
        {
            throw new NotSupportedException(); // nop
        }

        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            throw new NotSupportedException(SR.NotSupported_WriteBase64);
        }

        public override void WriteCData(string? text)
        {
            AddNode(new XCData(text!));
        }

        public override void WriteCharEntity(char ch)
        {
            AddString(char.ToString(ch));
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            AddString(new string(buffer, index, count));
        }

        public override void WriteComment(string? text)
        {
            AddNode(new XComment(text!));
        }

        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
        {
            AddNode(new XDocumentType(name, pubid, sysid, subset));
        }

        public override void WriteEndAttribute()
        {
            XAttribute a = new XAttribute(_attrName!, _attrValue!);
            _attrName = null;
            _attrValue = null;
            if (_parent != null)
            {
                _parent.Add(a);
            }
            else
            {
                Add(a);
            }
        }

        public override void WriteEndDocument()
        {
        }

        public override void WriteEndElement()
        {
            _parent = ((XElement)_parent!).parent;
        }

        public override void WriteEntityRef(string name)
        {
            switch (name)
            {
                case "amp":
                    AddString("&");
                    break;
                case "apos":
                    AddString("'");
                    break;
                case "gt":
                    AddString(">");
                    break;
                case "lt":
                    AddString("<");
                    break;
                case "quot":
                    AddString("\"");
                    break;
                default:
                    throw new NotSupportedException(SR.NotSupported_WriteEntityRef);
            }
        }

        public override void WriteFullEndElement()
        {
            XElement e = (XElement)_parent!;
            if (e.IsEmpty)
            {
                e.Add(string.Empty);
            }
            _parent = e.parent;
        }

        public override void WriteProcessingInstruction(string name, string? text)
        {
            if (name == "xml")
            {
                return;
            }
            AddNode(new XProcessingInstruction(name, text!));
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            AddString(new string(buffer, index, count));
        }

        public override void WriteRaw(string data)
        {
            AddString(data);
        }

        public override void WriteStartAttribute(string? prefix, string localName, string? namespaceName)
        {
            ArgumentNullException.ThrowIfNull(prefix);
            _attrName = XNamespace.Get(prefix.Length == 0 ? string.Empty : namespaceName!).GetName(localName);
            _attrValue = string.Empty;
        }

        public override void WriteStartDocument()
        {
        }

        public override void WriteStartDocument(bool standalone)
        {
        }

        public override void WriteStartElement(string? prefix, string localName, string? namespaceName)
        {
            AddNode(new XElement(XNamespace.Get(namespaceName!).GetName(localName)));
        }

        public override void WriteString(string? text)
        {
            AddString(text);
        }

        public override void WriteSurrogateCharEntity(char lowCh, char highCh)
        {
            AddString(new string([highCh, lowCh]));
        }

        public override void WriteValue(DateTimeOffset value)
        {
            // For compatibility with custom writers, XmlWriter writes DateTimeOffset as DateTime.
            // Our internal writers should use the DateTimeOffset-String conversion from XmlConvert.
            WriteString(XmlConvert.ToString(value));
        }

        public override void WriteWhitespace(string? ws)
        {
            AddString(ws);
        }

        private void Add(object o)
        {
            _content ??= new List<object>();
            _content.Add(o);
        }

        private void AddNode(XNode n)
        {
            if (_parent != null)
            {
                _parent.Add(n);
            }
            else
            {
                Add(n);
            }
            XContainer? c = n as XContainer;
            if (c != null)
            {
                _parent = c;
            }
        }

        private void AddString(string? s)
        {
            if (s == null)
            {
                return;
            }
            if (_attrValue != null)
            {
                _attrValue += s;
            }
            else if (_parent != null)
            {
                _parent.Add(s);
            }
            else
            {
                Add(s);
            }
        }
    }
}
