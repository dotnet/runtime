// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;

namespace System.Runtime.Serialization
{
    internal sealed class ReflectionXmlFormatWriter
    {
        internal void ReflectionWriteClass(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract)
        {
            InvokeOnSerializing(obj, context, classContract);
            obj = ResolveAdapterType(obj, classContract);
            ReflectionWriteMembers(xmlWriter, obj, context, classContract, classContract, 0 /*childElementIndex*/);
            InvokeOnSerialized(obj, context, classContract);
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
            if (obj.GetType() == typeof(DateTimeOffset))
            {
                obj = DateTimeOffsetAdapter.GetDateTimeOffsetAdapter((DateTimeOffset)obj);
            }
            else if (obj.GetType().GetTypeInfo().IsGenericType && obj.GetType().GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                obj = classContract.KeyValuePairAdapterConstructorInfo.Invoke(new object[] { obj });
            }

            return obj;
        }

        internal int ReflectionWriteMembers(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract, ClassDataContract derivedMostClassContract, int childElementIndex)
        {
            int memberCount = (classContract.BaseContract == null) ? 0 :
                ReflectionWriteMembers(xmlWriter, obj, context, classContract.BaseContract, derivedMostClassContract, childElementIndex);

            childElementIndex += memberCount;

            Type classType = classContract.UnadaptedClassType;
            XmlDictionaryString[] memberNames = classContract.MemberNames;
            XmlDictionaryString ns = classContract.Namespace;
            context.IncrementItemCount(classContract.Members.Count);
            for (int i = 0; i < classContract.Members.Count; i++, memberCount++)
            {
                DataMember member = classContract.Members[i];
                Type memberType = member.MemberType;
                if (member.IsGetOnlyCollection)
                {
                    context.StoreIsGetOnlyCollection();
                }
                bool writeXsiType = CheckIfMemberHasConflict(member, classContract, derivedMostClassContract);
                MemberInfo memberInfo = member.MemberInfo;
                object memberValue = ReflectionGetMemberValue(obj, memberInfo);
                if (writeXsiType || !ReflectionTryWritePrimitive(xmlWriter, context, memberType, memberValue, member.MemberInfo, null /*arrayItemIndex*/, ns, memberNames[i + childElementIndex] /*nameLocal*/, i + childElementIndex))
                {
                    ReflectionWriteStartElement(xmlWriter, memberType, ns, ns.Value, member.Name, 0);
                    if (classContract.ChildElementNamespaces[i + childElementIndex] != null)
                    {
                        var nsChildElement = classContract.ChildElementNamespaces[i + childElementIndex];
                        xmlWriter.WriteNamespaceDecl(nsChildElement);
                    }
                    ReflectionWriteValue(xmlWriter, context, memberType, memberValue, writeXsiType);
                    ReflectionWriteEndElement(xmlWriter);
                }
            }

            return memberCount;
        }

        private object ReflectionGetMemberValue(object obj, MemberInfo memberInfo)
        {
            object memberValue = null;
            if (memberInfo is PropertyInfo)
            {
                PropertyInfo propInfo = (PropertyInfo)memberInfo;
                memberValue = propInfo.GetValue(obj);
            }
            else if (memberInfo is FieldInfo)
            {
                FieldInfo fieldInfo = (FieldInfo)memberInfo;
                memberValue = fieldInfo.GetValue(obj);
            }
            else
            {
                throw new NotImplementedException("Unknown member type");
            }
            return memberValue;
        }

        private bool ReflectionTryWritePrimitive(XmlWriterDelegator xmlWriter, XmlObjectSerializerWriteContext context, Type type, object value, MemberInfo memberInfo, int? arrayItemIndex, XmlDictionaryString ns, XmlDictionaryString name, int nameIndex)
        {
            PrimitiveDataContract primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(type);
            if (primitiveContract == null || primitiveContract.UnderlyingType == Globals.TypeOfObject)
                return false;

            primitiveContract.WriteXmlElement(xmlWriter, value, context, name, ns);

            return true;
        }

