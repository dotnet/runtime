// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Xml;
using System.Collections;

namespace System.Xml.Xsl.Xslt
{
    internal class OutputScopeManager
    {
        public struct ScopeReord
        {
            public int scopeCount;
            public string prefix;
            public string nsUri;
        }
        private ScopeReord[] _records = new ScopeReord[32];
        private int _lastRecord = 0;
        private int _lastScopes = 0;  // This is cash of records[lastRecord].scopeCount field;
                                      // most often we will have PushScope()/PopScope pare over the same record.
                                      // It has sence to avoid adresing this field through array access.

        public OutputScopeManager()
        {
            Reset();
        }

        public void Reset()
        {
            //          AddNamespace(null, null);                  --  lookup barier
            _records[0].prefix = null;
            _records[0].nsUri = null;
            PushScope();
        }

        public void PushScope()
        {
            _lastScopes++;
        }

        public void PopScope()
        {
            if (0 < _lastScopes)
            {
                _lastScopes--;
            }
            else
            {
                while (_records[--_lastRecord].scopeCount == 0) ;
                _lastScopes = _records[_lastRecord].scopeCount;
                _lastScopes--;
            }
        }

        // This can be ns declaration or ns exclussion. Las one when prefix == null;
        public void AddNamespace(string prefix, string uri)
        {
            Debug.Assert(prefix != null);
            Debug.Assert(uri != null);
            //            uri = nameTable.Add(uri);
            AddRecord(prefix, uri);
        }

        private void AddRecord(string prefix, string uri)
        {
            _records[_lastRecord].scopeCount = _lastScopes;
            _lastRecord++;
            if (_lastRecord == _records.Length)
            {
                ScopeReord[] newRecords = new ScopeReord[_lastRecord * 2];
                Array.Copy(_records, 0, newRecords, 0, _lastRecord);
                _records = newRecords;
            }
            _lastScopes = 0;
            _records[_lastRecord].prefix = prefix;
            _records[_lastRecord].nsUri = uri;
        }

        // There are some cases where we can't predict namespace content. To garantee correct results we should output all 
        // literal namespaces once again. 
        // <xsl:element name="{}" namespace="{}"> all prefixes should be invalidated
        // <xsl:element name="{}" namespace="FOO"> all prefixes should be invalidated
        // <xsl:element name="foo:A" namespace="{}"> prefixe "foo" should be invalidated
        // <xsl:element name="foo:{}" namespace="{}"> prefixe "foo" should be invalidated
        // <xsl:element name="foo:A" namespace="FOO"> no invalidations reqired
        // <xsl:attribute name="{}" namespace="FOO"> all prefixes should be invalidated but not default ""
        // <xsl:attribute name="foo:A" namespace="{}"> all prefixes should be invalidated but not default ""
        // <xsl:element name="foo:A" namespace="FOO"> We can try to invalidate only foo prefix, but there to many thinks to consider here.
        //                                            So for now if attribute has non-null namespace it invalidates all prefixes in the
        //                                            scope of its element.
        //
        // <xsl:copy-of select="@*|namespace::*"> all prefixes are invalidated for the current element scope
        // <xsl:copy-of select="/|*|text()|etc."> no invalidations needed
        // <xsl:copy> if the node is either attribute or namespace, all prefixes are invalidated for the current element scope
        //            if the node is element, new scope is created, and all prefixes are invalidated
        //            otherwise, no invalidations needed

        //// We need following methods:
        //public void InvalidatePrefix(string prefix) {
        //    Debug.Assert(prefix != null);
        //    if (LookupNamespace(prefix) == null) { // This is optimisation. May be better just add this record?
        //        return;                            
        //    }
        //    AddRecord(prefix, null);
        //}

        public void InvalidateAllPrefixes()
        {
            if (_records[_lastRecord].prefix == null)
            {
                return;                            // Averything was invalidated already. Nothing to do.
            }
            AddRecord(null, null);
        }

        public void InvalidateNonDefaultPrefixes()
        {
            string defaultNs = LookupNamespace(string.Empty);
            if (defaultNs == null)
            {             // We don't know default NS anyway.
                InvalidateAllPrefixes();
            }
            else
            {
                if (
                    _records[_lastRecord].prefix.Length == 0 &&
                    _records[_lastRecord - 1].prefix == null
                )
                {
                    return;                       // Averything was already done
                }
                AddRecord(null, null);
                AddRecord(string.Empty, defaultNs);
            }
        }

        public string LookupNamespace(string prefix)
        {
            Debug.Assert(prefix != null);
            for (
                int record = _lastRecord;              // from last record 
                 _records[record].prefix != null;      // till lookup barrier
                --record                             // in reverce direction
            )
            {
                Debug.Assert(0 < record, "first record is lookup barrier, so we don't need to check this condition runtime");
                if (_records[record].prefix == prefix)
                {
                    return _records[record].nsUri;
                }
            }
            return null;
        }
    }
}
