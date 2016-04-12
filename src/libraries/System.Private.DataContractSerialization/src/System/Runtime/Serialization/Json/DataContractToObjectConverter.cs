// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Security;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal static class DataContractToObjectConverter
    {
        private static string GetTypeName(DataContract dataContract)
        {
            string ns = XmlObjectSerializerWriteContextComplexJson.TruncateDefaultDataContractNamespace(dataContract.Namespace.Value);
            return dataContract.Name.Value + ((ns.Length == 0) ? "" : (":" + ns));
        }

        public static IEnumerable ConvertGenericListToArray(DataContractJsonSerializer serializer, IEnumerable value, CollectionDataContract dataContract, XmlObjectSerializerWriteContextComplexJson context, bool writeServerType)
        {
            Type listArgumentType = dataContract.ItemType;
            if (listArgumentType.GetTypeInfo().IsGenericType)
            {
                listArgumentType = listArgumentType.GetGenericArguments()[0];
            }
            List<object> serializedList = new List<object>();
            MethodInfo getEnumeratorMethod = dataContract.GetEnumeratorMethod;

            IEnumerator enumerator = (getEnumeratorMethod == null) ? value.GetEnumerator() : (IEnumerator)getEnumeratorMethod.Invoke(value, Array.Empty<Type>());
            while (enumerator.MoveNext())
            {
                if (enumerator.Current == null || enumerator.Current.GetType().GetTypeInfo().IsPrimitive)
                {
                    serializedList.Add(enumerator.Current);
                }
                else
                {
                    Type currentItemType = enumerator.Current.GetType();
                    DataContract currentItemDataContract = DataContract.GetDataContract(currentItemType);
                    bool emitTypeInformation = EmitTypeInformation(dataContract.ItemContract, currentItemType);
                    if (writeServerType || emitTypeInformation)
                    {
                        context.CheckIfTypeNeedsVerifcation(dataContract.ItemContract, currentItemDataContract);
                    }
                    context.PushKnownTypes(dataContract);
                    serializedList.Add(serializer.ConvertDataContractToObject(enumerator.Current, currentItemDataContract, context, (writeServerType || emitTypeInformation), dataContract.ItemType.TypeHandle));
                    context.PopKnownTypes(dataContract);
                }
            }
            return serializedList;
        }

        public static List<object> ConvertGenericDictionaryToArray(DataContractJsonSerializer serializer, IEnumerable value, CollectionDataContract dataContract, XmlObjectSerializerWriteContextComplexJson context, bool writeServerType)
        {
            List<object> keyValuePair = new List<object>();
            Dictionary<string, object> currentEntry;
            Type[] declaredTypes = dataContract.ItemType.GetGenericArguments();
            MethodInfo getEnumeratorMethod = dataContract.GetEnumeratorMethod;

            IDictionaryEnumerator enumerator = (IDictionaryEnumerator)((getEnumeratorMethod == null) ? value.GetEnumerator() : (IDictionaryEnumerator)getEnumeratorMethod.Invoke(value, Array.Empty<Type>()));
            while (enumerator.MoveNext())
            {
                DictionaryEntry current = enumerator.Entry;
                DataContract currentDataContract = DataContract.GetDataContract(enumerator.Current.GetType());
                currentEntry = new Dictionary<string, object>();
                if (writeServerType)
                {
                    AddTypeInformation(currentEntry, currentDataContract);
                }
                context.PushKnownTypes(dataContract);
                AddDictionaryEntryData(serializer, currentEntry, writeServerType ? JsonGlobals.KeyString.ToLowerInvariant() : JsonGlobals.KeyString, declaredTypes[0], current.Key, context);
                AddDictionaryEntryData(serializer, currentEntry, writeServerType ? JsonGlobals.ValueString.ToLowerInvariant() : JsonGlobals.ValueString, declaredTypes[1], current.Value, context);
                keyValuePair.Add(currentEntry);
                context.PopKnownTypes(dataContract);
            }
            return keyValuePair;
        }

        private static void AddDictionaryEntryData(DataContractJsonSerializer serializer, Dictionary<string, object> currentEntry, string key, Type declaredType, object value, XmlObjectSerializerWriteContextComplexJson context)
        {
            if (value == null)
            {
                currentEntry[key] = value;
                return;
            }
            Type runtimeType = value.GetType();
            if (IsTypePrimitive(runtimeType))
            {
                currentEntry[key] = value;
            }
            else
            {
                if (declaredType.GetTypeInfo().IsGenericType && declaredType.GetGenericTypeDefinition() == Globals.TypeOfNullable)
                {
                    declaredType = Nullable.GetUnderlyingType(declaredType);
                }
                DataContract runtimeDataContract = DataContract.GetDataContract(runtimeType);
                if (declaredType != runtimeType)
                {
                    context.VerifyType(runtimeDataContract);
                }
                currentEntry[key] = serializer.ConvertDataContractToObject(value, runtimeDataContract, context, EmitTypeInformation(runtimeDataContract, runtimeType), runtimeDataContract.UnderlyingType.TypeHandle);
            }
        }

        public static Dictionary<string, object> ConvertClassDataContractToDictionary(DataContractJsonSerializer serializer, ClassDataContract dataContract, object value, XmlObjectSerializerWriteContextComplexJson context, bool writeServerType)
        {
            Dictionary<string, object> classToDictionary = new Dictionary<string, object>();

            if (writeServerType)
            {
                AddTypeInformation(classToDictionary, DataContract.GetDataContract(value.GetType()));
            }

            if (dataContract.UnderlyingType == Globals.TypeOfDateTimeOffsetAdapter)
            {
                DateTimeOffset dto = (DateTimeOffset)value;
                classToDictionary["DateTime"] = dto.UtcDateTime;
                classToDictionary["OffsetMinutes"] = (short)dto.Offset.TotalMinutes;
                return classToDictionary;
            }
            else if (dataContract.IsKeyValuePairAdapter)
            {
                //Convert KeyValuePair<K,T> to KeyValuePairAdapter<K,T>
                value = dataContract.KeyValuePairAdapterConstructorInfo.Invoke(new object[] { value });
            }

            DataContractJsonSerializer.InvokeOnSerializing(value, dataContract, context);
            WriteClassDataContractMembers(serializer, dataContract, ref classToDictionary, value, context);
            DataContractJsonSerializer.InvokeOnSerialized(value, dataContract, context);

            return classToDictionary;
        }

        private static void WriteClassDataContractMembers(DataContractJsonSerializer serializer, ClassDataContract dataContract, ref Dictionary<string, object> classToDictionary, object value, XmlObjectSerializerWriteContextComplexJson context)
        {
            if (dataContract.BaseContract != null)
            {
                WriteClassDataContractMembers(serializer, dataContract.BaseContract, ref classToDictionary, value, context);
            }

            for (int i = 0; i < dataContract.Members.Count; i++)
            {
                DataMember member = dataContract.Members[i];
                object memberValue = GetMemberValue(value, dataContract.Members[i].MemberInfo, dataContract.UnderlyingType);
                string memberName = System.Xml.XmlConvert.DecodeName(member.Name);

                if (classToDictionary.ContainsKey(memberName))
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SerializationException(SR.Format(SR.JsonDuplicateMemberNames,
    DataContract.GetClrTypeFullName(dataContract.UnderlyingType), memberName)));
                }

                if (!member.EmitDefaultValue)
                {
                    // Don't emit value if its null or default(valuetype)
                    if (memberValue == null || (member.MemberTypeContract.IsValueType && object.Equals(memberValue, Activator.CreateInstance(member.MemberType))))
                    {
                        continue;
                    }
                }
                if (memberValue == null || IsTypePrimitive(member.MemberType))
                {
                    classToDictionary[memberName] = memberValue;
                }
                else
                {
                    Type memberValueType = memberValue.GetType();
                    if (member.MemberType == memberValueType ||
                       //Special case Nullable<DateTimeOffset> and Nullable<Struct>
                       (member.IsNullable && !EmitTypeInformation(member.MemberTypeContract, memberValueType)))
                    {
                        classToDictionary[memberName] = serializer.ConvertDataContractToObject(memberValue, member.MemberTypeContract, context, false, member.MemberTypeContract.UnderlyingType.TypeHandle);
                    }
                    else
                    {
                        //Push KnownTypes of this DataContract
                        context.PushKnownTypes(dataContract);
                        DataContract memberValueContract = DataContract.GetDataContract(memberValue.GetType());
                        if (member.MemberType.GetTypeInfo().IsInterface)
                        {
                            XmlObjectSerializerWriteContextComplexJson.VerifyObjectCompatibilityWithInterface(memberValueContract, memberValue, member.MemberType);
                        }
                        context.CheckIfTypeNeedsVerifcation(member.MemberTypeContract, memberValueContract);
                        classToDictionary[memberName] = serializer.ConvertDataContractToObject(memberValue, memberValueContract, context, true, member.MemberTypeContract.UnderlyingType.TypeHandle);
                        context.PopKnownTypes(dataContract);
                    }
                }
            }
        }

        internal static object GetMemberValue(object value, MemberInfo memberInfo, Type typeToInvokeOn)
        {
            try
            {
                FieldInfo fieldInfo = memberInfo as FieldInfo;
                if (fieldInfo != null)
                {
                    return fieldInfo.GetValue(value);
                }
                else
                {
                    return ((PropertyInfo)memberInfo).GetValue(value);
                }
            }
            catch (Exception e)
            {
                if (e is MemberAccessException || e is ArgumentException)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new SecurityException(SR.Format(
                            SR.PartialTrustDataContractMemberGetNotPublic,
                            DataContract.GetClrTypeFullName(typeToInvokeOn),
                            memberInfo.Name),
                            e));
                }
                throw;
            }
        }

        private static bool IsTypePrimitive(Type t)
        {
            return (t.GetTypeInfo().IsPrimitive || t == typeof(String));
        }

        private static bool EmitTypeInformation(DataContract contract, Type runtimeType)
        {
            if ((contract is ClassDataContract && contract.UnderlyingType == Globals.TypeOfDateTimeOffsetAdapter) ||
                //For interface collectionDataContract EmitTypeInformation() is computed for each entry in the collection.
                (contract is CollectionDataContract && ((CollectionDataContract)contract).UnderlyingType.GetTypeInfo().IsInterface))
            {
                return false;
            }
            return contract.UnderlyingType != runtimeType || (contract.UnderlyingType == runtimeType && contract is CollectionDataContract);
        }

        private static void AddTypeInformation(IDictionary dictionary, DataContract contract)
        {
            dictionary[JsonGlobals.ServerTypeString] = GetTypeName(contract);
        }
    }
}
