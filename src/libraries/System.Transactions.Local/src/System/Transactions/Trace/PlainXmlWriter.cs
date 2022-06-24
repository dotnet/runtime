// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

#nullable disable

namespace System.Transactions.Diagnostics
{
    /// <summary>
    /// Writes out plain xml as fast as possible
    /// </summary>
    internal class PlainXmlWriter : XmlWriter
    {
        TraceXPathNavigator navigator;
        Stack<string> stack;
        bool writingAttribute  = false;
        string currentAttributeName;
        string currentAttributePrefix;
        string currentAttributeNs;
        bool format;

        public PlainXmlWriter(bool format)
        {
            this.navigator = new TraceXPathNavigator();
            this.stack = new Stack<string>();
            this.format = format;
        }

        public PlainXmlWriter() : this (false)
        {
        }

        public XPathNavigator ToNavigator()
        {
            return this.navigator;
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

        public override string LookupPrefix( string ns )
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

        public override void WriteNmToken( string name )
        {
            throw new NotSupportedException();
        }

        public override void WriteName( string name )
        {
            throw new NotSupportedException();
        }

        public override void WriteQualifiedName( string localName, string ns )
        {
            throw new NotSupportedException();
        }

        public override void WriteValue( object value )
        {
            this.navigator.AddText(value.ToString());
        }

        public override void WriteValue( string value )
        {
            this.navigator.AddText(value);
        }

        public override void WriteBase64(byte[] buffer, int offset, int count) { }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            Debug.Assert( localName != null && localName.Length > 0 );

            this.navigator.AddElement(prefix, localName, ns);
        }

        public override void WriteFullEndElement()
        {
            WriteEndElement();
        }

        public override void WriteEndElement()
        {
            this.navigator.CloseElement();
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            Debug.Assert(!this.writingAttribute);
            this.currentAttributeName = localName;
            this.currentAttributePrefix = prefix;
            this.currentAttributeNs = ns;

            this.writingAttribute = true;
        }

        public override void WriteEndAttribute()
        {
            Debug.Assert(this.writingAttribute);
            this.writingAttribute = false;
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
            if (this.writingAttribute)
            {
                this.navigator.AddAttribute(this.currentAttributeName, text, this.currentAttributeNs, this.currentAttributePrefix);
            }
            else
            {
                this.WriteValue(text);
            }
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            throw new NotSupportedException();
        }

        public override void WriteRaw(string data)
        {
            //assumed preformatted with a newline at the end
            throw new NotSupportedException();
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            throw new NotSupportedException();
        }


        public override void WriteBinHex(byte[] buffer, int index, int count)
        {
            throw new NotSupportedException();
        }

        public override void Close()
        {
        }

        public override void Flush()
        {
        }

    }
}
