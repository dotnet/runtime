// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using MACLPrinc = System.Security.Principal;

namespace System.DirectoryServices.AccountManagement
{
    internal partial class ADStoreCtx : StoreCtx
    {
        protected DirectoryEntry ctxBase;
        private const int mappingIndex = 0;

        private readonly object _ctxBaseLock = new object(); // when mutating ctxBase

        private readonly bool _ownCtxBase;    // if true, we "own" ctxBase and must Dispose of it when we're done

        private bool _disposed;

        protected internal NetCred Credentials { get { return this.credentials; } }
        protected NetCred credentials;

        protected internal AuthenticationTypes AuthTypes { get { return this.authTypes; } }
        protected AuthenticationTypes authTypes;

        protected ContextOptions contextOptions;

        protected internal virtual void InitializeNewDirectoryOptions(DirectoryEntry newDeChild)
        {
        }

        //
        // Static constructor: used for initializing static tables
        //
        static ADStoreCtx()
        {
            //
            // Load the filterPropertiesTable
            //
            LoadFilterMappingTable(mappingIndex, s_filterPropertiesTableRaw);
            LoadPropertyMappingTable(mappingIndex, s_propertyMappingTableRaw);
        }

        protected virtual int MappingTableIndex
        {
            get
            {
                return mappingIndex;
            }
        }

        protected static void LoadFilterMappingTable(int mappingIndex, object[,] rawFilterPropertiesTable)
        {
            if (null == s_filterPropertiesTable)
                s_filterPropertiesTable = new Hashtable();

            Hashtable mappingTable = new Hashtable();

            for (int i = 0; i < rawFilterPropertiesTable.GetLength(0); i++)
            {
                Type qbeType = rawFilterPropertiesTable[i, 0] as Type;
                string adPropertyName = rawFilterPropertiesTable[i, 1] as string;
                FilterConverterDelegate f = rawFilterPropertiesTable[i, 2] as FilterConverterDelegate;

                Debug.Assert(qbeType != null);
                Debug.Assert(f != null);

                // There should only be one entry per QBE type
                Debug.Assert(mappingTable[qbeType] == null);

                FilterPropertyTableEntry entry = new FilterPropertyTableEntry();
                entry.suggestedADPropertyName = adPropertyName;
                entry.converter = f;

                mappingTable[qbeType] = entry;
            }

            s_filterPropertiesTable.Add(mappingIndex, mappingTable);
        }

        protected static void LoadPropertyMappingTable(int mappingIndex, object[,] rawPropertyMappingTable)
        {
            //
            // Load the propertyMappingTableByProperty and propertyMappingTableByLDAP tables
            //
            if (null == s_propertyMappingTableByProperty)
                s_propertyMappingTableByProperty = new Hashtable();

            if (null == s_propertyMappingTableByLDAP)
                s_propertyMappingTableByLDAP = new Hashtable();

            if (null == s_propertyMappingTableByPropertyFull)
                s_propertyMappingTableByPropertyFull = new Hashtable();

            if (null == TypeToLdapPropListMap)
                TypeToLdapPropListMap = new Dictionary<int, Dictionary<Type, StringCollection>>();

            Hashtable mappingTableByProperty = new Hashtable();
            Hashtable mappingTableByLDAP = new Hashtable();
            Hashtable mappingTableByPropertyFull = new Hashtable();

            Dictionary<string, string[]> propertyNameToLdapAttr = new Dictionary<string, string[]>();

            Dictionary<Type, StringCollection> TypeToLdapDict = new Dictionary<Type, StringCollection>();

            for (int i = 0; i < s_propertyMappingTableRaw.GetLength(0); i++)
            {
                string propertyName = rawPropertyMappingTable[i, 0] as string;
                string ldapAttribute = rawPropertyMappingTable[i, 1] as string;
                FromLdapConverterDelegate fromLdap = rawPropertyMappingTable[i, 2] as FromLdapConverterDelegate;
                ToLdapConverterDelegate toLdap = rawPropertyMappingTable[i, 3] as ToLdapConverterDelegate;

                Debug.Assert(propertyName != null);
                Debug.Assert((ldapAttribute != null && fromLdap != null) || (fromLdap == null));
                //Debug.Assert(toLdap != null);

                // Build the table entry.  The same entry will be used in both tables.
                // Once constructed, the table entries are treated as read-only, so there's
                // no danger in sharing the entries between tables.
                PropertyMappingTableEntry propertyEntry = new PropertyMappingTableEntry();
                propertyEntry.propertyName = propertyName;
                propertyEntry.suggestedADPropertyName = ldapAttribute;
                propertyEntry.ldapToPapiConverter = fromLdap;
                propertyEntry.papiToLdapConverter = toLdap;

                // Build a mapping table from PAPI propertyname to ldapAttribute that we can use below
                // to build a list of ldap attributes for each object type.
                if (null != ldapAttribute)
                {
                    if (propertyNameToLdapAttr.TryGetValue(propertyName, out string[] ldapAttributes))
                    {
                        string[] props = new string[ldapAttributes.Length + 1];
                        ldapAttributes.CopyTo(props, 0);
                        props[ldapAttributes.Length] = ldapAttribute;
                        propertyNameToLdapAttr[propertyName] = props;
                    }
                    else
                        propertyNameToLdapAttr.Add(propertyName, new string[] { ldapAttribute });
                }

                // propertyMappingTableByProperty
                // If toLdap is null, there's no PAPI->LDAP mapping for this property
                // (it's probably read-only, e.g., "lastLogon").
                if (toLdap != null)
                {
                    mappingTableByProperty[propertyName] ??= new ArrayList();

                    ((ArrayList)mappingTableByProperty[propertyName]).Add(propertyEntry);
                }

                mappingTableByPropertyFull[propertyName] ??= new ArrayList();

                ((ArrayList)mappingTableByPropertyFull[propertyName]).Add(propertyEntry);

                // mappingTableByLDAP
                // If fromLdap is null, there's no direct LDAP->PAPI mapping for this property.
                // It's probably a property that requires custom handling, such as IdentityClaim.
                if (fromLdap != null)
                {
                    string ldapAttributeLower = ldapAttribute.ToLowerInvariant();

                    mappingTableByLDAP[ldapAttributeLower] ??= new ArrayList();

                    ((ArrayList)mappingTableByLDAP[ldapAttributeLower]).Add(propertyEntry);
                }
            }

            s_propertyMappingTableByProperty.Add(mappingIndex, mappingTableByProperty);
            s_propertyMappingTableByLDAP.Add(mappingIndex, mappingTableByLDAP);
            s_propertyMappingTableByPropertyFull.Add(mappingIndex, mappingTableByPropertyFull);

            // Build a table of Type mapped to a collection of all ldap attributes for that type.
            // This table will be used to load the objects when searching.

            StringCollection principalPropList = new StringCollection();
            StringCollection authPrincipalPropList = new StringCollection();
            StringCollection userPrincipalPropList = new StringCollection();
            StringCollection computerPrincipalPropList = new StringCollection();
            StringCollection groupPrincipalPropList = new StringCollection();

            foreach (string prop in principalProperties)
            {
                string[] attr;
                if (propertyNameToLdapAttr.TryGetValue(prop, out attr))
                {
                    foreach (string plist in attr)
                    {
                        principalPropList.Add(plist);
                        authPrincipalPropList.Add(plist);
                        userPrincipalPropList.Add(plist);
                        computerPrincipalPropList.Add(plist);
                        groupPrincipalPropList.Add(plist);
                    }
                }
            }

            foreach (string prop in authenticablePrincipalProperties)
            {
                string[] attr;
                if (propertyNameToLdapAttr.TryGetValue(prop, out attr))
                {
                    foreach (string plist in attr)
                    {
                        authPrincipalPropList.Add(plist);
                        userPrincipalPropList.Add(plist);
                        computerPrincipalPropList.Add(plist);
                    }
                }
            }

            foreach (string prop in groupProperties)
            {
                string[] attr;
                if (propertyNameToLdapAttr.TryGetValue(prop, out attr))
                {
                    foreach (string plist in attr)
                    {
                        groupPrincipalPropList.Add(plist);
                    }
                }
            }

            foreach (string prop in userProperties)
            {
                string[] attr;
                if (propertyNameToLdapAttr.TryGetValue(prop, out attr))
                {
                    foreach (string plist in attr)
                    {
                        userPrincipalPropList.Add(plist);
                    }
                }
            }

            foreach (string prop in computerProperties)
            {
                string[] attr;
                if (propertyNameToLdapAttr.TryGetValue(prop, out attr))
                {
                    foreach (string plist in attr)
                    {
                        computerPrincipalPropList.Add(plist);
                    }
                }
            }

            principalPropList.Add("objectClass");
            authPrincipalPropList.Add("objectClass");
            userPrincipalPropList.Add("objectClass");
            computerPrincipalPropList.Add("objectClass");
            groupPrincipalPropList.Add("objectClass");

            TypeToLdapDict.Add(typeof(Principal), principalPropList);
            TypeToLdapDict.Add(typeof(GroupPrincipal), groupPrincipalPropList);
            TypeToLdapDict.Add(typeof(AuthenticablePrincipal), authPrincipalPropList);
            TypeToLdapDict.Add(typeof(UserPrincipal), userPrincipalPropList);
            TypeToLdapDict.Add(typeof(ComputerPrincipal), computerPrincipalPropList);

            TypeToLdapPropListMap.Add(mappingIndex, TypeToLdapDict);
        }

        //
        // Constructor
        //

        // Throws ArgumentException if base is not a container class (as indicated by an empty possibleInferiors
        // attribute in the corresponding schema class definition)
        public ADStoreCtx(DirectoryEntry ctxBase, bool ownCtxBase, string username, string password, ContextOptions options)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "Constructing ADStoreCtx for {0}", ctxBase.Path);

            Debug.Assert(ctxBase != null);

            // This will also detect if the server is down or nonexistent
            if (!IsContainer(ctxBase))
                throw new InvalidOperationException(SR.ADStoreCtxMustBeContainer);

            this.ctxBase = ctxBase;
            _ownCtxBase = ownCtxBase;

            if (username != null && password != null)
                this.credentials = new NetCred(username, password);

