// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;

namespace System.Runtime.Serialization.DataContracts
{
    internal sealed class EnumDataContract : DataContract
    {
        internal const string ContractTypeString = nameof(EnumDataContract);
        public override string? ContractType => ContractTypeString;

        private readonly EnumDataContractCriticalHelper _helper;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal EnumDataContract(Type type) : base(new EnumDataContractCriticalHelper(type))
        {
            _helper = (base.Helper as EnumDataContractCriticalHelper)!;
        }

        internal static Type? GetBaseType(XmlQualifiedName baseContractName)
        {
            return EnumDataContractCriticalHelper.GetBaseType(baseContractName);
        }

        public override DataContract BaseContract
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get => _helper.BaseContract;
        }

        internal XmlQualifiedName BaseContractName
        {
            get => _helper.BaseContractName;
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            set => _helper.BaseContractName = value;
        }

        internal List<DataMember> Members
        {
            get => _helper.Members;
            set => _helper.Members = value;
        }

        public override ReadOnlyCollection<DataMember> DataMembers => (Members == null) ? ReadOnlyCollection<DataMember>.Empty : Members.AsReadOnly();

        internal List<long>? Values
        {
            get => _helper.Values;
            set => _helper.Values = value;
        }

        internal bool IsFlags
        {
            get => _helper.IsFlags;
            set => _helper.IsFlags = value;
        }

        internal bool IsULong => _helper.IsULong;

        internal XmlDictionaryString[]? ChildElementNames => _helper.ChildElementNames;

        internal override bool CanContainReferences => false;

        private sealed class EnumDataContractCriticalHelper : DataContract.DataContractCriticalHelper
        {
            private static readonly Dictionary<Type, XmlQualifiedName> s_typeToName = new Dictionary<Type, XmlQualifiedName>();
            private static readonly Dictionary<XmlQualifiedName, Type> s_nameToType = new Dictionary<XmlQualifiedName, Type>();

            private DataContract _baseContract;
            private List<DataMember> _members;
            private List<long>? _values;
            private bool _isULong;
            private bool _isFlags;
            private readonly bool _hasDataContract;
            private XmlDictionaryString[]? _childElementNames;

            static EnumDataContractCriticalHelper()
            {
                Add(typeof(sbyte), DictionaryGlobals.SignedByteLocalName.Value);        // "byte"
                Add(typeof(byte), DictionaryGlobals.UnsignedByteLocalName.Value);       // "unsignedByte"
                Add(typeof(short), DictionaryGlobals.ShortLocalName.Value);             // "short"
                Add(typeof(ushort), DictionaryGlobals.UnsignedShortLocalName.Value);    // "unsignedShort"
                Add(typeof(int), DictionaryGlobals.IntLocalName.Value);                 // "int"
                Add(typeof(uint), DictionaryGlobals.UnsignedIntLocalName.Value);        // "unsignedInt"
                Add(typeof(long), DictionaryGlobals.LongLocalName.Value);               // "long"
                Add(typeof(ulong), DictionaryGlobals.UnsignedLongLocalName.Value);      // "unsignedLong"
            }

            internal static void Add(Type type, string localName)
            {
                XmlQualifiedName xmlName = CreateQualifiedName(localName, Globals.SchemaNamespace);
                s_typeToName.Add(type, xmlName);
                s_nameToType.Add(xmlName, type);
            }

            internal static XmlQualifiedName GetBaseContractName(Type type)
            {
                s_typeToName.TryGetValue(type, out XmlQualifiedName? retVal);

                Debug.Assert(retVal != null);   // Enums can only have certain base types. We shouldn't come up empty here.
                return retVal;
            }

