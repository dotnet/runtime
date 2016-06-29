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
    internal sealed class ReflectionJsonFormatReader : ReflectionReader
    {
        

        private XmlObjectSerializerReadContextComplexJson _jsonContextArg;

        public ReflectionJsonFormatReader(ClassDataContract classDataContract) : base(classDataContract)
        {

        }

        public ReflectionJsonFormatReader(CollectionDataContract collectionContract) : base(collectionContract)
        {
        }

        public object ReflectionReadClass(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContextComplexJson context, XmlDictionaryString emptyDictionaryString, XmlDictionaryString[] memberNames)
        {

            _jsonContextArg = context;
            return ReflectionReadClassInternal(xmlReader, context, memberNames, null /*memberNamespaces*/);
        }

        public object ReflectionReadCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContextComplexJson context, XmlDictionaryString emptyDictionaryString, XmlDictionaryString itemName, CollectionDataContract collectionContract)
        {
            _jsonContextArg = context;
            return ReflectionReadCollectionInternal(xmlReader, context, itemName, emptyDictionaryString/*itemNamespace*/, collectionContract);
        }

        public void ReflectionReadGetOnlyCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContextComplexJson context, XmlDictionaryString emptyDictionaryString, XmlDictionaryString itemName, CollectionDataContract collectionContract)
        {
            _jsonContextArg = context;
            ReflectionReadGetOnlyCollectionInternal(xmlReader, context, itemName, emptyDictionaryString/*itemNamespace*/, collectionContract);
        }

        protected override void ReflectionReadMembers(ref object obj)
        {
            var classContract = _classContract;

            int memberCount = classContract.MemberNames.Length;
            _contextArg.IncrementItemCount(memberCount);

            DataMember[] members = new DataMember[memberCount];
            int reflectedMemberCount = ReflectionGetMembers(_classContract, members);

            int memberIndex = -1;
            while (true)
            {
                if (!XmlObjectSerializerReadContext.MoveToNextElement(_xmlReaderArg))
                {
                    return;
                }

                memberIndex = _jsonContextArg.GetJsonMemberIndex(_xmlReaderArg, _memberNamesArg, memberIndex, extensionData: null);
                // GetMemberIndex returns memberNames.Length if member not found
                if (memberIndex < members.Length)
                {
                    ReflectionReadMember(ref obj, memberIndex, _xmlReaderArg, _contextArg, members);
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

        protected override object ReflectionReadDictionaryItem(CollectionDataContract collectionContract)
        {
            Debug.Assert(collectionContract.Kind == CollectionKind.Dictionary || collectionContract.Kind == CollectionKind.GenericDictionary);
            _contextArg.ReadAttributes(_xmlReaderArg);

            // We need a test for GetRevisedItemContract.
            //var itemContract = XmlObjectSerializerWriteContextComplexJson.GetRevisedItemContract(collectionContract.ItemContract);
            var itemContract = collectionContract.ItemContract;
            return DataContractJsonSerializer.ReadJsonValue(itemContract, _xmlReaderArg, _jsonContextArg);
        }

        protected override bool ReflectionReadSpecialCollection(CollectionDataContract collectionContract, object resultCollection)
        {
            bool canReadSimpleDictionary = collectionContract.Kind == CollectionKind.Dictionary
                                        || collectionContract.Kind == CollectionKind.GenericDictionary;
            if (canReadSimpleDictionary && _jsonContextArg.UseSimpleDictionaryFormat)
            {
                ReadSimpleDictionary(collectionContract, collectionContract.ItemType, resultCollection);
            }

            return false;
        }

        private void ReadSimpleDictionary(CollectionDataContract collectionContract, Type keyValueType, object dictionary)
        {
            Type[] keyValueTypes = keyValueType.GetGenericArguments();
            Type keyType = keyValueTypes[0];
            Type valueType = keyValueTypes[1];

            int keyTypeNullableDepth = 0;
            Type keyTypeOriginal = keyType;
            while (keyType.GetTypeInfo().IsGenericType && keyType.GetGenericTypeDefinition() == Globals.TypeOfNullable)
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
            else if (keyType.GetTypeInfo().IsEnum)
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
                XmlNodeType nodeType = _xmlReaderArg.MoveToContent();
                if (nodeType == XmlNodeType.EndElement)
                {
                    return;
                }
                if (nodeType != XmlNodeType.Element)
                {
                    throw XmlObjectSerializerReadContext.CreateUnexpectedStateException(XmlNodeType.Element, _xmlReaderArg);
                }

                _contextArg.IncrementItemCount(1);
                string keyString = XmlObjectSerializerReadContextComplexJson.GetJsonMemberName(_xmlReaderArg);
                object pairKey;

                if (keyParseMode == KeyParseMode.UsingParseEnum)
                {
                    pairKey = Enum.Parse(keyType, keyString);
                }
                else if (keyParseMode == KeyParseMode.UsingCustomParse)
                {
                    pairKey = keyDataContract.ParseMethod.Invoke(null, new object[] { keyString });
                }
                else
                {
                    pairKey = keyString;
                }

                if (keyTypeNullableDepth > 0)
                {
                    throw new NotImplementedException("keyTypeNullableDepth > 0");
                }

                object pairValue = ReflectionReadValue(valueType, string.Empty, string.Empty);


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
