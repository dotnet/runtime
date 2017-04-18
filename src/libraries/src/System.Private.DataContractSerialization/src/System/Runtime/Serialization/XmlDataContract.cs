// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Xml;
    using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, DataContract>;
    using System.Xml.Serialization;
    using System.Xml.Schema;
    using System.Security;
    using System.Linq;

#if uapaot
    public delegate IXmlSerializable CreateXmlSerializableDelegate();
    public sealed class XmlDataContract : DataContract
#else
    internal delegate IXmlSerializable CreateXmlSerializableDelegate();
    internal sealed class XmlDataContract : DataContract
#endif
    {
        private XmlDataContractCriticalHelper _helper;

        public XmlDataContract() : base(new XmlDataContractCriticalHelper())
        {
            _helper = base.Helper as XmlDataContractCriticalHelper;
        }

        internal XmlDataContract(Type type) : base(new XmlDataContractCriticalHelper(type))
        {
            _helper = base.Helper as XmlDataContractCriticalHelper;
        }

        public override DataContractDictionary KnownDataContracts
        {
            get
            { return _helper.KnownDataContracts; }

            set
            { _helper.KnownDataContracts = value; }
        }

        internal XmlSchemaType XsdType
        {
            get { return _helper.XsdType; }
            set { _helper.XsdType = value; }
        }


        internal bool IsAnonymous
        {
            get
            { return _helper.IsAnonymous; }
        }

        public override bool HasRoot
        {
            get
            { return _helper.HasRoot; }

            set
            { _helper.HasRoot = value; }
        }

        public override XmlDictionaryString TopLevelElementName
        {
            get
            { return _helper.TopLevelElementName; }

            set
            { _helper.TopLevelElementName = value; }
        }

        public override XmlDictionaryString TopLevelElementNamespace
        {
            get
            { return _helper.TopLevelElementNamespace; }

            set
            { _helper.TopLevelElementNamespace = value; }
        }

        internal bool IsTopLevelElementNullable
        {
            get { return _helper.IsTopLevelElementNullable; }
            set { _helper.IsTopLevelElementNullable = value; }
        }

#if uapaot
        private CreateXmlSerializableDelegate _createXmlSerializableDelegate;
        public CreateXmlSerializableDelegate CreateXmlSerializableDelegate        
#else
        internal CreateXmlSerializableDelegate CreateXmlSerializableDelegate
#endif
        {
#if !uapaot
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
#else              
            get
            {
                if (DataContractSerializer.Option == SerializationOption.CodeGenOnly 
                 || (DataContractSerializer.Option == SerializationOption.ReflectionAsBackup && _createXmlSerializableDelegate != null))
                {
                    return _createXmlSerializableDelegate;
                }

                return () => ReflectionCreateXmlSerializable(this.UnderlyingType);
            }
            set
            {
                _createXmlSerializableDelegate = value;
            }
#endif            
        }

        internal override bool CanContainReferences => false;

        public override bool IsBuiltInDataContract => UnderlyingType == Globals.TypeOfXmlElement || UnderlyingType == Globals.TypeOfXmlNodeArray;

        private class XmlDataContractCriticalHelper : DataContract.DataContractCriticalHelper
        {
            private DataContractDictionary _knownDataContracts;
            private bool _isKnownTypeAttributeChecked;
            private XmlDictionaryString _topLevelElementName;
            private XmlDictionaryString _topLevelElementNamespace;
            private bool _isTopLevelElementNullable;
            private bool _hasRoot;
            private CreateXmlSerializableDelegate _createXmlSerializable;
            private XmlSchemaType _xsdType;

            internal XmlDataContractCriticalHelper()
            {
            }

            internal XmlDataContractCriticalHelper(Type type) : base(type)
            {
                if (type.IsDefined(Globals.TypeOfDataContractAttribute, false))
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.IXmlSerializableCannotHaveDataContract, DataContract.GetClrTypeFullName(type))));
                if (type.IsDefined(Globals.TypeOfCollectionDataContractAttribute, false))
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.IXmlSerializableCannotHaveCollectionDataContract, DataContract.GetClrTypeFullName(type))));
                XmlSchemaType xsdType;
                bool hasRoot;
                XmlQualifiedName stableName;
                SchemaExporter.GetXmlTypeInfo(type, out stableName, out xsdType, out hasRoot);
                this.StableName = stableName;
                this.HasRoot = hasRoot;
                XmlDictionary dictionary = new XmlDictionary();
                this.Name = dictionary.Add(StableName.Name);
                this.Namespace = dictionary.Add(StableName.Namespace);
                object[] xmlRootAttributes = (UnderlyingType == null) ? null : UnderlyingType.GetCustomAttributes(Globals.TypeOfXmlRootAttribute, false).ToArray();
                if (xmlRootAttributes == null || xmlRootAttributes.Length == 0)
                {
                    if (hasRoot)
                    {
                        _topLevelElementName = Name;
                        _topLevelElementNamespace = (this.StableName.Namespace == Globals.SchemaNamespace) ? DictionaryGlobals.EmptyString : Namespace;
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
                        _topLevelElementName = (elementName == null || elementName.Length == 0) ? Name : dictionary.Add(DataContract.EncodeLocalName(elementName));
                        string elementNs = xmlRootAttribute.Namespace;
                        _topLevelElementNamespace = (elementNs == null || elementNs.Length == 0) ? DictionaryGlobals.EmptyString : dictionary.Add(elementNs);
                    }
                    else
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.IsAnyCannotHaveXmlRoot, DataContract.GetClrTypeFullName(UnderlyingType))));
                    }
                }
            }

            internal override DataContractDictionary KnownDataContracts
            {
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
                        }
                    }
                    return _knownDataContracts;
                }

                set
                { _knownDataContracts = value; }
            }

            internal XmlSchemaType XsdType
            {
                get { return _xsdType; }
                set { _xsdType = value; }
            }

            internal bool IsAnonymous => _xsdType != null;

            internal override bool HasRoot
            {
                get
                { return _hasRoot; }

                set
                { _hasRoot = value; }
            }

            internal override XmlDictionaryString TopLevelElementName
            {
                get
                { return _topLevelElementName; }
                set
                { _topLevelElementName = value; }
            }

            internal override XmlDictionaryString TopLevelElementNamespace
            {
                get
                { return _topLevelElementNamespace; }
                set
                { _topLevelElementNamespace = value; }
            }

            internal bool IsTopLevelElementNullable
            {
                get { return _isTopLevelElementNullable; }
                set { _isTopLevelElementNullable = value; }
            }

            internal CreateXmlSerializableDelegate CreateXmlSerializableDelegate
            {
                get { return _createXmlSerializable; }
                set { _createXmlSerializable = value; }
            }
        }

        private ConstructorInfo GetConstructor()
        {
            Type type = UnderlyingType;

            if (type.IsValueType)
                return null;

            ConstructorInfo ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, Array.Empty<Type>());
            if (ctor == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.IXmlSerializableMustHaveDefaultConstructor, DataContract.GetClrTypeFullName(type))));

            return ctor;
        }

