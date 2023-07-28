// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Security;
using System.Xml.Schema;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml
{
    internal sealed partial class XmlTextReaderImpl
    {
        //
        // ParsingState
        //
        // Parsing state (aka. scanner data) - holds parsing buffer and entity input data information
        private struct ParsingState
        {
            // character buffer
            internal char[] chars;
            internal int charPos;
            internal int charsUsed;
            internal Encoding? encoding;
            internal bool appendMode;

            // input stream & byte buffer
            internal Stream? stream;
            internal Decoder? decoder;
            internal byte[]? bytes;
            internal int bytePos;
            internal int bytesUsed;

            // input text reader
            internal TextReader? textReader;

            // current line number & position
            internal int lineNo;
            internal int lineStartPos;

            // base uri of the current entity
            internal string baseUriStr;
            internal Uri? baseUri;

            // eof flag of the entity
            internal bool isEof;
            internal bool isStreamEof;

            // entity type & id
            internal IDtdEntityInfo? entity;
            internal int entityId;

            // normalization
            internal bool eolNormalized;

            // EndEntity reporting
            internal bool entityResolvedManually;

            internal void Clear()
            {
                chars = null!;
                charPos = 0;
                charsUsed = 0;
                encoding = null;
                stream = null;
                decoder = null;
                bytes = null;
                bytePos = 0;
                bytesUsed = 0;
                textReader = null;
                lineNo = 1;
                lineStartPos = -1;
                baseUriStr = string.Empty;
                baseUri = null;
                isEof = false;
                isStreamEof = false;
                eolNormalized = true;
                entityResolvedManually = false;
            }

            internal void Close(bool closeInput)
            {
                if (closeInput)
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }
                    else
                    {
                        textReader?.Dispose();
                    }
                }
            }

            internal int LineNo
            {
                get
                {
                    return lineNo;
                }
            }

            internal int LinePos
            {
                get
                {
                    return charPos - lineStartPos;
                }
            }
        }

        //
        // XmlContext
        //
        private sealed class XmlContext
        {
            internal XmlSpace xmlSpace;
            internal string xmlLang;
            internal string defaultNamespace;
            internal XmlContext? previousContext;

            internal XmlContext()
            {
                xmlSpace = XmlSpace.None;
                xmlLang = string.Empty;
                defaultNamespace = string.Empty;
                previousContext = null;
            }

            internal XmlContext(XmlContext previousContext)
            {
                this.xmlSpace = previousContext.xmlSpace;
                this.xmlLang = previousContext.xmlLang;
                this.defaultNamespace = previousContext.defaultNamespace;
                this.previousContext = previousContext;
            }
        }

        //
        // NoNamespaceManager
        //
        private sealed class NoNamespaceManager : XmlNamespaceManager
        {
            public NoNamespaceManager() : base() { }
            public override string DefaultNamespace { get { return string.Empty; } }
            public override void PushScope() { }
            public override bool PopScope() { return false; }
            public override void AddNamespace(string prefix, string uri) { }
            public override void RemoveNamespace(string prefix, string uri) { }
            public override IEnumerator GetEnumerator() { return null!; }
            public override IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope) { return null!; }
            public override string LookupNamespace(string? prefix) { return string.Empty; }
            public override string? LookupPrefix(string uri) { return null; }
            public override bool HasNamespace(string prefix) { return false; }
        }

        //
        // DtdParserProxy: IDtdParserAdapter proxy for XmlTextReaderImpl
        //
        internal sealed partial class DtdParserProxy : IDtdParserAdapterV1
        {
            // Fields
            private readonly XmlTextReaderImpl _reader;

            // Constructors
            internal DtdParserProxy(XmlTextReaderImpl reader)
            {
                _reader = reader;
            }

            // IDtdParserAdapter proxies
            XmlNameTable IDtdParserAdapter.NameTable
            {
                get { return _reader.DtdParserProxy_NameTable; }
            }

            IXmlNamespaceResolver? IDtdParserAdapter.NamespaceResolver
            {
                get { return _reader.DtdParserProxy_NamespaceResolver; }
            }

            Uri? IDtdParserAdapter.BaseUri
            {
                // SxS: DtdParserProxy_BaseUri property on the reader may expose machine scope resources. This property
                // is just returning the value of the other property, so it may expose machine scope resource as well.
                get { return _reader.DtdParserProxy_BaseUri; }
            }

            bool IDtdParserAdapter.IsEof
            {
                get { return _reader.DtdParserProxy_IsEof; }
            }

            char[] IDtdParserAdapter.ParsingBuffer
            {
                get { return _reader.DtdParserProxy_ParsingBuffer; }
            }

            int IDtdParserAdapter.ParsingBufferLength
            {
                get { return _reader.DtdParserProxy_ParsingBufferLength; }
            }

            int IDtdParserAdapter.CurrentPosition
            {
                get { return _reader.DtdParserProxy_CurrentPosition; }
                set { _reader.DtdParserProxy_CurrentPosition = value; }
            }

            int IDtdParserAdapter.EntityStackLength
            {
                get { return _reader.DtdParserProxy_EntityStackLength; }
            }

            bool IDtdParserAdapter.IsEntityEolNormalized
            {
                get { return _reader.DtdParserProxy_IsEntityEolNormalized; }
            }

            void IDtdParserAdapter.OnNewLine(int pos)
            {
                _reader.DtdParserProxy_OnNewLine(pos);
            }

            int IDtdParserAdapter.LineNo
            {
                get { return _reader.DtdParserProxy_LineNo; }
            }

            int IDtdParserAdapter.LineStartPosition
            {
                get { return _reader.DtdParserProxy_LineStartPosition; }
            }

            int IDtdParserAdapter.ReadData()
            {
                return _reader.DtdParserProxy_ReadData();
            }

            int IDtdParserAdapter.ParseNumericCharRef(StringBuilder? internalSubsetBuilder)
            {
                return _reader.DtdParserProxy_ParseNumericCharRef(internalSubsetBuilder);
            }

            int IDtdParserAdapter.ParseNamedCharRef(bool expand, StringBuilder? internalSubsetBuilder)
            {
                return _reader.DtdParserProxy_ParseNamedCharRef(expand, internalSubsetBuilder);
            }

            void IDtdParserAdapter.ParsePI(StringBuilder? sb)
            {
                _reader.DtdParserProxy_ParsePI(sb);
            }

            void IDtdParserAdapter.ParseComment(StringBuilder? sb)
            {
                _reader.DtdParserProxy_ParseComment(sb);
            }

            bool IDtdParserAdapter.PushEntity(IDtdEntityInfo entity, out int entityId)
            {
                return _reader.DtdParserProxy_PushEntity(entity, out entityId);
            }

            bool IDtdParserAdapter.PopEntity(out IDtdEntityInfo? oldEntity, out int newEntityId)
            {
                return _reader.DtdParserProxy_PopEntity(out oldEntity, out newEntityId);
            }

            bool IDtdParserAdapter.PushExternalSubset(string? systemId, string? publicId)
            {
                return _reader.DtdParserProxy_PushExternalSubset(systemId, publicId);
            }

            void IDtdParserAdapter.PushInternalDtd(string baseUri, string internalDtd)
            {
                Debug.Assert(internalDtd != null);
                _reader.DtdParserProxy_PushInternalDtd(baseUri, internalDtd);
            }

            [DoesNotReturn]
            void IDtdParserAdapter.Throw(Exception e)
            {
                _reader.DtdParserProxy_Throw(e);
            }

            void IDtdParserAdapter.OnSystemId(string systemId, LineInfo keywordLineInfo, LineInfo systemLiteralLineInfo)
            {
                _reader.DtdParserProxy_OnSystemId(systemId, keywordLineInfo, systemLiteralLineInfo);
            }

            void IDtdParserAdapter.OnPublicId(string publicId, LineInfo keywordLineInfo, LineInfo publicLiteralLineInfo)
            {
                _reader.DtdParserProxy_OnPublicId(publicId, keywordLineInfo, publicLiteralLineInfo);
            }

            bool IDtdParserAdapterWithValidation.DtdValidation
            {
                get { return _reader.DtdParserProxy_DtdValidation; }
            }

            IValidationEventHandling? IDtdParserAdapterWithValidation.ValidationEventHandling
            {
                get { return _reader.DtdParserProxy_ValidationEventHandling; }
            }

            bool IDtdParserAdapterV1.Normalization
            {
                get { return _reader.DtdParserProxy_Normalization; }
            }

            bool IDtdParserAdapterV1.Namespaces
            {
                get { return _reader.DtdParserProxy_Namespaces; }
            }

            bool IDtdParserAdapterV1.V1CompatibilityMode
            {
                get { return _reader.DtdParserProxy_V1CompatibilityMode; }
            }
        }

        //
        // NodeData
        //
        private sealed class NodeData : IComparable
        {
            // static instance with no data - is used when XmlTextReader is closed
            private static volatile NodeData? s_None;

            // NOTE: Do not use this property for reference comparison. It may not be unique.
            internal static NodeData None =>
                // no locking; s_None is immutable so it's not a problem that it may get initialized more than once
                s_None ??= new NodeData();

            // type
            internal XmlNodeType type;

            // name
            internal string localName = null!;
            internal string prefix = null!;
            internal string? ns;
            internal string? nameWPrefix;

            // value:
            // value == null -> the value is kept in the 'chars' buffer starting at valueStartPos and valueLength long
            private string? _value;
            private char[]? _chars;
            private int _valueStartPos;
            private int _valueLength;

            // main line info
            internal LineInfo lineInfo;

            // second line info
            internal LineInfo lineInfo2;

            // quote char for attributes
            internal char quoteChar;

            // depth
            internal int depth;

            // empty element / default attribute
            private bool _isEmptyOrDefault;

            // entity id
            internal int entityId;

            // helper members
            internal bool xmlContextPushed;

            // attribute value chunks
            internal NodeData? nextAttrValueChunk;

            // type info
            internal object? schemaType;
            internal object? typedValue;

            internal NodeData()
            {
                Clear(XmlNodeType.None);
                xmlContextPushed = false;
            }

            internal int LineNo
            {
                get
                {
                    return lineInfo.lineNo;
                }
            }

            internal int LinePos
            {
                get
                {
                    return lineInfo.linePos;
                }
            }

            internal bool IsEmptyElement
            {
                get
                {
                    return type == XmlNodeType.Element && _isEmptyOrDefault;
                }
                set
                {
                    Debug.Assert(type == XmlNodeType.Element);
                    _isEmptyOrDefault = value;
                }
            }

            internal bool IsDefaultAttribute
            {
                get
                {
                    return type == XmlNodeType.Attribute && _isEmptyOrDefault;
                }
                set
                {
                    Debug.Assert(type == XmlNodeType.Attribute);
                    _isEmptyOrDefault = value;
                }
            }

            internal bool ValueBuffered
            {
                get
                {
                    return _value == null;
                }
            }

            internal string StringValue
            {
                get
                {
                    Debug.Assert(_valueStartPos >= 0 || _value != null, "Value not ready.");

                    if (_value == null)
                    {
                        Debug.Assert(_chars != null);
                        _value = new string(_chars, _valueStartPos, _valueLength);
                    }
                    return _value;
                }
            }

            internal void TrimSpacesInValue()
            {
                if (ValueBuffered)
                {
                    Debug.Assert(_chars != null);
                    XmlTextReaderImpl.StripSpaces(_chars, _valueStartPos, ref _valueLength);
                }
                else
                {
                    Debug.Assert(_value != null);
                    _value = XmlTextReaderImpl.StripSpaces(_value);
                }
            }

            [MemberNotNull(nameof(_value))]
            [MemberNotNull(nameof(nameWPrefix))]
            [MemberNotNull(nameof(localName))]
            [MemberNotNull(nameof(prefix))]
            [MemberNotNull(nameof(ns))]
            internal void Clear(XmlNodeType type)
            {
                this.type = type;
                ClearName();
                _value = string.Empty;
                _valueStartPos = -1;
                schemaType = null;
                typedValue = null;
            }

            [MemberNotNull(nameof(localName))]
            [MemberNotNull(nameof(prefix))]
            [MemberNotNull(nameof(ns))]
            [MemberNotNull(nameof(nameWPrefix))]
            internal void ClearName()
            {
                localName = string.Empty;
                prefix = string.Empty;
                ns = string.Empty;
                nameWPrefix = string.Empty;
            }

            internal void SetLineInfo(int lineNo, int linePos)
            {
                lineInfo.Set(lineNo, linePos);
            }

            internal void SetLineInfo2(int lineNo, int linePos)
            {
                lineInfo2.Set(lineNo, linePos);
            }

            internal void SetValueNode(XmlNodeType type, string value)
            {
                Debug.Assert(value != null);

                this.type = type;
                ClearName();
                _value = value;
                _valueStartPos = -1;
            }

            internal void SetValueNode(XmlNodeType type, char[] chars, int startPos, int len)
            {
                this.type = type;
                ClearName();

                _value = null;
                _chars = chars;
                _valueStartPos = startPos;
                _valueLength = len;
            }

            internal void SetNamedNode(XmlNodeType type, string localName)
            {
                SetNamedNode(type, localName, string.Empty, localName);
            }

            internal void SetNamedNode(XmlNodeType type, string localName, string prefix, string? nameWPrefix)
            {
                Debug.Assert(localName != null);
                Debug.Assert(localName.Length > 0);

                this.type = type;
                this.localName = localName;
                this.prefix = prefix;
                this.nameWPrefix = nameWPrefix;
                this.ns = string.Empty;
                _value = string.Empty;
                _valueStartPos = -1;
            }

            internal void SetValue(string value)
            {
                _valueStartPos = -1;
                _value = value;
            }

            internal void SetValue(char[] chars, int startPos, int len)
            {
                _value = null;
                _chars = chars;
                _valueStartPos = startPos;
                _valueLength = len;
            }

            internal void OnBufferInvalidated()
            {
                if (_value == null)
                {
                    Debug.Assert(_valueStartPos != -1);
                    Debug.Assert(_chars != null);
                    _value = new string(_chars, _valueStartPos, _valueLength);
                }
                _valueStartPos = -1;
            }

            internal void CopyTo(int valueOffset, StringBuilder sb)
            {
                if (_value == null)
                {
                    Debug.Assert(_valueStartPos != -1);
                    Debug.Assert(_chars != null);
                    sb.Append(_chars, _valueStartPos + valueOffset, _valueLength - valueOffset);
                }
                else
                {
                    if (valueOffset <= 0)
                    {
                        sb.Append(_value);
                    }
                    else
                    {
                        sb.Append(_value, valueOffset, _value.Length - valueOffset);
                    }
                }
            }

            internal int CopyTo(int valueOffset, char[] buffer, int offset, int length)
            {
                if (_value == null)
                {
                    Debug.Assert(_valueStartPos != -1);
                    Debug.Assert(_chars != null);
                    int copyCount = _valueLength - valueOffset;
                    if (copyCount > length)
                    {
                        copyCount = length;
                    }
                    XmlTextReaderImpl.BlockCopyChars(_chars, _valueStartPos + valueOffset, buffer, offset, copyCount);
                    return copyCount;
                }
                else
                {
                    int copyCount = _value.Length - valueOffset;
                    if (copyCount > length)
                    {
                        copyCount = length;
                    }
                    _value.CopyTo(valueOffset, buffer, offset, copyCount);
                    return copyCount;
                }
            }

            internal int CopyToBinary(IncrementalReadDecoder decoder, int valueOffset)
            {
                if (_value == null)
                {
                    Debug.Assert(_valueStartPos != -1);
                    Debug.Assert(_chars != null);
                    return decoder.Decode(_chars, _valueStartPos + valueOffset, _valueLength - valueOffset);
                }
                else
                {
                    return decoder.Decode(_value, valueOffset, _value.Length - valueOffset);
                }
            }

            internal void AdjustLineInfo(int valueOffset, bool isNormalized, ref LineInfo lineInfo)
            {
                if (valueOffset == 0)
                {
                    return;
                }
                if (_valueStartPos != -1)
                {
                    Debug.Assert(_chars != null);
                    XmlTextReaderImpl.AdjustLineInfo(_chars, _valueStartPos, _valueStartPos + valueOffset, isNormalized, ref lineInfo);
                }
                else
                {
                    Debug.Assert(_value != null);
                    XmlTextReaderImpl.AdjustLineInfo(_value, 0, valueOffset, isNormalized, ref lineInfo);
                }
            }

            // This should be inlined by JIT compiler
            internal string GetNameWPrefix(XmlNameTable nt)
            {
                if (nameWPrefix != null)
                {
                    return nameWPrefix;
                }
                else
                {
                    return CreateNameWPrefix(nt);
                }
            }

            internal string CreateNameWPrefix(XmlNameTable nt)
            {
                Debug.Assert(nameWPrefix == null);
                if (prefix.Length == 0)
                {
                    nameWPrefix = localName;
                }
                else
                {
                    nameWPrefix = nt.Add($"{prefix}:{localName}");
                }
                return nameWPrefix;
            }

            int IComparable.CompareTo(object? obj)
            {
                NodeData? other = obj as NodeData;
                if (other != null)
                {
                    if (Ref.Equal(localName, other.localName))
                    {
                        if (Ref.Equal(ns, other.ns))
                        {
                            return 0;
                        }
                        else
                        {
                            return string.CompareOrdinal(ns, other.ns);
                        }
                    }
                    else
                    {
                        return string.CompareOrdinal(localName, other.localName);
                    }
                }
                else
                {
                    Debug.Fail("We should never get to this point.");
                    // 'other' is null, 'this' is not null. Always return 1, like "".CompareTo(null).
                    return 1;
                }
            }
        }

        //
        // DtdDefaultAttributeInfoToNodeDataComparer
        //
        // Compares IDtdDefaultAttributeInfo to NodeData
        private sealed class DtdDefaultAttributeInfoToNodeDataComparer : IComparer<object>
        {
            private static readonly IComparer<object> s_instance = new DtdDefaultAttributeInfoToNodeDataComparer();

            internal static IComparer<object> Instance
            {
                get { return s_instance; }
            }

            public int Compare(object? x, object? y)
            {
                Debug.Assert(x == null || x is NodeData || x is IDtdDefaultAttributeInfo);
                Debug.Assert(y == null || y is NodeData || y is IDtdDefaultAttributeInfo);

                string localName, localName2;
                string prefix, prefix2;

                if (x == null)
                {
                    return y == null ? 0 : -1;
                }
                else if (y == null)
                {
                    return 1;
                }

                NodeData? nodeData = x as NodeData;
                if (nodeData != null)
                {
                    localName = nodeData.localName;
                    prefix = nodeData.prefix;
                }
                else
                {
                    if (x is IDtdDefaultAttributeInfo attrDef)
                    {
                        localName = attrDef.LocalName;
                        prefix = attrDef.Prefix;
                    }
                    else
                    {
                        throw new XmlException(SR.Xml_DefaultException, string.Empty);
                    }
                }

                nodeData = y as NodeData;
                if (nodeData != null)
                {
                    localName2 = nodeData.localName;
                    prefix2 = nodeData.prefix;
                }
                else
                {
                    if (y is IDtdDefaultAttributeInfo attrDef)
                    {
                        localName2 = attrDef.LocalName;
                        prefix2 = attrDef.Prefix;
                    }
                    else
                    {
                        throw new XmlException(SR.Xml_DefaultException, string.Empty);
                    }
                }

                // string.Compare does reference euqality first for us, so we don't have to do it here
                int result = string.Compare(localName, localName2, StringComparison.Ordinal);
                if (result != 0)
                {
                    return result;
                }

                return string.Compare(prefix, prefix2, StringComparison.Ordinal);
            }
        }

        //
        // OnDefaultAttributeUse delegate
        //
        internal delegate void OnDefaultAttributeUseDelegate(IDtdDefaultAttributeInfo defaultAttribute, XmlTextReaderImpl coreReader);
    }
}
