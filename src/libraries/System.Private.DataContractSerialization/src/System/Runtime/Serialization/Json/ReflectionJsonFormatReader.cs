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
    internal sealed class ReflectionJsonClassReader
    {
        private readonly ClassDataContract _classContract;
        private readonly ReflectionReader _reflectionReader;

        public ReflectionJsonClassReader(ClassDataContract classDataContract)
        {
            Debug.Assert(classDataContract != null);
            _classContract = classDataContract;
            _reflectionReader = new ReflectionJsonReader();
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public object ReflectionReadClass(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContextComplexJson? context, XmlDictionaryString _, XmlDictionaryString[]? memberNames)
        {
            Debug.Assert(_classContract != null);
            return _reflectionReader.ReflectionReadClass(xmlReader, context, memberNames, null /*memberNamespaces*/, _classContract);
        }
    }

    internal sealed class ReflectionJsonCollectionReader
    {
        private readonly ReflectionReader _reflectionReader = new ReflectionJsonReader();

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public object ReflectionReadCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContextComplexJson context, XmlDictionaryString emptyDictionaryString, XmlDictionaryString itemName, CollectionDataContract collectionContract)
        {
            return _reflectionReader.ReflectionReadCollection(xmlReader, context, itemName, emptyDictionaryString/*itemNamespace*/, collectionContract);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void ReflectionReadGetOnlyCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContextComplexJson context, XmlDictionaryString emptyDictionaryString, XmlDictionaryString itemName, CollectionDataContract collectionContract)
        {
            _reflectionReader.ReflectionReadGetOnlyCollection(xmlReader, context, itemName, emptyDictionaryString/*itemNamespace*/, collectionContract);
        }
    }

    internal sealed class ReflectionJsonReader : ReflectionReader
    {
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override void ReflectionReadMembers(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString[] memberNames, XmlDictionaryString[]? memberNamespaces, ClassDataContract classContract, ref object obj)
        {
            var jsonContext = context as XmlObjectSerializerReadContextComplexJson;
            Debug.Assert(jsonContext != null);

            int memberCount = classContract.MemberNames!.Length;
            context.IncrementItemCount(memberCount);

            DataMember[] members = new DataMember[memberCount];
            ReflectionGetMembers(classContract, members);

            int memberIndex = -1;

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

                memberIndex = jsonContext.GetJsonMemberIndex(xmlReader, memberNames, memberIndex, extensionData);
                // GetMemberIndex returns memberNames.Length if member not found
                if (memberIndex < members.Length)
                {
                    ReflectionReadMember(xmlReader, context, classContract, ref obj, memberIndex, members);
                }
            }

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

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override object? ReflectionReadDictionaryItem(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, CollectionDataContract collectionContract)
        {
            var jsonContext = context as XmlObjectSerializerReadContextComplexJson;
            Debug.Assert(jsonContext != null);
            Debug.Assert(collectionContract.Kind == CollectionKind.Dictionary || collectionContract.Kind == CollectionKind.GenericDictionary);
            context.ReadAttributes(xmlReader);

            var itemContract = XmlObjectSerializerWriteContextComplexJson.GetRevisedItemContract(collectionContract.ItemContract);
            return DataContractJsonSerializer.ReadJsonValue(itemContract, xmlReader, jsonContext);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override bool ReflectionReadSpecialCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, CollectionDataContract collectionContract, object? resultCollection)
        {
            var jsonContext = context as XmlObjectSerializerReadContextComplexJson;
            Debug.Assert(jsonContext != null);

            bool canReadSimpleDictionary = collectionContract.Kind == CollectionKind.Dictionary
                                        || collectionContract.Kind == CollectionKind.GenericDictionary;
            if (canReadSimpleDictionary && jsonContext.UseSimpleDictionaryFormat)
            {
                ReadSimpleDictionary(xmlReader, context, collectionContract, collectionContract.ItemType, resultCollection);
            }

            return false;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void ReadSimpleDictionary(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, CollectionDataContract collectionContract, Type keyValueType, object? dictionary)
        {
            Type[] keyValueTypes = keyValueType.GetGenericArguments();
            Type keyType = keyValueTypes[0];
            Type valueType = keyValueTypes[1];

            int keyTypeNullableDepth = 0;
            while (keyType.IsGenericType && keyType.GetGenericTypeDefinition() == Globals.TypeOfNullable)
            {
                keyTypeNullableDepth++;
                keyType = keyType.GetGenericArguments()[0];
            }

            ClassDataContract keyValueDataContract = (ClassDataContract)collectionContract.ItemContract;
            DataContract keyDataContract = keyValueDataContract.Members![0].MemberTypeContract;

            KeyParseMode keyParseMode = KeyParseMode.Fail;

            if (keyType == Globals.TypeOfString || keyType == Globals.TypeOfObject)
            {
                keyParseMode = KeyParseMode.AsString;
            }
            else if (keyType.IsEnum)
            {
                keyParseMode = KeyParseMode.UsingParseEnum;
            }
            else if (keyDataContract.ParseMethod != null)
            {
                keyParseMode = KeyParseMode.UsingCustomParse;
            }

            if (keyParseMode == KeyParseMode.Fail)
            {
                throw new InvalidOperationException(
                        SR.Format(SR.KeyTypeCannotBeParsedInSimpleDictionary,
                            DataContract.GetClrTypeFullName(collectionContract.UnderlyingType),
                            DataContract.GetClrTypeFullName(keyType))
                    );
            }

            while (true)
            {
                XmlNodeType nodeType = xmlReader.MoveToContent();
                if (nodeType == XmlNodeType.EndElement)
                {
                    return;
                }
                if (nodeType != XmlNodeType.Element)
                {
                    throw XmlObjectSerializerReadContext.CreateUnexpectedStateException(XmlNodeType.Element, xmlReader);
                }

                context.IncrementItemCount(1);
                string keyString = XmlObjectSerializerReadContextComplexJson.GetJsonMemberName(xmlReader);
                object pairKey;

                if (keyParseMode == KeyParseMode.UsingParseEnum)
                {
                    pairKey = Enum.Parse(keyType, keyString);
                }
                else if (keyParseMode == KeyParseMode.UsingCustomParse)
                {
                    TypeCode typeCode = Type.GetTypeCode(keyDataContract.UnderlyingType);
                    pairKey = typeCode switch
                    {
                        TypeCode.Boolean => bool.Parse(keyString),
                        TypeCode.Int16 => short.Parse(keyString),
                        TypeCode.Int32 => int.Parse(keyString),
                        TypeCode.Int64 => long.Parse(keyString),
                        TypeCode.Char => char.Parse(keyString),
                        TypeCode.Byte => byte.Parse(keyString),
                        TypeCode.SByte => sbyte.Parse(keyString),
                        TypeCode.Double => double.Parse(keyString),
                        TypeCode.Decimal => decimal.Parse(keyString),
                        TypeCode.Single => float.Parse(keyString),
                        TypeCode.UInt16 => ushort.Parse(keyString),
                        TypeCode.UInt32 => uint.Parse(keyString),
                        TypeCode.UInt64 => ulong.Parse(keyString),
                        _ => keyDataContract.ParseMethod!.Invoke(null, new object[] { keyString })!,
                    };
                }
                else
                {
                    pairKey = keyString;
                }

                if (keyTypeNullableDepth > 0)
                {
                    throw new NotImplementedException(SR.Format(SR.MustBeGreaterThanZero, keyTypeNullableDepth));
                }

                object? pairValue = ReflectionReadValue(xmlReader, context, valueType, string.Empty, string.Empty);

                Debug.Assert(dictionary != null);
                ((IDictionary)dictionary).Add(pairKey, pairValue);
            }
        }

        private enum KeyParseMode
        {
            Fail,
            AsString,
            UsingParseEnum,
            UsingCustomParse
        }
    }
}
