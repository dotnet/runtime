// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Security;
using System.Diagnostics;
using System.Collections;
using System.Globalization;
using System.Xml.Schema;
using System.Runtime.Versioning;

namespace System.Xml
{
    // Represents a reader that provides fast, non-cached forward only stream access to XML data. 
    [DebuggerDisplay("{debuggerDisplayProxy}")]
    public abstract partial class XmlReader : IDisposable
    {
        private static uint s_isTextualNodeBitmap = 0x6018; // 00 0110 0000 0001 1000
        // 0 None, 
        // 0 Element,
        // 0 Attribute,
        // 1 Text,
        // 1 CDATA,
        // 0 EntityReference,
        // 0 Entity,
        // 0 ProcessingInstruction,
        // 0 Comment,
        // 0 Document,
        // 0 DocumentType,
        // 0 DocumentFragment,
        // 0 Notation,
        // 1 Whitespace,
        // 1 SignificantWhitespace,
        // 0 EndElement,
        // 0 EndEntity,
        // 0 XmlDeclaration

        private static uint s_canReadContentAsBitmap = 0x1E1BC; // 01 1110 0001 1011 1100
        // 0 None, 
        // 0 Element,
        // 1 Attribute,
        // 1 Text,
        // 1 CDATA,
        // 1 EntityReference,
        // 0 Entity,
        // 1 ProcessingInstruction,
        // 1 Comment,
        // 0 Document,
        // 0 DocumentType,
        // 0 DocumentFragment,
        // 0 Notation,
        // 1 Whitespace,
        // 1 SignificantWhitespace,
        // 1 EndElement,
        // 1 EndEntity,
        // 0 XmlDeclaration

        private static uint s_hasValueBitmap = 0x2659C; // 10 0110 0101 1001 1100
        // 0 None, 
        // 0 Element,
        // 1 Attribute,
        // 1 Text,
        // 1 CDATA,
        // 0 EntityReference,
        // 0 Entity,
        // 1 ProcessingInstruction,
        // 1 Comment,
        // 0 Document,
        // 1 DocumentType,
        // 0 DocumentFragment,
        // 0 Notation,
        // 1 Whitespace,
        // 1 SignificantWhitespace,
        // 0 EndElement,
        // 0 EndEntity,
        // 1 XmlDeclaration

        //
        // Constants
        //
        internal const int DefaultBufferSize = 4096;
        internal const int BiggerBufferSize = 8192;
        internal const int MaxStreamLengthForDefaultBufferSize = 64 * 1024; // 64kB

        internal const int AsyncBufferSize = 64 * 1024; //64KB

        // Settings
        public virtual XmlReaderSettings Settings
        {
            get
            {
                return null;
            }
        }

        // Node Properties
        // Get the type of the current node.
        public abstract XmlNodeType NodeType { get; }

        // Gets the name of the current node, including the namespace prefix.
        public virtual string Name
        {
            get
            {
                if (Prefix.Length == 0)
                {
                    return LocalName;
                }
                else
                {
                    return NameTable.Add(string.Concat(Prefix, ":", LocalName));
                }
            }
        }

        // Gets the name of the current node without the namespace prefix.
        public abstract string LocalName { get; }

        // Gets the namespace URN (as defined in the W3C Namespace Specification) of the current namespace scope.
        public abstract string NamespaceURI { get; }

        // Gets the namespace prefix associated with the current node.
        public abstract string Prefix { get; }

        // Gets a value indicating whether
        public virtual bool HasValue
        {
            get
            {
                return HasValueInternal(this.NodeType);
            }
        }

        // Gets the text value of the current node.
        public abstract string Value { get; }

        // Gets the depth of the current node in the XML element stack.
        public abstract int Depth { get; }

        // Gets the base URI of the current node.
        public abstract string BaseURI { get; }

        // Gets a value indicating whether the current node is an empty element (for example, <MyElement/>).
        public abstract bool IsEmptyElement { get; }

        // Gets a value indicating whether the current node is an attribute that was generated from the default value defined
        // in the DTD or schema.
        public virtual bool IsDefault
        {
            get
            {
                return false;
            }
        }

        // Gets the quotation mark character used to enclose the value of an attribute node.
        public virtual char QuoteChar
        {
            get
            {
                return '"';
            }
        }

        // Gets the current xml:space scope.
        public virtual XmlSpace XmlSpace
        {
            get
            {
                return XmlSpace.None;
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

        // returns the schema info interface of the reader
        public virtual IXmlSchemaInfo SchemaInfo
        {
            get
            {
                return this as IXmlSchemaInfo;
            }
        }

        // returns the type of the current node
        public virtual System.Type ValueType
        {
            get
            {
                return typeof(string);
            }
        }

        // Concatenates values of textual nodes of the current content, ignoring comments and PIs, expanding entity references, 
        // and returns the content as the most appropriate type (by default as string). Stops at start tags and end tags.
        public virtual object ReadContentAsObject()
        {
            if (!CanReadContentAs())
            {
                throw CreateReadContentAsException(nameof(ReadContentAsObject));
            }
            return InternalReadContentAsString();
        }

        // Concatenates values of textual nodes of the current content, ignoring comments and PIs, expanding entity references, 
        // and converts the content to a boolean. Stops at start tags and end tags.
        public virtual bool ReadContentAsBoolean()
        {
            if (!CanReadContentAs())
            {
                throw CreateReadContentAsException(nameof(ReadContentAsBoolean));
            }
            try
            {
                return XmlConvert.ToBoolean(InternalReadContentAsString());
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Boolean", e, this as IXmlLineInfo);
            }
        }

        // Concatenates values of textual nodes of the current content, ignoring comments and PIs, expanding entity references, 
        // and converts the content to a DateTime. Stops at start tags and end tags.
        public virtual DateTime ReadContentAsDateTime()
        {
            if (!CanReadContentAs())
            {
                throw CreateReadContentAsException(nameof(ReadContentAsDateTime));
            }
            try
            {
                return XmlConvert.ToDateTime(InternalReadContentAsString(), XmlDateTimeSerializationMode.RoundtripKind);
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "DateTime", e, this as IXmlLineInfo);
            }
        }

        // Concatenates values of textual nodes of the current content, ignoring comments and PIs, expanding entity references, 
        // and converts the content to a DateTimeOffset. Stops at start tags and end tags.
        public virtual DateTimeOffset ReadContentAsDateTimeOffset()
        {
            if (!CanReadContentAs())
            {
                throw CreateReadContentAsException(nameof(ReadContentAsDateTimeOffset));
            }
            try
            {
                return XmlConvert.ToDateTimeOffset(InternalReadContentAsString());
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "DateTimeOffset", e, this as IXmlLineInfo);
            }
        }

        // Concatenates values of textual nodes of the current content, ignoring comments and PIs, expanding entity references, 
        // and converts the content to a double. Stops at start tags and end tags.
        public virtual double ReadContentAsDouble()
        {
            if (!CanReadContentAs())
            {
                throw CreateReadContentAsException(nameof(ReadContentAsDouble));
            }
            try
            {
                return XmlConvert.ToDouble(InternalReadContentAsString());
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Double", e, this as IXmlLineInfo);
            }
        }

        // Concatenates values of textual nodes of the current content, ignoring comments and PIs, expanding entity references, 
        // and converts the content to a float. Stops at start tags and end tags.
        public virtual float ReadContentAsFloat()
        {
            if (!CanReadContentAs())
            {
                throw CreateReadContentAsException(nameof(ReadContentAsFloat));
            }
            try
            {
                return XmlConvert.ToSingle(InternalReadContentAsString());
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Float", e, this as IXmlLineInfo);
            }
        }

