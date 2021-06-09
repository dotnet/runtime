// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// WARNING: This file is generated and should not be modified directly.
// Instead, modify TextRawTextWriterGenerator.ttinclude

#nullable disable
using System;
using System.IO;
using System.Text;
using System.Xml.Schema;
using System.Diagnostics;
using System.Globalization;

namespace System.Xml
{
    // Concrete implementation of XmlRawWriter interface that serializes text events as encoded
    // text.  All other non-text events are ignored.  The general-purpose TextEncodedRawTextWriter uses the
    // Encoder class to output to any encoding.  The TextUtf8RawTextWriter class combined the encoding
    // operation with serialization in order to achieve better performance.
    // </summary>
    internal sealed class TextEncodedRawTextWriter : XmlEncodedRawTextWriter
    {
        // Construct an instance of this class that outputs text to the TextWriter interface.
        public TextEncodedRawTextWriter(TextWriter writer, XmlWriterSettings settings) : base(writer, settings)
        {
        }

        // Construct an instance of this class that serializes to a Stream interface.
        public TextEncodedRawTextWriter(Stream stream, XmlWriterSettings settings) : base(stream, settings)
        {
        }

        //
        // XmlRawWriter
        //
        // Ignore Xml declaration
        internal override void WriteXmlDeclaration(XmlStandalone standalone)
        {
        }
        internal override void WriteXmlDeclaration(string xmldecl)
        {
        }

        // Ignore DTD
        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
        }

        // Ignore Elements
        public override void WriteStartElement(string prefix, string localName, string ns)
        {
        }

        internal override void WriteEndElement(string prefix, string localName, string ns)
        {
        }

        internal override void WriteFullEndElement(string prefix, string localName, string ns)
        {
        }

        internal override void StartElementContent()
        {
        }

        // Ignore attributes
        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            base._inAttributeValue = true;
        }

        public override void WriteEndAttribute()
        {
            base._inAttributeValue = false;
        }

        // Ignore namespace declarations
        internal override void WriteNamespaceDeclaration(string prefix, string ns)
        {
        }

        internal override bool SupportsNamespaceDeclarationInChunks
        {
            get
            {
                return false;
            }
        }

        // Output content of CDATA sections as plain text without escaping
        public override void WriteCData(string text)
        {
            base.WriteRaw(text);
        }

        // Ignore comments
        public override void WriteComment(string text)
        {
        }

        // Ignore processing instructions
        public override void WriteProcessingInstruction(string name, string text)
        {
        }

        // Ignore entities
        public override void WriteEntityRef(string name)
        {
        }
        public override void WriteCharEntity(char ch)
        {
        }
        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
        }

        // Output text content without any escaping; ignore attribute values
        public override void WriteWhitespace(string ws)
        {
            if (!base._inAttributeValue)
            {
                base.WriteRaw(ws);
            }
        }

        // Output text content without any escaping; ignore attribute values
        public override void WriteString(string textBlock)
        {
            if (!base._inAttributeValue)
            {
                base.WriteRaw(textBlock);
            }
        }

        // Output text content without any escaping; ignore attribute values
        public override void WriteChars(char[] buffer, int index, int count)
        {
            if (!base._inAttributeValue)
            {
                base.WriteRaw(buffer, index, count);
            }
        }

        // Output text content without any escaping; ignore attribute values
        public override void WriteRaw(char[] buffer, int index, int count)
        {
            if (!base._inAttributeValue)
            {
                base.WriteRaw(buffer, index, count);
            }
        }

        // Output text content without any escaping; ignore attribute values
        public override void WriteRaw(string data)
        {
            if (!base._inAttributeValue)
            {
                base.WriteRaw(data);
            }
        }
    }
}
