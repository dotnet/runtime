/*--
Copyright (c) 2004  Microsoft Corporation

Module Name:

    SAMGroupsSet.cs

Abstract:

    Implements the SAMGroupsSet ResultSet class, used for
    enumerating the groups to which a user belongs
    
History:

    18-June-2004    MattRim     Created

--*/
 
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.DirectoryServices;

using System.Runtime.InteropServices;


namespace System.DirectoryServices.AccountManagement
{
    // <SecurityKernel Critical="True" Ring="0">
    // <Asserts Name="Declarative: [DirectoryServicesPermission(SecurityAction.Assert, Unrestricted = true)]" />
    // </SecurityKernel>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Assert, Unrestricted=true)]
    class SAMGroupsSet : ResultSet
    {
        internal SAMGroupsSet(UnsafeNativeMethods.IADsMembers iADsMembers, SAMStoreCtx storeCtx, DirectoryEntry ctxBase)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMGroupsSet", "SAMGroupsSet: creating for path={0}", ctxBase.Path);            
        
            this.groupsEnumerator = ((IEnumerable)iADsMembers).GetEnumerator();
            
            this.storeCtx = storeCtx;
            this.ctxBase = ctxBase;
        }
    
    	// Return the principal we're positioned at as a Principal object.
    	// Need to use our StoreCtx's GetAsPrincipal to convert the native object to a Principal
    	override internal object CurrentAsPrincipal
    	{
            get
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMGroupsSet", "CurrentAsPrincipal");
            
                Debug.Assert(this.current != null);

                return SAMUtils.DirectoryEntryAsPrincipal(this.current, this.storeCtx);              
            }
    	}

    	// Advance the enumerator to the next principal in the result set, pulling in additional pages
    	// of results (or ranges of attribute values) as needed.
    	// Returns true if successful, false if no more results to return.
    	override internal bool MoveNext()
    	{ 
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMGroupsSet", "MoveNext");            
    	
            this.atBeginning = false;
    	
            bool f = this.groupsEnumerator.MoveNext();

            if (f)
            {
                // Got a group.  Create a DirectoryEntry for it.
                // Clone the ctxBase to pick up its credentials, then build an appropriate path.
                UnsafeNativeMethods.IADs nativeMember = (UnsafeNativeMethods.IADs) groupsEnumerator.Current;

                // We do this, rather than using the DirectoryEntry constructor that takes a native IADs object,
                // is so the credentials get transferred to the new DirectoryEntry.  If we just use the native
                // object constructor, the native object will have the right credentials, but the DirectoryEntry
                // will have default (null) credentials, which it'll use anytime it needs to use credentials.                
                DirectoryEntry de = SDSUtils.BuildDirectoryEntry(
                                                nativeMember.ADsPath,
                                                this.storeCtx.Credentials,
                                                this.storeCtx.AuthTypes);

                this.current = de;
            }

            return f;
    	}

    	// Resets the enumerator to before the first result in the set.  This potentially can be an expensive
    	// operation, e.g., if doing a paged search, may need to re-retrieve the first page of results.
    	// As a special case, if the ResultSet is already at the very beginning, this is guaranteed to be
    	// a no-op.
    	override internal void Reset()
    	{
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMGroupsSet", "Reset");            
    	
            if (!this.atBeginning)
            {
                this.groupsEnumerator.Reset();
                this.current = null;

                this.atBeginning = true;
            }

    	}


        //
        // Private fields
        //

        IEnumerator groupsEnumerator;
        SAMStoreCtx storeCtx;
        DirectoryEntry ctxBase;

        bool atBeginning = true;

        DirectoryEntry current = null;
    }
}

// #endif
