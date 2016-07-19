// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;

namespace System.Transactions.Diagnostics
{
    /// <summary>
    /// Very basic performance-oriented XmlWriter implementation. No validation/encoding is made.
    /// Namespaces are not supported
    /// Minimal formatting support
    /// </summary>
    internal class PlainXmlWriter : XmlWriter
    {
        private TraceXPathNavigator _navigator;
        private Stack<string> _stack;
        private bool _writingAttribute = false;
        private string _currentAttributeName;
        private string _currentAttributePrefix;
        private string _currentAttributeNs;
        private bool _format;

        public PlainXmlWriter(bool format)
        {
            _navigator = new TraceXPathNavigator();
            _stack = new Stack<string>();
            _format = format;
        }

        public PlainXmlWriter() : this(false)
        {
        }

        public XPathNavigator ToNavigator()
        {
            return _navigator;
        }

        public override void WriteStartDocument() { }
        public override void WriteDocType(string name, string pubid, string sysid, string subset) { }

        public override void WriteStartDocument(bool standalone)
        {
            throw new NotSupportedException();
        }

        public override void WriteEndDocument()
        {
            throw new NotSupportedException();
        }

        public override string LookupPrefix(string ns)
        {
            throw new NotSupportedException();
        }

        public override WriteState WriteState
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override XmlSpace XmlSpace
        {
            get { throw new NotSupportedException(); }
        }

        public override string XmlLang
        {
            get { throw new NotSupportedException(); }
        }

        public override void WriteNmToken(string name)
        {
            throw new NotSupportedException();
        }

        public override void WriteName(string name)
        {
            throw new NotSupportedException();
        }

        public override void WriteQualifiedName(string localName, string ns)
        {
            throw new NotSupportedException();
        }

        public override void WriteValue(object value)
        {
            _navigator.AddText(value.ToString());
        }

        public override void WriteValue(string value)
        {
            _navigator.AddText(value);
        }

        public override void WriteBase64(byte[] buffer, int offset, int count) { }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            Debug.Assert(localName != null && localName.Length > 0);

            _navigator.AddElement(prefix, localName, ns);
        }

        public override void WriteFullEndElement()
        {
            WriteEndElement();
        }

        public override void WriteEndElement()
        {
            _navigator.CloseElement();
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            Debug.Assert(!_writingAttribute);
            _currentAttributeName = localName;
            _currentAttributePrefix = prefix;
            _currentAttributeNs = ns;

            _writingAttribute = true;
        }

        public override void WriteEndAttribute()
        {
            Debug.Assert(_writingAttribute);
            _writingAttribute = false;
        }

        public override void WriteCData(string text)
        {
            throw new NotSupportedException();
        }

        public override void WriteComment(string text)
        {
            throw new NotSupportedException();
        }

        public override void WriteProcessingInstruction(string name, string text)
        {
            throw new NotSupportedException();
        }

        public override void WriteEntityRef(string name)
        {
            throw new NotSupportedException();
        }

        public override void WriteCharEntity(char ch)
        {
            throw new NotSupportedException();
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            throw new NotSupportedException();
        }

        public override void WriteWhitespace(string ws)
        {
            throw new NotSupportedException();
        }

        public override void WriteString(string text)
        {
            if (_writingAttribute)
            {
                _navigator.AddAttribute(_currentAttributeName, text, _currentAttributeNs, _currentAttributePrefix);
            }
            else
            {
                WriteValue(text);
            }
        }

        public override void WriteChars(Char[] buffer, int index, int count)
        {
            throw new NotSupportedException();
        }

        public override void WriteRaw(string data)
        {
            //assumed preformatted with a newline at the end
            throw new NotSupportedException();
        }

        public override void WriteRaw(Char[] buffer, int index, int count)
        {
            throw new NotSupportedException();
        }


        public override void WriteBinHex(byte[] buffer, int index, int count)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
        }
    }
}
