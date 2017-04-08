// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Diagnostics;
using System.Xml.XPath;
using System.Xml.Schema;
using System.Collections;

namespace System.Xml
{
    /// <summary>
    /// Implementations of XmlRawWriter are intended to be wrapped by the XmlWellFormedWriter.  The
    /// well-formed writer performs many checks in behalf of the raw writer, and keeps state that the
    /// raw writer otherwise would have to keep.  Therefore, the well-formed writer will call the
    /// XmlRawWriter using the following rules, in order to make raw writers easier to implement:
    ///
    ///  1. The well-formed writer keeps a stack of element names, and always calls
    ///     WriteEndElement(string, string, string) instead of WriteEndElement().
    ///  2. The well-formed writer tracks namespaces, and will pass himself in via the
    ///     WellformedWriter property. It is used in the XmlRawWriter's implementation of IXmlNamespaceResolver.
    ///     Thus, LookupPrefix does not have to be implemented.
    ///  3. The well-formed writer tracks write states, so the raw writer doesn't need to.
    ///  4. The well-formed writer will always call StartElementContent.
    ///  5. The well-formed writer will always call WriteNamespaceDeclaration for namespace nodes,
    ///     rather than calling WriteStartAttribute(). If the writer is supporting namespace declarations in chunks
    ///     (SupportsNamespaceDeclarationInChunks is true), the XmlWellFormedWriter will call WriteStartNamespaceDeclaration,
    ///      then any method that can be used to write out a value of an attribute (WriteString, WriteChars, WriteRaw, WriteCharEntity...) 
    ///      and then WriteEndNamespaceDeclaration - instead of just a single WriteNamespaceDeclaration call. This feature will be 
    ///      supported by raw writers serializing to text that wish to preserve the attribute value escaping etc.
    ///  6. The well-formed writer guarantees a well-formed document, including correct call sequences,
    ///     correct namespaces, and correct document rule enforcement.
    ///  7. All element and attribute names will be fully resolved and validated.  Null will never be
    ///     passed for any of the name parts.
    ///  8. The well-formed writer keeps track of xml:space and xml:lang.
    ///  9. The well-formed writer verifies NmToken, Name, and QName values and calls WriteString().
    /// </summary>
    internal abstract partial class XmlRawWriter : XmlWriter
    {
        //
        // Fields
        //
        // base64 converter
        protected XmlRawWriterBase64Encoder base64Encoder;

        // namespace resolver
        protected IXmlNamespaceResolver resolver;

        //
        // XmlWriter implementation
        //

        // Raw writers do not have to track whether this is a well-formed document.
        public override void WriteStartDocument()
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        public override void WriteStartDocument(bool standalone)
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        public override void WriteEndDocument()
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
        }

