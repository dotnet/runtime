// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Xml;

    internal sealed class NamespaceDecl
    {
        private string _prefix;
        private string _nsUri;
        private string _prevDefaultNsUri;
        private NamespaceDecl? _next;

        internal string Prefix
        {
            get { return _prefix; }
        }

        internal string Uri
        {
            get { return _nsUri; }
        }

        internal string PrevDefaultNsUri
        {
            get { return _prevDefaultNsUri; }
        }

        internal NamespaceDecl? Next
        {
            get { return _next; }
        }

        internal NamespaceDecl(string prefix, string nsUri, string prevDefaultNsUri, NamespaceDecl? next)
        {
            Init(prefix, nsUri, prevDefaultNsUri, next);
        }

        [MemberNotNull(nameof(_prefix))]
        [MemberNotNull(nameof(_nsUri))]
        [MemberNotNull(nameof(_prevDefaultNsUri))]
        internal void Init(string prefix, string nsUri, string prevDefaultNsUri, NamespaceDecl? next)
        {
            _prefix = prefix;
            _nsUri = nsUri;
            _prevDefaultNsUri = prevDefaultNsUri;
            _next = next;
        }
    }
}
