// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections;
using System.Globalization;
using System.DirectoryServices;

namespace System.DirectoryServices.AccountManagement
{
    internal class ADEntriesSet : ResultSet
    {
        private SearchResultCollection _searchResults;
        private ADStoreCtx _storeCtx;

        private IEnumerator _enumerator;
        private SearchResult _current = null;
        private bool _endReached = false;

        private bool _disposed = false;

        private object _discriminant = null;

        internal ADEntriesSet(SearchResultCollection src, ADStoreCtx storeCtx)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADEntriesSet", "Ctor");

            _searchResults = src;
            _storeCtx = storeCtx;
            _enumerator = src.GetEnumerator();
        }

        internal ADEntriesSet(SearchResultCollection src, ADStoreCtx storeCtx, object discriminant) : this(src, storeCtx)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADEntriesSet", "Ctor");

            _discriminant = discriminant;
        }

        // Return the principal we're positioned at as a Principal object.
        // Need to use our StoreCtx's GetAsPrincipal to convert the native object to a Principal
        override internal object CurrentAsPrincipal
        {
            get
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADEntriesSet", "CurrentAsPrincipal");

                // Since this class is only used internally, none of our code should be even calling this
                // if MoveNext returned false, or before calling MoveNext.
                Debug.Assert(_endReached == false && _current != null);

                return ADUtils.SearchResultAsPrincipal(_current, _storeCtx, _discriminant);
            }
        }

        // Advance the enumerator to the next principal in the result set, pulling in additional pages
        // of results as needed.
        // Returns true if successful, false if no more results to return.
        override internal bool MoveNext()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADEntriesSet", "MoveNext");

            Debug.Assert(_enumerator != null);

            bool f = _enumerator.MoveNext();

            if (f)
            {
                _current = (SearchResult)_enumerator.Current;
            }
            else
            {
                _endReached = true;
            }

            return f;
        }

        // Resets the enumerator to before the first result in the set.  This potentially can be an expensive
        // operation, e.g., if doing a paged search, may need to re-retrieve the first page of results.
        // As a special case, if the ResultSet is already at the very beginning, this is guaranteed to be
        // a no-op.
        override internal void Reset()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADEntriesSet", "Reset");

            _endReached = false;
            _current = null;

            if (_enumerator != null)
                _enumerator.Reset();
        }

        // IDisposable implementation
        public override void Dispose()
        {
            try
            {
                if (!_disposed)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADEntriesSet", "Dispose: disposing");

                    _searchResults.Dispose();

                    _disposed = true;
                }
            }
            finally
            {
                base.Dispose();
            }
        }
    }
}

// #endif // PAPI_AD