        private bool ReflectionTryWritePrimitive(XmlWriterDelegator xmlWriter, XmlObjectSerializerWriteContext context, PrimitiveDataContract primitiveContract, object value, MemberInfo memberInfo, int? arrayItemIndex, XmlDictionaryString ns, XmlDictionaryString name, int nameIndex)
        {
            if (primitiveContract == null || primitiveContract.UnderlyingType == Globals.TypeOfObject)
                return false;

            primitiveContract.WriteXmlElement(xmlWriter, value, context, name, ns);

            return true;
        }

        internal void ReflectionWriteCollection(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, CollectionDataContract collectionDataContract)
        {
            XmlDictionaryString ns = collectionDataContract.Namespace;
            XmlDictionaryString itemName = collectionDataContract.CollectionItemName;

            if (collectionDataContract.Kind == CollectionKind.Array)
            {
                context.IncrementArrayCount(xmlWriter, (Array)obj);
                Type itemType = collectionDataContract.ItemType;
                if (!ReflectionTryWritePrimitiveArray(xmlWriter, obj, collectionDataContract.UnderlyingType, itemType, itemName, ns))
                {
                    Array a = (Array)obj;
                    for (int i = 0; i < a.Length; ++i)
                    {
                        ReflectionWriteStartElement(xmlWriter, itemType, ns, ns.Value, itemName.Value, 0);
                        ReflectionWriteValue(xmlWriter, context, itemType, a.GetValue(i), false);
                        ReflectionWriteEndElement(xmlWriter);
                    }
                }
            }
            else
            {
                switch (collectionDataContract.Kind)
                {
                    case CollectionKind.Collection:
                    case CollectionKind.List:
                    case CollectionKind.Dictionary:
                        {
                            context.IncrementCollectionCount(xmlWriter, (ICollection)obj);
                        }
                        break;
                    case CollectionKind.GenericCollection:
                    case CollectionKind.GenericList:
                        {
                            MethodInfo incrementCollectionCountMethod = XmlFormatGeneratorStatics.IncrementCollectionCountGenericMethod.MakeGenericMethod(collectionDataContract.ItemType);
                            incrementCollectionCountMethod.Invoke(context, new object[] { xmlWriter, obj });
                        }
                        break;
                    case CollectionKind.GenericDictionary:
                        {
                            MethodInfo incrementCollectionCountMethod = XmlFormatGeneratorStatics.IncrementCollectionCountGenericMethod.MakeGenericMethod(Globals.TypeOfKeyValuePair.MakeGenericType(collectionDataContract.ItemType.GetGenericArguments()));
                            incrementCollectionCountMethod.Invoke(context, new object[] { xmlWriter, obj });
                        }
                        break;
                }

                IEnumerator enumerator = ((IEnumerable)obj).GetEnumerator();
                bool isDictionary = false, isGenericDictionary = false;
                Type enumeratorType = null;
                Type[] keyValueTypes = null;
                Type elementType = null;
                if (collectionDataContract.Kind == CollectionKind.GenericDictionary)
                {
                    isGenericDictionary = true;
                    keyValueTypes = collectionDataContract.ItemType.GetGenericArguments();
                    enumeratorType = Globals.TypeOfGenericDictionaryEnumerator.MakeGenericType(keyValueTypes);
                    Type ctorParam = Globals.TypeOfIEnumeratorGeneric.MakeGenericType(Globals.TypeOfKeyValuePair.MakeGenericType(keyValueTypes));
                    ConstructorInfo dictEnumCtor = enumeratorType.GetConstructor(Globals.ScanAllMembers, new Type[] { ctorParam });
                    IEnumerator genericDictEnumerator = (IEnumerator)dictEnumCtor.Invoke(new object[] { enumerator });
                    enumerator = genericDictEnumerator;
                }
                else if (collectionDataContract.Kind == CollectionKind.Dictionary)
                {
                    isDictionary = true;
                    keyValueTypes = new Type[] { Globals.TypeOfObject, Globals.TypeOfObject };
                    enumeratorType = Globals.TypeOfDictionaryEnumerator;
                    IEnumerator nonGenericDictEnumerator = (IEnumerator)new CollectionDataContract.DictionaryEnumerator(((IDictionary)obj).GetEnumerator());
                    enumerator = nonGenericDictEnumerator;
                }
                else if (collectionDataContract.Kind == CollectionKind.GenericCollection)
                {
                    elementType = collectionDataContract.ItemType;
                }
                else
                {
                    enumeratorType = collectionDataContract.GetEnumeratorMethod.ReturnType;
                }

                if (elementType == null)
                {
                    // For GenericCollection we get elementType from the collection's ItemType. For other kinds of
                    // collection , we use enumeratorType.ReturnType.
                    if (enumeratorType == null)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.TypeNotSerializableViaReflection, collectionDataContract.UnderlyingType)));
                    }

