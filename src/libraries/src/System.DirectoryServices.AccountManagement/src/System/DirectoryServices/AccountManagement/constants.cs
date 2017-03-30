// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;

namespace System.DirectoryServices.AccountManagement
{
    // This enum tracks the load state of our principal side data cache.  
    // NotSet = default value, 
    // Loaded = Value from store was set into cache date, Data in cache matches data in store.
    // Changed = User has modifed the cache value but is has not been pushed to the store yet
    internal enum LoadState
    {
        NotSet = 0,
        Loaded,
        Changed
    }

    // These are the default options used when a user does not specify a context option to connect to the store.
    internal static class DefaultContextOptions
    {
        internal static ContextOptions MachineDefaultContextOption = ContextOptions.Negotiate;
        internal static ContextOptions ADDefaultContextOption = ContextOptions.Negotiate | ContextOptions.Signing | ContextOptions.Sealing;
    }

    internal class LdapConstants
    {
        public static int LDAP_SSL_PORT = 636;
        public static int LDAP_PORT = 389;
        internal static DateTime defaultUtcTime = new DateTime(1601, 1, 1, 0, 0, 0);
        private LdapConstants() { }
    }
    // The string constants used internally to specify each property
    internal class PropertyNames
    {
        private PropertyNames() { }
        // Principal
        internal const string PrincipalDisplayName = "Principal.DisplayName";
        internal const string PrincipalDescription = "Principal.Description";
        internal const string PrincipalSamAccountName = "Principal.SamAccountName";
        internal const string PrincipalUserPrincipalName = "Principal.UserPrincipalName";
        internal const string PrincipalGuid = "Principal.Guid";
        internal const string PrincipalSid = "Principal.Sid";
        internal const string PrincipalIdentityClaims = "Principal.IdentityClaims";
        internal const string PrincipalDistinguishedName = "Principal.DistinguishedName";
        internal const string PrincipalStructuralObjectClass = "Principal.StructuralObjectClass";
        internal const string PrincipalName = "Principal.Name";
        internal const string PrincipalExtensionCache = "Principal.ExtensionCache";

        // AuthenticablePrincipal
        internal const string AuthenticablePrincipalEnabled = "AuthenticablePrincipal.Enabled";
        internal const string AuthenticablePrincipalCertificates = "AuthenticablePrincipal.Certificates";

        // Group
        internal const string GroupIsSecurityGroup = "GroupPrincipal.IsSecurityGroup";
        internal const string GroupGroupScope = "GroupPrincipal.GroupScope";
        internal const string GroupMembers = "GroupPrincipal.Members";

        // User
        internal const string UserGivenName = "UserPrincipal.GivenName";
        internal const string UserMiddleName = "UserPrincipal.MiddleName";
        internal const string UserSurname = "UserPrincipal.Surname";
        internal const string UserEmailAddress = "UserPrincipal.EmailAddress";
        internal const string UserVoiceTelephoneNumber = "UserPrincipal.VoiceTelephoneNumber";
        internal const string UserEmployeeID = "UserPrincipal.EmployeeId";

        // Computer
        internal const string ComputerServicePrincipalNames = "ComputerPrincipal.ServicePrincipalNames";

