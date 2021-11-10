// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Security;
using System.Runtime.CompilerServices;
using ExtensionDataObject = System.Object;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization
{
    internal class XmlObjectSerializerWriteContext : XmlObjectSerializerContext
    {
        private ObjectReferenceStack _byValObjectsInScope;
        private XmlSerializableWriter? _xmlSerializableWriter;
        private const int depthToCheckCyclicReference = 512;
        private ObjectToIdCache? _serializedObjects;
        private bool _isGetOnlyCollection;
        private readonly bool _unsafeTypeForwardingEnabled;
        protected bool serializeReadOnlyTypes;
        protected bool preserveObjectReferences;

        internal static XmlObjectSerializerWriteContext CreateContext(DataContractSerializer serializer, DataContract rootTypeDataContract, DataContractResolver? dataContractResolver)
        {
            return (serializer.PreserveObjectReferences || serializer.SerializationSurrogateProvider != null)
                ? new XmlObjectSerializerWriteContextComplex(serializer, rootTypeDataContract, dataContractResolver)
                : new XmlObjectSerializerWriteContext(serializer, rootTypeDataContract, dataContractResolver);
        }

        protected XmlObjectSerializerWriteContext(DataContractSerializer serializer, DataContract rootTypeDataContract, DataContractResolver? resolver)
            : base(serializer, rootTypeDataContract, resolver)
        {
            this.serializeReadOnlyTypes = serializer.SerializeReadOnlyTypes;
            // Known types restricts the set of types that can be deserialized
            _unsafeTypeForwardingEnabled = true;
        }

        internal XmlObjectSerializerWriteContext(XmlObjectSerializer serializer, int maxItemsInObjectGraph, StreamingContext streamingContext, bool ignoreExtensionDataObject)
            : base(serializer, maxItemsInObjectGraph, streamingContext, ignoreExtensionDataObject)
        {
            // Known types restricts the set of types that can be deserialized
            _unsafeTypeForwardingEnabled = true;
        }

        protected ObjectToIdCache SerializedObjects
        {
            get
            {
                if (_serializedObjects == null)
                    _serializedObjects = new ObjectToIdCache();
                return _serializedObjects;
            }
        }

        internal override bool IsGetOnlyCollection
        {
            get { return _isGetOnlyCollection; }
            set { _isGetOnlyCollection = value; }
        }

        internal bool SerializeReadOnlyTypes
        {
            get { return this.serializeReadOnlyTypes; }
        }

        internal bool UnsafeTypeForwardingEnabled
        {
            get { return _unsafeTypeForwardingEnabled; }
        }

        internal void StoreIsGetOnlyCollection()
        {
            _isGetOnlyCollection = true;
        }

        internal void ResetIsGetOnlyCollection()
        {
            _isGetOnlyCollection = false;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void InternalSerializeReference(XmlWriterDelegator xmlWriter, object obj, bool isDeclaredType, bool writeXsiType, int declaredTypeID, RuntimeTypeHandle declaredTypeHandle)
        {
            if (!OnHandleReference(xmlWriter, obj, true /*canContainCyclicReference*/))
                InternalSerialize(xmlWriter, obj, isDeclaredType, writeXsiType, declaredTypeID, declaredTypeHandle);
            OnEndHandleReference(xmlWriter, obj, true /*canContainCyclicReference*/);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual void InternalSerialize(XmlWriterDelegator xmlWriter, object obj, bool isDeclaredType, bool writeXsiType, int declaredTypeID, RuntimeTypeHandle declaredTypeHandle)
        {
            if (writeXsiType)
            {
                Type declaredType = Globals.TypeOfObject;
                SerializeWithXsiType(xmlWriter, obj, obj.GetType().TypeHandle, null/*type*/, -1, declaredType.TypeHandle, declaredType);
            }
            else if (isDeclaredType)
            {
                DataContract contract = GetDataContract(declaredTypeID, declaredTypeHandle);
                SerializeWithoutXsiType(contract, xmlWriter, obj, declaredTypeHandle);
            }
            else
            {
                RuntimeTypeHandle objTypeHandle = obj.GetType().TypeHandle;
                if (declaredTypeHandle.GetHashCode() == objTypeHandle.GetHashCode()) // semantically the same as Value == Value; Value is not available in SL
                {
                    DataContract dataContract = (declaredTypeID >= 0)
                        ? GetDataContract(declaredTypeID, declaredTypeHandle)
                        : GetDataContract(declaredTypeHandle, null /*type*/);
                    SerializeWithoutXsiType(dataContract, xmlWriter, obj, declaredTypeHandle);
                }
                else
                {
                    SerializeWithXsiType(xmlWriter, obj, objTypeHandle, null /*type*/, declaredTypeID, declaredTypeHandle, Type.GetTypeFromHandle(declaredTypeHandle)!);
                }
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void SerializeWithoutXsiType(DataContract dataContract, XmlWriterDelegator xmlWriter, object obj, RuntimeTypeHandle declaredTypeHandle)
        {
            if (OnHandleIsReference(xmlWriter, dataContract, obj))
                return;
            if (dataContract.KnownDataContracts != null)
            {
                scopedKnownTypes.Push(dataContract.KnownDataContracts);
                WriteDataContractValue(dataContract, xmlWriter, obj, declaredTypeHandle);
                scopedKnownTypes.Pop();
            }
            else
            {
                WriteDataContractValue(dataContract, xmlWriter, obj, declaredTypeHandle);
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual void SerializeWithXsiTypeAtTopLevel(DataContract dataContract, XmlWriterDelegator xmlWriter, object obj, RuntimeTypeHandle originalDeclaredTypeHandle, Type graphType)
        {
            Debug.Assert(rootTypeDataContract != null);

            bool verifyKnownType = false;
            Type declaredType = rootTypeDataContract.UnderlyingType;

            if (declaredType.IsInterface && CollectionDataContract.IsCollectionInterface(declaredType))
            {
                if (DataContractResolver != null)
                {
                    WriteResolvedTypeInfo(xmlWriter, graphType, declaredType);
                }
            }
            else if (!declaredType.IsArray) //Array covariance is not supported in XSD. If declared type is array do not write xsi:type. Instead write xsi:type for each item
            {
                verifyKnownType = WriteTypeInfo(xmlWriter, dataContract, rootTypeDataContract);
            }
            SerializeAndVerifyType(dataContract, xmlWriter, obj, verifyKnownType, originalDeclaredTypeHandle, declaredType);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected virtual void SerializeWithXsiType(XmlWriterDelegator xmlWriter, object obj, RuntimeTypeHandle objectTypeHandle, Type? objectType, int declaredTypeID, RuntimeTypeHandle declaredTypeHandle, Type declaredType)
        {
            bool verifyKnownType = false;
            DataContract dataContract;
            if (declaredType.IsInterface && CollectionDataContract.IsCollectionInterface(declaredType))
            {
                dataContract = GetDataContractSkipValidation(DataContract.GetId(objectTypeHandle), objectTypeHandle, objectType);
                if (OnHandleIsReference(xmlWriter, dataContract, obj))
                    return;
                dataContract = GetDataContract(declaredTypeHandle, declaredType);
                if (!WriteClrTypeInfo(xmlWriter, dataContract) && DataContractResolver != null)
                {
                    if (objectType == null)
                    {
                        objectType = Type.GetTypeFromHandle(objectTypeHandle)!;
                    }
                    WriteResolvedTypeInfo(xmlWriter, objectType, declaredType);
                }
            }
            else if (declaredType.IsArray)//Array covariance is not supported in XSD. If declared type is array do not write xsi:type. Instead write xsi:type for each item
            {
                // A call to OnHandleIsReference is not necessary here -- arrays cannot be IsReference
                dataContract = GetDataContract(objectTypeHandle, objectType);
                WriteClrTypeInfo(xmlWriter, dataContract);
                dataContract = GetDataContract(declaredTypeHandle, declaredType);
            }
            else
            {
                dataContract = GetDataContract(objectTypeHandle, objectType);
                if (OnHandleIsReference(xmlWriter, dataContract, obj))
                    return;
                if (!WriteClrTypeInfo(xmlWriter, dataContract))
                {
                    DataContract declaredTypeContract = (declaredTypeID >= 0)
                        ? GetDataContract(declaredTypeID, declaredTypeHandle)
                        : GetDataContract(declaredTypeHandle, declaredType);
                    verifyKnownType = WriteTypeInfo(xmlWriter, dataContract, declaredTypeContract);
                }
            }

            SerializeAndVerifyType(dataContract, xmlWriter, obj, verifyKnownType, declaredTypeHandle, declaredType);
        }

        internal bool OnHandleIsReference(XmlWriterDelegator xmlWriter, DataContract contract, object obj)
        {
            if (preserveObjectReferences || !contract.IsReference || _isGetOnlyCollection)
            {
                return false;
            }

            bool isNew = true;
            int objectId = SerializedObjects.GetId(obj, ref isNew);
            _byValObjectsInScope.EnsureSetAsIsReference(obj);
            if (isNew)
            {
                xmlWriter.WriteAttributeString(Globals.SerPrefix, DictionaryGlobals.IdLocalName,
                                            DictionaryGlobals.SerializationNamespace, string.Create(CultureInfo.InvariantCulture, $"i{objectId}"));
                return false;
            }
            else
            {
                xmlWriter.WriteAttributeString(Globals.SerPrefix, DictionaryGlobals.RefLocalName, DictionaryGlobals.SerializationNamespace, string.Create(CultureInfo.InvariantCulture, $"i{objectId}"));
                return true;
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected void SerializeAndVerifyType(DataContract dataContract, XmlWriterDelegator xmlWriter, object obj, bool verifyKnownType, RuntimeTypeHandle declaredTypeHandle, Type declaredType)
        {
            bool knownTypesAddedInCurrentScope = false;
            if (dataContract.KnownDataContracts != null)
            {
                scopedKnownTypes.Push(dataContract.KnownDataContracts);
                knownTypesAddedInCurrentScope = true;
            }

            if (verifyKnownType)
            {
                if (!IsKnownType(dataContract, declaredType))
                {
                    DataContract? knownContract = ResolveDataContractFromKnownTypes(dataContract.StableName.Name, dataContract.StableName.Namespace, null /*memberTypeContract*/, declaredType);
                    if (knownContract == null || knownContract.UnderlyingType != dataContract.UnderlyingType)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.DcTypeNotFoundOnSerialize, DataContract.GetClrTypeFullName(dataContract.UnderlyingType), dataContract.StableName.Name, dataContract.StableName.Namespace)));
                    }
                }
            }

            WriteDataContractValue(dataContract, xmlWriter, obj, declaredTypeHandle);

            if (knownTypesAddedInCurrentScope)
            {
                scopedKnownTypes.Pop();
            }
        }

        internal virtual bool WriteClrTypeInfo(XmlWriterDelegator xmlWriter, DataContract dataContract)
        {
            return false;
        }

        internal virtual bool WriteClrTypeInfo(XmlWriterDelegator xmlWriter, string clrTypeName, string clrAssemblyName)
        {
            return false;
        }

        internal virtual bool WriteClrTypeInfo(XmlWriterDelegator xmlWriter, Type dataContractType, string? clrTypeName, string? clrAssemblyName)
        {
            return false;
        }

        internal virtual bool WriteClrTypeInfo(XmlWriterDelegator xmlWriter, Type dataContractType, SerializationInfo serInfo)
        {
            return false;
        }

        internal virtual void WriteAnyType(XmlWriterDelegator xmlWriter, object value)
        {
            xmlWriter.WriteAnyType(value);
        }

        internal virtual void WriteString(XmlWriterDelegator xmlWriter, string value)
        {
            xmlWriter.WriteString(value);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual void WriteString(XmlWriterDelegator xmlWriter, string? value, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            if (value == null)
                WriteNull(xmlWriter, typeof(string), true/*isMemberTypeSerializable*/, name, ns);
            else
            {
                xmlWriter.WriteStartElementPrimitive(name, ns);
                xmlWriter.WriteString(value);
                xmlWriter.WriteEndElementPrimitive();
            }
        }

        internal virtual void WriteBase64(XmlWriterDelegator xmlWriter, byte[] value)
        {
            xmlWriter.WriteBase64(value);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual void WriteBase64(XmlWriterDelegator xmlWriter, byte[] value, XmlDictionaryString name, XmlDictionaryString ns)
        {
            if (value == null)
                WriteNull(xmlWriter, typeof(byte[]), true/*isMemberTypeSerializable*/, name, ns);
            else
            {
                xmlWriter.WriteStartElementPrimitive(name, ns);
                xmlWriter.WriteBase64(value);
                xmlWriter.WriteEndElementPrimitive();
            }
        }

        internal virtual void WriteUri(XmlWriterDelegator xmlWriter, Uri value)
        {
            xmlWriter.WriteUri(value);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual void WriteUri(XmlWriterDelegator xmlWriter, Uri value, XmlDictionaryString name, XmlDictionaryString ns)
        {
            if (value == null)
                WriteNull(xmlWriter, typeof(Uri), true/*isMemberTypeSerializable*/, name, ns);
            else
            {
                xmlWriter.WriteStartElementPrimitive(name, ns);
                xmlWriter.WriteUri(value);
                xmlWriter.WriteEndElementPrimitive();
            }
        }

        internal virtual void WriteQName(XmlWriterDelegator xmlWriter, XmlQualifiedName value)
        {
            xmlWriter.WriteQName(value);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual void WriteQName(XmlWriterDelegator xmlWriter, XmlQualifiedName? value, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            if (value == null)
                WriteNull(xmlWriter, typeof(XmlQualifiedName), true/*isMemberTypeSerializable*/, name, ns);
            else
            {
                if (ns != null && ns.Value != null && ns.Value.Length > 0)
                    xmlWriter.WriteStartElement(Globals.ElementPrefix, name, ns);
                else
                    xmlWriter.WriteStartElement(name, ns);
                xmlWriter.WriteQName(value);
                xmlWriter.WriteEndElement();
            }
        }

        internal void HandleGraphAtTopLevel(XmlWriterDelegator writer, object obj, DataContract contract)
        {
            writer.WriteXmlnsAttribute(Globals.XsiPrefix, DictionaryGlobals.SchemaInstanceNamespace);
            if (contract.IsISerializable)
            {
                writer.WriteXmlnsAttribute(Globals.XsdPrefix, DictionaryGlobals.SchemaNamespace);
            }

            OnHandleReference(writer, obj, true /*canContainReferences*/);
        }

        internal virtual bool OnHandleReference(XmlWriterDelegator xmlWriter, object obj, bool canContainCyclicReference)
        {
            if (xmlWriter.depth < depthToCheckCyclicReference)
                return false;
            if (canContainCyclicReference)
            {
                if (_byValObjectsInScope.Contains(obj))
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.CannotSerializeObjectWithCycles, DataContract.GetClrTypeFullName(obj.GetType()))));
                _byValObjectsInScope.Push(obj);
            }
            return false;
        }

        internal virtual void OnEndHandleReference(XmlWriterDelegator xmlWriter, object obj, bool canContainCyclicReference)
        {
            if (xmlWriter.depth < depthToCheckCyclicReference)
                return;
            if (canContainCyclicReference)
            {
                _byValObjectsInScope.Pop(obj);
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void WriteNull(XmlWriterDelegator xmlWriter, Type memberType, bool isMemberTypeSerializable)
        {
            CheckIfTypeSerializable(memberType, isMemberTypeSerializable);
            WriteNull(xmlWriter);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void WriteNull(XmlWriterDelegator xmlWriter, Type memberType, bool isMemberTypeSerializable, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            xmlWriter.WriteStartElement(name, ns);
            WriteNull(xmlWriter, memberType, isMemberTypeSerializable);
            xmlWriter.WriteEndElement();
        }

        internal void IncrementArrayCount(XmlWriterDelegator xmlWriter, Array array)
        {
            IncrementCollectionCount(xmlWriter, array.GetLength(0));
        }

        internal void IncrementCollectionCount(XmlWriterDelegator xmlWriter, ICollection collection)
        {
            IncrementCollectionCount(xmlWriter, collection.Count);
        }

        internal void IncrementCollectionCountGeneric<T>(XmlWriterDelegator xmlWriter, ICollection<T> collection)
        {
            IncrementCollectionCount(xmlWriter, collection.Count);
        }

        private void IncrementCollectionCount(XmlWriterDelegator xmlWriter, int size)
        {
            IncrementItemCount(size);
            WriteArraySize(xmlWriter, size);
        }

        internal virtual void WriteArraySize(XmlWriterDelegator xmlWriter, int size)
        {
        }

        internal static bool IsMemberTypeSameAsMemberValue(object obj, Type memberType)
        {
            if (obj == null || memberType == null)
                return false;

            return obj.GetType().TypeHandle.Equals(memberType.TypeHandle);
        }

        internal static T GetDefaultValue<T>()
        {
            return default(T)!;
        }

        internal static T GetNullableValue<T>(Nullable<T> value) where T : struct
        {
            // value.Value will throw if hasValue is false
            return value!.Value;
        }

        internal static void ThrowRequiredMemberMustBeEmitted(string memberName, Type type)
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SerializationException(SR.Format(SR.RequiredMemberMustBeEmitted, memberName, type.FullName)));
        }

        internal static bool GetHasValue<T>(Nullable<T> value) where T : struct
        {
            return value.HasValue;
        }

        internal void WriteIXmlSerializable(XmlWriterDelegator xmlWriter, object obj)
        {
            if (_xmlSerializableWriter == null)
                _xmlSerializableWriter = new XmlSerializableWriter();
            WriteIXmlSerializable(xmlWriter, obj, _xmlSerializableWriter);
        }

        internal static void WriteRootIXmlSerializable(XmlWriterDelegator xmlWriter, object obj)
        {
            WriteIXmlSerializable(xmlWriter, obj, new XmlSerializableWriter());
        }

        private static void WriteIXmlSerializable(XmlWriterDelegator xmlWriter, object obj, XmlSerializableWriter xmlSerializableWriter)
        {
            xmlSerializableWriter.BeginWrite(xmlWriter.Writer, obj);
            IXmlSerializable? xmlSerializable = obj as IXmlSerializable;
            if (xmlSerializable != null)
                xmlSerializable.WriteXml(xmlSerializableWriter);
            else
            {
                XmlElement? xmlElement = obj as XmlElement;
                if (xmlElement != null)
                    xmlElement.WriteTo(xmlSerializableWriter);
                else
                {
                    XmlNode[]? xmlNodes = obj as XmlNode[];
                    if (xmlNodes != null)
                        foreach (XmlNode xmlNode in xmlNodes)
                            xmlNode.WriteTo(xmlSerializableWriter);
                    else
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.UnknownXmlType, DataContract.GetClrTypeFullName(obj.GetType()))));
                }
            }
            xmlSerializableWriter.EndWrite();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void GetObjectData(ISerializable obj, SerializationInfo serInfo, StreamingContext context)
        {
            obj.GetObjectData(serInfo, context);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void WriteISerializable(XmlWriterDelegator xmlWriter, ISerializable obj)
        {
            Type objType = obj.GetType();
            var serInfo = new SerializationInfo(objType, XmlObjectSerializer.FormatterConverter /*!UnsafeTypeForwardingEnabled is always false*/);
            GetObjectData(obj, serInfo, GetStreamingContext());

            if (!UnsafeTypeForwardingEnabled && serInfo.AssemblyName == Globals.MscorlibAssemblyName)
            {
                // Throw if a malicious type tries to set its assembly name to "0" to get deserialized in mscorlib
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ISerializableAssemblyNameSetToZero, DataContract.GetClrTypeFullName(obj.GetType()))));
            }

            WriteSerializationInfo(xmlWriter, objType, serInfo);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void WriteSerializationInfo(XmlWriterDelegator xmlWriter, Type objType, SerializationInfo serInfo)
        {
            if (DataContract.GetClrTypeFullName(objType) != serInfo.FullTypeName)
            {
                if (DataContractResolver != null)
                {
                    XmlDictionaryString? typeName, typeNs;
                    if (ResolveType(serInfo.ObjectType, objType, out typeName, out typeNs))
                    {
                        xmlWriter.WriteAttributeQualifiedName(Globals.SerPrefix, DictionaryGlobals.ISerializableFactoryTypeLocalName, DictionaryGlobals.SerializationNamespace, typeName, typeNs);
                    }
                }
                else
                {
                    string typeName, typeNs;
                    DataContract.GetDefaultStableName(serInfo.FullTypeName, out typeName, out typeNs);
                    xmlWriter.WriteAttributeQualifiedName(Globals.SerPrefix, DictionaryGlobals.ISerializableFactoryTypeLocalName, DictionaryGlobals.SerializationNamespace, DataContract.GetClrTypeString(typeName), DataContract.GetClrTypeString(typeNs));
                }
            }

            WriteClrTypeInfo(xmlWriter, objType, serInfo);
            IncrementItemCount(serInfo.MemberCount);
            foreach (SerializationEntry serEntry in serInfo)
            {
                XmlDictionaryString name = DataContract.GetClrTypeString(DataContract.EncodeLocalName(serEntry.Name));
                xmlWriter.WriteStartElement(name, DictionaryGlobals.EmptyString);
                object? obj = serEntry.Value;
                if (obj == null)
                {
                    WriteNull(xmlWriter);
                }
                else
                {
                    InternalSerializeReference(xmlWriter, obj, false /*isDeclaredType*/, false /*writeXsiType*/, -1, Globals.TypeOfObject.TypeHandle);
                }

                xmlWriter.WriteEndElement();
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected virtual void WriteDataContractValue(DataContract dataContract, XmlWriterDelegator xmlWriter, object obj, RuntimeTypeHandle declaredTypeHandle)
        {
            dataContract.WriteXmlValue(xmlWriter, obj, this);
        }

        protected virtual void WriteNull(XmlWriterDelegator xmlWriter)
        {
            XmlObjectSerializer.WriteNull(xmlWriter);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void WriteResolvedTypeInfo(XmlWriterDelegator writer, Type objectType, Type declaredType)
        {
            XmlDictionaryString? typeName, typeNamespace;
            if (ResolveType(objectType, declaredType, out typeName, out typeNamespace))
            {
                WriteTypeInfo(writer, typeName, typeNamespace);
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private bool ResolveType(Type objectType, Type declaredType, [NotNullWhen(true)] out XmlDictionaryString? typeName, [NotNullWhen(true)] out XmlDictionaryString? typeNamespace)
        {
            Debug.Assert(DataContractResolver != null);

            if (!DataContractResolver.TryResolveType(objectType, declaredType, KnownTypeResolver, out typeName, out typeNamespace))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ResolveTypeReturnedFalse, DataContract.GetClrTypeFullName(DataContractResolver.GetType()), DataContract.GetClrTypeFullName(objectType))));
            }
            if (typeName == null)
            {
                if (typeNamespace == null)
                {
                    return false;
                }
                else
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ResolveTypeReturnedNull, DataContract.GetClrTypeFullName(DataContractResolver.GetType()), DataContract.GetClrTypeFullName(objectType))));
                }
            }
            if (typeNamespace == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ResolveTypeReturnedNull, DataContract.GetClrTypeFullName(DataContractResolver.GetType()), DataContract.GetClrTypeFullName(objectType))));
            }
            return true;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected virtual bool WriteTypeInfo(XmlWriterDelegator writer, DataContract contract, DataContract declaredContract)
        {
            if (!XmlObjectSerializer.IsContractDeclared(contract, declaredContract))
            {
                if (DataContractResolver == null)
                {
                    WriteTypeInfo(writer, contract.Name, contract.Namespace);
                    return true;
                }
                else
                {
                    WriteResolvedTypeInfo(writer, contract.OriginalUnderlyingType, declaredContract.OriginalUnderlyingType);
                    return false;
                }
            }
            return false;
        }

        protected virtual void WriteTypeInfo(XmlWriterDelegator writer, string dataContractName, string? dataContractNamespace)
        {
            writer.WriteAttributeQualifiedName(Globals.XsiPrefix, DictionaryGlobals.XsiTypeLocalName, DictionaryGlobals.SchemaInstanceNamespace, dataContractName, dataContractNamespace);
        }

        protected virtual void WriteTypeInfo(XmlWriterDelegator writer, XmlDictionaryString dataContractName, XmlDictionaryString dataContractNamespace)
        {
            writer.WriteAttributeQualifiedName(Globals.XsiPrefix, DictionaryGlobals.XsiTypeLocalName, DictionaryGlobals.SchemaInstanceNamespace, dataContractName, dataContractNamespace);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void WriteExtensionData(XmlWriterDelegator xmlWriter, ExtensionDataObject? extensionData, int memberIndex)
        {
            if (IgnoreExtensionDataObject || extensionData == null)
                return;

            IList<ExtensionDataMember>? members = extensionData.Members;
            if (members != null)
            {
                for (int i = 0; i < members.Count; i++)
                {
                    ExtensionDataMember member = members[i];
                    if (member.MemberIndex == memberIndex)
                    {
                        WriteExtensionDataMember(xmlWriter, member);
                    }
                }
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void WriteExtensionDataMember(XmlWriterDelegator xmlWriter, ExtensionDataMember member)
        {
            xmlWriter.WriteStartElement(member.Name, member.Namespace);
            IDataNode? dataNode = member.Value;
            WriteExtensionDataValue(xmlWriter, dataNode);
            xmlWriter.WriteEndElement();
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual void WriteExtensionDataTypeInfo(XmlWriterDelegator xmlWriter, IDataNode dataNode)
        {
            if (dataNode.DataContractName != null)
                WriteTypeInfo(xmlWriter, dataNode.DataContractName, dataNode.DataContractNamespace);

            WriteClrTypeInfo(xmlWriter, dataNode.DataType, dataNode.ClrTypeName, dataNode.ClrAssemblyName);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void WriteExtensionDataValue(XmlWriterDelegator xmlWriter, IDataNode? dataNode)
        {
            IncrementItemCount(1);
            if (dataNode == null)
            {
                WriteNull(xmlWriter);
                return;
            }

            if (dataNode.PreservesReferences
                && OnHandleReference(xmlWriter, (dataNode.Value == null ? dataNode : dataNode.Value), true /*canContainCyclicReference*/))
                return;

            Type dataType = dataNode.DataType;
            if (dataType == Globals.TypeOfClassDataNode)
                WriteExtensionClassData(xmlWriter, (ClassDataNode)dataNode);
            else if (dataType == Globals.TypeOfCollectionDataNode)
                WriteExtensionCollectionData(xmlWriter, (CollectionDataNode)dataNode);
            else if (dataType == Globals.TypeOfXmlDataNode)
                WriteExtensionXmlData(xmlWriter, (XmlDataNode)dataNode);
            else if (dataType == Globals.TypeOfISerializableDataNode)
                WriteExtensionISerializableData(xmlWriter, (ISerializableDataNode)dataNode);
            else
            {
                WriteExtensionDataTypeInfo(xmlWriter, dataNode);

                if (dataType == Globals.TypeOfObject)
                {
                    // NOTE: serialize value in DataNode<object> since it may contain non-primitive
                    // deserialized object (ex. empty class)
                    object? o = dataNode.Value;
                    if (o != null)
                        InternalSerialize(xmlWriter, o, false /*isDeclaredType*/, false /*writeXsiType*/, -1, o.GetType().TypeHandle);
                }
                else
                    xmlWriter.WriteExtensionData(dataNode);
            }
            if (dataNode.PreservesReferences)
                OnEndHandleReference(xmlWriter, (dataNode.Value == null ? dataNode : dataNode.Value), true  /*canContainCyclicReference*/);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal bool TryWriteDeserializedExtensionData(XmlWriterDelegator xmlWriter, IDataNode dataNode)
        {
            object? o = dataNode.Value;
            if (o == null)
                return false;

            Type declaredType = (dataNode.DataContractName == null) ? o.GetType() : Globals.TypeOfObject;
            InternalSerialize(xmlWriter, o, false /*isDeclaredType*/, false /*writeXsiType*/, -1, declaredType.TypeHandle);
            return true;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void WriteExtensionClassData(XmlWriterDelegator xmlWriter, ClassDataNode dataNode)
        {
            if (!TryWriteDeserializedExtensionData(xmlWriter, dataNode))
            {
                WriteExtensionDataTypeInfo(xmlWriter, dataNode);

                IList<ExtensionDataMember>? members = dataNode.Members;
                if (members != null)
                {
                    for (int i = 0; i < members.Count; i++)
                    {
                        WriteExtensionDataMember(xmlWriter, members[i]);
                    }
                }
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void WriteExtensionCollectionData(XmlWriterDelegator xmlWriter, CollectionDataNode dataNode)
        {
            if (!TryWriteDeserializedExtensionData(xmlWriter, dataNode))
            {
                WriteExtensionDataTypeInfo(xmlWriter, dataNode);

                WriteArraySize(xmlWriter, dataNode.Size);

                IList<IDataNode?>? items = dataNode.Items;
                if (items != null)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        xmlWriter.WriteStartElement(dataNode.ItemName!, dataNode.ItemNamespace);
                        WriteExtensionDataValue(xmlWriter, items[i]);
                        xmlWriter.WriteEndElement();
                    }
                }
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void WriteExtensionISerializableData(XmlWriterDelegator xmlWriter, ISerializableDataNode dataNode)
        {
            if (!TryWriteDeserializedExtensionData(xmlWriter, dataNode))
            {
                WriteExtensionDataTypeInfo(xmlWriter, dataNode);

                if (dataNode.FactoryTypeName != null)
                    xmlWriter.WriteAttributeQualifiedName(Globals.SerPrefix, DictionaryGlobals.ISerializableFactoryTypeLocalName, DictionaryGlobals.SerializationNamespace, dataNode.FactoryTypeName, dataNode.FactoryTypeNamespace);

                IList<ISerializableDataMember>? members = dataNode.Members;
                if (members != null)
                {
                    for (int i = 0; i < members.Count; i++)
                    {
                        ISerializableDataMember member = members[i];
                        xmlWriter.WriteStartElement(member.Name, string.Empty);
                        WriteExtensionDataValue(xmlWriter, member.Value);
                        xmlWriter.WriteEndElement();
                    }
                }
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void WriteExtensionXmlData(XmlWriterDelegator xmlWriter, XmlDataNode dataNode)
        {
            if (!TryWriteDeserializedExtensionData(xmlWriter, dataNode))
            {
                IList<XmlAttribute>? xmlAttributes = dataNode.XmlAttributes;
                if (xmlAttributes != null)
                {
                    foreach (XmlAttribute attribute in xmlAttributes)
                        attribute.WriteTo(xmlWriter.Writer);
                }
                WriteExtensionDataTypeInfo(xmlWriter, dataNode);

                IList<XmlNode>? xmlChildNodes = dataNode.XmlChildNodes;
                if (xmlChildNodes != null)
                {
                    foreach (XmlNode node in xmlChildNodes)
                        node.WriteTo(xmlWriter.Writer);
                }
            }
        }
    }
}
