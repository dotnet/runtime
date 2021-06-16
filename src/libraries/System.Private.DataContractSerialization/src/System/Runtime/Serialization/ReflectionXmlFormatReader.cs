// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml;
using System.Xml.Schema;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization
{
    internal sealed class ReflectionXmlClassReader
    {
        private readonly ClassDataContract _classContract;
        private readonly ReflectionReader _reflectionReader;

        public ReflectionXmlClassReader(ClassDataContract classDataContract)
        {
            Debug.Assert(classDataContract != null);
            _classContract = classDataContract;
            _reflectionReader = new ReflectionXmlReader();
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public object ReflectionReadClass(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext? context, XmlDictionaryString[]? memberNames, XmlDictionaryString[]? memberNamespaces)
        {
            return _reflectionReader.ReflectionReadClass(xmlReader, context, memberNames, memberNamespaces, _classContract);
        }
    }

    internal sealed class ReflectionXmlCollectionReader
    {
        private readonly ReflectionReader _reflectionReader = new ReflectionXmlReader();

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public object ReflectionReadCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, CollectionDataContract collectionContract)
        {
            return _reflectionReader.ReflectionReadCollection(xmlReader, context, itemName, itemNamespace/*itemNamespace*/, collectionContract);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void ReflectionReadGetOnlyCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNs, CollectionDataContract collectionContract)
        {
            _reflectionReader.ReflectionReadGetOnlyCollection(xmlReader, context, itemName, itemNs, collectionContract);
        }
    }

    internal sealed class ReflectionXmlReader : ReflectionReader
    {
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override void ReflectionReadMembers(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString[] memberNames, XmlDictionaryString[]? memberNamespaces, ClassDataContract classContract, ref object obj)
        {
            Debug.Assert(memberNamespaces != null);

            int memberCount = classContract.MemberNames!.Length;
            context.IncrementItemCount(memberCount);
            int memberIndex = -1;
            int firstRequiredMember;
            _ = GetRequiredMembers(classContract, out firstRequiredMember);
            bool hasRequiredMembers = (firstRequiredMember < memberCount);
            int requiredIndex = hasRequiredMembers ? firstRequiredMember : -1;
            DataMember[] members = new DataMember[memberCount];
            int reflectedMemberCount = ReflectionGetMembers(classContract, members);
            Debug.Assert(reflectedMemberCount == memberCount, "The value returned by ReflectionGetMembers() should equal to memberCount.");
            ExtensionDataObject? extensionData = null;

            if (classContract.HasExtensionData)
            {
                extensionData = new ExtensionDataObject();
                ((IExtensibleDataObject)obj).ExtensionData = extensionData;
            }

            while (true)
            {
                if (!XmlObjectSerializerReadContext.MoveToNextElement(xmlReader))
                {
                    return;
                }
                if (hasRequiredMembers)
                {
                    memberIndex = context.GetMemberIndexWithRequiredMembers(xmlReader, memberNames, memberNamespaces, memberIndex, requiredIndex, extensionData);
                }
                else
                {
                    memberIndex = context.GetMemberIndex(xmlReader, memberNames, memberNamespaces, memberIndex, extensionData);
                }

                // GetMemberIndex returns memberNames.Length if member not found
                if (memberIndex < members.Length)
                {
                    ReflectionReadMember(xmlReader, context, classContract, ref obj, memberIndex, members);
                    requiredIndex = memberIndex + 1;
                }
            }
        }

        protected override string GetClassContractNamespace(ClassDataContract classContract)
        {
            return classContract.StableName!.Namespace;
        }

        protected override string GetCollectionContractItemName(CollectionDataContract collectionContract)
        {
            return collectionContract.ItemName;
        }

        protected override string GetCollectionContractNamespace(CollectionDataContract collectionContract)
        {
            return collectionContract.StableName.Namespace;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override object? ReflectionReadDictionaryItem(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, CollectionDataContract collectionContract)
        {
            Debug.Assert(collectionContract.Kind == CollectionKind.Dictionary || collectionContract.Kind == CollectionKind.GenericDictionary);
            context.ReadAttributes(xmlReader);
            return collectionContract.ItemContract.ReadXmlValue(xmlReader, context);
        }

        private bool[] GetRequiredMembers(ClassDataContract contract, out int firstRequiredMember)
        {
            int memberCount = contract.MemberNames!.Length;
            bool[] requiredMembers = new bool[memberCount];
            GetRequiredMembers(contract, requiredMembers);
            for (firstRequiredMember = 0; firstRequiredMember < memberCount; firstRequiredMember++)
                if (requiredMembers[firstRequiredMember])
                    break;
            return requiredMembers;
        }

        private int GetRequiredMembers(ClassDataContract contract, bool[] requiredMembers)
        {
            int memberCount = (contract.BaseContract == null) ? 0 : GetRequiredMembers(contract.BaseContract, requiredMembers);
            List<DataMember> members = contract.Members!;
            for (int i = 0; i < members.Count; i++, memberCount++)
            {
                requiredMembers[memberCount] = members[i].IsRequired;
            }
            return memberCount;
        }
    }
}
