// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Xml.XPath;
using System.Xml.Schema;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Versioning;

namespace System.Xml
{
    // Specifies the state of the XmlWriter.
    public enum WriteState
    {
        // Nothing has been written yet.
        Start,

        // Writing the prolog.
        Prolog,

        // Writing a the start tag for an element.
        Element,

        // Writing an attribute value.
        Attribute,

        // Writing element content.
        Content,

        // XmlWriter is closed; Close has been called.
        Closed,

        // Writer is in error state.
        Error,
    };

    // Represents a writer that provides fast non-cached forward-only way of generating XML streams containing XML documents 
    // that conform to the W3C Extensible Markup Language (XML) 1.0 specification and the Namespaces in XML specification.
    public abstract partial class XmlWriter : IDisposable
    {
        // Helper buffer for WriteNode(XmlReader, bool)
        private char[] _writeNodeBuffer;

        // Constants
        private const int WriteNodeBufferSize = 1024;

        // Returns the settings describing the features of the writer. Returns null for V1 XmlWriters (XmlTextWriter).
        public virtual XmlWriterSettings Settings
        {
            get
            {
                return null;
            }
        }

        // Write methods
        // Writes out the XML declaration with the version "1.0".

        public abstract void WriteStartDocument();

        //Writes out the XML declaration with the version "1.0" and the specified standalone attribute.

        public abstract void WriteStartDocument(bool standalone);

        //Closes any open elements or attributes and puts the writer back in the Start state.

        public abstract void WriteEndDocument();

        // Writes out the DOCTYPE declaration with the specified name and optional attributes.

        public abstract void WriteDocType(string name, string pubid, string sysid, string subset);

        // Writes out the specified start tag and associates it with the given namespace.
        public void WriteStartElement(string localName, string ns)
        {
            WriteStartElement(null, localName, ns);
        }

        // Writes out the specified start tag and associates it with the given namespace and prefix.

        public abstract void WriteStartElement(string prefix, string localName, string ns);

        // Writes out a start tag with the specified local name with no namespace.
        public void WriteStartElement(string localName)
        {
            WriteStartElement(null, localName, (string)null);
        }

        // Closes one element and pops the corresponding namespace scope.

        public abstract void WriteEndElement();

        // Closes one element and pops the corresponding namespace scope. Writes out a full end element tag, e.g. </element>.

        public abstract void WriteFullEndElement();

        // Writes out the attribute with the specified LocalName, value, and NamespaceURI.
        public void WriteAttributeString(string localName, string ns, string value)
        {
            WriteStartAttribute(null, localName, ns);
            WriteString(value);
            WriteEndAttribute();
        }

        // Writes out the attribute with the specified LocalName and value.
        public void WriteAttributeString(string localName, string value)
        {
            WriteStartAttribute(null, localName, (string)null);
            WriteString(value);
            WriteEndAttribute();
        }

        // Writes out the attribute with the specified prefix, LocalName, NamespaceURI and value.
        public void WriteAttributeString(string prefix, string localName, string ns, string value)
        {
            WriteStartAttribute(prefix, localName, ns);
            WriteString(value);
            WriteEndAttribute();
        }

        // Writes the start of an attribute.
        public void WriteStartAttribute(string localName, string ns)
        {
            WriteStartAttribute(null, localName, ns);
        }

        // Writes the start of an attribute.

        public abstract void WriteStartAttribute(string prefix, string localName, string ns);

        // Writes the start of an attribute.
        public void WriteStartAttribute(string localName)
        {
            WriteStartAttribute(null, localName, (string)null);
        }

        // Closes the attribute opened by WriteStartAttribute call.

        public abstract void WriteEndAttribute();

        // Writes out a <![CDATA[...]]>; block containing the specified text.

        public abstract void WriteCData(string text);

        // Writes out a comment <!--...-->; containing the specified text.

        public abstract void WriteComment(string text);

        // Writes out a processing instruction with a space between the name and text as follows: <?name text?>

