// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Xml;
using System.Security;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContract>;
#if !NET_NATIVE
using ExtensionDataObject = System.Object;
#endif

namespace System.Runtime.Serialization.Json
{
#if NET_NATIVE
    public class XmlObjectSerializerReadContextComplexJson : XmlObjectSerializerReadContextComplex
#elif MERGE_DCJS
    internal class XmlObjectSerializerReadContextComplexJson : XmlObjectSerializerReadContextComplex
#else
    internal class XmlObjectSerializerReadContextComplexJson : XmlObjectSerializerReadContext
#endif
    {
        private DataContractJsonSerializer _jsonSerializer;
#if !NET_NATIVE && !MERGE_DCJS
        private bool _isSerializerKnownDataContractsSetExplicit;
#endif
#if NET_NATIVE || MERGE_DCJS
        private DateTimeFormat _dateTimeFormat;
        private bool _useSimpleDictionaryFormat;
#endif
        public XmlObjectSerializerReadContextComplexJson(DataContractJsonSerializer serializer, DataContract rootTypeDataContract)
            : base(null, int.MaxValue, new StreamingContext(), true)
        {
            this.rootTypeDataContract = rootTypeDataContract;
            this.serializerKnownTypeList = serializer.KnownTypes;
            _jsonSerializer = serializer;
        }

#if NET_NATIVE || MERGE_DCJS
        internal XmlObjectSerializerReadContextComplexJson(DataContractJsonSerializerImpl serializer, DataContract rootTypeDataContract)
            : base(serializer, serializer.MaxItemsInObjectGraph, new StreamingContext(), false)
        {
            this.rootTypeDataContract = rootTypeDataContract;
            this.serializerKnownTypeList = serializer.knownTypeList;
            _dateTimeFormat = serializer.DateTimeFormat;
            _useSimpleDictionaryFormat = serializer.UseSimpleDictionaryFormat;
        }

        internal static XmlObjectSerializerReadContextComplexJson CreateContext(DataContractJsonSerializerImpl serializer, DataContract rootTypeDataContract)
        {
            return new XmlObjectSerializerReadContextComplexJson(serializer, rootTypeDataContract);
        }

        protected override object ReadDataContractValue(DataContract dataContract, XmlReaderDelegator reader)
        {
            return DataContractJsonSerializerImpl.ReadJsonValue(dataContract, reader, this);
        }

        public int GetJsonMemberIndex(XmlReaderDelegator xmlReader, XmlDictionaryString[] memberNames, int memberIndex, ExtensionDataObject extensionData)
        {
            int length = memberNames.Length;
            if (length != 0)
            {
                for (int i = 0, index = (memberIndex + 1) % length; i < length; i++, index = (index + 1) % length)
                {
                    if (xmlReader.IsStartElement(memberNames[index], XmlDictionaryString.Empty))
                    {
                        return index;
                    }
                }
                string name;
                if (TryGetJsonLocalName(xmlReader, out name))
                {
                    for (int i = 0, index = (memberIndex + 1) % length; i < length; i++, index = (index + 1) % length)
                    {
                        if (memberNames[index].Value == name)
                        {
                            return index;
                        }
                    }
                }
            }
            HandleMemberNotFound(xmlReader, extensionData, memberIndex);
            return length;
        }
#endif

#if !NET_NATIVE && !MERGE_DCJS
        internal override DataContractDictionary SerializerKnownDataContracts
        {
            get
            {
                // This field must be initialized during construction by serializers using data contracts.
                if (!_isSerializerKnownDataContractsSetExplicit)
                {
                    this.serializerKnownDataContracts = _jsonSerializer.KnownDataContracts;
                    _isSerializerKnownDataContractsSetExplicit = true;
                }
                return this.serializerKnownDataContracts;
            }
        }
#endif

        internal IList<Type> SerializerKnownTypeList
        {
            get
            {
                return this.serializerKnownTypeList;
            }
        }

#if NET_NATIVE || MERGE_DCJS
        public bool UseSimpleDictionaryFormat
        {
            get
            {
                return _useSimpleDictionaryFormat;
            }
        }

        internal override void ReadAttributes(XmlReaderDelegator xmlReader)
        {
            if (attributes == null)
                attributes = new Attributes();
            attributes.Reset();

            if (xmlReader.MoveToAttribute(JsonGlobals.typeString) && xmlReader.Value == JsonGlobals.nullString)
            {
                attributes.XsiNil = true;
            }
            else if (xmlReader.MoveToAttribute(JsonGlobals.serverTypeString))
            {
                XmlQualifiedName qualifiedTypeName = JsonReaderDelegator.ParseQualifiedName(xmlReader.Value);
                attributes.XsiTypeName = qualifiedTypeName.Name;

                string serverTypeNamespace = qualifiedTypeName.Namespace;

                if (!string.IsNullOrEmpty(serverTypeNamespace))
                {
                    switch (serverTypeNamespace[0])
                    {
                        case '#':
                            serverTypeNamespace = string.Concat(Globals.DataContractXsdBaseNamespace, serverTypeNamespace.Substring(1));
                            break;
                        case '\\':
                            if (serverTypeNamespace.Length >= 2)
                            {
                                switch (serverTypeNamespace[1])
                                {
                                    case '#':
                                    case '\\':
                                        serverTypeNamespace = serverTypeNamespace.Substring(1);
                                        break;
                                    default:
                                        break;
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }

                attributes.XsiTypeNamespace = serverTypeNamespace;
            }
            xmlReader.MoveToElement();
        }
#endif

        internal DataContract ResolveDataContractFromType(string typeName, string typeNs, DataContract memberTypeContract)
        {
            this.PushKnownTypes(this.rootTypeDataContract);
            this.PushKnownTypes(memberTypeContract);
            XmlQualifiedName qname = ParseQualifiedName(typeName);
            DataContract contract = ResolveDataContractFromKnownTypes(qname.Name, TrimNamespace(qname.Namespace), memberTypeContract);

            this.PopKnownTypes(this.rootTypeDataContract);
            this.PopKnownTypes(memberTypeContract);
            return contract;
        }

        internal void CheckIfTypeNeedsVerifcation(DataContract declaredContract, DataContract runtimeContract)
        {
            bool verifyType = true;
            CollectionDataContract collectionContract = declaredContract as CollectionDataContract;
            if (collectionContract != null && collectionContract.UnderlyingType.GetTypeInfo().IsInterface)
            {
                switch (collectionContract.Kind)
                {
                    case CollectionKind.Dictionary:
                    case CollectionKind.GenericDictionary:
                        verifyType = declaredContract.Name == runtimeContract.Name;
                        break;

                    default:
                        Type t = collectionContract.ItemType.MakeArrayType();
                        verifyType = (t != runtimeContract.UnderlyingType);
                        break;
                }
            }

            if (verifyType)
            {
                this.PushKnownTypes(declaredContract);
                VerifyType(runtimeContract);
                this.PopKnownTypes(declaredContract);
            }
        }

        internal void VerifyType(DataContract dataContract)
        {
            DataContract knownContract = ResolveDataContractFromKnownTypes(dataContract.StableName.Name, dataContract.StableName.Namespace, null /*memberTypeContract*/);
            if (knownContract == null || knownContract.UnderlyingType != dataContract.UnderlyingType)
            {
                throw System.ServiceModel.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.DcTypeNotFoundOnSerialize, DataContract.GetClrTypeFullName(dataContract.UnderlyingType), dataContract.StableName.Name, dataContract.StableName.Namespace)));
            }
        }

        internal string TrimNamespace(string serverTypeNamespace)
        {
            if (!string.IsNullOrEmpty(serverTypeNamespace))
            {
                switch (serverTypeNamespace[0])
                {
                    case '#':
                        serverTypeNamespace = string.Concat(Globals.DataContractXsdBaseNamespace, serverTypeNamespace.Substring(1));
                        break;
                    case '\\':
                        if (serverTypeNamespace.Length >= 2)
                        {
                            switch (serverTypeNamespace[1])
                            {
                                case '#':
                                case '\\':
                                    serverTypeNamespace = serverTypeNamespace.Substring(1);
                                    break;
                                default:
                                    break;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            return serverTypeNamespace;
        }

        internal static XmlQualifiedName ParseQualifiedName(string qname)
        {
            string name, ns;
            if (string.IsNullOrEmpty(qname))
            {
                name = ns = String.Empty;
            }
            else
            {
                qname = qname.Trim();
                int colon = qname.IndexOf(':');
                if (colon >= 0)
                {
                    name = qname.Substring(0, colon);
                    ns = qname.Substring(colon + 1);
                }
                else
                {
                    name = qname;
                    ns = string.Empty;
                }
            }
            return new XmlQualifiedName(name, ns);
        }
        internal override DataContract GetDataContract(RuntimeTypeHandle typeHandle, Type type)
        {
            DataContract dataContract = base.GetDataContract(typeHandle, type);
            DataContractJsonSerializer.CheckIfTypeIsReference(dataContract);
            return dataContract;
        }

        internal override DataContract GetDataContractSkipValidation(int typeId, RuntimeTypeHandle typeHandle, Type type)
        {
            DataContract dataContract = base.GetDataContractSkipValidation(typeId, typeHandle, type);
            DataContractJsonSerializer.CheckIfTypeIsReference(dataContract);
            return dataContract;
        }

        internal override DataContract GetDataContract(int id, RuntimeTypeHandle typeHandle)
        {
            DataContract dataContract = base.GetDataContract(id, typeHandle);
            DataContractJsonSerializer.CheckIfTypeIsReference(dataContract);
            return dataContract;
        }

#if NET_NATIVE || MERGE_DCJS
        internal static bool TryGetJsonLocalName(XmlReaderDelegator xmlReader, out string name)
        {
            if (xmlReader.IsStartElement(JsonGlobals.itemDictionaryString, JsonGlobals.itemDictionaryString))
            {
                if (xmlReader.MoveToAttribute(JsonGlobals.itemString))
                {
                    name = xmlReader.Value;
                    return true;
                }
            }
            name = null;
            return false;
        }

        public static string GetJsonMemberName(XmlReaderDelegator xmlReader)
        {
            string name;
            if (!TryGetJsonLocalName(xmlReader, out name))
            {
                name = xmlReader.LocalName;
            }
            return name;
        }
#endif

#if !NET_NATIVE && MERGE_DCJS
        public static void ThrowDuplicateMemberException(object obj, XmlDictionaryString[] memberNames, int memberIndex)
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SerializationException(
                SR.Format(SR.JsonDuplicateMemberInInput, DataContract.GetClrTypeFullName(obj.GetType()), memberNames[memberIndex])));
        }

        public static void ThrowMissingRequiredMembers(object obj, XmlDictionaryString[] memberNames, byte[] expectedElements, byte[] requiredElements)
        {
            StringBuilder stringBuilder = new StringBuilder();
            int missingMembersCount = 0;
            for (int i = 0; i < memberNames.Length; i++)
            {
                if (IsBitSet(expectedElements, i) && IsBitSet(requiredElements, i))
                {
                    if (stringBuilder.Length != 0)
                        stringBuilder.Append(", ");
                    stringBuilder.Append(memberNames[i]);
                    missingMembersCount++;
                }
            }

            if (missingMembersCount == 1)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SerializationException(SR.Format(
                 SR.JsonOneRequiredMemberNotFound, DataContract.GetClrTypeFullName(obj.GetType()), stringBuilder.ToString())));
            }
            else
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SerializationException(SR.Format(
                    SR.JsonRequiredMembersNotFound, DataContract.GetClrTypeFullName(obj.GetType()), stringBuilder.ToString())));
            }
        }

        [SecuritySafeCritical]
        private static bool IsBitSet(byte[] bytes, int bitIndex)
        {
            return BitFlagsGenerator.IsBitSet(bytes, bitIndex);
        }
#endif
    }
}
