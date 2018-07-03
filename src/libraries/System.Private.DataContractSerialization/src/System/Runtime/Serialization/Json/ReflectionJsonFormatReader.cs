// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

        public object ReflectionReadClass(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContextComplexJson context, XmlDictionaryString emptyDictionaryString, XmlDictionaryString[] memberNames)
        {
            Debug.Assert(_classContract != null);
            return _reflectionReader.ReflectionReadClass(xmlReader, context, memberNames, null /*memberNamespaces*/, _classContract);
        }
    }

    internal sealed class ReflectionJsonCollectionReader
    {
        private readonly ReflectionReader _reflectionReader = new ReflectionJsonReader();

        public object ReflectionReadCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContextComplexJson context, XmlDictionaryString emptyDictionaryString, XmlDictionaryString itemName, CollectionDataContract collectionContract)
        {
            return _reflectionReader.ReflectionReadCollection(xmlReader, context, itemName, emptyDictionaryString/*itemNamespace*/, collectionContract);
        }

        public void ReflectionReadGetOnlyCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContextComplexJson context, XmlDictionaryString emptyDictionaryString, XmlDictionaryString itemName, CollectionDataContract collectionContract)
        {
            _reflectionReader.ReflectionReadGetOnlyCollection(xmlReader, context, itemName, emptyDictionaryString/*itemNamespace*/, collectionContract);
        }
    }

    internal sealed class ReflectionJsonReader : ReflectionReader
    {
        protected override void ReflectionReadMembers(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces, ClassDataContract classContract, ref object obj)
        {
            var jsonContext = context as XmlObjectSerializerReadContextComplexJson;
            Debug.Assert(jsonContext != null);

            int memberCount = classContract.MemberNames.Length;
            context.IncrementItemCount(memberCount);

            DataMember[] members = new DataMember[memberCount];
            int reflectedMemberCount = ReflectionGetMembers(classContract, members);

            int memberIndex = -1;

            ExtensionDataObject extensionData = null;

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

        protected override object ReflectionReadDictionaryItem(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, CollectionDataContract collectionContract)
        {
            var jsonContext = context as XmlObjectSerializerReadContextComplexJson;
            Debug.Assert(jsonContext != null);
            Debug.Assert(collectionContract.Kind == CollectionKind.Dictionary || collectionContract.Kind == CollectionKind.GenericDictionary);
            context.ReadAttributes(xmlReader);

            var itemContract = XmlObjectSerializerWriteContextComplexJson.GetRevisedItemContract(collectionContract.ItemContract);
            return DataContractJsonSerializer.ReadJsonValue(itemContract, xmlReader, jsonContext);
        }

        protected override bool ReflectionReadSpecialCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, CollectionDataContract collectionContract, object resultCollection)
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

        private void ReadSimpleDictionary(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, CollectionDataContract collectionContract, Type keyValueType, object dictionary)
        {
            Type[] keyValueTypes = keyValueType.GetGenericArguments();
            Type keyType = keyValueTypes[0];
            Type valueType = keyValueTypes[1];

            int keyTypeNullableDepth = 0;
            Type keyTypeOriginal = keyType;
            while (keyType.IsGenericType && keyType.GetGenericTypeDefinition() == Globals.TypeOfNullable)
            {
                keyTypeNullableDepth++;
                keyType = keyType.GetGenericArguments()[0];
            }

            ClassDataContract keyValueDataContract = (ClassDataContract)collectionContract.ItemContract;
            DataContract keyDataContract = keyValueDataContract.Members[0].MemberTypeContract;

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
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new InvalidOperationException(
                        SR.Format(SR.KeyTypeCannotBeParsedInSimpleDictionary,
                            DataContract.GetClrTypeFullName(collectionContract.UnderlyingType),
                            DataContract.GetClrTypeFullName(keyType))
                    ));
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
                    switch (typeCode)
                    {
                        case TypeCode.Boolean:
                            pairKey = bool.Parse(keyString);
                            break;
                        case TypeCode.Int16:
                            pairKey = short.Parse(keyString);
                            break;
                        case TypeCode.Int32:
                            pairKey = int.Parse(keyString);
                            break;
                        case TypeCode.Int64:
                            pairKey = long.Parse(keyString);
                            break;
                        case TypeCode.Char:
                            pairKey = char.Parse(keyString);
                            break;
                        case TypeCode.Byte:
                            pairKey = byte.Parse(keyString);
                            break;
                        case TypeCode.SByte:
                            pairKey = sbyte.Parse(keyString);
                            break;
                        case TypeCode.Double:
                            pairKey = double.Parse(keyString);
                            break;
                        case TypeCode.Decimal:
                            pairKey = decimal.Parse(keyString);
                            break;
                        case TypeCode.Single:
                            pairKey = float.Parse(keyString);
                            break;
                        case TypeCode.UInt16:
                            pairKey = ushort.Parse(keyString);
                            break;
                        case TypeCode.UInt32:
                            pairKey = uint.Parse(keyString);
                            break;
                        case TypeCode.UInt64:
                            pairKey = ulong.Parse(keyString);
                            break;
                        default:
                            pairKey = keyDataContract.ParseMethod.Invoke(null, new object[] { keyString });
                            break;
                    }
                }
                else
                {
                    pairKey = keyString;
                }

                if (keyTypeNullableDepth > 0)
                {
                    throw new NotImplementedException("keyTypeNullableDepth > 0");
                }

                object pairValue = ReflectionReadValue(xmlReader, context, valueType, string.Empty, string.Empty);


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