        public abstract void WriteProcessingInstruction(string name, string text);

        // Writes out an entity reference as follows: "&"+name+";".

        public abstract void WriteEntityRef(string name);

        // Forces the generation of a character entity for the specified Unicode character value.

        public abstract void WriteCharEntity(char ch);

        // Writes out the given whitespace.

        public abstract void WriteWhitespace(string ws);

        // Writes out the specified text content.

        public abstract void WriteString(string text);

        // Write out the given surrogate pair as an entity reference.

        public abstract void WriteSurrogateCharEntity(char lowChar, char highChar);

        // Writes out the specified text content.

        public abstract void WriteChars(char[] buffer, int index, int count);

        // Writes raw markup from the given character buffer.

        public abstract void WriteRaw(char[] buffer, int index, int count);

        // Writes raw markup from the given string.

        public abstract void WriteRaw(string data);

        // Encodes the specified binary bytes as base64 and writes out the resulting text.

        public abstract void WriteBase64(byte[] buffer, int index, int count);

        // Encodes the specified binary bytes as binhex and writes out the resulting text.
        public virtual void WriteBinHex(byte[] buffer, int index, int count)
        {
            BinHexEncoder.Encode(buffer, index, count, this);
        }

        // Returns the state of the XmlWriter.
        public abstract WriteState WriteState { get; }

        // Closes the XmlWriter and the underlying stream/TextReader (if Settings.CloseOutput is true).
        public virtual void Close() { }

        // Flushes data that is in the internal buffers into the underlying streams/TextReader and flushes the stream/TextReader.

        public abstract void Flush();

        // Returns the closest prefix defined in the current namespace scope for the specified namespace URI.
        public abstract string LookupPrefix(string ns);

        // Gets an XmlSpace representing the current xml:space scope.
        public virtual XmlSpace XmlSpace
        {
            get
            {
                return XmlSpace.Default;
            }
        }

        // Gets the current xml:lang scope.
        public virtual string XmlLang
        {
            get
            {
                return string.Empty;
            }
        }

        // Scalar Value Methods

        // Writes out the specified name, ensuring it is a valid NmToken according to the XML specification 
        // (http://www.w3.org/TR/1998/REC-xml-19980210#NT-Name).
        public virtual void WriteNmToken(string name)
        {
            if (name == null || name.Length == 0)
            {
                throw new ArgumentException(SR.Xml_EmptyName);
            }
            WriteString(XmlConvert.VerifyNMTOKEN(name, ExceptionType.ArgumentException));
        }

        // Writes out the specified name, ensuring it is a valid Name according to the XML specification
        // (http://www.w3.org/TR/1998/REC-xml-19980210#NT-Name).
        public virtual void WriteName(string name)
        {
            WriteString(XmlConvert.VerifyQName(name, ExceptionType.ArgumentException));
        }

        // Writes out the specified namespace-qualified name by looking up the prefix that is in scope for the given namespace.
        public virtual void WriteQualifiedName(string localName, string ns)
        {
            if (ns != null && ns.Length > 0)
            {
                string prefix = LookupPrefix(ns);
                if (prefix == null)
                {
                    throw new ArgumentException(SR.Format(SR.Xml_UndefNamespace, ns));
                }
                WriteString(prefix);
                WriteString(":");
            }
            WriteString(localName);
        }

        // Writes out the specified value.
        public virtual void WriteValue(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            WriteString(XmlUntypedConverter.Untyped.ToString(value, null));
        }

        // Writes out the specified value.
        public virtual void WriteValue(string value)
        {
            if (value == null)
            {
                return;
            }
            WriteString(value);
        }

        // Writes out the specified value.
        public virtual void WriteValue(bool value)
        {
            WriteString(XmlConvert.ToString(value));
        }

        // Writes out the specified value.
        public virtual void WriteValue(DateTime value)
        {
            WriteString(XmlConvert.ToString(value, XmlDateTimeSerializationMode.RoundtripKind));
        }

