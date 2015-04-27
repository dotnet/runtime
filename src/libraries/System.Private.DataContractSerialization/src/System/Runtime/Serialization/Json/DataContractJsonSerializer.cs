// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------
//------------------------------------------------------------

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
    public sealed class DataContractJsonSerializer
    {
        private const char BACK_SLASH = '\\';
        private const char FORWARD_SLASH = '/';
        private const char HIGH_SURROGATE_START = (char)0xd800;
        private const char LOW_SURROGATE_END = (char)0xdfff;
        private const char MAX_CHAR = (char)0xfffe;
        private const char WHITESPACE = ' ';
        private const char CARRIAGE_RETURN = '\r';
        private const char NEWLINE = '\n';


        internal IList<Type> knownTypeList;
        internal DataContractDictionary knownDataContracts;
        private EmitTypeInformation _emitTypeInformation;
        private bool _ignoreExtensionDataObject;
        private ReadOnlyCollection<Type> _knownTypeCollection;
        private int _maxItemsInObjectGraph;
        private DataContract _rootContract; // post-surrogate
        private XmlDictionaryString _rootName;
        private JavaScriptSerializer _jsonSerializer;
        private JavaScriptDeserializer _jsonDeserializer;
        private bool _rootNameRequiresMapping;
        private Type _rootType;
        private bool _serializeReadOnlyTypes;
        private DateTimeFormat _dateTimeFormat;
        private bool _useSimpleDictionaryFormat;

        public DataContractJsonSerializer(Type type)
            : this(type, (IEnumerable<Type>)null)
        {
        }

        public DataContractJsonSerializer(Type type, IEnumerable<Type> knownTypes)
        {
            Initialize(type, knownTypes);
        }




        public DataContractJsonSerializer(Type type, DataContractJsonSerializerSettings settings)
        {
            if (settings == null)
            {
                settings = new DataContractJsonSerializerSettings();
            }

            XmlDictionaryString rootName = (settings.RootName == null) ? null : new XmlDictionary(1).Add(settings.RootName);
            Initialize(type, rootName, settings.KnownTypes, settings.MaxItemsInObjectGraph, settings.IgnoreExtensionDataObject,
                settings.EmitTypeInformation, settings.SerializeReadOnlyTypes, settings.DateTimeFormat, settings.UseSimpleDictionaryFormat);
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

        internal DataContractDictionary KnownDataContracts
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
        internal bool AlwaysEmitTypeInformation
        {
            get
            {
                return _emitTypeInformation == EmitTypeInformation.Always;
            }
        }

        public DateTimeFormat DateTimeFormat
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

        private void AddCollectionItemTypeToKnownTypes(Type knownType)
        {
            Type itemType;
            Type typeToCheck = knownType;
            while (CollectionDataContract.IsCollection(typeToCheck, out itemType))
            {
                if (itemType.GetTypeInfo().IsGenericType && (itemType.GetGenericTypeDefinition() == Globals.TypeOfKeyValue))
                {
                    itemType = Globals.TypeOfKeyValuePair.MakeGenericType(itemType.GetGenericArguments());
                }
                this.knownTypeList.Add(itemType);
                typeToCheck = itemType;
            }
        }

        private void Initialize(Type type,
 IEnumerable<Type> knownTypes)
        {
            XmlObjectSerializer.CheckNull(type, "type");
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
        }


        private void Initialize(Type type,
            IEnumerable<Type> knownTypes,
            int maxItemsInObjectGraph,
            bool ignoreExtensionDataObject,
            EmitTypeInformation emitTypeInformation,
            bool serializeReadOnlyTypes,
            DateTimeFormat dateTimeFormat,
            bool useSimpleDictionaryFormat)
        {
            CheckNull(type, "type");
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
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("maxItemsInObjectGraph", SR.Format(SR.ValueMustBeNonNegative)));
            }
            _maxItemsInObjectGraph = maxItemsInObjectGraph;
            _ignoreExtensionDataObject = ignoreExtensionDataObject;
            _emitTypeInformation = emitTypeInformation;
            _serializeReadOnlyTypes = serializeReadOnlyTypes;
            _dateTimeFormat = dateTimeFormat;
            _useSimpleDictionaryFormat = useSimpleDictionaryFormat;
        }

        private void Initialize(Type type,
            XmlDictionaryString rootName,
            IEnumerable<Type> knownTypes,
            int maxItemsInObjectGraph,
            bool ignoreExtensionDataObject,
            EmitTypeInformation emitTypeInformation,
            bool serializeReadOnlyTypes,
            DateTimeFormat dateTimeFormat,
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
                throw System.ServiceModel.DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    XmlObjectSerializer.CreateSerializationException(SR.Format(
                        SR.JsonUnsupportedForIsReference,
                        DataContract.GetClrTypeFullName(dataContract.UnderlyingType),
                        dataContract.IsReference)));
            }
        }

        internal static DataContract GetDataContract(DataContract declaredTypeContract, Type declaredType, Type objectType)
        {
            DataContract contract = DataContractSerializer.GetDataContract(declaredTypeContract, declaredType, objectType);
            CheckIfTypeIsReference(contract);
            return contract;
        }

        public void WriteObject(Stream stream, object graph)
        {
            _jsonSerializer = new JavaScriptSerializer(stream);
            DataContract contract = RootContract;
            Type declaredType = contract.UnderlyingType;
            Type graphType = (graph == null) ? declaredType : graph.GetType();
            System.Runtime.Serialization.XmlWriterDelegator writer = null;
            if (graph == null)
            {
                _jsonSerializer.SerializeObject(null);
            }
            else
            {
                if (declaredType == graphType)
                {
                    if (contract.CanContainReferences)
                    {
                        XmlObjectSerializerWriteContextComplexJson context = XmlObjectSerializerWriteContextComplexJson.CreateContext(this, contract);
                        context.SerializeWithoutXsiType(contract, writer, graph, declaredType.TypeHandle);
                    }
                    else
                    {
                        WriteObjectInternal(graph, contract, null, false, declaredType.TypeHandle);
                    }
                }
                else
                {
                    XmlObjectSerializerWriteContextComplexJson context = XmlObjectSerializerWriteContextComplexJson.CreateContext(this, RootContract);
                    contract = DataContractSerializer.GetDataContract(contract, declaredType, graphType);

                    if (contract.CanContainReferences)
                    {
                        context.SerializeWithXsiTypeAtTopLevel(contract, writer, graph, declaredType.TypeHandle, graphType);
                    }
                    else
                    {
                        context.SerializeWithoutXsiType(contract, writer, graph, declaredType.TypeHandle);
                    }
                }
            }
        }

        internal void WriteObjectInternal(object value, DataContract contract, XmlObjectSerializerWriteContextComplexJson context, bool writeServerType, RuntimeTypeHandle declaredTypeHandle)
        {
            _jsonSerializer.SerializeObject(ConvertDataContractToObject(value, contract, context, writeServerType, declaredTypeHandle));
        }

        internal object ConvertDataContractToObject(object value, DataContract contract, XmlObjectSerializerWriteContextComplexJson context, bool writeServerType, RuntimeTypeHandle declaredTypeHandle)
        {
            if (context != null)
            {
                context.OnHandleReference(null /*XmlWriter*/, value, true); //  canContainReferences 
            }
            try
            {
                if (contract is ObjectDataContract)
                {
                    Type valueType = value.GetType();
                    if (valueType != Globals.TypeOfObject)
                        return ConvertDataContractToObject(value, DataContract.GetDataContract(valueType), context, true, contract.UnderlyingType.TypeHandle);
                    else
                        return value;
                }
                else if (contract is TimeSpanDataContract)
                {
                    return XmlConvert.ToString((TimeSpan)value);
                }
                else if (contract is QNameDataContract)
                {
                    XmlQualifiedName qname = (XmlQualifiedName)value;
                    return (qname.IsEmpty) ? string.Empty : (qname.Name + ":" + qname.Namespace);
                }
                else if (contract is PrimitiveDataContract)
                {
                    return value;
                }
                else if (contract is CollectionDataContract)
                {
                    CollectionDataContract collectionContract = contract as CollectionDataContract;

                    switch (collectionContract.Kind)
                    {
                        case CollectionKind.GenericDictionary:
                        case CollectionKind.Dictionary:
                            return DataContractToObjectConverter.ConvertGenericDictionaryToArray(this, (IEnumerable)value, collectionContract, context, writeServerType);
                        default:
                            return DataContractToObjectConverter.ConvertGenericListToArray(this, (IEnumerable)value, collectionContract, context, writeServerType);
                    }
                }
                else if (contract is ClassDataContract)
                {
                    ClassDataContract classContract = contract as ClassDataContract;

                    if (Globals.TypeOfScriptObject_IsAssignableFrom(classContract.UnderlyingType))
                    {
                        return ConvertScriptObjectToObject(value);
                    }

                    return DataContractToObjectConverter.ConvertClassDataContractToDictionary(this, (ClassDataContract)contract, value, context, writeServerType);
                }
                else if (contract is EnumDataContract)
                {
                    if (((EnumDataContract)contract).IsULong)
                        return Convert.ToUInt64(value, null);
                    else
                        return Convert.ToInt64(value, null);
                }
                else if (contract is XmlDataContract)
                {
                    DataContractSerializer dataContractSerializer = new DataContractSerializer(Type.GetTypeFromHandle(declaredTypeHandle), GetKnownTypesFromContext(context, (context == null) ? null : context.SerializerKnownTypeList));
                    MemoryStream memoryStream = new MemoryStream();
                    dataContractSerializer.WriteObject(memoryStream, value);
                    memoryStream.Position = 0;
                    return new StreamReader(memoryStream, Encoding.UTF8).ReadToEnd();
                }
            }
            finally
            {
                if (context != null)
                {
                    context.OnEndHandleReference(null /*XmlWriter*/, value, true); //  canContainReferences 
                }
            }
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.UnknownDataContract, contract.Name)));
        }

        private object ConvertScriptObjectToObject(object value)
        {
            string jsonValue = Globals.ScriptObjectJsonSerialize(value);
            using (MemoryStream jsonStream = new MemoryStream(Encoding.Unicode.GetBytes(jsonValue)))
            {
                JavaScriptDeserializer jsDeserializer = new JavaScriptDeserializer(jsonStream);
                return jsDeserializer.DeserializeObject();
            }
        }

        public object ReadObject(Stream stream)
        {
            try
            {
                DataContract contract = RootContract;
                AddCollectionItemContractsToKnownDataContracts(contract);
                _jsonDeserializer = new JavaScriptDeserializer(stream);
                XmlObjectSerializerReadContextComplexJson context = new XmlObjectSerializerReadContextComplexJson(this, RootContract);
                object obj = ConvertObjectToDataContract(RootContract, _jsonDeserializer.DeserializeObject(), context);
                return obj;
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException || e is FormatException || e is OverflowException)
                {
                    throw XmlObjectSerializer.CreateSerializationException(XmlObjectSerializer.GetTypeInfoError(SR.ErrorDeserializing, _rootType, e), e);
                }
                throw;
            }
        }

        private object ConvertObjectToPrimitiveDataContract(DataContract contract, object value, XmlObjectSerializerReadContextComplexJson context)
        {
            // Taking the right deserialized value for datetime string based on contract information
            var tuple = value as Tuple<DateTime, string>;
            if (tuple != null)
            {
                if (contract is StringDataContract || contract.UnderlyingType == typeof(object))
                {
                    value = tuple.Item2;
                }
                else
                {
                    value = tuple.Item1;
                }
            }

            if (contract is TimeSpanDataContract)
            {
                return XmlConvert.ToTimeSpan(String.Format(CultureInfo.InvariantCulture, "{0}", value));
            }
            else if (contract is ByteArrayDataContract)
            {
                return ObjectToDataContractConverter.ConvertToArray(typeof(Byte), (IList)value);
            }
            else if (contract is GuidDataContract)
            {
                return new Guid(String.Format(CultureInfo.InvariantCulture, "{0}", value));
            }
            else if (contract is ObjectDataContract)
            {
                if (value is ICollection)
                {
                    return ConvertObjectToDataContract(DataContract.GetDataContract(Globals.TypeOfObjectArray), value, context);
                }

                return TryParseJsonNumber(value);
            }
            else if (contract is QNameDataContract)
            {
                return XmlObjectSerializerReadContextComplexJson.ParseQualifiedName(value.ToString());
            }
            else if (contract is StringDataContract)
            {
                if (value is bool)
                {
                    return ((bool)value) ? Globals.True : Globals.False;
                }
                return value.ToString();
            }
            else if (contract is UriDataContract)
            {
                return new Uri(value.ToString(), UriKind.RelativeOrAbsolute);
            }
            else if (contract is DoubleDataContract)
            {
                if (value is float)
                {
                    return (double)(float)value;
                }
                if (value is double)
                {
                    return (double)value;
                }
                return double.Parse(String.Format(CultureInfo.InvariantCulture, "{0}", value), NumberStyles.Float, CultureInfo.InvariantCulture);
            }
            else if (contract is DecimalDataContract)
            {
                return decimal.Parse(String.Format(CultureInfo.InvariantCulture, "{0}", value), NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            return Convert.ChangeType(value, contract.UnderlyingType, CultureInfo.InvariantCulture);
        }

        internal object ConvertObjectToDataContract(DataContract contract, object value, XmlObjectSerializerReadContextComplexJson context)
        {
            if (value == null)
            {
                return value;
            }
            else if (contract is PrimitiveDataContract)
            {
                return ConvertObjectToPrimitiveDataContract(contract, value, context);
            }
            else if (contract is CollectionDataContract)
            {
                return ObjectToDataContractConverter.ConvertICollectionToCollectionDataContract(this, (CollectionDataContract)contract, value, context);
            }
            else if (contract is ClassDataContract)
            {
                ClassDataContract classContract = contract as ClassDataContract;

                if (Globals.TypeOfScriptObject_IsAssignableFrom(classContract.UnderlyingType))
                {
                    return ConvertObjectToScriptObject(value);
                }

                return ObjectToDataContractConverter.ConvertDictionaryToClassDataContract(this, classContract, (Dictionary<string, object>)value, context);
            }
            else if (contract is EnumDataContract)
            {
                return Enum.ToObject(contract.UnderlyingType, ((EnumDataContract)contract).IsULong ? ulong.Parse(String.Format(CultureInfo.InvariantCulture, "{0}", value), NumberStyles.Float, NumberFormatInfo.InvariantInfo) : value);
            }
            else if (contract is XmlDataContract)
            {
                DataContractSerializer dataContractSerializer = new DataContractSerializer(contract.UnderlyingType, GetKnownTypesFromContext(context, (context == null) ? null : context.SerializerKnownTypeList));
                MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes((string)value));
                return dataContractSerializer.ReadObject(XmlDictionaryReader.CreateTextReader(memoryStream, XmlDictionaryReaderQuotas.Max));
            }
            return value;
        }

        private object ConvertObjectToScriptObject(object deserialzedValue)
        {
            MemoryStream memStream = new MemoryStream();
            JavaScriptSerializer jsSerializer = new JavaScriptSerializer(memStream);
            jsSerializer.SerializeObject(deserialzedValue);
            memStream.Flush();
            memStream.Position = 0;
            return Globals.ScriptObjectJsonDeserialize(new StreamReader(memStream).ReadToEnd());
        }

        private object TryParseJsonNumber(object value)
        {
            string input = value as string;

            if (input != null && input.IndexOfAny(JsonGlobals.FloatingPointCharacters) >= 0)
            {
                return JavaScriptObjectDeserializer.ParseJsonNumberAsDoubleOrDecimal(input);
            }
            return value;
        }

        private List<Type> GetKnownTypesFromContext(XmlObjectSerializerContext context, IList<Type> serializerKnownTypeList)
        {
            List<Type> knownTypesList = new List<Type>();
            if (context != null)
            {
                List<XmlQualifiedName> stableNames = new List<XmlQualifiedName>();
                Dictionary<XmlQualifiedName, DataContract>[] entries = context.scopedKnownTypes.dataContractDictionaries;
                if (entries != null)
                {
                    for (int i = 0; i < entries.Length; i++)
                    {
                        Dictionary<XmlQualifiedName, DataContract> entry = entries[i];
                        if (entry != null)
                        {
                            foreach (KeyValuePair<XmlQualifiedName, DataContract> pair in entry)
                            {
                                if (!stableNames.Contains(pair.Key))
                                {
                                    stableNames.Add(pair.Key);
                                    knownTypesList.Add(pair.Value.UnderlyingType);
                                }
                            }
                        }
                    }
                }
                if (serializerKnownTypeList != null)
                {
                    knownTypesList.AddRange(serializerKnownTypeList);
                }
            }
            return knownTypesList;
        }

        private void AddCollectionItemContractsToKnownDataContracts(DataContract traditionalDataContract)
        {
            if (traditionalDataContract.KnownDataContracts != null)
            {
                foreach (KeyValuePair<XmlQualifiedName, DataContract> knownDataContract in traditionalDataContract.KnownDataContracts)
                {
                    if (!object.ReferenceEquals(knownDataContract, null))
                    {
                        CollectionDataContract collectionDataContract = knownDataContract.Value as CollectionDataContract;
                        while (collectionDataContract != null)
                        {
                            DataContract itemContract = collectionDataContract.ItemContract;
                            if (knownDataContracts == null)
                            {
                                knownDataContracts = new Dictionary<XmlQualifiedName, DataContract>();
                            }

                            if (!knownDataContracts.ContainsKey(itemContract.StableName))
                            {
                                knownDataContracts.Add(itemContract.StableName, itemContract);
                            }

                            if (collectionDataContract.ItemType.GetTypeInfo().IsGenericType
                                && collectionDataContract.ItemType.GetGenericTypeDefinition() == typeof(KeyValue<,>))
                            {
                                DataContract itemDataContract = DataContract.GetDataContract(Globals.TypeOfKeyValuePair.MakeGenericType(collectionDataContract.ItemType.GetGenericArguments()));
                                if (!knownDataContracts.ContainsKey(itemDataContract.StableName))
                                {
                                    knownDataContracts.Add(itemDataContract.StableName, itemDataContract);
                                }
                            }

                            if (!(itemContract is CollectionDataContract))
                            {
                                break;
                            }
                            collectionDataContract = itemContract as CollectionDataContract;
                        }
                    }
                }
            }
        }

        static internal void InvokeOnSerializing(Object value, DataContract contract, XmlObjectSerializerWriteContextComplexJson context)
        {
            if (contract is ClassDataContract)
            {
                ClassDataContract classContract = contract as ClassDataContract;

                if (classContract.BaseContract != null)
                    InvokeOnSerializing(value, classContract.BaseContract, context);
                if (classContract.OnSerializing != null)
                {
                    bool memberAccessFlag = classContract.RequiresMemberAccessForWrite(null, JsonGlobals.JsonSerializationPatterns);
                    try
                    {
                        classContract.OnSerializing.Invoke(value, new object[] { context.GetStreamingContext() });
                    }
                    catch (SecurityException securityException)
                    {
                        if (memberAccessFlag)
                        {
                            classContract.RequiresMemberAccessForWrite(securityException, JsonGlobals.JsonSerializationPatterns);
                        }
                        else
                        {
                            throw;
                        }
                    }
                    catch (TargetInvocationException targetInvocationException)
                    {
                        if (targetInvocationException.InnerException == null)
                            throw;
                        //We are catching the TIE here and throws the inner exception only,
                        //this is needed to have a consistent exception story in all serializers
                        throw targetInvocationException.InnerException;
                    }
                }
            }
        }

        static internal void InvokeOnSerialized(Object value, DataContract contract, XmlObjectSerializerWriteContextComplexJson context)
        {
            if (contract is ClassDataContract)
            {
                ClassDataContract classContract = contract as ClassDataContract;

                if (classContract.BaseContract != null)
                    InvokeOnSerialized(value, classContract.BaseContract, context);
                if (classContract.OnSerialized != null)
                {
                    bool memberAccessFlag = classContract.RequiresMemberAccessForWrite(null, JsonGlobals.JsonSerializationPatterns);
                    try
                    {
                        classContract.OnSerialized.Invoke(value, new object[] { context.GetStreamingContext() });
                    }
                    catch (SecurityException securityException)
                    {
                        if (memberAccessFlag)
                        {
                            classContract.RequiresMemberAccessForWrite(securityException, JsonGlobals.JsonSerializationPatterns);
                        }
                        else
                        {
                            throw;
                        }
                    }
                    catch (TargetInvocationException targetInvocationException)
                    {
                        if (targetInvocationException.InnerException == null)
                            throw;
                        //We are catching the TIE here and throws the inner exception only,
                        //this is needed to have a consistent exception story in all serializers
                        throw targetInvocationException.InnerException;
                    }
                }
            }
        }

        static internal void InvokeOnDeserializing(Object value, DataContract contract, XmlObjectSerializerReadContextComplexJson context)
        {
            if (contract is ClassDataContract)
            {
                ClassDataContract classContract = contract as ClassDataContract;

                if (classContract.BaseContract != null)
                    InvokeOnDeserializing(value, classContract.BaseContract, context);
                if (classContract.OnDeserializing != null)
                {
                    bool memberAccessFlag = classContract.RequiresMemberAccessForRead(null, JsonGlobals.JsonSerializationPatterns);
                    try
                    {
                        classContract.OnDeserializing.Invoke(value, new object[] { context.GetStreamingContext() });
                    }
                    catch (SecurityException securityException)
                    {
                        if (memberAccessFlag)
                        {
                            classContract.RequiresMemberAccessForRead(securityException, JsonGlobals.JsonSerializationPatterns);
                        }
                        else
                        {
                            throw;
                        }
                    }
                    catch (TargetInvocationException targetInvocationException)
                    {
                        if (targetInvocationException.InnerException == null)
                            throw;
                        //We are catching the TIE here and throws the inner exception only,
                        //this is needed to have a consistent exception story in all serializers
                        throw targetInvocationException.InnerException;
                    }
                }
            }
        }

        static internal void InvokeOnDeserialized(object value, DataContract contract, XmlObjectSerializerReadContextComplexJson context)
        {
            if (contract is ClassDataContract)
            {
                ClassDataContract classContract = contract as ClassDataContract;

                if (classContract.BaseContract != null)
                    InvokeOnDeserialized(value, classContract.BaseContract, context);
                if (classContract.OnDeserialized != null)
                {
                    bool memberAccessFlag = classContract.RequiresMemberAccessForRead(null, JsonGlobals.JsonSerializationPatterns);
                    try
                    {
                        classContract.OnDeserialized.Invoke(value, new object[] { context.GetStreamingContext() });
                    }
                    catch (SecurityException securityException)
                    {
                        if (memberAccessFlag)
                        {
                            classContract.RequiresMemberAccessForRead(securityException, JsonGlobals.JsonSerializationPatterns);
                        }
                        else
                        {
                            throw;
                        }
                    }
                    catch (TargetInvocationException targetInvocationException)
                    {
                        if (targetInvocationException.InnerException == null)
                            throw;
                        //We are catching the TIE here and throws the inner exception only,
                        //this is needed to have a consistent exception story in all serializers
                        throw targetInvocationException.InnerException;
                    }
                }
            }
        }

        internal static bool CharacterNeedsEscaping(char ch)
        {
            return (ch == FORWARD_SLASH || ch == JsonGlobals.QuoteChar || ch < WHITESPACE || ch == BACK_SLASH
                || (ch >= HIGH_SURROGATE_START && (ch <= LOW_SURROGATE_END || ch >= MAX_CHAR)));
        }

        internal static bool CheckIfJsonNameRequiresMapping(string jsonName)
        {
            if (jsonName != null)
            {
                if (!DataContract.IsValidNCName(jsonName))
                {
                    return true;
                }

                for (int i = 0; i < jsonName.Length; i++)
                {
                    if (CharacterNeedsEscaping(jsonName[i]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal static bool CheckIfJsonNameRequiresMapping(XmlDictionaryString jsonName)
        {
            return (jsonName == null) ? false : CheckIfJsonNameRequiresMapping(jsonName.Value);
        }

        internal bool CheckIfNeedsContractNsAtRoot(XmlDictionaryString name, XmlDictionaryString ns, DataContract contract)
        {
            if (name == null)
                return false;

            if (contract.IsBuiltInDataContract || !contract.CanContainReferences)
                return false;

            string contractNs = XmlDictionaryString.GetString(contract.Namespace);
            if (string.IsNullOrEmpty(contractNs) || contractNs == XmlDictionaryString.GetString(ns))
                return false;

            return true;
        }

        internal static void CheckNull(object obj, string name)
        {
            if (obj == null)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(name));
        }

        internal static string ConvertXmlNameToJsonName(string xmlName)
        {
            return XmlConvert.DecodeName(xmlName);
        }

        internal static XmlDictionaryString ConvertXmlNameToJsonName(XmlDictionaryString xmlName)
        {
            return (xmlName == null) ? null : new XmlDictionary().Add(ConvertXmlNameToJsonName(xmlName.Value));
        }
    }
}
