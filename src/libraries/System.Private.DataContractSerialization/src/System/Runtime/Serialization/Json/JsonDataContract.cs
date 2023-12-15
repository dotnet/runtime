// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime;
using System.Runtime.Serialization;
using System.Runtime.Serialization.DataContracts;
using System.Xml;

using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContracts.DataContract>;

namespace System.Runtime.Serialization.Json
{
    internal class JsonDataContract
    {
        private readonly JsonDataContractCriticalHelper _helper;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected JsonDataContract(DataContract traditionalDataContract)
        {
            _helper = new JsonDataContractCriticalHelper(traditionalDataContract);
        }

        protected JsonDataContract(JsonDataContractCriticalHelper helper)
        {
            _helper = helper;
        }

        internal virtual string? TypeName => null;

        protected JsonDataContractCriticalHelper Helper => _helper;

        protected DataContract TraditionalDataContract => _helper.TraditionalDataContract;

        private DataContractDictionary? KnownDataContracts => _helper.KnownDataContracts;

        public static JsonReadWriteDelegates? GetGeneratedReadWriteDelegates(DataContract c)
        {
            // this method used to be rewritten by an IL transform
            // with the restructuring for multi-file, this is no longer true - instead
            // this has become a normal method
            JsonReadWriteDelegates? result;
            return JsonReadWriteDelegates.GetJsonDelegates().TryGetValue(c, out result) ? result : null;
        }

        internal static JsonReadWriteDelegates GetReadWriteDelegatesFromGeneratedAssembly(DataContract c)
        {
            JsonReadWriteDelegates? result = GetGeneratedReadWriteDelegates(c);
            if (result == null)
            {
                throw new InvalidDataContractException(SR.Format(SR.SerializationCodeIsMissingForType, c.UnderlyingType));
            }
            else
            {
                return result;
            }
        }

