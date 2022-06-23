// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;

namespace System.Runtime.Serialization
{
    internal sealed class ReflectionXmlFormatWriter
    {
        private readonly ReflectionXmlClassWriter _reflectionClassWriter = new ReflectionXmlClassWriter();

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void ReflectionWriteClass(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract)
        {
            _reflectionClassWriter.ReflectionWriteClass(xmlWriter, obj, context, classContract, null/*memberNames*/);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public static void ReflectionWriteCollection(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, CollectionDataContract collectionDataContract)
        {
            XmlDictionaryString ns = collectionDataContract.Namespace;
            XmlDictionaryString itemName = collectionDataContract.CollectionItemName;

            if (collectionDataContract.ChildElementNamespace != null)
            {
                xmlWriter.WriteNamespaceDecl(collectionDataContract.ChildElementNamespace);
            }

            if (collectionDataContract.Kind == CollectionKind.Array)
            {
                context.IncrementArrayCount(xmlWriter, (Array)obj);
                Type itemType = collectionDataContract.ItemType;
                if (!ReflectionTryWritePrimitiveArray(xmlWriter, obj, collectionDataContract.UnderlyingType, itemType, itemName, ns))
                {
                    Array array = (Array)obj;
                    PrimitiveDataContract? primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
                    for (int i = 0; i < array.Length; ++i)
                    {
                        ReflectionXmlClassWriter.ReflectionWriteStartElement(xmlWriter, itemType, ns, ns.Value, itemName.Value, 0);
                        ReflectionXmlClassWriter.ReflectionWriteValue(xmlWriter, context, itemType, array.GetValue(i), false, primitiveContract);
                        ReflectionXmlClassWriter.ReflectionWriteEndElement(xmlWriter);
                    }
                }
            }
            else
            {
                collectionDataContract.IncrementCollectionCount(xmlWriter, obj, context);

                IEnumerator enumerator = collectionDataContract.GetEnumeratorForCollection(obj);
                PrimitiveDataContract? primitiveContractForType = PrimitiveDataContract.GetPrimitiveDataContract(collectionDataContract.UnderlyingType);

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
                    Type elementType = collectionDataContract.GetCollectionElementType();
                    bool isDictionary = collectionDataContract.Kind == CollectionKind.Dictionary || collectionDataContract.Kind == CollectionKind.GenericDictionary;
                    while (enumerator.MoveNext())
                    {
                        object current = enumerator.Current;
                        context.IncrementItemCount(1);
                        ReflectionXmlClassWriter.ReflectionWriteStartElement(xmlWriter, elementType, ns, ns.Value, itemName.Value, 0);
                        if (isDictionary)
                        {
                            collectionDataContract.ItemContract.WriteXmlValue(xmlWriter, current, context);
                        }
                        else
                        {
                            ReflectionXmlClassWriter.ReflectionWriteValue(xmlWriter, context, elementType, current, false, primitiveContractForParamType: null);
                        }

                        ReflectionXmlClassWriter.ReflectionWriteEndElement(xmlWriter);
                    }
                }
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static bool ReflectionTryWritePrimitiveArray(XmlWriterDelegator xmlWriter, object obj, Type type, Type itemType, XmlDictionaryString collectionItemName, XmlDictionaryString itemNamespace)
        {
            PrimitiveDataContract? primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
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
    }

    internal sealed class ReflectionXmlClassWriter : ReflectionClassWriter
    {
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override int ReflectionWriteMembers(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract, ClassDataContract derivedMostClassContract, int childElementIndex, XmlDictionaryString[]? emptyStringArray)
        {
            int memberCount = (classContract.BaseContract == null) ? 0 :
                ReflectionWriteMembers(xmlWriter, obj, context, classContract.BaseContract, derivedMostClassContract, childElementIndex, emptyStringArray);

            childElementIndex += memberCount;

            XmlDictionaryString[] memberNames = classContract.MemberNames!;
            XmlDictionaryString ns = classContract.Namespace;
            context.IncrementItemCount(classContract.Members!.Count);
            for (int i = 0; i < classContract.Members.Count; i++, memberCount++)
            {
                DataMember member = classContract.Members[i];
                Type memberType = member.MemberType;
                if (member.IsGetOnlyCollection)
                {
                    context.StoreIsGetOnlyCollection();
                }
                else
                {
                    context.ResetIsGetOnlyCollection();
                }

                bool shouldWriteValue = true;
                object? memberValue = null;
                if (!member.EmitDefaultValue)
                {
                    memberValue = ReflectionGetMemberValue(obj, member);
                    object? defaultValue = XmlFormatGeneratorStatics.GetDefaultValue(memberType);
                    if ((memberValue == null && defaultValue == null)
                        || (memberValue != null && memberValue.Equals(defaultValue)))
                    {
                        shouldWriteValue = false;

                        if (member.IsRequired)
                        {
                            XmlObjectSerializerWriteContext.ThrowRequiredMemberMustBeEmitted(member.Name, classContract.UnderlyingType);
                        }
                    }
                }

                if (shouldWriteValue)
                {
                    bool writeXsiType = CheckIfMemberHasConflict(member, classContract, derivedMostClassContract);
                    if (memberValue == null)
                    {
                        memberValue = ReflectionGetMemberValue(obj, member);
                    }
                    PrimitiveDataContract? primitiveContract = member.MemberPrimitiveContract;

                    if (writeXsiType || !ReflectionTryWritePrimitive(xmlWriter, context, memberType, memberValue, memberNames[i + childElementIndex] /*name*/, ns, primitiveContract))
                    {
                        ReflectionWriteStartElement(xmlWriter, memberType, ns, ns.Value, member.Name, 0);
                        if (classContract.ChildElementNamespaces![i + childElementIndex] != null)
                        {
                            var nsChildElement = classContract.ChildElementNamespaces[i + childElementIndex]!;
                            xmlWriter.WriteNamespaceDecl(nsChildElement);
                        }
                        ReflectionWriteValue(xmlWriter, context, memberType, memberValue, writeXsiType, primitiveContractForParamType: null);
                        ReflectionWriteEndElement(xmlWriter);
                    }

                    if (classContract.HasExtensionData)
                    {
                        context.WriteExtensionData(xmlWriter, ((IExtensibleDataObject)obj).ExtensionData, memberCount);
                    }
                }
            }

            return memberCount;
        }

        public static void ReflectionWriteStartElement(XmlWriterDelegator xmlWriter, Type type, XmlDictionaryString ns, string namespaceLocal, string nameLocal, int nameIndex)
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

        public static void ReflectionWriteEndElement(XmlWriterDelegator xmlWriter)
        {
            xmlWriter.WriteEndElement();
        }

        private static bool NeedsPrefix(Type type, XmlDictionaryString? ns)
        {
            return type == Globals.TypeOfXmlQualifiedName && (ns != null && ns.Value != null && ns.Value.Length > 0);
        }

        private static bool CheckIfMemberHasConflict(DataMember member, ClassDataContract classContract, ClassDataContract derivedMostClassContract)
        {
            // Check for conflict with base type members
            if (CheckIfConflictingMembersHaveDifferentTypes(member))
                return true;

            // Check for conflict with derived type members
            string? name = member.Name;
            string? ns = classContract.StableName.Namespace;
            ClassDataContract? currentContract = derivedMostClassContract;
            while (currentContract != null && currentContract != classContract)
            {
                if (ns == currentContract.StableName.Namespace)
                {
                    List<DataMember> members = currentContract.Members!;
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

        private static bool CheckIfConflictingMembersHaveDifferentTypes(DataMember member)
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
