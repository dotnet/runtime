/*--
Copyright (c) 2004  Microsoft Corporation

Module Name:

    ADDNLinkedAttrSet.cs

Abstract:

    Implements the ADDNLinkedAttrSet ResultSet class.
    
History:

    04-June-2004    MattRim     Created

--*/


using System;
using System.DirectoryServices;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Security.Principal;

namespace System.DirectoryServices.AccountManagement
{
    // <SecurityKernel Critical="True" Ring="0">
    // <Asserts Name="Declarative: [DirectoryServicesPermission(SecurityAction.Assert, Unrestricted = true)]" />
    // </SecurityKernel>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Assert, Unrestricted=true)]
    class ADDNLinkedAttrSet : BookmarkableResultSet
    {

        // This class can be used to either enumerate the members of a group, or the groups
        // to which a principal belongs.  If being used to enumerate the members of a group:
        //      * groupDN --- the DN of the group we're enumerating
        //      * members --- array of enumerators containing the DNs of the members of the group we're enumerating (the "member" attribute)
        //      * primaryGroupDN --- should be null
        //      * recursive --- whether or not to recursively enumerate group membership
        //
        // If being used to enumerate the groups to which a principal belongs:
        //      * groupDN --- the DN of the principal (i.e., the user)
        //      * members --- the DNs of the groups to which that principal belongs (e.g, the "memberOf" attribute)
        //      * primaryGroupDN --- the DN of the principal's primary group (constructed from the "primaryGroupID" attribute)
        //      * recursive --- should be false
        //
        // Note that the variables in this class are generally named in accord with the "enumerating the members
        // of a group" case.
        //
        // It is assumed that recursive enumeration will only be performed for the "enumerating the members of a group"
        // case, not the "groups to which a principal belongs" case, thus, this.recursive == true implies the former
        // (but this.recursive == false could imply either case).
    
        internal ADDNLinkedAttrSet(
                            string groupDN,
                            IEnumerable[] members,
                            string primaryGroupDN, 
                            DirectorySearcher primaryGroupMembersSearcher ,
                            bool recursive, 
                            ADStoreCtx storeCtx)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, 
                                    "ADDNLinkedAttrSet", 
                                    "ADDNLinkedAttrSet: groupDN={0}, primaryGroupDN={1}, recursive={2}, PG queryFilter={3}, PG queryBase={4}",
                                    groupDN,
                                    (primaryGroupDN != null ? primaryGroupDN : "NULL"),
                                    recursive,
                                    (primaryGroupMembersSearcher  != null ? primaryGroupMembersSearcher .Filter : "NULL"),
                                    (primaryGroupMembersSearcher  != null ? primaryGroupMembersSearcher.SearchRoot.Path : "NULL"));

        
            this.groupsVisited.Add(groupDN);    // so we don't revisit it
            this.recursive = recursive;
            this.storeCtx = storeCtx;
            this.originalStoreCtx = storeCtx;

	    if ( null != members )
	    {
               foreach (IEnumerable enumerator in members)
                {
                    this.membersQueue.Enqueue(enumerator);
                    this.originalMembers.Enqueue(enumerator);
                }
            }

            this.members = null;


            this.currentMembersSearcher = null;
            this.primaryGroupDN = primaryGroupDN;
            if (primaryGroupDN == null)
                returnedPrimaryGroup = true;    // so we don't bother trying to return the primary group

            this.primaryGroupMembersSearcher  = primaryGroupMembersSearcher;

