// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class InputScopeManager
    {
        private InputScope? _scopeStack;
        private string _defaultNS = string.Empty;
        private readonly XPathNavigator _navigator;    // We need this nsvigator for document() function implementation

        public InputScopeManager(XPathNavigator navigator, InputScope? rootScope)
        {
            _navigator = navigator;
            _scopeStack = rootScope;
        }

        internal InputScope? CurrentScope
        {
            get { return _scopeStack; }
        }

        internal InputScope VariableScope
        {
            get
            {
                Debug.Assert(_scopeStack != null);
                Debug.Assert(_scopeStack.Parent != null);
                return _scopeStack.Parent;
            }
        }

        internal InputScopeManager Clone()
        {
            InputScopeManager manager = new InputScopeManager(_navigator, null);
            manager._scopeStack = _scopeStack;
            manager._defaultNS = _defaultNS;
            return manager;
        }

        public XPathNavigator Navigator
        {
            get { return _navigator; }
        }

        internal InputScope PushScope()
        {
            _scopeStack = new InputScope(_scopeStack);
            return _scopeStack;
        }

        internal void PopScope()
        {
            Debug.Assert(_scopeStack != null, "Push/Pop disbalance");
            if (_scopeStack == null)
            {
                return;
            }

            for (NamespaceDecl? scope = _scopeStack.Scopes; scope != null; scope = scope.Next)
            {
                _defaultNS = scope.PrevDefaultNsUri;
            }

            _scopeStack = _scopeStack.Parent;
        }

        internal void PushNamespace(string prefix, string nspace)
        {
            Debug.Assert(_scopeStack != null, "PushScope wasn't called");
            Debug.Assert(prefix != null);
            Debug.Assert(nspace != null);
            _scopeStack.AddNamespace(prefix, nspace, _defaultNS);

            if (prefix == null || prefix.Length == 0)
            {
                _defaultNS = nspace;
            }
        }

        // CompileContext

        public string DefaultNamespace
        {
            get { return _defaultNS; }
        }

        private string ResolveNonEmptyPrefix(string prefix)
        {
            Debug.Assert(_scopeStack != null, "PushScope wasn't called");
            Debug.Assert(!string.IsNullOrEmpty(prefix));
            if (prefix == "xml")
            {
                return XmlReservedNs.NsXml;
            }
            else if (prefix == "xmlns")
            {
                return XmlReservedNs.NsXmlNs;
            }

            for (InputScope? inputScope = _scopeStack; inputScope != null; inputScope = inputScope.Parent)
            {
                string? nspace = inputScope.ResolveNonAtom(prefix);
                if (nspace != null)
                {
                    return nspace;
                }
            }
            throw XsltException.Create(SR.Xslt_InvalidPrefix, prefix);
        }

        public string ResolveXmlNamespace(string prefix)
        {
            Debug.Assert(prefix != null);
            if (prefix.Length == 0)
            {
                return _defaultNS;
            }
            return ResolveNonEmptyPrefix(prefix);
        }

        public string ResolveXPathNamespace(string prefix)
        {
            Debug.Assert(prefix != null);
            if (prefix.Length == 0)
            {
                return string.Empty;
            }
            return ResolveNonEmptyPrefix(prefix);
        }

        internal void InsertExtensionNamespaces(string[] nsList)
        {
            Debug.Assert(_scopeStack != null, "PushScope wasn't called");
            Debug.Assert(nsList != null);
            for (int idx = 0; idx < nsList.Length; idx++)
            {
                _scopeStack.InsertExtensionNamespace(nsList[idx]);
            }
        }

        internal bool IsExtensionNamespace(string nspace)
        {
            Debug.Assert(_scopeStack != null, "PushScope wasn't called");
            for (InputScope? inputScope = _scopeStack; inputScope != null; inputScope = inputScope.Parent)
            {
                if (inputScope.IsExtensionNamespace(nspace))
                {
                    return true;
                }
            }
            return false;
        }

        internal void InsertExcludedNamespaces(string[] nsList)
        {
            Debug.Assert(_scopeStack != null, "PushScope wasn't called");
            Debug.Assert(nsList != null);
            for (int idx = 0; idx < nsList.Length; idx++)
            {
                _scopeStack.InsertExcludedNamespace(nsList[idx]);
            }
        }

        internal bool IsExcludedNamespace(string nspace)
        {
            Debug.Assert(_scopeStack != null, "PushScope wasn't called");
            for (InputScope? inputScope = _scopeStack; inputScope != null; inputScope = inputScope.Parent)
            {
                if (inputScope.IsExcludedNamespace(nspace))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