            internal static Type? GetBaseType(XmlQualifiedName baseContractName)
            {
                s_nameToType.TryGetValue(baseContractName, out Type? retVal);
                return retVal;
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal EnumDataContractCriticalHelper(
                [DynamicallyAccessedMembers(ClassDataContract.DataContractPreserveMemberTypes)]
                Type type) : base(type)
            {
                XmlName = DataContract.GetXmlName(type, out _hasDataContract);
                Type baseType = Enum.GetUnderlyingType(type);
                XmlQualifiedName baseTypeName = GetBaseContractName(baseType);
                _baseContract = DataContract.GetBuiltInDataContract(baseTypeName.Name, baseTypeName.Namespace)!;
                // Setting XmlName might be redundant. But I don't want to miss an edge case.
                _baseContract.XmlName = baseTypeName;
                ImportBaseType(baseType);
                IsFlags = type.IsDefined(Globals.TypeOfFlagsAttribute, false);
                ImportDataMembers();

                XmlDictionary dictionary = new XmlDictionary(2 + Members.Count);
                Name = dictionary.Add(XmlName.Name);
                Namespace = dictionary.Add(XmlName.Namespace);
                _childElementNames = new XmlDictionaryString[Members.Count];
                for (int i = 0; i < Members.Count; i++)
                    _childElementNames[i] = dictionary.Add(Members[i].Name);
                if (TryGetDCAttribute(type, out DataContractAttribute? dataContractAttribute))
                {
                    if (dataContractAttribute.IsReference)
                    {
                        DataContract.ThrowInvalidDataContractException(
                                SR.Format(SR.EnumTypeCannotHaveIsReference,
                                    DataContract.GetClrTypeFullName(type),
                                    dataContractAttribute.IsReference,
                                    false),
                                type);
                    }
                }
            }

            internal DataContract BaseContract => _baseContract;

            internal XmlQualifiedName BaseContractName
            {
                get => _baseContract.XmlName;

                [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
                [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
                set
                {
                    Type? baseType = GetBaseType(value);
                    if (baseType == null)
                        ThrowInvalidDataContractException(
                                SR.Format(SR.InvalidEnumBaseType, value.Name, value.Namespace, XmlName.Name, XmlName.Namespace));
                    ImportBaseType(baseType);
                    _baseContract = DataContract.GetBuiltInDataContract(value.Name, value.Namespace)!;
                    // Setting XmlName might be redundant. But I don't want to miss an edge case.
                    _baseContract.XmlName = value;
                }
            }

            internal List<DataMember> Members
            {
                get => _members;
                set => _members = value;
            }

            internal List<long>? Values
            {
                get => _values;
                set => _values = value;
            }

            internal bool IsFlags
            {
                get => _isFlags;
                set => _isFlags = value;
            }

            internal bool IsULong
            {
                get => _isULong;
                set => _isULong = value;
            }

            internal XmlDictionaryString[]? ChildElementNames
            {
                get => _childElementNames;
                set => _childElementNames = value;
            }

            private void ImportBaseType(Type baseType)
            {
                _isULong = (baseType == Globals.TypeOfULong);
            }

            [MemberNotNull(nameof(_members))]
            private void ImportDataMembers()
            {
                Type type = UnderlyingType;
                FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
                Dictionary<string, DataMember> memberValuesTable = new Dictionary<string, DataMember>();
                List<DataMember> tempMembers = new List<DataMember>(fields.Length);
                List<long> tempValues = new List<long>(fields.Length);

                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    bool enumMemberValid = false;
                    if (_hasDataContract)
                    {
                        object[] memberAttributes = field.GetCustomAttributes(Globals.TypeOfEnumMemberAttribute, false).ToArray();
                        if (memberAttributes != null && memberAttributes.Length > 0)
                        {
                            if (memberAttributes.Length > 1)
                                ThrowInvalidDataContractException(SR.Format(SR.TooManyEnumMembers, DataContract.GetClrTypeFullName(field.DeclaringType!), field.Name));
                            EnumMemberAttribute memberAttribute = (EnumMemberAttribute)memberAttributes[0];

                            DataMember memberContract = new DataMember(field);
                            if (memberAttribute.IsValueSetExplicitly)
                            {
                                if (memberAttribute.Value == null || memberAttribute.Value.Length == 0)
                                    ThrowInvalidDataContractException(SR.Format(SR.InvalidEnumMemberValue, field.Name, DataContract.GetClrTypeFullName(type)));
                                memberContract.Name = memberAttribute.Value;
                            }
                            else
                                memberContract.Name = field.Name;
                            memberContract.Order = _isULong ? (long)Convert.ToUInt64(field.GetValue(null)) : Convert.ToInt64(field.GetValue(null));
                            ClassDataContract.CheckAndAddMember(tempMembers, memberContract, memberValuesTable);
                            enumMemberValid = true;
                        }

                        object[] dataMemberAttributes = field.GetCustomAttributes(Globals.TypeOfDataMemberAttribute, false).ToArray();
                        if (dataMemberAttributes != null && dataMemberAttributes.Length > 0)
                            ThrowInvalidDataContractException(SR.Format(SR.DataMemberOnEnumField, DataContract.GetClrTypeFullName(field.DeclaringType!), field.Name));
                    }
                    else
                    {
#pragma warning disable SYSLIB0050 // FieldInfo.IsNotSerialized is obsolete
                        if (!field.IsNotSerialized)
                        {
                            DataMember memberContract = new DataMember(field) { Name = field.Name };
                            memberContract.Order = _isULong ? (long)Convert.ToUInt64(field.GetValue(null)) : Convert.ToInt64(field.GetValue(null));
                            ClassDataContract.CheckAndAddMember(tempMembers, memberContract, memberValuesTable);
                            enumMemberValid = true;
                        }
#pragma warning restore SYSLIB0050
                    }

                    if (enumMemberValid)
                    {
                        object? enumValue = field.GetValue(null);
                        if (_isULong)
                            tempValues.Add((long)Convert.ToUInt64(enumValue, null));
                        else
                            tempValues.Add(Convert.ToInt64(enumValue, null));
                    }
                }

                Interlocked.MemoryBarrier();
                _members = tempMembers;
                _values = tempValues;
            }
        }

        internal void WriteEnumValue(XmlWriterDelegator writer, object value)
        {
            long longValue = IsULong ? (long)Convert.ToUInt64(value, null) : Convert.ToInt64(value, null);
            for (int i = 0; i < Values!.Count; i++)
            {
                if (longValue == Values[i])
                {
                    writer.WriteString(ChildElementNames![i].Value);
                    return;
                }
            }
            if (IsFlags)
            {
                int zeroIndex = -1;
                bool noneWritten = true;
                for (int i = 0; i < Values.Count; i++)
                {
                    long current = Values[i];
                    if (current == 0)
                    {
                        zeroIndex = i;
                        continue;
                    }
                    if (longValue == 0)
                        break;
                    if ((current & longValue) == current)
                    {
                        if (noneWritten)
                            noneWritten = false;
                        else
                            writer.WriteString(DictionaryGlobals.Space.Value);

                        writer.WriteString(ChildElementNames![i].Value);
                        longValue &= ~current;
                    }
                }
                // enforce that enum value was completely parsed
                if (longValue != 0)
                    throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.InvalidEnumValueOnWrite, value, DataContract.GetClrTypeFullName(UnderlyingType)));

                if (noneWritten && zeroIndex >= 0)
                    writer.WriteString(ChildElementNames![zeroIndex].Value);
            }
            else
                throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.InvalidEnumValueOnWrite, value, DataContract.GetClrTypeFullName(UnderlyingType)));
        }

        internal object ReadEnumValue(XmlReaderDelegator reader)
        {
            string stringValue = reader.ReadElementContentAsString();
            long longValue = 0;
            int i = 0;
            if (IsFlags)
            {
                // Skip initial spaces
                for (; i < stringValue.Length; i++)
                    if (stringValue[i] != ' ')
                        break;

                // Read space-delimited values
                int startIndex = i;
                int count;
                for (; i < stringValue.Length; i++)
                {
                    if (stringValue[i] == ' ')
                    {
                        count = i - startIndex;
                        if (count > 0)
                            longValue |= ReadEnumValue(stringValue, startIndex, count);
                        for (++i; i < stringValue.Length; i++)
                            if (stringValue[i] != ' ')
                                break;
                        startIndex = i;
                        if (i == stringValue.Length)
                            break;
                    }
                }
                count = i - startIndex;
                if (count > 0)
                    longValue |= ReadEnumValue(stringValue, startIndex, count);
            }
            else
            {
                if (stringValue.Length == 0)
                    throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.InvalidEnumValueOnRead, stringValue, DataContract.GetClrTypeFullName(UnderlyingType)));
                longValue = ReadEnumValue(stringValue, 0, stringValue.Length);
            }

            if (IsULong)
                return Enum.ToObject(UnderlyingType, (object)(ulong)longValue);
            return Enum.ToObject(UnderlyingType, (object)longValue);
        }

        private long ReadEnumValue(string value, int index, int count)
        {
            for (int i = 0; i < Members.Count; i++)
            {
                string memberName = Members[i].Name;
                if (memberName.Length == count && string.CompareOrdinal(value, index, memberName, 0, count) == 0)
                {
                    return Values![i];
                }
            }
            throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.InvalidEnumValueOnRead, value.Substring(index, count), DataContract.GetClrTypeFullName(UnderlyingType)));
        }

        internal string GetStringFromEnumValue(long value)
        {
            if (IsULong)
            {
                return XmlConvert.ToString((ulong)value);
            }
            else
            {
                return XmlConvert.ToString(value);
            }
        }

        internal long GetEnumValueFromString(string value)
        {
            if (IsULong)
            {
                return (long)XmlConverter.ToUInt64(value);
            }
            else
            {
                return XmlConverter.ToInt64(value);
            }
        }

        internal override bool Equals(object? other, HashSet<DataContractPairKey>? checkedContracts)
        {
            if (IsEqualOrChecked(other, checkedContracts))
                return true;

            if (base.Equals(other, null))
            {
                if (other is EnumDataContract enumContract)
                {
                    if (Members.Count != enumContract.Members.Count || Values?.Count != enumContract.Values?.Count)
                        return false;
                    string[] memberNames1 = new string[Members.Count], memberNames2 = new string[Members.Count];
                    for (int i = 0; i < Members.Count; i++)
                    {
                        memberNames1[i] = Members[i].Name;
                        memberNames2[i] = enumContract.Members[i].Name;
                    }
                    Array.Sort(memberNames1);
                    Array.Sort(memberNames2);
                    for (int i = 0; i < Members.Count; i++)
                    {
                        if (memberNames1[i] != memberNames2[i])
                            return false;
                    }

                    return (IsFlags == enumContract.IsFlags);
                }
            }
            return false;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext? context)
        {
            WriteEnumValue(xmlWriter, obj);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override object ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext? context)
        {
            object obj = ReadEnumValue(xmlReader);
            context?.AddNewObject(obj);
            return obj;
        }
    }
}
