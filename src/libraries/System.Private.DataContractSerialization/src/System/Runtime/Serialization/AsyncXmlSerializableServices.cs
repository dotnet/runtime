// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization.DataContracts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace System.Runtime.Serialization;

internal static class AsyncXmlSerializableServices
{
    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal static async ValueTask<object?> ReadAsync(XmlReaderDelegator reader, XmlDataContract contract, bool isMemberType, CancellationToken cancellationToken,
        CreateXmlSerializableDelegate? createXmlSerializable = null)
    {
        bool empty = reader.UnderlyingReader.IsEmptyElement;
        using XmlDictionaryReader buffered = await BufferedXmlReader.CaptureSubtreeAsync(reader, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        object? value = XmlObjectSerializerReadContext.ReadIXmlSerializable(
            new XmlSerializableReader(), new XmlReaderDelegator(buffered), contract, isMemberType,
            createXmlSerializable ?? CreateFactory(contract));
        if (empty)
        {
            await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        return value;
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal static CreateXmlSerializableDelegate CreateFactory(XmlDataContract contract)
    {
        Type type = contract.UnderlyingType;
        return type.IsValueType ? () => (IXmlSerializable)Activator.CreateInstance(type)! :
            () => contract.ReflectionCreateXmlSerializable(type);
    }

    internal static async ValueTask<XmlNode> ReadNodeAsync(XmlReaderDelegator reader, XmlDocument document, CancellationToken cancellationToken)
    {
        bool element = reader.NodeType == XmlNodeType.Element;
        bool empty = element && reader.UnderlyingReader.IsEmptyElement;
        using XmlDictionaryReader buffered = element
            ? await BufferedXmlReader.CaptureSubtreeAsync(reader, cancellationToken).ConfigureAwait(false)
            : await BufferedXmlReader.CaptureCurrentNodeAsync(reader, cancellationToken).ConfigureAwait(false);
        XmlNode node = document.ReadNode(buffered)!;
        if (element)
        {
            if (empty)
            {
                await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            await reader.ReadEndElementAsync(cancellationToken).ConfigureAwait(false);
        }
        return node;
    }

    internal static async ValueTask WriteAsync(XmlWriterDelegator writer, object value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using BufferedXmlWriter buffered = new BufferedXmlWriter(writer.Writer);
        XmlObjectSerializerWriteContext.WriteIXmlSerializable(new XmlWriterDelegator(buffered), value, new XmlSerializableWriter());
        await buffered.ReplayAsync(writer.AsyncWriter, cancellationToken).ConfigureAwait(false);
    }

    internal static async ValueTask WriteNodeAsync(XmlWriterDelegator writer, XmlNode node, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using BufferedXmlWriter buffered = new BufferedXmlWriter(writer.Writer);
        node.WriteTo(buffered);
        await buffered.ReplayAsync(writer.AsyncWriter, cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class BufferedXmlWriter : XmlWriter
{
    private delegate ValueTask WriteEvent(XmlDictionaryWriter writer, CancellationToken cancellationToken);

    private readonly XmlWriter _outer;
    private readonly XmlDictionaryWriter _validator;
    private readonly List<WriteEvent> _events = new List<WriteEvent>();
    private readonly Stack<Dictionary<string, string>> _namespaceScopes = new Stack<Dictionary<string, string>>();
    private Dictionary<string, string> _namespaces = new Dictionary<string, string>();
    private string? _namespaceAttributePrefix;
    private StringBuilder? _namespaceAttributeValue;
    private bool _disposed;

    internal BufferedXmlWriter(XmlWriter outer)
    {
        _outer = outer;
        // The validator has a known CPU-only sink. Only this value's events are retained for external transfer.
        _validator = XmlDictionaryWriter.CreateTextWriter(Stream.Null, Encoding.UTF8, ownsStream: false);
        if (outer.WriteState == WriteState.Prolog)
        {
            _validator.WriteStartDocument();
        }
        if (outer.WriteState is WriteState.Element or WriteState.Content or WriteState.Attribute)
        {
            _validator.WriteStartElement("buffer");
            if (!string.IsNullOrEmpty(outer.XmlLang))
            {
                _validator.WriteAttributeString("xml", "lang", null, outer.XmlLang);
            }
            if (outer.XmlSpace != XmlSpace.None)
            {
                _validator.WriteAttributeString("xml", "space", null, outer.XmlSpace == XmlSpace.Preserve ? "preserve" : "default");
            }
            if (outer.WriteState == WriteState.Content)
            {
                _validator.WriteString(string.Empty);
            }
            else if (outer.WriteState == WriteState.Attribute)
            {
                _validator.WriteStartAttribute("buffer");
            }
        }
    }

    internal async ValueTask ReplayAsync(XmlDictionaryWriter writer, CancellationToken cancellationToken)
    {
        foreach (WriteEvent operation in _events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await operation(writer, cancellationToken).ConfigureAwait(false);
        }
    }

    public override XmlWriterSettings? Settings => _outer.Settings;
    public override WriteState WriteState => _validator.WriteState;
    public override XmlSpace XmlSpace => _validator.XmlSpace;
    public override string? XmlLang => _validator.XmlLang;

    public override string? LookupPrefix(string ns)
    {
        string? found = null;
        foreach (KeyValuePair<string, string> pair in _namespaces)
        {
            if (pair.Value == ns)
            {
                found = pair.Key;
            }
        }
        if (found is not null)
        {
            return found;
        }
        string? inherited = _outer.LookupPrefix(ns);
        return inherited is not null && _namespaces.TryGetValue(inherited, out string? rebound) && rebound != ns ? null : inherited;
    }

    public override void WriteStartElement(string? prefix, string localName, string? ns)
    {
        string? validatedPrefix = prefix;
        string? validatedNamespace = ns;
        if (ns is not null)
        {
            validatedPrefix ??= LookupPrefix(ns);
        }
        else if (!string.IsNullOrEmpty(prefix) && !_namespaces.TryGetValue(prefix, out validatedNamespace))
        {
            validatedPrefix = null;
            validatedNamespace = string.Empty;
        }
        _validator.WriteStartElement(validatedPrefix, localName, validatedNamespace);
        _namespaceScopes.Push(_namespaces);
        _namespaces = new Dictionary<string, string>(_namespaces);
        if (ns is not null)
        {
            _namespaces[validatedPrefix ?? string.Empty] = ns;
        }
        _events.Add((writer, token) => writer.WriteStartElementAsync(prefix, localName, ns, token));
    }

    public override void WriteEndElement()
    {
        _validator.WriteEndElement();
        if (_namespaceScopes.Count != 0)
        {
            _namespaces = _namespaceScopes.Pop();
        }
        _events.Add(static (writer, token) => writer.WriteEndElementAsync(token));
    }

    public override void WriteFullEndElement()
    {
        _validator.WriteFullEndElement();
        if (_namespaceScopes.Count != 0)
        {
            _namespaces = _namespaceScopes.Pop();
        }
        _events.Add(static (writer, token) => writer.WriteFullEndElementAsync(token));
    }

    public override void WriteStartAttribute(string? prefix, string localName, string? ns)
    {
        string? validatedNamespace = ns;
        string? validatedPrefix = prefix;
        if (ns is null && !string.IsNullOrEmpty(prefix) && prefix != Globals.XmlnsPrefix && prefix != "xml" &&
            !_namespaces.TryGetValue(prefix, out validatedNamespace))
        {
            validatedPrefix = null;
        }
        _validator.WriteStartAttribute(validatedPrefix, localName, validatedNamespace);
        if (prefix == Globals.XmlnsPrefix || localName == Globals.XmlnsPrefix && string.IsNullOrEmpty(prefix))
        {
            _namespaceAttributePrefix = prefix == Globals.XmlnsPrefix ? localName : string.Empty;
            _namespaceAttributeValue = new StringBuilder();
        }
        _events.Add((writer, token) => writer.WriteStartAttributeAsync(prefix, localName, ns, token));
    }

    public override void WriteEndAttribute()
    {
        _validator.WriteEndAttribute();
        if (_namespaceAttributePrefix is not null)
        {
            _namespaces.Remove(_namespaceAttributePrefix);
            _namespaces.Add(_namespaceAttributePrefix, _namespaceAttributeValue!.ToString());
            _namespaceAttributePrefix = null;
            _namespaceAttributeValue = null;
        }
        _events.Add(static (writer, token) => writer.WriteEndAttributeAsync(token));
    }

    public override void WriteString(string? text)
    {
        _validator.WriteString(text);
        _namespaceAttributeValue?.Append(text);
        _events.Add((writer, token) => writer.WriteStringAsync(text, token));
    }

    public override void WriteQualifiedName(string localName, string? ns)
    {
        XmlConvert.VerifyNCName(localName);
        string? prefix = string.IsNullOrEmpty(ns) ? string.Empty : LookupPrefix(ns);
        if (prefix is null)
        {
            _validator.WriteQualifiedName(localName, ns);
        }
        else
        {
            _validator.WriteString(prefix.Length == 0 ? localName : string.Concat(prefix, ":", localName));
        }
        _events.Add((writer, token) => writer.WriteQualifiedNameAsync(localName, ns, token));
    }

    public override void WriteRaw(string data)
    {
        _validator.WriteRaw(data);
        _namespaceAttributeValue?.Append(data);
        _events.Add((writer, token) => writer.WriteRawAsync(data, token));
    }

    public override void WriteRaw(char[] buffer, int index, int count)
    {
        _validator.WriteRaw(buffer, index, count);
        char[] copy = buffer.AsSpan(index, count).ToArray();
        _namespaceAttributeValue?.Append(copy);
        _events.Add((writer, token) => writer.WriteRawAsync(copy, 0, copy.Length, token));
    }

    public override void WriteChars(char[] buffer, int index, int count)
    {
        _validator.WriteChars(buffer, index, count);
        char[] copy = buffer.AsSpan(index, count).ToArray();
        _namespaceAttributeValue?.Append(copy);
        _events.Add((writer, token) => writer.WriteCharsAsync(copy, 0, copy.Length, token));
    }

    public override void WriteBase64(byte[] buffer, int index, int count)
    {
        _validator.WriteBase64(buffer, index, count);
        byte[] copy = buffer.AsSpan(index, count).ToArray();
        _events.Add((writer, token) => writer.WriteBase64Async(copy, 0, copy.Length, token));
    }

    public override void WriteBinHex(byte[] buffer, int index, int count)
    {
        _validator.WriteBinHex(buffer, index, count);
        byte[] copy = buffer.AsSpan(index, count).ToArray();
        _events.Add((writer, token) => writer.WriteBinHexAsync(copy, 0, copy.Length, token));
    }

    public override void WriteCData(string? text)
    {
        _validator.WriteCData(text);
        _events.Add((writer, token) => writer.WriteCDataAsync(text, token));
    }

    public override void WriteComment(string? text)
    {
        _validator.WriteComment(text);
        _events.Add((writer, token) => writer.WriteCommentAsync(text, token));
    }

    public override void WriteCharEntity(char value)
    {
        _validator.WriteCharEntity(value);
        _namespaceAttributeValue?.Append(value);
        _events.Add((writer, token) => writer.WriteCharEntityAsync(value, token));
    }

    public override void WriteSurrogateCharEntity(char low, char high)
    {
        _validator.WriteSurrogateCharEntity(low, high);
        _events.Add((writer, token) => writer.WriteSurrogateCharEntityAsync(low, high, token));
    }

    public override void WriteWhitespace(string? value)
    {
        _validator.WriteWhitespace(value);
        _namespaceAttributeValue?.Append(value);
        _events.Add((writer, token) => writer.RunWithCancellationAsync(value, token, static (target, text) => target.WriteWhitespaceAsync(text)));
    }

    public override void WriteProcessingInstruction(string name, string? text)
    {
        _validator.WriteProcessingInstruction(name, text);
        _events.Add((writer, token) => writer.RunWithCancellationAsync((name, text), token, static (target, state) => target.WriteProcessingInstructionAsync(state.name, state.text)));
    }

    public override void WriteEntityRef(string name)
    {
        _validator.WriteEntityRef(name);
        _events.Add((writer, token) => writer.RunWithCancellationAsync(name, token, static (target, value) => target.WriteEntityRefAsync(value)));
    }

    public override void WriteName(string name)
    {
        _validator.WriteName(name);
        _events.Add((writer, token) => writer.RunWithCancellationAsync(name, token, static (target, value) => target.WriteNameAsync(value)));
    }

    public override void WriteNmToken(string name)
    {
        _validator.WriteNmToken(name);
        _events.Add((writer, token) => writer.RunWithCancellationAsync(name, token, static (target, value) => target.WriteNmTokenAsync(value)));
    }

    public override void WriteStartDocument()
    {
        _validator.WriteStartDocument();
        _events.Add(static (writer, token) => writer.WriteStartDocumentAsync(token));
    }

    public override void WriteStartDocument(bool standalone)
    {
        _validator.WriteStartDocument(standalone);
        _events.Add((writer, token) => writer.WriteStartDocumentAsync(standalone, token));
    }

    public override void WriteEndDocument()
    {
        _validator.WriteEndDocument();
        _events.Add(static (writer, token) => writer.WriteEndDocumentAsync(token));
    }

    public override void WriteDocType(string name, string? publicId, string? systemId, string? subset)
    {
        _validator.WriteDocType(name, publicId, systemId, subset);
        _events.Add((writer, token) => writer.RunWithCancellationAsync((name, publicId, systemId, subset), token,
            static (target, value) => target.WriteDocTypeAsync(value.name, value.publicId, value.systemId, value.subset)));
    }

    public override void Flush()
    {
        _validator.Flush();
        _events.Add(static (writer, token) => writer.FlushAsync(token));
    }

    public override void Close() => throw XmlObjectSerializer.CreateSerializationException(SR.IXmlSerializableIllegalOperation);

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            _validator.Close();
        }
    }
}
