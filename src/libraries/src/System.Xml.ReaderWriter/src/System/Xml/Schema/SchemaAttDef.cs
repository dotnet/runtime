// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Collections.Generic;

namespace System.Xml.Schema
{
    /*
     * This class describes an attribute type and potential values.
     * This encapsulates the information for one Attdef * in an
     * Attlist in a DTD as described below:
     */
    internal sealed class SchemaAttDef : SchemaDeclBase, IDtdDefaultAttributeInfo
    {
        internal enum Reserve
        {
            None,
            XmlSpace,
            XmlLang
        };

        private String _defExpanded;  // default value in its expanded form

        private int _lineNum;
        private int _linePos;
        private int _valueLineNum;
        private int _valueLinePos;

        private Reserve _reserved = Reserve.None; // indicate the attribute type, such as xml:lang or xml:space   

        private XmlTokenizedType _tokenizedType;


        //
        // Constructors
        //
        public SchemaAttDef(XmlQualifiedName name, String prefix)
            : base(name, prefix)
        {
        }


        //
        // IDtdAttributeInfo interface
        //
        #region IDtdAttributeInfo Members
        string IDtdAttributeInfo.Prefix
        {
            get { return ((SchemaAttDef)this).Prefix; }
        }

        string IDtdAttributeInfo.LocalName
        {
            get { return ((SchemaAttDef)this).Name.Name; }
        }

        int IDtdAttributeInfo.LineNumber
        {
            get { return ((SchemaAttDef)this).LineNumber; }
        }

        int IDtdAttributeInfo.LinePosition
        {
            get { return ((SchemaAttDef)this).LinePosition; }
        }

        bool IDtdAttributeInfo.IsNonCDataType
        {
            get { return this.TokenizedType != XmlTokenizedType.CDATA; }
        }

        bool IDtdAttributeInfo.IsDeclaredInExternal
        {
            get { return ((SchemaAttDef)this).IsDeclaredInExternal; }
        }

        bool IDtdAttributeInfo.IsXmlAttribute
        {
            get { return this.Reserved != SchemaAttDef.Reserve.None; }
        }

        #endregion

        //
        // IDtdDefaultAttributeInfo interface
        //
        #region IDtdDefaultAttributeInfo Members
        string IDtdDefaultAttributeInfo.DefaultValueExpanded
        {
            get { return ((SchemaAttDef)this).DefaultValueExpanded; }
        }

        object IDtdDefaultAttributeInfo.DefaultValueTyped
        {
            get { return null; }
        }

        int IDtdDefaultAttributeInfo.ValueLineNumber
        {
            get { return ((SchemaAttDef)this).ValueLineNumber; }
        }

        int IDtdDefaultAttributeInfo.ValueLinePosition
        {
            get { return ((SchemaAttDef)this).ValueLinePosition; }
        }
        #endregion

        //
        // Internal properties
        //
        internal int LinePosition
        {
            get { return _linePos; }
            set { _linePos = value; }
        }

        internal int LineNumber
        {
            get { return _lineNum; }
            set { _lineNum = value; }
        }

        internal int ValueLinePosition
        {
            get { return _valueLinePos; }
            set { _valueLinePos = value; }
        }

        internal int ValueLineNumber
        {
            get { return _valueLineNum; }
            set { _valueLineNum = value; }
        }

        internal String DefaultValueExpanded
        {
            get { return (_defExpanded != null) ? _defExpanded : String.Empty; }
            set { _defExpanded = value; }
        }

        internal XmlTokenizedType TokenizedType
        {
            get
            {
                return _tokenizedType;
            }
            set
            {
                _tokenizedType = value;
            }
        }

        internal Reserve Reserved
        {
            get { return _reserved; }
            set { _reserved = value; }
        }
    }
}
