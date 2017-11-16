// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Net;
using System.Security.Principal;

using System.DirectoryServices;

namespace System.DirectoryServices.AccountManagement
{
    internal partial class SAMStoreCtx : StoreCtx
    {
        private DirectoryEntry _ctxBase;
        private object _ctxBaseLock = new object(); // when mutating ctxBase

        private bool _ownCtxBase;    // if true, we "own" ctxBase and must Dispose of it when we're done

        private bool _disposed = false;

        internal NetCred Credentials { get { return _credentials; } }
        private NetCred _credentials = null;

        internal AuthenticationTypes AuthTypes { get { return _authTypes; } }
        private AuthenticationTypes _authTypes;

        private ContextOptions _contextOptions;

        static SAMStoreCtx()
        {
            //
            // Load the *PropertyMappingTableByProperty and *PropertyMappingTableByWinNT tables
            //
            s_userPropertyMappingTableByProperty = new Hashtable();
            s_userPropertyMappingTableByWinNT = new Hashtable();

            s_groupPropertyMappingTableByProperty = new Hashtable();
            s_groupPropertyMappingTableByWinNT = new Hashtable();

            s_computerPropertyMappingTableByProperty = new Hashtable();
            s_computerPropertyMappingTableByWinNT = new Hashtable();

            s_validPropertyMap = new Dictionary<string, ObjectMask>();

            s_maskMap = new Dictionary<Type, ObjectMask>();
            s_maskMap.Add(typeof(UserPrincipal), ObjectMask.User);
            s_maskMap.Add(typeof(ComputerPrincipal), ObjectMask.Computer);
            s_maskMap.Add(typeof(GroupPrincipal), ObjectMask.Group);
            s_maskMap.Add(typeof(Principal), ObjectMask.Principal);

            for (int i = 0; i < s_propertyMappingTableRaw.GetLength(0); i++)
            {
                string propertyName = s_propertyMappingTableRaw[i, 0] as string;
                Type principalType = s_propertyMappingTableRaw[i, 1] as Type;
                string winNTAttribute = s_propertyMappingTableRaw[i, 2] as string;
                FromWinNTConverterDelegate fromWinNT = s_propertyMappingTableRaw[i, 3] as FromWinNTConverterDelegate;
                ToWinNTConverterDelegate toWinNT = s_propertyMappingTableRaw[i, 4] as ToWinNTConverterDelegate;

                Debug.Assert(propertyName != null);
                Debug.Assert((winNTAttribute != null && fromWinNT != null) || (fromWinNT == null));
                Debug.Assert(principalType == typeof(Principal) || principalType.IsSubclassOf(typeof(Principal)));

                // Build the table entry.  The same entry will be used in both tables.
                // Once constructed, the table entries are treated as read-only, so there's
                // no danger in sharing the entries between tables.
                PropertyMappingTableEntry propertyEntry = new PropertyMappingTableEntry();
                propertyEntry.propertyName = propertyName;
                propertyEntry.suggestedWinNTPropertyName = winNTAttribute;
                propertyEntry.winNTToPapiConverter = fromWinNT;
                propertyEntry.papiToWinNTConverter = toWinNT;

                // Add it to the appropriate tables
                List<Hashtable> byPropertyTables = new List<Hashtable>();
                List<Hashtable> byWinNTTables = new List<Hashtable>();

                ObjectMask BitMask = 0;

                if (principalType == typeof(UserPrincipal))
                {
                    byPropertyTables.Add(s_userPropertyMappingTableByProperty);
                    byWinNTTables.Add(s_userPropertyMappingTableByWinNT);
                    BitMask = ObjectMask.User;
                }
                else if (principalType == typeof(ComputerPrincipal))
                {
                    byPropertyTables.Add(s_computerPropertyMappingTableByProperty);
                    byWinNTTables.Add(s_computerPropertyMappingTableByWinNT);
                    BitMask = ObjectMask.Computer;
                }
                else if (principalType == typeof(GroupPrincipal))
                {
                    byPropertyTables.Add(s_groupPropertyMappingTableByProperty);
                    byWinNTTables.Add(s_groupPropertyMappingTableByWinNT);
                    BitMask = ObjectMask.Group;
                }
                else
                {
                    Debug.Assert(principalType == typeof(Principal));

                    byPropertyTables.Add(s_userPropertyMappingTableByProperty);
                    byPropertyTables.Add(s_computerPropertyMappingTableByProperty);
                    byPropertyTables.Add(s_groupPropertyMappingTableByProperty);

                    byWinNTTables.Add(s_userPropertyMappingTableByWinNT);
                    byWinNTTables.Add(s_computerPropertyMappingTableByWinNT);
                    byWinNTTables.Add(s_groupPropertyMappingTableByWinNT);
                    BitMask = ObjectMask.Principal;
                }

                if ((winNTAttribute == null) || (winNTAttribute == "*******"))
                {
                    BitMask = ObjectMask.None;
                }

                ObjectMask currentMask;
                if (s_validPropertyMap.TryGetValue(propertyName, out currentMask))
                {
                    s_validPropertyMap[propertyName] = currentMask | BitMask;
                }
                else
                {
                    s_validPropertyMap.Add(propertyName, BitMask);
                }

                // *PropertyMappingTableByProperty
                // If toWinNT is null, there's no PAPI->WinNT mapping for this property
                // (it's probably read-only, e.g., "LastLogon").
                //                if (toWinNT != null)
                //                {
                foreach (Hashtable propertyMappingTableByProperty in byPropertyTables)
                {
                    if (propertyMappingTableByProperty[propertyName] == null)
                        propertyMappingTableByProperty[propertyName] = new ArrayList();

                    ((ArrayList)propertyMappingTableByProperty[propertyName]).Add(propertyEntry);
                }
                //                }

                // *PropertyMappingTableByWinNT
                // If fromLdap is null, there's no direct WinNT->PAPI mapping for this property.
                // It's probably a property that requires custom handling, such as an IdentityClaim.
                if (fromWinNT != null)
                {
                    string winNTAttributeLower = winNTAttribute.ToLower(CultureInfo.InvariantCulture);

                    foreach (Hashtable propertyMappingTableByWinNT in byWinNTTables)
                    {
                        if (propertyMappingTableByWinNT[winNTAttributeLower] == null)
                            propertyMappingTableByWinNT[winNTAttributeLower] = new ArrayList();

                        ((ArrayList)propertyMappingTableByWinNT[winNTAttributeLower]).Add(propertyEntry);
                    }
                }
            }
        }

