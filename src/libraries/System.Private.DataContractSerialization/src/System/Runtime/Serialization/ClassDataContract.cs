// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Xml;

using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContracts.DataContract>;

namespace System.Runtime.Serialization.DataContracts
{
    internal sealed class ClassDataContract : DataContract
    {
        internal const string ContractTypeString = nameof(ClassDataContract);
        public override string? ContractType => ContractTypeString;

        public XmlDictionaryString[]? ContractNamespaces;

        public XmlDictionaryString[]? MemberNames;

        internal XmlDictionaryString[]? MemberNamespaces;

        private XmlDictionaryString?[]? _childElementNamespaces;

        private ClassDataContractCriticalHelper _helper;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal ClassDataContract(Type type) : base(new ClassDataContractCriticalHelper(type))
        {
            InitClassDataContract();
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private ClassDataContract(Type type, XmlDictionaryString ns, string[] memberNames) : base(new ClassDataContractCriticalHelper(type, ns, memberNames))
        {
            InitClassDataContract();
        }

        [MemberNotNull(nameof(_helper))]
        private void InitClassDataContract()
        {
            _helper = (base.Helper as ClassDataContractCriticalHelper)!;
            ContractNamespaces = _helper.ContractNamespaces;
            MemberNames = _helper.MemberNames;
            MemberNamespaces = _helper.MemberNamespaces;
        }

        public override DataContract? BaseContract
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get => BaseClassContract;
        }

        internal ClassDataContract? BaseClassContract
        {
            get => _helper.BaseClassContract;
            set => _helper.BaseClassContract = value;
        }

        internal List<DataMember>? Members
        {
            get => _helper.Members;
            set => _helper.Members = value;
        }

        public override ReadOnlyCollection<DataMember> DataMembers => (Members == null) ? ReadOnlyCollection<DataMember>.Empty : Members.AsReadOnly();

