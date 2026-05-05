// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Xml
{
    internal sealed class XmlDictionaryAsyncCheckWriter : XmlDictionaryWriter, IXmlTextWriterInitializer
    {
        private readonly XmlDictionaryWriter _coreWriter;
        private Task? _lastTask;

        public XmlDictionaryAsyncCheckWriter(XmlDictionaryWriter writer)
        {
            Debug.Assert(writer is IXmlTextWriterInitializer);
            _coreWriter = writer;
        }

        internal XmlDictionaryWriter CoreWriter
        {
            get
            {
                return _coreWriter;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckAsync()
        {
            if (_lastTask != null && !_lastTask.IsCompleted)
            {
                throw new InvalidOperationException(SR.XmlAsyncIsRunningException);
            }
        }

        private Task SetLastTask(Task task)
        {
            _lastTask = task;
            return task;
        }

        public override XmlWriterSettings? Settings
        {
            get
            {
                CheckAsync();
                return CoreWriter.Settings;
            }
        }

        public override WriteState WriteState
        {
            get
            {
                CheckAsync();
                return CoreWriter.WriteState;
            }
        }

        public override string? XmlLang
        {
            get
            {
                CheckAsync();
                return CoreWriter.XmlLang;
            }
        }

        public override XmlSpace XmlSpace
        {
            get
            {
                CheckAsync();
                return CoreWriter.XmlSpace;
            }
        }

        public override void Flush()
        {
            CheckAsync();
            CoreWriter.Flush();
        }

        public override async Task FlushAsync()
        {
            CheckAsync();
            await SetLastTask(CoreWriter.FlushAsync()).ConfigureAwait(false);
        }

        public override string? LookupPrefix(string ns)
        {
            CheckAsync();
            return CoreWriter.LookupPrefix(ns);
        }

        public override void WriteAttributes(XmlReader reader, bool defattr)
        {
            CheckAsync();
            CoreWriter.WriteAttributes(reader, defattr);
        }

        public override async Task WriteAttributesAsync(XmlReader reader, bool defattr)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteAttributesAsync(reader, defattr)).ConfigureAwait(false);
        }

        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            CheckAsync();
            CoreWriter.WriteBase64(buffer, index, count);
        }

        public override async Task WriteBase64Async(byte[] buffer, int index, int count)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteBase64Async(buffer, index, count)).ConfigureAwait(false);
        }

        public override void WriteBinHex(byte[] buffer, int index, int count)
        {
            CheckAsync();
            CoreWriter.WriteBinHex(buffer, index, count);
        }

        public override async Task WriteBinHexAsync(byte[] buffer, int index, int count)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteBinHexAsync(buffer, index, count)).ConfigureAwait(false);
        }

        public override void WriteCData(string? text)
        {
            CheckAsync();
            CoreWriter.WriteCData(text);
        }

        public override async Task WriteCDataAsync(string? text)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteCDataAsync(text)).ConfigureAwait(false);
        }

        public override void WriteCharEntity(char ch)
        {
            CheckAsync();
            CoreWriter.WriteCharEntity(ch);
        }

        public override async Task WriteCharEntityAsync(char ch)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteCharEntityAsync(ch)).ConfigureAwait(false);
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            CheckAsync();
            CoreWriter.WriteChars(buffer, index, count);
        }

        public override async Task WriteCharsAsync(char[] buffer, int index, int count)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteCharsAsync(buffer, index, count)).ConfigureAwait(false);
        }

        public override void WriteComment(string? text)
        {
            CheckAsync();
            CoreWriter.WriteComment(text);
        }

        public override async Task WriteCommentAsync(string? text)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteCommentAsync(text)).ConfigureAwait(false);
        }

        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
        {
            CheckAsync();
            CoreWriter.WriteDocType(name, pubid, sysid, subset);
        }

        public override async Task WriteDocTypeAsync(string name, string? pubid, string? sysid, string? subset)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteDocTypeAsync(name, pubid, sysid, subset)).ConfigureAwait(false);
        }

        public override void WriteEndAttribute()
        {
            CheckAsync();
            CoreWriter.WriteEndAttribute();
        }

        public override void WriteEndDocument()
        {
            CheckAsync();
            CoreWriter.WriteEndDocument();
        }

        public override async Task WriteEndDocumentAsync()
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteEndDocumentAsync()).ConfigureAwait(false);
        }

        public override void WriteEndElement()
        {
            CheckAsync();
            CoreWriter.WriteEndElement();
        }

        public override async Task WriteEndElementAsync()
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteEndElementAsync()).ConfigureAwait(false);
        }

        public override void WriteEntityRef(string name)
        {
            CheckAsync();
            CoreWriter.WriteEntityRef(name);
        }

        public override async Task WriteEntityRefAsync(string name)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteEntityRefAsync(name)).ConfigureAwait(false);
        }

        public override void WriteFullEndElement()
        {
            CheckAsync();
            CoreWriter.WriteFullEndElement();
        }

        public override async Task WriteFullEndElementAsync()
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteFullEndElementAsync()).ConfigureAwait(false);
        }

        public override void WriteName(string name)
        {
            CheckAsync();
            CoreWriter.WriteName(name);
        }

        public override async Task WriteNameAsync(string name)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteNameAsync(name)).ConfigureAwait(false);
        }

        public override void WriteNmToken(string name)
        {
            CheckAsync();
            CoreWriter.WriteNmToken(name);
        }

        public override async Task WriteNmTokenAsync(string name)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteNmTokenAsync(name)).ConfigureAwait(false);
        }

        public override void WriteNode(XmlReader reader, bool defattr)
        {
            CheckAsync();
            CoreWriter.WriteNode(reader, defattr);
        }

        public override async Task WriteNodeAsync(XmlReader reader, bool defattr)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteNodeAsync(reader, defattr)).ConfigureAwait(false);
        }

        public override void WriteProcessingInstruction(string name, string? text)
        {
            CheckAsync();
            CoreWriter.WriteProcessingInstruction(name, text);
        }

        public override async Task WriteProcessingInstructionAsync(string name, string? text)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteProcessingInstructionAsync(name, text)).ConfigureAwait(false);
        }

        public override void WriteQualifiedName(string localName, string? ns)
        {
            CheckAsync();
            CoreWriter.WriteQualifiedName(localName, ns);
        }

        public override async Task WriteQualifiedNameAsync(string localName, string? ns)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteQualifiedNameAsync(localName, ns)).ConfigureAwait(false);
        }

        public override void WriteRaw(string data)
        {
            CheckAsync();
            CoreWriter.WriteRaw(data);
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            CheckAsync();
            CoreWriter.WriteRaw(buffer, index, count);
        }

        public override async Task WriteRawAsync(string data)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteRawAsync(data)).ConfigureAwait(false);
        }

        public override async Task WriteRawAsync(char[] buffer, int index, int count)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteRawAsync(buffer, index, count)).ConfigureAwait(false);
        }

        public override void WriteStartAttribute(string? prefix, string localName, string? ns)
        {
            CheckAsync();
            CoreWriter.WriteStartAttribute(prefix, localName, ns);
        }

        public override void WriteStartDocument()
        {
            CheckAsync();
            CoreWriter.WriteStartDocument();
        }

        public override void WriteStartDocument(bool standalone)
        {
            CheckAsync();
            CoreWriter.WriteStartDocument(standalone);
        }

        public override async Task WriteStartDocumentAsync()
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteStartDocumentAsync()).ConfigureAwait(false);
        }

        public override async Task WriteStartDocumentAsync(bool standalone)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteStartDocumentAsync(standalone)).ConfigureAwait(false);
        }

        public override void WriteStartElement(string? prefix, string localName, string? ns)
        {
            CheckAsync();
            CoreWriter.WriteStartElement(prefix, localName, ns);
        }

        public override async Task WriteStartElementAsync(string? prefix, string localName, string? ns)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteStartElementAsync(prefix, localName, ns)).ConfigureAwait(false);
        }

        public override void WriteString(string? text)
        {
            CheckAsync();
            CoreWriter.WriteString(text);
        }

        public override async Task WriteStringAsync(string? text)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteStringAsync(text)).ConfigureAwait(false);
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            CheckAsync();
            CoreWriter.WriteSurrogateCharEntity(lowChar, highChar);
        }

        public override async Task WriteSurrogateCharEntityAsync(char lowChar, char highChar)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteSurrogateCharEntityAsync(lowChar, highChar)).ConfigureAwait(false);
        }

        public override void WriteValue(string? value)
        {
            CheckAsync();
            CoreWriter.WriteValue(value);
        }

        public override void WriteValue(double value)
        {
            CheckAsync();
            CoreWriter.WriteValue(value);
        }

        public override void WriteValue(int value)
        {
            CheckAsync();
            CoreWriter.WriteValue(value);
        }

        public override void WriteValue(long value)
        {
            CheckAsync();
            CoreWriter.WriteValue(value);
        }

        public override void WriteValue(object value)
        {
            CheckAsync();
            CoreWriter.WriteValue(value);
        }

        public override void WriteValue(float value)
        {
            CheckAsync();
            CoreWriter.WriteValue(value);
        }

        public override void WriteValue(decimal value)
        {
            CheckAsync();
            CoreWriter.WriteValue(value);
        }

        public override void WriteValue(DateTimeOffset value)
        {
            CheckAsync();
            CoreWriter.WriteValue(value);
        }

        public override void WriteValue(bool value)
        {
            CheckAsync();
            CoreWriter.WriteValue(value);
        }

        public override void WriteWhitespace(string? ws)
        {
            CheckAsync();
            CoreWriter.WriteWhitespace(ws);
        }

        public override async Task WriteWhitespaceAsync(string? ws)
        {
            CheckAsync();
            await SetLastTask(CoreWriter.WriteWhitespaceAsync(ws)).ConfigureAwait(false);
        }

        public override void WriteStartElement(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
        {
            CheckAsync();
            CoreWriter.WriteStartElement(prefix, localName, namespaceUri);
        }

        public override void WriteStartAttribute(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
        {
            CheckAsync();
            CoreWriter.WriteStartAttribute(prefix, localName, namespaceUri);
        }

        public override void WriteXmlnsAttribute(string? prefix, string namespaceUri)
        {
            CheckAsync();
            CoreWriter.WriteXmlnsAttribute(prefix, namespaceUri);
        }

        public override void WriteXmlnsAttribute(string? prefix, XmlDictionaryString namespaceUri)
        {
            CheckAsync();
            CoreWriter.WriteXmlnsAttribute(prefix, namespaceUri);
        }

        public override void WriteXmlAttribute(string localName, string? value)
        {
            CheckAsync();
            CoreWriter.WriteXmlAttribute(localName, value);
        }

        public override void WriteXmlAttribute(XmlDictionaryString localName, XmlDictionaryString? value)
        {
            CheckAsync();
            CoreWriter.WriteXmlAttribute(localName, value);
        }

        public override void WriteString(XmlDictionaryString? value)
        {
            CheckAsync();
            CoreWriter.WriteString(value);
        }

        public override void WriteQualifiedName(XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
        {
            CheckAsync();
            CoreWriter.WriteQualifiedName(localName, namespaceUri);
        }

        public override void WriteValue(XmlDictionaryString? value)
        {
            CheckAsync();
            CoreWriter.WriteValue(value);
        }

        public override void WriteValue(UniqueId value)
        {
            CheckAsync();
            CoreWriter.WriteValue(value);
        }

        public override void WriteValue(Guid value)
        {
            CheckAsync();
            CoreWriter.WriteValue(value);
        }

        public override void WriteValue(TimeSpan value)
        {
            CheckAsync();
            CoreWriter.WriteValue(value);
        }

        public override bool CanCanonicalize
        {
            get
            {
                CheckAsync();
                return CoreWriter.CanCanonicalize;
            }
        }

        public override void StartCanonicalization(Stream stream, bool includeComments, string[]? inclusivePrefixes)
        {
            CheckAsync();
            CoreWriter.StartCanonicalization(stream, includeComments, inclusivePrefixes);
        }

        public override void EndCanonicalization()
        {
            CheckAsync();
            CoreWriter.EndCanonicalization();
        }

        public override void WriteNode(XmlDictionaryReader reader, bool defattr)
        {
            CheckAsync();
            CoreWriter.WriteNode(reader, defattr);
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, bool[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, bool[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, short[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, short[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, int[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, int[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, long[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, long[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, float[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, float[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, double[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, double[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, decimal[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, decimal[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, DateTime[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, DateTime[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, Guid[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, Guid[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void WriteArray(string? prefix, string localName, string? namespaceUri, TimeSpan[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, TimeSpan[] array, int offset, int count)
        {
            CheckAsync();
            CoreWriter.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }

        public override void Close()
        {
            CheckAsync();
            CoreWriter.Close();
        }

        protected override void Dispose(bool disposing)
        {
            CheckAsync();
            CoreWriter.Dispose();
        }

        public void SetOutput(Stream stream, Encoding encoding, bool ownsStream)
        {
            if (CoreWriter is IXmlTextWriterInitializer initializer)
            {
                CheckAsync();
                initializer.SetOutput(stream, encoding, ownsStream);
            }
        }
    }
}
