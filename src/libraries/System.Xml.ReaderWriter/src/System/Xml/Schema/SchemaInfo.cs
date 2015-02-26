// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using System.Diagnostics;
using System.Collections.Generic;

namespace System.Xml.Schema
{
    internal class SchemaInfo : IDtdInfo
    {
        private Dictionary<XmlQualifiedName, SchemaElementDecl> _elementDecls = new Dictionary<XmlQualifiedName, SchemaElementDecl>();
        private Dictionary<XmlQualifiedName, SchemaElementDecl> _undeclaredElementDecls = new Dictionary<XmlQualifiedName, SchemaElementDecl>();

        private Dictionary<XmlQualifiedName, SchemaEntity> _generalEntities;
        private Dictionary<XmlQualifiedName, SchemaEntity> _parameterEntities;

        private XmlQualifiedName _docTypeName = XmlQualifiedName.Empty;
        private string _internalDtdSubset = string.Empty;
        private bool _hasNonCDataAttributes = false;
        private bool _hasDefaultAttributes = false;



        internal SchemaInfo()
        {
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

        internal Dictionary<XmlQualifiedName, SchemaEntity> GeneralEntities
        {
            get
            {
                if (_generalEntities == null)
                {
                    _generalEntities = new Dictionary<XmlQualifiedName, SchemaEntity>();
                }
                return _generalEntities;
            }
        }

        internal Dictionary<XmlQualifiedName, SchemaEntity> ParameterEntities
        {
            get
            {
                if (_parameterEntities == null)
                {
                    _parameterEntities = new Dictionary<XmlQualifiedName, SchemaEntity>();
                }
                return _parameterEntities;
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

        IDtdAttributeListInfo IDtdInfo.LookupAttributeList(string prefix, string localName)
        {
            XmlQualifiedName qname = new XmlQualifiedName(prefix, localName);
            SchemaElementDecl elementDecl;
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

        IDtdEntityInfo IDtdInfo.LookupEntity(string name)
        {
            if (_generalEntities == null)
            {
                return null;
            }
            XmlQualifiedName qname = new XmlQualifiedName(name);
            SchemaEntity entity;
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
