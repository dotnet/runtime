// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Internal.Runtime.CompilerServices;

namespace System.Collections.Concurrent
{
    internal abstract partial class DictionaryImpl<TKey, TKeyStore, TValue>
        : DictionaryImpl<TKey, TValue>
    {
        internal override Snapshot GetSnapshot()
        {
            return new SnapshotImpl(this);
        }

        private class SnapshotImpl : Snapshot
        {
            private readonly DictionaryImpl<TKey, TKeyStore, TValue> _table;

            public SnapshotImpl(DictionaryImpl<TKey, TKeyStore, TValue> dict)
            {
                this._table = dict;

                // linearization point.
                // if table is quiescent and has no copy in progress,
                // we can simply iterate over its table.
                while (true)
                {
                    if (_table._newTable == null)
                    {
                        break;
                    }

                    // there is a copy in progress, finish it and try again
                    _table.HelpCopy(copy_all: true);
                    this._table = (DictionaryImpl<TKey, TKeyStore, TValue>)(this._table._topDict._table);
                }

                // Warm-up the iterator
                MoveNext();
                _curValue = NULLVALUE;
            }

            public override int Count => _table.Count;

            public override bool MoveNext()
            {
                if (_nextV == NULLVALUE)
                {
                    return false;
                }

                _curKey = _nextK;
                _curValue = _nextV;
                _nextV = NULLVALUE;

                var entries = this._table._entries;
                while (_idx < entries.Length)
                {  // Scan array
                    var nextEntry = entries[_idx++];

                    if (nextEntry.value != null)
                    {
                        var nextKstore = nextEntry.key;
                        if (nextKstore == null)
                        {
                            // slot was deleted.
                            continue;
                        }

                        var nextK = _table.keyFromEntry(nextKstore);

                        object nextV = _table.TryGetValue(nextK);
                        if (nextV != null)
                        {
                            _nextK = nextK;

                            // PERF: this would be nice to have as a helper,
                            // but it does not get inlined
                            if (default(TValue) == null && nextV == NULLVALUE)
                            {
                                _nextV = default(TValue);
                            }
                            else
                            {
                                _nextV = nextV;
                            }

                            break;
                        }
                    }
                }

                return _curValue != NULLVALUE;
            }

            public override void Reset()
            {
                _idx = 0;
                _curKey = _nextK = default(TKey);
                _curValue = _nextV = null;
                MoveNext();
                _curValue = NULLVALUE;
            }
        }
    }
}
