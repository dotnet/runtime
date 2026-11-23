// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.DataContracts;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal enum JsonDictionaryFormat
    {
        Array,
        Simple
    }

    internal sealed class ReflectionJsonClassReader
    {
        private readonly SyncReflectionClassReader _readClass;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public ReflectionJsonClassReader(ClassDataContract classDataContract, XmlDictionaryString[]? memberNames)
        {
            _readClass = new SyncReflectionJsonReader().GenerateClassReader<SyncReflectionSerialization>(classDataContract, memberNames, null);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public object ReflectionReadClass(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContextComplexJson? context, XmlDictionaryString _, XmlDictionaryString[]? memberNames)
        {
            Debug.Assert(context is not null);
            return _readClass(xmlReader, context);
        }
    }

    internal sealed class ReflectionJsonCollectionReader
    {
        private readonly SyncReflectionValueReader _readCollection;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal ReflectionJsonCollectionReader(CollectionDataContract collectionContract, bool? isGetOnlyCollection, bool useSimpleDictionaryFormat)
        {
            _readCollection = new SyncReflectionJsonReader(useSimpleDictionaryFormat).GenerateCollectionReader<SyncReflectionSerialization>(collectionContract, JsonGlobals.itemDictionaryString, XmlDictionaryString.Empty, isGetOnlyCollection);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public object ReflectionReadCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContextComplexJson context, XmlDictionaryString emptyDictionaryString, XmlDictionaryString itemName, CollectionDataContract collectionContract)
        {
            return _readCollection(xmlReader, context)!;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void ReflectionReadGetOnlyCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContextComplexJson context, XmlDictionaryString emptyDictionaryString, XmlDictionaryString itemName, CollectionDataContract collectionContract)
        {
            _readCollection(xmlReader, context);
        }
    }

    internal sealed class ReflectionJsonReader : ReflectionReader
    {
        private readonly bool _useSimpleDictionaryFormat;

        internal ReflectionJsonReader(bool useSimpleDictionaryFormat = false)
        {
            _useSimpleDictionaryFormat = useSimpleDictionaryFormat;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override ReflectionValueReader CreateDictionaryItemReader<TMode>(CollectionDataContract collectionContract)
        {
            JsonDataContract itemContract = JsonDataContract.GetJsonDataContract(XmlObjectSerializerWriteContextComplexJson.GetRevisedItemContract(collectionContract.ItemContract));
            return (reader, context) =>
            {
                context.ReadAttributes(reader);
                return TMode.ReadJsonValue(itemContract, reader, (XmlObjectSerializerReadContextComplexJson)context);
            };
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override ReflectionCollectionBodyReader WrapCollectionBodyReader<TMode>(CollectionDataContract collectionContract, ReflectionCollectionBodyReader body)
        {
            if (_useSimpleDictionaryFormat && collectionContract.Kind is CollectionKind.Dictionary or CollectionKind.GenericDictionary)
            {
                return CreateSimpleDictionaryReader<TMode>(collectionContract);
            }

            return body;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private ReflectionCollectionBodyReader CreateSimpleDictionaryReader<TMode>(CollectionDataContract collectionContract)
            where TMode : struct, IReflectionSerialization
        {
            Type[] types = collectionContract.ItemType.GetGenericArguments();
            Type keyType = types[0];
            DataContract keyContract = DataContract.GetDataContract(keyType);
            Type? nullableKeyType = Nullable.GetUnderlyingType(keyType);
            keyType = nullableKeyType ?? keyType;
            Func<string, object>? parse;
            if (keyType == typeof(string) || keyType == typeof(object))
            {
                parse = static value => value;
            }
            else if (keyType.IsEnum)
            {
                parse = value => Enum.Parse(keyType, value);
            }
            else if (keyContract.ParseMethod is MethodInfo parseMethod)
            {
                parse = Type.GetTypeCode(keyContract.UnderlyingType) switch
                {
                    TypeCode.Boolean => static value => bool.Parse(value),
                    TypeCode.Int16 => static value => short.Parse(value),
                    TypeCode.Int32 => static value => int.Parse(value),
                    TypeCode.Int64 => static value => long.Parse(value),
                    TypeCode.Char => static value => char.Parse(value),
                    TypeCode.Byte => static value => byte.Parse(value),
                    TypeCode.SByte => static value => sbyte.Parse(value),
                    TypeCode.Double => static value => double.Parse(value),
                    TypeCode.Decimal => static value => decimal.Parse(value),
                    TypeCode.Single => static value => float.Parse(value),
                    TypeCode.UInt16 => static value => ushort.Parse(value),
                    TypeCode.UInt32 => static value => uint.Parse(value),
                    TypeCode.UInt64 => static value => ulong.Parse(value),
                    _ => value => parseMethod.Invoke(null, new object[] { value })!
                };
            }
            else
            {
                parse = null;
            }

            if (parse is null)
            {
                string message = SR.Format(SR.KeyTypeCannotBeParsedInSimpleDictionary, DataContract.GetClrTypeFullName(collectionContract.UnderlyingType), DataContract.GetClrTypeFullName(keyType));
                return (reader, context, collection, isGetOnly) => throw new InvalidOperationException(message);
            }
            if (nullableKeyType is not null)
            {
                Func<string, object> parseKey = parse;
                parse = value =>
                {
                    parseKey(value);
                    throw new NotImplementedException(SR.Format(SR.MustBeGreaterThanZero, 1));
                };
            }

            ReflectionValueReader readValue = CreateValueReader<TMode>(types[1], string.Empty, string.Empty);
            Action<object, object?, object?> add = CreateDictionaryAdder(collectionContract);
            Type collectionType = collectionContract.UnderlyingType;
            return async (reader, context, collection, isGetOnly) =>
            {
                while (await TMode.MoveToNextElement(reader).ConfigureAwait(false))
                {
                    if (!await TMode.IsStartElement(reader).ConfigureAwait(false))
                    {
                        throw XmlObjectSerializerReadContext.CreateUnexpectedStateException(XmlNodeType.Element, reader);
                    }
                    context.IncrementItemCount(1);
                    object key = parse(XmlObjectSerializerReadContextComplexJson.GetJsonMemberName(reader));
                    object? value = await readValue(reader, context).ConfigureAwait(false);
                    if (collection is null)
                    {
                        XmlObjectSerializerReadContext.ThrowNullValueReturnedForGetOnlyCollectionException(collectionType);
                    }
                    add(collection, key, value);
                }

                return collection;
            };
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override ReflectionObjectReader CreateMembersReader<TMode>(ClassDataContract classContract, ReflectionObjectReader[] members, XmlDictionaryString[]? memberNames, XmlDictionaryString[]? memberNamespaces)
        {
            Debug.Assert(memberNames is not null);
            int memberCount = members.Length;
            Debug.Assert(memberCount == memberNames.Length);
            byte[] required = new byte[(memberCount + 7) / 8];
            int requiredCount = SetRequiredMembers(classContract, required);
            Debug.Assert(requiredCount == memberCount);
            List<int> requiredMemberIndices = new List<int>();
            for (int i = 0; i < memberCount; i++)
            {
                if ((required[i / 8] & (1 << (i % 8))) != 0)
                {
                    requiredMemberIndices.Add(i);
                }
            }
            int[] requiredIndices = requiredMemberIndices.ToArray();
            if (requiredIndices.Length == 0)
            {
                return CreateMembersReader<TMode, ReflectionPlan.Disabled>(classContract, members, memberNames, required, requiredIndices);
            }

            return CreateMembersReader<TMode, ReflectionPlan.Enabled>(classContract, members, memberNames, required, requiredIndices);
        }

        private static ReflectionObjectReader CreateMembersReader<TMode, TRequiredMembers>(
            ClassDataContract classContract, ReflectionObjectReader[] members, XmlDictionaryString[] memberNames,
            byte[] required, int[] requiredIndices)
            where TMode : struct, IReflectionSerialization
            where TRequiredMembers : struct
        {
            if (classContract.HasExtensionData)
            {
                return CreateMembersReader<TMode, TRequiredMembers, ReflectionPlan.Enabled>(members, memberNames, required, requiredIndices);
            }

            return CreateMembersReader<TMode, TRequiredMembers, ReflectionPlan.Disabled>(members, memberNames, required, requiredIndices);
        }

        private static ReflectionObjectReader CreateMembersReader<TMode, TRequiredMembers, TExtensionData>(
            ReflectionObjectReader[] members, XmlDictionaryString[] memberNames, byte[] required, int[] requiredIndices)
            where TMode : struct, IReflectionSerialization
            where TRequiredMembers : struct
            where TExtensionData : struct =>
            members.Length <= 64
                ? CreateMembersReader<TMode, TRequiredMembers, TExtensionData, ReflectionPlan.Disabled>(members, memberNames, required, requiredIndices)
                : CreateMembersReader<TMode, TRequiredMembers, TExtensionData, ReflectionPlan.Enabled>(members, memberNames, required, requiredIndices);

        private static ReflectionObjectReader CreateMembersReader<TMode, TRequiredMembers, TExtensionData, TLargeClass>(
            ReflectionObjectReader[] members, XmlDictionaryString[] memberNames, byte[] required, int[] requiredIndices)
            where TMode : struct, IReflectionSerialization
            where TRequiredMembers : struct
            where TExtensionData : struct
            where TLargeClass : struct
        {
            int memberCount = members.Length;
            return async (reader, context, obj) =>
            {
                XmlObjectSerializerReadContextComplexJson jsonContext = (XmlObjectSerializerReadContextComplexJson)context;
                context.IncrementItemCount(memberCount);
                int memberIndex = -1;
                ulong seen = 0;
                ulong[]? additionalSeen = typeof(TLargeClass) == typeof(ReflectionPlan.Enabled)
                    ? new ulong[(memberCount - 64 + 63) / 64] : null;
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
                        !reader.IsNamespaceUri(XmlDictionaryString.Empty))
                    {
                        index = await TMode.GetJsonMemberIndex(reader, jsonContext, memberNames, memberIndex, extensionData).ConfigureAwait(false);
                    }
                    if (index < memberCount)
                    {
                        ulong mask = 1UL << (index % 64);
                        ulong observed = typeof(TLargeClass) == typeof(ReflectionPlan.Disabled) || index < 64
                            ? seen : additionalSeen![(index - 64) / 64];
                        if ((observed & mask) != 0)
                        {
                            XmlObjectSerializerReadContextComplexJson.ThrowDuplicateMemberException(obj, memberNames, index);
                        }
                        if (typeof(TLargeClass) == typeof(ReflectionPlan.Disabled) || index < 64)
                        {
                            seen |= mask;
                        }
                        else
                        {
                            additionalSeen![(index - 64) / 64] |= mask;
                        }
                        memberIndex = index;
                        ValueTask<object> value = members[index](reader, context, obj);
                        if (value.IsCompletedSuccessfully)
                            obj = value.Result;
                        else
                            obj = await value.ConfigureAwait(false);
                    }
                }
                if (typeof(TRequiredMembers) == typeof(ReflectionPlan.Enabled))
                {
                    foreach (int i in requiredIndices)
                    {
                        ulong observed = typeof(TLargeClass) == typeof(ReflectionPlan.Disabled) || i < 64
                            ? seen : additionalSeen![(i - 64) / 64];
                        if ((observed & (1UL << (i % 64))) == 0)
                        {
                            byte[] missing = new byte[required.Length];
                            for (int j = 0; j < memberCount; j++)
                            {
                                ulong visited = typeof(TLargeClass) == typeof(ReflectionPlan.Disabled) || j < 64
                                    ? seen : additionalSeen![(j - 64) / 64];
                                if ((visited & (1UL << (j % 64))) == 0)
                                {
                                    missing[j / 8] |= (byte)(1 << (j % 8));
                                }
                            }
                            XmlObjectSerializerReadContextComplexJson.ThrowMissingRequiredMembers(obj, memberNames, missing, required);
                        }
                    }
                }
                return obj;
            };
        }

        private static int SetRequiredMembers(ClassDataContract contract, byte[] required)
        {
            int index = contract.BaseClassContract is null ? 0 : SetRequiredMembers(contract.BaseClassContract, required);
            foreach (DataMember member in contract.Members!)
            {
                if (member.IsRequired)
                {
                    required[index / 8] |= (byte)(1 << (index % 8));
                }
                index++;
            }
            return index;
        }

        protected override string GetClassContractNamespace(ClassDataContract classContract)
        {
            return string.Empty;
        }

        protected override string GetCollectionContractItemName(CollectionDataContract collectionContract)
        {
            return JsonGlobals.itemString;
        }

        protected override string GetCollectionContractNamespace(CollectionDataContract collectionContract)
        {
            return string.Empty;
        }

    }
}
