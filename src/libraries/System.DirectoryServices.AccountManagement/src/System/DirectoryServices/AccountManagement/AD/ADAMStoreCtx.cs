// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Net;
using System.Security.Principal;

using System.DirectoryServices;
using System.Text;

namespace System.DirectoryServices.AccountManagement
{
    internal sealed partial class ADAMStoreCtx : ADStoreCtx
    {
        private const int mappingIndex = 1;
        private List<string> _cachedBindableObjectList;
        private string _cachedBindableObjectFilter;
        private readonly object _objectListLock = new object();

        public ADAMStoreCtx(DirectoryEntry ctxBase, bool ownCtxBase, string username, string password, string serverName, ContextOptions options) : base(ctxBase, ownCtxBase, username, password, options)
        {
            this.userSuppliedServerName = serverName;
        }

        //
        // Static constructor: used for initializing static tables
        //
#pragma warning disable CA1810 // Initialize reference type static fields inline
        static ADAMStoreCtx()
#pragma warning restore CA1810
        {
            LoadFilterMappingTable(mappingIndex, s_filterPropertiesTableRaw);
            LoadPropertyMappingTable(mappingIndex, s_propertyMappingTableRaw);

            NonPresentAttrDefaultStateMapping ??= new Dictionary<string, bool>();

            for (int i = 0; i < s_presenceStateTable.GetLength(0); i++)
            {
                string attributeName = s_presenceStateTable[i, 0] as string;
                string defaultState = s_presenceStateTable[i, 1] as string;
                NonPresentAttrDefaultStateMapping.Add(attributeName, (defaultState == "FALSE") ? false : true);
            }
        }

        protected override int MappingTableIndex
        {
            get
            {
                return mappingIndex;
            }
        }

        protected internal override void InitializeNewDirectoryOptions(DirectoryEntry newDeChild)
        {
            newDeChild.Options.PasswordPort = ctxBase.Options.PasswordPort;
        }

        protected override void SetAuthPrincipalEnableStatus(AuthenticablePrincipal ap, bool enable)
        {
            Debug.Assert(ap.fakePrincipal == false);

            bool acctDisabled;
            DirectoryEntry de = (DirectoryEntry)ap.UnderlyingObject;

            if (de.Properties["msDS-UserAccountDisabled"].Count > 0)
            {
                Debug.Assert(de.Properties["msDS-UserAccountDisabled"].Count == 1);

                acctDisabled = (bool)de.Properties["msDS-UserAccountDisabled"][0];
            }
            else
            {
                // Since we loaded the properties, we should have it.  Perhaps we don't have access
                // to it.  In that case, we don't want to blindly overwrite whatever other bits might be there.
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADAMStoreCtx", "SetAuthPrincipalEnableStatus: can't read userAccountControl");

                throw new PrincipalOperationException(
                            SR.ADStoreCtxUnableToReadExistingAccountControlFlagsToEnable);
            }

            if ((enable && acctDisabled) || (!enable && !acctDisabled))
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADAMStoreCtx", "SetAuthPrincipalEnableStatus: Enabling (old enabled ={0} new enabled= {1})", !acctDisabled, enable);

                WriteAttribute<bool>(ap, "msDS-UserAccountDisabled", !enable);
            }
        }

