// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization.DataContracts;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization
{
    internal sealed class ReflectionXmlFormatWriter
    {
        private readonly SyncReflectionWriter _writeClass;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal ReflectionXmlFormatWriter(ClassDataContract classContract)
        {
            _writeClass = new SyncReflectionXmlClassWriter().GenerateClassWriter<SyncReflectionSerialization>(classContract, null);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void ReflectionWriteClass(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract)
        {
            _writeClass(xmlWriter, obj, context);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static XmlFormatCollectionWriterDelegate CreateCollectionWriter(CollectionDataContract collectionContract)
        {
            SyncReflectionWriter write = new SyncReflectionXmlClassWriter().GenerateCollectionWriter<SyncReflectionSerialization>(collectionContract);
            return (writer, obj, context, contract) => write(writer, obj, context);
        }

    }

    internal sealed class ReflectionXmlClassWriter : ReflectionClassWriter
    {
        protected override XmlDictionaryString GetCollectionItemName(CollectionDataContract collectionContract) => collectionContract.CollectionItemName;
        protected override XmlDictionaryString GetCollectionNamespace(CollectionDataContract collectionContract) => collectionContract.Namespace;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override ReflectionValueWriter CreateCollectionItemWriter<TMode>(CollectionDataContract collectionContract)
        {
            bool isArray = collectionContract.Kind == CollectionKind.Array;
            Type elementType = isArray ? collectionContract.ItemType : collectionContract.GetCollectionElementType();
            ReflectionValueWriter writeValue;
            if (collectionContract.Kind is CollectionKind.Dictionary or CollectionKind.GenericDictionary)
            {
                DataContract itemContract = collectionContract.ItemContract;
                writeValue = (writer, value, context) => TMode.WriteXmlValue(itemContract, writer, value!, context);
            }
            else
            {
                PrimitiveDataContract? primitiveContract = isArray ? PrimitiveDataContract.GetPrimitiveDataContract(elementType) : null;
                writeValue = CreateValueWriter<TMode>(elementType, false, primitiveContract);
            }

            XmlDictionaryString name = collectionContract.CollectionItemName;
            XmlDictionaryString ns = collectionContract.Namespace;
            string? prefix = NeedsPrefix(elementType, ns) ? Globals.ElementPrefix : null;
            if (elementType == typeof(string))
            {
                if (typeof(TMode) == typeof(SyncReflectionSerialization))
                {
                    return (writer, value, context) =>
                    {
                        context.WriteString(writer, (string?)value, name, ns);
                        return Task.CompletedTask;
                    };
                }

                return (writer, value, context) =>
                    context.WriteReferencePrimitiveElementAsync<ReflectionPlan.StringValue>(
                        writer, value, typeof(string), name, ns, null);
            }
            return (writer, value, context) =>
                ReflectionSerialization.WriteElement<TMode>(writer, value, context, writeValue, prefix, name, ns);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override ReflectionWriter WrapCollectionWriter<TMode>(CollectionDataContract collectionContract, ReflectionWriter? count, ReflectionWriter body)
        {
            if (collectionContract.ChildElementNamespace is XmlDictionaryString childNamespace)
            {
                ReflectionWriter declare = (writer, obj, context) => TMode.WriteNamespaceDecl(writer, childNamespace);
                if (count is not null)
                {
                    return ReflectionSerialization.CreateWriterSequence(new[] { declare, count, body });
                }

                return Compose(declare, body);
            }

            return count is null ? body : Compose(count, body);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override ReflectionMemberValueWriter CreateMemberValueWriter<TMode>(ClassDataContract classContract, ClassDataContract rootContract, DataMember member, XmlDictionaryString[]? memberNames, int memberIndex)
        {
            XmlDictionaryString name = rootContract.MemberNames![memberIndex];
            XmlDictionaryString ns = classContract.Namespace;
            bool writeXsiType = CheckIfMemberHasConflict(member, classContract, rootContract);
            PrimitiveDataContract? primitiveContract = member.MemberPrimitiveContract;
            if (!writeXsiType && primitiveContract is not null && primitiveContract.UnderlyingType != typeof(object))
            {
                return CreatePrimitiveElementWriter<TMode>(primitiveContract, name, ns);
            }
            if (!writeXsiType && TryCreateNullablePrimitiveElementWriter<TMode>(member.MemberType, name, ns) is ReflectionMemberValueWriter nullableWriter)
            {
                return nullableWriter;
            }

            ReflectionValueWriter writeValue = CreateValueWriter<TMode>(member.MemberType, writeXsiType, null);
            string? prefix = NeedsPrefix(member.MemberType, ns) ? Globals.ElementPrefix : null;
            XmlDictionaryString? childNamespace = classContract.ChildElementNamespaces![memberIndex];
            return ReflectionSerialization.CreateElementWriter<TMode>(writeValue, prefix, name, ns, childNamespace);
        }

        private static bool NeedsPrefix(Type type, XmlDictionaryString? ns)
        {
            return type == typeof(XmlQualifiedName) && (ns != null && ns.Value != null && ns.Value.Length > 0);
        }

        private static bool CheckIfMemberHasConflict(DataMember member, ClassDataContract classContract, ClassDataContract derivedMostClassContract)
        {
            // Check for conflict with base type members
            if (CheckIfConflictingMembersHaveDifferentTypes(member))
                return true;

            // Check for conflict with derived type members
            string? name = member.Name;
            string? ns = classContract.XmlName.Namespace;
            ClassDataContract? currentContract = derivedMostClassContract;
            while (currentContract != null && currentContract != classContract)
            {
                if (ns == currentContract.XmlName.Namespace)
                {
                    List<DataMember> members = currentContract.Members!;
                    for (int j = 0; j < members.Count; j++)
                    {
                        if (name == members[j].Name)
                            return CheckIfConflictingMembersHaveDifferentTypes(members[j]);
                    }
                }
                currentContract = currentContract.BaseClassContract;
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
