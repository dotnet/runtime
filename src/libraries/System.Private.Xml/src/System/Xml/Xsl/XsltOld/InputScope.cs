// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;
    using System.Collections;

    internal class InputScope : DocumentScope
    {
        private InputScope? _parent;
        private bool _forwardCompatibility;
        private bool _canHaveApplyImports;
        private Hashtable? _variables;
        private Hashtable? _extensionNamespaces;
        private Hashtable? _excludedNamespaces;

        internal InputScope? Parent
        {
            get { return _parent; }
        }

        internal Hashtable? Variables
        {
            get { return _variables; }
        }

        internal bool ForwardCompatibility
        {
            get { return _forwardCompatibility; }
            set { _forwardCompatibility = value; }
        }

        internal bool CanHaveApplyImports
        {
            get { return _canHaveApplyImports; }
            set { _canHaveApplyImports = value; }
        }

        internal InputScope(InputScope? parent)
        {
            Init(parent);
        }

        internal void Init(InputScope? parent)
        {
            this.scopes = null;
            _parent = parent;

            if (_parent is not null)
            {
                _forwardCompatibility = _parent._forwardCompatibility;
                _canHaveApplyImports = _parent._canHaveApplyImports;
            }
        }

        internal void InsertExtensionNamespace(string nspace)
        {
            if (_extensionNamespaces is null)
            {
                _extensionNamespaces = new Hashtable();
            }
            _extensionNamespaces[nspace] = null;
        }

        internal bool IsExtensionNamespace(string nspace)
        {
            if (_extensionNamespaces is null)
            {
                return false;
            }
            return _extensionNamespaces.Contains(nspace);
        }

        internal void InsertExcludedNamespace(string nspace)
        {
            if (_excludedNamespaces is null)
            {
                _excludedNamespaces = new Hashtable();
            }
            _excludedNamespaces[nspace] = null;
        }

        internal bool IsExcludedNamespace(string nspace)
        {
            if (_excludedNamespaces is null)
            {
                return false;
            }
            return _excludedNamespaces.Contains(nspace);
        }

        internal void InsertVariable(VariableAction variable)
        {
            Debug.Assert(variable is not null);

            if (_variables is null)
            {
                _variables = new Hashtable();
            }
            _variables[variable.Name!] = variable;
        }

        internal int GetVeriablesCount()
        {
            if (_variables is null)
            {
                return 0;
            }
            return _variables.Count;
        }

        public VariableAction? ResolveVariable(XmlQualifiedName qname)
        {
            for (InputScope? inputScope = this; inputScope is not null; inputScope = inputScope.Parent)
            {
                if (inputScope.Variables is not null)
                {
                    VariableAction? variable = (VariableAction?)inputScope.Variables[qname];
                    if (variable is not null)
                    {
                        return variable;
                    }
                }
            }
            return null;
        }

        public VariableAction? ResolveGlobalVariable(XmlQualifiedName qname)
        {
            InputScope? prevScope = null;
            for (InputScope? inputScope = this; inputScope is not null; inputScope = inputScope.Parent)
            {
                prevScope = inputScope;
            }
            return prevScope!.ResolveVariable(qname);
        }
    }
}