        // Must be called inside of lock(domainInfoLock)
        protected override void LoadDomainInfo()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "LoadComputerInfo");

            Debug.Assert(this.ctxBase != null);

            //
            // DNS Domain Name
            //
            this.dnsHostName = ADUtils.GetServerName(this.ctxBase);
            // Treat the user supplied server name as the domain and forest name...
            this.domainFlatName = userSuppliedServerName;
            this.forestDnsName = userSuppliedServerName;
            this.domainDnsName = userSuppliedServerName;

            //
            // Find the partition in which the supplied ctxBase belongs by comparing it with the list of partitions hosted by this
            // LDS (ADAM) instance.
            //
            using (DirectoryEntry rootDse = new DirectoryEntry("LDAP://" + this.userSuppliedServerName + "/rootDse", "", "", AuthenticationTypes.Anonymous))
            {
                string ctxBaseDN = (string)this.ctxBase.Properties["distinguishedName"][0];
                int maxMatchLength = -1;
                foreach (string partitionDN in rootDse.Properties["namingContexts"])
                {
                    if ((partitionDN.Length > maxMatchLength) && ctxBaseDN.EndsWith(partitionDN, StringComparison.OrdinalIgnoreCase))
                    {
                        maxMatchLength = partitionDN.Length;
                        this.contextBasePartitionDN = partitionDN;
                    }
                }
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

        internal override ResultSet GetGroupsMemberOfAZ(Principal p)
        {
            // Enforced by the methods that call us
            Debug.Assert(p.unpersisted == false);
            Debug.Assert(p is UserPrincipal);

            Debug.Assert(p.UnderlyingObject != null);

            DirectoryEntry principalDE = (DirectoryEntry)p.UnderlyingObject;

            string principalDN = (string)principalDE.Properties["distinguishedName"].Value;
            return (new TokenGroupSet(principalDN, this, true));
        }

        // modifies the connections settings for the upcoming password operation..
        // If Signing + Sealing are enabled on the connection a direct call to SetPassword will always fail for ADAM.
        // ADSI will first attempt to set the password over SSL which will fail because double encryption is not supported.
        // It will then try KetSetPassword and NetApi which will both always fail against ADAM.
        // We need to tell ADSI to send a clear text password which is not actually clear text because
        // the initial connection is encrypted with sign + seal.  Once this call is made the user needs to call CleanupAfterPasswordModification
        // To reset the options that were modified.
        private void SetupPasswordModification(AuthenticablePrincipal p)
        {
            DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;

            if (((this.contextOptions & ContextOptions.Signing) != 0) &&
                 ((this.contextOptions & ContextOptions.Sealing) != 0))
            {
                try
                {
                    de.Invoke("SetOption", new object[]{UnsafeNativeMethods.ADS_OPTION_ENUM.ADS_OPTION_PASSWORD_METHOD,
                                                                          UnsafeNativeMethods.ADS_PASSWORD_ENCODING_ENUM.ADS_PASSWORD_ENCODE_CLEAR});

                    de.Options.PasswordPort = p.Context.ServerInformation.portLDAP;
                }
                catch (System.Reflection.TargetInvocationException e)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Error, "ADAMStoreCtx", "SetupPasswordModification: caught TargetInvocationException with message " + e.Message);

                    if (e.InnerException is System.Runtime.InteropServices.COMException)
                    {
                        throw (ExceptionHelper.GetExceptionFromCOMException((System.Runtime.InteropServices.COMException)e.InnerException));
                    }

                    // Unknown exception.  We don't want to suppress it.
                    throw;
                }
            }
        }

        internal override void SetPassword(AuthenticablePrincipal p, string newPassword)
        {
            Debug.Assert(p.fakePrincipal == false);

            Debug.Assert(p != null);
            Debug.Assert(newPassword != null);  // but it could be an empty string

            DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;
            Debug.Assert(de != null);

            SetupPasswordModification(p);

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

            DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;
            Debug.Assert(de != null);

            SetupPasswordModification(p);

            SDSUtils.ChangePassword(de, oldPassword, newPassword);
        }

        //------------------------------------------------------------------------------------
        // Taking a server target and Auxiliary class name return
        // a list of all possible objectClasses that include that auxClass.  A search for object that have a specific
        // aux class cannot be done directly on the objects because static auxClasses to not appear in the
        // actual object.  This is done by
        // 1.  Searching the schema container for schema classes that include the aux class as a
        //      SystemAuxiliaryClass.  This covers StaticAuxClasses.
        // 2.  Add the aux class name as an additional returned objectClass to cover Dynamic AuxClasses.
        //------------------------------------------------------------------------------------
        private List<string> PopulatAuxObjectList(string auxClassName)
        {
            string SchemaNamingContext;
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADAMStoreCtx", "PopulatAuxObjectList Building object list");

            try
            {
                using (DirectoryEntry deRoot = new DirectoryEntry("LDAP://" + userSuppliedServerName + "/rootDSE", credentials?.UserName, credentials?.Password, authTypes))
                {
                    if (deRoot.Properties["schemaNamingContext"].Count == 0)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Error, "ADAMStoreCtx", "PopulatAuxObjectList Unable to read schemaNamingContrext from " + userSuppliedServerName);
                        throw new PrincipalOperationException(SR.ADAMStoreUnableToPopulateSchemaList);
                    }

                    SchemaNamingContext = (string)deRoot.Properties["schemaNamingContext"].Value;
                }

                using (DirectoryEntry deSCN = new DirectoryEntry("LDAP://" + userSuppliedServerName + "/" + SchemaNamingContext, credentials?.UserName, credentials?.Password, authTypes))
                {
                    using (DirectorySearcher dirSearcher = new DirectorySearcher(deSCN))
                    {
                        dirSearcher.Filter = "(&(objectClass=classSchema)(systemAuxiliaryClass=" + auxClassName + "))";
                        dirSearcher.PropertiesToLoad.Add("ldapDisplayName");

                        List<string> objectClasses = new List<string>();
                        using (SearchResultCollection searchResCollection = dirSearcher.FindAll())
                        {
                            foreach (SearchResult res in searchResCollection)
                            {
                                if (null == res.Properties["ldapDisplayName"])
                                {
                                    GlobalDebug.WriteLineIf(GlobalDebug.Error, "ADAMStoreCtx", "PopulatAuxObjectList Unable to read ldapDisplayName from " + SchemaNamingContext);
                                    throw new PrincipalOperationException(SR.ADAMStoreUnableToPopulateSchemaList);
                                }

                                objectClasses.Add(res.Properties["ldapDisplayName"][0].ToString());
                                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADAMStoreCtx", "PopulatAuxObjectList Adding " + res.Properties["ldapDisplayName"][0].ToString());
                            }
                        }

                        objectClasses.Add(auxClassName);
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADAMStoreCtx", "PopulatAuxObjectList Adding " + auxClassName);

                        return objectClasses;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Error, "ADAMStoreCtx", "PopulatAuxObjectList COM Exception");
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
        }

        protected override string GetObjectClassPortion(Type principalType)
        {
            if (principalType == typeof(AuthenticablePrincipal) || principalType == typeof(Principal))
            {
                lock (_objectListLock)
                {
                    if (null == _cachedBindableObjectList)
                    {
                        _cachedBindableObjectList = PopulatAuxObjectList("msDS-BindableObject");
                    }

                    if (null == _cachedBindableObjectFilter)
                    {
                        StringBuilder filter = new StringBuilder();

                        filter.Append("(&(|");
                        foreach (string objectClass in _cachedBindableObjectList)
                        {
                            filter.Append("(objectClass=");
                            filter.Append(objectClass);
                            filter.Append(')');
                        }

                        _cachedBindableObjectFilter = filter.ToString();
                    }

                    if (principalType == typeof(Principal))
                    {
                        // IF we are searching for a principal then we also have to add Group type into the filter...
                        return _cachedBindableObjectFilter + "(objectClass=group))";
                    }
                    else
                    {
                        return _cachedBindableObjectFilter + ")";
                    }
                }
            }
            else
            {
                return base.GetObjectClassPortion(principalType);
            }
        }

        /// <summary>
        /// This method sets the default user account control bits for the new principal
        /// being created in this account store.
        /// </summary>
        /// <param name="p"> Principal to set the user account control bits for </param>
        internal override void InitializeUserAccountControl(AuthenticablePrincipal p)
        {
            // In ADAM, there is no user account control property that needs to be initialized
            // so do nothing
        }

        //
        // Property mapping tables
        //

        //
        // This table maps properties where the non-presence of the property
        // indicates the state shown in the table.  When searching for these properties
        // If the state desired matches that default state then we also must search for
        // non-existence of the attribute.
        private static readonly object[,] s_presenceStateTable =
        {
                {"ms-DS-UserPasswordNotRequired", "FALSE" },
                {"msDS-UserDontExpirePassword", "FALSE" },
                {"ms-DS-UserEncryptedTextPasswordAllowed", "FALSE" }
        };

        // We only list properties we map in this table.  At run-time, if we detect they set a
        // property that's not listed here when writing to AD, we throw an exception.
        //
        // When updating this table, be sure to also update LoadDirectoryEntryAttributes() to load
        // in any newly-added attributes.
        private static readonly object[,] s_propertyMappingTableRaw =
        {
            // PropertyName                          AD property             Converter(LDAP->PAPI)                                    Converter(PAPI->LDAP)
            {PropertyNames.PrincipalDescription,     "description",          new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.PrincipalDisplayName,     "displayName",          new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.PrincipalDistinguishedName,  "distinguishedName",    new FromLdapConverterDelegate(StringFromLdapConverter), new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.PrincipalSid,  "objectSid",            new FromLdapConverterDelegate(SidFromLdapConverter),  null},
            {PropertyNames.PrincipalSamAccountName,  "name",       null, null},
            {PropertyNames.PrincipalUserPrincipalName,  "userPrincipalName",    new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.PrincipalGuid,  "objectGuid",           new FromLdapConverterDelegate(GuidFromLdapConverter),   null},
            {PropertyNames.PrincipalStructuralObjectClass,  "objectClass",           new FromLdapConverterDelegate(ObjectClassFromLdapConverter),   null},
            {PropertyNames.PrincipalName,  "name",           new FromLdapConverterDelegate(StringFromLdapConverter), new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.PrincipalExtensionCache,  null,  null, new ToLdapConverterDelegate(ExtensionCacheToLdapConverter)},

            {PropertyNames.AuthenticablePrincipalEnabled,      "msDS-UserAccountDisabled", new FromLdapConverterDelegate(AcctDisabledFromLdapConverter),  new ToLdapConverterDelegate(AcctDisabledToLdapConverter)},
            {PropertyNames.AuthenticablePrincipalCertificates, "userCertificate",    new FromLdapConverterDelegate(CertFromLdapConverter), new ToLdapConverterDelegate(CertToLdap)},

            {PropertyNames.GroupIsSecurityGroup,   "groupType", new FromLdapConverterDelegate(GroupTypeFromLdapConverter), new ToLdapConverterDelegate(GroupTypeToLdapConverter)},
            {PropertyNames.GroupGroupScope, "groupType", new FromLdapConverterDelegate(GroupTypeFromLdapConverter), new ToLdapConverterDelegate(GroupTypeToLdapConverter)},

            {PropertyNames.UserGivenName,             "givenName",        new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.UserMiddleName,            "middleName",       new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.UserSurname,               "sn",               new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.UserEmailAddress,          "mail",             new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.UserVoiceTelephoneNumber,  "telephoneNumber",  new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.UserEmployeeID,            "employeeID",       new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},

            {PropertyNames.ComputerServicePrincipalNames, "servicePrincipalName", new FromLdapConverterDelegate(MultiStringFromLdapConverter), new ToLdapConverterDelegate(MultiStringToLdapConverter)},

            {PropertyNames.AcctInfoAcctLockoutTime,       "lockoutTime",        new FromLdapConverterDelegate(GenericDateTimeFromLdapConverter), null},
            {PropertyNames.AcctInfoLastLogon,             "lastLogon",          new FromLdapConverterDelegate(LastLogonFromLdapConverter),       null},
            {PropertyNames.AcctInfoLastLogon,             "lastLogonTimestamp", new FromLdapConverterDelegate(LastLogonFromLdapConverter),       null},
            {PropertyNames.AcctInfoPermittedWorkstations, "userWorkstations",   null,     null},
            {PropertyNames.AcctInfoPermittedLogonTimes,   "logonHours",         null,          null},
            {PropertyNames.AcctInfoExpirationDate,        "accountExpires",     new FromLdapConverterDelegate(AcctExpirFromLdapConverter),       new ToLdapConverterDelegate(AcctExpirToLdapConverter)},
            {PropertyNames.AcctInfoSmartcardRequired,     "userAccountControl", null,             null},
            {PropertyNames.AcctInfoDelegationPermitted,   "userAccountControl", null,             null},
            {PropertyNames.AcctInfoBadLogonCount,         "badPwdCount",        new FromLdapConverterDelegate(IntFromLdapConverter),             null},
            {PropertyNames.AcctInfoHomeDirectory,         "homeDirectory",   null,     null},
            {PropertyNames.AcctInfoHomeDrive,             "homeDrive",   null,     null},
            {PropertyNames.AcctInfoScriptPath,            "scriptPath",   null,     null},

            {PropertyNames.PwdInfoLastPasswordSet,        "pwdLastSet",           new FromLdapConverterDelegate(GenericDateTimeFromLdapConverter),     null},
            {PropertyNames.PwdInfoLastBadPasswordAttempt, "badPasswordTime",      new FromLdapConverterDelegate(GenericDateTimeFromLdapConverter),     null},
            {PropertyNames.PwdInfoPasswordNotRequired,    "ms-DS-UserPasswordNotRequired",   new FromLdapConverterDelegate(BoolFromLdapConverter),                 new ToLdapConverterDelegate(BoolToLdapConverter)},
            {PropertyNames.PwdInfoPasswordNeverExpires,   "msDS-UserDontExpirePassword",   new FromLdapConverterDelegate(BoolFromLdapConverter),                 new ToLdapConverterDelegate(BoolToLdapConverter)},
            {PropertyNames.PwdInfoCannotChangePassword,   "ntSecurityDescriptor", null,     new ToLdapConverterDelegate(CannotChangePwdToLdapConverter)},
            {PropertyNames.PwdInfoAllowReversiblePasswordEncryption,     "ms-DS-UserEncryptedTextPasswordAllowed",    new FromLdapConverterDelegate(BoolFromLdapConverter), new ToLdapConverterDelegate(BoolToLdapConverter)}
        };

        //
        // Query tables
        //

        // We only list properties we support filtering on in this table.  At run-time, if we detect they set a
        // property that's not listed here, we throw an exception.
        private static readonly object[,] s_filterPropertiesTableRaw =
        {
            // QbeType                                          AD property             Converter
            {typeof(DescriptionFilter),                         "description",          new FilterConverterDelegate(StringConverter)},
            {typeof(DisplayNameFilter),                         "displayName",          new FilterConverterDelegate(StringConverter)},
            {typeof(IdentityClaimFilter),                       "",                     new FilterConverterDelegate(IdentityClaimConverter)},
            {typeof(DistinguishedNameFilter),                         "distinguishedName",          new FilterConverterDelegate(StringConverter)},
            {typeof(GuidFilter),                         "objectGuid",          new FilterConverterDelegate(GuidConverter)},
            {typeof(UserPrincipalNameFilter),                         "userPrincipalName",          new FilterConverterDelegate(StringConverter)},
            {typeof(StructuralObjectClassFilter),                         "objectClass",          new FilterConverterDelegate(StringConverter)},
            {typeof(NameFilter),                         "name",          new FilterConverterDelegate(StringConverter)},

            {typeof(CertificateFilter),                         "",                     new FilterConverterDelegate(CertificateConverter)},
            {typeof(AuthPrincEnabledFilter),                    "msDS-UserAccountDisabled",   new FilterConverterDelegate(AcctDisabledConverter)}, /*##*/
            {typeof(PermittedWorkstationFilter),                "userWorkstations",     new FilterConverterDelegate(StringConverter)},
            {typeof(PermittedLogonTimesFilter),                 "logonHours",           new FilterConverterDelegate(BinaryConverter)},
            {typeof(ExpirationDateFilter),                      "accountExpires",       new FilterConverterDelegate(ExpirationDateConverter)},
            {typeof(SmartcardLogonRequiredFilter),              "userAccountControl",   new FilterConverterDelegate(UserAccountControlConverter)}, /*##*/
            {typeof(DelegationPermittedFilter),                 "userAccountControl",   new FilterConverterDelegate(UserAccountControlConverter)}, /*##*/
            {typeof(HomeDirectoryFilter),                       "homeDirectory",        new FilterConverterDelegate(StringConverter)},
            {typeof(HomeDriveFilter),                           "homeDrive",            new FilterConverterDelegate(StringConverter)},
            {typeof(ScriptPathFilter),                          "scriptPath",           new FilterConverterDelegate(StringConverter)},
            {typeof(PasswordNotRequiredFilter),                 "ms-DS-UserPasswordNotRequired",   new FilterConverterDelegate(DefaultValueBoolConverter)}, /*##*/
            {typeof(PasswordNeverExpiresFilter),                "msDS-UserDontExpirePassword",   new FilterConverterDelegate(DefaultValueBoolConverter)}, /*##*/
            {typeof(CannotChangePasswordFilter),                "userAccountControl",   new FilterConverterDelegate(UserAccountControlConverter)}, /*##*/
            {typeof(AllowReversiblePasswordEncryptionFilter),   "ms-DS-UserEncryptedTextPasswordAllowed",   new FilterConverterDelegate(DefaultValueBoolConverter)}, /*##*/
            {typeof(GivenNameFilter),                           "givenName",            new FilterConverterDelegate(StringConverter)},
            {typeof(MiddleNameFilter),                          "middleName",           new FilterConverterDelegate(StringConverter)},
            {typeof(SurnameFilter),                             "sn",                   new FilterConverterDelegate(StringConverter)},
            {typeof(EmailAddressFilter),                        "mail",                 new FilterConverterDelegate(StringConverter)},
            {typeof(VoiceTelephoneNumberFilter),                "telephoneNumber",      new FilterConverterDelegate(StringConverter)},
            {typeof(EmployeeIDFilter),                          "employeeID",           new FilterConverterDelegate(StringConverter)},
            {typeof(GroupIsSecurityGroupFilter),                        "groupType",            new FilterConverterDelegate(GroupTypeConverter)},
            {typeof(GroupScopeFilter),                          "groupType",            new FilterConverterDelegate(GroupTypeConverter)},
            {typeof(ServicePrincipalNameFilter),                "servicePrincipalName", new FilterConverterDelegate(StringConverter)},
            {typeof(ExtensionCacheFilter),                null, new FilterConverterDelegate(ExtensionCacheConverter)},
            {typeof(BadPasswordAttemptFilter),                "badPasswordTime", new FilterConverterDelegate(DefaultValutMatchingDateTimeConverter)},
            {typeof(ExpiredAccountFilter),                "accountExpires", new FilterConverterDelegate(MatchingDateTimeConverter)},
            {typeof(LastLogonTimeFilter),                "lastLogonTimestamp", new FilterConverterDelegate(DefaultValutMatchingDateTimeConverter)},
            {typeof(LockoutTimeFilter),                "lockoutTime", new FilterConverterDelegate(DefaultValutMatchingDateTimeConverter)},
            {typeof(PasswordSetTimeFilter),                "pwdLastSet", new FilterConverterDelegate(DefaultValutMatchingDateTimeConverter)}
        };
    }
}
