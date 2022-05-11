// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal sealed class XmlJsonWriter : XmlDictionaryWriter, IXmlJsonWriterInitializer
    {
        private const char BACK_SLASH = '\\';
        private const char FORWARD_SLASH = '/';

        private const char HIGH_SURROGATE_START = (char)0xd800;
        private const char LOW_SURROGATE_END = (char)0xdfff;
        private const char MAX_CHAR = (char)0xfffe;
        private const char WHITESPACE = ' ';
        private const char CARRIAGE_RETURN = '\r';
        private const char NEWLINE = '\n';
        private const string xmlNamespace = "http://www.w3.org/XML/1998/namespace";
        private const string xmlnsNamespace = "http://www.w3.org/2000/xmlns/";

        // This array was part of a perf improvement for escaping characters < WHITESPACE.
        private static readonly string[] s_escapedJsonStringTable =
        {
            "\\u0000",
            "\\u0001",
            "\\u0002",
            "\\u0003",
            "\\u0004",
            "\\u0005",
            "\\u0006",
            "\\u0007",
            "\\b",
            "\\t",
            "\\n",
            "\\u000b",
            "\\f",
            "\\r",
            "\\u000e",
            "\\u000f",
            "\\u0010",
            "\\u0011",
            "\\u0012",
            "\\u0013",
            "\\u0014",
            "\\u0015",
            "\\u0016",
            "\\u0017",
            "\\u0018",
            "\\u0019",
            "\\u001a",
            "\\u001b",
            "\\u001c",
            "\\u001d",
            "\\u001e",
            "\\u001f"
        };

        private static BinHexEncoding? s_binHexEncoding;

        private string? _attributeText;
        private JsonDataType _dataType;
        private int _depth;
        private bool _endElementBuffer;
        private bool _isWritingDataTypeAttribute;
        private bool _isWritingServerTypeAttribute;
        private bool _isWritingXmlnsAttribute;
        private bool _isWritingXmlnsAttributeDefaultNs;
        private NameState _nameState;
        private JsonNodeType _nodeType;
        private JsonNodeWriter _nodeWriter = null!; // initialized in SetOutput
        private JsonNodeType[]? _scopes;
        private string? _serverTypeValue;
        // Do not use this field's value anywhere other than the WriteState property.
        // It's OK to set this field's value anywhere and then change the WriteState property appropriately.
        // If it's necessary to check the WriteState outside WriteState, use the WriteState property.
        private WriteState _writeState;
        private bool _wroteServerTypeAttribute;
        private readonly bool _indent;
        private readonly string? _indentChars;
        private int _indentLevel;

        public XmlJsonWriter() : this(false, null) { }

        public XmlJsonWriter(bool indent, string? indentChars)
        {
            _indent = indent;
            if (indent)
            {
                ArgumentNullException.ThrowIfNull(indentChars);
                _indentChars = indentChars;
            }
            InitializeWriter();
        }

        private enum JsonDataType
        {
            None,
            Null,
            Boolean,
            Number,
            String,
            Object,
            Array
        };

        [Flags]
        private enum NameState
        {
            None = 0,
            IsWritingNameWithMapping = 1,
            IsWritingNameAttribute = 2,
            WrittenNameWithMapping = 4,
        }

        public override XmlWriterSettings? Settings
        {
            // The XmlWriterSettings object used to create this writer instance.
            // If this writer was not created using the Create method, this property
            // returns a null reference.
            get { return null; }
        }

        public override WriteState WriteState
        {
            get
            {
                if (_writeState == WriteState.Closed)
                {
                    return WriteState.Closed;
                }
                if (HasOpenAttribute)
                {
                    return WriteState.Attribute;
                }
                switch (_nodeType)
                {
                    case JsonNodeType.None:
                        return WriteState.Start;
                    case JsonNodeType.Element:
                        return WriteState.Element;
                    case JsonNodeType.QuotedText:
                    case JsonNodeType.StandaloneText:
                    case JsonNodeType.EndElement:
                        return WriteState.Content;
                    default:
                        return WriteState.Error;
                }
            }
        }

        public override string? XmlLang
        {
            get { return null; }
        }

        public override XmlSpace XmlSpace
        {
            get { return XmlSpace.None; }
        }

        private static BinHexEncoding BinHexEncoding
        {
            get
            {
                if (s_binHexEncoding == null)
                {
                    s_binHexEncoding = new BinHexEncoding();
                }
                return s_binHexEncoding;
            }
        }

        private bool HasOpenAttribute => (_isWritingDataTypeAttribute || _isWritingServerTypeAttribute || IsWritingNameAttribute || _isWritingXmlnsAttribute);

        private bool IsClosed => (WriteState == WriteState.Closed);

        private bool IsWritingCollection => (_depth > 0) && (_scopes![_depth] == JsonNodeType.Collection);

        private bool IsWritingNameAttribute => (_nameState & NameState.IsWritingNameAttribute) == NameState.IsWritingNameAttribute;

        private bool IsWritingNameWithMapping => (_nameState & NameState.IsWritingNameWithMapping) == NameState.IsWritingNameWithMapping;

        private bool WrittenNameWithMapping => (_nameState & NameState.WrittenNameWithMapping) == NameState.WrittenNameWithMapping;

        public override void Close()
        {
            if (!IsClosed)
            {
                try
                {
                    WriteEndDocument();
                }
                finally
                {
                    try
                    {
                        _nodeWriter.Flush();
                        _nodeWriter.Close();
                    }
                    finally
                    {
                        _writeState = WriteState.Closed;
                        if (_depth != 0)
                        {
                            _depth = 0;
                        }
                    }
                }
            }

            base.Close();
        }

        public override void Flush()
        {
            if (IsClosed)
            {
                ThrowClosed();
            }
            _nodeWriter.Flush();
        }

        public override string? LookupPrefix(string ns)
        {
            ArgumentNullException.ThrowIfNull(ns);

            if (ns == Globals.XmlnsNamespace)
            {
                return Globals.XmlnsPrefix;
            }
            if (ns == xmlNamespace)
            {
                return JsonGlobals.xmlPrefix;
            }
            if (ns.Length == 0)
            {
                return string.Empty;
            }
            return null;
        }

        public void SetOutput(Stream stream, Encoding encoding, bool ownsStream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(encoding);

            if (encoding.WebName != Encoding.UTF8.WebName)
            {
                stream = new JsonEncodingStreamWrapper(stream, encoding, false);
            }
            else
            {
                encoding = null!;
            }
            if (_nodeWriter == null)
            {
                _nodeWriter = new JsonNodeWriter();
            }

            _nodeWriter.SetOutput(stream, ownsStream, encoding);
            InitializeWriter();
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, bool[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, short[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, int[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, long[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, float[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, double[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, decimal[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, DateTime[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, Guid[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, TimeSpan[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, bool[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, decimal[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, double[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, float[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, int[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, long[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, short[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, DateTime[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, Guid[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, TimeSpan[] array, int offset, int count)
        {
            throw new NotSupportedException(SR.JsonWriteArrayNotSupported);
        }

        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            // Not checking upper bound because it will be caught by "count".  This is what XmlTextWriter does.
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ValueMustBeNonNegative);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative);
            }
            if (count > buffer.Length - index)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.JsonSizeExceedsRemainingBufferSpace, buffer.Length - index));
            }

            StartText();
            _nodeWriter.WriteBase64Text(buffer, 0, buffer, index, count);
        }

        public override void WriteBinHex(byte[] buffer, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            // Not checking upper bound because it will be caught by "count".  This is what XmlTextWriter does.
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ValueMustBeNonNegative);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative);
            }
            if (count > buffer.Length - index)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.JsonSizeExceedsRemainingBufferSpace, buffer.Length - index));
            }

            StartText();
            WriteEscapedJsonString(BinHexEncoding.GetString(buffer, index, count));
        }

        public override void WriteCData(string? text)
        {
            WriteString(text);
        }

        public override void WriteCharEntity(char ch)
        {
            WriteString(ch.ToString());
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            // Not checking upper bound because it will be caught by "count".  This is what XmlTextWriter does.
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ValueMustBeNonNegative);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative);
            }
            if (count > buffer.Length - index)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.JsonSizeExceedsRemainingBufferSpace, buffer.Length - index));
            }

            WriteString(new string(buffer, index, count));
        }

        public override void WriteComment(string? text)
        {
            throw new NotSupportedException(SR.Format(SR.JsonMethodNotSupported, "WriteComment"));
        }

        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
        {
            throw new NotSupportedException(SR.Format(SR.JsonMethodNotSupported, "WriteDocType"));
        }

        public override void WriteEndAttribute()
        {
            if (IsClosed)
            {
                ThrowClosed();
            }
            if (!HasOpenAttribute)
            {
                throw new XmlException(SR.JsonNoMatchingStartAttribute);
            }

            Fx.Assert(!(_isWritingDataTypeAttribute && _isWritingServerTypeAttribute),
                "Can not write type attribute and __type attribute at the same time.");

            if (_isWritingDataTypeAttribute)
            {
                switch (_attributeText)
                {
                    case JsonGlobals.numberString:
                        {
                            ThrowIfServerTypeWritten(JsonGlobals.numberString);
                            _dataType = JsonDataType.Number;
                            break;
                        }
                    case JsonGlobals.stringString:
                        {
                            ThrowIfServerTypeWritten(JsonGlobals.stringString);
                            _dataType = JsonDataType.String;
                            break;
                        }
                    case JsonGlobals.arrayString:
                        {
                            ThrowIfServerTypeWritten(JsonGlobals.arrayString);
                            _dataType = JsonDataType.Array;
                            break;
                        }
                    case JsonGlobals.objectString:
                        {
                            _dataType = JsonDataType.Object;
                            break;
                        }
                    case JsonGlobals.nullString:
                        {
                            ThrowIfServerTypeWritten(JsonGlobals.nullString);
                            _dataType = JsonDataType.Null;
                            break;
                        }
                    case JsonGlobals.booleanString:
                        {
                            ThrowIfServerTypeWritten(JsonGlobals.booleanString);
                            _dataType = JsonDataType.Boolean;
                            break;
                        }
                    default:
                        throw new XmlException(SR.Format(SR.JsonUnexpectedAttributeValue, _attributeText));
                }

                _attributeText = null;
                _isWritingDataTypeAttribute = false;

                if (!IsWritingNameWithMapping || WrittenNameWithMapping)
                {
                    WriteDataTypeServerType();
                }
            }
            else if (_isWritingServerTypeAttribute)
            {
                _serverTypeValue = _attributeText;
                _attributeText = null;
                _isWritingServerTypeAttribute = false;

                // we are writing __type after type="object" (enforced by WSE)
                if ((!IsWritingNameWithMapping || WrittenNameWithMapping) && _dataType == JsonDataType.Object)
                {
                    WriteServerTypeAttribute();
                }
            }
            else if (IsWritingNameAttribute)
            {
                WriteJsonElementName(_attributeText!);
                _attributeText = null;
                _nameState = NameState.IsWritingNameWithMapping | NameState.WrittenNameWithMapping;
                WriteDataTypeServerType();
            }
            else if (_isWritingXmlnsAttribute)
            {
                if (!string.IsNullOrEmpty(_attributeText) && _isWritingXmlnsAttributeDefaultNs)
                {
                    throw new ArgumentException(SR.Format(SR.JsonNamespaceMustBeEmpty, _attributeText));
                }

                _attributeText = null;
                _isWritingXmlnsAttribute = false;
                _isWritingXmlnsAttributeDefaultNs = false;
            }
        }

        public override void WriteEndDocument()
        {
            if (IsClosed)
            {
                ThrowClosed();
            }
            if (_nodeType != JsonNodeType.None)
            {
                while (_depth > 0)
                {
                    WriteEndElement();
                }
            }
        }

        public override void WriteEndElement()
        {
            if (IsClosed)
            {
                ThrowClosed();
            }

            if (_depth == 0)
            {
                throw new XmlException(SR.JsonEndElementNoOpenNodes);
            }
            if (HasOpenAttribute)
            {
                throw new XmlException(SR.Format(SR.JsonOpenAttributeMustBeClosedFirst, "WriteEndElement"));
            }

            _endElementBuffer = false;

            JsonNodeType token = ExitScope();
            if (token == JsonNodeType.Collection)
            {
                _indentLevel--;
                if (_indent)
                {
                    if (_nodeType == JsonNodeType.Element)
                    {
                        _nodeWriter.WriteText(WHITESPACE);
                    }
                    else
                    {
                        WriteNewLine();
                        WriteIndent();
                    }
                }
                _nodeWriter.WriteText(JsonGlobals.EndCollectionChar);
                token = ExitScope();
            }
            else if (_nodeType == JsonNodeType.QuotedText)
            {
                // For writing "
                WriteJsonQuote();
            }
            else if (_nodeType == JsonNodeType.Element)
            {
                if ((_dataType == JsonDataType.None) && (_serverTypeValue != null))
                {
                    throw new XmlException(SR.Format(SR.JsonMustSpecifyDataType, JsonGlobals.typeString, JsonGlobals.objectString, JsonGlobals.serverTypeString));
                }

                if (IsWritingNameWithMapping && !WrittenNameWithMapping)
                {
                    // Ending </item> without writing item attribute
                    // Not providing a better error message because localization deadline has passed.
                    throw new XmlException(SR.Format(SR.JsonMustSpecifyDataType, JsonGlobals.itemString, string.Empty, JsonGlobals.itemString));
                }

                // the element is empty, it does not have any content,
                if ((_dataType == JsonDataType.None) ||
                    (_dataType == JsonDataType.String))
                {
                    _nodeWriter.WriteText(JsonGlobals.QuoteChar);
                    _nodeWriter.WriteText(JsonGlobals.QuoteChar);
                }
            }
            else
            {
                // Assert on only StandaloneText and EndElement because preceding if
                //    conditions take care of checking for QuotedText and Element.
                Fx.Assert((_nodeType == JsonNodeType.StandaloneText) || (_nodeType == JsonNodeType.EndElement),
                    "nodeType has invalid value " + _nodeType + ". Expected it to be QuotedText, Element, StandaloneText, or EndElement.");
            }
            if (_depth != 0)
            {
                if (token == JsonNodeType.Element)
                {
                    _endElementBuffer = true;
                }
                else if (token == JsonNodeType.Object)
                {
                    _indentLevel--;
                    if (_indent)
                    {
                        if (_nodeType == JsonNodeType.Element)
                        {
                            _nodeWriter.WriteText(WHITESPACE);
                        }
                        else
                        {
                            WriteNewLine();
                            WriteIndent();
                        }
                    }
                    _nodeWriter.WriteText(JsonGlobals.EndObjectChar);
                    if ((_depth > 0) && _scopes![_depth] == JsonNodeType.Element)
                    {
                        ExitScope();
                        _endElementBuffer = true;
                    }
                }
            }

            _dataType = JsonDataType.None;
            _nodeType = JsonNodeType.EndElement;
            _nameState = NameState.None;
            _wroteServerTypeAttribute = false;
        }

        public override void WriteEntityRef(string name)
        {
            throw new NotSupportedException(SR.Format(SR.JsonMethodNotSupported, "WriteEntityRef"));
        }

        public override void WriteFullEndElement()
        {
            WriteEndElement();
        }

        public override void WriteProcessingInstruction(string name, string? text)
        {
            if (IsClosed)
            {
                ThrowClosed();
            }

            if (!name.Equals("xml", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(SR.JsonXmlProcessingInstructionNotSupported, nameof(name));
            }

            if (WriteState != WriteState.Start)
            {
                throw new XmlException(SR.JsonXmlInvalidDeclaration);
            }
        }

        public override void WriteQualifiedName(string localName, string? ns)
        {
            ArgumentNullException.ThrowIfNull(localName);

            if (localName.Length == 0)
            {
                throw new ArgumentException(SR.JsonInvalidLocalNameEmpty, nameof(localName));
            }
            if (ns == null)
            {
                ns = string.Empty;
            }

            base.WriteQualifiedName(localName, ns);
        }

        public override void WriteRaw(string data)
        {
            WriteString(data);
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            // Not checking upper bound because it will be caught by "count".  This is what XmlTextWriter does.
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ValueMustBeNonNegative);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative);
            }
            if (count > buffer.Length - index)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.JsonSizeExceedsRemainingBufferSpace, buffer.Length - index));
            }

            WriteString(new string(buffer, index, count));
        }

        public override void WriteStartAttribute(string? prefix, string localName, string? ns)
        {
            if (IsClosed)
            {
                ThrowClosed();
            }
            if (!string.IsNullOrEmpty(prefix))
            {
                if (IsWritingNameWithMapping && prefix == JsonGlobals.xmlnsPrefix)
                {
                    if (ns != null && ns != xmlnsNamespace)
                    {
                        throw new ArgumentException(SR.Format(SR.XmlPrefixBoundToNamespace, "xmlns", xmlnsNamespace, ns), nameof(ns));
                    }
                }
                else
                {
                    throw new ArgumentException(SR.Format(SR.JsonPrefixMustBeNullOrEmpty, prefix), nameof(prefix));
                }
            }
            else
            {
                if (IsWritingNameWithMapping && ns == xmlnsNamespace && localName != JsonGlobals.xmlnsPrefix)
                {
                    prefix = JsonGlobals.xmlnsPrefix;
                }
            }
            if (!string.IsNullOrEmpty(ns))
            {
                if (IsWritingNameWithMapping && ns == xmlnsNamespace)
                {
                    prefix = JsonGlobals.xmlnsPrefix;
                }
                else if (string.IsNullOrEmpty(prefix) && localName == JsonGlobals.xmlnsPrefix && ns == xmlnsNamespace)
                {
                    prefix = JsonGlobals.xmlnsPrefix;
                    _isWritingXmlnsAttributeDefaultNs = true;
                }
                else
                {
                    throw new ArgumentException(SR.Format(SR.JsonNamespaceMustBeEmpty, ns), nameof(ns));
                }
            }
            ArgumentNullException.ThrowIfNull(localName);
            if (localName.Length == 0)
            {
                throw new ArgumentException(SR.JsonInvalidLocalNameEmpty, nameof(localName));
            }
            if ((_nodeType != JsonNodeType.Element) && !_wroteServerTypeAttribute)
            {
                throw new XmlException(SR.JsonAttributeMustHaveElement);
            }
            if (HasOpenAttribute)
            {
                throw new XmlException(SR.Format(SR.JsonOpenAttributeMustBeClosedFirst, "WriteStartAttribute"));
            }
            if (prefix == JsonGlobals.xmlnsPrefix)
            {
                _isWritingXmlnsAttribute = true;
            }
            else if (localName == JsonGlobals.typeString)
            {
                if (_dataType != JsonDataType.None)
                {
                    throw new XmlException(SR.Format(SR.JsonAttributeAlreadyWritten, JsonGlobals.typeString));
                }

                _isWritingDataTypeAttribute = true;
            }
            else if (localName == JsonGlobals.serverTypeString)
            {
                if (_serverTypeValue != null)
                {
                    throw new XmlException(SR.Format(SR.JsonAttributeAlreadyWritten, JsonGlobals.serverTypeString));
                }

                if ((_dataType != JsonDataType.None) && (_dataType != JsonDataType.Object))
                {
                    throw new XmlException(SR.Format(SR.JsonServerTypeSpecifiedForInvalidDataType,
                        JsonGlobals.serverTypeString, JsonGlobals.typeString, _dataType.ToString().ToLowerInvariant(), JsonGlobals.objectString));
                }

                _isWritingServerTypeAttribute = true;
            }
            else if (localName == JsonGlobals.itemString)
            {
                if (WrittenNameWithMapping)
                {
                    throw new XmlException(SR.Format(SR.JsonAttributeAlreadyWritten, JsonGlobals.itemString));
                }

                if (!IsWritingNameWithMapping)
                {
                    // Don't write attribute with local name "item" if <item> element is not open.
                    // Not providing a better error message because localization deadline has passed.
                    throw new XmlException(SR.JsonEndElementNoOpenNodes);
                }

                _nameState |= NameState.IsWritingNameAttribute;
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.JsonUnexpectedAttributeLocalName, localName), nameof(localName));
            }
        }

        public override void WriteStartDocument(bool standalone)
        {
            // In XML, writes the XML declaration with the version "1.0" and the standalone attribute.
            WriteStartDocument();
        }

        public override void WriteStartDocument()
        {
            // In XML, writes the XML declaration with the version "1.0".
            if (IsClosed)
            {
                ThrowClosed();
            }
            if (WriteState != WriteState.Start)
            {
                throw new XmlException(SR.Format(SR.JsonInvalidWriteState, "WriteStartDocument", WriteState.ToString()));
            }
        }

        public override void WriteStartElement(string? prefix, string localName, string? ns)
        {
            ArgumentNullException.ThrowIfNull(localName);

            if (localName.Length == 0)
            {
                throw new ArgumentException(SR.JsonInvalidLocalNameEmpty, nameof(localName));
            }
            if (!string.IsNullOrEmpty(prefix))
            {
                if (string.IsNullOrEmpty(ns) || !TrySetWritingNameWithMapping(localName, ns))
                {
                    throw new ArgumentException(SR.Format(SR.JsonPrefixMustBeNullOrEmpty, prefix), nameof(prefix));
                }
            }
            if (!string.IsNullOrEmpty(ns))
            {
                if (!TrySetWritingNameWithMapping(localName, ns))
                {
                    throw new ArgumentException(SR.Format(SR.JsonNamespaceMustBeEmpty, ns), nameof(ns));
                }
            }
            if (IsClosed)
            {
                ThrowClosed();
            }
            if (HasOpenAttribute)
            {
                throw new XmlException(SR.Format(SR.JsonOpenAttributeMustBeClosedFirst, "WriteStartElement"));
            }
            if ((_nodeType != JsonNodeType.None) && _depth == 0)
            {
                throw new XmlException(SR.JsonMultipleRootElementsNotAllowedOnWriter);
            }

            switch (_nodeType)
            {
                case JsonNodeType.None:
                    {
                        if (!localName.Equals(JsonGlobals.rootString))
                        {
                            throw new XmlException(SR.Format(SR.JsonInvalidRootElementName, localName, JsonGlobals.rootString));
                        }
                        EnterScope(JsonNodeType.Element);
                        break;
                    }
                case JsonNodeType.Element:
                    {
                        if ((_dataType != JsonDataType.Array) && (_dataType != JsonDataType.Object))
                        {
                            throw new XmlException(SR.JsonNodeTypeArrayOrObjectNotSpecified);
                        }
                        if (_indent)
                        {
                            WriteNewLine();
                            WriteIndent();
                        }
                        if (!IsWritingCollection)
                        {
                            if (_nameState != NameState.IsWritingNameWithMapping)
                            {
                                WriteJsonElementName(localName);
                            }
                        }
                        else if (!localName.Equals(JsonGlobals.itemString))
                        {
                            throw new XmlException(SR.Format(SR.JsonInvalidItemNameForArrayElement, localName, JsonGlobals.itemString));
                        }
                        EnterScope(JsonNodeType.Element);
                        break;
                    }
                case JsonNodeType.EndElement:
                    {
                        if (_endElementBuffer)
                        {
                            _nodeWriter.WriteText(JsonGlobals.MemberSeparatorChar);
                        }
                        if (_indent)
                        {
                            WriteNewLine();
                            WriteIndent();
                        }
                        if (!IsWritingCollection)
                        {
                            if (_nameState != NameState.IsWritingNameWithMapping)
                            {
                                WriteJsonElementName(localName);
                            }
                        }
                        else if (!localName.Equals(JsonGlobals.itemString))
                        {
                            throw new XmlException(SR.Format(SR.JsonInvalidItemNameForArrayElement, localName, JsonGlobals.itemString));
                        }
                        EnterScope(JsonNodeType.Element);
                        break;
                    }
                default:
                    throw new XmlException(SR.JsonInvalidStartElementCall);
            }

            _isWritingDataTypeAttribute = false;
            _isWritingServerTypeAttribute = false;
            _isWritingXmlnsAttribute = false;
            _wroteServerTypeAttribute = false;
            _serverTypeValue = null;
            _dataType = JsonDataType.None;
            _nodeType = JsonNodeType.Element;
        }

        public override void WriteString(string? text)
        {
            if (HasOpenAttribute && (text != null))
            {
                _attributeText += text;
            }
            else
            {
                if (text == null)
                {
                    text = string.Empty;
                }

                // do work only when not indenting whitespace
                if (!((_dataType == JsonDataType.Array || _dataType == JsonDataType.Object || _nodeType == JsonNodeType.EndElement) && XmlConverter.IsWhitespace(text)))
                {
                    StartText();
                    WriteEscapedJsonString(text);
                }
            }
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            WriteString(new string(stackalloc char[2] { highChar, lowChar }));
        }

        public override void WriteValue(bool value)
        {
            StartText();
            _nodeWriter.WriteBoolText(value);
        }

        public override void WriteValue(decimal value)
        {
            StartText();
            _nodeWriter.WriteDecimalText(value);
        }

        public override void WriteValue(double value)
        {
            StartText();
            _nodeWriter.WriteDoubleText(value);
        }

        public override void WriteValue(float value)
        {
            StartText();
            _nodeWriter.WriteFloatText(value);
        }

        public override void WriteValue(int value)
        {
            StartText();
            _nodeWriter.WriteInt32Text(value);
        }

        public override void WriteValue(long value)
        {
            StartText();
            _nodeWriter.WriteInt64Text(value);
        }

        public override void WriteValue(Guid value)
        {
            StartText();
            _nodeWriter.WriteGuidText(value);
        }

        public override void WriteValue(DateTime value)
        {
            StartText();
            _nodeWriter.WriteDateTimeText(value);
        }

        public override void WriteValue(string? value)
        {
            WriteString(value);
        }

        public override void WriteValue(TimeSpan value)
        {
            StartText();
            _nodeWriter.WriteTimeSpanText(value);
        }

        public override void WriteValue(UniqueId value)
        {
            ArgumentNullException.ThrowIfNull(value);

            StartText();
            _nodeWriter.WriteUniqueIdText(value);
        }

        public override void WriteValue(object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (IsClosed)
            {
                ThrowClosed();
            }

            if (value is Array)
            {
                WriteValue((Array)value);
            }
            else if (value is IStreamProvider)
            {
                WriteValue((IStreamProvider)value);
            }
            else
            {
                WritePrimitiveValue(value);
            }
        }

        public override void WriteWhitespace(string? ws)
        {
            if (IsClosed)
            {
                ThrowClosed();
            }
            ArgumentNullException.ThrowIfNull(ws);

            for (int i = 0; i < ws.Length; ++i)
            {
                char c = ws[i];
                if (c != ' ' &&
                    c != '\t' &&
                    c != '\n' &&
                    c != '\r')
                {
                    throw new ArgumentException(SR.Format(SR.JsonOnlyWhitespace, c.ToString(), "WriteWhitespace"), nameof(ws));
                }
            }

            WriteString(ws);
        }

        public override void WriteXmlAttribute(string localName, string? value)
        {
            throw new NotSupportedException(SR.Format(SR.JsonMethodNotSupported, "WriteXmlAttribute"));
        }

        public override void WriteXmlAttribute(XmlDictionaryString localName, XmlDictionaryString? value)
        {
            throw new NotSupportedException(SR.Format(SR.JsonMethodNotSupported, "WriteXmlAttribute"));
        }

        public override void WriteXmlnsAttribute(string? prefix, string namespaceUri)
        {
            if (!IsWritingNameWithMapping)
            {
                throw new NotSupportedException(SR.Format(SR.JsonMethodNotSupported, "WriteXmlnsAttribute"));
            }
        }

        public override void WriteXmlnsAttribute(string? prefix, XmlDictionaryString namespaceUri)
        {
            if (!IsWritingNameWithMapping)
            {
                throw new NotSupportedException(SR.Format(SR.JsonMethodNotSupported, "WriteXmlnsAttribute"));
            }
        }

        internal static bool CharacterNeedsEscaping(char ch)
        {
            return (ch == FORWARD_SLASH || ch == JsonGlobals.QuoteChar || ch < WHITESPACE || ch == BACK_SLASH
                || (ch >= HIGH_SURROGATE_START && (ch <= LOW_SURROGATE_END || ch >= MAX_CHAR)));
        }


        private static void ThrowClosed()
        {
            throw new InvalidOperationException(SR.JsonWriterClosed);
        }

        private void CheckText(JsonNodeType nextNodeType)
        {
            if (IsClosed)
            {
                ThrowClosed();
            }
            if (_depth == 0)
            {
                throw new InvalidOperationException(SR.XmlIllegalOutsideRoot);
            }

            if ((nextNodeType == JsonNodeType.StandaloneText) &&
                (_nodeType == JsonNodeType.QuotedText))
            {
                throw new XmlException(SR.JsonCannotWriteStandaloneTextAfterQuotedText);
            }
        }

        private void EnterScope(JsonNodeType currentNodeType)
        {
            _depth++;
            if (_scopes == null)
            {
                _scopes = new JsonNodeType[4];
            }
            else if (_scopes.Length == _depth)
            {
                JsonNodeType[] newScopes = new JsonNodeType[_depth * 2];
                Array.Copy(_scopes, newScopes, _depth);
                _scopes = newScopes;
            }
            _scopes[_depth] = currentNodeType;
        }

        private JsonNodeType ExitScope()
        {
            JsonNodeType nodeTypeToReturn = _scopes![_depth];
            _scopes[_depth] = JsonNodeType.None;
            _depth--;
            return nodeTypeToReturn;
        }

        private void InitializeWriter()
        {
            _nodeType = JsonNodeType.None;
            _dataType = JsonDataType.None;
            _isWritingDataTypeAttribute = false;
            _wroteServerTypeAttribute = false;
            _isWritingServerTypeAttribute = false;
            _serverTypeValue = null;
            _attributeText = null;

            if (_depth != 0)
            {
                _depth = 0;
            }
            if ((_scopes != null) && (_scopes.Length > JsonGlobals.maxScopeSize))
            {
                _scopes = null;
            }

            // Can't let writeState be at Closed if reinitializing.
            _writeState = WriteState.Start;
            _endElementBuffer = false;
            _indentLevel = 0;
        }

        private static bool IsUnicodeNewlineCharacter(char c)
        {
            // Newline characters in JSON strings need to be encoded on the way out (DevDiv #665974)
            // See Unicode 6.2, Table 5-1 (http://www.unicode.org/versions/Unicode6.2.0/ch05.pdf]) for the full list.

            // We only care about NEL, LS, and PS, since the other newline characters are all
            // control characters so are already encoded.
            return (c == '\u0085' || c == '\u2028' || c == '\u2029');
        }

        private void StartText()
        {
            if (HasOpenAttribute)
            {
                throw new InvalidOperationException(SR.JsonMustUseWriteStringForWritingAttributeValues);
            }

            if ((_dataType == JsonDataType.None) && (_serverTypeValue != null))
            {
                throw new XmlException(SR.Format(SR.JsonMustSpecifyDataType, JsonGlobals.typeString, JsonGlobals.objectString, JsonGlobals.serverTypeString));
            }

            if (IsWritingNameWithMapping && !WrittenNameWithMapping)
            {
                // Don't write out any text content unless the local name has been written.
                // Not providing a better error message because localization deadline has passed.
                throw new XmlException(SR.Format(SR.JsonMustSpecifyDataType, JsonGlobals.itemString, string.Empty, JsonGlobals.itemString));
            }

            if ((_dataType == JsonDataType.String) ||
                (_dataType == JsonDataType.None))
            {
                CheckText(JsonNodeType.QuotedText);
                if (_nodeType != JsonNodeType.QuotedText)
                {
                    WriteJsonQuote();
                }
                _nodeType = JsonNodeType.QuotedText;
            }
            else if ((_dataType == JsonDataType.Number) ||
                (_dataType == JsonDataType.Boolean))
            {
                CheckText(JsonNodeType.StandaloneText);
                _nodeType = JsonNodeType.StandaloneText;
            }
            else
            {
                ThrowInvalidAttributeContent();
            }
        }

        private void ThrowIfServerTypeWritten(string dataTypeSpecified)
        {
            if (_serverTypeValue != null)
            {
                throw new XmlException(SR.Format(SR.JsonInvalidDataTypeSpecifiedForServerType, JsonGlobals.typeString, dataTypeSpecified, JsonGlobals.serverTypeString, JsonGlobals.objectString));
            }
        }

        private void ThrowInvalidAttributeContent()
        {
            if (HasOpenAttribute)
            {
                throw new XmlException(SR.JsonInvalidMethodBetweenStartEndAttribute);
            }
            else
            {
                throw new XmlException(SR.Format(SR.JsonCannotWriteTextAfterNonTextAttribute, _dataType.ToString().ToLowerInvariant()));
            }
        }

        private bool TrySetWritingNameWithMapping(string localName, string ns)
        {
            if (localName.Equals(JsonGlobals.itemString) && ns.Equals(JsonGlobals.itemString))
            {
                _nameState = NameState.IsWritingNameWithMapping;
                return true;
            }
            return false;
        }

        private void WriteDataTypeServerType()
        {
            if (_dataType != JsonDataType.None)
            {
                switch (_dataType)
                {
                    case JsonDataType.Array:
                        {
                            EnterScope(JsonNodeType.Collection);
                            _nodeWriter.WriteText(JsonGlobals.CollectionChar);
                            _indentLevel++;
                            break;
                        }
                    case JsonDataType.Object:
                        {
                            EnterScope(JsonNodeType.Object);
                            _nodeWriter.WriteText(JsonGlobals.ObjectChar);
                            _indentLevel++;
                            break;
                        }
                    case JsonDataType.Null:
                        {
                            _nodeWriter.WriteText(JsonGlobals.nullString);
                            break;
                        }
                    default:
                        break;
                }

                if (_serverTypeValue != null)
                {
                    // dataType must be object because we throw in all other case.
                    WriteServerTypeAttribute();
                }
            }
        }

        private unsafe void WriteEscapedJsonString(string str)
        {
            fixed (char* chars = str)
            {
                int i = 0;
                int j;
                for (j = 0; j < str.Length; j++)
                {
                    char ch = chars[j];
                    if (ch <= FORWARD_SLASH)
                    {
                        if (ch == FORWARD_SLASH || ch == JsonGlobals.QuoteChar)
                        {
                            _nodeWriter.WriteChars(chars + i, j - i);
                            _nodeWriter.WriteText(BACK_SLASH);
                            _nodeWriter.WriteText(ch);
                            i = j + 1;
                        }
                        else if (ch < WHITESPACE)
                        {
                            _nodeWriter.WriteChars(chars + i, j - i);
                            _nodeWriter.WriteText(s_escapedJsonStringTable[ch]);
                            i = j + 1;
                        }
                    }
                    else if (ch == BACK_SLASH)
                    {
                        _nodeWriter.WriteChars(chars + i, j - i);
                        _nodeWriter.WriteText(BACK_SLASH);
                        _nodeWriter.WriteText(ch);
                        i = j + 1;
                    }
                    else if ((ch >= HIGH_SURROGATE_START && (ch <= LOW_SURROGATE_END || ch >= MAX_CHAR)) || IsUnicodeNewlineCharacter(ch))
                    {
                        _nodeWriter.WriteChars(chars + i, j - i);
                        _nodeWriter.WriteText(BACK_SLASH);
                        _nodeWriter.WriteText('u');
                        _nodeWriter.WriteText($"{(int)ch:x4}");
                        i = j + 1;
                    }
                }
                if (i < j)
                {
                    _nodeWriter.WriteChars(chars + i, j - i);
                }
            }
        }

        private void WriteIndent()
        {
            for (int i = 0; i < _indentLevel; i++)
            {
                _nodeWriter.WriteText(_indentChars!);
            }
        }

        private void WriteNewLine()
        {
            _nodeWriter.WriteText(CARRIAGE_RETURN);
            _nodeWriter.WriteText(NEWLINE);
        }

        private void WriteJsonElementName(string localName)
        {
            WriteJsonQuote();
            WriteEscapedJsonString(localName);
            WriteJsonQuote();
            _nodeWriter.WriteText(JsonGlobals.NameValueSeparatorChar);
            if (_indent)
            {
                _nodeWriter.WriteText(WHITESPACE);
            }
        }

        private void WriteJsonQuote()
        {
            _nodeWriter.WriteText(JsonGlobals.QuoteChar);
        }

        private void WritePrimitiveValue(object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (IsClosed)
            {
                ThrowClosed();
            }

            if (value is ulong)
            {
                WriteValue((ulong)value);
            }
            else if (value is string)
            {
                WriteValue((string)value);
            }
            else if (value is int)
            {
                WriteValue((int)value);
            }
            else if (value is long)
            {
                WriteValue((long)value);
            }
            else if (value is bool)
            {
                WriteValue((bool)value);
            }
            else if (value is double)
            {
                WriteValue((double)value);
            }
            else if (value is DateTime)
            {
                WriteValue((DateTime)value);
            }
            else if (value is float)
            {
                WriteValue((float)value);
            }
            else if (value is decimal)
            {
                WriteValue((decimal)value);
            }
            else if (value is XmlDictionaryString)
            {
                WriteValue((XmlDictionaryString)value);
            }
            else if (value is UniqueId)
            {
                WriteValue((UniqueId)value);
            }
            else if (value is Guid)
            {
                WriteValue((Guid)value);
            }
            else if (value is TimeSpan)
            {
                WriteValue((TimeSpan)value);
            }
            else if (value.GetType().IsArray)
            {
                throw new ArgumentException(SR.JsonNestedArraysNotSupported, nameof(value));
            }
            else
            {
                base.WriteValue(value);
            }
        }

        private void WriteServerTypeAttribute()
        {
            string? value = _serverTypeValue;
            JsonDataType oldDataType = _dataType;
            NameState oldNameState = _nameState;
            WriteStartElement(JsonGlobals.serverTypeString);
            WriteValue(value);
            WriteEndElement();
            _dataType = oldDataType;
            _nameState = oldNameState;
            _wroteServerTypeAttribute = true;
        }

        private void WriteValue(ulong value)
        {
            StartText();
            _nodeWriter.WriteUInt64Text(value);
        }

        private void WriteValue(Array array)
        {
            // This method is called only if WriteValue(object) is called with an array
            // The contract for XmlWriter.WriteValue(object) requires that this object array be written out as a string.
            // E.g. WriteValue(new int[] { 1, 2, 3}) should be equivalent to WriteString("1 2 3").
            JsonDataType oldDataType = _dataType;
            // Set attribute mode to String because WritePrimitiveValue might write numerical text.
            //  Calls to methods that write numbers can't be mixed with calls that write quoted text unless the attribute mode is explicitly string.
            _dataType = JsonDataType.String;
            StartText();
            for (int i = 0; i < array.Length; i++)
            {
                if (i != 0)
                {
                    _nodeWriter.WriteText(JsonGlobals.WhitespaceChar);
                }
                WritePrimitiveValue(array.GetValue(i)!);
            }
            _dataType = oldDataType;
        }

        private sealed class JsonNodeWriter : XmlUTF8NodeWriter
        {
            internal unsafe void WriteChars(char* chars, int charCount)
            {
                base.UnsafeWriteUTF8Chars(chars, charCount);
            }
        }
    }
}
