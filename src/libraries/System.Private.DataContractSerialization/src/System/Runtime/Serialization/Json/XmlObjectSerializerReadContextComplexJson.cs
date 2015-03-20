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

namespace System.Runtime.Serialization.Json
{
    internal class XmlObjectSerializerReadContextComplexJson : XmlObjectSerializerReadContext
    {
        private DataContractJsonSerializer _jsonSerializer;
        private bool _isSerializerKnownDataContractsSetExplicit;
        public XmlObjectSerializerReadContextComplexJson(DataContractJsonSerializer serializer, DataContract rootTypeDataContract)
            : base(null, int.MaxValue, new StreamingContext(), true)
        {
            this.rootTypeDataContract = rootTypeDataContract;
            this.serializerKnownTypeList = serializer.knownTypeList;
            _jsonSerializer = serializer;
        }

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

        internal IList<Type> SerializerKnownTypeList
        {
            get
            {
                return this.serializerKnownTypeList;
            }
        }

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
    }
}
