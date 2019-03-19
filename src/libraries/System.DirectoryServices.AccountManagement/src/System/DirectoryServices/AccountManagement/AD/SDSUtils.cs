// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.DirectoryServices;
using System.Text;
using System.Net;

namespace System.DirectoryServices.AccountManagement
{
    internal class SDSUtils
    {
        // To stop the compiler from autogenerating a constructor for this class
        private SDSUtils() { }

        static internal Principal SearchResultToPrincipal(SearchResult sr, PrincipalContext owningContext, Type principalType)
        {
            Principal p;

            // Construct an appropriate Principal object.
            // Make* constructs a Principal that is marked persisted
            // and not loaded (p.unpersisted = false, p.loaded = false).

            // Since there should be no more multistore contexts, the owning context IS
            // the specific context     

            // If we know the type we should just construct it ourselves so that we don't need to incur the costs of reflection.
            // If this is an extension type then we must reflect teh constructor to create the object.

            if (typeof(UserPrincipal) == principalType)
            {
                p = UserPrincipal.MakeUser(owningContext);
            }
            else if (typeof(ComputerPrincipal) == principalType)
            {
                p = ComputerPrincipal.MakeComputer(owningContext);
            }
            else if (typeof(GroupPrincipal) == principalType)
            {
                p = GroupPrincipal.MakeGroup(owningContext);
            }
            else if (null == principalType ||
                         typeof(AuthenticablePrincipal) == principalType ||
                         typeof(Principal) == principalType)
            {
                if (SDSUtils.IsOfObjectClass(sr, "computer"))
                {
                    p = ComputerPrincipal.MakeComputer(owningContext);
                }
                else if (SDSUtils.IsOfObjectClass(sr, "user"))
                {
                    p = UserPrincipal.MakeUser(owningContext);
                }
                else if (SDSUtils.IsOfObjectClass(sr, "group"))
                {
                    p = GroupPrincipal.MakeGroup(owningContext);
                }
                else
                {
                    p = AuthenticablePrincipal.MakeAuthenticablePrincipal(owningContext);
                }
            }
            else
            {
                p = Principal.MakePrincipal(owningContext, principalType);
            }

            // The DirectoryEntry we're constructing the Principal from
            // will serve as the underlying object for that Principal.
            p.UnderlyingSearchObject = sr;

            // It's up to our caller to assign an appropriate StoreKey.
            // Caller must also populate the underlyingObject field if the P is to be used R/W
            return p;
        }
        // Used to implement StoreCtx.GetAsPrincipal for AD and SAM
        static internal Principal DirectoryEntryToPrincipal(DirectoryEntry de, PrincipalContext owningContext, Type principalType)
        {
            Principal p;

            // Construct an appropriate Principal object.
            // Make* constructs a Principal that is marked persisted
            // and not loaded (p.unpersisted = false, p.loaded = false).

            // Since there should be no more multistore contexts, the owning context IS
            // the specific context     

            if (typeof(UserPrincipal) == principalType)
            {
                p = UserPrincipal.MakeUser(owningContext);
            }
            else if (typeof(ComputerPrincipal) == principalType)
            {
                p = ComputerPrincipal.MakeComputer(owningContext);
            }
            else if (typeof(GroupPrincipal) == principalType)
            {
                p = GroupPrincipal.MakeGroup(owningContext);
            }
            else if (null == principalType ||
                         typeof(AuthenticablePrincipal) == principalType ||
                         typeof(Principal) == principalType)
            {
                if (SDSUtils.IsOfObjectClass(de, "computer"))
                {
                    p = ComputerPrincipal.MakeComputer(owningContext);
                }
                else if (SDSUtils.IsOfObjectClass(de, "user"))
                {
                    p = UserPrincipal.MakeUser(owningContext);
                }
                else if (SDSUtils.IsOfObjectClass(de, "group"))
                {
                    p = GroupPrincipal.MakeGroup(owningContext);
                }
                else
                {
                    p = AuthenticablePrincipal.MakeAuthenticablePrincipal(owningContext);
                }
            }
            else
            {
                p = Principal.MakePrincipal(owningContext, principalType);
            }
            // The DirectoryEntry we're constructing the Principal from
            // will serve as the underlying object for that Principal.
            p.UnderlyingObject = de;

            // It's up to our caller to assign an appropriate StoreKey.

            return p;
        }

