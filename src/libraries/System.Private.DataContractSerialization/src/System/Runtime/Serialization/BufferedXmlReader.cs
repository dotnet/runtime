// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Runtime.Serialization;

internal sealed class BufferedXmlReader : XmlDictionaryReader, IXmlLineInfo, IXmlNamespaceResolver, IXmlTextParser
{
    private sealed class Node
    {
        internal XmlNodeType Kind;
        internal string LocalName = string.Empty;
        internal string Namespace = string.Empty;
        internal string Prefix = string.Empty;
        internal bool NativeNames;
        internal bool NamespaceDeclaration;
        internal XmlDictionaryString? DictionaryLocalName;
        internal XmlDictionaryString? DictionaryNamespace;
        internal XmlDictionaryString? DictionaryValue;
        internal object? TypedValue;
        internal string? Text;
        internal Type ValueType = typeof(string);
        internal int Depth;
        internal bool Empty;
        internal bool Default;
        internal char Quote;
        internal string BaseUri = string.Empty;
        internal string XmlLang = string.Empty;
        internal XmlSpace XmlSpace;
        internal bool HasValue;
        internal int Line;
        internal int Column;
        internal bool HasLineInfo;
        internal IXmlSchemaInfo? SchemaInfo;
        internal Node[] Attributes = Array.Empty<Node>();
        internal IDictionary<string, string>? Namespaces;

        internal string GetText() => Text ??= ToLexical(TypedValue);
    }

    private readonly List<Node> _nodes;
    private readonly XmlDictionaryReaderQuotas _quotas;
    private readonly XmlReader _sourceReader;
    private XmlNameTable? _nameTable;
    private readonly XmlReaderSettings? _settings;
    private readonly bool _canReadBinaryContent;
    private readonly bool _canReadValueChunk;
    private int _index;
    private int _attributeIndex = -1;
    private bool _attributeValue;
    private bool _closed;
    private bool _valueAccessed;
    private string? _cachedValue;
    private int _valueOffset;
    private int _byteOffset;
    private bool _normalized;
    private WhitespaceHandling _whitespaceHandling;

    private BufferedXmlReader(XmlReaderDelegator source, List<Node> nodes)
    {
        _nodes = nodes;
        _quotas = (source.UnderlyingReader as XmlDictionaryReader)?.Quotas ?? XmlDictionaryReaderQuotas.Max;
        _sourceReader = source.UnderlyingReader;
        _settings = source.UnderlyingReader.Settings;
        _canReadBinaryContent = source.UnderlyingReader.CanReadBinaryContent;
        _canReadValueChunk = source.UnderlyingReader.CanReadValueChunk;
        _normalized = source.Normalized;
        _whitespaceHandling = source.WhitespaceHandling;
    }

    private Node? Element => !_closed && _index < _nodes.Count ? _nodes[_index] : null;
    private Node? Current => _attributeIndex >= 0 ? Element!.Attributes[_attributeIndex] : Element;
    private bool HasNativeNames => Current?.NativeNames ?? _sourceReader is XmlBaseReader;
    private string RawLocalName => _attributeValue && !HasNativeNames ? string.Empty : GetComparisonLocalName(Current);
    private string RawNamespace => _attributeValue && !HasNativeNames ? string.Empty : Current?.Namespace ?? string.Empty;
    private string RawPrefix => _attributeValue && !HasNativeNames ? string.Empty : Current?.Prefix ?? string.Empty;

    private static string GetComparisonLocalName(Node? node) =>
        node?.NamespaceDeclaration == true && node.Prefix.Length == 0 ? string.Empty : node?.LocalName ?? string.Empty;

