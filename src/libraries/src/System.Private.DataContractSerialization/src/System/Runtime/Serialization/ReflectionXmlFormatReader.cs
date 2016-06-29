// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Xml.Schema;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using System.Diagnostics;
#if NET_NATIVE
using Internal.Runtime.Augments;
#endif

namespace System.Runtime.Serialization
{
    internal sealed class ReflectionXmlFormatReader : ReflectionReader
    {
        public ReflectionXmlFormatReader(ClassDataContract classDataContract) : base(classDataContract)
        {
        }

        public ReflectionXmlFormatReader(CollectionDataContract collectionContract) :base(collectionContract)
        {
        }

        public object ReflectionReadClass(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces)
        {
            return ReflectionReadClassInternal(xmlReader, context, memberNames, memberNamespaces);
        }

        protected override void ReflectionReadMembers(ref object obj)
        {
            int memberCount = _classContract.MemberNames.Length;
            _contextArg.IncrementItemCount(memberCount);
            int memberIndex = -1;
            int firstRequiredMember;
            bool[] requiredMembers = GetRequiredMembers(_classContract, out firstRequiredMember);
            bool hasRequiredMembers = (firstRequiredMember < memberCount);
            int requiredIndex = hasRequiredMembers ? firstRequiredMember : -1;
            DataMember[] members = new DataMember[memberCount];
            int reflectedMemberCount = ReflectionGetMembers(_classContract, members);
            Debug.Assert(reflectedMemberCount == memberCount, "The value returned by ReflectionGetMembers() should equal to memberCount.");

            while (true)
            {
                if (!XmlObjectSerializerReadContext.MoveToNextElement(_xmlReaderArg))
                {
                    return;
                }
                if (hasRequiredMembers)
                {
                    memberIndex = _contextArg.GetMemberIndexWithRequiredMembers(_xmlReaderArg, _memberNamesArg, _memberNamespacesArg, memberIndex, requiredIndex, null);
                }
                else
                {
                    memberIndex = _contextArg.GetMemberIndex(_xmlReaderArg, _memberNamesArg, _memberNamespacesArg, memberIndex, null);
                }

                // GetMemberIndex returns memberNames.Length if member not found
                if (memberIndex < members.Length)
                {
                    ReflectionReadMember(ref obj, memberIndex, _xmlReaderArg, _contextArg, members);
                    requiredIndex = memberIndex + 1;
                }
            }
        }

        protected override string GetClassContractNamespace(ClassDataContract classContract)
        {
            return classContract.StableName.Namespace;
        }

        protected override string GetCollectionContractItemName(CollectionDataContract collectionContract)
        {
            return collectionContract.ItemName;
        }

        protected override string GetCollectionContractNamespace(CollectionDataContract collectionContract)
        {
            return collectionContract.StableName.Namespace;
        }

        public object ReflectionReadCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, CollectionDataContract collectionContract)
        {
            return ReflectionReadCollectionInternal(xmlReader, context, itemName, itemNamespace/*itemNamespace*/, collectionContract);
        }
        

        public void ReflectionReadGetOnlyCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNs, CollectionDataContract collectionContract)
        {
            ReflectionReadGetOnlyCollectionInternal(xmlReader, context, itemName, itemNs, collectionContract);
        }

        protected override object ReflectionReadDictionaryItem(CollectionDataContract collectionContract)
        {
            Debug.Assert(collectionContract.Kind == CollectionKind.Dictionary || collectionContract.Kind == CollectionKind.GenericDictionary);
            _contextArg.ReadAttributes(_xmlReaderArg);
            return collectionContract.ItemContract.ReadXmlValue(_xmlReaderArg, _contextArg);
        }

        private bool[] GetRequiredMembers(ClassDataContract contract, out int firstRequiredMember)
        {
            int memberCount = contract.MemberNames.Length;
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
            List<DataMember> members = contract.Members;
            for (int i = 0; i < members.Count; i++, memberCount++)
            {
                requiredMembers[memberCount] = members[i].IsRequired;
            }
            return memberCount;
        }
    }
}
