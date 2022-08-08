// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using QilName = System.Xml.Xsl.Qil.QilName;

namespace System.Xml.Xsl.Xslt
{
    // Compiler scope manager keeps track of
    //   Variable declarations
    //   Namespace declarations
    //   Extension and excluded namespaces
    internal sealed class CompilerScopeManager<V>
    {
        public enum ScopeFlags
        {
            BackwardCompatibility = 0x1,
            ForwardCompatibility = 0x2,
            CanHaveApplyImports = 0x4,
            NsDecl = 0x10,                   // NS declaration
            NsExcl = 0x20,                   // NS Extencion (null for ExcludeAll)
            Variable = 0x40,

            CompatibilityFlags = BackwardCompatibility | ForwardCompatibility,
            InheritedFlags = CompatibilityFlags | CanHaveApplyImports,
            ExclusiveFlags = NsDecl | NsExcl | Variable
        }

        public struct ScopeRecord
        {
            public int scopeCount;
            public ScopeFlags flags;
            public string? ncName;     // local-name for variable, prefix for namespace, null for extension or excluded namespace
            public string? nsUri;      // namespace uri
            [AllowNull]
            public V value;      // value for variable, null for namespace

            // Exactly one of these three properties is true for every given record
            public bool IsVariable { get { return (flags & ScopeFlags.Variable) != 0; } }
            public bool IsNamespace { get { return (flags & ScopeFlags.NsDecl) != 0; } }
            //          public bool IsExNamespace   { get { return (flags & ScopeFlags.NsExcl  ) != 0; } }
        }

        // Number of predefined records minus one
        private const int LastPredefRecord = 0;

        private ScopeRecord[] _records = new ScopeRecord[32];
        private int _lastRecord = LastPredefRecord;

        // This is cache of records[lastRecord].scopeCount field;
        // most often we will have PushScope()/PopScope pare over the same record.
        // It has sence to avoid adresing this field through array access.
        private int _lastScopes;

        public CompilerScopeManager()
        {
            // The prefix 'xml' is by definition bound to the namespace name http://www.w3.org/XML/1998/namespace
            _records[0].flags = ScopeFlags.NsDecl;
            _records[0].ncName = "xml";
            _records[0].nsUri = XmlReservedNs.NsXml;
        }

        public CompilerScopeManager(KeywordsTable atoms)
        {
            _records[0].flags = ScopeFlags.NsDecl;
            _records[0].ncName = atoms.Xml;
            _records[0].nsUri = atoms.UriXml;
        }

        public void EnterScope()
        {
            _lastScopes++;
        }

        public void ExitScope()
        {
            if (0 < _lastScopes)
            {
                _lastScopes--;
            }
            else
            {
                while (_records[--_lastRecord].scopeCount == 0)
                {
                }
                _lastScopes = _records[_lastRecord].scopeCount;
                _lastScopes--;
            }
        }

        [Conditional("DEBUG")]
        public void CheckEmpty()
        {
            ExitScope();
            Debug.Assert(_lastRecord == 0 && _lastScopes == 0, "PushScope() and PopScope() calls are unbalanced");
        }

        // returns true if ns decls was added to scope
        public bool EnterScope([NotNullWhen(true)] NsDecl? nsDecl)
        {
            _lastScopes++;

            bool hasNamespaces = false;
            bool excludeAll = false;
            for (; nsDecl != null; nsDecl = nsDecl.Prev)
            {
                if (nsDecl.NsUri == null)
                {
                    Debug.Assert(nsDecl.Prefix == null, "NS may be null only when prefix is null where it is used for extension-element-prefixes='#all'");
                    excludeAll = true;
                }
                else if (nsDecl.Prefix == null)
                {
                    AddExNamespace(nsDecl.NsUri);
                }
                else
                {
                    hasNamespaces = true;
                    AddNsDeclaration(nsDecl.Prefix, nsDecl.NsUri);
                }
            }
            if (excludeAll)
            {
                // #all should be on the top of the stack, because all NSs on this element should be excluded as well
                AddExNamespace(null);
            }
            return hasNamespaces;
        }

        private void AddRecord()
        {
            // Store cached fields:
            _records[_lastRecord].scopeCount = _lastScopes;
            // Extend record buffer:
            if (++_lastRecord == _records.Length)
            {
                ScopeRecord[] newRecords = new ScopeRecord[_lastRecord * 2];
                Array.Copy(_records, newRecords, _lastRecord);
                _records = newRecords;
            }
            // reset scope count:
            _lastScopes = 0;
        }

