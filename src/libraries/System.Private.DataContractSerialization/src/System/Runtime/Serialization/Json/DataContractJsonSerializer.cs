// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization.Json
{
    using System.Runtime.Serialization;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Collections;
    using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, DataContract>;

    using System.Globalization;
    using System.Reflection;
    using System.Security;
    using System.Diagnostics.CodeAnalysis;

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


        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public DataContractJsonSerializer(Type type)
            : this(type, (IEnumerable<Type>?)null)
        {
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public DataContractJsonSerializer(Type type, string? rootName)
            : this(type, rootName, null)
        {
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public DataContractJsonSerializer(Type type, XmlDictionaryString? rootName)
            : this(type, rootName, null)
        {
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public DataContractJsonSerializer(Type type, IEnumerable<Type>? knownTypes)
            : this(type, null, knownTypes, int.MaxValue, false, false)
        {
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public DataContractJsonSerializer(Type type, string? rootName, IEnumerable<Type>? knownTypes)
            : this(type, new DataContractJsonSerializerSettings() { RootName = rootName, KnownTypes = knownTypes })
        {
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public DataContractJsonSerializer(Type type, XmlDictionaryString? rootName, IEnumerable<Type>? knownTypes)
            : this(type, rootName, knownTypes, int.MaxValue, false, false)
        {
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public DataContractJsonSerializer(Type type, DataContractJsonSerializerSettings? settings)
        {
            if (settings == null)
            {
                settings = new DataContractJsonSerializerSettings();
            }

            XmlDictionaryString? rootName = (settings.RootName == null) ? null : new XmlDictionary(1).Add(settings.RootName);
            Initialize(type, rootName, settings.KnownTypes, settings.MaxItemsInObjectGraph, settings.IgnoreExtensionDataObject,
                settings.EmitTypeInformation, settings.SerializeReadOnlyTypes, settings.DateTimeFormat, settings.UseSimpleDictionaryFormat);
        }

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

        public bool IgnoreExtensionDataObject
        {
            get { return _ignoreExtensionDataObject; }
        }

        public ReadOnlyCollection<Type> KnownTypes
        {
            get
            {
                if (_knownTypeCollection == null)
                {
                    if (knownTypeList != null)
                    {
                        _knownTypeCollection = new ReadOnlyCollection<Type>(knownTypeList);
                    }
                    else
                    {
                        _knownTypeCollection = new ReadOnlyCollection<Type>(Type.EmptyTypes);
                    }
                }
                return _knownTypeCollection;
            }
        }

        internal override DataContractDictionary? KnownDataContracts
        {
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
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (_rootContract == null)
                {
                    _rootContract = DataContract.GetDataContract(_rootType);
                    CheckIfTypeIsReference(_rootContract);
                }
                return _rootContract;
            }
        }

        private XmlDictionaryString RootName
        {
            get
            {
                return _rootName ?? JsonGlobals.rootDictionaryString;
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override bool IsStartObject(XmlReader reader)
        {
            // No need to pass in DateTimeFormat to JsonReaderDelegator: no DateTimes will be read in IsStartObject
            return IsStartObjectHandleExceptions(new JsonReaderDelegator(reader));
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override bool IsStartObject(XmlDictionaryReader reader)
        {
            // No need to pass in DateTimeFormat to JsonReaderDelegator: no DateTimes will be read in IsStartObject
            return IsStartObjectHandleExceptions(new JsonReaderDelegator(reader));
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadObject(Stream stream)
        {
            CheckNull(stream, nameof(stream));
            return ReadObject(JsonReaderWriterFactory.CreateJsonReader(stream, XmlDictionaryReaderQuotas.Max));
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadObject(XmlReader reader)
        {
            return ReadObjectHandleExceptions(new JsonReaderDelegator(reader, this.DateTimeFormat), true);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadObject(XmlReader reader, bool verifyObjectName)
        {
            return ReadObjectHandleExceptions(new JsonReaderDelegator(reader, this.DateTimeFormat), verifyObjectName);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadObject(XmlDictionaryReader reader)
        {
            return ReadObjectHandleExceptions(new JsonReaderDelegator(reader, this.DateTimeFormat), true); // verifyObjectName
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
        {
            return ReadObjectHandleExceptions(new JsonReaderDelegator(reader, this.DateTimeFormat), verifyObjectName);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteEndObject(XmlWriter writer)
        {
            // No need to pass in DateTimeFormat to JsonWriterDelegator: no DateTimes will be written in end object
            WriteEndObjectHandleExceptions(new JsonWriterDelegator(writer));
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteEndObject(XmlDictionaryWriter writer)
        {
            // No need to pass in DateTimeFormat to JsonWriterDelegator: no DateTimes will be written in end object
            WriteEndObjectHandleExceptions(new JsonWriterDelegator(writer));
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteObject(Stream stream, object? graph)
        {
            CheckNull(stream, nameof(stream));
            XmlDictionaryWriter jsonWriter = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false); //  ownsStream
            WriteObject(jsonWriter, graph);
            jsonWriter.Flush();
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteObject(XmlWriter writer, object? graph)
        {
            WriteObjectHandleExceptions(new JsonWriterDelegator(writer, this.DateTimeFormat), graph);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteObject(XmlDictionaryWriter writer, object? graph)
        {
            WriteObjectHandleExceptions(new JsonWriterDelegator(writer, this.DateTimeFormat), graph);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteObjectContent(XmlWriter writer, object? graph)
        {
            WriteObjectContentHandleExceptions(new JsonWriterDelegator(writer, this.DateTimeFormat), graph);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteObjectContent(XmlDictionaryWriter writer, object? graph)
        {
            WriteObjectContentHandleExceptions(new JsonWriterDelegator(writer, this.DateTimeFormat), graph);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteStartObject(XmlWriter writer, object? graph)
        {
            // No need to pass in DateTimeFormat to JsonWriterDelegator: no DateTimes will be written in start object
            WriteStartObjectHandleExceptions(new JsonWriterDelegator(writer), graph);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteStartObject(XmlDictionaryWriter writer, object? graph)
        {
            // No need to pass in DateTimeFormat to JsonWriterDelegator: no DateTimes will be written in start object
            WriteStartObjectHandleExceptions(new JsonWriterDelegator(writer), graph);
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

        [return: NotNullIfNotNull("xmlName")]
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static object? ReadJsonValue(DataContract contract, XmlReaderDelegator reader, XmlObjectSerializerReadContextComplexJson? context)
        {
            return JsonDataContract.GetJsonDataContract(contract).ReadJsonValue(reader, context);
        }

        internal static void WriteJsonNull(XmlWriterDelegator writer)
        {
            writer.WriteAttributeString(null, JsonGlobals.typeString, null, JsonGlobals.nullString); //  prefix //  namespace
        }

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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override bool InternalIsStartObject(XmlReaderDelegator reader)
        {
            if (IsRootElement(reader, RootContract, RootName, XmlDictionaryString.Empty))
            {
                return true;
            }

            return IsJsonLocalName(reader, RootName.Value);
        }

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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override void InternalWriteEndObject(XmlWriterDelegator writer)
        {
            writer.WriteEndElement();
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override void InternalWriteObject(XmlWriterDelegator writer, object? graph)
        {
            InternalWriteStartObject(writer, graph);
            InternalWriteObjectContent(writer, graph);
            InternalWriteEndObject(writer);
        }

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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void AddCollectionItemTypeToKnownTypes(Type knownType)
        {
            Type? itemType;
            Type typeToCheck = knownType;
            while (CollectionDataContract.IsCollection(typeToCheck, out itemType))
            {
                if (itemType.IsGenericType && (itemType.GetGenericTypeDefinition() == Globals.TypeOfKeyValue))
                {
                    itemType = Globals.TypeOfKeyValuePair.MakeGenericType(itemType.GenericTypeArguments);
                }
                this.knownTypeList!.Add(itemType);
                typeToCheck = itemType;
            }
        }

        [MemberNotNull(nameof(_rootType))]
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
            CheckNull(type, nameof(type));
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

            if (maxItemsInObjectGraph < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxItemsInObjectGraph), SR.ValueMustBeNonNegative);
            }
            _maxItemsInObjectGraph = maxItemsInObjectGraph;
            _ignoreExtensionDataObject = ignoreExtensionDataObject;
            _emitTypeInformation = emitTypeInformation;
            _serializeReadOnlyTypes = serializeReadOnlyTypes;
            _dateTimeFormat = dateTimeFormat;
            _useSimpleDictionaryFormat = useSimpleDictionaryFormat;
        }

        [MemberNotNull(nameof(_rootType))]
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
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    XmlObjectSerializer.CreateSerializationException(SR.Format(
                        SR.JsonUnsupportedForIsReference,
                        DataContract.GetClrTypeFullName(dataContract.UnderlyingType),
                        dataContract.IsReference)));
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static DataContract GetDataContract(DataContract declaredTypeContract, Type declaredType, Type objectType)
        {
            DataContract contract = DataContractSerializer.GetDataContract(declaredTypeContract, declaredType, objectType);
            CheckIfTypeIsReference(contract);
            return contract;
        }
    }
}
