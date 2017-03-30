// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Runtime.CompilerServices;
    using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, DataContract>;

    public sealed class DataContractSerializer : XmlObjectSerializer
    {
        private Type _rootType;
        private DataContract _rootContract; // post-surrogate
        private bool _needsContractNsAtRoot;
        private XmlDictionaryString _rootName;
        private XmlDictionaryString _rootNamespace;
        private int _maxItemsInObjectGraph;
        private bool _ignoreExtensionDataObject;
        private bool _preserveObjectReferences;
        private ReadOnlyCollection<Type> _knownTypeCollection;
        internal IList<Type> knownTypeList;
        internal DataContractDictionary knownDataContracts;
        private DataContractResolver _dataContractResolver;
        private ISerializationSurrogateProvider _serializationSurrogateProvider;
        private bool _serializeReadOnlyTypes;

        private static SerializationOption _option = SerializationOption.ReflectionAsBackup;
        private static bool _optionAlreadySet;
        internal static SerializationOption Option
        {
            get { return _option; }
            set
            {
                if (_optionAlreadySet)
                {
                    throw new InvalidOperationException("Can only set once");
                }
                _optionAlreadySet = true;
                _option = value;
            }
        }

        public DataContractSerializer(Type type)
            : this(type, (IEnumerable<Type>)null)
        {
        }

        public DataContractSerializer(Type type, IEnumerable<Type> knownTypes)
        {
            Initialize(type, knownTypes, int.MaxValue, false, false, null, false);
        }


        public DataContractSerializer(Type type, string rootName, string rootNamespace)
            : this(type, rootName, rootNamespace, null)
        {
        }

        public DataContractSerializer(Type type, string rootName, string rootNamespace, IEnumerable<Type> knownTypes)
        {
            XmlDictionary dictionary = new XmlDictionary(2);
            Initialize(type, dictionary.Add(rootName), dictionary.Add(DataContract.GetNamespace(rootNamespace)), knownTypes, int.MaxValue, false, false, null, false);
        }


        public DataContractSerializer(Type type, XmlDictionaryString rootName, XmlDictionaryString rootNamespace)
            : this(type, rootName, rootNamespace, null)
        {
        }

        public DataContractSerializer(Type type, XmlDictionaryString rootName, XmlDictionaryString rootNamespace, IEnumerable<Type> knownTypes)
        {
            Initialize(type, rootName, rootNamespace, knownTypes, int.MaxValue, false, false, null, false);
        }

#if uapaot
        public DataContractSerializer(Type type, IEnumerable<Type> knownTypes, int maxItemsInObjectGraph, bool ignoreExtensionDataObject, bool preserveObjectReferences)
#else
        internal DataContractSerializer(Type type, IEnumerable<Type> knownTypes, int maxItemsInObjectGraph, bool ignoreExtensionDataObject, bool preserveObjectReferences)
#endif
        {
            Initialize(type, knownTypes, maxItemsInObjectGraph, ignoreExtensionDataObject, preserveObjectReferences, null, false);
        }

        public DataContractSerializer(Type type, XmlDictionaryString rootName, XmlDictionaryString rootNamespace,
            IEnumerable<Type> knownTypes,
            int maxItemsInObjectGraph,
            bool ignoreExtensionDataObject,
            bool preserveObjectReferences,
            DataContractResolver dataContractResolver)
        {
            Initialize(type, rootName, rootNamespace, knownTypes, maxItemsInObjectGraph, ignoreExtensionDataObject, preserveObjectReferences, /*dataContractSurrogate,*/ dataContractResolver, false);
        }

        public DataContractSerializer(Type type, DataContractSerializerSettings settings)
        {
            if (settings == null)
            {
                settings = new DataContractSerializerSettings();
            }
            Initialize(type, settings.RootName, settings.RootNamespace, settings.KnownTypes, settings.MaxItemsInObjectGraph, false,
                settings.PreserveObjectReferences, settings.DataContractResolver, settings.SerializeReadOnlyTypes);
        }

        private void Initialize(Type type,
            IEnumerable<Type> knownTypes,
            int maxItemsInObjectGraph,
            bool ignoreExtensionDataObject,
            bool preserveObjectReferences,
            DataContractResolver dataContractResolver,
            bool serializeReadOnlyTypes)
        {
            CheckNull(type, nameof(type));
            _rootType = type;

            if (knownTypes != null)
            {
                this.knownTypeList = new List<Type>();
                foreach (Type knownType in knownTypes)
                {
                    this.knownTypeList.Add(knownType);
                }
            }

            if (maxItemsInObjectGraph < 0)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(maxItemsInObjectGraph), SR.Format(SR.ValueMustBeNonNegative)));
            _maxItemsInObjectGraph = maxItemsInObjectGraph;

            _ignoreExtensionDataObject = ignoreExtensionDataObject;
            _preserveObjectReferences = preserveObjectReferences;
            _dataContractResolver = dataContractResolver;
            _serializeReadOnlyTypes = serializeReadOnlyTypes;
        }

        private void Initialize(Type type, XmlDictionaryString rootName, XmlDictionaryString rootNamespace,
            IEnumerable<Type> knownTypes,
            int maxItemsInObjectGraph,
            bool ignoreExtensionDataObject,
            bool preserveObjectReferences,
            DataContractResolver dataContractResolver,
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
                if (_knownTypeCollection == null)
                {
                    if (knownTypeList != null)
                    {
                        _knownTypeCollection = new ReadOnlyCollection<Type>(knownTypeList);
                    }
                    else
                    {
                        _knownTypeCollection = new ReadOnlyCollection<Type>(Array.Empty<Type>());
                    }
                }
                return _knownTypeCollection;
            }
        }

        internal override DataContractDictionary KnownDataContracts
        {
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

        internal ISerializationSurrogateProvider SerializationSurrogateProvider
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

        public DataContractResolver DataContractResolver
        {
            get { return _dataContractResolver; }
        }

        public bool SerializeReadOnlyTypes
        {
            get { return _serializeReadOnlyTypes; }
        }

        private DataContract RootContract
        {
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

        internal override void InternalWriteObject(XmlWriterDelegator writer, object graph)
        {
            InternalWriteObject(writer, graph, null);
        }

        internal override void InternalWriteObject(XmlWriterDelegator writer, object graph, DataContractResolver dataContractResolver)
        {
            InternalWriteStartObject(writer, graph);
            InternalWriteObjectContent(writer, graph, dataContractResolver);
            InternalWriteEndObject(writer);
        }

        public override void WriteObject(XmlWriter writer, object graph)
        {
            WriteObjectHandleExceptions(new XmlWriterDelegator(writer), graph);
        }

        public override void WriteStartObject(XmlWriter writer, object graph)
        {
            WriteStartObjectHandleExceptions(new XmlWriterDelegator(writer), graph);
        }

        public override void WriteObjectContent(XmlWriter writer, object graph)
        {
            WriteObjectContentHandleExceptions(new XmlWriterDelegator(writer), graph);
        }

        public override void WriteEndObject(XmlWriter writer)
        {
            WriteEndObjectHandleExceptions(new XmlWriterDelegator(writer));
        }

        public override void WriteStartObject(XmlDictionaryWriter writer, object graph)
        {
            WriteStartObjectHandleExceptions(new XmlWriterDelegator(writer), graph);
        }

        public override void WriteObjectContent(XmlDictionaryWriter writer, object graph)
        {
            WriteObjectContentHandleExceptions(new XmlWriterDelegator(writer), graph);
        }

        public override void WriteEndObject(XmlDictionaryWriter writer)
        {
            WriteEndObjectHandleExceptions(new XmlWriterDelegator(writer));
        }

        public void WriteObject(XmlDictionaryWriter writer, object graph, DataContractResolver dataContractResolver)
        {
            WriteObjectHandleExceptions(new XmlWriterDelegator(writer), graph, dataContractResolver);
        }

        public override object ReadObject(XmlReader reader)
        {
            return ReadObjectHandleExceptions(new XmlReaderDelegator(reader), true /*verifyObjectName*/);
        }

        public override object ReadObject(XmlReader reader, bool verifyObjectName)
        {
            return ReadObjectHandleExceptions(new XmlReaderDelegator(reader), verifyObjectName);
        }

        public override bool IsStartObject(XmlReader reader)
        {
            return IsStartObjectHandleExceptions(new XmlReaderDelegator(reader));
        }

        public override object ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
        {
            return ReadObjectHandleExceptions(new XmlReaderDelegator(reader), verifyObjectName);
        }

        public override bool IsStartObject(XmlDictionaryReader reader)
        {
            return IsStartObjectHandleExceptions(new XmlReaderDelegator(reader));
        }

        public object ReadObject(XmlDictionaryReader reader, bool verifyObjectName, DataContractResolver dataContractResolver)
        {
            return ReadObjectHandleExceptions(new XmlReaderDelegator(reader), verifyObjectName, dataContractResolver);
        }

        internal override void InternalWriteStartObject(XmlWriterDelegator writer, object graph)
        {
            WriteRootElement(writer, RootContract, _rootName, _rootNamespace, _needsContractNsAtRoot);
        }

        internal override void InternalWriteObjectContent(XmlWriterDelegator writer, object graph)
        {
            InternalWriteObjectContent(writer, graph, null);
        }

        internal void InternalWriteObjectContent(XmlWriterDelegator writer, object graph, DataContractResolver dataContractResolver)
        {
            if (MaxItemsInObjectGraph == 0)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ExceededMaxItemsQuota, MaxItemsInObjectGraph)));

            DataContract contract = RootContract;
            Type declaredType = contract.UnderlyingType;
            Type graphType = (graph == null) ? declaredType : graph.GetType();

            if (_serializationSurrogateProvider != null)
            {
                graph = SurrogateToDataContractType(_serializationSurrogateProvider, graph, declaredType, ref graphType);
            }

            if (dataContractResolver == null)
                dataContractResolver = this.DataContractResolver;

            if (graph == null)
            {
                if (IsRootXmlAny(_rootName, contract))
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.IsAnyCannotBeNull, declaredType)));
                WriteNull(writer);
            }
            else
            {
                if (declaredType == graphType)
                {
                    if (contract.CanContainReferences)
                    {
                        XmlObjectSerializerWriteContext context = XmlObjectSerializerWriteContext.CreateContext(this, contract
                                                                                                                              , dataContractResolver
                                                                                                                                                    );
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
                    XmlObjectSerializerWriteContext context = null;
                    if (IsRootXmlAny(_rootName, contract))
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.IsAnyCannotBeSerializedAsDerivedType, graphType, contract.UnderlyingType)));

                    contract = GetDataContract(contract, declaredType, graphType);
                    context = XmlObjectSerializerWriteContext.CreateContext(this, RootContract
                                                                                              , dataContractResolver
                                                                                                                    );
                    if (contract.CanContainReferences)
                    {
                        context.HandleGraphAtTopLevel(writer, graph, contract);
                    }
                    context.OnHandleIsReference(writer, contract, graph);
                    context.SerializeWithXsiTypeAtTopLevel(contract, writer, graph, declaredType.TypeHandle, graphType);
                }
            }
        }

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
                return DataContract.GetDataContract(objectType.TypeHandle, objectType, SerializationMode.SharedContract);
            }
        }

        internal override void InternalWriteEndObject(XmlWriterDelegator writer)
        {
            if (!IsRootXmlAny(_rootName, RootContract))
            {
                writer.WriteEndElement();
            }
        }

        internal override object InternalReadObject(XmlReaderDelegator xmlReader, bool verifyObjectName)
        {
            return InternalReadObject(xmlReader, verifyObjectName, null);
        }

        internal override object InternalReadObject(XmlReaderDelegator xmlReader, bool verifyObjectName, DataContractResolver dataContractResolver)
        {
            if (MaxItemsInObjectGraph == 0)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ExceededMaxItemsQuota, MaxItemsInObjectGraph)));

            if (dataContractResolver == null)
                dataContractResolver = this.DataContractResolver;

