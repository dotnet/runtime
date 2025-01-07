// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace System.DirectoryServices.AccountManagement
{
    public class PrincipalSearcher : IDisposable
    {
        //
        // Public constructors
        //
        public PrincipalSearcher()
        {
            SetDefaultPageSizeForContext();
        }

        public PrincipalSearcher(Principal queryFilter)
        {
            if (null == queryFilter)
                throw new ArgumentException(null, nameof(queryFilter));

            _ctx = queryFilter.Context;
            this.QueryFilter = queryFilter; // use property to enforce "no persisted principals" check

            SetDefaultPageSizeForContext();
        }

        //
        // Public properties
        //
        public PrincipalContext Context
        {
            get
            {
                CheckDisposed();

                return _ctx;
            }
        }

        public Principal QueryFilter
        {
            get
            {
                CheckDisposed();

                return _qbeFilter;
            }

            set
            {
                if (null == value)
                    throw new ArgumentNullException(nameof(QueryFilter));

                CheckDisposed();
                Debug.Assert(value.Context != null);

                // Make sure they're not passing in a persisted Principal object
                if ((value != null) && (!value.unpersisted))
                    throw new ArgumentException(SR.PrincipalSearcherPersistedPrincipal);

                _qbeFilter = value;
                _ctx = _qbeFilter.Context;
            }
        }

        //
        // Public methods
        //

        // Calls FindAll(false) to retrieve all matching results
        public PrincipalSearchResult<Principal> FindAll()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearcher", "Entering FindAll()");

            CheckDisposed();

            return FindAll(false);
        }

        // Calls FindAll(true) to retrieve at most one result, then retrieves the first (and only) result from the
        // FindResult<Principal> and returns it.
        public Principal FindOne()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearcher", "Entering FindOne()");

            CheckDisposed();

            using (PrincipalSearchResult<Principal> fr = FindAll(true))
            {
                FindResultEnumerator<Principal> fre = (FindResultEnumerator<Principal>)fr.GetEnumerator();

                // If there's (at least) one result, return it.  Else return null.
                if (fre.MoveNext())
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearcher", "FindOne(): found a principal");
                    return (Principal)fre.Current;
                }
                else
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearcher", "FindOne(): found no principal");
                    return null;
                }
            }
        }

        // The wormhole to the native searcher underlying this PrincipalSearcher.
        // This method validates that a PrincipalContext has been set on the searcher and that the QBE
        // filter, if supplied, has no referential properties set.
        //
        // If the underlying StoreCtx does not expose a native searcher (StoreCtx.SupportsSearchNatively is false),
        // throws an exception.
        //
        // Otherwise, calls StoreCtx.PushFilterToNativeSearcher to push the current QBE filter
        // into underlyingSearcher (automatically constructing a fresh native searcher if underlyingSearcher is null),
        // and returns underlyingSearcher.
        public object GetUnderlyingSearcher()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearcher", "Entering GetUnderlyingSearcher");

            CheckDisposed();

            // We have to have a filter
            if (_qbeFilter == null)
                throw new InvalidOperationException(SR.PrincipalSearcherMustSetFilter);

            // Double-check that the Principal isn't persisted.  We don't allow them to assign a persisted
            // Principal as the filter, but they could have persisted it after assigning it to the QueryFilter
            // property.
            if (!_qbeFilter.unpersisted)
                throw new InvalidOperationException(SR.PrincipalSearcherPersistedPrincipal);

            // Validate the QBE filter: make sure it doesn't have any non-scalar properties set.
            if (HasReferentialPropertiesSet())
                throw new InvalidOperationException(SR.PrincipalSearcherNonReferentialProps);

            StoreCtx storeCtx = _ctx.QueryCtx;
            Debug.Assert(storeCtx != null);

            // The underlying context must actually support search (i.e., no MSAM/reg-SAM)
            if (storeCtx.SupportsSearchNatively == false)
                throw new InvalidOperationException(SR.PrincipalSearcherNoUnderlying);

            // We need to generate the searcher every time because the object could change
            // outside of our control.
            _underlyingSearcher = storeCtx.PushFilterToNativeSearcher(this);

            return _underlyingSearcher;
        }

        public Type GetUnderlyingSearcherType()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearcher", "Entering GetUnderlyingSearcherType");

            CheckDisposed();

            // We have to have a filter
            if (_qbeFilter == null)
                throw new InvalidOperationException(SR.PrincipalSearcherMustSetFilter);

            StoreCtx storeCtx = _ctx.QueryCtx;
            Debug.Assert(storeCtx != null);

            // The underlying context must actually support search (i.e., no MSAM/reg-SAM)
            if (storeCtx.SupportsSearchNatively == false)
                throw new InvalidOperationException(SR.PrincipalSearcherNoUnderlying);

            return storeCtx.SearcherNativeType();
        }

        public virtual void Dispose()
        {
            if (!_disposed)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearcher", "Dispose: disposing");

                if ((this.UnderlyingSearcher != null) && (this.UnderlyingSearcher is IDisposable))
                {
                    GlobalDebug.WriteLineIf(
                            GlobalDebug.Info,
                            "PrincipalSearcher",
                            "Dispose: disposing underlying searcher of type " + this.UnderlyingSearcher.GetType().ToString());

                    ((IDisposable)this.UnderlyingSearcher).Dispose();
                }

                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        //
        // Private implementation
        //
        private PrincipalContext _ctx;

        // Are we disposed?
        private bool _disposed;

        // Directly corresponds to the PrincipalSearcher.QueryFilter property.
        // Null means "return all principals".
        private Principal _qbeFilter;

        // The default page size to use.  This value is automatically set
        // whenever a PrincipalContext is assigned to this object.
        private int _pageSize;

        internal int PageSize
        {
            get { return _pageSize; }
        }

        // The underlying searcher (e.g., DirectorySearcher) corresponding to this PrincipalSearcher.
        // Set by StoreCtx. PushFilterToNativeSearcher(), based on the qbeFilter.
        // If not set, either there is no underlying searcher (SAM), or PushFilterToNativeSearcher has not
        // yet been called.
        private object _underlyingSearcher;
        internal object UnderlyingSearcher
        {
            get
            {
                return _underlyingSearcher;
            }

            set
            {
                _underlyingSearcher = value;
            }
        }

        // The core search method.
        // This method validates that a PrincipalContext has been set on the searcher and that the QBE
        // filter, if supplied, has no referential properties set.
        //
        // For the ctx.QueryCtx, calls StoreCtx.Query to perform the query and retrieve a
        // ResultSet representing the results of that query.
        // Then constructs a FindResult<Principal>, passing it the collection of one or more ResultSets.
        //
        // Returns at most one result in the FindResult<Principal> if returnOne == true, no limit on results
        // returned otherwise.
        private PrincipalSearchResult<Principal> FindAll(bool returnOne)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearcher", "Entering FindAll, returnOne=" + returnOne.ToString());

            if (_qbeFilter == null)
                throw new InvalidOperationException(SR.PrincipalSearcherMustSetFilter);
            // Double-check that the Principal isn't persisted.  We don't allow them to assign a persisted
            // Principal as the filter, but they could have persisted it after assigning it to the QueryFilter
            // property.
            if (!_qbeFilter.unpersisted)
                throw new InvalidOperationException(SR.PrincipalSearcherPersistedPrincipal);

            // Validate the QBE filter: make sure it doesn't have any non-scalar properties set.
            if (HasReferentialPropertiesSet())
                throw new InvalidOperationException(SR.PrincipalSearcherNonReferentialProps);

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearcher", "FindAll: qbeFilter is non-null and passes");

            ResultSet resultSet = _ctx.QueryCtx.Query(this, returnOne ? 1 : -1);

            PrincipalSearchResult<Principal> fr = new PrincipalSearchResult<Principal>(resultSet);
            return fr;
        }

        private void SetDefaultPageSizeForContext()
        {
            _pageSize = 0;

            if (_qbeFilter != null)
            {
                // If our context is AD-backed (has an ADStoreCtx), use pagesize of 256.
                // Otherwise, turn off paging.
                GlobalDebug.WriteLineIf(
                        GlobalDebug.Info,
                        "PrincipalSearcher",
                        "SetDefaultPageSizeForContext: type is " + _ctx.QueryCtx.GetType().ToString());

                if (_ctx.QueryCtx is ADStoreCtx)
                {
                    // Found an AD context
                    _pageSize = 256;
                }
            }

            return;
        }

        // Checks this.qbeFilter to determine if any referential properties are set
        private bool HasReferentialPropertiesSet()
        {
            // If using a null query filter, nothing to validate, as it can't have any referential
            // properties set.
            if (_qbeFilter == null)
                return false;

            // Since the QBE filter must be in the "unpersisted" state, any set properties have their changed
            // flag still set (qbeFilter.GetChangeStatusForProperty() == true).  Therefore, checking which properties
            // have been set == checking which properties have their change flag set to true.
            Debug.Assert(_qbeFilter.unpersisted);

            // Retrieve the list of referential properties for this type of Principal.
            // If this type of Principal doesn't have any, the Properties hashtable will return null.
            Type t = _qbeFilter.GetType();

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearcher", "HasReferentialPropertiesSet: using type " + t.ToString());

            ArrayList referentialProperties = (ArrayList)ReferentialProperties.Properties[t];

            if (referentialProperties != null)
            {
                foreach (string propertyName in referentialProperties)
                {
                    if (_qbeFilter.GetChangeStatusForProperty(propertyName))
                    {
                        // Property was set.
                        GlobalDebug.WriteLineIf(GlobalDebug.Warn, "PrincipalSearcher", "HasReferentialPropertiesSet: found ref property " + propertyName);
                        return true;
                    }
                }
            }

            return false;
        }

        // Checks if the principal searcher has been disposed, and throws an appropriate exception if it has.
        private void CheckDisposed()
        {
            if (_disposed)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "PrincipalSearcher", "CheckDisposed: accessing disposed object");
                throw new ObjectDisposedException(this.GetType().ToString());
            }
        }
    }
}