        private void AddRecord(ScopeFlags flag, string? ncName, string? uri, [AllowNull] V value)
        {
            Debug.Assert(flag == (flag & ScopeFlags.ExclusiveFlags) && (flag & (flag - 1)) == 0 && flag != 0, "One exclusive flag");
            Debug.Assert(uri != null || ncName == null, "null, null means exclude '#all'");

            ScopeFlags flags = _records[_lastRecord].flags;
            bool canReuseLastRecord = (_lastScopes == 0) && (flags & ScopeFlags.ExclusiveFlags) == 0;
            if (!canReuseLastRecord)
            {
                AddRecord();
                flags &= ScopeFlags.InheritedFlags;
            }

            _records[_lastRecord].flags = flags | flag;
            _records[_lastRecord].ncName = ncName;
            _records[_lastRecord].nsUri = uri;
            _records[_lastRecord].value = value;
        }

        private void SetFlag(ScopeFlags flag, bool value)
        {
            Debug.Assert(flag == (flag & ScopeFlags.InheritedFlags) && (flag & (flag - 1)) == 0 && flag != 0, "one inherited flag");
            ScopeFlags flags = _records[_lastRecord].flags;
            if (((flags & flag) != 0) != value)
            {
                // lastScopes == records[lastRecord].scopeCount;          // we know this because we are cashing it.
                bool canReuseLastRecord = _lastScopes == 0;                // last record is from last scope
                if (!canReuseLastRecord)
                {
                    AddRecord();
                    flags &= ScopeFlags.InheritedFlags;
                }
                if (flag == ScopeFlags.CanHaveApplyImports)
                {
                    flags ^= flag;
                }
                else
                {
                    flags &= ~ScopeFlags.CompatibilityFlags;
                    if (value)
                    {
                        flags |= flag;
                    }
                }
                _records[_lastRecord].flags = flags;
            }
            Debug.Assert((_records[_lastRecord].flags & ScopeFlags.CompatibilityFlags) != ScopeFlags.CompatibilityFlags,
                "BackwardCompatibility and ForwardCompatibility flags are mutually exclusive"
            );
        }

        // Add variable to the current scope.  Returns false in case of duplicates.
        public void AddVariable(QilName varName, V value)
        {
            Debug.Assert(varName.LocalName != null && varName.NamespaceUri != null);
            AddRecord(ScopeFlags.Variable, varName.LocalName, varName.NamespaceUri, value);
        }

        // Since the prefix might be redefined in an inner scope, we search in descending order in [to, from]
        // If interval is empty (from < to), the function returns null.
        private string? LookupNamespace(string prefix, int from, int to)
        {
            Debug.Assert(prefix != null);
            for (int record = from; to <= record; --record)
            {
                string? recPrefix, recNsUri;
                ScopeFlags flags = GetName(ref _records[record], out recPrefix, out recNsUri);
                if (
                    (flags & ScopeFlags.NsDecl) != 0 &&
                    recPrefix == prefix
                )
                {
                    return recNsUri;
                }
            }
            return null;
        }

        public string? LookupNamespace(string prefix)
        {
            return LookupNamespace(prefix, _lastRecord, 0);
        }

        private static ScopeFlags GetName(ref ScopeRecord re, out string? prefix, out string? nsUri)
        {
            prefix = re.ncName;
            nsUri = re.nsUri;
            return re.flags;
        }

        public void AddNsDeclaration(string prefix, string nsUri)
        {
            AddRecord(ScopeFlags.NsDecl, prefix, nsUri, default(V));
        }

        public void AddExNamespace(string? nsUri)
        {
            AddRecord(ScopeFlags.NsExcl, null, nsUri, default(V));
        }

