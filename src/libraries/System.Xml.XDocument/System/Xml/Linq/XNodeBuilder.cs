// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using CultureInfo = System.Globalization.CultureInfo;
using Debug = System.Diagnostics.Debug;
using IEnumerable = System.Collections.IEnumerable;
using SuppressMessageAttribute = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;
using Enumerable = System.Linq.Enumerable;
using IComparer = System.Collections.IComparer;
using IEqualityComparer = System.Collections.IEqualityComparer;
using StringBuilder = System.Text.StringBuilder;
using Encoding = System.Text.Encoding;
using Interlocked = System.Threading.Interlocked;
using System.Reflection;

namespace System.Xml.Linq
{
    internal class XNodeBuilder : XmlWriter
    {
        List<object> content;
        XContainer parent;
        XName attrName;
        string attrValue;
        XContainer root;

        public XNodeBuilder(XContainer container)
        {
            root = container;
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

        private void Close()
        {
            root.Add(content);
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

        public override void WriteCData(string text)
        {
            AddNode(new XCData(text));
        }

        public override void WriteCharEntity(char ch)
        {
            AddString(new string(ch, 1));
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            AddString(new string(buffer, index, count));
        }

        public override void WriteComment(string text)
        {
            AddNode(new XComment(text));
        }

        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            AddNode(new XDocumentType(name, pubid, sysid, subset));
        }

        public override void WriteEndAttribute()
        {
            XAttribute a = new XAttribute(attrName, attrValue);
            attrName = null;
            attrValue = null;
            if (parent != null)
            {
                parent.Add(a);
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
            parent = ((XElement)parent).parent;
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
            XElement e = (XElement)parent;
            if (e.IsEmpty)
            {
                e.Add(string.Empty);
            }
            parent = e.parent;
        }

        public override void WriteProcessingInstruction(string name, string text)
        {
            if (name == "xml")
            {
                return;
            }
            AddNode(new XProcessingInstruction(name, text));
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            AddString(new string(buffer, index, count));
        }

        public override void WriteRaw(string data)
        {
            AddString(data);
        }

        public override void WriteStartAttribute(string prefix, string localName, string namespaceName)
        {
            if (prefix == null) throw new ArgumentNullException("prefix");
            attrName = XNamespace.Get(prefix.Length == 0 ? string.Empty : namespaceName).GetName(localName);
            attrValue = string.Empty;
        }

        public override void WriteStartDocument()
        {
        }

        public override void WriteStartDocument(bool standalone)
        {
        }

        public override void WriteStartElement(string prefix, string localName, string namespaceName)
        {
            AddNode(new XElement(XNamespace.Get(namespaceName).GetName(localName)));
        }

        public override void WriteString(string text)
        {
            AddString(text);
        }

        public override void WriteSurrogateCharEntity(char lowCh, char highCh)
        {
            AddString(new string(new char[] { highCh, lowCh }));
        }

        public override void WriteValue(DateTimeOffset value)
        {
            // For compatibility with custom writers, XmlWriter writes DateTimeOffset as DateTime. 
            // Our internal writers should use the DateTimeOffset-String conversion from XmlConvert.
            WriteString(XmlConvert.ToString(value));
        }

        public override void WriteWhitespace(string ws)
        {
            AddString(ws);
        }

        void Add(object o)
        {
            if (content == null)
            {
                content = new List<object>();
            }
            content.Add(o);
        }

        void AddNode(XNode n)
        {
            if (parent != null)
            {
                parent.Add(n);
            }
            else
            {
                Add(n);
            }
            XContainer c = n as XContainer;
            if (c != null)
            {
                parent = c;
            }
        }

        void AddString(string s)
        {
            if (s == null)
            {
                return;
            }
            if (attrValue != null)
            {
                attrValue += s;
            }
            else if (parent != null)
            {
                parent.Add(s);
            }
            else
            {
                Add(s);
            }
        }
    }
}