// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization.DataContracts;

public abstract partial class DataContract
{
    private ReflectionWriter? _asyncXmlWriter;
    private ReflectionValueReader? _asyncXmlReader;

    [RequiresDynamicCode(SerializerAOTWarning)]
    [RequiresUnreferencedCode(SerializerTrimmerWarning)]
    internal Task WriteXmlValueAsync(XmlWriterDelegator writer, object value, XmlObjectSerializerWriteContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Debug.Assert(cancellationToken == context.CancellationToken && cancellationToken == writer.CancellationToken);
        ReflectionWriter write = Volatile.Read(ref _asyncXmlWriter) ?? InitializeAsyncXmlWriter();
        if (!XmlObjectSerializerContext.TryEnterAsyncStack())
            return YieldXmlValueWriteAsync(writer, value, context, cancellationToken);
        try
        {
            return write(writer, value, context);
        }
        finally
        {
            XmlObjectSerializerContext.ExitAsyncStack();
        }
    }

    [RequiresDynamicCode(SerializerAOTWarning)]
    [RequiresUnreferencedCode(SerializerTrimmerWarning)]
    private Task YieldXmlValueWriteAsync(XmlWriterDelegator writer, object value, XmlObjectSerializerWriteContext context, CancellationToken cancellationToken) =>
        XmlObjectSerializerContext.YieldAsyncStack(() => WriteXmlValueAsync(writer, value, context, cancellationToken));

    [RequiresDynamicCode(SerializerAOTWarning)]
    [RequiresUnreferencedCode(SerializerTrimmerWarning)]
    private ReflectionWriter InitializeAsyncXmlWriter()
    {
        lock (this)
        {
            ReflectionWriter? write = _asyncXmlWriter;
            if (write is null)
            {
                write = AsyncDataContractOperations.CreateXmlWriter(this);
                Volatile.Write(ref _asyncXmlWriter, write);
            }
            return write;
        }
    }

    [RequiresDynamicCode(SerializerAOTWarning)]
    [RequiresUnreferencedCode(SerializerTrimmerWarning)]
    internal ValueTask<object?> ReadXmlValueAsync(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Debug.Assert(cancellationToken == context.CancellationToken && cancellationToken == reader.CancellationToken);
        ReflectionValueReader? read = Volatile.Read(ref _asyncXmlReader);
        if (read is null)
        {
            lock (this)
            {
                read = _asyncXmlReader;
                if (read is null)
                {
                    read = AsyncDataContractOperations.CreateXmlReader(this);
                    Volatile.Write(ref _asyncXmlReader, read);
                }
            }
        }

        if (!XmlObjectSerializerContext.TryEnterAsyncStack())
            return YieldXmlValueReadAsync(reader, context, cancellationToken);
        try
        {
            return read(reader, context);
        }
        finally
        {
            XmlObjectSerializerContext.ExitAsyncStack();
        }
    }

    [RequiresDynamicCode(SerializerAOTWarning)]
    [RequiresUnreferencedCode(SerializerTrimmerWarning)]
    private ValueTask<object?> YieldXmlValueReadAsync(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, CancellationToken cancellationToken) =>
        XmlObjectSerializerContext.YieldAsyncStack(() => ReadXmlValueAsync(reader, context, cancellationToken));
}

internal static class AsyncDataContractOperations
{
    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal static ReflectionValueWriter CreateContractValueWriter(Type type, bool isSerializable, bool writeXsiType,
        int id, RuntimeTypeHandle handle) => new ContractValueWriter(type, isSerializable, writeXsiType, id, handle, type.IsValueType).Write;

