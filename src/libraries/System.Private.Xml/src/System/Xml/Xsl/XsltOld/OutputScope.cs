// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class OutputScope : DocumentScope
    {
        private string _name;
        private string _nsUri;
        private string _prefix;
        private XmlSpace _space;
        private string _lang;
        private bool _mixed;
        private bool _toCData;
        private HtmlElementProps? _htmlElementProps; // in HTML output -- atomized name of element

        internal string Name
        {
            get { return _name; }
        }
        internal string Namespace
        {
            get { return _nsUri; }
        }
        internal string Prefix
        {
            get { return _prefix; }
            set { _prefix = value; }
        }
        internal XmlSpace Space
        {
            get { return _space; }
            set { _space = value; }
        }
        internal string Lang
        {
            get { return _lang; }
            set { _lang = value; }
        }
        internal bool Mixed
        {
            get { return _mixed; }
            set { _mixed = value; }
        }
        internal bool ToCData
        {
            get { return _toCData; }
            set { _toCData = value; }
        }
        internal HtmlElementProps? HtmlElementProps
        {
            get { return _htmlElementProps; }
            set { _htmlElementProps = value; }
        }

        internal OutputScope()
        {
            Init(string.Empty, string.Empty, string.Empty, XmlSpace.None, string.Empty, false);
        }

        [MemberNotNull(nameof(_name))]
        [MemberNotNull(nameof(_nsUri))]
        [MemberNotNull(nameof(_prefix))]
        [MemberNotNull(nameof(_lang))]
        internal void Init(string name, string nspace, string prefix, XmlSpace space, string lang, bool mixed)
        {
            this.scopes = null;
            _name = name;
            _nsUri = nspace;
            _prefix = prefix;
            _space = space;
            _lang = lang;
            _mixed = mixed;
            _toCData = false;
            _htmlElementProps = null;
        }

        internal bool FindPrefix(string urn, out string prefix)
        {
            Debug.Assert(urn != null);

            for (NamespaceDecl? scope = this.scopes; scope != null; scope = scope.Next)
            {
                if (Ref.Equal(scope.Uri, urn) &&
                    scope.Prefix != null &&
                    scope.Prefix.Length > 0)
                {
                    prefix = scope.Prefix;
                    return true;
                }
            }

            prefix = string.Empty;
            return false;
        }
    }
}