        public bool IsExNamespace(string nsUri)
        {
            Debug.Assert(nsUri != null);
            int exAll = 0;
            for (int record = _lastRecord; 0 <= record; record--)
            {
                string? recPrefix, recNsUri;
                ScopeFlags flags = GetName(ref _records[record], out recPrefix, out recNsUri);
                if ((flags & ScopeFlags.NsExcl) != 0)
                {
                    Debug.Assert(recPrefix == null);
                    if (recNsUri == nsUri)
                    {
                        return true;               // This namespace is excluded
                    }
                    if (recNsUri == null)
                    {
                        exAll = record;            // #all namespaces below are excluded
                    }
                }
                else if (
                  exAll != 0 &&
                  (flags & ScopeFlags.NsDecl) != 0 &&
                  recNsUri == nsUri
              )
                {
                    // We need to check that this namespace wasn't undefined before last "#all"
                    bool undefined = false;
                    for (int prev = record + 1; prev < exAll; prev++)
                    {
                        string? prevPrefix;
                        GetName(ref _records[prev], out prevPrefix, out _);
                        if (
                            (flags & ScopeFlags.NsDecl) != 0 &&
                            prevPrefix == recPrefix
                        )
                        {
                            // We don't care if records[prev].nsUri == records[record].nsUri.
                            // In this case the namespace was already undefined above.
                            undefined = true;
                            break;
                        }
                    }
                    if (!undefined)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private int SearchVariable(string localName, string uri)
        {
            Debug.Assert(localName != null);
            for (int record = _lastRecord; 0 <= record; --record)
            {
                string? recLocal, recNsUri;
                ScopeFlags flags = GetName(ref _records[record], out recLocal, out recNsUri);
                if (
                    (flags & ScopeFlags.Variable) != 0 &&
                    recLocal == localName &&
                    recNsUri == uri
                )
                {
                    return record;
                }
            }
            return -1;
        }

        [return: MaybeNull]
        public V LookupVariable(string localName, string uri)
        {
            int record = SearchVariable(localName, uri);
            return (record < 0) ? default(V) : _records[record].value;
        }

        public bool IsLocalVariable(string localName, string uri)
        {
            int record = SearchVariable(localName, uri);
            while (0 <= --record)
            {
                if (_records[record].scopeCount != 0)
                {
                    return true;
                }
            }
            return false;
        }

        public bool ForwardCompatibility
        {
            get { return (_records[_lastRecord].flags & ScopeFlags.ForwardCompatibility) != 0; }
            set { SetFlag(ScopeFlags.ForwardCompatibility, value); }
        }

        public bool BackwardCompatibility
        {
            get { return (_records[_lastRecord].flags & ScopeFlags.BackwardCompatibility) != 0; }
            set { SetFlag(ScopeFlags.BackwardCompatibility, value); }
        }

        public bool CanHaveApplyImports
        {
            get { return (_records[_lastRecord].flags & ScopeFlags.CanHaveApplyImports) != 0; }
            set { SetFlag(ScopeFlags.CanHaveApplyImports, value); }
        }

        internal System.Collections.Generic.IEnumerable<ScopeRecord> GetActiveRecords()
        {
            int currentRecord = _lastRecord + 1;
            // This logic comes from NamespaceEnumerator.MoveNext but also returns variables
            while (LastPredefRecord < --currentRecord)
            {
                if (_records[currentRecord].IsNamespace)
                {
                    // This is a namespace declaration
                    if (LookupNamespace(_records[currentRecord].ncName!, _lastRecord, currentRecord + 1) != null)
                    {
                        continue;
                    }
                    // Its prefix has not been redefined later in [currentRecord + 1, lastRecord]
                }
                yield return _records[currentRecord];
            }
        }

        public NamespaceEnumerator GetEnumerator()
        {
            return new NamespaceEnumerator(this);
        }

        internal struct NamespaceEnumerator
        {
            private readonly CompilerScopeManager<V> _scope;
            private readonly int _lastRecord;
            private int _currentRecord;

            public NamespaceEnumerator(CompilerScopeManager<V> scope)
            {
                _scope = scope;
                _lastRecord = scope._lastRecord;
                _currentRecord = _lastRecord + 1;
            }

            public bool MoveNext()
            {
                while (LastPredefRecord < --_currentRecord)
                {
                    if (_scope._records[_currentRecord].IsNamespace)
                    {
                        // This is a namespace declaration
                        if (_scope.LookupNamespace(_scope._records[_currentRecord].ncName!, _lastRecord, _currentRecord + 1) == null)
                        {
                            // Its prefix has not been redefined later in [currentRecord + 1, lastRecord]
                            return true;
                        }
                    }
                }
                return false;
            }

            public ScopeRecord Current
            {
                get
                {
                    Debug.Assert(LastPredefRecord <= _currentRecord && _currentRecord <= _scope._lastRecord, "MoveNext() either was not called or returned false");
                    Debug.Assert(_scope._records[_currentRecord].IsNamespace);
                    return _scope._records[_currentRecord];
                }
            }
        }
    }
}
