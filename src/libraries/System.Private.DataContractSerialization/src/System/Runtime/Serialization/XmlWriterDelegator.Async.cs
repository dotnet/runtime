// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization;

internal partial class XmlWriterDelegator
{
    private XmlDictionaryWriter? _asyncDictionaryWriter;

    internal CancellationToken CancellationToken { get; set; }

    internal XmlDictionaryWriter AsyncWriter =>
        _asyncDictionaryWriter ??= dictionaryWriter ?? XmlDictionaryWriter.CreateDictionaryWriter(writer);

    internal Task RunAsyncOperation<TState>(TState state, Func<XmlWriterDelegator, TState, Task> operation)
    {
        if (CancellationToken.IsCancellationRequested)
            return Task.FromCanceled(CancellationToken);

        if (writer is XmlDictionaryAsyncCheckWriter checkedWriter)
        {
            return checkedWriter.RunSerializationAsync((Delegator: this, State: state, Operation: operation),
                static (coreWriter, state) =>
                {
                    state.Delegator.writer = coreWriter;
                    state.Delegator.dictionaryWriter = coreWriter;
                    state.Delegator._asyncDictionaryWriter = coreWriter;
                    return state.Operation(state.Delegator, state.State);
                });
        }

        return operation(this, state);
    }

    internal AsyncOperationScope EnterAsyncOperation() => new AsyncOperationScope(AsyncWriter, CancellationToken);

    internal readonly struct AsyncOperationScope : IDisposable
    {
        private readonly XmlDictionaryWriter _writer;
        private readonly CancellationToken _previousToken;

        internal AsyncOperationScope(XmlDictionaryWriter writer, CancellationToken cancellationToken)
        {
            _writer = writer;
            _previousToken = writer.CancellationToken;
            writer.CancellationToken = cancellationToken;
        }

        public void Dispose() => _writer.CancellationToken = _previousToken;
    }

    internal Task FlushAsync() => AsyncWriter.FlushAsync();

    internal Task WriteStartElementAsync(string? prefix, XmlDictionaryString name, XmlDictionaryString? ns)
    {
        Task operation = AsyncWriter.WriteStartElementAsync(prefix, name, ns);
        return CompleteElementOperation(operation, 1);
    }

    internal virtual Task WriteStartElementAsync(string? prefix, string name, string? ns)
    {
        Task operation = AsyncWriter.WriteStartElementAsync(prefix, name, ns);
        return CompleteElementOperation(operation, 1);
    }

    internal Task WriteEndElementAsync() => CompleteElementOperation(AsyncWriter.WriteEndElementAsync(), -1);

    private Task CompleteElementOperation(Task operation, int depthChange)
    {
        if (operation.IsCompletedSuccessfully)
        {
            depth += depthChange;
            return Task.CompletedTask;
        }

        return AwaitElementOperation(operation, depthChange);
    }

    private async Task AwaitElementOperation(Task operation, int depthChange)
    {
        await operation.ConfigureAwait(false);
        depth += depthChange;
    }

    internal Task WriteStartElementPrimitiveAsync(XmlDictionaryString name, XmlDictionaryString? ns) =>
        AsyncWriter.WriteStartElementAsync(null, name, ns);

    internal Task WriteEndElementPrimitiveAsync() => AsyncWriter.WriteEndElementAsync();

    internal Task WritePrimitiveElementAsync<T>(object value, XmlDictionaryString name, XmlDictionaryString? ns) where T : struct
    {
        if (this is Json.JsonWriterDelegator jsonWriter)
            return jsonWriter.WriteJsonPrimitiveElementAsync<T>(value, name, ns);
        XmlDictionaryWriter output = AsyncWriter;
        return output is XmlBaseWriter xmlWriter
            ? xmlWriter.WriteXmlPrimitiveElementAsync<T>(null, name, ns, value)
            : output.WritePrimitiveElementAsync<T>(null, name, ns, value);
    }