            expansionMode = ExpansionMode.Enum;
            originalExpansionMode = expansionMode;
            
        }
                            
        internal ADDNLinkedAttrSet(
                            string groupDN,
                            DirectorySearcher[] membersSearcher,
                            string primaryGroupDN, 
                            DirectorySearcher primaryGroupMembersSearcher ,
                            bool recursive, 
                            ADStoreCtx storeCtx)
                            
                            
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, 
                                    "ADDNLinkedAttrSet", 
                                    "ADDNLinkedAttrSet: groupDN={0}, primaryGroupDN={1}, recursive={2}, M queryFilter={3}, M queryBase={4}, PG queryFilter={5}, PG queryBase={6}",
                                    groupDN,
                                    (primaryGroupDN != null ? primaryGroupDN : "NULL"),
                                    recursive,
                                    (membersSearcher  != null ? membersSearcher[0].Filter : "NULL"),
                                    (membersSearcher  != null ? membersSearcher[0].SearchRoot.Path : "NULL"),
                                    (primaryGroupMembersSearcher  != null ? primaryGroupMembersSearcher .Filter : "NULL"),
                                    (primaryGroupMembersSearcher  != null ? primaryGroupMembersSearcher.SearchRoot.Path : "NULL"));

        
            this.groupsVisited.Add(groupDN);    // so we don't revisit it
            this.recursive = recursive;
            this.storeCtx = storeCtx;
            this.originalStoreCtx = storeCtx;

            this.members = null;
            this.originalMembers = null;
            this.membersEnum = null;            

            this.primaryGroupDN = primaryGroupDN;
            if (primaryGroupDN == null)
                returnedPrimaryGroup = true;    // so we don't bother trying to return the primary group

	    if ( null != membersSearcher)
            {
                foreach (DirectorySearcher ds in membersSearcher)
                {
                    this.memberSearchersQueue.Enqueue(ds);
                    this.memberSearchersQueueOriginal.Enqueue(ds);
                }
            }

            this.currentMembersSearcher = null;

            this.primaryGroupMembersSearcher  = primaryGroupMembersSearcher;
            
            expansionMode = ExpansionMode.ASQ;
            originalExpansionMode = expansionMode;
            
        }
    
    	// Return the principal we're positioned at as a Principal object.
    	// Need to use our StoreCtx's GetAsPrincipal to convert the native object to a Principal
    	override internal object CurrentAsPrincipal
    	{
    	    get
    	    {
                if (this.current != null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "CurrentAsPrincipal: using current");   
                    if ( this.current is DirectoryEntry )
                        return ADUtils.DirectoryEntryAsPrincipal((DirectoryEntry)this.current, this.storeCtx);
                    else
                    {
                        return ADUtils.SearchResultAsPrincipal((SearchResult)this.current, this.storeCtx, null);
                    }
                        
                }
                else
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "CurrentAsPrincipal: using currentForeignPrincipal");                
                    Debug.Assert(this.currentForeignPrincipal != null);
                    return this.currentForeignPrincipal;
                }
    	    }
	    }

    	// Advance the enumerator to the next principal in the result set, pulling in additional pages
    	// of results (or ranges of attribute values) as needed.
    	// Returns true if successful, false if no more results to return.
    	override internal bool MoveNext()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Entering MoveNext");    	        
        
            this.atBeginning = false;

            bool needToRetry;
            bool f = false;

            do
            {
                needToRetry = false;
                // reset our found state.  If we are restarting the loop we don't have a current principal yet.
                f = false;

                if (!this.returnedPrimaryGroup)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNext: trying PrimaryGroup DN");
                    f = MoveNextPrimaryGroupDN();
                }

                if ( !f )
                {
                    if ( expansionMode == ExpansionMode.ASQ )
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNext: trying member searcher");
                        f = MoveNextMemberSearcher();                
                    }
                    else
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNext: trying member enum");                
                        f = MoveNextMemberEnum();
                    }
                }

                if (!f)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNext: trying foreign");                
                    f = MoveNextForeign(ref needToRetry);
                }

                if (!f)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNext: trying primary group search");                
                    f = MoveNextQueryPrimaryGroupMember();
                }
            }
            while (needToRetry);

            return f;
        }



        bool MoveNextPrimaryGroupDN()
        {
                // Do the special primary group ID processing if we haven't yet returned the primary group.
            Debug.Assert(this.primaryGroupDN != null);

            this.current = SDSUtils.BuildDirectoryEntry(
                                        BuildPathFromDN(this.primaryGroupDN),
                                        this.storeCtx.Credentials,
                                        this.storeCtx.AuthTypes);
            
            this.storeCtx.InitializeNewDirectoryOptions( (DirectoryEntry)this.current );
            
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberSearcher: returning primary group {0}", ((DirectoryEntry)this.current).Path);

            this.currentForeignDE = null;
            this.currentForeignPrincipal = null;

            this.returnedPrimaryGroup = true;
            return true;
        }

        bool GetNextSearchResult()
        {
            bool memberFound = false;

            do
            {
                if (this.currentMembersSearcher == null)
                {
                    Debug.Assert(this.memberSearchersQueue != null);

                    if (this.memberSearchersQueue.Count == 0)
                    {
                        // We are out of searchers in the queue.
                        return false;
                    }
                    else
                    {
                        // Remove the next searcher from the queue and place it in the current search variable.
                        this.currentMembersSearcher = this.memberSearchersQueue.Dequeue();
                        this.memberSearchResults = this.currentMembersSearcher.FindAll();
                        Debug.Assert(this.memberSearchResults != null);
                        this.memberSearchResultsEnumerator = this.memberSearchResults.GetEnumerator();
                    }
                }

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextQueryMember: have a searcher");

                memberFound = this.memberSearchResultsEnumerator.MoveNext();

                // The search is complete.
                // Dipose the searcher and search results.
                if (!memberFound)
                {
                    this.currentMembersSearcher.Dispose();
                    this.currentMembersSearcher = null;
                    this.memberSearchResults.Dispose();
                    this.memberSearchResults = null;
                }

            } while (!memberFound);

            return memberFound;
        }

        bool MoveNextMemberSearcher()
    	{
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Entering MoveNextMemberSearcher");    	        
    	
            bool needToRetry = false;
            bool f = false;
           
    	    do
            {
                    f = GetNextSearchResult();
                    needToRetry = false;

                    if (f)
                    {

                        SearchResult currentSR = (SearchResult)this.memberSearchResultsEnumerator.Current;

                        // Got a member from this group (or, got a group of which we're a member).
                        // Create a DirectoryEntry for it.
                        string memberDN = (string) currentSR.Properties["distinguishedName"][0];
                        
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberSearcher: got a value from the enumerator: {0}", memberDN);
                        
                        // Make sure the member is a principal
                        if ( (!ADUtils.IsOfObjectClass(currentSR, "group")) &&
                             (!ADUtils.IsOfObjectClass(currentSR, "user"))  &&     // includes computer as well
                             (!ADUtils.IsOfObjectClass(currentSR, "foreignSecurityPrincipal")) )
                        {
                            // We found a member, but it's not a principal type.  Skip it.
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberSearcher: not a principal, skipping");                        
                            needToRetry = true;
                        }
                        // If we're processing recursively, and the member is a group, we DON'T return it,
                        // but rather treat it as something to recursively visit later
                        // (unless we've already visited the group previously)
                        else if (this.recursive && ADUtils.IsOfObjectClass(currentSR, "group"))
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberSearcher: adding to groupsToVisit");
                        
                            if (!this.groupsVisited.Contains(memberDN) && !this.groupsToVisit.Contains(memberDN))
                                this.groupsToVisit.Add(memberDN);

                            // and go on to the next member....
                            needToRetry = true;
                        }
                        else if (this.recursive && ADUtils.IsOfObjectClass(currentSR, "foreignSecurityPrincipal"))
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberSearcher: foreign principal, adding to foreignMembers");

                            // If we haven't seen this FPO yet then add it to the seen user database.
                            if (!usersVisited.ContainsKey( currentSR.Properties["distinguishedName"][0].ToString() ))
                            {                        
                                // The FPO might represent a group, in which case we should recursively enumerate its
                                // membership.  So save it off for later processing.
                                this.foreignMembersCurrentGroup.Add(currentSR.GetDirectoryEntry());
                                usersVisited.Add(currentSR.Properties["distinguishedName"][0].ToString(), true);
                            }
                            
                            // and go on to the next member....
                            needToRetry = true;                        
                        }
                        else
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberSearcher: using as current");

                            // Check to see if we have already seen this user during the enumeration
                            // If so then move on to the next user.  If not then return it as current.
                            if (!usersVisited.ContainsKey(currentSR.Properties["distinguishedName"][0].ToString()))
                            {                        
                                this.current = currentSR;
                                this.currentForeignDE = null;
                                this.currentForeignPrincipal = null;
                                usersVisited.Add(currentSR.Properties["distinguishedName"][0].ToString(), true);
                            }
                            else
                            {
                                needToRetry = true;                        
                            }
                        
                        }
                        
                    }
                    else
                    {
                        // We reached the end of this group's membership.  If we're not processing recursively,
                        // we're done.  Otherwise, go on to the next group to visit.
                        // First create a DE that points to the group we want to expand,  Using that as a search root run
                        // an ASQ search against  member and and start enumerting those results.
                        if (this.recursive)
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, 
                                                    "ADDNLinkedAttrSet",
                                                    "MoveNextMemberSearcher: recursive processing, groupsToVisit={0}",
                                                    groupsToVisit.Count);
                        
                            if (groupsToVisit.Count > 0)
                            {
                                // Pull off the next group to visit
                                string groupDN = groupsToVisit[0];
                                groupsToVisit.RemoveAt(0);
                                groupsVisited.Add(groupDN);

                                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberSearcher: recursively processing {0}", groupDN);

                                // get the membership of this new group
                                DirectoryEntry groupDE = SDSUtils.BuildDirectoryEntry( BuildPathFromDN(groupDN), this.storeCtx.Credentials, this.storeCtx.AuthTypes);

                                this.storeCtx.InitializeNewDirectoryOptions( groupDE );
            
                                // Queue up a searcher for the new group expansion.
                                DirectorySearcher ds = SDSUtils.ConstructSearcher(groupDE);
                                ds.Filter = "(objectClass=*)";
                                ds.SearchScope = SearchScope.Base;
                                ds.AttributeScopeQuery = "member";
                                ds.CacheResults = false;

                                this.memberSearchersQueue.Enqueue(ds);

                                // and go on to the first member of this new group.
                                needToRetry = true;
                            }
                        }                   
                    }           
            }
            while (needToRetry);

            return f;
    	}



        bool GetNextEnum()
        {
            bool memberFound = false;

            do
            {
                if (null == members)
                {
                    if (membersQueue.Count == 0)
                    {
                        return false;
                    }

                    members = membersQueue.Dequeue();
                    membersEnum = members.GetEnumerator();
                }

                memberFound = membersEnum.MoveNext();

                if (!memberFound)
                {
                    IDisposable disposableMembers = members as IDisposable;
                    if (disposableMembers != null)
                    {
                        disposableMembers.Dispose();
                    }
                    IDisposable disposableMembersEnum = membersEnum as IDisposable;
                    if (disposableMembersEnum != null)
                    {
                        disposableMembersEnum.Dispose();
                    }
                    members = null;
                    membersEnum = null;
                }

            } while (!memberFound);

            return memberFound;
        }

    	bool MoveNextMemberEnum()
    	{
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Entering MoveNextMemberEnum");    	        
    	
            bool needToRetry = false;
            bool disposeMemberDE = false;
            bool f;

    	    do
            {
                f = GetNextEnum();
                needToRetry = false;
                disposeMemberDE = false;

                if (f)
                {
                    DirectoryEntry memberDE = null;
                    try
                    {
                        // Got a member from this group (or, got a group of which we're a member).
                        // Create a DirectoryEntry for it.
                        string memberDN = (string)membersEnum.Current;

                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberEnum: got a value from the enumerator: {0}", memberDN);

                        memberDE = SDSUtils.BuildDirectoryEntry(
                                                        BuildPathFromDN(memberDN),
                                                        this.storeCtx.Credentials,
                                                        this.storeCtx.AuthTypes);

                        this.storeCtx.InitializeNewDirectoryOptions(memberDE);

                        storeCtx.LoadDirectoryEntryAttributes(memberDE);

                        // Make sure the member is a principal
                        if ((!ADUtils.IsOfObjectClass(memberDE, "group")) &&
                             (!ADUtils.IsOfObjectClass(memberDE, "user")) &&     // includes computer as well
                             (!ADUtils.IsOfObjectClass(memberDE, "foreignSecurityPrincipal")))
                        {
                            // We found a member, but it's not a principal type.  Skip it.
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberEnum: not a principal, skipping");
                            needToRetry = true;
                            disposeMemberDE = true; //Since member is not principal we don't return it. So mark it for dispose.
                        }
                        // If we're processing recursively, and the member is a group, we DON'T return it,
                        // but rather treat it as something to recursively visit later
                        // (unless we've already visited the group previously)
                        else if (this.recursive && ADUtils.IsOfObjectClass(memberDE, "group"))
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberEnum: adding to groupsToVisit");

                            if (!this.groupsVisited.Contains(memberDN) && !this.groupsToVisit.Contains(memberDN))
                                this.groupsToVisit.Add(memberDN);

                            // and go on to the next member....
                            needToRetry = true;
                            disposeMemberDE = true; //Since recursive is set to true, we do not return groups. So mark it for dispose.
                        }
                        else if (this.recursive && ADUtils.IsOfObjectClass(memberDE, "foreignSecurityPrincipal"))
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberEnum: foreign principal, adding to foreignMembers");

                            // If we haven't seen this FPO yet then add it to the seen user database.
                            if (!usersVisited.ContainsKey(memberDE.Properties["distinguishedName"][0].ToString()))
                            {
                                // The FPO might represent a group, in which case we should recursively enumerate its
                                // membership.  So save it off for later processing.
                                this.foreignMembersCurrentGroup.Add(memberDE);
                                usersVisited.Add(memberDE.Properties["distinguishedName"][0].ToString(), true);
                                disposeMemberDE = false; //We store the FPO DirectoryEntry objects for further processing. So do NOT dispose it. 
                            }

                            // and go on to the next member....
                            needToRetry = true;
                        }
                        else
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberEnum: using as current");

                            // Check to see if we have already seen this user during the enumeration
                            // If so then move on to the next user.  If not then return it as current.
                            if (!usersVisited.ContainsKey(memberDE.Properties["distinguishedName"][0].ToString()))
                            {
                                this.current = memberDE;
                                this.currentForeignDE = null;
                                this.currentForeignPrincipal = null;
                                usersVisited.Add(memberDE.Properties["distinguishedName"][0].ToString(), true);
                                disposeMemberDE = false; //memberDE will be set in the Principal object we return. So do NOT dispose it.
                            }
                            else
                            {
                                needToRetry = true;
                            }

                        }
                    }
                    finally
                    {
                        if (disposeMemberDE && memberDE != null)
                        {
                            //This means the constructed member is not used in the new principal
                            memberDE.Dispose();
                        }
                    }
                    
                }
                else
                {
                    // We reached the end of this group's membership.  If we're not processing recursively,
                    // we're done.  Otherwise, go on to the next group to visit.
                    if (this.recursive)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, 
                                                "ADDNLinkedAttrSet",
                                                "MoveNextLocal: recursive processing, groupsToVisit={0}",
                                                groupsToVisit.Count);
                    
                        if (groupsToVisit.Count > 0)
                        {
                            // Pull off the next group to visit
                            string groupDN = groupsToVisit[0];
                            groupsToVisit.RemoveAt(0);
                            groupsVisited.Add(groupDN);

                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberEnum: recursively processing {0}", groupDN);

                            // get the membership of this new group
                            DirectoryEntry groupDE = SDSUtils.BuildDirectoryEntry(
                                                                    BuildPathFromDN(groupDN),
                                                    this.storeCtx.Credentials,
                                                    this.storeCtx.AuthTypes);

                            this.storeCtx.InitializeNewDirectoryOptions( groupDE );

                            // set up for the next round of enumeration
                            //Here a new DirectoryEntry object is created and passed 
                            //to RangeRetriever object. Hence, configure 
                            //RangeRetriever to dispose the DirEntry on its dispose. 
                            membersQueue.Enqueue( new RangeRetriever(groupDE, "member", true));

                            // and go on to the first member of this new group....
                            needToRetry = true;
                        }
                    }
                }

            }
            while (needToRetry);

            return f;
    	}



        void TranslateForeignMembers()
        {

            GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADDNLinkedAttrSet", "TranslateForeignMembers: Translating foreign members");

            List< Byte[] > sidList = new List<Byte[]>(this.foreignMembersCurrentGroup.Count);

            // Foreach foreign principal retrive the sid. 
            // If the SID is for a fake object we have to track it seperately.  If we were attempt to translate it
            // it would fail and not be returned and we would lose it.
            // Once we have a list of sids then translate them against the target store in one call.
            foreach( DirectoryEntry de in this.foreignMembersCurrentGroup)
            {
                // Get the SID of the foreign principal
                if (de.Properties["objectSid"].Count == 0)
                {
                    throw new PrincipalOperationException(StringResources.ADStoreCtxCantRetrieveObjectSidForCrossStore);
                }

                Byte[] sid = (Byte[])de.Properties["objectSid"].Value ;

                // What type of SID is it?
                SidType sidType = Utils.ClassifySID(sid);

                if (sidType == SidType.FakeObject)
                {
                    //Add the foreign member DirectoryEntry to fakePrincipalMembers list for further translation
                    //This de will be disposed after completing the translation by another code block. 
                    this.fakePrincipalMembers.Add(de);
                
                    // It's a FPO for something like NT AUTHORITY\NETWORK SERVICE.
                    // There's no real store object corresponding to this FPO, so
                    // fake a Principal.
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, 
                                            "ADDNLinkedAttrSet",
                                            "TranslateForeignMembers: fake principal, SID={0}",
                                            Utils.ByteArrayToString(sid));                                                                
                    
                }
                else
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, 
                                            "ADDNLinkedAttrSet",
                                            "TranslateForeignMembers: standard principal, SID={0}",
                                            Utils.ByteArrayToString(sid));
                
                    sidList.Add(sid);
                    //We do NOT need the Foreign member DirectoryEntry object once it has been translated and added to sidList.
                    //So disposing it off now
                    de.Dispose();
                }
            }

            // This call will perform a bulk sid translate to the name + issuer domain.
            this.foreignMembersToReturn = new SidList( sidList, this.storeCtx.DnsHostName, this.storeCtx.Credentials);

            // We have translated the sids so clear the group now.
            this.foreignMembersCurrentGroup.Clear();

        }

        
        bool MoveNextForeign(ref bool outerNeedToRetry)
        {
            outerNeedToRetry = false;
            bool needToRetry;
            Principal foreignPrincipal;
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Entering MoveNextForeign");

            do
            {
                needToRetry = false;


                if (this.foreignMembersCurrentGroup.Count > 0)
                {
                    TranslateForeignMembers();
                }


                if (fakePrincipalMembers.Count > 0)
                {
                    foreignPrincipal = this.storeCtx.ConstructFakePrincipalFromSID((Byte[])fakePrincipalMembers[0].Properties["objectSid"].Value);
                    fakePrincipalMembers[0].Dispose();
                    fakePrincipalMembers.RemoveAt(0);
                }
                else if (( foreignMembersToReturn != null ) && (foreignMembersToReturn.Length > 0))
                {

                    StoreCtx foreignStoreCtx;


                    SidListEntry foreignSid = foreignMembersToReturn[0];


                    // sidIssuerName is null only if SID was not resolved
                    // return a unknown principal back
                    if ( null == foreignSid.sidIssuerName )
                    {
                        // create and return the unknown principal if it is not yet present in usersVisited
                        if ( !usersVisited.ContainsKey( foreignSid.name ))
                        {
                            byte[] sid = Utils.ConvertNativeSidToByteArray( foreignSid.pSid );
                            UnknownPrincipal unknownPrincipal = UnknownPrincipal.CreateUnknownPrincipal( this.storeCtx.OwningContext, sid, foreignSid.name );
                            usersVisited.Add( foreignSid.name, true );
                            this.current = null;
                            this.currentForeignDE = null;
                            this.currentForeignPrincipal = unknownPrincipal;
                            // remove the current member
                            foreignMembersToReturn.RemoveAt( 0 );
                            return true;
                        }

                        // remove the current member
                        foreignMembersToReturn.RemoveAt( 0 );


                        needToRetry = true;
                        continue;
                    }


                    SidType sidType = Utils.ClassifySID(foreignSid.pSid);

                    if (sidType == SidType.RealObjectFakeDomain)
                    {

                        // This is a BUILTIN object.  It's a real object on the store we're connected to, but LookupSid
                        // will tell us it's a member of the BUILTIN domain.  Resolve it as a principal on our store.
                        GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADDNLinkedAttrSet", "MoveNextForeign: builtin principal");
                        foreignStoreCtx = this.storeCtx;
                    }
                    else
                    {

                        ContextOptions remoteOptions = DefaultContextOptions.ADDefaultContextOption;

#if USE_CTX_CACHE
                        PrincipalContext remoteCtx = SDSCache.Domain.GetContext(foreignSid.sidIssuerName, this.storeCtx.Credentials, remoteOptions);
#else
                        PrincipalContext remoteCtx = new PrincipalContext(
                                        ContextType.Domain,
                                        foreignSid.sidIssuerName,
                                        null,
                                        (this.storeCtx.Credentials != null ? this.storeCtx.Credentials.UserName : null),
                                        (this.storeCtx.Credentials != null ? storeCtx.storeCtx.Credentials.Password : null),
                                        remoteOptions);

#endif
                        foreignStoreCtx = remoteCtx.QueryCtx;

                    }


                    foreignPrincipal = foreignStoreCtx.FindPrincipalByIdentRef(
                                                     typeof(Principal),
                                                     UrnScheme.SidScheme,
                                                     (new SecurityIdentifier(Utils.ConvertNativeSidToByteArray(foreignMembersToReturn[0].pSid), 0)).ToString(),
                                                     DateTime.UtcNow);

                    if (null == foreignPrincipal)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADDNLinkedAttrSet", "MoveNextForeign: no matching principal");
                        throw new PrincipalOperationException(StringResources.ADStoreCtxFailedFindCrossStoreTarget);
                    }

                    foreignMembersToReturn.RemoveAt(0);
                    
                }
                else
                {
                    // We don't have any more foreign principals to return so start with the foreign groups
                    if (this.foreignGroups.Count > 0)
                    {
                        outerNeedToRetry = true;

                        // Determine the domainFunctionalityMode of the foreign domain.  If they are W2k or not a global group then we can't use ASQ.                    
                        if (this.foreignGroups[0].Context.ServerInformation.OsVersion == DomainControllerMode.Win2k ||
                            this.foreignGroups[0].GroupScope != GroupScope.Global)
                        {
                            expansionMode = ExpansionMode.Enum;
                            return ExpandForeignGroupEnumerator();
                        }
                        else
                        {
                            expansionMode = ExpansionMode.ASQ;
                            return ExpandForeignGroupSearcher();
                        }
                    }
                    else
                    {
                        // We are done with foreign principals and groups..
                        return false;
                    }
                }

                if (foreignPrincipal is GroupPrincipal)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextForeign: foreign member is a group");

                    // A group, need to recursively expand it (unless it's a fake group,
                    // in which case it is by definition empty and so contains nothing to expand, or unless
                    // we've already or will visit it).
                    // Postpone to later.
                    if (!foreignPrincipal.fakePrincipal)
                    {
                        string groupDN = (string)((DirectoryEntry)foreignPrincipal.UnderlyingObject).Properties["distinguishedName"].Value;

                        GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                                "ADDNLinkedAttrSet",
                                                "MoveNextForeign: not a fake group, adding {0} to foreignGroups",
                                                groupDN);

                        if (!this.groupsVisited.Contains(groupDN) && !this.groupsToVisit.Contains(groupDN))
                        {
                            this.foreignGroups.Add((GroupPrincipal)foreignPrincipal);
                        }
                        else
                        {
                            foreignPrincipal.Dispose();
                        }
                    }

                    needToRetry = true;
                    continue;
                }
                else
                {
                    // Not a group, nothing to recursively expand, so just return it.
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextForeign: using as currentForeignDE/currentForeignPrincipal");

                    DirectoryEntry foreignDE = (DirectoryEntry)foreignPrincipal.GetUnderlyingObject();

                    storeCtx.LoadDirectoryEntryAttributes(foreignDE);

                    if (!usersVisited.ContainsKey(foreignDE.Properties["distinguishedName"][0].ToString()))
                    {
                        usersVisited.Add(foreignDE.Properties["distinguishedName"][0].ToString(), true);
                        this.current = null;
                        this.currentForeignDE = null;
                        this.currentForeignPrincipal = foreignPrincipal;
                        return true;
                    }
                    else
                    {
                        foreignPrincipal.Dispose();
                    }

                    needToRetry = true;
                    continue;
                }
            }
            while (needToRetry);

            return false;
        }



        bool ExpandForeignGroupEnumerator()
        {
            Debug.Assert(this.recursive == true);
            GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                    "ADDNLinkedAttrSet",
                                    "ExpandForeignGroupEnumerator: there are {0} foreignGroups",
                                    this.foreignGroups.Count);
        
            GroupPrincipal foreignGroup = this.foreignGroups[0];
            this.foreignGroups.RemoveAt(0);

            // Since members of AD groups must be AD objects
            Debug.Assert(foreignGroup.Context.QueryCtx is ADStoreCtx);
            Debug.Assert(foreignGroup.UnderlyingObject is DirectoryEntry);
            Debug.Assert(((DirectoryEntry)foreignGroup.UnderlyingObject).Path.StartsWith("LDAP:", StringComparison.Ordinal));

            this.storeCtx = (ADStoreCtx) foreignGroup.Context.QueryCtx;

            //Here the foreignGroup object is removed from the foreignGroups collection.
            //and not used anymore. Hence, configure RangeRetriever to dispose the DirEntry on its dispose. 
            this.membersQueue.Enqueue(new RangeRetriever((DirectoryEntry)foreignGroup.UnderlyingObject, "member", true)); 

            string groupDN = (string) ((DirectoryEntry)foreignGroup.UnderlyingObject).Properties["distinguishedName"].Value;
            this.groupsVisited.Add(groupDN);

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "ExpandForeignGroupEnumerator: recursively processing {0}", groupDN);


            return true;
        }

        bool ExpandForeignGroupSearcher()
        {
            Debug.Assert(this.recursive == true);
            GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                    "ADDNLinkedAttrSet",
                                    "ExpandForeignGroupSearcher: there are {0} foreignGroups",
                                    this.foreignGroups.Count);
        
            GroupPrincipal foreignGroup = this.foreignGroups[0];
            this.foreignGroups.RemoveAt(0);

            // Since members of AD groups must be AD objects
            Debug.Assert(foreignGroup.Context.QueryCtx is ADStoreCtx);
            Debug.Assert(foreignGroup.UnderlyingObject is DirectoryEntry);
            Debug.Assert(((DirectoryEntry)foreignGroup.UnderlyingObject).Path.StartsWith("LDAP:", StringComparison.Ordinal));

            this.storeCtx = (ADStoreCtx) foreignGroup.Context.QueryCtx;

            // Queue up a searcher for the new group expansion.
            DirectorySearcher ds = SDSUtils.ConstructSearcher((DirectoryEntry)foreignGroup.UnderlyingObject);
            ds.Filter = "(objectClass=*)";
            ds.SearchScope = SearchScope.Base;
            ds.AttributeScopeQuery = "member";
            ds.CacheResults = false;

            this.memberSearchersQueue.Enqueue(ds);
                                    
            string groupDN = (string) ((DirectoryEntry)foreignGroup.UnderlyingObject).Properties["distinguishedName"].Value;
            this.groupsVisited.Add(groupDN);

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "ExpandForeignGroupSearcher: recursively processing {0}", groupDN);

            return true;
        }

        bool MoveNextQueryPrimaryGroupMember()
        {
            bool f = false;
        
            if (this.primaryGroupMembersSearcher != null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextQueryMember: have a searcher");
            
                if (this.queryMembersResults == null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextQueryMember: issuing query");
                
                    this.queryMembersResults = this.primaryGroupMembersSearcher.FindAll();

                    Debug.Assert(this.queryMembersResults != null);

                    this.queryMembersResultEnumerator = this.queryMembersResults.GetEnumerator();
                }

                f = this.queryMembersResultEnumerator.MoveNext();

                if (f)
                {
                    this.current = (SearchResult) this.queryMembersResultEnumerator.Current;
                    Debug.Assert(this.current != null);
                    
                    this.currentForeignDE = null;
                    this.currentForeignPrincipal = null;           

                    GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                            "ADDNLinkedAttrSet",
                                            "MoveNextQueryMember: got a result, using as current {0}",
                                            ((SearchResult)this.current).Path);                                                               
                }
                
            }

            return f;
        }

    	// Resets the enumerator to before the first result in the set.  This potentially can be an expensive
    	// operation, e.g., if doing a paged search, may need to re-retrieve the first page of results.
    	// As a special case, if the ResultSet is already at the very beginning, this is guaranteed to be
    	// a no-op.
    	override internal void Reset()
    	{
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Reset");    	        
    	
    	    if (!atBeginning)
    	    {
    	        this.usersVisited.Clear();
                this.groupsToVisit.Clear();
                string originalGroupDN = this.groupsVisited[0];
                this.groupsVisited.Clear();
                this.groupsVisited.Add(originalGroupDN);

                // clear the current enumerator
                this.members = null;
                this.membersEnum = null;

                // replace all items in the queue with the originals and reset them.
                if (null != this.originalMembers)
                {
                    this.membersQueue.Clear();
                    foreach (IEnumerable ie in this.originalMembers)
                    {
                        this.membersQueue.Enqueue(ie);
                        IEnumerator enumerator = ie.GetEnumerator();
                        enumerator.Reset();
                    }
                }

                expansionMode = originalExpansionMode;
                
                this.storeCtx = this.originalStoreCtx;

                this.current = null;
                if (this.primaryGroupDN != null)
                    returnedPrimaryGroup = false;

                this.foreignMembersCurrentGroup.Clear();
                this.fakePrincipalMembers.Clear();
                
                if ( null != foreignMembersToReturn )
                    this.foreignMembersToReturn.Clear();
                
                this.currentForeignPrincipal = null;
                this.currentForeignDE = null;

                this.foreignGroups.Clear();

                this.queryMembersResultEnumerator = null;
                if (this.queryMembersResults != null)
                {
                    this.queryMembersResults.Dispose();
                    this.queryMembersResults = null;
                }


                if (null != currentMembersSearcher)
                {
                    this.currentMembersSearcher.Dispose();
                    this.currentMembersSearcher = null;
                }
                
                this.memberSearchResultsEnumerator = null;
                if (this.memberSearchResults != null)
                {
                    this.memberSearchResults.Dispose();
                    this.memberSearchResults = null;
                }

                if (null != this.memberSearchersQueue)
                {
                    foreach (DirectorySearcher ds in this.memberSearchersQueue)
                    {
                        ds.Dispose();
                    }

                    this.memberSearchersQueue.Clear();

                    if (null != this.memberSearchersQueueOriginal)
                    {
                        foreach (DirectorySearcher ds in this.memberSearchersQueueOriginal)
                        {
                            this.memberSearchersQueue.Enqueue(ds);
                        }
                    }
                }

                this.atBeginning = true;
            }
    	}


        override internal ResultSetBookmark BookmarkAndReset()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Bookmarking");    	        
        
            ADDNLinkedAttrSetBookmark bookmark = new ADDNLinkedAttrSetBookmark();

            bookmark.usersVisited = this.usersVisited;
            this.usersVisited = new Dictionary<string, bool>();
            
            bookmark.groupsToVisit = this.groupsToVisit;
            this.groupsToVisit = new List<string>();

            string originalGroupDN = this.groupsVisited[0];
            bookmark.groupsVisited = this.groupsVisited;
            this.groupsVisited = new List<string>();
            this.groupsVisited.Add(originalGroupDN);

            bookmark.expansionMode = this.expansionMode;

            // bookmark the current enumerators
            bookmark.members = this.members;
            bookmark.membersEnum = this.membersEnum;

            // Clear the current enumerators for reset
            this.members = null;
            this.membersEnum = null;

            // Copy all enumerators in the queue over to the bookmark queue.
            if (null != this.membersQueue)
            {
                bookmark.membersQueue = new Queue<IEnumerable>(this.membersQueue.Count);
                foreach (IEnumerable ie in this.membersQueue)
                {
                    bookmark.membersQueue.Enqueue(ie);
                }
            }

            // Refill the original queue with the original enumerators and reset them
            if (null != this.membersQueue)
            {
                this.membersQueue.Clear();

                if (this.originalMembers != null)
                {
                    foreach (IEnumerable ie in this.originalMembers)
                    {
                        this.membersQueue.Enqueue(ie);
                        IEnumerator enumerator = ie.GetEnumerator();
                        enumerator.Reset();
                    }
                }
            }


            bookmark.storeCtx = this.storeCtx;

            expansionMode = originalExpansionMode;

            if (null != currentMembersSearcher)
            {
                this.currentMembersSearcher.Dispose();
                this.currentMembersSearcher = null;
            }            
            
            this.storeCtx = this.originalStoreCtx;

            bookmark.current = this.current;
            bookmark.returnedPrimaryGroup = this.returnedPrimaryGroup;
            this.current = null;
            if (this.primaryGroupDN != null)
                returnedPrimaryGroup = false;

            bookmark.foreignMembersCurrentGroup = this.foreignMembersCurrentGroup;   
            bookmark.fakePrincipalMembers = this.fakePrincipalMembers;   
            bookmark.foreignMembersToReturn = this.foreignMembersToReturn;   
            bookmark.currentForeignPrincipal = this.currentForeignPrincipal;
            bookmark.currentForeignDE = this.currentForeignDE;            
            this.foreignMembersCurrentGroup = new List<DirectoryEntry>();
            this.fakePrincipalMembers = new List<DirectoryEntry>();
            this.currentForeignDE = null;

            bookmark.foreignGroups = this.foreignGroups;
            this.foreignGroups = new List<GroupPrincipal>();

            bookmark.queryMembersResults = this.queryMembersResults;
            bookmark.queryMembersResultEnumerator = this.queryMembersResultEnumerator;
            this.queryMembersResults = null;
            this.queryMembersResultEnumerator = null;

            bookmark.memberSearchResults = this.memberSearchResults;
            bookmark.memberSearchResultsEnumerator = this.memberSearchResultsEnumerator;
            this.memberSearchResults = null;
            this.memberSearchResultsEnumerator = null;

            if (null != this.memberSearchersQueue)
            {
                bookmark.memberSearcherQueue = new Queue<DirectorySearcher>(this.memberSearchersQueue.Count);

                foreach (DirectorySearcher ds in this.memberSearchersQueue)
                {
                    bookmark.memberSearcherQueue.Enqueue(ds);
                }
            }

            if (null != this.memberSearchersQueueOriginal)
            {
                this.memberSearchersQueue.Clear();

                foreach (DirectorySearcher ds in this.memberSearchersQueueOriginal)
                {
                    this.memberSearchersQueue.Enqueue(ds);
                }
            }

            bookmark.atBeginning = this.atBeginning;
            this.atBeginning = true;

            return bookmark;
        }

        override internal void RestoreBookmark(ResultSetBookmark bookmark)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Restoring from bookmark");    	        
        
            Debug.Assert(bookmark is ADDNLinkedAttrSetBookmark);
            ADDNLinkedAttrSetBookmark adBookmark = (ADDNLinkedAttrSetBookmark) bookmark;

            this.usersVisited = adBookmark.usersVisited;
            this.groupsToVisit = adBookmark.groupsToVisit;
            this.groupsVisited = adBookmark.groupsVisited;
            this.storeCtx = adBookmark.storeCtx;
            this.current = adBookmark.current;
            this.returnedPrimaryGroup = adBookmark.returnedPrimaryGroup;
            this.foreignMembersCurrentGroup = adBookmark.foreignMembersCurrentGroup;
            this.fakePrincipalMembers = adBookmark.fakePrincipalMembers;
            this.foreignMembersToReturn = adBookmark.foreignMembersToReturn;
            this.currentForeignPrincipal = adBookmark.currentForeignPrincipal;
            this.currentForeignDE = adBookmark.currentForeignDE;
            this.foreignGroups = adBookmark.foreignGroups;
            if (this.queryMembersResults != null)
                this.queryMembersResults.Dispose();
            this.queryMembersResults = adBookmark.queryMembersResults;
            this.queryMembersResultEnumerator = adBookmark.queryMembersResultEnumerator;
            this.memberSearchResults = adBookmark.memberSearchResults;
            this.memberSearchResultsEnumerator = adBookmark.memberSearchResultsEnumerator;
            this.atBeginning = adBookmark.atBeginning; 
            this.expansionMode = adBookmark.expansionMode;

            // Replace enumerators
            this.members = adBookmark.members;
            this.membersEnum = adBookmark.membersEnum;

            // Replace the enumerator queue elements
            if (null != this.membersQueue)
            {
                this.membersQueue.Clear();

                if (null != adBookmark.membersQueue)
                {
                    foreach (IEnumerable ie in adBookmark.membersQueue)
                    {
                        this.membersQueue.Enqueue(ie);
                    }
                }
            }

            if (null != this.memberSearchersQueue)
            {
                foreach (DirectorySearcher ds in this.memberSearchersQueue)
                {
                    ds.Dispose();
                }

                this.memberSearchersQueue.Clear();

                if (null != adBookmark.memberSearcherQueue)
                {
                    foreach (DirectorySearcher ds in adBookmark.memberSearcherQueue)
                    {
                        this.memberSearchersQueue.Enqueue(ds);
                    }
                }

            }

        }

    	// IDisposable implementation
        public override void Dispose()
        {
            try
            {
                if (!this.disposed)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Dispose: disposing");    	        

                    if (this.primaryGroupMembersSearcher != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Dispose: disposing primaryGroupMembersSearcher");                
                        this.primaryGroupMembersSearcher.Dispose();
                    }

                    if (this.queryMembersResults != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Dispose: disposing queryMembersResults");                
                        this.queryMembersResults.Dispose();
                    }

                    if (this.currentMembersSearcher != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Dispose: disposing membersSearcher");                
                        this.currentMembersSearcher.Dispose();
                    }

                    if (this.memberSearchResults != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Dispose: disposing memberSearchResults");                
                        this.memberSearchResults.Dispose();
                    }

                    if (this.memberSearchersQueue != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Dispose: disposing memberSearchersQueue");                
                        foreach (DirectorySearcher ds in memberSearchersQueue)
                        {
                            ds.Dispose();
                        }

                        this.memberSearchersQueue.Clear();
                    }
                    IDisposable disposableMembers = this.members as IDisposable;
                    if (disposableMembers != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Dispose: disposing members Enumerable");
                        disposableMembers.Dispose();
                    }
                    IDisposable disposableMembersEnum = this.membersEnum as IDisposable;
                    if (disposableMembersEnum != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Dispose: disposing membersEnum Enumerator");
                        disposableMembersEnum.Dispose();
                    }
                    if (this.membersQueue != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Dispose: disposing membersQueue");
                        foreach (IEnumerable enumerable in membersQueue)
                        {
                            IDisposable disposableEnum = enumerable as IDisposable;
                            if (disposableEnum != null)
                            {
                                disposableEnum.Dispose();
                            }
                        }
                    }
                    if (this.foreignGroups != null)
                    {
                        foreach (GroupPrincipal gp in this.foreignGroups)
                        {
                            gp.Dispose();
                        }
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
        //
        //

        private UnsafeNativeMethods.IADsPathname pathCracker = null;
        private object pathLock = new object();
        Dictionary<string, bool> usersVisited = new Dictionary<string, bool>();
        
        // The 0th entry in this list is always the DN of the original group/user whose membership we're querying
        List<string> groupsVisited = new List<string>();
        
        List<string> groupsToVisit = new List<string>();

        protected Object current = null; // current member of the group (or current group of the user)
        
        bool returnedPrimaryGroup = false;
        string primaryGroupDN;                      // the DN of the user's PrimaryGroup (not included in this.members/originalMembers)

        bool recursive;

        Queue<IEnumerable> membersQueue = new Queue<IEnumerable>();
        IEnumerable members;            // the membership we're currently enumerating over
        Queue<IEnumerable> originalMembers = new Queue<IEnumerable>();    // the membership we started off with (before recursing)

        IEnumerator membersEnum = null;
        
        ADStoreCtx storeCtx;
        ADStoreCtx originalStoreCtx;

        bool atBeginning = true;

        bool disposed = false;

        // foreign
        // This contains a list of employees built while enumerating the current group.  These are FSP objects in the current domain and need to 
        // be translated to find out the domain that holds the actual object.
        List<DirectoryEntry> foreignMembersCurrentGroup = new List<DirectoryEntry>();
        // List of objects from the group tha are actual fake group objects.
        List<DirectoryEntry> fakePrincipalMembers = new List<DirectoryEntry>();
        // list of SIDs + store that have been translated.  These could be any principal object
        SidList foreignMembersToReturn = null;
        
        Principal currentForeignPrincipal = null;
        DirectoryEntry currentForeignDE = null;
        
        List<GroupPrincipal> foreignGroups = new List<GroupPrincipal>();

        // members based on a query (used for users who are group members by virtue of their primaryGroupId pointing to the group)
        DirectorySearcher primaryGroupMembersSearcher;
        SearchResultCollection queryMembersResults = null;
        IEnumerator queryMembersResultEnumerator = null;

        DirectorySearcher currentMembersSearcher = null;

        Queue<DirectorySearcher> memberSearchersQueue = new Queue<DirectorySearcher>();
        Queue<DirectorySearcher> memberSearchersQueueOriginal = new Queue<DirectorySearcher>();

        SearchResultCollection memberSearchResults = null;
        IEnumerator memberSearchResultsEnumerator = null;

        ExpansionMode expansionMode;
        ExpansionMode originalExpansionMode;
        
        string BuildPathFromDN(string dn)
        {
            string userSuppliedServername = this.storeCtx.UserSuppliedServerName;

            if ( null == pathCracker )
            {
                lock( pathLock  )
                {
                    if ( null == pathCracker )
                    {
                       UnsafeNativeMethods.Pathname pathNameObj = new UnsafeNativeMethods.Pathname();
                       pathCracker = (UnsafeNativeMethods.IADsPathname) pathNameObj;
                       pathCracker.EscapedMode = 2 /* ADS_ESCAPEDMODE_ON */;
                    }
                }
            }

            pathCracker.Set(dn, 4 /* ADS_SETTYPE_DN */);
            
            string escapedDn = pathCracker.Retrieve(7 /* ADS_FORMAT_X500_DN */);
            
            if (userSuppliedServername.Length > 0)            
                return "LDAP://" + this.storeCtx.UserSuppliedServerName + "/" + escapedDn;
            else
                return "LDAP://" + escapedDn;
        }
    }


    internal enum ExpansionMode
    {
        Enum = 0,
         ASQ = 1,
    }
    
    class ADDNLinkedAttrSetBookmark : ResultSetBookmark
    {
        public Dictionary<string, bool> usersVisited;
        public List<string> groupsToVisit;
        public List<string> groupsVisited;
        public IEnumerable members;
        public IEnumerator membersEnum = null;
        public Queue<IEnumerable> membersQueue;
        public ADStoreCtx storeCtx;
        public Object current;
        public bool returnedPrimaryGroup;
        public List<DirectoryEntry> foreignMembersCurrentGroup;
        public List<DirectoryEntry> fakePrincipalMembers;
        public SidList foreignMembersToReturn;
        public Principal currentForeignPrincipal;
        public DirectoryEntry currentForeignDE;        
        public List<GroupPrincipal> foreignGroups;
        public SearchResultCollection queryMembersResults;
        public IEnumerator queryMembersResultEnumerator;
        public SearchResultCollection memberSearchResults;
        public IEnumerator memberSearchResultsEnumerator;
        public bool atBeginning;        
        public ExpansionMode expansionMode;
        public Queue<DirectorySearcher> memberSearcherQueue;
    }
}


// #endif
