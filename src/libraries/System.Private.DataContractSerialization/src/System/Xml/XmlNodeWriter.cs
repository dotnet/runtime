// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Xml
{
    internal abstract class XmlNodeWriter
    {
        public static XmlNodeWriter Null => field ??= new XmlNullNodeWriter();

        internal virtual CancellationToken CancellationToken { get; set; }

        public abstract void Flush();
        public virtual Task FlushAsync()
        {
            throw NotImplemented.ByDesign;
        }
        public abstract void Close();
        public virtual ValueTask CloseAsync() => throw NotImplemented.ByDesign;
        public abstract void WriteDeclaration();
        public virtual Task WriteDeclarationAsync() => throw NotImplemented.ByDesign;
        public abstract void WriteComment(string text);
        public virtual Task WriteCommentAsync(string text) => throw NotImplemented.ByDesign;
        public abstract void WriteCData(string text);
        public virtual Task WriteCDataAsync(string text) => throw NotImplemented.ByDesign;
        public abstract void WriteStartElement(string? prefix, string localName);
        public virtual Task WriteStartElementAsync(string? prefix, string localName)
        {
            throw NotImplemented.ByDesign;
        }
        public virtual void WriteStartElement(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength)
        {
            WriteStartElement(Encoding.UTF8.GetString(prefixBuffer, prefixOffset, prefixLength), Encoding.UTF8.GetString(localNameBuffer, localNameOffset, localNameLength));
        }
        public abstract void WriteStartElement(string? prefix, XmlDictionaryString localName);
        public virtual Task WriteStartElementAsync(string? prefix, XmlDictionaryString localName) =>
            WriteStartElementAsync(prefix, localName.Value);
        public virtual Task WriteStartElementAsync(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength) =>
            WriteStartElementAsync(Encoding.UTF8.GetString(prefixBuffer, prefixOffset, prefixLength), Encoding.UTF8.GetString(localNameBuffer, localNameOffset, localNameLength));
        public abstract void WriteEndStartElement(bool isEmpty);
        public virtual Task WriteEndStartElementAsync(bool isEmpty)
        {
            throw NotImplemented.ByDesign;
        }
        public abstract void WriteEndElement(string? prefix, string localName);
        public virtual Task WriteEndElementAsync(string? prefix, string localName)
        {
            throw NotImplemented.ByDesign;
        }
        public virtual void WriteEndElement(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength)
        {
            WriteEndElement(Encoding.UTF8.GetString(prefixBuffer, prefixOffset, prefixLength), Encoding.UTF8.GetString(localNameBuffer, localNameOffset, localNameLength));
        }
        public abstract void WriteXmlnsAttribute(string? prefix, string ns);
        public virtual Task WriteXmlnsAttributeAsync(string? prefix, string ns) => throw NotImplemented.ByDesign;
        public virtual void WriteXmlnsAttribute(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] nsBuffer, int nsOffset, int nsLength)
        {
            WriteXmlnsAttribute(Encoding.UTF8.GetString(prefixBuffer, prefixOffset, prefixLength), Encoding.UTF8.GetString(nsBuffer, nsOffset, nsLength));
        }
        public abstract void WriteXmlnsAttribute(string? prefix, XmlDictionaryString ns);
        public virtual Task WriteXmlnsAttributeAsync(string? prefix, XmlDictionaryString ns) =>
            WriteXmlnsAttributeAsync(prefix, ns.Value);
        public virtual Task WriteXmlnsAttributeAsync(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] nsBuffer, int nsOffset, int nsLength) =>
            WriteXmlnsAttributeAsync(Encoding.UTF8.GetString(prefixBuffer, prefixOffset, prefixLength), Encoding.UTF8.GetString(nsBuffer, nsOffset, nsLength));
        public abstract void WriteStartAttribute(string prefix, string localName);
        public virtual Task WriteStartAttributeAsync(string prefix, string localName) => throw NotImplemented.ByDesign;
        public virtual void WriteStartAttribute(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength)
        {
            WriteStartAttribute(Encoding.UTF8.GetString(prefixBuffer, prefixOffset, prefixLength), Encoding.UTF8.GetString(localNameBuffer, localNameOffset, localNameLength));
        }
        public virtual Task WriteStartAttributeAsync(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength) =>
            WriteStartAttributeAsync(Encoding.UTF8.GetString(prefixBuffer, prefixOffset, prefixLength), Encoding.UTF8.GetString(localNameBuffer, localNameOffset, localNameLength));
        public abstract void WriteStartAttribute(string prefix, XmlDictionaryString localName);
        public virtual Task WriteStartAttributeAsync(string prefix, XmlDictionaryString localName) =>
            WriteStartAttributeAsync(prefix, localName.Value);
        public abstract void WriteEndAttribute();
        public virtual Task WriteEndAttributeAsync()
        {
            throw NotImplemented.ByDesign;
        }
        public abstract void WriteCharEntity(int ch);
        public virtual Task WriteCharEntityAsync(int ch) => throw NotImplemented.ByDesign;
        public abstract void WriteEscapedText(string value);
        public abstract void WriteEscapedText(XmlDictionaryString value);
        public abstract void WriteEscapedText(char[] chars, int offset, int count);
        public abstract void WriteEscapedText(byte[] buffer, int offset, int count);
        public virtual Task WriteEscapedTextAsync(ReadOnlyMemory<char> value) => throw NotImplemented.ByDesign;
        public virtual Task WriteEscapedTextAsync(ReadOnlyMemory<byte> value) => throw NotImplemented.ByDesign;
        public virtual Task WriteEscapedTextAsync(string value) => WriteEscapedTextAsync(value.AsMemory());
        public virtual Task WriteEscapedTextAsync(XmlDictionaryString value) => WriteEscapedTextAsync(value.Value);
        public virtual Task WriteEscapedTextAsync(char[] chars, int offset, int count) => WriteEscapedTextAsync(chars.AsMemory(offset, count));
        public virtual Task WriteEscapedTextAsync(byte[] bytes, int offset, int count) => WriteEscapedTextAsync(bytes.AsMemory(offset, count));
        public abstract void WriteText(string value);
        public abstract void WriteText(XmlDictionaryString value);
        public abstract void WriteText(char[] chars, int offset, int count);
        public abstract void WriteText(byte[] buffer, int offset, int count);
        public virtual Task WriteTextAsync(ReadOnlyMemory<char> value) => throw NotImplemented.ByDesign;
        public virtual Task WriteTextAsync(ReadOnlyMemory<byte> value) => throw NotImplemented.ByDesign;
        public virtual Task WriteTextAsync(string value) => WriteTextAsync(value.AsMemory());
        public virtual Task WriteTextAsync(XmlDictionaryString value) => WriteTextAsync(value.Value);
        public virtual Task WriteTextAsync(char[] chars, int offset, int count) => WriteTextAsync(chars.AsMemory(offset, count));
        public virtual Task WriteTextAsync(byte[] bytes, int offset, int count) => WriteTextAsync(bytes.AsMemory(offset, count));
        public abstract void WriteInt32Text(int value);
        public abstract void WriteInt64Text(long value);
        public abstract void WriteBoolText(bool value);
        public abstract void WriteUInt64Text(ulong value);
        public abstract void WriteFloatText(float value);
        public abstract void WriteDoubleText(double value);
        public abstract void WriteDecimalText(decimal value);
        public abstract void WriteDateTimeText(DateTime value);
        public abstract void WriteUniqueIdText(UniqueId value);
        public abstract void WriteTimeSpanText(TimeSpan value);
        public abstract void WriteGuidText(Guid value);
        public virtual Task WriteValueAsync(object value) => throw NotImplemented.ByDesign;
        public virtual Task WritePrimitiveValueAsync<T>(object value) where T : struct => WriteValueAsync(value);
        public abstract void WriteStartListText();
        public virtual Task WriteStartListTextAsync() => throw NotImplemented.ByDesign;
        public abstract void WriteListSeparator();
        public virtual Task WriteListSeparatorAsync() => throw NotImplemented.ByDesign;
        public abstract void WriteEndListText();
        public virtual Task WriteEndListTextAsync() => throw NotImplemented.ByDesign;
        public abstract void WriteBase64Text(byte[] trailBuffer, int trailCount, byte[] buffer, int offset, int count);
        public virtual Task WriteBase64TextAsync(byte[] trailBuffer, int trailCount, byte[] buffer, int offset, int count)
        {
            throw NotImplemented.ByDesign;
        }
        public abstract void WriteQualifiedName(string prefix, XmlDictionaryString localName);
        public virtual async Task WriteQualifiedNameAsync(string prefix, XmlDictionaryString localName)
        {
            if (prefix.Length != 0)
            {
                await WriteTextAsync(prefix).ConfigureAwait(false);
                await WriteTextAsync(":").ConfigureAwait(false);
            }
            await WriteTextAsync(localName).ConfigureAwait(false);
        }

        public virtual Task WriteEndElementAsync(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength) =>
            WriteEndElementAsync(Encoding.UTF8.GetString(prefixBuffer, prefixOffset, prefixLength), Encoding.UTF8.GetString(localNameBuffer, localNameOffset, localNameLength));

        private sealed class XmlNullNodeWriter : XmlNodeWriter
        {
            internal override CancellationToken CancellationToken
            {
                get => System.Threading.CancellationToken.None;
                set { }
            }

            public override void Flush() { }
            public override void Close() { }
            public override void WriteDeclaration() { }
            public override void WriteComment(string text) { }
            public override void WriteCData(string text) { }
            public override void WriteStartElement(string? prefix, string localName) { }
            public override void WriteStartElement(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength) { }
            public override void WriteStartElement(string? prefix, XmlDictionaryString localName) { }
            public override void WriteEndStartElement(bool isEmpty) { }
            public override void WriteEndElement(string? prefix, string localName) { }
            public override void WriteEndElement(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength) { }
            public override void WriteXmlnsAttribute(string? prefix, string ns) { }
            public override void WriteXmlnsAttribute(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] nsBuffer, int nsOffset, int nsLength) { }
            public override void WriteXmlnsAttribute(string? prefix, XmlDictionaryString ns) { }
            public override void WriteStartAttribute(string prefix, string localName) { }
            public override void WriteStartAttribute(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength) { }
            public override void WriteStartAttribute(string prefix, XmlDictionaryString localName) { }
            public override void WriteEndAttribute() { }
            public override void WriteCharEntity(int ch) { }
            public override void WriteEscapedText(string value) { }
            public override void WriteEscapedText(XmlDictionaryString value) { }
            public override void WriteEscapedText(char[] chars, int offset, int count) { }
            public override void WriteEscapedText(byte[] buffer, int offset, int count) { }
            public override void WriteText(string value) { }
            public override void WriteText(XmlDictionaryString value) { }
            public override void WriteText(char[] chars, int offset, int count) { }
            public override void WriteText(byte[] buffer, int offset, int count) { }
            public override void WriteInt32Text(int value) { }
            public override void WriteInt64Text(long value) { }
            public override void WriteBoolText(bool value) { }
            public override void WriteUInt64Text(ulong value) { }
            public override void WriteFloatText(float value) { }
            public override void WriteDoubleText(double value) { }
            public override void WriteDecimalText(decimal value) { }
            public override void WriteDateTimeText(DateTime value) { }
            public override void WriteUniqueIdText(UniqueId value) { }
            public override void WriteTimeSpanText(TimeSpan value) { }
            public override void WriteGuidText(Guid value) { }
            public override void WriteStartListText() { }
            public override void WriteListSeparator() { }
            public override void WriteEndListText() { }
            public override void WriteBase64Text(byte[] trailBuffer, int trailCount, byte[] buffer, int offset, int count) { }
            public override void WriteQualifiedName(string prefix, XmlDictionaryString localName) { }
            public override Task FlushAsync() => Task.CompletedTask;
            public override ValueTask CloseAsync() => default;
            public override Task WriteDeclarationAsync() => Task.CompletedTask;
            public override Task WriteCommentAsync(string text) => Task.CompletedTask;
            public override Task WriteCDataAsync(string text) => Task.CompletedTask;
            public override Task WriteStartElementAsync(string? prefix, string localName) => Task.CompletedTask;
            public override Task WriteEndStartElementAsync(bool isEmpty) => Task.CompletedTask;
            public override Task WriteEndElementAsync(string? prefix, string localName) => Task.CompletedTask;
            public override Task WriteXmlnsAttributeAsync(string? prefix, string ns) => Task.CompletedTask;
            public override Task WriteStartAttributeAsync(string prefix, string localName) => Task.CompletedTask;
            public override Task WriteEndAttributeAsync() => Task.CompletedTask;
            public override Task WriteCharEntityAsync(int ch) => Task.CompletedTask;
            public override Task WriteEscapedTextAsync(ReadOnlyMemory<char> value) => Task.CompletedTask;
            public override Task WriteEscapedTextAsync(ReadOnlyMemory<byte> value) => Task.CompletedTask;
            public override Task WriteTextAsync(ReadOnlyMemory<char> value) => Task.CompletedTask;
            public override Task WriteTextAsync(ReadOnlyMemory<byte> value) => Task.CompletedTask;
            public override Task WriteValueAsync(object value) => Task.CompletedTask;
            public override Task WriteStartListTextAsync() => Task.CompletedTask;
            public override Task WriteListSeparatorAsync() => Task.CompletedTask;
            public override Task WriteEndListTextAsync() => Task.CompletedTask;
            public override Task WriteBase64TextAsync(byte[] trailBuffer, int trailCount, byte[] buffer, int offset, int count) => Task.CompletedTask;
        }
    }
}
