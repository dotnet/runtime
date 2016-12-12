/*--
Copyright (c) 2004  Microsoft Corporation

Module Name:

    ADEntriesSet.cs

Abstract:

    Implements the ADEntriesSet ResultSet class.
    
History:

    25-May-2004    MattRim     Created

--*/
 
using System;
using System.Diagnostics;
using System.Collections;
using System.Globalization;
using System.DirectoryServices;

namespace System.DirectoryServices.AccountManagement
{
    // <SecurityKernel Critical="True" Ring="0">
    // <Asserts Name="Declarative: [DirectoryServicesPermission(SecurityAction.Assert, Unrestricted = true)]" />
    // </SecurityKernel>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Assert, Unrestricted=true)]
    class ADEntriesSet : ResultSet
    {
        SearchResultCollection searchResults;
        ADStoreCtx storeCtx;

        IEnumerator enumerator;
        SearchResult current = null;
        bool endReached = false;

        bool disposed = false;

        object discriminant = null;

        internal ADEntriesSet(SearchResultCollection src, ADStoreCtx storeCtx)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADEntriesSet", "Ctor");
            
            this.searchResults = src;
            this.storeCtx = storeCtx;
            this.enumerator = src.GetEnumerator();
        }

    
        internal ADEntriesSet(SearchResultCollection src, ADStoreCtx storeCtx, object discriminant) : this(src, storeCtx)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADEntriesSet", "Ctor");
            
            this.discriminant = discriminant;
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
                Debug.Assert(this.endReached == false && this.current != null);

                return ADUtils.SearchResultAsPrincipal(this.current, this.storeCtx, this.discriminant);
    	    }
	    }


    	// Advance the enumerator to the next principal in the result set, pulling in additional pages
    	// of results as needed.
    	// Returns true if successful, false if no more results to return.
    	override internal bool MoveNext()
    	{
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADEntriesSet", "MoveNext");
    	
            Debug.Assert(this.enumerator != null);

            bool f = this.enumerator.MoveNext();

            if (f)
            {
                this.current = (SearchResult)this.enumerator.Current;
            }
            else
            {
                endReached = true;
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
    	
            this.endReached = false;
            this.current = null;

            if (this.enumerator != null)
                this.enumerator.Reset();
    	}


    	// IDisposable implementation
        public override void Dispose()
        {

            try
            {
                if (!this.disposed)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADEntriesSet", "Dispose: disposing");            
                
                    searchResults.Dispose();
                    
                    this.disposed = true;
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
