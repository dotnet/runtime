// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using System.Xml;
using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContract>;

namespace System.Runtime.Serialization
{
    internal sealed class ClassDataContract : DataContract
    {
        public XmlDictionaryString[]? ContractNamespaces;

        public XmlDictionaryString[]? MemberNames;

        public XmlDictionaryString[]? MemberNamespaces;

        private XmlDictionaryString?[]? _childElementNamespaces;

        private ClassDataContractCriticalHelper _helper;

        internal const DynamicallyAccessedMemberTypes DataContractPreserveMemberTypes =
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods |
            DynamicallyAccessedMemberTypes.PublicConstructors |
            DynamicallyAccessedMemberTypes.NonPublicConstructors |
            DynamicallyAccessedMemberTypes.PublicFields |
            DynamicallyAccessedMemberTypes.PublicProperties;

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal ClassDataContract(Type type) : base(new ClassDataContractCriticalHelper(type))
        {
            InitClassDataContract();
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private ClassDataContract(Type type, XmlDictionaryString ns, string[] memberNames) : base(new ClassDataContractCriticalHelper(type, ns, memberNames))
        {
            InitClassDataContract();
        }

        [MemberNotNull(nameof(_helper))]
        private void InitClassDataContract()
        {
            _helper = (base.Helper as ClassDataContractCriticalHelper)!;
            this.ContractNamespaces = _helper.ContractNamespaces;
            this.MemberNames = _helper.MemberNames;
            this.MemberNamespaces = _helper.MemberNamespaces;
        }

        internal ClassDataContract? BaseContract
        {
            get { return _helper.BaseContract; }
            set { _helper.BaseContract = value; }
        }

        internal List<DataMember>? Members
        {
            get { return _helper.Members; }
            set { _helper.Members = value; }
        }

        public XmlDictionaryString?[]? ChildElementNamespaces
        {
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

        internal MethodInfo? OnSerializing
        {
            get { return _helper.OnSerializing; }
        }

        internal MethodInfo? OnSerialized
        {
            get { return _helper.OnSerialized; }
        }

        internal MethodInfo? OnDeserializing
        {
            get { return _helper.OnDeserializing; }
        }

        internal MethodInfo? OnDeserialized
        {
            get { return _helper.OnDeserialized; }
        }

        internal MethodInfo? ExtensionDataSetMethod
        {
            get { return _helper.ExtensionDataSetMethod; }
        }

        internal override DataContractDictionary? KnownDataContracts
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get { return _helper.KnownDataContracts; }
            set { _helper.KnownDataContracts = value; }
        }

        internal override bool IsISerializable
        {
            get { return _helper.IsISerializable; }
            set { _helper.IsISerializable = value; }
        }

        internal bool IsNonAttributedType
        {
            get { return _helper.IsNonAttributedType; }
        }

        internal bool HasExtensionData
        {
            get { return _helper.HasExtensionData; }
        }

        internal string? SerialiazationExceptionMessage { get { return _helper.SerializationExceptionMessage; } }
        internal string? DeserializationExceptionMessage { get { return _helper.DeserializationExceptionMessage; } }

        internal bool IsReadOnlyContract
        {
            get { return DeserializationExceptionMessage != null; }
        }

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
        private Func<object> MakeNewInstance => _makeNewInstance ??= FastInvokerBuilder.GetMakeNewInstanceFunc(UnderlyingType);

        internal bool CreateNewInstanceViaDefaultConstructor([NotNullWhen(true)] out object? obj)
        {
            ConstructorInfo? ci = GetNonAttributedTypeConstructor();
            if (ci == null || UnderlyingType == Globals.TypeOfSchemaTypePlaceholder)
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private XmlFormatClassWriterDelegate CreateXmlFormatWriterDelegate()
        {
            Debug.Assert(UnderlyingType != Globals.TypeOfSchemaTypePlaceholder);
            return new XmlFormatWriterGenerator().GenerateClassWriter(this);
        }

        internal XmlFormatClassWriterDelegate XmlFormatWriterDelegate
        {
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private XmlFormatClassReaderDelegate CreateXmlFormatReaderDelegate()
        {
            Debug.Assert(UnderlyingType != Globals.TypeOfSchemaTypePlaceholder);
            return new XmlFormatReaderGenerator().GenerateClassReader(this);
        }

        internal XmlFormatClassReaderDelegate XmlFormatReaderDelegate
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (_helper.XmlFormatReaderDelegate == null)
                {
                    lock (this)
                    {
                        if (_helper.XmlFormatReaderDelegate == null)
                        {
                            if (this.IsReadOnlyContract)
                            {
                                ThrowInvalidDataContractException(DeserializationExceptionMessage, null /*type*/);
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static ClassDataContract CreateClassDataContractForKeyValue(Type type, XmlDictionaryString ns, string[] memberNames)
        {
            return new ClassDataContract(type, ns, memberNames);
        }

        internal static void CheckAndAddMember(List<DataMember> members, DataMember memberContract, Dictionary<string, DataMember> memberNamesTable)
        {
            DataMember? existingMemberContract;
            if (memberNamesTable.TryGetValue(memberContract.Name, out existingMemberContract))
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static XmlDictionaryString? GetChildNamespaceToDeclare(DataContract dataContract, Type childType, XmlDictionary dictionary)
        {
            childType = DataContract.UnwrapNullableType(childType);
            if (!childType.IsEnum && !Globals.TypeOfIXmlSerializable.IsAssignableFrom(childType)
                && DataContract.GetBuiltInDataContract(childType) == null && childType != Globals.TypeOfDBNull)
            {
                string ns = DataContract.GetStableName(childType).Namespace;
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
                Type[] interfaceTypes = type.GetInterfaces();
                foreach (Type interfaceType in interfaceTypes)
                {
                    if (CollectionDataContract.IsCollectionInterface(interfaceType))
                        return false;
                }
            }

            if (type.IsSerializable)
                return false;

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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private XmlDictionaryString?[]? CreateChildElementNamespaces()
        {
            if (Members == null)
                return null;

            XmlDictionaryString?[]? baseChildElementNamespaces = null;
            if (this.BaseContract != null)
                baseChildElementNamespaces = this.BaseContract.ChildElementNamespaces;
            int baseChildElementNamespaceCount = (baseChildElementNamespaces != null) ? baseChildElementNamespaces.Length : 0;
            XmlDictionaryString?[] childElementNamespaces = new XmlDictionaryString?[Members.Count + baseChildElementNamespaceCount];
            if (baseChildElementNamespaceCount > 0)
                Array.Copy(baseChildElementNamespaces!, childElementNamespaces, baseChildElementNamespaces!.Length);

            XmlDictionary dictionary = new XmlDictionary();
            for (int i = 0; i < this.Members.Count; i++)
            {
                childElementNamespaces[i + baseChildElementNamespaceCount] = GetChildNamespaceToDeclare(this, this.Members[i].MemberType, dictionary);
            }

            return childElementNamespaces;
        }

        private void EnsureMethodsImported()
        {
            _helper.EnsureMethodsImported();
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext? context)
        {
            Debug.Assert(context != null);
            XmlFormatWriterDelegate(xmlWriter, obj, context, this);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext? context)
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
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new SecurityException(SR.Format(
                                SR.PartialTrustDataContractTypeNotPublic,
                                DataContract.GetClrTypeFullName(UnderlyingType)),
                            securityException));
                }
                return true;
            }
            if (this.BaseContract != null && this.BaseContract.RequiresMemberAccessForRead(securityException))
                return true;

            if (ConstructorRequiresMemberAccess(GetISerializableConstructor()))
            {
                if (securityException != null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new SecurityException(SR.Format(
                                SR.PartialTrustIXmlSerialzableNoPublicConstructor,
                                DataContract.GetClrTypeFullName(UnderlyingType)),
                            securityException));
                }
                return true;
            }

            if (ConstructorRequiresMemberAccess(GetNonAttributedTypeConstructor()))
            {
                if (securityException != null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new SecurityException(SR.Format(
                                SR.PartialTrustNonAttributedSerializableTypeNoPublicConstructor,
                                DataContract.GetClrTypeFullName(UnderlyingType)),
                            securityException));
                }
                return true;
            }

            if (MethodRequiresMemberAccess(this.OnDeserializing))
            {
                if (securityException != null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new SecurityException(SR.Format(
                                SR.PartialTrustDataContractOnDeserializingNotPublic,
                                DataContract.GetClrTypeFullName(UnderlyingType),
                                this.OnDeserializing!.Name),
                            securityException));
                }
                return true;
            }

            if (MethodRequiresMemberAccess(this.OnDeserialized))
            {
                if (securityException != null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new SecurityException(SR.Format(
                                SR.PartialTrustDataContractOnDeserializedNotPublic,
                                DataContract.GetClrTypeFullName(UnderlyingType),
                                this.OnDeserialized!.Name),
                            securityException));
                }
                return true;
            }

            if (this.Members != null)
            {
                for (int i = 0; i < this.Members.Count; i++)
                {
                    if (this.Members[i].RequiresMemberAccessForSet())
                    {
                        if (securityException != null)
                        {
                            if (this.Members[i].MemberInfo is FieldInfo)
                            {
                                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                                    new SecurityException(SR.Format(
                                            SR.PartialTrustDataContractFieldSetNotPublic,
                                            DataContract.GetClrTypeFullName(UnderlyingType),
                                            this.Members[i].MemberInfo.Name),
                                        securityException));
                            }
                            else
                            {
                                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                                    new SecurityException(SR.Format(
                                            SR.PartialTrustDataContractPropertySetNotPublic,
                                            DataContract.GetClrTypeFullName(UnderlyingType),
                                            this.Members[i].MemberInfo.Name),
                                        securityException));
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
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new SecurityException(SR.Format(
                                SR.PartialTrustDataContractTypeNotPublic,
                                DataContract.GetClrTypeFullName(UnderlyingType)),
                            securityException));
                }
                return true;
            }

            if (this.BaseContract != null && this.BaseContract.RequiresMemberAccessForWrite(securityException))
                return true;

            if (MethodRequiresMemberAccess(this.OnSerializing))
            {
                if (securityException != null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new SecurityException(SR.Format(
                                SR.PartialTrustDataContractOnSerializingNotPublic,
                                DataContract.GetClrTypeFullName(this.UnderlyingType),
                                this.OnSerializing!.Name),
                            securityException));
                }
                return true;
            }

            if (MethodRequiresMemberAccess(this.OnSerialized))
            {
                if (securityException != null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new SecurityException(SR.Format(
                                SR.PartialTrustDataContractOnSerializedNotPublic,
                                DataContract.GetClrTypeFullName(UnderlyingType),
                                this.OnSerialized!.Name),
                            securityException));
                }
                return true;
            }

