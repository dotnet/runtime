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
        private int _childElementIndex = 0;

        private XmlWriterDelegator _arg0XmlWriter;
        private object _arg1Object;
        private XmlObjectSerializerWriteContext _arg2Context;
        private ClassDataContract _arg3ClassDataContract;
        private CollectionDataContract _arg3CollectionDataContract;

        internal void ReflectionWriteClass(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract)
        {
            ReflectionInitArgs(xmlWriter, obj, context, classContract);
            ReflectionWriteMembers(xmlWriter, obj, context, classContract, classContract);
        }

        private void ReflectionInitArgs(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract)
        {
            _arg0XmlWriter = xmlWriter;
            _arg1Object = obj;
            _arg2Context = context;
            _arg3ClassDataContract = classContract;
        }

        internal int ReflectionWriteMembers(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract, ClassDataContract derivedMostClassContract)
        {
            int memberCount = (classContract.BaseContract == null) ? 0 :
                ReflectionWriteMembers(xmlWriter, obj, context, classContract.BaseContract, derivedMostClassContract);

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
                MemberInfo[] memberInfos = classType.GetMember(member.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                MemberInfo memberInfo = memberInfos[0];
                object memberValue = ReflectionGetMemberValue(obj, memberInfo);
                if (writeXsiType || !ReflectionTryWritePrimitive(xmlWriter, context, memberType, memberValue, member.MemberInfo, null /*arrayItemIndex*/, ns, memberNames[i] /*nameLocal*/, i + _childElementIndex))
                {
                    ReflectionWriteStartElement(memberType, ns, ns.Value, member.Name, 0);
                    if (classContract.ChildElementNamespaces[i + _childElementIndex] != null)
                    {
                        var nsChildElement = classContract.ChildElementNamespaces[i + _childElementIndex];
                        _arg0XmlWriter.WriteNamespaceDecl(nsChildElement);
                    }
                    if (memberValue == null)
                    {
                        throw new NotImplementedException("memberValue == null");
                    }
                    ReflectionWriteValue(memberType, memberValue, writeXsiType);
                    ReflectionWriteEndElement();
                }
            }

            return 0;
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
                Type itemType = collectionDataContract.ItemType;

                bool isDictionary = false, isGenericDictionary = false;
                Type enumeratorType = null;
                Type[] keyValueTypes = null;
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

                while (enumerator.MoveNext())
                {
                    object current = enumerator.Current;
                    _arg2Context.IncrementItemCount(1);
                    if (!ReflectionTryWritePrimitive(_arg0XmlWriter, _arg2Context, itemType, current, null, null, ns, itemName, 0))
                    {
                        ReflectionWriteStartElement(itemType, ns, ns.Value, itemName.Value, 0);
                        if (isGenericDictionary || isDictionary)
                        {
                            _arg3CollectionDataContract.ItemContract.WriteXmlValue(_arg0XmlWriter, current, _arg2Context);
                        }
                        else
                        {
                            ReflectionWriteValue(itemType, current, false);
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
                    throw new NotImplementedException();
                }
            }
            else
            {
                if (isNullableOfT)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    if (memberValue == null)
                    {
                        _arg2Context.WriteNull(_arg0XmlWriter, memberType, DataContract.IsTypeSerializable(memberType));
                    }
                    else
                    {
                        PrimitiveDataContract primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(memberType);
                        if (primitiveContract != null && primitiveContract.UnderlyingType != Globals.TypeOfObject && !writeXsiType)
                        {
                            if (isNullableOfT)
                            {
                                primitiveContract.WriteXmlValue(_arg0XmlWriter, memberValue, _arg2Context);
                            }
                            else
                            {
                                primitiveContract.WriteXmlValue(_arg0XmlWriter, memberValue, _arg2Context);
                            }
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
