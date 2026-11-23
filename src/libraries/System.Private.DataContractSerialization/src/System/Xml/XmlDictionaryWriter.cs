// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace System.Xml
{
    public abstract class XmlDictionaryWriter : XmlWriter
    {
        internal virtual CancellationToken CancellationToken { get; set; }

        internal static ValueTask<XmlDictionaryWriter> CreateBinaryWriterAsync(Stream stream, IXmlDictionary? dictionary,
            XmlBinaryWriterSession? session, bool ownsStream, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(stream);
            cancellationToken.ThrowIfCancellationRequested();
            XmlBinaryWriter writer = new XmlBinaryWriter();
            writer.SetOutput(stream, dictionary, session, ownsStream);
            return ValueTask.FromResult<XmlDictionaryWriter>(new XmlDictionaryAsyncCheckWriter(writer));
        }

        internal static async ValueTask<XmlDictionaryWriter> CreateTextWriterAsync(Stream stream, Encoding encoding,
            bool ownsStream, CancellationToken cancellationToken)
        {
            XmlUTF8TextWriter writer = new XmlUTF8TextWriter();
            await writer.SetOutputAsync(stream, encoding, ownsStream, cancellationToken).ConfigureAwait(false);
            return new XmlDictionaryAsyncCheckWriter(writer);
        }

        internal static ValueTask<XmlDictionaryWriter> CreateDictionaryWriterAsync(XmlWriter writer, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(writer);
            cancellationToken.ThrowIfCancellationRequested();
            XmlDictionaryWriter dictionaryWriter = CreateDictionaryWriter(writer);
            return ValueTask.FromResult(dictionaryWriter is XmlDictionaryAsyncCheckWriter
                ? dictionaryWriter
                : new XmlDictionaryAsyncCheckWriter(dictionaryWriter));
        }

        public static XmlDictionaryWriter CreateBinaryWriter(Stream stream)
        {
            return CreateBinaryWriter(stream, null);
        }

        public static XmlDictionaryWriter CreateBinaryWriter(Stream stream, IXmlDictionary? dictionary)
        {
            return CreateBinaryWriter(stream, dictionary, null);
        }

        public static XmlDictionaryWriter CreateBinaryWriter(Stream stream, IXmlDictionary? dictionary, XmlBinaryWriterSession? session)
        {
            return CreateBinaryWriter(stream, dictionary, session, true);
        }

        public static XmlDictionaryWriter CreateBinaryWriter(Stream stream, IXmlDictionary? dictionary, XmlBinaryWriterSession? session, bool ownsStream)
        {
            XmlBinaryWriter writer = new XmlBinaryWriter();
            writer.SetOutput(stream, dictionary, session, ownsStream);
            return writer;
        }

        public static XmlDictionaryWriter CreateTextWriter(Stream stream)
        {
            return CreateTextWriter(stream, DataContractSerializer.UTF8NoBom, true);
        }

        public static XmlDictionaryWriter CreateTextWriter(Stream stream, Encoding encoding)
        {
            return CreateTextWriter(stream, encoding, true);
        }

        public static XmlDictionaryWriter CreateTextWriter(Stream stream, Encoding encoding, bool ownsStream)
        {
            XmlUTF8TextWriter writer = new XmlUTF8TextWriter();
            writer.SetOutput(stream, encoding, ownsStream);
            var asyncWriter = new XmlDictionaryAsyncCheckWriter(writer);
            return asyncWriter;
        }

        public static XmlDictionaryWriter CreateMtomWriter(Stream stream, Encoding encoding, int maxSizeInBytes, string startInfo)
        {
            return CreateMtomWriter(stream, encoding, maxSizeInBytes, startInfo, null, null, true, true);
        }

        public static XmlDictionaryWriter CreateMtomWriter(Stream stream, Encoding encoding, int maxSizeInBytes, string startInfo, string? boundary, string? startUri, bool writeMessageHeaders, bool ownsStream)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_MtomEncoding);
        }

        public static XmlDictionaryWriter CreateDictionaryWriter(XmlWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);

            return writer as XmlDictionaryWriter ?? new XmlWrappedWriter(writer);
        }

        public override Task WriteBase64Async(byte[] buffer, int index, int count)
        {
            return base.WriteBase64Async(buffer, index, count);
        }

        internal Task WriteStartAttributeAsyncInternal(string? prefix, string localName, string? namespaceUri) =>
            WriteStartAttributeAsync(prefix, localName, namespaceUri);

        internal Task WriteEndAttributeAsyncInternal() => WriteEndAttributeAsync();

        internal virtual Task WriteStartElementAsync(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri) =>
            WriteStartElementAsync(prefix, localName.Value, namespaceUri?.Value);

        internal virtual Task WriteStartAttributeAsync(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri) =>
            WriteStartAttributeAsync(prefix, localName.Value, namespaceUri?.Value);

        internal async Task WriteAttributeStringAsync(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, string? value)
        {
            await WriteStartAttributeAsync(prefix, localName, namespaceUri).ConfigureAwait(false);
            await WriteStringAsync(value).ConfigureAwait(false);
            await WriteEndAttributeAsync().ConfigureAwait(false);
        }

        internal virtual Task WriteStringAsync(XmlDictionaryString? value) => WriteStringAsync(XmlDictionaryString.GetString(value));

        internal virtual Task WriteQualifiedNameAsync(XmlDictionaryString localName, XmlDictionaryString? namespaceUri) =>
            WriteQualifiedNameAsync(localName.Value, namespaceUri?.Value);

        internal virtual Task WriteXmlAttributeAsync(string localName, string? value) =>
            WriteAttributeStringAsync("xml", localName, null, value);

        internal virtual Task WriteXmlnsAttributeAsync(string? prefix, string namespaceUri)
        {
            ArgumentNullException.ThrowIfNull(namespaceUri);
            if (prefix is null)
            {
                if (LookupPrefix(namespaceUri) is not null)
                {
                    return Task.CompletedTask;
                }
                prefix = namespaceUri.Length == 0 ? string.Empty : $"d{namespaceUri.Length}";
            }
            return WriteAttributeStringAsync("xmlns", prefix, null, namespaceUri);
        }

        internal virtual Task WriteXmlnsAttributeAsync(string? prefix, XmlDictionaryString namespaceUri) =>
            WriteXmlnsAttributeAsync(prefix, namespaceUri.Value);

        internal virtual Task WriteValueAsync(object value)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value is IStreamProvider provider)
            {
                return WriteValueAsync(provider);
            }
            if (value is Array array)
            {
                return WriteArrayValueAsync(array);
            }
            return WriteStringAsync(GetValueText(value));
        }

        internal Task WritePrimitiveValueAsync<T>(object value) where T : struct
        {
            if (this is XmlBaseWriter xmlWriter) return xmlWriter.WriteXmlPrimitiveValueAsync<T>(value);
            if (this is Runtime.Serialization.Json.XmlJsonWriter jsonWriter) return jsonWriter.WriteJsonPrimitiveValueAsync<T>(value);
            if (this is XmlDictionaryAsyncCheckWriter checkedWriter) return checkedWriter.WriteCheckedPrimitiveValueAsync<T>(value);
            if (IsInt32Primitive<T>()) return WriteValueAsync(GetInt32Primitive<T>(value));
            if (typeof(T) == typeof(uint)) return WriteValueAsync((long)(uint)value);

            return WriteValueAsync(value);
        }

        internal Task WritePrimitiveElementAsync<T>(
            string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, object value) where T : struct
        {
            if (this is XmlBaseWriter writer)
                return writer.WriteXmlPrimitiveElementAsync<T>(prefix, localName, namespaceUri, value);
            if (this is XmlDictionaryAsyncCheckWriter checkedWriter)
                return checkedWriter.WriteCheckedPrimitiveElementAsync<T>(prefix, localName, namespaceUri, value);

            return WritePrimitiveElementAsyncFallback<T>(prefix, localName, namespaceUri, value);
        }

        internal async Task WritePrimitiveElementAsyncFallback<T>(
            string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, object value) where T : struct
        {
            await WriteStartElementAsync(prefix, localName, namespaceUri).ConfigureAwait(false);
            await WritePrimitiveValueAsync<T>(value).ConfigureAwait(false);
            await WriteEndElementAsync().ConfigureAwait(false);
        }

        internal Task WriteArrayAsync<T>(
            string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, T[] array, int offset, int count) where T : struct =>
            this is XmlUTF8TextWriter writer
                ? writer.WriteTextArrayAsync(prefix, localName, namespaceUri, array, offset, count)
                : this is XmlDictionaryAsyncCheckWriter checkedWriter
                    ? checkedWriter.WriteCheckedArrayAsync(prefix, localName, namespaceUri, array, offset, count)
                    : WriteArrayAsync(prefix, localName, namespaceUri, (Array)array, offset, count);

        private async Task WriteArrayValueAsync(Array array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (i != 0)
                {
                    await WriteStringAsync(" ").ConfigureAwait(false);
                }
                object? value = array.GetValue(i);
                ArgumentNullException.ThrowIfNull(value);
                if (value is Array)
                {
                    throw new ArgumentException(SR.XmlNestedArraysNotSupported, nameof(value));
                }
                await WriteValueAsync(value).ConfigureAwait(false);
            }
        }

        internal static string GetValueText(object value) => value switch
        {
            XmlDictionaryString v => v.Value,
            UniqueId v => XmlConverter.ToString(v),
            Guid v => XmlConverter.ToString(v),
            TimeSpan v => XmlConverter.ToString(v),
            DateTime v => XmlConverter.ToString(v),
            DateTimeOffset v => XmlConvert.ToString(v),
            _ => (string)XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.UntypedAtomic).Datatype!.ChangeType(value, typeof(string))
        };

        internal static string GetPrimitiveValueText<T>(object value) where T : struct
        {
            if (typeof(T) == typeof(bool)) return XmlConverter.ToString((bool)value);
            if (IsInt32Primitive<T>()) return XmlConverter.ToString(GetInt32Primitive<T>(value));
            if (typeof(T) == typeof(uint)) return XmlConverter.ToString((long)(uint)value);
            if (typeof(T) == typeof(long)) return XmlConverter.ToString((long)value);
            if (typeof(T) == typeof(ulong)) return XmlConverter.ToString((ulong)value);
            if (typeof(T) == typeof(float)) return XmlConverter.ToString((float)value);
            if (typeof(T) == typeof(double)) return XmlConverter.ToString((double)value);
            if (typeof(T) == typeof(decimal)) return XmlConverter.ToString((decimal)value);
            if (typeof(T) == typeof(DateTime)) return XmlConverter.ToString((DateTime)value);
            if (typeof(T) == typeof(Guid)) return XmlConverter.ToString((Guid)value);
            if (typeof(T) == typeof(TimeSpan)) return XmlConverter.ToString((TimeSpan)value);

            return GetValueText(value);
        }

        internal static bool IsInt32Primitive<T>() where T : struct =>
            typeof(T) == typeof(int) || typeof(T) == typeof(char) || typeof(T) == typeof(byte) ||
            typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(ushort);

        internal static int GetInt32Primitive<T>(object value) where T : struct =>
            GetInt32Primitive<T, object>(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetInt32Primitive<TPrimitive, TValue>(TValue value) where TPrimitive : struct
        {
            if (typeof(TPrimitive) == typeof(char)) return (char)(object)value!;
            if (typeof(TPrimitive) == typeof(byte)) return (byte)(object)value!;
            if (typeof(TPrimitive) == typeof(sbyte)) return (sbyte)(object)value!;
            if (typeof(TPrimitive) == typeof(short)) return (short)(object)value!;
            if (typeof(TPrimitive) == typeof(ushort)) return (ushort)(object)value!;

            return (int)(object)value!;
        }

        internal virtual async Task WriteArrayAsync(string? prefix, string localName, string? namespaceUri, Array array, int offset, int count)
        {
            CheckArrayBounds(array, offset, count);
            for (int i = offset; i < offset + count; i++)
            {
                await WriteStartElementAsync(prefix, localName, namespaceUri).ConfigureAwait(false);
                object value = array.GetValue(i)!;
                await WriteValueAsync(value is short shortValue ? (int)shortValue : value).ConfigureAwait(false);
                await WriteEndElementAsync().ConfigureAwait(false);
            }
        }

        internal virtual async Task WriteArrayAsync(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, Array array, int offset, int count)
        {
            CheckArrayBounds(array, offset, count);
            for (int i = offset; i < offset + count; i++)
            {
                await WriteStartElementAsync(prefix, localName, namespaceUri).ConfigureAwait(false);
                object value = array.GetValue(i)!;
                await WriteValueAsync(value is short shortValue ? (int)shortValue : value).ConfigureAwait(false);
                await WriteEndElementAsync().ConfigureAwait(false);
            }
        }

        internal static void CheckArrayBounds(Array array, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (offset > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, array.Length));
            }
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count > array.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, array.Length - offset));
            }
        }

        internal ValueTask RunWithCancellationAsync<TState>(TState state, CancellationToken cancellationToken,
            Func<XmlDictionaryWriter, TState, Task> operation)
        {
            if (this is XmlDictionaryAsyncCheckWriter writer)
                return writer.RunCheckedWithCancellationAsync(state, cancellationToken, operation);

            CancellationToken previousToken = CancellationToken;
            bool changeToken = previousToken != cancellationToken;
            if (changeToken)
                CancellationToken = cancellationToken;
            Task task;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                task = operation(this, state);
                if (!task.IsCompletedSuccessfully)
                {
                    return AwaitWithCancellationAsync(task, previousToken, cancellationToken);
                }
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (Exception exception)
            {
                if (changeToken)
                    CancellationToken = previousToken;
                return exception is OperationCanceledException
                    ? AwaitWithCancellationAsync(Task.FromException(exception), previousToken, cancellationToken)
                    : ValueTask.FromException(exception);
            }

            if (changeToken)
                CancellationToken = previousToken;
            return ValueTask.CompletedTask;
        }

        private async ValueTask AwaitWithCancellationAsync(Task task, CancellationToken previousToken, CancellationToken cancellationToken)
        {
            try
            {
                await task.ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
            finally
            {
                if (CancellationToken != previousToken)
                    CancellationToken = previousToken;
            }
        }

        internal ValueTask FlushAsync(CancellationToken cancellationToken) =>
            RunWithCancellationAsync(0, cancellationToken, static (writer, _) => writer.FlushAsync());

        internal ValueTask WriteStartElementAsync(string? prefix, string localName, string? namespaceUri, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((prefix, localName, namespaceUri), cancellationToken, static (writer, state) => writer.WriteStartElementAsync(state.prefix, state.localName, state.namespaceUri));

        internal ValueTask WriteStartElementAsync(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((prefix, localName, namespaceUri), cancellationToken, static (writer, state) => writer.WriteStartElementAsync(state.prefix, state.localName, state.namespaceUri));

        internal ValueTask WriteStartAttributeAsync(string? prefix, string localName, string? namespaceUri, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((prefix, localName, namespaceUri), cancellationToken, static (writer, state) => writer.WriteStartAttributeAsync(state.prefix, state.localName, state.namespaceUri));

        internal ValueTask WriteStartAttributeAsync(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((prefix, localName, namespaceUri), cancellationToken, static (writer, state) => writer.WriteStartAttributeAsync(state.prefix, state.localName, state.namespaceUri));

        internal ValueTask WriteEndAttributeAsync(CancellationToken cancellationToken) =>
            RunWithCancellationAsync(0, cancellationToken, static (writer, _) => writer.WriteEndAttributeAsync());

        internal Task WriteEndAttributeAsyncCore() => WriteEndAttributeAsync();

        internal ValueTask WriteAttributeStringAsync(string? prefix, string localName, string? namespaceUri, string? value, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((prefix, localName, namespaceUri, value), cancellationToken,
                static (writer, state) => writer.WriteAttributeStringAsync(state.prefix, state.localName, state.namespaceUri, state.value));

        internal ValueTask WriteAttributeStringAsync(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, string? value, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((prefix, localName, namespaceUri, value), cancellationToken,
                static (writer, state) => writer.WriteAttributeStringAsync(state.prefix, state.localName, state.namespaceUri, state.value));

        internal ValueTask WriteAttributeQualifiedNameAsync(string? prefix, string localName, string? namespaceUri,
            string valueLocalName, string? valueNamespaceUri, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((prefix, localName, namespaceUri, valueLocalName, valueNamespaceUri), cancellationToken,
                static async (writer, state) =>
                {
                    await writer.WriteStartAttributeAsync(state.prefix, state.localName, state.namespaceUri).ConfigureAwait(false);
                    await writer.WriteQualifiedNameAsync(state.valueLocalName, state.valueNamespaceUri).ConfigureAwait(false);
                    await writer.WriteEndAttributeAsync().ConfigureAwait(false);
                });

        internal ValueTask WriteAttributeQualifiedNameAsync(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri,
            XmlDictionaryString valueLocalName, XmlDictionaryString? valueNamespaceUri, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((prefix, localName, namespaceUri, valueLocalName, valueNamespaceUri), cancellationToken,
                static async (writer, state) =>
                {
                    await writer.WriteStartAttributeAsync(state.prefix, state.localName, state.namespaceUri).ConfigureAwait(false);
                    await writer.WriteQualifiedNameAsync(state.valueLocalName, state.valueNamespaceUri).ConfigureAwait(false);
                    await writer.WriteEndAttributeAsync().ConfigureAwait(false);
                });

        internal ValueTask WriteEndElementAsync(CancellationToken cancellationToken) =>
            RunWithCancellationAsync(0, cancellationToken, static (writer, _) => writer.WriteEndElementAsync());

        internal ValueTask WriteFullEndElementAsync(CancellationToken cancellationToken) =>
            RunWithCancellationAsync(0, cancellationToken, static (writer, _) => writer.WriteFullEndElementAsync());

        internal ValueTask WriteStartDocumentAsync(CancellationToken cancellationToken) =>
            RunWithCancellationAsync(0, cancellationToken, static (writer, _) => writer.WriteStartDocumentAsync());

        internal ValueTask WriteStartDocumentAsync(bool standalone, CancellationToken cancellationToken) =>
            RunWithCancellationAsync(standalone, cancellationToken, static (writer, state) => writer.WriteStartDocumentAsync(state));

        internal ValueTask WriteEndDocumentAsync(CancellationToken cancellationToken) =>
            RunWithCancellationAsync(0, cancellationToken, static (writer, _) => writer.WriteEndDocumentAsync());

        internal ValueTask WriteStringAsync(string? value, CancellationToken cancellationToken) =>
            RunWithCancellationAsync(value, cancellationToken, static (writer, state) => writer.WriteStringAsync(state));

        internal ValueTask WriteStringAsync(XmlDictionaryString? value, CancellationToken cancellationToken) =>
            RunWithCancellationAsync(value, cancellationToken, static (writer, state) => writer.WriteStringAsync(state));

        internal ValueTask WriteCharsAsync(char[] chars, int offset, int count, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((chars, offset, count), cancellationToken, static (writer, state) => writer.WriteCharsAsync(state.chars, state.offset, state.count));

        internal ValueTask WriteBase64Async(byte[] bytes, int offset, int count, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((bytes, offset, count), cancellationToken, static (writer, state) => writer.WriteBase64Async(state.bytes, state.offset, state.count));

        internal ValueTask WriteBinHexAsync(byte[] bytes, int offset, int count, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((bytes, offset, count), cancellationToken, static (writer, state) => writer.WriteBinHexAsync(state.bytes, state.offset, state.count));

        internal ValueTask WriteRawAsync(string data, CancellationToken cancellationToken) =>
            RunWithCancellationAsync(data, cancellationToken, static (writer, state) => writer.WriteRawAsync(state));

        internal ValueTask WriteRawAsync(char[] chars, int offset, int count, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((chars, offset, count), cancellationToken, static (writer, state) => writer.WriteRawAsync(state.chars, state.offset, state.count));

        internal ValueTask WriteCDataAsync(string? text, CancellationToken cancellationToken) =>
            RunWithCancellationAsync(text, cancellationToken, static (writer, state) => writer.WriteCDataAsync(state));

        internal ValueTask WriteCommentAsync(string? text, CancellationToken cancellationToken) =>
            RunWithCancellationAsync(text, cancellationToken, static (writer, state) => writer.WriteCommentAsync(state));

        internal ValueTask WriteCharEntityAsync(char ch, CancellationToken cancellationToken) =>
            RunWithCancellationAsync(ch, cancellationToken, static (writer, state) => writer.WriteCharEntityAsync(state));

        internal ValueTask WriteSurrogateCharEntityAsync(char lowChar, char highChar, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((lowChar, highChar), cancellationToken, static (writer, state) => writer.WriteSurrogateCharEntityAsync(state.lowChar, state.highChar));

        internal ValueTask WriteNodeAsync(XmlReader reader, bool defattr, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((reader, defattr), cancellationToken, static (writer, state) => writer.WriteNodeAsync(state.reader, state.defattr));

        internal ValueTask WriteValueAsync(object value, CancellationToken cancellationToken) =>
            RunWithCancellationAsync(value, cancellationToken, static (writer, state) => writer.WriteValueAsync(state));

        internal ValueTask WritePrimitiveValueAsync<T>(object value, CancellationToken cancellationToken) where T : struct =>
            RunWithCancellationAsync(value, cancellationToken, static (writer, state) => writer.WritePrimitiveValueAsync<T>(state));

        internal ValueTask WritePrimitiveElementAsync<T>(string? prefix, XmlDictionaryString localName,
            XmlDictionaryString? namespaceUri, object value, CancellationToken cancellationToken) where T : struct =>
            RunWithCancellationAsync((prefix, localName, namespaceUri, value), cancellationToken,
                static (writer, state) => writer.WritePrimitiveElementAsync<T>(state.prefix, state.localName, state.namespaceUri, state.value));

        internal ValueTask WriteArrayAsync<T>(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri,
            T[] array, int offset, int count, CancellationToken cancellationToken) where T : struct =>
            RunWithCancellationAsync((prefix, localName, namespaceUri, array, offset, count), cancellationToken,
                static (writer, state) => writer.WriteArrayAsync(state.prefix, state.localName, state.namespaceUri, state.array, state.offset, state.count));

        internal ValueTask WriteValueAsync(bool value, CancellationToken cancellationToken) => WritePrimitiveValueAsync<bool>(value, cancellationToken);
        internal ValueTask WriteValueAsync(int value, CancellationToken cancellationToken) => WritePrimitiveValueAsync<int>(value, cancellationToken);
        internal ValueTask WriteValueAsync(long value, CancellationToken cancellationToken) => WritePrimitiveValueAsync<long>(value, cancellationToken);
        internal ValueTask WriteValueAsync(ulong value, CancellationToken cancellationToken) => WritePrimitiveValueAsync<ulong>(value, cancellationToken);
        internal ValueTask WriteValueAsync(float value, CancellationToken cancellationToken) => WritePrimitiveValueAsync<float>(value, cancellationToken);
        internal ValueTask WriteValueAsync(double value, CancellationToken cancellationToken) => WritePrimitiveValueAsync<double>(value, cancellationToken);
        internal ValueTask WriteValueAsync(decimal value, CancellationToken cancellationToken) => WritePrimitiveValueAsync<decimal>(value, cancellationToken);
        internal ValueTask WriteValueAsync(DateTime value, CancellationToken cancellationToken) => WritePrimitiveValueAsync<DateTime>(value, cancellationToken);
        internal ValueTask WriteValueAsync(DateTimeOffset value, CancellationToken cancellationToken) => WriteValueAsync((object)value, cancellationToken);
        internal ValueTask WriteValueAsync(Guid value, CancellationToken cancellationToken) => WriteValueAsync((object)value, cancellationToken);
        internal ValueTask WriteValueAsync(TimeSpan value, CancellationToken cancellationToken) => WriteValueAsync((object)value, cancellationToken);
        internal ValueTask WriteValueAsync(UniqueId value, CancellationToken cancellationToken) => WriteValueAsync((object)value, cancellationToken);
        internal ValueTask WriteValueAsync(string? value, CancellationToken cancellationToken) => WriteStringAsync(value, cancellationToken);
        internal ValueTask WriteValueAsync(XmlDictionaryString? value, CancellationToken cancellationToken) => WriteStringAsync(value, cancellationToken);

        internal ValueTask WriteXmlnsAttributeAsync(string? prefix, string namespaceUri, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((prefix, namespaceUri), cancellationToken, static (writer, state) => writer.WriteXmlnsAttributeAsync(state.prefix, state.namespaceUri));

        internal ValueTask WriteXmlnsAttributeAsync(string? prefix, XmlDictionaryString namespaceUri, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((prefix, namespaceUri), cancellationToken, static (writer, state) => writer.WriteXmlnsAttributeAsync(state.prefix, state.namespaceUri));

        internal ValueTask WriteXmlAttributeAsync(string localName, string? value, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((localName, value), cancellationToken, static (writer, state) => writer.WriteXmlAttributeAsync(state.localName, state.value));

        internal ValueTask WriteQualifiedNameAsync(XmlDictionaryString localName, XmlDictionaryString? namespaceUri, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((localName, namespaceUri), cancellationToken, static (writer, state) => writer.WriteQualifiedNameAsync(state.localName, state.namespaceUri));

        internal ValueTask WriteQualifiedNameAsync(string localName, string? namespaceUri, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((localName, namespaceUri), cancellationToken, static (writer, state) => writer.WriteQualifiedNameAsync(state.localName, state.namespaceUri));

        internal ValueTask WriteArrayAsync(string? prefix, string localName, string? namespaceUri, Array array, int offset, int count, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((prefix, localName, namespaceUri, array, offset, count), cancellationToken, static (writer, state) => writer.WriteArrayAsync(state.prefix, state.localName, state.namespaceUri, state.array, state.offset, state.count));

        internal ValueTask WriteArrayAsync(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, Array array, int offset, int count, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((prefix, localName, namespaceUri, array, offset, count), cancellationToken, static (writer, state) => writer.WriteArrayAsync(state.prefix, state.localName, state.namespaceUri, state.array, state.offset, state.count));

        internal virtual Task StartCanonicalizationAsync(Stream stream, bool includeComments, string[]? inclusivePrefixes) => throw new NotSupportedException();
        internal virtual Task EndCanonicalizationAsync() => throw new NotSupportedException();

        internal ValueTask StartCanonicalizationAsync(Stream stream, bool includeComments, string[]? inclusivePrefixes, CancellationToken cancellationToken) =>
            RunWithCancellationAsync((stream, includeComments, inclusivePrefixes), cancellationToken, static (writer, state) => writer.StartCanonicalizationAsync(state.stream, state.includeComments, state.inclusivePrefixes));

        internal ValueTask EndCanonicalizationAsync(CancellationToken cancellationToken) =>
            RunWithCancellationAsync(0, cancellationToken, static (writer, _) => writer.EndCanonicalizationAsync());

        internal virtual async ValueTask CloseAsync(CancellationToken cancellationToken)
        {
            CancellationToken previousToken = CancellationToken;
            CancellationToken = cancellationToken;
            try
            {
                await DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                CancellationToken = previousToken;
            }
        }

        public void WriteStartElement(XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
        {
            WriteStartElement((string?)null, localName, namespaceUri);
        }

        public virtual void WriteStartElement(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
        {
            WriteStartElement(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri));
        }

        public void WriteStartAttribute(XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
        {
            WriteStartAttribute((string?)null, localName, namespaceUri);
        }

        public virtual void WriteStartAttribute(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
        {
            WriteStartAttribute(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri));
        }

        public void WriteAttributeString(XmlDictionaryString localName, XmlDictionaryString? namespaceUri, string? value)
        {
            WriteAttributeString((string?)null, localName, namespaceUri, value);
        }

        public virtual void WriteXmlnsAttribute(string? prefix, string namespaceUri)
        {
            ArgumentNullException.ThrowIfNull(namespaceUri);

            if (prefix == null)
            {
                if (LookupPrefix(namespaceUri) != null)
                    return;
                prefix = namespaceUri.Length == 0 ? string.Empty : $"d{namespaceUri.Length}";
            }
            WriteAttributeString("xmlns", prefix, null, namespaceUri);
        }

        public virtual void WriteXmlnsAttribute(string? prefix, XmlDictionaryString namespaceUri)
        {
            WriteXmlnsAttribute(prefix, XmlDictionaryString.GetString(namespaceUri));
        }

        public virtual void WriteXmlAttribute(string localName, string? value)
        {
            WriteAttributeString("xml", localName, null, value);
        }

        public virtual void WriteXmlAttribute(XmlDictionaryString localName, XmlDictionaryString? value)
        {
            WriteXmlAttribute(XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(value));
        }

        public void WriteAttributeString(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, string? value)
        {
            WriteStartAttribute(prefix, localName, namespaceUri);
            WriteString(value);
            WriteEndAttribute();
        }

        public void WriteElementString(XmlDictionaryString localName, XmlDictionaryString? namespaceUri, string? value)
        {
            WriteElementString((string?)null, localName, namespaceUri, value);
        }

        public void WriteElementString(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, string? value)
        {
            WriteStartElement(prefix, localName, namespaceUri);
            WriteString(value);
            WriteEndElement();
        }

        public virtual void WriteString(XmlDictionaryString? value)
        {
            WriteString(XmlDictionaryString.GetString(value));
        }

        public virtual void WriteQualifiedName(XmlDictionaryString localName, XmlDictionaryString? namespaceUri)
        {
            ArgumentNullException.ThrowIfNull(localName);

            namespaceUri ??= XmlDictionaryString.Empty;
            WriteQualifiedName(localName.Value, namespaceUri.Value);
        }

        public virtual void WriteValue(XmlDictionaryString? value)
        {
            WriteValue(XmlDictionaryString.GetString(value));
        }


        public virtual void WriteValue(UniqueId value)
        {
            ArgumentNullException.ThrowIfNull(value);

            WriteString(value.ToString());
        }

        public virtual void WriteValue(Guid value)
        {
            WriteString(value.ToString());
        }

        public virtual void WriteValue(TimeSpan value)
        {
            WriteString(XmlConvert.ToString(value));
        }

        public virtual void WriteValue(IStreamProvider value)
        {
            ArgumentNullException.ThrowIfNull(value);

            Stream stream = value.GetStream();
            if (stream == null)
                throw new XmlException(SR.XmlInvalidStream);
            int blockSize = 256;
            int bytesRead;
            byte[] block = new byte[blockSize];
            while (true)
            {
                bytesRead = stream.Read(block, 0, blockSize);
                if (bytesRead > 0)
                    WriteBase64(block, 0, bytesRead);
                else
                    break;
                if (blockSize < 65536 && bytesRead == blockSize)
                {
                    blockSize *= 16;
                    block = new byte[blockSize];
                }
            }
            value.ReleaseStream(stream);
        }

        public virtual Task WriteValueAsync(IStreamProvider value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return WriteStreamValueAsync(value);
        }

        private async Task WriteStreamValueAsync(IStreamProvider value)
        {
            CancellationToken.ThrowIfCancellationRequested();
            Stream stream = value.GetStream();
            if (stream is null)
            {
                throw new XmlException(SR.XmlInvalidStream);
            }
            try
            {
                int blockSize = 256;
                byte[] block = new byte[blockSize];
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(block, 0, blockSize, CancellationToken).ConfigureAwait(false);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    await WriteBase64Async(block, 0, bytesRead).ConfigureAwait(false);
                    if (blockSize < 65536 && bytesRead == blockSize)
                    {
                        blockSize *= 16;
                        block = new byte[blockSize];
                    }
                }
            }
            finally
            {
                value.ReleaseStream(stream);
            }
        }

        public virtual bool CanCanonicalize
        {
            get
            {
                return false;
            }
        }

        public virtual void StartCanonicalization(Stream stream, bool includeComments, string[]? inclusivePrefixes)
        {
            throw new NotSupportedException();
        }

        public virtual void EndCanonicalization()
        {
            throw new NotSupportedException();
        }

        private void WriteElementNode(XmlDictionaryReader reader, bool defattr)
        {
            XmlDictionaryString? localName;
            XmlDictionaryString? namespaceUri;
            if (reader.TryGetLocalNameAsDictionaryString(out localName) && reader.TryGetNamespaceUriAsDictionaryString(out namespaceUri))
            {
                WriteStartElement(reader.Prefix, localName, namespaceUri);
            }
            else
            {
                WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
            }
            if (defattr || (!reader.IsDefault && (reader.SchemaInfo == null || !reader.SchemaInfo.IsDefault)))
            {
                if (reader.MoveToFirstAttribute())
                {
                    do
                    {
                        if (reader.TryGetLocalNameAsDictionaryString(out localName) && reader.TryGetNamespaceUriAsDictionaryString(out namespaceUri))
                        {
                            WriteStartAttribute(reader.Prefix, localName, namespaceUri);
                        }
                        else
                        {
                            WriteStartAttribute(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                        }
                        while (reader.ReadAttributeValue())
                        {
                            if (reader.NodeType == XmlNodeType.EntityReference)
                            {
                                WriteEntityRef(reader.Name);
                            }
                            else
                            {
                                WriteTextNode(reader, true);
                            }
                        }
                        WriteEndAttribute();
                    }
                    while (reader.MoveToNextAttribute());
                    reader.MoveToElement();
                }
            }
            if (reader.IsEmptyElement)
            {
                WriteEndElement();
            }
        }

        private void WriteArrayNode(XmlDictionaryReader reader, string prefix, string localName, string namespaceUri, Type type)
        {
            if (type == typeof(bool))
                BooleanArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(short))
                Int16ArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(int))
                Int32ArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(long))
                Int64ArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(float))
                SingleArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(double))
                DoubleArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(decimal))
                DecimalArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(DateTime))
                DateTimeArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(Guid))
                GuidArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(TimeSpan))
                TimeSpanArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else
            {
                WriteElementNode(reader, false);
                reader.Read();
            }
        }

        private void WriteArrayNode(XmlDictionaryReader reader, string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, Type type)
        {
            if (type == typeof(bool))
                BooleanArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(short))
                Int16ArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(int))
                Int32ArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(long))
                Int64ArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(float))
                SingleArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(double))
                DoubleArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(decimal))
                DecimalArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(DateTime))
                DateTimeArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(Guid))
                GuidArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(TimeSpan))
                TimeSpanArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else
            {
                WriteElementNode(reader, false);
                reader.Read();
            }
        }

        private void WriteArrayNode(XmlDictionaryReader reader, Type type)
        {
            XmlDictionaryString? localName;
            XmlDictionaryString? namespaceUri;
            if (reader.TryGetLocalNameAsDictionaryString(out localName) && reader.TryGetNamespaceUriAsDictionaryString(out namespaceUri))
                WriteArrayNode(reader, reader.Prefix, localName, namespaceUri, type);
            else
                WriteArrayNode(reader, reader.Prefix, reader.LocalName, reader.NamespaceURI, type);
        }

        protected virtual void WriteTextNode(XmlDictionaryReader reader, bool isAttribute)
        {
            XmlDictionaryString? value;
            if (reader.TryGetValueAsDictionaryString(out value))
            {
                WriteString(value);
            }
            else
            {
                WriteString(reader.Value);
            }
            if (!isAttribute)
            {
                reader.Read();
            }
        }

        public override void WriteNode(XmlReader reader, bool defattr)
        {
            if (reader is XmlDictionaryReader dictionaryReader)
                WriteNode(dictionaryReader, defattr);
            else
                base.WriteNode(reader, defattr);
        }

        public virtual void WriteNode(XmlDictionaryReader reader, bool defattr)
        {
            ArgumentNullException.ThrowIfNull(reader);

            int d = (reader.NodeType == XmlNodeType.None ? -1 : reader.Depth);
            do
            {
                XmlNodeType nodeType = reader.NodeType;
                Type? type;
                if (nodeType == XmlNodeType.Text || nodeType == XmlNodeType.Whitespace || nodeType == XmlNodeType.SignificantWhitespace)
                {
                    // This will advance if necessary, so we don't need to call Read() explicitly
                    WriteTextNode(reader, false);
                }
                else if (reader.Depth > d && reader.IsStartArray(out type))
                {
                    WriteArrayNode(reader, type);
                }
                else
                {
                    // These will not advance, so we must call Read() explicitly
                    switch (nodeType)
                    {
                        case XmlNodeType.Element:
                            WriteElementNode(reader, defattr);
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
                    if (!reader.Read())
                        break;
                }
            }
            while (d < reader.Depth || (d == reader.Depth && reader.NodeType == XmlNodeType.EndElement));
        }

        private static void CheckArray(Array array, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(array);

            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (offset > array.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, array.Length));
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count > array.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, array.Length - offset));
        }

        // bool
        public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, bool[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, bool[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // Int16
        public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, short[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, short[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // Int32
        public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, int[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, int[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // Int64
        public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, long[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, long[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // float
        public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, float[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, float[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // double
        public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, double[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, double[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // decimal
        public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, decimal[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, decimal[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // DateTime
        public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, DateTime[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, DateTime[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // Guid
        public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, Guid[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, Guid[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // TimeSpan
        public virtual void WriteArray(string? prefix, string localName, string? namespaceUri, TimeSpan[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, TimeSpan[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && WriteState != WriteState.Closed)
            {
                Close();
            }
        }

        protected override ValueTask DisposeAsyncCore()
        {
            if (WriteState == WriteState.Closed)
                return default;
            throw NotImplemented.ByDesign;
        }

        public override void Close() { }

        private sealed class XmlWrappedWriter : XmlDictionaryWriter
        {
            private delegate Task StartAttributeAsyncDelegate(XmlWriter writer, string? prefix, string localName, string? namespaceUri);
            private delegate Task EndAttributeAsyncDelegate(XmlWriter writer);

            // Open delegates preserve virtual dispatch to the caller's protected async attribute APIs.
            private static readonly StartAttributeAsyncDelegate s_startAttributeAsync =
                typeof(XmlWriter).GetMethod(nameof(WriteStartAttributeAsync), BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate<StartAttributeAsyncDelegate>();
            private static readonly EndAttributeAsyncDelegate s_endAttributeAsync =
                typeof(XmlWriter).GetMethod(nameof(WriteEndAttributeAsync), BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate<EndAttributeAsyncDelegate>();

            private readonly XmlWriter _writer;
            private int _depth;
            private int _prefix;

            public XmlWrappedWriter(XmlWriter writer)
            {
                _writer = writer;
                _depth = 0;
            }

            public override XmlWriterSettings? Settings => _writer.Settings;

            public override Task FlushAsync() => _writer.FlushAsync();
            public override Task WriteAttributesAsync(XmlReader reader, bool defattr) => _writer.WriteAttributesAsync(reader, defattr);
            public override Task WriteBase64Async(byte[] buffer, int index, int count) => _writer.WriteBase64Async(buffer, index, count);
            public override Task WriteBinHexAsync(byte[] buffer, int index, int count) => _writer.WriteBinHexAsync(buffer, index, count);
            public override Task WriteCDataAsync(string? text) => _writer.WriteCDataAsync(text);
            public override Task WriteCharEntityAsync(char ch) => _writer.WriteCharEntityAsync(ch);
            public override Task WriteCharsAsync(char[] buffer, int index, int count) => _writer.WriteCharsAsync(buffer, index, count);
            public override Task WriteCommentAsync(string? text) => _writer.WriteCommentAsync(text);
            public override Task WriteDocTypeAsync(string name, string? pubid, string? sysid, string? subset) => _writer.WriteDocTypeAsync(name, pubid, sysid, subset);
            public override Task WriteEndDocumentAsync() => _writer.WriteEndDocumentAsync();
            public override Task WriteEntityRefAsync(string name) => _writer.WriteEntityRefAsync(name);
            public override Task WriteFullEndElementAsync() => _writer.WriteFullEndElementAsync();
            public override Task WriteNameAsync(string name) => _writer.WriteNameAsync(name);
            public override Task WriteNmTokenAsync(string name) => _writer.WriteNmTokenAsync(name);
            public override Task WriteNodeAsync(XmlReader reader, bool defattr) => _writer.WriteNodeAsync(reader, defattr);
            public override Task WriteProcessingInstructionAsync(string name, string? text) => _writer.WriteProcessingInstructionAsync(name, text);
            public override Task WriteQualifiedNameAsync(string localName, string? ns) => _writer.WriteQualifiedNameAsync(localName, ns);
            public override Task WriteRawAsync(string data) => _writer.WriteRawAsync(data);
            public override Task WriteRawAsync(char[] buffer, int index, int count) => _writer.WriteRawAsync(buffer, index, count);
            public override Task WriteStartDocumentAsync() => _writer.WriteStartDocumentAsync();
            public override Task WriteStartDocumentAsync(bool standalone) => _writer.WriteStartDocumentAsync(standalone);
            public override Task WriteStringAsync(string? text) => _writer.WriteStringAsync(text);
            public override Task WriteSurrogateCharEntityAsync(char lowChar, char highChar) => _writer.WriteSurrogateCharEntityAsync(lowChar, highChar);
            public override Task WriteWhitespaceAsync(string? ws) => _writer.WriteWhitespaceAsync(ws);

            protected override async Task WriteStartAttributeAsync(string? prefix, string localName, string? ns)
            {
                await s_startAttributeAsync(_writer, prefix, localName, ns).ConfigureAwait(false);
                _prefix++;
            }

            protected override Task WriteEndAttributeAsync() => s_endAttributeAsync(_writer);

            public override async Task WriteStartElementAsync(string? prefix, string localName, string? ns)
            {
                await _writer.WriteStartElementAsync(prefix, localName, ns).ConfigureAwait(false);
                _depth++;
                _prefix = 1;
            }

            public override async Task WriteEndElementAsync()
            {
                await _writer.WriteEndElementAsync().ConfigureAwait(false);
                _depth--;
            }

            internal override Task WriteXmlnsAttributeAsync(string? prefix, string namespaceUri)
            {
                ArgumentNullException.ThrowIfNull(namespaceUri);
                if (prefix is null)
                {
                    if (LookupPrefix(namespaceUri) is not null)
                        return Task.CompletedTask;
                    prefix = namespaceUri.Length == 0 ? string.Empty : string.Create(System.Globalization.CultureInfo.InvariantCulture, $"d{_depth}p{_prefix}");
                }
                return WriteAttributeStringAsync("xmlns", prefix, null, namespaceUri);
            }

            protected override ValueTask DisposeAsyncCore() => _writer.DisposeAsync();

            public override void Close()
            {
                _writer.Dispose();
            }

            public override void Flush()
            {
                _writer.Flush();
            }

            public override string? LookupPrefix(string namespaceUri)
            {
                return _writer.LookupPrefix(namespaceUri);
            }

            public override void WriteAttributes(XmlReader reader, bool defattr)
            {
                _writer.WriteAttributes(reader, defattr);
            }

            public override void WriteBase64(byte[] buffer, int index, int count)
            {
                _writer.WriteBase64(buffer, index, count);
            }

            public override void WriteBinHex(byte[] buffer, int index, int count)
            {
                _writer.WriteBinHex(buffer, index, count);
            }

            public override void WriteCData(string? text)
            {
                _writer.WriteCData(text);
            }

            public override void WriteCharEntity(char ch)
            {
                _writer.WriteCharEntity(ch);
            }

            public override void WriteChars(char[] buffer, int index, int count)
            {
                _writer.WriteChars(buffer, index, count);
            }

            public override void WriteComment(string? text)
            {
                _writer.WriteComment(text);
            }

            public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
            {
                _writer.WriteDocType(name, pubid, sysid, subset);
            }

            public override void WriteEndAttribute()
            {
                _writer.WriteEndAttribute();
            }

            public override void WriteEndDocument()
            {
                _writer.WriteEndDocument();
            }

            public override void WriteEndElement()
            {
                _writer.WriteEndElement();
                _depth--;
            }

            public override void WriteEntityRef(string name)
            {
                _writer.WriteEntityRef(name);
            }

            public override void WriteFullEndElement()
            {
                _writer.WriteFullEndElement();
            }

            public override void WriteName(string name)
            {
                _writer.WriteName(name);
            }

            public override void WriteNmToken(string name)
            {
                _writer.WriteNmToken(name);
            }

            public override void WriteNode(XmlReader reader, bool defattr)
            {
                _writer.WriteNode(reader, defattr);
            }

            public override void WriteProcessingInstruction(string name, string? text)
            {
                _writer.WriteProcessingInstruction(name, text);
            }

            public override void WriteQualifiedName(string localName, string? namespaceUri)
            {
                _writer.WriteQualifiedName(localName, namespaceUri);
            }

            public override void WriteRaw(char[] buffer, int index, int count)
            {
                _writer.WriteRaw(buffer, index, count);
            }

            public override void WriteRaw(string data)
            {
                _writer.WriteRaw(data);
            }

            public override void WriteStartAttribute(string? prefix, string localName, string? namespaceUri)
            {
                _writer.WriteStartAttribute(prefix, localName, namespaceUri);
                _prefix++;
            }

            public override void WriteStartDocument()
            {
                _writer.WriteStartDocument();
            }

            public override void WriteStartDocument(bool standalone)
            {
                _writer.WriteStartDocument(standalone);
            }

            public override void WriteStartElement(string? prefix, string localName, string? namespaceUri)
            {
                _writer.WriteStartElement(prefix, localName, namespaceUri);
                _depth++;
                _prefix = 1;
            }

            public override WriteState WriteState
            {
                get
                {
                    return _writer.WriteState;
                }
            }

            public override void WriteString(string? text)
            {
                _writer.WriteString(text);
            }

            public override void WriteSurrogateCharEntity(char lowChar, char highChar)
            {
                _writer.WriteSurrogateCharEntity(lowChar, highChar);
            }

            public override void WriteWhitespace(string? whitespace)
            {
                _writer.WriteWhitespace(whitespace);
            }

            public override void WriteValue(object value)
            {
                _writer.WriteValue(value);
            }

            public override void WriteValue(string? value)
            {
                _writer.WriteValue(value);
            }

            public override void WriteValue(bool value)
            {
                _writer.WriteValue(value);
            }

            public override void WriteValue(DateTimeOffset value)
            {
                _writer.WriteValue(value);
            }

            public override void WriteValue(double value)
            {
                _writer.WriteValue(value);
            }

            public override void WriteValue(int value)
            {
                _writer.WriteValue(value);
            }

            public override void WriteValue(long value)
            {
                _writer.WriteValue(value);
            }

            public override void WriteXmlnsAttribute(string? prefix, string namespaceUri)
            {
                ArgumentNullException.ThrowIfNull(namespaceUri);

                if (prefix == null)
                {
                    if (LookupPrefix(namespaceUri) != null)
                        return;

                    if (namespaceUri.Length == 0)
                    {
                        prefix = string.Empty;
                    }
                    else
                    {
                        string depthStr = _depth.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
                        string prefixStr = _prefix.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
                        prefix = string.Concat("d", depthStr, "p", prefixStr);
                    }
                }
                WriteAttributeString("xmlns", prefix, null, namespaceUri);
            }

            public override string? XmlLang
            {
                get
                {
                    return _writer.XmlLang;
                }
            }

            public override XmlSpace XmlSpace
            {
                get
                {
                    return _writer.XmlSpace;
                }
            }
        }
    }
}
