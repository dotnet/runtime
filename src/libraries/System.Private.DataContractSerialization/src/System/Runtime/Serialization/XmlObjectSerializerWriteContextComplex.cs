// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Collections.Generic;
using System.Security;
using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization
{
#if USE_REFEMIT || uapaot
    public class XmlObjectSerializerWriteContextComplex : XmlObjectSerializerWriteContext
#else
    internal class XmlObjectSerializerWriteContextComplex : XmlObjectSerializerWriteContext
#endif
    {
        private ISerializationSurrogateProvider _serializationSurrogateProvider;
        private SerializationMode _mode;

        internal XmlObjectSerializerWriteContextComplex(DataContractSerializer serializer, DataContract rootTypeDataContract, DataContractResolver dataContractResolver)
            : base(serializer, rootTypeDataContract, dataContractResolver)
        {
            _mode = SerializationMode.SharedContract;
            this.preserveObjectReferences = serializer.PreserveObjectReferences;
            _serializationSurrogateProvider = serializer.SerializationSurrogateProvider;
        }

        internal XmlObjectSerializerWriteContextComplex(XmlObjectSerializer serializer, int maxItemsInObjectGraph, StreamingContext streamingContext, bool ignoreExtensionDataObject)
            : base(serializer, maxItemsInObjectGraph, streamingContext, ignoreExtensionDataObject)
        {
        }

        internal override SerializationMode Mode
        {
            get { return _mode; }
        }

        internal override bool WriteClrTypeInfo(XmlWriterDelegator xmlWriter, DataContract dataContract)
        {
            return false;
        }

        internal override bool WriteClrTypeInfo(XmlWriterDelegator xmlWriter, Type dataContractType, string clrTypeName, string clrAssemblyName)
        {
            return false;
        }

#if uapaot
        public override void WriteAnyType(XmlWriterDelegator xmlWriter, object value)
#else
        internal override void WriteAnyType(XmlWriterDelegator xmlWriter, object value)
#endif
        {
            if (!OnHandleReference(xmlWriter, value, false /*canContainCyclicReference*/))
                xmlWriter.WriteAnyType(value);
        }

#if uapaot
        public override void WriteString(XmlWriterDelegator xmlWriter, string value)
#else
        internal override void WriteString(XmlWriterDelegator xmlWriter, string value)
#endif
        {
            if (!OnHandleReference(xmlWriter, value, false /*canContainCyclicReference*/))
                xmlWriter.WriteString(value);
        }

#if uapaot
        public override void WriteString(XmlWriterDelegator xmlWriter, string value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal override void WriteString(XmlWriterDelegator xmlWriter, string value, XmlDictionaryString name, XmlDictionaryString ns)
#endif
        {
            if (value == null)
                WriteNull(xmlWriter, typeof(string), true/*isMemberTypeSerializable*/, name, ns);
            else
            {
                xmlWriter.WriteStartElementPrimitive(name, ns);
                if (!OnHandleReference(xmlWriter, value, false /*canContainCyclicReference*/))
                    xmlWriter.WriteString(value);
                xmlWriter.WriteEndElementPrimitive();
            }
        }

#if uapaot
        public override void WriteBase64(XmlWriterDelegator xmlWriter, byte[] value)
#else
        internal override void WriteBase64(XmlWriterDelegator xmlWriter, byte[] value)
#endif
        {
            if (!OnHandleReference(xmlWriter, value, false /*canContainCyclicReference*/))
                xmlWriter.WriteBase64(value);
        }

#if uapaot
        public override void WriteBase64(XmlWriterDelegator xmlWriter, byte[] value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal override void WriteBase64(XmlWriterDelegator xmlWriter, byte[] value, XmlDictionaryString name, XmlDictionaryString ns)
#endif
        {
            if (value == null)
                WriteNull(xmlWriter, typeof(byte[]), true/*isMemberTypeSerializable*/, name, ns);
            else
            {
                xmlWriter.WriteStartElementPrimitive(name, ns);
                if (!OnHandleReference(xmlWriter, value, false /*canContainCyclicReference*/))
                    xmlWriter.WriteBase64(value);
                xmlWriter.WriteEndElementPrimitive();
            }
        }

#if uapaot
        public override void WriteUri(XmlWriterDelegator xmlWriter, Uri value)
#else
        internal override void WriteUri(XmlWriterDelegator xmlWriter, Uri value)
#endif
        {
            if (!OnHandleReference(xmlWriter, value, false /*canContainCyclicReference*/))
                xmlWriter.WriteUri(value);
        }

#if uapaot
        public override void WriteUri(XmlWriterDelegator xmlWriter, Uri value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal override void WriteUri(XmlWriterDelegator xmlWriter, Uri value, XmlDictionaryString name, XmlDictionaryString ns)
#endif
        {
            if (value == null)
                WriteNull(xmlWriter, typeof(Uri), true/*isMemberTypeSerializable*/, name, ns);
            else
            {
                xmlWriter.WriteStartElementPrimitive(name, ns);
                if (!OnHandleReference(xmlWriter, value, false /*canContainCyclicReference*/))
                    xmlWriter.WriteUri(value);
                xmlWriter.WriteEndElementPrimitive();
            }
        }

#if uapaot
        public override void WriteQName(XmlWriterDelegator xmlWriter, XmlQualifiedName value)
#else
        internal override void WriteQName(XmlWriterDelegator xmlWriter, XmlQualifiedName value)
#endif
        {
            if (!OnHandleReference(xmlWriter, value, false /*canContainCyclicReference*/))
                xmlWriter.WriteQName(value);
        }

#if uapaot
        public override void WriteQName(XmlWriterDelegator xmlWriter, XmlQualifiedName value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal override void WriteQName(XmlWriterDelegator xmlWriter, XmlQualifiedName value, XmlDictionaryString name, XmlDictionaryString ns)
#endif        
        {
            if (value == null)
                WriteNull(xmlWriter, typeof(XmlQualifiedName), true/*isMemberTypeSerializable*/, name, ns);
            else
            {
                if (ns != null && ns.Value != null && ns.Value.Length > 0)
                    xmlWriter.WriteStartElement(Globals.ElementPrefix, name, ns);
                else
                    xmlWriter.WriteStartElement(name, ns);
                if (!OnHandleReference(xmlWriter, value, false /*canContainCyclicReference*/))
                    xmlWriter.WriteQName(value);
                xmlWriter.WriteEndElement();
            }
        }

        internal override void InternalSerialize(XmlWriterDelegator xmlWriter, object obj, bool isDeclaredType, bool writeXsiType, int declaredTypeID, RuntimeTypeHandle declaredTypeHandle)
        {
            if (_serializationSurrogateProvider == null)
            {
                base.InternalSerialize(xmlWriter, obj, isDeclaredType, writeXsiType, declaredTypeID, declaredTypeHandle);
            }
            else
            {
                InternalSerializeWithSurrogate(xmlWriter, obj, isDeclaredType, writeXsiType, declaredTypeID, declaredTypeHandle);
            }
        }

        internal override bool OnHandleReference(XmlWriterDelegator xmlWriter, object obj, bool canContainCyclicReference)
        {
            if (preserveObjectReferences && !this.IsGetOnlyCollection)
            {
                bool isNew = true;
                int objectId = SerializedObjects.GetId(obj, ref isNew);
                if (isNew)
                    xmlWriter.WriteAttributeInt(Globals.SerPrefix, DictionaryGlobals.IdLocalName, DictionaryGlobals.SerializationNamespace, objectId);
                else
                {
                    xmlWriter.WriteAttributeInt(Globals.SerPrefix, DictionaryGlobals.RefLocalName, DictionaryGlobals.SerializationNamespace, objectId);
                    xmlWriter.WriteAttributeBool(Globals.XsiPrefix, DictionaryGlobals.XsiNilLocalName, DictionaryGlobals.SchemaInstanceNamespace, true);
                }
                return !isNew;
            }
            return base.OnHandleReference(xmlWriter, obj, canContainCyclicReference);
        }

        internal override void OnEndHandleReference(XmlWriterDelegator xmlWriter, object obj, bool canContainCyclicReference)
        {
            if (preserveObjectReferences && !this.IsGetOnlyCollection)
                return;
            base.OnEndHandleReference(xmlWriter, obj, canContainCyclicReference);
        }

        internal override void CheckIfTypeSerializable(Type memberType, bool isMemberTypeSerializable)
        {
            if (_serializationSurrogateProvider != null)
            {
                while (memberType.IsArray)
                    memberType = memberType.GetElementType();
                memberType = DataContractSurrogateCaller.GetDataContractType(_serializationSurrogateProvider, memberType);
                if (!DataContract.IsTypeSerializable(memberType))
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.TypeNotSerializable, memberType)));
                return;
            }

            base.CheckIfTypeSerializable(memberType, isMemberTypeSerializable);
        }

        internal override Type GetSurrogatedType(Type type)
        {
            if (_serializationSurrogateProvider == null)
            {
                return base.GetSurrogatedType(type);
            }
            else
            {
                type = DataContract.UnwrapNullableType(type);
                Type surrogateType = DataContractSerializer.GetSurrogatedType(_serializationSurrogateProvider, type);
                if (this.IsGetOnlyCollection && surrogateType != type)
                {
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.SurrogatesWithGetOnlyCollectionsNotSupportedSerDeser,
                        DataContract.GetClrTypeFullName(type))));
                }
                else
                {
                    return surrogateType;
                }
            }
        }

        private void InternalSerializeWithSurrogate(XmlWriterDelegator xmlWriter, object obj, bool isDeclaredType, bool writeXsiType, int declaredTypeID, RuntimeTypeHandle declaredTypeHandle)
        {
            RuntimeTypeHandle objTypeHandle = isDeclaredType ? declaredTypeHandle : obj.GetType().TypeHandle;
            object oldObj = obj;
            int objOldId = 0;
            Type objType = Type.GetTypeFromHandle(objTypeHandle);
            Type declaredType = GetSurrogatedType(Type.GetTypeFromHandle(declaredTypeHandle));

            declaredTypeHandle = declaredType.TypeHandle;

            obj = DataContractSerializer.SurrogateToDataContractType(_serializationSurrogateProvider, obj, declaredType, ref objType);
            objTypeHandle = objType.TypeHandle;
            if (oldObj != obj)
                objOldId = SerializedObjects.ReassignId(0, oldObj, obj);

            if (writeXsiType)
            {
                declaredType = Globals.TypeOfObject;
                SerializeWithXsiType(xmlWriter, obj, objTypeHandle, objType, -1, declaredType.TypeHandle, declaredType);
            }
            else if (declaredTypeHandle.Equals(objTypeHandle))
            {
                DataContract contract = GetDataContract(objTypeHandle, objType);
                SerializeWithoutXsiType(contract, xmlWriter, obj, declaredTypeHandle);
            }
            else
            {
                SerializeWithXsiType(xmlWriter, obj, objTypeHandle, objType, -1, declaredTypeHandle, declaredType);
            }
            if (oldObj != obj)
                SerializedObjects.ReassignId(objOldId, obj, oldObj);
        }

        internal override void WriteArraySize(XmlWriterDelegator xmlWriter, int size)
        {
            if (preserveObjectReferences && size > -1)
                xmlWriter.WriteAttributeInt(Globals.SerPrefix, DictionaryGlobals.ArraySizeLocalName, DictionaryGlobals.SerializationNamespace, size);
        }
    }
}

