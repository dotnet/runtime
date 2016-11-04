// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
    using System.Security;
    using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, DataContract>;

#if USE_REFEMIT || NET_NATIVE
    public class XmlObjectSerializerReadContext : XmlObjectSerializerContext
#else
    internal class XmlObjectSerializerReadContext : XmlObjectSerializerContext
#endif
    {
        internal Attributes attributes;
        private HybridObjectCache _deserializedObjects;
        private XmlSerializableReader _xmlSerializableReader;
        private object _getOnlyCollectionValue;
        private bool _isGetOnlyCollection;

        private HybridObjectCache DeserializedObjects
        {
            get
            {
                if (_deserializedObjects == null)
                    _deserializedObjects = new HybridObjectCache();
                return _deserializedObjects;
            }
        }


        internal override bool IsGetOnlyCollection
        {
            get { return _isGetOnlyCollection; }
            set { _isGetOnlyCollection = value; }
        }


#if USE_REFEMIT
        public object GetCollectionMember()
#else
        internal object GetCollectionMember()
#endif
        {
            return _getOnlyCollectionValue;
        }

#if USE_REFEMIT
        public void StoreCollectionMemberInfo(object collectionMember)
#else
        internal void StoreCollectionMemberInfo(object collectionMember)
#endif
        {
            _getOnlyCollectionValue = collectionMember;
            _isGetOnlyCollection = true;
        }

#if USE_REFEMIT
        public static void ThrowNullValueReturnedForGetOnlyCollectionException(Type type)
#else
        internal static void ThrowNullValueReturnedForGetOnlyCollectionException(Type type)
#endif
        {
            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.NullValueReturnedForGetOnlyCollection, DataContract.GetClrTypeFullName(type))));
        }

#if USE_REFEMIT
        public static void ThrowArrayExceededSizeException(int arraySize, Type type)
#else
        internal static void ThrowArrayExceededSizeException(int arraySize, Type type)
#endif
        {
            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ArrayExceededSize, arraySize, DataContract.GetClrTypeFullName(type))));
        }

        internal static XmlObjectSerializerReadContext CreateContext(DataContractSerializer serializer, DataContract rootTypeDataContract, DataContractResolver dataContractResolver)
        {
            return (serializer.PreserveObjectReferences || serializer.SerializationSurrogateProvider != null)
                ? new XmlObjectSerializerReadContextComplex(serializer, rootTypeDataContract, dataContractResolver)
                : new XmlObjectSerializerReadContext(serializer, rootTypeDataContract, dataContractResolver);
        }

        internal XmlObjectSerializerReadContext(XmlObjectSerializer serializer, int maxItemsInObjectGraph, StreamingContext streamingContext, bool ignoreExtensionDataObject)
            : base(serializer, maxItemsInObjectGraph, streamingContext, ignoreExtensionDataObject)
        {
        }

        internal XmlObjectSerializerReadContext(DataContractSerializer serializer, DataContract rootTypeDataContract, DataContractResolver dataContractResolver)
            : base(serializer, rootTypeDataContract, dataContractResolver)
        {
            this.attributes = new Attributes();
        }


#if USE_REFEMIT
        public virtual object InternalDeserialize(XmlReaderDelegator xmlReader, int id, RuntimeTypeHandle declaredTypeHandle, string name, string ns)
#else
        internal virtual object InternalDeserialize(XmlReaderDelegator xmlReader, int id, RuntimeTypeHandle declaredTypeHandle, string name, string ns)
