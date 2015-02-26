// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

namespace System.Xml.Schema
{
    internal sealed class SchemaElementDecl : SchemaDeclBase, IDtdAttributeListInfo
    {
        private Dictionary<XmlQualifiedName, SchemaAttDef> _attdefs = new Dictionary<XmlQualifiedName, SchemaAttDef>();
        private List<IDtdDefaultAttributeInfo> _defaultAttdefs;
        private bool _isIdDeclared;
        private bool _hasNonCDataAttribute = false;


        //
        // Constructor
        //

        internal SchemaElementDecl(XmlQualifiedName name, String prefix)
        : base(name, prefix)
        {
        }

        //
        // Static methods
        //

        //
        // IDtdAttributeListInfo interface
        //
        #region IDtdAttributeListInfo Members

        string IDtdAttributeListInfo.Prefix
        {
            get { return ((SchemaElementDecl)this).Prefix; }
        }

        string IDtdAttributeListInfo.LocalName
        {
            get { return ((SchemaElementDecl)this).Name.Name; }
        }

        bool IDtdAttributeListInfo.HasNonCDataAttributes
        {
            get { return _hasNonCDataAttribute; }
        }

        IDtdAttributeInfo IDtdAttributeListInfo.LookupAttribute(string prefix, string localName)
        {
            XmlQualifiedName qname = new XmlQualifiedName(localName, prefix);
            SchemaAttDef attDef;
            if (_attdefs.TryGetValue(qname, out attDef))
            {
                return attDef;
            }
            return null;
        }

        IEnumerable<IDtdDefaultAttributeInfo> IDtdAttributeListInfo.LookupDefaultAttributes()
        {
            return _defaultAttdefs;
        }
        #endregion

        //
        // SchemaElementDecl properties
        //
        internal bool IsIdDeclared
        {
            get { return _isIdDeclared; }
            set { _isIdDeclared = value; }
        }

        internal bool HasNonCDataAttribute
        {
            get { return _hasNonCDataAttribute; }
            set { _hasNonCDataAttribute = value; }
        }

        // add a new SchemaAttDef to the SchemaElementDecl
        internal void AddAttDef(SchemaAttDef attdef)
        {
            _attdefs.Add(attdef.Name, attdef);
            if (attdef.Presence == SchemaDeclBase.Use.Default || attdef.Presence == SchemaDeclBase.Use.Fixed)
            { //Not adding RequiredFixed here
                if (_defaultAttdefs == null)
                {
                    _defaultAttdefs = new List<IDtdDefaultAttributeInfo>();
                }
                _defaultAttdefs.Add(attdef);
            }
        }

        /*
         * Retrieves the attribute definition of the named attribute.
         * @param name  The name of the attribute.
         * @return  an attribute definition object; returns null if it is not found.
         */
        internal SchemaAttDef GetAttDef(XmlQualifiedName qname)
        {
            SchemaAttDef attDef;
            if (_attdefs.TryGetValue(qname, out attDef))
            {
                return attDef;
            }
            return null;
        }

        internal IList<IDtdDefaultAttributeInfo> DefaultAttDefs
        {
            get { return _defaultAttdefs; }
        }
    }
}
