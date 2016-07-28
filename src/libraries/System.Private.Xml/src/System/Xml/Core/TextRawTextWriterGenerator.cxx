//------------------------------------------------------------------------------
// <copyright file="TextWriterGenerator.cxx" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">helenak</owner>
//------------------------------------------------------------------------------

// WARNING: This file is generated and should not be modified directly.  Instead,
// modify TextWriterGenerator.cxx and run gen.bat in the same directory.
// This batch file will execute the following commands:
//
//   cl.exe /C /EP /D _UTF8_TEXT_WRITER TextWriterGenerator.cxx > Utf8TextWriter.cs
//   cl.exe /C /EP /D _ENCODED_TEXT_WRITER TextWriterGenerator.cxx > EncodedTextWriter.cs
//
// Because these two implementations of TextWriter are so similar, the C++ preprocessor
// is used to generate each implementation from one template file, using macros and ifdefs.

#ifdef _UTF8_TEXT_WRITER
#define _CLASS_NAME TextUtf8RawTextWriter
#define _BASE_CLASS_NAME XmlUtf8RawTextWriter
#endif

#ifdef _ENCODED_TEXT_WRITER
#define _CLASS_NAME TextEncodedRawTextWriter
#define _BASE_CLASS_NAME XmlEncodedRawTextWriter
#endif

using System;
using System.IO;
using System.Text;
//using System.Xml.Query;
using System.Xml.Schema;
using System.Diagnostics;
using System.Globalization;

namespace System.Xml {
    // Concrete implementation of XmlRawWriter interface that serializes text events as encoded
    // text.  All other non-text events are ignored.  The general-purpose TextEncodedRawTextWriter uses the
    // Encoder class to output to any encoding.  The TextUtf8RawTextWriter class combined the encoding
    // operation with serialization in order to achieve better performance.
    // </summary>
    internal class _CLASS_NAME : _BASE_CLASS_NAME {

#ifdef _ENCODED_TEXT_WRITER
        // Construct an instance of this class that outputs text to the TextWriter interface.
        public _CLASS_NAME( TextWriter writer, XmlWriterSettings settings ) : base ( writer, settings ) {
        }
#endif

        // Construct an instance of this class that serializes to a Stream interface.
        public _CLASS_NAME( Stream stream, XmlWriterSettings settings ) : base( stream, settings ) {
        }


//
// XmlRawWriter
//
        // Ignore Xml declaration
        internal override void WriteXmlDeclaration( XmlStandalone standalone ) {
        }
        internal override void WriteXmlDeclaration( string xmldecl ) {
        }

        // Ignore DTD
        public override void WriteDocType( string name, string pubid, string sysid, string subset ) {
        }

        // Ignore Elements
        public override void WriteStartElement( string prefix, string localName, string ns ) {
        }

        internal override void WriteEndElement( string prefix, string localName, string ns ) {
        }

        internal override void WriteFullEndElement( string prefix, string localName, string ns ) {
        }

        internal override void StartElementContent() {
        }

        // Ignore attributes
        public override void WriteStartAttribute( string prefix, string localName, string ns ) {
            base.inAttributeValue = true;
        }

        public override void WriteEndAttribute() {
            base.inAttributeValue = false;
        }

        // Ignore namespace declarations
        internal override void WriteNamespaceDeclaration( string prefix, string ns ) {
        }

        internal override bool SupportsNamespaceDeclarationInChunks {
            get {
                return false;
            }
        }

        // Output content of CDATA sections as plain text without escaping
        public override void WriteCData( string text ) {
            base.WriteRaw( text );
        }

        // Ignore comments
        public override void WriteComment( string text ) {
        }

        // Ignore processing instructions
        public override void WriteProcessingInstruction( string name, string text ) {
        }

        // Ignore entities
        public override void WriteEntityRef( string name ) {
        }
        public override void WriteCharEntity( char ch ) {
        }
        public override void WriteSurrogateCharEntity( char lowChar, char highChar ) {
        }

        // Output text content without any escaping; ignore attribute values
        public override void WriteWhitespace( string ws ) {
            if ( !base.inAttributeValue ) {
                base.WriteRaw( ws  );
            }
        }

        // Output text content without any escaping; ignore attribute values
        public override void WriteString( string textBlock ) {
            if ( !base.inAttributeValue ) {
                base.WriteRaw( textBlock );
            }
        }

        // Output text content without any escaping; ignore attribute values
        public override void WriteChars( char[] buffer, int index, int count ) {
            if ( !base.inAttributeValue ) {
                base.WriteRaw( buffer, index, count );
            }
        }

        // Output text content without any escaping; ignore attribute values
        public override void WriteRaw( char[] buffer, int index, int count ) {
            if ( !base.inAttributeValue ) {
                base.WriteRaw( buffer, index, count );
            }
        }

        // Output text content without any escaping; ignore attribute values
        public override void WriteRaw( string data ) {
            if ( !base.inAttributeValue ) {
                base.WriteRaw( data );
            }
        }
    }
}