#endif
        {
            DataContract dataContract = GetDataContract(id, declaredTypeHandle);
            return InternalDeserialize(xmlReader, name, ns, ref dataContract);
        }

        internal virtual object InternalDeserialize(XmlReaderDelegator xmlReader, Type declaredType, string name, string ns)
        {
            DataContract dataContract = GetDataContract(declaredType);
            return InternalDeserialize(xmlReader, name, ns, ref dataContract);
        }

        internal virtual object InternalDeserialize(XmlReaderDelegator xmlReader, Type declaredType, DataContract dataContract, string name, string ns)
        {
            if (dataContract == null)
                GetDataContract(declaredType);
            return InternalDeserialize(xmlReader, name, ns, ref dataContract);
        }

        protected bool TryHandleNullOrRef(XmlReaderDelegator reader, Type declaredType, string name, string ns, ref object retObj)
        {
            ReadAttributes(reader);

            if (attributes.Ref != Globals.NewObjectId)
            {
                if (_isGetOnlyCollection)
                {
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ErrorDeserializing, SR.Format(SR.ErrorTypeInfo, DataContract.GetClrTypeFullName(declaredType)), SR.Format(SR.XmlStartElementExpected, Globals.RefLocalName))));
                }
                else
                {
                    retObj = GetExistingObject(attributes.Ref, declaredType, name, ns);
                    reader.Skip();
                    return true;
                }
            }
            else if (attributes.XsiNil)
            {
                reader.Skip();
                return true;
            }
            return false;
        }

        protected object InternalDeserialize(XmlReaderDelegator reader, string name, string ns, ref DataContract dataContract)
        {
            object retObj = null;
            if (TryHandleNullOrRef(reader, dataContract.UnderlyingType, name, ns, ref retObj))
                return retObj;

            bool knownTypesAddedInCurrentScope = false;
            if (dataContract.KnownDataContracts != null)
            {
                scopedKnownTypes.Push(dataContract.KnownDataContracts);
                knownTypesAddedInCurrentScope = true;
            }

            if (attributes.XsiTypeName != null)
            {
                dataContract = ResolveDataContractFromKnownTypes(attributes.XsiTypeName, attributes.XsiTypeNamespace, dataContract);
                if (dataContract == null)
                {
                    if (DataContractResolver == null)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(XmlObjectSerializer.TryAddLineInfo(reader, SR.Format(SR.DcTypeNotFoundOnDeserialize, attributes.XsiTypeNamespace, attributes.XsiTypeName, reader.NamespaceURI, reader.LocalName))));
                    }
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(XmlObjectSerializer.TryAddLineInfo(reader, SR.Format(SR.DcTypeNotResolvedOnDeserialize, attributes.XsiTypeNamespace, attributes.XsiTypeName, reader.NamespaceURI, reader.LocalName))));
                }
                knownTypesAddedInCurrentScope = ReplaceScopedKnownTypesTop(dataContract.KnownDataContracts, knownTypesAddedInCurrentScope);
            }

            if (knownTypesAddedInCurrentScope)
            {
                object obj = ReadDataContractValue(dataContract, reader);
                scopedKnownTypes.Pop();
                return obj;
            }
            else
            {
                return ReadDataContractValue(dataContract, reader);
            }
        }

        private bool ReplaceScopedKnownTypesTop(DataContractDictionary knownDataContracts, bool knownTypesAddedInCurrentScope)
        {
            if (knownTypesAddedInCurrentScope)
            {
                scopedKnownTypes.Pop();
                knownTypesAddedInCurrentScope = false;
            }
            if (knownDataContracts != null)
            {
                scopedKnownTypes.Push(knownDataContracts);
                knownTypesAddedInCurrentScope = true;
            }
            return knownTypesAddedInCurrentScope;
        }

#if USE_REFEMIT
        public static bool MoveToNextElement(XmlReaderDelegator xmlReader)
#else
        internal static bool MoveToNextElement(XmlReaderDelegator xmlReader)
#endif
        {
            return (xmlReader.MoveToContent() != XmlNodeType.EndElement);
        }

#if USE_REFEMIT
        public int GetMemberIndex(XmlReaderDelegator xmlReader, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces, int memberIndex, ExtensionDataObject extensionData)
#else
        internal int GetMemberIndex(XmlReaderDelegator xmlReader, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces, int memberIndex, ExtensionDataObject extensionData)
#endif
        {
            for (int i = memberIndex + 1; i < memberNames.Length; i++)
            {
                if (xmlReader.IsStartElement(memberNames[i], memberNamespaces[i]))
                    return i;
            }
            HandleMemberNotFound(xmlReader, extensionData, memberIndex);
            return memberNames.Length;
        }

#if USE_REFEMIT
        public int GetMemberIndexWithRequiredMembers(XmlReaderDelegator xmlReader, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces, int memberIndex, int requiredIndex, ExtensionDataObject extensionData)
#else
        internal int GetMemberIndexWithRequiredMembers(XmlReaderDelegator xmlReader, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces, int memberIndex, int requiredIndex, ExtensionDataObject extensionData)
#endif
        {
            for (int i = memberIndex + 1; i < memberNames.Length; i++)
            {
                if (xmlReader.IsStartElement(memberNames[i], memberNamespaces[i]))
                {
                    if (requiredIndex < i)
                        ThrowRequiredMemberMissingException(xmlReader, memberIndex, requiredIndex, memberNames);
                    return i;
                }
            }
            HandleMemberNotFound(xmlReader, extensionData, memberIndex);
            return memberNames.Length;
        }

