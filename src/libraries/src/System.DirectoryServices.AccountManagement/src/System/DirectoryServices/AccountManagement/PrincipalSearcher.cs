// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace System.DirectoryServices.AccountManagement
{
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.LinkDemand, Unrestricted = true)]
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, Unrestricted = true)]
    public class PrincipalSearcher : IDisposable
    {
        //
        // Public constructors
        //
        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="SetDefaultPageSizeForContext():Void" />
        // <ReferencesCritical Name="Method: SetDefaultPageSizeForContext():Void" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Demand, Unrestricted = true)]
        public PrincipalSearcher()
        {
            SetDefaultPageSizeForContext();
        }

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="Principal.get_Context():System.DirectoryServices.AccountManagement.PrincipalContext" />
        // <SatisfiesLinkDemand Name="set_QueryFilter(Principal):Void" />
        // <SatisfiesLinkDemand Name="SetDefaultPageSizeForContext():Void" />
        // <ReferencesCritical Name="Field: ctx" Ring="1" />
        // <ReferencesCritical Name="Method: SetDefaultPageSizeForContext():Void" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        public PrincipalSearcher(Principal queryFilter)
        {
            if (null == queryFilter)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, StringResources.InvalidNullArgument, "queryFilter"));

            _ctx = queryFilter.Context;
            this.QueryFilter = queryFilter; // use property to enforce "no persisted principals" check

            SetDefaultPageSizeForContext();
        }

        //
        // Public properties
        //
        public PrincipalContext Context
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="CheckDisposed():Void" />
            // <ReferencesCritical Name="Field: ctx" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecuritySafeCritical]
            get
            {
                CheckDisposed();

                return _ctx;
            }
        }

        public Principal QueryFilter
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="CheckDisposed():Void" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {
                CheckDisposed();

                return _qbeFilter;
            }

            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="CheckDisposed():Void" />
            // <SatisfiesLinkDemand Name="Principal.get_Context():System.DirectoryServices.AccountManagement.PrincipalContext" />
            // <ReferencesCritical Name="Field: ctx" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            set
            {
                if (null == value)
                    throw new ArgumentNullException(String.Format(CultureInfo.CurrentCulture, StringResources.InvalidNullArgument, "queryFilter"));

                CheckDisposed();
                Debug.Assert(value.Context != null);

                // Make sure they're not passing in a persisted Principal object
                if ((value != null) && (!value.unpersisted))
                    throw new ArgumentException(StringResources.PrincipalSearcherPersistedPrincipal);

                _qbeFilter = value;
                _ctx = _qbeFilter.Context;
            }
        }

        //
        // Public methods
        //

        // Calls FindAll(false) to retrieve all matching results
        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="CheckDisposed():Void" />
        // <SatisfiesLinkDemand Name="FindAll(Boolean):PrincipalSearchResult`1<System.DirectoryServices.AccountManagement.Principal>" />
        // <ReferencesCritical Name="Method: FindAll(Boolean):PrincipalSearchResult`1<System.DirectoryServices.AccountManagement.Principal>" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        public PrincipalSearchResult<Principal> FindAll()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearcher", "Entering FindAll()");

            CheckDisposed();

            return FindAll(false);
        }

        // Calls FindAll(true) to retrieve at most one result, then retrieves the first (and only) result from the
        // FindResult<Principal> and returns it.
        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="CheckDisposed():Void" />
        // <SatisfiesLinkDemand Name="FindAll(Boolean):PrincipalSearchResult`1<System.DirectoryServices.AccountManagement.Principal>" />
        // <SatisfiesLinkDemand Name="PrincipalSearchResult`1<System.DirectoryServices.AccountManagement.Principal>.GetEnumerator():System.Collections.Generic.IEnumerator`1<System.DirectoryServices.AccountManagement.Principal>" />
        // <SatisfiesLinkDemand Name="FindResultEnumerator`1<System.DirectoryServices.AccountManagement.Principal>.MoveNext():System.Boolean" />
        // <SatisfiesLinkDemand Name="FindResultEnumerator`1<System.DirectoryServices.AccountManagement.Principal>.get_Current():System.DirectoryServices.AccountManagement.Principal" />
        // <ReferencesCritical Name="Method: FindAll(Boolean):PrincipalSearchResult`1<System.DirectoryServices.AccountManagement.Principal>" Ring="1" />
        // <ReferencesCritical Name="Method: FindResultEnumerator`1.MoveNext():System.Boolean" Ring="1" />
        // <ReferencesCritical Name="Method: FindResultEnumerator`1.get_Current():T" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
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
        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="CheckDisposed():Void" />
        // <SatisfiesLinkDemand Name="HasReferentialPropertiesSet():Boolean" />
        // <SatisfiesLinkDemand Name="PrincipalContext.get_QueryCtx():System.DirectoryServices.AccountManagement.StoreCtx" />
        // <ReferencesCritical Name="Method: HasReferentialPropertiesSet():Boolean" Ring="1" />
        // <ReferencesCritical Name="Field: ctx" Ring="1" />
        // <ReferencesCritical Name="Method: PrincipalContext.get_QueryCtx():System.DirectoryServices.AccountManagement.StoreCtx" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        public object GetUnderlyingSearcher()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearcher", "Entering GetUnderlyingSearcher");

            CheckDisposed();

            // We have to have a filter
            if (_qbeFilter == null)
                throw new InvalidOperationException(StringResources.PrincipalSearcherMustSetFilter);

            // Double-check that the Principal isn't persisted.  We don't allow them to assign a persisted
            // Principal as the filter, but they could have persisted it after assigning it to the QueryFilter
            // property.
            if (!_qbeFilter.unpersisted)
                throw new InvalidOperationException(StringResources.PrincipalSearcherPersistedPrincipal);

            // Validate the QBE filter: make sure it doesn't have any non-scalar properties set.
            if (HasReferentialPropertiesSet())
                throw new InvalidOperationException(StringResources.PrincipalSearcherNonReferentialProps);

            StoreCtx storeCtx = _ctx.QueryCtx;
            Debug.Assert(storeCtx != null);

            // The underlying context must actually support search (i.e., no MSAM/reg-SAM)
            if (storeCtx.SupportsSearchNatively == false)
                throw new InvalidOperationException(StringResources.PrincipalSearcherNoUnderlying);

            // We need to generate the searcher every time because the object could change
            // outside of our control.
            _underlyingSearcher = storeCtx.PushFilterToNativeSearcher(this);

            return _underlyingSearcher;
        }

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="CheckDisposed():Void" />
        // <SatisfiesLinkDemand Name="PrincipalContext.get_QueryCtx():System.DirectoryServices.AccountManagement.StoreCtx" />
        // <ReferencesCritical Name="Field: ctx" Ring="1" />
        // <ReferencesCritical Name="Method: PrincipalContext.get_QueryCtx():System.DirectoryServices.AccountManagement.StoreCtx" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        public Type GetUnderlyingSearcherType()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearcher", "Entering GetUnderlyingSearcherType");

            CheckDisposed();

            // We have to have a filter
            if (_qbeFilter == null)
                throw new InvalidOperationException(StringResources.PrincipalSearcherMustSetFilter);

            StoreCtx storeCtx = _ctx.QueryCtx;
            Debug.Assert(storeCtx != null);

            // The underlying context must actually support search (i.e., no MSAM/reg-SAM)
            if (storeCtx.SupportsSearchNatively == false)
                throw new InvalidOperationException(StringResources.PrincipalSearcherNoUnderlying);

            return storeCtx.SearcherNativeType();
        }

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="get_UnderlyingSearcher():Object" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
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
        [System.Security.SecuritySafeCritical]
        private PrincipalContext _ctx;

        // Are we disposed?
        private bool _disposed = false;

        // Directly corresponds to the PrincipalSearcher.QueryFilter property.
        // Null means "return all principals".
        private Principal _qbeFilter;

        // The default page size to use.  This value is automatically set
        // whenever a PrincipalContext is assigned to this object.
        private int _pageSize = 0;

        internal int PageSize
        {
            get { return _pageSize; }
        }

        // The underlying searcher (e.g., DirectorySearcher) corresponding to this PrincipalSearcher.
        // Set by StoreCtx. PushFilterToNativeSearcher(), based on the qbeFilter.
        // If not set, either there is no underlying searcher (SAM), or PushFilterToNativeSearcher has not
        // yet been called.
        private object _underlyingSearcher = null;
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
        [System.Security.SecuritySafeCritical]
        private PrincipalSearchResult<Principal> FindAll(bool returnOne)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearcher", "Entering FindAll, returnOne=" + returnOne.ToString());

            if (_qbeFilter == null)
                throw new InvalidOperationException(StringResources.PrincipalSearcherMustSetFilter);
            // Double-check that the Principal isn't persisted.  We don't allow them to assign a persisted
            // Principal as the filter, but they could have persisted it after assigning it to the QueryFilter
            // property.
            if (!_qbeFilter.unpersisted)
                throw new InvalidOperationException(StringResources.PrincipalSearcherPersistedPrincipal);

            // Validate the QBE filter: make sure it doesn't have any non-scalar properties set.
            if (HasReferentialPropertiesSet())
                throw new InvalidOperationException(StringResources.PrincipalSearcherNonReferentialProps);

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearcher", "FindAll: qbeFilter is non-null and passes");

            ResultSet resultSet = _ctx.QueryCtx.Query(this, returnOne ? 1 : -1);

            PrincipalSearchResult<Principal> fr = new PrincipalSearchResult<Principal>(resultSet);
            return fr;
        }

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="PrincipalContext.get_QueryCtx():System.DirectoryServices.AccountManagement.StoreCtx" />
        // <ReferencesCritical Name="Field: ctx" Ring="1" />
        // <ReferencesCritical Name="Method: PrincipalContext.get_QueryCtx():System.DirectoryServices.AccountManagement.StoreCtx" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
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
        [System.Security.SecuritySafeCritical]
        private bool HasReferentialPropertiesSet()
        {
            // If using a null query filter, nothing to validate, as it can't have any referential
            // properties set.
            if (_qbeFilter == null)
                return false;

            // Since the QBE filter must be in the "unpersisted" state, any set properties have their changed
            // flag still set (qbeFilter.GetChangeStatusForProperty() == true).  Therefore, checking which properties
            // have been set == checking which properties have their change flag set to true.
            Debug.Assert(_qbeFilter.unpersisted == true);

            // Retrieve the list of referential properties for this type of Principal.
            // If this type of Principal doesn't have any, the Properties hashtable will return null.
            Type t = _qbeFilter.GetType();

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearcher", "HasReferentialPropertiesSet: using type " + t.ToString());

            ArrayList referentialProperties = (ArrayList)ReferentialProperties.Properties[t];

            if (referentialProperties != null)
            {
                foreach (string propertyName in referentialProperties)
                {
                    if (_qbeFilter.GetChangeStatusForProperty(propertyName) == true)
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