        internal XmlDictionaryString?[]? ChildElementNamespaces
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (_childElementNamespaces == null)
                {
                    lock (this)
                    {
                        if (_childElementNamespaces == null)
                        {
                            if (_helper.ChildElementNamespaces == null)
                            {
                                XmlDictionaryString?[]? tempChildElementamespaces = CreateChildElementNamespaces();
                                Interlocked.MemoryBarrier();
                                _helper.ChildElementNamespaces = tempChildElementamespaces;
                            }
                            _childElementNamespaces = _helper.ChildElementNamespaces;
                        }
                    }
                }
                return _childElementNamespaces;
            }
        }

        internal MethodInfo? OnSerializing => _helper.OnSerializing;

        internal MethodInfo? OnSerialized => _helper.OnSerialized;

        internal MethodInfo? OnDeserializing => _helper.OnDeserializing;

        internal MethodInfo? OnDeserialized => _helper.OnDeserialized;

        internal MethodInfo? ExtensionDataSetMethod => _helper.ExtensionDataSetMethod;

        public override DataContractDictionary? KnownDataContracts
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get => _helper.KnownDataContracts;
            internal set => _helper.KnownDataContracts = value;
        }

        public override bool IsISerializable
        {
            get => _helper.IsISerializable;
            internal set => _helper.IsISerializable = value;
        }

        internal bool IsNonAttributedType => _helper.IsNonAttributedType;

        internal bool HasExtensionData => _helper.HasExtensionData;

        internal string? SerializationExceptionMessage => _helper.SerializationExceptionMessage;
        internal string? DeserializationExceptionMessage => _helper.DeserializationExceptionMessage;

        internal bool IsReadOnlyContract => DeserializationExceptionMessage != null;

        internal ConstructorInfo? GetISerializableConstructor()
        {
            return _helper.GetISerializableConstructor();
        }

        private ConstructorInfo? _nonAttributedTypeConstructor;

        internal ConstructorInfo? GetNonAttributedTypeConstructor()
        {
            if (_nonAttributedTypeConstructor == null)
            {
                // Cache the ConstructorInfo to improve performance.
                _nonAttributedTypeConstructor = _helper.GetNonAttributedTypeConstructor();
            }

            return _nonAttributedTypeConstructor;
        }

        private Func<object>? _makeNewInstance;

        [UnconditionalSuppressMessage("AOT Analysis", "IL3050:RequiresDynamicCodeAttribute",
            Justification = "Fields cannot be annotated, annotating the use instead")]
        private Func<object> MakeNewInstance => _makeNewInstance ??= FastInvokerBuilder.GetMakeNewInstanceFunc(UnderlyingType);


        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        internal bool CreateNewInstanceViaDefaultConstructor([NotNullWhen(true)] out object? obj)
        {
            ConstructorInfo? ci = GetNonAttributedTypeConstructor();
            if (ci == null || UnderlyingType == Globals.TypeOfSchemaDefinedType)
            {
                obj = null;
                return false;
            }

            if (ci.IsPublic)
            {
                // Optimization for calling public default ctor.
                obj = MakeNewInstance();
            }
            else
            {
                obj = ci.Invoke(Array.Empty<object>());
            }

            return true;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private XmlFormatClassWriterDelegate CreateXmlFormatWriterDelegate()
        {
            Debug.Assert(UnderlyingType != Globals.TypeOfSchemaDefinedType);
            return new XmlFormatWriterGenerator().GenerateClassWriter(this);
        }

        internal XmlFormatClassWriterDelegate XmlFormatWriterDelegate
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (_helper.XmlFormatWriterDelegate == null)
                {
                    lock (this)
                    {
                        if (_helper.XmlFormatWriterDelegate == null)
                        {
                            XmlFormatClassWriterDelegate tempDelegate = CreateXmlFormatWriterDelegate();
                            Interlocked.MemoryBarrier();
                            _helper.XmlFormatWriterDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.XmlFormatWriterDelegate;
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private XmlFormatClassReaderDelegate CreateXmlFormatReaderDelegate()
        {
            Debug.Assert(UnderlyingType != Globals.TypeOfSchemaDefinedType);
            return new XmlFormatReaderGenerator().GenerateClassReader(this);
        }

        internal XmlFormatClassReaderDelegate XmlFormatReaderDelegate
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (_helper.XmlFormatReaderDelegate == null)
                {
                    lock (this)
                    {
                        if (_helper.XmlFormatReaderDelegate == null)
                        {
                            if (IsReadOnlyContract)
                            {
                                ThrowInvalidDataContractException(DeserializationExceptionMessage, type: null);
                            }
                            XmlFormatClassReaderDelegate tempDelegate = CreateXmlFormatReaderDelegate();
                            Interlocked.MemoryBarrier();
                            _helper.XmlFormatReaderDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.XmlFormatReaderDelegate;
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static ClassDataContract CreateClassDataContractForKeyValue(Type type, XmlDictionaryString ns, string[] memberNames)
        {
            return new ClassDataContract(type, ns, memberNames);
        }

        internal static void CheckAndAddMember(List<DataMember> members, DataMember memberContract, Dictionary<string, DataMember> memberNamesTable)
        {
            if (memberNamesTable.TryGetValue(memberContract.Name, out DataMember? existingMemberContract))
            {
                Type declaringType = memberContract.MemberInfo.DeclaringType!;
                DataContract.ThrowInvalidDataContractException(
                    SR.Format((declaringType.IsEnum ? SR.DupEnumMemberValue : SR.DupMemberName),
                        existingMemberContract.MemberInfo.Name,
                        memberContract.MemberInfo.Name,
                        DataContract.GetClrTypeFullName(declaringType),
                        memberContract.Name),
                    declaringType);
            }
            memberNamesTable.Add(memberContract.Name, memberContract);
            members.Add(memberContract);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static XmlDictionaryString? GetChildNamespaceToDeclare(DataContract dataContract, Type childType, XmlDictionary dictionary)
        {
            childType = DataContract.UnwrapNullableType(childType);
            if (!childType.IsEnum && !Globals.TypeOfIXmlSerializable.IsAssignableFrom(childType)
                && DataContract.GetBuiltInDataContract(childType) == null && childType != Globals.TypeOfDBNull)
            {
                string ns = DataContract.GetXmlName(childType).Namespace;
                if (ns.Length > 0 && ns != dataContract.Namespace.Value)
                    return dictionary.Add(ns);
            }
            return null;
        }

        private static bool IsArraySegment(Type t)
        {
            return t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(ArraySegment<>));
        }

        /// <SecurityNote>
        /// RequiresReview - callers may need to depend on isNonAttributedType for a security decision
        ///            isNonAttributedType must be calculated correctly
        ///            IsNonAttributedTypeValidForSerialization is used as part of the isNonAttributedType calculation and
        ///            is therefore marked SRR
        /// Safe - does not let caller influence isNonAttributedType calculation; no harm in leaking value
        /// </SecurityNote>
        internal static bool IsNonAttributedTypeValidForSerialization(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)]
            Type type)
        {
            if (type.IsArray)
                return false;

            if (type.IsEnum)
                return false;

            if (type.IsGenericParameter)
                return false;

            if (Globals.TypeOfIXmlSerializable.IsAssignableFrom(type))
                return false;

            if (type.IsPointer)
                return false;

            if (type.IsDefined(Globals.TypeOfCollectionDataContractAttribute, false))
                return false;

            if (!IsArraySegment(type))
            {
                foreach (Type interfaceType in type.GetInterfaces())
                {
                    if (CollectionDataContract.IsCollectionInterface(interfaceType))
                        return false;
                }
            }

#pragma warning disable SYSLIB0050 // Type.IsSerializable is obsolete
            if (type.IsSerializable)
                return false;
#pragma warning restore SYSLIB0050

            if (Globals.TypeOfISerializable.IsAssignableFrom(type))
                return false;

            if (type.IsDefined(Globals.TypeOfDataContractAttribute, false))
                return false;

            if (type == Globals.TypeOfExtensionDataObject)
                return false;

            if (type.IsValueType)
                return type.IsVisible;

            return (type.IsVisible &&
                type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, Type.EmptyTypes) != null);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private XmlDictionaryString?[]? CreateChildElementNamespaces()
        {
            if (Members == null)
                return null;

            XmlDictionaryString?[]? baseChildElementNamespaces = null;
            if (BaseClassContract != null)
                baseChildElementNamespaces = BaseClassContract.ChildElementNamespaces;
            int baseChildElementNamespaceCount = (baseChildElementNamespaces != null) ? baseChildElementNamespaces.Length : 0;
            XmlDictionaryString?[] childElementNamespaces = new XmlDictionaryString?[Members.Count + baseChildElementNamespaceCount];
            if (baseChildElementNamespaceCount > 0)
                Array.Copy(baseChildElementNamespaces!, childElementNamespaces, baseChildElementNamespaces!.Length);

            XmlDictionary dictionary = new XmlDictionary();
            for (int i = 0; i < Members.Count; i++)
            {
                childElementNamespaces[i + baseChildElementNamespaceCount] = GetChildNamespaceToDeclare(this, Members[i].MemberType, dictionary);
            }

            return childElementNamespaces;
        }

        private void EnsureMethodsImported()
        {
            _helper.EnsureMethodsImported();
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext? context)
        {
            Debug.Assert(context != null);
            XmlFormatWriterDelegate(xmlWriter, obj, context, this);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override object? ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext? context)
        {
            xmlReader.Read();
            object? o = XmlFormatReaderDelegate(xmlReader, context, MemberNames, MemberNamespaces);
            xmlReader.ReadEndElement();
            return o;
        }

        /// <SecurityNote>
        /// Review - calculates whether this class requires MemberAccessPermission for deserialization.
        ///          since this information is used to determine whether to give the generated code access
        ///          permissions to private members, any changes to the logic should be reviewed.
        /// </SecurityNote>
        internal bool RequiresMemberAccessForRead(SecurityException? securityException)
        {
            EnsureMethodsImported();
            if (!IsTypeVisible(UnderlyingType))
            {
                if (securityException != null)
                {
                    throw new SecurityException(SR.Format(
                                SR.PartialTrustDataContractTypeNotPublic,
                                DataContract.GetClrTypeFullName(UnderlyingType)),
                            securityException);
                }
                return true;
            }
            if (BaseClassContract != null && BaseClassContract.RequiresMemberAccessForRead(securityException))
                return true;

            if (ConstructorRequiresMemberAccess(GetISerializableConstructor()))
            {
                if (securityException != null)
                {
                    throw new SecurityException(SR.Format(
                                SR.PartialTrustIXmlSerialzableNoPublicConstructor,
                                DataContract.GetClrTypeFullName(UnderlyingType)),
                            securityException);
                }
                return true;
            }

            if (ConstructorRequiresMemberAccess(GetNonAttributedTypeConstructor()))
            {
                if (securityException != null)
                {
                    throw new SecurityException(SR.Format(
                                SR.PartialTrustNonAttributedSerializableTypeNoPublicConstructor,
                                DataContract.GetClrTypeFullName(UnderlyingType)),
                            securityException);
                }
                return true;
            }

            if (MethodRequiresMemberAccess(OnDeserializing))
            {
                if (securityException != null)
                {
                    throw new SecurityException(SR.Format(
                                SR.PartialTrustDataContractOnDeserializingNotPublic,
                                DataContract.GetClrTypeFullName(UnderlyingType),
                                OnDeserializing!.Name),
                            securityException);
                }
                return true;
            }

            if (MethodRequiresMemberAccess(OnDeserialized))
            {
                if (securityException != null)
                {
                    throw new SecurityException(SR.Format(
                                SR.PartialTrustDataContractOnDeserializedNotPublic,
                                DataContract.GetClrTypeFullName(UnderlyingType),
                                OnDeserialized!.Name),
                            securityException);
                }
                return true;
            }

            if (Members != null)
            {
                for (int i = 0; i < Members.Count; i++)
                {
                    if (Members[i].RequiresMemberAccessForSet())
                    {
                        if (securityException != null)
                        {
                            if (Members[i].MemberInfo is FieldInfo)
                            {
                                throw new SecurityException(SR.Format(
                                            SR.PartialTrustDataContractFieldSetNotPublic,
                                            DataContract.GetClrTypeFullName(UnderlyingType),
                                            Members[i].MemberInfo.Name),
                                        securityException);
                            }
                            else
                            {
                                throw new SecurityException(SR.Format(
                                            SR.PartialTrustDataContractPropertySetNotPublic,
                                            DataContract.GetClrTypeFullName(UnderlyingType),
                                            Members[i].MemberInfo.Name),
                                        securityException);
                            }
                        }
                        return true;
                    }
                }
            }

            return false;
        }

        /// <SecurityNote>
        /// Review - calculates whether this class requires MemberAccessPermission for serialization.
        ///          since this information is used to determine whether to give the generated code access
        ///          permissions to private members, any changes to the logic should be reviewed.
        /// </SecurityNote>
        internal bool RequiresMemberAccessForWrite(SecurityException? securityException)
        {
            EnsureMethodsImported();

            if (!IsTypeVisible(UnderlyingType))
            {
                if (securityException != null)
                {
                    throw new SecurityException(SR.Format(
                                SR.PartialTrustDataContractTypeNotPublic,
                                DataContract.GetClrTypeFullName(UnderlyingType)),
                            securityException);
                }
                return true;
            }

            if (BaseClassContract != null && BaseClassContract.RequiresMemberAccessForWrite(securityException))
                return true;

            if (MethodRequiresMemberAccess(OnSerializing))
            {
                if (securityException != null)
                {
                    throw new SecurityException(SR.Format(
                                SR.PartialTrustDataContractOnSerializingNotPublic,
                                DataContract.GetClrTypeFullName(UnderlyingType),
                                OnSerializing!.Name),
                            securityException);
                }
                return true;
            }

            if (MethodRequiresMemberAccess(OnSerialized))
            {
                if (securityException != null)
                {
                    throw new SecurityException(SR.Format(
                                SR.PartialTrustDataContractOnSerializedNotPublic,
                                DataContract.GetClrTypeFullName(UnderlyingType),
                                OnSerialized!.Name),
                            securityException);
                }
                return true;
            }

            if (Members != null)
            {
                for (int i = 0; i < Members.Count; i++)
                {
                    if (Members[i].RequiresMemberAccessForGet())
                    {
                        if (securityException != null)
                        {
                            if (Members[i].MemberInfo is FieldInfo)
                            {
                                throw new SecurityException(SR.Format(
                                            SR.PartialTrustDataContractFieldGetNotPublic,
                                            DataContract.GetClrTypeFullName(UnderlyingType),
                                            Members[i].MemberInfo.Name),
                                        securityException);
                            }
                            else
                            {
                                throw new SecurityException(SR.Format(
                                            SR.PartialTrustDataContractPropertyGetNotPublic,
                                            DataContract.GetClrTypeFullName(UnderlyingType),
                                            Members[i].MemberInfo.Name),
                                        securityException);
                            }
                        }
                        return true;
                    }
                }
            }

            return false;
        }

        private sealed class ClassDataContractCriticalHelper : DataContract.DataContractCriticalHelper
        {
            private static Type[]? s_serInfoCtorArgs;

            private ClassDataContract? _baseContract;
            private List<DataMember>? _members;
            private MethodInfo? _onSerializing, _onSerialized;
            private MethodInfo? _onDeserializing, _onDeserialized;
            private MethodInfo? _extensionDataSetMethod;
            private DataContractDictionary? _knownDataContracts;
            private string? _serializationExceptionMessage;
            private bool _isKnownTypeAttributeChecked;
            private bool _isMethodChecked;

            /// <SecurityNote>
            /// in serialization/deserialization we base the decision whether to Demand SerializationFormatter permission on this value and isNonAttributedType
            /// </SecurityNote>
            private bool _isNonAttributedType;

            /// <SecurityNote>
            /// in serialization/deserialization we base the decision whether to Demand SerializationFormatter permission on this value and hasDataContract
            /// </SecurityNote>
            private bool _hasDataContract;
            private readonly bool _hasExtensionData;

            internal XmlDictionaryString[]? ContractNamespaces;
            internal XmlDictionaryString[]? MemberNames;
            internal XmlDictionaryString[]? MemberNamespaces;

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal ClassDataContractCriticalHelper([DynamicallyAccessedMembers(DataContractPreserveMemberTypes)]
                Type type) : base(type)
            {
                XmlQualifiedName xmlName = GetXmlNameAndSetHasDataContract(type);
                if (type == Globals.TypeOfDBNull)
                {
                    XmlName = xmlName;
                    _members = new List<DataMember>();
                    XmlDictionary dictionary = new XmlDictionary(2);
                    Name = dictionary.Add(XmlName.Name);
                    Namespace = dictionary.Add(XmlName.Namespace);
                    ContractNamespaces = MemberNames = MemberNamespaces = Array.Empty<XmlDictionaryString>();
                    EnsureMethodsImported();
                    return;
                }
                Type? baseType = type.BaseType;
                IsISerializable = (Globals.TypeOfISerializable.IsAssignableFrom(type));
                SetIsNonAttributedType(type);
                if (IsISerializable)
                {
                    if (HasDataContract)
                        throw new InvalidDataContractException(SR.Format(SR.ISerializableCannotHaveDataContract, DataContract.GetClrTypeFullName(type)));
#pragma warning disable SYSLIB0050 // Type.IsSerializable is obsolete
                    if (baseType != null && !(baseType.IsSerializable && Globals.TypeOfISerializable.IsAssignableFrom(baseType)))
                        baseType = null;
#pragma warning restore SYSLIB0050
                }
                IsValueType = type.IsValueType;
                if (baseType != null && baseType != Globals.TypeOfObject && baseType != Globals.TypeOfValueType && baseType != Globals.TypeOfUri)
                {
                    DataContract baseContract = DataContract.GetDataContract(baseType);
                    if (baseContract is CollectionDataContract collectionDC)
                    {
                        BaseClassContract = collectionDC.SharedTypeContract as ClassDataContract;
                    }
                    else
                    {
                        BaseClassContract = baseContract as ClassDataContract;
                    }

                    if (BaseClassContract != null && BaseClassContract.IsNonAttributedType && !_isNonAttributedType)
                    {
                        throw new InvalidDataContractException(SR.Format(SR.AttributedTypesCannotInheritFromNonAttributedSerializableTypes,
                            DataContract.GetClrTypeFullName(type), DataContract.GetClrTypeFullName(baseType)));
                    }
                }
                else
                {
                    BaseClassContract = null;
                }

                _hasExtensionData = (Globals.TypeOfIExtensibleDataObject.IsAssignableFrom(type));
                if (_hasExtensionData && !HasDataContract && !IsNonAttributedType)
                {
                    throw new InvalidDataContractException(SR.Format(SR.OnlyDataContractTypesCanHaveExtensionData, DataContract.GetClrTypeFullName(type)));
                }

                if (IsISerializable)
                {
                    SetDataContractName(xmlName);
                }
                else
                {
                    XmlName = xmlName;
                    ImportDataMembers();
                    XmlDictionary dictionary = new XmlDictionary(2 + Members.Count);
                    Name = dictionary.Add(XmlName.Name);
                    Namespace = dictionary.Add(XmlName.Namespace);

                    int baseMemberCount = 0;
                    int baseContractCount = 0;
                    if (BaseClassContract == null)
                    {
                        MemberNames = new XmlDictionaryString[Members.Count];
                        MemberNamespaces = new XmlDictionaryString[Members.Count];
                        ContractNamespaces = new XmlDictionaryString[1];
                    }
                    else
                    {
                        if (BaseClassContract.IsReadOnlyContract)
                        {
                            _serializationExceptionMessage = BaseClassContract.SerializationExceptionMessage;
                        }
                        baseMemberCount = BaseClassContract.MemberNames!.Length;
                        MemberNames = new XmlDictionaryString[Members.Count + baseMemberCount];
                        Array.Copy(BaseClassContract.MemberNames, MemberNames, baseMemberCount);
                        MemberNamespaces = new XmlDictionaryString[Members.Count + baseMemberCount];
                        Array.Copy(BaseClassContract.MemberNamespaces!, MemberNamespaces, baseMemberCount);
                        baseContractCount = BaseClassContract.ContractNamespaces!.Length;
                        ContractNamespaces = new XmlDictionaryString[1 + baseContractCount];
                        Array.Copy(BaseClassContract.ContractNamespaces, ContractNamespaces, baseContractCount);
                    }
                    ContractNamespaces[baseContractCount] = Namespace;
                    for (int i = 0; i < Members.Count; i++)
                    {
                        MemberNames[i + baseMemberCount] = dictionary.Add(Members[i].Name);
                        MemberNamespaces[i + baseMemberCount] = Namespace;
                    }
                }

                EnsureMethodsImported();
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal ClassDataContractCriticalHelper(
                [DynamicallyAccessedMembers(DataContractPreserveMemberTypes)]
                Type type, XmlDictionaryString ns, string[] memberNames) : base(type)
            {
                XmlName = new XmlQualifiedName(GetXmlNameAndSetHasDataContract(type).Name, ns.Value);
                ImportDataMembers();
                XmlDictionary dictionary = new XmlDictionary(1 + Members.Count);
                Name = dictionary.Add(XmlName.Name);
                Namespace = ns;
                ContractNamespaces = new XmlDictionaryString[] { Namespace };
                MemberNames = new XmlDictionaryString[Members.Count];
                MemberNamespaces = new XmlDictionaryString[Members.Count];
                for (int i = 0; i < Members.Count; i++)
                {
                    Members[i].Name = memberNames[i];
                    MemberNames[i] = dictionary.Add(Members[i].Name);
                    MemberNamespaces[i] = Namespace;
                }
                EnsureMethodsImported();
            }

            private void EnsureIsReferenceImported(Type type)
            {
                bool isReference = false;
                bool hasDataContractAttribute = TryGetDCAttribute(type, out DataContractAttribute? dataContractAttribute);

                if (BaseClassContract != null)
                {
                    if (hasDataContractAttribute && dataContractAttribute!.IsReferenceSetExplicitly)
                    {
                        bool baseIsReference = BaseClassContract.IsReference;
                        if ((baseIsReference && !dataContractAttribute.IsReference) ||
                            (!baseIsReference && dataContractAttribute.IsReference))
                        {
                            DataContract.ThrowInvalidDataContractException(
                                    SR.Format(SR.InconsistentIsReference,
                                        DataContract.GetClrTypeFullName(type),
                                        dataContractAttribute.IsReference,
                                        DataContract.GetClrTypeFullName(BaseClassContract.UnderlyingType),
                                        BaseClassContract.IsReference),
                                    type);
                        }
                        else
                        {
                            isReference = dataContractAttribute.IsReference;
                        }
                    }
                    else
                    {
                        isReference = BaseClassContract.IsReference;
                    }
                }
                else if (hasDataContractAttribute)
                {
                    if (dataContractAttribute!.IsReference)
                        isReference = dataContractAttribute.IsReference;
                }

                if (isReference && type.IsValueType)
                {
                    DataContract.ThrowInvalidDataContractException(
                            SR.Format(SR.ValueTypeCannotHaveIsReference,
                                DataContract.GetClrTypeFullName(type),
                                true,
                                false),
                            type);
                    return;
                }

                IsReference = isReference;
            }

            [MemberNotNull(nameof(_members))]
            [MemberNotNull(nameof(Members))]
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private void ImportDataMembers()
            {
                Type type = UnderlyingType;
                EnsureIsReferenceImported(type);
                List<DataMember> tempMembers = new List<DataMember>();
                Dictionary<string, DataMember> memberNamesTable = new Dictionary<string, DataMember>();

                MemberInfo[] memberInfos;

                if (_isNonAttributedType)
                {
                    memberInfos = type.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                }
                else
                {
                    memberInfos = type.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }

                for (int i = 0; i < memberInfos.Length; i++)
                {
                    MemberInfo member = memberInfos[i];
                    if (HasDataContract)
                    {
                        object[] memberAttributes = member.GetCustomAttributes(typeof(DataMemberAttribute), false).ToArray();
                        if (memberAttributes != null && memberAttributes.Length > 0)
                        {
                            if (memberAttributes.Length > 1)
                                ThrowInvalidDataContractException(SR.Format(SR.TooManyDataMembers, DataContract.GetClrTypeFullName(member.DeclaringType!), member.Name));

                            DataMember memberContract = new DataMember(member);

                            if (member is PropertyInfo property)
                            {
                                MethodInfo? getMethod = property.GetMethod;
                                if (getMethod != null && IsMethodOverriding(getMethod))
                                    continue;
                                MethodInfo? setMethod = property.SetMethod;
                                if (setMethod != null && IsMethodOverriding(setMethod))
                                    continue;
                                if (getMethod == null)
                                    ThrowInvalidDataContractException(SR.Format(SR.NoGetMethodForProperty, property.DeclaringType, property.Name));
                                if (setMethod == null)
                                {
                                    if (!SetIfGetOnlyCollection(memberContract, skipIfReadOnlyContract: false))
                                    {
                                        _serializationExceptionMessage = SR.Format(SR.NoSetMethodForProperty, property.DeclaringType, property.Name);
                                    }
                                }
                                if (getMethod.GetParameters().Length > 0)
                                    ThrowInvalidDataContractException(SR.Format(SR.IndexedPropertyCannotBeSerialized, property.DeclaringType, property.Name));
                            }
                            else if (!(member is FieldInfo))
                                ThrowInvalidDataContractException(SR.Format(SR.InvalidMember, DataContract.GetClrTypeFullName(type), member.Name));

                            DataMemberAttribute memberAttribute = (DataMemberAttribute)memberAttributes[0];
                            if (memberAttribute.IsNameSetExplicitly)
                            {
                                if (string.IsNullOrEmpty(memberAttribute.Name))
                                    ThrowInvalidDataContractException(SR.Format(SR.InvalidDataMemberName, member.Name, DataContract.GetClrTypeFullName(type)));
                                memberContract.Name = memberAttribute.Name;
                            }
                            else
                                memberContract.Name = member.Name;

                            memberContract.Name = DataContract.EncodeLocalName(memberContract.Name);
                            memberContract.IsNullable = DataContract.IsTypeNullable(memberContract.MemberType);
                            memberContract.IsRequired = memberAttribute.IsRequired;
                            if (memberAttribute.IsRequired && IsReference)
                            {
                                ThrowInvalidDataContractException(
                                    SR.Format(SR.IsRequiredDataMemberOnIsReferenceDataContractType,
                                    DataContract.GetClrTypeFullName(member.DeclaringType!),
                                    member.Name, true), type);
                            }
                            memberContract.EmitDefaultValue = memberAttribute.EmitDefaultValue;
                            memberContract.Order = memberAttribute.Order;
                            CheckAndAddMember(tempMembers, memberContract, memberNamesTable);
                        }
                    }
                    else if (_isNonAttributedType)
                    {
                        FieldInfo? field = member as FieldInfo;
                        PropertyInfo? property = member as PropertyInfo;
                        if ((field == null && property == null) || (field != null && field.IsInitOnly))
                            continue;

                        object[] memberAttributes = member.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), false).ToArray();
                        if (memberAttributes != null && memberAttributes.Length > 0)
                        {
                            if (memberAttributes.Length > 1)
                                ThrowInvalidDataContractException(SR.Format(SR.TooManyIgnoreDataMemberAttributes, DataContract.GetClrTypeFullName(member.DeclaringType!), member.Name));
                            else
                                continue;
                        }
                        DataMember memberContract = new DataMember(member);
                        if (property != null)
                        {
                            MethodInfo? getMethod = property.GetGetMethod();
                            if (getMethod == null || IsMethodOverriding(getMethod) || getMethod.GetParameters().Length > 0)
                                continue;

                            MethodInfo? setMethod = property.SetMethod;
                            if (setMethod == null)
                            {
                                if (!SetIfGetOnlyCollection(memberContract, skipIfReadOnlyContract: true))
                                    continue;
                            }
                            else
                            {
                                if (!setMethod.IsPublic || IsMethodOverriding(setMethod))
                                    continue;
                            }

                            //skip ExtensionData member of type ExtensionDataObject if IExtensibleDataObject is implemented in non-attributed type
                            if (_hasExtensionData && memberContract.MemberType == Globals.TypeOfExtensionDataObject
                                && member.Name == Globals.ExtensionDataObjectPropertyName)
                                continue;
                        }

                        memberContract.Name = DataContract.EncodeLocalName(member.Name);
                        memberContract.IsNullable = DataContract.IsTypeNullable(memberContract.MemberType);
                        CheckAndAddMember(tempMembers, memberContract, memberNamesTable);
                    }
                    else
                    {
                        FieldInfo? field = member as FieldInfo;

#pragma warning disable SYSLIB0050 // Field.IsNotSerialized is obsolete
                        if (field != null && !field.IsNotSerialized)
#pragma warning restore SYSLIB0050
                        {
                            DataMember memberContract = new DataMember(member);

                            memberContract.Name = DataContract.EncodeLocalName(member.Name);
                            object[] optionalFields = field!.GetCustomAttributes(Globals.TypeOfOptionalFieldAttribute, false);
                            if (optionalFields == null || optionalFields.Length == 0)
                            {
                                if (IsReference)
                                {
                                    ThrowInvalidDataContractException(
                                        SR.Format(SR.NonOptionalFieldMemberOnIsReferenceSerializableType,
                                        DataContract.GetClrTypeFullName(member.DeclaringType!),
                                        member.Name, true), type);
                                }
                                memberContract.IsRequired = true;
                            }
                            memberContract.IsNullable = DataContract.IsTypeNullable(memberContract.MemberType);
                            CheckAndAddMember(tempMembers, memberContract, memberNamesTable);
                        }
                    }
                }
                if (tempMembers.Count > 1)
                    tempMembers.Sort(DataMemberComparer.Singleton);

                SetIfMembersHaveConflict(tempMembers);

                Interlocked.MemoryBarrier();
                _members = tempMembers;
                Debug.Assert(Members != null);
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private static bool SetIfGetOnlyCollection(DataMember memberContract, bool skipIfReadOnlyContract)
            {
                //OK to call IsCollection here since the use of surrogated collection types is not supported in get-only scenarios
                if (CollectionDataContract.IsCollection(memberContract.MemberType, false /*isConstructorRequired*/, skipIfReadOnlyContract) && !memberContract.MemberType.IsValueType)
                {
                    memberContract.IsGetOnlyCollection = true;
                    return true;
                }
                return false;
            }

            private void SetIfMembersHaveConflict(List<DataMember> members)
            {
                if (BaseClassContract == null)
                    return;

                int baseTypeIndex = 0;
                List<Member> membersInHierarchy = new List<Member>();
                foreach (DataMember member in members)
                {
                    membersInHierarchy.Add(new Member(member, XmlName!.Namespace, baseTypeIndex));
                }
                ClassDataContract? currContract = BaseClassContract;
                while (currContract != null)
                {
                    baseTypeIndex++;

                    foreach (DataMember member in currContract.Members!)
                    {
                        membersInHierarchy.Add(new Member(member, currContract.XmlName!.Namespace, baseTypeIndex));
                    }
                    currContract = currContract.BaseClassContract;
                }

                IComparer<Member> comparer = DataMemberConflictComparer.Singleton;
                membersInHierarchy.Sort(comparer);

                for (int i = 0; i < membersInHierarchy.Count - 1; i++)
                {
                    int startIndex = i;
                    int endIndex = i;
                    bool hasConflictingType = false;
                    while (endIndex < membersInHierarchy.Count - 1
                        && membersInHierarchy[endIndex]._member.Name == membersInHierarchy[endIndex + 1]._member.Name
                        && membersInHierarchy[endIndex]._ns == membersInHierarchy[endIndex + 1]._ns)
                    {
                        membersInHierarchy[endIndex]._member.ConflictingMember = membersInHierarchy[endIndex + 1]._member;
                        if (!hasConflictingType)
                        {
                            if (membersInHierarchy[endIndex + 1]._member.HasConflictingNameAndType)
                            {
                                hasConflictingType = true;
                            }
                            else
                            {
                                hasConflictingType = (membersInHierarchy[endIndex]._member.MemberType != membersInHierarchy[endIndex + 1]._member.MemberType);
                            }
                        }
                        endIndex++;
                    }

                    if (hasConflictingType)
                    {
                        for (int j = startIndex; j <= endIndex; j++)
                        {
                            membersInHierarchy[j]._member.HasConflictingNameAndType = true;
                        }
                    }

                    i = endIndex + 1;
                }
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private XmlQualifiedName GetXmlNameAndSetHasDataContract(Type type)
            {
                return DataContract.GetXmlName(type, out _hasDataContract);
            }

            /// <SecurityNote>
            /// RequiresReview - marked SRR because callers may need to depend on isNonAttributedType for a security decision
            ///            isNonAttributedType must be calculated correctly
            ///            SetIsNonAttributedType should not be called before GetXmlNameAndSetHasDataContract since it
            ///            is dependent on the correct calculation of hasDataContract
            /// Safe - does not let caller influence isNonAttributedType calculation; no harm in leaking value
            /// </SecurityNote>
            private void SetIsNonAttributedType(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)]
                Type type)
            {
#pragma warning disable SYSLIB0050 // Type.IsSerializable is obsolete
                _isNonAttributedType = !type.IsSerializable && !_hasDataContract && IsNonAttributedTypeValidForSerialization(type);
#pragma warning restore SYSLIB0050
            }

            private static bool IsMethodOverriding(MethodInfo method)
            {
                return method.IsVirtual && ((method.Attributes & MethodAttributes.NewSlot) == 0);
            }

            internal void EnsureMethodsImported()
            {
                if (!_isMethodChecked && UnderlyingType != null)
                {
                    lock (this)
                    {
                        if (!_isMethodChecked)
                        {
                            Type type = UnderlyingType;
                            MethodInfo[] methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            for (int i = 0; i < methods.Length; i++)
                            {
                                MethodInfo method = methods[i];
                                Type? prevAttributeType = null;
                                ParameterInfo[] parameters = method.GetParameters();
                                if (HasExtensionData && IsValidExtensionDataSetMethod(method, parameters))
                                {
                                    if (method.Name == Globals.ExtensionDataSetExplicitMethod || !method.IsPublic)
                                        _extensionDataSetMethod = XmlFormatGeneratorStatics.ExtensionDataSetExplicitMethodInfo;
                                    else
                                        _extensionDataSetMethod = method;
                                }
                                if (IsValidCallback(method, parameters, Globals.TypeOfOnSerializingAttribute, _onSerializing, ref prevAttributeType))
                                    _onSerializing = method;
                                if (IsValidCallback(method, parameters, Globals.TypeOfOnSerializedAttribute, _onSerialized, ref prevAttributeType))
                                    _onSerialized = method;
                                if (IsValidCallback(method, parameters, Globals.TypeOfOnDeserializingAttribute, _onDeserializing, ref prevAttributeType))
                                    _onDeserializing = method;
                                if (IsValidCallback(method, parameters, Globals.TypeOfOnDeserializedAttribute, _onDeserialized, ref prevAttributeType))
                                    _onDeserialized = method;
                            }
                            Interlocked.MemoryBarrier();
                            _isMethodChecked = true;
                        }
                    }
                }
            }

            private bool IsValidExtensionDataSetMethod(MethodInfo method, ParameterInfo[] parameters)
            {
                if (method.Name == Globals.ExtensionDataSetExplicitMethod || method.Name == Globals.ExtensionDataSetMethod)
                {
                    Debug.Assert(method.DeclaringType != null);

                    if (_extensionDataSetMethod != null)
                        ThrowInvalidDataContractException(SR.Format(SR.DuplicateExtensionDataSetMethod, method, _extensionDataSetMethod, DataContract.GetClrTypeFullName(method.DeclaringType)));
                    if (method.ReturnType != Globals.TypeOfVoid)
                        DataContract.ThrowInvalidDataContractException(SR.Format(SR.ExtensionDataSetMustReturnVoid, DataContract.GetClrTypeFullName(method.DeclaringType), method), method.DeclaringType);
                    if (parameters == null || parameters.Length != 1 || parameters[0].ParameterType != Globals.TypeOfExtensionDataObject)
                        DataContract.ThrowInvalidDataContractException(SR.Format(SR.ExtensionDataSetParameterInvalid, DataContract.GetClrTypeFullName(method.DeclaringType), method, Globals.TypeOfExtensionDataObject), method.DeclaringType);
                    return true;
                }
                return false;
            }

            private static bool IsValidCallback(MethodInfo method, ParameterInfo[] parameters, Type attributeType, MethodInfo? currentCallback, ref Type? prevAttributeType)
            {
                if (method.IsDefined(attributeType, false))
                {
                    Debug.Assert(method.DeclaringType != null);

                    if (currentCallback != null)
                        DataContract.ThrowInvalidDataContractException(SR.Format(SR.DuplicateCallback, method, currentCallback, DataContract.GetClrTypeFullName(method.DeclaringType), attributeType), method.DeclaringType);
                    else if (prevAttributeType != null)
                        DataContract.ThrowInvalidDataContractException(SR.Format(SR.DuplicateAttribute, prevAttributeType, attributeType, DataContract.GetClrTypeFullName(method.DeclaringType), method), method.DeclaringType);
                    else if (method.IsVirtual)
                        DataContract.ThrowInvalidDataContractException(SR.Format(SR.CallbacksCannotBeVirtualMethods, method, DataContract.GetClrTypeFullName(method.DeclaringType), attributeType), method.DeclaringType);
                    else
                    {
                        if (method.ReturnType != Globals.TypeOfVoid)
                            DataContract.ThrowInvalidDataContractException(SR.Format(SR.CallbackMustReturnVoid, DataContract.GetClrTypeFullName(method.DeclaringType), method), method.DeclaringType);
                        if (parameters == null || parameters.Length != 1 || parameters[0].ParameterType != Globals.TypeOfStreamingContext)
                            DataContract.ThrowInvalidDataContractException(SR.Format(SR.CallbackParameterInvalid, DataContract.GetClrTypeFullName(method.DeclaringType), method, Globals.TypeOfStreamingContext), method.DeclaringType);

                        prevAttributeType = attributeType;
                    }
                    return true;
                }
                return false;
            }

            internal ClassDataContract? BaseClassContract
            {
                get => _baseContract;
                set
                {
                    _baseContract = value;
                    if (_baseContract != null && IsValueType)
                        ThrowInvalidDataContractException(SR.Format(SR.ValueTypeCannotHaveBaseType, XmlName!.Name, XmlName.Namespace, _baseContract.XmlName!.Name, _baseContract.XmlName.Namespace));
                }
            }

            internal List<DataMember>? Members
            {
                get => _members;
                set => _members = value;
            }

            internal MethodInfo? OnSerializing
            {
                get
                {
                    EnsureMethodsImported();
                    return _onSerializing;
                }
            }

            internal MethodInfo? OnSerialized
            {
                get
                {
                    EnsureMethodsImported();
                    return _onSerialized;
                }
            }

            internal MethodInfo? OnDeserializing
            {
                get
                {
                    EnsureMethodsImported();
                    return _onDeserializing;
                }
            }

            internal MethodInfo? OnDeserialized
            {
                get
                {
                    EnsureMethodsImported();
                    return _onDeserialized;
                }
            }

            internal MethodInfo? ExtensionDataSetMethod
            {
                get
                {
                    EnsureMethodsImported();
                    return _extensionDataSetMethod;
                }
            }

            internal override DataContractDictionary? KnownDataContracts
            {
                [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
                [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
                get
                {
                    if (!_isKnownTypeAttributeChecked && UnderlyingType != null)
                    {
                        lock (this)
                        {
                            if (!_isKnownTypeAttributeChecked)
                            {
                                _knownDataContracts = DataContract.ImportKnownTypeAttributes(UnderlyingType);
                                Interlocked.MemoryBarrier();
                                _isKnownTypeAttributeChecked = true;
                            }
                            _knownDataContracts ??= new DataContractDictionary();
                        }
                    }
                    return _knownDataContracts;
                }

                set { _knownDataContracts = value; }
            }

            internal string? SerializationExceptionMessage => _serializationExceptionMessage;

            internal string? DeserializationExceptionMessage => (_serializationExceptionMessage == null) ? null : SR.Format(SR.ReadOnlyClassDeserialization, _serializationExceptionMessage);

            internal override bool IsISerializable { get; set; }

            internal bool HasDataContract => _hasDataContract;

            internal bool HasExtensionData => _hasExtensionData;

            internal bool IsNonAttributedType => _isNonAttributedType;

            internal ConstructorInfo? GetISerializableConstructor()
            {
                if (!IsISerializable)
                    return null;

                ConstructorInfo? ctor = UnderlyingType.GetConstructor(Globals.ScanAllMembers, SerInfoCtorArgs);
                if (ctor == null)
                    throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.SerializationInfo_ConstructorNotFound, DataContract.GetClrTypeFullName(UnderlyingType)));

                return ctor;
            }

            internal ConstructorInfo? GetNonAttributedTypeConstructor()
            {
                if (!IsNonAttributedType)
                    return null;

                Type type = UnderlyingType;

                if (type.IsValueType)
                    return null;

                ConstructorInfo? ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, Type.EmptyTypes);
                if (ctor == null)
                    throw new InvalidDataContractException(SR.Format(SR.NonAttributedSerializableTypesMustHaveDefaultConstructor, DataContract.GetClrTypeFullName(type)));

                return ctor;
            }

            internal XmlFormatClassWriterDelegate? XmlFormatWriterDelegate { get; set; }

            internal XmlFormatClassReaderDelegate? XmlFormatReaderDelegate { get; set; }

            internal XmlDictionaryString?[]? ChildElementNamespaces { get; set; }

            private static Type[] SerInfoCtorArgs => s_serInfoCtorArgs ??= new Type[] { typeof(SerializationInfo), typeof(StreamingContext) };

            internal readonly struct Member
            {
                internal Member(DataMember member, string ns, int baseTypeIndex)
                {
                    _member = member;
                    _ns = ns;
                    _baseTypeIndex = baseTypeIndex;
                }
                internal readonly DataMember _member;
                internal readonly string _ns;
                internal readonly int _baseTypeIndex;
            }

            internal sealed class DataMemberConflictComparer : IComparer<Member>
            {
                public int Compare(Member x, Member y)
                {
                    int nsCompare = string.CompareOrdinal(x._ns, y._ns);
                    if (nsCompare != 0)
                        return nsCompare;

                    int nameCompare = string.CompareOrdinal(x._member.Name, y._member.Name);
                    if (nameCompare != 0)
                        return nameCompare;

                    return x._baseTypeIndex - y._baseTypeIndex;
                }

                internal static DataMemberConflictComparer Singleton = new DataMemberConflictComparer();
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override DataContract BindGenericParameters(DataContract[] paramContracts, Dictionary<DataContract, DataContract>? boundContracts = null)
        {
            Type type = UnderlyingType;
            if (!type.IsGenericType || !type.ContainsGenericParameters)
                return this;

            lock (this)
            {
                if (boundContracts != null && boundContracts.TryGetValue(this, out DataContract? boundContract))
                    return boundContract;

                XmlQualifiedName xmlName;
                object[] genericParams;
                Type boundType;
                if (type.IsGenericTypeDefinition)
                {
                    xmlName = XmlName;
                    genericParams = paramContracts;

                    // This type-binding ('boundType') stuff is new. We did not do this in NetFx. We used to use default contract constructors and let the
                    // underlying type get filled in later. But default constructors for DataContracts runs afoul of requiring an underlying type. Our web of nullable
                    // notations make it hard to get around. But it also allows us to feel good about using .UnderlyingType from matching parameter contracts.
                    Type[] underlyingParamTypes = new Type[paramContracts.Length];
                    for (int i = 0; i < paramContracts.Length; i++)
                        underlyingParamTypes[i] = paramContracts[i].UnderlyingType;
                    boundType = type.MakeGenericType(underlyingParamTypes);
                }
                else
                {
                    //partial Generic: Construct xml name from its open generic type definition
                    xmlName = DataContract.GetXmlName(type.GetGenericTypeDefinition());
                    Type[] paramTypes = type.GetGenericArguments();
                    genericParams = new object[paramTypes.Length];
                    for (int i = 0; i < paramTypes.Length; i++)
                    {
                        Type paramType = paramTypes[i];
                        if (paramType.IsGenericParameter)
                        {
                            genericParams[i] = paramContracts[paramType.GenericParameterPosition];
                            paramTypes[i] = paramContracts[paramType.GenericParameterPosition].UnderlyingType;
                        }
                        else
                        {
                            genericParams[i] = paramType;
                        }
                    }
                    boundType = type.MakeGenericType(paramTypes);
                }
                ClassDataContract boundClassContract = new ClassDataContract(boundType);
                boundContracts ??= new Dictionary<DataContract, DataContract>();
                boundContracts.Add(this, boundClassContract);
                boundClassContract.XmlName = CreateQualifiedName(DataContract.ExpandGenericParameters(XmlConvert.DecodeName(xmlName.Name), new GenericNameProvider(DataContract.GetClrTypeFullName(UnderlyingType), genericParams)), xmlName.Namespace);
                if (BaseClassContract != null)
                    boundClassContract.BaseClassContract = (ClassDataContract)BaseClassContract.BindGenericParameters(paramContracts, boundContracts);
                boundClassContract.IsISerializable = IsISerializable;
                boundClassContract.IsValueType = IsValueType;
                boundClassContract.IsReference = IsReference;
                if (Members != null)
                {
                    boundClassContract.Members = new List<DataMember>(Members.Count);
                    foreach (DataMember member in Members)
                        boundClassContract.Members.Add(member.BindGenericParameters(paramContracts, boundContracts));
                }
                return boundClassContract;
            }
        }

        [UnconditionalSuppressMessage("AOT Analysis", "IL3050:RequiresDynamicCode",
            Justification = "All ctor's required to create an instance of this type are marked with RequiresDynamicCode.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "All ctor's required to create an instance of this type are marked with RequiresUnreferencedCode.")]
        internal override bool Equals(object? other, HashSet<DataContractPairKey>? checkedContracts)
        {
            if (IsEqualOrChecked(other, checkedContracts))
                return true;

            if (base.Equals(other, checkedContracts))
            {
                if (other is ClassDataContract dataContract)
                {
                    if (IsISerializable)
                    {
                        if (!dataContract.IsISerializable)
                            return false;
                    }
                    else
                    {
                        if (dataContract.IsISerializable)
                            return false;

                        if (Members == null)
                        {
                            if (dataContract.Members != null)
                            {
                                // check that all the datamembers in dataContract.Members are optional
                                if (!IsEveryDataMemberOptional(dataContract.Members))
                                    return false;
                            }
                        }
                        else if (dataContract.Members == null)
                        {
                            // check that all the datamembers in Members are optional
                            if (!IsEveryDataMemberOptional(Members))
                                return false;
                        }
                        else
                        {
                            Dictionary<string, DataMember> membersDictionary = new Dictionary<string, DataMember>(Members.Count);
                            List<DataMember> dataContractMembersList = new List<DataMember>();
                            for (int i = 0; i < Members.Count; i++)
                            {
                                membersDictionary.Add(Members[i].Name, Members[i]);
                            }

                            for (int i = 0; i < dataContract.Members.Count; i++)
                            {
                                // check that all datamembers common to both datacontracts match
                                if (membersDictionary.TryGetValue(dataContract.Members[i].Name, out DataMember? dataMember))
                                {
                                    if (dataMember.Equals(dataContract.Members[i], checkedContracts))
                                    {
                                        membersDictionary.Remove(dataMember.Name);
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                                // otherwise save the non-matching datamembers for later verification
                                else
                                {
                                    dataContractMembersList.Add(dataContract.Members[i]);
                                }
                            }

                            // check that datamembers left over from either datacontract are optional
                            if (!IsEveryDataMemberOptional(membersDictionary.Values))
                                return false;
                            if (!IsEveryDataMemberOptional(dataContractMembersList))
                                return false;
                        }
                    }

                    if (BaseClassContract == null)
                        return (dataContract.BaseClassContract == null);
                    else if (dataContract.BaseClassContract == null)
                        return false;
                    else
                        return BaseClassContract.Equals(dataContract.BaseClassContract, checkedContracts);
                }
            }
            return false;
        }

        private static bool IsEveryDataMemberOptional(IEnumerable<DataMember> dataMembers)
        {
            return !dataMembers.Any(dm => dm.IsRequired);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        internal sealed class DataMemberComparer : IComparer<DataMember>
        {
            public int Compare(DataMember? x, DataMember? y)
            {
                if (x == null && y == null)
                    return 0;
                if (x == null || y == null)
                    return -1;

                int orderCompare = (int)(x.Order - y.Order);
                if (orderCompare != 0)
                    return orderCompare;

                return string.CompareOrdinal(x.Name, y.Name);
            }

            internal static DataMemberComparer Singleton = new DataMemberComparer();
        }

        /// <summary>
        ///  Get object type for Xml/JsonFormatReaderGenerator
        /// </summary>
        internal Type ObjectType
        {
            get
            {
                Type type = UnderlyingType;
                if (type.IsValueType && !IsNonAttributedType)
                {
                    type = Globals.TypeOfValueType;
                }
                return type;
            }
        }
    }
}
