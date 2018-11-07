// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Xml;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContract>;
using System.Diagnostics;

namespace System.Runtime.Serialization.Json
{
#if uapaot
    public class XmlObjectSerializerReadContextComplexJson : XmlObjectSerializerReadContextComplex
#else
    internal class XmlObjectSerializerReadContextComplexJson : XmlObjectSerializerReadContextComplex
#endif
    {
        private string _extensionDataValueType;
        private DataContractJsonSerializer _jsonSerializer;
        private DateTimeFormat _dateTimeFormat;
        private bool _useSimpleDictionaryFormat;

        public XmlObjectSerializerReadContextComplexJson(DataContractJsonSerializer serializer, DataContract rootTypeDataContract)
            : base(null, int.MaxValue, new StreamingContext(), true)
        {
            this.rootTypeDataContract = rootTypeDataContract;
            this.serializerKnownTypeList = serializer.KnownTypes;
            _jsonSerializer = serializer;
        }

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

        internal IList<Type> SerializerKnownTypeList
        {
            get
            {
                return this.serializerKnownTypeList;
            }
        }

        public bool UseSimpleDictionaryFormat
        {
            get
            {
                return _useSimpleDictionaryFormat;
            }
        }

        protected override void StartReadExtensionDataValue(XmlReaderDelegator xmlReader)
        {
            _extensionDataValueType = xmlReader.GetAttribute(JsonGlobals.typeString);
        }

        protected override IDataNode ReadPrimitiveExtensionDataValue(XmlReaderDelegator xmlReader, string dataContractName, string dataContractNamespace)
        {
            IDataNode dataNode;

            switch (_extensionDataValueType)
            {
                case null:
                case JsonGlobals.stringString:
                    dataNode = new DataNode<string>(xmlReader.ReadContentAsString());
                    break;
                case JsonGlobals.booleanString:
                    dataNode = new DataNode<bool>(xmlReader.ReadContentAsBoolean());
                    break;
                case JsonGlobals.numberString:
                    dataNode = ReadNumericalPrimitiveExtensionDataValue(xmlReader);
                    break;
                default:
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        XmlObjectSerializer.CreateSerializationException(SR.Format(SR.JsonUnexpectedAttributeValue, _extensionDataValueType)));
            }

            xmlReader.ReadEndElement();
            return dataNode;
        }

        private IDataNode ReadNumericalPrimitiveExtensionDataValue(XmlReaderDelegator xmlReader)
        {
            TypeCode type;
            object numericalValue = JsonObjectDataContract.ParseJsonNumber(xmlReader.ReadContentAsString(), out type);
            switch (type)
            {
                case TypeCode.Byte:
                    return new DataNode<byte>((byte)numericalValue);
                case TypeCode.SByte:
                    return new DataNode<sbyte>((sbyte)numericalValue);
                case TypeCode.Int16:
                    return new DataNode<short>((short)numericalValue);
                case TypeCode.Int32:
                    return new DataNode<int>((int)numericalValue);
                case TypeCode.Int64:
                    return new DataNode<long>((long)numericalValue);
                case TypeCode.UInt16:
                    return new DataNode<ushort>((ushort)numericalValue);
                case TypeCode.UInt32:
                    return new DataNode<uint>((uint)numericalValue);
                case TypeCode.UInt64:
                    return new DataNode<ulong>((ulong)numericalValue);
                case TypeCode.Single:
                    return new DataNode<float>((float)numericalValue);
                case TypeCode.Double:
                    return new DataNode<double>((double)numericalValue);
                case TypeCode.Decimal:
                    return new DataNode<decimal>((decimal)numericalValue);
                default:
                    throw new InvalidOperationException(SR.ParseJsonNumberReturnInvalidNumber);
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
                name = ns = string.Empty;
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

#if !uapaot
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

        private static bool IsBitSet(byte[] bytes, int bitIndex)
        {
            return BitFlagsGenerator.IsBitSet(bytes, bitIndex);
        }
#endif
    }
}