        internal static JsonReadWriteDelegates? TryGetReadWriteDelegatesFromGeneratedAssembly(DataContract c)
        {
            JsonReadWriteDelegates? result = GetGeneratedReadWriteDelegates(c);
            return result;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public static JsonDataContract GetJsonDataContract(DataContract traditionalDataContract)
        {
            return JsonDataContractCriticalHelper.GetJsonDataContract(traditionalDataContract);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public object? ReadJsonValue(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson? context)
        {
            PushKnownDataContracts(context);
            object? deserializedObject = ReadJsonValueCore(jsonReader, context);
            PopKnownDataContracts(context);
            return deserializedObject;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public virtual object? ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson? context)
        {
            return TraditionalDataContract.ReadXmlValue(jsonReader, context);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void WriteJsonValue(XmlWriterDelegator jsonWriter, object obj, XmlObjectSerializerWriteContextComplexJson? context, RuntimeTypeHandle declaredTypeHandle)
        {
            PushKnownDataContracts(context);
            WriteJsonValueCore(jsonWriter, obj, context, declaredTypeHandle);
            PopKnownDataContracts(context);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public virtual void WriteJsonValueCore(XmlWriterDelegator jsonWriter, object obj, XmlObjectSerializerWriteContextComplexJson? context, RuntimeTypeHandle declaredTypeHandle)
        {
            TraditionalDataContract.WriteXmlValue(jsonWriter, obj, context);
        }

        protected static object HandleReadValue(object obj, XmlObjectSerializerReadContext context)
        {
            context.AddNewObject(obj);
            return obj;
        }

        protected static bool TryReadNullAtTopLevel(XmlReaderDelegator reader)
        {
            if (reader.MoveToAttribute(JsonGlobals.typeString) && (reader.Value == JsonGlobals.nullString))
            {
                reader.Skip();
                reader.MoveToElement();
                return true;
            }

            reader.MoveToElement();
            return false;
        }

        protected void PopKnownDataContracts(XmlObjectSerializerContext? context)
        {
            if (KnownDataContracts != null)
            {
                Debug.Assert(context != null);
                context.scopedKnownTypes.Pop();
            }
        }

        protected void PushKnownDataContracts(XmlObjectSerializerContext? context)
        {
            if (KnownDataContracts != null)
            {
                Debug.Assert(context != null);
                context.scopedKnownTypes.Push(KnownDataContracts);
            }
        }

        internal class JsonDataContractCriticalHelper
        {
            private static readonly object s_cacheLock = new object();
            private static readonly object s_createDataContractLock = new object();

            private static JsonDataContract[] s_dataContractCache = new JsonDataContract[32];
            private static int s_dataContractID;

            private static readonly ConcurrentDictionary<nint, int> s_typeToIDCache = new();
            private DataContractDictionary? _knownDataContracts;
            private readonly DataContract _traditionalDataContract;
            private readonly string _typeName;

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal JsonDataContractCriticalHelper(DataContract traditionalDataContract)
            {
                _traditionalDataContract = traditionalDataContract;
                AddCollectionItemContractsToKnownDataContracts();
                _typeName = string.IsNullOrEmpty(traditionalDataContract.Namespace.Value) ? traditionalDataContract.Name.Value : string.Concat(traditionalDataContract.Name.Value, JsonGlobals.NameValueSeparatorString, XmlObjectSerializerWriteContextComplexJson.TruncateDefaultDataContractNamespace(traditionalDataContract.Namespace.Value));
            }

            internal DataContractDictionary? KnownDataContracts => _knownDataContracts;

            internal DataContract TraditionalDataContract => _traditionalDataContract;

            internal virtual string TypeName => _typeName;

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            public static JsonDataContract GetJsonDataContract(DataContract traditionalDataContract)
            {
                int id = JsonDataContractCriticalHelper.GetId(traditionalDataContract.UnderlyingType.TypeHandle);
                JsonDataContract dataContract = s_dataContractCache[id];
                if (dataContract == null)
                {
                    dataContract = CreateJsonDataContract(id, traditionalDataContract);
                    s_dataContractCache[id] = dataContract;
                }
                return dataContract;
            }

            internal static int GetId(RuntimeTypeHandle typeHandle)
            {
                if (s_typeToIDCache.TryGetValue(typeHandle.Value, out int id))
                    return id;

                lock (s_cacheLock)
                {
                    return s_typeToIDCache.GetOrAdd(typeHandle.Value, static _ =>
                    {
                        int nextId = s_dataContractID++;
                        if (nextId >= s_dataContractCache.Length)
                        {
                            int newSize = (nextId < int.MaxValue / 2) ? nextId * 2 : int.MaxValue;
                            if (newSize <= nextId)
                            {
                                Debug.Fail("DataContract cache overflow");
                                throw new SerializationException(SR.DataContractCacheOverflow);
                            }
                            Array.Resize<JsonDataContract>(ref s_dataContractCache, newSize);
                        }
                        return nextId;
                    });
                }
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private static JsonDataContract CreateJsonDataContract(int id, DataContract traditionalDataContract)
            {
                lock (s_createDataContractLock)
                {
                    JsonDataContract dataContract = s_dataContractCache[id];
                    if (dataContract == null)
                    {
                        Type traditionalDataContractType = traditionalDataContract.GetType();
                        if (traditionalDataContractType == typeof(ObjectDataContract))
                        {
                            dataContract = new JsonObjectDataContract(traditionalDataContract);
                        }
                        else if (traditionalDataContractType == typeof(StringDataContract))
                        {
                            dataContract = new JsonStringDataContract((StringDataContract)traditionalDataContract);
                        }
                        else if (traditionalDataContractType == typeof(UriDataContract))
                        {
                            dataContract = new JsonUriDataContract((UriDataContract)traditionalDataContract);
                        }
                        else if (traditionalDataContractType == typeof(QNameDataContract))
                        {
                            dataContract = new JsonQNameDataContract((QNameDataContract)traditionalDataContract);
                        }
                        else if (traditionalDataContractType == typeof(ByteArrayDataContract))
                        {
                            dataContract = new JsonByteArrayDataContract((ByteArrayDataContract)traditionalDataContract);
                        }
                        else if (traditionalDataContract.IsPrimitive ||
                            traditionalDataContract.UnderlyingType == Globals.TypeOfXmlQualifiedName)
                        {
                            dataContract = new JsonDataContract(traditionalDataContract);
                        }
                        else if (traditionalDataContractType == typeof(ClassDataContract))
                        {
                            dataContract = new JsonClassDataContract((ClassDataContract)traditionalDataContract);
                        }
                        else if (traditionalDataContractType == typeof(EnumDataContract))
                        {
                            dataContract = new JsonEnumDataContract((EnumDataContract)traditionalDataContract);
                        }
                        else if ((traditionalDataContractType == typeof(GenericParameterDataContract)) ||
                            (traditionalDataContractType == typeof(SpecialTypeDataContract)))
                        {
                            dataContract = new JsonDataContract(traditionalDataContract);
                        }
                        else if (traditionalDataContractType == typeof(CollectionDataContract))
                        {
                            dataContract = new JsonCollectionDataContract((CollectionDataContract)traditionalDataContract);
                        }
                        else if (traditionalDataContractType == typeof(XmlDataContract))
                        {
                            dataContract = new JsonXmlDataContract((XmlDataContract)traditionalDataContract);
                        }
                        else
                        {
                            throw new ArgumentException(SR.Format(SR.JsonTypeNotSupportedByDataContractJsonSerializer, traditionalDataContract.UnderlyingType), nameof(traditionalDataContract));
                        }
                    }
                    return dataContract;
                }
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private void AddCollectionItemContractsToKnownDataContracts()
            {
                if (_traditionalDataContract.KnownDataContracts != null)
                {
                    foreach (KeyValuePair<XmlQualifiedName, DataContract> knownDataContract in _traditionalDataContract.KnownDataContracts)
                    {
                        CollectionDataContract? collectionDataContract = knownDataContract.Value as CollectionDataContract;
                        while (collectionDataContract != null)
                        {
                            DataContract itemContract = collectionDataContract.ItemContract;
                            _knownDataContracts ??= new DataContractDictionary();

                            _knownDataContracts.TryAdd(itemContract.XmlName, itemContract);

                            if (collectionDataContract.ItemType.IsGenericType
                                && collectionDataContract.ItemType.GetGenericTypeDefinition() == typeof(KeyValue<,>))
                            {
                                DataContract itemDataContract = DataContract.GetDataContract(Globals.TypeOfKeyValuePair.MakeGenericType(collectionDataContract.ItemType.GenericTypeArguments));
                                _knownDataContracts.TryAdd(itemDataContract.XmlName, itemDataContract);
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
    }

    internal sealed class JsonReadWriteDelegates
    {
        // this is the global dictionary for JSON delegates introduced for multi-file
        private static readonly Dictionary<DataContract, JsonReadWriteDelegates> s_jsonDelegates = new Dictionary<DataContract, JsonReadWriteDelegates>();

        public static Dictionary<DataContract, JsonReadWriteDelegates> GetJsonDelegates()
        {
            return s_jsonDelegates;
        }

        public JsonFormatClassWriterDelegate? ClassWriterDelegate { get; set; }
        public JsonFormatClassReaderDelegate? ClassReaderDelegate { get; set; }
        public JsonFormatCollectionWriterDelegate? CollectionWriterDelegate { get; set; }
        public JsonFormatCollectionReaderDelegate? CollectionReaderDelegate { get; set; }
        public JsonFormatGetOnlyCollectionReaderDelegate? GetOnlyCollectionReaderDelegate { get; set; }
    }
}
