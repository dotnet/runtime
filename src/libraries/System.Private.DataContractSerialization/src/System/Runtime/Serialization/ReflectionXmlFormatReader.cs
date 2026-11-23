// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Reflection.Emit;
using System.Runtime.Serialization.DataContracts;
using System.Security;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace System.Runtime.Serialization
{
    internal sealed class ReflectionXmlClassReader
    {
        private readonly SyncReflectionClassReader _readClass;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public ReflectionXmlClassReader(ClassDataContract classDataContract)
        {
            _readClass = new SyncReflectionXmlReader().GenerateClassReader<SyncReflectionSerialization>(classDataContract, classDataContract.MemberNames, classDataContract.MemberNamespaces);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public object ReflectionReadClass(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext? context, XmlDictionaryString[]? memberNames, XmlDictionaryString[]? memberNamespaces)
        {
            Debug.Assert(context is not null);
            return _readClass(xmlReader, context);
        }
    }

    internal sealed class ReflectionXmlCollectionReader
    {
        private readonly SyncReflectionValueReader _readCollection;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal ReflectionXmlCollectionReader(CollectionDataContract collectionContract, bool? isGetOnlyCollection)
        {
            _readCollection = new SyncReflectionXmlReader().GenerateCollectionReader<SyncReflectionSerialization>(collectionContract, collectionContract.CollectionItemName, collectionContract.Namespace, isGetOnlyCollection);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public object ReflectionReadCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, CollectionDataContract collectionContract)
        {
            return _readCollection(xmlReader, context)!;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void ReflectionReadGetOnlyCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNs, CollectionDataContract collectionContract)
        {
            _readCollection(xmlReader, context);
        }
    }

    internal sealed partial class ReflectionXmlReader : ReflectionReader
    {
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override ReflectionValueReader CreateDictionaryItemReader<TMode>(CollectionDataContract collectionContract)
        {
            DataContract itemContract = collectionContract.ItemContract;
            return (reader, context) =>
            {
                context.ReadAttributes(reader);
                return TMode.ReadXmlValue(itemContract, reader, context);
            };
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override ReflectionCollectionBodyReader WrapCollectionBodyReader<TMode>(CollectionDataContract collectionContract, ReflectionCollectionBodyReader body) => body;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override ReflectionObjectReader CreateMembersReader<TMode>(ClassDataContract classContract, ReflectionObjectReader[] members, XmlDictionaryString[]? memberNames, XmlDictionaryString[]? memberNamespaces)
        {
            Debug.Assert(memberNames is not null && memberNamespaces is not null);
            int memberCount = members.Length;
            Debug.Assert(memberCount == memberNames.Length);
            int firstRequiredMember;
            bool[] requiredMembers = GetRequiredMembers(classContract, out firstRequiredMember);
            int[] nextRequiredMember = new int[memberCount + 1];
            nextRequiredMember[memberCount] = memberCount;
            for (int i = memberCount - 1; i >= 0; i--)
            {
                nextRequiredMember[i] = requiredMembers[i] ? i : nextRequiredMember[i + 1];
            }
            if (firstRequiredMember < memberCount)
            {
                return classContract.HasExtensionData
                    ? CreateMembersReader<TMode, ReflectionPlan.Enabled, ReflectionPlan.Enabled>(members, memberNames, memberNamespaces, firstRequiredMember, nextRequiredMember)
                    : CreateMembersReader<TMode, ReflectionPlan.Enabled, ReflectionPlan.Disabled>(members, memberNames, memberNamespaces, firstRequiredMember, nextRequiredMember);
            }

            return classContract.HasExtensionData
                ? CreateMembersReader<TMode, ReflectionPlan.Disabled, ReflectionPlan.Enabled>(members, memberNames, memberNamespaces, firstRequiredMember, nextRequiredMember)
                : CreateMembersReader<TMode, ReflectionPlan.Disabled, ReflectionPlan.Disabled>(members, memberNames, memberNamespaces, firstRequiredMember, nextRequiredMember);
        }

        private static ReflectionObjectReader CreateMembersReader<TMode, TRequiredMembers, TExtensionData>(
            ReflectionObjectReader[] members, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces,
            int firstRequiredMember, int[] nextRequiredMember)
            where TMode : struct, IReflectionSerialization
            where TRequiredMembers : struct
            where TExtensionData : struct
        {
            // Mixed member operations keep the profiled loop instead of adding object-valued async handoffs.
            if (RuntimeFeature.IsDynamicCodeCompiled && typeof(TExtensionData) == typeof(ReflectionPlan.Disabled) && members.Length != 0 &&
                Array.TrueForAll(members, static member => member.Target is IPrimitiveMemberReader))
            {
                ReflectionObjectReader[] remaining = new ReflectionObjectReader[members.Length + 1];
                for (int i = 0; i < remaining.Length; i++)
                {
                    int memberIndex = i - 1;
                    remaining[i] = (reader, context, obj) =>
                    {
                        if (typeof(TMode) == typeof(SyncReflectionSerialization))
                        {
                            return new ValueTask<object>(ReadMembersSync<TRequiredMembers, TExtensionData>(
                                reader, context, obj, members, memberNames, memberNamespaces, firstRequiredMember, nextRequiredMember,
                                memberIndex, initialize: false));
                        }

                        return ReadMembers<TMode, TRequiredMembers, TExtensionData>(
                            reader, context, obj, members, memberNames, memberNamespaces, firstRequiredMember, nextRequiredMember,
                            memberIndex, initialize: false);
                    };
                }

                return ReflectionReaderPipeline.Compile(members, memberNames, memberNamespaces, nextRequiredMember, remaining);
            }

            if (typeof(TMode) == typeof(SyncReflectionSerialization))
            {
                return (reader, context, obj) =>
                {
                    return new ValueTask<object>(ReadMembersSync<TRequiredMembers, TExtensionData>(
                        reader, context, obj, members, memberNames, memberNamespaces, firstRequiredMember, nextRequiredMember));
                };
            }
            return (reader, context, obj) => ReadMembers<TMode, TRequiredMembers, TExtensionData>(
                reader, context, obj, members, memberNames, memberNamespaces, firstRequiredMember, nextRequiredMember);
        }

        private static async ValueTask<object> ReadMembers<TMode, TRequiredMembers, TExtensionData>(
            XmlReaderDelegator reader, XmlObjectSerializerReadContext context, object obj,
            ReflectionObjectReader[] members, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces,
            int firstRequiredMember, int[] nextRequiredMember, int memberIndex = -1, bool initialize = true)
            where TMode : struct, IReflectionSerialization
            where TRequiredMembers : struct
            where TExtensionData : struct
        {
            int memberCount = members.Length;
            if (initialize)
                context.IncrementItemCount(memberCount);
            int requiredIndex = memberIndex < 0 ? firstRequiredMember : nextRequiredMember[memberIndex + 1];
            ExtensionDataObject? extensionData = null;
            if (typeof(TExtensionData) == typeof(ReflectionPlan.Enabled))
            {
                extensionData = new ExtensionDataObject();
                ((IExtensibleDataObject)obj).ExtensionData = extensionData;
            }
            while (reader.NodeType == XmlNodeType.Element ||
                reader.NodeType != XmlNodeType.EndElement && await TMode.MoveToNextElement(reader).ConfigureAwait(false))
            {
                int index = memberIndex + 1;
                if (index >= memberCount || !reader.IsLocalName(memberNames[index]) ||
                    !reader.IsNamespaceUri(memberNamespaces[index]))
                {
                    if (typeof(TRequiredMembers) == typeof(ReflectionPlan.Enabled))
                    {
                        index = await TMode.GetMemberIndexWithRequiredMembers(reader, context, memberNames, memberNamespaces, memberIndex, requiredIndex, extensionData).ConfigureAwait(false);
                    }
                    else
                    {
                        index = await TMode.GetMemberIndex(reader, context, memberNames, memberNamespaces, memberIndex, extensionData).ConfigureAwait(false);
                    }
                }
                if (index < memberCount)
                {
                    memberIndex = index;
                    ValueTask<object> value = ReadMember(members[index], reader, context, obj);
                    if (value.IsCompletedSuccessfully)
                        obj = value.Result;
                    else
                        obj = await value.ConfigureAwait(false);
                    if (typeof(TRequiredMembers) == typeof(ReflectionPlan.Enabled))
                    {
                        requiredIndex = nextRequiredMember[index + 1];
                    }
                }
            }
            if (typeof(TRequiredMembers) == typeof(ReflectionPlan.Enabled) && requiredIndex < memberCount)
            {
                XmlObjectSerializerReadContext.ThrowRequiredMemberMissingException(reader, memberIndex, requiredIndex, memberNames);
            }
            return obj;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ValueTask<object> ReadMember(ReflectionObjectReader member, XmlReaderDelegator reader,
            XmlObjectSerializerReadContext context, object obj) =>
            member(reader, context, obj);

        protected override string GetClassContractNamespace(ClassDataContract classContract)
        {
            return classContract.XmlName!.Namespace;
        }

        protected override string GetCollectionContractItemName(CollectionDataContract collectionContract)
        {
            return collectionContract.ItemName;
        }

        protected override string GetCollectionContractNamespace(CollectionDataContract collectionContract)
        {
            return collectionContract.XmlName.Namespace;
        }

        private static bool[] GetRequiredMembers(ClassDataContract contract, out int firstRequiredMember)
        {
            int memberCount = contract.MemberNames!.Length;
            bool[] requiredMembers = new bool[memberCount];
            GetRequiredMembers(contract, requiredMembers);
            for (firstRequiredMember = 0; firstRequiredMember < memberCount; firstRequiredMember++)
                if (requiredMembers[firstRequiredMember])
                    break;
            return requiredMembers;
        }

        private static int GetRequiredMembers(ClassDataContract contract, bool[] requiredMembers)
        {
            int memberCount = (contract.BaseClassContract == null) ? 0 : GetRequiredMembers(contract.BaseClassContract, requiredMembers);
            List<DataMember> members = contract.Members!;
            for (int i = 0; i < members.Count; i++, memberCount++)
            {
                requiredMembers[memberCount] = members[i].IsRequired;
            }
            return memberCount;
        }
    }

}
