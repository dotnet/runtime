// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization.DataContracts;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization;

internal readonly struct AsyncReflectionSerialization : IReflectionSerialization
{
    public static ValueTask<T> ReadPrimitiveElement<T>(XmlReaderDelegator reader) where T : struct =>
        reader.ReadPrimitiveElementAsync<T>(reader.CancellationToken);

    public static Task WriteStartElement(XmlWriterDelegator writer, string? prefix, XmlDictionaryString name, XmlDictionaryString? ns) =>
        writer.WriteStartElementAsync(prefix, name, ns);
    public static Task WriteStartElementString(XmlWriterDelegator writer, string? prefix, string name, string? ns) =>
        writer.WriteStartElementAsync(prefix, name, ns);
    public static Task WriteEndElement(XmlWriterDelegator writer) => writer.WriteEndElementAsync();
    public static Task WriteNamespaceDecl(XmlWriterDelegator writer, XmlDictionaryString ns) => writer.WriteNamespaceDeclAsync(ns);
    public static Task WriteJsonNameWithMapping(XmlWriterDelegator writer, XmlDictionaryString[] names, int index) =>
        Json.XmlObjectSerializerWriteContextComplexJson.WriteJsonNameWithMappingAsync(writer, names, index);
    public static Task WriteAttributeString(XmlWriterDelegator writer, string? prefix, string name, string? ns, string value) =>
        writer.WriteAttributeStringAsync(prefix, name, ns, value);

    public static Task IncrementCollectionCount(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context, int count)
    {
        context.IncrementItemCount(count);
        return context.WriteArraySizeAsync(writer, count);
    }

    public static Task WriteBooleanArray(XmlWriterDelegator writer, bool[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        writer.WriteBooleanArrayAsync(value, name, ns);
    public static Task WriteDateTimeArray(XmlWriterDelegator writer, DateTime[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        writer.WriteDateTimeArrayAsync(value, name, ns);
    public static Task WriteDecimalArray(XmlWriterDelegator writer, decimal[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        writer.WriteDecimalArrayAsync(value, name, ns);
    public static Task WriteInt32Array(XmlWriterDelegator writer, int[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        writer.WriteInt32ArrayAsync(value, name, ns);
    public static Task WriteInt64Array(XmlWriterDelegator writer, long[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        writer.WriteInt64ArrayAsync(value, name, ns);
    public static Task WriteSingleArray(XmlWriterDelegator writer, float[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        writer.WriteSingleArrayAsync(value, name, ns);
    public static Task WriteDoubleArray(XmlWriterDelegator writer, double[] value, XmlDictionaryString name, XmlDictionaryString? ns) =>
        writer.WriteDoubleArrayAsync(value, name, ns);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static Task WriteXmlValue(DataContract contract, XmlWriterDelegator writer, object value, XmlObjectSerializerWriteContext context) =>
        contract.WriteXmlValueAsync(writer, value, context, context.CancellationToken);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static Task WriteJsonValue(Json.JsonDataContract contract, XmlWriterDelegator writer, object value, Json.XmlObjectSerializerWriteContextComplexJson context, RuntimeTypeHandle declaredTypeHandle) =>
        contract.WriteJsonValueAsync(writer, value, context, declaredTypeHandle, context.CancellationToken);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static async Task WriteXmlElement(PrimitiveDataContract contract, XmlWriterDelegator writer, object? value, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
    {
        Type type = contract.UnderlyingType;
        if (type == typeof(string) || type == typeof(Uri) || type == typeof(XmlQualifiedName))
        {
            await context.WriteReferencePrimitiveElementAsync(writer, value, type, name, ns).ConfigureAwait(false);
            return;
        }
        if (type == typeof(byte[]))
        {
            await writer.WriteStartElementAsync(null, name, ns).ConfigureAwait(false);
            await writer.WriteBase64Async((byte[]?)value).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            return;
        }

        await writer.WriteStartElementPrimitiveAsync(name, ns).ConfigureAwait(false);
        await contract.WriteXmlValueAsync(writer, value!, context, context.CancellationToken).ConfigureAwait(false);
        await writer.WriteEndElementPrimitiveAsync().ConfigureAwait(false);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static Task WriteNull(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context, Type type, bool isSerializable) =>
        context.WriteNullAsync(writer, type, isSerializable);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static Task SerializeValue(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context, object value, bool isDeclaredType, bool writeXsiType, int declaredTypeId, RuntimeTypeHandle declaredTypeHandle) =>
        context.InternalSerializeAsync(writer, value, isDeclaredType, writeXsiType, declaredTypeId, declaredTypeHandle);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static Task SerializeReference(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context, object value, bool isDeclaredType, bool writeXsiType, int declaredTypeId, RuntimeTypeHandle declaredTypeHandle) =>
        context.InternalSerializeReferenceAsync(writer, value, isDeclaredType, writeXsiType, declaredTypeId, declaredTypeHandle);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static Task WriteExtensionData(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context, ExtensionDataObject? extensionData, int memberIndex) =>
        context.WriteExtensionDataAsync(writer, extensionData, memberIndex);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static Task WriteISerializable(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context, ISerializable value) =>
        context.WriteISerializableAsync(writer, value);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static ValueTask<object?> Deserialize(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, int declaredTypeId, RuntimeTypeHandle declaredTypeHandle, string name, string ns) =>
        context.InternalDeserializeAsync(reader, declaredTypeId, declaredTypeHandle, name, ns);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static ValueTask<object?> ReadXmlValue(DataContract contract, XmlReaderDelegator reader, XmlObjectSerializerReadContext context) =>
        contract.ReadXmlValueAsync(reader, context, context.CancellationToken);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static ValueTask<string?> ReadIfNullOrRef(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, Type type, bool isSerializable) =>
        context.ReadIfNullOrRefAsync(reader, type, isSerializable);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static ValueTask<SerializationInfo> ReadSerializationInfo(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, Type type) =>
        context.ReadSerializationInfoAsync(reader, type);

    public static async ValueTask<bool> MoveToNextElement(XmlReaderDelegator reader) =>
        await reader.MoveToContentAsync(reader.CancellationToken).ConfigureAwait(false) != XmlNodeType.EndElement;
    public static ValueTask<bool> IsStartElement(XmlReaderDelegator reader, XmlDictionaryString name, XmlDictionaryString ns) =>
        reader.IsStartElementAsync(name, ns, reader.CancellationToken);
    public static ValueTask<bool> IsStartElement(XmlReaderDelegator reader) => reader.IsStartElementAsync(reader.CancellationToken);
    public static ValueTask SkipUnknownElement(XmlReaderDelegator reader, XmlObjectSerializerReadContext context) => context.SkipUnknownElementAsync(reader);

    public static ValueTask<object?> ReadBooleanArray(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size) =>
        reader.ReadPrimitiveArrayAsync<bool>(context, name, ns, size);
    public static ValueTask<object?> ReadDateTimeArray(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size) =>
        reader.ReadPrimitiveArrayAsync<DateTime>(context, name, ns, size);
    public static ValueTask<object?> ReadDecimalArray(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size) =>
        reader.ReadPrimitiveArrayAsync<decimal>(context, name, ns, size);
    public static ValueTask<object?> ReadInt32Array(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size) =>
        reader.ReadPrimitiveArrayAsync<int>(context, name, ns, size);
    public static ValueTask<object?> ReadInt64Array(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size) =>
        reader.ReadPrimitiveArrayAsync<long>(context, name, ns, size);
    public static ValueTask<object?> ReadSingleArray(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size) =>
        reader.ReadPrimitiveArrayAsync<float>(context, name, ns, size);
    public static ValueTask<object?> ReadDoubleArray(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString name, XmlDictionaryString ns, int size) =>
        reader.ReadPrimitiveArrayAsync<double>(context, name, ns, size);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static ValueTask<object?> ReadJsonValue(Json.JsonDataContract contract, XmlReaderDelegator reader, Json.XmlObjectSerializerReadContextComplexJson context) =>
        contract.ReadJsonValueAsync(reader, context, context.CancellationToken);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static ValueTask<int> GetMemberIndex(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString[] names, XmlDictionaryString[] namespaces, int memberIndex, ExtensionDataObject? extensionData) =>
        context.GetMemberIndexAsync(reader, names, namespaces, memberIndex, names.Length, extensionData);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static ValueTask<int> GetMemberIndexWithRequiredMembers(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, XmlDictionaryString[] names, XmlDictionaryString[] namespaces, int memberIndex, int requiredIndex, ExtensionDataObject? extensionData) =>
        context.GetMemberIndexAsync(reader, names, namespaces, memberIndex, requiredIndex, extensionData);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static ValueTask<int> GetJsonMemberIndex(XmlReaderDelegator reader, Json.XmlObjectSerializerReadContextComplexJson context, XmlDictionaryString[] names, int memberIndex, ExtensionDataObject? extensionData) =>
        context.GetJsonMemberIndexAsync(reader, names, memberIndex, extensionData);

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    public static ValueTask<object?> GetExistingObject(XmlObjectSerializerReadContext context, string id, Type type, string name, string ns) =>
        context.GetExistingObjectAsync(id, type, name, ns);
}
