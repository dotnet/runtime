// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System;
    using System.Diagnostics;

    internal sealed class SchemaEntity : IDtdEntityInfo
    {
        private XmlQualifiedName _qname;      // Name of entity
        private String _url;                  // Url for external entity (system id)
        private String _pubid;                // Pubid for external entity
        private String _text;                 // Text for internal entity
        private XmlQualifiedName _ndata = XmlQualifiedName.Empty; // NDATA identifier
        private int _lineNumber;           // line number
        private int _linePosition;         // character position
        private bool _isParameter;          // parameter entity flag
        private bool _isExternal;           // external entity flag
        private bool _parsingInProgress;      // whether entity is being parsed (DtdParser infinite recursion check)
        private bool _isDeclaredInExternal; // declared in external markup or not
        private string _baseURI;
        private string _declaredURI;

        //
        // Constructor
        //
        internal SchemaEntity(XmlQualifiedName qname, bool isParameter)
        {
            _qname = qname;
            _isParameter = isParameter;
        }

        //
        // IDtdEntityInfo interface
        //
        #region IDtdEntityInfo Members

        string IDtdEntityInfo.Name
        {
            get { return this.Name.Name; }
        }

        bool IDtdEntityInfo.IsExternal
        {
            get { return ((SchemaEntity)this).IsExternal; }
        }

        bool IDtdEntityInfo.IsDeclaredInExternal
        {
            get { return this.DeclaredInExternal; }
        }

        bool IDtdEntityInfo.IsUnparsedEntity
        {
            get { return !this.NData.IsEmpty; }
        }

        bool IDtdEntityInfo.IsParameterEntity
        {
            get { return _isParameter; }
        }

        string IDtdEntityInfo.BaseUriString
        {
            get { return this.BaseURI; }
        }

        string IDtdEntityInfo.DeclaredUriString
        {
            get { return this.DeclaredURI; }
        }

        string IDtdEntityInfo.SystemId
        {
            get { return this.Url; }
        }

        string IDtdEntityInfo.PublicId
        {
            get { return this.Pubid; }
        }

        string IDtdEntityInfo.Text
        {
            get { return ((SchemaEntity)this).Text; }
        }

        int IDtdEntityInfo.LineNumber
        {
            get { return this.Line; }
        }

        int IDtdEntityInfo.LinePosition
        {
            get { return this.Pos; }
        }

        #endregion

        //
        // Internal methods and properties
        //
        internal static bool IsPredefinedEntity(String n)
        {
            return (n == "lt" ||
                   n == "gt" ||
                   n == "amp" ||
                   n == "apos" ||
                   n == "quot");
        }

        internal XmlQualifiedName Name
        {
            get { return _qname; }
        }

        internal String Url
        {
            get { return _url; }
            set { _url = value; _isExternal = true; }
        }

        internal String Pubid
        {
            get { return _pubid; }
            set { _pubid = value; }
        }

        internal bool IsExternal
        {
            get { return _isExternal; }
            set { _isExternal = value; }
        }

        internal bool DeclaredInExternal
        {
            get { return _isDeclaredInExternal; }
            set { _isDeclaredInExternal = value; }
        }

        internal XmlQualifiedName NData
        {
            get { return _ndata; }
            set { _ndata = value; }
        }

        internal String Text
        {
            get { return _text; }
            set { _text = value; _isExternal = false; }
        }

        internal int Line
        {
            get { return _lineNumber; }
            set { _lineNumber = value; }
        }

        internal int Pos
        {
            get { return _linePosition; }
            set { _linePosition = value; }
        }

        internal String BaseURI
        {
            get { return (_baseURI == null) ? String.Empty : _baseURI; }
            set { _baseURI = value; }
        }

        internal bool ParsingInProgress
        {
            get { return _parsingInProgress; }
            set { _parsingInProgress = value; }
        }

        internal String DeclaredURI
        {
            get { return (_declaredURI == null) ? String.Empty : _declaredURI; }
            set { _declaredURI = value; }
        }
    };
}
