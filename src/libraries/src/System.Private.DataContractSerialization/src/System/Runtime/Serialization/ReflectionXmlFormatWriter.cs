// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
                object memberValue = ReflectionGetMemberValue(obj, member);
                PrimitiveDataContract primitiveContract = member.MemberPrimitiveContract;

                if (writeXsiType || !ReflectionTryWritePrimitive(xmlWriter, context, memberType, memberValue, ns, memberNames[i + childElementIndex] /*name*/, primitiveContract))
                {
                    ReflectionWriteStartElement(xmlWriter, memberType, ns, ns.Value, member.Name, 0);
                    if (classContract.ChildElementNamespaces[i + childElementIndex] != null)
                    {
                        var nsChildElement = classContract.ChildElementNamespaces[i + childElementIndex];
                        xmlWriter.WriteNamespaceDecl(nsChildElement);
                    }
                    ReflectionWriteValue(xmlWriter, context, memberType, memberValue, writeXsiType, primitiveContractForParamType: null);
                    ReflectionWriteEndElement(xmlWriter);
                }
            }

            return memberCount;
        }

        private object ReflectionGetMemberValue(object obj, DataMember dataMember)
        {
            return dataMember.Getter(obj);
        }

        private bool ReflectionTryWritePrimitive(XmlWriterDelegator xmlWriter, XmlObjectSerializerWriteContext context, Type type, object value, XmlDictionaryString ns, XmlDictionaryString name, PrimitiveDataContract primitiveContract)
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

            if (collectionDataContract.ChildElementNamespace != null)
            {
                // TODO: should add tests for this.
                // xmlWriter.WriteNamespaceDecl(collectionContract.ChildElementNamespace);
                throw new NotImplementedException("collectionContract.ChildElementNamespace != null");
            }

            if (collectionDataContract.Kind == CollectionKind.Array)
            {
                context.IncrementArrayCount(xmlWriter, (Array)obj);
                Type itemType = collectionDataContract.ItemType;
                if (!ReflectionTryWritePrimitiveArray(xmlWriter, obj, collectionDataContract.UnderlyingType, itemType, itemName, ns))
                {
                    Array array = (Array)obj;
                    PrimitiveDataContract primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
                    for (int i = 0; i < array.Length; ++i)
                    {
                        ReflectionWriteStartElement(xmlWriter, itemType, ns, ns.Value, itemName.Value, 0);
                        ReflectionWriteValue(xmlWriter, context, itemType, array.GetValue(i), false, primitiveContract);
                        ReflectionWriteEndElement(xmlWriter);
                    }
                }
            }
            else
            {
                collectionDataContract.IncrementCollectionCount(xmlWriter, obj, context);

                Type enumeratorReturnType;
                IEnumerator enumerator = collectionDataContract.GetEnumeratorForCollection(obj, out enumeratorReturnType);
                PrimitiveDataContract primitiveContractForType = PrimitiveDataContract.GetPrimitiveDataContract(enumeratorReturnType);

                if (primitiveContractForType != null && primitiveContractForType.UnderlyingType != Globals.TypeOfObject)
                {
                    while (enumerator.MoveNext())
                    {
                        object current = enumerator.Current;
                        context.IncrementItemCount(1);
                        primitiveContractForType.WriteXmlElement(xmlWriter, current, context, itemName, ns);
                    }
                }
                else
                {
                    bool isDictionary = collectionDataContract.Kind == CollectionKind.Dictionary || collectionDataContract.Kind == CollectionKind.GenericDictionary;
                    while (enumerator.MoveNext())
                    {
                        object current = enumerator.Current;
                        context.IncrementItemCount(1);
                        ReflectionWriteStartElement(xmlWriter, enumeratorReturnType, ns, ns.Value, itemName.Value, 0);
                        if (isDictionary)
                        {
                            collectionDataContract.ItemContract.WriteXmlValue(xmlWriter, current, context);
                        }
                        else
                        {
                            ReflectionWriteValue(xmlWriter, context, enumeratorReturnType, current, false, primitiveContractForParamType: null);
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
