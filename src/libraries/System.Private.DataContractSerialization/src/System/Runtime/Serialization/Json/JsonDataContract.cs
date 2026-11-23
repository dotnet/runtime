// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.DataContracts;
using System.Threading;
using System.Xml;

using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContracts.DataContract>;

namespace System.Runtime.Serialization.Json
{
    internal partial class JsonDataContract
    {
        private protected const int DictionaryFormatCount = 2;

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

        private protected bool HasDictionaryFormatVariants => _helper.HasDictionaryFormatVariants;

        private protected void PublishDictionaryFormatPlan<TDelegate>(TDelegate?[] plans, int format, TDelegate plan)
            where TDelegate : Delegate
        {
            if (HasDictionaryFormatVariants)
            {
                Volatile.Write(ref plans[format], plan);
            }
            else
            {
                Volatile.Write(ref plans[(int)JsonDictionaryFormat.Array], plan);
                Volatile.Write(ref plans[(int)JsonDictionaryFormat.Simple], plan);
            }
        }

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
            private static readonly ConditionalWeakTable<DataContract, JsonDataContract> s_dataContractCache = new ConditionalWeakTable<DataContract, JsonDataContract>();
            private DataContractDictionary? _knownDataContracts;
            private readonly DataContract _traditionalDataContract;
            private readonly string _typeName;
            internal bool HasDictionaryFormatVariants { get; }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal JsonDataContractCriticalHelper(DataContract traditionalDataContract)
            {
                _traditionalDataContract = traditionalDataContract;
                HasDictionaryFormatVariants = traditionalDataContract is CollectionDataContract
                    { Kind: CollectionKind.Dictionary or CollectionKind.GenericDictionary };
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
                return s_dataContractCache.GetValue(traditionalDataContract, static contract => CreateJsonDataContract(contract));
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private static JsonDataContract CreateJsonDataContract(DataContract traditionalDataContract)
            {
                Type type = traditionalDataContract.GetType();
                return type switch
                {
                    _ when type == typeof(ObjectDataContract) => new JsonObjectDataContract(traditionalDataContract),
                    _ when type == typeof(StringDataContract) => new JsonStringDataContract((StringDataContract)traditionalDataContract),
                    _ when type == typeof(UriDataContract) => new JsonUriDataContract((UriDataContract)traditionalDataContract),
                    _ when type == typeof(QNameDataContract) => new JsonQNameDataContract((QNameDataContract)traditionalDataContract),
                    _ when type == typeof(ByteArrayDataContract) => new JsonByteArrayDataContract((ByteArrayDataContract)traditionalDataContract),
                    _ when traditionalDataContract.IsPrimitive || traditionalDataContract.UnderlyingType == typeof(XmlQualifiedName) => new JsonDataContract(traditionalDataContract),
                    _ when type == typeof(ClassDataContract) => new JsonClassDataContract((ClassDataContract)traditionalDataContract),
                    _ when type == typeof(EnumDataContract) => new JsonEnumDataContract((EnumDataContract)traditionalDataContract),
                    _ when type == typeof(GenericParameterDataContract) || type == typeof(SpecialTypeDataContract) => new JsonDataContract(traditionalDataContract),
                    _ when type == typeof(CollectionDataContract) => new JsonCollectionDataContract((CollectionDataContract)traditionalDataContract),
                    _ when type == typeof(XmlDataContract) => new JsonXmlDataContract((XmlDataContract)traditionalDataContract),
                    _ => throw new ArgumentException(SR.Format(SR.JsonTypeNotSupportedByDataContractJsonSerializer, traditionalDataContract.UnderlyingType), nameof(traditionalDataContract))
                };
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
                                DataContract itemDataContract = DataContract.GetDataContract(typeof(System.Collections.Generic.KeyValuePair<,>).MakeGenericType(collectionDataContract.ItemType.GenericTypeArguments));
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