            this.contextOptions = options;
            this.authTypes = SDSUtils.MapOptionsToAuthTypes(options);
        }

        protected bool IsContainer(DirectoryEntry de)
        {
            //NOTE: Invoking de.SchemaEntry creates a new DirectoryEntry object, which is not disposed by de.
            using (DirectoryEntry schemaDE = de.SchemaEntry)
            {
                if (schemaDE.Properties["possibleInferiors"].Count == 0)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADStoreCtx", "IsContainer: not a container ({0})", schemaDE.Path);
                    return false;
                }
                return true;
            }
        }

        //
        // IDisposable implementation
        //

        public override void Dispose()
        {
            try
            {
                if (!_disposed)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "Dispose: disposing, ownCtxBase={0}", _ownCtxBase);

                    if (_ownCtxBase)
                        ctxBase.Dispose();

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
                Debug.Assert(this.ctxBase != null);
                return this.ctxBase.Path;
            }
        }

        //
        // CRUD
        //

        // Used to perform the specified operation on the Principal.
        //
        // Insert() and Update() must check to make sure no properties not supported by this StoreCtx
        // have been set, prior to persisting the Principal.
        internal override void Insert(Principal p)
        {
            try
            {
                Debug.Assert(p.unpersisted);
                Debug.Assert(!p.fakePrincipal);

                // Insert the principal into the store
                SDSUtils.InsertPrincipal(
                                p,
                                this,
                                new SDSUtils.GroupMembershipUpdater(UpdateGroupMembership),
                                this.credentials,
                                this.authTypes,
                                true
                                );

                // Load in all the initial values from the store
                //((DirectoryEntry)p.UnderlyingObject).RefreshCache();
                LoadDirectoryEntryAttributes((DirectoryEntry)p.UnderlyingObject);

                // If they set p.Enabled == true, enable the principal
                EnablePrincipalIfNecessary(p);

                // If they set CannotChangePassword then we need to set it here after the object is already created.
                SetPasswordSecurityifNecessary(p);

                // Load in the StoreKey
                Debug.Assert(p.Key == null); // since it was previously unpersisted

                Debug.Assert(p.UnderlyingObject != null); // since we just persisted it
                Debug.Assert(p.UnderlyingObject is DirectoryEntry);

                ADStoreKey key = new ADStoreKey(((DirectoryEntry)p.UnderlyingObject).Guid);
                p.Key = key;

                // Reset the change tracking
                p.ResetAllChangeStatus();

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "Insert: new GUID is ", ((DirectoryEntry)p.UnderlyingObject).Guid);
            }
            catch (PrincipalExistsException)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "Insert,  object already exists");
                throw;
            }
            catch (System.SystemException e)
            {
                try
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Error, "ADStoreCtx", "Insert,  Save Failed (attempting to delete) Exception {0} ", e.Message);
                    if (null != p.UnderlyingObject)
                    {
                        SDSUtils.DeleteDirectoryEntry((DirectoryEntry)p.UnderlyingObject);
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "Insert,  object deleted");
                    }
                    else
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "Insert,  No object was created nothing to delete");
                    }
                }
                catch (System.Runtime.InteropServices.COMException deleteFail)
                {
                    // The delete failed.  Just continue we will throw the original exception below.
                    GlobalDebug.WriteLineIf(GlobalDebug.Error, "ADStoreCtx", "Insert,  Deletion Failed {0} ", deleteFail.Message);
                }

                if (e is System.Runtime.InteropServices.COMException)
                    throw ExceptionHelper.GetExceptionFromCOMException((System.Runtime.InteropServices.COMException)e);
                else
                    throw;
            }
        }

        internal override bool AccessCheck(Principal p, PrincipalAccessMask targetPermission)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "AccessCheck " + targetPermission.ToString());

            switch (targetPermission)
            {
                case PrincipalAccessMask.ChangePassword:

                    return CannotChangePwdFromLdapConverter((DirectoryEntry)p.GetUnderlyingObject());

                default:

                    GlobalDebug.WriteLineIf(GlobalDebug.Error, "ADStoreCtx", "Invalid targetPermission in AccessCheck");

                    break;
            }

            return false;
        }

        /// <summary>
        /// If The enabled property was set on the principal then perform actions
        /// necessary on the principal to set the enabled status to match
        /// the set value.
        /// </summary>
        /// <param name="p"></param>
        private void EnablePrincipalIfNecessary(Principal p)
        {
            if (p.GetChangeStatusForProperty(PropertyNames.AuthenticablePrincipalEnabled))
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "EnablePrincipalIfNecessary: enabling principal");

                Debug.Assert(p is AuthenticablePrincipal);

                bool enable = (bool)p.GetValueForProperty(PropertyNames.AuthenticablePrincipalEnabled);

                SetAuthPrincipalEnableStatus((AuthenticablePrincipal)p, enable);
            }
        }

        private void SetPasswordSecurityifNecessary(Principal p)
        {
            if (p.GetChangeStatusForProperty(PropertyNames.PwdInfoCannotChangePassword))
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "EnablePrincipalIfNecessary: enabling principal");

                Debug.Assert(p is AuthenticablePrincipal);

                SetCannotChangePasswordStatus((AuthenticablePrincipal)p, (bool)p.GetValueForProperty(PropertyNames.PwdInfoCannotChangePassword), true);
            }
        }

        private static void SetCannotChangePasswordStatus(Principal ap, bool userCannotChangePassword, bool commitChanges)
        {
            Debug.Assert(ap is AuthenticablePrincipal);
            Debug.Assert(ap.GetUnderlyingObject() is DirectoryEntry);

            DirectoryEntry de = (DirectoryEntry)ap.GetUnderlyingObject();
            // retrieving ObjectSecurity after
            // previously modifying the ACL will return null unless we force a cache refresh.  We have to do this always,
            // even before we call ObjectSecurity to see if it would return null, because once ObjectSecurity returns null the
            // first time, it'll keep returning null even if we refresh the cache.
            if (!de.Properties.Contains("nTSecurityDescriptor"))
                de.RefreshCache(s_nTSecurityDescriptor);
            ActiveDirectorySecurity adsSecurity = de.ObjectSecurity;

            bool denySelfFound;
            bool denyWorldFound;
            bool allowSelfFound;
            bool allowWorldFound;

            // Scan the existing ACL to determine its current state

            ScanACLForChangePasswordRight(adsSecurity, out denySelfFound, out denyWorldFound, out allowSelfFound, out allowWorldFound);

            // Build the ACEs that we'll use
            ActiveDirectoryAccessRule denySelfACE = new ExtendedRightAccessRule(
                                                                new MACLPrinc.SecurityIdentifier(SelfSddl),
                                                                AccessControlType.Deny,
                                                                s_changePasswordGuid);

            ActiveDirectoryAccessRule denyWorldAce = new ExtendedRightAccessRule(
                                                                new MACLPrinc.SecurityIdentifier(WorldSddl),
                                                                AccessControlType.Deny,
                                                                s_changePasswordGuid);

            ActiveDirectoryAccessRule allowSelfACE = new ExtendedRightAccessRule(
                                                                new MACLPrinc.SecurityIdentifier(SelfSddl),
                                                                AccessControlType.Allow,
                                                                s_changePasswordGuid);

            ActiveDirectoryAccessRule allowWorldAce = new ExtendedRightAccessRule(
                                                                new MACLPrinc.SecurityIdentifier(WorldSddl),
                                                                AccessControlType.Allow,
                                                                s_changePasswordGuid);

            // Based on the current state of the ACL and the userCannotChangePassword status, perform the necessary modifications,
            // if any
            if (userCannotChangePassword)
            {
                // If we want to make it so the user cannot change their password, we need to remove the ALLOW ACEs
                // (if they exist) and add the necessary explicit DENY ACEs if they don't already exist.

                if (!denySelfFound)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "CannotChangePwdToLdapConverter: add deny self");
                    adsSecurity.AddAccessRule(denySelfACE);
                }

                if (!denyWorldFound)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "CannotChangePwdToLdapConverter: add deny world");
                    adsSecurity.AddAccessRule(denyWorldAce);
                }

                if (allowSelfFound)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "CannotChangePwdToLdapConverter: remove allow self");
                    adsSecurity.RemoveAccessRuleSpecific(allowSelfACE);
                }

                if (allowWorldFound)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "CannotChangePwdToLdapConverter: remove allow world");
                    adsSecurity.RemoveAccessRuleSpecific(allowWorldAce);
                }
            }
            else
            {
                // If we want to make to give the user back the right to change their password, we need to remove
                // the explicit DENY ACEs if they exist.  We'll also add in explicit ALLOW ACEs.

                if (denySelfFound)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "CannotChangePwdToLdapConverter: remove deny self");
                    adsSecurity.RemoveAccessRuleSpecific(denySelfACE);
                }

                if (denyWorldFound)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "CannotChangePwdToLdapConverter: remove deny world");
                    adsSecurity.RemoveAccessRuleSpecific(denyWorldAce);
                }

                if (!allowSelfFound)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "CannotChangePwdToLdapConverter: add allow self");
                    adsSecurity.AddAccessRule(allowSelfACE);
                }

                if (!allowWorldFound)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "CannotChangePwdToLdapConverter: add allow world");
                    adsSecurity.AddAccessRule(allowWorldAce);
                }
            }

            if (commitChanges)
                de.CommitChanges();
        }
        /// <summary>
        /// Read the Account Control From the Directory entry.  If the control is read then set or
        /// clear bit 0x2 corresponding to the enable parameter
        /// </summary>
        /// <param name="ap">Principal to modify</param>
        /// <param name="enable">New state of the enable bit</param>
        ///

        protected virtual void SetAuthPrincipalEnableStatus(AuthenticablePrincipal ap, bool enable)
        {
            try
            {
                Debug.Assert(ap.fakePrincipal == false);

                int uacValue;

                DirectoryEntry de = (DirectoryEntry)ap.UnderlyingObject;

                if (de.Properties["userAccountControl"].Count > 0)
                {
                    Debug.Assert(de.Properties["userAccountControl"].Count == 1);

                    uacValue = (int)de.Properties["userAccountControl"][0];
                }
                else
                {
                    // Since we loaded the properties, we should have it.  Perhaps we don't have access
                    // to it.  In that case, we don't want to blindly overwrite whatever other bits might be there.
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADStoreCtx", "SetAuthPrincipalEnableStatus: can't read userAccountControl");

                    throw new PrincipalOperationException(
                                SR.ADStoreCtxUnableToReadExistingAccountControlFlagsToEnable);
                }

                if (enable && ((uacValue & 0x2) != 0))
                {
                    // It's currently disabled, and we need to enable it
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADStoreCtx", "SetAuthPrincipalEnableStatus: Enabling (old uac={0})", uacValue);

                    Utils.ClearBit(ref uacValue, 0x2);    // UF_ACCOUNTDISABLE

                    WriteAttribute(ap, "userAccountControl", uacValue);
                }
                else if (!enable && ((uacValue & 0x2) == 0))
                {
                    // It's current enabled, and we need to disable it
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADStoreCtx", "SetAuthPrincipalEnableStatus: Disabling (old uac={0})", uacValue);

                    Utils.SetBit(ref uacValue, 0x2);    // UF_ACCOUNTDISABLE

                    WriteAttribute(ap, "userAccountControl", uacValue);
                }
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
        }
        /// <summary>
        /// Apply all changed properties on the principal to the Directory Entry.
        /// Reset the changed status on all the properties
        /// </summary>
        /// <param name="p">Principal to update</param>
        internal override void Update(Principal p)
        {
            try
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "Update");

                Debug.Assert(p.fakePrincipal == false);
                Debug.Assert(p.unpersisted == false);
                Debug.Assert(p.UnderlyingObject != null);
                Debug.Assert(p.UnderlyingObject is DirectoryEntry);

                // Commit the properties
                SDSUtils.ApplyChangesToDirectory(
                                            p,
                                            this,
                                            new SDSUtils.GroupMembershipUpdater(UpdateGroupMembership),
                                            this.credentials,
                                            this.authTypes
                                            );

                // Reset the change tracking
                p.ResetAllChangeStatus();
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
        }

        /// <summary>
        /// Delete the directory entry that corresponds to the principal
        /// </summary>
        /// <param name="p">Principal to delete</param>
        internal override void Delete(Principal p)
        {
            try
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "Delete");

                Debug.Assert(p.fakePrincipal == false);

                // Principal.Delete() shouldn't be calling us on an unpersisted Principal.
                Debug.Assert(p.unpersisted == false);
                Debug.Assert(p.UnderlyingObject != null);

                Debug.Assert(p.UnderlyingObject is DirectoryEntry);
                SDSUtils.DeleteDirectoryEntry((DirectoryEntry)p.UnderlyingObject);
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
        }

        internal override void Move(StoreCtx originalStore, Principal p)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "Move");

            Debug.Assert(p != null);
            Debug.Assert(originalStore != null);
            Debug.Assert(originalStore is ADStoreCtx);

            string name = null;
            string rdnPrefix = p.ExtensionHelper.RdnPrefix;
            string baseObjectRdnPrefix = null;
            Type principalType = p.GetType();

            if (null == rdnPrefix)
            {
                throw new InvalidOperationException(SR.ExtensionInvalidClassAttributes);
            }

            if (p.GetChangeStatusForProperty(PropertyNames.PrincipalName))
            {
                name = rdnPrefix + "=" + (string)p.GetValueForProperty(PropertyNames.PrincipalName);

                // If the principal class is derived from Group, Computer or User then we need to see
                // if the class has an RdnPrefix set that differs from the base class prefix.  If so then we need
                // to modify that attribute when if changed the name during the move.

                if (principalType.IsSubclassOf(typeof(GroupPrincipal)) ||
                     principalType.IsSubclassOf(typeof(UserPrincipal)) ||
                     principalType.IsSubclassOf(typeof(ComputerPrincipal)))
                {
                    DirectoryRdnPrefixAttribute[] MyAttribute =
                    (DirectoryRdnPrefixAttribute[])Attribute.GetCustomAttributes(principalType.BaseType, typeof(DirectoryRdnPrefixAttribute), false);

                    if (MyAttribute == null)
                        throw new InvalidOperationException(SR.ExtensionInvalidClassAttributes);

                    string defaultRdn = null;

                    // Search for the rdn prefix.  This will use either the prefix that has a context type
                    // that matches the principals context or the first rdnPrefix that has a null context type
                    for (int i = 0; i < MyAttribute.Length; i++)
                    {
                        if ((MyAttribute[i].Context == null && null == defaultRdn) ||
                            (p.ContextType == MyAttribute[i].Context))
                        {
                            defaultRdn = MyAttribute[i].RdnPrefix;
                        }
                    }

                    // If the base objects RDN prefix is not the same as the derived class then we need to set both
                    if (defaultRdn != rdnPrefix)
                    {
                        baseObjectRdnPrefix = defaultRdn;
                    }
                }
            }

            SDSUtils.MoveDirectoryEntry((DirectoryEntry)p.GetUnderlyingObject(),
                                                        ctxBase,
                                                        name);

            p.LoadValueIntoProperty(PropertyNames.PrincipalName, p.GetValueForProperty(PropertyNames.PrincipalName));

            if (null != baseObjectRdnPrefix)
            {
                ((DirectoryEntry)p.GetUnderlyingObject()).Properties[baseObjectRdnPrefix].Value = (string)p.GetValueForProperty(PropertyNames.PrincipalName);
            }
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
            Debug.Assert(!p.fakePrincipal);
            Debug.Assert(p.unpersisted); // should only ever be called for new principals

            // set the userAccountControl bits on the underlying directory entry
            DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;
            Debug.Assert(de != null);
            Type principalType = p.GetType();

            if ((principalType == typeof(ComputerPrincipal)) || (principalType.IsSubclassOf(typeof(ComputerPrincipal))))
            {
                de.Properties["userAccountControl"].Value = SDSUtils.AD_DefaultUAC_Machine;
            }
            else if ((principalType == typeof(UserPrincipal)) || (principalType.IsSubclassOf(typeof(UserPrincipal))))
            {
                de.Properties["userAccountControl"].Value = SDSUtils.AD_DefaultUAC;
            }
        }

        /// <summary>
        /// Determine if principal account is locked.
        /// First read User-Account-control-computed from the DE.  On Uplevel platforms this computed attribute will exist and we can
        /// just check bit 0x0010.  On DL platforms this attribute does not exist so we must read lockoutTime and return locked if
        /// this is greater than 0
        /// </summary>
        /// <param name="p">Principal to check status</param>
        /// <returns>true is account is locked, false if not</returns>
        internal override bool IsLockedOut(AuthenticablePrincipal p)
        {
            try
            {
                Debug.Assert(p.fakePrincipal == false);

                Debug.Assert(p.unpersisted == false);

                DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;
                Debug.Assert(de != null);

                de.RefreshCache(s_msDSUACCLockoutTime);

                if (de.Properties["msDS-User-Account-Control-Computed"].Count > 0)
                {
                    // Uplevel platform --- the DC will compute it for us
                    Debug.Assert(de.Properties["msDS-User-Account-Control-Computed"].Count == 1);
                    int uacComputed = (int)de.Properties["msDS-User-Account-Control-Computed"][0];

                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "IsLockedOut: using computed uac={0}", uacComputed);

                    return ((uacComputed & 0x0010) != 0);   // UF_LOCKOUT
                }
                else
                {
                    // Downlevel platform --- we have to compute it
                    bool isLockedOut = false;

                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "IsLockedOut: downlevel");

                    if (de.Properties["lockoutTime"].Count > 0)
                    {
                        ulong lockoutTime = (ulong)ADUtils.LargeIntToInt64((UnsafeNativeMethods.IADsLargeInteger)de.Properties["lockoutTime"][0]);

                        if (lockoutTime != 0)
                        {
                            ulong lockoutDuration = this.LockoutDuration;

                            GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                                    "ADStoreCtx",
                                                    "IsLockedOut: lockoutTime={0}, lockoutDuration={1}",
                                                    lockoutTime,
                                                    lockoutDuration);

                            if ((lockoutDuration + lockoutTime) > ((ulong)ADUtils.DateTimeToADFileTime(DateTime.UtcNow)))
                                isLockedOut = true;
                        }
                    }

                    return isLockedOut;
                }
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
        }

        /// <summary>
        /// Unlock account by setting LockoutTime to 0
        /// </summary>
        /// <param name="p">Principal to unlock</param>
        internal override void UnlockAccount(AuthenticablePrincipal p)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "UnlockAccount");

            Debug.Assert(p.fakePrincipal == false);

            WriteAttribute(p, "lockoutTime", 0);
        }

        // methods for manipulating passwords
        /// <summary>
        /// Set the password on the principal. This function requires administrator privileges
        /// </summary>
        /// <param name="p">Principal to modify</param>
        /// <param name="newPassword">New password</param>
        internal override void SetPassword(AuthenticablePrincipal p, string newPassword)
        {
            Debug.Assert(p.fakePrincipal == false);

            Debug.Assert(p != null);
            Debug.Assert(newPassword != null);  // but it could be an empty string

            DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;
            Debug.Assert(de != null);

            SDSUtils.SetPassword(de, newPassword);
        }

        /// <summary>
        /// Change the password on the principal
        /// </summary>
        /// <param name="p">Principal to modify</param>
        /// <param name="oldPassword">Current password</param>
        /// <param name="newPassword">New password</param>
        internal override void ChangePassword(AuthenticablePrincipal p, string oldPassword, string newPassword)
        {
            Debug.Assert(p.fakePrincipal == false);

            // Shouldn't be being called if this is the case
            Debug.Assert(p.unpersisted == false);

            Debug.Assert(p != null);
            Debug.Assert(newPassword != null);  // but it could be an empty string
            Debug.Assert(oldPassword != null);  // but it could be an empty string

            if ((p.GetType() == typeof(ComputerPrincipal)) || (p.GetType().IsSubclassOf(typeof(ComputerPrincipal))))
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Error, "ADStoreCtx", "ChangePassword: computer acct, can't change password.");
                throw new NotSupportedException(SR.ADStoreCtxNoComputerPasswordChange);
            }

            DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;
            Debug.Assert(de != null);

            SDSUtils.ChangePassword(de, oldPassword, newPassword);
        }
        /// <summary>
        /// Expire password by setting pwdLastSet to 0
        /// </summary>
        /// <param name="p"></param>
        internal override void ExpirePassword(AuthenticablePrincipal p)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "ExpirePassword");

            Debug.Assert(p.fakePrincipal == false);

            WriteAttribute(p, "pwdLastSet", 0);
        }

        /// <summary>
        /// Unexpire password by setting pwdLastSet to -1
        /// </summary>
        /// <param name="p"></param>
        internal override void UnexpirePassword(AuthenticablePrincipal p)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "UnexpirePassword");

            Debug.Assert(p.fakePrincipal == false);

            WriteAttribute(p, "pwdLastSet", -1);
        }

        /// <summary>
        /// Set value for attribute on the passed principal.  This is only valid for integer attribute types
        /// </summary>
        /// <param name="p"></param>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        protected void WriteAttribute(Principal p, string attribute, int value)
        {
            ;

            Debug.Assert(p != null);

            DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;

            SDSUtils.WriteAttribute(de.Path, attribute, value, this.credentials, this.authTypes);
        }

        protected void WriteAttribute<T>(Principal p, string attribute, T value)
        {
            Debug.Assert(p != null);

            DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;

            SDSUtils.WriteAttribute<T>(de.Path, attribute, value, this.credentials, this.authTypes);
        }

        // the various FindBy* methods
        internal override ResultSet FindByLockoutTime(
            DateTime dt, MatchType matchType, Type principalType)
        {
            return FindByDate(principalType, s_lockoutTime, matchType, dt);
        }

        internal override ResultSet FindByLogonTime(
            DateTime dt, MatchType matchType, Type principalType)
        {
            return FindByDate(principalType, s_lastLogonTime, matchType, dt);
        }

        internal override ResultSet FindByPasswordSetTime(
            DateTime dt, MatchType matchType, Type principalType)
        {
            return FindByDate(principalType, s_pwdLastSet, matchType, dt);
        }

        internal override ResultSet FindByBadPasswordAttempt(
            DateTime dt, MatchType matchType, Type principalType)
        {
            return FindByDate(principalType, s_badPasswordTime, matchType, dt);
        }

        internal override ResultSet FindByExpirationTime(
            DateTime dt, MatchType matchType, Type principalType)
        {
            return FindByDate(principalType, s_accountExpires, matchType, dt);
        }

        private ADEntriesSet FindByDate(Type subtype, string[] ldapAttributes, MatchType matchType, DateTime value)
        {
            Debug.Assert(ldapAttributes != null);
            Debug.Assert(ldapAttributes.Length > 0);
            Debug.Assert(subtype == typeof(Principal) || subtype.IsSubclassOf(typeof(Principal)));
            DirectorySearcher ds = new DirectorySearcher(this.ctxBase);

            try
            {
                // Pick some reasonable default values
                ds.PageSize = 256;
                ds.ServerTimeLimit = new TimeSpan(0, 0, 30);  // 30 seconds

                // We don't need any attributes returned, since we're just going to get a DirectoryEntry
                // for the result.  Per RFC 2251, OID 1.1 == no attributes.
                BuildPropertySet(subtype, ds.PropertiesToLoad);

                // Build the LDAP filter
                string ldapValue = ADUtils.DateTimeToADString(value);
                StringBuilder ldapFilter = new StringBuilder();

                ldapFilter.Append(GetObjectClassPortion(subtype));
                ldapFilter.Append("(|");

                foreach (string ldapAttribute in ldapAttributes)
                {
                    ldapFilter.Append('(');

                    switch (matchType)
                    {
                        case MatchType.Equals:
                            ldapFilter.Append(ldapAttribute);
                            ldapFilter.Append('=');
                            ldapFilter.Append(ldapValue);
                            break;

                        case MatchType.NotEquals:
                            ldapFilter.Append("!(");
                            ldapFilter.Append(ldapAttribute);
                            ldapFilter.Append('=');
                            ldapFilter.Append(ldapValue);
                            ldapFilter.Append(')');
                            break;

                        case MatchType.GreaterThanOrEquals:
                            ldapFilter.Append(ldapAttribute);
                            ldapFilter.Append(">=");
                            ldapFilter.Append(ldapValue);
                            break;

                        case MatchType.LessThanOrEquals:
                            ldapFilter.Append(ldapAttribute);
                            ldapFilter.Append("<=");
                            ldapFilter.Append(ldapValue);
                            break;

                        case MatchType.GreaterThan:
                            ldapFilter.Append('&');

                            // Greater-than-or-equals (or less-than-or-equals))
                            ldapFilter.Append('(');
                            ldapFilter.Append(ldapAttribute);
                            ldapFilter.Append(matchType == MatchType.GreaterThan ? ">=" : "<=");
                            ldapFilter.Append(ldapValue);
                            ldapFilter.Append(')');

                            // And not-equal
                            ldapFilter.Append("(!(");
                            ldapFilter.Append(ldapAttribute);
                            ldapFilter.Append('=');
                            ldapFilter.Append(ldapValue);
                            ldapFilter.Append("))");

                            // And exists (need to include because of tristate LDAP logic)
                            ldapFilter.Append('(');
                            ldapFilter.Append(ldapAttribute);
                            ldapFilter.Append("=*)");
                            break;

                        case MatchType.LessThan:
                            goto case MatchType.GreaterThan;

                        default:
                            Debug.Fail("ADStoreCtx.FindByDate: fell off end looking for " + matchType.ToString());
                            break;
                    }

                    ldapFilter.Append(')');
                }

                ldapFilter.Append("))");

                ds.Filter = ldapFilter.ToString();
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "FindByDate: using LDAP filter {0}", ds.Filter);

                // Perform the search
                SearchResultCollection src = ds.FindAll();
                Debug.Assert(src != null);

                // Create a ResultSet for the search results
                ADEntriesSet resultSet = new ADEntriesSet(src, this);

                return resultSet;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
            finally
            {
                ds.Dispose();
            }
        }

        // Get groups of which p is a direct member
        // search
        // 1.  search for group with same group id as principals primary group ID.
        // Then
        // 2.  use enumeration to expand the users group membership
        // ASQ will not work because we cannot correctly generate referrals if one of the users
        // groups if from another domain in the forest.
        internal override ResultSet GetGroupsMemberOf(Principal p)
        {
            // Enforced by the methods that call us
            Debug.Assert(p.unpersisted == false);

            DirectoryEntry gcPrincipalDe = null;
            DirectorySearcher memberOfSearcher = null;
            ADDNConstraintLinkedAttrSet.ResultValidator resultValidator = null;

            try
            {
                if (p.fakePrincipal)
                {
                    // If p is a fake principal, this will find the representation of p in the store
                    // (namely, a FPO), and return the groups of which that FPO is a member
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "GetGroupsMemberOf: fake principal");
                    return GetGroupsMemberOf(p, this);
                }

                Debug.Assert(p.UnderlyingObject != null);

                string primaryGroupDN = null;
                ResultSet resultSet = null;
                bool useASQ = false;
                List<DirectoryEntry> roots = new List<DirectoryEntry>(1);
                DirectorySearcher[] searchers = null;
                IEnumerable[] enumerators = null;

                DirectoryEntry principalDE = (DirectoryEntry)p.GetUnderlyingObject();

                if ((p.ContextType == ContextType.ApplicationDirectory) || (p.Context.ServerInformation.OsVersion == DomainControllerMode.Win2k))
                {
                    useASQ = false;
                }
                else
                {
                    useASQ = true;
                }

                if (p.ContextType != ContextType.ApplicationDirectory)
                {
                    // A users group membership that applies to a particular domain includes the domain's universal, local and global groups plus the
                    // universal groups from every other domain in the forest that the user is a member of.  To get this list we must contact both a GlobalCatalog to get the forest
                    // universal list and a DC in the users domain to get the domain local groups which are not replicated to the GC.
                    // If we happen to get a GC in the same domain as the user
                    // then we don't also need a DC because the domain local group memberships will show up as well.  The enumerator code that expands these lists must detect
                    // duplicates because the list of global groups will show up on both the GC and DC.
                    Debug.Assert(p.ContextType == ContextType.Domain);

                    Forest forest = Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, this.DnsForestName, this.credentials?.UserName, this.credentials?.Password));

                    DirectoryContext dc = new DirectoryContext(DirectoryContextType.Domain, this.DnsDomainName, this.credentials?.UserName, this.credentials?.Password);
                    DomainController dd = DomainController.FindOne(dc);

                    GlobalCatalog gc = null;

                    try
                    {
                        gc = forest.FindGlobalCatalog();

                        var gg = forest.FindAllGlobalCatalogs(dd.SiteName);
                        foreach (GlobalCatalog g in gg)
                        {
                            if (string.Equals(this.DnsDomainName, g.Domain.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                gc = g;
                                break;
                            }
                        }

                        roots.Add(new DirectoryEntry("GC://" + gc.Name + "/" + p.DistinguishedName, this.credentials?.UserName, this.credentials?.Password, this.AuthTypes));

                        if (!string.Equals(this.DnsDomainName, gc.Domain.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            //useASQ = false;
                            roots.Add(principalDE);

                            //Since the GC does not belong to the same domain (as the principal object passed)
                            //We should make sure that we ignore domain local groups that we obtained from the cross-domain GC.
                            resultValidator = delegate (dSPropertyCollection resultPropCollection)
                            {
                                if (resultPropCollection["groupType"].Count > 0 && resultPropCollection["objectSid"].Count > 0)
                                {
                                    int? groupTypeValue = (int?)resultPropCollection["groupType"][0];
                                    if (groupTypeValue.HasValue && ((groupTypeValue.Value & ADGroupScope.Local) == ADGroupScope.Local))
                                    {
                                        byte[] sidByteArray = (byte[])resultPropCollection["objectSid"][0];
                                        SecurityIdentifier resultSid = new SecurityIdentifier(sidByteArray, 0);
                                        return ADUtils.AreSidsInSameDomain(p.Sid, resultSid);
                                    }
                                }
                                return true; //Return true for all other case, including the case where we don't have permissions
                                //to read groupType/objectSid attribute then we declare the result as a match.
                            };
                        }
                    }
                    catch (System.DirectoryServices.ActiveDirectory.ActiveDirectoryOperationException e)
                    {
                        // if we can't get a GC then just fail.
                        throw new PrincipalOperationException(e.Message, e);
                    }
                    catch (System.DirectoryServices.ActiveDirectory.ActiveDirectoryObjectNotFoundException e)
                    {
                        // if we can't get a GC then just fail.
                        throw new PrincipalOperationException(e.Message, e);
                    }
                    finally
                    {
                        gc?.Dispose();
                        forest?.Dispose();
                    }
                }

                if (false == useASQ)
                {
                    // If this is ADAM then we only need to use the original object.
                    // IF AD then we will use whatever enumerators we discovered above.
                    if (p.ContextType != ContextType.ApplicationDirectory)
                    {
                        int index = 0;
                        enumerators = new IEnumerable[roots.Count];

                        foreach (DirectoryEntry de in roots)
                        {
                            //If, de is not equal to principalDE then it must have been created by this function (above code)
                            //In that case de is NOT owned by any other modules outside. Hence, configure RangeRetriever to dispose the DirEntry on its dispose.
                            enumerators[index] = new RangeRetriever(de, "memberOf", (de != principalDE));
                            index++;
                        }
                    }
                    else
                    {
                        enumerators = new IEnumerable[1];
                        //Since principalDE is not owned by us,
                        //configuring RangeRetriever _NOT_ to dispose the DirEntry on its dispose.
                        enumerators[0] = new RangeRetriever(principalDE, "memberOf", false);
                    }
                }
                else
                {
                    int index = 0;
                    searchers = new DirectorySearcher[roots.Count];

                    foreach (DirectoryEntry de in roots)
                    {
                        searchers[index] = SDSUtils.ConstructSearcher(de);
                        searchers[index].SearchScope = SearchScope.Base;
                        searchers[index].AttributeScopeQuery = "memberOf";
                        searchers[index].Filter = "(objectClass=*)";
                        searchers[index].CacheResults = false;

                        BuildPropertySet(typeof(GroupPrincipal), searchers[index].PropertiesToLoad);
                        index++;
                    }
                }

                string principalDN = (string)principalDE.Properties["distinguishedName"].Value;

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "GetGroupsMemberOf: principalDN={0}", principalDN);

                principalDE.RefreshCache(s_memberOfPrimaryGroupId);

                if ((principalDE.Properties["primaryGroupID"].Count > 0) &&
                    (principalDE.Properties["objectSid"].Count > 0))
                {
                    Debug.Assert(principalDE.Properties["primaryGroupID"].Count == 1);
                    Debug.Assert(principalDE.Properties["objectSid"].Count == 1);

                    int primaryGroupID = (int)principalDE.Properties["primaryGroupID"].Value;
                    byte[] principalSid = (byte[])principalDE.Properties["objectSid"].Value;

                    primaryGroupDN = GetGroupDnFromGroupID(principalSid, primaryGroupID);
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "GetGroupsMemberOf: primary group DN={0}", primaryGroupDN);
                }

                // We must use enumeration to expand the users group membership
                // ASQ will not work because we cannot correctly generate referrals if one of the users
                // groups if from another domain in the forest.

                if (useASQ)
                {
                    if (resultValidator != null)
                    {
                        resultSet = new ADDNConstraintLinkedAttrSet(
                            ADDNConstraintLinkedAttrSet.ConstraintType.ResultValidatorDelegateMatch,
                            resultValidator, principalDN, searchers, primaryGroupDN, null, false, this);
                    }
                    else
                    {
                        resultSet = new ADDNLinkedAttrSet(principalDN, searchers, primaryGroupDN, null, false, this);
                    }
                }
                else
                {
                    if (resultValidator != null)
                    {
                        resultSet = new ADDNConstraintLinkedAttrSet(
                            ADDNConstraintLinkedAttrSet.ConstraintType.ResultValidatorDelegateMatch,
                            resultValidator, principalDN, enumerators, primaryGroupDN, null, false, this);
                    }
                    else
                    {
                        resultSet = new ADDNLinkedAttrSet(principalDN, enumerators, primaryGroupDN, null, false, this);
                    }
                }
                return resultSet;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
            finally
            {
                gcPrincipalDe?.Dispose();
                memberOfSearcher?.Dispose();
            }
        }

        // Get groups from this ctx which contain a principal corresponding to foreignPrincipal
        // (which is a principal from foreignContext)
        internal override ResultSet GetGroupsMemberOf(Principal foreignPrincipal, StoreCtx foreignContext)
        {
            // Get the Principal's SID, so we can look it up by SID in our store
            SecurityIdentifier Sid = foreignPrincipal.Sid;

            if (Sid == null)
                throw new InvalidOperationException(SR.StoreCtxNeedValueSecurityIdentityClaimToQuery);

            // Search our store for a object with a matching SID.  This could be a user/group/computer object,
            // or a foreignSecurityPrincipal.  Doesn't really matter --- either way, the store object will have a objectSid
            // and a memberOf attribute.
            // SID search
            //
            //
            //  If we can read the defaultNamingContext and retrieve the well known path for the foreignSecurityPrincipal container start there.
            //  If we can only read the defaultNamingContext then start there
            //  Else just start at the base DN from the original context
            //
            //  If the object was not found and we started at teh fsp container then search the entire DC.
            //  Else just exit. we have nothing else to search

            // An object exists in the domain that contains links to all the groups it is a member of.
            bool rootPrincipalExists = true;

            if ((foreignContext is ADStoreCtx) && !(foreignContext is ADAMStoreCtx))
            {
                // We only need to check forest status for AD stores. Forest concept does not apply to ADAM.
                ADStoreCtx foreignADStore = (ADStoreCtx)foreignContext;

                // If same forest but different domain then we have a child or alternate tree domain.  We don't have a starting user
                // object and must do a search on all groups to find membership.
                if (string.Equals(foreignADStore.DnsForestName, this.DnsForestName, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(foreignADStore.DnsDomainName, this.DnsDomainName, StringComparison.OrdinalIgnoreCase))
                    {
                        rootPrincipalExists = true;
                    }
                    else
                    {
                        rootPrincipalExists = false;
                    }
                }
            }

            DirectoryEntry dncContainer = null;
            string fspWkDn = null;
            DirectoryEntry fspContainer = null;
            ResultSet resultSet = null;
            DirectorySearcher ds = null;

            try
            {
                if (rootPrincipalExists)
                {
                    if (this.DefaultNamingContext != null)
                    {
                        dncContainer = new DirectoryEntry(@"LDAP://" + this.UserSuppliedServerName + @"/" + this.DefaultNamingContext, Credentials != null ? this.Credentials.UserName : null, Credentials != null ? this.Credentials.Password : null, this.AuthTypes);

                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "GetGroupsMemberOf(ctx): Read DNC of {0}", this.DefaultNamingContext);

                        fspWkDn = ADUtils.RetrieveWkDn(dncContainer, this.DefaultNamingContext, this.UserSuppliedServerName, Constants.GUID_FOREIGNSECURITYPRINCIPALS_CONTAINER_BYTE);

                        if (null != fspWkDn)
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "GetGroupsMemberOf(ctx): Read fsp DN {0}", fspWkDn);
                            fspContainer = new DirectoryEntry(fspWkDn, Credentials != null ? this.credentials.UserName : null, Credentials != null ? this.credentials.Password : null, this.authTypes);
                        }
                    }

                    ds = new DirectorySearcher(fspContainer ?? dncContainer ?? this.ctxBase);

                    // Pick some reasonable default values
                    ds.PageSize = 256;
                    ds.ServerTimeLimit = new TimeSpan(0, 0, 30);  // 30 seconds

                    // Build the LDAP filter
                    // Converr the object to a SDDL format
                    string stringSid = Utils.SecurityIdentifierToLdapHexFilterString(Sid);
                    if (stringSid == null)
                        throw new InvalidOperationException(SR.StoreCtxNeedValueSecurityIdentityClaimToQuery);

                    ds.Filter = "(objectSid=" + stringSid + ")";

                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "GetGroupsMemberOf(ctx): using LDAP filter {0}", ds.Filter);

                    // We only need a few attributes
                    ds.PropertiesToLoad.Add("memberOf");
                    ds.PropertiesToLoad.Add("distinguishedName");
                    ds.PropertiesToLoad.Add("primaryGroupID");
                    ds.PropertiesToLoad.Add("objectSid");

                    // If no corresponding principal exists in this store, then by definition the principal isn't
                    // a member of any groups in this store.
                    SearchResult sr = ds.FindOne();

                    if (sr == null)
                    {
                        // no match so we better do a root level search in case we are targeting a domain where
                        // the user is not an FSP.

                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "GetGroupsMemberOf(ctx): No match");

                        // We already did a root level search so just exit.

                        if (null == fspWkDn)
                            return new EmptySet();

                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "GetGroupsMemberOf(ctx): performing DNC level search");

                        ds.SearchRoot = dncContainer;
                        sr = ds.FindOne();

                        if (sr == null)
                            return new EmptySet();
                    }

                    // Now that we found the corresponding principal, the rest is very similar to the plain GetGroupsMemberOf()
                    // case, exception we're working with search results (SearchResult/ResultPropertyValueCollection) rather
                    // than DirectoryEntry/PropertyValueCollection.
                    string principalDN = (string)sr.Properties["distinguishedName"][0];
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "GetGroupsMemberOf(ctx): match, DN={0}", principalDN);

                    //Here a new DirectoryEntry object is created by sr.GetDirectoryEntry() and passed
                    //to RangeRetriever object. Hence, configuring RangeRetriever to dispose the DirEntry on its dispose.
                    IEnumerable memberOf = new RangeRetriever(sr.GetDirectoryEntry(), "memberOf", true);

                    string primaryGroupDN = null;

                    if ((sr.Properties["primaryGroupID"].Count > 0) &&
                        (sr.Properties["objectSid"].Count > 0))
                    {
                        Debug.Assert(sr.Properties["primaryGroupID"].Count == 1);
                        Debug.Assert(sr.Properties["objectSid"].Count == 1);

                        int primaryGroupID = (int)sr.Properties["primaryGroupID"][0];
                        byte[] principalSid = (byte[])sr.Properties["objectSid"][0];

                        primaryGroupDN = GetGroupDnFromGroupID(principalSid, primaryGroupID);
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "GetGroupsMemberOf(ctx): primary group DN={0}", primaryGroupDN);
                    }

                    resultSet = new ADDNConstraintLinkedAttrSet(ADDNConstraintLinkedAttrSet.ConstraintType.ContainerStringMatch, this.ctxBase.Properties["distinguishedName"].Value, principalDN, new IEnumerable[] { memberOf }, primaryGroupDN, null, false, this);
                }
                else
                {
                    // We don't need to retrieve the Primary group ID here because we have already established that this user is not from this domain
                    // and the users primary group must be from the same domain as the user.
                    Debug.Assert(foreignPrincipal.ContextType != ContextType.ApplicationDirectory);

                    DirectorySearcher[] memberSearcher = { SDSUtils.ConstructSearcher(this.ctxBase) };
                    memberSearcher[0].Filter = "(&(objectClass=Group)(member=" + foreignPrincipal.DistinguishedName + "))";
                    memberSearcher[0].CacheResults = false;

                    resultSet = new ADDNLinkedAttrSet(foreignPrincipal.DistinguishedName, memberSearcher, null, null, false, this);
                }

                return resultSet;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
            finally
            {
                fspContainer?.Dispose();
                ds?.Dispose();
                dncContainer?.Dispose();
            }
        }

        private unsafe string GetGroupDnFromGroupID(byte[] userSid, int primaryGroupId)
        {
            void* pGroupSid = null;
            byte[] groupSid = null;

            // This function is based on the technique in KB article 297951.

            try
            {
                string sddlSid = Utils.ConvertSidToSDDL(userSid);
                if (sddlSid != null)
                {
                    // Next, we modify the SDDL SID to replace with final subauthority
                    // with the primary group's RID (the primaryGroupID)
                    int index = sddlSid.LastIndexOf('-');

                    if (index != -1)
                    {
                        sddlSid = sddlSid.Substring(0, index) + "-" + ((uint)primaryGroupId).ToString(CultureInfo.InvariantCulture);

                        // Now, we convert the SDDL back into a SID
                        if (Interop.Advapi32.ConvertStringSidToSid(sddlSid, out pGroupSid) != Interop.BOOL.FALSE)
                        {
                            // Now we convert the native SID to a byte[] SID
                            groupSid = Utils.ConvertNativeSidToByteArray((IntPtr)pGroupSid);
                        }
                    }
                }
            }
            finally
            {
                if (pGroupSid is not null)
                    Interop.Kernel32.LocalFree(pGroupSid);
            }

            if (groupSid != null)
            {
                return "<SID=" + Utils.ByteArrayToString(groupSid) + ">";
            }

            return null;
        }

        // Get groups of which p is a member, using AuthZ S4U APIs for recursive membership
        internal override ResultSet GetGroupsMemberOfAZ(Principal p)
        {
            // Enforced by the methods that call us
            Debug.Assert(p.unpersisted == false);
            Debug.Assert(p is UserPrincipal);

            // Get the user SID that AuthZ will use.
            SecurityIdentifier SidObj = p.Sid;

            if (SidObj == null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADStoreCtx", "GetGroupsMemberOfAZ: no SID IC");
                throw new InvalidOperationException(SR.StoreCtxNeedValueSecurityIdentityClaimToQuery);
            }

            byte[] sid = new byte[SidObj.BinaryLength];
            SidObj.GetBinaryForm(sid, 0);

            if (sid == null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADStoreCtx", "GetGroupsMemberOfAZ: bad SID IC");
                throw new ArgumentException(SR.StoreCtxSecurityIdentityClaimBadFormat);
            }

            try
            {
                if (ADUtils.VerifyOutboundTrust(this.DnsDomainName, this.credentials?.UserName, this.credentials?.Password))
                {
                    return new AuthZSet(sid, this.credentials, this.contextOptions, this.FlatDomainName, this, this.ctxBase);
                }
                else
                {
                    DirectoryEntry principalDE = (DirectoryEntry)p.UnderlyingObject;
                    string principalDN = (string)principalDE.Properties["distinguishedName"].Value;

                    return (new TokenGroupSet(principalDN, this, true));
                }
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
        }

        // Get members of group g
        // Need 2 searchers
        // 1.  Users with this group as their primary group ID
        // 2.  ASQ search against the member attribute on the group object for all contained objects.
        internal override BookmarkableResultSet GetGroupMembership(GroupPrincipal g, bool recursive)
        {
            // Enforced by the methods that call us
            Debug.Assert(g.unpersisted == false);

            // Fake groups are a member of other groups, but they themselves have no members
            // (they don't even exist in the store)
            if (g.fakePrincipal)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "GetGroupMembership: fake principal");
                return new EmptySet();
            }

            Debug.Assert(g.UnderlyingObject != null);

            try
            {
                DirectoryEntry groupDE = (DirectoryEntry)g.UnderlyingObject;

                // Create a DirectorySearcher for users who are members of this group via their primaryGroupId attribute
                DirectorySearcher ds = null;

                if (groupDE.Properties["objectSid"].Count > 0)
                {
                    Debug.Assert(groupDE.Properties["objectSid"].Count == 1);
                    byte[] groupSid = (byte[])groupDE.Properties["objectSid"][0];

                    ds = GetDirectorySearcherFromGroupID(groupSid);
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "GetGroupMembership: using LDAP filter={0}", ds.Filter);
                }

                string groupDN = (string)groupDE.Properties["distinguishedName"].Value;
                BookmarkableResultSet resultSet = null;

                // We must use enumeration to expand groups if their scope is Universal or Local
                // or if the domain controller is w2k
                // or if the context type is ApplicationDirectory (in AD LDS, Global groups can contain members from other partition)
                // Universal and Local groups can contain members from other domains in the forest.  When this occurs
                // the referral is not generated correctly and we get an error.
                //
                if (g.Context.ContextType == ContextType.ApplicationDirectory ||
                    g.Context.ServerInformation.OsVersion == DomainControllerMode.Win2k ||
                    g.GroupScope != GroupScope.Global)
                {
                    //Here the directory entry passed to RangeRetriever constructor belongs to
                    //the GroupPrincipal object supplied to this function, which is not owned by us.
                    //Hence, configuring RangeRetriever _NOT_ to dispose the DirEntry on its dispose.
                    IEnumerable members = new RangeRetriever(groupDE, "member", false);

                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "GetGroupMembership: groupDN={0}", groupDN);
                    resultSet = new ADDNLinkedAttrSet(groupDN, new IEnumerable[] { members }, null, ds, recursive, this);
                }
                else
                {
                    DirectorySearcher[] dsMembers = new DirectorySearcher[1];
                    dsMembers[0] = SDSUtils.ConstructSearcher((DirectoryEntry)g.UnderlyingObject);
                    dsMembers[0].AttributeScopeQuery = "member";
                    dsMembers[0].SearchScope = SearchScope.Base;
                    dsMembers[0].Filter = "(objectClass=*)";
                    dsMembers[0].CacheResults = false;

                    BuildPropertySet(typeof(UserPrincipal), dsMembers[0].PropertiesToLoad);
                    BuildPropertySet(typeof(GroupPrincipal), dsMembers[0].PropertiesToLoad);
                    resultSet = new ADDNLinkedAttrSet(groupDN, dsMembers, null, ds, recursive, this);
                }

                return resultSet;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
        }

        private DirectorySearcher GetDirectorySearcherFromGroupID(byte[] groupSid)
        {
            Debug.Assert(groupSid != null);

            // Get the group's RID from the group's SID
            int groupRid = Utils.GetLastRidFromSid(groupSid);

            // Build a DirectorySearcher for users whose primaryGroupId == the group's RID
            DirectorySearcher ds = new DirectorySearcher(this.ctxBase);
            ds.Filter = GetObjectClassPortion(typeof(Principal)) + "(primaryGroupId=" + groupRid.ToString(CultureInfo.InvariantCulture) + "))";

            // Pick some reasonable default values
            ds.PageSize = 256;
            ds.ServerTimeLimit = new TimeSpan(0, 0, 30);  // 30 seconds

            BuildPropertySet(typeof(Principal), ds.PropertiesToLoad);

            return ds;
        }

        // Is p a member of g in the store?
        internal override bool SupportsNativeMembershipTest { get { return true; } }

        /// First check direct group membership by using DE.IsMember
        /// If this fails then we may have a ForeignSecurityPrincipal so search for Foreign Security Principals
        /// With the p's SID and then call IsMember with the ADS Path returned from the search.
        internal override bool IsMemberOfInStore(GroupPrincipal g, Principal p)
        {
            Debug.Assert(g.unpersisted == false);
            Debug.Assert(p.unpersisted == false);

            // Consistent with GetGroupMembership, a group that is a fake principal has no members
            if (g.fakePrincipal)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "IsMemberOfInStore: fake group");
                return false;
            }

            // AD Groups can only have AD principals as members
            if (p.ContextType != ContextType.Domain && p.ContextType != ContextType.ApplicationDirectory)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "IsMemberOfInStore: member is not a domain principal");
                return false;
            }

            Debug.Assert(g.UnderlyingObject != null && g.UnderlyingObject is DirectoryEntry);
            IEnumerable cachedMembersEnum = null; //This variables stores a reference to the direct members enumerator of the group.

            // Only real principals can be directly a member of the group, since only real principals
            // actually exist in the store.
            if (!p.fakePrincipal)
            {
                Debug.Assert(p.UnderlyingObject != null && p.UnderlyingObject is DirectoryEntry);

                //// Test for direct membership
                ////

                DirectoryEntry principalDE = (DirectoryEntry)p.UnderlyingObject;
                DirectoryEntry groupDE = (DirectoryEntry)g.UnderlyingObject;
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "IsMemberOfInStore: real principal, DN={0}", principalDE.Path);
                string principalDN = (string)principalDE.Properties["distinguishedName"].Value;

                // we want to find if a group is "small", meaning that it has less than MaxValRange values (usually 1500)
                // the property list for the searcher of a group has "member" attribute. if there are more results than MaxValRange, there will also be a "member;range=..." attribute
                if (g.IsSmallGroup())
                {
                    // small groups has special search object that holds the member attribute so we use it for our search (no need to use the DirectoryEntry)
                    Debug.Assert(g.SmallGroupMemberSearchResult != null);
                    cachedMembersEnum = g.SmallGroupMemberSearchResult.Properties["member"];
                    if ((g.SmallGroupMemberSearchResult != null) && g.SmallGroupMemberSearchResult.Properties["member"].Contains(principalDN))
                    {
                        return true;
                    }
                }
                else
                {
                    // this is a large group. use range retrieval instead of simple attribute check.
                    RangeRetriever rangeRetriever = new RangeRetriever(groupDE, "member", false);
                    rangeRetriever.CacheValues = true;
                    foreach (string memberDN in rangeRetriever)
                    {
                        if (principalDN.Equals(memberDN, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    rangeRetriever.Reset(); //Reset range-retriever enum, so that it can be traversed again.
                    cachedMembersEnum = rangeRetriever;
                }
            }

            //
            // Might be an FPO (either a real principal from another forest, or a fake principal
            // that AD represents as an FPO).  Search for the FPO, and test its DN for membership.
            //

            // Get the Principal's SID, so we can look it up by SID in our store
            SecurityIdentifier Sid = p.Sid;

            if (Sid == null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADStoreCtx", "IsMemberOfInStore: no SID IC or null UrnValue");
                throw new ArgumentException(SR.StoreCtxNeedValueSecurityIdentityClaimToQuery);
            }
            DirectoryEntry defaultNCDirEntry = null;
            DirectorySearcher ds = null;

            try
            {
                string path = $"LDAP://{(string.IsNullOrEmpty(this.UserSuppliedServerName) ? this.DnsHostName : this.UserSuppliedServerName)}/{this.ContextBasePartitionDN}";

                defaultNCDirEntry = SDSUtils.BuildDirectoryEntry(path, this.credentials, this.authTypes);

                ds = new DirectorySearcher(defaultNCDirEntry);

                // Pick some reasonable default values
                ds.ServerTimeLimit = new TimeSpan(0, 0, 30);  // 30 seconds

                // Build the LDAP filter, Convert the sid to SDDL format
                string stringSid = Utils.SecurityIdentifierToLdapHexFilterString(Sid);
                if (stringSid == null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADStoreCtx", "IsMemberOfInStore: bad SID IC");
                    throw new ArgumentException(SR.StoreCtxNeedValueSecurityIdentityClaimToQuery);
                }

                ds.Filter = "(&(objectClass=foreignSecurityPrincipal)(objectSid=" + stringSid + "))";
                GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                        "ADStoreCtx",
                                        "IsMemberOfInStore: FPO principal, using LDAP filter {0}",
                                        ds.Filter);

                ds.PropertiesToLoad.Add("distinguishedName");

                SearchResult sr = ds.FindOne();

                // No FPO ---> not a member
                if (sr == null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "IsMemberOfInStore: no FPO found");
                    return false;
                }
                string fpoDN = (string)sr.Properties["distinguishedName"][0];
                foreach (string memberDN in cachedMembersEnum)
                {
                    if (string.Equals(fpoDN, memberDN, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
            finally
            {
                ds?.Dispose();
                defaultNCDirEntry?.Dispose();
            }
        }

        // The only reason a Clear() operation can not be performed on the group is if there
        // are one or more members on the store who are a member of the group by virtue of their
        // primaryGroupId, rather than by the group's "member" attribute.
        internal override bool CanGroupBeCleared(GroupPrincipal g, out string explanationForFailure)
        {
            explanationForFailure = null;

            // If the group is unpersisted, there are certainly no principals in the store who
            // are a member of it by vitue of primaryGroupId.  If the group is a fake group, it has no
            // members.  Either way, they can clear it.
            if (g.unpersisted || g.fakePrincipal)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                        "ADStoreCtx",
                                        "CanGroupBeCleared: unpersisted={0}, fake={1}",
                                        g.unpersisted,
                                        g.fakePrincipal);
                return true;
            }

            Debug.Assert(g.UnderlyingObject != null);
            DirectoryEntry groupDE = (DirectoryEntry)g.UnderlyingObject;

            // Create a DirectorySearcher for users who are members of this group via their primaryGroupId attribute
            DirectorySearcher ds = null;

            try
            {
                if (groupDE.Properties["objectSid"].Count > 0)
                {
                    Debug.Assert(groupDE.Properties["objectSid"].Count == 1);
                    byte[] groupSid = (byte[])groupDE.Properties["objectSid"][0];

                    ds = GetDirectorySearcherFromGroupID(groupSid);

                    // We only need to know if there's at least one such user
                    ds.SizeLimit = 1;

                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "IsMemberOfInStore: using LDAP filter {0}", ds.Filter);

                    SearchResult sr = ds.FindOne();

                    if (sr != null)
                    {
                        // there is such a member, we can't clear the group
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "IsMemberOfInStore: found member, can't clear");

                        explanationForFailure = SR.ADStoreCtxCantClearGroup;
                        return false;
                    }
                    else
                    {
                        // no such members, we can clear the group
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "IsMemberOfInStore: no member, can clear");

                        return true;
                    }
                }
                else
                {
                    // We don't have sufficient information.  Assume we can clear it.
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADStoreCtx", "IsMemberOfInStore: can't search, assume can clear");
                    return true;
                }
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
            finally
            {
                ds?.Dispose();
            }
        }

        // The only reason we wouldn't be able to remove this member is if it's a member by virtue of its
        // primaryGroupId rather than the group's "member" attribute
        internal override bool CanGroupMemberBeRemoved(GroupPrincipal g, Principal member, out string explanationForFailure)
        {
            explanationForFailure = null;

            // If the member is unpersisted, it has no primaryGroupId attribute that could point to the group.
            // If the member is a fake princiapl, it also has no primaryGroupId attribute that could point to the group.
            // So either way, we have no objections to it being removed from the group.
            if (member.unpersisted || member.fakePrincipal)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                        "ADStoreCtx",
                                        "CanGroupMemberBeRemoved: member unpersisted={0}, fake={1}",
                                        member.unpersisted,
                                        member.fakePrincipal);

                return true;
            }

            // If the group is unpersisted, then clearly there is no way the principal is
            // a member of it by vitue of primaryGroupId.  If the group is a fake group, it has no
            // members and so we don't care about it.
            if (g.unpersisted || g.fakePrincipal)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                        "ADStoreCtx",
                                        "CanGroupMemberBeRemoved: group unpersisted={0}, fake={1}",
                                        g.unpersisted,
                                        g.fakePrincipal);
                return true;
            }

            //  ADAM groups can have AD or ADAM members,  AD groups can only have AD members
            //, but we could be called before our caller
            // has verified the principal being passed in as the member.  Just ignore it if the member isn't an AD principal,
            // it'll be caught later in PrincipalCollection.Remove().
            if ((g.ContextType == ContextType.Domain && member.ContextType != ContextType.Domain) ||
                (member.ContextType != ContextType.Domain && member.ContextType != ContextType.ApplicationDirectory))
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "CanGroupMemberBeRemoved: member is not a domain or application directory principal");
                return true;
            }

            try
            {
                Debug.Assert(g.UnderlyingObject != null);
                Debug.Assert(member.UnderlyingObject != null);
                DirectoryEntry groupDE = (DirectoryEntry)g.UnderlyingObject;
                DirectoryEntry memberDE = (DirectoryEntry)member.UnderlyingObject;

                if ((groupDE.Properties["objectSid"].Count > 0) &&
                    (memberDE.Properties["primaryGroupID"].Count > 0))
                {
                    Debug.Assert(groupDE.Properties["objectSid"].Count == 1);
                    Debug.Assert(memberDE.Properties["primaryGroupID"].Count == 1);

                    byte[] groupSid = (byte[])groupDE.Properties["objectSid"][0];
                    int primaryGroupID = (int)memberDE.Properties["primaryGroupID"][0];

                    int groupRid = Utils.GetLastRidFromSid(groupSid);

                    if (groupRid == primaryGroupID)
                    {
                        // It is a primary group member, we can't remove it.
                        GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                                "ADStoreCtx",
                                                "CanGroupMemberBeRemoved: primary group member (rid={0}), can't remove",
                                                groupRid);

                        explanationForFailure = SR.ADStoreCtxCantRemoveMemberFromGroup;
                        return false;
                    }
                    else
                    {
                        // It's not a primary group member, we can remove it.
                        GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                                "ADStoreCtx",
                                                "CanGroupMemberBeRemoved: not primary group member (group rid={0}, primary group={1}), can remove",
                                                groupRid,
                                                primaryGroupID);

                        return true;
                    }
                }
                else
                {
                    // We don't have sufficient information.  Assume we can remove it.
                    // If we can't, we'll get an exception when we try to save the changes.
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADStoreCtx", "CanGroupMemberBeRemoved: can't test, assume can remove");
                    return true;
                }
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
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
            Debug.Assert(ADUtils.IsOfObjectClass((DirectoryEntry)o, "foreignSecurityPrincipal"));

            try
            {
                // Get the SID of the foreign principal
                DirectoryEntry fpoDE = (DirectoryEntry)o;

                if (fpoDE.Properties["objectSid"].Count == 0)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADStoreCtx", "ResolveCrossStoreRefToPrincipal: no objectSid found");
                    throw new PrincipalOperationException(SR.ADStoreCtxCantRetrieveObjectSidForCrossStore);
                }

                Debug.Assert(fpoDE.Properties["objectSid"].Count == 1);

                byte[] sid = (byte[])fpoDE.Properties["objectSid"].Value;

                // What type of SID is it?
                SidType sidType = Utils.ClassifySID(sid);

                if (sidType == SidType.FakeObject)
                {
                    // It's a FPO for something like NT AUTHORITY\NETWORK SERVICE.
                    // There's no real store object corresponding to this FPO, so
                    // fake a Principal.
                    GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                            "ADStoreCtx",
                                            "ResolveCrossStoreRefToPrincipal: fake principal, SID={0}",
                                            Utils.ByteArrayToString(sid));

                    return ConstructFakePrincipalFromSID(sid);
                }

                StoreCtx foreignStoreCtx;
                if (sidType == SidType.RealObjectFakeDomain)
                {
                    // This is a BUILTIN object.  It's a real object on the store we're connected to, but LookupSid
                    // will tell us it's a member of the BUILTIN domain.  Resolve it as a principal on our store.
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADStoreCtx", "ResolveCrossStoreRefToPrincipal: builtin principal");

                    foreignStoreCtx = this;
                }
                else
                {
                    // Ask the OS to resolve the SID to its target.
                    UnsafeNativeMethods.IAdsObjectOptions objOptions = (UnsafeNativeMethods.IAdsObjectOptions)this.ctxBase.NativeObject;
                    string serverName = (string)objOptions.GetOption(0 /* == ADS_OPTION_SERVERNAME */);

                    int accountUsage = 0;

                    string name;
                    string domainName;

                    int err = Utils.LookupSid(serverName, this.credentials, sid, out name, out domainName, out accountUsage);

                    if (err != 0)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Warn,
                                                "ADStoreCtx",
                                                "ResolveCrossStoreRefToPrincipal: LookupSid failed, err={0}, server={1}",
                                                err,
                                                serverName);

                        throw new PrincipalOperationException(
                                SR.Format(SR.ADStoreCtxCantResolveSidForCrossStore, err));
                    }

                    GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                            "ADStoreCtx",
                                            "ResolveCrossStoreRefToPrincipal: LookupSid found {0} in {1}",
                                            name,
                                            domainName);

                    // Since this is AD, the remote principal must be an AD principal.
                    // Build a PrincipalContext for the store which owns the principal

                    // We are now connecting to AD so change to negotiate with signing and sealing

                    ContextOptions remoteOptions = DefaultContextOptions.ADDefaultContextOption;

                    PrincipalContext remoteCtx = SDSCache.Domain.GetContext(domainName, this.credentials, remoteOptions);
                    foreignStoreCtx = remoteCtx.QueryCtx;
                }

                Principal p = foreignStoreCtx.FindPrincipalByIdentRef(
                                                typeof(Principal),
                                                UrnScheme.SidScheme,
                                                (new SecurityIdentifier(sid, 0)).ToString(),
                                                DateTime.UtcNow);

                if (p != null)
                    return p;
                else
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADStoreCtx", "ResolveCrossStoreRefToPrincipal: no matching principal");
                    throw new PrincipalOperationException(SR.ADStoreCtxFailedFindCrossStoreTarget);
                }
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
        }

        // Returns true if AccountInfo is supported for the specified principal, false otherwise.
        // Used when an application tries to access the AccountInfo property of a newly-inserted
        // (not yet persisted) AuthenticablePrincipal, to determine whether it should be allowed.
        internal override bool SupportsAccounts(AuthenticablePrincipal p)
        {
            // Fake principals do not have store objects, so they certainly don't have stored account info.
            if (p.fakePrincipal)
                return false;

            // Computer is a subclass of user in AD, and both therefore support account info.
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

            return CredentialTypes.Password | CredentialTypes.Certificate;
        }

        //
        // When called, this function does a GetInfoEx() to preload the DirectoryEntry's
        // property cache with all the attributes we'll be using.  This avoids DirectoryEntry
        // doing an implicit GetInfo() and pulling down every attribute.
        //
        // This function is currently loading every attribute from the directory instead of using the known list.
        internal void LoadDirectoryEntryAttributes(DirectoryEntry de)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "LoadDirectoryEntryAttributes, path={0}", de.Path);

            try
            {
                //            de.RefreshCache(ldapAttributesUsed);
                de.RefreshCache();
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
        }

        //
        // Construct a fake Principal to represent a well-known SID like
        // "\Everyone" or "NT AUTHORITY\NETWORK SERVICE"
        //
        internal override Principal ConstructFakePrincipalFromSID(byte[] sid)
        {
            UnsafeNativeMethods.IAdsObjectOptions objOptions = (UnsafeNativeMethods.IAdsObjectOptions)this.ctxBase.NativeObject;
            string serverName = (string)objOptions.GetOption(0 /* == ADS_OPTION_SERVERNAME */);

            GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                    "ADStoreCtx",
                                    "ConstructFakePrincipalFromSID: sid={0}, server={1}, authority={2}",
                                    Utils.ByteArrayToString(sid),
                                    serverName,
                                    this.DnsDomainName);

            Principal p = Utils.ConstructFakePrincipalFromSID(
                                                        sid,
                                                        this.OwningContext,
                                                        serverName,
                                                        this.credentials,
                                                        this.DnsDomainName);

            // Assign it a StoreKey
            ADStoreKey key = new ADStoreKey(this.DnsDomainName, sid);
            p.Key = key;

            return p;
        }

        //
        // Private data
        //

        ///
        /// <summary>
        /// Returns the DN of the Partition to which the user supplied
        /// context base (this.ctxBase) belongs.
        /// </summary>
        ///
        internal string ContextBasePartitionDN
        {
            get
            {
                if (this.contextBasePartitionDN == null)
                {
                    lock (this.domainInfoLock)
                    {
                        if (contextBasePartitionDN == null)
                            LoadDomainInfo();
                    }
                }

                return this.contextBasePartitionDN;
            }
        }

        internal string DefaultNamingContext
        {
            get
            {
                if (this.defaultNamingContext == null)
                {
                    lock (this.domainInfoLock)
                    {
                        if (defaultNamingContext == null)
                            LoadDomainInfo();
                    }
                }

                return this.defaultNamingContext;
            }
        }

        private string FlatDomainName
        {
            get
            {
                if (this.domainFlatName == null)
                {
                    lock (this.domainInfoLock)
                    {
                        if (domainFlatName == null)
                            LoadDomainInfo();
                    }
                }

                return this.domainFlatName;
            }
        }

        internal string DnsDomainName
        {
            get
            {
                if (this.domainDnsName == null)
                {
                    lock (this.domainInfoLock)
                    {
                        if (domainDnsName == null)
                            LoadDomainInfo();
                    }
                }

                return this.domainDnsName;
            }
        }

        internal string DnsHostName
        {
            get
            {
                if (this.dnsHostName == null)
                {
                    lock (this.domainInfoLock)
                    {
                        if (dnsHostName == null)
                            LoadDomainInfo();
                    }
                }

                return this.dnsHostName;
            }
        }

        internal string DnsForestName
        {
            get
            {
                if (this.forestDnsName == null)
                {
                    lock (this.domainInfoLock)
                    {
                        if (forestDnsName == null)
                            LoadDomainInfo();
                    }
                }

                return this.forestDnsName;
            }
        }

        internal string UserSuppliedServerName
        {
            get
            {
                if (this.userSuppliedServerName == null)
                {
                    lock (this.domainInfoLock)
                    {
                        if (this.userSuppliedServerName == null)
                            LoadDomainInfo();
                    }
                }

                return this.userSuppliedServerName;
            }
        }

        private ulong LockoutDuration
        {
            get
            {
                // We test domainDnsName for null because lockoutDuration could legitimately be 0,
                // but lockoutDuration is valid iff domainDnsName is non-null
                if (this.domainDnsName == null)
                {
                    lock (this.domainInfoLock)
                    {
                        if (domainDnsName == null)
                            LoadDomainInfo();
                    }
                }

                return this.lockoutDuration;
            }
        }

        protected object domainInfoLock = new object();
        protected string domainFlatName;
        protected string domainDnsName;
        protected string forestDnsName;
        protected string userSuppliedServerName;
        protected string defaultNamingContext;
        protected string contextBasePartitionDN; //contains the DN of the Partition to which the user supplied context base (this.ctxBase) belongs.
        protected string dnsHostName;
        protected ulong lockoutDuration;
        private static readonly string[] s_lockoutTime = new string[] { "lockoutTime" };
        private static readonly string[] s_lastLogonTime = new string[] { "lastLogon", "lastLogonTimestamp" };
        private static readonly string[] s_pwdLastSet = new string[] { "pwdLastSet" };
        private static readonly string[] s_badPasswordTime = new string[] { "badPasswordTime" };
        private static readonly string[] s_accountExpires = new string[] { "accountExpires" };
        private static readonly string[] s_nTSecurityDescriptor = new string[] { "nTSecurityDescriptor" };
        private static readonly string[] s_msDSUACCLockoutTime = new string[] { "msDS-User-Account-Control-Computed", "lockoutTime" };
        private static readonly string[] s_memberOfPrimaryGroupId = new string[] { "memberOf", "primaryGroupID" };
        private static readonly string[] s_lockoutDuration = new string[] { "lockoutDuration" };
        internal static readonly char[] s_comma = new char[] { ',' };

        protected enum StoreCapabilityMap
        {
            ASQSearch = 1,
        }

        // Must be called inside of lock(domainInfoLock)
        protected virtual void LoadDomainInfo()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "LoadComputerInfo");

            Debug.Assert(this.ctxBase != null);

            //
            // DNS Domain Name
            //

            // We need to connect to the server's rootDse to get the naming context
            // From that, we can build the DNS Domain Name
            this.dnsHostName = ADUtils.GetServerName(this.ctxBase);

            // Pull the requested port number
            Uri ldapUri = new Uri(this.ctxBase.Path);
            int port = ldapUri.Port != -1 ? ldapUri.Port : (ldapUri.Scheme.ToUpperInvariant() == "LDAPS" ? 636 : 389);

            string dnsDomainName = "";

            using (DirectoryEntry rootDse = new DirectoryEntry("LDAP://" + this.dnsHostName + ":" + port + "/rootDse", "", "", AuthenticationTypes.Anonymous))
            {
                this.defaultNamingContext = (string)rootDse.Properties["defaultNamingContext"][0];
                this.contextBasePartitionDN = this.defaultNamingContext;

                // Split the naming context's DN into its RDNs
                string[] ncComponents = defaultNamingContext.Split(s_comma);

                StringBuilder sb = new StringBuilder();

                // Reassemble the RDNs (minus the tag) into the DNS domain name
                foreach (string component in ncComponents)
                {
                    // If it's not a "DC=" component, skip it
                    if ((component.Length > 3) &&
                        string.Equals(component.Substring(0, 3), "DC=", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append(component, 3, component.Length - 3);
                        sb.Append('.');
                    }
                }

                dnsDomainName = sb.ToString();

                // The loop added an extra period at the end.  Remove it.
                if (dnsDomainName.Length > 0)
                    dnsDomainName = dnsDomainName.Substring(0, dnsDomainName.Length - 1);

                this.domainDnsName = dnsDomainName;
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "LoadComputerInfo: using DNS domain name {0}", dnsDomainName);
            }

            //
            // NetBios (flat) Domain Name, and DNS Forest Name
            //
            // Given the DNS domain name, we use DsGetDcName to get the flat name.
            // The same DsGetDcName call also retrieves the DNS forest name as a side effect.
            //

            // DS_IS_DNS_NAME | DS_RETURN_FLAT_NAME | DS_DIRECTORY_SERVICE_REQUIRED | DS_BACKGROUND_ONLY
            int flags = unchecked((int)(0x00020000 | 0x80000000 | 0x00000010 | 0x00000100));
            try
            {
                UnsafeNativeMethods.DomainControllerInfo info = Utils.GetDcName(null, dnsDomainName, null, flags);

                this.domainFlatName = info.DomainName;
                this.forestDnsName = info.DnsForestName;
            }
            catch (PrincipalOperationException ex) when (ex.ErrorCode == 1355) // ERROR_NO_SUCH_DOMAIN
            {
                // We couldn't get the NetBios name using DsGetDcName. This could be because we are running on
                // a computer which is not connected to the forest of the domain at all. In such a case, we fall back
                // to using the caller-specified DNS domain name. It may not work in 100% of cases, but it's better than
                // simply failing outright, and if it's not good enough, we'll run into an appropriate error down the line
                // when other calls fail.

                GlobalDebug.WriteLineIf(GlobalDebug.Warn,
                                        "ADStoreCtx",
                                        "GetDcName failed, so falling back to the DNS domain name ({0}) for the Flat/DNS names",
                                        dnsDomainName);

                this.domainFlatName = dnsDomainName;
                this.forestDnsName = dnsDomainName;
            }

            GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                    "ADStoreCtx",
                                    "LoadComputerInfo: using domainFlatName={0}, forestDnsName={1}",
                                    this.domainFlatName,
                                    this.forestDnsName);

            //
            // Lockout duration
            //
            // Query the domain NC head to determine the lockout duration.  Note that this is stored
            // on the server as a negative filetime.
            //
            DirectoryEntry domainNC = SDSUtils.BuildDirectoryEntry(
                                                    "LDAP://" + this.dnsHostName + "/" + this.defaultNamingContext,
                                                    this.credentials,
                                                    this.authTypes);

            // So we don't load every property
            domainNC.RefreshCache(s_lockoutDuration);

            if (domainNC.Properties["lockoutDuration"].Count > 0)
            {
                Debug.Assert(domainNC.Properties["lockoutDuration"].Count == 1);
                long negativeLockoutDuration = ADUtils.LargeIntToInt64((UnsafeNativeMethods.IADsLargeInteger)domainNC.Properties["lockoutDuration"][0]);
                Debug.Assert(negativeLockoutDuration <= 0);
                ulong lockoutDuration = (ulong)(-negativeLockoutDuration);
                this.lockoutDuration = lockoutDuration;

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "LoadComputerInfo: using lockout duration {0}", lockoutDuration);
            }

            //
            // User supplied name
            //
            UnsafeNativeMethods.Pathname pathCracker = new UnsafeNativeMethods.Pathname();
            UnsafeNativeMethods.IADsPathname pathName = (UnsafeNativeMethods.IADsPathname)pathCracker;

            pathName.Set(this.ctxBase.Path, 1 /* ADS_SETTYPE_FULL */);

            try
            {
                this.userSuppliedServerName = pathName.Retrieve(9 /*ADS_FORMAT_SERVER */);
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "LoadComputerInfo: using user-supplied name {0}", this.userSuppliedServerName);
            }
            catch (COMException e)
            {
                if (((uint)e.ErrorCode) == ((uint)0x80005000))  // E_ADS_BAD_PATHNAME
                {
                    // Serverless path
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "LoadComputerInfo: using empty string as user-supplied name");
                    this.userSuppliedServerName = "";
                }
                else
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Error,
                                            "ADStoreCtx",
                                            "LoadComputerInfo: caught COMException {0} {1} looking for user-supplied name",
                                            e.ErrorCode,
                                            e.Message);

                    throw;
                }
            }
        }

        internal override bool IsValidProperty(Principal p, string propertyName)
        {
            return ((Hashtable)s_propertyMappingTableByProperty[this.MappingTableIndex]).Contains(propertyName);
        }
    }
}