        // AccountInfo
        internal const string AcctInfoPrefix = "AuthenticablePrincipal.AccountInfo";
        internal const string AcctInfoAcctLockoutTime = "AuthenticablePrincipal.AccountInfo.AccountLockoutTime";
        internal const string AcctInfoLastLogon = "AuthenticablePrincipal.AccountInfo.LastLogon";
        internal const string AcctInfoPermittedWorkstations = "AuthenticablePrincipal.AccountInfo.PermittedWorkstations";
        internal const string AcctInfoPermittedLogonTimes = "AuthenticablePrincipal.AccountInfo.PermittedLogonTimes";
        internal const string AcctInfoExpirationDate = "AuthenticablePrincipal.AccountInfo.AccountExpirationDate";
        internal const string AcctInfoSmartcardRequired = "AuthenticablePrincipal.AccountInfo.SmartcardLogonRequired";
        internal const string AcctInfoDelegationPermitted = "AuthenticablePrincipal.AccountInfo.DelegationPermitted";
        internal const string AcctInfoBadLogonCount = "AuthenticablePrincipal.AccountInfo.BadLogonCount";
        internal const string AcctInfoHomeDirectory = "AuthenticablePrincipal.AccountInfo.HomeDirectory";
        internal const string AcctInfoHomeDrive = "AuthenticablePrincipal.AccountInfo.HomeDrive";
        internal const string AcctInfoScriptPath = "AuthenticablePrincipal.AccountInfo.ScriptPath";
        // This property is not publicly exposed but is used be a ReadOnlySearchFilter.
        internal const string AcctInfoExpiredAccount = "AuthenticablePrincipal.AccountInfoExpired";

        // PasswordInfo
        internal const string PwdInfoPrefix = "AuthenticablePrincipal.PasswordInfo";
        internal const string PwdInfoLastPasswordSet = "AuthenticablePrincipal.PasswordInfo.LastPasswordSet";
        internal const string PwdInfoLastBadPasswordAttempt = "AuthenticablePrincipal.PasswordInfo.LastBadPasswordAttempt";
        internal const string PwdInfoPasswordNotRequired = "AuthenticablePrincipal.PasswordInfo.PasswordNotRequired";
        internal const string PwdInfoPasswordNeverExpires = "AuthenticablePrincipal.PasswordInfo.PasswordNeverExpires";
        internal const string PwdInfoCannotChangePassword = "AuthenticablePrincipal.PasswordInfo.UserCannotChangePassword";
        internal const string PwdInfoAllowReversiblePasswordEncryption = "AuthenticablePrincipal.PasswordInfo.AllowReversiblePasswordEncryption";

        // these two are not publicly exposed properties, but are used internally to track ResetPassword/ExpirePasswordNow
        // operations against unpersisted principals, so that they can be performed once the principal has been Saved
        internal const string PwdInfoPassword = "AuthenticablePrincipal.PasswordInfo.Password";
        internal const string PwdInfoExpireImmediately = "AuthenticablePrincipal.PasswordInfo.ExpireImmediately";
    }

    // Given an internal property name (from PropertyNames), returns the external form of the name for use in error-reporting
    internal class PropertyNamesExternal
    {
        private PropertyNamesExternal() { }

        private static int s_acctInfoPrefixLength = PropertyNames.AcctInfoPrefix.Length;
        private static int s_pwdInfoPrefixLength = PropertyNames.PwdInfoPrefix.Length;

        internal static string GetExternalForm(string propertyName)
        {
            if (propertyName.StartsWith(PropertyNames.AcctInfoPrefix, StringComparison.Ordinal))
            {
                return "AuthenticablePrincipal" + propertyName.Substring(s_acctInfoPrefixLength);
            }
            else if (propertyName.StartsWith(PropertyNames.PwdInfoPrefix, StringComparison.Ordinal))
            {
                return "AuthenticablePrincipal" + propertyName.Substring(s_pwdInfoPrefixLength);
            }
            else
            {
                return propertyName;
            }
        }
    }

    // The list of properties considered referential (they refer to or contain Principal objects)
    //
    // At present, referential properties are the following:
    //
    //  Group.Members
    //
    internal class ReferentialProperties
    {
        private ReferentialProperties() { }

        // Maps from Type of the Principal object --> ArrayList of the object's referential property names
        // (expressed as strings from the PropertyNames class)
        internal static readonly Hashtable Properties;

        static ReferentialProperties()
        {
            Properties = new Hashtable();

            // Referential properties for groups
            ArrayList groupList = new ArrayList(1);
            groupList.Add(PropertyNames.GroupMembers);

            Properties[typeof(GroupPrincipal)] = groupList;

            // Referential properties for users
            // None at this time.

            // Referential properties for computers
            // None at this time.
        }
    }
}
