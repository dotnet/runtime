// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.DataContracts;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContracts.DataContract>;

namespace System.Runtime.Serialization.Json
{
    public sealed class DataContractJsonSerializer : XmlObjectSerializer
    {
        internal IList<Type>? knownTypeList;
        internal DataContractDictionary? knownDataContracts;
        private EmitTypeInformation _emitTypeInformation;
        private ReadOnlyCollection<Type>? _knownTypeCollection;
        private int _maxItemsInObjectGraph;
        private bool _serializeReadOnlyTypes;
        private DateTimeFormat? _dateTimeFormat;
        private bool _useSimpleDictionaryFormat;
        private bool _ignoreExtensionDataObject;
        private DataContract? _rootContract; // post-surrogate
        private XmlDictionaryString? _rootName;
        private bool _rootNameRequiresMapping;
        private Type _rootType;
        private ISerializationSurrogateProvider? _serializationSurrogateProvider;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public DataContractJsonSerializer(Type type)
            : this(type, (IEnumerable<Type>?)null)
        {
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public DataContractJsonSerializer(Type type, string? rootName)
            : this(type, rootName, null)
        {
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public DataContractJsonSerializer(Type type, XmlDictionaryString? rootName)
            : this(type, rootName, null)
        {
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public DataContractJsonSerializer(Type type, IEnumerable<Type>? knownTypes)
            : this(type, null, knownTypes, int.MaxValue, false, false)
        {
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public DataContractJsonSerializer(Type type, string? rootName, IEnumerable<Type>? knownTypes)
            : this(type, new DataContractJsonSerializerSettings() { RootName = rootName, KnownTypes = knownTypes })
        {
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public DataContractJsonSerializer(Type type, XmlDictionaryString? rootName, IEnumerable<Type>? knownTypes)
            : this(type, rootName, knownTypes, int.MaxValue, false, false)
        {
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public DataContractJsonSerializer(Type type, DataContractJsonSerializerSettings? settings)
        {
            settings ??= new DataContractJsonSerializerSettings();

            XmlDictionaryString? rootName = (settings.RootName == null) ? null : new XmlDictionary(1).Add(settings.RootName);
            Initialize(type, rootName, settings.KnownTypes, settings.MaxItemsInObjectGraph, settings.IgnoreExtensionDataObject,
                settings.EmitTypeInformation, settings.SerializeReadOnlyTypes, settings.DateTimeFormat, settings.UseSimpleDictionaryFormat);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal DataContractJsonSerializer(Type type,
            XmlDictionaryString? rootName,
            IEnumerable<Type>? knownTypes,
            int maxItemsInObjectGraph,
            bool ignoreExtensionDataObject,
            bool alwaysEmitTypeInformation)
        {
            EmitTypeInformation emitTypeInformation = alwaysEmitTypeInformation ? EmitTypeInformation.Always : EmitTypeInformation.AsNeeded;
            Initialize(type, rootName, knownTypes, maxItemsInObjectGraph, ignoreExtensionDataObject, emitTypeInformation, false, null, false);
        }

        internal ISerializationSurrogateProvider? SerializationSurrogateProvider
        {
            get { return Volatile.Read(ref _serializationSurrogateProvider); }
            set { Volatile.Write(ref _serializationSurrogateProvider, value); }
        }

        internal override DataContractJsonSerializer CreateAsyncOperationSnapshot()
        {
            DataContractJsonSerializer snapshot = new DataContractJsonSerializer(this, SerializationSurrogateProvider);
            if (_dateTimeFormat is DateTimeFormat format)
            {
                snapshot._dateTimeFormat = new DateTimeFormat(format.FormatString, format.FormatProvider)
                {
                    DateTimeStyles = format.DateTimeStyles
                };
            }
            return snapshot;
        }

        private DataContractJsonSerializer(DataContractJsonSerializer serializer, ISerializationSurrogateProvider? surrogateProvider)
        {
            knownTypeList = serializer.knownTypeList;
            knownDataContracts = serializer.knownDataContracts;
            _emitTypeInformation = serializer._emitTypeInformation;
            _knownTypeCollection = serializer._knownTypeCollection;
            _maxItemsInObjectGraph = serializer._maxItemsInObjectGraph;
            _serializeReadOnlyTypes = serializer._serializeReadOnlyTypes;
            _dateTimeFormat = serializer._dateTimeFormat;
            _useSimpleDictionaryFormat = serializer._useSimpleDictionaryFormat;
            _ignoreExtensionDataObject = serializer._ignoreExtensionDataObject;
            _rootContract = Volatile.Read(ref serializer._rootContract);
            _rootName = serializer._rootName;
            _rootNameRequiresMapping = serializer._rootNameRequiresMapping;
            _rootType = serializer._rootType;
            _serializationSurrogateProvider = surrogateProvider;
        }

        public bool IgnoreExtensionDataObject
        {
            get { return _ignoreExtensionDataObject; }
        }

        public ReadOnlyCollection<Type> KnownTypes
        {
            get
            {
                return _knownTypeCollection ??=
                    knownTypeList != null ?
                        new ReadOnlyCollection<Type>(knownTypeList) :
                        ReadOnlyCollection<Type>.Empty;
            }
        }

        internal override DataContractDictionary? KnownDataContracts
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (this.knownDataContracts == null && this.knownTypeList != null)
                {
                    // This assignment may be performed concurrently and thus is a race condition.
                    // It's safe, however, because at worse a new (and identical) dictionary of
                    // data contracts will be created and re-assigned to this field.  Introduction
                    // of a lock here could lead to deadlocks.
                    this.knownDataContracts = XmlObjectSerializerContext.GetDataContractsForKnownTypes(this.knownTypeList);
                }
                return this.knownDataContracts;
            }
        }

        public int MaxItemsInObjectGraph
        {
            get { return _maxItemsInObjectGraph; }
        }

        public DateTimeFormat? DateTimeFormat
        {
            get
            {
                return _dateTimeFormat;
            }
        }

        public EmitTypeInformation EmitTypeInformation
        {
            get
            {
                return _emitTypeInformation;
            }
        }

        public bool SerializeReadOnlyTypes
        {
            get
            {
                return _serializeReadOnlyTypes;
            }
        }


        public bool UseSimpleDictionaryFormat
        {
            get
            {
                return _useSimpleDictionaryFormat;
            }
        }

        private DataContract RootContract
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                DataContract? contract = Volatile.Read(ref _rootContract);
                if (contract is null)
                {
                    contract = DataContract.GetDataContract((_serializationSurrogateProvider == null) ? _rootType : GetSurrogatedType(_serializationSurrogateProvider, _rootType));
                    CheckIfTypeIsReference(contract);
                    Volatile.Write(ref _rootContract, contract);
                }
                return contract;
            }
        }

        private XmlDictionaryString RootName
        {
            get
            {
                return _rootName ?? JsonGlobals.rootDictionaryString;
            }
        }

        // These Get/Set methods mirror the extensions that were added to DCS in the early days of Core, which allowed
        // using a slimmed-down surrogate on both NetFx and Core via type-forwarding mechanisms. That's why these are
        // a pair of methods instead of making the property itself public.

        /// <summary>
        /// Gets the current serialization surrogate provider.
        /// </summary>
        /// <returns>The current <see cref="ISerializationSurrogateProvider"/> instance, or <see langword="null"/> if no provider is set.</returns>
        public ISerializationSurrogateProvider? GetSerializationSurrogateProvider()
        {
            return SerializationSurrogateProvider;
        }

        /// <summary>
        /// Sets the serialization surrogate provider.
        /// </summary>
        /// <param name="provider">The <see cref="ISerializationSurrogateProvider"/> to use for serialization, or <see langword="null"/> to remove the current provider.</param>
        public void SetSerializationSurrogateProvider(ISerializationSurrogateProvider? provider)
        {
            SerializationSurrogateProvider = provider;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override bool IsStartObject(XmlReader reader)
        {
            // No need to pass in DateTimeFormat to JsonReaderDelegator: no DateTimes will be read in IsStartObject
            return IsStartObjectHandleExceptions(new JsonReaderDelegator(reader));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override bool IsStartObject(XmlDictionaryReader reader)
        {
            // No need to pass in DateTimeFormat to JsonReaderDelegator: no DateTimes will be read in IsStartObject
            return IsStartObjectHandleExceptions(new JsonReaderDelegator(reader));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadObject(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            return ReadObject(JsonReaderWriterFactory.CreateJsonReader(stream, XmlDictionaryReaderQuotas.Max));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadObject(XmlReader reader)
        {
            return ReadObjectHandleExceptions(new JsonReaderDelegator(reader, this.DateTimeFormat), true);
        }

        /// <inheritdoc/>
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override Task<object?> ReadObjectAsync(XmlReader reader, CancellationToken cancellationToken = default) =>
            ReadObjectAsync(reader, verifyObjectName: true, cancellationToken);

        /// <inheritdoc/>
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override Task<object?> ReadObjectAsync(XmlDictionaryReader reader, CancellationToken cancellationToken = default) =>
            ReadObjectAsync(reader, verifyObjectName: true, cancellationToken);

        /// <inheritdoc/>
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override Task<object?> ReadObjectAsync(XmlReader reader, bool verifyObjectName, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(reader);
            DataContractJsonSerializer snapshot = CreateAsyncOperationSnapshot();
            return snapshot.ReadObjectHandleExceptionsAsync(new JsonReaderDelegator(reader, snapshot.DateTimeFormat) { CancellationToken = cancellationToken }, verifyObjectName);
        }

        /// <inheritdoc/>
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override Task<object?> ReadObjectAsync(XmlDictionaryReader reader, bool verifyObjectName, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(reader);
            DataContractJsonSerializer snapshot = CreateAsyncOperationSnapshot();
            return snapshot.ReadObjectHandleExceptionsAsync(new JsonReaderDelegator(reader, snapshot.DateTimeFormat) { CancellationToken = cancellationToken }, verifyObjectName);
        }

        /// <inheritdoc/>
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override Task<object?> ReadObjectAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stream);
            return CreateAsyncOperationSnapshot().ReadObjectFromStreamAsync(stream, cancellationToken);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private async Task<object?> ReadObjectFromStreamAsync(Stream stream, CancellationToken cancellationToken)
        {
            XmlDictionaryReader reader = await JsonReaderWriterFactory.CreateJsonReaderAsync(
                new SerializationStream(stream, cancellationToken), null, XmlDictionaryReaderQuotas.Max, null, cancellationToken).ConfigureAwait(false);
            try
            {
                return await ReadObjectHandleExceptionsAsync(new JsonReaderDelegator(reader, DateTimeFormat) { CancellationToken = cancellationToken }, verifyObjectName: true).ConfigureAwait(false);
            }
            finally
            {
                await ((XmlBaseReader)reader).CloseAsync().ConfigureAwait(false);
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadObject(XmlReader reader, bool verifyObjectName)
        {
            return ReadObjectHandleExceptions(new JsonReaderDelegator(reader, this.DateTimeFormat), verifyObjectName);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadObject(XmlDictionaryReader reader)
        {
            return ReadObjectHandleExceptions(new JsonReaderDelegator(reader, this.DateTimeFormat), true); // verifyObjectName
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
        {
            return ReadObjectHandleExceptions(new JsonReaderDelegator(reader, this.DateTimeFormat), verifyObjectName);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteEndObject(XmlWriter writer)
        {
            // No need to pass in DateTimeFormat to JsonWriterDelegator: no DateTimes will be written in end object
            WriteEndObjectHandleExceptions(new JsonWriterDelegator(writer));
        }

        /// <inheritdoc/>
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override Task WriteEndObjectAsync(XmlWriter writer, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(writer);
            return CreateAsyncOperationSnapshot().WriteEndObjectHandleExceptionsAsync(new JsonWriterDelegator(writer) { CancellationToken = cancellationToken });
        }

        /// <inheritdoc/>
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override Task WriteEndObjectAsync(XmlDictionaryWriter writer, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(writer);
            return CreateAsyncOperationSnapshot().WriteEndObjectHandleExceptionsAsync(new JsonWriterDelegator(writer) { CancellationToken = cancellationToken });
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteEndObject(XmlDictionaryWriter writer)
        {
            // No need to pass in DateTimeFormat to JsonWriterDelegator: no DateTimes will be written in end object
            WriteEndObjectHandleExceptions(new JsonWriterDelegator(writer));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteObject(Stream stream, object? graph)
        {
            ArgumentNullException.ThrowIfNull(stream);

            XmlDictionaryWriter jsonWriter = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false); //  ownsStream
            WriteObject(jsonWriter, graph);
            jsonWriter.Flush();
        }

        /// <inheritdoc/>
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override Task WriteObjectAsync(Stream stream, object? graph, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stream);
            return CreateAsyncOperationSnapshot().WriteObjectToStreamAsync(stream, graph, cancellationToken);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private async Task WriteObjectToStreamAsync(Stream stream, object? graph, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(
                new SerializationStream(stream, cancellationToken), Encoding.UTF8, ownsStream: false);
            await WriteObjectHandleExceptionsAsync(new JsonWriterDelegator(writer, DateTimeFormat) { CancellationToken = cancellationToken }, graph).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteObject(XmlWriter writer, object? graph)
        {
            WriteObjectHandleExceptions(new JsonWriterDelegator(writer, this.DateTimeFormat), graph);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteObject(XmlDictionaryWriter writer, object? graph)
        {
            WriteObjectHandleExceptions(new JsonWriterDelegator(writer, this.DateTimeFormat), graph);
        }

        /// <inheritdoc/>
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override Task WriteObjectAsync(XmlWriter writer, object? graph, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(writer);
            DataContractJsonSerializer snapshot = CreateAsyncOperationSnapshot();
            return snapshot.WriteObjectHandleExceptionsAsync(new JsonWriterDelegator(writer, snapshot.DateTimeFormat) { CancellationToken = cancellationToken }, graph);
        }

        /// <inheritdoc/>
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override Task WriteObjectAsync(XmlDictionaryWriter writer, object? graph, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(writer);
            DataContractJsonSerializer snapshot = CreateAsyncOperationSnapshot();
            return snapshot.WriteObjectHandleExceptionsAsync(new JsonWriterDelegator(writer, snapshot.DateTimeFormat) { CancellationToken = cancellationToken }, graph);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteObjectContent(XmlWriter writer, object? graph)
        {
            WriteObjectContentHandleExceptions(new JsonWriterDelegator(writer, this.DateTimeFormat), graph);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteObjectContent(XmlDictionaryWriter writer, object? graph)
        {
            WriteObjectContentHandleExceptions(new JsonWriterDelegator(writer, this.DateTimeFormat), graph);
        }

        /// <inheritdoc/>
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override Task WriteObjectContentAsync(XmlWriter writer, object? graph, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(writer);
            DataContractJsonSerializer snapshot = CreateAsyncOperationSnapshot();
            return snapshot.WriteObjectContentHandleExceptionsAsync(new JsonWriterDelegator(writer, snapshot.DateTimeFormat) { CancellationToken = cancellationToken }, graph);
        }

        /// <inheritdoc/>
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override Task WriteObjectContentAsync(XmlDictionaryWriter writer, object? graph, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(writer);
            DataContractJsonSerializer snapshot = CreateAsyncOperationSnapshot();
            return snapshot.WriteObjectContentHandleExceptionsAsync(new JsonWriterDelegator(writer, snapshot.DateTimeFormat) { CancellationToken = cancellationToken }, graph);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteStartObject(XmlWriter writer, object? graph)
        {
            // No need to pass in DateTimeFormat to JsonWriterDelegator: no DateTimes will be written in start object
            WriteStartObjectHandleExceptions(new JsonWriterDelegator(writer), graph);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteStartObject(XmlDictionaryWriter writer, object? graph)
        {
            // No need to pass in DateTimeFormat to JsonWriterDelegator: no DateTimes will be written in start object
            WriteStartObjectHandleExceptions(new JsonWriterDelegator(writer), graph);
        }

        /// <inheritdoc/>
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override Task WriteStartObjectAsync(XmlWriter writer, object? graph, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(writer);
            return CreateAsyncOperationSnapshot().WriteStartObjectHandleExceptionsAsync(new JsonWriterDelegator(writer) { CancellationToken = cancellationToken }, graph);
        }

        /// <inheritdoc/>
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override Task WriteStartObjectAsync(XmlDictionaryWriter writer, object? graph, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(writer);
            return CreateAsyncOperationSnapshot().WriteStartObjectHandleExceptionsAsync(new JsonWriterDelegator(writer) { CancellationToken = cancellationToken }, graph);
        }

        internal static bool CheckIfJsonNameRequiresMapping(string? jsonName)
        {
            if (jsonName != null)
            {
                if (!DataContract.IsValidNCName(jsonName))
                {
                    return true;
                }

                for (int i = 0; i < jsonName.Length; i++)
                {
                    if (XmlJsonWriter.CharacterNeedsEscaping(jsonName[i]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal static bool CheckIfJsonNameRequiresMapping(XmlDictionaryString? jsonName)
        {
            return (jsonName == null) ? false : CheckIfJsonNameRequiresMapping(jsonName.Value);
        }

        internal static bool CheckIfXmlNameRequiresMapping(string xmlName)
        {
            return (xmlName == null) ? false : CheckIfJsonNameRequiresMapping(ConvertXmlNameToJsonName(xmlName));
        }

        internal static bool CheckIfXmlNameRequiresMapping(XmlDictionaryString xmlName)
        {
            return (xmlName == null) ? false : CheckIfXmlNameRequiresMapping(xmlName.Value);
        }

        internal static string ConvertXmlNameToJsonName(string xmlName)
        {
            return XmlConvert.DecodeName(xmlName);
        }

        [return: NotNullIfNotNull(nameof(xmlName))]
        internal static XmlDictionaryString? ConvertXmlNameToJsonName(XmlDictionaryString? xmlName)
        {
            return (xmlName == null) ? null : new XmlDictionary().Add(ConvertXmlNameToJsonName(xmlName.Value));
        }

        internal static bool IsJsonLocalName(XmlReaderDelegator reader, string elementName)
        {
            string? name;
            if (XmlObjectSerializerReadContextComplexJson.TryGetJsonLocalName(reader, out name))
            {
                return (elementName == name);
            }
            return false;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static object? ReadJsonValue(DataContract contract, XmlReaderDelegator reader, XmlObjectSerializerReadContextComplexJson? context)
        {
            return JsonDataContract.GetJsonDataContract(contract).ReadJsonValue(reader, context);
        }

        internal static void WriteJsonNull(XmlWriterDelegator writer)
        {
            writer.WriteAttributeString(null, JsonGlobals.typeString, null, JsonGlobals.nullString); //  prefix //  namespace
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static void WriteJsonValue(JsonDataContract contract, XmlWriterDelegator writer, object graph, XmlObjectSerializerWriteContextComplexJson? context, RuntimeTypeHandle declaredTypeHandle)
        {
            contract.WriteJsonValue(writer, graph, context, declaredTypeHandle);
        }

        internal override Type? GetDeserializeType()
        {
            return _rootType;
        }

        internal override Type? GetSerializeType(object? graph)
        {
            return (graph == null) ? _rootType : graph.GetType();
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override bool InternalIsStartObject(XmlReaderDelegator reader)
        {
            if (IsRootElement(reader, RootContract, RootName, XmlDictionaryString.Empty))
            {
                return true;
            }

            return IsJsonLocalName(reader, RootName.Value);
        }

        /// <inheritdoc/>
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override Task<bool> IsStartObjectAsync(XmlReader reader, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(reader);
            DataContractJsonSerializer snapshot = CreateAsyncOperationSnapshot();
            return snapshot.IsStartObjectAsyncCore(new JsonReaderDelegator(reader, snapshot.DateTimeFormat), cancellationToken);
        }

        /// <inheritdoc/>
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override Task<bool> IsStartObjectAsync(XmlDictionaryReader reader, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(reader);
            DataContractJsonSerializer snapshot = CreateAsyncOperationSnapshot();
            return snapshot.IsStartObjectAsyncCore(new JsonReaderDelegator(reader, snapshot.DateTimeFormat), cancellationToken);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private async Task<bool> IsStartObjectAsyncCore(XmlReaderDelegator reader, CancellationToken cancellationToken)
        {
            try
            {
                if (await reader.MoveToContentAsync(cancellationToken).ConfigureAwait(false) != XmlNodeType.Element)
                {
                    return false;
                }
                return InternalIsStartObject(reader);
            }
            catch (XmlException ex)
            {
                throw CreateSerializationException(GetTypeInfoError(SR.ErrorIsStartObject, GetDeserializeType(), ex), ex);
            }
            catch (FormatException ex)
            {
                throw CreateSerializationException(GetTypeInfoError(SR.ErrorIsStartObject, GetDeserializeType(), ex), ex);
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override object? InternalReadObject(XmlReaderDelegator xmlReader, bool verifyObjectName)
        {
            if (MaxItemsInObjectGraph == 0)
            {
                throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ExceededMaxItemsQuota, MaxItemsInObjectGraph));
            }

            if (verifyObjectName)
            {
                if (!InternalIsStartObject(xmlReader))
                {
                    throw XmlObjectSerializer.CreateSerializationExceptionWithReaderDetails(SR.Format(SR.ExpectingElement, XmlDictionaryString.Empty, RootName), xmlReader);
                }
            }
            else if (!IsStartElement(xmlReader))
            {
                throw XmlObjectSerializer.CreateSerializationExceptionWithReaderDetails(SR.Format(SR.ExpectingElementAtDeserialize, XmlNodeType.Element), xmlReader);
            }

            DataContract contract = RootContract;
            if (contract.IsPrimitive && object.ReferenceEquals(contract.UnderlyingType, _rootType))// handle Nullable<T> differently
            {
                return DataContractJsonSerializer.ReadJsonValue(contract, xmlReader, null);
            }

            XmlObjectSerializerReadContextComplexJson context = XmlObjectSerializerReadContextComplexJson.CreateContext(this, contract);
            return context.InternalDeserialize(xmlReader, _rootType, contract, null, null);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override async ValueTask<object?> InternalReadObjectAsync(XmlReaderDelegator reader, bool verifyObjectName, DataContractResolver? dataContractResolver)
        {
            if (MaxItemsInObjectGraph == 0)
            {
                throw CreateSerializationException(SR.Format(SR.ExceededMaxItemsQuota, MaxItemsInObjectGraph));
            }

            XmlNodeType nodeType = await reader.MoveToContentAsync(reader.CancellationToken).ConfigureAwait(false);
            if (verifyObjectName)
            {
                if (nodeType != XmlNodeType.Element || !InternalIsStartObject(reader))
                {
                    throw CreateSerializationExceptionWithReaderDetails(SR.Format(SR.ExpectingElement, XmlDictionaryString.Empty, RootName), reader);
                }
            }
            else if (nodeType != XmlNodeType.Element)
            {
                throw CreateSerializationExceptionWithReaderDetails(SR.Format(SR.ExpectingElementAtDeserialize, XmlNodeType.Element), reader);
            }

            DataContract contract = RootContract;
            XmlObjectSerializerReadContextComplexJson context = XmlObjectSerializerReadContextComplexJson.CreateContext(this, contract);
            context.InitializeAsync(reader, reader.CancellationToken);
            if (contract.IsPrimitive && ReferenceEquals(contract.UnderlyingType, _rootType))
            {
                context.attributes!.Reset();
                if (!contract.UnderlyingType.IsValueType)
                {
                    context.ReadAttributes(reader);
                    if (context.attributes.XsiNil)
                    {
                        await reader.SkipAsync(reader.CancellationToken).ConfigureAwait(false);
                        return null;
                    }
                }
                return await JsonDataContract.GetJsonDataContract(contract).ReadJsonValueAsync(reader, context, reader.CancellationToken).ConfigureAwait(false);
            }

            return await context.InternalDeserializeAsync(reader, _rootType, contract, null, null).ConfigureAwait(false);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override void InternalWriteEndObject(XmlWriterDelegator writer)
        {
            writer.WriteEndElement();
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override Task InternalWriteEndObjectAsync(XmlWriterDelegator writer) => writer.WriteEndElementAsync();

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override void InternalWriteObject(XmlWriterDelegator writer, object? graph)
        {
            InternalWriteStartObject(writer, graph);
            InternalWriteObjectContent(writer, graph);
            InternalWriteEndObject(writer);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override void InternalWriteObjectContent(XmlWriterDelegator writer, object? graph)
        {
            if (MaxItemsInObjectGraph == 0)
            {
                throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ExceededMaxItemsQuota, MaxItemsInObjectGraph));
            }

            DataContract contract = RootContract;
            Type declaredType = contract.UnderlyingType;
            Type graphType = (graph == null) ? declaredType : graph.GetType();

            if (_serializationSurrogateProvider != null)
            {
                graph = DataContractSerializer.SurrogateToDataContractType(_serializationSurrogateProvider, graph, declaredType, ref graphType);
            }

            if (graph == null)
            {
                WriteJsonNull(writer);
            }
            else
            {
                if (declaredType == graphType)
                {
                    if (contract.CanContainReferences)
                    {
                        XmlObjectSerializerWriteContextComplexJson context = XmlObjectSerializerWriteContextComplexJson.CreateContext(this, contract);
                        context.OnHandleReference(writer, graph, true); //  canContainReferences
                        context.SerializeWithoutXsiType(contract, writer, graph, declaredType.TypeHandle);
                    }
                    else
                    {
                        DataContractJsonSerializer.WriteJsonValue(JsonDataContract.GetJsonDataContract(contract), writer, graph, null, declaredType.TypeHandle); //  XmlObjectSerializerWriteContextComplexJson
                    }
                }
                else
                {
                    XmlObjectSerializerWriteContextComplexJson context = XmlObjectSerializerWriteContextComplexJson.CreateContext(this, RootContract);
                    contract = DataContractJsonSerializer.GetDataContract(contract, declaredType, graphType);
                    if (contract.CanContainReferences)
                    {
                        context.OnHandleReference(writer, graph, true); //  canContainCyclicReference
                        context.SerializeWithXsiTypeAtTopLevel(contract, writer, graph, declaredType.TypeHandle, graphType);
                    }
                    else
                    {
                        context.SerializeWithoutXsiType(contract, writer, graph, declaredType.TypeHandle);
                    }
                }
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override async Task InternalWriteObjectContentAsync(XmlWriterDelegator writer, object? graph, DataContractResolver? dataContractResolver)
        {
            if (MaxItemsInObjectGraph == 0)
            {
                throw CreateSerializationException(SR.Format(SR.ExceededMaxItemsQuota, MaxItemsInObjectGraph));
            }

            DataContract contract = RootContract;
            Type declaredType = contract.UnderlyingType;
            Type graphType = graph?.GetType() ?? declaredType;
            if (_serializationSurrogateProvider is not null)
            {
                graph = DataContractSerializer.SurrogateToDataContractType(_serializationSurrogateProvider, graph, declaredType, ref graphType);
            }
            if (graph is null)
            {
                await writer.WriteAttributeStringAsync(null, JsonGlobals.typeString, null, JsonGlobals.nullString).ConfigureAwait(false);
                return;
            }

            XmlObjectSerializerWriteContextComplexJson context = XmlObjectSerializerWriteContextComplexJson.CreateContext(this, RootContract);
            context.InitializeAsync(writer, writer.CancellationToken);
            if (declaredType == graphType)
            {
                if (contract.CanContainReferences)
                {
                    await context.OnHandleReferenceAsync(writer, graph, canContainCyclicReference: true).ConfigureAwait(false);
                    await context.SerializeWithoutXsiTypeAsync(contract, writer, graph, declaredType.TypeHandle).ConfigureAwait(false);
                }
                else
                {
                    await JsonDataContract.GetJsonDataContract(contract).WriteJsonValueAsync(
                        writer, graph, context, declaredType.TypeHandle, writer.CancellationToken).ConfigureAwait(false);
                }
                return;
            }

            contract = GetDataContract(contract, declaredType, graphType);
            if (contract.CanContainReferences)
            {
                await context.OnHandleReferenceAsync(writer, graph, canContainCyclicReference: true).ConfigureAwait(false);
                await context.SerializeWithXsiTypeAtTopLevelAsync(contract, writer, graph, declaredType.TypeHandle, graphType).ConfigureAwait(false);
            }
            else
            {
                await context.SerializeWithoutXsiTypeAsync(contract, writer, graph, declaredType.TypeHandle).ConfigureAwait(false);
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override void InternalWriteStartObject(XmlWriterDelegator writer, object? graph)
        {
            if (_rootNameRequiresMapping)
            {
                writer.WriteStartElement("a", JsonGlobals.itemString, JsonGlobals.itemString);
                writer.WriteAttributeString(null, JsonGlobals.itemString, null, RootName.Value);
            }
            else
            {
                writer.WriteStartElement(RootName, XmlDictionaryString.Empty);
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override async Task InternalWriteStartObjectAsync(XmlWriterDelegator writer, object? graph)
        {
            if (_rootNameRequiresMapping)
            {
                await writer.WriteStartElementAsync("a", JsonGlobals.itemString, JsonGlobals.itemString).ConfigureAwait(false);
                await writer.WriteAttributeStringAsync(null, JsonGlobals.itemString, null, RootName.Value).ConfigureAwait(false);
            }
            else
            {
                await writer.WriteStartElementAsync(null, RootName, XmlDictionaryString.Empty).ConfigureAwait(false);
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void AddCollectionItemTypeToKnownTypes(Type knownType)
        {
            Type? itemType;
            Type typeToCheck = knownType;
            while (CollectionDataContract.IsCollection(typeToCheck, out itemType))
            {
                if (itemType.IsGenericType && (itemType.GetGenericTypeDefinition() == typeof(KeyValue<,>)))
                {
                    itemType = typeof(System.Collections.Generic.KeyValuePair<,>).MakeGenericType(itemType.GenericTypeArguments);
                }
                this.knownTypeList!.Add(itemType);
                typeToCheck = itemType;
            }
        }

        [MemberNotNull(nameof(_rootType))]
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void Initialize(Type type,
            IEnumerable<Type>? knownTypes,
            int maxItemsInObjectGraph,
            bool ignoreExtensionDataObject,
            EmitTypeInformation emitTypeInformation,
            bool serializeReadOnlyTypes,
            DateTimeFormat? dateTimeFormat,
            bool useSimpleDictionaryFormat)
        {
            ArgumentNullException.ThrowIfNull(type);

            _rootType = type;

            if (knownTypes != null)
            {
                this.knownTypeList = new List<Type>();
                foreach (Type knownType in knownTypes)
                {
                    this.knownTypeList.Add(knownType);
                    if (knownType != null)
                    {
                        AddCollectionItemTypeToKnownTypes(knownType);
                    }
                }
            }

            ArgumentOutOfRangeException.ThrowIfNegative(maxItemsInObjectGraph);
            _maxItemsInObjectGraph = maxItemsInObjectGraph;
            _ignoreExtensionDataObject = ignoreExtensionDataObject;
            _emitTypeInformation = emitTypeInformation;
            _serializeReadOnlyTypes = serializeReadOnlyTypes;
            _dateTimeFormat = dateTimeFormat;
            _useSimpleDictionaryFormat = useSimpleDictionaryFormat;
        }

        [MemberNotNull(nameof(_rootType))]
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void Initialize(Type type,
            XmlDictionaryString? rootName,
            IEnumerable<Type>? knownTypes,
            int maxItemsInObjectGraph,
            bool ignoreExtensionDataObject,
            EmitTypeInformation emitTypeInformation,
            bool serializeReadOnlyTypes,
            DateTimeFormat? dateTimeFormat,
            bool useSimpleDictionaryFormat)
        {
            Initialize(type, knownTypes, maxItemsInObjectGraph, ignoreExtensionDataObject, emitTypeInformation, serializeReadOnlyTypes, dateTimeFormat, useSimpleDictionaryFormat);
            _rootName = ConvertXmlNameToJsonName(rootName);
            _rootNameRequiresMapping = CheckIfJsonNameRequiresMapping(_rootName);
        }

        internal static void CheckIfTypeIsReference(DataContract dataContract)
        {
            if (dataContract.IsReference)
            {
                throw XmlObjectSerializer.CreateSerializationException(SR.Format(
                        SR.JsonUnsupportedForIsReference,
                        DataContract.GetClrTypeFullName(dataContract.UnderlyingType),
                        dataContract.IsReference));
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static DataContract GetDataContract(DataContract declaredTypeContract, Type declaredType, Type objectType)
        {
            DataContract contract = DataContractSerializer.GetDataContract(declaredTypeContract, declaredType, objectType);
            CheckIfTypeIsReference(contract);
            return contract;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static Type GetSurrogatedType(ISerializationSurrogateProvider serializationSurrogateProvider, Type type)
        {
            return DataContractSurrogateCaller.GetDataContractType(serializationSurrogateProvider, DataContract.UnwrapNullableType(type));
        }
    }
}