    internal static async ValueTask<XmlDictionaryReader> CaptureSubtreeAsync(XmlReaderDelegator source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (source.NodeType != XmlNodeType.Element)
        {
            throw XmlObjectSerializerReadContext.CreateUnexpectedStateException(XmlNodeType.Element, source);
        }
        XmlReader reader = source.UnderlyingReader;
        int depth = reader.Depth;
        bool empty = reader.IsEmptyElement;
        List<Node> nodes = new List<Node>();
        (Node root, _) = await CaptureNodeAsync(reader, cancellationToken).ConfigureAwait(false);
        nodes.Add(root);
        if (!empty)
        {
            bool advance = true;
            while (true)
            {
                if (advance && !await ReadSourceAsync(reader, cancellationToken).ConfigureAwait(false))
                {
                    throw XmlObjectSerializer.CreateSerializationException(SR.UnexpectedEndOfFile);
                }
                (Node node, bool advanced) = await CaptureNodeAsync(reader, cancellationToken).ConfigureAwait(false);
                nodes.Add(node);
                if (node.Kind == XmlNodeType.EndElement && node.Depth == depth)
                {
                    break;
                }
                advance = !advanced;
            }
        }
        return new BufferedXmlReader(source, nodes);
    }

    internal static async ValueTask<XmlDictionaryReader> CaptureCurrentNodeAsync(XmlReaderDelegator source, CancellationToken cancellationToken)
    {
        (Node node, bool advanced) = await CaptureNodeAsync(source.UnderlyingReader, cancellationToken).ConfigureAwait(false);
        if (!advanced && node.Kind != XmlNodeType.Attribute)
        {
            await ReadSourceAsync(source.UnderlyingReader, cancellationToken).ConfigureAwait(false);
        }
        return new BufferedXmlReader(source, new List<Node> { node });
    }

    private static async ValueTask<bool> ReadSourceAsync(XmlReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (reader is ExtensionDataReader || reader.GetType() == typeof(XmlNodeReader))
        {
            return reader.Read();
        }
        return reader is XmlDictionaryReader dictionaryReader
            ? await dictionaryReader.ReadAsync(cancellationToken).ConfigureAwait(false)
            : await reader.ReadAsync().ConfigureAwait(false);
    }

    private static async ValueTask<(Node Node, bool Advanced)> CaptureNodeAsync(XmlReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool nativeNames = reader is XmlBaseReader;
        string localName;
        string namespaceUri;
        string prefix;
        if (reader is XmlBaseReader nativeReader)
        {
            nativeReader.GetBufferedNames(out localName, out namespaceUri, out prefix);
        }
        else
        {
            localName = reader.LocalName;
            namespaceUri = reader.NamespaceURI;
            prefix = reader.Prefix;
        }
        Node node = new Node
        {
            Kind = reader.NodeType,
            LocalName = localName,
            Namespace = namespaceUri,
            Prefix = prefix,
            NativeNames = nativeNames,
            NamespaceDeclaration = nativeNames && namespaceUri == Globals.XmlnsNamespace,
            Depth = reader.Depth,
            Empty = reader.IsEmptyElement,
            Default = reader.IsDefault,
            Quote = reader.QuoteChar,
            BaseUri = reader.BaseURI,
            XmlLang = reader.XmlLang,
            XmlSpace = reader.XmlSpace,
            ValueType = reader.ValueType,
            HasValue = reader.HasValue,
            SchemaInfo = reader.SchemaInfo,
            Namespaces = reader is XmlDictionaryReader namespaceReader ? namespaceReader.GetNamespacesForSerialization() :
                reader is IXmlNamespaceResolver resolver ? resolver.GetNamespacesInScope(XmlNamespaceScope.All) : null
        };
        if (reader is XmlDictionaryReader dictionaryReader)
        {
            dictionaryReader.TryGetLocalNameAsDictionaryString(out node.DictionaryLocalName);
            dictionaryReader.TryGetNamespaceUriAsDictionaryString(out node.DictionaryNamespace);
            dictionaryReader.TryGetValueAsDictionaryString(out node.DictionaryValue);
        }
        if (reader is IXmlLineInfo lineInfo)
        {
            node.HasLineInfo = lineInfo.HasLineInfo();
            node.Line = lineInfo.LineNumber;
            node.Column = lineInfo.LinePosition;
        }
        if (node.Kind == XmlNodeType.Element && reader.HasAttributes)
        {
            node.Attributes = new Node[reader.AttributeCount];
            for (int i = 0; i < node.Attributes.Length; i++)
            {
                reader.MoveToAttribute(i);
                (node.Attributes[i], _) = await CaptureNodeAsync(reader, cancellationToken).ConfigureAwait(false);
            }
            reader.MoveToElement();
        }
        if (reader is XmlBaseReader baseReader && baseReader.TryGetBufferedValue(out object? value))
        {
            node.TypedValue = value;
            return (node, false);
        }
        node.Text = !node.HasValue ? string.Empty :
            reader is XmlBaseReader or ExtensionDataReader || reader.GetType() == typeof(XmlNodeReader) ? reader.Value :
            await reader.GetValueAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return (node, false);
    }