        // Writes out the specified value.
        public virtual void WriteValue(DateTimeOffset value)
        {
            // Under Win8P, WriteValue(DateTime) will invoke this overload, but custom writers
            // might not have implemented it. This base implementation should call WriteValue(DateTime).
            // The following conversion results in the same string as calling ToString with DateTimeOffset.
            if (value.Offset != TimeSpan.Zero)
            {
                WriteValue(value.LocalDateTime);
            }
            else
            {
                WriteValue(value.UtcDateTime);
            }
        }

        // Writes out the specified value.
        public virtual void WriteValue(double value)
        {
            WriteString(XmlConvert.ToString(value));
        }

        // Writes out the specified value.
        public virtual void WriteValue(float value)
        {
            WriteString(XmlConvert.ToString(value));
        }

        // Writes out the specified value.
        public virtual void WriteValue(decimal value)
        {
            WriteString(XmlConvert.ToString(value));
        }

        // Writes out the specified value.
        public virtual void WriteValue(int value)
        {
            WriteString(XmlConvert.ToString(value));
        }

        // Writes out the specified value.
        public virtual void WriteValue(long value)
        {
            WriteString(XmlConvert.ToString(value));
        }

        // XmlReader Helper Methods

        // Writes out all the attributes found at the current position in the specified XmlReader.
        public virtual void WriteAttributes(XmlReader reader, bool defattr)
        {
            if (null == reader)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.NodeType == XmlNodeType.Element || reader.NodeType == XmlNodeType.XmlDeclaration)
            {
                if (reader.MoveToFirstAttribute())
                {
                    WriteAttributes(reader, defattr);
                    reader.MoveToElement();
                }
            }
            else if (reader.NodeType != XmlNodeType.Attribute)
            {
                throw new XmlException(SR.Xml_InvalidPosition, string.Empty);
            }
            else
            {
                do
                {
                    // we need to check both XmlReader.IsDefault and XmlReader.SchemaInfo.IsDefault. 
                    // If either of these is true and defattr=false, we should not write the attribute out
                    if (defattr || !reader.IsDefaultInternal)
                    {
                        WriteStartAttribute(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                        while (reader.ReadAttributeValue())
                        {
                            if (reader.NodeType == XmlNodeType.EntityReference)
                            {
                                WriteEntityRef(reader.Name);
                            }
                            else
                            {
                                WriteString(reader.Value);
                            }
                        }
                        WriteEndAttribute();
                    }
                }
                while (reader.MoveToNextAttribute());
            }
        }

