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

        private bool _isScriptObject;

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
            _isScriptObject = _helper.IsScriptObject;
        }

        internal ClassDataContract? BaseContract
        {
            get { return _helper.BaseContract; }
        }

        internal List<DataMember>? Members
        {
            get { return _helper.Members; }
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
            set
            {
                _childElementNamespaces = value;
            }
        }

        internal MethodInfo? OnSerializing
        {
            get
            { return _helper.OnSerializing; }
        }

        internal MethodInfo? OnSerialized
        {
            get
            { return _helper.OnSerialized; }
        }

        internal MethodInfo? OnDeserializing
        {
            get
            { return _helper.OnDeserializing; }
        }

        internal MethodInfo? OnDeserialized
        {
            get
            { return _helper.OnDeserialized; }
        }

        internal MethodInfo? ExtensionDataSetMethod
        {
            get { return _helper.ExtensionDataSetMethod; }
        }

        public override DataContractDictionary? KnownDataContracts
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            { return _helper.KnownDataContracts; }
        }

        public override bool IsISerializable
        {
            get { return _helper.IsISerializable; }
            set { _helper.IsISerializable = value; }
        }

        internal bool IsNonAttributedType
        {
            get
            { return _helper.IsNonAttributedType; }
        }

        public bool HasExtensionData
        {
            get
            { return _helper.HasExtensionData; }
            set { _helper.HasExtensionData = value; }
        }

        [MemberNotNullWhen(true, nameof(KeyValuePairGenericArguments))]
        [MemberNotNullWhen(true, nameof(KeyValuePairAdapterConstructorInfo))]
        [MemberNotNullWhen(true, nameof(GetKeyValuePairMethodInfo))]
        internal bool IsKeyValuePairAdapter
        {
            get
#pragma warning disable CS8775 // Member must have a non-null value when exiting in some condition.
            { return _helper.IsKeyValuePairAdapter; }
#pragma warning restore CS8775 // Member must have a non-null value when exiting in some condition.
        }

        internal Type[]? KeyValuePairGenericArguments
        {
            get
            { return _helper.KeyValuePairGenericArguments; }
        }

        internal ConstructorInfo? KeyValuePairAdapterConstructorInfo
        {
            get
            { return _helper.KeyValuePairAdapterConstructorInfo; }
        }

        internal MethodInfo? GetKeyValuePairMethodInfo
        {
            get
            { return _helper.GetKeyValuePairMethodInfo; }
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
            if (ci == null)
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
            set
            {
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private XmlFormatClassReaderDelegate CreateXmlFormatReaderDelegate()
        {
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
                            XmlFormatClassReaderDelegate tempDelegate = CreateXmlFormatReaderDelegate();
                            Interlocked.MemoryBarrier();
                            _helper.XmlFormatReaderDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.XmlFormatReaderDelegate;
            }
            set
            {
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static ClassDataContract CreateClassDataContractForKeyValue(Type type, XmlDictionaryString ns, string[] memberNames)
        {
            ClassDataContract? cdc = (ClassDataContract?)DataContract.GetDataContractFromGeneratedAssembly(type);
            if (cdc == null)
            {
                return new ClassDataContract(type, ns, memberNames);
            }
            else
            {
                ClassDataContract cloned = cdc.Clone();
                cloned.UpdateNamespaceAndMembers(type, ns, memberNames);
                return cloned;
            }
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

            Type[] interfaceTypes = type.GetInterfaces();

            if (!IsArraySegment(type))
            {
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
            if (type.IsValueType)
            {
                return type.IsVisible;
            }
            else
            {
                return (type.IsVisible &&
                    type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, Type.EmptyTypes) != null);
            }
        }

        private static readonly Dictionary<string, string[]> s_knownSerializableTypeInfos = new Dictionary<string, string[]> {
            { "System.Collections.Generic.KeyValuePair`2", Array.Empty<string>() },
            { "System.Collections.Generic.Queue`1", new[] { "_syncRoot" } },
            { "System.Collections.Generic.Stack`1", new[] {"_syncRoot" } },
            { "System.Collections.ObjectModel.ReadOnlyCollection`1", new[] {"_syncRoot" } },
            { "System.Collections.ObjectModel.ReadOnlyDictionary`2", new[] {"_syncRoot", "_keys", "_values" } },
            { "System.Tuple`1", Array.Empty<string>() },
            { "System.Tuple`2", Array.Empty<string>() },
            { "System.Tuple`3", Array.Empty<string>() },
            { "System.Tuple`4", Array.Empty<string>() },
            { "System.Tuple`5", Array.Empty<string>() },
            { "System.Tuple`6", Array.Empty<string>() },
            { "System.Tuple`7", Array.Empty<string>() },
            { "System.Tuple`8", Array.Empty<string>() },
            { "System.Collections.Queue", new[] {"_syncRoot" } },
            { "System.Collections.Stack", new[] {"_syncRoot" } },
            { "System.Globalization.CultureInfo", Array.Empty<string>() },
            { "System.Version", Array.Empty<string>() },
        };

        private static string GetGeneralTypeName(Type type)
        {
            return type.IsGenericType && !type.IsGenericParameter
                ? type.GetGenericTypeDefinition().FullName!
                : type.FullName!;
        }

        internal static bool IsKnownSerializableType(Type type)
        {
            // Applies to known types that DCS understands how to serialize/deserialize
            //
            string typeFullName = GetGeneralTypeName(type);

            return s_knownSerializableTypeInfos.ContainsKey(typeFullName);
        }

        internal static bool IsNonSerializedMember(Type type, string memberName)
        {
            string typeFullName = GetGeneralTypeName(type);

            string[]? members;
            return s_knownSerializableTypeInfos.TryGetValue(typeFullName, out members)
                && members.Contains(memberName);
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

            if (_isScriptObject)
            {
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.UnexpectedContractType, DataContract.GetClrTypeFullName(this.GetType()), DataContract.GetClrTypeFullName(UnderlyingType))));
            }
            XmlFormatWriterDelegate(xmlWriter, obj, context, this);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext? context)
        {
            if (_isScriptObject)
            {
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.UnexpectedContractType, DataContract.GetClrTypeFullName(this.GetType()), DataContract.GetClrTypeFullName(UnderlyingType))));
            }
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

            if (ConstructorRequiresMemberAccess(GetNonAttributedTypeConstructor()))
            {
                if (Globals.TypeOfScriptObject_IsAssignableFrom(UnderlyingType))
                {
                    return true;
                }
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
            private static readonly MethodInfo s_getKeyValuePairMethod = typeof(KeyValuePairAdapter<,>).GetMethod("GetKeyValuePair", Globals.ScanAllMembers)!;
            private static readonly ConstructorInfo s_ctorGenericMethod = typeof(KeyValuePairAdapter<,>).GetConstructor(Globals.ScanAllMembers, new Type[] { typeof(KeyValuePair<,>).MakeGenericType(typeof(KeyValuePairAdapter<,>).GetGenericArguments()) })!;

            private ClassDataContract? _baseContract;
            private List<DataMember>? _members;
            private MethodInfo? _onSerializing, _onSerialized;
            private MethodInfo? _onDeserializing, _onDeserialized;
            private MethodInfo? _extensionDataSetMethod;
            private DataContractDictionary? _knownDataContracts;
            private bool _isISerializable;
            private bool _isKnownTypeAttributeChecked;
            private bool _isMethodChecked;
            /// <SecurityNote>
            /// in serialization/deserialization we base the decision whether to Demand SerializationFormatter permission on this value and hasDataContract
            /// </SecurityNote>
            private bool _isNonAttributedType;

            /// <SecurityNote>
            /// in serialization/deserialization we base the decision whether to Demand SerializationFormatter permission on this value and isNonAttributedType
            /// </SecurityNote>
            private bool _hasDataContract;
            private bool _hasExtensionData;
            private readonly bool _isScriptObject;

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
                SetKeyValuePairAdapterFlags(type);
                this.IsValueType = type.IsValueType;
                if (baseType != null && baseType != Globals.TypeOfObject && baseType != Globals.TypeOfValueType && baseType != Globals.TypeOfUri)
                {
                    // https://github.com/dotnet/corefx/pull/19629:
                    // A DataContract created at runtime does not work with the base DataContract pre-generated by SG.
                    // It's because the runtime DataContract requires full information of a base DataContract while a
                    // pre-generated DataContract is incomplete.
                    //
                    // At this point in code, we're in the midlle of creating a new DataContract at runtime, so we need to make
                    // sure that we create our own base type DataContract when the base type could potentially have SG generated
                    // DataContract.
                    //
                    // We wanted to enable the fix for the issue described above only when SG generated DataContracts are available.
                    // Currently we don't have a good way of detecting usage of SG (either globally or per data contract).

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
                _isScriptObject = this.IsNonAttributedType &&
                    Globals.TypeOfScriptObject_IsAssignableFrom(this.UnderlyingType);
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

                bool isPodSerializable = !_isNonAttributedType || IsKnownSerializableType(type);

                if (!isPodSerializable)
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
                                    if (!SetIfGetOnlyCollection(memberContract))
                                    {
                                        ThrowInvalidDataContractException(SR.Format(SR.NoSetMethodForProperty, property.DeclaringType, property.Name));
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
                    else if (!isPodSerializable)
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
                                if (!SetIfGetOnlyCollection(memberContract))
                                    continue;
                            }
                            else
                            {
                                if (!setMethod.IsPublic || IsMethodOverriding(setMethod))
                                    continue;
                            }
                        }

                        memberContract.Name = DataContract.EncodeLocalName(member.Name);
                        memberContract.IsNullable = DataContract.IsTypeNullable(memberContract.MemberType);
                        CheckAndAddMember(tempMembers, memberContract, memberNamesTable);
                    }
                    else
                    {
                        FieldInfo? field = member as FieldInfo;

                        bool canSerializeMember;

                        // Previously System.SerializableAttribute was not available in NetCore, so we had
                        // a list of known [Serializable] types for type in the framework. Although now SerializableAttribute
                        // is available in NetCore, some framework types still do not have [Serializable]
                        // yet, e.g. ReadOnlyDictionary<TKey, TValue>. So, we still need to maintain the known serializable
                        // type list.
                        if (IsKnownSerializableType(type))
                        {
                            canSerializeMember = CanSerializeMember(field);
                        }
                        else
                        {
                            canSerializeMember = field != null && !field.IsNotSerialized;
                        }

                        if (canSerializeMember)
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

            private static bool CanSerializeMember(FieldInfo? field)
            {
                return field != null && !ClassDataContract.IsNonSerializedMember(field.DeclaringType!, field.Name);
            }

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private static bool SetIfGetOnlyCollection(DataMember memberContract)
            {
                //OK to call IsCollection here since the use of surrogated collection types is not supported in get-only scenarios
                if (CollectionDataContract.IsCollection(memberContract.MemberType, false /*isConstructorRequired*/) && !memberContract.MemberType.IsValueType)
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

                set
                { _knownDataContracts = value; }
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
                set { _hasExtensionData = value; }
            }

            internal bool IsNonAttributedType
            {
                get { return _isNonAttributedType; }
            }

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private void SetKeyValuePairAdapterFlags(
                [DynamicallyAccessedMembers(DataContractPreserveMemberTypes)]
                Type type)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfKeyValuePairAdapter)
                {
                    _isKeyValuePairAdapter = true;
                    _keyValuePairGenericArguments = type.GetGenericArguments();
                    _keyValuePairCtorInfo = (ConstructorInfo)type.GetMemberWithSameMetadataDefinitionAs(s_ctorGenericMethod);
                    _getKeyValuePairMethodInfo = (MethodInfo)type.GetMemberWithSameMetadataDefinitionAs(s_getKeyValuePairMethod);
                }
            }

            private bool _isKeyValuePairAdapter;
            private Type[]? _keyValuePairGenericArguments;
            private ConstructorInfo? _keyValuePairCtorInfo;
            private MethodInfo? _getKeyValuePairMethodInfo;

            internal bool IsKeyValuePairAdapter
            {
                get { return _isKeyValuePairAdapter; }
            }

            internal bool IsScriptObject
            {
                get { return _isScriptObject; }
            }

            internal Type[]? KeyValuePairGenericArguments
            {
                get { return _keyValuePairGenericArguments; }
            }

            internal ConstructorInfo? KeyValuePairAdapterConstructorInfo
            {
                get { return _keyValuePairCtorInfo; }
            }

            internal MethodInfo? GetKeyValuePairMethodInfo
            {
                get { return _getKeyValuePairMethodInfo; }
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

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal ClassDataContractCriticalHelper Clone()
            {
                ClassDataContractCriticalHelper clonedHelper = new ClassDataContractCriticalHelper(this.UnderlyingType);

                clonedHelper._baseContract = this._baseContract;
                clonedHelper._childElementNamespaces = this._childElementNamespaces;
                clonedHelper.ContractNamespaces = this.ContractNamespaces;
                clonedHelper._hasDataContract = this._hasDataContract;
                clonedHelper._isMethodChecked = this._isMethodChecked;
                clonedHelper._isNonAttributedType = this._isNonAttributedType;
                clonedHelper.IsReference = this.IsReference;
                clonedHelper.IsValueType = this.IsValueType;
                clonedHelper.MemberNames = this.MemberNames;
                clonedHelper.MemberNamespaces = this.MemberNamespaces;
                clonedHelper._members = this._members;
                clonedHelper.Name = this.Name;
                clonedHelper.Namespace = this.Namespace;
                clonedHelper._onDeserialized = this._onDeserialized;
                clonedHelper._onDeserializing = this._onDeserializing;
                clonedHelper._onSerialized = this._onSerialized;
                clonedHelper._onSerializing = this._onSerializing;
                clonedHelper.StableName = this.StableName;
                clonedHelper.TopLevelElementName = this.TopLevelElementName;
                clonedHelper.TopLevelElementNamespace = this.TopLevelElementNamespace;
                clonedHelper._xmlFormatReaderDelegate = this._xmlFormatReaderDelegate;
                clonedHelper._xmlFormatWriterDelegate = this._xmlFormatWriterDelegate;

                return clonedHelper;
            }
        }

        internal sealed class DataMemberComparer : IComparer<DataMember>
        {
            public int Compare(DataMember? x, DataMember? y)
            {
                int orderCompare = x!.Order - y!.Order;
                if (orderCompare != 0)
                    return orderCompare;

                return string.CompareOrdinal(x.Name, y.Name);
            }

            internal static DataMemberComparer Singleton = new DataMemberComparer();
        }

        /// <summary>
        ///  Get object type for Xml/JsonFormmatReaderGenerator
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal ClassDataContract Clone()
        {
            ClassDataContract clonedDc = new ClassDataContract(this.UnderlyingType);
            clonedDc._helper = _helper.Clone();
            clonedDc.ContractNamespaces = this.ContractNamespaces;
            clonedDc.ChildElementNamespaces = this.ChildElementNamespaces;
            clonedDc.MemberNames = this.MemberNames;
            clonedDc.MemberNamespaces = this.MemberNamespaces;
            clonedDc.XmlFormatWriterDelegate = this.XmlFormatWriterDelegate;
            clonedDc.XmlFormatReaderDelegate = this.XmlFormatReaderDelegate;
            return clonedDc;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void UpdateNamespaceAndMembers(Type type, XmlDictionaryString ns, string[] memberNames)
        {
            this.StableName = new XmlQualifiedName(GetStableName(type).Name, ns.Value);
            this.Namespace = ns;
            XmlDictionary dictionary = new XmlDictionary(1 + memberNames.Length);
            this.Name = dictionary.Add(StableName.Name);
            this.Namespace = ns;
            this.ContractNamespaces = new XmlDictionaryString[] { ns };
            this.MemberNames = new XmlDictionaryString[memberNames.Length];
            this.MemberNamespaces = new XmlDictionaryString[memberNames.Length];
            for (int i = 0; i < memberNames.Length; i++)
            {
                this.MemberNames[i] = dictionary.Add(memberNames[i]);
                this.MemberNamespaces[i] = ns;
            }
        }
    }
}