    public override XmlDictionaryReaderQuotas Quotas => _quotas;
    public override XmlReaderSettings? Settings => _settings;
    public override XmlNameTable NameTable => _nameTable ??= _sourceReader.NameTable;
    public override XmlNodeType NodeType => _attributeValue ? XmlNodeType.Text : Current?.Kind ?? XmlNodeType.None;
    public override string LocalName
    {
        get
        {
            Node? node = Current;
            string value = _attributeValue && !HasNativeNames ? string.Empty : node?.LocalName ?? string.Empty;
            if (HasNativeNames && !(node?.NamespaceDeclaration == true && node.Prefix.Length == 0))
            {
                return NameTable.Add(value);
            }
            return value;
        }
    }
    public override string NamespaceURI
    {
        get
        {
            Node? node = Current;
            string value = _attributeValue && !HasNativeNames ? string.Empty : node?.Namespace ?? string.Empty;
            return HasNativeNames && node?.NamespaceDeclaration != true ? NameTable.Add(value) : value;
        }
    }
    public override string Prefix
    {
        get
        {
            Node? node = Current;
            string value = _attributeValue && !HasNativeNames ? string.Empty : node?.Prefix ?? string.Empty;
            return HasNativeNames && node?.NamespaceDeclaration != true ? NameTable.Add(value) : value;
        }
    }
    public override int Depth => (Current?.Depth ?? 0) + (_attributeValue ? 1 : 0);
    public override string BaseURI => Current?.BaseUri ?? string.Empty;
    public override bool IsEmptyElement => Current?.Empty ?? false;
    public override bool IsDefault => Current?.Default ?? false;
    public override char QuoteChar => Current?.Quote ?? '"';
    public override XmlSpace XmlSpace => Current?.XmlSpace ?? XmlSpace.None;
    public override string XmlLang => Current?.XmlLang ?? string.Empty;
    public override IXmlSchemaInfo? SchemaInfo => Current?.SchemaInfo;
    public override bool HasValue => _attributeValue || (Current?.HasValue ?? false);
    public override Type ValueType => _valueAccessed ? typeof(string) : Current?.ValueType ?? typeof(string);
    public override bool EOF => _index >= _nodes.Count;
    public override ReadState ReadState => _closed ? ReadState.Closed : EOF ? ReadState.EndOfFile : ReadState.Interactive;
    public override int AttributeCount => Element?.Kind == XmlNodeType.Element ? Element.Attributes.Length : 0;
    public override bool CanReadBinaryContent => _canReadBinaryContent;
    public override bool CanReadValueChunk => _canReadValueChunk;
    public override bool CanResolveEntity => false;

    public override string Value
    {
        get
        {
            _valueAccessed = true;
            return _cachedValue ??= Current?.GetText() ?? string.Empty;
        }
    }

    public override bool IsLocalName(string localName)
    {
        ArgumentNullException.ThrowIfNull(localName);
        return RawLocalName == localName;
    }

