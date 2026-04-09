// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.DirectoryServices.AccountManagement
{
    internal sealed class SAMMembersSet : BookmarkableResultSet
    {
        internal SAMMembersSet(string groupPath, UnsafeNativeMethods.IADsGroup group, bool recursive, SAMStoreCtx storeCtx, DirectoryEntry ctxBase)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                    "SAMMembersSet",
                                    "SAMMembersSet: groupPath={0}, recursive={1}, base={2}",
                                    groupPath,
                                    recursive,
                                    ctxBase.Path);

            _storeCtx = storeCtx;

            _group = group;
            _originalGroup = group;
            _recursive = recursive;

            _groupsVisited.Add(groupPath);    // so we don't revisit it

            UnsafeNativeMethods.IADsMembers iADsMembers = group.Members();
            _membersEnumerator = ((IEnumerable)iADsMembers).GetEnumerator();
        }

        // Return the principal we're positioned at as a Principal object.
        // Need to use our StoreCtx's GetAsPrincipal to convert the native object to a Principal
        internal override object CurrentAsPrincipal
        {
            get
            {
                if (_current != null)
                {
                    // Local principal --- handle it ourself
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "CurrentAsPrincipal: returning current");
                    return SAMUtils.DirectoryEntryAsPrincipal(_current, _storeCtx);
                }
                else if (_currentFakePrincipal != null)
                {
                    // Local fake principal --- handle it ourself
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "CurrentAsPrincipal: returning currentFakePrincipal");
                    return _currentFakePrincipal;
                }
                else if (_currentForeign != null)
                {
                    // Foreign, non-recursive principal.  Just return the principal.
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "CurrentAsPrincipal: returning currentForeign");
                    return _currentForeign;
                }
                else
                {
                    // Foreign recursive expansion.  Proxy the call to the foreign ResultSet.
                    Debug.Assert(_foreignResultSet != null);
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "CurrentAsPrincipal: returning foreignResultSet");
                    return _foreignResultSet.CurrentAsPrincipal;
                }
            }
        }

        // Advance the enumerator to the next principal in the result set, pulling in additional pages
        // of results (or ranges of attribute values) as needed.
        // Returns true if successful, false if no more results to return.
        internal override bool MoveNext()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "Entering MoveNext");

            _atBeginning = false;

            bool f = MoveNextLocal();

            if (!f)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNext: trying foreign");

                f = MoveNextForeign();
            }

            return f;
        }

        private bool MoveNextLocal()
        {
            bool needToRetry;

            do
            {
                needToRetry = false;

                bool f = _membersEnumerator.MoveNext();

                if (f) // got a value
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextLocal: got a value from the enumerator");

                    UnsafeNativeMethods.IADs nativeMember = (UnsafeNativeMethods.IADs)_membersEnumerator.Current;

                    // If we encountered a group member corresponding to a fake principal such as
                    // NT AUTHORITY/NETWORK SERVICE, construct and prepare to return the fake principal.
                    byte[] sid = (byte[])nativeMember.Get("objectSid");
                    SidType sidType = Utils.ClassifySID(sid);
                    if (sidType == SidType.FakeObject)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextLocal: fake principal, sid={0}", Utils.ByteArrayToString(sid));

                        _currentFakePrincipal = _storeCtx.ConstructFakePrincipalFromSID(sid);
                        _current = null;
                        _currentForeign = null;

                        _foreignResultSet?.Dispose();
                        _foreignResultSet = null;
                        return true;
                    }

                    // We do this, rather than using the DirectoryEntry constructor that takes a native IADs object,
                    // is so the credentials get transferred to the new DirectoryEntry.  If we just use the native
                    // object constructor, the native object will have the right credentials, but the DirectoryEntry
                    // will have default (null) credentials, which it'll use anytime it needs to use credentials.
                    DirectoryEntry de = SDSUtils.BuildDirectoryEntry(
                                                        _storeCtx.Credentials,
                                                        _storeCtx.AuthTypes);

                    if (sidType == SidType.RealObjectFakeDomain)
                    {
                        // Transform the "WinNT://BUILTIN/foo" path to "WinNT://machineName/foo"
                        string builtinADsPath = nativeMember.ADsPath;

                        UnsafeNativeMethods.Pathname pathCracker = new UnsafeNativeMethods.Pathname();
                        UnsafeNativeMethods.IADsPathname pathName = (UnsafeNativeMethods.IADsPathname)pathCracker;

                        pathName.Set(builtinADsPath, 1 /* ADS_SETTYPE_FULL */);

                        // Build the "WinNT://" portion of the new path
                        StringBuilder adsPath = new StringBuilder();
                        adsPath.Append("WinNT://");
                        //adsPath.Append(pathName.Retrieve(9 /*ADS_FORMAT_SERVER */));

                        // Build the "WinNT://machineName/" portion of the new path
                        adsPath.Append(_storeCtx.MachineUserSuppliedName);
                        adsPath.Append('/');

                        // Build the "WinNT://machineName/foo" portion of the new path
                        int cElements = pathName.GetNumElements();

                        Debug.Assert(cElements >= 2);       // "WinNT://BUILTIN/foo" == 2 elements

                        // Note that the ADSI WinNT provider indexes them backwards, e.g., in
                        // "WinNT://BUILTIN/A/B", BUILTIN == 2, A == 1, B == 0.
                        for (int i = cElements - 2; i >= 0; i--)
                        {
                            adsPath.Append(pathName.GetElement(i));
                            adsPath.Append('/');
                        }

                        adsPath.Remove(adsPath.Length - 1, 1);  // remove the trailing "/"

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
                        if (!_recursive || !SAMUtils.IsOfObjectClass(de, "Group"))
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextLocal: setting current to {0}", de.Path);

                            // Not recursive, or not a group.  Return the principal.
                            _current = de;
                            _currentFakePrincipal = null;
                            _currentForeign = null;

                            _foreignResultSet?.Dispose();
                            _foreignResultSet = null;
                            return true;
                        }
                        else
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextLocal: adding {0} to groupsToVisit", de.Path);

                            // Save off for later, if we haven't done so already.
                            if (!_groupsVisited.Contains(de.Path) && !_groupsToVisit.Contains(de.Path))
                                _groupsToVisit.Add(de.Path);

                            needToRetry = true;
                            continue;
                        }
                    }
                    else
                    {
                        // It's a foreign principal (e..g, an AD user or group).
                        // Save it off for later.

                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextLocal: adding {0} to foreignMembers", de.Path);

                        _foreignMembers.Add(de);
                        needToRetry = true;
                        continue;
                    }
                }
                else
                {
                    // We reached the end of this group's membership.
                    // If we're supposed to be recursively expanding, we need to expand
                    // any remaining non-foreign groups we earlier visited.
                    if (_recursive)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextLocal: recursive processing, groupsToVisit={0}", _groupsToVisit.Count);

                        if (_groupsToVisit.Count > 0)
                        {
                            // Pull off the next group to visit
                            string groupPath = _groupsToVisit[0];
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextLocal: recursively processing {0}", groupPath);

                            _groupsToVisit.RemoveAt(0);
                            _groupsVisited.Add(groupPath);

                            // Set up for the next round of enumeration
                            DirectoryEntry de = SDSUtils.BuildDirectoryEntry(
                                                                        groupPath,
                                                                        _storeCtx.Credentials,
                                                                        _storeCtx.AuthTypes);

                            _group = (UnsafeNativeMethods.IADsGroup)de.NativeObject;

                            UnsafeNativeMethods.IADsMembers iADsMembers = _group.Members();
                            _membersEnumerator = ((IEnumerable)iADsMembers).GetEnumerator();

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

        private bool MoveNextForeign()
        {
            bool needToRetry;

            do
            {
                needToRetry = false;

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextForeign: foreignMembers count={0}", _foreignMembers.Count);

                if (_foreignMembers.Count > 0)
                {
                    // foreignDE is a DirectoryEntry in _this_ store representing a principal in another store
                    DirectoryEntry foreignDE = _foreignMembers[0];
                    _foreignMembers.RemoveAt(0);

                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextForeign: foreignDE={0}", foreignDE.Path);

                    // foreignPrincipal is a principal from _another_ store (e.g., it's backed by an ADStoreCtx)
                    Principal foreignPrincipal = _storeCtx.ResolveCrossStoreRefToPrincipal(foreignDE);

                    // If we're not enumerating recursively, return the principal.
                    // If we are enumerating recursively, and it's a group, save it off for later.
                    if (!_recursive || !(foreignPrincipal is GroupPrincipal))
                    {
                        // Return the principal.
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextForeign: setting currentForeign to {0}", foreignDE.Path);

                        _current = null;
                        _currentFakePrincipal = null;
                        _currentForeign = foreignPrincipal;

                        _foreignResultSet?.Dispose();
                        _foreignResultSet = null;
                        return true;
                    }
                    else
                    {
                        // Save off the group for recursive expansion, and go on to the next principal.
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextForeign: adding {0} to foreignGroups", foreignDE.Path);

                        _foreignGroups.Add((GroupPrincipal)foreignPrincipal);
                        needToRetry = true;
                        continue;
                    }
                }

                if (_foreignResultSet == null && _foreignGroups.Count > 0)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                            "SAMMembersSet",
                                            "MoveNextForeign: getting foreignResultSet (foreignGroups count={0})",
                                            _foreignGroups.Count);

                    // We're expanding recursively, and either (1) we're immediately before
                    // the recursive expansion of the first foreign group, or (2) we just completed
                    // the recursive expansion of a foreign group, and now are moving on to the next.
                    Debug.Assert(_recursive);

                    // Pull off a foreign group to expand.
                    GroupPrincipal foreignGroup = _foreignGroups[0];
                    _foreignGroups.RemoveAt(0);

                    // Since it's a foreign group, we don't know how to enumerate its members.  So we'll
                    // ask the group, through its StoreCtx, to do it for us.  Effectively, we'll end up acting
                    // as a proxy to the foreign group's ResultSet.
                    _foreignResultSet = foreignGroup.GetStoreCtxToUse().GetGroupMembership(foreignGroup, true);
                }

                // We're either just beginning the recursive expansion of a foreign group, or we're continuing the expansion
                // that we started on a previous call to MoveNext().
                if (_foreignResultSet != null)
                {
                    Debug.Assert(_recursive);

                    bool f = _foreignResultSet.MoveNext();

                    if (f)
                    {
                        // By setting current, currentFakePrincipal, and currentForeign to null,
                        // CurrentAsPrincipal/CurrentAsIdentityReference will know to proxy out to foreignResultSet.
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextForeign: using foreignResultSet");

                        _current = null;
                        _currentFakePrincipal = null;
                        _currentForeign = null;
                        return true;
                    }

                    // Ran out of members in the foreign group, is there another foreign group remaining that we need
                    // to expand?
                    if (_foreignGroups.Count > 0)
                    {
                        // Yes, there is.  Null out the foreignResultSet so we'll pull out the next foreign group
                        // the next time around the loop.
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextForeign: ran out of members, using next foreignResultSet");

                        _foreignResultSet.Dispose();
                        _foreignResultSet = null;
                        Debug.Assert(_foreignMembers.Count == 0);
                        needToRetry = true;
                    }
                    else
                    {
                        // No, there isn't.  Nothing left to do.  We set foreignResultSet to null here just
                        // to leave things in a clean state --- it shouldn't really be necessary.
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "MoveNextForeign: ran out of members, nothing more to do");

                        _foreignResultSet.Dispose();
                        _foreignResultSet = null;
                    }
                }
            }
            while (needToRetry);

            return false;
        }

        private bool IsLocalMember(byte[] sid)
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
            string domainName;

            int err = Utils.LookupSid(
                                _storeCtx.MachineUserSuppliedName,
                                _storeCtx.Credentials,
                                sid,
                                out _,
                                out domainName,
                                out _);

            if (err != 0)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Error,
                                        "SAMMembersSet",
                                        "IsLocalMember: LookupSid failed, sid={0}, server={1}, err={2}",
                                        Utils.ByteArrayToString(sid),
                                        _storeCtx.MachineUserSuppliedName,
                                        err);

                throw new PrincipalOperationException(
                            SR.Format(SR.SAMStoreCtxErrorEnumeratingGroup, err));
            }

            if (string.Equals(_storeCtx.MachineFlatName, domainName, StringComparison.OrdinalIgnoreCase))
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
        internal override void Reset()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "Reset");

            if (!_atBeginning)
            {
                _groupsToVisit.Clear();
                string originalGroupPath = _groupsVisited[0];
                _groupsVisited.Clear();
                _groupsVisited.Add(originalGroupPath);

                _group = _originalGroup;
                UnsafeNativeMethods.IADsMembers iADsMembers = _group.Members();
                _membersEnumerator = ((IEnumerable)iADsMembers).GetEnumerator();

                _current = null;
                _currentFakePrincipal = null;
                _currentForeign = null;

                _foreignMembers.Clear();
                _foreignGroups.Clear();

                if (_foreignResultSet != null)
                {
                    _foreignResultSet.Dispose();
                    _foreignResultSet = null;
                }

                _atBeginning = true;
            }
        }

        internal override ResultSetBookmark BookmarkAndReset()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "Bookmarking");

            SAMMembersSetBookmark bookmark = new SAMMembersSetBookmark();

            bookmark.groupsToVisit = _groupsToVisit;
            _groupsToVisit = new List<string>();

            string originalGroupPath = _groupsVisited[0];
            bookmark.groupsVisited = _groupsVisited;
            _groupsVisited = new List<string>();
            _groupsVisited.Add(originalGroupPath);

            bookmark.group = _group;
            bookmark.membersEnumerator = _membersEnumerator;
            _group = _originalGroup;
            UnsafeNativeMethods.IADsMembers iADsMembers = _group.Members();
            _membersEnumerator = ((IEnumerable)iADsMembers).GetEnumerator();

            bookmark.current = _current;
            bookmark.currentFakePrincipal = _currentFakePrincipal;
            bookmark.currentForeign = _currentForeign;
            _current = null;
            _currentFakePrincipal = null;
            _currentForeign = null;

            bookmark.foreignMembers = _foreignMembers;
            bookmark.foreignGroups = _foreignGroups;
            bookmark.foreignResultSet = _foreignResultSet;
            _foreignMembers = new List<DirectoryEntry>();
            _foreignGroups = new List<GroupPrincipal>();
            _foreignResultSet = null;

            bookmark.atBeginning = _atBeginning;
            _atBeginning = true;

            return bookmark;
        }

        internal override void RestoreBookmark(ResultSetBookmark bookmark)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "Restoring from bookmark");

            Debug.Assert(bookmark is SAMMembersSetBookmark);
            SAMMembersSetBookmark samBookmark = (SAMMembersSetBookmark)bookmark;

            _groupsToVisit = samBookmark.groupsToVisit;
            _groupsVisited = samBookmark.groupsVisited;
            _group = samBookmark.group;
            _membersEnumerator = samBookmark.membersEnumerator;
            _current = samBookmark.current;
            _currentFakePrincipal = samBookmark.currentFakePrincipal;
            _currentForeign = samBookmark.currentForeign;
            _foreignMembers = samBookmark.foreignMembers;
            _foreignGroups = samBookmark.foreignGroups;

            _foreignResultSet?.Dispose();

            _foreignResultSet = samBookmark.foreignResultSet;
            _atBeginning = samBookmark.atBeginning;
        }

        public override void Dispose()
        {
            try
            {
                if (!_disposed)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "Dispose: disposing");

                    if (_foreignResultSet != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMMembersSet", "Dispose: disposing foreignResultSet");
                        _foreignResultSet.Dispose();
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
        // Private fields
        //

        private readonly bool _recursive;

        private bool _disposed;

        private readonly SAMStoreCtx _storeCtx;

        private bool _atBeginning = true;

        // local

        // The 0th entry in this list is always the ADsPath of the original group whose membership we're querying
        private List<string> _groupsVisited = new List<string>();

        private List<string> _groupsToVisit = new List<string>();

        private DirectoryEntry _current; // current member of the group (if enumerating local group and found a real principal)
        private Principal _currentFakePrincipal;  // current member of the group (if enumerating local group and found a fake pricipal)

        private UnsafeNativeMethods.IADsGroup _group;            // the group whose membership we're currently enumerating over
        private readonly UnsafeNativeMethods.IADsGroup _originalGroup;    // the group whose membership we started off with (before recursing)

        private IEnumerator _membersEnumerator;         // the current group's membership enumerator

        // foreign
        private List<DirectoryEntry> _foreignMembers = new List<DirectoryEntry>();
        private Principal _currentForeign; // current member of the group (if enumerating foreign principal)

        private List<GroupPrincipal> _foreignGroups = new List<GroupPrincipal>();
        private ResultSet _foreignResultSet; // current foreign group's ResultSet (if enumerating via proxy to foreign group)
    }

    internal sealed class SAMMembersSetBookmark : ResultSetBookmark
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