        private static bool IsOfObjectClass(SearchResult sr, string className)
        {
            Debug.Assert(sr.Path.StartsWith("LDAP:", StringComparison.Ordinal) || sr.Path.StartsWith("GC:", StringComparison.Ordinal));
            return ADUtils.IsOfObjectClass(sr, className);
        }

        private static bool IsOfObjectClass(DirectoryEntry de, string className)
        {
            if (de.Path.StartsWith("WinNT:", StringComparison.Ordinal))
            {
                return SAMUtils.IsOfObjectClass(de, className);
            }
            else
            {
                Debug.Assert(de.Path.StartsWith("LDAP:", StringComparison.Ordinal));
                return ADUtils.IsOfObjectClass(de, className);
            }
        }
        static internal AuthenticationTypes MapOptionsToAuthTypes(ContextOptions options)
        {
            AuthenticationTypes authTypes = AuthenticationTypes.Secure;

            if ((options & ContextOptions.SimpleBind) != 0)
                authTypes = AuthenticationTypes.None;

            if ((options & ContextOptions.ServerBind) != 0)
                authTypes |= AuthenticationTypes.ServerBind;

            if ((options & ContextOptions.SecureSocketLayer) != 0)
                authTypes |= AuthenticationTypes.SecureSocketsLayer;

            if ((options & ContextOptions.Signing) != 0)
                authTypes |= AuthenticationTypes.Signing;

            if ((options & ContextOptions.Sealing) != 0)
                authTypes |= AuthenticationTypes.Sealing;

            return authTypes;
        }

        static internal void MoveDirectoryEntry(DirectoryEntry deToMove, DirectoryEntry newParent, string newName)
        {
            if (newName != null)
                deToMove.MoveTo(newParent, newName);
            else
                deToMove.MoveTo(newParent);
        }

        static internal void DeleteDirectoryEntry(DirectoryEntry deToDelete)
        {
            DirectoryEntry deParent = deToDelete.Parent;

            try
            {
                deParent.Children.Remove(deToDelete);
            }
            finally
            {
                deParent.Dispose();
            }
        }

        static internal void InsertPrincipal(
                                    Principal p,
                                    StoreCtx storeCtx,
                                    GroupMembershipUpdater updateGroupMembership,
                                    NetCred credentials,
                                    AuthenticationTypes authTypes,
                                    bool needToSetPassword)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SDSUtils", "Entering InsertPrincipal");

            Debug.Assert(storeCtx != null);
            Debug.Assert(storeCtx is ADStoreCtx || storeCtx is SAMStoreCtx);
            Debug.Assert(p != null);

            if ((!(p is UserPrincipal)) &&
                 (!(p is GroupPrincipal)) &&
                 (!(p is AuthenticablePrincipal)) &&
                 (!(p is ComputerPrincipal)))
            {
                // It's not a type of Principal that we support
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "SDSUtils", "InsertPrincipal: Bad principal type:" + p.GetType().ToString());

                throw new InvalidOperationException(SR.Format(SR.StoreCtxUnsupportedPrincipalTypeForSave, p.GetType()));
            }

            // Commit the properties
            SDSUtils.ApplyChangesToDirectory(
                                        p,
                                        storeCtx,
                                        updateGroupMembership,
                                        credentials,
                                        authTypes
                                        );

            // Handle any saved-off operations

            // For SAM, we set password elsewhere prior to creating the principal, so needToSetPassword == false
            // For AD, we have to set the password after creating the principal, so needToSetPassword == true
            if (needToSetPassword && p.GetChangeStatusForProperty(PropertyNames.PwdInfoPassword))
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SDSUtils", "InsertPrincipal: Setting password");

                // Only AuthenticablePrincipals can have PasswordInfo
                Debug.Assert(p is AuthenticablePrincipal);