        // Concatenates values of textual nodes of the current content, ignoring comments and PIs, expanding entity references, 
        // and converts the content to a decimal. Stops at start tags and end tags.
        public virtual decimal ReadContentAsDecimal()
        {
            if (!CanReadContentAs())
            {
                throw CreateReadContentAsException(nameof(ReadContentAsDecimal));
            }
            try
            {
                return XmlConvert.ToDecimal(InternalReadContentAsString());
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Decimal", e, this as IXmlLineInfo);
            }
        }

        // Concatenates values of textual nodes of the current content, ignoring comments and PIs, expanding entity references, 
        // and converts the content to an int. Stops at start tags and end tags.
        public virtual int ReadContentAsInt()
        {
            if (!CanReadContentAs())
            {
                throw CreateReadContentAsException(nameof(ReadContentAsInt));
            }
            try
            {
                return XmlConvert.ToInt32(InternalReadContentAsString());
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Int", e, this as IXmlLineInfo);
            }
        }

        // Concatenates values of textual nodes of the current content, ignoring comments and PIs, expanding entity references, 
        // and converts the content to a long. Stops at start tags and end tags.
        public virtual long ReadContentAsLong()
        {
            if (!CanReadContentAs())
            {
                throw CreateReadContentAsException(nameof(ReadContentAsLong));
            }
            try
            {
                return XmlConvert.ToInt64(InternalReadContentAsString());
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "Long", e, this as IXmlLineInfo);
            }
        }

        // Concatenates values of textual nodes of the current content, ignoring comments and PIs, expanding entity references, 
        // and returns the content as a string. Stops at start tags and end tags.
        public virtual string ReadContentAsString()
        {
            if (!CanReadContentAs())
            {
                throw CreateReadContentAsException(nameof(ReadContentAsString));
            }
            return InternalReadContentAsString();
        }

        // Concatenates values of textual nodes of the current content, ignoring comments and PIs, expanding entity references, 
        // and converts the content to the requested type. Stops at start tags and end tags.
        public virtual object ReadContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            if (!CanReadContentAs())
            {
                throw CreateReadContentAsException(nameof(ReadContentAs));
            }

