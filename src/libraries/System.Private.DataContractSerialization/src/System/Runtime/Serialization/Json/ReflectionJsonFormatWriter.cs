// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.DataContracts;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal sealed class ReflectionJsonFormatWriter
    {
        private readonly SyncReflectionWriter _writeClass;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal ReflectionJsonFormatWriter(ClassDataContract classContract, XmlDictionaryString[]? memberNames)
        {
            _writeClass = new SyncReflectionJsonClassWriter().GenerateClassWriter<SyncReflectionSerialization>(classContract, memberNames);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void ReflectionWriteClass(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, ClassDataContract classContract, XmlDictionaryString[]? memberNames)
        {
            _writeClass(xmlWriter, obj, context);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static JsonFormatCollectionWriterDelegate CreateCollectionWriter(CollectionDataContract collectionContract, bool useSimpleDictionaryFormat)
        {
            SyncReflectionWriter write = new SyncReflectionJsonClassWriter(useSimpleDictionaryFormat).GenerateCollectionWriter<SyncReflectionSerialization>(collectionContract);
            return (writer, obj, context, contract) => write(writer, obj, context);
        }

    }

    internal sealed class ReflectionJsonClassWriter : ReflectionClassWriter
    {
        private readonly bool _useSimpleDictionaryFormat;

        internal ReflectionJsonClassWriter(bool useSimpleDictionaryFormat = false)
        {
            _useSimpleDictionaryFormat = useSimpleDictionaryFormat;
        }

        protected override XmlDictionaryString GetCollectionItemName(CollectionDataContract collectionContract) => XmlObjectSerializerWriteContextComplexJson.CollectionItemName;
        protected override XmlDictionaryString? GetCollectionNamespace(CollectionDataContract collectionContract) => null;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override ReflectionValueWriter CreateCollectionItemWriter<TMode>(CollectionDataContract collectionContract)
        {
            bool isArray = collectionContract.Kind == CollectionKind.Array;
            Type elementType = isArray ? collectionContract.ItemType : collectionContract.GetCollectionElementType();
            ReflectionValueWriter writeValue;
            if (collectionContract.Kind is CollectionKind.Dictionary or CollectionKind.GenericDictionary)
            {
                JsonDataContract itemContract = JsonDataContract.GetJsonDataContract(XmlObjectSerializerWriteContextComplexJson.GetRevisedItemContract(collectionContract.ItemContract));
                RuntimeTypeHandle itemTypeHandle = RuntimeFeature.IsDynamicCodeSupported ? collectionContract.ItemType.TypeHandle : default;
                writeValue = (writer, value, context) => TMode.WriteJsonValue(itemContract, writer, value!, (XmlObjectSerializerWriteContextComplexJson)context, itemTypeHandle);
            }
            else
            {
                PrimitiveDataContract? primitiveContract = isArray ? PrimitiveDataContract.GetPrimitiveDataContract(elementType) : null;
                writeValue = CreateValueWriter<TMode>(elementType, false, primitiveContract);
            }

            XmlDictionaryString name = XmlObjectSerializerWriteContextComplexJson.CollectionItemName;
            return (writer, value, context) =>
                ReflectionSerialization.WriteElement<TMode>(writer, value, context, writeValue, null, name, null);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override ReflectionWriter WrapCollectionWriter<TMode>(CollectionDataContract collectionContract, ReflectionWriter? count, ReflectionWriter body)
        {
            if (_useSimpleDictionaryFormat && collectionContract.Kind is CollectionKind.Dictionary or CollectionKind.GenericDictionary)
            {
                ReflectionWriter simple = CreateSimpleDictionaryWriter<TMode>(collectionContract);
                return count is null ? simple : Compose(count, simple);
            }

            if (count is not null)
            {
                return async (writer, obj, context) =>
                {
                    await count(writer, obj, context).ConfigureAwait(false);
                    await TMode.WriteAttributeString(writer, null, JsonGlobals.typeString, null, JsonGlobals.arrayString).ConfigureAwait(false);
                    await body(writer, obj, context).ConfigureAwait(false);
                };
            }

            return async (writer, obj, context) =>
            {
                await TMode.WriteAttributeString(writer, null, JsonGlobals.typeString, null, JsonGlobals.arrayString).ConfigureAwait(false);
                await body(writer, obj, context).ConfigureAwait(false);
            };
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static ReflectionWriter CreateSimpleDictionaryWriter<TMode>(CollectionDataContract collectionContract)
            where TMode : struct, IReflectionSerialization
        {
            Func<object, IEnumerator> getEnumerator = CreateEnumerator(collectionContract);
            Type valueType = collectionContract.ItemType.GetGenericArguments()[1];
            ReflectionValueWriter writeValue = CreateValueWriter<TMode>(valueType, false, null);
            return async (writer, obj, context) =>
            {
                await TMode.WriteAttributeString(writer, null, JsonGlobals.typeString, null, JsonGlobals.objectString).ConfigureAwait(false);
                IEnumerator enumerator = getEnumerator(obj);
                try
                {
                    while (enumerator.MoveNext())
                    {
                        IKeyValue item = (IKeyValue)enumerator.Current;
                        await TMode.WriteStartElementString(writer, null, item.Key!.ToString()!, null).ConfigureAwait(false);
                        await writeValue(writer, item.Value, context).ConfigureAwait(false);
                        await TMode.WriteEndElement(writer).ConfigureAwait(false);
                    }
                }
                finally
                {
                    (enumerator as IDisposable)?.Dispose();
                }
            };
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override ReflectionMemberValueWriter CreateMemberValueWriter<TMode>(ClassDataContract classContract, ClassDataContract rootContract, DataMember member, XmlDictionaryString[]? memberNames, int memberIndex)
        {
            Debug.Assert(memberNames is not null);
            XmlDictionaryString name = memberNames[memberIndex];
            bool requiresNameAttribute = DataContractJsonSerializer.CheckIfJsonNameRequiresMapping(name);
            PrimitiveDataContract? primitiveContract = member.MemberPrimitiveContract;
            if (!requiresNameAttribute && primitiveContract is not null && primitiveContract.UnderlyingType != typeof(object))
            {
                return CreatePrimitiveElementWriter<TMode>(primitiveContract, name, null);
            }
            if (!requiresNameAttribute &&
                TryCreateNullablePrimitiveElementWriter<TMode>(member.MemberType, name, null) is ReflectionMemberValueWriter nullableWriter)
            {
                return nullableWriter;
            }

            ReflectionValueWriter writeValue = CreateValueWriter<TMode>(member.MemberType, false, null);
            if (requiresNameAttribute)
            {
                return async (writer, obj, value, context) =>
                {
                    await TMode.WriteJsonNameWithMapping(writer, memberNames, memberIndex).ConfigureAwait(false);
                    await writeValue(writer, value, context).ConfigureAwait(false);
                    await TMode.WriteEndElement(writer).ConfigureAwait(false);
                };
            }

            return ReflectionSerialization.CreateElementWriter<TMode>(writeValue, null, name, null, null);
        }

    }
}
