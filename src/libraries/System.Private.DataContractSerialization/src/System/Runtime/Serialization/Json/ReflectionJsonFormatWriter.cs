// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal class ReflectionJsonFormatWriter
    {
        public void ReflectionWriteClass(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, ClassDataContract classContract, XmlDictionaryString[] memberNames)
        {
            InvokeOnSerializing(obj, context, classContract);
            obj = ResolveAdapterType(obj, classContract);
            ReflectionWriteMembers(xmlWriter, obj, context, classContract, classContract, 0 /*childElementIndex*/, memberNames);
            InvokeOnSerialized(obj, context, classContract);
        }

        private int ReflectionWriteMembers(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, ClassDataContract classContract, ClassDataContract derivedMostClassContract, int childElementIndex, XmlDictionaryString[] memberNames)
        {
            int memberCount = (classContract.BaseContract == null) ? 0 :
                ReflectionWriteMembers(xmlWriter, obj, context, classContract.BaseContract, derivedMostClassContract, childElementIndex, memberNames);

            childElementIndex += memberCount;

            Type classType = classContract.UnadaptedClassType;
            context.IncrementItemCount(classContract.Members.Count);
            for (int i = 0; i < classContract.Members.Count; i++, memberCount++)
            {
                DataMember member = classContract.Members[i];
                Type memberType = member.MemberType;
                if (member.IsGetOnlyCollection)
                {
                    context.StoreIsGetOnlyCollection();
                }

                if (!member.EmitDefaultValue)
                {
                    // We need to add tests for this case.
                    throw new NotImplementedException("EmitDefaultValue = false");
                }

                object memberValue = ReflectionGetMemberValue(obj, member);

                bool requiresNameAttribute = DataContractJsonSerializerImpl.CheckIfXmlNameRequiresMapping(classContract.MemberNames[i]);
                PrimitiveDataContract primitiveContract = member.MemberPrimitiveContract;
                if (requiresNameAttribute || !ReflectionTryWritePrimitive(xmlWriter, context, memberType, memberValue, memberNames[i + childElementIndex] /*name*/, primitiveContract))
                {
                    // Note: DataContractSerializer has member-conflict logic here to deal with the schema export
                    //       requirement that the same member can't be of two different types.
                    if (requiresNameAttribute)
                    {
                        XmlObjectSerializerWriteContextComplexJson.WriteJsonNameWithMapping(xmlWriter, memberNames, i + childElementIndex);
                    }
                    else
                    {
                        ReflectionWriteStartElement(xmlWriter, memberNames[i + childElementIndex]);
                    }

                    ReflectionWriteValue(xmlWriter, context, memberType, memberValue, false/*writeXsiType*/, primitiveContractForParamType: null);
                    ReflectionWriteEndElement(xmlWriter);
                }

            }

            return memberCount;
        }

        private void ReflectionWriteEndElement(XmlWriterDelegator xmlWriter)
        {
            xmlWriter.WriteEndElement();
        }

        // This method is identical to ReflectionXmlFormatWriter.ReflectionWriteValue
        private void ReflectionWriteValue(XmlWriterDelegator xmlWriter, XmlObjectSerializerWriteContext context, Type type, object value, bool writeXsiType, PrimitiveDataContract primitiveContractForParamType)
        {
            Type memberType = type;
            object memberValue = value;
            TypeInfo memberTypeInfo = memberType.GetTypeInfo();
            bool originValueIsNullableOfT = (memberTypeInfo.IsGenericType && memberType.GetGenericTypeDefinition() == Globals.TypeOfNullable);
            if (memberTypeInfo.IsValueType && !originValueIsNullableOfT)
            {
                PrimitiveDataContract primitiveContract = primitiveContractForParamType;
                if (primitiveContract != null && !writeXsiType)
                {
                    primitiveContract.WriteXmlValue(xmlWriter, memberValue, context);
                }
                else
                {
                    ReflectionInternalSerialize(xmlWriter, context, memberValue, memberValue.GetType().TypeHandle.Equals(memberType.TypeHandle), writeXsiType, memberType);
                }
            }
            else
            {
                if (originValueIsNullableOfT)
                {
                    if (memberValue == null)
                    {
                        memberType = Nullable.GetUnderlyingType(memberType);
                    }
                    else
                    {
                        MethodInfo getValue = memberType.GetMethod("get_Value", Array.Empty<Type>());
                        memberValue = getValue.Invoke(memberValue, Array.Empty<object>());
                        memberType = memberValue.GetType();
                    }
                }

                if (memberValue == null)
                {
                    context.WriteNull(xmlWriter, memberType, DataContract.IsTypeSerializable(memberType));
                }
                else
                {
                    PrimitiveDataContract primitiveContract = originValueIsNullableOfT ? PrimitiveDataContract.GetPrimitiveDataContract(memberType) : primitiveContractForParamType;
                    if (primitiveContract != null && primitiveContract.UnderlyingType != Globals.TypeOfObject && !writeXsiType)
                    {
                        primitiveContract.WriteXmlValue(xmlWriter, memberValue, context);
                    }
                    else
                    {
                        if (memberValue == null &&
                            (memberType == Globals.TypeOfObject
                            || (originValueIsNullableOfT && memberType.GetTypeInfo().IsValueType)))
                        {
                            context.WriteNull(xmlWriter, memberType, DataContract.IsTypeSerializable(memberType));
                        }
                        else
                        {
                            ReflectionInternalSerialize(xmlWriter, context, memberValue, memberValue.GetType().TypeHandle.Equals(memberType.TypeHandle), writeXsiType, memberType, originValueIsNullableOfT);
                        }
                    }
                }
            }
        }

        private void ReflectionInternalSerialize(XmlWriterDelegator xmlWriter, XmlObjectSerializerWriteContext context, object obj, bool isDeclaredType, bool writeXsiType, Type memberType, bool isNullableOfT = false)
        {
            if (isNullableOfT)
            {
                context.InternalSerialize(xmlWriter, obj, isDeclaredType, writeXsiType, DataContract.GetId(memberType.TypeHandle), memberType.TypeHandle);
            }
            else
            {
                context.InternalSerializeReference(xmlWriter, obj, isDeclaredType, writeXsiType, DataContract.GetId(memberType.TypeHandle), memberType.TypeHandle);
            }
        }

        private void ReflectionWriteStartElement(XmlWriterDelegator xmlWriter, XmlDictionaryString name)
        {
            xmlWriter.WriteStartElement(name, null);
        }

        private void ReflectionWriteStartElement(XmlWriterDelegator xmlWriter, string name)
        {
            xmlWriter.WriteStartElement(name, null);
        }

        private bool ReflectionTryWritePrimitive(XmlWriterDelegator xmlWriter, XmlObjectSerializerWriteContext context, Type type, object value, XmlDictionaryString name, PrimitiveDataContract primitiveContract)
        {
            if (primitiveContract == null || primitiveContract.UnderlyingType == Globals.TypeOfObject)
                return false;

            primitiveContract.WriteXmlElement(xmlWriter, value, context, name, null /*namespace*/);

            return true;
        }

        private object ReflectionGetMemberValue(object obj, DataMember dataMember)
        {
            return dataMember.Getter(obj);
        }

        private void InvokeOnSerializing(object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract)
        {
            if (classContract.BaseContract != null)
                InvokeOnSerializing(obj, context, classContract.BaseContract);
            if (classContract.OnSerializing != null)
            {
                var contextArg = context.GetStreamingContext() ;
                classContract.OnSerializing.Invoke(obj, new object[] { contextArg });
            }
        }

        private void InvokeOnSerialized(object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract)
        {
            if (classContract.BaseContract != null)
                InvokeOnSerialized(obj, context, classContract.BaseContract);
            if (classContract.OnSerialized != null)
            {
                var contextArg = context.GetStreamingContext() ;
                classContract.OnSerialized.Invoke(obj, new object[] { contextArg });
            }
        }

        private object ResolveAdapterType(object obj, ClassDataContract classContract)
        {
            Type type = obj.GetType();
            if (type == Globals.TypeOfDateTimeOffset)
            {
                obj = DateTimeOffsetAdapter.GetDateTimeOffsetAdapter((DateTimeOffset)obj);
            }
            else if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfKeyValuePair)
            {
                obj = classContract.KeyValuePairAdapterConstructorInfo.Invoke(new object[] { obj });
            }

            return obj;
        }

        public void ReflectionWriteCollection(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, CollectionDataContract collectionContract)
        {
            JsonWriterDelegator jsonWriter = xmlWriter as JsonWriterDelegator;
            if (jsonWriter == null)
            {
                throw new ArgumentException(nameof(xmlWriter));
            }

            XmlDictionaryString itemName = context.CollectionItemName;

            if (collectionContract.Kind == CollectionKind.Array)
            {
                context.IncrementArrayCount(jsonWriter, (Array)obj);
                Type itemType = collectionContract.ItemType;
                if (!ReflectionTryWritePrimitiveArray(jsonWriter, obj, collectionContract.UnderlyingType, itemType, itemName))
                {
                    ReflectionWriteArrayAttribute(jsonWriter);

                    Array array = (Array)obj;
                    PrimitiveDataContract primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
                    for (int i = 0; i < array.Length; ++i)
                    {
                        ReflectionWriteStartElement(jsonWriter, itemName);
                        ReflectionWriteValue(jsonWriter, context, itemType, array.GetValue(i), false, primitiveContract);
                        ReflectionWriteEndElement(jsonWriter);
                    }
                }
            }
            else
            {
                collectionContract.IncrementCollectionCount(jsonWriter, obj, context);

                Type enumeratorReturnType;
                IEnumerator enumerator = collectionContract.GetEnumeratorForCollection(obj, out enumeratorReturnType);

                bool canWriteSimpleDictionary = collectionContract.Kind == CollectionKind.GenericDictionary
                                             || collectionContract.Kind == CollectionKind.Dictionary;

                bool useSimpleDictionaryFormat = context.UseSimpleDictionaryFormat;

                if (canWriteSimpleDictionary && useSimpleDictionaryFormat)
                {
                    ReflectionWriteObjectAttribute(jsonWriter);

                    while (enumerator.MoveNext())
                    {
                        object current = enumerator.Current;
                        object key = ((IKeyValue)current).Key;
                        object value = ((IKeyValue)current).Value;
                        ReflectionWriteStartElement(jsonWriter, key.ToString());
                        ReflectionWriteValue(jsonWriter, context, value.GetType(), value, false, primitiveContractForParamType: null);
                        ReflectionWriteEndElement(jsonWriter);
                    }
                }
                else
                {
                    ReflectionWriteArrayAttribute(jsonWriter);

                    PrimitiveDataContract primitiveContractForType = PrimitiveDataContract.GetPrimitiveDataContract(enumeratorReturnType);
                    if (primitiveContractForType != null && primitiveContractForType.UnderlyingType != Globals.TypeOfObject)
                    {
                        while (enumerator.MoveNext())
                        {
                            object current = enumerator.Current;
                            context.IncrementItemCount(1);
                            primitiveContractForType.WriteXmlElement(jsonWriter, current, context, itemName, null /*namespace*/);
                        }
                    }
                    else
                    {
                        bool isDictionary = collectionContract.Kind == CollectionKind.Dictionary || collectionContract.Kind == CollectionKind.GenericDictionary;
                        while (enumerator.MoveNext())
                        {
                            object current = enumerator.Current;
                            context.IncrementItemCount(1);
                            ReflectionWriteStartElement(jsonWriter, itemName);
                            if (isDictionary)
                            {
                                var itemContract = XmlObjectSerializerWriteContextComplexJson.GetRevisedItemContract(collectionContract.ItemContract);
                                var jsonDataContract = JsonDataContract.GetJsonDataContract(itemContract);

                                jsonDataContract.WriteJsonValue(jsonWriter, current, context, collectionContract.ItemType.TypeHandle);
                            }
                            else
                            {
                                ReflectionWriteValue(jsonWriter, context, enumeratorReturnType, current, false, primitiveContractForParamType: null);
                            }

                            ReflectionWriteEndElement(jsonWriter);
                        }
                    }
                }
            }
        }

        private void ReflectionWriteObjectAttribute(XmlWriterDelegator xmlWriter)
        {
            xmlWriter.WriteAttributeString(
                null /* prefix */,
                JsonGlobals.typeString /* local name */,
                null /* namespace */,
                JsonGlobals.objectString /* value */);
        }

        private bool ReflectionTryWritePrimitiveArray(JsonWriterDelegator jsonWriter, object obj, Type underlyingType, Type itemType, XmlDictionaryString collectionItemName)
        {
            PrimitiveDataContract primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
            if (primitiveContract == null)
                return false;

            XmlDictionaryString itemNamespace = null;

            switch (itemType.GetTypeCode())
            {
                case TypeCode.Boolean:
                    ReflectionWriteArrayAttribute(jsonWriter);
                    jsonWriter.WriteJsonBooleanArray((bool[])obj, collectionItemName, itemNamespace);
                    break;
                case TypeCode.DateTime:
                    ReflectionWriteArrayAttribute(jsonWriter);
                    jsonWriter.WriteJsonDateTimeArray((DateTime[])obj, collectionItemName, itemNamespace);
                    break;
                case TypeCode.Decimal:
                    ReflectionWriteArrayAttribute(jsonWriter);
                    jsonWriter.WriteJsonDecimalArray((decimal[])obj, collectionItemName, itemNamespace);
                    break;
                case TypeCode.Int32:
                    ReflectionWriteArrayAttribute(jsonWriter);
                    jsonWriter.WriteJsonInt32Array((int[])obj, collectionItemName, itemNamespace);
                    break;
                case TypeCode.Int64:
                    ReflectionWriteArrayAttribute(jsonWriter);
                    jsonWriter.WriteJsonInt64Array((long[])obj, collectionItemName, itemNamespace);
                    break;
                case TypeCode.Single:
                    ReflectionWriteArrayAttribute(jsonWriter);
                    jsonWriter.WriteJsonSingleArray((float[])obj, collectionItemName, itemNamespace);
                    break;
                case TypeCode.Double:
                    ReflectionWriteArrayAttribute(jsonWriter);
                    jsonWriter.WriteJsonDoubleArray((double[])obj, collectionItemName, itemNamespace);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private void ReflectionWriteArrayAttribute(XmlWriterDelegator xmlWriter)
        {
            xmlWriter.WriteAttributeString(
                null /* prefix */,
                JsonGlobals.typeString /* local name */,
                string.Empty /* namespace */,
                JsonGlobals.arrayString /* value */);
        }
    }
}
