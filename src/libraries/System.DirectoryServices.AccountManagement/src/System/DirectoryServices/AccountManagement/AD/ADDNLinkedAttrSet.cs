// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.DirectoryServices;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Security.Principal;

namespace System.DirectoryServices.AccountManagement
{
    internal class ADDNLinkedAttrSet : BookmarkableResultSet
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
                            DirectorySearcher primaryGroupMembersSearcher,
                            bool recursive,
                            ADStoreCtx storeCtx)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                    "ADDNLinkedAttrSet",
                                    "ADDNLinkedAttrSet: groupDN={0}, primaryGroupDN={1}, recursive={2}, PG queryFilter={3}, PG queryBase={4}",
                                    groupDN,
                                    primaryGroupDN ?? "NULL",
                                    recursive,
                                    (primaryGroupMembersSearcher != null ? primaryGroupMembersSearcher.Filter : "NULL"),
                                    (primaryGroupMembersSearcher != null ? primaryGroupMembersSearcher.SearchRoot.Path : "NULL"));

            _groupsVisited.Add(groupDN);    // so we don't revisit it
            _recursive = recursive;
            _storeCtx = storeCtx;
            _originalStoreCtx = storeCtx;

            if (null != members)
            {
                foreach (IEnumerable enumerator in members)
                {
                    _membersQueue.Enqueue(enumerator);
                    _originalMembers.Enqueue(enumerator);
                }
            }

            _members = null;

            _currentMembersSearcher = null;
            _primaryGroupDN = primaryGroupDN;
            if (primaryGroupDN == null)
                _returnedPrimaryGroup = true;    // so we don't bother trying to return the primary group

            _primaryGroupMembersSearcher = primaryGroupMembersSearcher;

            _expansionMode = ExpansionMode.Enum;
            _originalExpansionMode = _expansionMode;
        }

        internal ADDNLinkedAttrSet(
                            string groupDN,
                            DirectorySearcher[] membersSearcher,
                            string primaryGroupDN,
                            DirectorySearcher primaryGroupMembersSearcher,
                            bool recursive,
                            ADStoreCtx storeCtx)

        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                    "ADDNLinkedAttrSet",
                                    "ADDNLinkedAttrSet: groupDN={0}, primaryGroupDN={1}, recursive={2}, M queryFilter={3}, M queryBase={4}, PG queryFilter={5}, PG queryBase={6}",
                                    groupDN,
                                    primaryGroupDN ?? "NULL",
                                    recursive,
                                    (membersSearcher != null ? membersSearcher[0].Filter : "NULL"),
                                    (membersSearcher != null ? membersSearcher[0].SearchRoot.Path : "NULL"),
                                    (primaryGroupMembersSearcher != null ? primaryGroupMembersSearcher.Filter : "NULL"),
                                    (primaryGroupMembersSearcher != null ? primaryGroupMembersSearcher.SearchRoot.Path : "NULL"));

            _groupsVisited.Add(groupDN);    // so we don't revisit it
            _recursive = recursive;
            _storeCtx = storeCtx;
            _originalStoreCtx = storeCtx;

            _members = null;
            _originalMembers = null;
            _membersEnum = null;

            _primaryGroupDN = primaryGroupDN;
            if (primaryGroupDN == null)
                _returnedPrimaryGroup = true;    // so we don't bother trying to return the primary group

            if (null != membersSearcher)
            {
                foreach (DirectorySearcher ds in membersSearcher)
                {
                    _memberSearchersQueue.Enqueue(ds);
                    _memberSearchersQueueOriginal.Enqueue(ds);
                }
            }

            _currentMembersSearcher = null;

            _primaryGroupMembersSearcher = primaryGroupMembersSearcher;

            _expansionMode = ExpansionMode.ASQ;
            _originalExpansionMode = _expansionMode;
        }

        // Return the principal we're positioned at as a Principal object.
        // Need to use our StoreCtx's GetAsPrincipal to convert the native object to a Principal
        internal override object CurrentAsPrincipal
        {
            get
            {
                if (this.current != null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "CurrentAsPrincipal: using current");
                    if (this.current is DirectoryEntry)
                        return ADUtils.DirectoryEntryAsPrincipal((DirectoryEntry)this.current, _storeCtx);
                    else
                    {
                        return ADUtils.SearchResultAsPrincipal((SearchResult)this.current, _storeCtx, null);
                    }
                }
                else
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "CurrentAsPrincipal: using currentForeignPrincipal");
                    Debug.Assert(_currentForeignPrincipal != null);
                    return _currentForeignPrincipal;
                }
            }
        }

        // Advance the enumerator to the next principal in the result set, pulling in additional pages
        // of results (or ranges of attribute values) as needed.
        // Returns true if successful, false if no more results to return.
        internal override bool MoveNext()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Entering MoveNext");

            _atBeginning = false;

            bool needToRetry;
            bool f = false;

            do
            {
                needToRetry = false;
                // reset our found state.  If we are restarting the loop we don't have a current principal yet.
                f = false;

                if (!_returnedPrimaryGroup)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNext: trying PrimaryGroup DN");
                    f = MoveNextPrimaryGroupDN();
                }

                if (!f)
                {
                    if (_expansionMode == ExpansionMode.ASQ)
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

        private bool MoveNextPrimaryGroupDN()
        {
            // Do the special primary group ID processing if we haven't yet returned the primary group.
            Debug.Assert(_primaryGroupDN != null);

            this.current = SDSUtils.BuildDirectoryEntry(
                                        BuildPathFromDN(_primaryGroupDN),
                                        _storeCtx.Credentials,
                                        _storeCtx.AuthTypes);

            _storeCtx.InitializeNewDirectoryOptions((DirectoryEntry)this.current);

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberSearcher: returning primary group {0}", ((DirectoryEntry)this.current).Path);

            _currentForeignDE = null;
            _currentForeignPrincipal = null;

            _returnedPrimaryGroup = true;
            return true;
        }

        private bool GetNextSearchResult()
        {
            bool memberFound = false;

            do
            {
                if (_currentMembersSearcher == null)
                {
                    Debug.Assert(_memberSearchersQueue != null);

                    if (_memberSearchersQueue.Count == 0)
                    {
                        // We are out of searchers in the queue.
                        return false;
                    }
                    else
                    {
                        // Remove the next searcher from the queue and place it in the current search variable.
                        _currentMembersSearcher = _memberSearchersQueue.Dequeue();
                        _memberSearchResults = _currentMembersSearcher.FindAll();
                        Debug.Assert(_memberSearchResults != null);
                        _memberSearchResultsEnumerator = _memberSearchResults.GetEnumerator();
                    }
                }

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextQueryMember: have a searcher");

                memberFound = _memberSearchResultsEnumerator.MoveNext();

                // The search is complete.
                // Dipose the searcher and search results.
                if (!memberFound)
                {
                    _currentMembersSearcher.Dispose();
                    _currentMembersSearcher = null;
                    _memberSearchResults.Dispose();
                    _memberSearchResults = null;
                }
            } while (!memberFound);

            return memberFound;
        }

        private bool MoveNextMemberSearcher()
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
                    SearchResult currentSR = (SearchResult)_memberSearchResultsEnumerator.Current;

                    // Got a member from this group (or, got a group of which we're a member).
                    // Create a DirectoryEntry for it.
                    string memberDN = (string)currentSR.Properties["distinguishedName"][0];

                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberSearcher: got a value from the enumerator: {0}", memberDN);

                    // Make sure the member is a principal
                    if ((!ADUtils.IsOfObjectClass(currentSR, "group")) &&
                         (!ADUtils.IsOfObjectClass(currentSR, "user")) &&     // includes computer as well
                         (!ADUtils.IsOfObjectClass(currentSR, "foreignSecurityPrincipal")))
                    {
                        // We found a member, but it's not a principal type.  Skip it.
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberSearcher: not a principal, skipping");
                        needToRetry = true;
                    }
                    // If we're processing recursively, and the member is a group, we DON'T return it,
                    // but rather treat it as something to recursively visit later
                    // (unless we've already visited the group previously)
                    else if (_recursive && ADUtils.IsOfObjectClass(currentSR, "group"))
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberSearcher: adding to groupsToVisit");

                        if (!_groupsVisited.Contains(memberDN) && !_groupsToVisit.Contains(memberDN))
                            _groupsToVisit.Add(memberDN);

                        // and go on to the next member....
                        needToRetry = true;
                    }
                    else if (_recursive && ADUtils.IsOfObjectClass(currentSR, "foreignSecurityPrincipal"))
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberSearcher: foreign principal, adding to foreignMembers");

                        // If we haven't seen this FPO yet then add it to the seen user database.
                        if (!_usersVisited.ContainsKey(currentSR.Properties["distinguishedName"][0].ToString()))
                        {
                            // The FPO might represent a group, in which case we should recursively enumerate its
                            // membership.  So save it off for later processing.
                            _foreignMembersCurrentGroup.Add(currentSR.GetDirectoryEntry());
                            _usersVisited.Add(currentSR.Properties["distinguishedName"][0].ToString(), true);
                        }

                        // and go on to the next member....
                        needToRetry = true;
                    }
                    else
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberSearcher: using as current");

                        // Check to see if we have already seen this user during the enumeration
                        // If so then move on to the next user.  If not then return it as current.
                        if (!_usersVisited.ContainsKey(currentSR.Properties["distinguishedName"][0].ToString()))
                        {
                            this.current = currentSR;
                            _currentForeignDE = null;
                            _currentForeignPrincipal = null;
                            _usersVisited.Add(currentSR.Properties["distinguishedName"][0].ToString(), true);
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
                    // an ASQ search against  member and start enumerating those results.
                    if (_recursive)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                                "ADDNLinkedAttrSet",
                                                "MoveNextMemberSearcher: recursive processing, groupsToVisit={0}",
                                                _groupsToVisit.Count);

                        if (_groupsToVisit.Count > 0)
                        {
                            // Pull off the next group to visit
                            string groupDN = _groupsToVisit[0];
                            _groupsToVisit.RemoveAt(0);
                            _groupsVisited.Add(groupDN);

                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberSearcher: recursively processing {0}", groupDN);

                            // get the membership of this new group
                            DirectoryEntry groupDE = SDSUtils.BuildDirectoryEntry(BuildPathFromDN(groupDN), _storeCtx.Credentials, _storeCtx.AuthTypes);

                            _storeCtx.InitializeNewDirectoryOptions(groupDE);

                            // Queue up a searcher for the new group expansion.
                            DirectorySearcher ds = SDSUtils.ConstructSearcher(groupDE);
                            ds.Filter = "(objectClass=*)";
                            ds.SearchScope = SearchScope.Base;
                            ds.AttributeScopeQuery = "member";
                            ds.CacheResults = false;

                            _memberSearchersQueue.Enqueue(ds);

                            // and go on to the first member of this new group.
                            needToRetry = true;
                        }
                    }
                }
            }
            while (needToRetry);

            return f;
        }

        private bool GetNextEnum()
        {
            bool memberFound = false;

            do
            {
                if (null == _members)
                {
                    if (_membersQueue.Count == 0)
                    {
                        return false;
                    }

                    _members = _membersQueue.Dequeue();
                    _membersEnum = _members.GetEnumerator();
                }

                memberFound = _membersEnum.MoveNext();

                if (!memberFound)
                {
                    (_members as IDisposable)?.Dispose();
                    (_membersEnum as IDisposable)?.Dispose();
                    _members = null;
                    _membersEnum = null;
                }
            } while (!memberFound);

            return memberFound;
        }

        private bool MoveNextMemberEnum()
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
                        string memberDN = (string)_membersEnum.Current;

                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberEnum: got a value from the enumerator: {0}", memberDN);

                        memberDE = SDSUtils.BuildDirectoryEntry(
                                                        BuildPathFromDN(memberDN),
                                                        _storeCtx.Credentials,
                                                        _storeCtx.AuthTypes);

                        _storeCtx.InitializeNewDirectoryOptions(memberDE);

                        _storeCtx.LoadDirectoryEntryAttributes(memberDE);

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
                        else if (_recursive && ADUtils.IsOfObjectClass(memberDE, "group"))
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberEnum: adding to groupsToVisit");

                            if (!_groupsVisited.Contains(memberDN) && !_groupsToVisit.Contains(memberDN))
                                _groupsToVisit.Add(memberDN);

                            // and go on to the next member....
                            needToRetry = true;
                            disposeMemberDE = true; //Since recursive is set to true, we do not return groups. So mark it for dispose.
                        }
                        else if (_recursive && ADUtils.IsOfObjectClass(memberDE, "foreignSecurityPrincipal"))
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberEnum: foreign principal, adding to foreignMembers");

                            // If we haven't seen this FPO yet then add it to the seen user database.
                            if (!_usersVisited.ContainsKey(memberDE.Properties["distinguishedName"][0].ToString()))
                            {
                                // The FPO might represent a group, in which case we should recursively enumerate its
                                // membership.  So save it off for later processing.
                                _foreignMembersCurrentGroup.Add(memberDE);
                                _usersVisited.Add(memberDE.Properties["distinguishedName"][0].ToString(), true);
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
                            if (!_usersVisited.ContainsKey(memberDE.Properties["distinguishedName"][0].ToString()))
                            {
                                this.current = memberDE;
                                _currentForeignDE = null;
                                _currentForeignPrincipal = null;
                                _usersVisited.Add(memberDE.Properties["distinguishedName"][0].ToString(), true);
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
                    if (_recursive)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                                "ADDNLinkedAttrSet",
                                                "MoveNextLocal: recursive processing, groupsToVisit={0}",
                                                _groupsToVisit.Count);

                        if (_groupsToVisit.Count > 0)
                        {
                            // Pull off the next group to visit
                            string groupDN = _groupsToVisit[0];
                            _groupsToVisit.RemoveAt(0);
                            _groupsVisited.Add(groupDN);

                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextMemberEnum: recursively processing {0}", groupDN);

                            // get the membership of this new group
                            DirectoryEntry groupDE = SDSUtils.BuildDirectoryEntry(
                                                                    BuildPathFromDN(groupDN),
                                                    _storeCtx.Credentials,
                                                    _storeCtx.AuthTypes);

                            _storeCtx.InitializeNewDirectoryOptions(groupDE);

                            // set up for the next round of enumeration
                            //Here a new DirectoryEntry object is created and passed
                            //to RangeRetriever object. Hence, configure
                            //RangeRetriever to dispose the DirEntry on its dispose.
                            _membersQueue.Enqueue(new RangeRetriever(groupDE, "member", true));

                            // and go on to the first member of this new group....
                            needToRetry = true;
                        }
                    }
                }
            }
            while (needToRetry);

            return f;
        }

        private void TranslateForeignMembers()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADDNLinkedAttrSet", "TranslateForeignMembers: Translating foreign members");

            List<byte[]> sidList = new List<byte[]>(_foreignMembersCurrentGroup.Count);

            // Foreach foreign principal retrieve the sid.
            // If the SID is for a fake object we have to track it separately.  If we were attempt to translate it
            // it would fail and not be returned and we would lose it.
            // Once we have a list of sids then translate them against the target store in one call.
            foreach (DirectoryEntry de in _foreignMembersCurrentGroup)
            {
                // Get the SID of the foreign principal
                if (de.Properties["objectSid"].Count == 0)
                {
                    throw new PrincipalOperationException(SR.ADStoreCtxCantRetrieveObjectSidForCrossStore);
                }

                byte[] sid = (byte[])de.Properties["objectSid"].Value;

                // What type of SID is it?
                SidType sidType = Utils.ClassifySID(sid);

                if (sidType == SidType.FakeObject)
                {
                    //Add the foreign member DirectoryEntry to fakePrincipalMembers list for further translation
                    //This de will be disposed after completing the translation by another code block.
                    _fakePrincipalMembers.Add(de);

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
            _foreignMembersToReturn = new SidList(sidList, _storeCtx.DnsHostName, _storeCtx.Credentials);

            // We have translated the sids so clear the group now.
            _foreignMembersCurrentGroup.Clear();
        }

        private bool MoveNextForeign(ref bool outerNeedToRetry)
        {
            outerNeedToRetry = false;
            bool needToRetry;
            Principal foreignPrincipal;
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Entering MoveNextForeign");

            do
            {
                needToRetry = false;

                if (_foreignMembersCurrentGroup.Count > 0)
                {
                    TranslateForeignMembers();
                }

                if (_fakePrincipalMembers.Count > 0)
                {
                    foreignPrincipal = _storeCtx.ConstructFakePrincipalFromSID((byte[])_fakePrincipalMembers[0].Properties["objectSid"].Value);
                    _fakePrincipalMembers[0].Dispose();
                    _fakePrincipalMembers.RemoveAt(0);
                }
                else if ((_foreignMembersToReturn != null) && (_foreignMembersToReturn.Length > 0))
                {
                    StoreCtx foreignStoreCtx;

                    SidListEntry foreignSid = _foreignMembersToReturn[0];

                    // sidIssuerName is null only if SID was not resolved
                    // return a unknown principal back
                    if (null == foreignSid.sidIssuerName)
                    {
                        // create and return the unknown principal if it is not yet present in usersVisited
                        if (!_usersVisited.ContainsKey(foreignSid.name))
                        {
                            byte[] sid = Utils.ConvertNativeSidToByteArray(foreignSid.pSid);
                            UnknownPrincipal unknownPrincipal = UnknownPrincipal.CreateUnknownPrincipal(_storeCtx.OwningContext, sid, foreignSid.name);
                            _usersVisited.Add(foreignSid.name, true);
                            this.current = null;
                            _currentForeignDE = null;
                            _currentForeignPrincipal = unknownPrincipal;
                            // remove the current member
                            _foreignMembersToReturn.RemoveAt(0);
                            return true;
                        }

                        // remove the current member
                        _foreignMembersToReturn.RemoveAt(0);

                        needToRetry = true;
                        continue;
                    }

                    SidType sidType = Utils.ClassifySID(foreignSid.pSid);

                    if (sidType == SidType.RealObjectFakeDomain)
                    {
                        // This is a BUILTIN object.  It's a real object on the store we're connected to, but LookupSid
                        // will tell us it's a member of the BUILTIN domain.  Resolve it as a principal on our store.
                        GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADDNLinkedAttrSet", "MoveNextForeign: builtin principal");
                        foreignStoreCtx = _storeCtx;
                    }
                    else
                    {
                        ContextOptions remoteOptions = DefaultContextOptions.ADDefaultContextOption;

                        PrincipalContext remoteCtx = SDSCache.Domain.GetContext(foreignSid.sidIssuerName, _storeCtx.Credentials, remoteOptions);
                        foreignStoreCtx = remoteCtx.QueryCtx;
                    }

                    foreignPrincipal = foreignStoreCtx.FindPrincipalByIdentRef(
                                                     typeof(Principal),
                                                     UrnScheme.SidScheme,
                                                     (new SecurityIdentifier(Utils.ConvertNativeSidToByteArray(_foreignMembersToReturn[0].pSid), 0)).ToString(),
                                                     DateTime.UtcNow);

                    if (null == foreignPrincipal)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADDNLinkedAttrSet", "MoveNextForeign: no matching principal");
                        throw new PrincipalOperationException(SR.ADStoreCtxFailedFindCrossStoreTarget);
                    }

                    _foreignMembersToReturn.RemoveAt(0);
                }
                else
                {
                    // We don't have any more foreign principals to return so start with the foreign groups
                    if (_foreignGroups.Count > 0)
                    {
                        outerNeedToRetry = true;

                        // Determine the domainFunctionalityMode of the foreign domain.  If they are W2k or not a global group then we can't use ASQ.
                        if (_foreignGroups[0].Context.ServerInformation.OsVersion == DomainControllerMode.Win2k ||
                            _foreignGroups[0].GroupScope != GroupScope.Global)
                        {
                            _expansionMode = ExpansionMode.Enum;
                            return ExpandForeignGroupEnumerator();
                        }
                        else
                        {
                            _expansionMode = ExpansionMode.ASQ;
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

                        if (!_groupsVisited.Contains(groupDN) && !_groupsToVisit.Contains(groupDN))
                        {
                            _foreignGroups.Add((GroupPrincipal)foreignPrincipal);
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

                    _storeCtx.LoadDirectoryEntryAttributes(foreignDE);

                    if (!_usersVisited.ContainsKey(foreignDE.Properties["distinguishedName"][0].ToString()))
                    {
                        _usersVisited.Add(foreignDE.Properties["distinguishedName"][0].ToString(), true);
                        this.current = null;
                        _currentForeignDE = null;
                        _currentForeignPrincipal = foreignPrincipal;
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

        private bool ExpandForeignGroupEnumerator()
        {
            Debug.Assert(_recursive);
            GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                    "ADDNLinkedAttrSet",
                                    "ExpandForeignGroupEnumerator: there are {0} foreignGroups",
                                    _foreignGroups.Count);

            GroupPrincipal foreignGroup = _foreignGroups[0];
            _foreignGroups.RemoveAt(0);

            // Since members of AD groups must be AD objects
            Debug.Assert(foreignGroup.Context.QueryCtx is ADStoreCtx);
            Debug.Assert(foreignGroup.UnderlyingObject is DirectoryEntry);
            Debug.Assert(((DirectoryEntry)foreignGroup.UnderlyingObject).Path.StartsWith("LDAP:", StringComparison.Ordinal));

            _storeCtx = (ADStoreCtx)foreignGroup.Context.QueryCtx;

            //Here the foreignGroup object is removed from the foreignGroups collection.
            //and not used anymore. Hence, configure RangeRetriever to dispose the DirEntry on its dispose.
            _membersQueue.Enqueue(new RangeRetriever((DirectoryEntry)foreignGroup.UnderlyingObject, "member", true));

            string groupDN = (string)((DirectoryEntry)foreignGroup.UnderlyingObject).Properties["distinguishedName"].Value;
            _groupsVisited.Add(groupDN);

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "ExpandForeignGroupEnumerator: recursively processing {0}", groupDN);

            return true;
        }

        private bool ExpandForeignGroupSearcher()
        {
            Debug.Assert(_recursive);
            GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                    "ADDNLinkedAttrSet",
                                    "ExpandForeignGroupSearcher: there are {0} foreignGroups",
                                    _foreignGroups.Count);

            GroupPrincipal foreignGroup = _foreignGroups[0];
            _foreignGroups.RemoveAt(0);

            // Since members of AD groups must be AD objects
            Debug.Assert(foreignGroup.Context.QueryCtx is ADStoreCtx);
            Debug.Assert(foreignGroup.UnderlyingObject is DirectoryEntry);
            Debug.Assert(((DirectoryEntry)foreignGroup.UnderlyingObject).Path.StartsWith("LDAP:", StringComparison.Ordinal));

            _storeCtx = (ADStoreCtx)foreignGroup.Context.QueryCtx;

            // Queue up a searcher for the new group expansion.
            DirectorySearcher ds = SDSUtils.ConstructSearcher((DirectoryEntry)foreignGroup.UnderlyingObject);
            ds.Filter = "(objectClass=*)";
            ds.SearchScope = SearchScope.Base;
            ds.AttributeScopeQuery = "member";
            ds.CacheResults = false;

            _memberSearchersQueue.Enqueue(ds);

            string groupDN = (string)((DirectoryEntry)foreignGroup.UnderlyingObject).Properties["distinguishedName"].Value;
            _groupsVisited.Add(groupDN);

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "ExpandForeignGroupSearcher: recursively processing {0}", groupDN);

            return true;
        }

        private bool MoveNextQueryPrimaryGroupMember()
        {
            bool f = false;

            if (_primaryGroupMembersSearcher != null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextQueryMember: have a searcher");

                if (_queryMembersResults == null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "MoveNextQueryMember: issuing query");

                    _queryMembersResults = _primaryGroupMembersSearcher.FindAll();

                    Debug.Assert(_queryMembersResults != null);

                    _queryMembersResultEnumerator = _queryMembersResults.GetEnumerator();
                }

                f = _queryMembersResultEnumerator.MoveNext();

                if (f)
                {
                    this.current = (SearchResult)_queryMembersResultEnumerator.Current;
                    Debug.Assert(this.current != null);

                    _currentForeignDE = null;
                    _currentForeignPrincipal = null;

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
        internal override void Reset()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Reset");

            if (!_atBeginning)
            {
                _usersVisited.Clear();
                _groupsToVisit.Clear();
                string originalGroupDN = _groupsVisited[0];
                _groupsVisited.Clear();
                _groupsVisited.Add(originalGroupDN);

                // clear the current enumerator
                _members = null;
                _membersEnum = null;

                // replace all items in the queue with the originals and reset them.
                if (null != _originalMembers)
                {
                    _membersQueue.Clear();
                    foreach (IEnumerable ie in _originalMembers)
                    {
                        _membersQueue.Enqueue(ie);
                        IEnumerator enumerator = ie.GetEnumerator();
                        enumerator.Reset();
                    }
                }

                _expansionMode = _originalExpansionMode;

                _storeCtx = _originalStoreCtx;

                this.current = null;
                if (_primaryGroupDN != null)
                    _returnedPrimaryGroup = false;

                _foreignMembersCurrentGroup.Clear();
                _fakePrincipalMembers.Clear();

                _foreignMembersToReturn?.Clear();

                _currentForeignPrincipal = null;
                _currentForeignDE = null;

                _foreignGroups.Clear();

                _queryMembersResultEnumerator = null;
                if (_queryMembersResults != null)
                {
                    _queryMembersResults.Dispose();
                    _queryMembersResults = null;
                }

                if (null != _currentMembersSearcher)
                {
                    _currentMembersSearcher.Dispose();
                    _currentMembersSearcher = null;
                }

                _memberSearchResultsEnumerator = null;
                if (_memberSearchResults != null)
                {
                    _memberSearchResults.Dispose();
                    _memberSearchResults = null;
                }

                if (null != _memberSearchersQueue)
                {
                    foreach (DirectorySearcher ds in _memberSearchersQueue)
                    {
                        ds.Dispose();
                    }

                    _memberSearchersQueue.Clear();

                    if (null != _memberSearchersQueueOriginal)
                    {
                        foreach (DirectorySearcher ds in _memberSearchersQueueOriginal)
                        {
                            _memberSearchersQueue.Enqueue(ds);
                        }
                    }
                }

                _atBeginning = true;
            }
        }

        internal override ResultSetBookmark BookmarkAndReset()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Bookmarking");

            ADDNLinkedAttrSetBookmark bookmark = new ADDNLinkedAttrSetBookmark();

            bookmark.usersVisited = _usersVisited;
            _usersVisited = new Dictionary<string, bool>();

            bookmark.groupsToVisit = _groupsToVisit;
            _groupsToVisit = new List<string>();

            string originalGroupDN = _groupsVisited[0];
            bookmark.groupsVisited = _groupsVisited;
            _groupsVisited = new List<string>();
            _groupsVisited.Add(originalGroupDN);

            bookmark.expansionMode = _expansionMode;

            // bookmark the current enumerators
            bookmark.members = _members;
            bookmark.membersEnum = _membersEnum;

            // Clear the current enumerators for reset
            _members = null;
            _membersEnum = null;

            // Copy all enumerators in the queue over to the bookmark queue.
            if (null != _membersQueue)
            {
                bookmark.membersQueue = new Queue<IEnumerable>(_membersQueue.Count);
                foreach (IEnumerable ie in _membersQueue)
                {
                    bookmark.membersQueue.Enqueue(ie);
                }
            }

            // Refill the original queue with the original enumerators and reset them
            if (null != _membersQueue)
            {
                _membersQueue.Clear();

                if (_originalMembers != null)
                {
                    foreach (IEnumerable ie in _originalMembers)
                    {
                        _membersQueue.Enqueue(ie);
                        IEnumerator enumerator = ie.GetEnumerator();
                        enumerator.Reset();
                    }
                }
            }

            bookmark.storeCtx = _storeCtx;

            _expansionMode = _originalExpansionMode;

            if (null != _currentMembersSearcher)
            {
                _currentMembersSearcher.Dispose();
                _currentMembersSearcher = null;
            }

            _storeCtx = _originalStoreCtx;

            bookmark.current = this.current;
            bookmark.returnedPrimaryGroup = _returnedPrimaryGroup;
            this.current = null;
            if (_primaryGroupDN != null)
                _returnedPrimaryGroup = false;

            bookmark.foreignMembersCurrentGroup = _foreignMembersCurrentGroup;
            bookmark.fakePrincipalMembers = _fakePrincipalMembers;
            bookmark.foreignMembersToReturn = _foreignMembersToReturn;
            bookmark.currentForeignPrincipal = _currentForeignPrincipal;
            bookmark.currentForeignDE = _currentForeignDE;
            _foreignMembersCurrentGroup = new List<DirectoryEntry>();
            _fakePrincipalMembers = new List<DirectoryEntry>();
            _currentForeignDE = null;

            bookmark.foreignGroups = _foreignGroups;
            _foreignGroups = new List<GroupPrincipal>();

            bookmark.queryMembersResults = _queryMembersResults;
            bookmark.queryMembersResultEnumerator = _queryMembersResultEnumerator;
            _queryMembersResults = null;
            _queryMembersResultEnumerator = null;

            bookmark.memberSearchResults = _memberSearchResults;
            bookmark.memberSearchResultsEnumerator = _memberSearchResultsEnumerator;
            _memberSearchResults = null;
            _memberSearchResultsEnumerator = null;

            if (null != _memberSearchersQueue)
            {
                bookmark.memberSearcherQueue = new Queue<DirectorySearcher>(_memberSearchersQueue.Count);

                foreach (DirectorySearcher ds in _memberSearchersQueue)
                {
                    bookmark.memberSearcherQueue.Enqueue(ds);
                }
            }

            if (null != _memberSearchersQueueOriginal)
            {
                _memberSearchersQueue.Clear();

                foreach (DirectorySearcher ds in _memberSearchersQueueOriginal)
                {
                    _memberSearchersQueue.Enqueue(ds);
                }
            }

            bookmark.atBeginning = _atBeginning;
            _atBeginning = true;

            return bookmark;
        }

        internal override void RestoreBookmark(ResultSetBookmark bookmark)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Restoring from bookmark");

            Debug.Assert(bookmark is ADDNLinkedAttrSetBookmark);
            ADDNLinkedAttrSetBookmark adBookmark = (ADDNLinkedAttrSetBookmark)bookmark;

            _usersVisited = adBookmark.usersVisited;
            _groupsToVisit = adBookmark.groupsToVisit;
            _groupsVisited = adBookmark.groupsVisited;
            _storeCtx = adBookmark.storeCtx;
            this.current = adBookmark.current;
            _returnedPrimaryGroup = adBookmark.returnedPrimaryGroup;
            _foreignMembersCurrentGroup = adBookmark.foreignMembersCurrentGroup;
            _fakePrincipalMembers = adBookmark.fakePrincipalMembers;
            _foreignMembersToReturn = adBookmark.foreignMembersToReturn;
            _currentForeignPrincipal = adBookmark.currentForeignPrincipal;
            _currentForeignDE = adBookmark.currentForeignDE;
            _foreignGroups = adBookmark.foreignGroups;
            _queryMembersResults?.Dispose();
            _queryMembersResults = adBookmark.queryMembersResults;
            _queryMembersResultEnumerator = adBookmark.queryMembersResultEnumerator;
            _memberSearchResults = adBookmark.memberSearchResults;
            _memberSearchResultsEnumerator = adBookmark.memberSearchResultsEnumerator;
            _atBeginning = adBookmark.atBeginning;
            _expansionMode = adBookmark.expansionMode;

            // Replace enumerators
            _members = adBookmark.members;
            _membersEnum = adBookmark.membersEnum;

            // Replace the enumerator queue elements
            if (null != _membersQueue)
            {
                _membersQueue.Clear();

                if (null != adBookmark.membersQueue)
                {
                    foreach (IEnumerable ie in adBookmark.membersQueue)
                    {
                        _membersQueue.Enqueue(ie);
                    }
                }
            }

            if (null != _memberSearchersQueue)
            {
                foreach (DirectorySearcher ds in _memberSearchersQueue)
                {
                    ds.Dispose();
                }

                _memberSearchersQueue.Clear();

                if (null != adBookmark.memberSearcherQueue)
                {
                    foreach (DirectorySearcher ds in adBookmark.memberSearcherQueue)
                    {
                        _memberSearchersQueue.Enqueue(ds);
                    }
                }
            }
        }

        // IDisposable implementation
        public override void Dispose()
        {
            try
            {
                if (!_disposed)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Dispose: disposing");

                    if (_primaryGroupMembersSearcher != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Dispose: disposing primaryGroupMembersSearcher");
                        _primaryGroupMembersSearcher.Dispose();
                    }

                    if (_queryMembersResults != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Dispose: disposing queryMembersResults");
                        _queryMembersResults.Dispose();
                    }

                    if (_currentMembersSearcher != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Dispose: disposing membersSearcher");
                        _currentMembersSearcher.Dispose();
                    }

                    if (_memberSearchResults != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Dispose: disposing memberSearchResults");
                        _memberSearchResults.Dispose();
                    }

                    if (_memberSearchersQueue != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Dispose: disposing memberSearchersQueue");
                        foreach (DirectorySearcher ds in _memberSearchersQueue)
                        {
                            ds.Dispose();
                        }

                        _memberSearchersQueue.Clear();
                    }
                    IDisposable disposableMembers = _members as IDisposable;
                    if (disposableMembers != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Dispose: disposing members Enumerable");
                        disposableMembers.Dispose();
                    }
                    IDisposable disposableMembersEnum = _membersEnum as IDisposable;
                    if (disposableMembersEnum != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Dispose: disposing membersEnum Enumerator");
                        disposableMembersEnum.Dispose();
                    }
                    if (_membersQueue != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADDNLinkedAttrSet", "Dispose: disposing membersQueue");
                        foreach (IEnumerable enumerable in _membersQueue)
                        {
                            (enumerable as IDisposable)?.Dispose();
                        }
                    }
                    if (_foreignGroups != null)
                    {
                        foreach (GroupPrincipal gp in _foreignGroups)
                        {
                            gp.Dispose();
                        }
                    }

                    _disposed = true;
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

        private UnsafeNativeMethods.IADsPathname _pathCracker;
        private readonly object _pathLock = new object();
        private Dictionary<string, bool> _usersVisited = new Dictionary<string, bool>();

        // The 0th entry in this list is always the DN of the original group/user whose membership we're querying
        private List<string> _groupsVisited = new List<string>();

        private List<string> _groupsToVisit = new List<string>();

        protected object current; // current member of the group (or current group of the user)

        private bool _returnedPrimaryGroup;
        private readonly string _primaryGroupDN;                      // the DN of the user's PrimaryGroup (not included in this.members/originalMembers)

        private readonly bool _recursive;

        private readonly Queue<IEnumerable> _membersQueue = new Queue<IEnumerable>();
        private IEnumerable _members;            // the membership we're currently enumerating over
        private readonly Queue<IEnumerable> _originalMembers = new Queue<IEnumerable>();    // the membership we started off with (before recursing)

        private IEnumerator _membersEnum;

        private ADStoreCtx _storeCtx;
        private readonly ADStoreCtx _originalStoreCtx;

        private bool _atBeginning = true;

        private bool _disposed;

        // foreign
        // This contains a list of employees built while enumerating the current group.  These are FSP objects in the current domain and need to
        // be translated to find out the domain that holds the actual object.
        private List<DirectoryEntry> _foreignMembersCurrentGroup = new List<DirectoryEntry>();
        // List of objects from the group tha are actual fake group objects.
        private List<DirectoryEntry> _fakePrincipalMembers = new List<DirectoryEntry>();
        // list of SIDs + store that have been translated.  These could be any principal object
        private SidList _foreignMembersToReturn;

        private Principal _currentForeignPrincipal;
        private DirectoryEntry _currentForeignDE;

        private List<GroupPrincipal> _foreignGroups = new List<GroupPrincipal>();

        // members based on a query (used for users who are group members by virtue of their primaryGroupId pointing to the group)
        private readonly DirectorySearcher _primaryGroupMembersSearcher;
        private SearchResultCollection _queryMembersResults;
        private IEnumerator _queryMembersResultEnumerator;

        private DirectorySearcher _currentMembersSearcher;

        private readonly Queue<DirectorySearcher> _memberSearchersQueue = new Queue<DirectorySearcher>();
        private readonly Queue<DirectorySearcher> _memberSearchersQueueOriginal = new Queue<DirectorySearcher>();

        private SearchResultCollection _memberSearchResults;
        private IEnumerator _memberSearchResultsEnumerator;

        private ExpansionMode _expansionMode;
        private readonly ExpansionMode _originalExpansionMode;

        private string BuildPathFromDN(string dn)
        {
            string userSuppliedServername = _storeCtx.UserSuppliedServerName;

            if (null == _pathCracker)
            {
                lock (_pathLock)
                {
                    if (null == _pathCracker)
                    {
                        UnsafeNativeMethods.Pathname pathNameObj = new UnsafeNativeMethods.Pathname();
                        _pathCracker = (UnsafeNativeMethods.IADsPathname)pathNameObj;
                        _pathCracker.EscapedMode = 2 /* ADS_ESCAPEDMODE_ON */;
                    }
                }
            }

            _pathCracker.Set(dn, 4 /* ADS_SETTYPE_DN */);

            string escapedDn = _pathCracker.Retrieve(7 /* ADS_FORMAT_X500_DN */);

            if (userSuppliedServername.Length > 0)
                return "LDAP://" + _storeCtx.UserSuppliedServerName + "/" + escapedDn;
            else
                return "LDAP://" + escapedDn;
        }
    }

    internal enum ExpansionMode
    {
        Enum = 0,
        ASQ = 1,
    }

    internal sealed class ADDNLinkedAttrSetBookmark : ResultSetBookmark
    {
        public Dictionary<string, bool> usersVisited;
        public List<string> groupsToVisit;
        public List<string> groupsVisited;
        public IEnumerable members;
        public IEnumerator membersEnum;
        public Queue<IEnumerable> membersQueue;
        public ADStoreCtx storeCtx;
        public object current;
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
