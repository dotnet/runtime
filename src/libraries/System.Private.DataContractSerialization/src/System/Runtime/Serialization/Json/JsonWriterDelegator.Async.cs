// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed partial class JsonWriterDelegator
{
    internal Task WriteJsonPrimitiveElementAsync<T>(object value, XmlDictionaryString name, XmlDictionaryString? ns) where T : struct =>
        AsyncWriter is XmlJsonWriter jsonWriter && jsonWriter.TryWritePrimitiveElement<T, object>(name, value, out Task? operation)
            ? operation : WriteJsonPrimitiveElementAsyncCore<T>(value, name, ns);

    internal Task WriteTypedJsonPrimitiveElementAsync<T>(T value, XmlDictionaryString name, XmlDictionaryString? ns) where T : struct =>
        AsyncWriter is XmlJsonWriter jsonWriter && jsonWriter.TryWritePrimitiveElement<T, T>(name, value, out Task? operation, this)
            ? operation : WriteJsonPrimitiveElementAsyncCore<T>(value, name, ns);

    internal bool UsesDefaultDateTimeFormat => _dateTimeFormat is null;

    internal override Task WriteStartElementAsync(string? prefix, string name, string? ns) =>
        name is { Length: 0 } ? WriteEmptyNameStartElementAsync() : base.WriteStartElementAsync(prefix, name, ns);

    private async Task WriteEmptyNameStartElementAsync()
    {
        await base.WriteStartElementAsync(null, JsonGlobals.itemString, JsonGlobals.itemString).ConfigureAwait(false);
        await WriteAttributeStringAsync(null, JsonGlobals.itemString, null, string.Empty).ConfigureAwait(false);
    }

    private async Task WriteJsonPrimitiveElementAsyncCore<T>(object value, XmlDictionaryString name, XmlDictionaryString? ns) where T : struct
    {
        await WriteStartElementPrimitiveAsync(name, ns).ConfigureAwait(false);
        await WritePrimitiveValueAsync<T>(value).ConfigureAwait(false);
        await WriteEndElementPrimitiveAsync().ConfigureAwait(false);
    }

    internal override Task WritePrimitiveValueAsync(object value) => WriteAnyTypeAsync(value, value.GetType());

    internal Task WriteJsonPrimitiveValueAsync<T>(object value) where T : struct
    {
        if (typeof(T) == typeof(char)) return WriteCharAsync((char)value);
        if (typeof(T) == typeof(DateTime)) return WriteDateTimeAsync((DateTime)value);
        if (XmlDictionaryWriter.IsInt32Primitive<T>() || typeof(T) == typeof(bool) ||
            typeof(T) == typeof(uint) || typeof(T) == typeof(long) || typeof(T) == typeof(ulong) ||
            typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(decimal))
        {
            return WriteNumberAsync<T>(value);
        }

        return WriteXmlPrimitiveValueAsync<T>(value);
    }

    private async Task WriteNumberAsync<T>(object value) where T : struct
    {
        await WriteAttributeStringAsync(null, JsonGlobals.typeString, null,
            typeof(T) == typeof(bool) ? JsonGlobals.booleanString : JsonGlobals.numberString).ConfigureAwait(false);
        await AsyncWriter.WritePrimitiveValueAsync<T>(value, CancellationToken).ConfigureAwait(false);
    }

    private async Task WriteNumberAsync(object value)
    {
        await WriteAttributeStringAsync(null, JsonGlobals.typeString, null, JsonGlobals.numberString).ConfigureAwait(false);
        await AsyncWriter.WriteValueAsync(value, CancellationToken).ConfigureAwait(false);
    }

    internal override async Task WriteBooleanAsync(bool value)
    {
        await WriteAttributeStringAsync(null, JsonGlobals.typeString, null, JsonGlobals.booleanString).ConfigureAwait(false);
        await base.WriteBooleanAsync(value).ConfigureAwait(false);
    }

    internal override Task WriteIntAsync(int value) => WriteNumberAsync(value);
    internal override Task WriteLongAsync(long value) => WriteNumberAsync(value);
    internal override Task WriteDecimalAsync(decimal value) => WriteNumberAsync(value);
    internal override Task WriteFloatAsync(float value) => WriteNumberAsync(value);
    internal override Task WriteDoubleAsync(double value) => WriteNumberAsync(value);
    internal override Task WriteShortAsync(short value) => WriteNumberAsync((int)value);
    internal override Task WriteUnsignedShortAsync(ushort value) => WriteNumberAsync((int)value);
    internal override Task WriteSignedByteAsync(sbyte value) => WriteNumberAsync((int)value);
    internal override Task WriteUnsignedByteAsync(byte value) => WriteNumberAsync((int)value);
    internal override Task WriteUnsignedIntAsync(uint value) => WriteNumberAsync((long)value);
    internal override Task WriteUnsignedLongAsync(ulong value) => WriteNumberAsync((decimal)value);
    internal override Task WriteCharAsync(char value) => WriteStringAsync(XmlConvert.ToString(value));

    internal override Task WriteQNameAsync(XmlQualifiedName value) =>
        value == XmlQualifiedName.Empty ? Task.CompletedTask :
            WriteStringAsync(string.Concat(value.Name, JsonGlobals.NameValueSeparatorString, value.Namespace));

    internal override async Task WriteBase64Async(byte[]? value)
    {
        if (value is not null)
        {
            await WriteAttributeStringAsync(null, JsonGlobals.typeString, null, JsonGlobals.arrayString).ConfigureAwait(false);
            await WritePrimitiveArrayAsync(value, JsonGlobals.itemDictionaryString, null).ConfigureAwait(false);
        }
    }

    internal override async Task WriteDateTimeAsync(DateTime value)
    {
        if (_dateTimeFormat is not null)
        {
            await WriteStringAsync(value.ToString(_dateTimeFormat.FormatString, _dateTimeFormat.FormatProvider)).ConfigureAwait(false);
            return;
        }

        CheckDateTimeRange(value);
        await WriteStringAsync(JsonGlobals.DateTimeStartGuardReader).ConfigureAwait(false);
        await WriteStringAsync(XmlConvert.ToString((value.ToUniversalTime().Ticks - JsonGlobals.unixEpochTicks) / 10000)).ConfigureAwait(false);
        if (value.Kind is DateTimeKind.Unspecified or DateTimeKind.Local)
        {
            TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(value.ToLocalTime());
            await WriteStringAsync(string.Create(CultureInfo.InvariantCulture, $"{offset.Hours:+00;-00}{offset.Minutes:00;00}")).ConfigureAwait(false);
        }
        await WriteStringAsync(JsonGlobals.DateTimeEndGuardReader).ConfigureAwait(false);
    }

    internal override Task WriteBooleanArrayAsync(bool[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        WritePrimitiveArrayAsync(value, name, ns);
    internal override Task WriteDateTimeArrayAsync(DateTime[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        WriteArrayItemsAsync(value, name, ns, static (writer, item) => writer.WriteDateTimeAsync(item));
    internal override Task WriteDecimalArrayAsync(decimal[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        WritePrimitiveArrayAsync(value, name, ns);
    internal override Task WriteInt32ArrayAsync(int[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        WritePrimitiveArrayAsync(value, name, ns);
    internal override Task WriteInt64ArrayAsync(long[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        WritePrimitiveArrayAsync(value, name, ns);
    internal override Task WriteSingleArrayAsync(float[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        WritePrimitiveArrayAsync(value, name, ns);
    internal override Task WriteDoubleArrayAsync(double[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        WritePrimitiveArrayAsync(value, name, ns);

    private Task WritePrimitiveArrayAsync<T>(T[] values, XmlDictionaryString name, XmlDictionaryString? ns) where T : struct
    {
        for (int index = 0; index < values.Length; index++)
        {
            Task operation = WriteTypedJsonPrimitiveElementAsync(values[index], name, ns);
            if (!operation.IsCompletedSuccessfully)
                return AwaitPrimitiveArrayAsync(operation, values, index + 1, name, ns);
        }

        return Task.CompletedTask;
    }

    private async Task AwaitPrimitiveArrayAsync<T>(Task operation, T[] values, int index, XmlDictionaryString name,
        XmlDictionaryString? ns) where T : struct
    {
        await operation.ConfigureAwait(false);
        for (; index < values.Length; index++)
            await WriteTypedJsonPrimitiveElementAsync(values[index], name, ns).ConfigureAwait(false);
    }
}
