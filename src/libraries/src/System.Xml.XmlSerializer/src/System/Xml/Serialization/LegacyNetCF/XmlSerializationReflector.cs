// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

using System;
using System.Xml;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Schema;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace System.Xml.Serialization.LegacyNetCF
{
    /// <summary>
    /// Interface representing an object that can find a LogicalMemberValue and 
    /// Type given the name as the key.
    /// </summary>
    internal interface IEntityFinder
    {
        bool find(string name, out LogicalMemberValue member, out Type type);
    }

    /// <summary>
    /// Finds information regarding a Type's Member. This class implements that 
    /// EntityFinder interface, so it looks up the LogicalMemberValue and Type for 
    /// the specified member name. 
    /// </summary>
    internal class MemberFinder : IEntityFinder
    {
        private Type _owningType;
        private Dictionary<string, MemberInfo> _members;

        public MemberFinder(Type owningType)
        {
            _owningType = owningType;
            _members = new Dictionary<string, MemberInfo>();

            FieldInfo[] fields = _owningType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo field in fields)
            {
                _members[field.Name] = field;
            }

            PropertyInfo[] props = _owningType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo prop in props)
            {
                _members[prop.Name] = prop;
            }
        }

        public bool find(string name, out LogicalMemberValue member, out Type type)
        {
            if (!_members.ContainsKey(name))
            {
                member = null;
                type = null;
                return false;
            }

            MemberInfo m = _members[name];
            if (m is FieldInfo)
            {
                type = ((FieldInfo)m).FieldType;
                member = new LogicalMemberValue(null/*accessors*/, false/*required*/, true/*canRead*/, true/*canWrite*/ );
            }
            else
            {
                PropertyInfo pInfo = (PropertyInfo)m;
                type = pInfo.PropertyType;
                member = new LogicalMemberValue(null/*accessors*/, false/*required*/, pInfo.CanRead, pInfo.CanWrite);
            }

            member.Fetcher = new MemberFetcher(m);
            MemberFixup memberFixup = new MemberFixup(m);
            memberFixup.TargetType = null;
            member.Fixup = memberFixup;
            return true;
        }
    }

    /// <summary>
    /// Represents the information found in the XmlInclude and SoapInclude 
    /// attributes. This information is stored so that included types can be batch
    /// processed/reflected after all other types have been processed/reflected.
    /// </summary>
    internal class IncludedTypeEntry
    {
        public Type IncludedType;
        public string DefaultNamespace;
        public bool Encoded;

        public IncludedTypeEntry(Type type, string defaultNamespace, bool encoded)
        {
            IncludedType = type;
            DefaultNamespace = defaultNamespace;
            Encoded = encoded;
        }
    }

    internal partial class XmlSerializationReflector
    {
        [Flags]
        protected enum SpecialMember : byte
        {
            None = 0x0,
            Xmlns = 0x1,
            XmlText = 0x2,
            XmlAnyAttribute = 0x4,
            XmlAnyElement = 0x8,
        }
        protected static List<Type> m_UnsupportedTypes;
        private static string s_ANY_ELEMENT_NAME = "##any:";
        private const string ANY_TYPE_STR = "anyType";

        protected TypeContainer m_TypeContainer;
        protected XmlAttributeOverrides m_XmlOverrides;
        protected SoapAttributeOverrides m_SoapOverrides;
        protected List<IncludedTypeEntry> m_IncludedTypes;
        protected string m_DefaultNamespace;
        protected bool m_ReflectionDisabled;

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
        // Constructors
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//


        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public XmlSerializationReflector()
        {
            m_TypeContainer = new TypeContainer();
            m_DefaultNamespace = null;
        }

        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public XmlSerializationReflector(SoapAttributeOverrides soapOverrides, string defaultNamespace)
        {
            m_TypeContainer = new TypeContainer();
            m_SoapOverrides = soapOverrides;
            m_DefaultNamespace = defaultNamespace;
        }

        public XmlSerializationReflector(XmlAttributeOverrides xmlOverrides, string defaultNamespace)
        {
            m_TypeContainer = new TypeContainer();
            m_XmlOverrides = xmlOverrides;
            m_DefaultNamespace = defaultNamespace;
        }


        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
        // Properties
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//

        /// <summary>
        /// The namespace that will be applied to the root type if no namespace is explicitly defined on that type via xml attributes.
        /// </summary>
        internal string DefaultNamespace
        {
            get { return m_DefaultNamespace; }

            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            set
            { m_DefaultNamespace = value; }
        }

        internal XmlAttributeOverrides XmlAttributeOverrides
        {
            get { return m_XmlOverrides; }
            set { m_XmlOverrides = value; }
        }

        internal SoapAttributeOverrides SoapAttributeOverrides
        {
            get { return m_SoapOverrides; }

            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            set
            { m_SoapOverrides = value; }
        }

        protected List<Type> UnsupportedTypes
        {
            get
            {
                if (m_UnsupportedTypes == null)
                {
                    m_UnsupportedTypes = new List<Type>();
                    m_UnsupportedTypes.Add(typeof(IntPtr));
                    m_UnsupportedTypes.Add(typeof(UIntPtr));
                    m_UnsupportedTypes.Add(typeof(ValueType));
                }
                return m_UnsupportedTypes;
            }
        }

        /// <summary>
        /// Whether the reflection stage of serialization is completed.  When true, if an unexpected type is encountered an exception will be thrown rather than reflected over.
        /// </summary>
        internal bool ReflectionDisabled
        {
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            get
            { return m_ReflectionDisabled; }
            set { m_ReflectionDisabled = value; }
        }

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
        // Reflection Methods
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
        private object _includedTypesLock = new object();

        /// <summary>
        /// Reflects over all of the types that have been added to the included types
        /// collection. This allows us to batch process/reflect over included types.
        /// </summary>
        /// <remarks>
        /// Care must be taken in moving over the included types collection. Types can 
        /// be added to the collection while moving through the collection. In order to 
        /// handle this, we make a copy of the included types collection before moving
        /// through it. We also check the collection again after moving through the copy
        /// just in case more types were added.
        /// </remarks>
        public void ReflectIncludedTypes()
        {
            if (m_IncludedTypes == null)
                return;

            List<IncludedTypeEntry> includedTypeCopy = null;
            while (m_IncludedTypes.Count > 0)
            {
                lock (_includedTypesLock)
                {
                    includedTypeCopy = new List<IncludedTypeEntry>(m_IncludedTypes);
                    m_IncludedTypes.Clear();
                }

                LogicalType includeType;
                foreach (IncludedTypeEntry entry in includedTypeCopy)
                {
                    includeType = FindType(entry.IncludedType, entry.Encoded, entry.DefaultNamespace);
                    if (includeType != null && includeType.IsAnonymousType)
                    {
                        //XmlAnonymousInclude=Cannot include anonymous type '{0}'.
                        throw new InvalidOperationException(SR.Format(SR.XmlAnonymousInclude, entry.IncludedType.FullName));
                    }
                }
            }
        }

        /// <summary>
        /// Handles the XmlInclude and SoapInclude attributes. The data from the 
        /// attributes is added to the included types collection so that the types can 
        /// be batch processed/reflected after all other types are processed/reflected.
        /// </summary>
        protected void ReflectIncludeAttributes(SharedAttributes attrs)
        {
            lock (_includedTypesLock)
            {
                if (m_IncludedTypes == null)
                    m_IncludedTypes = new List<IncludedTypeEntry>();

                //SoapIncludeAttribute
                if (attrs.EncodedIncludedTypes != null)
                {
                    foreach (Type type in attrs.EncodedIncludedTypes)
                    {
                        m_IncludedTypes.Add(new IncludedTypeEntry(type, m_DefaultNamespace, true));
                    }
                }

                //XmlIncludeAttribute
                if (attrs.LiteralIncludedTypes != null)
                {
                    foreach (Type type in attrs.LiteralIncludedTypes)
                    {
                        m_IncludedTypes.Add(new IncludedTypeEntry(type, m_DefaultNamespace, false));
                    }
                }
            }
        }

        /// <summary>
        /// Finds the OverrideXmlAttributes for a given type
        /// </summary>  
        private XmlAttributes FindOverrideXmlAttributes(Type type)
        {
            return FindOverrideXmlAttributes(type, string.Empty);
        }

        /// <summary>
        /// Finds the OverrideXmlAttributes for a member on a given type
        /// </summary>  
        private XmlAttributes FindOverrideXmlAttributes(Type type, string memberName)
        {
            Debug.Assert(memberName != null, "The memberName parameter should never be null. Pass string.Empty instead");
            Debug.Assert(type != null, "The type parameter can not be null.");

            if (m_XmlOverrides == null)
                return null;

            // Find the XmlAttributes object mapped to the specified member 
            // of the specified type in the m_XmlAttributes object.            
            return m_XmlOverrides[type, memberName];
        }

        /// <summary>
        /// Finds the OverrideSoapAttributes for a given type
        /// </summary>
        private SoapAttributes FindOverrideSoapAttributes(Type type)
        {
            return FindOverrideSoapAttributes(type, string.Empty);
        }

        /// <summary>
        /// Finds the OverrideSoapAttributes for a member on a given type
        /// </summary>
        private SoapAttributes FindOverrideSoapAttributes(Type type, string memberName)
        {
            Debug.Assert(memberName != null, "The memberName parameter should never be null. Pass string.Empty instead");
            Debug.Assert(type != null, "The type parameter can not be null.");

            if (m_SoapOverrides == null)
                return null;

            // Find the SoapAttributes object mapped to the specified 
            // member of the specified type in the m_SoapAttributes object. 
            return m_SoapOverrides[type, memberName];
        }

        /// <summary>
        /// Reflects over the member of a given type. This method builds a 
        /// LogicalMemberValue and adds it to the specified MemberValueCollection.
        /// </summary>
        protected LogicalMemberValue ReflectMemberValue(Type memberType,
            MemberInfo memberAttrProv,
            string defaultName,
            string defaultNS,
            IEntityFinder memberFinder,
            Fetcher fetcher,
            Fixup fixup,
            MemberValueCollection members,
            bool encoded,
            bool canRead,
            bool canWrite,
            out SpecialMember specialType,
            ref bool shouldBeOrdered)
        {
            LogicalMemberValue member;
            string memberName = string.Empty;
            specialType = SpecialMember.None;
            try
            {
                memberName = memberAttrProv.Name;

                if (encoded)
                {
                    EncodedAttributes attrs;
                    SoapAttributes soapAtts = FindOverrideSoapAttributes(memberAttrProv.DeclaringType, memberAttrProv.Name);
                    attrs = new EncodedAttributes(memberAttrProv, soapAtts);

                    member = ReflectEncodedMemberValue(memberType, memberName, attrs, defaultName, defaultNS, canRead, canWrite);
                }
                else
                {
                    LiteralAttributes attrs;
                    XmlAttributes xmlAtts = FindOverrideXmlAttributes(memberAttrProv.DeclaringType, memberAttrProv.Name);
                    attrs = new LiteralAttributes(memberAttrProv, xmlAtts);

                    // Handle XmlAttributes: DefaultValue, Specified, XmlElement, XmlAttribute, XmlArray, and XmlArrayItem
                    member = ReflectLiteralMemberValue(memberType, memberName, attrs, defaultName, defaultNS, memberFinder, canRead, canWrite, ref shouldBeOrdered);

                    // Handle XmlAttributes: Handle XmlAnys, XmlTexts, and Xmlns, if present
                    LogicalMemberValue specialMember = null;
                    if (member == null)
                    {
                        specialType = ReflectCollectionSpecials(memberType, memberName, attrs, defaultName, defaultNS, fetcher, fixup, members, memberFinder, out specialMember, ref shouldBeOrdered);
                        if (specialMember != null)
                        {
                            // although the specialMember var falls out of scope, references to its object remain,
                            // so setting its MemberName here is useful.
                            specialMember.MemberName = memberName;
                        }
                    }
                }

                if (member == null) // if XmlIgnore'd or some unsupported member type.
                    return null;

                if (fetcher != null) member.Fetcher = fetcher;
                if (fixup != null) member.Fixup = fixup;
                member.MemberName = memberName;
                members.addMemberValue(member, memberAttrProv.Name);

                return member;
            }
            catch (Exception e)
            {
                string msg = "";
                if (memberAttrProv is FieldInfo)
                    msg = SR.XmlFieldReflectionError;
                else if (memberAttrProv is PropertyInfo)
                    msg = SR.XmlPropertyReflectionError;
                if (msg != "")
                    throw new InvalidOperationException(SR.Format(msg, defaultName), e);
                else
                    throw; // unable to find an appropriate wrapping message, so just rethrow.
            }
        }

        /// <summary>
        /// Reflects over the member of a given type using encoding semantics and builds a 
        /// LogicalMemberValue.
        /// </summary>
        protected LogicalMemberValue ReflectEncodedMemberValue(Type memberType, string memberName,
            EncodedAttributes attrProv, string defaultName, string defaultNS,
            bool canRead, bool canWrite)
        {
            if (attrProv.Ignore)
                return null;

            AccessorCollection accessors = new AccessorCollection();
            LogicalMemberValue memberValue = new LogicalMemberValue(accessors, false/*required*/, canRead, canWrite);
            LogicalType memberLogicalType = FindType(memberType, true/*encoded*/, defaultNS);

            memberValue.DefaultValue = ReflectDefaultValueAttribute(memberType, attrProv);
            ReflectSoapElementAttribute(memberType, attrProv, memberLogicalType, accessors, defaultName, null/*defaultNS*/ );
            ReflectSoapAttributeAttribute(memberType, memberName, attrProv, memberLogicalType, accessors, defaultName, defaultNS);

            Accessor defaultAcc = accessors.findAccessorNoThrow(memberType);
            if (defaultAcc != null)
            {
                accessors.Default = defaultAcc;
            }
            else
            {
                LogicalType defaultLType = FindType(memberType, true/*encoded*/, defaultNS);
                accessors.Default = new Accessor(defaultName, defaultNS, defaultLType, false, false);
            }

            return memberValue;
        }

        /// <summary>
        /// Reflects over the SoapAttributeAttribute 
        /// </summary>
        protected void ReflectSoapAttributeAttribute(Type memberType, string memberName, EncodedAttributes attrProv,
            LogicalType logicalMemberType, AccessorCollection memberAccessors,
            string defaultName, string defaultNS)
        {
            if (attrProv.SoapAttribute != null)
            {
                if (memberType == typeof(object))
                    throw new InvalidOperationException(SR.Format(SR.XmlIllegalSoapAttribute, memberName, memberType.FullName));
                SoapAttributeAttribute saa = attrProv.SoapAttribute;

                string name = string.IsNullOrEmpty(saa.AttributeName) ? defaultName : saa.AttributeName;

                string ns = defaultNS;
                if (name != null && name.IndexOf(':') != -1)
                {
                    CreateNameAndNamespaceFromQnameString(name, out name, out ns);
                    if (Soap.XmlPrefix.Equals(ns)) ns = Soap.XmlNamespace;
                }
                else if (saa.Namespace != null)
                {
                    ns = saa.Namespace;
                }

                LogicalType type;
                if (saa.DataType != null && saa.DataType.Length > 0)
                {
                    if (!SerializationHelper.IsSerializationPrimitive(memberType) && memberType != typeof(byte[]))
                        throw new InvalidOperationException(SR.Format(SR.XmlInvalidDataTypeUsage, saa.DataType, "SoapAttributeAttribute.DataType"));
                    type = FindType(saa.DataType, true/*encoded*/ );
                }
                else
                {
                    type = logicalMemberType;
                }

                Accessor accessor = new Accessor(name, ns, type, false, true);
                memberAccessors.add(accessor);
            }
        }

        /// <summary>
        /// Reflects over the SoapElementAttribute 
        /// </summary>
        protected void ReflectSoapElementAttribute(Type memberType, EncodedAttributes attrProv,
            LogicalType logicalMemberType, AccessorCollection memberAccessors,
            string defaultName, string defaultNS)
        {
            if (attrProv.SoapElement != null)
            {
                SoapElementAttribute sea = attrProv.SoapElement;

                bool isNullable = sea.IsNullable;
                if (true /* AppDomain.CompatVersion > AppDomain.Whidbey */)
                    CheckNullable(isNullable, memberType);

                string name = string.IsNullOrEmpty(sea.ElementName) ? defaultName : sea.ElementName;

                LogicalType type;
                if (!string.IsNullOrEmpty(sea.DataType))
                {
                    if (!SerializationHelper.IsSerializationPrimitive(memberType) && memberType != typeof(byte[]))
                        throw new InvalidOperationException(SR.Format(SR.XmlInvalidDataTypeUsage, sea.DataType, "SoapElementAttribute.DataType"));
                    type = FindType(sea.DataType, true/*encoded*/ );
                }
                else
                {
                    type = logicalMemberType;
                }

                memberAccessors.add(new Accessor(name, defaultNS, type, isNullable, false));
            }
        }

        /// <summary>
        /// Reflects over the member of a given type using literal semantics and builds a 
        /// LogicalMemberValue.
        /// </summary>
        protected LogicalMemberValue ReflectLiteralMemberValue(Type memberType, string memberName, LiteralAttributes attrProv,
            string defaultName, string defaultNS, IEntityFinder memberFinder, bool canRead, bool canWrite, ref bool shouldBeOrdered)
        {
            if (IgnoreLiteralMember(attrProv))
                return null;

            AccessorCollection accessors = new AccessorCollection();
            LogicalMemberValue memberValue = new LogicalMemberValue(accessors, false, canRead, canWrite);
            Type serializingType = null;

            memberValue.DefaultValue = ReflectDefaultValueAttribute(memberType, attrProv);
            memberValue.SpecifiedMember = FindSpecifiedMember(defaultName, memberFinder);
            // NOTE: THIS MUST BE DONE HERE BECAUSE THE FOLLOWING ATTRIBUTES DEPEND ON IT.    
            memberValue.Accessors.Choice = ReflectXmlChoiceIdentifierAttribute(memberType, memberName, attrProv, memberFinder, defaultNS);
            ReflectXmlElementAttributes(memberType, memberValue, memberName, attrProv, accessors, defaultName, defaultNS, ref serializingType, ref shouldBeOrdered);
            ReflectXmlAttributeAttribute(memberType, memberValue, attrProv, accessors, defaultName, defaultNS);
            ReflectXmlArrayAttribute(memberType, memberValue, memberName, attrProv, accessors, defaultName, defaultNS, ref shouldBeOrdered);
            ReflectXmlArrayItemAttribute(memberType, attrProv, accessors, defaultName, defaultNS);

            // Now, we look for the default accessor. We look up the accessor by the type it 
            // actually serializes as. If a type different from the member's declared type is 
            // to be used then we use that type as the key. Otherwise, we use the declared type.                                          
            Accessor defaultAcc = accessors.findAccessorNoThrow((serializingType != null ? serializingType : memberType));
            if (defaultAcc != null)
            {
                accessors.Default = defaultAcc;
            }
            else
            {
                LogicalType defaultLType = FindType(memberType, false/*encoding*/, defaultNS);
                if (defaultLType != null)
                {
                    accessors.Default = new Accessor(defaultName, defaultNS, defaultLType, defaultLType.IsNullableType, false/*isAttribute*/ );
                }
            }

            return accessors.Default != null ? memberValue : null;
        }

        /// <summary>
        /// Reflects over the XmlAttributeAttribute
        /// </summary>
        protected void ReflectXmlAttributeAttribute(Type memberType, LogicalMemberValue memberValue,
            LiteralAttributes attrProv, AccessorCollection memberAccessors,
            string defaultName, string defaultNS)
        {
            if (attrProv.XmlAttribute != null)
            {
                if (true /* AppDomain.CompatVersion >= AppDomain.Orcas */)
                {
                    LiteralAttributeFlags flags = attrProv.XmlFlags;
                    if ((flags & LiteralAttributes.XmlAttributeFlags) != flags)
                        throw new InvalidOperationException(SR.XmlIllegalAttributesArrayAttribute);
                }

                XmlAttributeAttribute xaa = attrProv.XmlAttribute;

                string name = string.IsNullOrEmpty(xaa.AttributeName) ? defaultName : xaa.AttributeName;

                string ns = null;
                if (xaa.Form != XmlSchemaForm.Unqualified)
                {
                    if (name != null && name.IndexOf(':') != -1)
                    {
                        CreateNameAndNamespaceFromQnameString(name, out name, out ns);
                        if (Soap.XmlPrefix.Equals(ns)) ns = Soap.XmlNamespace;
                    }
                    else if (xaa.Namespace != null)
                        ns = xaa.Namespace;
                }

                bool isArray;
                LogicalType type;
                LogicalType elementType;
                ResolveLiteralType(xaa.DataType, xaa.Type, memberType, defaultNS, out isArray, out type, out elementType);

                Accessor accessor = new Accessor(name, ns, type, false, true);
                if (SerializationHelper.isLogicalArray(type))
                {
                    //if this is a logical array, then this is a sequence, so construct a nested accessor for this.
                    Accessor nested = new Accessor(name, defaultNS, elementType, false, true);
                    accessor.NestedAccessors = new AccessorCollection();
                    accessor.NestedAccessors.add(nested);
                    accessor.NestedAccessors.Default = nested;
                }

                memberAccessors.add(accessor);
            }
        }

        /// <summary>
        /// Reflects over the XmlElementAttribute
        /// </summary>
        protected void ReflectXmlElementAttributes(Type memberType, LogicalMemberValue memberValue, string memberName,
            LiteralAttributes attrProv, AccessorCollection memberAccessors,
            string defaultName, string defaultNS, ref Type serializingType, ref bool shouldBeOrdered)
        {
            if (attrProv.XmlElements != null)
            {
                Debug.Assert(attrProv.XmlElements.Count > 0);

                // If this type is an array, collection, or enumerable then create one accessor with multiple nested accessors                
                foreach (XmlElementAttribute xea in attrProv.XmlElements)
                {
                    if (true /* AppDomain.CompatVersion >= AppDomain.Orcas */)
                    {
                        LiteralAttributeFlags flags = attrProv.XmlFlags;
                        if ((flags & LiteralAttributes.XmlElementFlags) != flags)
                            throw new InvalidOperationException(SR.XmlIllegalElementsArrayAttribute);
                    }

                    bool isNullable = xea.IsNullable;

                    if (xea.Order != -1)
                    {
                        if (memberValue.Order != -1 && memberValue.Order != xea.Order)
                            throw new InvalidOperationException(SR.Format(SR.XmlSequenceMatch, "Order"));
                        memberValue.Order = xea.Order;
                    }

                    string ns;

                    if (xea.Form == XmlSchemaForm.Unqualified)
                        ns = string.Empty;
                    else if (xea.Namespace != null)
                        ns = xea.Namespace;
                    else
                        ns = defaultNS;

                    bool isArray;
                    LogicalType type;
                    LogicalType elementType;
                    ResolveLiteralType(xea.DataType, xea.Type, memberType, ns, out isArray, out type, out elementType);
                    if (true /* AppDomain.CompatVersion >= AppDomain.Rogue */)
                    {
                        CheckNullable(isNullable, memberType);
                        if (isArray && true /* AppDomain.CompatVersion > AppDomain.Orcas */)
                        {
                            CheckNullable(isNullable, elementType.Type);
                        }
                    }

                    string name;
                    if (!string.IsNullOrEmpty(xea.ElementName))
                    {
                        name = xea.ElementName;
                    }
                    else
                    {
                        if (attrProv.XmlChoiceIdentifier != null)
                        {
                            if (SerializationHelper.isLogicalArray(type))
                                name = (elementType.CustomSerializer == CustomSerializerType.Object) ? ANY_TYPE_STR : elementType.TypeAccessor.Name;
                            else
                                name = (type.CustomSerializer == CustomSerializerType.Object) ? ANY_TYPE_STR : type.TypeAccessor.Name;
                        }
                        else
                        {
                            if (attrProv.XmlElements.Count > 1)
                            {
                                name = SerializationHelper.isLogicalArray(type) ? elementType.TypeAccessor.Name : type.TypeAccessor.Name;
                            }
                            else
                                name = defaultName;
                        }
                    }

                    //I'll search for this name during deserialization         
                    Accessor accessor = new Accessor(name, ns, type, isArray ? false : isNullable, false);
                    if (isArray)
                    {
                        accessor.AllowRepeats = true;

                        //make the element accessor for a repeated element array
                        Debug.Assert(elementType != null, "Null element type");
                        Accessor rElement = new Accessor(name, ns, elementType, isNullable, false);
                        rElement.IsXmlArrayItem = true;

                        // In order to deserialize, I'm creating an accessor with the repeated element name that maps to the
                        // array-like type (stored currently in accessor).  So, if there are 4 XEAs applied, I have 4 accessors.
                        // However, these accessors need to share a nested AccessorCollection, so that all accessors have all 4
                        // nested accessors (one for each type with a name).
                        //
                        // If there is only one XEA applied, than it is the default.  If there is more than one XEA applied,
                        // then there is no default.  I'm going to keep the default around since it doesn't really do any harm.                                              
                        Accessor existing = memberAccessors.findAccessorNoThrow(type.Type);
                        if (existing == null)
                        {
                            accessor.NestedAccessors = new AccessorCollection();
                            accessor.NestedAccessors.Choice = memberValue.Accessors.Choice;
                        }
                        else
                        {
                            accessor.NestedAccessors = existing.NestedAccessors;
                        }
                        accessor.NestedAccessors.add(rElement);
                        accessor.NestedAccessors.Default = rElement;
                    }
                    else
                    {
                        // If an XmlElement was found on a member that is not an array, then the type the property will be serialized as 
                        // may be different from its declared type. It could be DataType, Type, or its declared type. Therefore, we should 
                        // store a reference to the type it will actually serialize as. This type will be used later to find the default 
                        // accessor.
                        serializingType = type.Type;
                    }
                    memberAccessors.add(accessor);
                }

                if (shouldBeOrdered && memberValue != null && memberValue.Order == -1)
                    throw new InvalidOperationException(SR.Format(SR.XmlSequenceInconsistent, "Order", memberName));
                if (memberValue != null && memberValue.Order != -1)
                    shouldBeOrdered = true;
            }
        }

        /// <summary>
        /// Reflects over the XmlArrayAttribute
        /// </summary>
        protected void ReflectXmlArrayAttribute(Type memberType, LogicalMemberValue memberValue, string memberName,
            LiteralAttributes attrProv, AccessorCollection memberAccessors,
            string defaultName, string defaultNS, ref bool shouldBeOrdered)
        {
            if (attrProv.XmlArray != null)
            {
                if (true /* AppDomain.CompatVersion >= AppDomain.Orcas */)
                {
                    LiteralAttributeFlags flags = attrProv.XmlFlags;
                    if ((flags & LiteralAttributes.XmlArrayFlags) != flags)
                        throw new InvalidOperationException(SR.XmlIllegalArrayArrayAttribute);
                }

                XmlArrayAttribute xaa = attrProv.XmlArray;

                bool isNullable = xaa.IsNullable;
                memberValue.Order = xaa.Order;
                if (shouldBeOrdered && memberValue.Order == -1)
                    throw new InvalidOperationException(SR.Format(SR.XmlSequenceInconsistent, "Order", memberName));

                string name;
                if (xaa.ElementName != null && xaa.ElementName.Length > 0)
                    name = xaa.ElementName;
                else
                    name = defaultName;

                string ns;
                if (xaa.Form == XmlSchemaForm.Unqualified)
                    ns = string.Empty;
                else
                    ns = xaa.Namespace ?? defaultNS;

                bool searchIntrinsics = (memberType != typeof(byte[]));
                LogicalType memberLType = FindType(memberType, false/*encoded*/, defaultNS, searchIntrinsics ? TypeOrigin.All : TypeOrigin.User);
                if (true /* AppDomain.CompatVersion > AppDomain.Whidbey */)
                    CheckNullable(isNullable, memberType);

                if (memberLType != null)
                {
                    if (true /* AppDomain.CompatVersion >= AppDomain.Orcas */)
                    {
                        if (!SerializationHelper.isLogicalArray(memberLType))
                            throw new InvalidOperationException(SR.XmlIllegalAttribute);
                    }

                    Accessor accessor = new Accessor(name, ns, memberLType, isNullable, false);
                    memberAccessors.add(accessor);
                }

                if (shouldBeOrdered && memberValue != null && memberValue.Order == -1)
                    throw new InvalidOperationException(SR.Format(SR.XmlSequenceInconsistent, "Order", memberName));
                if (memberValue != null && memberValue.Order != -1)
                    shouldBeOrdered = true;
            }
        }

        /// <summary>
        /// Reflects over the XmlArrayItemAttribute
        /// </summary>
        protected void ReflectXmlArrayItemAttribute(Type memberType,
            LiteralAttributes attrProv, AccessorCollection memberAccessors,
            string defaultName, string defaultNS)
        {
            if (attrProv.XmlArrayItems != null)
            {
                Debug.Assert(attrProv.XmlArrayItems.Count > 0);

                if (true /* AppDomain.CompatVersion >= AppDomain.Orcas */)
                {
                    LiteralAttributeFlags flags = attrProv.XmlFlags;
                    if ((flags & LiteralAttributes.XmlArrayFlags) != flags)
                        throw new InvalidOperationException(SR.XmlIllegalArrayArrayAttribute);
                }

                LogicalType memberLType;

                // If there are XmlArrayItemAttributes, then make sure we have a default accessor for the array itself.                
                Accessor defaultArrayAccessor = memberAccessors.findAccessorNoThrow(memberType);
                if (defaultArrayAccessor != null)
                {
                    memberLType = defaultArrayAccessor.Type;
                }
                else
                {
                    bool searchIntrinsics = (memberType != typeof(byte[]));
                    memberLType = FindType(memberType, false/*encoded*/, defaultNS, searchIntrinsics ? TypeOrigin.All : TypeOrigin.User);
                    defaultArrayAccessor = new Accessor(defaultName, defaultNS, memberLType, false, false);
                    memberAccessors.add(defaultArrayAccessor);
                }

                if (memberLType != null)
                {
                    if (!SerializationHelper.isLogicalArray(memberLType))
                        throw new InvalidOperationException(SR.XmlIllegalAttribute);
                }

                //Map NestingLevels to AccessorCollections, then string them up.
                List<AccessorCollection> nestedCollections = new List<AccessorCollection>();
                foreach (XmlArrayItemAttribute xaa in attrProv.XmlArrayItems)
                {
                    int nestingLevel = xaa.NestingLevel;

                    // Arrays, by default, are in no particular namespace
                    string ns;
                    if (xaa.Form == XmlSchemaForm.Unqualified)
                        ns = string.Empty;
                    else
                    {
                        // Get a hint about the namespace from the member accessor
                        // that may have been built from an XmlArrayAttribute.
                        Accessor arrayAccessor = memberAccessors.findAccessor(memberType);
                        string arrayNS = (arrayAccessor != null) ? arrayAccessor.Namespace : null;
                        ns = xaa.Namespace ?? arrayNS ?? defaultNS;
                    }

                    LogicalType type;
                    if (xaa.DataType != null && xaa.DataType.Length > 0)
                        type = FindType(xaa.DataType, false/*encoded*/ );
                    else if (xaa.Type != null)
                        type = FindType(xaa.Type, false/*encoded*/, ns);
                    else
                    {
                        //the nesting level actually maps to something in
                        //the array, so figure it out.
                        int nest = nestingLevel;
                        bool deepEnough = true;
                        type = memberLType;
                        while (nest >= 0)
                        {
                            if (!SerializationHelper.isLogicalArray(type))
                            {
                                deepEnough = false;
                                break;
                            }
                            type = GetElementType(type);
                            --nest;
                        }
                        //defaults to typeof object if you go too deep.
                        if (!deepEnough)
                            type = TypeContainer.GetIntrinsicType(typeof(object));
                    }

                    string name;
                    if (xaa.ElementName != null && xaa.ElementName.Length > 0)
                        name = xaa.ElementName;
                    else
                        name = type.TypeAccessor.Name;

                    // We default XmlArrayItemAttribute.IsNullable to whether the array item type is nullable if the app dev didn't specify it.
                    bool isNullable = IsNullableOrReferenceType(type.Type);
                    if (xaa.IsNullableSpecified)
                    {
                        isNullable = xaa.IsNullable;
                        if (true /* AppDomain.CompatVersion > AppDomain.Whidbey */)
                            CheckNullable(isNullable, type.Type);
                    }

                    Accessor accessor = new Accessor(name, ns, type, isNullable, false);
                    accessor.IsXmlArrayItem = true;
                    //make a slot for the new nested collection
                    while (nestedCollections.Count <= nestingLevel)
                    {
                        nestedCollections.Add(new AccessorCollection());
                    }
                    nestedCollections[nestingLevel].add(accessor);
                }
                //we have a collection for each nesting level.  attach the
                //collections to the nested one.

                // Build default accessors      
                LogicalType lTypeAtLevel = GetElementType(memberLType);
                for (int i = 0; i < nestedCollections.Count; ++i)
                {
                    AccessorCollection currentLevel = nestedCollections[i];
                    Accessor def = currentLevel.findAccessorNoThrow(lTypeAtLevel.Type);
                    if (def == null)
                    {
                        //array elements are in no particular namespace
                        def = new Accessor(lTypeAtLevel.TypeAccessor.Name, null, lTypeAtLevel, lTypeAtLevel.IsNullable, false);
                        currentLevel.add(def);
                    }
                    currentLevel.Default = def;
                    currentLevel.Default.IsXmlArrayItem = true;
                    foreach (Accessor a in currentLevel)
                    {
                        if (i + 1 < nestedCollections.Count)
                        {
                            a.NestedAccessors = nestedCollections[i + 1];
                        }
                    }
                    //move the type down one level
                    if (SerializationHelper.isLogicalArray(lTypeAtLevel))
                        lTypeAtLevel = GetElementType(lTypeAtLevel);
                    else
                        lTypeAtLevel = TypeContainer.GetIntrinsicType(typeof(object));
                }
                AccessorCollection topLevel = nestedCollections[0];
                foreach (Accessor a in memberAccessors)
                {
                    a.NestedAccessors = topLevel;
                }
            }
        }

        /// <summary>
        /// Reflects over the XmlChoiceIdentifierAttribute
        /// </summary>
        protected XmlChoiceSupport ReflectXmlChoiceIdentifierAttribute(Type memberType, string memberName, LiteralAttributes attrProv, IEntityFinder choiceMemberFinder, string defaultNS)
        {
            XmlChoiceSupport choice = null;
            if (attrProv.XmlChoiceIdentifier != null)
            {
                XmlChoiceIdentifierAttribute xcia = attrProv.XmlChoiceIdentifier;
                string choiceMemberName = xcia.MemberName;

                Type choiceType;
                LogicalMemberValue choiceMember;
                bool foundChoiceMember = choiceMemberFinder.find(choiceMemberName, out choiceMember, out choiceType);
                if (!foundChoiceMember)
                    throw new InvalidOperationException(SR.Format(SR.XmlChoiceIdentiferMemberMissing, choiceMemberName, memberName));

                // choiceType and memberType must agree on array or not -- except in special binary byte[] case.
                bool memberIsArrayLike = memberType.IsArray && memberType != typeof(byte[]);
                if (choiceType.IsArray ^ memberIsArrayLike)
                {
                    if (choiceType.IsArray)
                        throw new InvalidOperationException(SR.Format(SR.XmlChoiceIdentifierType, choiceMemberName, memberName, choiceType.GetElementType().Name));
                    else
                        throw new InvalidOperationException(SR.Format(SR.XmlChoiceIdentifierArrayType, choiceMemberName, memberName, choiceType));
                }
                else if ((choiceType.IsArray && !choiceType.GetElementType().GetTypeInfo().IsEnum) || (!choiceType.IsArray && !choiceType.GetTypeInfo().IsEnum))
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlChoiceIdentifierTypeEnum, choiceMemberName));
                }

                LogicalEnum choiceLType = null;
                LogicalType choiceEnumArrayType = null;
                if (choiceType.IsArray)
                {
                    choiceLType = (LogicalEnum)FindType(choiceType.GetElementType(), false/*encoded*/, defaultNS);
                    choiceEnumArrayType = FindType(choiceType, false/*encoded*/, defaultNS);
                }
                else
                {
                    choiceLType = (LogicalEnum)FindType(choiceType, false/*encoded*/, defaultNS);
                }

                choice = new XmlChoiceSupport(choiceMember, choiceLType, choiceType.IsArray, choiceEnumArrayType);
            }

            return choice;
        }

        /// <summary>
        /// This function reflect attributes that apply to the entire 
        /// MemberValueCollection, rather than mapping to accessors on the specific 
        /// MemberValue.
        /// </summary>
        /// <remarks>
        /// - XmlNamespaceDeclarationsAttribute        
        /// - XmlTextAttribute        
        /// - XmlAnyAttributeAttribute               
        /// - XmlAnyElementAttribute        
        /// </remarks>
        protected SpecialMember ReflectCollectionSpecials(Type memberType, string memberName, LiteralAttributes attrProv,
            string defaultName, string defaultNS,
            Fetcher fetcher, Fixup fixup,
            MemberValueCollection members, IEntityFinder memberFinder,
            out LogicalMemberValue specialMember, ref bool shouldBeOrdered)
        {
            SpecialMember specialType = SpecialMember.None;
            specialMember = null;

            specialType |= ReflectXmlNamespaceDeclarations(memberType, attrProv, fetcher, fixup, members, defaultNS, ref specialMember);
            specialType |= ReflectXmlAnyAttribute(memberType, attrProv, fetcher, fixup, members, defaultNS, ref specialMember);
            specialType |= ReflectXmlAnyElement(memberType, memberName, attrProv, defaultName, defaultNS, fetcher, fixup, members, memberFinder, ref specialMember, ref shouldBeOrdered);
            specialType |= ReflectXmlText(memberType, memberName, attrProv, defaultName, defaultNS, fetcher, fixup, members, ref specialMember);

            return specialType;
        }

        /// <summary>
        /// Reflects over the XmlAnyElementAttribute
        /// </summary>
        protected SpecialMember ReflectXmlAnyElement(Type memberType, string memberName, LiteralAttributes attrProv,
            string defaultName, string defaultNS,
            Fetcher fetcher, Fixup fixup,
            MemberValueCollection members, IEntityFinder memberFinder,
            ref LogicalMemberValue specialMember, ref bool shouldBeOrdered)
        {
            SpecialMember ret = SpecialMember.None;

            if (attrProv.XmlAnyElements != null)
            {
                Debug.Assert(attrProv.XmlAnyElements.Count > 0);

                if (true /* AppDomain.CompatVersion >= AppDomain.Orcas */)
                {
                    LiteralAttributeFlags flags = attrProv.XmlFlags;
                    if ((flags & LiteralAttributes.XmlElementFlags) != flags)
                        throw new InvalidOperationException(SR.XmlIllegalElementsArrayAttribute);
                }

                LogicalType memberLType = FindType(memberType, false/*encoded*/, defaultNS);
                ret = SpecialMember.XmlAnyElement;
                int order = -1;
                bool isArray = SerializationHelper.isLogicalArray(memberLType);
                //if this is applied to an array, make sure its an array of
                //things assignable to XmlNode.  Otherwise, make sure the type
                //is assignable to XmlNode.
                if (isArray)
                {
                    Type eltType = GetElementType(memberLType).Type;
                    if (!typeof(IXmlSerializable).IsAssignableFrom(eltType))
                    {
                        if (!typeof(XmlNode).IsAssignableFrom(eltType) && eltType != typeof(object))
                        {
                            Debug.WriteLine("Wrong array type for XAEA: " + memberType);
                            throw new InvalidOperationException(SR.Format(SR.XmlS_WrongXAEAType_1, memberType));
                        }
                    }
                }
                else
                {
                    if (!typeof(IXmlSerializable).IsAssignableFrom(memberType))
                    {
                        if (!typeof(XmlNode).IsAssignableFrom(memberType) && memberType != typeof(object))
                        {
                            Debug.WriteLine("Wrong element type for XAEA: " + memberType);
                            throw new InvalidOperationException(SR.Format(SR.XmlS_WrongXAEAType_1, memberType));
                        }
                    }
                }

                specialMember = MakeMemberWithDefault(memberLType, fetcher, fixup);
                specialMember.IsXmlAnyElement = true;
                specialMember.IsXmlAnyElementArray = isArray;

                if (attrProv.XmlElements != null)
                {
                    // reflect overt the choice identifier
                    XmlChoiceSupport choice = ReflectXmlChoiceIdentifierAttribute(memberType, memberName, attrProv, memberFinder, defaultNS);
                    if (choice != null) members.XmlAny.addChoiceSupport(choice, specialMember);

                    // reflect over each the XmlElementAttribtues                    
                    foreach (XmlElementAttribute xae in attrProv.XmlElements)
                    {
                        Accessor elementAccessor = ReflectXmlElementOnXmlAny(xae, attrProv, memberType, memberName, defaultName, defaultNS, ref order);
                        members.XmlAny.addElementAccessor(elementAccessor, specialMember);
                    }
                }

                // Initialize the member order to the order id from XmlElementAttributes, if present.
                if (order != -1)
                    specialMember.Order = order;

                // reflect over the XmlAnyElementAttributes
                foreach (XmlAnyElementAttribute xaea in attrProv.XmlAnyElements)
                {
                    string name = null, ns = null;
                    name = xaea.Name;
                    ns = xaea.Namespace != null ? xaea.Namespace : defaultNS;
                    XmlChoiceSupport choice = members.XmlAny.lookupChoiceSupport(specialMember);

                    if (xaea.Order != -1)
                    {
                        if (specialMember.Order != -1 && specialMember.Order != xaea.Order)
                            throw new InvalidOperationException(SR.Format(SR.XmlSequenceMatch, "Order"));
                        specialMember.Order = xaea.Order;
                    }

                    if (name == null || name.Length == 0)
                    {
                        members.XmlAny.setDefault(specialMember, memberName);
                        if (choice != null) members.XmlAny.addAnyMember(s_ANY_ELEMENT_NAME, ns, specialMember, memberName);
                    }
                    else
                    {
                        members.XmlAny.addAnyMember(name, ns, specialMember, memberName);
                    }

                    if (choice != null)
                    {
                        // Create an accessor using the name of the specialized any
                        // or "##any:" if no name was specified.
                        string choiceAccessorName = name == null || name.Length == 0 ? s_ANY_ELEMENT_NAME : name;
                        Accessor anyChoiceAccessor = new Accessor(choiceAccessorName, ns, FindType(typeof(XmlNode), false, null), true/*isNullable*/, false/*isAttribute*/);
                        // Make the accessor as an XmlAnyElement
                        anyChoiceAccessor.IsXmlAnyElement = true;
                        // Add a mapping between the Specialize XmlAny<->Enum Value.
                        choice.addMapping(anyChoiceAccessor);
                    }
                }

                if (shouldBeOrdered && specialMember != null && specialMember.Order == -1)
                    throw new InvalidOperationException(SR.Format(SR.XmlSequenceInconsistent, "Order", memberName));
                if (specialMember != null && specialMember.Order != -1)
                    shouldBeOrdered = true;
            }

            return ret;
        }

        /// <summary>
        /// Reflects over the XmlAnyAttributeAttribute
        /// </summary>
        protected SpecialMember ReflectXmlAnyAttribute(Type memberType,
            LiteralAttributes attrProv,
            Fetcher fetcher, Fixup fixup,
            MemberValueCollection members,
            string defaultNS,
            ref LogicalMemberValue member)
        {
            SpecialMember ret = SpecialMember.None;
            if (attrProv.XmlAnyAttribute != null)
            {
                LiteralAttributeFlags flags = attrProv.XmlFlags;
                if ((flags & LiteralAttributes.XmlAttributeFlags) != flags)
                    throw new InvalidOperationException(SR.XmlIllegalAttributesArrayAttribute);

                LogicalType memberLType = FindType(memberType, false/*encoded*/, defaultNS);
                ret = SpecialMember.XmlAnyAttribute;

                //if this is applied to an array, make sure its an array of
                //things assignable to XmlNode.  Otherwise, make sure the type
                //is assignable to XmlNode.
                if (SerializationHelper.isLogicalArray(memberLType))
                {
                    Type eltType = GetElementType(memberLType).Type;
                    if (!typeof(XmlNode).IsAssignableFrom(eltType))
                    {
                        Debug.WriteLine("Wrong array type for XAAA: " + memberType);
                        throw new InvalidOperationException(SR.Format(SR.XmlS_WrongXAAAType_1, memberType));
                    }
                }
                else
                {
                    if (!typeof(XmlNode).IsAssignableFrom(memberType))
                    {
                        Debug.WriteLine("Wrong element type for XAAA: " + memberType);
                        throw new InvalidOperationException(SR.Format(SR.XmlS_WrongXAAAType_1, memberType));
                    }
                }

                member = MakeMemberWithDefault(memberLType, fetcher, fixup);
                members.XmlAny.XmlAnyAttribute = member;
            }
            return ret;
        }

        /// <summary>
        /// Reflects over the XmlTextAttribute
        /// </summary>
        protected SpecialMember ReflectXmlText(Type memberType, string memberName,
            LiteralAttributes attrProv,
            string defaultName, string defaultNS,
            Fetcher fetcher, Fixup fixup,
            MemberValueCollection members,
            ref LogicalMemberValue splMember)
        {
            SpecialMember ret = SpecialMember.None;
            if (attrProv.XmlText != null)
            {
                LogicalMemberValue member;
                LiteralAttributeFlags flags = attrProv.XmlFlags;
                if ((flags & LiteralAttributes.XmlElementFlags) != flags)
                    throw new InvalidOperationException(SR.XmlIllegalElementsArrayAttribute);

                XmlTextAttribute xta = attrProv.XmlText;
                ret = SpecialMember.XmlText;

                if (memberType == typeof(object[]))
                {
                    // The XmlTextAttribute can be applied to object arrays,
                    // but XmlText.Type must be equal to typeof(string)                    
                    if (xta.Type == null || xta.Type != typeof(string))
                        throw new InvalidOperationException(SR.Format(SR.XmlIllegalTypeContext, memberType.GetElementType().FullName, "text"));

                    AccessorCollection accessors = new AccessorCollection();
                    accessors.Default = new Accessor(null, null, FindType(memberType, false/*encoded*/, defaultNS), false, false);

                    ReflectXmlTextArrayMember(memberType, memberName, attrProv, defaultName, defaultNS, accessors);
                    member = new LogicalMemberValue(accessors, false /*reqiured*/, true/*canRead*/, true/*canWrite*/ );
                    member.Fetcher = fetcher;
                    member.Fixup = fixup;
                }
                else if (memberType == typeof(string[]) || memberType == typeof(XmlNode[]))
                {
                    // The XmlTextAttribute can be applied to string arrays or XmlNode[]
                    AccessorCollection accessors = new AccessorCollection();
                    accessors.Default = new Accessor(null, null, FindType(memberType, false/*encoded*/, defaultNS), false, false);

                    ReflectXmlTextArrayMember(memberType, memberName, attrProv, defaultName, defaultNS, accessors);
                    member = new LogicalMemberValue(accessors, false /*reqiured*/, true/*canRead*/, true/*canWrite*/ );
                    member.Fetcher = fetcher;
                    member.Fixup = fixup;
                }
                else if (memberType == typeof(byte[]) || !memberType.IsArray)
                {
                    if (!memberType.IsArray && !memberType.GetTypeInfo().IsEnum && memberType != typeof(XmlQualifiedName) &&
                        !typeof(XmlNode).IsAssignableFrom(memberType) && !SerializationHelper.IsSerializationPrimitive(memberType))
                        throw new InvalidOperationException(SR.Format(SR.XmlIllegalAttrOrText, memberName, memberType.FullName));

                    LogicalType textType = null;
                    if (xta.DataType.Length > 0)
                        textType = FindType(xta.DataType, false/*encoded*/ );
                    else if (xta.Type != null)
                        textType = FindType(xta.Type, false/*encoded*/, defaultNS);
                    else
                        textType = FindType(memberType, false/*encoded*/, defaultNS);

                    member = MakeMemberWithDefault(textType, fetcher, fixup);
                }
                else
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlIllegalArrayTextAttribute, defaultName));
                }
                if (splMember != null)
                    member.IsXmlAnyElement = splMember.IsXmlAnyElement;
                member.IsXmlText = true;
                members.XmlText = member;
            }
            return ret;
        }

        /// <summary>
        /// Reflects over the XmlNamespaceDeclarationAttribute
        /// </summary>
        protected SpecialMember ReflectXmlNamespaceDeclarations(Type memberType,
            LiteralAttributes attrProv,
            Fetcher fetcher, Fixup fixup,
            MemberValueCollection members,
            string defaultNS,
            ref LogicalMemberValue member)
        {
            SpecialMember ret = SpecialMember.None;
            if (attrProv.XmlNamespaceDeclaration != null)
            {
                //only applicable to something of type XmlSerializerNamespaces.
                if (!memberType.Equals(typeof(XmlSerializerNamespaces)))
                {
                    Debug.WriteLine("wrong place for an XNDA");
                    throw new InvalidOperationException(SR.XmlS_XNDANotXSN_1);
                }

                member = MakeMemberWithDefault(FindType(memberType, false/*encoded*/, defaultNS), fetcher, fixup);
                members.XmlNamespaceDecls = member;
                ret = SpecialMember.Xmlns;
            }
            return ret;
        }

        /// <summary>
        /// Reflects over the XmlElementAttribute that appears on a member that is also
        /// decorated with an XmlAnyAttribute.
        /// </summary>
        protected Accessor ReflectXmlElementOnXmlAny(XmlElementAttribute xea, LiteralAttributes attrProv, Type memberType, string memberName, string defaultName, string defaultNS, ref int order)
        {
            if (xea == null) return null;
            string name, ns;
            LogicalType type;

            ns = (xea.Form == XmlSchemaForm.Unqualified) ?     // If attribute is unqualified
                string.Empty :                                  //      return null
                (xea.Namespace != null) ?                       // elseif the Namespace in the attribute is not null
                xea.Namespace :                                 //      use the namespace
                defaultNS;                                      // else
            //      use the default namespace;

            if (xea.Order != -1)
            {
                if (order != -1 && xea.Order != order)
                    throw new InvalidOperationException(SR.Format(SR.XmlSequenceMatch, "Order"));
                order = xea.Order;
            }

            bool isArray;
            LogicalType elementType;
            ResolveLiteralType(xea.DataType, xea.Type, memberType, ns, out isArray, out type, out elementType);
            if (attrProv.XmlElements.Count > 1 && !SerializationHelper.isLogicalArray(type) && attrProv.XmlChoiceIdentifier == null)
            {
                throw new InvalidOperationException(SR.Format(SR.XmlChoiceIdentiferMissing, typeof(XmlChoiceIdentifierAttribute).Name, memberName));
            }

            if (xea.ElementName != null && xea.ElementName.Length > 0)
            {
                name = xea.ElementName;
            }
            else
            {
                if (attrProv.XmlChoiceIdentifier != null)
                {
                    if (SerializationHelper.isLogicalArray(type))
                        name = elementType.Type == typeof(object) ? ANY_TYPE_STR : elementType.TypeAccessor.Name;
                    else
                        name = type.Type == typeof(object) ? ANY_TYPE_STR : type.TypeAccessor.Name;
                }
                else
                {
                    if (attrProv.XmlElements.Count > 1)
                        name = elementType.TypeAccessor.Name;
                    else
                        name = defaultName;
                }
            }

            // make the element accessor for a repeated element array            
            Accessor rElement;
            if (memberType.IsArray)
            {
                Debug.Assert(elementType != null, "Null element type");
                rElement = new Accessor(name, ns, elementType, xea.IsNullable, false);
                rElement.IsXmlAnyArrayElement = true;
                rElement.AllowRepeats = false;
            }
            else
            {
                rElement = new Accessor(name, ns, type, xea.IsNullable, false);
            }

            return rElement;
        }

        /// <summary>
        /// Reflects over an array member that is marked with the XmlTextAttribute.
        /// </summary>
        /// <example>
        ///  [XmlText()]
        ///  string[] textStrings;        
        /// </example>
        protected void ReflectXmlTextArrayMember(Type memberType, string memberName, LiteralAttributes attrProv, string defaultName, string defaultNS, AccessorCollection accessors)
        {
            if (attrProv.XmlElements != null)
            {
                Debug.Assert(attrProv.XmlElements.Count > 0);
                int i = -1;
                foreach (XmlElementAttribute xea in attrProv.XmlElements)
                {
                    ++i;
                    string name;
                    string ns;
                    LogicalType type;

                    ns = (xea.Form == XmlSchemaForm.Unqualified) ?     // If attribute is unqualified
                        string.Empty :                                  //      return null
                        (xea.Namespace != null) ?                       // elseif the Namespace in the attribute is not null
                        xea.Namespace :                                 //      use the namespace
                        defaultNS;                                      // else
                    //      use the default namespace;

                    bool isArray;
                    LogicalType elementType;
                    ResolveLiteralType(xea.DataType, xea.Type, memberType, ns, out isArray, out type, out elementType);

                    if (xea.ElementName != null && xea.ElementName.Length > 0)
                    {
                        name = xea.ElementName;
                    }
                    else
                    {
                        if (attrProv.XmlElements.Count > 1)
                            name = elementType.TypeAccessor.Name;
                        else
                            name = defaultName;
                    }

                    // make the element accessor for a repeated element array
                    Debug.Assert(elementType != null, "Null element type");
                    Accessor rElement = new Accessor(name, ns, elementType, xea.IsNullable, false);
                    rElement.IsXmlTextArrayElement = true;
                    rElement.AllowRepeats = true;
                    accessors.add(rElement);
                }
            }
        }

        /// <summary>
        /// Reflects over the DefaultValueAttribute
        /// </summary>
        protected object ReflectDefaultValueAttribute(Type memberType, SharedAttributes attrProv)
        {
            object defaultValue = null;
            if (attrProv.DefaultValue != null && attrProv.DefaultValue.Value != null)
            {
                defaultValue = attrProv.DefaultValue.Value;

                if (!memberType.IsAssignableFrom(defaultValue.GetType()))
                {
                    if (memberType.GetTypeInfo().IsEnum && defaultValue.GetType().GetTypeInfo().IsPrimitive)
                    {
                        defaultValue = Enum.ToObject(memberType, defaultValue);
                    }
                    else if (defaultValue.GetType().GetTypeInfo().IsPrimitive && memberType.GetTypeInfo().IsPrimitive)
                    {
                        defaultValue = Convert.ChangeType(defaultValue, memberType, null);
                    }
                }

                if (memberType.GetTypeInfo().IsEnum && !memberType.GetTypeInfo().GetCustomAttributes(typeof(FlagsAttribute), false).Any())
                {
                    if (!Enum.IsDefined(memberType, defaultValue))
                        throw new InvalidOperationException(SR.Format(SR.XmlInvalidDefaultValue, defaultValue, memberType));
                }
            }

            return defaultValue;
        }

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
        // Static Helper Methods
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//

        private static bool IgnoreLiteralMember(LiteralAttributes attrProv)
        {
            if (attrProv.Ignore)
                return true;

            // The following attributes indicate that we are to "ignore"
            // them. They are the "Special" attributes             
            if (attrProv.XmlText != null)
                return true;

            // We still need to reflect XmlChoiceIdentifier and 
            // XmlElement even XmlAnyElements is present
            if (attrProv.XmlAnyElements != null)
                return true;

            if (attrProv.XmlAnyAttribute != null)
                return true;

            if (attrProv.XmlNamespaceDeclaration != null)
                return true;

            return false;
        }

        private static void CreateNameAndNamespaceFromQnameString(string qName, out string name, out string ns)
        {
            Debug.Assert(qName != null);

            name = null;
            ns = null;

            string[] names = qName.Split(new char[] { ':' });
            if (names.Length > 1)
            {
                name = names[1];
                ns = names[0];
            }
            else
            {
                name = names[0];
            }
        }

        private static LogicalMemberValue FindSpecifiedMember(string memberName, IEntityFinder memberFinder)
        {
            LogicalMemberValue specifiedMember;
            Type specifiedType;

            bool success = memberFinder.find(memberName + "Specified", out specifiedMember, out specifiedType);

            if (success && specifiedType.Equals(typeof(bool)))
                return specifiedMember;
            return null;
        }

        internal static LogicalType GetElementType(LogicalType type)
        {
            Debug.Assert(SerializationHelper.isLogicalArray(type), "not an array");
            return type.TypeAccessor.NestedAccessors.Default.Type;
        }

        private static void CheckNullable(bool requestNullable, Type type)
        {
            Debug.Assert(type != null, "Type unavailable.");
            if (requestNullable && !(IsIXmlSerializable(type) || IsNullableOrReferenceType(type)))
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidIsNullable, type.FullName));
        }

        private static bool IsNullableOrReferenceType(Type type)
        {
            Debug.Assert(type != null);
            bool isReferenceType = !type.GetTypeInfo().IsValueType;
            return isReferenceType || IsNullableType(type);
        }

        private static bool IsIXmlSerializable(Type type)
        {
            Debug.Assert(type != null);
            return typeof(IXmlSerializable).IsAssignableFrom(type);
        }
    }
}