                    MethodInfo getCurrentMethod = enumeratorType.GetMethod(Globals.GetCurrentMethodName, BindingFlags.Instance | BindingFlags.Public, Array.Empty<Type>());
                    elementType = getCurrentMethod.ReturnType;
                }

                PrimitiveDataContract primitiveContractForType = PrimitiveDataContract.GetPrimitiveDataContract(elementType);

                while (enumerator.MoveNext())
                {
                    object current = enumerator.Current;
                    context.IncrementItemCount(1);
                    if (!ReflectionTryWritePrimitive(xmlWriter, context, primitiveContractForType, current, null, null, ns, itemName, 0))
                    {
                        ReflectionWriteStartElement(xmlWriter, elementType, ns, ns.Value, itemName.Value, 0);
                        if (isGenericDictionary || isDictionary)
                        {
                            collectionDataContract.ItemContract.WriteXmlValue(xmlWriter, current, context);
                        }
                        else
                        {
                            ReflectionWriteValue(xmlWriter, context, elementType, current, false);
                        }
                        ReflectionWriteEndElement(xmlWriter);
                    }
                }
            }
        }

        private void ReflectionWriteStartElement(XmlWriterDelegator xmlWriter, Type type, XmlDictionaryString ns, string namespaceLocal, string nameLocal, int nameIndex)
        {
            bool needsPrefix = NeedsPrefix(type, ns);

            if (needsPrefix)
            {
                xmlWriter.WriteStartElement(Globals.ElementPrefix, nameLocal, namespaceLocal);
            }
            else
            {
                xmlWriter.WriteStartElement(nameLocal, namespaceLocal);
            }
        }

        private void ReflectionWriteEndElement(XmlWriterDelegator xmlWriter)
        {
            xmlWriter.WriteEndElement();
        }

        private void ReflectionWriteValue(XmlWriterDelegator xmlWriter, XmlObjectSerializerWriteContext context, Type type, object value, bool writeXsiType)
        {
            Type memberType = type;
            object memberValue = value;
            bool isNullableOfT = (memberType.GetTypeInfo().IsGenericType &&
                                  memberType.GetGenericTypeDefinition() == Globals.TypeOfNullable);
            if (memberType.GetTypeInfo().IsValueType && !isNullableOfT)
            {
                PrimitiveDataContract primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(memberType);
                if (primitiveContract != null && !writeXsiType)
                {
                    primitiveContract.WriteXmlValue(xmlWriter, value, context);
                }
                else
                {
                    ReflectionInternalSerialize(xmlWriter, context, value, value.GetType().TypeHandle.Equals(memberType.TypeHandle), writeXsiType, memberType);
                }
            }
            else
            {
                if (isNullableOfT)
                {
                    if (memberValue == null)
                    {
                        Type[] genericArgumentTypes = memberType.GetGenericArguments();
                        if (genericArgumentTypes.Length != 1)
                        {
                            throw new InvalidOperationException(string.Format("Cannot serialize type: {0}", memberType.Name));
                        }

                        memberType = genericArgumentTypes[0];
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
                    PrimitiveDataContract primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(memberType);
                    if (primitiveContract != null && primitiveContract.UnderlyingType != Globals.TypeOfObject && !writeXsiType)
                    {
                        primitiveContract.WriteXmlValue(xmlWriter, memberValue, context);
                    }
                    else
                    {
                        if (memberType == Globals.TypeOfObject ||//boxed Nullable<T>
                            memberType == Globals.TypeOfValueType ||
                            ((IList)Globals.TypeOfNullable.GetInterfaces()).Contains(memberType))
                        {
                            if (value == null)
                            {
                                context.WriteNull(xmlWriter, memberType, DataContract.IsTypeSerializable(memberType));
                            }
                            else
                            {
                                ReflectionInternalSerialize(xmlWriter, context, value, value.GetType().TypeHandle.Equals(memberType.TypeHandle), writeXsiType, memberType);
                            }
                        }
                        else
                        {
                            ReflectionInternalSerialize(xmlWriter, context, value, value.GetType().TypeHandle.Equals(memberType.TypeHandle), writeXsiType, memberType);
                        }
                    }
                }
            }
        }

        private void ReflectionInternalSerialize(XmlWriterDelegator xmlWriter, XmlObjectSerializerWriteContext context, object obj, bool isDeclaredType, bool writeXsiType, Type memberType)
        {
            context.InternalSerializeReference(xmlWriter, obj, isDeclaredType, writeXsiType, DataContract.GetId(memberType.TypeHandle), memberType.TypeHandle);
        }

        private bool ReflectionTryWritePrimitiveArray(XmlWriterDelegator xmlWriter, object obj, Type type, Type itemType, XmlDictionaryString collectionItemName, XmlDictionaryString itemNamespace)
        {
            PrimitiveDataContract primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
            if (primitiveContract == null)
                return false;

            switch (itemType.GetTypeCode())
            {
                case TypeCode.Boolean:
                    xmlWriter.WriteBooleanArray((bool[])obj, collectionItemName, itemNamespace);
                    break;
                case TypeCode.DateTime:
                    xmlWriter.WriteDateTimeArray((DateTime[])obj, collectionItemName, itemNamespace);
                    break;
                case TypeCode.Decimal:
                    xmlWriter.WriteDecimalArray((decimal[])obj, collectionItemName, itemNamespace);
                    break;
                case TypeCode.Int32:
                    xmlWriter.WriteInt32Array((int[])obj, collectionItemName, itemNamespace);
                    break;
                case TypeCode.Int64:
                    xmlWriter.WriteInt64Array((long[])obj, collectionItemName, itemNamespace);
                    break;
                case TypeCode.Single:
                    xmlWriter.WriteSingleArray((float[])obj, collectionItemName, itemNamespace);
                    break;
                case TypeCode.Double:
                    xmlWriter.WriteDoubleArray((double[])obj, collectionItemName, itemNamespace);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private bool NeedsPrefix(Type type, XmlDictionaryString ns)
        {
            return type == Globals.TypeOfXmlQualifiedName && (ns != null && ns.Value != null && ns.Value.Length > 0);
        }

        private bool CheckIfMemberHasConflict(DataMember member, ClassDataContract classContract, ClassDataContract derivedMostClassContract)
        {
            // Check for conflict with base type members
            if (CheckIfConflictingMembersHaveDifferentTypes(member))
                return true;

            // Check for conflict with derived type members
            string name = member.Name;
            string ns = classContract.StableName.Namespace;
            ClassDataContract currentContract = derivedMostClassContract;
            while (currentContract != null && currentContract != classContract)
            {
                if (ns == currentContract.StableName.Namespace)
                {
                    List<DataMember> members = currentContract.Members;
                    for (int j = 0; j < members.Count; j++)
                    {
                        if (name == members[j].Name)
                            return CheckIfConflictingMembersHaveDifferentTypes(members[j]);
                    }
                }
                currentContract = currentContract.BaseContract;
            }

            return false;
        }

        private bool CheckIfConflictingMembersHaveDifferentTypes(DataMember member)
        {
            while (member.ConflictingMember != null)
            {
                if (member.MemberType != member.ConflictingMember.MemberType)
                    return true;
                member = member.ConflictingMember;
            }
            return false;
        }
    }

}