#if !uapaot
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
                System.Reflection.Emit.LocalBuilder local = ilg.DeclareLocal(type, type.Name + "Value");
                ilg.Ldloca(local);
                ilg.InitObj(type);
                ilg.Ldloc(local);
            }
            else
            {
                // Special case XElement
                // codegen the same as 'internal XElement : this("default") { }'
                ConstructorInfo ctor = GetConstructor();
                if (!ctor.IsPublic && type.FullName == "System.Xml.Linq.XElement")
                {
                    Type xName = type.Assembly.GetType("System.Xml.Linq.XName");
                    if (xName != null)
                    {
                        MethodInfo XName_op_Implicit = xName.GetMethod(
                            "op_Implicit",
                            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                            new Type[] { typeof(String) }
                            );
                        ConstructorInfo XElement_ctor = type.GetConstructor(
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
        private bool RequiresMemberAccessForCreate(SecurityException securityException)
        {
            if (!IsTypeVisible(UnderlyingType))
            {
                if (securityException != null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new SecurityException(SR.Format(SR.PartialTrustIXmlSerializableTypeNotPublic, DataContract.GetClrTypeFullName(UnderlyingType)),
                        securityException));
                }
                return true;
            }

            if (ConstructorRequiresMemberAccess(GetConstructor()))
            {
                if (securityException != null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new SecurityException(SR.Format(SR.PartialTrustIXmlSerialzableNoPublicConstructor, DataContract.GetClrTypeFullName(UnderlyingType)),
                        securityException));
                }
                return true;
            }

            return false;
        }
#endif

        internal IXmlSerializable ReflectionCreateXmlSerializable(Type type)
        {
            if (type.IsValueType)
            {
                throw new NotImplementedException("ReflectionCreateXmlSerializable - value type");
            }
            else
            {
                object o = null;
                if (type == typeof(System.Xml.Linq.XElement))
                {
                    o = new System.Xml.Linq.XElement("default");
                }
                else
                {
                    ConstructorInfo ctor = GetConstructor();
                    o = ctor.Invoke(new object[] { });
                }

                return (IXmlSerializable)o;
            }
        }

        public override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
        {
            if (context == null)
                XmlObjectSerializerWriteContext.WriteRootIXmlSerializable(xmlWriter, obj);
            else
                context.WriteIXmlSerializable(xmlWriter, obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context)
        {
            object o;
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