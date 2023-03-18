// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.DataContracts;
using System.Text;
using System.Xml;

using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContracts.DataContract>;

namespace System.Runtime.Serialization
{
    public sealed class DataContractSerializer : XmlObjectSerializer
    {
        private Type _rootType;
        private DataContract? _rootContract; // post-surrogate
        private bool _needsContractNsAtRoot;
        private XmlDictionaryString? _rootName;
        private XmlDictionaryString? _rootNamespace;
        private int _maxItemsInObjectGraph;
        private bool _ignoreExtensionDataObject;
        private bool _preserveObjectReferences;
        private ReadOnlyCollection<Type>? _knownTypeCollection;
        internal IList<Type>? _knownTypeList;
        internal DataContractDictionary? _knownDataContracts;
        private DataContractResolver? _dataContractResolver;
        private ISerializationSurrogateProvider? _serializationSurrogateProvider;
        private bool _serializeReadOnlyTypes;

        private static SerializationOption s_option = IsReflectionBackupAllowed() ? SerializationOption.ReflectionAsBackup : SerializationOption.CodeGenOnly;
        private static bool s_optionAlreadySet;

        internal static UTF8Encoding UTF8NoBom { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
        internal static UTF8Encoding ValidatingUTF8 { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        internal static UnicodeEncoding UTF16NoBom { get; } = new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: false);
        internal static UnicodeEncoding BEUTF16NoBom { get; } = new UnicodeEncoding(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: false);
        internal static UnicodeEncoding ValidatingUTF16 { get; } = new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);
        internal static UnicodeEncoding ValidatingBEUTF16 { get; } = new UnicodeEncoding(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: true);

        internal static Base64Encoding Base64Encoding { get; } = new Base64Encoding();
        internal static BinHexEncoding BinHexEncoding { get; } = new BinHexEncoding();

        internal static SerializationOption Option
        {
            get { return RuntimeFeature.IsDynamicCodeSupported ? s_option : SerializationOption.ReflectionOnly; }
            set
            {
                if (s_optionAlreadySet)
                {
                    throw new InvalidOperationException(SR.CannotSetTwice);
                }
                s_optionAlreadySet = true;
                s_option = value;
            }
        }

        private static bool IsReflectionBackupAllowed()
        {
            return true;
        }

        public DataContractSerializer(Type type)
            : this(type, (IEnumerable<Type>?)null)
        {
        }

        public DataContractSerializer(Type type, IEnumerable<Type>? knownTypes)
            : this(type, knownTypes, int.MaxValue, false, false)
        {
        }

        public DataContractSerializer(Type type, string rootName, string rootNamespace)
            : this(type, rootName, rootNamespace, null)
        {
        }

        public DataContractSerializer(Type type, string rootName, string rootNamespace, IEnumerable<Type>? knownTypes)
            : this(type, rootName, rootNamespace, knownTypes, false, false)
        {
        }

        internal DataContractSerializer(Type type, string rootName, string rootNamespace, IEnumerable<Type>? knownTypes,
                bool ignoreExtensionDataObject, bool preserveObjectReferences)
        {
            XmlDictionary dictionary = new XmlDictionary(2);
            Initialize(type, dictionary.Add(rootName), dictionary.Add(DataContract.GetNamespace(rootNamespace)), knownTypes, int.MaxValue, ignoreExtensionDataObject, preserveObjectReferences, null, false);
        }

        public DataContractSerializer(Type type, XmlDictionaryString rootName, XmlDictionaryString rootNamespace)
            : this(type, rootName, rootNamespace, null)
        {
        }

        public DataContractSerializer(Type type, XmlDictionaryString rootName, XmlDictionaryString rootNamespace, IEnumerable<Type>? knownTypes)
        {
            Initialize(type, rootName, rootNamespace, knownTypes, int.MaxValue, false, false, null, false);
        }

        internal DataContractSerializer(Type type, IEnumerable<Type>? knownTypes, int maxItemsInObjectGraph, bool ignoreExtensionDataObject, bool preserveObjectReferences)
        {
            Initialize(type, knownTypes, maxItemsInObjectGraph, ignoreExtensionDataObject, preserveObjectReferences, null, false);
        }

