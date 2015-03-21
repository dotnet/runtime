// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Reflection;
using System.Collections;
using System.IO;
using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContract>;

namespace System.Runtime.Serialization.Json
{
    internal class XmlObjectSerializerWriteContextComplexJson : XmlObjectSerializerWriteContext
    {
        private DataContractJsonSerializer _jsonSerializer;
        private bool _isSerializerKnownDataContractsSetExplicit;

        public XmlObjectSerializerWriteContextComplexJson(DataContractJsonSerializer serializer, DataContract rootTypeDataContract)
            : base(null, int.MaxValue, new StreamingContext(), true)

        {
            _jsonSerializer = serializer;
            this.rootTypeDataContract = rootTypeDataContract;
            this.serializerKnownTypeList = serializer.knownTypeList;
        }

        internal static XmlObjectSerializerWriteContextComplexJson CreateContext(DataContractJsonSerializer serializer, DataContract rootTypeDataContract)
        {
            return new XmlObjectSerializerWriteContextComplexJson(serializer, rootTypeDataContract);
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

        internal override bool WriteClrTypeInfo(XmlWriterDelegator xmlWriter, string clrTypeName, string clrAssemblyName)
        {
            return false;
        }

        internal override bool WriteClrTypeInfo(XmlWriterDelegator xmlWriter, DataContract dataContract)
        {
            return false;
        }

        internal override void WriteArraySize(XmlWriterDelegator xmlWriter, int size)
        {
            //Noop
        }

        internal static string TruncateDefaultDataContractNamespace(string dataContractNamespace)
        {
            if (!string.IsNullOrEmpty(dataContractNamespace))
            {
                if (dataContractNamespace[0] == '#')
                {
                    return string.Concat("\\", dataContractNamespace);
                }
                else if (dataContractNamespace[0] == '\\')
                {
                    return string.Concat("\\", dataContractNamespace);
                }
                else if (dataContractNamespace.StartsWith(Globals.DataContractXsdBaseNamespace, StringComparison.Ordinal))
                {
                    return string.Concat("#", dataContractNamespace.Substring(JsonGlobals.DataContractXsdBaseNamespaceLength));
                }
            }

            return dataContractNamespace;
        }

        protected override bool WriteTypeInfo(XmlWriterDelegator writer, DataContract contract, DataContract declaredContract)
        {
            if (!((object.ReferenceEquals(contract.Name, declaredContract.Name) &&
                   object.ReferenceEquals(contract.Namespace, declaredContract.Namespace)) ||
                 (contract.Name.Value == declaredContract.Name.Value &&
                 contract.Namespace.Value == declaredContract.Namespace.Value)) &&
                 (contract.UnderlyingType != Globals.TypeOfObjectArray))
            {
                // We always deserialize collections assigned to System.Object as object[]
                // Because of its common and JSON-specific nature, 
                //    we don't want to validate known type information for object[]

                // Return true regardless of whether we actually wrote __type information
                // E.g. We don't write __type information for enums, but we still want verifyKnownType
                //      to be true for them.
                return true;
            }
            return false;
        }



        protected override void WriteDataContractValue(DataContract dataContract, XmlWriterDelegator xmlWriter, object obj, RuntimeTypeHandle declaredTypeHandle)
        {
            _jsonSerializer.WriteObjectInternal(obj, dataContract, this, WriteTypeInfo(null, dataContract, DataContract.GetDataContract(declaredTypeHandle, obj.GetType())), declaredTypeHandle);
        }

        protected override void WriteNull(XmlWriterDelegator xmlWriter)
        {
        }


        internal static void VerifyObjectCompatibilityWithInterface(DataContract contract, object graph, Type declaredType)
        {
            Type contractType = contract.GetType();
            if ((contractType == typeof(XmlDataContract)) && !Globals.TypeOfIXmlSerializable.IsAssignableFrom(declaredType))
            {
                throw System.ServiceModel.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.XmlObjectAssignedToIncompatibleInterface, graph.GetType(), declaredType)));
            }

            if ((contractType == typeof(CollectionDataContract)) && !CollectionDataContract.IsCollectionInterface(declaredType))
            {
                throw System.ServiceModel.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.CollectionAssignedToIncompatibleInterface, graph.GetType(), declaredType)));
            }
        }

        internal void CheckIfTypeNeedsVerifcation(DataContract declaredContract, DataContract runtimeContract)
        {
            if (WriteTypeInfo(null, runtimeContract, declaredContract))
            {
                VerifyType(runtimeContract);
            }
        }

        internal void VerifyType(DataContract dataContract)
        {
            bool knownTypesAddedInCurrentScope = false;
            if (dataContract.KnownDataContracts != null)
            {
                scopedKnownTypes.Push(dataContract.KnownDataContracts);
                knownTypesAddedInCurrentScope = true;
            }

            DataContract knownContract = ResolveDataContractFromKnownTypes(dataContract.StableName.Name, dataContract.StableName.Namespace, null /*memberTypeContract*/);
            if (knownContract == null || knownContract.UnderlyingType != dataContract.UnderlyingType)
            {
                throw System.ServiceModel.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.DcTypeNotFoundOnSerialize, DataContract.GetClrTypeFullName(dataContract.UnderlyingType), dataContract.StableName.Name, dataContract.StableName.Namespace)));
            }

            if (knownTypesAddedInCurrentScope)
            {
                scopedKnownTypes.Pop();
            }
        }
        private ObjectReferenceStack _byValObjectsInScope = new ObjectReferenceStack();
        internal override bool OnHandleReference(XmlWriterDelegator xmlWriter, object obj, bool canContainCyclicReference)
        {
            if (obj.GetType().GetTypeInfo().IsValueType)
            {
                return false;
            }
            if (_byValObjectsInScope.Contains(obj))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.CannotSerializeObjectWithCycles, DataContract.GetClrTypeFullName(obj.GetType()))));
            }
            _byValObjectsInScope.Push(obj);
            return false;
        }

        internal override void OnEndHandleReference(XmlWriterDelegator xmlWriter, object obj, bool canContainCyclicReference)
        {
            if (!obj.GetType().GetTypeInfo().IsValueType)
                _byValObjectsInScope.Pop(obj);
        }

        internal static DataContract GetRevisedItemContract(DataContract oldItemContract)
        {
            if ((oldItemContract != null) &&
                oldItemContract.UnderlyingType.GetTypeInfo().IsGenericType &&
                (oldItemContract.UnderlyingType.GetGenericTypeDefinition() == Globals.TypeOfKeyValue))
            {
                return ClassDataContract.CreateClassDataContractForKeyValue(oldItemContract.UnderlyingType, oldItemContract.Namespace, new string[] { JsonGlobals.KeyString, JsonGlobals.ValueString });
            }
            return oldItemContract;
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