        // Copies the current node from the given reader to the writer (including child nodes), and if called on an element moves the XmlReader 
        // to the corresponding end element.
        public virtual void WriteNode(XmlReader reader, bool defattr)
        {
            if (null == reader)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            bool canReadChunk = reader.CanReadValueChunk;
            int d = reader.NodeType == XmlNodeType.None ? -1 : reader.Depth;
            do
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                        WriteAttributes(reader, defattr);
                        if (reader.IsEmptyElement)
                        {
                            WriteEndElement();
                            break;
                        }
                        break;
                    case XmlNodeType.Text:
                        if (canReadChunk)
                        {
                            if (_writeNodeBuffer == null)
                            {
                                _writeNodeBuffer = new char[WriteNodeBufferSize];
                            }
                            int read;
                            while ((read = reader.ReadValueChunk(_writeNodeBuffer, 0, WriteNodeBufferSize)) > 0)
                            {
                                this.WriteChars(_writeNodeBuffer, 0, read);
                            }
                        }
                        else
                        {
                            WriteString(reader.Value);
                        }
                        break;
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:

                        WriteWhitespace(reader.Value);

                        break;
                    case XmlNodeType.CDATA:
                        WriteCData(reader.Value);
                        break;
                    case XmlNodeType.EntityReference:
                        WriteEntityRef(reader.Name);
                        break;
                    case XmlNodeType.XmlDeclaration:
                    case XmlNodeType.ProcessingInstruction:
                        WriteProcessingInstruction(reader.Name, reader.Value);
                        break;
                    case XmlNodeType.DocumentType:
                        WriteDocType(reader.Name, reader.GetAttribute("PUBLIC"), reader.GetAttribute("SYSTEM"), reader.Value);
                        break;

                    case XmlNodeType.Comment:
                        WriteComment(reader.Value);
                        break;
                    case XmlNodeType.EndElement:
                        WriteFullEndElement();
                        break;
                }
            } while (reader.Read() && (d < reader.Depth || (d == reader.Depth && reader.NodeType == XmlNodeType.EndElement)));
        }

        // Copies the current node from the given XPathNavigator to the writer (including child nodes).
        public virtual void WriteNode(XPathNavigator navigator, bool defattr)
        {
            if (navigator == null)
            {
                throw new ArgumentNullException(nameof(navigator));
            }
            int iLevel = 0;

            navigator = navigator.Clone();

            while (true)
            {
                bool mayHaveChildren = false;
                XPathNodeType nodeType = navigator.NodeType;

                switch (nodeType)
                {
                    case XPathNodeType.Element:
                        WriteStartElement(navigator.Prefix, navigator.LocalName, navigator.NamespaceURI);

                        // Copy attributes
                        if (navigator.MoveToFirstAttribute())
                        {
                            do
                            {
                                IXmlSchemaInfo schemaInfo = navigator.SchemaInfo;
                                if (defattr || (schemaInfo == null || !schemaInfo.IsDefault))
                                {
                                    WriteStartAttribute(navigator.Prefix, navigator.LocalName, navigator.NamespaceURI);
                                    // copy string value to writer
                                    WriteString(navigator.Value);
                                    WriteEndAttribute();
                                }
                            } while (navigator.MoveToNextAttribute());
                            navigator.MoveToParent();
                        }

                        // Copy namespaces
                        if (navigator.MoveToFirstNamespace(XPathNamespaceScope.Local))
                        {
                            WriteLocalNamespaces(navigator);
                            navigator.MoveToParent();
                        }
                        mayHaveChildren = true;
                        break;
                    case XPathNodeType.Attribute:
                        // do nothing on root level attribute
                        break;
                    case XPathNodeType.Text:
                        WriteString(navigator.Value);
                        break;
                    case XPathNodeType.SignificantWhitespace:
                    case XPathNodeType.Whitespace:
                        WriteWhitespace(navigator.Value);
                        break;
                    case XPathNodeType.Root:
                        mayHaveChildren = true;
                        break;
                    case XPathNodeType.Comment:
                        WriteComment(navigator.Value);
                        break;
                    case XPathNodeType.ProcessingInstruction:
                        WriteProcessingInstruction(navigator.LocalName, navigator.Value);
                        break;
                    case XPathNodeType.Namespace:
                        // do nothing on root level namespace
                        break;
                    default:
                        Debug.Fail($"Unexpected node type {nodeType}");
                        break;
                }

                if (mayHaveChildren)
                {
                    // If children exist, move down to next level
                    if (navigator.MoveToFirstChild())
                    {
                        iLevel++;
                        continue;
                    }
                    else
                    {
                        // EndElement
                        if (navigator.NodeType == XPathNodeType.Element)
                        {
                            if (navigator.IsEmptyElement)
                            {
                                WriteEndElement();
                            }
                            else
                            {
                                WriteFullEndElement();
                            }
                        }
                    }
                }

                // No children
                while (true)
                {
                    if (iLevel == 0)
                    {
                        // The entire subtree has been copied
                        return;
                    }

                    if (navigator.MoveToNext())
                    {
                        // Found a sibling, so break to outer loop
                        break;
                    }

                    // No siblings, so move up to previous level
                    iLevel--;
                    navigator.MoveToParent();

                    // EndElement
                    if (navigator.NodeType == XPathNodeType.Element)
                        WriteFullEndElement();
                }
            }
        }

        // Element Helper Methods

        // Writes out an element with the specified name containing the specified string value.
        public void WriteElementString(string localName, string value)
        {
            WriteElementString(localName, null, value);
        }

        // Writes out an attribute with the specified name, namespace URI and string value.
        public void WriteElementString(string localName, string ns, string value)
        {
            WriteStartElement(localName, ns);
            if (null != value && 0 != value.Length)
            {
                WriteString(value);
            }
            WriteEndElement();
        }

        // Writes out an attribute with the specified name, namespace URI, and string value.
        public void WriteElementString(string prefix, string localName, string ns, string value)
        {
            WriteStartElement(prefix, localName, ns);
            if (null != value && 0 != value.Length)
            {
                WriteString(value);
            }
            WriteEndElement();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        // Dispose the underline stream objects (calls Close on the XmlWriter)
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && WriteState != WriteState.Closed)
            {
                Close();
            }
        }

        // Copy local namespaces on the navigator's current node to the raw writer. The namespaces are returned by the navigator in reversed order. 
        // The recursive call reverses them back.
        private void WriteLocalNamespaces(XPathNavigator nsNav)
        {
            string prefix = nsNav.LocalName;
            string ns = nsNav.Value;

            if (nsNav.MoveToNextNamespace(XPathNamespaceScope.Local))
            {
                WriteLocalNamespaces(nsNav);
            }

            if (prefix.Length == 0)
            {
                WriteAttributeString(string.Empty, "xmlns", XmlReservedNs.NsXmlNs, ns);
            }
            else
            {
                WriteAttributeString("xmlns", prefix, XmlReservedNs.NsXmlNs, ns);
            }
        }

        //
        // Static methods for creating writers
        //
        // Creates an XmlWriter for writing into the provided file.
        public static XmlWriter Create(string outputFileName)
        {
            return Create(outputFileName, null);
        }

        // Creates an XmlWriter for writing into the provided file with the specified settings.
        public static XmlWriter Create(string outputFileName, XmlWriterSettings settings)
        {
            if (settings == null)
            {
                settings = new XmlWriterSettings();
            }
            return settings.CreateWriter(outputFileName);
        }

        // Creates an XmlWriter for writing into the provided stream.
        public static XmlWriter Create(Stream output)
        {
            return Create(output, null);
        }

        // Creates an XmlWriter for writing into the provided stream with the specified settings.
        public static XmlWriter Create(Stream output, XmlWriterSettings settings)
        {
            if (settings == null)
            {
                settings = new XmlWriterSettings();
            }
            return settings.CreateWriter(output);
        }

        // Creates an XmlWriter for writing into the provided TextWriter.
        public static XmlWriter Create(TextWriter output)
        {
            return Create(output, null);
        }

        // Creates an XmlWriter for writing into the provided TextWriter with the specified settings.
        public static XmlWriter Create(TextWriter output, XmlWriterSettings settings)
        {
            if (settings == null)
            {
                settings = new XmlWriterSettings();
            }
            return settings.CreateWriter(output);
        }

        // Creates an XmlWriter for writing into the provided StringBuilder.
        public static XmlWriter Create(StringBuilder output)
        {
            return Create(output, null);
        }

        // Creates an XmlWriter for writing into the provided StringBuilder with the specified settings.
        public static XmlWriter Create(StringBuilder output, XmlWriterSettings settings)
        {
            if (settings == null)
            {
                settings = new XmlWriterSettings();
            }
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }
            return settings.CreateWriter(new StringWriter(output, CultureInfo.InvariantCulture));
        }

        // Creates an XmlWriter wrapped around the provided XmlWriter with the default settings.
        public static XmlWriter Create(XmlWriter output)
        {
            return Create(output, null);
        }

        // Creates an XmlWriter wrapped around the provided XmlWriter with the specified settings.
        public static XmlWriter Create(XmlWriter output, XmlWriterSettings settings)
        {
            if (settings == null)
            {
                settings = new XmlWriterSettings();
            }
            return settings.CreateWriter(output);
        }
    }
}