            string strContentValue = InternalReadContentAsString();
            if (returnType == typeof(string))
            {
                return strContentValue;
            }
            else
            {
                try
                {
                    return XmlUntypedStringConverter.Instance.FromString(strContentValue, returnType, (namespaceResolver == null ? this as IXmlNamespaceResolver : namespaceResolver));
                }
                catch (FormatException e)
                {
                    throw new XmlException(SR.Xml_ReadContentAsFormatException, returnType.ToString(), e, this as IXmlLineInfo);
                }
                catch (InvalidCastException e)
                {
                    throw new XmlException(SR.Xml_ReadContentAsFormatException, returnType.ToString(), e, this as IXmlLineInfo);
                }
            }
        }

        // Returns the content of the current element as the most appropriate type. Moves to the node following the element's end tag.
        public virtual object ReadElementContentAsObject()
        {
            if (SetupReadElementContentAsXxx("ReadElementContentAsObject"))
            {
                object value = ReadContentAsObject();
                FinishReadElementContentAsXxx();
                return value;
            }
            return string.Empty;
        }

        // Checks local name and namespace of the current element and returns its content as the most appropriate type. Moves to the node following the element's end tag.
        public virtual object ReadElementContentAsObject(string localName, string namespaceURI)
        {
            CheckElement(localName, namespaceURI);
            return ReadElementContentAsObject();
        }

        // Returns the content of the current element as a boolean. Moves to the node following the element's end tag.
        public virtual bool ReadElementContentAsBoolean()
        {
            if (SetupReadElementContentAsXxx("ReadElementContentAsBoolean"))
            {
                bool value = ReadContentAsBoolean();
                FinishReadElementContentAsXxx();
                return value;
            }
            return XmlConvert.ToBoolean(string.Empty);
        }

        // Checks local name and namespace of the current element and returns its content as a boolean. Moves to the node following the element's end tag.
        public virtual bool ReadElementContentAsBoolean(string localName, string namespaceURI)
        {
            CheckElement(localName, namespaceURI);
            return ReadElementContentAsBoolean();
        }

        // Returns the content of the current element as a DateTime. Moves to the node following the element's end tag.
        public virtual DateTime ReadElementContentAsDateTime()
        {
            if (SetupReadElementContentAsXxx("ReadElementContentAsDateTime"))
            {
                DateTime value = ReadContentAsDateTime();
                FinishReadElementContentAsXxx();
                return value;
            }
            return XmlConvert.ToDateTime(string.Empty, XmlDateTimeSerializationMode.RoundtripKind);
        }

        // Checks local name and namespace of the current element and returns its content as a DateTime. 
        // Moves to the node following the element's end tag.
        public virtual DateTime ReadElementContentAsDateTime(string localName, string namespaceURI)
        {
            CheckElement(localName, namespaceURI);
            return ReadElementContentAsDateTime();
        }

        // Returns the content of the current element as a double. Moves to the node following the element's end tag.
        public virtual double ReadElementContentAsDouble()
        {
            if (SetupReadElementContentAsXxx("ReadElementContentAsDouble"))
            {
                double value = ReadContentAsDouble();
                FinishReadElementContentAsXxx();
                return value;
            }
            return XmlConvert.ToDouble(string.Empty);
        }

        // Checks local name and namespace of the current element and returns its content as a double. 
        // Moves to the node following the element's end tag.
        public virtual double ReadElementContentAsDouble(string localName, string namespaceURI)
        {
            CheckElement(localName, namespaceURI);
            return ReadElementContentAsDouble();
        }

        // Returns the content of the current element as a float. Moves to the node following the element's end tag.
        public virtual float ReadElementContentAsFloat()
        {
            if (SetupReadElementContentAsXxx("ReadElementContentAsFloat"))
            {
                float value = ReadContentAsFloat();
                FinishReadElementContentAsXxx();
                return value;
            }
            return XmlConvert.ToSingle(string.Empty);
        }

        // Checks local name and namespace of the current element and returns its content as a float. 
        // Moves to the node following the element's end tag.
        public virtual float ReadElementContentAsFloat(string localName, string namespaceURI)
        {
            CheckElement(localName, namespaceURI);
            return ReadElementContentAsFloat();
        }

        // Returns the content of the current element as a decimal. Moves to the node following the element's end tag.
        public virtual decimal ReadElementContentAsDecimal()
        {
            if (SetupReadElementContentAsXxx("ReadElementContentAsDecimal"))
            {
                decimal value = ReadContentAsDecimal();
                FinishReadElementContentAsXxx();
                return value;
            }
            return XmlConvert.ToDecimal(string.Empty);
        }

        // Checks local name and namespace of the current element and returns its content as a decimal. 
        // Moves to the node following the element's end tag.
        public virtual decimal ReadElementContentAsDecimal(string localName, string namespaceURI)
        {
            CheckElement(localName, namespaceURI);
            return ReadElementContentAsDecimal();
        }

        // Returns the content of the current element as an int. Moves to the node following the element's end tag.
        public virtual int ReadElementContentAsInt()
        {
            if (SetupReadElementContentAsXxx("ReadElementContentAsInt"))
            {
                int value = ReadContentAsInt();
                FinishReadElementContentAsXxx();
                return value;
            }
            return XmlConvert.ToInt32(string.Empty);
        }

        // Checks local name and namespace of the current element and returns its content as an int. 
        // Moves to the node following the element's end tag.
        public virtual int ReadElementContentAsInt(string localName, string namespaceURI)
        {
            CheckElement(localName, namespaceURI);
            return ReadElementContentAsInt();
        }

        // Returns the content of the current element as a long. Moves to the node following the element's end tag.
        public virtual long ReadElementContentAsLong()
        {
            if (SetupReadElementContentAsXxx("ReadElementContentAsLong"))
            {
                long value = ReadContentAsLong();
                FinishReadElementContentAsXxx();
                return value;
            }
            return XmlConvert.ToInt64(string.Empty);
        }

        // Checks local name and namespace of the current element and returns its content as a long. 
        // Moves to the node following the element's end tag.
        public virtual long ReadElementContentAsLong(string localName, string namespaceURI)
        {
            CheckElement(localName, namespaceURI);
            return ReadElementContentAsLong();
        }

        // Returns the content of the current element as a string. Moves to the node following the element's end tag.
        public virtual string ReadElementContentAsString()
        {
            if (SetupReadElementContentAsXxx("ReadElementContentAsString"))
            {
                string value = ReadContentAsString();
                FinishReadElementContentAsXxx();
                return value;
            }
            return string.Empty;
        }

        // Checks local name and namespace of the current element and returns its content as a string. 
        // Moves to the node following the element's end tag.
        public virtual string ReadElementContentAsString(string localName, string namespaceURI)
        {
            CheckElement(localName, namespaceURI);
            return ReadElementContentAsString();
        }

        // Returns the content of the current element as the requested type. Moves to the node following the element's end tag.
        public virtual object ReadElementContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            if (SetupReadElementContentAsXxx("ReadElementContentAs"))
            {
                object value = ReadContentAs(returnType, namespaceResolver);
                FinishReadElementContentAsXxx();
                return value;
            }
            return (returnType == typeof(string)) ? string.Empty : XmlUntypedStringConverter.Instance.FromString(string.Empty, returnType, namespaceResolver);
        }

        // Checks local name and namespace of the current element and returns its content as the requested type. 
        // Moves to the node following the element's end tag.
        public virtual object ReadElementContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver, string localName, string namespaceURI)
        {
            CheckElement(localName, namespaceURI);
            return ReadElementContentAs(returnType, namespaceResolver);
        }

        // Attribute Accessors
        // The number of attributes on the current node.
        public abstract int AttributeCount { get; }

        // Gets the value of the attribute with the specified Name
        public abstract string GetAttribute(string name);

        // Gets the value of the attribute with the LocalName and NamespaceURI
        public abstract string GetAttribute(string name, string namespaceURI);

        // Gets the value of the attribute with the specified index.
        public abstract string GetAttribute(int i);

        // Gets the value of the attribute with the specified index.
        public virtual string this[int i]
        {
            get
            {
                return GetAttribute(i);
            }
        }

        // Gets the value of the attribute with the specified Name.
        public virtual string this[string name]
        {
            get
            {
                return GetAttribute(name);
            }
        }

        // Gets the value of the attribute with the LocalName and NamespaceURI
        public virtual string this[string name, string namespaceURI]
        {
            get
            {
                return GetAttribute(name, namespaceURI);
            }
        }

        // Moves to the attribute with the specified Name.
        public abstract bool MoveToAttribute(string name);

        // Moves to the attribute with the specified LocalName and NamespaceURI.
        public abstract bool MoveToAttribute(string name, string ns);

        // Moves to the attribute with the specified index.
        public virtual void MoveToAttribute(int i)
        {
            if (i < 0 || i >= AttributeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(i));
            }
            MoveToElement();
            MoveToFirstAttribute();
            int j = 0;
            while (j < i)
            {
                MoveToNextAttribute();
                j++;
            }
        }

        // Moves to the first attribute of the current node.
        public abstract bool MoveToFirstAttribute();

        // Moves to the next attribute.
        public abstract bool MoveToNextAttribute();

        // Moves to the element that contains the current attribute node.
        public abstract bool MoveToElement();

        // Parses the attribute value into one or more Text and/or EntityReference node types.

        public abstract bool ReadAttributeValue();

        // Moving through the Stream
        // Reads the next node from the stream.

        public abstract bool Read();

        // Returns true when the XmlReader is positioned at the end of the stream.
        public abstract bool EOF { get; }

        // Closes the stream/TextReader (if CloseInput==true), changes the ReadState to Closed, and sets all the properties back to zero/empty string.
        public virtual void Close() { }

        // Returns the read state of the XmlReader.
        public abstract ReadState ReadState { get; }

        // Skips to the end tag of the current element.
        public virtual void Skip()
        {
            if (ReadState != ReadState.Interactive)
            {
                return;
            }
            SkipSubtree();
        }

        // Gets the XmlNameTable associated with the XmlReader.
        public abstract XmlNameTable NameTable { get; }

        // Resolves a namespace prefix in the current element's scope.
        public abstract string LookupNamespace(string prefix);

        // Returns true if the XmlReader can expand general entities.
        public virtual bool CanResolveEntity
        {
            get
            {
                return false;
            }
        }

        // Resolves the entity reference for nodes of NodeType EntityReference.
        public abstract void ResolveEntity();

        // Binary content access methods
        // Returns true if the reader supports call to ReadContentAsBase64, ReadElementContentAsBase64, ReadContentAsBinHex and ReadElementContentAsBinHex.
        public virtual bool CanReadBinaryContent
        {
            get
            {
                return false;
            }
        }

        // Returns decoded bytes of the current base64 text content. Call this methods until it returns 0 to get all the data.
        public virtual int ReadContentAsBase64(byte[] buffer, int index, int count)
        {
            throw new NotSupportedException(SR.Format(SR.Xml_ReadBinaryContentNotSupported, "ReadContentAsBase64"));
        }

        // Returns decoded bytes of the current base64 element content. Call this methods until it returns 0 to get all the data.
        public virtual int ReadElementContentAsBase64(byte[] buffer, int index, int count)
        {
            throw new NotSupportedException(SR.Format(SR.Xml_ReadBinaryContentNotSupported, "ReadElementContentAsBase64"));
        }

        // Returns decoded bytes of the current binhex text content. Call this methods until it returns 0 to get all the data.
        public virtual int ReadContentAsBinHex(byte[] buffer, int index, int count)
        {
            throw new NotSupportedException(SR.Format(SR.Xml_ReadBinaryContentNotSupported, "ReadContentAsBinHex"));
        }

        // Returns decoded bytes of the current binhex element content. Call this methods until it returns 0 to get all the data.
        public virtual int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
        {
            throw new NotSupportedException(SR.Format(SR.Xml_ReadBinaryContentNotSupported, "ReadElementContentAsBinHex"));
        }

        // Text streaming methods

        // Returns true if the XmlReader supports calls to ReadValueChunk.
        public virtual bool CanReadValueChunk
        {
            get
            {
                return false;
            }
        }

        // Returns a chunk of the value of the current node. Call this method in a loop to get all the data. 
        // Use this method to get a streaming access to the value of the current node.
        public virtual int ReadValueChunk(char[] buffer, int index, int count)
        {
            throw new NotSupportedException(SR.Xml_ReadValueChunkNotSupported);
        }

        // Virtual helper methods
        // Reads the contents of an element as a string. Stops of comments, PIs or entity references.
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual string ReadString()
        {
            if (this.ReadState != ReadState.Interactive)
            {
                return string.Empty;
            }
            this.MoveToElement();
            if (this.NodeType == XmlNodeType.Element)
            {
                if (this.IsEmptyElement)
                {
                    return string.Empty;
                }
                else if (!this.Read())
                {
                    throw new InvalidOperationException(SR.Xml_InvalidOperation);
                }
                if (this.NodeType == XmlNodeType.EndElement)
                {
                    return string.Empty;
                }
            }
            string result = string.Empty;
            while (IsTextualNode(this.NodeType))
            {
                result += this.Value;
                if (!this.Read())
                {
                    break;
                }
            }
            return result;
        }

        // Checks whether the current node is a content (non-whitespace text, CDATA, Element, EndElement, EntityReference
        // or EndEntity) node. If the node is not a content node, then the method skips ahead to the next content node or 
        // end of file. Skips over nodes of type ProcessingInstruction, DocumentType, Comment, Whitespace and SignificantWhitespace.
        public virtual XmlNodeType MoveToContent()
        {
            do
            {
                switch (this.NodeType)
                {
                    case XmlNodeType.Attribute:
                        MoveToElement();
                        goto case XmlNodeType.Element;
                    case XmlNodeType.Element:
                    case XmlNodeType.EndElement:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.Text:
                    case XmlNodeType.EntityReference:
                    case XmlNodeType.EndEntity:
                        return this.NodeType;
                }
            } while (Read());
            return this.NodeType;
        }

        // Checks that the current node is an element and advances the reader to the next node.
        public virtual void ReadStartElement()
        {
            if (MoveToContent() != XmlNodeType.Element)
            {
                throw new XmlException(SR.Xml_InvalidNodeType, this.NodeType.ToString(), this as IXmlLineInfo);
            }
            Read();
        }

        // Checks that the current content node is an element with the given Name and advances the reader to the next node.
        public virtual void ReadStartElement(string name)
        {
            if (MoveToContent() != XmlNodeType.Element)
            {
                throw new XmlException(SR.Xml_InvalidNodeType, this.NodeType.ToString(), this as IXmlLineInfo);
            }
            if (this.Name == name)
            {
                Read();
            }
            else
            {
                throw new XmlException(SR.Xml_ElementNotFound, name, this as IXmlLineInfo);
            }
        }

        // Checks that the current content node is an element with the given LocalName and NamespaceURI
        // and advances the reader to the next node.
        public virtual void ReadStartElement(string localname, string ns)
        {
            if (MoveToContent() != XmlNodeType.Element)
            {
                throw new XmlException(SR.Xml_InvalidNodeType, this.NodeType.ToString(), this as IXmlLineInfo);
            }
            if (this.LocalName == localname && this.NamespaceURI == ns)
            {
                Read();
            }
            else
            {
                throw new XmlException(SR.Xml_ElementNotFoundNs, new string[2] { localname, ns }, this as IXmlLineInfo);
            }
        }

        // Reads a text-only element.
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual string ReadElementString()
        {
            string result = string.Empty;

            if (MoveToContent() != XmlNodeType.Element)
            {
                throw new XmlException(SR.Xml_InvalidNodeType, this.NodeType.ToString(), this as IXmlLineInfo);
            }
            if (!this.IsEmptyElement)
            {
                Read();
                result = ReadString();
                if (this.NodeType != XmlNodeType.EndElement)
                {
                    throw new XmlException(SR.Xml_UnexpectedNodeInSimpleContent, new string[] { this.NodeType.ToString(), "ReadElementString" }, this as IXmlLineInfo);
                }
                Read();
            }
            else
            {
                Read();
            }
            return result;
        }

        // Checks that the Name property of the element found matches the given string before reading a text-only element.
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual string ReadElementString(string name)
        {
            string result = string.Empty;

            if (MoveToContent() != XmlNodeType.Element)
            {
                throw new XmlException(SR.Xml_InvalidNodeType, this.NodeType.ToString(), this as IXmlLineInfo);
            }
            if (this.Name != name)
            {
                throw new XmlException(SR.Xml_ElementNotFound, name, this as IXmlLineInfo);
            }

            if (!this.IsEmptyElement)
            {
                //Read();
                result = ReadString();
                if (this.NodeType != XmlNodeType.EndElement)
                {
                    throw new XmlException(SR.Xml_InvalidNodeType, this.NodeType.ToString(), this as IXmlLineInfo);
                }
                Read();
            }
            else
            {
                Read();
            }
            return result;
        }

        // Checks that the LocalName and NamespaceURI properties of the element found matches the given strings 
        // before reading a text-only element.
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual string ReadElementString(string localname, string ns)
        {
            string result = string.Empty;
            if (MoveToContent() != XmlNodeType.Element)
            {
                throw new XmlException(SR.Xml_InvalidNodeType, this.NodeType.ToString(), this as IXmlLineInfo);
            }
            if (this.LocalName != localname || this.NamespaceURI != ns)
            {
                throw new XmlException(SR.Xml_ElementNotFoundNs, new string[2] { localname, ns }, this as IXmlLineInfo);
            }

            if (!this.IsEmptyElement)
            {
                //Read();
                result = ReadString();
                if (this.NodeType != XmlNodeType.EndElement)
                {
                    throw new XmlException(SR.Xml_InvalidNodeType, this.NodeType.ToString(), this as IXmlLineInfo);
                }
                Read();
            }
            else
            {
                Read();
            }

            return result;
        }

        // Checks that the current content node is an end tag and advances the reader to the next node.
        public virtual void ReadEndElement()
        {
            if (MoveToContent() != XmlNodeType.EndElement)
            {
                throw new XmlException(SR.Xml_InvalidNodeType, this.NodeType.ToString(), this as IXmlLineInfo);
            }
            Read();
        }

        // Calls MoveToContent and tests if the current content node is a start tag or empty element tag (XmlNodeType.Element).
        public virtual bool IsStartElement()
        {
            return MoveToContent() == XmlNodeType.Element;
        }

        // Calls MoveToContentand tests if the current content node is a start tag or empty element tag (XmlNodeType.Element) and if the
        // Name property of the element found matches the given argument.
        public virtual bool IsStartElement(string name)
        {
            return (MoveToContent() == XmlNodeType.Element) &&
                   (this.Name == name);
        }

        // Calls MoveToContent and tests if the current content node is a start tag or empty element tag (XmlNodeType.Element) and if
        // the LocalName and NamespaceURI properties of the element found match the given strings.
        public virtual bool IsStartElement(string localname, string ns)
        {
            return (MoveToContent() == XmlNodeType.Element) &&
                   (this.LocalName == localname && this.NamespaceURI == ns);
        }

        // Reads to the following element with the given Name.
        public virtual bool ReadToFollowing(string name)
        {
            if (name == null || name.Length == 0)
            {
                throw XmlConvert.CreateInvalidNameArgumentException(name, nameof(name));
            }
            // atomize name
            name = NameTable.Add(name);

            // find following element with that name
            while (Read())
            {
                if (NodeType == XmlNodeType.Element && Ref.Equal(name, Name))
                {
                    return true;
                }
            }
            return false;
        }

        // Reads to the following element with the given LocalName and NamespaceURI.
        public virtual bool ReadToFollowing(string localName, string namespaceURI)
        {
            if (localName == null || localName.Length == 0)
            {
                throw XmlConvert.CreateInvalidNameArgumentException(localName, nameof(localName));
            }
            if (namespaceURI == null)
            {
                throw new ArgumentNullException(nameof(namespaceURI));
            }

            // atomize local name and namespace
            localName = NameTable.Add(localName);
            namespaceURI = NameTable.Add(namespaceURI);

            // find following element with that name
            while (Read())
            {
                if (NodeType == XmlNodeType.Element && Ref.Equal(localName, LocalName) && Ref.Equal(namespaceURI, NamespaceURI))
                {
                    return true;
                }
            }
            return false;
        }

        // Reads to the first descendant of the current element with the given Name.
        public virtual bool ReadToDescendant(string name)
        {
            if (name == null || name.Length == 0)
            {
                throw XmlConvert.CreateInvalidNameArgumentException(name, nameof(name));
            }
            // save the element or root depth
            int parentDepth = Depth;
            if (NodeType != XmlNodeType.Element)
            {
                // adjust the depth if we are on root node
                if (ReadState == ReadState.Initial)
                {
                    Debug.Assert(parentDepth == 0);
                    parentDepth--;
                }
                else
                {
                    return false;
                }
            }
            else if (IsEmptyElement)
            {
                return false;
            }

            // atomize name
            name = NameTable.Add(name);

            // find the descendant
            while (Read() && Depth > parentDepth)
            {
                if (NodeType == XmlNodeType.Element && Ref.Equal(name, Name))
                {
                    return true;
                }
            }
            Debug.Assert(NodeType == XmlNodeType.EndElement || NodeType == XmlNodeType.None || ReadState == ReadState.Error);
            return false;
        }

        // Reads to the first descendant of the current element with the given LocalName and NamespaceURI.
        public virtual bool ReadToDescendant(string localName, string namespaceURI)
        {
            if (localName == null || localName.Length == 0)
            {
                throw XmlConvert.CreateInvalidNameArgumentException(localName, nameof(localName));
            }
            if (namespaceURI == null)
            {
                throw new ArgumentNullException(nameof(namespaceURI));
            }
            // save the element or root depth
            int parentDepth = Depth;
            if (NodeType != XmlNodeType.Element)
            {
                // adjust the depth if we are on root node
                if (ReadState == ReadState.Initial)
                {
                    Debug.Assert(parentDepth == 0);
                    parentDepth--;
                }
                else
                {
                    return false;
                }
            }
            else if (IsEmptyElement)
            {
                return false;
            }

            // atomize local name and namespace
            localName = NameTable.Add(localName);
            namespaceURI = NameTable.Add(namespaceURI);

            // find the descendant
            while (Read() && Depth > parentDepth)
            {
                if (NodeType == XmlNodeType.Element && Ref.Equal(localName, LocalName) && Ref.Equal(namespaceURI, NamespaceURI))
                {
                    return true;
                }
            }
            Debug.Assert(NodeType == XmlNodeType.EndElement);
            return false;
        }

        // Reads to the next sibling of the current element with the given Name.
        public virtual bool ReadToNextSibling(string name)
        {
            if (name == null || name.Length == 0)
            {
                throw XmlConvert.CreateInvalidNameArgumentException(name, nameof(name));
            }

            // atomize name
            name = NameTable.Add(name);

            // find the next sibling
            XmlNodeType nt;
            do
            {
                if (!SkipSubtree())
                {
                    break;
                }
                nt = NodeType;
                if (nt == XmlNodeType.Element && Ref.Equal(name, Name))
                {
                    return true;
                }
            } while (nt != XmlNodeType.EndElement && !EOF);
            return false;
        }

        // Reads to the next sibling of the current element with the given LocalName and NamespaceURI.
        public virtual bool ReadToNextSibling(string localName, string namespaceURI)
        {
            if (localName == null || localName.Length == 0)
            {
                throw XmlConvert.CreateInvalidNameArgumentException(localName, nameof(localName));
            }
            if (namespaceURI == null)
            {
                throw new ArgumentNullException(nameof(namespaceURI));
            }

            // atomize local name and namespace
            localName = NameTable.Add(localName);
            namespaceURI = NameTable.Add(namespaceURI);

            // find the next sibling
            XmlNodeType nt;
            do
            {
                if (!SkipSubtree())
                {
                    break;
                }
                nt = NodeType;
                if (nt == XmlNodeType.Element && Ref.Equal(localName, LocalName) && Ref.Equal(namespaceURI, NamespaceURI))
                {
                    return true;
                }
            } while (nt != XmlNodeType.EndElement && !EOF);
            return false;
        }

        // Returns true if the given argument is a valid Name.
        public static bool IsName(string str)
        {
            if (str == null)
            {
                throw new NullReferenceException();
            }
            return ValidateNames.IsNameNoNamespaces(str);
        }

        // Returns true if the given argument is a valid NmToken.
        public static bool IsNameToken(string str)
        {
            if (str == null)
            {
                throw new NullReferenceException();
            }
            return ValidateNames.IsNmtokenNoNamespaces(str);
        }

        // Returns the inner content (including markup) of an element or attribute as a string.
        public virtual string ReadInnerXml()
        {
            if (ReadState != ReadState.Interactive)
            {
                return string.Empty;
            }
            if ((this.NodeType != XmlNodeType.Attribute) && (this.NodeType != XmlNodeType.Element))
            {
                Read();
                return string.Empty;
            }

            StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
            XmlWriter xtw = CreateWriterForInnerOuterXml(sw);

            try
            {
                if (this.NodeType == XmlNodeType.Attribute)
                {
                    ((XmlTextWriter)xtw).QuoteChar = this.QuoteChar;
                    WriteAttributeValue(xtw);
                }
                if (this.NodeType == XmlNodeType.Element)
                {
                    this.WriteNode(xtw, false);
                }
            }
            finally
            {
                xtw.Close();
            }
            return sw.ToString();
        }

        // Writes the content (inner XML) of the current node into the provided XmlWriter.
        private void WriteNode(XmlWriter xtw, bool defattr)
        {
            Debug.Assert(xtw is XmlTextWriter);
            int d = this.NodeType == XmlNodeType.None ? -1 : this.Depth;
            while (this.Read() && (d < this.Depth))
            {
                switch (this.NodeType)
                {
                    case XmlNodeType.Element:
                        xtw.WriteStartElement(this.Prefix, this.LocalName, this.NamespaceURI);
                        ((XmlTextWriter)xtw).QuoteChar = this.QuoteChar;
                        xtw.WriteAttributes(this, defattr);
                        if (this.IsEmptyElement)
                        {
                            xtw.WriteEndElement();
                        }
                        break;
                    case XmlNodeType.Text:
                        xtw.WriteString(this.Value);
                        break;
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        xtw.WriteWhitespace(this.Value);
                        break;
                    case XmlNodeType.CDATA:
                        xtw.WriteCData(this.Value);
                        break;
                    case XmlNodeType.EntityReference:
                        xtw.WriteEntityRef(this.Name);
                        break;
                    case XmlNodeType.XmlDeclaration:
                    case XmlNodeType.ProcessingInstruction:
                        xtw.WriteProcessingInstruction(this.Name, this.Value);
                        break;
                    case XmlNodeType.DocumentType:
                        xtw.WriteDocType(this.Name, this.GetAttribute("PUBLIC"), this.GetAttribute("SYSTEM"), this.Value);
                        break;
                    case XmlNodeType.Comment:
                        xtw.WriteComment(this.Value);
                        break;
                    case XmlNodeType.EndElement:
                        xtw.WriteFullEndElement();
                        break;
                }
            }
            if (d == this.Depth && this.NodeType == XmlNodeType.EndElement)
            {
                Read();
            }
        }

        // Writes the attribute into the provided XmlWriter.
        private void WriteAttributeValue(XmlWriter xtw)
        {
            string attrName = this.Name;
            while (ReadAttributeValue())
            {
                if (this.NodeType == XmlNodeType.EntityReference)
                {
                    xtw.WriteEntityRef(this.Name);
                }
                else
                {
                    xtw.WriteString(this.Value);
                }
            }
            this.MoveToAttribute(attrName);
        }

        // Returns the current element and its descendants or an attribute as a string.
        public virtual string ReadOuterXml()
        {
            if (ReadState != ReadState.Interactive)
            {
                return string.Empty;
            }
            if ((this.NodeType != XmlNodeType.Attribute) && (this.NodeType != XmlNodeType.Element))
            {
                Read();
                return string.Empty;
            }

            StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
            XmlWriter xtw = CreateWriterForInnerOuterXml(sw);

            try
            {
                if (this.NodeType == XmlNodeType.Attribute)
                {
                    xtw.WriteStartAttribute(this.Prefix, this.LocalName, this.NamespaceURI);
                    WriteAttributeValue(xtw);
                    xtw.WriteEndAttribute();
                }
                else
                {
                    xtw.WriteNode(this, false);
                }
            }
            finally
            {
                xtw.Close();
            }
            return sw.ToString();
        }

        private XmlWriter CreateWriterForInnerOuterXml(StringWriter sw)
        {
            XmlTextWriter w = new XmlTextWriter(sw);
            // This is a V1 hack; we can put a custom implementation of ReadOuterXml on XmlTextReader/XmlValidatingReader
            SetNamespacesFlag(w);
            return w;
        }

        private void SetNamespacesFlag(XmlTextWriter xtw)
        {
            XmlTextReader tr = this as XmlTextReader;
            if (tr != null)
            {
                xtw.Namespaces = tr.Namespaces;
            }
            else
            {
#pragma warning disable 618
                XmlValidatingReader vr = this as XmlValidatingReader;
                if (vr != null)
                {
                    xtw.Namespaces = vr.Namespaces;
                }
            }
#pragma warning restore 618
        }

        // Returns an XmlReader that will read only the current element and its descendants and then go to EOF state.
        public virtual XmlReader ReadSubtree()
        {
            if (NodeType != XmlNodeType.Element)
            {
                throw new InvalidOperationException(SR.Xml_ReadSubtreeNotOnElement);
            }
            return new XmlSubtreeReader(this);
        }

        // Returns true when the current node has any attributes.
        public virtual bool HasAttributes
        {
            get
            {
                return AttributeCount > 0;
            }
        }

        //
        // IDisposable interface
        //
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && ReadState != ReadState.Closed)
            {
                Close();
            }
        }

        //
        // Internal methods
        //
        // Validation support
        internal virtual XmlNamespaceManager NamespaceManager
        {
            get
            {
                return null;
            }
        }

        internal static bool IsTextualNode(XmlNodeType nodeType)
        {
#if DEBUG
            // This code verifies IsTextualNodeBitmap mapping of XmlNodeType to a bool specifying
            // whether the node is 'textual' = Text, CDATA, Whitespace or SignificantWhitespace.
            Debug.Assert(0 == (s_isTextualNodeBitmap & (1 << (int)XmlNodeType.None)));
            Debug.Assert(0 == (s_isTextualNodeBitmap & (1 << (int)XmlNodeType.Element)));
            Debug.Assert(0 == (s_isTextualNodeBitmap & (1 << (int)XmlNodeType.Attribute)));
            Debug.Assert(0 != (s_isTextualNodeBitmap & (1 << (int)XmlNodeType.Text)));
            Debug.Assert(0 != (s_isTextualNodeBitmap & (1 << (int)XmlNodeType.CDATA)));
            Debug.Assert(0 == (s_isTextualNodeBitmap & (1 << (int)XmlNodeType.EntityReference)));
            Debug.Assert(0 == (s_isTextualNodeBitmap & (1 << (int)XmlNodeType.Entity)));
            Debug.Assert(0 == (s_isTextualNodeBitmap & (1 << (int)XmlNodeType.ProcessingInstruction)));
            Debug.Assert(0 == (s_isTextualNodeBitmap & (1 << (int)XmlNodeType.Comment)));
            Debug.Assert(0 == (s_isTextualNodeBitmap & (1 << (int)XmlNodeType.Document)));
            Debug.Assert(0 == (s_isTextualNodeBitmap & (1 << (int)XmlNodeType.DocumentType)));
            Debug.Assert(0 == (s_isTextualNodeBitmap & (1 << (int)XmlNodeType.DocumentFragment)));
            Debug.Assert(0 == (s_isTextualNodeBitmap & (1 << (int)XmlNodeType.Notation)));
            Debug.Assert(0 != (s_isTextualNodeBitmap & (1 << (int)XmlNodeType.Whitespace)));
            Debug.Assert(0 != (s_isTextualNodeBitmap & (1 << (int)XmlNodeType.SignificantWhitespace)));
            Debug.Assert(0 == (s_isTextualNodeBitmap & (1 << (int)XmlNodeType.EndElement)));
            Debug.Assert(0 == (s_isTextualNodeBitmap & (1 << (int)XmlNodeType.EndEntity)));
            Debug.Assert(0 == (s_isTextualNodeBitmap & (1 << (int)XmlNodeType.XmlDeclaration)));
#endif
            return 0 != (s_isTextualNodeBitmap & (1 << (int)nodeType));
        }

        internal static bool CanReadContentAs(XmlNodeType nodeType)
        {
#if DEBUG
            // This code verifies IsTextualNodeBitmap mapping of XmlNodeType to a bool specifying
            // whether ReadContentAsXxx calls are allowed on his node type
            Debug.Assert(0 == (s_canReadContentAsBitmap & (1 << (int)XmlNodeType.None)));
            Debug.Assert(0 == (s_canReadContentAsBitmap & (1 << (int)XmlNodeType.Element)));
            Debug.Assert(0 != (s_canReadContentAsBitmap & (1 << (int)XmlNodeType.Attribute)));
            Debug.Assert(0 != (s_canReadContentAsBitmap & (1 << (int)XmlNodeType.Text)));
            Debug.Assert(0 != (s_canReadContentAsBitmap & (1 << (int)XmlNodeType.CDATA)));
            Debug.Assert(0 != (s_canReadContentAsBitmap & (1 << (int)XmlNodeType.EntityReference)));
            Debug.Assert(0 == (s_canReadContentAsBitmap & (1 << (int)XmlNodeType.Entity)));
            Debug.Assert(0 != (s_canReadContentAsBitmap & (1 << (int)XmlNodeType.ProcessingInstruction)));
            Debug.Assert(0 != (s_canReadContentAsBitmap & (1 << (int)XmlNodeType.Comment)));
            Debug.Assert(0 == (s_canReadContentAsBitmap & (1 << (int)XmlNodeType.Document)));
            Debug.Assert(0 == (s_canReadContentAsBitmap & (1 << (int)XmlNodeType.DocumentType)));
            Debug.Assert(0 == (s_canReadContentAsBitmap & (1 << (int)XmlNodeType.DocumentFragment)));
            Debug.Assert(0 == (s_canReadContentAsBitmap & (1 << (int)XmlNodeType.Notation)));
            Debug.Assert(0 != (s_canReadContentAsBitmap & (1 << (int)XmlNodeType.Whitespace)));
            Debug.Assert(0 != (s_canReadContentAsBitmap & (1 << (int)XmlNodeType.SignificantWhitespace)));
            Debug.Assert(0 != (s_canReadContentAsBitmap & (1 << (int)XmlNodeType.EndElement)));
            Debug.Assert(0 != (s_canReadContentAsBitmap & (1 << (int)XmlNodeType.EndEntity)));
            Debug.Assert(0 == (s_canReadContentAsBitmap & (1 << (int)XmlNodeType.XmlDeclaration)));
#endif
            return 0 != (s_canReadContentAsBitmap & (1 << (int)nodeType));
        }

        internal static bool HasValueInternal(XmlNodeType nodeType)
        {
#if DEBUG
            // This code verifies HasValueBitmap mapping of XmlNodeType to a bool specifying
            // whether the node can have a non-empty Value
            Debug.Assert(0 == (s_hasValueBitmap & (1 << (int)XmlNodeType.None)));
            Debug.Assert(0 == (s_hasValueBitmap & (1 << (int)XmlNodeType.Element)));
            Debug.Assert(0 != (s_hasValueBitmap & (1 << (int)XmlNodeType.Attribute)));
            Debug.Assert(0 != (s_hasValueBitmap & (1 << (int)XmlNodeType.Text)));
            Debug.Assert(0 != (s_hasValueBitmap & (1 << (int)XmlNodeType.CDATA)));
            Debug.Assert(0 == (s_hasValueBitmap & (1 << (int)XmlNodeType.EntityReference)));
            Debug.Assert(0 == (s_hasValueBitmap & (1 << (int)XmlNodeType.Entity)));
            Debug.Assert(0 != (s_hasValueBitmap & (1 << (int)XmlNodeType.ProcessingInstruction)));
            Debug.Assert(0 != (s_hasValueBitmap & (1 << (int)XmlNodeType.Comment)));
            Debug.Assert(0 == (s_hasValueBitmap & (1 << (int)XmlNodeType.Document)));
            Debug.Assert(0 != (s_hasValueBitmap & (1 << (int)XmlNodeType.DocumentType)));
            Debug.Assert(0 == (s_hasValueBitmap & (1 << (int)XmlNodeType.DocumentFragment)));
            Debug.Assert(0 == (s_hasValueBitmap & (1 << (int)XmlNodeType.Notation)));
            Debug.Assert(0 != (s_hasValueBitmap & (1 << (int)XmlNodeType.Whitespace)));
            Debug.Assert(0 != (s_hasValueBitmap & (1 << (int)XmlNodeType.SignificantWhitespace)));
            Debug.Assert(0 == (s_hasValueBitmap & (1 << (int)XmlNodeType.EndElement)));
            Debug.Assert(0 == (s_hasValueBitmap & (1 << (int)XmlNodeType.EndEntity)));
            Debug.Assert(0 != (s_hasValueBitmap & (1 << (int)XmlNodeType.XmlDeclaration)));
#endif
            return 0 != (s_hasValueBitmap & (1 << (int)nodeType));
        }

        //
        // Private methods
        //
        //SkipSubTree is called whenever validation of the skipped subtree is required on a reader with XsdValidation
        private bool SkipSubtree()
        {
            MoveToElement();
            if (NodeType == XmlNodeType.Element && !IsEmptyElement)
            {
                int depth = Depth;

                while (Read() && depth < Depth)
                {
                    // Nothing, just read on
                }

                // consume end tag
                if (NodeType == XmlNodeType.EndElement)
                    return Read();
            }
            else
            {
                return Read();
            }

            return false;
        }

        internal void CheckElement(string localName, string namespaceURI)
        {
            if (localName == null || localName.Length == 0)
            {
                throw XmlConvert.CreateInvalidNameArgumentException(localName, nameof(localName));
            }
            if (namespaceURI == null)
            {
                throw new ArgumentNullException(nameof(namespaceURI));
            }
            if (NodeType != XmlNodeType.Element)
            {
                throw new XmlException(SR.Xml_InvalidNodeType, this.NodeType.ToString(), this as IXmlLineInfo);
            }
            if (LocalName != localName || NamespaceURI != namespaceURI)
            {
                throw new XmlException(SR.Xml_ElementNotFoundNs, new string[2] { localName, namespaceURI }, this as IXmlLineInfo);
            }
        }

        internal Exception CreateReadContentAsException(string methodName)
        {
            return CreateReadContentAsException(methodName, NodeType, this as IXmlLineInfo);
        }

        internal Exception CreateReadElementContentAsException(string methodName)
        {
            return CreateReadElementContentAsException(methodName, NodeType, this as IXmlLineInfo);
        }

        internal bool CanReadContentAs()
        {
            return CanReadContentAs(this.NodeType);
        }

        internal static Exception CreateReadContentAsException(string methodName, XmlNodeType nodeType, IXmlLineInfo lineInfo)
        {
            return new InvalidOperationException(AddLineInfo(SR.Format(SR.Xml_InvalidReadContentAs, methodName, nodeType), lineInfo));
        }

        internal static Exception CreateReadElementContentAsException(string methodName, XmlNodeType nodeType, IXmlLineInfo lineInfo)
        {
            return new InvalidOperationException(AddLineInfo(SR.Format(SR.Xml_InvalidReadElementContentAs, methodName, nodeType), lineInfo));
        }

        private static string AddLineInfo(string message, IXmlLineInfo lineInfo)
        {
            if (lineInfo != null)
            {
                string[] lineArgs = new string[2];
                lineArgs[0] = lineInfo.LineNumber.ToString(CultureInfo.InvariantCulture);
                lineArgs[1] = lineInfo.LinePosition.ToString(CultureInfo.InvariantCulture);
                message += " " + SR.Format(SR.Xml_ErrorPosition, lineArgs);
            }
            return message;
        }

        internal string InternalReadContentAsString()
        {
            string value = string.Empty;
            StringBuilder sb = null;
            do
            {
                switch (this.NodeType)
                {
                    case XmlNodeType.Attribute:
                        return this.Value;
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.CDATA:
                        // merge text content
                        if (value.Length == 0)
                        {
                            value = this.Value;
                        }
                        else
                        {
                            if (sb == null)
                            {
                                sb = new StringBuilder();
                                sb.Append(value);
                            }
                            sb.Append(this.Value);
                        }
                        break;
                    case XmlNodeType.ProcessingInstruction:
                    case XmlNodeType.Comment:
                    case XmlNodeType.EndEntity:
                        // skip comments, pis and end entity nodes
                        break;
                    case XmlNodeType.EntityReference:
                        if (this.CanResolveEntity)
                        {
                            this.ResolveEntity();
                            break;
                        }
                        goto default;
                    case XmlNodeType.EndElement:
                    default:
                        goto ReturnContent;
                }
            } while ((this.AttributeCount != 0) ? this.ReadAttributeValue() : this.Read());

        ReturnContent:
            return (sb == null) ? value : sb.ToString();
        }

        private bool SetupReadElementContentAsXxx(string methodName)
        {
            if (this.NodeType != XmlNodeType.Element)
            {
                throw CreateReadElementContentAsException(methodName);
            }

            bool isEmptyElement = this.IsEmptyElement;

            // move to content or beyond the empty element
            this.Read();

            if (isEmptyElement)
            {
                return false;
            }

            XmlNodeType nodeType = this.NodeType;
            if (nodeType == XmlNodeType.EndElement)
            {
                this.Read();
                return false;
            }
            else if (nodeType == XmlNodeType.Element)
            {
                throw new XmlException(SR.Xml_MixedReadElementContentAs, string.Empty, this as IXmlLineInfo);
            }
            return true;
        }

        private void FinishReadElementContentAsXxx()
        {
            if (this.NodeType != XmlNodeType.EndElement)
            {
                throw new XmlException(SR.Xml_InvalidNodeType, this.NodeType.ToString());
            }
            this.Read();
        }

        internal bool IsDefaultInternal
        {
            get
            {
                if (this.IsDefault)
                {
                    return true;
                }
                IXmlSchemaInfo schemaInfo = this.SchemaInfo;
                if (schemaInfo != null && schemaInfo.IsDefault)
                {
                    return true;
                }
                return false;
            }
        }

        internal virtual IDtdInfo DtdInfo
        {
            get
            {
                return null;
            }
        }

        internal static ConformanceLevel GetV1ConformanceLevel(XmlReader reader)
        {
            XmlTextReaderImpl tri = GetXmlTextReaderImpl(reader);
            return tri != null ? tri.V1ComformanceLevel : ConformanceLevel.Document;
        }

        private static XmlTextReaderImpl GetXmlTextReaderImpl(XmlReader reader)
        {
            XmlTextReaderImpl tri = reader as XmlTextReaderImpl;
            if (tri != null)
            {
                return tri;
            }

            XmlTextReader tr = reader as XmlTextReader;
            if (tr != null)
            {
                return tr.Impl;
            }

            XmlValidatingReaderImpl vri = reader as XmlValidatingReaderImpl;
            if (vri != null)
            {
                return vri.ReaderImpl;
            }
#pragma warning disable 618
            XmlValidatingReader vr = reader as XmlValidatingReader;
#pragma warning restore 618
            if (vr != null)
            {
                return vr.Impl.ReaderImpl;
            }

            return null;
        }

        //
        // Static methods for creating readers
        //

        // Creates an XmlReader for parsing XML from the given Uri.
        public static XmlReader Create(string inputUri)
        {
            return XmlReader.Create(inputUri, (XmlReaderSettings)null, (XmlParserContext)null);
        }

        // Creates an XmlReader according to the settings for parsing XML from the given Uri.
        public static XmlReader Create(string inputUri, XmlReaderSettings settings)
        {
            return XmlReader.Create(inputUri, settings, (XmlParserContext)null);
        }

        // Creates an XmlReader according to the settings and parser context for parsing XML from the given Uri.
        public static XmlReader Create(string inputUri, XmlReaderSettings settings, XmlParserContext inputContext)
        {
            if (settings == null)
            {
                settings = new XmlReaderSettings();
            }
            return settings.CreateReader(inputUri, inputContext);
        }

        // Creates an XmlReader according for parsing XML from the given stream.
        public static XmlReader Create(Stream input)
        {
            return Create(input, (XmlReaderSettings)null, (string)string.Empty);
        }

        // Creates an XmlReader according to the settings for parsing XML from the given stream.
        public static XmlReader Create(Stream input, XmlReaderSettings settings)
        {
            return Create(input, settings, string.Empty);
        }

        // Creates an XmlReader according to the settings and base Uri for parsing XML from the given stream.
        public static XmlReader Create(Stream input, XmlReaderSettings settings, string baseUri)
        {
            if (settings == null)
            {
                settings = new XmlReaderSettings();
            }
            return settings.CreateReader(input, null, (string)baseUri, null);
        }

        // Creates an XmlReader according to the settings and parser context for parsing XML from the given stream.
        public static XmlReader Create(Stream input, XmlReaderSettings settings, XmlParserContext inputContext)
        {
            if (settings == null)
            {
                settings = new XmlReaderSettings();
            }
            return settings.CreateReader(input, null, (string)string.Empty, inputContext);
        }

        // Creates an XmlReader according for parsing XML from the given TextReader.
        public static XmlReader Create(TextReader input)
        {
            return Create(input, (XmlReaderSettings)null, (string)string.Empty);
        }

        // Creates an XmlReader according to the settings for parsing XML from the given TextReader.
        public static XmlReader Create(TextReader input, XmlReaderSettings settings)
        {
            return Create(input, settings, string.Empty);
        }

        // Creates an XmlReader according to the settings and baseUri for parsing XML from the given TextReader.
        public static XmlReader Create(TextReader input, XmlReaderSettings settings, string baseUri)
        {
            if (settings == null)
            {
                settings = new XmlReaderSettings();
            }
            return settings.CreateReader(input, baseUri, null);
        }

        // Creates an XmlReader according to the settings and parser context for parsing XML from the given TextReader.
        public static XmlReader Create(TextReader input, XmlReaderSettings settings, XmlParserContext inputContext)
        {
            if (settings == null)
            {
                settings = new XmlReaderSettings();
            }
            return settings.CreateReader(input, string.Empty, inputContext);
        }

        // Creates an XmlReader according to the settings wrapped over the given reader.
        public static XmlReader Create(XmlReader reader, XmlReaderSettings settings)
        {
            if (settings == null)
            {
                settings = new XmlReaderSettings();
            }
            return settings.CreateReader(reader);
        }

        // !!!!!!
        // NOTE: This method is called via reflection from System.Data.dll and from Analysis Services in Yukon. 
        // Do not change its signature without notifying the appropriate teams!
        // !!!!!!
