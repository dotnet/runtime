// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml;
using System.Diagnostics;
using System.Collections.Generic;

namespace System.Xml.Schema
{
    internal enum AttributeMatchState
    {
        AttributeFound,
        AnyIdAttributeFound,
        UndeclaredElementAndAttribute,
        UndeclaredAttribute,
        AnyAttributeLax,
        AnyAttributeSkip,
        ProhibitedAnyAttribute,
        ProhibitedAttribute,
        AttributeNameMismatch,
        ValidateAttributeInvalidCall,
    }

    internal sealed class SchemaInfo : IDtdInfo
    {
        private readonly Dictionary<XmlQualifiedName, SchemaElementDecl> _elementDecls = new Dictionary<XmlQualifiedName, SchemaElementDecl>();
        private readonly Dictionary<XmlQualifiedName, SchemaElementDecl> _undeclaredElementDecls = new Dictionary<XmlQualifiedName, SchemaElementDecl>();

        private Dictionary<XmlQualifiedName, SchemaEntity>? _generalEntities;
        private Dictionary<XmlQualifiedName, SchemaEntity>? _parameterEntities;

        private XmlQualifiedName _docTypeName = XmlQualifiedName.Empty;
        private string _internalDtdSubset = string.Empty;
        private bool _hasNonCDataAttributes;
        private bool _hasDefaultAttributes;

        private readonly Dictionary<string, bool> _targetNamespaces = new Dictionary<string, bool>();
        private readonly Dictionary<XmlQualifiedName, SchemaAttDef> _attributeDecls = new Dictionary<XmlQualifiedName, SchemaAttDef>();
        private int _errorCount;
        private SchemaType _schemaType;
        private readonly Dictionary<XmlQualifiedName, SchemaElementDecl> _elementDeclsByType = new Dictionary<XmlQualifiedName, SchemaElementDecl>();
        private Dictionary<string, SchemaNotation>? _notations;


        internal SchemaInfo()
        {
            _schemaType = SchemaType.None;
        }

        public XmlQualifiedName DocTypeName
        {
            get { return _docTypeName; }
            set { _docTypeName = value; }
        }

        internal string InternalDtdSubset
        {
            get { return _internalDtdSubset; }
            set { _internalDtdSubset = value; }
        }

        internal Dictionary<XmlQualifiedName, SchemaElementDecl> ElementDecls
        {
            get { return _elementDecls; }
        }

        internal Dictionary<XmlQualifiedName, SchemaElementDecl> UndeclaredElementDecls
        {
            get { return _undeclaredElementDecls; }
        }

        internal Dictionary<XmlQualifiedName, SchemaEntity> GeneralEntities =>
            _generalEntities ??= new Dictionary<XmlQualifiedName, SchemaEntity>();

        internal Dictionary<XmlQualifiedName, SchemaEntity> ParameterEntities =>
            _parameterEntities ??= new Dictionary<XmlQualifiedName, SchemaEntity>();

        internal SchemaType SchemaType
        {
            get { return _schemaType; }
            set { _schemaType = value; }
        }

        internal Dictionary<string, bool> TargetNamespaces
        {
            get { return _targetNamespaces; }
        }

        internal Dictionary<XmlQualifiedName, SchemaElementDecl> ElementDeclsByType
        {
            get { return _elementDeclsByType; }
        }

        internal Dictionary<XmlQualifiedName, SchemaAttDef> AttributeDecls
        {
            get { return _attributeDecls; }
        }

        internal Dictionary<string, SchemaNotation> Notations =>
            _notations ??= new Dictionary<string, SchemaNotation>();

        internal int ErrorCount
        {
            get { return _errorCount; }
            set { _errorCount = value; }
        }

        internal SchemaElementDecl? GetElementDecl(XmlQualifiedName qname)
        {
            SchemaElementDecl? elemDecl;
            if (_elementDecls.TryGetValue(qname, out elemDecl))
            {
                return elemDecl;
            }

            return null;
        }

        internal SchemaElementDecl? GetTypeDecl(XmlQualifiedName qname)
        {
            SchemaElementDecl? elemDecl;
            if (_elementDeclsByType.TryGetValue(qname, out elemDecl))
            {
                return elemDecl;
            }

            return null;
        }


        internal XmlSchemaElement? GetElement(XmlQualifiedName qname)
        {
            SchemaElementDecl? ed = GetElementDecl(qname);
            if (ed != null)
            {
                return ed.SchemaElement;
            }

            return null;
        }