        // Throws exception if ctxBase is not a computer object
        public SAMStoreCtx(DirectoryEntry ctxBase, bool ownCtxBase, string username, string password, ContextOptions options)
        {
            Debug.Assert(ctxBase != null);
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "Constructing SAMStoreCtx for {0}", ctxBase.Path);

            Debug.Assert(SAMUtils.IsOfObjectClass(ctxBase, "Computer"));

            _ctxBase = ctxBase;
            _ownCtxBase = ownCtxBase;

            if (username != null && password != null)
                _credentials = new NetCred(username, password);

            _contextOptions = options;
            _authTypes = SDSUtils.MapOptionsToAuthTypes(options);
        }

        public override void Dispose()
        {
            try
            {
                if (!_disposed)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "Dispose: disposing, ownCtxBase={0}", _ownCtxBase);

                    if (_ownCtxBase)
                        _ctxBase.Dispose();

                    _disposed = true;
                }
            }
            finally
            {
                base.Dispose();
            }
        }

        //
        // StoreCtx information
        //

        // Retrieves the Path (ADsPath) of the object used as the base of the StoreCtx
        internal override string BasePath
        {
            get
            {
                Debug.Assert(_ctxBase != null);
                return _ctxBase.Path;
            }
        }

        //
        // CRUD
        //

        // Used to perform the specified operation on the Principal.  They also make any needed security subsystem
        // calls to obtain digitial signatures.
        //
        // Insert() and Update() must check to make sure no properties not supported by this StoreCtx
        // have been set, prior to persisting the Principal.
        internal override void Insert(Principal p)
        {
            Debug.Assert(p.unpersisted == true);
            Debug.Assert(p.fakePrincipal == false);

            try
            {
                // Insert the principal into the store
                SDSUtils.InsertPrincipal(
                                p,
                                this,
                                new SDSUtils.GroupMembershipUpdater(UpdateGroupMembership),
                                _credentials,
                                _authTypes,
                                false   // handled in PushChangesToNative
                                );

                // Load in all the initial values from the store
                ((DirectoryEntry)p.UnderlyingObject).RefreshCache();

                // Load in the StoreKey
                Debug.Assert(p.Key == null); // since it was previously unpersisted

                Debug.Assert(p.UnderlyingObject != null); // since we just persisted it
                Debug.Assert(p.UnderlyingObject is DirectoryEntry);
                DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;

                // We just created a principal, so it should have an objectSid
                Debug.Assert((de.Properties["objectSid"] != null) && (de.Properties["objectSid"].Count == 1));

                SAMStoreKey key = new SAMStoreKey(
                                            this.MachineFlatName,
                                            (byte[])de.Properties["objectSid"].Value
                                            );
                p.Key = key;

                // Reset the change tracking
                p.ResetAllChangeStatus();

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "Insert: new SID is ", Utils.ByteArrayToString((byte[])de.Properties["objectSid"].Value));
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
        }

        internal override void Update(Principal p)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "Update");

            Debug.Assert(p.fakePrincipal == false);
            Debug.Assert(p.unpersisted == false);
            Debug.Assert(p.UnderlyingObject != null);
            Debug.Assert(p.UnderlyingObject is DirectoryEntry);

            try
            {
                // Commit the properties
                SDSUtils.ApplyChangesToDirectory(
                                            p,
                                            this,
                                            new SDSUtils.GroupMembershipUpdater(UpdateGroupMembership),
                                            _credentials,
                                            _authTypes
                                            );

                // Reset the change tracking
                p.ResetAllChangeStatus();
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
        }

        internal override void Delete(Principal p)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "Delete");
            Debug.Assert(p.fakePrincipal == false);

            // Principal.Delete() shouldn't be calling us on an unpersisted Principal.
            Debug.Assert(p.unpersisted == false);
            Debug.Assert(p.UnderlyingObject != null);

            Debug.Assert(p.UnderlyingObject is DirectoryEntry);

            try
            {
                SDSUtils.DeleteDirectoryEntry((DirectoryEntry)p.UnderlyingObject);
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
        }

        internal override bool AccessCheck(Principal p, PrincipalAccessMask targetPermission)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "AccessCheck " + targetPermission.ToString());

            switch (targetPermission)
            {
                case PrincipalAccessMask.ChangePassword:

                    PropertyValueCollection values = ((DirectoryEntry)p.GetUnderlyingObject()).Properties["UserFlags"];

                    if (values.Count != 0)
                    {
                        Debug.Assert(values.Count == 1);
                        Debug.Assert(values[0] is int);

                        return (SDSUtils.StatusFromAccountControl((int)values[0], PropertyNames.PwdInfoCannotChangePassword));
                    }

                    Debug.Fail("SAMStoreCtx.AccessCheck:  user entry has an empty UserFlags value");

                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "AccessCheck Unable to read userAccountControl");

                    break;

                default:

                    Debug.Fail("SAMStoreCtx.AccessCheck: Fell off end looking for " + targetPermission.ToString());

                    break;
            }

            return false;
        }

        internal override void Move(StoreCtx originalStore, Principal p)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "Move");
        }

        //
        // Special operations: the Principal classes delegate their implementation of many of the
        // special methods to their underlying StoreCtx
        //

        // methods for manipulating accounts

        /// <summary>
        /// This method sets the default user account control bits for the new principal
        /// being created in this account store.
        /// </summary>
        /// <param name="p"> Principal to set the user account control bits for </param>
        internal override void InitializeUserAccountControl(AuthenticablePrincipal p)
        {
            Debug.Assert(p != null);
            Debug.Assert(p.fakePrincipal == false);
            Debug.Assert(p.unpersisted == true); // should only ever be called for new principals            

            // set the userAccountControl bits on the underlying directory entry
            DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;
            Debug.Assert(de != null);
            Type principalType = p.GetType();

            if ((principalType == typeof(UserPrincipal)) || (principalType.IsSubclassOf(typeof(UserPrincipal))))
            {
                de.Properties["userFlags"].Value = SDSUtils.SAM_DefaultUAC;
            }
        }

        internal override bool IsLockedOut(AuthenticablePrincipal p)
        {
            Debug.Assert(p.fakePrincipal == false);

            Debug.Assert(p.unpersisted == false);

            DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;
            Debug.Assert(de != null);

            try
            {
                de.RefreshCache();
                return (bool)de.InvokeGet("IsAccountLocked");
            }
            catch (System.Reflection.TargetInvocationException e)
            {
                if (e.InnerException is System.Runtime.InteropServices.COMException)
                {
                    throw (ExceptionHelper.GetExceptionFromCOMException((System.Runtime.InteropServices.COMException)e.InnerException));
                }
                throw;
            }
        }

        internal override void UnlockAccount(AuthenticablePrincipal p)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "UnlockAccount");

            Debug.Assert(p.fakePrincipal == false);

            Debug.Assert(p.unpersisted == false);

            // Computer accounts are never locked out, so nothing to do
            if (p is ComputerPrincipal)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "UnlockAccount: computer acct, skipping");
                return;
            }

            DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;
            Debug.Assert(de != null);

            // After setting the property, we need to commit the change to the store.
            // We do it in a copy of de, so that we don't inadvertently commit any other
            // pending changes in de.
            DirectoryEntry copyOfDe = null;

            try
            {
                copyOfDe = SDSUtils.BuildDirectoryEntry(de.Path, _credentials, _authTypes);

                Debug.Assert(copyOfDe != null);
                copyOfDe.InvokeSet("IsAccountLocked", new object[] { false });
                copyOfDe.CommitChanges();
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                // ADSI threw an exception trying to write the change
                GlobalDebug.WriteLineIf(GlobalDebug.Error, "SAMStoreCtx", "UnlockAccount: caught COMException, message={0}", e.Message);
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
            finally
            {
                if (copyOfDe != null)
                    copyOfDe.Dispose();
            }
        }

        // methods for manipulating passwords
        internal override void SetPassword(AuthenticablePrincipal p, string newPassword)
        {
            Debug.Assert(p.fakePrincipal == false);

            Debug.Assert(p is UserPrincipal || p is ComputerPrincipal);

            // Shouldn't be being called if this is the case
            Debug.Assert(p.unpersisted == false);

            // ********** In SAM, computer accounts don't have a set password method
            if (p is ComputerPrincipal)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "SAMStoreCtx", "SetPassword: computer acct, can't reset");
                throw new InvalidOperationException(SR.SAMStoreCtxNoComputerPasswordSet);
            }

            Debug.Assert(p != null);
            Debug.Assert(newPassword != null);  // but it could be an empty string

            DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;
            Debug.Assert(de != null);

            SDSUtils.SetPassword(de, newPassword);
        }

        internal override void ChangePassword(AuthenticablePrincipal p, string oldPassword, string newPassword)
        {
            Debug.Assert(p.fakePrincipal == false);

            Debug.Assert(p is UserPrincipal || p is ComputerPrincipal);

            // Shouldn't be being called if this is the case
            Debug.Assert(p.unpersisted == false);

            // ********** In SAM, computer accounts don't have a change password method
            if (p is ComputerPrincipal)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "SAMStoreCtx", "ChangePassword: computer acct, can't change");
                throw new InvalidOperationException(SR.SAMStoreCtxNoComputerPasswordSet);
            }

            Debug.Assert(p != null);
            Debug.Assert(newPassword != null);  // but it could be an empty string
            Debug.Assert(oldPassword != null);  // but it could be an empty string

            DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;
            Debug.Assert(de != null);

            SDSUtils.ChangePassword(de, oldPassword, newPassword);
        }

        internal override void ExpirePassword(AuthenticablePrincipal p)
        {
            Debug.Assert(p.fakePrincipal == false);

            // ********** In SAM, computer accounts don't have a password-expired property
            if (p is ComputerPrincipal)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "SAMStoreCtx", "ExpirePassword: computer acct, can't expire");
                throw new InvalidOperationException(SR.SAMStoreCtxNoComputerPasswordExpire);
            }

            WriteAttribute(p, "PasswordExpired", 1);
        }

        internal override void UnexpirePassword(AuthenticablePrincipal p)
        {
            Debug.Assert(p.fakePrincipal == false);

            // ********** In SAM, computer accounts don't have a password-expired property
            if (p is ComputerPrincipal)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "SAMStoreCtx", "UnexpirePassword: computer acct, can't unexpire");
                throw new InvalidOperationException(SR.SAMStoreCtxNoComputerPasswordExpire);
            }

            WriteAttribute(p, "PasswordExpired", 0);
        }

        private void WriteAttribute(AuthenticablePrincipal p, string attribute, int value)
        {
            Debug.Assert(p is UserPrincipal || p is ComputerPrincipal);

            Debug.Assert(p != null);

            DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;

            SDSUtils.WriteAttribute(de.Path, attribute, value, _credentials, _authTypes);
        }

        //
        // the various FindBy* methods
        //

        internal override ResultSet FindByLockoutTime(
            DateTime dt, MatchType matchType, Type principalType)
        {
            throw new NotSupportedException(SR.StoreNotSupportMethod);
        }

        internal override ResultSet FindByBadPasswordAttempt(
            DateTime dt, MatchType matchType, Type principalType)
        {
            throw new NotSupportedException(SR.StoreNotSupportMethod);
        }

        internal override ResultSet FindByLogonTime(
            DateTime dt, MatchType matchType, Type principalType)
        {
            return FindByDate(FindByDateMatcher.DateProperty.LogonTime, matchType, dt, principalType);
        }

        internal override ResultSet FindByPasswordSetTime(
            DateTime dt, MatchType matchType, Type principalType)
        {
            return FindByDate(FindByDateMatcher.DateProperty.PasswordSetTime, matchType, dt, principalType);
        }

        internal override ResultSet FindByExpirationTime(
            DateTime dt, MatchType matchType, Type principalType)
        {
            return FindByDate(FindByDateMatcher.DateProperty.AccountExpirationTime, matchType, dt, principalType);
        }

        private ResultSet FindByDate(
                        FindByDateMatcher.DateProperty property,
                        MatchType matchType,
                        DateTime value,
                        Type principalType
                        )
        {
            // We use the same SAMQuery set that we use for query-by-example, but with a different
            // SAMMatcher class to perform the date-range filter.

            // Get the entries we'll iterate over.  Write access to Children is controlled through the
            // ctxBaseLock, but we don't want to have to hold that lock while we're iterating over all
            // the child entries.  So we have to clone the ctxBase --- not ideal, but it prevents
            // multithreading issues.
            DirectoryEntries entries = SDSUtils.BuildDirectoryEntry(_ctxBase.Path, _credentials, _authTypes).Children;
            Debug.Assert(entries != null);

            // The SAMQuerySet will use this to restrict the types of DirectoryEntry objects returned.
            List<string> schemaTypes = GetSchemaFilter(principalType);

            // Create the ResultSet that will perform the client-side filtering
            SAMQuerySet resultSet = new SAMQuerySet(
                                                schemaTypes,
                                                entries,
                                                _ctxBase,
                                                -1,             // no size limit 
                                                this,
                                                new FindByDateMatcher(property, matchType, value));

            return resultSet;
        }

        // Get groups of which p is a direct member
        internal override ResultSet GetGroupsMemberOf(Principal p)
        {
            // Enforced by the methods that call us
            Debug.Assert(p.unpersisted == false);

            if (!p.fakePrincipal)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "GetGroupsMemberOf: is real principal");

                // No nested groups or computers as members of groups in SAM
                if (!(p is UserPrincipal))
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "GetGroupsMemberOf: not a user, returning empty set");
                    return new EmptySet();
                }

                Debug.Assert(p.UnderlyingObject != null);

                DirectoryEntry userDE = (DirectoryEntry)p.UnderlyingObject;

                UnsafeNativeMethods.IADsMembers iadsMembers = (UnsafeNativeMethods.IADsMembers)userDE.Invoke("Groups");

                ResultSet resultSet = new SAMGroupsSet(iadsMembers, this, _ctxBase);
                return resultSet;
            }
            else
            {
                // ADSI's IADsGroups doesn't work for fake principals like NT AUTHORITY\NETWORK SERVICE

                // We use the same SAMQuery set that we use for query-by-example, but with a different
                // SAMMatcher class to match groups which contain the specified principal as a member

                // Get the entries we'll iterate over.  Write access to Children is controlled through the
                // ctxBaseLock, but we don't want to have to hold that lock while we're iterating over all
                // the child entries.  So we have to clone the ctxBase --- not ideal, but it prevents
                // multithreading issues.
                DirectoryEntries entries = SDSUtils.BuildDirectoryEntry(_ctxBase.Path, _credentials, _authTypes).Children;
                Debug.Assert(entries != null);

                // The SAMQuerySet will use this to restrict the types of DirectoryEntry objects returned.
                List<string> schemaTypes = GetSchemaFilter(typeof(GroupPrincipal));

                SecurityIdentifier principalSid = p.Sid;
                byte[] SidB = new byte[principalSid.BinaryLength];
                principalSid.GetBinaryForm(SidB, 0);

                if (principalSid == null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "SAMStoreCtx", "GetGroupsMemberOf: bad SID IC");
                    throw new InvalidOperationException(SR.StoreCtxNeedValueSecurityIdentityClaimToQuery);
                }

                // Create the ResultSet that will perform the client-side filtering
                SAMQuerySet resultSet = new SAMQuerySet(
                                                    schemaTypes,
                                                    entries,
                                                    _ctxBase,
                                                    -1,             // no size limit 
                                                    this,
                                                    new GroupMemberMatcher(SidB));

                return resultSet;
            }
        }

        // Get groups from this ctx which contain a principal corresponding to foreignPrincipal
        // (which is a principal from foreignContext)
        internal override ResultSet GetGroupsMemberOf(Principal foreignPrincipal, StoreCtx foreignContext)
        {
            // If it's a fake principal, we don't need to do any of the lookup to find a local representation.
            // We'll skip straight to GetGroupsMemberOf(Principal), which will lookup the groups to which it belongs via its SID.
            if (foreignPrincipal.fakePrincipal)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "GetGroupsMemberOf(ctx): is fake principal");
                return GetGroupsMemberOf(foreignPrincipal);
            }

            // Get the Principal's SID, so we can look it up by SID in our store
            SecurityIdentifier Sid = foreignPrincipal.Sid;

            if (Sid == null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "SAMStoreCtx", "GetGroupsMemberOf(ctx): no SID IC");
                throw new InvalidOperationException(SR.StoreCtxNeedValueSecurityIdentityClaimToQuery);
            }

            // In SAM, only users can be member of SAM groups (no nested groups)
            UserPrincipal u = UserPrincipal.FindByIdentity(this.OwningContext, IdentityType.Sid, Sid.ToString());

            // If no corresponding principal exists in this store, then by definition the principal isn't
            // a member of any groups in this store.
            if (u == null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "GetGroupsMemberOf(ctx): no corresponding user, returning empty set");
                return new EmptySet();
            }

            // Now that we got the principal in our store, enumerating its group membership can be handled the
            // usual way.
            return GetGroupsMemberOf(u);
        }

        // Get groups of which p is a member, using AuthZ S4U APIs for recursive membership
        internal override ResultSet GetGroupsMemberOfAZ(Principal p)
        {
            // Enforced by the methods that call us
            Debug.Assert(p.unpersisted == false);
            Debug.Assert(p is UserPrincipal);

            // Get the user SID that AuthZ will use.
            SecurityIdentifier Sid = p.Sid;

            if (Sid == null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "SAMStoreCtx", "GetGroupsMemberOfAZ: no SID IC");
                throw new InvalidOperationException(SR.StoreCtxNeedValueSecurityIdentityClaimToQuery);
            }

            byte[] Sidb = new byte[Sid.BinaryLength];
            Sid.GetBinaryForm(Sidb, 0);

            if (Sidb == null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "SAMStoreCtx", "GetGroupsMemberOfAZ: Bad SID IC");
                throw new ArgumentException(SR.StoreCtxSecurityIdentityClaimBadFormat);
            }

            try
            {
                return new AuthZSet(Sidb, _credentials, _contextOptions, this.MachineFlatName, this, _ctxBase);
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
        }

        // Get members of group g
        internal override BookmarkableResultSet GetGroupMembership(GroupPrincipal g, bool recursive)
        {
            // Enforced by the methods that call us
            Debug.Assert(g.unpersisted == false);

            // Fake groups are a member of other groups, but they themselves have no members
            // (they don't even exist in the store)
            if (g.fakePrincipal)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "GetGroupMembership: is fake principal, returning empty set");
                return new EmptySet();
            }

            Debug.Assert(g.UnderlyingObject != null);

            DirectoryEntry groupDE = (DirectoryEntry)g.UnderlyingObject;

            UnsafeNativeMethods.IADsGroup iADsGroup = (UnsafeNativeMethods.IADsGroup)groupDE.NativeObject;

            BookmarkableResultSet resultSet = new SAMMembersSet(groupDE.Path, iADsGroup, recursive, this, _ctxBase);
            return resultSet;
        }

        // Is p a member of g in the store?
        internal override bool SupportsNativeMembershipTest { get { return false; } }

        internal override bool IsMemberOfInStore(GroupPrincipal g, Principal p)
        {
            Debug.Fail("SAMStoreCtx.IsMemberOfInStore: Shouldn't be here.");
            return false;
        }

        // Can a Clear() operation be performed on the specified group?  If not, also returns
        // a string containing a human-readable explanation of why not, suitable for use in an exception.
        internal override bool CanGroupBeCleared(GroupPrincipal g, out string explanationForFailure)
        {
            // Always true for this type of StoreCtx
            explanationForFailure = null;
            return true;
        }

        // Can the given member be removed from the specified group?  If not, also returns
        // a string containing a human-readable explanation of why not, suitable for use in an exception.
        internal override bool CanGroupMemberBeRemoved(GroupPrincipal g, Principal member, out string explanationForFailure)
        {
            // Always true for this type of StoreCtx
            explanationForFailure = null;
            return true;
        }

        //
        // Cross-store support
        //

        // Given a native store object that represents a "foreign" principal (e.g., a FPO object in this store that 
        // represents a pointer to another store), maps that representation to the other store's StoreCtx and returns 
        // a Principal from that other StoreCtx.  The implementation of this method is highly dependent on the
        // details of the particular store, and must have knowledge not only of this StoreCtx, but also of how to
        // interact with other StoreCtxs to fulfill the request.
        //
        // This method is typically used by ResultSet implementations, when they're iterating over a collection
        // (e.g., of group membership) and encounter an entry that represents a foreign principal.
        internal override Principal ResolveCrossStoreRefToPrincipal(object o)
        {
            Debug.Assert(o is DirectoryEntry);

            // Get the SID of the foreign principal
            DirectoryEntry foreignDE = (DirectoryEntry)o;

            if (foreignDE.Properties["objectSid"].Count == 0)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "SAMStoreCtx", "ResolveCrossStoreRefToPrincipal: no objectSid found");
                throw new PrincipalOperationException(SR.SAMStoreCtxCantRetrieveObjectSidForCrossStore);
            }

            Debug.Assert(foreignDE.Properties["objectSid"].Count == 1);

            byte[] sid = (byte[])foreignDE.Properties["objectSid"].Value;

            // Ask the OS to resolve the SID to its target.
            int accountUsage = 0;
            string name;
            string domainName;

            int err = Utils.LookupSid(this.MachineUserSuppliedName, _credentials, sid, out name, out domainName, out accountUsage);

            if (err != 0)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn,
                                        "SAMStoreCtx",
                                        "ResolveCrossStoreRefToPrincipal: LookupSid failed, err={0}, server={1}",
                                        err,
                                        this.MachineUserSuppliedName);

                throw new PrincipalOperationException(
                            String.Format(CultureInfo.CurrentCulture,
                                          SR.SAMStoreCtxCantResolveSidForCrossStore,
                                          err));
            }

            GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                    "SAMStoreCtx",
                                    "ResolveCrossStoreRefToPrincipal: LookupSid found {0} in {1}",
                                    name,
                                    domainName);

            // Since this is SAM, the remote principal must be an AD principal.
            // Build a PrincipalContext for the store which owns the principal
            // Use the ad default options so we turn sign and seal back on.