#if UAPAOT
        public static XmlReader CreateSqlReader(Stream input, XmlReaderSettings settings, XmlParserContext inputContext)
#else
        internal static XmlReader CreateSqlReader(Stream input, XmlReaderSettings settings, XmlParserContext inputContext)
#endif
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            if (settings == null)
            {
                settings = new XmlReaderSettings();
            }

            XmlReader reader;

            // allocate byte buffer
            byte[] bytes = new byte[CalcBufferSize(input)];

            int byteCount = 0;
            int read;
            do
            {
                read = input.Read(bytes, byteCount, bytes.Length - byteCount);
                byteCount += read;
            } while (read > 0 && byteCount< 2);

            // create text or binary XML reader depenting on the stream first 2 bytes
            if (byteCount >= 2 && (bytes[0] == 0xdf && bytes[1] == 0xff))
            {
                if (inputContext != null)
                    throw new ArgumentException(SR.XmlBinary_NoParserContext, nameof(inputContext));
                reader = new XmlSqlBinaryReader(input, bytes, byteCount, string.Empty, settings.CloseInput, settings);
            }
            else
            {
                reader = new XmlTextReaderImpl(input, bytes, byteCount, settings, null, string.Empty, inputContext, settings.CloseInput);
            }

            // wrap with validating reader
            if (settings.ValidationType != ValidationType.None)
            {
                reader = settings.AddValidation(reader);
            }

            if (settings.Async)
            {
                reader = XmlAsyncCheckReader.CreateAsyncCheckWrapper(reader);
            }

            return reader;
        }

        internal static int CalcBufferSize(Stream input)
        {
            // determine the size of byte buffer
            int bufferSize = DefaultBufferSize;
            if (input.CanSeek)
            {
                long len = input.Length;
                if (len < bufferSize)
                {
                    bufferSize = checked((int)len);
                }
                else if (len > MaxStreamLengthForDefaultBufferSize)
                {
                    bufferSize = BiggerBufferSize;
                }
            }

            // return the byte buffer size
            return bufferSize;
        }

        private object debuggerDisplayProxy { get { return new XmlReaderDebuggerDisplayProxy(this); } }

        [DebuggerDisplay("{ToString()}")]
        private struct XmlReaderDebuggerDisplayProxy
        {
            private XmlReader _reader;

            internal XmlReaderDebuggerDisplayProxy(XmlReader reader)
            {
                _reader = reader;
            }

            public override string ToString()
            {
                XmlNodeType nt = _reader.NodeType;
                string result = nt.ToString();
                switch (nt)
                {
                    case XmlNodeType.Element:
                    case XmlNodeType.EndElement:
                    case XmlNodeType.EntityReference:
                    case XmlNodeType.EndEntity:
                        result += ", Name=\"" + _reader.Name + '"';
                        break;
                    case XmlNodeType.Attribute:
                    case XmlNodeType.ProcessingInstruction:
                        result += ", Name=\"" + _reader.Name + "\", Value=\"" + XmlConvert.EscapeValueForDebuggerDisplay(_reader.Value) + '"';
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Comment:
                    case XmlNodeType.XmlDeclaration:
                    case XmlNodeType.CDATA:
                        result += ", Value=\"" + XmlConvert.EscapeValueForDebuggerDisplay(_reader.Value) + '"';
                        break;
                    case XmlNodeType.DocumentType:
                        result += ", Name=\"" + _reader.Name + "'";
                        result += ", SYSTEM=\"" + _reader.GetAttribute("SYSTEM") + '"';
                        result += ", PUBLIC=\"" + _reader.GetAttribute("PUBLIC") + '"';
                        result += ", Value=\"" + XmlConvert.EscapeValueForDebuggerDisplay(_reader.Value) + '"';
                        break;
                }
                return result;
            }
        }
    }
}

