// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization;

internal partial class XmlReaderDelegator
{
    internal ValueTask<object> ReadReferencePrimitiveElementAsync<TPrimitiveKind>(CancellationToken cancellationToken)
        where TPrimitiveKind : struct
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (isEndOfEmptyElement)
        {
            ThrowNotAtElement();
        }
        if (typeof(TPrimitiveKind) == typeof(ReflectionPlan.StringValue) && reader is XmlBinaryReader binaryReader)
        {
            ValueTask<string> value = binaryReader.ReadStringElementAsync(cancellationToken);
            return value.IsCompletedSuccessfully ? new ValueTask<object>(value.Result) : AwaitStringElement(value);
        }
        if (typeof(TPrimitiveKind) == typeof(ReflectionPlan.StringValue) &&
            reader is Json.XmlJsonReader jsonReader && jsonReader.TryReadBufferedPrimitiveElement(out string text))
        {
            ValueTask<bool> next = ReadAsync(cancellationToken);
            if (next.IsCompletedSuccessfully)
            {
                next.GetAwaiter().GetResult();
                return new ValueTask<object>(text);
            }

            return AwaitStringElementRead(next, text);
        }

        return ReadReferencePrimitiveElementCoreAsync<TPrimitiveKind>(cancellationToken);
    }

    private static async ValueTask<object> AwaitStringElementRead(ValueTask<bool> next, string value)
    {
        await next.ConfigureAwait(false);
        return value;
    }

    private static async ValueTask<object> AwaitStringElement(ValueTask<string> value) =>
        await value.ConfigureAwait(false);

    private async ValueTask<object> ReadReferencePrimitiveElementCoreAsync<TPrimitiveKind>(CancellationToken cancellationToken)
        where TPrimitiveKind : struct
    {
        if (await MoveToContentAsync(cancellationToken).ConfigureAwait(false) != XmlNodeType.Element)
        {
            throw XmlObjectSerializerReadContext.CreateUnexpectedStateException(XmlNodeType.Element, this);
        }

        await ReadAsync(cancellationToken).ConfigureAwait(false);
        object value = await ReadReferencePrimitiveContentAsync<TPrimitiveKind>(cancellationToken).ConfigureAwait(false);
        await ReadEndElementAsync(cancellationToken).ConfigureAwait(false);
        return value;
    }

    internal virtual async ValueTask<object> ReadReferencePrimitiveContentAsync<TPrimitiveKind>(CancellationToken cancellationToken)
        where TPrimitiveKind : struct
    {
        if (typeof(TPrimitiveKind) == typeof(ReflectionPlan.StringValue))
            return await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (typeof(TPrimitiveKind) == typeof(ReflectionPlan.ByteArrayValue))
            return await ReadContentAsBase64Async(cancellationToken).ConfigureAwait(false);
        if (typeof(TPrimitiveKind) == typeof(ReflectionPlan.QualifiedValue))
            return ParseQualifiedName(await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false));
        if (typeof(TPrimitiveKind) == typeof(ReflectionPlan.UriValue))
        {
            string value = await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return new Uri(value, UriKind.RelativeOrAbsolute);
            }
            catch (Exception exception) when (exception is FormatException or ArgumentException)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "Uri", exception);
            }
        }

        throw new System.Diagnostics.UnreachableException();
    }

    internal ValueTask<T> ReadPrimitiveElementAsync<T>(CancellationToken cancellationToken) where T : struct
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (isEndOfEmptyElement)
        {
            ThrowNotAtElement();
        }
        if ((reader is XmlBinaryReader or XmlUTF8TextReader && typeof(T) == typeof(char)) ||
            reader is XmlBinaryReader or XmlUTF8TextReader or Json.XmlJsonReader &&
            (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte) ||
            typeof(T) == typeof(short) || typeof(T) == typeof(ushort) || typeof(T) == typeof(uint)))
        {
            return ReadConvertedPrimitiveElementAsync<T>(cancellationToken);
        }
        if (reader is XmlBinaryReader binaryReader && (typeof(T) == typeof(bool) || typeof(T) == typeof(int) ||
            typeof(T) == typeof(long) || typeof(T) == typeof(float) || typeof(T) == typeof(double) ||
            typeof(T) == typeof(decimal) || typeof(T) == typeof(DateTime)))
        {
            return binaryReader.ReadPrimitiveElementAsync<T>(cancellationToken);
        }
        if ((typeof(T) == typeof(bool) || typeof(T) == typeof(int) ||
            typeof(T) == typeof(long) || typeof(T) == typeof(float) || typeof(T) == typeof(double) ||
            typeof(T) == typeof(decimal) || typeof(T) == typeof(DateTime)) &&
            (reader is XmlUTF8TextReader textReader && textReader.TryReadBufferedPrimitiveElement(out T value) ||
                typeof(T) != typeof(DateTime) && reader is Json.XmlJsonReader jsonReader && jsonReader.TryReadBufferedPrimitiveElement(out value)))
        {
            ValueTask<bool> next = ReadAsync(cancellationToken);
            if (next.IsCompletedSuccessfully)
            {
                next.GetAwaiter().GetResult();
                return new ValueTask<T>(value);
            }

            return AwaitPrimitiveElementRead(next, value);
        }

        return ReadPrimitiveElementCoreAsync<T>(cancellationToken);
    }

    private ValueTask<T> ReadConvertedPrimitiveElementAsync<T>(CancellationToken cancellationToken) where T : struct
    {
        if (typeof(T) == typeof(uint))
        {
            if (reader is XmlBinaryReader bufferedReader &&
                bufferedReader.TryReadBufferedPrimitiveElement(cancellationToken, out long bufferedValue, out ValueTask<bool> next))
            {
                if (next.IsCompletedSuccessfully)
                {
                    next.GetAwaiter().GetResult();
                    return new ValueTask<T>((T)(object)ToUInt32(bufferedValue));
                }

                return AwaitBufferedUnsignedPrimitiveElement<T>(next, bufferedValue);
            }

            ValueTask<long> value = reader is XmlBinaryReader binaryReader
                ? binaryReader.ReadPrimitiveElementAsync<long>(cancellationToken)
                : ReadPrimitiveElementAsync<long>(cancellationToken);
            return value.IsCompletedSuccessfully
                ? new ValueTask<T>((T)(object)ToUInt32(value.Result))
                : AwaitUnsignedPrimitiveElement<T>(value);
        }

        if (reader is XmlBinaryReader signedBufferedReader &&
            signedBufferedReader.TryReadBufferedPrimitiveElement(cancellationToken, out int bufferedSignedValue, out ValueTask<bool> signedNext))
        {
            if (signedNext.IsCompletedSuccessfully)
            {
                signedNext.GetAwaiter().GetResult();
                return new ValueTask<T>(ConvertPrimitiveElement<T>(bufferedSignedValue));
            }

            return AwaitBufferedConvertedPrimitiveElement<T>(signedNext, bufferedSignedValue);
        }

        ValueTask<int> signedValue = reader is XmlBinaryReader signedBinaryReader
            ? signedBinaryReader.ReadPrimitiveElementAsync<int>(cancellationToken)
            : ReadPrimitiveElementAsync<int>(cancellationToken);
        return signedValue.IsCompletedSuccessfully
            ? new ValueTask<T>(ConvertPrimitiveElement<T>(signedValue.Result))
            : AwaitConvertedPrimitiveElement<T>(signedValue);
    }

    private T ConvertPrimitiveElement<T>(int value) where T : struct
    {
        if (typeof(T) == typeof(char))
            return (T)(object)ToChar(value);
        if (typeof(T) == typeof(byte))
            return (T)(object)ToByte(value);
        if (typeof(T) == typeof(sbyte))
            return (T)(object)ToSByte(value);
        if (typeof(T) == typeof(short))
            return (T)(object)ToShort(value);

        return (T)(object)ToUInt16(value);
    }

    private async ValueTask<T> AwaitUnsignedPrimitiveElement<T>(ValueTask<long> value) where T : struct =>
        (T)(object)ToUInt32(await value.ConfigureAwait(false));

    private async ValueTask<T> AwaitConvertedPrimitiveElement<T>(ValueTask<int> value) where T : struct =>
        ConvertPrimitiveElement<T>(await value.ConfigureAwait(false));

    private async ValueTask<T> AwaitBufferedUnsignedPrimitiveElement<T>(ValueTask<bool> next, long value) where T : struct
    {
        await next.ConfigureAwait(false);
        return (T)(object)ToUInt32(value);
    }

    private async ValueTask<T> AwaitBufferedConvertedPrimitiveElement<T>(ValueTask<bool> next, int value) where T : struct
    {
        await next.ConfigureAwait(false);
        return ConvertPrimitiveElement<T>(value);
    }

    private static async ValueTask<T> AwaitPrimitiveElementRead<T>(ValueTask<bool> next, T value)
    {
        await next.ConfigureAwait(false);
        return value;
    }

    private async ValueTask<T> ReadPrimitiveElementCoreAsync<T>(CancellationToken cancellationToken) where T : struct
    {
        if (await MoveToContentAsync(cancellationToken).ConfigureAwait(false) != XmlNodeType.Element)
        {
            throw XmlObjectSerializerReadContext.CreateUnexpectedStateException(XmlNodeType.Element, this);
        }

        await ReadAsync(cancellationToken).ConfigureAwait(false);
        T value = await ReadPrimitiveContentAsync<T>(cancellationToken).ConfigureAwait(false);
        await ReadEndElementAsync(cancellationToken).ConfigureAwait(false);
        return value;
    }

    internal virtual ValueTask<T> ReadPrimitiveContentAsync<T>(CancellationToken cancellationToken) where T : struct
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (typeof(T) == typeof(bool) || typeof(T) == typeof(int) || typeof(T) == typeof(long) ||
            typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(decimal) || typeof(T) == typeof(DateTime))
        {
            return ReadTypedPrimitiveContentAsync<T>(cancellationToken);
        }

        return ReadConvertedPrimitiveContentAsync<T>(cancellationToken);
    }

    private async ValueTask<T> ReadConvertedPrimitiveContentAsync<T>(CancellationToken cancellationToken) where T : struct
    {
        if (typeof(T) == typeof(char))
            return (T)(object)ToChar(await ReadTypedPrimitiveContentAsync<int>(cancellationToken).ConfigureAwait(false));
        if (typeof(T) == typeof(byte))
            return (T)(object)ToByte(await ReadTypedPrimitiveContentAsync<int>(cancellationToken).ConfigureAwait(false));
        if (typeof(T) == typeof(sbyte))
            return (T)(object)ToSByte(await ReadTypedPrimitiveContentAsync<int>(cancellationToken).ConfigureAwait(false));
        if (typeof(T) == typeof(short))
            return (T)(object)ToShort(await ReadTypedPrimitiveContentAsync<int>(cancellationToken).ConfigureAwait(false));
        if (typeof(T) == typeof(ushort))
            return (T)(object)ToUInt16(await ReadTypedPrimitiveContentAsync<int>(cancellationToken).ConfigureAwait(false));
        if (typeof(T) == typeof(uint))
            return (T)(object)ToUInt32(await ReadTypedPrimitiveContentAsync<long>(cancellationToken).ConfigureAwait(false));
        if (typeof(T) == typeof(ulong))
            return (T)(object)XmlConverter.ToUInt64(await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false));
        if (typeof(T) == typeof(Guid))
            return (T)(object)XmlConverter.ToGuid(await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false));
        if (typeof(T) == typeof(TimeSpan))
            return (T)(object)XmlConverter.ToTimeSpan(await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false));
        if (typeof(T) == typeof(DateOnly) || typeof(T) == typeof(TimeOnly))
        {
            string value = await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (typeof(T) == typeof(DateOnly))
                    return (T)(object)ParseDateOnly(value);

                return (T)(object)TimeOnly.FromTimeSpan(XmlConvert.ToDateTimeOffset(value).TimeOfDay);
            }
            catch (Exception exception) when (exception is FormatException or ArgumentException)
            {
                ThrowConversionException(value, typeof(T).Name);
                throw;
            }
        }

        throw CreateInvalidPrimitiveTypeException(typeof(T));
    }

    private ValueTask<T> ReadTypedPrimitiveContentAsync<T>(CancellationToken cancellationToken)
    {
        if (isEndOfEmptyElement)
        {
            ThrowConversionException(string.Empty, typeof(T).Name);
        }
        if (reader is ExtensionDataReader || reader.GetType() == typeof(XmlNodeReader))
        {
            return new ValueTask<T>((T)reader.ReadContentAs(typeof(T), null));
        }
        if (reader is XmlBaseReader baseReader)
        {
            return baseReader.ReadContentAsPrimitiveAsync<T>(cancellationToken);
        }

        return ReadExternalTypedPrimitiveContentAsync<T>();
    }

    private async ValueTask<T> ReadExternalTypedPrimitiveContentAsync<T>()
    {
        return (T)await reader.ReadContentAsAsync(typeof(T), null).ConfigureAwait(false);
    }

    internal async ValueTask<object> ReadElementContentAsAnyTypeAsync(Type valueType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (isEndOfEmptyElement)
        {
            ThrowNotAtElement();
        }
        if (reader is XmlBinaryReader binaryReader && Type.GetTypeCode(valueType) is TypeCode.Boolean or
            TypeCode.Int32 or TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal or TypeCode.DateTime)
        {
            return await binaryReader.ReadElementContentAsAsync(valueType, null, cancellationToken).ConfigureAwait(false);
        }
        if (await MoveToContentAsync(cancellationToken).ConfigureAwait(false) != XmlNodeType.Element)
        {
            throw XmlObjectSerializerReadContext.CreateUnexpectedStateException(XmlNodeType.Element, this);
        }

        await ReadAsync(cancellationToken).ConfigureAwait(false);
        object value = await ReadContentAsAnyTypeAsync(valueType, cancellationToken).ConfigureAwait(false);
        await ReadEndElementAsync(cancellationToken).ConfigureAwait(false);
        return value;
    }

    internal virtual async ValueTask<object> ReadContentAsAnyTypeAsync(Type valueType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        switch (Type.GetTypeCode(valueType))
        {
            case TypeCode.Boolean:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal:
            case TypeCode.DateTime:
                return await ReadTypedContentAsync(valueType, cancellationToken).ConfigureAwait(false);
            case TypeCode.Char:
                return ToChar((int)await ReadTypedContentAsync(typeof(int), cancellationToken).ConfigureAwait(false));
            case TypeCode.Byte:
                return ToByte((int)await ReadTypedContentAsync(typeof(int), cancellationToken).ConfigureAwait(false));
            case TypeCode.SByte:
                return ToSByte((int)await ReadTypedContentAsync(typeof(int), cancellationToken).ConfigureAwait(false));
            case TypeCode.Int16:
                return ToShort((int)await ReadTypedContentAsync(typeof(int), cancellationToken).ConfigureAwait(false));
            case TypeCode.UInt16:
                return ToUInt16((int)await ReadTypedContentAsync(typeof(int), cancellationToken).ConfigureAwait(false));
            case TypeCode.UInt32:
                return ToUInt32((long)await ReadTypedContentAsync(typeof(long), cancellationToken).ConfigureAwait(false));
            case TypeCode.UInt64:
                return XmlConverter.ToUInt64(await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false));
            case TypeCode.String:
                return await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        if (valueType == typeof(byte[]))
        {
            return await ReadContentAsBase64Async(cancellationToken).ConfigureAwait(false);
        }
        if (valueType == typeof(object))
        {
            return new object();
        }
        if (valueType == typeof(TimeSpan))
        {
            return XmlConverter.ToTimeSpan(await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false));
        }
        if (valueType == typeof(Guid))
        {
            return XmlConverter.ToGuid(await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false));
        }
        if (valueType == typeof(Uri))
        {
            string value = await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return new Uri(value, UriKind.RelativeOrAbsolute);
            }
            catch (Exception exception) when (exception is FormatException or ArgumentException)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "Uri", exception);
            }
        }
        if (valueType == typeof(XmlQualifiedName))
        {
            return ParseQualifiedName(await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false));
        }
        if (valueType == typeof(DateOnly) || valueType == typeof(TimeOnly))
        {
            string value = await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return valueType == typeof(DateOnly) ? ParseDateOnly(value) :
                    TimeOnly.FromTimeSpan(XmlConvert.ToDateTimeOffset(value).TimeOfDay);
            }
            catch (Exception exception) when (exception is FormatException or ArgumentException)
            {
                ThrowConversionException(value, valueType.Name);
                throw;
            }
        }

        throw CreateInvalidPrimitiveTypeException(valueType);
    }

    private async ValueTask<object> ReadTypedContentAsync(Type type, CancellationToken cancellationToken)
    {
        if (isEndOfEmptyElement)
        {
            ThrowConversionException(string.Empty, type.Name);
        }
        if (reader is ExtensionDataReader || reader.GetType() == typeof(XmlNodeReader))
        {
            return reader.ReadContentAs(type, null);
        }

        return reader is XmlBaseReader baseReader
            ? await baseReader.ReadContentAsAsync(type, null, cancellationToken).ConfigureAwait(false)
            : await reader.ReadContentAsAsync(type, null).ConfigureAwait(false);
    }

    internal virtual async ValueTask<byte[]> ReadContentAsBase64Async(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (isEndOfEmptyElement)
        {
            return Array.Empty<byte>();
        }
        if (reader is XmlBaseReader baseReader)
        {
            return await baseReader.ReadContentAsBase64Async(cancellationToken).ConfigureAwait(false);
        }
        if (dictionaryReader is null)
        {
            return ReadContentAsBase64(await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false))!;
        }

        int quota = dictionaryReader.Quotas.MaxArrayLength;
        byte[] buffer = new byte[Math.Min(512, Math.Max(1, quota))];
        using MemoryStream content = new MemoryStream();
        int count;
        while ((count = await dictionaryReader.ReadContentAsBase64Async(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
        {
            if (count > quota - content.Length)
            {
                XmlExceptionHelper.ThrowMaxArrayLengthExceeded(dictionaryReader, quota);
            }
            content.Write(buffer, 0, count);
        }

        return content.ToArray();
    }

    internal async ValueTask<IDataNode> ReadExtensionDataAsync(Type valueType, CancellationToken cancellationToken)
    {
        object value = await ReadContentAsAnyTypeAsync(valueType, cancellationToken).ConfigureAwait(false);
        return value switch
        {
            bool item => new DataNode<bool>(item),
            char item => new DataNode<char>(item),
            byte item => new DataNode<byte>(item),
            sbyte item => new DataNode<sbyte>(item),
            short item => new DataNode<short>(item),
            ushort item => new DataNode<ushort>(item),
            int item => new DataNode<int>(item),
            uint item => new DataNode<uint>(item),
            long item => new DataNode<long>(item),
            ulong item => new DataNode<ulong>(item),
            float item => new DataNode<float>(item),
            double item => new DataNode<double>(item),
            decimal item => new DataNode<decimal>(item),
            DateTime item => new DataNode<DateTime>(item),
            string item => new DataNode<string>(item),
            byte[] item => new DataNode<byte[]>(item),
            TimeSpan item => new DataNode<TimeSpan>(item),
            Guid item => new DataNode<Guid>(item),
            Uri item => new DataNode<Uri>(item),
            XmlQualifiedName item => new DataNode<XmlQualifiedName>(item),
            DateOnly item => new DataNode<DateOnly>(item),
            TimeOnly item => new DataNode<TimeOnly>(item),
            _ => new DataNode<object>(value)
        };
    }

    internal ValueTask<object?> ReadPrimitiveArrayAsync<T>(XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, int size)
        where T : unmanaged
    {
        // These instantiations are closed over the built-in primitive types, never a reflected user value type.
        CancellationToken cancellationToken = context.CancellationToken;
        string objectId = context.GetObjectId();
        if (size >= 0)
        {
            context.IncrementItemCount(size);
        }
        int quota = size >= 0 ? size : GetArrayLengthQuota(context);
        XmlBinaryReader? binaryReader = reader as XmlBinaryReader;
        int packedCount = binaryReader?.GetArrayLength<T>(itemName, itemNamespace) ?? 0;
        int capacity = size >= 0 ? size : packedCount > 0 ? Math.Min(packedCount, 32) : 32;
        if (size < 0 && packedCount > 0 && binaryReader!.TryGetArrayLength(out _))
        {
            if (packedCount > quota)
            {
                XmlExceptionHelper.ThrowMaxArrayLengthExceeded(binaryReader, quota);
            }
            capacity = packedCount;
        }
        T[] items = new T[Math.Min(capacity, quota)];
        if (packedCount > 0 && packedCount == items.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (size < 0)
                context.IncrementItemCount(packedCount);
            ValueTask pending = binaryReader!.ReadArrayAsync(items, 0, packedCount, cancellationToken);
            if (!pending.IsCompletedSuccessfully)
                return AwaitPackedPrimitiveArray(pending, context, itemName, itemNamespace, size, quota, objectId, items);

            pending.GetAwaiter().GetResult();
            int count = packedCount;
            if (NodeType == XmlNodeType.EndElement)
                return new ValueTask<object?>(CompletePrimitiveArray(context, itemName, itemNamespace, size, objectId, items, count));

            return ReadPrimitiveArrayCoreAsync(context, itemName, itemNamespace, size, quota, objectId, items, count);
        }

        return ReadPrimitiveArrayCoreAsync(context, itemName, itemNamespace, size, quota, objectId, items, 0);
    }

    private async ValueTask<object?> AwaitPackedPrimitiveArray<T>(ValueTask pending, XmlObjectSerializerReadContext context,
        XmlDictionaryString itemName, XmlDictionaryString itemNamespace, int size, int quota, string objectId, T[] items) where T : unmanaged
    {
        await pending.ConfigureAwait(false);
        int count = items.Length;
        if (NodeType == XmlNodeType.EndElement)
            return CompletePrimitiveArray(context, itemName, itemNamespace, size, objectId, items, count);

        return await ReadPrimitiveArrayCoreAsync(context, itemName, itemNamespace, size, quota, objectId, items, count).ConfigureAwait(false);
    }

    private async ValueTask<object?> ReadPrimitiveArrayCoreAsync<T>(XmlObjectSerializerReadContext context,
        XmlDictionaryString itemName, XmlDictionaryString itemNamespace, int size, int quota, string objectId, T[] items, int count) where T : unmanaged
    {
        CancellationToken cancellationToken = context.CancellationToken;
        XmlBinaryReader? binaryReader = reader as XmlBinaryReader;
        while (NodeType == XmlNodeType.Element || await MoveToContentAsync(cancellationToken).ConfigureAwait(false) != XmlNodeType.EndElement)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (NodeType != XmlNodeType.Element || !IsLocalName(itemName) || !IsNamespaceUri(itemNamespace))
            {
                if (NodeType != XmlNodeType.Element && !await IsStartElementAsync(cancellationToken).ConfigureAwait(false))
                {
                    throw XmlObjectSerializerReadContext.CreateUnexpectedStateException(XmlNodeType.Element, this);
                }
                await context.SkipUnknownElementAsync(this).ConfigureAwait(false);
                continue;
            }
            if (count == quota)
            {
                if (size >= 0)
                {
                    throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ArrayExceededSizeAttribute, size, itemName.Value, itemNamespace.Value));
                }
                XmlExceptionHelper.ThrowMaxArrayLengthExceeded(dictionaryReader ?? XmlDictionaryReader.CreateDictionaryReader(reader), quota);
            }
            if (count == items.Length)
            {
                int newCapacity = Math.Min(quota, items.Length <= quota / 2 ? Math.Max(1, items.Length * 2) : quota);
                Array.Resize(ref items, newCapacity);
            }
            int packedCount = binaryReader?.GetArrayLength<T>(itemName, itemNamespace) ?? 0;
            if (packedCount > 0)
            {
                if (packedCount > quota - count)
                {
                    if (size >= 0)
                    {
                        throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ArrayExceededSizeAttribute, size, itemName.Value, itemNamespace.Value));
                    }
                    XmlExceptionHelper.ThrowMaxArrayLengthExceeded(binaryReader!, quota);
                }
                int actual = Math.Min(packedCount, items.Length - count);
                if (size < 0)
                {
                    context.IncrementItemCount(actual);
                }
                await binaryReader!.ReadArrayAsync(items, count, actual, cancellationToken).ConfigureAwait(false);
                count += actual;
                continue;
            }
            if (size < 0)
            {
                context.IncrementItemCount(1);
            }
            items[count++] = await ReadPrimitiveElementAsync<T>(cancellationToken).ConfigureAwait(false);
        }
        return CompletePrimitiveArray(context, itemName, itemNamespace, size, objectId, items, count);
    }

    private object CompletePrimitiveArray<T>(XmlObjectSerializerReadContext context,
        XmlDictionaryString itemName, XmlDictionaryString itemNamespace, int size, string objectId, T[] items, int count)
    {
        if (size >= 0)
        {
            CheckActualArrayLength(size, count, itemName, itemNamespace);
        }
        items = XmlObjectSerializerReadContext.TrimArraySize(items, count);
        context.AddNewObjectWithId(objectId, items);
        return items;
    }
}
