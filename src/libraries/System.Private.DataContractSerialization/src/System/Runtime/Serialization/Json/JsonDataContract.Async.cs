// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization.DataContracts;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal partial class JsonDataContract
{
    private readonly ReflectionWriter?[] _asyncJsonWriters = new ReflectionWriter?[DictionaryFormatCount];
    private readonly ReflectionValueReader?[] _asyncJsonReaders = new ReflectionValueReader?[DictionaryFormatCount];

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal Task WriteJsonValueAsync(XmlWriterDelegator writer, object value, XmlObjectSerializerWriteContextComplexJson context, RuntimeTypeHandle declaredHandle, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Debug.Assert(cancellationToken == context.CancellationToken && cancellationToken == writer.CancellationToken);
        if (!XmlObjectSerializerContext.TryEnterAsyncStack())
            return YieldJsonValueWriteAsync(writer, value, context, declaredHandle, cancellationToken);
        try
        {
            return KnownDataContracts is null
                ? WriteJsonValueCoreAsync(writer, value, context, declaredHandle)
                : WriteJsonValueWithKnownTypesAsync(writer, value, context, declaredHandle);
        }
        finally
        {
            XmlObjectSerializerContext.ExitAsyncStack();
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private Task YieldJsonValueWriteAsync(XmlWriterDelegator writer, object value, XmlObjectSerializerWriteContextComplexJson context, RuntimeTypeHandle declaredHandle, CancellationToken cancellationToken) =>
        XmlObjectSerializerContext.YieldAsyncStack(() => WriteJsonValueAsync(writer, value, context, declaredHandle, cancellationToken));

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private async Task WriteJsonValueWithKnownTypesAsync(XmlWriterDelegator writer, object value, XmlObjectSerializerWriteContextComplexJson context, RuntimeTypeHandle declaredHandle)
    {
        PushKnownDataContracts(context);
        try
        {
            await WriteJsonValueCoreAsync(writer, value, context, declaredHandle).ConfigureAwait(false);
        }
        finally
        {
            PopKnownDataContracts(context);
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal virtual Task WriteJsonValueCoreAsync(XmlWriterDelegator writer, object value, XmlObjectSerializerWriteContextComplexJson context, RuntimeTypeHandle declaredHandle)
    {
        int format = (int)context.ReflectionDictionaryFormat;
        ReflectionWriter write = Volatile.Read(ref _asyncJsonWriters[format]) ?? InitializeAsyncJsonWriter(format);
        return write(writer, value, context);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private ReflectionWriter InitializeAsyncJsonWriter(int format)
    {
        lock (this)
        {
            ReflectionWriter? write = _asyncJsonWriters[format];
            if (write is null)
            {
                write = CreateAsyncWriter((JsonDictionaryFormat)format);
                PublishDictionaryFormatPlan(_asyncJsonWriters, format, write);
            }
            return write;
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal ValueTask<object?> ReadJsonValueAsync(XmlReaderDelegator reader, XmlObjectSerializerReadContextComplexJson context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Debug.Assert(cancellationToken == context.CancellationToken && cancellationToken == reader.CancellationToken);
        if (!XmlObjectSerializerContext.TryEnterAsyncStack())
            return YieldJsonValueReadAsync(reader, context, cancellationToken);
        try
        {
            return ReadJsonValueCoreAsync(reader, context);
        }
        finally
        {
            XmlObjectSerializerContext.ExitAsyncStack();
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private ValueTask<object?> YieldJsonValueReadAsync(XmlReaderDelegator reader, XmlObjectSerializerReadContextComplexJson context, CancellationToken cancellationToken) =>
        XmlObjectSerializerContext.YieldAsyncStack(() => ReadJsonValueAsync(reader, context, cancellationToken));

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private ValueTask<object?> ReadJsonValueCoreAsync(XmlReaderDelegator reader, XmlObjectSerializerReadContextComplexJson context)
    {
        int format = (int)context.ReflectionDictionaryFormat;
        ReflectionValueReader? read = Volatile.Read(ref _asyncJsonReaders[format]);
        if (read is null)
        {
            lock (this)
            {
                read = _asyncJsonReaders[format];
                if (read is null)
                {
                    read = CreateAsyncReader((JsonDictionaryFormat)format);
                    if (KnownDataContracts is { } knownContracts)
                    {
                        ReflectionValueReader readValue = read;
                        read = async (jsonReader, readContext) =>
                        {
                            readContext.scopedKnownTypes.Push(knownContracts);
                            try
                            {
                                return await readValue(jsonReader, readContext).ConfigureAwait(false);
                            }
                            finally
                            {
                                readContext.scopedKnownTypes.Pop();
                            }
                        };
                    }
                    PublishDictionaryFormatPlan(_asyncJsonReaders, format, read);
                }
            }
        }
        return read(reader, context);
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private ReflectionWriter CreateAsyncWriter(JsonDictionaryFormat format)
    {
        switch (TraditionalDataContract)
        {
            case ClassDataContract classContract:
                ReflectionWriter writeClass = new ReflectionJsonClassWriter().GenerateClassWriter<AsyncReflectionSerialization>(classContract, ((JsonClassDataContract)this).MemberNames);
                ReflectionWriter writeClassType = static (writer, value, context) =>
                    writer.WriteAttributeStringAsync(null, JsonGlobals.typeString, null, JsonGlobals.objectString);
                return ReflectionSerialization.CreateWriterSequence(new[]
                {
                    writeClassType,
                    writeClass
                });
            case CollectionDataContract collectionContract:
                ReflectionWriter writeCollection = new ReflectionJsonClassWriter(format is JsonDictionaryFormat.Simple).GenerateCollectionWriter<AsyncReflectionSerialization>(collectionContract);
                if (collectionContract.IsReadOnlyContract)
                {
                    string? message = collectionContract.SerializationExceptionMessage;
                    return (writer, value, context) =>
                    {
                        if (!context.SerializeReadOnlyTypes)
                        {
                            DataContract.ThrowInvalidDataContractException(message, null);
                        }
                        context.IsGetOnlyCollection = false;
                        return writeCollection(writer, value, context);
                    };
                }
                return (writer, value, context) =>
                {
                    context.IsGetOnlyCollection = false;
                    return writeCollection(writer, value, context);
                };
            case EnumDataContract enumContract:
                return enumContract.IsULong
                    ? static (writer, value, context) => writer.WriteUnsignedLongAsync(Convert.ToUInt64(value))
                    : static (writer, value, context) => writer.WriteLongAsync(Convert.ToInt64(value));
            case ObjectDataContract:
                return static (writer, value, context) => writer.WriteAttributeStringAsync(null, JsonGlobals.typeString, null, JsonGlobals.objectString);
            case PrimitiveDataContract:
                return AsyncDataContractOperations.CreatePrimitiveWriter(TraditionalDataContract.UnderlyingType);
            default:
                throw new InvalidDataContractException(SR.Format(SR.JsonTypeNotSupportedByDataContractJsonSerializer, TraditionalDataContract.UnderlyingType));
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private protected virtual ReflectionValueReader CreateAsyncReader(JsonDictionaryFormat format)
    {
        switch (TraditionalDataContract)
        {
            case ClassDataContract classContract:
                ReflectionClassReader readClass = new ReflectionJsonReader().GenerateClassReader<AsyncReflectionSerialization>(classContract, ((JsonClassDataContract)this).MemberNames, null);
                return (reader, context) => AsyncDataContractOperations.ReadClass(reader, context, readClass);
            case CollectionDataContract collectionContract:
                Lazy<ReflectionValueReader> readCollection = new Lazy<ReflectionValueReader>(() =>
                    new ReflectionJsonReader(format is JsonDictionaryFormat.Simple).GenerateCollectionReader<AsyncReflectionSerialization>(
                        collectionContract, JsonGlobals.itemDictionaryString, XmlDictionaryString.Empty, null));
                return async (reader, context) =>
                {
                    await reader.ReadAsync(context.CancellationToken).ConfigureAwait(false);
                    object? value = await readCollection.Value(reader, context).ConfigureAwait(false);
                    await reader.ReadEndElementAsync(context.CancellationToken).ConfigureAwait(false);
                    return value;
                };
            case EnumDataContract enumContract:
                Type enumType = enumContract.UnderlyingType;
                if (enumContract.IsULong)
                {
                    return async (reader, context) =>
                    {
                        object value = Enum.ToObject(enumType, await reader.ReadPrimitiveElementAsync<ulong>(context.CancellationToken).ConfigureAwait(false));
                        context.AddNewObject(value);
                        return value;
                    };
                }
                return async (reader, context) =>
                {
                    object value = Enum.ToObject(enumType, await reader.ReadPrimitiveElementAsync<long>(context.CancellationToken).ConfigureAwait(false));
                    context.AddNewObject(value);
                    return value;
                };
            case ObjectDataContract:
                return ReadObjectAsync;
            case PrimitiveDataContract:
                return AsyncDataContractOperations.CreatePrimitiveReader(TraditionalDataContract);
            default:
                throw new InvalidDataContractException(SR.Format(SR.JsonTypeNotSupportedByDataContractJsonSerializer, TraditionalDataContract.UnderlyingType));
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    private static async ValueTask<object?> ReadObjectAsync(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
    {
        string? mode = reader.GetAttribute(JsonGlobals.typeString);
        object? value;
        switch (mode)
        {
            case JsonGlobals.nullString:
                await reader.SkipAsync(context.CancellationToken).ConfigureAwait(false);
                value = null;
                break;
            case JsonGlobals.booleanString:
                value = await reader.ReadPrimitiveElementAsync<bool>(context.CancellationToken).ConfigureAwait(false);
                break;
            case null:
            case JsonGlobals.stringString:
                value = await reader.ReadReferencePrimitiveElementAsync<ReflectionPlan.StringValue>(context.CancellationToken).ConfigureAwait(false);
                break;
            case JsonGlobals.numberString:
                value = JsonObjectDataContract.ParseJsonNumber((string)await reader.ReadReferencePrimitiveElementAsync<ReflectionPlan.StringValue>(context.CancellationToken).ConfigureAwait(false), out _);
                break;
            case JsonGlobals.objectString:
                await reader.SkipAsync(context.CancellationToken).ConfigureAwait(false);
                value = new object();
                break;
            case JsonGlobals.arrayString:
                return await GetJsonDataContract(DataContract.GetDataContract(typeof(object[]))).ReadJsonValueAsync(reader, (XmlObjectSerializerReadContextComplexJson)context, context.CancellationToken).ConfigureAwait(false);
            default:
                throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.JsonUnexpectedAttributeValue, mode));
        }
        context.AddNewObject(value);
        return value;
    }
}
