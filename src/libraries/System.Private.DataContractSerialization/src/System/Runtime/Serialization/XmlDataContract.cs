// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContracts.DataContract>;

namespace System.Runtime.Serialization.DataContracts
{
    internal delegate IXmlSerializable CreateXmlSerializableDelegate();
    public sealed class XmlDataContract : DataContract
    {
        internal const string ContractTypeString = nameof(XmlDataContract);
        public override string? ContractType => ContractTypeString;

        private readonly XmlDataContractCriticalHelper _helper;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal XmlDataContract(Type type) : base(new XmlDataContractCriticalHelper(type))
        {
            _helper = (base.Helper as XmlDataContractCriticalHelper)!;
        }

        public override DataContractDictionary? KnownDataContracts
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get => _helper.KnownDataContracts;
            internal set => _helper.KnownDataContracts = value;
        }

        public XmlSchemaType? XsdType
        {
            get => _helper.XsdType;
            internal set => _helper.XsdType = value;
        }

        public bool IsAnonymous
        {
            get => _helper.IsAnonymous;
        }

        public new bool IsValueType
        {
            get => _helper.IsValueType;
            set => _helper.IsValueType = value;
        }

        public new bool HasRoot
        {
            get => _helper.HasRoot;
            internal set => _helper.HasRoot = value;
        }

        public override XmlDictionaryString? TopLevelElementName
        {
            get => _helper.TopLevelElementName;
            internal set => _helper.TopLevelElementName = value;
        }

        public override XmlDictionaryString? TopLevelElementNamespace
        {
            get => _helper.TopLevelElementNamespace;
            internal set => _helper.TopLevelElementNamespace = value;
        }

        public bool IsTopLevelElementNullable
        {
            get => _helper.IsTopLevelElementNullable;
            internal set => _helper.IsTopLevelElementNullable = value;
        }

        public bool IsTypeDefinedOnImport
        {
            get => _helper.IsTypeDefinedOnImport;
            set => _helper.IsTypeDefinedOnImport = value;
        }

        internal CreateXmlSerializableDelegate CreateXmlSerializableDelegate
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                // We create XmlSerializableDelegate via CodeGen when CodeGen is enabled;
                // otherwise, we would create the delegate via reflection.
                if (DataContractSerializer.Option == SerializationOption.CodeGenOnly || DataContractSerializer.Option == SerializationOption.ReflectionAsBackup)
                {
                    if (_helper.CreateXmlSerializableDelegate == null)
                    {
                        lock (this)
                        {
                            if (_helper.CreateXmlSerializableDelegate == null)
                            {
                                CreateXmlSerializableDelegate tempCreateXmlSerializable = GenerateCreateXmlSerializableDelegate();
                                Interlocked.MemoryBarrier();
                                _helper.CreateXmlSerializableDelegate = tempCreateXmlSerializable;
                            }
                        }
                    }
                    return _helper.CreateXmlSerializableDelegate;
                }