    internal Task WriteTypedPrimitiveElementAsync<T>(T value, XmlDictionaryString name, XmlDictionaryString? ns) where T : struct
    {
        if (typeof(T) == typeof(Guid) || typeof(T) == typeof(TimeSpan))
        {
            if (AsyncWriter is XmlBaseWriter or Json.XmlJsonWriter)
            {
                string text = typeof(T) == typeof(Guid)
                    ? XmlConvert.ToString((Guid)(object)value)
                    : XmlConvert.ToString((TimeSpan)(object)value);
                return WritePrimitiveElementAsync<ReflectionPlan.StringValue>(text, name, ns);
            }
        }
        if (this is Json.JsonWriterDelegator jsonWriter)
            return jsonWriter.WriteTypedJsonPrimitiveElementAsync(value, name, ns);
        XmlDictionaryWriter output = AsyncWriter;
        // These contracts use lexical values even with the binary XML writer.
        if (typeof(T) == typeof(ulong) && output is XmlBaseWriter binaryOrTextWriter)
            return binaryOrTextWriter.WriteXmlPrimitiveElementAsync<ReflectionPlan.StringValue>(null, name, ns, XmlConvert.ToString((ulong)(object)value));
        return output is XmlBaseWriter xmlWriter
            ? xmlWriter.WriteTypedPrimitiveElementAsync(null, name, ns, value)
            : output.WritePrimitiveElementAsync<T>(null, name, ns, value);
    }

    internal Task WriteAttributeStringAsync(string? prefix, string name, string? ns, string? value) =>
        AsyncWriter is Json.XmlJsonWriter jsonWriter && jsonWriter.TryWriteDataTypeAttribute(prefix, name, ns, value)
            ? Task.CompletedTask : AsyncWriter.WriteAttributeStringAsync(prefix, name, ns, value);

    internal Task WriteAttributeStringAsync(string? prefix, XmlDictionaryString name, XmlDictionaryString? ns, string? value) =>
        AsyncWriter.WriteAttributeStringAsync(prefix, name, ns, value);

    internal async Task WriteAttributeIntAsync(string prefix, XmlDictionaryString name, XmlDictionaryString ns, int value)
    {
        await AsyncWriter.WriteStartAttributeAsync(prefix, name, ns).ConfigureAwait(false);
        await AsyncWriter.WritePrimitiveValueAsync<int>(value).ConfigureAwait(false);
        await AsyncWriter.WriteEndAttributeAsyncCore().ConfigureAwait(false);
    }

    internal async Task WriteAttributeBoolAsync(string prefix, XmlDictionaryString name, XmlDictionaryString ns, bool value)
    {
        await AsyncWriter.WriteStartAttributeAsync(prefix, name, ns).ConfigureAwait(false);
        await AsyncWriter.WritePrimitiveValueAsync<bool>(value).ConfigureAwait(false);
        await AsyncWriter.WriteEndAttributeAsyncCore().ConfigureAwait(false);
    }

    internal async Task WriteAttributeQualifiedNameAsync(string prefix, XmlDictionaryString name, XmlDictionaryString ns, string valueName, string? valueNamespace)
    {
        await WriteXmlnsAttributeAsync(valueNamespace).ConfigureAwait(false);
        await AsyncWriter.WriteStartAttributeAsync(prefix, name, ns).ConfigureAwait(false);
        await AsyncWriter.WriteQualifiedNameAsync(valueName, valueNamespace).ConfigureAwait(false);
        await AsyncWriter.WriteEndAttributeAsyncCore().ConfigureAwait(false);
    }

    internal async Task WriteAttributeQualifiedNameAsync(string prefix, XmlDictionaryString name, XmlDictionaryString ns, XmlDictionaryString valueName, XmlDictionaryString valueNamespace)
    {
        await WriteXmlnsAttributeAsync(valueNamespace).ConfigureAwait(false);
        await AsyncWriter.WriteStartAttributeAsync(prefix, name, ns).ConfigureAwait(false);
        await AsyncWriter.WriteQualifiedNameAsync(valueName, valueNamespace).ConfigureAwait(false);
        await AsyncWriter.WriteEndAttributeAsyncCore().ConfigureAwait(false);
    }

    internal Task WriteXmlnsAttributeAsync(string prefix, XmlDictionaryString ns) =>
        AsyncWriter.WriteXmlnsAttributeAsync(prefix, ns);

    internal Task WriteNamespaceDeclAsync(XmlDictionaryString ns) => WriteXmlnsAttributeAsync(ns);

    internal Task WriteXmlnsAttributeAsync(XmlDictionaryString ns) =>
        dictionaryWriter is not null ? AsyncWriter.WriteXmlnsAttributeAsync(null, ns) :
            WriteXmlnsAttributeAsync(ns.Value);