                string password = (string)p.GetValueForProperty(PropertyNames.PwdInfoPassword);
                Debug.Assert(password != null); // if null, PasswordInfo should not have indicated it was changed

                storeCtx.SetPassword((AuthenticablePrincipal)p, password);
            }

            if (p.GetChangeStatusForProperty(PropertyNames.PwdInfoExpireImmediately))
            {
                // Only AuthenticablePrincipals can have PasswordInfo
                Debug.Assert(p is AuthenticablePrincipal);

                bool expireImmediately = (bool)p.GetValueForProperty(PropertyNames.PwdInfoExpireImmediately);

                if (expireImmediately)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SDSUtils", "InsertPrincipal: Setting pwd expired");

                    storeCtx.ExpirePassword((AuthenticablePrincipal)p);
                }
            }
        }

        internal delegate void GroupMembershipUpdater(Principal p, DirectoryEntry de, NetCred credentials, AuthenticationTypes authTypes);

        static internal void ApplyChangesToDirectory(
                                                Principal p,
                                                StoreCtx storeCtx,
                                                GroupMembershipUpdater updateGroupMembership,
                                                NetCred credentials,
                                                AuthenticationTypes authTypes)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SDSUtils", "Entering ApplyChangesToDirectory");
            Debug.Assert(storeCtx != null);
            Debug.Assert(storeCtx is ADStoreCtx || storeCtx is SAMStoreCtx || storeCtx is ADAMStoreCtx);
            Debug.Assert(p != null);
            Debug.Assert(updateGroupMembership != null);

            // Update the properties in the DirectoryEntry.  Note that this does NOT
            // update group membership.
            DirectoryEntry de = (DirectoryEntry)storeCtx.PushChangesToNative(p);
            Debug.Assert(de == p.UnderlyingObject);

            // Commit the property update
            try
            {
                de.CommitChanges();
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Error, "SDSUtils", "ApplyChangesToDirectory: caught COMException with message " + e.Message);

                throw (ExceptionHelper.GetExceptionFromCOMException(e));
            }

            if ((p is GroupPrincipal) && (p.GetChangeStatusForProperty(PropertyNames.GroupMembers)))
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SDSUtils", "ApplyChangesToDirectory: Updating group membership");

                // It's a group, and it's membership has changed.  Commit those membership changes.
                // Note that this is an immediate operation, because it goes through IADsGroup,
                // and does not require a call to de.CommitChanges().
                updateGroupMembership(p, de, credentials, authTypes);
            }
        }

        static internal void SetPassword(DirectoryEntry de, string newPassword)
        {
            Debug.Assert(newPassword != null);  // but it could be an empty string
            Debug.Assert(de != null);

            try
            {
                de.Invoke("SetPassword", new object[] { newPassword });
            }
            catch (System.Reflection.TargetInvocationException e)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Error, "SDSUtils", "SetPassword: caught TargetInvocationException with message " + e.Message);

                if (e.InnerException is System.Runtime.InteropServices.COMException)
                {
                    if (((System.Runtime.InteropServices.COMException)e.InnerException).ErrorCode == unchecked((int)ExceptionHelper.ERROR_HRESULT_CONSTRAINT_VIOLATION))
                    {
                        // We have a special case of constraint violation here.  We know this is a password failure to convert to this
                        // specialized type instead of the generic InvalidOperationException
                        throw (new PasswordException(((System.Runtime.InteropServices.COMException)e.InnerException).Message, (System.Runtime.InteropServices.COMException)e.InnerException));
                    }
                    else
                    {
                        throw (ExceptionHelper.GetExceptionFromCOMException((System.Runtime.InteropServices.COMException)e.InnerException));
                    }
                }

                // Unknown exception.  We don't want to suppress it.
                throw;
            }
        }

        static internal void ChangePassword(DirectoryEntry de, string oldPassword, string newPassword)
        {
            Debug.Assert(newPassword != null);  // but it could be an empty string
            Debug.Assert(oldPassword != null);  // but it could be an empty string

            Debug.Assert(de != null);

            try
            {
                de.Invoke("ChangePassword", new object[] { oldPassword, newPassword });
            }
            catch (System.Reflection.TargetInvocationException e)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Error, "SDSUtils", "ChangePassword: caught TargetInvocationException with message " + e.Message);

                if (e.InnerException is System.Runtime.InteropServices.COMException)
                {
                    if (((System.Runtime.InteropServices.COMException)e.InnerException).ErrorCode == unchecked((int)ExceptionHelper.ERROR_HRESULT_CONSTRAINT_VIOLATION))
                    {
                        // We have a special case of constraint violation here.  We know this is a password failure to convert to this
                        // specialized type instead of the generic InvalidOperationException
                        throw (new PasswordException(((System.Runtime.InteropServices.COMException)e.InnerException).Message, (System.Runtime.InteropServices.COMException)e.InnerException));
                    }
                    else
                    {
                        throw (ExceptionHelper.GetExceptionFromCOMException((System.Runtime.InteropServices.COMException)e.InnerException));
                    }
                }

                // Unknown exception.  We don't want to suppress it.
                throw;
            }
        }

        static internal DirectoryEntry BuildDirectoryEntry(string path, NetCred credentials, AuthenticationTypes authTypes)
        {
            DirectoryEntry de = new DirectoryEntry(path,
                                                                               credentials != null ? credentials.UserName : null,
                                                                               credentials != null ? credentials.Password : null,
                                                                               authTypes);

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SDSUtils", "BuildDirectoryEntry (1): built DE for  " + de.Path);

            return de;
        }

        static internal DirectoryEntry BuildDirectoryEntry(NetCred credentials, AuthenticationTypes authTypes)
        {
            DirectoryEntry de = new DirectoryEntry();

            de.Username = credentials != null ? credentials.UserName : null;
            de.Password = credentials != null ? credentials.Password : null;
            de.AuthenticationType = authTypes;

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SDSUtils", "BuildDirectoryEntry (2): built DE");

            return de;
        }

        static internal void WriteAttribute<T>(string dePath, string attribute, T value, NetCred credentials, AuthenticationTypes authTypes)
        {
            Debug.Assert(attribute != null && attribute.Length > 0);

            // Ideally, we'd just like to set the property in the principal's DirectoryEntry and write
            // the changes to the store.  However, there might be other changes in the DirectoryEntry,
            // and we don't want to write all of them to the store.  So we must make
            // a fresh DirectoryEntry and write using that.

            DirectoryEntry copyOfDe = null;

            try
            {
                copyOfDe = SDSUtils.BuildDirectoryEntry(dePath, credentials, authTypes);

                Debug.Assert(copyOfDe != null);

                // So we don't do a implicit GetInfo() and retrieve every attribute
                copyOfDe.RefreshCache(new string[] { attribute });

                copyOfDe.Properties[attribute].Value = value;
                copyOfDe.CommitChanges();
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                // ADSI threw an exception trying to write the change
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
            finally
            {
                if (copyOfDe != null)
                    copyOfDe.Dispose();
            }
        }

        static internal void WriteAttribute(string dePath, string attribute, int value, NetCred credentials, AuthenticationTypes authTypes)
        {
            GlobalDebug.WriteLineIf(
                        GlobalDebug.Info,
                        "SDSUtils",
                        "WriteAttribute: writing {0} to {1} on {2}",
                        value.ToString(CultureInfo.InvariantCulture),
                        attribute,
                        dePath);

            Debug.Assert(attribute != null && attribute.Length > 0);

            // Ideally, we'd just like to set the property in the principal's DirectoryEntry and write
            // the changes to the store.  However, there might be other changes in the DirectoryEntry,
            // and we don't want to write all of them to the store.  So we must make
            // a fresh DirectoryEntry and write using that.

            DirectoryEntry copyOfDe = null;

            try
            {
                copyOfDe = SDSUtils.BuildDirectoryEntry(dePath, credentials, authTypes);

                Debug.Assert(copyOfDe != null);

                // So we don't do a implicit GetInfo() and retrieve every attribute
                copyOfDe.RefreshCache(new string[] { attribute });

                copyOfDe.Properties[attribute].Value = value;
                copyOfDe.CommitChanges();
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                GlobalDebug.WriteLineIf(
                            GlobalDebug.Error,
                            "SDSUtils",
                            "WriteAttribute: caught exception with message '{0}' writing {1} to {2} on {3}",
                            e.Message,
                            value.ToString(CultureInfo.InvariantCulture),
                            attribute,
                            dePath);

                // ADSI threw an exception trying to write the change
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
            finally
            {
                if (copyOfDe != null)
                    copyOfDe.Dispose();
            }
        }

        //
        // S.DS (LDAP or WinNT) --> PAPI conversion routines
        //
        static internal void SingleScalarFromDirectoryEntry<T>(dSPropertyCollection properties, string suggestedProperty, Principal p, string propertyName)
        {
            if (properties[suggestedProperty].Count != 0 && properties[suggestedProperty][0] != null)
            {
                // We're intended to handle single-valued scalar properties
                Debug.Assert(properties[suggestedProperty].Count == 1);
                Debug.Assert(properties[suggestedProperty][0] is T);

                p.LoadValueIntoProperty(propertyName, (T)properties[suggestedProperty][0]);
            }
        }

        static internal void MultiScalarFromDirectoryEntry<T>(dSPropertyCollection properties, string suggestedProperty, Principal p, string propertyName)
        {
            dSPropertyValueCollection values = properties[suggestedProperty];

            List<T> list = new List<T>();

            foreach (object value in values)
            {
                Debug.Assert(value is T);

                list.Add((T)value);
            }

            p.LoadValueIntoProperty(propertyName, list);
        }

        static internal bool StatusFromAccountControl(int uacValue, string propertyName)
        {
            bool flag = false;

            switch (propertyName)
            {
                case PropertyNames.AuthenticablePrincipalEnabled:
                    // UF_ACCOUNTDISABLE
                    // Note that the logic is inverted on this one.  We expose "Enabled",
                    // but AD/SAM store it as "Disabled".
                    flag = ((uacValue & 0x2) == 0);
                    break;

                case PropertyNames.AcctInfoSmartcardRequired:
                    // UF_SMARTCARD_REQUIRED
                    flag = ((uacValue & 0x40000) != 0);
                    break;

                case PropertyNames.AcctInfoDelegationPermitted:
                    // UF_NOT_DELEGATED
                    // Note that the logic is inverted on this one.  That's because we expose
                    // "delegation allowed", but AD represents it as the inverse, "delegation NOT allowed"                        
                    flag = ((uacValue & 0x100000) == 0);
                    break;

                case PropertyNames.PwdInfoPasswordNotRequired:
                    // UF_PASSWD_NOTREQD
                    flag = ((uacValue & 0x0020) != 0);
                    break;

                case PropertyNames.PwdInfoPasswordNeverExpires:
                    // UF_DONT_EXPIRE_PASSWD
                    flag = ((uacValue & 0x10000) != 0);
                    break;

                // This bit doesn't work in userAccountControl
                case PropertyNames.PwdInfoCannotChangePassword:
                    // UF_PASSWD_CANT_CHANGE
                    flag = ((uacValue & 0x0040) != 0);
                    break;

                case PropertyNames.PwdInfoAllowReversiblePasswordEncryption:
                    // UF_ENCRYPTED_TEXT_PASSWORD_ALLOWED
                    flag = ((uacValue & 0x0080) != 0);
                    break;

                default:
                    Debug.Fail("SDSUtils.AccountControlFromDirectoryEntry: Fell off end looking for " + propertyName);
                    flag = false;
                    break;
            }

            return flag;
        }
        static internal void AccountControlFromDirectoryEntry(dSPropertyCollection properties, string suggestedProperty, Principal p, string propertyName, bool testCantChangePassword)
        {
            Debug.Assert(
                (!testCantChangePassword && (string.Equals(suggestedProperty, "userAccountControl", StringComparison.OrdinalIgnoreCase))) ||
                (testCantChangePassword && (string.Equals(suggestedProperty, "UserFlags", StringComparison.OrdinalIgnoreCase)))
                );

            Debug.Assert(!string.Equals(propertyName, PropertyNames.PwdInfoCannotChangePassword, StringComparison.OrdinalIgnoreCase) || testCantChangePassword);

            dSPropertyValueCollection values = properties[suggestedProperty];

            if (values.Count != 0)
            {
                Debug.Assert(values.Count == 1);
                Debug.Assert(values[0] is int);

                int uacValue = (int)values[0];
                bool flag;

                flag = StatusFromAccountControl(uacValue, propertyName);

                p.LoadValueIntoProperty(propertyName, flag);
            }
        }

        //
        // PAPI --> S.DS (LDAP or WinNT) conversion routines
        //

        static internal void MultiStringToDirectoryEntryConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedProperty)
        {
            PrincipalValueCollection<string> trackingList = (PrincipalValueCollection<string>)p.GetValueForProperty(propertyName);

            if (p.unpersisted && trackingList == null)
                return;

            List<string> insertedValues = trackingList.Inserted;
            List<string> removedValues = trackingList.Removed;
            List<Pair<string, string>> changedValues = trackingList.ChangedValues;

            PropertyValueCollection properties = de.Properties[suggestedProperty];

            // We test to make sure the change hasn't already been applied to the PropertyValueCollection,
            // because PushChangesToNative can be called multiple times prior to committing the changes and
            // we want to maintain idempotency
            foreach (string value in removedValues)
            {
                if (value != null && properties.Contains(value))
                    properties.Remove(value);
            }

            foreach (Pair<string, string> changedValue in changedValues)
            {
                // Remove the original value and add in the new value
                Debug.Assert(changedValue.Left != null);    // since it came from the system
                properties.Remove(changedValue.Left);

                if (changedValue.Right != null && !properties.Contains(changedValue.Right))
                    properties.Add(changedValue.Right);
            }

            foreach (string value in insertedValues)
            {
                if (value != null && !properties.Contains(value))
                    properties.Add(value);
            }
        }

        internal const int AD_DefaultUAC = (int)(0x200 | 0X20 | 0x2);  // UF_NORMAL_ACCOUNT  | UF_PASSWD_NOTREQD | UF_ACCOUNTDISABLE
        internal const int AD_DefaultUAC_Machine = (int)(0x1000 | 0X20 | 0x2);  // UF_WORKSTATION_TRUST_ACCOUNT | UF_PASSWD_NOTREQD | UF_ACCOUNTDISABLE
        internal const int SAM_DefaultUAC = (int)(0x200 | 0x1);        // UF_NORMAL_ACCOUNT | UF_SCRIPT

        static internal void AccountControlToDirectoryEntry(
                                        Principal p,
                                        string propertyName,
                                        DirectoryEntry de,
                                        string suggestedProperty,
                                        bool isSAM,
                                        bool isUnpersisted)
        {
            Debug.Assert(
                (!isSAM && (string.Equals(suggestedProperty, "userAccountControl", StringComparison.OrdinalIgnoreCase))) ||
                (isSAM && (string.Equals(suggestedProperty, "UserFlags", StringComparison.OrdinalIgnoreCase)))
                );

            bool flag = (bool)p.GetValueForProperty(propertyName);

            int uacValue = 0;

            // We want to get the current value, so we can flip the appropriate bit while leaving the other bits as-is.
            // If this is a to-be-inserted Principal, we should have already initialized the userAccountControl property
            if (de.Properties[suggestedProperty].Count > 0)
            {
                Debug.Assert(de.Properties[suggestedProperty].Count == 1);

                uacValue = (int)de.Properties[suggestedProperty][0];

                // When we write to userAccountControl, we must OR the new value with the value of msDS-User-Account-Control-Computed.
                // Otherwise we might mistakenly clear computed bits like UF_LOCKOUT.
                if (!isSAM && de.Properties["msDS-User-Account-Control-Computed"].Count > 0)
                {
                    Debug.Assert(de.Properties["msDS-User-Account-Control-Computed"].Count == 1);
                    uacValue = uacValue | (int)de.Properties["msDS-User-Account-Control-Computed"][0];
                }
            }
            else
            {
                // We don't have the userAccountControl property, this must be a persisted principal.  Perhaps we don't have access
                // to it.  In that case, we don't want to blindly overwrite whatever other bits might be there.            
                Debug.Assert(p.unpersisted == false);
                throw new PrincipalOperationException(
                                SR.ADStoreCtxUnableToReadExistingAccountControlFlagsForUpdate);
            }

            uint bitmask;

            // Get the right bitmask for the property
            switch (propertyName)
            {
                case PropertyNames.AuthenticablePrincipalEnabled:
                    if (!isUnpersisted || isSAM)
                    {
                        // UF_ACCOUNTDISABLE
                        // Note that the logic is inverted on this one.  We expose "Enabled",
                        // but AD/SAM store it as "Disabled".
                        bitmask = 0x2;
                        flag = !flag;
                    }
                    else
                    {
                        // We're writing to an unpersisted AD principal
                        Debug.Assert(!isSAM && isUnpersisted);

                        // Nothing to do here.  The ADStoreCtx will take care of enabling the
                        // principal after it's persisted.
                        bitmask = 0;
                    }
                    break;

                case PropertyNames.AcctInfoSmartcardRequired:
                    // UF_SMARTCARD_REQUIRED
                    bitmask = 0x40000;
                    break;

                case PropertyNames.AcctInfoDelegationPermitted:
                    // UF_NOT_DELEGATED
                    // Note that the logic is inverted on this one.  That's because we expose
                    // "delegation allowed", but AD represents it as the inverse, "delegation NOT allowed"                        
                    bitmask = 0x100000;
                    flag = !flag;
                    break;

                case PropertyNames.PwdInfoPasswordNotRequired:
                    // UF_PASSWD_NOTREQD
                    bitmask = 0x0020;
                    break;

                case PropertyNames.PwdInfoPasswordNeverExpires:
                    // UF_DONT_EXPIRE_PASSWD
                    bitmask = 0x10000;
                    break;

                case PropertyNames.PwdInfoAllowReversiblePasswordEncryption:
                    // UF_ENCRYPTED_TEXT_PASSWORD_ALLOWED
                    bitmask = 0x0080;
                    break;

                // This bit doesn't work in userAccountControl
                case PropertyNames.PwdInfoCannotChangePassword:
                    if (isSAM)
                    {
                        // UF_PASSWD_CANT_CHANGE
                        bitmask = 0x0040;
                        break;
                    }
                    else
                    {
                        Debug.Fail(
                            "SDSUtils.AccountControlToDirectoryEntry: At PwdInfoCannotChangePassword but isSAM==false, path=" + de.Path
                            );

                        goto default;
                    }

                default:
                    Debug.Fail("SDSUtils.AccountControlToDirectoryEntry: Fell off end looking for " + propertyName);
                    bitmask = 0;
                    break;
            }

            // Set/clear the "bitmask" bit in "uacValue", based on the value of "flag"
            if (flag)
                Utils.SetBit(ref uacValue, bitmask);
            else
                Utils.ClearBit(ref uacValue, bitmask);

            de.Properties[suggestedProperty].Value = uacValue;
        }

        static internal DirectorySearcher ConstructSearcher(DirectoryEntry de)
        {
            DirectorySearcher ds = new DirectorySearcher(de);
            ds.ClientTimeout = new TimeSpan(0, 0, 30);
            ds.PageSize = 256;
            return ds;
        }

        static internal bool IsObjectFromGC(string path)
        {
            return path.StartsWith("GC:", StringComparison.OrdinalIgnoreCase);
        }

        static internal string ConstructDnsDomainNameFromDn(string dn)
        {
            // Split the DN into its RDNs
            string[] ncComponents = dn.Split(new char[] { ',' });

            StringBuilder sb = new StringBuilder();

            // Reassemble the RDNs (minus the tag) into the DNS domain name
            foreach (string component in ncComponents)
            {
                if ((component.Length > 3) &&
                    string.Equals(component.Substring(0, 3), "DC=", StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append(component, 3, component.Length - 3);
                    sb.Append('.');
                }
            }

            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);

            return sb.ToString();
        }
    }
}