                return () => ReflectionCreateXmlSerializable(this.UnderlyingType);
            }
        }

        internal override bool CanContainReferences => false;

        public override bool IsBuiltInDataContract => UnderlyingType == Globals.TypeOfXmlElement || UnderlyingType == Globals.TypeOfXmlNodeArray;

        private sealed class XmlDataContractCriticalHelper : DataContract.DataContractCriticalHelper
        {
            private DataContractDictionary? _knownDataContracts;
            private bool _isKnownTypeAttributeChecked;
            private XmlDictionaryString? _topLevelElementName;
            private XmlDictionaryString? _topLevelElementNamespace;
            private bool _isTopLevelElementNullable;
            private bool _isTypeDefinedOnImport;
            private CreateXmlSerializableDelegate? _createXmlSerializable;
            private XmlSchemaType? _xsdType;

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal XmlDataContractCriticalHelper(
                [DynamicallyAccessedMembers(ClassDataContract.DataContractPreserveMemberTypes)]
                Type type) : base(type)
            {
                if (type.IsDefined(Globals.TypeOfDataContractAttribute, false))
                    throw new InvalidDataContractException(SR.Format(SR.IXmlSerializableCannotHaveDataContract, DataContract.GetClrTypeFullName(type)));
                if (type.IsDefined(Globals.TypeOfCollectionDataContractAttribute, false))
                    throw new InvalidDataContractException(SR.Format(SR.IXmlSerializableCannotHaveCollectionDataContract, DataContract.GetClrTypeFullName(type)));
                bool hasRoot;
                XmlSchemaType? xsdType;
                XmlQualifiedName xmlName;
                SchemaExporter.GetXmlTypeInfo(type, out xmlName, out xsdType, out hasRoot);
                XmlName = xmlName;
                XsdType = xsdType;
                HasRoot = hasRoot;
                XmlDictionary dictionary = new XmlDictionary();
                Name = dictionary.Add(XmlName.Name);
                Namespace = dictionary.Add(XmlName.Namespace);
                object[]? xmlRootAttributes = UnderlyingType?.GetCustomAttributes(Globals.TypeOfXmlRootAttribute, false).ToArray();
                if (xmlRootAttributes == null || xmlRootAttributes.Length == 0)
                {
                    if (hasRoot)
                    {
                        _topLevelElementName = Name;
                        _topLevelElementNamespace = (this.XmlName.Namespace == Globals.SchemaNamespace) ? DictionaryGlobals.EmptyString : Namespace;
                        _isTopLevelElementNullable = true;
                    }
                }
                else
                {
                    if (hasRoot)
                    {
                        XmlRootAttribute xmlRootAttribute = (XmlRootAttribute)xmlRootAttributes[0];
                        _isTopLevelElementNullable = xmlRootAttribute.IsNullable;
                        string elementName = xmlRootAttribute.ElementName;
                        _topLevelElementName = string.IsNullOrEmpty(elementName) ? Name : dictionary.Add(DataContract.EncodeLocalName(elementName));
                        string? elementNs = xmlRootAttribute.Namespace;
                        _topLevelElementNamespace = string.IsNullOrEmpty(elementNs) ? DictionaryGlobals.EmptyString : dictionary.Add(elementNs);
                    }
                    else
                    {
                        throw new InvalidDataContractException(SR.Format(SR.IsAnyCannotHaveXmlRoot, DataContract.GetClrTypeFullName(UnderlyingType!)));
                    }
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
                                _knownDataContracts = DataContract.ImportKnownTypeAttributes(this.UnderlyingType);
                                Interlocked.MemoryBarrier();
                                _isKnownTypeAttributeChecked = true;
                            }
                            _knownDataContracts ??= new DataContractDictionary();
                        }
                    }
                    return _knownDataContracts;
                }

                set
                { _knownDataContracts = value; }
            }

            internal XmlSchemaType? XsdType
            {
                get => _xsdType;
                set => _xsdType = value;
            }

            internal bool IsAnonymous => _xsdType != null;

            internal override XmlDictionaryString? TopLevelElementName
            {
                get => _topLevelElementName;
                set => _topLevelElementName = value;
            }

            internal override XmlDictionaryString? TopLevelElementNamespace
            {
                get => _topLevelElementNamespace;
                set => _topLevelElementNamespace = value;
            }

            internal bool IsTopLevelElementNullable
            {
                get => _isTopLevelElementNullable;
                set => _isTopLevelElementNullable = value;
            }

            internal bool IsTypeDefinedOnImport
            {
                get => _isTypeDefinedOnImport;
                set => _isTypeDefinedOnImport = value;
            }

            internal CreateXmlSerializableDelegate? CreateXmlSerializableDelegate
            {
                get => _createXmlSerializable;
                set => _createXmlSerializable = value;
            }
        }

        private ConstructorInfo? GetConstructor()
        {
            if (UnderlyingType.IsValueType)
                return null;

            ConstructorInfo? ctor = UnderlyingType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, Type.EmptyTypes);
            if (ctor == null)
                throw new InvalidDataContractException(SR.Format(SR.IXmlSerializableMustHaveDefaultConstructor, DataContract.GetClrTypeFullName(UnderlyingType)));

            return ctor;
        }

        internal void SetTopLevelElementName(XmlQualifiedName? elementName)
        {
            if (elementName != null)
            {
                XmlDictionary dictionary = new XmlDictionary();
                TopLevelElementName = dictionary.Add(elementName.Name);
                TopLevelElementNamespace = dictionary.Add(elementName.Namespace);
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal CreateXmlSerializableDelegate GenerateCreateXmlSerializableDelegate()
        {
            Type type = this.UnderlyingType;
            CodeGenerator ilg = new CodeGenerator();
            bool memberAccessFlag = RequiresMemberAccessForCreate(null) && !(type.FullName == "System.Xml.Linq.XElement");
            try
            {
                ilg.BeginMethod("Create" + DataContract.GetClrTypeFullName(type), typeof(CreateXmlSerializableDelegate), memberAccessFlag);
            }
            catch (SecurityException securityException)
            {
                if (memberAccessFlag)
                {
                    RequiresMemberAccessForCreate(securityException);
                }
                else
                {
                    throw;
                }
            }
            if (type.IsValueType)
            {
                System.Reflection.Emit.LocalBuilder local = ilg.DeclareLocal(type);
                ilg.Ldloca(local);
                ilg.InitObj(type);
                ilg.Ldloc(local);
            }
            else
            {
                // Special case XElement
                // codegen the same as 'internal XElement : this("default") { }'
                ConstructorInfo ctor = GetConstructor()!;
                if (!ctor.IsPublic && type.FullName == "System.Xml.Linq.XElement")
                {
                    Type? xName = type.Assembly.GetType("System.Xml.Linq.XName");
                    if (xName != null)
                    {
                        MethodInfo? XName_op_Implicit = xName.GetMethod(
                            "op_Implicit",
                            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                            new Type[] { typeof(string) }
                            );
                        ConstructorInfo? XElement_ctor = type.GetConstructor(
                            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                            new Type[] { xName }
                            );
                        if (XName_op_Implicit != null && XElement_ctor != null)
                        {
                            ilg.Ldstr("default");
                            ilg.Call(XName_op_Implicit);
                            ctor = XElement_ctor;
                        }
                    }
                }
                ilg.New(ctor);
            }
            ilg.ConvertValue(this.UnderlyingType, Globals.TypeOfIXmlSerializable);
            ilg.Ret();
            return (CreateXmlSerializableDelegate)ilg.EndMethod();
        }

        /// <SecurityNote>
        /// Review - calculates whether this Xml type requires MemberAccessPermission for deserialization.
        ///          since this information is used to determine whether to give the generated code access
        ///          permissions to private members, any changes to the logic should be reviewed.
        /// </SecurityNote>
        private bool RequiresMemberAccessForCreate(SecurityException? securityException)
        {
            if (!IsTypeVisible(UnderlyingType))
            {
                if (securityException != null)
                {
                    throw new SecurityException(SR.Format(SR.PartialTrustIXmlSerializableTypeNotPublic, DataContract.GetClrTypeFullName(UnderlyingType)),
                        securityException);
                }
                return true;
            }

            if (ConstructorRequiresMemberAccess(GetConstructor()))
            {
                if (securityException != null)
                {
                    throw new SecurityException(SR.Format(SR.PartialTrustIXmlSerialzableNoPublicConstructor, DataContract.GetClrTypeFullName(UnderlyingType)),
                        securityException);
                }
                return true;
            }

            return false;
        }

        internal IXmlSerializable ReflectionCreateXmlSerializable(Type type)
        {
            if (type.IsValueType)
            {
                throw new NotImplementedException("ReflectionCreateXmlSerializable - value type");
            }
            else
            {
                object? o;
                if (type == typeof(System.Xml.Linq.XElement))
                {
                    o = new System.Xml.Linq.XElement("default");
                }
                else
                {
                    ConstructorInfo ctor = GetConstructor()!;
                    o = ctor.Invoke(Array.Empty<object>());
                }

                return (IXmlSerializable)o;
            }
        }

        internal override bool Equals(object? other, HashSet<DataContractPairKey>? checkedContracts)
        {
            if (IsEqualOrChecked(other, checkedContracts))
                return true;

            if (other is XmlDataContract dataContract)
            {
                if (this.HasRoot != dataContract.HasRoot)
                    return false;

                if (this.IsAnonymous)
                {
                    return dataContract.IsAnonymous;
                }
                else
                {
                    return (XmlName.Name == dataContract.XmlName.Name && XmlName.Namespace == dataContract.XmlName.Namespace);
                }
            }
            return false;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext? context)
        {
            if (context == null)
                XmlObjectSerializerWriteContext.WriteRootIXmlSerializable(xmlWriter, obj);
            else
                context.WriteIXmlSerializable(xmlWriter, obj);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override object? ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext? context)
        {
            object? o;
            if (context == null)
            {
                o = XmlObjectSerializerReadContext.ReadRootIXmlSerializable(xmlReader, this, true /*isMemberType*/);
            }
            else
            {
                o = context.ReadIXmlSerializable(xmlReader, this, true /*isMemberType*/);
                context.AddNewObject(o);
            }
            xmlReader.ReadEndElement();
            return o;
        }
    }
}
