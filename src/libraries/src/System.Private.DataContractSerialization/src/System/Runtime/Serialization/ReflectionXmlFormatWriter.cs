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
        private XmlWriterDelegator _arg0XmlWriter;
        private object _arg1Object;
        private XmlObjectSerializerWriteContext _arg2Context;
        private ClassDataContract _arg3ClassDataContract;
        private CollectionDataContract _arg3CollectionDataContract;

        internal void ReflectionWriteClass(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract)
        {
            InvokeOnSerializing(obj, context, classContract);
            ReflectionInitArgs(xmlWriter, obj, context, classContract);
            ReflectionWriteMembers(xmlWriter, _arg1Object, _arg2Context, _arg3ClassDataContract, _arg3ClassDataContract, 0 /*childElementIndex*/);
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

        private void ReflectionInitArgs(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract)
        {
            if (obj.GetType() == typeof(DateTimeOffset))
            {
                obj = DateTimeOffsetAdapter.GetDateTimeOffsetAdapter((DateTimeOffset)obj);
            }
            else if (obj.GetType().GetTypeInfo().IsGenericType && obj.GetType().GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                obj = classContract.KeyValuePairAdapterConstructorInfo.Invoke(new object[] { obj });
            }

            _arg0XmlWriter = xmlWriter;
            _arg1Object = obj;
            _arg2Context = context;
            _arg3ClassDataContract = classContract;
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
                    ReflectionWriteStartElement(memberType, ns, ns.Value, member.Name, 0);
                    if (classContract.ChildElementNamespaces[i + childElementIndex] != null)
                    {
                        var nsChildElement = classContract.ChildElementNamespaces[i + childElementIndex];
                        _arg0XmlWriter.WriteNamespaceDecl(nsChildElement);
                    }
                    ReflectionWriteValue(memberType, memberValue, writeXsiType);
                    ReflectionWriteEndElement();
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

        private void ReflectionWriteEndElement(XmlWriterDelegator xmlWriter)
        {
            xmlWriter.WriteEndElement();
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
            ReflectionInitArgs(xmlWriter, obj, context, null);
            _arg3CollectionDataContract = collectionDataContract;

            XmlDictionaryString ns = collectionDataContract.Namespace;
            XmlDictionaryString itemName = collectionDataContract.CollectionItemName;

            if (collectionDataContract.Kind == CollectionKind.Array)
            {
                _arg2Context.IncrementArrayCount(xmlWriter, (Array)_arg1Object);
                Type itemType = collectionDataContract.ItemType;
                if (!ReflectionTryWritePrimitiveArray(collectionDataContract.UnderlyingType, itemType, itemName, ns))
                {
                    Array a = (Array)obj;
                    for (int i = 0; i < a.Length; ++i)
                    {
                        ReflectionWriteStartElement(itemType, ns, ns.Value, itemName.Value, 0);
                        ReflectionWriteValue(itemType, a.GetValue(i), false);
                        ReflectionWriteEndElement();
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
                            _arg2Context.IncrementCollectionCount(xmlWriter, (ICollection)obj);
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
                    _arg2Context.IncrementItemCount(1);
                    if (!ReflectionTryWritePrimitive(_arg0XmlWriter, _arg2Context, primitiveContractForType, current, null, null, ns, itemName, 0))
                    {
                        ReflectionWriteStartElement(elementType, ns, ns.Value, itemName.Value, 0);
                        if (isGenericDictionary || isDictionary)
                        {
                            _arg3CollectionDataContract.ItemContract.WriteXmlValue(_arg0XmlWriter, current, _arg2Context);
                        }
                        else
                        {
                            ReflectionWriteValue(elementType, current, false);
                        }
                        ReflectionWriteEndElement();
                    }
                }
            }
        }

        private void ReflectionWriteStartElement(Type type, XmlDictionaryString ns, string namespaceLocal, string nameLocal, int nameIndex)
        {
            bool needsPrefix = NeedsPrefix(type, ns);

            if (needsPrefix)
            {
                _arg0XmlWriter.WriteStartElement(Globals.ElementPrefix, nameLocal, namespaceLocal);
            }
            else
            {
                _arg0XmlWriter.WriteStartElement(nameLocal, namespaceLocal);
            }
        }

        private void ReflectionWriteEndElement()
        {
            _arg0XmlWriter.WriteEndElement();
        }

        private void ReflectionWriteValue(Type type, object value, bool writeXsiType)
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
                    primitiveContract.WriteXmlValue(_arg0XmlWriter, value, _arg2Context);
                }
                else
                {
                    ReflectionInternalSerialize(value, value.GetType().TypeHandle.Equals(memberType.TypeHandle), writeXsiType, memberType);
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
                    _arg2Context.WriteNull(_arg0XmlWriter, memberType, DataContract.IsTypeSerializable(memberType));
                }
                else
                {
                    PrimitiveDataContract primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(memberType);
                    if (primitiveContract != null && primitiveContract.UnderlyingType != Globals.TypeOfObject && !writeXsiType)
                    {
                        primitiveContract.WriteXmlValue(_arg0XmlWriter, memberValue, _arg2Context);
                    }
                    else
                    {
                        if (memberType == Globals.TypeOfObject ||//boxed Nullable<T>
                            memberType == Globals.TypeOfValueType ||
                            ((IList)Globals.TypeOfNullable.GetInterfaces()).Contains(memberType))
                        {
                            if (value == null)
                            {
                                _arg2Context.WriteNull(_arg0XmlWriter, memberType, DataContract.IsTypeSerializable(memberType));
                            }
                            else
                            {
                                ReflectionInternalSerialize(value, value.GetType().TypeHandle.Equals(memberType.TypeHandle), writeXsiType, memberType);
                            }
                        }
                        else
                        {
                            ReflectionInternalSerialize(value, value.GetType().TypeHandle.Equals(memberType.TypeHandle), writeXsiType, memberType);
                        }
                    }
                }
            }
        }

        private void ReflectionInternalSerialize(object obj, bool isDeclaredType, bool writeXsiType, Type memberType)
        {
            _arg2Context.InternalSerializeReference(_arg0XmlWriter, obj, isDeclaredType, writeXsiType, DataContract.GetId(memberType.TypeHandle), memberType.TypeHandle);
        }

        private bool ReflectionTryWritePrimitiveArray(Type type, Type itemType, XmlDictionaryString collectionItemName, XmlDictionaryString itemNamespace)
        {
            PrimitiveDataContract primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
            if (primitiveContract == null)
                return false;

            switch (itemType.GetTypeCode())
            {
                case TypeCode.Boolean:
                    _arg0XmlWriter.WriteBooleanArray((bool[])_arg1Object, collectionItemName, itemNamespace);
                    break;
                case TypeCode.DateTime:
                    _arg0XmlWriter.WriteDateTimeArray((DateTime[])_arg1Object, collectionItemName, itemNamespace);
                    break;
                case TypeCode.Decimal:
                    _arg0XmlWriter.WriteDecimalArray((decimal[])_arg1Object, collectionItemName, itemNamespace);
                    break;
                case TypeCode.Int32:
                    _arg0XmlWriter.WriteInt32Array((int[])_arg1Object, collectionItemName, itemNamespace);
                    break;
                case TypeCode.Int64:
                    _arg0XmlWriter.WriteInt64Array((long[])_arg1Object, collectionItemName, itemNamespace);
                    break;
                case TypeCode.Single:
                    _arg0XmlWriter.WriteSingleArray((float[])_arg1Object, collectionItemName, itemNamespace);
                    break;
                case TypeCode.Double:
                    _arg0XmlWriter.WriteDoubleArray((double[])_arg1Object, collectionItemName, itemNamespace);
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