    internal Task WriteXmlnsAttributeAsync(string? ns)
    {
        CancellationToken.ThrowIfCancellationRequested();
        if (ns is null)
        {
            return Task.CompletedTask;
        }
        if (dictionaryWriter is not null)
        {
            return AsyncWriter.WriteXmlnsAttributeAsync(null, ns);
        }
        if (ns.Length == 0)
        {
            return WriteAttributeStringAsync("xmlns", string.Empty, null, ns);
        }
        if (writer.LookupPrefix(ns) is not null)
        {
            return Task.CompletedTask;
        }

        string prefix = string.Create(CultureInfo.InvariantCulture, $"d{depth}p{_prefixes++}");
        return WriteAttributeStringAsync("xmlns", prefix, null, ns);
    }

    internal Task WriteRawAsync(string value) => AsyncWriter.WriteRawAsync(value);
    internal virtual Task WritePrimitiveValueAsync(object value) => AsyncWriter.WriteValueAsync(value);
    internal Task WritePrimitiveValueAsync<T>(object value) where T : struct =>
        this is Json.JsonWriterDelegator jsonWriter
            ? jsonWriter.WriteJsonPrimitiveValueAsync<T>(value)
            : WriteXmlPrimitiveValueAsync<T>(value);

    internal Task WriteXmlPrimitiveValueAsync<T>(object value) where T : struct
    {
        if (typeof(T) == typeof(ulong)) return WriteUnsignedLongAsync((ulong)value);
        if (typeof(T) == typeof(Guid)) return WriteGuidAsync((Guid)value);
        if (typeof(T) == typeof(TimeSpan)) return WriteTimeSpanAsync((TimeSpan)value);
        if (typeof(T) == typeof(DateOnly)) return WriteDateOnlyAsync((DateOnly)value);
        if (typeof(T) == typeof(TimeOnly)) return WriteTimeOnlyAsync((TimeOnly)value);
        if (typeof(T) == typeof(ReflectionPlan.StringValue)) return WriteStringAsync((string)value);
        if (typeof(T) == typeof(ReflectionPlan.ByteArrayValue)) return WriteBase64Async((byte[])value);
        if (typeof(T) == typeof(ReflectionPlan.UriValue)) return WriteUriAsync((Uri)value);
        if (typeof(T) == typeof(ReflectionPlan.QualifiedValue)) return WriteQNameAsync((XmlQualifiedName)value);

        return AsyncWriter.WritePrimitiveValueAsync<T>(value);
    }
    internal virtual Task WriteStringAsync(string? value) => AsyncWriter.WriteStringAsync(value);
    internal virtual Task WriteBooleanAsync(bool value) => AsyncWriter.WritePrimitiveValueAsync<bool>(value);
    internal virtual Task WriteIntAsync(int value) => AsyncWriter.WritePrimitiveValueAsync<int>(value);
    internal virtual Task WriteLongAsync(long value) => AsyncWriter.WritePrimitiveValueAsync<long>(value);
    internal virtual Task WriteFloatAsync(float value) => AsyncWriter.WritePrimitiveValueAsync<float>(value);
    internal virtual Task WriteDoubleAsync(double value) => AsyncWriter.WritePrimitiveValueAsync<double>(value);
    internal virtual Task WriteDecimalAsync(decimal value) => AsyncWriter.WritePrimitiveValueAsync<decimal>(value);
    internal virtual Task WriteDateTimeAsync(DateTime value) => AsyncWriter.WritePrimitiveValueAsync<DateTime>(value);
    internal virtual Task WriteShortAsync(short value) => AsyncWriter.WritePrimitiveValueAsync<int>((int)value);
    internal virtual Task WriteUnsignedShortAsync(ushort value) => AsyncWriter.WritePrimitiveValueAsync<int>((int)value);
    internal virtual Task WriteUnsignedByteAsync(byte value) => AsyncWriter.WritePrimitiveValueAsync<int>((int)value);
    internal virtual Task WriteSignedByteAsync(sbyte value) => AsyncWriter.WritePrimitiveValueAsync<int>((int)value);
    internal virtual Task WriteUnsignedIntAsync(uint value) => AsyncWriter.WritePrimitiveValueAsync<long>((long)value);
    internal virtual Task WriteUnsignedLongAsync(ulong value) => WriteRawAsync(XmlConvert.ToString(value));
    internal virtual Task WriteCharAsync(char value) => AsyncWriter.WritePrimitiveValueAsync<int>((int)value);
    internal Task WriteGuidAsync(Guid value) => WriteRawAsync(value.ToString());
    internal Task WriteTimeSpanAsync(TimeSpan value) => WriteRawAsync(XmlConvert.ToString(value));
    internal Task WriteDateOnlyAsync(DateOnly value) => WriteStringAsync(value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    internal Task WriteTimeOnlyAsync(TimeOnly value) => WriteStringAsync(value.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture));
    internal Task WriteUriAsync(Uri? value) => WriteStringAsync(value?.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped));

    internal virtual Task WriteBase64Async(byte[]? value) =>
        value is null ? Task.CompletedTask : AsyncWriter.WriteBase64Async(value, 0, value.Length);

    internal virtual async Task WriteQNameAsync(XmlQualifiedName value)
    {
        if (value != XmlQualifiedName.Empty)
        {
            await WriteXmlnsAttributeAsync(value.Namespace).ConfigureAwait(false);
            await AsyncWriter.WriteQualifiedNameAsync(value.Name, value.Namespace).ConfigureAwait(false);
        }
    }

    internal Task WriteAnyTypeAsync(object value, Type valueType)
    {
        CancellationToken.ThrowIfCancellationRequested();
        return Type.GetTypeCode(valueType) switch
        {
            TypeCode.Boolean => WriteBooleanAsync((bool)value),
            TypeCode.Char => WriteCharAsync((char)value),
            TypeCode.Byte => WriteUnsignedByteAsync((byte)value),
            TypeCode.SByte => WriteSignedByteAsync((sbyte)value),
            TypeCode.Int16 => WriteShortAsync((short)value),
            TypeCode.UInt16 => WriteUnsignedShortAsync((ushort)value),
            TypeCode.Int32 => WriteIntAsync((int)value),
            TypeCode.UInt32 => WriteUnsignedIntAsync((uint)value),
            TypeCode.Int64 => WriteLongAsync((long)value),
            TypeCode.UInt64 => WriteUnsignedLongAsync((ulong)value),
            TypeCode.Single => WriteFloatAsync((float)value),
            TypeCode.Double => WriteDoubleAsync((double)value),
            TypeCode.Decimal => WriteDecimalAsync((decimal)value),
            TypeCode.DateTime => WriteDateTimeAsync((DateTime)value),
            TypeCode.String => WriteStringAsync((string)value),
            _ => value switch
            {
                byte[] item => WriteBase64Async(item),
                TimeSpan item => WriteTimeSpanAsync(item),
                Guid item => WriteGuidAsync(item),
                Uri item => WriteUriAsync(item),
                XmlQualifiedName item => WriteQNameAsync(item),
                DateOnly item => WriteDateOnlyAsync(item),
                TimeOnly item => WriteTimeOnlyAsync(item),
                _ when valueType == typeof(object) => Task.CompletedTask,
                _ => throw CreateInvalidPrimitiveTypeException(valueType)
            }
        };
    }

    internal Task WriteExtensionDataAsync(IDataNode node) =>
        node.Value is object value ? WriteAnyTypeAsync(value, node.DataType) : Task.CompletedTask;

    internal virtual Task WriteBooleanArrayAsync(bool[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        AsyncWriter.WriteArrayAsync(null, name, ns, value, 0, value.Length);
    internal virtual Task WriteDateTimeArrayAsync(DateTime[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        AsyncWriter.WriteArrayAsync(null, name, ns, value, 0, value.Length);
    internal virtual Task WriteDecimalArrayAsync(decimal[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        AsyncWriter.WriteArrayAsync(null, name, ns, value, 0, value.Length);
    internal virtual Task WriteInt32ArrayAsync(int[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        AsyncWriter.WriteArrayAsync(null, name, ns, value, 0, value.Length);
    internal virtual Task WriteInt64ArrayAsync(long[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        AsyncWriter.WriteArrayAsync(null, name, ns, value, 0, value.Length);
    internal virtual Task WriteSingleArrayAsync(float[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        AsyncWriter.WriteArrayAsync(null, name, ns, value, 0, value.Length);
    internal virtual Task WriteDoubleArrayAsync(double[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        AsyncWriter.WriteArrayAsync(null, name, ns, value, 0, value.Length);

    protected async Task WriteArrayItemsAsync<T>(T[] value, XmlDictionaryString name, XmlDictionaryString? ns, Func<XmlWriterDelegator, T, Task> write)
    {
        foreach (T item in value)
        {
            await WriteStartElementPrimitiveAsync(name, ns).ConfigureAwait(false);
            await write(this, item).ConfigureAwait(false);
            await WriteEndElementPrimitiveAsync().ConfigureAwait(false);
        }
    }
}