#if USE_REFEMIT
        public static void ThrowRequiredMemberMissingException(XmlReaderDelegator xmlReader, int memberIndex, int requiredIndex, XmlDictionaryString[] memberNames)
#else
        internal static void ThrowRequiredMemberMissingException(XmlReaderDelegator xmlReader, int memberIndex, int requiredIndex, XmlDictionaryString[] memberNames)
#endif
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (requiredIndex == memberNames.Length)
                requiredIndex--;
            for (int i = memberIndex + 1; i <= requiredIndex; i++)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(" | ");
                stringBuilder.Append(memberNames[i].Value);
            }
            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(XmlObjectSerializer.TryAddLineInfo(xmlReader, SR.Format(SR.UnexpectedElementExpectingElements, xmlReader.NodeType, xmlReader.LocalName, xmlReader.NamespaceURI, stringBuilder.ToString()))));
        }

#if NET_NATIVE
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
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.JsonOneRequiredMemberNotFound, DataContract.GetClrTypeFullName(obj.GetType()), stringBuilder.ToString())));
            }
            else
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.JsonRequiredMembersNotFound, DataContract.GetClrTypeFullName(obj.GetType()), stringBuilder.ToString())));
            }
        }

        public static void ThrowDuplicateMemberException(object obj, XmlDictionaryString[] memberNames, int memberIndex)
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.JsonDuplicateMemberInInput, DataContract.GetClrTypeFullName(obj.GetType()), memberNames[memberIndex])));
        }

        [SecuritySafeCritical]
        private static bool IsBitSet(byte[] bytes, int bitIndex)
        {
            throw new NotImplementedException();
            //return BitFlagsGenerator.IsBitSet(bytes, bitIndex);
        }
#endif

        protected void HandleMemberNotFound(XmlReaderDelegator xmlReader, ExtensionDataObject extensionData, int memberIndex)
        {
            xmlReader.MoveToContent();
            if (xmlReader.NodeType != XmlNodeType.Element)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateUnexpectedStateException(XmlNodeType.Element, xmlReader));

            if (IgnoreExtensionDataObject || extensionData == null)
                SkipUnknownElement(xmlReader);
            else
                HandleUnknownElement(xmlReader, extensionData, memberIndex);
        }

        internal void HandleUnknownElement(XmlReaderDelegator xmlReader, ExtensionDataObject extensionData, int memberIndex)
        {
        }

#if USE_REFEMIT
        public void SkipUnknownElement(XmlReaderDelegator xmlReader)
#else
        internal void SkipUnknownElement(XmlReaderDelegator xmlReader)
#endif
        {
            ReadAttributes(xmlReader);
            xmlReader.Skip();
        }

#if USE_REFEMIT
        public string ReadIfNullOrRef(XmlReaderDelegator xmlReader, Type memberType, bool isMemberTypeSerializable)
#else
        internal string ReadIfNullOrRef(XmlReaderDelegator xmlReader, Type memberType, bool isMemberTypeSerializable)
#endif
        {
            if (attributes.Ref != Globals.NewObjectId)
            {
                CheckIfTypeSerializable(memberType, isMemberTypeSerializable);
                xmlReader.Skip();
                return attributes.Ref;
            }
            else if (attributes.XsiNil)
            {
                CheckIfTypeSerializable(memberType, isMemberTypeSerializable);
                xmlReader.Skip();
                return Globals.NullObjectId;
            }
            return Globals.NewObjectId;
        }

#if USE_REFEMIT
        public virtual void ReadAttributes(XmlReaderDelegator xmlReader)
#else
        internal virtual void ReadAttributes(XmlReaderDelegator xmlReader)
#endif
        {
            if (attributes == null)
                attributes = new Attributes();
            attributes.Read(xmlReader);
        }

#if USE_REFEMIT
        public void ResetAttributes()
#else
        internal void ResetAttributes()
#endif
        {
            if (attributes != null)
                attributes.Reset();
        }

#if USE_REFEMIT
        public string GetObjectId()
#else
        internal string GetObjectId()
#endif
        {
            return attributes.Id;
        }

#if USE_REFEMIT
        public virtual int GetArraySize()
#else
        internal virtual int GetArraySize()
#endif
        {
            return -1;
        }

#if USE_REFEMIT
        public void AddNewObject(object obj)
#else
        internal void AddNewObject(object obj)
#endif
        {
            AddNewObjectWithId(attributes.Id, obj);
        }