#if USE_CTX_CACHE
            PrincipalContext remoteCtx = SDSCache.Domain.GetContext(domainName, _credentials, DefaultContextOptions.ADDefaultContextOption);
#else
            PrincipalContext remoteCtx = new PrincipalContext(
                            ContextType.Domain,
                            domainName,
                            null,
                            (this.credentials != null ? credentials.UserName : null),
                            (this.credentials != null ? credentials.Password : null),
                            DefaultContextOptions.ADDefaultContextOption);
            
#endif

            SecurityIdentifier sidObj = new SecurityIdentifier(sid, 0);

            Principal p = remoteCtx.QueryCtx.FindPrincipalByIdentRef(
                                            typeof(Principal),
                                            UrnScheme.SidScheme,
                                            sidObj.ToString(),
                                            DateTime.UtcNow);

            if (p != null)
                return p;
            else
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "SAMStoreCtx", "ResolveCrossStoreRefToPrincipal: no matching principal");
                throw new PrincipalOperationException(SR.SAMStoreCtxFailedFindCrossStoreTarget);
            }
        }

        //
        // Data Validation
        //

        // Returns true if AccountInfo is supported for the specified principal, false otherwise.
        // Used when an application tries to access the AccountInfo property of a newly-inserted
        // (not yet persisted) AuthenticablePrincipal, to determine whether it should be allowed.
        internal override bool SupportsAccounts(AuthenticablePrincipal p)
        {
            // Fake principals do not have store objects, so they certainly don't have stored account info.
            if (p.fakePrincipal)
                return false;

            // Both Computer and User support accounts.
            return true;
        }

        // Returns the set of credential types supported by this store for the specified principal.
        // Used when an application tries to access the PasswordInfo property of a newly-inserted
        // (not yet persisted) AuthenticablePrincipal, to determine whether it should be allowed.
        // Also used to implement AuthenticablePrincipal.SupportedCredentialTypes.
        internal override CredentialTypes SupportedCredTypes(AuthenticablePrincipal p)
        {
            // Fake principals do not have store objects, so they certainly don't have stored creds.
            if (p.fakePrincipal)
                return (CredentialTypes)0;

            CredentialTypes supportedTypes = CredentialTypes.Password;

            // Longhorn SAM supports certificate-based authentication
            if (this.IsLSAM)
                supportedTypes |= CredentialTypes.Certificate;

            return supportedTypes;
        }

        //
        // Construct a fake Principal to represent a well-known SID like
        // "\Everyone" or "NT AUTHORITY\NETWORK SERVICE"
        //
        internal override Principal ConstructFakePrincipalFromSID(byte[] sid)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                    "SAMStoreCtx",
                                    "ConstructFakePrincipalFromSID: sid={0}, machine={1}",
                                    Utils.ByteArrayToString(sid),
                                    this.MachineFlatName);

            Principal p = Utils.ConstructFakePrincipalFromSID(
                                                        sid,
                                                        this.OwningContext,
                                                        this.MachineUserSuppliedName,
                                                        _credentials,
                                                        this.MachineUserSuppliedName);

            // Assign it a StoreKey
            SAMStoreKey key = new SAMStoreKey(this.MachineFlatName, sid);
            p.Key = key;

            return p;
        }

        //
        // Private data
        //

        private bool IsLSAM  // IsLonghornSAM (also called MSAM or LH-SAM)
        {
            get
            {
                if (!_isLSAM.HasValue)
                {
                    lock (_computerInfoLock)
                    {
                        if (!_isLSAM.HasValue)
                            LoadComputerInfo();
                    }
                }

                Debug.Assert(_isLSAM.HasValue);
                return _isLSAM.Value;
            }
        }

        internal string MachineUserSuppliedName
        {
            get
            {
                if (_machineUserSuppliedName == null)
                {
                    lock (_computerInfoLock)
                    {
                        if (_machineUserSuppliedName == null)
                            LoadComputerInfo();
                    }
                }

                Debug.Assert(_machineUserSuppliedName != null);
                return _machineUserSuppliedName;
            }
        }

        internal string MachineFlatName
        {
            get
            {
                if (_machineFlatName == null)
                {
                    lock (_computerInfoLock)
                    {
                        if (_machineFlatName == null)
                            LoadComputerInfo();
                    }
                }

                Debug.Assert(_machineFlatName != null);
                return _machineFlatName;
            }
        }

        private object _computerInfoLock = new object();
        private Nullable<bool> _isLSAM = null;
        private string _machineUserSuppliedName = null;
        private string _machineFlatName = null;

        // computerInfoLock must be held coming in here
        private void LoadComputerInfo()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "LoadComputerInfo");

            Debug.Assert(_ctxBase != null);
            Debug.Assert(SAMUtils.IsOfObjectClass(_ctxBase, "Computer"));

            //
            // Target OS version
            //
            int versionMajor;
            int versionMinor;

            if (!SAMUtils.GetOSVersion(_ctxBase, out versionMajor, out versionMinor))
            {
                throw new PrincipalOperationException(SR.SAMStoreCtxUnableToRetrieveVersion);
            }

            Debug.Assert(versionMajor > 0);
            Debug.Assert(versionMinor >= 0);

            if (versionMajor >= 6)      // 6.0 == Longhorn
                _isLSAM = true;
            else
                _isLSAM = false;

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "LoadComputerInfo: ver={0}.{1}", versionMajor, versionMinor);

            //
            // Machine user-supplied name
            //

            // This ADSI property stores the machine name as supplied by the user in the ADsPath.
            // It could be a flat name or a DNS name.
            if (_ctxBase.Properties["Name"].Count > 0)
            {
                Debug.Assert(_ctxBase.Properties["Name"].Count == 1);

                _machineUserSuppliedName = (string)_ctxBase.Properties["Name"].Value;
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "LoadComputerInfo: machineUserSuppliedName={0}", _machineUserSuppliedName);
            }
            else
            {
                throw new PrincipalOperationException(SR.SAMStoreCtxUnableToRetrieveMachineName);
            }

            //
            // Machine flat name
            //
            IntPtr buffer = IntPtr.Zero;

            try
            {
                // This function takes in a flat or DNS name, and returns the flat name of the computer
                int err = UnsafeNativeMethods.NetWkstaGetInfo(_machineUserSuppliedName, 100, ref buffer);
                if (err == 0)
                {
                    UnsafeNativeMethods.WKSTA_INFO_100 wkstaInfo =
                        (UnsafeNativeMethods.WKSTA_INFO_100)Marshal.PtrToStructure(buffer, typeof(UnsafeNativeMethods.WKSTA_INFO_100));

                    _machineFlatName = wkstaInfo.wki100_computername;
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "LoadComputerInfo: machineFlatName={0}", _machineFlatName);
                }
                else
                {
                    throw new PrincipalOperationException(
                                    String.Format(
                                        CultureInfo.CurrentCulture,
                                        SR.SAMStoreCtxUnableToRetrieveFlatMachineName,
                                        err));
                }
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    UnsafeNativeMethods.NetApiBufferFree(buffer);
            }
        }

        internal override bool IsValidProperty(Principal p, string propertyName)
        {
            ObjectMask value = ObjectMask.None;

            if (s_validPropertyMap.TryGetValue(propertyName, out value))
            {
                return ((s_maskMap[p.GetType()] & value) > 0 ? true : false);
            }
            else
            {
                Debug.Assert(false);
                return false;
            }
        }
    }
}

// #endif  // PAPI_REGSAM