        internal bool HasSchema(string ns)
        {
            return _targetNamespaces.ContainsKey(ns);
        }

        internal bool Contains(string ns)
        {
            return _targetNamespaces.ContainsKey(ns);
        }

        internal SchemaAttDef? GetAttributeXdr(SchemaElementDecl? ed, XmlQualifiedName qname)
        {
            SchemaAttDef? attdef = null;
            if (ed != null)
            {
                attdef = ed.GetAttDef(qname);
                if (attdef == null)
                {
                    if (!ed.ContentValidator!.IsOpen || qname.Namespace.Length == 0)
                    {
                        throw new XmlSchemaException(SR.Sch_UndeclaredAttribute, qname.ToString());
                    }
                    if (!_attributeDecls.TryGetValue(qname, out attdef) && _targetNamespaces.ContainsKey(qname.Namespace))
                    {
                        throw new XmlSchemaException(SR.Sch_UndeclaredAttribute, qname.ToString());
                    }
                }
            }

            return attdef;
        }


        internal SchemaAttDef? GetAttributeXsd(SchemaElementDecl? ed, XmlQualifiedName qname, XmlSchemaObject? partialValidationType, out AttributeMatchState attributeMatchState)
        {
            SchemaAttDef? attdef = null;
            attributeMatchState = AttributeMatchState.UndeclaredAttribute;
            if (ed != null)
            {
                attdef = ed.GetAttDef(qname);
                if (attdef != null)
                {
                    attributeMatchState = AttributeMatchState.AttributeFound;
                    return attdef;
                }
                XmlSchemaAnyAttribute? any = ed.AnyAttribute;
                if (any != null)
                {
                    if (!any.NamespaceList!.Allows(qname))
                    {
                        attributeMatchState = AttributeMatchState.ProhibitedAnyAttribute;
                    }
                    else if (any.ProcessContentsCorrect != XmlSchemaContentProcessing.Skip)
                    {
                        if (_attributeDecls.TryGetValue(qname, out attdef))
                        {
                            if (attdef.Datatype.TypeCode == XmlTypeCode.Id)
                            { //anyAttribute match whose type is ID
                                attributeMatchState = AttributeMatchState.AnyIdAttributeFound;
                            }
                            else
                            {
                                attributeMatchState = AttributeMatchState.AttributeFound;
                            }
                        }
                        else if (any.ProcessContentsCorrect == XmlSchemaContentProcessing.Lax)
                        {
                            attributeMatchState = AttributeMatchState.AnyAttributeLax;
                        }
                    }
                    else
                    {
                        attributeMatchState = AttributeMatchState.AnyAttributeSkip;
                    }
                }
                else if (ed.ProhibitedAttributes.ContainsKey(qname))
                {
                    attributeMatchState = AttributeMatchState.ProhibitedAttribute;
                }
            }
            else if (partialValidationType != null)
            {
                XmlSchemaAttribute? attr = partialValidationType as XmlSchemaAttribute;
                if (attr != null)
                {
                    if (qname.Equals(attr.QualifiedName))
                    {
                        attdef = attr.AttDef;
                        attributeMatchState = AttributeMatchState.AttributeFound;
                    }
                    else
                    {
                        attributeMatchState = AttributeMatchState.AttributeNameMismatch;
                    }
                }
                else
                {
                    attributeMatchState = AttributeMatchState.ValidateAttributeInvalidCall;
                }
            }
            else
            {
                if (_attributeDecls.TryGetValue(qname, out attdef))
                {
                    attributeMatchState = AttributeMatchState.AttributeFound;
                }
                else
                {
                    attributeMatchState = AttributeMatchState.UndeclaredElementAndAttribute;
                }
            }
            return attdef;
        }

        internal SchemaAttDef? GetAttributeXsd(SchemaElementDecl? ed, XmlQualifiedName qname, ref bool skip)
        {
            AttributeMatchState attributeMatchState;

            SchemaAttDef? attDef = GetAttributeXsd(ed, qname, null, out attributeMatchState);
            switch (attributeMatchState)
            {
                case AttributeMatchState.UndeclaredAttribute:
                    throw new XmlSchemaException(SR.Sch_UndeclaredAttribute, qname.ToString());

                case AttributeMatchState.ProhibitedAnyAttribute:
                case AttributeMatchState.ProhibitedAttribute:
                    throw new XmlSchemaException(SR.Sch_ProhibitedAttribute, qname.ToString());

                case AttributeMatchState.AttributeFound:
                case AttributeMatchState.AnyIdAttributeFound:
                case AttributeMatchState.AnyAttributeLax:
                case AttributeMatchState.UndeclaredElementAndAttribute:
                    break;

                case AttributeMatchState.AnyAttributeSkip:
                    skip = true;
                    break;

                default:
                    Debug.Fail($"Unexpected match state {attributeMatchState}");
                    break;
            }
            return attDef;
        }

