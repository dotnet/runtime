// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Globalization;
    using System.Diagnostics;
    using System.Xml;

    internal class OutputScopeManager
    {
        private const int STACK_INCREMENT = 10;

        private HWStack _elementScopesStack;
        private string _defaultNS;
        private OutKeywords _atoms;
        private XmlNameTable _nameTable;
        private int _prefixIndex;

        internal string DefaultNamespace
        {
            get { return _defaultNS; }
        }

        internal OutputScope CurrentElementScope
        {
            get
            {
                Debug.Assert(_elementScopesStack.Peek() != null); // We adding rootElementScope to garantee this
                return (OutputScope)_elementScopesStack.Peek();
            }
        }

        internal XmlSpace XmlSpace
        {
            get { return CurrentElementScope.Space; }
        }

        internal string XmlLang
        {
            get { return CurrentElementScope.Lang; }
        }

        internal OutputScopeManager(XmlNameTable nameTable, OutKeywords atoms)
        {
            Debug.Assert(nameTable != null);
            Debug.Assert(atoms != null);

            _elementScopesStack = new HWStack(STACK_INCREMENT);
            _nameTable = nameTable;
            _atoms = atoms;
            _defaultNS = _atoms.Empty;

            // We always adding rootElementScope to garantee that CurrentElementScope != null
            // This context is active between PI and first element for example
            OutputScope rootElementScope = (OutputScope)_elementScopesStack.Push();
            if (rootElementScope == null)
            {
                rootElementScope = new OutputScope();
                _elementScopesStack.AddToTop(rootElementScope);
            }
            rootElementScope.Init(string.Empty, string.Empty, string.Empty, /*space:*/XmlSpace.None, /*lang:*/string.Empty, /*mixed:*/false);
        }

        internal void PushNamespace(string prefix, string nspace)
        {
            Debug.Assert(prefix != null);
            Debug.Assert(nspace != null);
            CurrentElementScope.AddNamespace(prefix, nspace, _defaultNS);

            if (prefix == null || prefix.Length == 0)
            {
                _defaultNS = nspace;
            }
        }

        internal void PushScope(string name, string nspace, string prefix)
        {
            Debug.Assert(name != null);
            Debug.Assert(nspace != null);
            Debug.Assert(prefix != null);
            OutputScope parentScope = CurrentElementScope;
            OutputScope elementScope = (OutputScope)_elementScopesStack.Push();

            if (elementScope == null)
            {
                elementScope = new OutputScope();
                _elementScopesStack.AddToTop(elementScope);
            }

            Debug.Assert(elementScope != null);
            elementScope.Init(name, nspace, prefix, parentScope.Space, parentScope.Lang, parentScope.Mixed);
        }

        internal void PopScope()
        {
            OutputScope elementScope = (OutputScope)_elementScopesStack.Pop();

            Debug.Assert(elementScope != null); // We adding rootElementScope to garantee this

            for (NamespaceDecl scope = elementScope.Scopes; scope != null; scope = scope.Next)
            {
                _defaultNS = scope.PrevDefaultNsUri;
            }
        }

        internal string ResolveNamespace(string prefix)
        {
            bool thisScope;
            return ResolveNamespace(prefix, out thisScope);
        }

        internal string ResolveNamespace(string prefix, out bool thisScope)
        {
            Debug.Assert(prefix != null);
            thisScope = true;

            if (prefix == null || prefix.Length == 0)
            {
                return _defaultNS;
            }
            else
            {
                if (Ref.Equal(prefix, _atoms.Xml))
                {
                    return _atoms.XmlNamespace;
                }
                else if (Ref.Equal(prefix, _atoms.Xmlns))
                {
                    return _atoms.XmlnsNamespace;
                }

                for (int i = _elementScopesStack.Length - 1; i >= 0; i--)
                {
                    Debug.Assert(_elementScopesStack[i] is OutputScope);
                    OutputScope elementScope = (OutputScope)_elementScopesStack[i];

                    string nspace = elementScope.ResolveAtom(prefix);
                    if (nspace != null)
                    {
                        thisScope = (i == _elementScopesStack.Length - 1);
                        return nspace;
                    }
                }
            }

            return null;
        }

        internal bool FindPrefix(string nspace, out string prefix)
        {
            Debug.Assert(nspace != null);
            for (int i = _elementScopesStack.Length - 1; 0 <= i; i--)
            {
                Debug.Assert(_elementScopesStack[i] is OutputScope);

                OutputScope elementScope = (OutputScope)_elementScopesStack[i];
                string pfx = null;
                if (elementScope.FindPrefix(nspace, out pfx))
                {
                    string testNspace = ResolveNamespace(pfx);
                    if (testNspace != null && Ref.Equal(testNspace, nspace))
                    {
                        prefix = pfx;
                        return true;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            prefix = null;
            return false;
        }

        internal string GeneratePrefix(string format)
        {
            string prefix;

            do
            {
                prefix = string.Format(CultureInfo.InvariantCulture, format, _prefixIndex++);
            } while (_nameTable.Get(prefix) != null);

            return _nameTable.Add(prefix);
        }
    }
}