        public DataContractSerializer(Type type, DataContractSerializerSettings? settings)
        {
            settings ??= new DataContractSerializerSettings();
            Initialize(type, settings.RootName, settings.RootNamespace, settings.KnownTypes, settings.MaxItemsInObjectGraph, settings.IgnoreExtensionDataObject,
                settings.PreserveObjectReferences, settings.DataContractResolver, settings.SerializeReadOnlyTypes);
        }

        [MemberNotNull(nameof(_rootType))]
        private void Initialize(Type type,
            IEnumerable<Type>? knownTypes,
            int maxItemsInObjectGraph,
            bool ignoreExtensionDataObject,
            bool preserveObjectReferences,
            DataContractResolver? dataContractResolver,
            bool serializeReadOnlyTypes)
        {
            ArgumentNullException.ThrowIfNull(type);

            _rootType = type;

            if (knownTypes != null)
            {
                _knownTypeList = new List<Type>();
                foreach (Type knownType in knownTypes)
                {
                    _knownTypeList.Add(knownType);
                }
            }

            ArgumentOutOfRangeException.ThrowIfNegative(maxItemsInObjectGraph);
            _maxItemsInObjectGraph = maxItemsInObjectGraph;

            _ignoreExtensionDataObject = ignoreExtensionDataObject;
            _preserveObjectReferences = preserveObjectReferences;
            _dataContractResolver = dataContractResolver;
            _serializeReadOnlyTypes = serializeReadOnlyTypes;
        }

        [MemberNotNull(nameof(_rootType))]
        private void Initialize(Type type, XmlDictionaryString? rootName, XmlDictionaryString? rootNamespace,
            IEnumerable<Type>? knownTypes,
            int maxItemsInObjectGraph,
            bool ignoreExtensionDataObject,
            bool preserveObjectReferences,
            DataContractResolver? dataContractResolver,
            bool serializeReadOnlyTypes)
        {
            Initialize(type, knownTypes, maxItemsInObjectGraph, ignoreExtensionDataObject, preserveObjectReferences, dataContractResolver, serializeReadOnlyTypes);

            // validate root name and namespace are both non-null
            _rootName = rootName;
            _rootNamespace = rootNamespace;
        }

        public ReadOnlyCollection<Type> KnownTypes
        {
            get
            {
                return _knownTypeCollection ??=
                    _knownTypeList != null ?
                        new ReadOnlyCollection<Type>(_knownTypeList) :
                        ReadOnlyCollection<Type>.Empty;
            }
        }

