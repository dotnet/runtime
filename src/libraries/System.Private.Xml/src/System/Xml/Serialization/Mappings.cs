// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Serialization
{
    using System.Reflection;
    using System.Collections;
    using System.Xml.Schema;
    using System;
    using System.Text;
    using System.ComponentModel;
    using System.Xml;
    using System.Xml.Serialization;
    using System.Diagnostics.CodeAnalysis;

    // These classes represent a mapping between classes and a particular XML format.
    // There are two class of mapping information: accessors (such as elements and
    // attributes), and mappings (which specify the type of an accessor).

    internal abstract class Accessor
    {
        private string? _name;
        private object? _defaultValue;
        private string? _ns;
        private TypeMapping? _mapping;
        private bool _any;
        private string? _anyNs;
        private bool _topLevelInSchema;
        private bool _isFixed;
        private bool _isOptional;
        private XmlSchemaForm _form = XmlSchemaForm.None;

        internal Accessor() { }

        internal TypeMapping? Mapping
        {
            get { return _mapping; }
            set { _mapping = value; }
        }

        internal object? Default
        {
            get { return _defaultValue; }
            set { _defaultValue = value; }
        }

        internal bool HasDefault
        {
            get { return _defaultValue != null && _defaultValue != DBNull.Value; }
        }

        [AllowNull]
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

        internal string? AnyNamespaces
        {
            get { return _anyNs; }
            set { _anyNs = value; }
        }

        internal string? Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }

        internal XmlSchemaForm Form
        {
            get { return _form; }
            set { _form = value; }
        }

        internal bool IsFixed
        {
            get { return _isFixed; }
            set { _isFixed = value; }
        }

        internal bool IsOptional
        {
            get { return _isOptional; }
            set { _isOptional = value; }
        }

        internal bool IsTopLevelInSchema
        {
            get { return _topLevelInSchema; }
            set { _topLevelInSchema = value; }
        }

        [return: NotNullIfNotNull("name")]
        internal static string? EscapeName(string? name)
        {
            if (name == null || name.Length == 0) return name;
            return XmlConvert.EncodeLocalName(name);
        }

        [return: NotNullIfNotNull("name")]
        internal static string? EscapeQName(string? name)
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

        [return: NotNullIfNotNull("name")]
        internal static string? UnescapeName(string? name)
        {
            return XmlConvert.DecodeName(name);
        }

        internal string ToString(string? defaultNs)
        {
            if (Any)
            {
                return (Namespace == null ? "##any" : Namespace) + ":" + Name;
            }
            else
            {
                return Namespace == defaultNs ? Name : Namespace + ":" + Name;
            }
        }
    }

    internal sealed class ElementAccessor : Accessor
    {
        private bool _nullable;
        private bool _isSoap;
        private bool _unbounded;

        internal bool IsSoap
        {
            get { return _isSoap; }
            set { _isSoap = value; }
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
            newAccessor._isSoap = _isSoap;
            newAccessor.Name = this.Name;
            newAccessor.Default = this.Default;
            newAccessor.Namespace = this.Namespace;
            newAccessor.Mapping = this.Mapping;
            newAccessor.Any = this.Any;

            return newAccessor;
        }
    }

    internal sealed class ChoiceIdentifierAccessor : Accessor
    {
        private string? _memberName;
        private string[]? _memberIds;
        private MemberInfo? _memberInfo;

        internal string? MemberName
        {
            get { return _memberName; }
            set { _memberName = value; }
        }

        internal string[]? MemberIds
        {
            get { return _memberIds; }
            set { _memberIds = value; }
        }

        internal MemberInfo? MemberInfo
        {
            get { return _memberInfo; }
            set { _memberInfo = value; }
        }
    }

    internal sealed class TextAccessor : Accessor
    {
    }

    internal sealed class XmlnsAccessor : Accessor
    {
    }

    internal sealed class AttributeAccessor : Accessor
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
        private bool _isSoap;

        internal Mapping() { }

        protected Mapping(Mapping mapping)
        {
            _isSoap = mapping._isSoap;
        }

        internal bool IsSoap
        {
            get { return _isSoap; }
            set { _isSoap = value; }
        }
    }

    internal abstract class TypeMapping : Mapping
    {
        private TypeDesc? _typeDesc;
        private string? _typeNs;
        private string? _typeName;
        private bool _referencedByElement;
        private bool _referencedByTopLevelElement;
        private bool _includeInSchema = true;
        private bool _reference;

        internal bool ReferencedByTopLevelElement
        {
            get { return _referencedByTopLevelElement; }
            set { _referencedByTopLevelElement = value; }
        }

        internal bool ReferencedByElement
        {
            get { return _referencedByElement || _referencedByTopLevelElement; }
            set { _referencedByElement = value; }
        }
        internal string? Namespace
        {
            get { return _typeNs; }
            set { _typeNs = value; }
        }

        internal string? TypeName
        {
            get { return _typeName; }
            set { _typeName = value; }
        }

        internal TypeDesc? TypeDesc
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

        internal bool IsReference
        {
            get { return _reference; }
            set { _reference = value; }
        }

        [MemberNotNullWhen(false, nameof(_typeName))]
        internal bool IsAnonymousType
        {
            get { return _typeName == null || _typeName.Length == 0; }
        }

        internal virtual string DefaultElementName
        {
            get { return IsAnonymousType ? XmlConvert.EncodeLocalName(_typeDesc!.Name) : _typeName; }
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

    internal sealed class NullableMapping : TypeMapping
    {
        private TypeMapping? _baseMapping;

        internal TypeMapping? BaseMapping
        {
            get { return _baseMapping; }
            set { _baseMapping = value; }
        }

        internal override string DefaultElementName
        {
            get { return BaseMapping!.DefaultElementName; }
        }
    }

    internal sealed class ArrayMapping : TypeMapping
    {
        private ElementAccessor[]? _elements;
        private ElementAccessor[]? _sortedElements;
        private ArrayMapping? _next;
        private StructMapping? _topLevelMapping;

        internal ElementAccessor[]? Elements
        {
            get { return _elements; }
            set { _elements = value; _sortedElements = null; }
        }

        internal ElementAccessor[]? ElementsSortedByDerivation
        {
            get
            {
                if (_sortedElements != null)
                    return _sortedElements;
                if (_elements == null)
                    return null;
                _sortedElements = new ElementAccessor[_elements.Length];
                Array.Copy(_elements, _sortedElements, _elements.Length);
                AccessorMapping.SortMostToLeastDerived(_sortedElements);
                return _sortedElements;
            }
        }


        internal ArrayMapping? Next
        {
            get { return _next; }
            set { _next = value; }
        }

        internal StructMapping? TopLevelMapping
        {
            get { return _topLevelMapping; }
            set { _topLevelMapping = value; }
        }
    }

    internal sealed class EnumMapping : PrimitiveMapping
    {
        private ConstantMapping[]? _constants;
        private bool _isFlags;

        internal bool IsFlags
        {
            get { return _isFlags; }
            set { _isFlags = value; }
        }

        internal ConstantMapping[]? Constants
        {
            get { return _constants; }
            set { _constants = value; }
        }
    }

    internal sealed class ConstantMapping : Mapping
    {
        private string? _xmlName;
        private string? _name;
        private long _value;

        [AllowNull]
        internal string XmlName
        {
            get { return _xmlName == null ? string.Empty : _xmlName; }
            set { _xmlName = value; }
        }

        [AllowNull]
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

    internal sealed class StructMapping : TypeMapping, INameScope
    {
        private MemberMapping[]? _members;
        private StructMapping? _baseMapping;
        private StructMapping? _derivedMappings;
        private StructMapping? _nextDerivedMapping;
        private MemberMapping? _xmlnsMember;
        private bool _hasSimpleContent;
        private bool _openModel;
        private bool _isSequence;
        private NameTable? _elements;
        private NameTable? _attributes;
        private CodeIdentifiers? _scope;

        [DisallowNull]
        internal StructMapping? BaseMapping
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
                    if (_baseMapping!.IsSequence)
                    {
                        for (StructMapping? derived = _derivedMappings; derived != null; derived = derived.NextDerivedMapping)
                        {
                            derived.SetSequence();
                        }
                    }
                }
            }
        }

        internal StructMapping? DerivedMappings
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
        object? INameScope.this[string? name, string? ns]
        {
            get
            {
                object? named = LocalElements[name, ns];
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
        internal StructMapping? NextDerivedMapping
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
                StructMapping? mapping = this;
                while (mapping != null)
                {
                    if (mapping.XmlnsMember != null)
                        return true;
                    mapping = mapping.BaseMapping;
                }
                return false;
            }
        }

        internal MemberMapping[]? Members
        {
            get { return _members; }
            set { _members = value; }
        }

        internal MemberMapping? XmlnsMember
        {
            get { return _xmlnsMember; }
            set { _xmlnsMember = value; }
        }

        internal bool IsOpenModel
        {
            get { return _openModel; }
            set { _openModel = value; }
        }

        internal CodeIdentifiers Scope
        {
            get
            {
                if (_scope == null)
                    _scope = new CodeIdentifiers();
                return _scope;
            }
            set { _scope = value; }
        }

        internal MemberMapping? FindDeclaringMapping(MemberMapping member, out StructMapping? declaringMapping, string? parent)
        {
            declaringMapping = null;
            if (BaseMapping != null)
            {
                MemberMapping? baseMember = BaseMapping.FindDeclaringMapping(member, out declaringMapping, parent);
                if (baseMember != null) return baseMember;
            }
            if (_members == null) return null;

            for (int i = 0; i < _members.Length; i++)
            {
                if (_members[i].Name == member.Name)
                {
                    if (_members[i].TypeDesc != member.TypeDesc)
                        throw new InvalidOperationException(SR.Format(SR.XmlHiddenMember, parent, member.Name, member.TypeDesc!.FullName, this.TypeName, _members[i].Name, _members[i].TypeDesc!.FullName));
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
        internal bool Declares(MemberMapping member, string? parent)
        {
            StructMapping? m;
            return (FindDeclaringMapping(member, out m, parent) != null);
        }

        internal void SetContentModel(TextAccessor? text, bool hasElements)
        {
            if (BaseMapping == null || BaseMapping.TypeDesc!.IsRoot)
            {
                _hasSimpleContent = !hasElements && text != null && !text.Mapping!.IsList;
            }
            else if (BaseMapping.HasSimpleContent)
            {
                if (text != null || hasElements)
                {
                    // we can only extent a simleContent type with attributes
                    throw new InvalidOperationException(SR.Format(SR.XmlIllegalSimpleContentExtension, TypeDesc!.FullName, BaseMapping.TypeDesc.FullName));
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
            if (!_hasSimpleContent && text != null && !text.Mapping!.TypeDesc!.CanBeTextValue && !(BaseMapping != null && !BaseMapping.TypeDesc!.IsRoot && (text.Mapping.TypeDesc.IsEnum || text.Mapping.TypeDesc.IsPrimitive)))
            {
                throw new InvalidOperationException(SR.Format(SR.XmlIllegalTypedTextAttribute, TypeDesc!.FullName, text.Name, text.Mapping.TypeDesc.FullName));
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
            if (TypeDesc!.IsRoot)
                return;

            StructMapping start = this;

            // find first mapping that does not have the sequence set
            while (start.BaseMapping != null && !start.BaseMapping.IsSequence && !start.BaseMapping.TypeDesc!.IsRoot)
                start = start.BaseMapping;

            start.IsSequence = true;
            for (StructMapping? derived = start.DerivedMappings; derived != null; derived = derived.NextDerivedMapping)
            {
                derived.SetSequence();
            }
        }

        internal bool IsSequence
        {
            get { return _isSequence && !TypeDesc!.IsRoot; }
            set { _isSequence = value; }
        }
    }

    internal abstract class AccessorMapping : Mapping
    {
        private TypeDesc? _typeDesc;
        private AttributeAccessor? _attribute;
        private ElementAccessor[]? _elements;
        private ElementAccessor[]? _sortedElements;
        private TextAccessor? _text;
        private ChoiceIdentifierAccessor? _choiceIdentifier;
        private XmlnsAccessor? _xmlns;
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

        internal TypeDesc? TypeDesc
        {
            get { return _typeDesc; }
            set { _typeDesc = value; }
        }

        internal AttributeAccessor? Attribute
        {
            get { return _attribute; }
            set { _attribute = value; }
        }

        internal ElementAccessor[]? Elements
        {
            get { return _elements; }
            set { _elements = value; _sortedElements = null; }
        }

        internal static void SortMostToLeastDerived(ElementAccessor[] elements)
        {
            Array.Sort(elements, new AccessorComparer());
        }

        internal sealed class AccessorComparer : IComparer
        {
            public int Compare(object? o1, object? o2)
            {
                if (o1 == o2)
                    return 0;
                Accessor a1 = (Accessor)o1!;
                Accessor a2 = (Accessor)o2!;
                int w1 = a1.Mapping!.TypeDesc!.Weight;
                int w2 = a2.Mapping!.TypeDesc!.Weight;
                if (w1 == w2)
                    return 0;
                if (w1 < w2)
                    return 1;
                return -1;
            }
        }

        internal ElementAccessor[]? ElementsSortedByDerivation
        {
            get
            {
                if (_sortedElements != null)
                    return _sortedElements;
                if (_elements == null)
                    return null;
                _sortedElements = new ElementAccessor[_elements.Length];
                Array.Copy(_elements, _sortedElements, _elements.Length);
                SortMostToLeastDerived(_sortedElements);
                return _sortedElements;
            }
        }

        internal TextAccessor? Text
        {
            get { return _text; }
            set { _text = value; }
        }

        internal ChoiceIdentifierAccessor? ChoiceIdentifier
        {
            get { return _choiceIdentifier; }
            set { _choiceIdentifier = value; }
        }

        internal XmlnsAccessor? Xmlns
        {
            get { return _xmlns; }
            set { _xmlns = value; }
        }

        internal bool Ignore
        {
            get { return _ignore; }
            set { _ignore = value; }
        }

        internal Accessor? Accessor
        {
            get
            {
                if (_xmlns != null) return _xmlns;
                if (_attribute != null) return _attribute;
                if (_elements != null && _elements.Length > 0) return _elements[0];
                return _text;
            }
        }

        internal static bool ElementsMatch(ElementAccessor[]? a, ElementAccessor[]? b)
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

    internal sealed class MemberMappingComparer : IComparer
    {
        public int Compare(object? o1, object? o2)
        {
            MemberMapping m1 = (MemberMapping)o1!;
            MemberMapping m2 = (MemberMapping)o2!;

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

    internal sealed class MemberMapping : AccessorMapping
    {
        private string? _name;
        private bool _checkShouldPersist;
        private SpecifiedAccessor _checkSpecified;
        private bool _isReturnValue;
        private bool _readOnly;
        private int _sequenceId = -1;
        private MemberInfo? _memberInfo;
        private MemberInfo? _checkSpecifiedMemberInfo;
        private MethodInfo? _checkShouldPersistMethodInfo;

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

        internal MemberInfo? MemberInfo
        {
            get { return _memberInfo; }
            set { _memberInfo = value; }
        }

        internal MemberInfo? CheckSpecifiedMemberInfo
        {
            get { return _checkSpecifiedMemberInfo; }
            set { _checkSpecifiedMemberInfo = value; }
        }

        internal MethodInfo? CheckShouldPersistMethodInfo
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

        internal MemberMapping Clone()
        {
            return new MemberMapping(this);
        }
    }

    internal sealed class MembersMapping : TypeMapping
    {
        private MemberMapping[]? _members;
        private bool _hasWrapperElement = true;
        private bool _validateRpcWrapperElement;
        private bool _writeAccessors = true;
        private MemberMapping? _xmlnsMember;

        internal MemberMapping[]? Members
        {
            get { return _members; }
            set { _members = value; }
        }

        internal MemberMapping? XmlnsMember
        {
            get { return _xmlnsMember; }
            set { _xmlnsMember = value; }
        }

        internal bool HasWrapperElement
        {
            get { return _hasWrapperElement; }
            set { _hasWrapperElement = value; }
        }

        internal bool ValidateRpcWrapperElement
        {
            get { return _validateRpcWrapperElement; }
            set { _validateRpcWrapperElement = value; }
        }

        internal bool WriteAccessors
        {
            get { return _writeAccessors; }
            set { _writeAccessors = value; }
        }
    }

    internal class SpecialMapping : TypeMapping
    {
        private bool _namedAny;

        internal bool NamedAny
        {
            get { return _namedAny; }
            set { _namedAny = value; }
        }
    }

    internal sealed class SerializableMapping : SpecialMapping
    {
        private XmlSchema? _schema;
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        private Type? _type;
        private bool _needSchema = true;

        // new implementation of the IXmlSerializable
        private readonly MethodInfo? _getSchemaMethod;
        private XmlQualifiedName? _xsiType;
        private XmlSchemaType? _xsdType;
        private XmlSchemaSet? _schemas;
        private bool _any;
        private string? _namespaces;

        private SerializableMapping? _baseMapping;
        private SerializableMapping? _derivedMappings;
        private SerializableMapping? _nextDerivedMapping;
        private SerializableMapping? _next; // all mappings with the same qname

        internal SerializableMapping() { }
        internal SerializableMapping(MethodInfo getSchemaMethod, bool any, string? ns)
        {
            _getSchemaMethod = getSchemaMethod;
            _any = any;
            this.Namespace = ns;
            _needSchema = getSchemaMethod != null;
        }

        internal SerializableMapping(XmlQualifiedName xsiType, XmlSchemaSet schemas)
        {
            _xsiType = xsiType;
            _schemas = schemas;
            this.TypeName = xsiType.Name;
            this.Namespace = xsiType.Namespace;
            _needSchema = false;
        }

        internal void SetBaseMapping(SerializableMapping? mapping)
        {
            _baseMapping = mapping;
            if (_baseMapping != null)
            {
                _nextDerivedMapping = _baseMapping._derivedMappings;
                _baseMapping._derivedMappings = this;
                if (this == _nextDerivedMapping)
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlCircularDerivation, TypeDesc!.FullName));
                }
            }
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

        internal string NamespaceList
        {
            get
            {
                RetrieveSerializableSchema();
                if (_namespaces == null)
                {
                    if (_schemas != null)
                    {
                        StringBuilder anyNamespaces = new StringBuilder();
                        foreach (XmlSchema s in _schemas.Schemas())
                        {
                            if (s.TargetNamespace != null && s.TargetNamespace.Length > 0)
                            {
                                if (anyNamespaces.Length > 0)
                                    anyNamespaces.Append(' ');
                                anyNamespaces.Append(s.TargetNamespace);
                            }
                        }
                        _namespaces = anyNamespaces.ToString();
                    }
                    else
                    {
                        _namespaces = string.Empty;
                    }
                }
                return _namespaces;
            }
        }

        internal SerializableMapping? DerivedMappings
        {
            get
            {
                return _derivedMappings;
            }
        }

        internal SerializableMapping? NextDerivedMapping
        {
            get
            {
                return _nextDerivedMapping;
            }
        }

        internal SerializableMapping? Next
        {
            get { return _next; }
            set { _next = value; }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        internal Type? Type
        {
            get { return _type; }
            set { _type = value; }
        }

        internal XmlSchemaSet? Schemas
        {
            get
            {
                RetrieveSerializableSchema();
                return _schemas;
            }
        }

        internal XmlSchema? Schema
        {
            get
            {
                RetrieveSerializableSchema();
                return _schema;
            }
        }

        internal XmlQualifiedName? XsiType
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

        internal XmlSchemaType? XsdType
        {
            get
            {
                RetrieveSerializableSchema();
                return _xsdType;
            }
        }

        internal static void ValidationCallbackWithErrorCode(object? sender, ValidationEventArgs args)
        {
            // CONSIDER: need the real type name
            if (args.Severity == XmlSeverityType.Error)
                throw new InvalidOperationException(SR.Format(SR.XmlSerializableSchemaError, nameof(IXmlSerializable), args.Message));
        }

        internal void CheckDuplicateElement(XmlSchemaElement? element, string? elementNs)
        {
            if (element == null)
                return;

            // only check duplicate definitions for top-level element
            if (element.Parent == null || !(element.Parent is XmlSchema))
                return;

            XmlSchemaObjectTable? elements = null;
            if (Schema != null && Schema.TargetNamespace == elementNs)
            {
                XmlSchemas.Preprocess(Schema);
                elements = Schema.Elements;
            }
            else if (Schemas != null)
            {
                elements = Schemas.GlobalElements;
            }
            else
            {
                return;
            }
            foreach (XmlSchemaElement e in elements.Values)
            {
                if (e.Name == element.Name && e.QualifiedName.Namespace == elementNs)
                {
                    if (Match(e, element))
                        return;
                    // XmlSerializableRootDupName=Cannot reconcile schema for '{0}'. Please use [XmlRoot] attribute to change name or namepace of the top-level element to avoid duplicate element declarations: element name='{1} namespace='{2}'.
                    throw new InvalidOperationException(SR.Format(SR.XmlSerializableRootDupName, _getSchemaMethod!.DeclaringType!.FullName, e.Name, elementNs));
                }
            }
        }

        private bool Match(XmlSchemaElement e1, XmlSchemaElement e2)
        {
            if (e1.IsNillable != e2.IsNillable)
                return false;
            if (e1.RefName != e2.RefName)
                return false;
            if (e1.SchemaType != e2.SchemaType)
                return false;
            if (e1.SchemaTypeName != e2.SchemaTypeName)
                return false;
            if (e1.MinOccurs != e2.MinOccurs)
                return false;
            if (e1.MaxOccurs != e2.MaxOccurs)
                return false;
            if (e1.IsAbstract != e2.IsAbstract)
                return false;
            if (e1.DefaultValue != e2.DefaultValue)
                return false;
            if (e1.SubstitutionGroup != e2.SubstitutionGroup)
                return false;
            return true;
        }

        private void RetrieveSerializableSchema()
        {
            if (_needSchema)
            {
                _needSchema = false;
                if (_getSchemaMethod != null)
                {
                    // get the type info
                    if (_schemas == null)
                        _schemas = new XmlSchemaSet();
                    object? typeInfo = _getSchemaMethod.Invoke(null, new object[] { _schemas });
                    _xsiType = XmlQualifiedName.Empty;

                    if (typeInfo != null)
                    {
                        if (typeof(XmlSchemaType).IsAssignableFrom(_getSchemaMethod.ReturnType))
                        {
                            _xsdType = (XmlSchemaType)typeInfo;
                            // check if type is named
                            _xsiType = _xsdType.QualifiedName;
                        }
                        else if (typeof(XmlQualifiedName).IsAssignableFrom(_getSchemaMethod.ReturnType))
                        {
                            _xsiType = (XmlQualifiedName)typeInfo;
                            if (_xsiType.IsEmpty)
                            {
                                throw new InvalidOperationException(SR.Format(SR.XmlGetSchemaEmptyTypeName, _type!.FullName, _getSchemaMethod.Name));
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException(SR.Format(SR.XmlGetSchemaMethodReturnType, _type!.Name, _getSchemaMethod.Name, nameof(XmlSchemaProviderAttribute), typeof(XmlQualifiedName).FullName));
                        }
                    }
                    else
                    {
                        _any = true;
                    }

                    // make sure that user-specified schemas are valid
                    _schemas.ValidationEventHandler += new ValidationEventHandler(ValidationCallbackWithErrorCode);
                    _schemas.Compile();

                    // at this point we verified that the information returned by the IXmlSerializable is valid
                    // Now check to see if the type was referenced before:
                    // UNDONE check for the duplcate types
                    if (!_xsiType.IsEmpty)
                    {
                        // try to find the type in the schemas collection
                        if (_xsiType.Namespace != XmlSchema.Namespace)
                        {
                            ArrayList srcSchemas = (ArrayList)_schemas.Schemas(_xsiType.Namespace);

                            if (srcSchemas.Count == 0)
                            {
                                throw new InvalidOperationException(SR.Format(SR.XmlMissingSchema, _xsiType.Namespace));
                            }
                            if (srcSchemas.Count > 1)
                            {
                                throw new InvalidOperationException(SR.Format(SR.XmlGetSchemaInclude, _xsiType.Namespace, _getSchemaMethod.DeclaringType!.FullName, _getSchemaMethod.Name));
                            }
                            XmlSchema? s = (XmlSchema?)srcSchemas[0];
                            if (s == null)
                            {
                                throw new InvalidOperationException(SR.Format(SR.XmlMissingSchema, _xsiType.Namespace));
                            }
                            _xsdType = (XmlSchemaType?)s.SchemaTypes[_xsiType];
                            if (_xsdType == null)
                            {
                                throw new InvalidOperationException(SR.Format(SR.XmlGetSchemaTypeMissing, _getSchemaMethod.DeclaringType!.FullName, _getSchemaMethod.Name, _xsiType.Name, _xsiType.Namespace));
                            }
                            _xsdType = _xsdType.Redefined != null ? _xsdType.Redefined : _xsdType;
                        }
                    }
                }
                else
                {
                    IXmlSerializable serializable = (IXmlSerializable)Activator.CreateInstance(_type!)!;
                    _schema = serializable.GetSchema();

                    if (_schema != null)
                    {
                        if (_schema.Id == null || _schema.Id.Length == 0) throw new InvalidOperationException(SR.Format(SR.XmlSerializableNameMissing1, _type!.FullName));
                    }
                }
            }
        }
    }
}
