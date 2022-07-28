// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Xml;
using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization.Json
{
    internal sealed class XmlObjectSerializerReadContextComplexJson : XmlObjectSerializerReadContextComplex
    {
        private string? _extensionDataValueType;
        private readonly DateTimeFormat? _dateTimeFormat;
        private readonly bool _useSimpleDictionaryFormat;

        internal XmlObjectSerializerReadContextComplexJson(DataContractJsonSerializer serializer, DataContract rootTypeDataContract)
            : base(serializer, serializer.MaxItemsInObjectGraph, new StreamingContext(StreamingContextStates.All), serializer.IgnoreExtensionDataObject)
        {
            this.rootTypeDataContract = rootTypeDataContract;
            this.serializerKnownTypeList = serializer.knownTypeList;
            _dateTimeFormat = serializer.DateTimeFormat;
            _useSimpleDictionaryFormat = serializer.UseSimpleDictionaryFormat;
        }

        internal static XmlObjectSerializerReadContextComplexJson CreateContext(DataContractJsonSerializer serializer, DataContract rootTypeDataContract)
        {
            return new XmlObjectSerializerReadContextComplexJson(serializer, rootTypeDataContract);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override object? ReadDataContractValue(DataContract dataContract, XmlReaderDelegator reader)
        {
            return DataContractJsonSerializer.ReadJsonValue(dataContract, reader, this);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public int GetJsonMemberIndex(XmlReaderDelegator xmlReader, XmlDictionaryString[] memberNames, int memberIndex, ExtensionDataObject? extensionData)
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
                string? name;
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

        internal IList<Type>? SerializerKnownTypeList
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

        protected override IDataNode ReadPrimitiveExtensionDataValue(XmlReaderDelegator xmlReader, string? dataContractName, string? dataContractNamespace)
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

        private static IDataNode ReadNumericalPrimitiveExtensionDataValue(XmlReaderDelegator xmlReader)
        {
            TypeCode type;
            object numericalValue = JsonObjectDataContract.ParseJsonNumber(xmlReader.ReadContentAsString(), out type);
            return type switch
            {
                TypeCode.Byte => new DataNode<byte>((byte)numericalValue),
                TypeCode.SByte => new DataNode<sbyte>((sbyte)numericalValue),
                TypeCode.Int16 => new DataNode<short>((short)numericalValue),
                TypeCode.Int32 => new DataNode<int>((int)numericalValue),
                TypeCode.Int64 => new DataNode<long>((long)numericalValue),
                TypeCode.UInt16 => new DataNode<ushort>((ushort)numericalValue),
                TypeCode.UInt32 => new DataNode<uint>((uint)numericalValue),
                TypeCode.UInt64 => new DataNode<ulong>((ulong)numericalValue),
                TypeCode.Single => new DataNode<float>((float)numericalValue),
                TypeCode.Double => new DataNode<double>((double)numericalValue),
                TypeCode.Decimal => new DataNode<decimal>((decimal)numericalValue),
                _ => throw new InvalidOperationException(SR.ParseJsonNumberReturnInvalidNumber),
            };
        }

        internal override int GetArraySize()
        {
            return -1;
        }

        internal override void ReadAttributes(XmlReaderDelegator xmlReader)
        {
            attributes ??= new Attributes();
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
                            serverTypeNamespace = string.Concat(Globals.DataContractXsdBaseNamespace, serverTypeNamespace.AsSpan(1));
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

        internal static string TrimNamespace(string serverTypeNamespace)
        {
            if (!string.IsNullOrEmpty(serverTypeNamespace))
            {
                switch (serverTypeNamespace[0])
                {
                    case '#':
                        serverTypeNamespace = string.Concat(Globals.DataContractXsdBaseNamespace, serverTypeNamespace.AsSpan(1));
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

        protected override bool IsReadingCollectionExtensionData(XmlReaderDelegator xmlReader)
        {
            return xmlReader.GetAttribute(JsonGlobals.typeString) == JsonGlobals.arrayString;
        }

        protected override bool IsReadingClassExtensionData(XmlReaderDelegator xmlReader)
        {
            return xmlReader.GetAttribute(JsonGlobals.typeString) == JsonGlobals.objectString;
        }

        protected override XmlReaderDelegator CreateReaderDelegatorForReader(XmlReader xmlReader)
        {
            return new JsonReaderDelegator(xmlReader, this._dateTimeFormat);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override DataContract GetDataContract(RuntimeTypeHandle typeHandle, Type? type)
        {
            DataContract dataContract = base.GetDataContract(typeHandle, type);
            DataContractJsonSerializer.CheckIfTypeIsReference(dataContract);
            return dataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override DataContract GetDataContractSkipValidation(int typeId, RuntimeTypeHandle typeHandle, Type? type)
        {
            DataContract dataContract = base.GetDataContractSkipValidation(typeId, typeHandle, type);
            DataContractJsonSerializer.CheckIfTypeIsReference(dataContract);
            return dataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override DataContract GetDataContract(int id, RuntimeTypeHandle typeHandle)
        {
            DataContract dataContract = base.GetDataContract(id, typeHandle);
            DataContractJsonSerializer.CheckIfTypeIsReference(dataContract);
            return dataContract;
        }

        internal static bool TryGetJsonLocalName(XmlReaderDelegator xmlReader, [NotNullWhen(true)] out string? name)
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
            string? name;
            if (!TryGetJsonLocalName(xmlReader, out name))
            {
                name = xmlReader.LocalName;
            }
            return name;
        }

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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected override DataContract? ResolveDataContractFromRootDataContract(XmlQualifiedName typeQName)
        {
            return XmlObjectSerializerWriteContextComplexJson.ResolveJsonDataContractFromRootDataContract(this, typeQName, rootTypeDataContract!);
        }
    }
}