        internal override DataContractDictionary? KnownDataContracts
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (_knownDataContracts == null && _knownTypeList != null)
                {
                    // This assignment may be performed concurrently and thus is a race condition.
                    // It's safe, however, because at worse a new (and identical) dictionary of
                    // data contracts will be created and re-assigned to this field.  Introduction
                    // of a lock here could lead to deadlocks.
                    _knownDataContracts = XmlObjectSerializerContext.GetDataContractsForKnownTypes(_knownTypeList);
                }
                return _knownDataContracts;
            }
        }

        public int MaxItemsInObjectGraph
        {
            get { return _maxItemsInObjectGraph; }
        }

        internal ISerializationSurrogateProvider? SerializationSurrogateProvider
        {
            get { return _serializationSurrogateProvider; }
            set { _serializationSurrogateProvider = value; }
        }

        public bool PreserveObjectReferences
        {
            get { return _preserveObjectReferences; }
        }

        public bool IgnoreExtensionDataObject
        {
            get { return _ignoreExtensionDataObject; }
        }

        public DataContractResolver? DataContractResolver
        {
            get { return _dataContractResolver; }
        }

        public bool SerializeReadOnlyTypes
        {
            get { return _serializeReadOnlyTypes; }
        }

        private DataContract RootContract
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (_rootContract == null)
                {
                    _rootContract = DataContract.GetDataContract((_serializationSurrogateProvider == null) ? _rootType : GetSurrogatedType(_serializationSurrogateProvider, _rootType));
                    _needsContractNsAtRoot = CheckIfNeedsContractNsAtRoot(_rootName, _rootNamespace, _rootContract);
                }
                return _rootContract;
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override void InternalWriteObject(XmlWriterDelegator writer, object? graph)
        {
            InternalWriteObject(writer, graph, null);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override void InternalWriteObject(XmlWriterDelegator writer, object? graph, DataContractResolver? dataContractResolver)
        {
            InternalWriteStartObject(writer, graph);
            InternalWriteObjectContent(writer, graph, dataContractResolver);
            InternalWriteEndObject(writer);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteObject(XmlWriter writer, object? graph)
        {
            WriteObjectHandleExceptions(new XmlWriterDelegator(writer), graph);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteStartObject(XmlWriter writer, object? graph)
        {
            WriteStartObjectHandleExceptions(new XmlWriterDelegator(writer), graph);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteObjectContent(XmlWriter writer, object? graph)
        {
            WriteObjectContentHandleExceptions(new XmlWriterDelegator(writer), graph);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteEndObject(XmlWriter writer)
        {
            WriteEndObjectHandleExceptions(new XmlWriterDelegator(writer));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteStartObject(XmlDictionaryWriter writer, object? graph)
        {
            WriteStartObjectHandleExceptions(new XmlWriterDelegator(writer), graph);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteObjectContent(XmlDictionaryWriter writer, object? graph)
        {
            WriteObjectContentHandleExceptions(new XmlWriterDelegator(writer), graph);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteEndObject(XmlDictionaryWriter writer)
        {
            WriteEndObjectHandleExceptions(new XmlWriterDelegator(writer));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void WriteObject(XmlDictionaryWriter writer, object? graph, DataContractResolver? dataContractResolver)
        {
            WriteObjectHandleExceptions(new XmlWriterDelegator(writer), graph, dataContractResolver);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadObject(XmlReader reader)
        {
            return ReadObjectHandleExceptions(new XmlReaderDelegator(reader), true /*verifyObjectName*/);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadObject(XmlReader reader, bool verifyObjectName)
        {
            return ReadObjectHandleExceptions(new XmlReaderDelegator(reader), verifyObjectName);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override bool IsStartObject(XmlReader reader)
        {
            return IsStartObjectHandleExceptions(new XmlReaderDelegator(reader));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
        {
            return ReadObjectHandleExceptions(new XmlReaderDelegator(reader), verifyObjectName);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override bool IsStartObject(XmlDictionaryReader reader)
        {
            return IsStartObjectHandleExceptions(new XmlReaderDelegator(reader));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public object? ReadObject(XmlDictionaryReader reader, bool verifyObjectName, DataContractResolver? dataContractResolver)
        {
            return ReadObjectHandleExceptions(new XmlReaderDelegator(reader), verifyObjectName, dataContractResolver);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override void InternalWriteStartObject(XmlWriterDelegator writer, object? graph)
        {
            WriteRootElement(writer, RootContract, _rootName, _rootNamespace, _needsContractNsAtRoot);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override void InternalWriteObjectContent(XmlWriterDelegator writer, object? graph)
        {
            InternalWriteObjectContent(writer, graph, null);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void InternalWriteObjectContent(XmlWriterDelegator writer, object? graph, DataContractResolver? dataContractResolver)
        {
            if (MaxItemsInObjectGraph == 0)
                throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ExceededMaxItemsQuota, MaxItemsInObjectGraph));

            DataContract contract = RootContract;
            Type declaredType = contract.UnderlyingType;
            Type graphType = (graph == null) ? declaredType : graph.GetType();

            if (_serializationSurrogateProvider != null)
            {
                graph = SurrogateToDataContractType(_serializationSurrogateProvider, graph, declaredType, ref graphType);
            }

            dataContractResolver ??= DataContractResolver;

            if (graph == null)
            {
                if (IsRootXmlAny(_rootName, contract))
                    throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.IsAnyCannotBeNull, declaredType));
                WriteNull(writer);
            }
            else
            {
                if (declaredType == graphType)
                {
                    if (contract.CanContainReferences)
                    {
                        XmlObjectSerializerWriteContext context = XmlObjectSerializerWriteContext.CreateContext(this, contract, dataContractResolver);
                        context.HandleGraphAtTopLevel(writer, graph, contract);
                        context.SerializeWithoutXsiType(contract, writer, graph, declaredType.TypeHandle);
                    }
                    else
                    {
                        contract.WriteXmlValue(writer, graph, null);
                    }
                }
                else
                {
                    XmlObjectSerializerWriteContext? context;
                    if (IsRootXmlAny(_rootName, contract))
                        throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.IsAnyCannotBeSerializedAsDerivedType, graphType, contract.UnderlyingType));

                    contract = GetDataContract(contract, declaredType, graphType);
                    context = XmlObjectSerializerWriteContext.CreateContext(this, RootContract, dataContractResolver);
                    if (contract.CanContainReferences)
                    {
                        context.HandleGraphAtTopLevel(writer, graph, contract);
                    }
                    context.OnHandleIsReference(writer, contract, graph);
                    context.SerializeWithXsiTypeAtTopLevel(contract, writer, graph, declaredType.TypeHandle, graphType);
                }
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static DataContract GetDataContract(DataContract declaredTypeContract, Type declaredType, Type objectType)
        {
            if (declaredType.IsInterface && CollectionDataContract.IsCollectionInterface(declaredType))
            {
                return declaredTypeContract;
            }
            else if (declaredType.IsArray)//Array covariance is not supported in XSD
            {
                return declaredTypeContract;
            }
            else
            {
                return DataContract.GetDataContract(objectType);
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override void InternalWriteEndObject(XmlWriterDelegator writer)
        {
            if (!IsRootXmlAny(_rootName, RootContract))
            {
                writer.WriteEndElement();
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override object? InternalReadObject(XmlReaderDelegator xmlReader, bool verifyObjectName)
        {
            return InternalReadObject(xmlReader, verifyObjectName, null);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override object? InternalReadObject(XmlReaderDelegator xmlReader, bool verifyObjectName, DataContractResolver? dataContractResolver)
        {
            if (MaxItemsInObjectGraph == 0)
                throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ExceededMaxItemsQuota, MaxItemsInObjectGraph));

            dataContractResolver ??= DataContractResolver;

            if (verifyObjectName)
            {
                if (!InternalIsStartObject(xmlReader))
                {
                    XmlDictionaryString? expectedName;
                    XmlDictionaryString? expectedNs;
                    if (_rootName == null)
                    {
                        expectedName = RootContract.TopLevelElementName;
                        expectedNs = RootContract.TopLevelElementNamespace;
                    }
                    else
                    {
                        expectedName = _rootName;
                        expectedNs = _rootNamespace;
                    }
                    throw XmlObjectSerializer.CreateSerializationExceptionWithReaderDetails(SR.Format(SR.ExpectingElement, expectedNs, expectedName), xmlReader);
                }
            }
            else if (!IsStartElement(xmlReader))
            {
                throw XmlObjectSerializer.CreateSerializationExceptionWithReaderDetails(SR.Format(SR.ExpectingElementAtDeserialize, XmlNodeType.Element), xmlReader);
            }

            DataContract contract = RootContract;
            if (contract.IsPrimitive && object.ReferenceEquals(contract.UnderlyingType, _rootType) /*handle Nullable<T> differently*/)
            {
                return contract.ReadXmlValue(xmlReader, null);
            }

            if (IsRootXmlAny(_rootName, contract))
            {
                return XmlObjectSerializerReadContext.ReadRootIXmlSerializable(xmlReader, (contract as XmlDataContract)!, false /*isMemberType*/);
            }

            XmlObjectSerializerReadContext context = XmlObjectSerializerReadContext.CreateContext(this, contract, dataContractResolver);

            return context.InternalDeserialize(xmlReader, _rootType, contract, null, null);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override bool InternalIsStartObject(XmlReaderDelegator reader)
        {
            return IsRootElement(reader, RootContract, _rootName, _rootNamespace);
        }

        internal override Type? GetSerializeType(object? graph)
        {
            return (graph == null) ? _rootType : graph.GetType();
        }

        internal override Type? GetDeserializeType()
        {
            return _rootType;
        }

        [return: NotNullIfNotNull(nameof(oldObj))]
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static object? SurrogateToDataContractType(ISerializationSurrogateProvider serializationSurrogateProvider, object? oldObj, Type surrogatedDeclaredType, ref Type objType)
        {
            object? obj = DataContractSurrogateCaller.GetObjectToSerialize(serializationSurrogateProvider, oldObj, objType, surrogatedDeclaredType);
            if (obj != oldObj)
            {
                objType = obj != null ? obj.GetType() : Globals.TypeOfObject;
            }
            return obj;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static Type GetSurrogatedType(ISerializationSurrogateProvider serializationSurrogateProvider, Type type)
        {
            return DataContractSurrogateCaller.GetDataContractType(serializationSurrogateProvider, DataContract.UnwrapNullableType(type));
        }
    }
}