#if USE_REFEMIT
        public void AddNewObjectWithId(string id, object obj)
#else
        internal void AddNewObjectWithId(string id, object obj)
#endif
        {
            if (id != Globals.NewObjectId)
                DeserializedObjects.Add(id, obj);
        }

        public void ReplaceDeserializedObject(string id, object oldObj, object newObj)
        {
            if (object.ReferenceEquals(oldObj, newObj))
                return;

            if (id != Globals.NewObjectId)
            {
                // In certain cases (IObjectReference, SerializationSurrogate or DataContractSurrogate),
                // an object can be replaced with a different object once it is deserialized. If the 
                // object happens to be referenced from within itself, that reference needs to be updated
                // with the new instance. BinaryFormatter supports this by fixing up such references later. 
                // These XmlObjectSerializer implementations do not currently support fix-ups. Hence we 
                // throw in such cases to allow us add fix-up support in the future if we need to.
                if (DeserializedObjects.IsObjectReferenced(id))
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.FactoryObjectContainsSelfReference, DataContract.GetClrTypeFullName(oldObj.GetType()), DataContract.GetClrTypeFullName(newObj.GetType()), id)));
                DeserializedObjects.Remove(id);
                DeserializedObjects.Add(id, newObj);
            }
        }

#if USE_REFEMIT
        public object GetExistingObject(string id, Type type, string name, string ns)
#else
        internal object GetExistingObject(string id, Type type, string name, string ns)
#endif
        {
            object retObj = DeserializedObjects.GetObject(id);
            if (retObj == null)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.DeserializedObjectWithIdNotFound, id)));
            return retObj;
        }


#if USE_REFEMIT
        public static void Read(XmlReaderDelegator xmlReader)
#else
        internal static void Read(XmlReaderDelegator xmlReader)
#endif
        {
            if (!xmlReader.Read())
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.UnexpectedEndOfFile)));
        }

        internal static void ParseQualifiedName(string qname, XmlReaderDelegator xmlReader, out string name, out string ns, out string prefix)
        {
            int colon = qname.IndexOf(':');
            prefix = "";
            if (colon >= 0)
                prefix = qname.Substring(0, colon);
            name = qname.Substring(colon + 1);
            ns = xmlReader.LookupNamespace(prefix);
        }

#if USE_REFEMIT
        public static T[] EnsureArraySize<T>(T[] array, int index)
#else
        internal static T[] EnsureArraySize<T>(T[] array, int index)
#endif
        {
            if (array.Length <= index)
            {
                if (index == Int32.MaxValue)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        XmlObjectSerializer.CreateSerializationException(
                        SR.Format(SR.MaxArrayLengthExceeded, Int32.MaxValue,
                        DataContract.GetClrTypeFullName(typeof(T)))));
                }
                int newSize = (index < Int32.MaxValue / 2) ? index * 2 : Int32.MaxValue;
                T[] newArray = new T[newSize];
                Array.Copy(array, 0, newArray, 0, array.Length);
                array = newArray;
            }
            return array;
        }

#if USE_REFEMIT
        public static T[] TrimArraySize<T>(T[] array, int size)
#else
        internal static T[] TrimArraySize<T>(T[] array, int size)
#endif
        {
            if (size != array.Length)
            {
                T[] newArray = new T[size];
                Array.Copy(array, 0, newArray, 0, size);
                array = newArray;
            }
            return array;
        }

