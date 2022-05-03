// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml
{
    public class XmlNamespaceManager : IXmlNamespaceResolver, IEnumerable
    {
        private struct NamespaceDeclaration
        {
            public string prefix;
            public string? uri;
            public int scopeId;
            public int previousNsIndex;

            public void Set(string prefix, string uri, int scopeId, int previousNsIndex)
            {
                this.prefix = prefix;
                this.uri = uri;
                this.scopeId = scopeId;
                this.previousNsIndex = previousNsIndex;
            }
        }

        // array with namespace declarations
        private NamespaceDeclaration[]? _nsdecls;

        // index of last declaration
        private int _lastDecl;

        // name table
        private readonly XmlNameTable? _nameTable;

        // ID (depth) of the current scope
        private int _scopeId;

        // hash table for faster lookup when there is lots of namespaces
        private Dictionary<string, int>? _hashTable;
        private bool _useHashtable;

        // atomized prefixes for "xml" and "xmlns"
        private readonly string? _xml;
        private readonly string? _xmlNs;

        // Constants
        private const int MinDeclsCountForHashtable = 16;

        internal XmlNamespaceManager()
        {
        }

        public XmlNamespaceManager(XmlNameTable nameTable)
        {
            _nameTable = nameTable;
            _xml = nameTable.Add("xml");
            _xmlNs = nameTable.Add("xmlns");

            _nsdecls = new NamespaceDeclaration[8];
            string emptyStr = nameTable.Add(string.Empty);
            _nsdecls[0].Set(emptyStr, emptyStr, -1, -1);
            _nsdecls[1].Set(_xmlNs, nameTable.Add(XmlReservedNs.NsXmlNs), -1, -1);
            _nsdecls[2].Set(_xml, nameTable.Add(XmlReservedNs.NsXml), 0, -1);
            _lastDecl = 2;
            _scopeId = 1;
        }

        public virtual XmlNameTable? NameTable
        {
            get
            {
                return _nameTable;
            }
        }

        public virtual string DefaultNamespace
        {
            get
            {
                string? defaultNs = LookupNamespace(string.Empty);
                return (defaultNs == null) ? string.Empty : defaultNs;
            }
        }

        public virtual void PushScope()
        {
            _scopeId++;
        }

        public virtual bool PopScope()
        {
            int decl = _lastDecl;
            if (_scopeId == 1)
            {
                return false;
            }

            Debug.Assert(_nsdecls != null);

            while (_nsdecls[decl].scopeId == _scopeId)
            {
                if (_useHashtable)
                {
                    Debug.Assert(_hashTable != null);
                    _hashTable[_nsdecls[decl].prefix] = _nsdecls[decl].previousNsIndex;
                }

                decl--;
                Debug.Assert(decl >= 2);
            }

            _lastDecl = decl;
            _scopeId--;
            return true;
        }

        public virtual void AddNamespace(string prefix, [StringSyntax(StringSyntaxAttribute.Uri)] string uri)
        {
            ArgumentNullException.ThrowIfNull(prefix);
            ArgumentNullException.ThrowIfNull(uri);

            Debug.Assert(_nameTable != null);
            Debug.Assert(_nsdecls != null);
            prefix = _nameTable.Add(prefix);
            uri = _nameTable.Add(uri);

            if ((Ref.Equal(_xml, prefix) && !uri.Equals(XmlReservedNs.NsXml)))
            {
                throw new ArgumentException(SR.Xml_XmlPrefix);
            }
            if (Ref.Equal(_xmlNs, prefix))
            {
                throw new ArgumentException(SR.Xml_XmlnsPrefix);
            }

            int declIndex = LookupNamespaceDecl(prefix);
            int previousDeclIndex = -1;
            if (declIndex != -1)
            {
                if (_nsdecls[declIndex].scopeId == _scopeId)
                {
                    // redefine if in the same scope
                    _nsdecls[declIndex].uri = uri;
                    return;
                }
                else
                {
                    // otherwise link
                    previousDeclIndex = declIndex;
                }
            }

            // set new namespace declaration
            if (_lastDecl == _nsdecls.Length - 1)
            {
                NamespaceDeclaration[] newNsdecls = new NamespaceDeclaration[_nsdecls.Length * 2];
                Array.Copy(_nsdecls, newNsdecls, _nsdecls.Length);
                _nsdecls = newNsdecls;
            }

            _nsdecls[++_lastDecl].Set(prefix, uri, _scopeId, previousDeclIndex);

            // add to hashTable
            if (_useHashtable)
            {
                Debug.Assert(_hashTable != null);
                _hashTable[prefix] = _lastDecl;
            }
            // or create a new hashTable if the threshold has been reached
            else if (_lastDecl >= MinDeclsCountForHashtable)
            {
                // add all to hash table
                Debug.Assert(_hashTable == null);
                _hashTable = new Dictionary<string, int>(_lastDecl);
                for (int i = 0; i <= _lastDecl; i++)
                {
                    _hashTable[_nsdecls[i].prefix] = i;
                }

                _useHashtable = true;
            }
        }

        public virtual void RemoveNamespace(string prefix, [StringSyntax(StringSyntaxAttribute.Uri)] string uri)
        {
            ArgumentNullException.ThrowIfNull(prefix);
            ArgumentNullException.ThrowIfNull(uri);

            Debug.Assert(_nsdecls != null);

            int declIndex = LookupNamespaceDecl(prefix);
            while (declIndex != -1)
            {
                if (string.Equals(_nsdecls[declIndex].uri, uri) && _nsdecls[declIndex].scopeId == _scopeId)
                {
                    _nsdecls[declIndex].uri = null;
                }
                declIndex = _nsdecls[declIndex].previousNsIndex;
            }
        }

        public virtual IEnumerator GetEnumerator()
        {
            Debug.Assert(_nsdecls != null);

            Dictionary<string, string> prefixes = new Dictionary<string, string>(_lastDecl + 1);
            for (int thisDecl = 0; thisDecl <= _lastDecl; thisDecl++)
            {
                if (_nsdecls[thisDecl].uri != null)
                {
                    prefixes[_nsdecls[thisDecl].prefix] = _nsdecls[thisDecl].prefix;
                }
            }
            return prefixes.Keys.GetEnumerator();
        }

        // This pragma disables a warning that the return type is not CLS-compliant, but generics are part of CLS in Whidbey.
#pragma warning disable 3002
        public virtual IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
#pragma warning restore 3002
        {
            Debug.Assert(_nsdecls != null);

            int i = 0;
            switch (scope)
            {
                case XmlNamespaceScope.All:
                    i = 2;
                    break;
                case XmlNamespaceScope.ExcludeXml:
                    i = 3;
                    break;
                case XmlNamespaceScope.Local:
                    i = _lastDecl;
                    while (_nsdecls[i].scopeId == _scopeId)
                    {
                        i--;
                        Debug.Assert(i >= 2);
                    }
                    i++;
                    break;
            }

            Dictionary<string, string> dict = new Dictionary<string, string>(_lastDecl - i + 1);
            for (; i <= _lastDecl; i++)
            {
                string? prefix = _nsdecls[i].prefix;
                string? uri = _nsdecls[i].uri;
                Debug.Assert(prefix != null);

                if (uri != null)
                {
                    if (uri.Length > 0 || prefix.Length > 0 || scope == XmlNamespaceScope.Local)
                    {
                        dict[prefix] = uri;
                    }
                    else
                    {
                        // default namespace redeclared to "" -> remove from list for all scopes other than local
                        dict.Remove(prefix);
                    }
                }
            }

            return dict;
        }

        public virtual string? LookupNamespace(string prefix)
        {
            Debug.Assert(_nsdecls != null);
            int declIndex = LookupNamespaceDecl(prefix);
            return (declIndex == -1) ? null : _nsdecls[declIndex].uri;
        }

        private int LookupNamespaceDecl(string prefix)
        {
            Debug.Assert(_nsdecls != null);

            if (_useHashtable)
            {
                Debug.Assert(_hashTable != null);
                int declIndex;
                if (_hashTable.TryGetValue(prefix, out declIndex))
                {
                    while (declIndex != -1 && _nsdecls[declIndex].uri == null)
                    {
                        declIndex = _nsdecls[declIndex].previousNsIndex;
                    }
                    return declIndex;
                }
                return -1;
            }
            else
            {
                // First assume that prefix is atomized
                for (int thisDecl = _lastDecl; thisDecl >= 0; thisDecl--)
                {
                    if ((object)_nsdecls[thisDecl].prefix == (object)prefix && _nsdecls[thisDecl].uri != null)
                    {
                        return thisDecl;
                    }
                }

                // Non-atomized lookup
                for (int thisDecl = _lastDecl; thisDecl >= 0; thisDecl--)
                {
                    if (string.Equals(_nsdecls[thisDecl].prefix, prefix) && _nsdecls[thisDecl].uri != null)
                    {
                        return thisDecl;
                    }
                }
            }
            return -1;
        }

        public virtual string? LookupPrefix([StringSyntax(StringSyntaxAttribute.Uri)] string uri)
        {
            Debug.Assert(_nsdecls != null);

            // Don't assume that prefix is atomized
            for (int thisDecl = _lastDecl; thisDecl >= 0; thisDecl--)
            {
                if (string.Equals(_nsdecls[thisDecl].uri, uri))
                {
                    string prefix = _nsdecls[thisDecl].prefix;
                    if (string.Equals(LookupNamespace(prefix), uri))
                    {
                        return prefix;
                    }
                }
            }
            return null;
        }

        public virtual bool HasNamespace(string prefix)
        {
            Debug.Assert(_nsdecls != null);

            // Don't assume that prefix is atomized
            for (int thisDecl = _lastDecl; _nsdecls[thisDecl].scopeId == _scopeId; thisDecl--)
            {
                if (string.Equals(_nsdecls[thisDecl].prefix, prefix) && _nsdecls[thisDecl].uri != null)
                {
                    if (prefix.Length > 0 || _nsdecls[thisDecl].uri!.Length > 0)
                    {
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

        internal bool GetNamespaceDeclaration(int idx, [NotNullWhen(true)] out string? prefix, out string? uri)
        {
            idx = _lastDecl - idx;
            if (idx < 0)
            {
                prefix = uri = null;
                return false;
            }

            Debug.Assert(_nsdecls != null);
            prefix = _nsdecls[idx].prefix;
            uri = _nsdecls[idx].uri;

            return true;
        }
    } //XmlNamespaceManager
}