            if (this.Members != null)
            {
                for (int i = 0; i < this.Members.Count; i++)
                {
                    if (this.Members[i].RequiresMemberAccessForGet())
                    {
                        if (securityException != null)
                        {
                            if (this.Members[i].MemberInfo is FieldInfo)
                            {
                                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                                    new SecurityException(SR.Format(
                                            SR.PartialTrustDataContractFieldGetNotPublic,
                                            DataContract.GetClrTypeFullName(UnderlyingType),
                                            this.Members[i].MemberInfo.Name),
                                        securityException));
                            }
                            else
                            {
                                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                                    new SecurityException(SR.Format(
                                            SR.PartialTrustDataContractPropertyGetNotPublic,
                                            DataContract.GetClrTypeFullName(UnderlyingType),
                                            this.Members[i].MemberInfo.Name),
                                        securityException));
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
            private bool _isISerializable;
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
            private bool _hasExtensionData;

            private XmlDictionaryString?[]? _childElementNamespaces;
            private XmlFormatClassReaderDelegate? _xmlFormatReaderDelegate;
            private XmlFormatClassWriterDelegate? _xmlFormatWriterDelegate;

            public XmlDictionaryString[]? ContractNamespaces;
            public XmlDictionaryString[]? MemberNames;
            public XmlDictionaryString[]? MemberNamespaces;

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal ClassDataContractCriticalHelper([DynamicallyAccessedMembers(DataContractPreserveMemberTypes)]
                Type type) : base(type)
            {
                XmlQualifiedName stableName = GetStableNameAndSetHasDataContract(type);
                if (type == Globals.TypeOfDBNull)
                {
                    this.StableName = stableName;
                    _members = new List<DataMember>();
                    XmlDictionary dictionary = new XmlDictionary(2);
                    this.Name = dictionary.Add(StableName.Name);
                    this.Namespace = dictionary.Add(StableName.Namespace);
                    this.ContractNamespaces = this.MemberNames = this.MemberNamespaces = Array.Empty<XmlDictionaryString>();
                    EnsureMethodsImported();
                    return;
                }
                Type? baseType = type.BaseType;
                _isISerializable = (Globals.TypeOfISerializable.IsAssignableFrom(type));
                SetIsNonAttributedType(type);
                if (_isISerializable)
                {
                    if (HasDataContract)
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.ISerializableCannotHaveDataContract, DataContract.GetClrTypeFullName(type))));
                    if (baseType != null && !(baseType.IsSerializable && Globals.TypeOfISerializable.IsAssignableFrom(baseType)))
                        baseType = null;
                }
                this.IsValueType = type.IsValueType;
                if (baseType != null && baseType != Globals.TypeOfObject && baseType != Globals.TypeOfValueType && baseType != Globals.TypeOfUri)
                {
                    DataContract baseContract = DataContract.GetDataContract(baseType);
                    if (baseContract is CollectionDataContract)
                    {
                        this.BaseContract = ((CollectionDataContract)baseContract).SharedTypeContract as ClassDataContract;
                    }
                    else
                    {
                        this.BaseContract = baseContract as ClassDataContract;
                    }

                    if (this.BaseContract != null && this.BaseContract.IsNonAttributedType && !_isNonAttributedType)
                    {
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError
                            (new InvalidDataContractException(SR.Format(SR.AttributedTypesCannotInheritFromNonAttributedSerializableTypes,
                            DataContract.GetClrTypeFullName(type), DataContract.GetClrTypeFullName(baseType))));
                    }
                }
                else
                {
                    this.BaseContract = null;
                }

                _hasExtensionData = (Globals.TypeOfIExtensibleDataObject.IsAssignableFrom(type));
                if (_hasExtensionData && !HasDataContract && !IsNonAttributedType)
                {
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.OnlyDataContractTypesCanHaveExtensionData, DataContract.GetClrTypeFullName(type))));
                }

                if (_isISerializable)
                {
                    SetDataContractName(stableName);
                }
                else
                {
                    this.StableName = stableName;
                    ImportDataMembers();
                    XmlDictionary dictionary = new XmlDictionary(2 + Members.Count);
                    Name = dictionary.Add(StableName.Name);
                    Namespace = dictionary.Add(StableName.Namespace);

                    int baseMemberCount = 0;
                    int baseContractCount = 0;
                    if (BaseContract == null)
                    {
                        MemberNames = new XmlDictionaryString[Members.Count];
                        MemberNamespaces = new XmlDictionaryString[Members.Count];
                        ContractNamespaces = new XmlDictionaryString[1];
                    }
                    else
                    {
                        if (BaseContract.IsReadOnlyContract)
                        {
                            _serializationExceptionMessage = BaseContract.SerialiazationExceptionMessage;
                        }
                        baseMemberCount = BaseContract.MemberNames!.Length;
                        MemberNames = new XmlDictionaryString[Members.Count + baseMemberCount];
                        Array.Copy(BaseContract.MemberNames, MemberNames, baseMemberCount);
                        MemberNamespaces = new XmlDictionaryString[Members.Count + baseMemberCount];
                        Array.Copy(BaseContract.MemberNamespaces!, MemberNamespaces, baseMemberCount);
                        baseContractCount = BaseContract.ContractNamespaces!.Length;
                        ContractNamespaces = new XmlDictionaryString[1 + baseContractCount];
                        Array.Copy(BaseContract.ContractNamespaces, ContractNamespaces, baseContractCount);
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

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal ClassDataContractCriticalHelper(
                [DynamicallyAccessedMembers(DataContractPreserveMemberTypes)]
                Type type, XmlDictionaryString ns, string[] memberNames) : base(type)
            {
                this.StableName = new XmlQualifiedName(GetStableNameAndSetHasDataContract(type).Name, ns.Value);
                ImportDataMembers();
                XmlDictionary dictionary = new XmlDictionary(1 + Members.Count);
                Name = dictionary.Add(StableName.Name);
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
                DataContractAttribute? dataContractAttribute;
                bool isReference = false;
                bool hasDataContractAttribute = TryGetDCAttribute(type, out dataContractAttribute);

                if (BaseContract != null)
                {
                    if (hasDataContractAttribute && dataContractAttribute!.IsReferenceSetExplicitly)
                    {
                        bool baseIsReference = this.BaseContract.IsReference;
                        if ((baseIsReference && !dataContractAttribute.IsReference) ||
                            (!baseIsReference && dataContractAttribute.IsReference))
                        {
                            DataContract.ThrowInvalidDataContractException(
                                    SR.Format(SR.InconsistentIsReference,
                                        DataContract.GetClrTypeFullName(type),
                                        dataContractAttribute.IsReference,
                                        DataContract.GetClrTypeFullName(this.BaseContract.UnderlyingType),
                                        this.BaseContract.IsReference),
                                    type);
                        }
                        else
                        {
                            isReference = dataContractAttribute.IsReference;
                        }
                    }
                    else
                    {
                        isReference = this.BaseContract.IsReference;
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

                this.IsReference = isReference;
            }

            [MemberNotNull(nameof(_members))]
            [MemberNotNull(nameof(Members))]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private void ImportDataMembers()
            {
                Type type = this.UnderlyingType;
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
                                if (memberAttribute.Name == null || memberAttribute.Name.Length == 0)
                                    ThrowInvalidDataContractException(SR.Format(SR.InvalidDataMemberName, member.Name, DataContract.GetClrTypeFullName(type)));
                                memberContract.Name = memberAttribute.Name;
                            }
                            else
                                memberContract.Name = member.Name;

                            memberContract.Name = DataContract.EncodeLocalName(memberContract.Name);
                            memberContract.IsNullable = DataContract.IsTypeNullable(memberContract.MemberType);
                            memberContract.IsRequired = memberAttribute.IsRequired;
                            if (memberAttribute.IsRequired && this.IsReference)
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

                        if (field != null && !field.IsNotSerialized)
                        {
                            DataMember memberContract = new DataMember(member);

                            memberContract.Name = DataContract.EncodeLocalName(member.Name);
                            object[] optionalFields = field!.GetCustomAttributes(Globals.TypeOfOptionalFieldAttribute, false);
                            if (optionalFields == null || optionalFields.Length == 0)
                            {
                                if (this.IsReference)
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
                if (BaseContract == null)
                    return;

                int baseTypeIndex = 0;
                List<Member> membersInHierarchy = new List<Member>();
                foreach (DataMember member in members)
                {
                    membersInHierarchy.Add(new Member(member, this.StableName!.Namespace, baseTypeIndex));
                }
                ClassDataContract? currContract = BaseContract;
                while (currContract != null)
                {
                    baseTypeIndex++;

                    foreach (DataMember member in currContract.Members!)
                    {
                        membersInHierarchy.Add(new Member(member, currContract.StableName!.Namespace, baseTypeIndex));
                    }
                    currContract = currContract.BaseContract;
                }

                IComparer<Member> comparer = DataMemberConflictComparer.Singleton;
                membersInHierarchy.Sort(comparer);

                for (int i = 0; i < membersInHierarchy.Count - 1; i++)
                {
                    int startIndex = i;
                    int endIndex = i;
                    bool hasConflictingType = false;
                    while (endIndex < membersInHierarchy.Count - 1
                        && string.CompareOrdinal(membersInHierarchy[endIndex].member.Name, membersInHierarchy[endIndex + 1].member.Name) == 0
                        && string.CompareOrdinal(membersInHierarchy[endIndex].ns, membersInHierarchy[endIndex + 1].ns) == 0)
                    {
                        membersInHierarchy[endIndex].member.ConflictingMember = membersInHierarchy[endIndex + 1].member;
                        if (!hasConflictingType)
                        {
                            if (membersInHierarchy[endIndex + 1].member.HasConflictingNameAndType)
                            {
                                hasConflictingType = true;
                            }
                            else
                            {
                                hasConflictingType = (membersInHierarchy[endIndex].member.MemberType != membersInHierarchy[endIndex + 1].member.MemberType);
                            }
                        }
                        endIndex++;
                    }

                    if (hasConflictingType)
                    {
                        for (int j = startIndex; j <= endIndex; j++)
                        {
                            membersInHierarchy[j].member.HasConflictingNameAndType = true;
                        }
                    }

                    i = endIndex + 1;
                }
            }

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private XmlQualifiedName GetStableNameAndSetHasDataContract(Type type)
            {
                return DataContract.GetStableName(type, out _hasDataContract);
            }

            /// <SecurityNote>
            /// RequiresReview - marked SRR because callers may need to depend on isNonAttributedType for a security decision
            ///            isNonAttributedType must be calculated correctly
            ///            SetIsNonAttributedType should not be called before GetStableNameAndSetHasDataContract since it
            ///            is dependent on the correct calculation of hasDataContract
            /// Safe - does not let caller influence isNonAttributedType calculation; no harm in leaking value
            /// </SecurityNote>
            private void SetIsNonAttributedType(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)]
                Type type)
            {
                _isNonAttributedType = !type.IsSerializable && !_hasDataContract && IsNonAttributedTypeValidForSerialization(type);
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
                            Type type = this.UnderlyingType;
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

            internal ClassDataContract? BaseContract
            {
                get { return _baseContract; }
                set
                {
                    _baseContract = value;
                    if (_baseContract != null && IsValueType)
                        ThrowInvalidDataContractException(SR.Format(SR.ValueTypeCannotHaveBaseType, StableName!.Name, StableName.Namespace, _baseContract.StableName!.Name, _baseContract.StableName.Namespace));
                }
            }

            internal List<DataMember>? Members
            {
                get { return _members; }
                set { _members = value; }
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

                [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
                get
                {
                    // NOTE TODO smolloy - Oddly, this shortcut was not here in NetFx.  I'm not sure this was added strictly as a perf-optimization though.
                    // See https://github.com/dotnet/runtime/commit/5d2dba7ffcc4ad6a11a570f5bd8d1964000099ea
                    // This shouldn't change between invocations though, right? So seems like a good thing to leave in.
                    if (_knownDataContracts != null)
                    {
                        return _knownDataContracts;
                    }

                    if (!_isKnownTypeAttributeChecked && UnderlyingType != null)
                    {
                        lock (this)
                        {
                            if (!_isKnownTypeAttributeChecked)
                            {
                                _knownDataContracts = DataContract.ImportKnownTypeAttributes(this.UnderlyingType);
                                Interlocked.MemoryBarrier();
                                _isKnownTypeAttributeChecked = true;
                            }
                        }
                    }
                    return _knownDataContracts;
                }

                set { _knownDataContracts = value; }
            }

            internal string? SerializationExceptionMessage
            {
                get { return _serializationExceptionMessage; }
            }

            internal string? DeserializationExceptionMessage
            {
                get { return (_serializationExceptionMessage == null) ? null : SR.Format(SR.ReadOnlyClassDeserialization, _serializationExceptionMessage); }
            }

            internal override bool IsISerializable
            {
                get { return _isISerializable; }
                set { _isISerializable = value; }
            }

            internal bool HasDataContract
            {
                get { return _hasDataContract; }
            }

            internal bool HasExtensionData
            {
                get { return _hasExtensionData; }
            }

            internal bool IsNonAttributedType
            {
                get { return _isNonAttributedType; }
            }

            internal ConstructorInfo? GetISerializableConstructor()
            {
                if (!IsISerializable)
                    return null;

                ConstructorInfo? ctor = UnderlyingType.GetConstructor(Globals.ScanAllMembers, SerInfoCtorArgs);
                if (ctor == null)
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.SerializationInfo_ConstructorNotFound, DataContract.GetClrTypeFullName(UnderlyingType))));

                return ctor;
            }

            internal ConstructorInfo? GetNonAttributedTypeConstructor()
            {
                if (!this.IsNonAttributedType)
                    return null;

                Type type = UnderlyingType;

                if (type.IsValueType)
                    return null;

                ConstructorInfo? ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, Type.EmptyTypes);
                if (ctor == null)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.NonAttributedSerializableTypesMustHaveDefaultConstructor, DataContract.GetClrTypeFullName(type))));

                return ctor;
            }

            internal XmlFormatClassWriterDelegate? XmlFormatWriterDelegate
            {
                get { return _xmlFormatWriterDelegate; }
                set { _xmlFormatWriterDelegate = value; }
            }

            internal XmlFormatClassReaderDelegate? XmlFormatReaderDelegate
            {
                get { return _xmlFormatReaderDelegate; }
                set { _xmlFormatReaderDelegate = value; }
            }

            public XmlDictionaryString?[]? ChildElementNamespaces
            {
                get { return _childElementNamespaces; }
                set { _childElementNamespaces = value; }
            }

            private static Type[] SerInfoCtorArgs => s_serInfoCtorArgs ??= new Type[] { typeof(SerializationInfo), typeof(StreamingContext) };

            internal struct Member
            {
                internal Member(DataMember member, string ns, int baseTypeIndex)
                {
                    this.member = member;
                    this.ns = ns;
                    this.baseTypeIndex = baseTypeIndex;
                }
                internal DataMember member;
                internal string ns;
                internal int baseTypeIndex;
            }

            internal sealed class DataMemberConflictComparer : IComparer<Member>
            {
                public int Compare(Member x, Member y)
                {
                    int nsCompare = string.CompareOrdinal(x.ns, y.ns);
                    if (nsCompare != 0)
                        return nsCompare;

                    int nameCompare = string.CompareOrdinal(x.member.Name, y.member.Name);
                    if (nameCompare != 0)
                        return nameCompare;

                    return x.baseTypeIndex - y.baseTypeIndex;
                }

                internal static DataMemberConflictComparer Singleton = new DataMemberConflictComparer();
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override DataContract BindGenericParameters(DataContract[] paramContracts, Dictionary<DataContract, DataContract> boundContracts)
        {
            Type type = UnderlyingType;
            if (!type.IsGenericType || !type.ContainsGenericParameters)
                return this;

            lock (this)
            {
                DataContract? boundContract;
                if (boundContracts.TryGetValue(this, out boundContract))
                    return boundContract;

                XmlQualifiedName stableName;
                object[] genericParams;
                Type boundType;
                if (type.IsGenericTypeDefinition)
                {
                    stableName = this.StableName;
                    genericParams = paramContracts;

                    // NOTE TODO smolloy - this type-binding ('boundType') stuff is new. We did not do this in NetFx. We used to use default constructors. But default
                    // constructors for DataContracts runs afoul of requiring an underlying type. Our web of nullable notations make it hard to get around.
                    // But it also allows us to feel good about using .UnderlyingType from matching parameter contracts.
                    Type[] underlyingParamTypes = new Type[paramContracts.Length];
                    for (int i = 0; i < paramContracts.Length; i++)
                        underlyingParamTypes[i] = paramContracts[i].UnderlyingType;
                    boundType = type.MakeGenericType(underlyingParamTypes);
                }
                else
                {
                    //partial Generic: Construct stable name from its open generic type definition
                    stableName = DataContract.GetStableName(type.GetGenericTypeDefinition());
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
                boundContracts.Add(this, boundClassContract);
                boundClassContract.StableName = CreateQualifiedName(DataContract.ExpandGenericParameters(XmlConvert.DecodeName(stableName.Name), new GenericNameProvider(DataContract.GetClrTypeFullName(this.UnderlyingType), genericParams)), stableName.Namespace);
                if (BaseContract != null)
                    boundClassContract.BaseContract = (ClassDataContract)BaseContract.BindGenericParameters(paramContracts, boundContracts);
                boundClassContract.IsISerializable = this.IsISerializable;
                boundClassContract.IsValueType = this.IsValueType;
                boundClassContract.IsReference = this.IsReference;
                if (Members != null)
                {
                    boundClassContract.Members = new List<DataMember>(Members.Count);
                    foreach (DataMember member in Members)
                        boundClassContract.Members.Add(member.BindGenericParameters(paramContracts, boundContracts));
                }
                return boundClassContract;
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override bool Equals(object? other, HashSet<DataContractPairKey> checkedContracts)
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
                                DataMember? dataMember;
                                if (membersDictionary.TryGetValue(dataContract.Members[i].Name, out dataMember))
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

                    if (BaseContract == null)
                        return (dataContract.BaseContract == null);
                    else if (dataContract.BaseContract == null)
                        return false;
                    else
                        return BaseContract.Equals(dataContract.BaseContract, checkedContracts);
                }
            }
            return false;
        }

        private static bool IsEveryDataMemberOptional(IEnumerable<DataMember> dataMembers)
        {
            foreach (DataMember dataMember in dataMembers)
            {
                if (dataMember.IsRequired)
                    return false;
            }
            return true;
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

                int orderCompare = x.Order - y.Order;
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