        internal void Add(SchemaInfo sinfo, ValidationEventHandler? eventhandler)
        {
            if (_schemaType == SchemaType.None)
            {
                _schemaType = sinfo.SchemaType;
            }
            else if (_schemaType != sinfo.SchemaType)
            {
                eventhandler?.Invoke(this, new ValidationEventArgs(new XmlSchemaException(SR.Sch_MixSchemaTypes, string.Empty)));
                return;
            }

            foreach (string tns in sinfo.TargetNamespaces.Keys)
            {
                if (!_targetNamespaces.ContainsKey(tns))
                {
                    _targetNamespaces.Add(tns, true);
                }
            }

            foreach (KeyValuePair<XmlQualifiedName, SchemaElementDecl> entry in sinfo._elementDecls)
            {
                if (!_elementDecls.ContainsKey(entry.Key))
                {
                    _elementDecls.Add(entry.Key, entry.Value);
                }
            }
            foreach (KeyValuePair<XmlQualifiedName, SchemaElementDecl> entry in sinfo._elementDeclsByType)
            {
                if (!_elementDeclsByType.ContainsKey(entry.Key))
                {
                    _elementDeclsByType.Add(entry.Key, entry.Value);
                }
            }
            foreach (SchemaAttDef attdef in sinfo.AttributeDecls.Values)
            {
                if (!_attributeDecls.ContainsKey(attdef.Name))
                {
                    _attributeDecls.Add(attdef.Name, attdef);
                }
            }
            foreach (SchemaNotation notation in sinfo.Notations.Values)
            {
                if (!Notations.ContainsKey(notation.Name.Name))
                {
                    Notations.Add(notation.Name.Name, notation);
                }
            }
        }

        internal void Finish()
        {
            Dictionary<XmlQualifiedName, SchemaElementDecl> elements = _elementDecls;
            for (int i = 0; i < 2; i++)
            {
                foreach (SchemaElementDecl e in elements.Values)
                {
                    if (e.HasNonCDataAttribute)
                    {
                        _hasNonCDataAttributes = true;
                    }
                    if (e.DefaultAttDefs != null)
                    {
                        _hasDefaultAttributes = true;
                    }
                }
                elements = _undeclaredElementDecls;
            }
        }
        //
        // IDtdInfo interface
        //
        #region IDtdInfo Members
        bool IDtdInfo.HasDefaultAttributes
        {
            get
            {
                return _hasDefaultAttributes;
            }
        }

        bool IDtdInfo.HasNonCDataAttributes
        {
            get
            {
                return _hasNonCDataAttributes;
            }
        }

        IDtdAttributeListInfo? IDtdInfo.LookupAttributeList(string prefix, string localName)
        {
            XmlQualifiedName qname = new XmlQualifiedName(prefix, localName);
            SchemaElementDecl? elementDecl;
            if (!_elementDecls.TryGetValue(qname, out elementDecl))
            {
                _undeclaredElementDecls.TryGetValue(qname, out elementDecl);
            }
            return elementDecl;
        }

        IEnumerable<IDtdAttributeListInfo> IDtdInfo.GetAttributeLists()
        {
            foreach (SchemaElementDecl elemDecl in _elementDecls.Values)
            {
                IDtdAttributeListInfo eleDeclAsAttList = (IDtdAttributeListInfo)elemDecl;
                yield return eleDeclAsAttList;
            }
        }

        IDtdEntityInfo? IDtdInfo.LookupEntity(string name)
        {
            if (_generalEntities == null)
            {
                return null;
            }
            XmlQualifiedName qname = new XmlQualifiedName(name);
            SchemaEntity? entity;
            if (_generalEntities.TryGetValue(qname, out entity))
            {
                return entity;
            }
            return null;
        }

        XmlQualifiedName IDtdInfo.Name
        {
            get { return _docTypeName; }
        }

        string IDtdInfo.InternalDtdSubset
        {
            get { return _internalDtdSubset; }
        }
        #endregion
    }
}