#if uapaot
            // Give the root contract a chance to initialize or pre-verify the read
            RootContract.PrepareToRead(xmlReader);
#endif
            if (verifyObjectName)
            {
                if (!InternalIsStartObject(xmlReader))
                {
                    XmlDictionaryString expectedName;
                    XmlDictionaryString expectedNs;
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
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationExceptionWithReaderDetails(SR.Format(SR.ExpectingElement, expectedNs, expectedName), xmlReader));
                }
            }
            else if (!IsStartElement(xmlReader))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationExceptionWithReaderDetails(SR.Format(SR.ExpectingElementAtDeserialize, XmlNodeType.Element), xmlReader));
            }

            DataContract contract = RootContract;
            if (contract.IsPrimitive && object.ReferenceEquals(contract.UnderlyingType, _rootType) /*handle Nullable<T> differently*/)
            {
                return contract.ReadXmlValue(xmlReader, null);
            }

            if (IsRootXmlAny(_rootName, contract))
            {
                return XmlObjectSerializerReadContext.ReadRootIXmlSerializable(xmlReader, contract as XmlDataContract, false /*isMemberType*/);
            }

            XmlObjectSerializerReadContext context = XmlObjectSerializerReadContext.CreateContext(this, contract, dataContractResolver);

            return context.InternalDeserialize(xmlReader, _rootType, contract, null, null);
        }

        internal override bool InternalIsStartObject(XmlReaderDelegator reader)
        {
            return IsRootElement(reader, RootContract, _rootName, _rootNamespace);
        }

        internal override Type GetSerializeType(object graph)
        {
            return (graph == null) ? _rootType : graph.GetType();
        }

        internal override Type GetDeserializeType()
        {
            return _rootType;
        }

        internal static object SurrogateToDataContractType(ISerializationSurrogateProvider serializationSurrogateProvider, object oldObj, Type surrogatedDeclaredType, ref Type objType)
        {
            object obj = DataContractSurrogateCaller.GetObjectToSerialize(serializationSurrogateProvider, oldObj, objType, surrogatedDeclaredType);
            if (obj != oldObj)
            {
                objType = obj != null ? obj.GetType() : Globals.TypeOfObject;
            }
            return obj;
        }

        internal static Type GetSurrogatedType(ISerializationSurrogateProvider serializationSurrogateProvider, Type type)
        {
            return DataContractSurrogateCaller.GetDataContractType(serializationSurrogateProvider, DataContract.UnwrapNullableType(type));
        }
    }
}
