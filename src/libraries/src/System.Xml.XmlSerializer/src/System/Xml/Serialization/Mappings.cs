// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System.Reflection;
    using System.Collections;
    using System.Xml.Schema;
    using System;
    using System.Text;
    using System.ComponentModel;
    using System.Xml;
    using System.CodeDom.Compiler;
    using IComparer = System.Collections.Generic.IComparer<object>;

    // These classes represent a mapping between classes and a particular XML format.
    // There are two class of mapping information: accessors (such as elements and
    // attributes), and mappings (which specify the type of an accessor).

    internal abstract class Accessor
    {
        private string _name;
        private object _defaultValue = null;
        private string _ns;
        private TypeMapping _mapping;
        private bool _any;
        private string _anyNs;
        private bool _topLevelInSchema;
        private XmlSchemaForm _form = XmlSchemaForm.None;

        internal Accessor() { }

        internal TypeMapping Mapping
        {
            get { return _mapping; }
            set { _mapping = value; }
        }

        internal object Default
        {
            get { return _defaultValue; }
            set { _defaultValue = value; }
        }


        internal virtual string Name
        {
            get { return _name == null ? string.Empty : _name; }
            set { _name = value; }
        }

        internal bool Any
        {
            get { return _any; }
            set { _any = value; }
        }

        internal string AnyNamespaces
        {
            get { return _anyNs; }
            set { _anyNs = value; }
        }

        internal string Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }

        internal XmlSchemaForm Form
        {
            get { return _form; }
            set { _form = value; }
        }


        internal bool IsTopLevelInSchema
        {
            get { return _topLevelInSchema; }
            set { _topLevelInSchema = value; }
        }


        internal static string EscapeQName(string name)
        {
            if (name == null || name.Length == 0) return name;
            int colon = name.LastIndexOf(':');
            if (colon < 0)
                return XmlConvert.EncodeLocalName(name);
            else
            {
                if (colon == 0 || colon == name.Length - 1)
                    throw new ArgumentException(SR.Format(SR.Xml_InvalidNameChars, name), nameof(name));
                return new XmlQualifiedName(XmlConvert.EncodeLocalName(name.Substring(colon + 1)), XmlConvert.EncodeLocalName(name.Substring(0, colon))).ToString();
            }
        }

        internal static string UnescapeName(string name)
        {
            return XmlConvert.DecodeName(name);
        }
    }

    internal class ElementAccessor : Accessor
    {
        private bool _nullable;
        private bool _unbounded = false;

        internal bool IsSoap
        {
            get { return false; }
        }

        internal bool IsNullable
        {
            get { return _nullable; }
            set { _nullable = value; }
        }

        internal bool IsUnbounded
        {
            get { return _unbounded; }
            set { _unbounded = value; }
        }

        internal ElementAccessor Clone()
        {
            ElementAccessor newAccessor = new ElementAccessor();
            newAccessor._nullable = _nullable;
            newAccessor.IsTopLevelInSchema = this.IsTopLevelInSchema;
            newAccessor.Form = this.Form;
            newAccessor.Name = this.Name;
            newAccessor.Default = this.Default;
            newAccessor.Namespace = this.Namespace;
            newAccessor.Mapping = this.Mapping;
            newAccessor.Any = this.Any;

            return newAccessor;
        }
    }

    internal class ChoiceIdentifierAccessor : Accessor
    {
        private string _memberName;
        private string[] _memberIds;
        private MemberInfo _memberInfo;

        internal string MemberName
        {
            get { return _memberName; }
            set { _memberName = value; }
        }

        internal string[] MemberIds
        {
            get { return _memberIds; }
            set { _memberIds = value; }
        }

        internal MemberInfo MemberInfo
        {
            get { return _memberInfo; }
            set { _memberInfo = value; }
        }
    }

    internal class TextAccessor : Accessor
    {
    }

    internal class XmlnsAccessor : Accessor
    {
    }

    internal class AttributeAccessor : Accessor
    {
        private bool _isSpecial;
        private bool _isList;

        internal bool IsSpecialXmlNamespace
        {
            get { return _isSpecial; }
        }

        internal bool IsList
        {
            get { return _isList; }
            set { _isList = value; }
        }

        internal void CheckSpecial()
        {
            int colon = Name.LastIndexOf(':');

            if (colon >= 0)
            {
                if (!Name.StartsWith("xml:", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(SR.Format(SR.Xml_InvalidNameChars, Name));
                }
                Name = Name.Substring("xml:".Length);
                Namespace = XmlReservedNs.NsXml;
                _isSpecial = true;
            }
            else
            {
                if (Namespace == XmlReservedNs.NsXml)
                {
                    _isSpecial = true;
                }
                else
                {
                    _isSpecial = false;
                }
            }
            if (_isSpecial)
            {
                Form = XmlSchemaForm.Qualified;
            }
        }
    }

    internal abstract class Mapping
    {
        internal Mapping() { }

        protected Mapping(Mapping mapping)
        {
        }

        internal bool IsSoap
        {
            get { return false; }
        }
    }

    internal abstract class TypeMapping : Mapping
    {
        private TypeDesc _typeDesc;
        private string _typeNs;
        private string _typeName;
        private bool _includeInSchema = true;

        internal string Namespace
        {
            get { return _typeNs; }
            set { _typeNs = value; }
        }

        internal string TypeName
        {
            get { return _typeName; }
            set { _typeName = value; }
        }

        internal TypeDesc TypeDesc
        {
            get { return _typeDesc; }
            set { _typeDesc = value; }
        }

        internal bool IncludeInSchema
        {
            get { return _includeInSchema; }
            set { _includeInSchema = value; }
        }

        internal virtual bool IsList
        {
            get { return false; }
            set { }
        }


        internal bool IsAnonymousType
        {
            get { return _typeName == null || _typeName.Length == 0; }
        }

        internal virtual string DefaultElementName
        {
            get { return IsAnonymousType ? XmlConvert.EncodeLocalName(_typeDesc.Name) : _typeName; }
        }
    }

    internal class PrimitiveMapping : TypeMapping
    {
        private bool _isList;

        internal override bool IsList
        {
            get { return _isList; }
            set { _isList = value; }
        }
    }

    internal class NullableMapping : TypeMapping
    {
        private TypeMapping _baseMapping;

        internal TypeMapping BaseMapping
        {
            get { return _baseMapping; }
            set { _baseMapping = value; }
        }

        internal override string DefaultElementName
        {
            get { return BaseMapping.DefaultElementName; }
        }
    }

    internal class ArrayMapping : TypeMapping
    {
        private ElementAccessor[] _elements;
        private ElementAccessor[] _sortedElements;
        private ArrayMapping _next;

        internal ElementAccessor[] Elements
        {
            get { return _elements; }
            set { _elements = value; _sortedElements = null; }
        }

        internal ElementAccessor[] ElementsSortedByDerivation
        {
            get
            {
                if (_sortedElements != null)
                    return _sortedElements;
                if (_elements == null)
                    return null;
                _sortedElements = new ElementAccessor[_elements.Length];
                Array.Copy(_elements, 0, _sortedElements, 0, _elements.Length);
                AccessorMapping.SortMostToLeastDerived(_sortedElements);
                return _sortedElements;
            }
        }


        internal ArrayMapping Next
        {
            get { return _next; }
            set { _next = value; }
        }
    }

    internal class EnumMapping : PrimitiveMapping
    {
        private ConstantMapping[] _constants;
        private bool _isFlags;

        internal bool IsFlags
        {
            get { return _isFlags; }
            set { _isFlags = value; }
        }

        internal ConstantMapping[] Constants
        {
            get { return _constants; }
            set { _constants = value; }
        }
    }

    internal class ConstantMapping : Mapping
    {
        private string _xmlName;
        private string _name;
        private long _value;

        internal string XmlName
        {
            get { return _xmlName == null ? string.Empty : _xmlName; }
            set { _xmlName = value; }
        }

        internal string Name
        {
            get { return _name == null ? string.Empty : _name; }
            set { _name = value; }
        }

        internal long Value
        {
            get { return _value; }
            set { _value = value; }
        }
    }

    internal class StructMapping : TypeMapping, INameScope
    {
        private MemberMapping[] _members;
        private StructMapping _baseMapping;
        private StructMapping _derivedMappings;
        private StructMapping _nextDerivedMapping;
        private MemberMapping _xmlnsMember = null;
        private bool _hasSimpleContent;
        private bool _isSequence;
        private NameTable _elements;
        private NameTable _attributes;

        internal StructMapping BaseMapping
        {
            get { return _baseMapping; }
            set
            {
                _baseMapping = value;
                if (!IsAnonymousType && _baseMapping != null)
                {
                    _nextDerivedMapping = _baseMapping._derivedMappings;
                    _baseMapping._derivedMappings = this;
                }
                if (value._isSequence && !_isSequence)
                {
                    _isSequence = true;
                    if (_baseMapping.IsSequence)
                    {
                        for (StructMapping derived = _derivedMappings; derived != null; derived = derived.NextDerivedMapping)
                        {
                            derived.SetSequence();
                        }
                    }
                }
            }
        }

        internal StructMapping DerivedMappings
        {
            get { return _derivedMappings; }
        }

        internal bool IsFullyInitialized
        {
            get { return _baseMapping != null && Members != null; }
        }

        internal NameTable LocalElements
        {
            get
            {
                if (_elements == null)
                    _elements = new NameTable();
                return _elements;
            }
        }
        internal NameTable LocalAttributes
        {
            get
            {
                if (_attributes == null)
                    _attributes = new NameTable();
                return _attributes;
            }
        }
        object INameScope.this[string name, string ns]
        {
            get
            {
                object named = LocalElements[name, ns];
                if (named != null)
                    return named;
                if (_baseMapping != null)
                    return ((INameScope)_baseMapping)[name, ns];
                return null;
            }
            set
            {
                LocalElements[name, ns] = value;
            }
        }
        internal StructMapping NextDerivedMapping
        {
            get { return _nextDerivedMapping; }
        }

        internal bool HasSimpleContent
        {
            get { return _hasSimpleContent; }
        }

        internal bool HasXmlnsMember
        {
            get
            {
                StructMapping mapping = this;
                while (mapping != null)
                {
                    if (mapping.XmlnsMember != null)
                        return true;
                    mapping = mapping.BaseMapping;
                }
                return false;
            }
        }

        internal MemberMapping[] Members
        {
            get { return _members; }
            set { _members = value; }
        }

        internal MemberMapping XmlnsMember
        {
            get { return _xmlnsMember; }
            set { _xmlnsMember = value; }
        }



        internal MemberMapping FindDeclaringMapping(MemberMapping member, out StructMapping declaringMapping, string parent)
        {
            declaringMapping = null;
            if (BaseMapping != null)
            {
                MemberMapping baseMember = BaseMapping.FindDeclaringMapping(member, out declaringMapping, parent);
                if (baseMember != null) return baseMember;
            }
            if (_members == null) return null;

            for (int i = 0; i < _members.Length; i++)
            {
                if (_members[i].Name == member.Name)
                {
                    if (_members[i].TypeDesc != member.TypeDesc)
                        throw new InvalidOperationException(SR.Format(SR.XmlHiddenMember, parent, member.Name, member.TypeDesc.FullName, this.TypeName, _members[i].Name, _members[i].TypeDesc.FullName));
                    else if (!_members[i].Match(member))
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlInvalidXmlOverride, parent, member.Name, this.TypeName, _members[i].Name));
                    }
                    declaringMapping = this;
                    return _members[i];
                }
            }
            return null;
        }
        internal bool Declares(MemberMapping member, string parent)
        {
            StructMapping m;
            return (FindDeclaringMapping(member, out m, parent) != null);
        }

        internal void SetContentModel(TextAccessor text, bool hasElements)
        {
            if (BaseMapping == null || BaseMapping.TypeDesc.IsRoot)
            {
                _hasSimpleContent = !hasElements && text != null && !text.Mapping.IsList;
            }
            else if (BaseMapping.HasSimpleContent)
            {
                if (text != null || hasElements)
                {
                    // we can only extent a simleContent type with attributes
                    throw new InvalidOperationException(SR.Format(SR.XmlIllegalSimpleContentExtension, TypeDesc.FullName, BaseMapping.TypeDesc.FullName));
                }
                else
                {
                    _hasSimpleContent = true;
                }
            }
            else
            {
                _hasSimpleContent = false;
            }
            if (!_hasSimpleContent && text != null && !text.Mapping.TypeDesc.CanBeTextValue)
            {
                throw new InvalidOperationException(SR.Format(SR.XmlIllegalTypedTextAttribute, TypeDesc.FullName, text.Name, text.Mapping.TypeDesc.FullName));
            }
        }


        internal bool HasExplicitSequence()
        {
            if (_members != null)
            {
                for (int i = 0; i < _members.Length; i++)
                {
                    if (_members[i].IsParticle && _members[i].IsSequence)
                    {
                        return true;
                    }
                }
            }
            return (_baseMapping != null && _baseMapping.HasExplicitSequence());
        }

        internal void SetSequence()
        {
            if (TypeDesc.IsRoot)
                return;

            StructMapping start = this;

            // find first mapping that does not have the sequence set
            while (start.BaseMapping != null && !start.BaseMapping.IsSequence && !start.BaseMapping.TypeDesc.IsRoot)
                start = start.BaseMapping;

            start.IsSequence = true;
            for (StructMapping derived = start.DerivedMappings; derived != null; derived = derived.NextDerivedMapping)
            {
                derived.SetSequence();
            }
        }

        internal bool IsSequence
        {
            get { return _isSequence && !TypeDesc.IsRoot; }
            set { _isSequence = value; }
        }
    }

    internal abstract class AccessorMapping : Mapping
    {
        private TypeDesc _typeDesc;
        private AttributeAccessor _attribute;
        private ElementAccessor[] _elements;
        private ElementAccessor[] _sortedElements;
        private TextAccessor _text;
        private ChoiceIdentifierAccessor _choiceIdentifier;
        private XmlnsAccessor _xmlns;
        private bool _ignore;

        internal AccessorMapping()
        { }

        protected AccessorMapping(AccessorMapping mapping)
            : base(mapping)
        {
            _typeDesc = mapping._typeDesc;
            _attribute = mapping._attribute;
            _elements = mapping._elements;
            _sortedElements = mapping._sortedElements;
            _text = mapping._text;
            _choiceIdentifier = mapping._choiceIdentifier;
            _xmlns = mapping._xmlns;
            _ignore = mapping._ignore;
        }

        internal bool IsAttribute
        {
            get { return _attribute != null; }
        }

        internal bool IsText
        {
            get { return _text != null && (_elements == null || _elements.Length == 0); }
        }

        internal bool IsParticle
        {
            get { return (_elements != null && _elements.Length > 0); }
        }

        internal TypeDesc TypeDesc
        {
            get { return _typeDesc; }
            set { _typeDesc = value; }
        }

        internal AttributeAccessor Attribute
        {
            get { return _attribute; }
            set { _attribute = value; }
        }

        internal ElementAccessor[] Elements
        {
            get { return _elements; }
            set { _elements = value; _sortedElements = null; }
        }

        internal static void SortMostToLeastDerived(ElementAccessor[] elements)
        {
            Array.Sort(elements, new AccessorComparer());
        }

        internal class AccessorComparer : IComparer
        {
            public int Compare(object o1, object o2)
            {
                if (o1 == o2)
                    return 0;
                Accessor a1 = (Accessor)o1;
                Accessor a2 = (Accessor)o2;
                int w1 = a1.Mapping.TypeDesc.Weight;
                int w2 = a2.Mapping.TypeDesc.Weight;
                if (w1 == w2)
                    return 0;
                if (w1 < w2)
                    return 1;
                return -1;
            }
        }

        internal ElementAccessor[] ElementsSortedByDerivation
        {
            get
            {
                if (_sortedElements != null)
                    return _sortedElements;
                if (_elements == null)
                    return null;
                _sortedElements = new ElementAccessor[_elements.Length];
                Array.Copy(_elements, 0, _sortedElements, 0, _elements.Length);
                SortMostToLeastDerived(_sortedElements);
                return _sortedElements;
            }
        }

        internal TextAccessor Text
        {
            get { return _text; }
            set { _text = value; }
        }

        internal ChoiceIdentifierAccessor ChoiceIdentifier
        {
            get { return _choiceIdentifier; }
            set { _choiceIdentifier = value; }
        }

        internal XmlnsAccessor Xmlns
        {
            get { return _xmlns; }
            set { _xmlns = value; }
        }

        internal bool Ignore
        {
            get { return _ignore; }
            set { _ignore = value; }
        }

        internal Accessor Accessor
        {
            get
            {
                if (_xmlns != null) return _xmlns;
                if (_attribute != null) return _attribute;
                if (_elements != null && _elements.Length > 0) return _elements[0];
                return _text;
            }
        }



        internal static bool ElementsMatch(ElementAccessor[] a, ElementAccessor[] b)
        {
            if (a == null)
            {
                if (b == null)
                    return true;
                return false;
            }
            if (b == null)
                return false;
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].Name != b[i].Name || a[i].Namespace != b[i].Namespace || a[i].Form != b[i].Form || a[i].IsNullable != b[i].IsNullable)
                    return false;
            }
            return true;
        }

        internal bool Match(AccessorMapping mapping)
        {
            if (Elements != null && Elements.Length > 0)
            {
                if (!ElementsMatch(Elements, mapping.Elements))
                {
                    return false;
                }
                if (Text == null)
                {
                    return (mapping.Text == null);
                }
            }
            if (Attribute != null)
            {
                if (mapping.Attribute == null)
                    return false;
                return (Attribute.Name == mapping.Attribute.Name && Attribute.Namespace == mapping.Attribute.Namespace && Attribute.Form == mapping.Attribute.Form);
            }
            if (Text != null)
            {
                return (mapping.Text != null);
            }
            return (mapping.Accessor == null);
        }
    }

    internal class MemberMappingComparer : IComparer
    {
        public int Compare(object o1, object o2)
        {
            MemberMapping m1 = (MemberMapping)o1;
            MemberMapping m2 = (MemberMapping)o2;

            bool m1Text = m1.IsText;
            if (m1Text)
            {
                if (m2.IsText)
                    return 0;
                return 1;
            }
            else if (m2.IsText)
                return -1;

            if (m1.SequenceId < 0 && m2.SequenceId < 0)
                return 0;
            if (m1.SequenceId < 0)
                return 1;
            if (m2.SequenceId < 0)
                return -1;
            if (m1.SequenceId < m2.SequenceId)
                return -1;
            if (m1.SequenceId > m2.SequenceId)
                return 1;
            return 0;
        }
    }

    internal class MemberMapping : AccessorMapping
    {
        private string _name;
        private bool _checkShouldPersist;
        private SpecifiedAccessor _checkSpecified;
        private bool _isReturnValue;
        private bool _readOnly = false;
        private int _sequenceId = -1;
        private MemberInfo _memberInfo;
        private MemberInfo _checkSpecifiedMemberInfo;
        private MethodInfo _checkShouldPersistMethodInfo;

        internal MemberMapping() { }

        private MemberMapping(MemberMapping mapping)
            : base(mapping)
        {
            _name = mapping._name;
            _checkShouldPersist = mapping._checkShouldPersist;
            _checkSpecified = mapping._checkSpecified;
            _isReturnValue = mapping._isReturnValue;
            _readOnly = mapping._readOnly;
            _sequenceId = mapping._sequenceId;
            _memberInfo = mapping._memberInfo;
            _checkSpecifiedMemberInfo = mapping._checkSpecifiedMemberInfo;
            _checkShouldPersistMethodInfo = mapping._checkShouldPersistMethodInfo;
        }

        internal bool CheckShouldPersist
        {
            get { return _checkShouldPersist; }
            set { _checkShouldPersist = value; }
        }

        internal SpecifiedAccessor CheckSpecified
        {
            get { return _checkSpecified; }
            set { _checkSpecified = value; }
        }

        internal string Name
        {
            get { return _name == null ? string.Empty : _name; }
            set { _name = value; }
        }

        internal MemberInfo MemberInfo
        {
            get { return _memberInfo; }
            set { _memberInfo = value; }
        }
        internal MemberInfo CheckSpecifiedMemberInfo
        {
            get { return _checkSpecifiedMemberInfo; }
            set { _checkSpecifiedMemberInfo = value; }
        }
        internal MethodInfo CheckShouldPersistMethodInfo
        {
            get { return _checkShouldPersistMethodInfo; }
            set { _checkShouldPersistMethodInfo = value; }
        }
        internal bool IsReturnValue
        {
            get { return _isReturnValue; }
            set { _isReturnValue = value; }
        }

        internal bool ReadOnly
        {
            get { return _readOnly; }
            set { _readOnly = value; }
        }

        internal bool IsSequence
        {
            get { return _sequenceId >= 0; }
        }

        internal int SequenceId
        {
            get { return _sequenceId; }
            set { _sequenceId = value; }
        }

        private string GetNullableType(TypeDesc td)
        {
            // SOAP encoded arrays not mapped to Nullable<T> since they always derive from soapenc:Array
            if (td.IsMappedType || (!td.IsValueType && (Elements[0].IsSoap || td.ArrayElementTypeDesc == null)))
                return td.FullName;
            if (td.ArrayElementTypeDesc != null)
            {
                return GetNullableType(td.ArrayElementTypeDesc) + "[]";
            }
            return "System.Nullable`1[" + td.FullName + "]";
        }

        internal MemberMapping Clone()
        {
            return new MemberMapping(this);
        }
    }

    internal class MembersMapping : TypeMapping
    {
        private MemberMapping[] _members;
        private bool _hasWrapperElement = true;
        private bool _writeAccessors = true;
        private MemberMapping _xmlnsMember = null;

        internal MemberMapping[] Members
        {
            get { return _members; }
            set { _members = value; }
        }

        internal MemberMapping XmlnsMember
        {
            get { return _xmlnsMember; }
            set { _xmlnsMember = value; }
        }

        internal bool HasWrapperElement
        {
            get { return _hasWrapperElement; }
            set { _hasWrapperElement = value; }
        }


        internal bool WriteAccessors
        {
            get { return _writeAccessors; }
        }
    }

    internal class SpecialMapping : TypeMapping
    {
    }

    internal class SerializableMapping : SpecialMapping
    {
        private Type _type;
        private bool _needSchema = true;

        // new implementation of the IXmlSerializable
        private MethodInfo _getSchemaMethod;
        private XmlQualifiedName _xsiType;
        private XmlSchemaSet _schemas;
        private bool _any;

        private SerializableMapping _derivedMappings;
        private SerializableMapping _nextDerivedMapping;

        internal SerializableMapping() { }
        internal SerializableMapping(MethodInfo getSchemaMethod, bool any, string ns)
        {
            _getSchemaMethod = getSchemaMethod;
            _any = any;
            this.Namespace = ns;
            _needSchema = getSchemaMethod != null;
        }


        internal bool IsAny
        {
            get
            {
                if (_any)
                    return true;
                if (_getSchemaMethod == null)
                    return false;
                if (_needSchema && typeof(XmlSchemaType).IsAssignableFrom(_getSchemaMethod.ReturnType))
                    return false;
                RetrieveSerializableSchema();
                return _any;
            }
        }


        internal SerializableMapping DerivedMappings
        {
            get
            {
                return _derivedMappings;
            }
        }

        internal SerializableMapping NextDerivedMapping
        {
            get
            {
                return _nextDerivedMapping;
            }
        }


        internal Type Type
        {
            get { return _type; }
            set { _type = value; }
        }


        internal XmlQualifiedName XsiType
        {
            get
            {
                if (!_needSchema)
                    return _xsiType;
                if (_getSchemaMethod == null)
                    return null;
                if (typeof(XmlSchemaType).IsAssignableFrom(_getSchemaMethod.ReturnType))
                    return null;
                RetrieveSerializableSchema();
                return _xsiType;
            }
        }



        private void RetrieveSerializableSchema()
        {
            if (_needSchema)
            {
                _needSchema = false;
                if (_getSchemaMethod != null)
                {
                    // get the type info
                    object typeInfo = _getSchemaMethod.Invoke(null, new object[] { _schemas });
                    _xsiType = XmlQualifiedName.Empty;

                    if (typeInfo != null)
                    {
                        if (typeof(XmlSchemaType).IsAssignableFrom(_getSchemaMethod.ReturnType))
                        {
                            throw Globals.NotSupported("No XmlSchemaType in SL");
                        }
                        else if (typeof(XmlQualifiedName).IsAssignableFrom(_getSchemaMethod.ReturnType))
                        {
                            _xsiType = (XmlQualifiedName)typeInfo;
                            if (_xsiType.IsEmpty)
                            {
                                throw new InvalidOperationException(SR.Format(SR.XmlGetSchemaEmptyTypeName, _type.FullName, _getSchemaMethod.Name));
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException(SR.Format(SR.XmlGetSchemaMethodReturnType, _type.Name, _getSchemaMethod.Name, typeof(XmlSchemaProviderAttribute).Name, typeof(XmlQualifiedName).FullName));
                        }
                    }
                    else
                    {
                        _any = true;
                    }
                }
            }
        }
    }
}