    private sealed class ContractValueWriter(Type type, bool isSerializable, bool writeXsiType, int id, RuntimeTypeHandle handle,
        bool isValueType)
    {
        private DataContract? _contract;
        private readonly bool _isSealed = type.IsSealed && !type.IsArray;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal Task Write(XmlWriterDelegator writer, object? value, XmlObjectSerializerWriteContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (value is null)
                return context.WriteNullAsync(writer, type, isSerializable);
            bool exactType = _isSealed || value.GetType().TypeHandle.Equals(handle);
            if (!writeXsiType && exactType && context.CanWriteCachedReference(writer))
                return WriteDeclaredValue(writer, value, context);

            return isValueType
                ? context.InternalSerializeAsync(writer, value, true, writeXsiType, id, handle)
                : context.InternalSerializeReferenceAsync(writer, value, exactType, writeXsiType, id, handle);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private Task WriteDeclaredValue(XmlWriterDelegator writer, object value, XmlObjectSerializerWriteContext context)
        {
            DataContract? contract = Volatile.Read(ref _contract);
            if (contract is null)
            {
                contract = context.GetDataContract(id, handle);
                Volatile.Write(ref _contract, contract);
            }
            return contract.IsReference
                ? context.InternalSerializeReferenceAsync(writer, value, true, false, id, handle)
                : context.WriteWithKnownTypesAsync(contract, writer, value, handle);
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal static ReflectionWriter CreateXmlWriter(DataContract contract)
    {
        switch (contract)
        {
            case ClassDataContract classContract:
                ReflectionWriter writeClass = new ReflectionXmlClassWriter().GenerateClassWriter<AsyncReflectionSerialization>(classContract, null);
                return classContract.IsReadOnlyContract ? GuardReadOnlyWriter(writeClass, classContract.SerializationExceptionMessage) : writeClass;
            case CollectionDataContract collectionContract:
                ReflectionWriter writeCollection = new ReflectionXmlClassWriter().GenerateCollectionWriter<AsyncReflectionSerialization>(collectionContract);
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
                return (writer, value, context) =>
                {
                    long number = enumContract.IsULong ? (long)Convert.ToUInt64(value, null) : Convert.ToInt64(value, null);
                    for (int index = 0; index < enumContract.Values!.Count; index++)
                    {
                        if (number == enumContract.Values[index])
                            return writer.WriteStringAsync(enumContract.ChildElementNames![index].Value);
                    }
                    EnumValueWriter lexicalWriter = new EnumValueWriter();
                    enumContract.WriteEnumValue(new XmlWriterDelegator(lexicalWriter), value);
                    return writer.WriteStringAsync(lexicalWriter.Value);
                };
            case XmlDataContract:
                return (writer, value, context) => AsyncXmlSerializableServices.WriteAsync(writer, value, context.CancellationToken).AsTask();
            case PrimitiveDataContract:
                return CreatePrimitiveWriter(contract.UnderlyingType);
            default:
                throw CreateUnexpectedContractException(contract);
        }
    }

    [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
    [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
    internal static ReflectionValueReader CreateXmlReader(DataContract contract)
    {
        switch (contract)
        {
            case ClassDataContract classContract:
                if (classContract.IsReadOnlyContract)
                {
                    DataContract.ThrowInvalidDataContractException(classContract.DeserializationExceptionMessage, null);
                }
                ReflectionClassReader readClass = new ReflectionXmlReader().GenerateClassReader<AsyncReflectionSerialization>(classContract, classContract.MemberNames, classContract.MemberNamespaces);
                return (reader, context) => ReadClass(reader, context, readClass);
            case CollectionDataContract collectionContract:
                Lazy<ReflectionValueReader> readCollection = new Lazy<ReflectionValueReader>(() =>
                    new ReflectionXmlReader().GenerateCollectionReader<AsyncReflectionSerialization>(collectionContract, collectionContract.CollectionItemName, collectionContract.Namespace, null));
                return (reader, context) => ReadCollection(reader, context, readCollection);
            case EnumDataContract enumContract:
                return async (reader, context) =>
                {
                    string lexical = (string)await reader.ReadReferencePrimitiveElementAsync<ReflectionPlan.StringValue>(context.CancellationToken).ConfigureAwait(false);
                    object value = ParseEnum(enumContract, lexical);
                    context.AddNewObject(value);
                    return value;
                };
            case XmlDataContract xmlContract:
                CreateXmlSerializableDelegate createXmlSerializable = AsyncXmlSerializableServices.CreateFactory(xmlContract);
                return async (reader, context) =>
                {
                    string objectId = context.GetObjectId();
                    object? value = await AsyncXmlSerializableServices.ReadAsync(reader, xmlContract, true, context.CancellationToken, createXmlSerializable).ConfigureAwait(false);
                    context.AddNewObjectWithId(objectId, value);
                    await reader.ReadEndElementAsync(context.CancellationToken).ConfigureAwait(false);
                    return value;
                };
            case ObjectDataContract:
                return async (reader, context) =>
                {
                    string name = reader.LocalName;
                    string ns = reader.NamespaceURI;
                    await reader.ReadAsync(context.CancellationToken).ConfigureAwait(false);
                    try
                    {
                        await reader.ReadEndElementAsync(context.CancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception exception) when (exception is XmlException or SerializationException)
                    {
                        throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.XmlForObjectCannotHaveContent, name, ns), exception);
                    }
                    object value = new object();
                    context.AddNewObject(value);
                    return value;
                };
            case PrimitiveDataContract:
                return CreatePrimitiveReader(contract);
            default:
                throw CreateUnexpectedContractException(contract);
        }
    }

    internal static ValueTask<object?> ReadClass(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, ReflectionClassReader readClass)
    {
        ValueTask<bool> start = reader.ReadAsync(context.CancellationToken);
        if (!start.IsCompletedSuccessfully)
            return AwaitClassStart(start, reader, context, readClass);

        start.GetAwaiter().GetResult();
        return ReadClassBody(reader, context, readClass);
    }

    private static ValueTask<object?> ReadClassBody(XmlReaderDelegator reader, XmlObjectSerializerReadContext context, ReflectionClassReader readClass)
    {
        ValueTask<object> value = readClass(reader, context);
        if (!value.IsCompletedSuccessfully)
            return AwaitClassBody(value, reader, context);

        return ReadValueEnd(value.Result, reader, context);
    }

    private static ValueTask<object?> ReadValueEnd(object? value, XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
    {
        ValueTask end = reader.ReadEndElementAsync(context.CancellationToken);
        if (!end.IsCompletedSuccessfully)
            return AwaitValueEnd(end, value);

        end.GetAwaiter().GetResult();
        return new ValueTask<object?>(value);
    }

    private static async ValueTask<object?> AwaitClassStart(ValueTask<bool> start, XmlReaderDelegator reader,
        XmlObjectSerializerReadContext context, ReflectionClassReader readClass)
    {
        await start.ConfigureAwait(false);
        return await ReadClassBody(reader, context, readClass).ConfigureAwait(false);
    }

    private static async ValueTask<object?> AwaitClassBody(ValueTask<object> value, XmlReaderDelegator reader, XmlObjectSerializerReadContext context) =>
        await ReadValueEnd(await value.ConfigureAwait(false), reader, context).ConfigureAwait(false);

    private static async ValueTask<object?> AwaitValueEnd(ValueTask end, object? value)
    {
        await end.ConfigureAwait(false);
        return value;
    }

    private static ValueTask<object?> ReadCollection(XmlReaderDelegator reader, XmlObjectSerializerReadContext context,
        Lazy<ReflectionValueReader> readCollection)
    {
        ValueTask<bool> start = reader.ReadAsync(context.CancellationToken);
        if (!start.IsCompletedSuccessfully)
            return AwaitCollectionStart(start, reader, context, readCollection);

        start.GetAwaiter().GetResult();
        return ReadCollectionBody(reader, context, readCollection);
    }

    private static ValueTask<object?> ReadCollectionBody(XmlReaderDelegator reader, XmlObjectSerializerReadContext context,
        Lazy<ReflectionValueReader> readCollection)
    {
        ValueTask<object?> value = readCollection.Value(reader, context);
        if (!value.IsCompletedSuccessfully)
            return AwaitCollectionBody(value, reader, context);

        return ReadValueEnd(value.Result, reader, context);
    }

    private static async ValueTask<object?> AwaitCollectionStart(ValueTask<bool> start, XmlReaderDelegator reader,
        XmlObjectSerializerReadContext context, Lazy<ReflectionValueReader> readCollection)
    {
        await start.ConfigureAwait(false);
        return await ReadCollectionBody(reader, context, readCollection).ConfigureAwait(false);
    }

    private static async ValueTask<object?> AwaitCollectionBody(ValueTask<object?> value, XmlReaderDelegator reader,
        XmlObjectSerializerReadContext context) =>
        await ReadValueEnd(await value.ConfigureAwait(false), reader, context).ConfigureAwait(false);

    internal static ReflectionValueReader CreatePrimitiveReader(DataContract contract)
    {
        Type type = contract.UnderlyingType;
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => CreatePrimitiveReader<bool>(),
            TypeCode.Char => CreatePrimitiveReader<char>(),
            TypeCode.Byte => CreatePrimitiveReader<byte>(),
            TypeCode.SByte => CreatePrimitiveReader<sbyte>(),
            TypeCode.Int16 => CreatePrimitiveReader<short>(),
            TypeCode.UInt16 => CreatePrimitiveReader<ushort>(),
            TypeCode.Int32 => CreatePrimitiveReader<int>(),
            TypeCode.UInt32 => CreatePrimitiveReader<uint>(),
            TypeCode.Int64 => CreatePrimitiveReader<long>(),
            TypeCode.UInt64 => CreatePrimitiveReader<ulong>(),
            TypeCode.Single => CreatePrimitiveReader<float>(),
            TypeCode.Double => CreatePrimitiveReader<double>(),
            TypeCode.Decimal => CreatePrimitiveReader<decimal>(),
            TypeCode.DateTime => CreatePrimitiveReader<DateTime>(),
            TypeCode.String => CreateReferencePrimitiveReader<ReflectionPlan.StringValue>(),
            _ when type == typeof(Guid) => CreatePrimitiveReader<Guid>(),
            _ when type == typeof(TimeSpan) => CreatePrimitiveReader<TimeSpan>(),
            _ when type == typeof(DateOnly) => CreatePrimitiveReader<DateOnly>(),
            _ when type == typeof(TimeOnly) => CreatePrimitiveReader<TimeOnly>(),
            _ when type == typeof(byte[]) => CreateReferencePrimitiveReader<ReflectionPlan.ByteArrayValue>(),
            _ when type == typeof(Uri) => CreateReferencePrimitiveReader<ReflectionPlan.UriValue>(),
            _ when type == typeof(XmlQualifiedName) => CreateReferencePrimitiveReader<ReflectionPlan.QualifiedValue>(),
            _ => throw new InvalidDataContractException(SR.Format(SR.InvalidPrimitiveType_Serialization, DataContract.GetClrTypeFullName(type)))
        };
    }

    private static ReflectionValueReader CreatePrimitiveReader<T>() where T : struct =>
        static (reader, context) =>
        {
            string objectId = context.GetObjectId();
            ValueTask<T> value = reader.ReadPrimitiveElementAsync<T>(context.CancellationToken);
            if (value.IsCompletedSuccessfully)
                return CompletePrimitiveRead(value.Result, context, objectId);

            return AwaitPrimitiveRead(value, context, objectId);
        };

    private static ValueTask<object?> CompletePrimitiveRead<T>(T value, XmlObjectSerializerReadContext context, string objectId)
    {
        object? result = value;
        context.AddNewObjectWithId(objectId, result);
        return new ValueTask<object?>(result);
    }

    private static async ValueTask<object?> AwaitPrimitiveRead<T>(ValueTask<T> value, XmlObjectSerializerReadContext context, string objectId) =>
        await CompletePrimitiveRead(await value.ConfigureAwait(false), context, objectId).ConfigureAwait(false);

    private static ReflectionValueReader CreateReferencePrimitiveReader<TPrimitiveKind>() where TPrimitiveKind : struct =>
        static (reader, context) =>
        {
            string objectId = context.GetObjectId();
            ValueTask<object> value = reader.ReadReferencePrimitiveElementAsync<TPrimitiveKind>(context.CancellationToken);
            return value.IsCompletedSuccessfully ? CompletePrimitiveRead(value.Result, context, objectId) :
                AwaitPrimitiveRead(value, context, objectId);
        };

    internal static ReflectionWriter CreatePrimitiveWriter(Type type) =>
        type == typeof(object) ? static (writer, value, context) => Task.CompletedTask : GetPrimitiveWriter(type).Writer;

    internal static ReflectionValueWriter CreatePrimitiveValueWriter(Type type) => GetPrimitiveWriter(type).ValueWriter;

    internal static ReflectionMemberValueWriter CreatePrimitiveElementWriter(Type type, XmlDictionaryString name, XmlDictionaryString? ns) =>
        GetPrimitiveWriter(type).CreateElementWriter(name, ns);

    internal static ReflectionMemberValueWriter? TryCreateNullablePrimitiveElementWriter(Type type, XmlDictionaryString name, XmlDictionaryString? ns)
    {
        Type? underlyingType = Nullable.GetUnderlyingType(type);
        return underlyingType is not null && PrimitiveDataContract.GetPrimitiveDataContract(underlyingType) is not null
            ? GetPrimitiveWriter(underlyingType).CreateNullableElementWriter(name, ns)
            : null;
    }

    private static PrimitiveWriter GetPrimitiveWriter(Type type) =>
        Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => PrimitiveWriter<bool>.Instance,
            TypeCode.Char => PrimitiveWriter<char>.Instance,
            TypeCode.Byte => PrimitiveWriter<byte>.Instance,
            TypeCode.SByte => PrimitiveWriter<sbyte>.Instance,
            TypeCode.Int16 => PrimitiveWriter<short>.Instance,
            TypeCode.UInt16 => PrimitiveWriter<ushort>.Instance,
            TypeCode.Int32 => PrimitiveWriter<int>.Instance,
            TypeCode.UInt32 => PrimitiveWriter<uint>.Instance,
            TypeCode.Int64 => PrimitiveWriter<long>.Instance,
            TypeCode.UInt64 => PrimitiveWriter<ulong>.Instance,
            TypeCode.Single => PrimitiveWriter<float>.Instance,
            TypeCode.Double => PrimitiveWriter<double>.Instance,
            TypeCode.Decimal => PrimitiveWriter<decimal>.Instance,
            TypeCode.DateTime => PrimitiveWriter<DateTime>.Instance,
            TypeCode.String => PrimitiveWriter<ReflectionPlan.StringValue>.Instance,
            _ when type == typeof(byte[]) => PrimitiveWriter<ReflectionPlan.ByteArrayValue>.Instance,
            _ when type == typeof(Guid) => PrimitiveWriter<Guid>.Instance,
            _ when type == typeof(TimeSpan) => PrimitiveWriter<TimeSpan>.Instance,
            _ when type == typeof(Uri) => PrimitiveWriter<ReflectionPlan.UriValue>.Instance,
            _ when type == typeof(XmlQualifiedName) => PrimitiveWriter<ReflectionPlan.QualifiedValue>.Instance,
            _ when type == typeof(DateOnly) => PrimitiveWriter<DateOnly>.Instance,
            _ when type == typeof(TimeOnly) => PrimitiveWriter<TimeOnly>.Instance,
            _ => throw new InvalidDataContractException(SR.Format(SR.InvalidPrimitiveType_Serialization, DataContract.GetClrTypeFullName(type)))
        };

    private abstract class PrimitiveWriter
    {
        internal abstract ReflectionWriter Writer { get; }
        internal abstract ReflectionValueWriter ValueWriter { get; }
        internal abstract ReflectionMemberValueWriter CreateElementWriter(XmlDictionaryString name, XmlDictionaryString? ns);
        internal abstract ReflectionMemberValueWriter? CreateNullableElementWriter(XmlDictionaryString name, XmlDictionaryString? ns);
    }

    private sealed class PrimitiveWriter<T> : PrimitiveWriter where T : struct
    {
        internal static PrimitiveWriter<T> Instance { get; } = new PrimitiveWriter<T>();
        internal override ReflectionWriter Writer => Write;
        internal override ReflectionValueWriter ValueWriter => Write;

        private static Task Write(XmlWriterDelegator writer, object? value, XmlObjectSerializerWriteContext context) =>
            writer.WritePrimitiveValueAsync<T>(value!);

        private static bool HasTypedElementWriter =>
            typeof(T) == typeof(bool) || XmlDictionaryWriter.IsInt32Primitive<T>() ||
            typeof(T) == typeof(uint) || typeof(T) == typeof(long) || typeof(T) == typeof(float) ||
            typeof(T) == typeof(double) || typeof(T) == typeof(decimal) || typeof(T) == typeof(DateTime) ||
            typeof(T) == typeof(ulong) || typeof(T) == typeof(Guid) || typeof(T) == typeof(TimeSpan);

        internal override ReflectionMemberValueWriter? CreateNullableElementWriter(XmlDictionaryString name, XmlDictionaryString? ns) =>
            HasTypedElementWriter ? new NullablePrimitiveElementWriter<T>(name, ns).Write : null;

        internal override ReflectionMemberValueWriter CreateElementWriter(XmlDictionaryString name, XmlDictionaryString? ns)
        {
            if (HasTypedElementWriter)
            {
                return new PrimitiveElementWriter<T>(name, ns).Write;
            }

            return async (writer, obj, value, context) =>
            {
                await writer.WriteStartElementPrimitiveAsync(name, ns).ConfigureAwait(false);
                await writer.WritePrimitiveValueAsync<T>(value!).ConfigureAwait(false);
                await writer.WriteEndElementPrimitiveAsync().ConfigureAwait(false);
            };
        }
    }

    private sealed class NullablePrimitiveElementWriter<T>(XmlDictionaryString name, XmlDictionaryString? ns) : IPrimitiveElementWriter where T : struct
    {
        private static readonly ReflectionValueWriter s_writeNull =
            static (writer, value, context) => context.WriteNullAsync(writer, typeof(T), isSerializable: true);

        public MethodInfo TypedWriteMethod => typeof(NullablePrimitiveElementWriter<T>).GetMethod(nameof(WriteTyped), BindingFlags.Instance | BindingFlags.NonPublic)!;

        internal Task Write(XmlWriterDelegator writer, object obj, object? value, XmlObjectSerializerWriteContext context) =>
            value is null ? WriteNull(writer, context) : writer.WriteTypedPrimitiveElementAsync((T)value, name, ns);

        private Task WriteTyped(XmlWriterDelegator writer, T? value, XmlObjectSerializerWriteContext context) =>
            value.HasValue ? writer.WriteTypedPrimitiveElementAsync(value.GetValueOrDefault(), name, ns) : WriteNull(writer, context);

        private Task WriteNull(XmlWriterDelegator writer, XmlObjectSerializerWriteContext context) =>
            ReflectionSerialization.WriteElement<AsyncReflectionSerialization>(writer, null, context, s_writeNull, null, name, ns);
    }

    private sealed class PrimitiveElementWriter<T>(XmlDictionaryString name, XmlDictionaryString? ns) : IPrimitiveElementWriter where T : struct
    {
        public MethodInfo TypedWriteMethod => typeof(PrimitiveElementWriter<T>).GetMethod(nameof(WriteTyped), BindingFlags.Instance | BindingFlags.NonPublic)!;

        internal Task Write(XmlWriterDelegator writer, object obj, object? value, XmlObjectSerializerWriteContext context) =>
            writer.WriteTypedPrimitiveElementAsync((T)value!, name, ns);

        private Task WriteTyped(XmlWriterDelegator writer, T value, XmlObjectSerializerWriteContext context) =>
            writer.WriteTypedPrimitiveElementAsync(value, name, ns);
    }

    private static ReflectionWriter GuardReadOnlyWriter(ReflectionWriter write, string? message) =>
        (writer, value, context) =>
        {
            if (!context.SerializeReadOnlyTypes)
            {
                DataContract.ThrowInvalidDataContractException(message, null);
            }
            return write(writer, value, context);
        };

    internal static object ParseEnum(EnumDataContract contract, string lexical) =>
        contract.ReadEnumValue(new XmlReaderDelegator(new EnumTextReader(lexical)));

    private static InvalidDataContractException CreateUnexpectedContractException(DataContract contract) =>
        new InvalidDataContractException(SR.Format(SR.UnexpectedContractType, DataContract.GetClrTypeFullName(contract.GetType()), DataContract.GetClrTypeFullName(contract.UnderlyingType)));

    private sealed class EnumValueWriter : XmlWriter
    {
        private readonly StringBuilder _value = new StringBuilder();

        internal string Value => _value.ToString();

        public override void WriteString(string? value) => _value.Append(value);
        public override WriteState WriteState => WriteState.Content;
        public override string? LookupPrefix(string ns) => null;
        public override void Flush() { }
        public override void WriteStartDocument() => ThrowUnexpectedOperation();
        public override void WriteStartDocument(bool standalone) => ThrowUnexpectedOperation();
        public override void WriteEndDocument() => ThrowUnexpectedOperation();
        public override void WriteDocType(string name, string? publicId, string? systemId, string? subset) => ThrowUnexpectedOperation();
        public override void WriteStartElement(string? prefix, string localName, string? ns) => ThrowUnexpectedOperation();
        public override void WriteEndElement() => ThrowUnexpectedOperation();
        public override void WriteFullEndElement() => ThrowUnexpectedOperation();
        public override void WriteStartAttribute(string? prefix, string localName, string? ns) => ThrowUnexpectedOperation();
        public override void WriteEndAttribute() => ThrowUnexpectedOperation();
        public override void WriteCData(string? text) => ThrowUnexpectedOperation();
        public override void WriteComment(string? text) => ThrowUnexpectedOperation();
        public override void WriteProcessingInstruction(string name, string? text) => ThrowUnexpectedOperation();
        public override void WriteEntityRef(string name) => ThrowUnexpectedOperation();
        public override void WriteCharEntity(char value) => ThrowUnexpectedOperation();
        public override void WriteWhitespace(string? value) => ThrowUnexpectedOperation();
        public override void WriteSurrogateCharEntity(char low, char high) => ThrowUnexpectedOperation();
        public override void WriteChars(char[] buffer, int index, int count) => ThrowUnexpectedOperation();
        public override void WriteRaw(char[] buffer, int index, int count) => ThrowUnexpectedOperation();
        public override void WriteRaw(string value) => ThrowUnexpectedOperation();
        public override void WriteBase64(byte[] buffer, int index, int count) => ThrowUnexpectedOperation();
        private static void ThrowUnexpectedOperation() => throw XmlObjectSerializer.CreateSerializationException(SR.IXmlSerializableIllegalOperation);
    }

    private sealed class EnumTextReader(string lexical) : XmlReader
    {
        public override string ReadElementContentAsString() => lexical;
        public override XmlNodeType NodeType => XmlNodeType.Element;
        public override string LocalName => string.Empty;
        public override string NamespaceURI => string.Empty;
        public override string Prefix => string.Empty;
        public override string Value => lexical;
        public override int Depth => 0;
        public override string BaseURI => string.Empty;
        public override bool IsEmptyElement => false;
        public override int AttributeCount => 0;
        public override bool EOF => false;
        public override ReadState ReadState => ReadState.Interactive;
        public override XmlNameTable NameTable { get; } = new NameTable();
        public override string? GetAttribute(string name) => null;
        public override string? GetAttribute(string name, string? ns) => null;
        public override string GetAttribute(int i) => throw new ArgumentOutOfRangeException(nameof(i));
        public override bool MoveToAttribute(string name) => false;
        public override bool MoveToAttribute(string name, string? ns) => false;
        public override bool MoveToFirstAttribute() => false;
        public override bool MoveToNextAttribute() => false;
        public override bool MoveToElement() => false;
        public override bool Read() => false;
        public override bool ReadAttributeValue() => false;
        public override string? LookupNamespace(string prefix) => null;
        public override void ResolveEntity() => throw XmlObjectSerializer.CreateSerializationException(SR.IXmlSerializableIllegalOperation);
    }
}
