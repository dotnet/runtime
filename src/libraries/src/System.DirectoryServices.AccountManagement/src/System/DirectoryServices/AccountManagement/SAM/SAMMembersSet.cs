/*--
Copyright (c) 2004  Microsoft Corporation

Module Name:

    SAMMembersSet.cs

Abstract:

    Implements the SAMMembersSet ResultSet class, used for
    enumerating the members of a group.
    
History:

    17-June-2004    MattRim     Created

--*/
 
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.DirectoryServices;
using System.Text;

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
    class SAMMembersSet : BookmarkableResultSet
    {

        internal SAMMembersSet(string groupPath, UnsafeNativeMethods.IADsGroup group, bool recursive, SAMStoreCtx storeCtx, DirectoryEntry ctxBase)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, 
                                    "SAMMembersSet", 
                                    "SAMMembersSet: groupPath={0}, recursive={1}, base={2}",
                                    groupPath,
                                    recursive,
                                    ctxBase.Path);
        
            this.storeCtx = storeCtx;
            this.ctxBase = ctxBase;

            this.group = group;
            this.originalGroup = group;
            this.recursive = recursive;

            this.groupsVisited.Add(groupPath);    // so we don't revisit it

            UnsafeNativeMethods.IADsMembers iADsMembers = group.Members();
            this.membersEnumerator = ((IEnumerable) iADsMembers).GetEnumerator();
        }

    	// Return the principal we're positioned at as a Principal object.
    	// Need to use our StoreCtx's GetAsPrincipal to convert the native object to a Principal
    	override internal object CurrentAsPrincipal
    	{
            get
            {
                if (this.current != null)
                {
                    // Local principal --- handle it ourself
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "CurrentAsPrincipal: returning current");                    
                    return SAMUtils.DirectoryEntryAsPrincipal(this.current, this.storeCtx);
                }
                else if (this.currentFakePrincipal != null)
                {
                    // Local fake principal --- handle it ourself
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "CurrentAsPrincipal: returning currentFakePrincipal");                                        
                    return this.currentFakePrincipal;
                }
                else if (this.currentForeign != null)
                {
                    // Foreign, non-recursive principal.  Just return the principal.
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "CurrentAsPrincipal: returning currentForeign");                      
                    return this.currentForeign;
                }
                else
                {
                    // Foreign recursive expansion.  Proxy the call to the foreign ResultSet.
                    Debug.Assert(this.foreignResultSet != null);
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "CurrentAsPrincipal: returning foreignResultSet");                    
                    return this.foreignResultSet.CurrentAsPrincipal;
                }
                
            }
    	}


    	// Advance the enumerator to the next principal in the result set, pulling in additional pages
    	// of results (or ranges of attribute values) as needed.
    	// Returns true if successful, false if no more results to return.
    	override internal bool MoveNext()
    	{
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "Entering MoveNext");                    
    	
            this.atBeginning = false;

            bool f = MoveNextLocal();

            if (!f)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNext: trying foreign");
            
                f = MoveNextForeign();
            }

            return f;
        }

        bool MoveNextLocal()
        {
            bool needToRetry;

            do
            {
                needToRetry = false;
            
                object[] nativeMembers = new object[1];
                
                bool f = membersEnumerator.MoveNext();

                if (f) // got a value
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextLocal: got a value from the enumerator");                    
                
                    UnsafeNativeMethods.IADs nativeMember = (UnsafeNativeMethods.IADs) membersEnumerator.Current;

                    // If we encountered a group member corresponding to a fake principal such as
                    // NT AUTHORITY/NETWORK SERVICE, construct and prepare to return the fake principal.
                    byte[] sid = (byte[]) nativeMember.Get("objectSid");
                    SidType sidType = Utils.ClassifySID(sid);
                    if (sidType == SidType.FakeObject)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextLocal: fake principal, sid={0}", Utils.ByteArrayToString(sid));                    

                        this.currentFakePrincipal = this.storeCtx.ConstructFakePrincipalFromSID(sid);
                        this.current = null;
                        this.currentForeign = null;

                        if (this.foreignResultSet != null)
                            this.foreignResultSet.Dispose();
                        this.foreignResultSet = null;
                        return true;
                    }


                    // We do this, rather than using the DirectoryEntry constructor that takes a native IADs object,
                    // is so the credentials get transferred to the new DirectoryEntry.  If we just use the native
                    // object constructor, the native object will have the right credentials, but the DirectoryEntry
                    // will have default (null) credentials, which it'll use anytime it needs to use credentials.
                    DirectoryEntry de = SDSUtils.BuildDirectoryEntry(
                                                        this.storeCtx.Credentials,
                                                        this.storeCtx.AuthTypes);

                    if (sidType == SidType.RealObjectFakeDomain)
                    {
                        // Transform the "WinNT://BUILTIN/foo" path to "WinNT://machineName/foo"
                        string builtinADsPath = nativeMember.ADsPath;

                        UnsafeNativeMethods.Pathname pathCracker = new UnsafeNativeMethods.Pathname();
                        UnsafeNativeMethods.IADsPathname pathName = (UnsafeNativeMethods.IADsPathname) pathCracker;

                        pathName.Set(builtinADsPath, 1 /* ADS_SETTYPE_FULL */);

                        // Build the "WinNT://" portion of the new path
                        StringBuilder adsPath = new StringBuilder();
                        adsPath.Append("WinNT://");
                        //adsPath.Append(pathName.Retrieve(9 /*ADS_FORMAT_SERVER */));

                        // Build the "WinNT://machineName/" portion of the new path
                        adsPath.Append(this.storeCtx.MachineUserSuppliedName);
                        adsPath.Append("/");

                        // Build the "WinNT://machineName/foo" portion of the new path
                        int cElements = pathName.GetNumElements();

                        Debug.Assert(cElements >= 2);       // "WinNT://BUILTIN/foo" == 2 elements

                        // Note that the ADSI WinNT provider indexes them backwards, e.g., in
                        // "WinNT://BUILTIN/A/B", BUILTIN == 2, A == 1, B == 0.
                        for(int i = cElements-2; i >= 0; i--)
                        {
                            adsPath.Append(pathName.GetElement(i));
                            adsPath.Append("/");
                        }

                        adsPath.Remove(adsPath.Length-1, 1);  // remove the trailing "/"
                    
                        de.Path = adsPath.ToString();

                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextLocal: fake domain: {0} --> {1}", builtinADsPath, adsPath);                        
                    }
                    else
                    {
                        Debug.Assert(sidType == SidType.RealObject);
                        de.Path = nativeMember.ADsPath;

                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextLocal: real domain {0}", de.Path);                        
                    }



                  //  Debug.Assert(Utils.AreBytesEqual(sid, (byte[]) de.Properties["objectSid"].Value));

                    if (IsLocalMember(sid))
                    {
                        // If we're processing recursively, and the member is a group,
                        // we don't return it but instead treat it as something to recursively
                        // visit (expand) later.
                        if (!this.recursive || !SAMUtils.IsOfObjectClass(de, "Group"))
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextLocal: setting current to {0}", de.Path);                    
                        
                            // Not recursive, or not a group.  Return the principal.
                            this.current = de;
                            this.currentFakePrincipal = null;
                            this.currentForeign = null;

                            if (this.foreignResultSet != null)
                                this.foreignResultSet.Dispose();
                            this.foreignResultSet = null;
                            return true;
                        }
                        else
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextLocal: adding {0} to groupsToVisit", de.Path);                    
                        
                            // Save off for later, if we haven't done so already.
                            if (!this.groupsVisited.Contains(de.Path) && !this.groupsToVisit.Contains(de.Path))
                                this.groupsToVisit.Add(de.Path);

                            needToRetry = true;
                            continue;
                        }
                    }
                    else
                    {
                        // It's a foreign principal (e..g, an AD user or group).
                        // Save it off for later.

                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextLocal: adding {0} to foreignMembers", de.Path);
                        
                        this.foreignMembers.Add(de);
                        needToRetry = true;
                        continue;
                    }
                }
                else
                {
                    // We reached the end of this group's membership.
                    // If we're supposed to be recursively expanding, we need to expand
                    // any remaining non-foreign groups we earlier visited.
                    if (this.recursive)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextLocal: recursive processing, groupsToVisit={0}", groupsToVisit.Count); 
                    
                        if (this.groupsToVisit.Count > 0)
                        {
                            // Pull off the next group to visit
                            string groupPath = this.groupsToVisit[0];
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextLocal: recursively processing {0}", groupPath);                    
                            
                            this.groupsToVisit.RemoveAt(0);
                            groupsVisited.Add(groupPath);

                            // Set up for the next round of enumeration
                            DirectoryEntry de = SDSUtils.BuildDirectoryEntry(
                                                                        groupPath,
                                                                        this.storeCtx.Credentials,
                                                                        this.storeCtx.AuthTypes);

                            this.group = (UnsafeNativeMethods.IADsGroup) de.NativeObject;
                            
                            UnsafeNativeMethods.IADsMembers iADsMembers = this.group.Members();
                            this.membersEnumerator = ((IEnumerable)iADsMembers).GetEnumerator();

                            // and go on to the first member of this new group
                            needToRetry = true;
                            continue;
                        }
                    }
                }
            }
            while (needToRetry);

            return false;
    	}

        bool MoveNextForeign()
        {

            bool needToRetry;
        
            do
            {            
                needToRetry = false;

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextForeign: foreignMembers count={0}", this.foreignMembers.Count);                    
            
                if (this.foreignMembers.Count > 0)
                {                
                    // foreignDE is a DirectoryEntry in _this_ store representing a principal in another store
                    DirectoryEntry foreignDE = this.foreignMembers[0];
                    this.foreignMembers.RemoveAt(0);

                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextForeign: foreignDE={0}", foreignDE.Path);

                    // foreignPrincipal is a principal from _another_ store (e.g., it's backed by an ADStoreCtx)
                    Principal foreignPrincipal = this.storeCtx.ResolveCrossStoreRefToPrincipal(foreignDE);

                    // If we're not enumerating recursively, return the principal.
                    // If we are enumerating recursively, and it's a group, save it off for later.
                    if (!this.recursive || !(foreignPrincipal is GroupPrincipal))
                    {
                        // Return the principal.
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextForeign: setting currentForeign to {0}", foreignDE.Path);

                        this.current = null;
                        this.currentFakePrincipal = null;
                        this.currentForeign = foreignPrincipal;

                        if (this.foreignResultSet != null)
                            this.foreignResultSet.Dispose();
                        this.foreignResultSet = null;
                        return true;
                    }
                    else
                    {
                        // Save off the group for recursive expansion, and go on to the next principal.
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextForeign: adding {0} to foreignGroups", foreignDE.Path);
                        
                        this.foreignGroups.Add((GroupPrincipal)foreignPrincipal);
                        needToRetry = true;
                        continue;
                    }
                }


                if (this.foreignResultSet == null && this.foreignGroups.Count > 0)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                            "SAMMembersSet",
                                            "MoveNextForeign: getting foreignResultSet (foreignGroups count={0})",
                                            this.foreignGroups.Count);
                
                    // We're expanding recursively, and either (1) we're immediately before
                    // the recursive expansion of the first foreign group, or (2) we just completed
                    // the recursive expansion of a foreign group, and now are moving on to the next.
                    Debug.Assert(this.recursive == true);

                    // Pull off a foreign group to expand.
                    GroupPrincipal foreignGroup = this.foreignGroups[0];
                    this.foreignGroups.RemoveAt(0);

                    // Since it's a foreign group, we don't know how to enumerate its members.  So we'll
                    // ask the group, through its StoreCtx, to do it for us.  Effectively, we'll end up acting
                    // as a proxy to the foreign group's ResultSet.
                    this.foreignResultSet = foreignGroup.GetStoreCtxToUse().GetGroupMembership(foreignGroup, true);
                }

                // We're either just beginning the recursive expansion of a foreign group, or we're continuing the expansion
                // that we started on a previous call to MoveNext().
                if (this.foreignResultSet != null)
                {
                    Debug.Assert(this.recursive == true);
                
                    bool f = this.foreignResultSet.MoveNext();

                    if (f)
                    {
                        // By setting current, currentFakePrincipal, and currentForeign to null,
                        // CurrentAsPrincipal/CurrentAsIdentityReference will know to proxy out to foreignResultSet.
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextForeign: using foreignResultSet");
                        
                        this.current = null;
                        this.currentFakePrincipal = null;
                        this.currentForeign = null;
                        return true;
                    }

                    // Ran out of members in the foreign group, is there another foreign group remaining that we need
                    // to expand?
                    if (this.foreignGroups.Count > 0)
                    {
                        // Yes, there is.  Null out the foreignResultSet so we'll pull out the next foreign group
                        // the next time around the loop.
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextForeign: ran out of members, using next foreignResultSet");
                        
                        this.foreignResultSet.Dispose();
                        this.foreignResultSet = null;
                        Debug.Assert(this.foreignMembers.Count == 0);
                        needToRetry = true;
                    }
                    else
                    {
                        // No, there isn't.  Nothing left to do.  We set foreignResultSet to null here just
                        // to leave things in a clean state --- it shouldn't really be necessary.
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextForeign: ran out of members, nothing more to do");
                        
                        this.foreignResultSet.Dispose();
                        this.foreignResultSet = null;
                    }
                }
                
            }
            while (needToRetry);

            return false;
        }

        bool IsLocalMember(byte[] sid)
        {
            // BUILTIN SIDs are local, but we can't determine that by looking at domainName
            SidType sidType = Utils.ClassifySID(sid);
            Debug.Assert(sidType != SidType.FakeObject);
            
            if (sidType == SidType.RealObjectFakeDomain)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, 
                                        "SAMMembersSet", 
                                        "IsLocalMember: fake domain, SID={0}",
                                        Utils.ByteArrayToString(sid)); 
            
                return true;
            }

            bool isLocal = false;

            // Ask the OS to resolve the SID to its target.
            int accountUsage = 0;
            string name;
            string domainName;

            int err = Utils.LookupSid(
                                this.storeCtx.MachineUserSuppliedName,
                                this.storeCtx.Credentials, 
                                sid, 
                                out name,
                                out domainName, 
                                out accountUsage);

            if (err != 0)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Error, 
                                        "SAMMembersSet",
                                        "IsLocalMember: LookupSid failed, sid={0}, server={1}, err={2}",
                                        Utils.ByteArrayToString(sid),
                                        this.storeCtx.MachineUserSuppliedName,
                                        err);
                
                throw new PrincipalOperationException(
                            String.Format(CultureInfo.CurrentCulture,
                                          StringResources.SAMStoreCtxErrorEnumeratingGroup,
                                          err));
            }

            if (String.Compare(this.storeCtx.MachineFlatName, domainName,  StringComparison.OrdinalIgnoreCase) == 0)
                isLocal = true;

            GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                    "SAMMembersSet",
                                    "IsLocalMember: sid={0}, isLocal={1}, domainName={2}",
                                    Utils.ByteArrayToString(sid),
                                    isLocal,
                                    domainName);

            return isLocal;
        }

    	// Resets the enumerator to before the first result in the set.  This potentially can be an expensive
    	// operation, e.g., if doing a paged search, may need to re-retrieve the first page of results.
    	// As a special case, if the ResultSet is already at the very beginning, this is guaranteed to be
    	// a no-op.
    	override internal void Reset()
    	{
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "Reset");
    	
            if (!this.atBeginning)
            {
                this.groupsToVisit.Clear();
                string originalGroupPath = this.groupsVisited[0];
                this.groupsVisited.Clear();
                this.groupsVisited.Add(originalGroupPath);

                this.group = this.originalGroup;
                UnsafeNativeMethods.IADsMembers iADsMembers = this.group.Members();
                this.membersEnumerator = ((IEnumerable) iADsMembers).GetEnumerator();

                this.current = null;
                this.currentFakePrincipal = null;
                this.currentForeign = null;

                this.foreignMembers.Clear();
                this.foreignGroups.Clear();

                if (this.foreignResultSet != null)
                {
                    this.foreignResultSet.Dispose();
                    this.foreignResultSet = null;
                }
                
                this.atBeginning = true;
            }
    	}


        override internal ResultSetBookmark BookmarkAndReset()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "Bookmarking");
        
            SAMMembersSetBookmark bookmark = new SAMMembersSetBookmark();

            bookmark.groupsToVisit = this.groupsToVisit;
            this.groupsToVisit = new List<string>();
            
            string originalGroupPath = this.groupsVisited[0];
            bookmark.groupsVisited = this.groupsVisited;
            this.groupsVisited = new List<string>();
            this.groupsVisited.Add(originalGroupPath);

            bookmark.group = this.group;
            bookmark.membersEnumerator = this.membersEnumerator;
            this.group = this.originalGroup;
            UnsafeNativeMethods.IADsMembers iADsMembers = this.group.Members();
            this.membersEnumerator = ((IEnumerable) iADsMembers).GetEnumerator();

            bookmark.current = this.current;
            bookmark.currentFakePrincipal = this.currentFakePrincipal;
            bookmark.currentForeign = this.currentForeign;
            this.current = null;
            this.currentFakePrincipal = null;
            this.currentForeign = null;

            bookmark.foreignMembers = this.foreignMembers;
            bookmark.foreignGroups = this.foreignGroups;
            bookmark.foreignResultSet = this.foreignResultSet;
            this.foreignMembers = new List<DirectoryEntry>();
            this.foreignGroups = new List<GroupPrincipal>();
            this.foreignResultSet = null;

            bookmark.atBeginning = this.atBeginning;
            this.atBeginning = true;

            return bookmark;
        }

        override internal void RestoreBookmark(ResultSetBookmark bookmark)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "Restoring from bookmark"); 
        
            Debug.Assert(bookmark is SAMMembersSetBookmark);
            SAMMembersSetBookmark samBookmark = (SAMMembersSetBookmark) bookmark;

            this.groupsToVisit = samBookmark.groupsToVisit;
            this.groupsVisited = samBookmark.groupsVisited;
            this.group = samBookmark.group;
            this.membersEnumerator = samBookmark.membersEnumerator;
            this.current = samBookmark.current;
            this.currentFakePrincipal = samBookmark.currentFakePrincipal;
            this.currentForeign = samBookmark.currentForeign;
            this.foreignMembers = samBookmark.foreignMembers;
            this.foreignGroups = samBookmark.foreignGroups;

            if (this.foreignResultSet != null)
                this.foreignResultSet.Dispose();
            
            this.foreignResultSet = samBookmark.foreignResultSet;
            this.atBeginning = samBookmark.atBeginning;
        }

        override public void Dispose()
        {
            try
            {
                if (!this.disposed)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "Dispose: disposing"); 
                
                    if (this.foreignResultSet != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "Dispose: disposing foreignResultSet");                
                        this.foreignResultSet.Dispose();
                    }
                
                    this.disposed = true;
                } 
            }
            finally
            {
            
                 base.Dispose();
            }
            
        }

        //
        // Private fields
        //

       
        bool recursive;

        bool disposed = false;
        
        SAMStoreCtx storeCtx;
        DirectoryEntry ctxBase;

        bool atBeginning = true;
        

        // local
        
        // The 0th entry in this list is always the ADsPath of the original group whose membership we're querying
        List<string> groupsVisited = new List<string>();
        
        List<string> groupsToVisit = new List<string>();

        DirectoryEntry current = null; // current member of the group (if enumerating local group and found a real principal)
        Principal currentFakePrincipal = null;  // current member of the group (if enumerating local group and found a fake pricipal)

        UnsafeNativeMethods.IADsGroup group;            // the group whose membership we're currently enumerating over
        UnsafeNativeMethods.IADsGroup originalGroup;    // the group whose membership we started off with (before recursing)

        IEnumerator membersEnumerator;         // the current group's membership enumerator


        // foreign
        List<DirectoryEntry> foreignMembers = new List<DirectoryEntry>();
        Principal currentForeign = null; // current member of the group (if enumerating foreign principal)

        List<GroupPrincipal> foreignGroups = new List<GroupPrincipal>();
        ResultSet foreignResultSet = null; // current foreign group's ResultSet (if enumerating via proxy to foreign group)
    }


    class SAMMembersSetBookmark : ResultSetBookmark
    {
        public List<string> groupsToVisit;
        public List<string> groupsVisited;
        public UnsafeNativeMethods.IADsGroup group;
        public IEnumerator membersEnumerator;
        public DirectoryEntry current;
        public Principal currentFakePrincipal;     
        public Principal currentForeign;
        public List<DirectoryEntry> foreignMembers;
        public List<GroupPrincipal> foreignGroups;
        public ResultSet foreignResultSet;
        public bool atBeginning;
    }
}
// #endif