#if USE_REFEMIT
        public void CheckEndOfArray(XmlReaderDelegator xmlReader, int arraySize, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
#else
        internal void CheckEndOfArray(XmlReaderDelegator xmlReader, int arraySize, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
#endif
        {
            if (xmlReader.NodeType == XmlNodeType.EndElement)
                return;
            while (xmlReader.IsStartElement())
            {
                if (xmlReader.IsStartElement(itemName, itemNamespace))
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ArrayExceededSizeAttribute, arraySize, itemName.Value, itemNamespace.Value)));
                SkipUnknownElement(xmlReader);
            }
            if (xmlReader.NodeType != XmlNodeType.EndElement)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateUnexpectedStateException(XmlNodeType.EndElement, xmlReader));
        }

        internal object ReadIXmlSerializable(XmlReaderDelegator xmlReader, XmlDataContract xmlDataContract, bool isMemberType)
        {
            if (_xmlSerializableReader == null)
                _xmlSerializableReader = new XmlSerializableReader();
            return ReadIXmlSerializable(_xmlSerializableReader, xmlReader, xmlDataContract, isMemberType);
        }

        internal static object ReadRootIXmlSerializable(XmlReaderDelegator xmlReader, XmlDataContract xmlDataContract, bool isMemberType)
        {
            return ReadIXmlSerializable(new XmlSerializableReader(), xmlReader, xmlDataContract, isMemberType);
        }

        internal static object ReadIXmlSerializable(XmlSerializableReader xmlSerializableReader, XmlReaderDelegator xmlReader, XmlDataContract xmlDataContract, bool isMemberType)
        {
            object obj = null;
            xmlSerializableReader.BeginRead(xmlReader);
            if (isMemberType && !xmlDataContract.HasRoot)
            {
                xmlReader.Read();
                xmlReader.MoveToContent();
            }
            if (xmlDataContract.UnderlyingType == Globals.TypeOfXmlElement)
            {
                if (!xmlReader.IsStartElement())
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateUnexpectedStateException(XmlNodeType.Element, xmlReader));
                XmlDocument xmlDoc = new XmlDocument();
                obj = (XmlElement)xmlDoc.ReadNode(xmlSerializableReader);
            }
            else if (xmlDataContract.UnderlyingType == Globals.TypeOfXmlNodeArray)
            {
                obj = XmlSerializableServices.ReadNodes(xmlSerializableReader);
            }
            else
            {
                IXmlSerializable xmlSerializable = xmlDataContract.CreateXmlSerializableDelegate();
                xmlSerializable.ReadXml(xmlSerializableReader);
                obj = xmlSerializable;
            }
            xmlSerializableReader.EndRead();
            return obj;
        }

        public SerializationInfo ReadSerializationInfo(XmlReaderDelegator xmlReader, Type type)
        {
            var serInfo = new SerializationInfo(type, XmlObjectSerializer.FormatterConverter);
            XmlNodeType nodeType;
            while ((nodeType = xmlReader.MoveToContent()) != XmlNodeType.EndElement)
            {
                if (nodeType != XmlNodeType.Element)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateUnexpectedStateException(XmlNodeType.Element, xmlReader));
                }

                if (xmlReader.NamespaceURI.Length != 0)
                {
                    SkipUnknownElement(xmlReader);
                    continue;
                }

                string name = XmlConvert.DecodeName(xmlReader.LocalName);

                IncrementItemCount(1);
                ReadAttributes(xmlReader);
                object value;
                if (attributes.Ref != Globals.NewObjectId)
                {
                    xmlReader.Skip();
                    value = GetExistingObject(attributes.Ref, null, name, String.Empty);
                }
                else if (attributes.XsiNil)
                {
                    xmlReader.Skip();
                    value = null;
                }
                else
                {
                    value = InternalDeserialize(xmlReader, Globals.TypeOfObject, name, String.Empty);
                }

                serInfo.AddValue(name, value);
            }

            return serInfo;
        }

        protected virtual DataContract ResolveDataContractFromTypeName()
        {
            return (attributes.XsiTypeName == null) ? null : ResolveDataContractFromKnownTypes(attributes.XsiTypeName, attributes.XsiTypeNamespace, null /*memberTypeContract*/);
        }


#if USE_REFEMIT
        public static Exception CreateUnexpectedStateException(XmlNodeType expectedState, XmlReaderDelegator xmlReader)
#else
        internal static Exception CreateUnexpectedStateException(XmlNodeType expectedState, XmlReaderDelegator xmlReader)
#endif
        {
            return XmlObjectSerializer.CreateSerializationExceptionWithReaderDetails(SR.Format(SR.ExpectingState, expectedState), xmlReader);
        }

        //Silverlight only helper function to create SerializationException
#if USE_REFEMIT
        public static Exception CreateSerializationException(string message)
#else
        internal static Exception CreateSerializationException(string message)
#endif
        {
            return XmlObjectSerializer.CreateSerializationException(message);
        }

        protected virtual object ReadDataContractValue(DataContract dataContract, XmlReaderDelegator reader)
        {
            return dataContract.ReadXmlValue(reader, this);
        }

        protected virtual XmlReaderDelegator CreateReaderDelegatorForReader(XmlReader xmlReader)
        {
            return new XmlReaderDelegator(xmlReader);
        }

        protected virtual bool IsReadingCollectionExtensionData(XmlReaderDelegator xmlReader)
        {
            return (attributes.ArraySZSize != -1);
        }

        protected virtual bool IsReadingClassExtensionData(XmlReaderDelegator xmlReader)
        {
            return false;
        }
    }
}