    public override bool IsLocalName(XmlDictionaryString localName)
    {
        ArgumentNullException.ThrowIfNull(localName);
        return RawLocalName == localName.Value;
    }

    public override bool IsNamespaceUri(string namespaceUri)
    {
        ArgumentNullException.ThrowIfNull(namespaceUri);
        return RawNamespace == namespaceUri;
    }

    public override bool IsNamespaceUri(XmlDictionaryString namespaceUri)
    {
        ArgumentNullException.ThrowIfNull(namespaceUri);
        return RawNamespace == namespaceUri.Value;
    }

    public override bool IsStartElement() =>
        NodeType == XmlNodeType.Element || NodeType != XmlNodeType.EndElement && MoveToContent() == XmlNodeType.Element;

    public override bool IsStartElement(string name)
    {
        if (HasNativeNames && name is null)
        {
            return false;
        }
        ArgumentNullException.ThrowIfNull(name);
        if (!IsStartElement())
        {
            return false;
        }
        int colon = name.IndexOf(':');
        return colon < 0 ? RawPrefix.Length == 0 && RawLocalName == name :
            RawPrefix.AsSpan().SequenceEqual(name.AsSpan(0, colon)) && RawLocalName.AsSpan().SequenceEqual(name.AsSpan(colon + 1));
    }

    public override bool IsStartElement(string localName, string namespaceUri)
    {
        if (HasNativeNames && (localName is null || namespaceUri is null))
        {
            return false;
        }
        ArgumentNullException.ThrowIfNull(localName);
        ArgumentNullException.ThrowIfNull(namespaceUri);
        return IsStartElement() && RawLocalName == localName && RawNamespace == namespaceUri;
    }

