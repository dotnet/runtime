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
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal static class ObjectToDataContractConverter
    {
        private static void CheckDuplicateNames(ClassDataContract dataContract)
        {
            if (dataContract.MemberNames != null)
            {
                Dictionary<string, object> memberTable = new Dictionary<string, object>();
                for (int i = 0; i < dataContract.MemberNames.Length; i++)
                {
                    if (memberTable.ContainsKey(dataContract.MemberNames[i].Value))
                    {
                        throw new SerializationException(SR.Format(SR.JsonDuplicateMemberInInput, dataContract.MemberNames[i].Value));
                    }
                    memberTable.Add(dataContract.MemberNames[i].Value, null);
                }
            }
        }

        public static object ConvertDictionaryToClassDataContract(DataContractJsonSerializer serializer, ClassDataContract dataContract, Dictionary<string, object> deserialzedValue, XmlObjectSerializerReadContextComplexJson context)
        {
            if (deserialzedValue == null)
            {
                return null;
            }

            if (dataContract.UnderlyingType == Globals.TypeOfDateTimeOffsetAdapter)
            {
                var tuple = deserialzedValue["DateTime"] as Tuple<DateTime, string>;
                DateTimeOffset dto = new DateTimeOffset(tuple != null ? tuple.Item1 : (DateTime)deserialzedValue["DateTime"]);
                return dto.ToOffset(new TimeSpan(0, (int)deserialzedValue["OffsetMinutes"], 0));
            }

            object serverTypeStringValue;
            if (deserialzedValue.TryGetValue(JsonGlobals.ServerTypeString, out serverTypeStringValue))
            {
                dataContract = ResolveDataContractFromTypeInformation(serverTypeStringValue.ToString(), dataContract, context);
            }

            object o = CreateInstance(dataContract);

            CheckDuplicateNames(dataContract);
            DataContractJsonSerializer.InvokeOnDeserializing(o, dataContract, context);
            ReadClassDataContractMembers(serializer, dataContract, deserialzedValue, o, context);
            DataContractJsonSerializer.InvokeOnDeserialized(o, dataContract, context);
            if (dataContract.IsKeyValuePairAdapter)
            {
                return dataContract.GetKeyValuePairMethodInfo.Invoke(o, Array.Empty<Type>());
            }
            return o;
        }

        private static void ReadClassDataContractMembers(DataContractJsonSerializer serializer, ClassDataContract dataContract, Dictionary<string, object> deserialzedValue, object newInstance, XmlObjectSerializerReadContextComplexJson context)
        {
            if (dataContract.BaseContract != null)
            {
                ReadClassDataContractMembers(serializer, dataContract.BaseContract, deserialzedValue, newInstance, context);
            }
            for (int i = 0; i < dataContract.Members.Count; i++)
            {
                DataMember member = dataContract.Members[i];
                object currentMemberValue;
                if (deserialzedValue.TryGetValue(XmlConvert.DecodeName(dataContract.Members[i].Name), out currentMemberValue) ||
                    dataContract.IsKeyValuePairAdapter && deserialzedValue.TryGetValue(XmlConvert.DecodeName(dataContract.Members[i].Name.ToLowerInvariant()), out currentMemberValue))
                {
                    if (member.MemberType.GetTypeInfo().IsPrimitive || currentMemberValue == null)
                    {
                        SetMemberValue(newInstance, serializer.ConvertObjectToDataContract(member.MemberTypeContract, currentMemberValue, context), dataContract.Members[i].MemberInfo, dataContract.UnderlyingType);
                    }
                    else
                    {
                        context.PushKnownTypes(dataContract);
                        object subMemberValue = serializer.ConvertObjectToDataContract(member.MemberTypeContract, currentMemberValue, context);
                        Type declaredType = (member.MemberType.GetTypeInfo().IsGenericType && member.MemberType.GetGenericTypeDefinition() == Globals.TypeOfNullable)
                            ? Nullable.GetUnderlyingType(member.MemberType)
                            : member.MemberType;

                        if (!(declaredType == Globals.TypeOfObject && subMemberValue.GetType() == Globals.TypeOfObjectArray) && declaredType != subMemberValue.GetType())
                        {
                            DataContract memberValueContract = DataContract.GetDataContract(subMemberValue.GetType());
                            context.CheckIfTypeNeedsVerifcation(member.MemberTypeContract, memberValueContract);
                        }

                        if (member.IsGetOnlyCollection)
                        {
                            PopulateReadOnlyCollection(newInstance, member, (IEnumerable)subMemberValue);
                        }
                        else
                        {
                            SetMemberValue(newInstance, subMemberValue, dataContract.Members[i].MemberInfo, dataContract.UnderlyingType);
                        }
                        context.PopKnownTypes(dataContract);
                    }
                }
                else if (member.IsRequired)
                {
                    XmlObjectSerializerWriteContext.ThrowRequiredMemberMustBeEmitted(dataContract.MemberNames[i].Value, dataContract.UnderlyingType);
                }
            }
        }

        private static void PopulateReadOnlyCollection(object instance, DataMember member, IEnumerable value)
        {
            Debug.Assert(member.IsGetOnlyCollection);

            CollectionDataContract memberContract = (CollectionDataContract)member.MemberTypeContract;
            var collection = DataContractToObjectConverter.GetMemberValue(instance, member.MemberInfo, instance.GetType());

            // Special case an array
            var array = collection as Array;
            if (array != null)
            {
                Array srcArray = (Array)value;
                Type elementType = srcArray.GetType().GetElementType();

                // Resize
                var resizeMethod = typeof(Array).GetMethod("Resize", BindingFlags.Static | BindingFlags.Public);
                var properResizeMethod = resizeMethod.MakeGenericMethod(elementType);
                properResizeMethod.Invoke(null, new object[] { array, srcArray.Length });

                // Copy
                Array.Copy(srcArray, 0, array, 0, srcArray.Length);
                return;
            }

            // General collection
            IEnumerator enumerator = value.GetEnumerator();
            object currentItem = null;
            object[] currentItemArray = null;

            while (enumerator.MoveNext())
            {
                currentItem = enumerator.Current;
                currentItemArray = new object[] { currentItem };
                // Dictionary
                if (memberContract.IsDictionary)
                {
                    Type currentItemType = currentItem.GetType();
                    MemberInfo keyMember = currentItemType.GetMember("Key")[0];
                    MemberInfo valueMember = currentItemType.GetMember("Value")[0];
                    currentItemArray = new object[] { DataContractToObjectConverter.GetMemberValue(currentItem, keyMember, currentItemType), DataContractToObjectConverter.GetMemberValue(currentItem, valueMember, currentItemType) };
                }

                memberContract.AddMethod.Invoke(collection, currentItemArray);
            }
        }

        //Deserialize '[...]' json string. The contents of the list can also be a dictionary i.e. [{...}]. The content type is detected
        //based on the type of CollectionDataContract.ItemContract.
        public static object ConvertICollectionToCollectionDataContract(DataContractJsonSerializer serializer, CollectionDataContract contract, object deserializedValue, XmlObjectSerializerReadContextComplexJson context)
        {
            Dictionary<string, object> valueAsDictionary = deserializedValue as Dictionary<string, object>;
            //Check to see if the dictionary (if it is a dictionary)is a regular Dictionary i.e { Key="key"; Value="value} and doesnt contain the __type string
            //for ex. the dictionary { __type="XXX"; Key="key"; Value="value} needs to be parsed as ClassDataContract
            if (valueAsDictionary != null && (!valueAsDictionary.ContainsKey(JsonGlobals.KeyString) || valueAsDictionary.ContainsKey(JsonGlobals.ServerTypeString)))
            {
                //If not then its a dictionary for either of these cases
                //1. Empty object - {}
                //2. Containes the __type information
                //3. Is a DateTimeOffsetDictionary
                return ConvertDictionary(serializer, contract, valueAsDictionary, context);
            }

            object returnValue = (contract.Constructor != null) ? contract.Constructor.Invoke(Array.Empty<Type>()) : null;

            bool isCollectionDataContractDictionary = contract.IsDictionary;
            MethodInfo addMethod = contract.AddMethod;
            bool convertToArray = contract.Kind == CollectionKind.Array;

            if (contract.UnderlyingType.GetTypeInfo().IsInterface || returnValue == null)
            {
                switch (contract.Kind)
                {
                    case CollectionKind.Collection:
                    case CollectionKind.GenericCollection:
                    case CollectionKind.Enumerable:
                    case CollectionKind.GenericEnumerable:
                    case CollectionKind.List:
                    case CollectionKind.GenericList:
                    case CollectionKind.Array:
                        if (contract.UnderlyingType.GetTypeInfo().IsValueType)
                        {
                            //Initialize struct
                            returnValue = XmlFormatReaderGenerator.TryGetUninitializedObjectWithFormatterServices(contract.UnderlyingType);
                        }
                        else
                        {
                            returnValue = Activator.CreateInstance(Globals.TypeOfListGeneric.MakeGenericType(contract.ItemType));
                            convertToArray = true;
                        }
                        break;
                    case CollectionKind.GenericDictionary:
                        returnValue = Activator.CreateInstance(Globals.TypeOfDictionaryGeneric.MakeGenericType(contract.ItemType.GetGenericArguments()));
                        break;
                    case CollectionKind.Dictionary:
                        returnValue = Activator.CreateInstance(Globals.TypeOfDictionaryGeneric.MakeGenericType(Globals.TypeOfObject, Globals.TypeOfObject));
                        break;
                }
            }
            if (addMethod == null)
            {
                //addMethod is null for IDictionary, IList and array types.
                Type[] paramArray = (contract.ItemType.GetTypeInfo().IsGenericType && !convertToArray) ? contract.ItemType.GetGenericArguments() : new Type[] { contract.ItemType };
                addMethod = returnValue.GetType().GetMethod(Globals.AddMethodName, paramArray);
            }

            IEnumerator enumerator = ((ICollection)deserializedValue).GetEnumerator();
            object currentItem = null;
            object[] currentItemArray = null;
            while (enumerator.MoveNext())
            {
                DataContract itemContract = contract.ItemContract;
                if (itemContract is ClassDataContract)
                {
                    itemContract = XmlObjectSerializerWriteContextComplexJson.GetRevisedItemContract(itemContract);
                }
                currentItem = serializer.ConvertObjectToDataContract(itemContract, enumerator.Current, context);
                currentItemArray = new object[] { currentItem };
                if (isCollectionDataContractDictionary)
                {
                    Type currentItemType = currentItem.GetType();
                    MemberInfo keyMember = currentItemType.GetMember("Key")[0];
                    MemberInfo valueMember = currentItemType.GetMember("Value")[0];
                    currentItemArray = new object[] { DataContractToObjectConverter.GetMemberValue(currentItem, keyMember, currentItemType), DataContractToObjectConverter.GetMemberValue(currentItem, valueMember, currentItemType) };
                }
                addMethod.Invoke(returnValue, currentItemArray);
            }

            return (convertToArray) ? ConvertToArray(contract.ItemType, (ICollection)returnValue) : returnValue;
        }

        public static object ConvertToArray(Type type, ICollection newList)
        {
            if (newList.GetType().IsArray)
            {
                //Optimization if its already an array
                return newList;
            }
            Array array = Array.CreateInstance(type, newList.Count);
            //Special case byte[] as Int32 cant be implicitly converted to byte.
            if (type == typeof(Byte))
            {
                int index = 0;
                foreach (object o in newList)
                    array.SetValue(Convert.ChangeType(o, type, null), index++);
            }
            else
            {
                newList.CopyTo(array, 0);
            }
            return array;
        }

        private static object ConvertDictionary(DataContractJsonSerializer serializer, DataContract contract, object obj, XmlObjectSerializerReadContextComplexJson context)
        {
            System.Diagnostics.Debug.Assert(obj is IDictionary, "obj is IDictionary");
            Dictionary<string, object> dictOfStringObject = obj as Dictionary<string, object>;
            object serverTypeStringValue;
            if (dictOfStringObject.TryGetValue(JsonGlobals.ServerTypeString, out serverTypeStringValue))
            {
                return ConvertDictionaryToClassDataContract(serializer,
                    ResolveDataContractFromTypeInformation(serverTypeStringValue.ToString(), null, context),
                    dictOfStringObject, context);
            }
            else if (dictOfStringObject.ContainsKey("DateTime") && dictOfStringObject.ContainsKey("OffsetMinutes"))
            {
                return ConvertDictionaryToClassDataContract(serializer, (ClassDataContract)DataContract.GetDataContract(typeof(DateTimeOffset)), dictOfStringObject, context);
            }
            else
            {
                //Its either an empty object "{}" or a weakly typed Json Object such as {"a",1;"b";2} which we don't support reading in Orcas
                return new Object();
            }
        }

        private static ClassDataContract ResolveDataContractFromTypeInformation(string typeName, DataContract contract, XmlObjectSerializerReadContextComplexJson context)
        {
            DataContract dataContract = context.ResolveDataContractFromType(typeName, Globals.DataContractXsdBaseNamespace, contract);
            if (dataContract == null)
            {
                XmlQualifiedName qname = XmlObjectSerializerReadContextComplexJson.ParseQualifiedName(typeName);
                throw System.ServiceModel.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.DcTypeNotFoundOnDeserialize, qname.Namespace, qname.Name)));
            }
            return (ClassDataContract)dataContract;
        }

        private static void SetMemberValue(object newInstance, object value, MemberInfo memberInfo, Type typeToInvokeOn)
        {
            try
            {
                FieldInfo fieldInfo = memberInfo as FieldInfo;
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(newInstance, value);
                }
                else
                {
                    ((PropertyInfo)memberInfo).SetValue(newInstance, value);
                }
            }
            catch (Exception e)
            {
                if (e is MemberAccessException || e is ArgumentException)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new SecurityException(SR.Format(
                                SR.PartialTrustDataContractMemberSetNotPublic,
                                DataContract.GetClrTypeFullName(typeToInvokeOn),
                                    memberInfo.Name),
                            e));
                }
                throw;
            }
        }

        private static object CreateInstance(ClassDataContract dataContract)
        {
            if (dataContract.IsNonAttributedType && !Globals.TypeOfScriptObject_IsAssignableFrom(dataContract.UnderlyingType))
            {
                try
                {
                    return Activator.CreateInstance(dataContract.UnderlyingType);
                }
                catch (MemberAccessException e)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new SecurityException(SR.Format(
                                SR.PartialTrustNonAttributedSerializableTypeNoPublicConstructor,
                                DataContract.GetClrTypeFullName(dataContract.UnderlyingType)),
                            e));
                }
            }

            if (dataContract.UnderlyingType == Globals.TypeOfDBNull)
            {
                return Globals.ValueOfDBNull;
            }

            return XmlFormatReaderGenerator.UnsafeGetUninitializedObject(DataContract.GetIdForInitialization(dataContract));
        }
    }
}
