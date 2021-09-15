// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Security.Principal;

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System.DirectoryServices.AccountManagement
{
    internal sealed class AuthZSet : ResultSet
    {
        internal AuthZSet(
                    byte[] userSid,
                    NetCred credentials,
                    ContextOptions contextOptions,
                    string flatUserAuthority,
                    StoreCtx userStoreCtx,
                    object userCtxBase)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                    "AuthZSet",
                                    "AuthZSet: SID={0}, authority={1}, storeCtx={2}",
                                    Utils.ByteArrayToString(userSid),
                                    flatUserAuthority,
                                    userStoreCtx.GetType());

            _userType = userStoreCtx.OwningContext.ContextType;
            _userStoreCtx = userStoreCtx;
            _credentials = credentials;
            _contextOptions = contextOptions;

            // flatUserAuthority is flat domain name if userType == Domain,
            // flat host name if userType == LocalMachine
            _flatUserAuthority = flatUserAuthority;

            // Preload the PrincipalContext cache with the user's PrincipalContext
            _contexts[flatUserAuthority] = userStoreCtx.OwningContext;

            //
            // Get the SIDs of the groups to which the user belongs
            //

            IntPtr pClientContext = IntPtr.Zero;
            IntPtr pResManager = IntPtr.Zero;
            IntPtr pBuffer = IntPtr.Zero;

            try
            {
                UnsafeNativeMethods.LUID luid = default;
                luid.low = 0;
                luid.high = 0;

                _psMachineSid = new SafeMemoryPtr(Utils.GetMachineDomainSid());
                _psUserSid = new SafeMemoryPtr(Utils.ConvertByteArrayToIntPtr(userSid));

                bool f;
                int lastError = 0;

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "Initializing resource manager");

                // Create a resource manager
                f = UnsafeNativeMethods.AuthzInitializeResourceManager(
                                            UnsafeNativeMethods.AUTHZ_RM_FLAG.AUTHZ_RM_FLAG_NO_AUDIT,
                                            IntPtr.Zero,
                                            IntPtr.Zero,
                                            IntPtr.Zero,
                                            null,
                                            out pResManager
                                            );

                if (f)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "Getting ctx from SID");

                    // Construct a context for the user based on the user's SID
                    f = UnsafeNativeMethods.AuthzInitializeContextFromSid(
                                                0,                  // default flags
                                                _psUserSid.DangerousGetHandle(),
                                                pResManager,
                                                IntPtr.Zero,
                                                luid,
                                                IntPtr.Zero,
                                                out pClientContext
                                                );

                    if (f)
                    {
                        int bufferSize = 0;

                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "Getting info from ctx");

                        // Extract the group SIDs from the user's context.  Determine the size of the buffer we need.
                        f = UnsafeNativeMethods.AuthzGetInformationFromContext(
                                                    pClientContext,
                                                    2,                    // AuthzContextInfoGroupsSids
                                                    0,
                                                    out bufferSize,
                                                    IntPtr.Zero
                                                    );
                        if (!f && (bufferSize > 0) && (Marshal.GetLastWin32Error() == 122) /*ERROR_INSUFFICIENT_BUFFER*/)
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "Getting info from ctx (size={0})", bufferSize);

                            Debug.Assert(bufferSize > 0);

                            // Set up the needed buffer
                            pBuffer = Marshal.AllocHGlobal(bufferSize);

                            // Extract the group SIDs from the user's context, into our buffer.0
                            f = UnsafeNativeMethods.AuthzGetInformationFromContext(
                                                        pClientContext,
                                                        2,                    // AuthzContextInfoGroupsSids
                                                        bufferSize,
                                                        out bufferSize,
                                                        pBuffer
                                                        );

                            if (f)
                            {
                                // Marshall the native buffer into managed SID_AND_ATTR structures.
                                // The native buffer holds a TOKEN_GROUPS structure:
                                //
                                //        struct TOKEN_GROUPS {
                                //                DWORD GroupCount;
                                //                SID_AND_ATTRIBUTES Groups[ANYSIZE_ARRAY];
                                //        };
                                //

                                // Extract TOKEN_GROUPS.GroupCount

                                UnsafeNativeMethods.TOKEN_GROUPS tokenGroups = (UnsafeNativeMethods.TOKEN_GROUPS)Marshal.PtrToStructure(pBuffer, typeof(UnsafeNativeMethods.TOKEN_GROUPS));

                                int groupCount = tokenGroups.groupCount;

                                GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "Found {0} groups", groupCount);

                                // Extract TOKEN_GROUPS.Groups, by iterating over the array and marshalling
                                // each native SID_AND_ATTRIBUTES into a managed SID_AND_ATTR.
                                UnsafeNativeMethods.SID_AND_ATTR[] groups = new UnsafeNativeMethods.SID_AND_ATTR[groupCount];

                                IntPtr currentItem = new IntPtr(pBuffer.ToInt64() + Marshal.SizeOf(typeof(UnsafeNativeMethods.TOKEN_GROUPS)) - IntPtr.Size);

                                for (int i = 0; i < groupCount; i++)
                                {
                                    groups[i] = (UnsafeNativeMethods.SID_AND_ATTR)Marshal.PtrToStructure(currentItem, typeof(UnsafeNativeMethods.SID_AND_ATTR));

                                    currentItem = new IntPtr(currentItem.ToInt64() + Marshal.SizeOf(typeof(UnsafeNativeMethods.SID_AND_ATTR)));
                                }

                                _groupSidList = new SidList(groups);
                            }
                            else
                            {
                                lastError = Marshal.GetLastWin32Error();
                            }
                        }
                        else
                        {
                            lastError = Marshal.GetLastWin32Error();
                            Debug.Fail("With a zero-length buffer, this should have never succeeded");
                        }
                    }
                    else
                    {
                        lastError = Marshal.GetLastWin32Error();
                    }
                }
                else
                {
                    lastError = Marshal.GetLastWin32Error();
                }

                if (!f)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "AuthZSet", "Failed to retrieve group list, {0}", lastError);

                    throw new PrincipalOperationException(
                                    SR.Format(
                                            SR.AuthZFailedToRetrieveGroupList,
                                            lastError));
                }

                // Save off the buffer since it still holds the native SIDs referenced by SidList
                _psBuffer = new SafeMemoryPtr(pBuffer);
                pBuffer = IntPtr.Zero;
            }
            catch (Exception e)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Error, "AuthZSet", "Caught exception {0} with message {1}", e.GetType(), e.Message);

                if (_psBuffer != null && !_psBuffer.IsInvalid)
                    _psBuffer.Close();

                if (_psUserSid != null && !_psUserSid.IsInvalid)
                    _psUserSid.Close();

                if (_psMachineSid != null && !_psMachineSid.IsInvalid)
                    _psMachineSid.Close();

                // We're on a platform that doesn't have the AuthZ library
                if (e is DllNotFoundException)
                    throw new NotSupportedException(SR.AuthZNotSupported, e);

                if (e is EntryPointNotFoundException)
                    throw new NotSupportedException(SR.AuthZNotSupported, e);

                throw;
            }
            finally
            {
                if (pClientContext != IntPtr.Zero)
                    UnsafeNativeMethods.AuthzFreeContext(pClientContext);

                if (pResManager != IntPtr.Zero)
                    UnsafeNativeMethods.AuthzFreeResourceManager(pResManager);

                if (pBuffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(pBuffer);
            }
        }

        internal override object CurrentAsPrincipal
        {
            get
            {
                Debug.Assert(_currentGroup >= 0 && _currentGroup < _groupSidList.Length);

                GlobalDebug.WriteLineIf(
                                GlobalDebug.Info,
                                "AuthZSet",
                                "CurrentAsPrincipal: currentGroup={0}, list length={1}",
                                _currentGroup,
                                _groupSidList.Length);

                // Convert native SID to byte[] SID
                IntPtr pSid = _groupSidList[_currentGroup].pSid;
                byte[] sid = Utils.ConvertNativeSidToByteArray(pSid);

                // sidIssuerName is null only if SID was not resolved
                // return a fake principal back
                if (null == _groupSidList[_currentGroup].sidIssuerName)
                {
                    string name = _groupSidList[_currentGroup].name;
                    // Create a Principal object to represent it
                    GroupPrincipal g = GroupPrincipal.MakeGroup(_userStoreCtx.OwningContext);
                    g.fakePrincipal = true;
                    g.LoadValueIntoProperty(PropertyNames.PrincipalDisplayName, name);
                    g.LoadValueIntoProperty(PropertyNames.PrincipalName, name);
                    SecurityIdentifier sidObj = new SecurityIdentifier(Utils.ConvertSidToSDDL(sid));
                    g.LoadValueIntoProperty(PropertyNames.PrincipalSid, sidObj);
                    g.LoadValueIntoProperty(PropertyNames.GroupIsSecurityGroup, true);
                    return g;
                }

                GroupPrincipal group = null;

                // Classify the SID
                SidType sidType = Utils.ClassifySID(pSid);

                // It's a fake principal.  Construct and respond the corresponding fake Principal object.
                if (sidType == SidType.FakeObject)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "CurrentAsPrincipal: fake principal {0}", Utils.ByteArrayToString(sid));
                    return _userStoreCtx.ConstructFakePrincipalFromSID(sid);
                }

                // Try to figure out who issued the SID
                string sidIssuerName = null;

                if (sidType == SidType.RealObjectFakeDomain)
                {
                    // BUILTIN principal --> issuer is the same authority as the user's SID
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "CurrentAsPrincipal: builtin principal {0}", Utils.ByteArrayToString(sid));

                    sidIssuerName = _flatUserAuthority;
                }
                else
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "CurrentAsPrincipal: real principal {0}", Utils.ByteArrayToString(sid));

                    // Is the SID from the same domain as the user?
                    bool sameDomain = false;

                    bool success = UnsafeNativeMethods.EqualDomainSid(_psUserSid.DangerousGetHandle(), pSid, ref sameDomain);

                    // if failed, psUserSid must not be a domain sid
                    if (!success)
                    {
#if !SUPPORT_WK_USER_OBJS
                        Debug.Fail("AuthZSet.CurrentAsPrincipal: hit a user with a non-domain SID");
#endif // SUPPORT_WK_USER_OBJS

                        sameDomain = false;
                    }

                    // same domain --> issuer is the same authority as the user's SID
                    if (sameDomain)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "CurrentAsPrincipal: same domain as user ({0})", _flatUserAuthority);
                        sidIssuerName = _flatUserAuthority;
                    }
                }

                // The SID comes from another domain.  Use the domain name that the OS resolved the SID to.
                if (sidIssuerName == null)
                {
                    sidIssuerName = _groupSidList[_currentGroup].sidIssuerName;
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "CurrentAsPrincipal: different domain ({0}) than user ({1})", sidIssuerName, _flatUserAuthority);
                }

                Debug.Assert(sidIssuerName != null);
                Debug.Assert(sidIssuerName.Length > 0);

                // Determine whether it's a local (WinNT) or Active Directory domain (LDAP) group
                bool isLocalGroup = false;
                if (_userType == ContextType.Machine)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "CurrentAsPrincipal: local group (user is SAM)");

                    // Machine local user ---> must be a local group
                    isLocalGroup = true;
                }
                else
                {
                    Debug.Assert(_userType == ContextType.Domain);

                    // Domain user, but the group SID is from the machine domain --> must be a local group
                    // EqualDomainSid will return false if pSid is a BUILTIN SID, but that's okay, we treat those as domain (not local)
                    // groups for domain users.
                    bool inMachineDomain = false;
                    if (UnsafeNativeMethods.EqualDomainSid(_psMachineSid.DangerousGetHandle(), pSid, ref inMachineDomain))
                        if (inMachineDomain)
                        {
                            // At this point we know that the group was issued by the local machine.  Now determine if this machine is
                            // a DC.  Cache the results of the read.

                            if (!_localMachineIsDC.HasValue)
                            {
                                _localMachineIsDC = (bool?)Utils.IsMachineDC(null);
                                GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "CurrentAsPrincipal: IsLocalMachine a DC, localMachineIsDC={0}", _localMachineIsDC.Value);
                            }

                            isLocalGroup = !_localMachineIsDC.Value;
                        }

                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "CurrentAsPrincipal: user is non-SAM, isLocalGroup={0}", isLocalGroup);
                }

                if (isLocalGroup)
                {
                    // It's a local group, because either (1) it's a local machine user, and local users can't be a member of a domain group,
                    // or (2) it's a domain user that's a member of a group on the local machine.  Pass the default machine context options
                    // If we initially targetted AD then those options will not be valid for the machine store.

#if USE_CTX_CACHE
                    PrincipalContext ctx = SDSCache.LocalMachine.GetContext(
                                                                    sidIssuerName,
                                                                    _credentials,
                                                                    DefaultContextOptions.MachineDefaultContextOption);
#else
                    PrincipalContext ctx = (PrincipalContext) this.contexts[sidIssuerName];
                    if (ctx == null)
                    {
                        // Build a PrincipalContext for the machine
                        ctx = new PrincipalContext(
                                        ContextType.Machine,
                                        sidIssuerName,
                                        null,
                                        (this.credentials != null ? credentials.UserName : null),
                                        (this.credentials != null ? credentials.Password : null),
                                        DefaultContextOptions.MachineDefaultContextOption);

                        this.contexts[sidIssuerName] = ctx;
                    }
#endif
                    SecurityIdentifier sidObj = new SecurityIdentifier(sid, 0);
                    group = GroupPrincipal.FindByIdentity(ctx, IdentityType.Sid, sidObj.ToString());
                }
                else
                {
                    Debug.Assert((_userType == ContextType.Domain) &&
                                 !string.Equals(Utils.GetComputerFlatName(), sidIssuerName, StringComparison.OrdinalIgnoreCase));

                    // It's a domain group, because it's a domain user and the SID issuer isn't the local machine

#if USE_CTX_CACHE
                    PrincipalContext ctx = SDSCache.Domain.GetContext(
                                                                sidIssuerName,
                                                                _credentials,
                                                                _contextOptions);
#else
                    PrincipalContext ctx = (PrincipalContext) this.contexts[sidIssuerName];
                    if (ctx == null)
                    {
                        // Determine the domain DNS name

                        // DS_RETURN_DNS_NAME | DS_DIRECTORY_SERVICE_REQUIRED | DS_BACKGROUND_ONLY
                        int flags = unchecked((int) (0x40000000 | 0x00000010 | 0x00000100));
                        UnsafeNativeMethods.DomainControllerInfo info = Utils.GetDcName(null, sidIssuerName, null, flags);

                        // Build a PrincipalContext for the domain
                        ctx = new PrincipalContext(
                                        ContextType.Domain,
                                        info.DomainName,
                                        null,
                                        (this.credentials != null ? credentials.UserName : null),
                                        (this.credentials != null ? credentials.Password : null),
                                        this.contextOptions);

                        this.contexts[sidIssuerName] = ctx;
                    }
#endif
                    // Retrieve the group.  We'd normally just do a Group.FindByIdentity here, but
                    // because the AZMan API can return "old" SIDs, we also need to check the SID
                    // history.  So we'll use the special FindPrincipalBySID method that the ADStoreCtx
                    // exposes for that purpose.
                    Debug.Assert(ctx.QueryCtx is ADStoreCtx);

                    IdentityReference ir = new IdentityReference();
                    // convert byte sid to SDDL string.
                    SecurityIdentifier sidObj = new SecurityIdentifier(sid, 0);
                    ir.UrnScheme = UrnScheme.SidScheme;
                    ir.UrnValue = sidObj.ToString();

                    group = (GroupPrincipal)((ADStoreCtx)ctx.QueryCtx).FindPrincipalBySID(typeof(GroupPrincipal), ir, true);
                }

                if (group == null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "AuthZSet", "CurrentAsPrincipal: Couldn't find group {0}");
                    throw new NoMatchingPrincipalException(SR.AuthZCantFindGroup);
                }

                return group;
            }
        }

        internal override bool MoveNext()
        {
            bool needToRetry;

            do
            {
                needToRetry = false;

                _currentGroup++;

                if (_currentGroup >= _groupSidList.Length)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "MoveNext: ran off end of list ({0})", _groupSidList.Length);
                    return false;
                }

                // Test for the NONE group for a local user.  We recognize it by:
                //      * we're enumerating the authz groups for a local machine user
                //      * it's a domain sid (SidType.RealObject) for the same domain as the user
                //      * it has the RID of the "Domain Users" group, 513
                if (_userType == ContextType.Machine)
                {
                    IntPtr pSid = _groupSidList[_currentGroup].pSid;

                    bool sameDomain = false;
                    if (Utils.ClassifySID(pSid) == SidType.RealObject && UnsafeNativeMethods.EqualDomainSid(_psUserSid.DangerousGetHandle(), pSid, ref sameDomain))
                    {
                        if (sameDomain)
                        {
                            int lastRid = Utils.GetLastRidFromSid(pSid);

                            if (lastRid == 513)     // DOMAIN_GROUP_RID_USERS
                            {
                                // This is the NONE group for a local user.  This isn't a real group, and
                                // has no impact on authorization (per ColinBr).  Skip it.
                                needToRetry = true;

                                GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "MoveNext: found NONE group, skipping");
                            }
                        }
                    }
                }
            }
            while (needToRetry);

            return true;
        }

        internal override void Reset()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "Reset");

            _currentGroup = -1;
        }

        // IDisposable implementation
        public override void Dispose()
        {
            try
            {
                if (!_disposed)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "Dispose: disposing");

                    _psBuffer.Close();
                    _psUserSid.Close();
                    _psMachineSid.Close();

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

        // The user whose groups we're retrieving
        private readonly SafeMemoryPtr _psUserSid;

        // The SID of the machine domain of the machine we're running on
        private readonly SafeMemoryPtr _psMachineSid;

        // The user's StoreCtx
        private readonly StoreCtx _userStoreCtx;

        // The user's credentials
        private readonly NetCred _credentials;

        // The user's options
        private readonly ContextOptions _contextOptions;

        // The type (domain, local, etc.) of the user
        private readonly ContextType _userType;

        // The authority's name (hostname or domainname)
        private readonly string _flatUserAuthority;

        // The index (into this.groupSidList) of the group we're currently enumerating
        private int _currentGroup = -1;

        // The groups we're enumerating over
        private readonly SidList _groupSidList;

        // The native TOKEN_GROUPS returned by AuthzGetInformationFromContext
        private readonly SafeMemoryPtr _psBuffer;

        // Have we been disposed?
        private bool _disposed;

        // Maps sidIssuerName --> PrincipalContext
        private readonly Hashtable _contexts = new Hashtable();

        // Contains cached results if the local machine is  a DC.
        private bool? _localMachineIsDC;

        //
        // Guarantees finalization of the native resources
        //
        private sealed class SafeMemoryPtr : SafeHandle
        {
            public SafeMemoryPtr() : base(IntPtr.Zero, true)
            {
            }

            internal SafeMemoryPtr(IntPtr handle) : base(IntPtr.Zero, true)
            {
                SetHandle(handle);
            }

            // for the critial finalizer object
            public override bool IsInvalid
            {
                get { return (handle == IntPtr.Zero); }
            }

            protected override bool ReleaseHandle()
            {
                if (handle != IntPtr.Zero)
                    Marshal.FreeHGlobal(handle);

                return true;
            }
        }

        /*
                //
                // Holds the list of group SIDs.  Also translates them in bulk into domain name and group name.
                //
                class SidList
                {
                    public SidList(UnsafeNativeMethods.SID_AND_ATTR[] groupSidAndAttrs)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "SidList: processing {0} SIDs", groupSidAndAttrs.Length);

                        // Build the list of SIDs to resolve
                        int groupCount = groupSidAndAttrs.Length;
                        IntPtr[] pGroupSids = new IntPtr[groupCount];

                        for (int i=0; i < groupCount; i++)
                        {
                            pGroupSids[i] = groupSidAndAttrs[i].pSid;

                        }

                        // Translate the SIDs in bulk
                        IntPtr pOA = IntPtr.Zero;
                        IntPtr pPolicyHandle = IntPtr.Zero;

                        IntPtr pDomains = IntPtr.Zero;
                        UnsafeNativeMethods.LSA_TRUST_INFORMATION[] domains;

                        IntPtr pNames = IntPtr.Zero;
                        UnsafeNativeMethods.LSA_TRANSLATED_NAME[] names;

                        try
                        {
                            //
                            // Get the policy handle
                            //
                            UnsafeNativeMethods.LSA_OBJECT_ATTRIBUTES oa = new UnsafeNativeMethods.LSA_OBJECT_ATTRIBUTES();

                            pOA = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(UnsafeNativeMethods.LSA_OBJECT_ATTRIBUTES)));
                            Marshal.StructureToPtr(oa, pOA, false);
                            int err = UnsafeNativeMethods.LsaOpenPolicy(
                                            IntPtr.Zero,
                                            pOA,
                                            0x800,        // POLICY_LOOKUP_NAMES
                                            ref pPolicyHandle);

                            if (err != 0)
                            {
                                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "AuthZSet", "SidList: couldn't get policy handle, err={0}", err);

                                throw new PrincipalOperationException(SR.Format(
                                                                           SR.AuthZErrorEnumeratingGroups,
                                                                           SafeNativeMethods.LsaNtStatusToWinError(err)));
                            }

                            Debug.Assert(pPolicyHandle != IntPtr.Zero);

                            //
                            // Translate the SIDs
                            //

                            err = UnsafeNativeMethods.LsaLookupSids(
                                                pPolicyHandle,
                                                groupCount,
                                                pGroupSids,
                                                out pDomains,
                                                out pNames);

                            if (err != 0)
                            {
                                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "AuthZSet", "SidList: LsaLookupSids failed, err={0}", err);

                                throw new PrincipalOperationException(SR.Format(
                                                                           SR.AuthZErrorEnumeratingGroups,
                                                                           SafeNativeMethods.LsaNtStatusToWinError(err)));
                            }

                            //
                            // Get the group names in managed form
                            //
                            names = new UnsafeNativeMethods.LSA_TRANSLATED_NAME[groupCount];
                            IntPtr pCurrentName = pNames;

                            for (int i=0; i < groupCount; i++)
                            {
                                names[i] = (UnsafeNativeMethods.LSA_TRANSLATED_NAME)
                                                Marshal.PtrToStructure(pCurrentName, typeof(UnsafeNativeMethods.LSA_TRANSLATED_NAME));

                                pCurrentName = new IntPtr(pCurrentName.ToInt64() + Marshal.SizeOf(typeof(UnsafeNativeMethods.LSA_TRANSLATED_NAME)));
                            }

                            //
                            // Get the domain names in managed form
                            //

                            // Extract LSA_REFERENCED_DOMAIN_LIST.Entries
                            int domainCount = Marshal.ReadInt32(pDomains);

                            // Extract LSA_REFERENCED_DOMAIN_LIST.Domains, by iterating over the array and marshalling
                            // each native LSA_TRUST_INFORMATION into a managed LSA_TRUST_INFORMATION.
                            domains = new UnsafeNativeMethods.LSA_TRUST_INFORMATION[domainCount];

                            IntPtr pCurrentDomain = Marshal.ReadIntPtr(pDomains, Marshal.SizeOf(typeof(Int32)));

                            for (int i=0; i < domainCount; i++)
                            {
                                domains[i] =(UnsafeNativeMethods.LSA_TRUST_INFORMATION) Marshal.PtrToStructure(pCurrentDomain, typeof(UnsafeNativeMethods.LSA_TRUST_INFORMATION));
                                pCurrentDomain = new IntPtr(pCurrentDomain.ToInt64() + Marshal.SizeOf(typeof(UnsafeNativeMethods.LSA_TRUST_INFORMATION)));
                            }

                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthZSet", "SidList: got {0} groups in {1} domains", groupCount, domainCount);

                            //
                            // Build the list of entries
                            //
                            Debug.Assert(names.Length == groupCount);

                            for (int i = 0; i < names.Length; i++)
                            {
                                UnsafeNativeMethods.LSA_TRANSLATED_NAME name = names[i];

                                // Get the domain associated with this name
                                Debug.Assert(name.domainIndex >= 0);
                                Debug.Assert(name.domainIndex < domains.Length);
                                UnsafeNativeMethods.LSA_TRUST_INFORMATION domain = domains[name.domainIndex];

                                // Build an entry.  Note that LSA_UNICODE_STRING.length is in bytes,
                                // while PtrToStringUni expects a length in characters.
                                SidListEntry entry = new SidListEntry();

                                Debug.Assert(name.name.length % 2 == 0);
                                entry.name = Marshal.PtrToStringUni(name.name.buffer, name.name.length/2);

                                Debug.Assert(domain.name.length % 2 == 0);
                                entry.sidIssuerName = Marshal.PtrToStringUni(domain.name.buffer, domain.name.length/2);

                                entry.pSid = groupSidAndAttrs[i].pSid;

                                this.entries.Add(entry);
                            }

                        }
                        finally
                        {
                            if (pDomains != IntPtr.Zero)
                                UnsafeNativeMethods.LsaFreeMemory(pDomains);

                            if (pNames != IntPtr.Zero)
                                UnsafeNativeMethods.LsaFreeMemory(pNames);

                            if (pPolicyHandle != IntPtr.Zero)
                                UnsafeNativeMethods.LsaClose(pPolicyHandle);

                            if (pOA != IntPtr.Zero)
                                Marshal.FreeHGlobal(pOA);
                        }
                    }

                    List<SidListEntry> entries = new List<SidListEntry>();

                    public SidListEntry this[int index]
                    {
                        get { return this.entries[index]; }
                    }

                    public int Length
                    {
                        get { return this.entries.Count; }
                    }
                }

                class SidListEntry
                {
                    public IntPtr pSid;

                    public string name;
                    public string sidIssuerName;
                }
        */
    }
}