    public override bool IsStartElement(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
    {
        ArgumentNullException.ThrowIfNull(localName);
        ArgumentNullException.ThrowIfNull(namespaceUri);
        return IsStartElement() && RawLocalName == localName.Value && RawNamespace == namespaceUri.Value;
    }

    public override int IndexOfLocalName(string[] localNames, string namespaceUri)
    {
        ArgumentNullException.ThrowIfNull(localNames);
        ArgumentNullException.ThrowIfNull(namespaceUri);
        if (RawNamespace == namespaceUri)
        {
            for (int i = 0; i < localNames.Length; i++)
            {
                if (localNames[i] is null)
                {
                    throw new ArgumentNullException($"{nameof(localNames)}[{i}]");
                }
                if (RawLocalName == localNames[i])
                {
                    return i;
                }
            }
        }
        return -1;
    }

    public override int IndexOfLocalName(XmlDictionaryString[] localNames, XmlDictionaryString namespaceUri)
    {
        ArgumentNullException.ThrowIfNull(localNames);
        ArgumentNullException.ThrowIfNull(namespaceUri);
        if (RawNamespace == namespaceUri.Value)
        {
            for (int i = 0; i < localNames.Length; i++)
            {
                if (localNames[i] is null)
                {
                    throw new ArgumentNullException($"{nameof(localNames)}[{i}]");
                }
                if (RawLocalName == localNames[i].Value)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    public override bool TryGetLocalNameAsDictionaryString([NotNullWhen(true)] out XmlDictionaryString? localName)
    {
        localName = _attributeValue && !HasNativeNames ? null : Current?.DictionaryLocalName;
        if (localName is null && Current is null && HasNativeNames)
        {
            localName = XmlDictionaryString.Empty;
        }
        return localName is not null;
    }

    public override bool TryGetNamespaceUriAsDictionaryString([NotNullWhen(true)] out XmlDictionaryString? namespaceUri)
    {
        namespaceUri = _attributeValue && !HasNativeNames ? null : Current?.DictionaryNamespace;
        if (namespaceUri is null && Current is null && HasNativeNames)
        {
            namespaceUri = XmlDictionaryString.Empty;
        }
        return namespaceUri is not null;
    }

    public override bool TryGetValueAsDictionaryString([NotNullWhen(true)] out XmlDictionaryString? value)
    {
        value = Current?.DictionaryValue;
        return value is not null;
    }

    public override string? GetAttribute(string name)
    {
        foreach (Node node in Element?.Attributes ?? Array.Empty<Node>())
        {
            if ((node.Prefix.Length == 0 ? node.LocalName : string.Concat(node.Prefix, ":", node.LocalName)) == name)
            {
                return node.GetText();
            }
        }
        return null;
    }

    public override string? GetAttribute(string localName, string? namespaceUri)
    {
        foreach (Node node in Element?.Attributes ?? Array.Empty<Node>())
        {
            if (GetComparisonLocalName(node) == localName && node.Namespace == (namespaceUri ?? string.Empty))
            {
                return node.GetText();
            }
        }
        return null;
    }

    public override string GetAttribute(int i)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(i);
        if (i >= AttributeCount)
        {
            throw new ArgumentOutOfRangeException(nameof(i));
        }
        return Element!.Attributes[i].GetText();
    }

    public override void MoveToAttribute(int i)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(i);
        if (i >= AttributeCount)
        {
            throw new ArgumentOutOfRangeException(nameof(i));
        }
        _attributeIndex = i;
        ResetValueState();
    }

    public override bool MoveToAttribute(string name)
    {
        for (int i = 0; i < AttributeCount; i++)
        {
            Node attribute = Element!.Attributes[i];
            if ((attribute.Prefix.Length == 0 ? attribute.LocalName : string.Concat(attribute.Prefix, ":", attribute.LocalName)) == name)
            {
                MoveToAttribute(i);
                return true;
            }
        }
        return false;
    }

    public override bool MoveToAttribute(string localName, string? namespaceUri)
    {
        for (int i = 0; i < AttributeCount; i++)
        {
            Node attribute = Element!.Attributes[i];
            if (GetComparisonLocalName(attribute) == localName && attribute.Namespace == (namespaceUri ?? string.Empty))
            {
                MoveToAttribute(i);
                return true;
            }
        }
        return false;
    }

    public override bool MoveToFirstAttribute()
    {
        if (AttributeCount == 0)
        {
            return false;
        }
        MoveToAttribute(0);
        return true;
    }

    public override bool MoveToNextAttribute()
    {
        if (_attributeIndex + 1 >= AttributeCount)
        {
            return false;
        }
        MoveToAttribute(_attributeIndex + 1);
        return true;
    }

    public override bool MoveToElement()
    {
        bool moved = _attributeIndex >= 0;
        if (!moved)
        {
            return false;
        }
        _attributeIndex = -1;
        ResetValueState();
        return moved;
    }

    public override bool ReadAttributeValue()
    {
        if (_attributeIndex < 0)
        {
            return false;
        }
        if (_attributeValue)
        {
            return false;
        }
        _valueAccessed = false;
        _cachedValue = null;
        _valueOffset = 0;
        _byteOffset = 0;
        _attributeValue = true;
        return true;
    }

    public override bool Read()
    {
        if (_closed)
        {
            return false;
        }
        _attributeIndex = -1;
        ResetValueState();
        _index++;
        return !EOF;
    }

    private void ResetValueState()
    {
        _attributeValue = false;
        _valueAccessed = false;
        _cachedValue = null;
        _valueOffset = 0;
        _byteOffset = 0;
    }

    public override object ReadContentAsObject()
    {
        if (!_valueAccessed && Current?.TypedValue is object value)
        {
            if (_attributeIndex < 0)
            {
                Read();
            }
            return value;
        }
        return HasNativeNames ? ReadContentAsString() : base.ReadContentAsObject();
    }

    public override string ReadContentAsString()
    {
        if (HasNativeNames && Current?.TypedValue is not null)
        {
            string value;
            if (_valueAccessed)
            {
                value = _cachedValue ?? Current.GetText();
                if (_attributeIndex < 0 || _attributeValue)
                {
                    _cachedValue = string.Empty;
                }
            }
            else
            {
                value = Current.GetText();
                if (_attributeIndex < 0)
                {
                    Read();
                }
            }
            if (value.Length > _quotas.MaxStringContentLength)
            {
                XmlExceptionHelper.ThrowMaxStringContentLengthExceeded(this, _quotas.MaxStringContentLength);
            }
            return value;
        }
        return base.ReadContentAsString();
    }

    public override object ReadContentAs(Type returnType, IXmlNamespaceResolver? namespaceResolver)
    {
        if (returnType == typeof(object))
        {
            return ReadContentAsObject();
        }
        if (!_valueAccessed && Current?.TypedValue is object value &&
            returnType != typeof(string) && returnType == value.GetType())
        {
            return ReadContentAsObject();
        }
        return base.ReadContentAs(returnType, namespaceResolver);
    }

    public override bool ReadContentAsBoolean() => (bool)ReadContentAs(typeof(bool), this);
    public override int ReadContentAsInt() => (int)ReadContentAs(typeof(int), this);
    public override long ReadContentAsLong() => (long)ReadContentAs(typeof(long), this);
    public override float ReadContentAsFloat() => (float)ReadContentAs(typeof(float), this);
    public override double ReadContentAsDouble() => (double)ReadContentAs(typeof(double), this);
    public override decimal ReadContentAsDecimal() => (decimal)ReadContentAs(typeof(decimal), this);
    public override DateTime ReadContentAsDateTime() => (DateTime)ReadContentAs(typeof(DateTime), this);
    public override Guid ReadContentAsGuid() => (Guid)ReadContentAs(typeof(Guid), this);
    public override TimeSpan ReadContentAsTimeSpan() => (TimeSpan)ReadContentAs(typeof(TimeSpan), this);
    public override UniqueId ReadContentAsUniqueId() => (UniqueId)ReadContentAs(typeof(UniqueId), this);

    public override byte[] ReadContentAsBase64()
    {
        if (!_valueAccessed && Current?.TypedValue is byte[] bytes)
        {
            if (bytes.Length > _quotas.MaxArrayLength)
            {
                XmlExceptionHelper.ThrowMaxArrayLengthExceeded(this, _quotas.MaxArrayLength);
            }
            if (_attributeIndex < 0)
            {
                Read();
            }
            return bytes;
        }
        return base.ReadContentAsBase64();
    }

    public override int ReadContentAsBase64(byte[] buffer, int index, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (index > buffer.Length || count > buffer.Length - index)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
        if (!_valueAccessed && Current?.TypedValue is byte[] bytes)
        {
            int copied = Math.Min(count, bytes.Length - _byteOffset);
            bytes.AsSpan(_byteOffset, copied).CopyTo(buffer.AsSpan(index, copied));
            _byteOffset += copied;
            if (_byteOffset == bytes.Length && _attributeIndex < 0 && count != 0)
            {
                Read();
            }
            return copied;
        }
        return base.ReadContentAsBase64(buffer, index, count);
    }

    public override bool TryGetBase64ContentLength(out int length)
    {
        if (!_valueAccessed && Current?.TypedValue is byte[] bytes)
        {
            length = bytes.Length;
            return true;
        }
        return base.TryGetBase64ContentLength(out length);
    }

    public override int ReadValueChunk(char[] buffer, int index, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (index > buffer.Length || count > buffer.Length - index)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
        string value = Value;
        int copied = Math.Min(count, value.Length - _valueOffset);
        value.AsSpan(_valueOffset, copied).CopyTo(buffer.AsSpan(index, copied));
        _valueOffset += copied;
        return copied;
    }

    public override Task<bool> ReadAsync() => Task.FromResult(Read());
    public override Task<string> GetValueAsync() => Task.FromResult(Value);
    public override Task<string> ReadContentAsStringAsync() => Task.FromResult(ReadContentAsString());
    public override Task<object> ReadContentAsObjectAsync() => Task.FromResult(ReadContentAsObject());
    public override Task<object> ReadContentAsAsync(Type returnType, IXmlNamespaceResolver? namespaceResolver) => Task.FromResult(ReadContentAs(returnType, namespaceResolver));
    public override Task<int> ReadContentAsBase64Async(byte[] buffer, int index, int count) => Task.FromResult(ReadContentAsBase64(buffer, index, count));
    public override Task<int> ReadValueChunkAsync(char[] buffer, int index, int count) => Task.FromResult(ReadValueChunk(buffer, index, count));
    public override void Close()
    {
        _attributeIndex = -1;
        ResetValueState();
        _closed = true;
    }
    public override void ResolveEntity() => throw XmlObjectSerializer.CreateSerializationException(SR.IXmlSerializableIllegalOperation);

    public override string? LookupNamespace(string prefix)
    {
        if (prefix == "xml")
        {
            const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
            return HasNativeNames ? NameTable.Add(XmlNamespace) : XmlNamespace;
        }
        if (prefix == Globals.XmlnsPrefix)
        {
            return Globals.XmlnsNamespace;
        }
        if (Current?.Namespaces is IDictionary<string, string> namespaces && namespaces.TryGetValue(prefix, out string? ns))
        {
            return HasNativeNames ? NameTable.Add(ns) : ns;
        }
        return null;
    }

    IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
    {
        Dictionary<string, string> result = new Dictionary<string, string>();
        if (scope == XmlNamespaceScope.Local)
        {
            foreach (Node attribute in Element?.Attributes ?? Array.Empty<Node>())
            {
                if (attribute.Prefix == Globals.XmlnsPrefix || attribute.LocalName == Globals.XmlnsPrefix)
                {
                    result[attribute.Prefix == Globals.XmlnsPrefix ? attribute.LocalName : string.Empty] = attribute.GetText();
                }
            }
        }
        else if (Current?.Namespaces is IDictionary<string, string> namespaces)
        {
            foreach (KeyValuePair<string, string> pair in namespaces)
            {
                if (scope == XmlNamespaceScope.All || pair.Key != "xml")
                {
                    result.Add(pair.Key, pair.Value);
                }
            }
        }
        return result;
    }

    string? IXmlNamespaceResolver.LookupPrefix(string namespaceName)
    {
        foreach (KeyValuePair<string, string> pair in ((IXmlNamespaceResolver)this).GetNamespacesInScope(XmlNamespaceScope.All))
        {
            if (pair.Value == namespaceName)
            {
                return pair.Key;
            }
        }
        return null;
    }

    bool IXmlTextParser.Normalized { get => _normalized; set => _normalized = value; }
    WhitespaceHandling IXmlTextParser.WhitespaceHandling { get => _whitespaceHandling; set => _whitespaceHandling = value; }
    bool IXmlLineInfo.HasLineInfo() => Current?.HasLineInfo ?? false;
    int IXmlLineInfo.LineNumber => Current?.Line ?? 0;
    int IXmlLineInfo.LinePosition => Current?.Column ?? 0;

    private static string ToLexical(object? value) => value switch
    {
        null => string.Empty,
        string text => text,
        bool item => XmlConvert.ToString(item),
        float item => XmlConvert.ToString(item),
        double item => XmlConvert.ToString(item),
        decimal item => XmlConvert.ToString(item),
        DateTime item => XmlConvert.ToString(item, XmlDateTimeSerializationMode.RoundtripKind),
        TimeSpan item => XmlConvert.ToString(item),
        byte[] bytes => Convert.ToBase64String(bytes),
        Array items => FormatList(items),
        IFormattable item => item.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()!
    };

    private static string FormatList(Array items)
    {
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < items.Length; i++)
        {
            if (i != 0)
            {
                text.Append(' ');
            }
            text.Append(ToLexical(items.GetValue(i)));
        }
        return text.ToString();
    }
}