        // Raw writers do not have to keep a stack of element names.
        public override void WriteEndElement()
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        // Raw writers do not have to keep a stack of element names.
        public override void WriteFullEndElement()
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        // By default, convert base64 value to string and call WriteString.
        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            if (base64Encoder == null)
            {
                base64Encoder = new XmlRawWriterBase64Encoder(this);
            }
            // Encode will call WriteRaw to write out the encoded characters
            base64Encoder.Encode(buffer, index, count);
        }

        // Raw writers do not have to keep track of namespaces.
        public override string LookupPrefix(string ns)
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        // Raw writers do not have to keep track of write states.
        public override WriteState WriteState
        {
            get
            {
                throw new InvalidOperationException(SR.Xml_InvalidOperation);
            }
        }

        // Raw writers do not have to keep track of xml:space.
        public override XmlSpace XmlSpace
        {
            get { throw new InvalidOperationException(SR.Xml_InvalidOperation); }
        }

        // Raw writers do not have to keep track of xml:lang.
        public override string XmlLang
        {
            get { throw new InvalidOperationException(SR.Xml_InvalidOperation); }
        }

        // Raw writers do not have to verify NmToken values.
        public override void WriteNmToken(string name)
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        // Raw writers do not have to verify Name values.
        public override void WriteName(string name)
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        // Raw writers do not have to verify QName values.
        public override void WriteQualifiedName(string localName, string ns)
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        // Forward call to WriteString(string).
        public override void WriteCData(string text)
        {
            WriteString(text);
        }

        // Forward call to WriteString(string).
        public override void WriteCharEntity(char ch)
        {
            WriteString(new string(new char[] { ch }));
        }

        // Forward call to WriteString(string).
        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            WriteString(new string(new char[] { lowChar, highChar }));
        }

        // Forward call to WriteString(string).
        public override void WriteWhitespace(string ws)
        {
            WriteString(ws);
        }

        // Forward call to WriteString(string).
        public override void WriteChars(char[] buffer, int index, int count)
        {
            WriteString(new string(buffer, index, count));
        }

        // Forward call to WriteString(string).
        public override void WriteRaw(char[] buffer, int index, int count)
        {
            WriteString(new string(buffer, index, count));
        }

        // Forward call to WriteString(string).
        public override void WriteRaw(string data)
        {
            WriteString(data);
        }

        // Override in order to handle Xml simple typed values and to pass resolver for QName values
        public override void WriteValue(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            WriteString(XmlUntypedConverter.Untyped.ToString(value, resolver));
        }

        // Override in order to handle Xml simple typed values and to pass resolver for QName values
        public override void WriteValue(string value)
        {
            WriteString(value);
        }

        public override void WriteValue(DateTimeOffset value)
        {
            // For compatibility with custom writers, XmlWriter writes DateTimeOffset as DateTime. 
            // Our internal writers should use the DateTimeOffset-String conversion from XmlConvert.
            WriteString(XmlConvert.ToString(value));
        }

        // Copying to XmlRawWriter is not currently supported.
        public override void WriteAttributes(XmlReader reader, bool defattr)
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        public override void WriteNode(XmlReader reader, bool defattr)
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        public override void WriteNode(System.Xml.XPath.XPathNavigator navigator, bool defattr)
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        //
        // XmlRawWriter methods and properties
        //

        // Get and set the namespace resolver that's used by this RawWriter to resolve prefixes.
        internal virtual IXmlNamespaceResolver NamespaceResolver
        {
            get
            {
                return resolver;
            }
            set
            {
                resolver = value;
            }
        }

        // Write the xml declaration.  This must be the first call.
        internal virtual void WriteXmlDeclaration(XmlStandalone standalone)
        {
        }

        internal virtual void WriteXmlDeclaration(string xmldecl)
        {
        }

        // Called after an element's attributes have been enumerated, but before any children have been
        // enumerated.  This method must always be called, even for empty elements.

        internal abstract void StartElementContent();

        // Called before a root element is written (before the WriteStartElement call)
        //   the conformanceLevel specifies the current conformance level the writer is operating with.
        internal virtual void OnRootElement(ConformanceLevel conformanceLevel) { }

        // WriteEndElement() and WriteFullEndElement() overloads, in which caller gives the full name of the
        // element, so that raw writers do not need to keep a stack of element names.  This method should
        // always be called instead of WriteEndElement() or WriteFullEndElement() without parameters.

        internal abstract void WriteEndElement(string prefix, string localName, string ns);

        internal virtual void WriteFullEndElement(string prefix, string localName, string ns)
        {
            WriteEndElement(prefix, localName, ns);
        }

        internal virtual void WriteQualifiedName(string prefix, string localName, string ns)
        {
            if (prefix.Length != 0)
            {
                WriteString(prefix);
                WriteString(":");
            }
            WriteString(localName);
        }

        // This method must be called instead of WriteStartAttribute() for namespaces.

        internal abstract void WriteNamespaceDeclaration(string prefix, string ns);

        // When true, the XmlWellFormedWriter will call:
        //      1) WriteStartNamespaceDeclaration
        //      2) any method that can be used to write out a value of an attribute: WriteString, WriteChars, WriteRaw, WriteCharEntity... 
        //      3) WriteEndNamespaceDeclaration
        // instead of just a single WriteNamespaceDeclaration call. 
        //
        // This feature will be supported by raw writers serializing to text that wish to preserve the attribute value escaping and entities.
        internal virtual bool SupportsNamespaceDeclarationInChunks
        {
            get
            {
                return false;
            }
        }

        internal virtual void WriteStartNamespaceDeclaration(string prefix)
        {
            throw new NotSupportedException();
        }

        internal virtual void WriteEndNamespaceDeclaration()
        {
            throw new NotSupportedException();
        }

        // This is called when the remainder of a base64 value should be output.
        internal virtual void WriteEndBase64()
        {
            // The Flush will call WriteRaw to write out the rest of the encoded characters
            base64Encoder.Flush();
        }

        internal virtual void Close(WriteState currentState)
        {
            Close();
        }
    }
}

