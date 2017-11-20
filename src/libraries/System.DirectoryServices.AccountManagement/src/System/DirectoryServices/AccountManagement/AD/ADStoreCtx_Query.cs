// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Globalization;
using System.Security.Principal;
using System.DirectoryServices;
using System.Collections.Specialized;

namespace System.DirectoryServices.AccountManagement
{
    internal partial class ADStoreCtx : StoreCtx
    {
        //
        // Query operations
        //

        // Returns true if this store has native support for search (and thus a wormhole).
        // Returns true for everything but SAM (both reg-SAM and MSAM).
        internal override bool SupportsSearchNatively { get { return true; } }

        // Returns a type indicating the type of object that would be returned as the wormhole for the specified
        // PrincipalSearcher.
        internal override Type SearcherNativeType() { return typeof(DirectorySearcher); }

        private void BuildExtensionPropertyList(Hashtable propertyList, Type p)
        {
            System.Reflection.PropertyInfo[] propertyInfoList = p.GetProperties();

            foreach (System.Reflection.PropertyInfo pInfo in propertyInfoList)
            {
                DirectoryPropertyAttribute[] pAttributeList = (DirectoryPropertyAttribute[])(pInfo.GetCustomAttributes(typeof(DirectoryPropertyAttribute), true));
                foreach (DirectoryPropertyAttribute pAttribute in pAttributeList)
                {
                    if (!propertyList.Contains(pAttribute.SchemaAttributeName))
                        propertyList.Add(pAttribute.SchemaAttributeName, pAttribute.SchemaAttributeName);
                }
            }
        }

        protected void BuildPropertySet(Type p, StringCollection propertySet)
        {
            if (TypeToLdapPropListMap[this.MappingTableIndex].ContainsKey(p))
            {
                Debug.Assert(TypeToLdapPropListMap[this.MappingTableIndex].ContainsKey(p));
                string[] props = new string[TypeToLdapPropListMap[this.MappingTableIndex][p].Count];
                TypeToLdapPropListMap[this.MappingTableIndex][p].CopyTo(props, 0);
                propertySet.AddRange(props);
            }
            else
            {
                Type baseType;

                if (p.IsSubclassOf(typeof(UserPrincipal)))
                {
                    baseType = typeof(UserPrincipal);
                }
                else if (p.IsSubclassOf(typeof(GroupPrincipal)))
                {
                    baseType = typeof(GroupPrincipal);
                }
                else if (p.IsSubclassOf(typeof(ComputerPrincipal)))
                {
                    baseType = typeof(ComputerPrincipal);
                }
                else if (p.IsSubclassOf(typeof(AuthenticablePrincipal)))
                {
                    baseType = typeof(AuthenticablePrincipal);
                }
                else
                {
                    baseType = typeof(Principal);
                }

                Hashtable propertyList = new Hashtable();

                // Load the properties for the base types...
                foreach (string s in TypeToLdapPropListMap[this.MappingTableIndex][baseType])
                {
                    if (!propertyList.Contains(s))
                    {
                        propertyList.Add(s, s);
                    }
                }

                // Reflect the properties off the extension class and add them to the list.                
                BuildExtensionPropertyList(propertyList, p);

                foreach (string property in propertyList.Values)
                {
                    propertySet.Add(property);
                }

                // Cache the list for this property type so we don't need to reflect again in the future.                                
                this.AddPropertySetToTypePropListMap(p, propertySet);
            }
        }

        // Pushes the query represented by the QBE filter into the PrincipalSearcher's underlying native
        // searcher object (creating a fresh native searcher and assigning it to the PrincipalSearcher if one
        // doesn't already exist) and returns the native searcher.
        // If the PrincipalSearcher does not have a query filter set (PrincipalSearcher.QueryFilter == null), 
        // produces a query that will match all principals in the store.
        //
        // For stores which don't have a native searcher (SAM), the StoreCtx
        // is free to create any type of object it chooses to use as its internal representation of the query.
        // 
        // Also adds in any clauses to the searcher to ensure that only principals, not mere
        // contacts, are retrieved from the store.
        internal override object PushFilterToNativeSearcher(PrincipalSearcher ps)
        {
            // This is the first time we're being called on this principal.  Create a fresh searcher.
            if (ps.UnderlyingSearcher == null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "PushFilterToNativeSearcher: creating fresh DirectorySearcher");

                ps.UnderlyingSearcher = new DirectorySearcher(this.ctxBase);
                ((DirectorySearcher)ps.UnderlyingSearcher).PageSize = ps.PageSize;
                ((DirectorySearcher)ps.UnderlyingSearcher).ServerTimeLimit = new TimeSpan(0, 0, 30);  // 30 seconds
            }

            DirectorySearcher ds = (DirectorySearcher)ps.UnderlyingSearcher;

            Principal qbeFilter = ps.QueryFilter;

            StringBuilder ldapFilter = new StringBuilder();

            if (qbeFilter == null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "PushFilterToNativeSearcher: no qbeFilter specified");

                // No filter specified.  Search for all principals (all users, computers, groups).
                ldapFilter.Append("(|(objectClass=user)(objectClass=computer)(objectClass=group))");
            }
            else
            {
                //
                // Start by appending the appropriate objectClass given the Principal type
                //
                ldapFilter.Append(GetObjectClassPortion(qbeFilter.GetType()));

                //
                // Next, fill in the properties (if any)
                //
                QbeFilterDescription filters = BuildQbeFilterDescription(qbeFilter);

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "PushFilterToNativeSearcher: using {0} filters", filters.FiltersToApply.Count);

                Hashtable filterTable = (Hashtable)s_filterPropertiesTable[this.MappingTableIndex];

                foreach (FilterBase filter in filters.FiltersToApply)
                {
                    FilterPropertyTableEntry entry = (FilterPropertyTableEntry)filterTable[filter.GetType()];

                    if (entry == null)
                    {
                        // Must be a property we don't support
                        throw new InvalidOperationException(
                                    String.Format(
                                        CultureInfo.CurrentCulture,
                                        SR.StoreCtxUnsupportedPropertyForQuery,
                                        PropertyNamesExternal.GetExternalForm(filter.PropertyName)));
                    }

                    ldapFilter.Append(entry.converter(filter, entry.suggestedADPropertyName));
                }

                //
                // Wrap off the filter
                //
                ldapFilter.Append(")");
            }

            // We don't need any attributes returned, since we're just going to get a DirectoryEntry
            // for the result.  Per RFC 2251, OID 1.1 == no attributes.
            //ds.PropertiesToLoad.Add("1.1");
            BuildPropertySet(qbeFilter.GetType(), ds.PropertiesToLoad);

            ds.Filter = ldapFilter.ToString();
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "PushFilterToNativeSearcher: using LDAP filter {0}", ds.Filter);

            return ds;
        }

        virtual protected string GetObjectClassPortion(Type principalType)
        {
            string ldapFilter;

            if (principalType == typeof(UserPrincipal))
                ldapFilter = "(&(objectCategory=user)(objectClass=user)";   // objCat because we don't want to match on computer accounts
            else if (principalType == typeof(GroupPrincipal))
                ldapFilter = "(&(objectClass=group)";
            else if (principalType == typeof(ComputerPrincipal))
                ldapFilter = "(&(objectClass=computer)";
            else if (principalType == typeof(Principal))
                ldapFilter = "(&(|(objectClass=user)(objectClass=group))";
            else if (principalType == typeof(AuthenticablePrincipal))
                ldapFilter = "(&(objectClass=user)";
            else
            {
                string objClass = ExtensionHelper.ReadStructuralObjectClass(principalType);
                if (null == objClass)
                {
                    Debug.Fail("ADStoreCtx.GetObjectClassPortion: fell off end looking for " + principalType.ToString());
                    throw new InvalidOperationException(
                                    String.Format(CultureInfo.CurrentCulture, SR.StoreCtxUnsupportedPrincipalTypeForQuery, principalType.ToString()));
                }
                StringBuilder SB = new StringBuilder();
                SB.Append("(&(objectClass=");
                SB.Append(objClass);
                SB.Append(")");
                ldapFilter = SB.ToString();
            }

            return ldapFilter;
        }

        // The core query operation.
        // Given a PrincipalSearcher containg a query filter, transforms it into the store schema 
        // and performs the query to get a collection of matching native objects (up to a maximum of sizeLimit,
        // or uses the sizelimit already set on the DirectorySearcher if sizeLimit == -1). 
        // If the PrincipalSearcher does not have a query filter (PrincipalSearcher.QueryFilter == null), 
        // matches all principals in the store.
        //
        // The collection may not be complete, i.e., paging - the returned ResultSet will automatically
        // page in additional results as needed.
        internal override ResultSet Query(PrincipalSearcher ps, int sizeLimit)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "Query");

            try
            {
                // Set up the DirectorySearcher
                DirectorySearcher ds = (DirectorySearcher)PushFilterToNativeSearcher(ps);
                int oldSizeLimit = ds.SizeLimit;

                Debug.Assert(sizeLimit >= -1);

                if (sizeLimit != -1)
                    ds.SizeLimit = sizeLimit;

                // Perform the actual search
                SearchResultCollection src = ds.FindAll();
                Debug.Assert(src != null);

                // Create a ResultSet for the search results
                ADEntriesSet resultSet = new ADEntriesSet(src, this, ps.QueryFilter.GetType());

                ds.SizeLimit = oldSizeLimit;

                return resultSet;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
        }

        //
        // Query tables
        //

        // We only list properties we support filtering on in this table.  At run-time, if we detect they set a
        // property that's not listed here, we throw an exception.
        private static object[,] s_filterPropertiesTableRaw =
        {
            // QbeType                                          AD property             Converter
            {typeof(DescriptionFilter),                         "description",          new FilterConverterDelegate(StringConverter)},
            {typeof(DisplayNameFilter),                         "displayName",          new FilterConverterDelegate(StringConverter)},
            {typeof(IdentityClaimFilter),                       "",                     new FilterConverterDelegate(IdentityClaimConverter)},
            {typeof(SamAccountNameFilter),       "sAMAccountName",          new FilterConverterDelegate(StringConverter)},
            {typeof(DistinguishedNameFilter),                         "distinguishedName",          new FilterConverterDelegate(StringConverter)},
            {typeof(GuidFilter),                         "objectGuid",          new FilterConverterDelegate(GuidConverter)},
            {typeof(UserPrincipalNameFilter),                         "userPrincipalName",          new FilterConverterDelegate(StringConverter)},
            {typeof(StructuralObjectClassFilter),                         "objectClass",          new FilterConverterDelegate(StringConverter)},
            {typeof(NameFilter),                         "name",          new FilterConverterDelegate(StringConverter)},
            {typeof(CertificateFilter),                         "",                     new FilterConverterDelegate(CertificateConverter)},
            {typeof(AuthPrincEnabledFilter),                    "userAccountControl",   new FilterConverterDelegate(UserAccountControlConverter)},
            {typeof(PermittedWorkstationFilter),                "userWorkstations",     new FilterConverterDelegate(CommaStringConverter)},
            {typeof(PermittedLogonTimesFilter),                 "logonHours",           new FilterConverterDelegate(BinaryConverter)},
            {typeof(ExpirationDateFilter),                      "accountExpires",       new FilterConverterDelegate(ExpirationDateConverter)},
            {typeof(SmartcardLogonRequiredFilter),              "userAccountControl",   new FilterConverterDelegate(UserAccountControlConverter)},
            {typeof(DelegationPermittedFilter),                 "userAccountControl",   new FilterConverterDelegate(UserAccountControlConverter)},
            {typeof(HomeDirectoryFilter),                       "homeDirectory",        new FilterConverterDelegate(StringConverter)},
            {typeof(HomeDriveFilter),                           "homeDrive",            new FilterConverterDelegate(StringConverter)},
            {typeof(ScriptPathFilter),                          "scriptPath",           new FilterConverterDelegate(StringConverter)},
            {typeof(PasswordNotRequiredFilter),                 "userAccountControl",   new FilterConverterDelegate(UserAccountControlConverter)},
            {typeof(PasswordNeverExpiresFilter),                "userAccountControl",   new FilterConverterDelegate(UserAccountControlConverter)},
            {typeof(CannotChangePasswordFilter),                "userAccountControl",   new FilterConverterDelegate(UserAccountControlConverter)},
            {typeof(AllowReversiblePasswordEncryptionFilter),   "userAccountControl",   new FilterConverterDelegate(UserAccountControlConverter)},
            {typeof(GivenNameFilter),                           "givenName",            new FilterConverterDelegate(StringConverter)},
            {typeof(MiddleNameFilter),                          "middleName",           new FilterConverterDelegate(StringConverter)},
            {typeof(SurnameFilter),                             "sn",                   new FilterConverterDelegate(StringConverter)},
            {typeof(EmailAddressFilter),                        "mail",                 new FilterConverterDelegate(StringConverter)},
            {typeof(VoiceTelephoneNumberFilter),                "telephoneNumber",      new FilterConverterDelegate(StringConverter)},
            {typeof(EmployeeIDFilter),                          "employeeID",           new FilterConverterDelegate(StringConverter)},
            {typeof(GroupIsSecurityGroupFilter),                        "groupType",            new FilterConverterDelegate(GroupTypeConverter)},
            {typeof(GroupScopeFilter),                          "groupType",            new FilterConverterDelegate(GroupTypeConverter)},
            {typeof(ServicePrincipalNameFilter),                "servicePrincipalName",new FilterConverterDelegate(StringConverter)},
            {typeof(ExtensionCacheFilter),                null ,new FilterConverterDelegate(ExtensionCacheConverter)},
            {typeof(BadPasswordAttemptFilter),                "badPasswordTime",new FilterConverterDelegate(DefaultValutMatchingDateTimeConverter)},
            {typeof(ExpiredAccountFilter),                "accountExpires",new FilterConverterDelegate(MatchingDateTimeConverter)},
            {typeof(LastLogonTimeFilter),                "lastLogon",new FilterConverterDelegate(LastLogonConverter)},
            {typeof(LockoutTimeFilter),                "lockoutTime",new FilterConverterDelegate(MatchingDateTimeConverter)},
            {typeof(PasswordSetTimeFilter),                "pwdLastSet",new FilterConverterDelegate(DefaultValutMatchingDateTimeConverter)},
            {typeof(BadLogonCountFilter),                "badPwdCount",new FilterConverterDelegate(MatchingIntConverter)}
        };

        private static Hashtable s_filterPropertiesTable = null;

        private class FilterPropertyTableEntry
        {
            internal string suggestedADPropertyName;
            internal FilterConverterDelegate converter;
        }

        //
        // Conversion routines
        //

        // returns LDAP filter clause, e.g., "(description=foo*")
        protected delegate string FilterConverterDelegate(FilterBase filter, string suggestedAdProperty);

        protected static string StringConverter(FilterBase filter, string suggestedAdProperty)
        {
            StringBuilder sb = new StringBuilder();

            if (filter.Value != null)
            {
                sb.Append("(");
                sb.Append(suggestedAdProperty);
                sb.Append("=");
                sb.Append(ADUtils.PAPIQueryToLdapQueryString((string)filter.Value));

                sb.Append(")");
            }
            else
            {
                sb.Append("(!(");
                sb.Append(suggestedAdProperty);
                sb.Append("=*))");
            }

            return sb.ToString();
        }

        protected static string AcctDisabledConverter(FilterBase filter, string suggestedAdProperty)
        {
            // Principal property is AccountEnabled  where TRUE = enabled FALSE = disabled.  In ADAM
            // this is stored as accountDisabled where TRUE = disabled and FALSE = enabled so here we need to revese the value.
            StringBuilder sb = new StringBuilder();

            if (filter.Value != null)
            {
                sb.Append("(");
                sb.Append(suggestedAdProperty);
                sb.Append("=");
                sb.Append(!(bool)filter.Value ? "TRUE" : "FALSE");
                sb.Append(")");
            }
            else
            {
                sb.Append("(!(");
                sb.Append(suggestedAdProperty);
                sb.Append("=*))");
            }

            return sb.ToString();
        }

        // Use this function when searching for an attribute where the absence of the attribute = a default setting. 
        // i.e.  ms-DS-UserPasswordNotRequired in ADAM where non existence equals false.
        protected static string DefaultValueBoolConverter(FilterBase filter, string suggestedAdProperty)
        {
            Debug.Assert(NonPresentAttrDefaultStateMapping != null);
            Debug.Assert(NonPresentAttrDefaultStateMapping.ContainsKey(suggestedAdProperty));

            StringBuilder sb = new StringBuilder();

            if (filter.Value != null)
            {
                bool defaultState = NonPresentAttrDefaultStateMapping[suggestedAdProperty];

                if (defaultState == (bool)filter.Value)
                {
                    sb.Append("(|(!(");
                    sb.Append(suggestedAdProperty);
                    sb.Append("=*)(");
                    sb.Append(suggestedAdProperty);
                    sb.Append("=");
                    sb.Append((bool)filter.Value ? "TRUE" : "FALSE");
                    sb.Append(")))");
                }
                else
                {
                    sb.Append("(");
                    sb.Append(suggestedAdProperty);
                    sb.Append("=");
                    sb.Append((bool)filter.Value ? "TRUE" : "FALSE");
                    sb.Append(")");
                }
            }
            else
            {
                sb.Append("(!(");
                sb.Append(suggestedAdProperty);
                sb.Append("=*))");
            }

            return sb.ToString();
        }
        /*** If standard bool conversion is needed uncomment this function        
                protected static string BoolConverter(FilterBase filter, string suggestedAdProperty)
                {
                    StringBuilder sb = new StringBuilder();

                    if (filter.Value != null)
                    {
                        sb.Append("(");
                        sb.Append(suggestedAdProperty);
                        sb.Append("=");
                        sb.Append( (bool)filter.Value ? "TRUE" : "FALSE" );                
                        sb.Append(")");
                    }
                    else
                    {
                        sb.Append("(!(");
                        sb.Append(suggestedAdProperty);
                        sb.Append("=*))");                
                    }

                    return sb.ToString();
                }
        *****/
        protected static string CommaStringConverter(FilterBase filter, string suggestedAdProperty)
        {
            StringBuilder sb = new StringBuilder();

            if (filter.Value != null)
            {
                sb.Append("(");
                sb.Append(suggestedAdProperty);
                sb.Append("=*");
                sb.Append(ADUtils.PAPIQueryToLdapQueryString((string)filter.Value));
                sb.Append("*");
                sb.Append(")");
            }
            else
            {
                sb.Append("(!(");
                sb.Append(suggestedAdProperty);
                sb.Append("=*))");
            }

            return sb.ToString();
        }

        protected static bool IdentityClaimToFilter(string identity, string identityFormat, ref String filter, bool throwOnFail)
        {
            if (identity == null)
                identity = "";

            StringBuilder sb = new StringBuilder();

            switch (identityFormat)
            {
                case UrnScheme.GuidScheme:

                    // Transform from hex string ("1AFF") to LDAP hex string ("\1A\FF")
                    // The string passed is the string format of a GUID.  We neeed to convert it into the ldap hex string
                    // to build a query
                    Guid g;

                    try
                    {
                        g = new Guid(identity);
                    }
                    catch (FormatException e)
                    {
                        if (throwOnFail)
                            // For now throw an exception to let the caller know the type was invalid.
                            throw new ArgumentException(e.Message, e);
                        else
                            return false;
                    }

                    Byte[] gByte = g.ToByteArray();

                    StringBuilder stringguid = new StringBuilder();

                    foreach (byte b in gByte)
                    {
                        stringguid.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                    }

                    string ldapHexGuid = ADUtils.HexStringToLdapHexString(stringguid.ToString());

                    if (ldapHexGuid == null)
                    {
                        if (throwOnFail)
                            throw new ArgumentException(SR.StoreCtxGuidIdentityClaimBadFormat);
                        else
                            return false;
                    }

                    sb.Append("(objectGuid=");

                    sb.Append(ldapHexGuid);

                    sb.Append(")");
                    break;

                case UrnScheme.DistinguishedNameScheme:
                    sb.Append("(distinguishedName=");
                    sb.Append(ADUtils.EscapeRFC2254SpecialChars(identity));
                    sb.Append(")");
                    break;

                case UrnScheme.SidScheme:

                    if (false == SecurityIdentityClaimConverterHelper(identity, false, sb, throwOnFail))
                    {
                        return false;
                    }

                    break;

                case UrnScheme.SamAccountScheme:

                    int index = identity.IndexOf('\\');

                    if (index == identity.Length - 1)
                        if (throwOnFail)
                            throw new ArgumentException(SR.StoreCtxNT4IdentityClaimWrongForm);
                        else
                            return false;

                    string samAccountName = (index != -1) ? identity.Substring(index + 1) :    // +1 to skip the '/'
                                                            identity;

                    sb.Append("(samAccountName=");
                    sb.Append(ADUtils.EscapeRFC2254SpecialChars(samAccountName));
                    sb.Append(")");
                    break;

                case UrnScheme.NameScheme:
                    sb.Append("(name=");
                    sb.Append(ADUtils.EscapeRFC2254SpecialChars(identity));
                    sb.Append(")");
                    break;

                case UrnScheme.UpnScheme:
                    sb.Append("(userPrincipalName=");
                    sb.Append(ADUtils.EscapeRFC2254SpecialChars(identity));
                    sb.Append(")");
                    break;

                default:
                    if (throwOnFail)
                        throw new ArgumentException(SR.StoreCtxUnsupportedIdentityClaimForQuery);
                    else
                        return false;
            }

            filter = sb.ToString();
            return true;
        }

        protected static string IdentityClaimConverter(FilterBase filter, string suggestedAdProperty)
        {
            IdentityClaim ic = (IdentityClaim)filter.Value;

            if (ic.UrnScheme == null)
                throw new ArgumentException(SR.StoreCtxIdentityClaimMustHaveScheme);

            string urnValue = ic.UrnValue;
            if (urnValue == null)
                urnValue = "";

            string filterString = null;

            IdentityClaimToFilter(urnValue, ic.UrnScheme, ref filterString, true);

            return filterString;
        }

        // If useSidHistory == false, build a filter for objectSid.
        // If useSidHistory == true, build a filter for objectSid and sidHistory.
        protected static bool SecurityIdentityClaimConverterHelper(string urnValue, bool useSidHistory, StringBuilder filter, bool throwOnFail)
        {
            // String is in SDDL format.  Translate it to ldap hex format

            IntPtr pBytePtr = IntPtr.Zero;
            byte[] sidB = null;

            try
            {
                if (UnsafeNativeMethods.ConvertStringSidToSid(urnValue, ref pBytePtr))
                {
                    // Now we convert the native SID to a byte[] SID
                    sidB = Utils.ConvertNativeSidToByteArray(pBytePtr);
                    if (null == sidB)
                    {
                        if (throwOnFail)
                            throw new ArgumentException(SR.StoreCtxSecurityIdentityClaimBadFormat);
                        else
                            return false;
                    }
                }
                else
                {
                    if (throwOnFail)
                        throw new ArgumentException(SR.StoreCtxSecurityIdentityClaimBadFormat);
                    else
                        return false;
                }
            }
            finally
            {
                if (IntPtr.Zero != pBytePtr)
                    UnsafeNativeMethods.LocalFree(pBytePtr);
            }

            StringBuilder stringizedBinarySid = new StringBuilder();
            foreach (byte b in sidB)
            {
                stringizedBinarySid.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }
            string ldapHexSid = ADUtils.HexStringToLdapHexString(stringizedBinarySid.ToString());

            if (ldapHexSid == null)
                return false;

            if (useSidHistory)
            {
                filter.Append("(|(objectSid=");
                filter.Append(ldapHexSid);
                filter.Append(")(sidHistory=");
                filter.Append(ldapHexSid);
                filter.Append("))");
            }
            else
            {
                filter.Append("(objectSid=");
                filter.Append(ldapHexSid);
                filter.Append(")");
            }

            return true;
        }

        protected static string CertificateConverter(FilterBase filter, string suggestedAdProperty)
        {
            System.Security.Cryptography.X509Certificates.X509Certificate2 certificate =
                                    (System.Security.Cryptography.X509Certificates.X509Certificate2)filter.Value;

            byte[] rawCertificate = certificate.RawData;

            StringBuilder sb = new StringBuilder();

            sb.Append("(userCertificate=");
            sb.Append(ADUtils.EscapeBinaryValue(rawCertificate));
            sb.Append(")");

            return sb.ToString();
        }
        protected static string UserAccountControlConverter(FilterBase filter, string suggestedAdProperty)
        {
            Debug.Assert(String.Compare(suggestedAdProperty, "userAccountControl", StringComparison.OrdinalIgnoreCase) == 0);

            StringBuilder sb = new StringBuilder();

            // bitwise-AND

            bool value = (bool)filter.Value;

            switch (filter.PropertyName)
            {
                case AuthPrincEnabledFilter.PropertyNameStatic:
                    // UF_ACCOUNTDISABLE
                    // Note that the logic is inverted on this one.  We expose "Enabled",
                    // but AD stores it as "Disabled".                    
                    if (value)
                        sb.Append("(!(userAccountControl:1.2.840.113556.1.4.803:=2))");
                    else
                        sb.Append("(userAccountControl:1.2.840.113556.1.4.803:=2)");
                    break;

                case SmartcardLogonRequiredFilter.PropertyNameStatic:
                    // UF_SMARTCARD_REQUIRED
                    if (value)
                        sb.Append("(userAccountControl:1.2.840.113556.1.4.803:=262144)");
                    else
                        sb.Append("(!(userAccountControl:1.2.840.113556.1.4.803:=262144))");

                    break;

                case DelegationPermittedFilter.PropertyNameStatic:
                    // UF_NOT_DELEGATED
                    // Note that the logic is inverted on this one.  That's because we expose
                    // "delegation allowed", but AD represents it as the inverse, "delegation NOT allowed"
                    if (value)
                        sb.Append("(!(userAccountControl:1.2.840.113556.1.4.803:=1048576))");
                    else
                        sb.Append("(userAccountControl:1.2.840.113556.1.4.803:=1048576)");

                    break;

                case PasswordNotRequiredFilter.PropertyNameStatic:
                    // UF_PASSWD_NOTREQD
                    if (value)
                        sb.Append("(userAccountControl:1.2.840.113556.1.4.803:=32)");
                    else
                        sb.Append("(!(userAccountControl:1.2.840.113556.1.4.803:=32))");

                    break;

                case PasswordNeverExpiresFilter.PropertyNameStatic:
                    // UF_DONT_EXPIRE_PASSWD
                    if (value)
                        sb.Append("(userAccountControl:1.2.840.113556.1.4.803:=65536)");
                    else
                        sb.Append("(!(userAccountControl:1.2.840.113556.1.4.803:=65536))");

                    break;

                case CannotChangePasswordFilter.PropertyNameStatic:
                    // UF_PASSWD_CANT_CHANGE
                    // This bit doesn't work correctly in AD (AD models the "user can't change password"
                    // setting as special ACEs in the ntSecurityDescriptor).
                    throw new InvalidOperationException(
                                            String.Format(
                                                    CultureInfo.CurrentCulture,
                                                    SR.StoreCtxUnsupportedPropertyForQuery,
                                                    PropertyNamesExternal.GetExternalForm(filter.PropertyName)));

                case AllowReversiblePasswordEncryptionFilter.PropertyNameStatic:
                    // UF_ENCRYPTED_TEXT_PASSWORD_ALLOWED
                    if (value)
                        sb.Append("(userAccountControl:1.2.840.113556.1.4.803:=128)");
                    else
                        sb.Append("(!(userAccountControl:1.2.840.113556.1.4.803:=128))");

                    break;

                default:
                    Debug.Fail("ADStoreCtx.UserAccountControlConverter: fell off end looking for " + filter.PropertyName);
                    break;
            }

            return sb.ToString();
        }

        protected static string BinaryConverter(FilterBase filter, string suggestedAdProperty)
        {
            StringBuilder sb = new StringBuilder();

            if (filter.Value != null)
            {
                sb.Append("(");
                sb.Append(suggestedAdProperty);
                sb.Append("=");
                sb.Append(ADUtils.EscapeBinaryValue((byte[])filter.Value));
            }
            else
            {
                sb.Append("(!(");
                sb.Append(suggestedAdProperty);
                sb.Append("=*))");
            }

            sb.Append(")");

            return sb.ToString();
        }

        protected static string ExpirationDateConverter(FilterBase filter, string suggestedAdProperty)
        {
            Debug.Assert(String.Compare(suggestedAdProperty, "accountExpires", StringComparison.OrdinalIgnoreCase) == 0);
            Debug.Assert(filter is ExpirationDateFilter);

            Nullable<DateTime> date = (Nullable<DateTime>)filter.Value;
            StringBuilder sb = new StringBuilder();

            if (!date.HasValue)
            {
                // Spoke with ColinBr.  Both values are used to represent "no expiration date set".
                sb.Append("(|(accountExpires=9223372036854775807)(accountExpires=0))");
            }
            else
            {
                sb.Append("(accountExpires=");
                sb.Append(ADUtils.DateTimeToADString(date.Value));
                sb.Append(")");
            }

            return sb.ToString();
        }

        protected static string GuidConverter(FilterBase filter, string suggestedAdProperty)
        {
            Debug.Assert(String.Compare(suggestedAdProperty, "objectGuid", StringComparison.OrdinalIgnoreCase) == 0);
            Debug.Assert(filter is GuidFilter);

            Nullable<Guid> guid = (Nullable<Guid>)filter.Value;
            StringBuilder sb = new StringBuilder();

            if (guid == null)
            {
                // Spoke with ColinBr.  Both values are used to represent "no expiration date set".
                // sb.Append("(|(accountExpires=9223372036854775807)(accountExpires=0))");
            }
            else
            {
                sb.Append("(objectGuid=");

                // Transform from hex string ("1AFF") to LDAP hex string ("\1A\FF")
                string ldapHexGuid = ADUtils.HexStringToLdapHexString(guid.ToString());

                if (ldapHexGuid == null)
                    throw new InvalidOperationException(SR.StoreCtxGuidIdentityClaimBadFormat);

                sb.Append(ldapHexGuid);

                sb.Append(")");
            }

            return sb.ToString();
        }

        protected static string MatchingIntConverter(FilterBase filter, string suggestedAdProperty)
        {
            Debug.Assert(filter.Value is QbeMatchType);

            QbeMatchType qmt = (QbeMatchType)filter.Value;

            return (ExtensionTypeConverter(suggestedAdProperty, qmt.Value.GetType(), qmt.Value, qmt.Match));
        }

        protected static string DefaultValutMatchingDateTimeConverter(FilterBase filter, string suggestedAdProperty)
        {
            Debug.Assert(filter.Value is QbeMatchType);

            QbeMatchType qmt = (QbeMatchType)filter.Value;

            Debug.Assert(qmt.Value is DateTime);

            return (DateTimeFilterBuilder(suggestedAdProperty, (DateTime)qmt.Value, LdapConstants.defaultUtcTime, false, qmt.Match));
        }

        protected static string MatchingDateTimeConverter(FilterBase filter, string suggestedAdProperty)
        {
            Debug.Assert(filter.Value is QbeMatchType);

            QbeMatchType qmt = (QbeMatchType)filter.Value;

            Debug.Assert(qmt.Value is DateTime);

            return (ExtensionTypeConverter(suggestedAdProperty, qmt.Value.GetType(), qmt.Value, qmt.Match));
        }

        protected static string LastLogonConverter(FilterBase filter, string suggestedAdProperty)
        {
            Debug.Assert(filter.Value is QbeMatchType);

            QbeMatchType qmt = (QbeMatchType)filter.Value;

            Debug.Assert(qmt.Value is DateTime);
            Debug.Assert((suggestedAdProperty == "lastLogon") || (suggestedAdProperty == "lastLogonTimestamp"));

            StringBuilder sb = new StringBuilder();
            sb.Append("(|");
            sb.Append(DateTimeFilterBuilder("lastLogon", (DateTime)qmt.Value, LdapConstants.defaultUtcTime, false, qmt.Match));
            sb.Append(DateTimeFilterBuilder("lastLogonTimestamp", (DateTime)qmt.Value, LdapConstants.defaultUtcTime, true, qmt.Match));
            sb.Append(")");

            return (sb.ToString());
        }

        protected static string GroupTypeConverter(FilterBase filter, string suggestedAdProperty)
        {
            Debug.Assert(String.Compare(suggestedAdProperty, "groupType", StringComparison.OrdinalIgnoreCase) == 0);
            Debug.Assert(filter is GroupIsSecurityGroupFilter || filter is GroupScopeFilter);

            // 1.2.840.113556.1.4.803 is like a bit-wise AND operator
            switch (filter.PropertyName)
            {
                case GroupIsSecurityGroupFilter.PropertyNameStatic:

                    bool value = (bool)filter.Value;

                    // GROUP_TYPE_SECURITY_ENABLED
                    // If group is enabled, it IS security-enabled
                    if (value)
                        return "(groupType:1.2.840.113556.1.4.803:=2147483648)";
                    else
                        return "(!(groupType:1.2.840.113556.1.4.803:=2147483648))";

                case GroupScopeFilter.PropertyNameStatic:

                    GroupScope value2 = (GroupScope)filter.Value;

                    switch (value2)
                    {
                        case GroupScope.Local:
                            // GROUP_TYPE_RESOURCE_GROUP, a.k.a. ADS_GROUP_TYPE_DOMAIN_LOCAL_GROUP
                            return "(groupType:1.2.840.113556.1.4.803:=4)";

                        case GroupScope.Global:
                            // GROUP_TYPE_ACCOUNT_GROUP, a.k.a. ADS_GROUP_TYPE_GLOBAL_GROUP
                            return "(groupType:1.2.840.113556.1.4.803:=2)";

                        default:
                            // GROUP_TYPE_UNIVERSAL_GROUP, a.k.a. ADS_GROUP_TYPE_UNIVERSAL_GROUP
                            Debug.Assert(value2 == GroupScope.Universal);
                            return "(groupType:1.2.840.113556.1.4.803:=8)";
                    }

                default:
                    Debug.Fail("ADStoreCtx.GroupTypeConverter: fell off end looking for " + filter.PropertyName);
                    return "";
            }
        }

        public static string DateTimeFilterBuilder(string attributeName, DateTime searchValue, DateTime defaultValue, bool requirePresence, MatchType mt)
        {
            string ldapSearchValue = null;
            string ldapDefaultValue = null;
            bool defaultNeeded = false;

            ldapSearchValue = ADUtils.DateTimeToADString(searchValue);
            if (defaultValue != null)
            {
                ldapDefaultValue = ADUtils.DateTimeToADString(defaultValue);
            }

            StringBuilder ldapFilter = new StringBuilder("(");

            if (defaultValue != null && (mt != MatchType.Equals && mt != MatchType.NotEquals))
            {
                defaultNeeded = true;
            }

            if (defaultNeeded || (mt == MatchType.NotEquals && requirePresence))
            {
                ldapFilter.Append("&(");
            }

            switch (mt)
            {
                case MatchType.Equals:
                    ldapFilter.Append(attributeName);
                    ldapFilter.Append("=");
                    ldapFilter.Append(ldapSearchValue);
                    break;

                case MatchType.NotEquals:
                    ldapFilter.Append("!(");
                    ldapFilter.Append(attributeName);
                    ldapFilter.Append("=");
                    ldapFilter.Append(ldapSearchValue);
                    ldapFilter.Append(")");
                    break;

                case MatchType.GreaterThanOrEquals:
                    ldapFilter.Append(attributeName);
                    ldapFilter.Append(">=");
                    ldapFilter.Append(ldapSearchValue);
                    break;

                case MatchType.LessThanOrEquals:
                    ldapFilter.Append(attributeName);
                    ldapFilter.Append("<=");
                    ldapFilter.Append(ldapSearchValue);
                    break;

                case MatchType.GreaterThan:
                    ldapFilter.Append("&");

                    // Greater-than-or-equals (or less-than-or-equals))
                    ldapFilter.Append("(");
                    ldapFilter.Append(attributeName);
                    ldapFilter.Append(mt == MatchType.GreaterThan ? ">=" : "<=");
                    ldapFilter.Append(ldapSearchValue);
                    ldapFilter.Append(")");

                    // And not-equal
                    ldapFilter.Append("(!(");
                    ldapFilter.Append(attributeName);
                    ldapFilter.Append("=");
                    ldapFilter.Append(ldapSearchValue);
                    ldapFilter.Append("))");

                    // And exists (need to include because of tristate LDAP logic)
                    ldapFilter.Append("(");
                    ldapFilter.Append(attributeName);
                    ldapFilter.Append("=*)");
                    break;

                case MatchType.LessThan:
                    goto case MatchType.GreaterThan;
            }

            ldapFilter.Append(")");
            bool closeFilter = false;

            if (defaultNeeded)
            {
                ldapFilter.Append("(!");
                ldapFilter.Append(attributeName);
                ldapFilter.Append("=");
                ldapFilter.Append(ldapDefaultValue);
                ldapFilter.Append(")");
                closeFilter = true;
            }

            if (mt == MatchType.NotEquals && requirePresence)
            {
                ldapFilter.Append("(");
                ldapFilter.Append(attributeName);
                ldapFilter.Append("=*)");
                closeFilter = true;
            }

            if (closeFilter)
                ldapFilter.Append(")");

            return (ldapFilter.ToString());
        }

        public static string ExtensionTypeConverter(string attributeName, Type type, Object value, MatchType mt)
        {
            StringBuilder ldapFilter = new StringBuilder("(");
            string ldapValue;

            if (typeof(Boolean) == type)
            {
                ldapValue = ((bool)value ? "TRUE" : "FALSE");
            }
            else if (type is ICollection)
            {
                StringBuilder collectionFilter = new StringBuilder();

                ICollection collection = (ICollection)value;

                foreach (object o in collection)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "ExtensionTypeConverter collection filter type " + o.GetType().ToString());
                    collectionFilter.Append(ExtensionTypeConverter(attributeName, o.GetType(), o, mt));
                }
                return collectionFilter.ToString();
            }
            else if (typeof(DateTime) == type)
            {
                ldapValue = ADUtils.DateTimeToADString((DateTime)value);
            }
            else
            {
                ldapValue = ADUtils.PAPIQueryToLdapQueryString(value.ToString());
            }

            switch (mt)
            {
                case MatchType.Equals:
                    ldapFilter.Append(attributeName);
                    ldapFilter.Append("=");
                    ldapFilter.Append(ldapValue);
                    break;

                case MatchType.NotEquals:
                    ldapFilter.Append("!(");
                    ldapFilter.Append(attributeName);
                    ldapFilter.Append("=");
                    ldapFilter.Append(ldapValue);
                    ldapFilter.Append(")");
                    break;

                case MatchType.GreaterThanOrEquals:
                    ldapFilter.Append(attributeName);
                    ldapFilter.Append(">=");
                    ldapFilter.Append(ldapValue);
                    break;

                case MatchType.LessThanOrEquals:
                    ldapFilter.Append(attributeName);
                    ldapFilter.Append("<=");
                    ldapFilter.Append(ldapValue);
                    break;

                case MatchType.GreaterThan:
                    ldapFilter.Append("&");

                    // Greater-than-or-equals (or less-than-or-equals))
                    ldapFilter.Append("(");
                    ldapFilter.Append(attributeName);
                    ldapFilter.Append(mt == MatchType.GreaterThan ? ">=" : "<=");
                    ldapFilter.Append(ldapValue);
                    ldapFilter.Append(")");

                    // And not-equal
                    ldapFilter.Append("(!(");
                    ldapFilter.Append(attributeName);
                    ldapFilter.Append("=");
                    ldapFilter.Append(ldapValue);
                    ldapFilter.Append("))");

                    // And exists (need to include because of tristate LDAP logic)
                    ldapFilter.Append("(");
                    ldapFilter.Append(attributeName);
                    ldapFilter.Append("=*)");
                    break;

                case MatchType.LessThan:
                    goto case MatchType.GreaterThan;
            }

            ldapFilter.Append(")");

            return ldapFilter.ToString();
        }

        protected static string ExtensionCacheConverter(FilterBase filter, string suggestedAdProperty)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "ExtensionCacheConverter ");

            StringBuilder query = new StringBuilder();

            if (filter.Value != null)
            {
                ExtensionCache ec = (ExtensionCache)filter.Value;

                foreach (KeyValuePair<string, ExtensionCacheValue> kvp in ec.properties)
                {
                    Type type = kvp.Value.Type == null ? kvp.Value.Value.GetType() : kvp.Value.Type;

                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "ExtensionCacheConverter filter type " + type.ToString());
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "ExtensionCacheConverter match type " + kvp.Value.MatchType.ToString());

                    if (kvp.Value.Value is ICollection)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "ExtensionCacheConverter encountered collection.");

                        ICollection collection = (ICollection)kvp.Value.Value;
                        foreach (object o in collection)
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "ExtensionCacheConverter collection filter type " + o.GetType().ToString());
                            query.Append(ExtensionTypeConverter(kvp.Key, o.GetType(), o, kvp.Value.MatchType));
                        }
                    }
                    else
                    {
                        query.Append(ExtensionTypeConverter(kvp.Key, type, kvp.Value.Value, kvp.Value.MatchType));
                    }
                }
            }

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "ExtensionCacheConverter complete built filter  " + query.ToString());
            return query.ToString();
        }

        ///
        /// <summary>
        /// Adds the specified Property set to the TypeToPropListMap data structure.
        /// </summary>
        ///
        private void AddPropertySetToTypePropListMap(Type principalType, StringCollection propertySet)
        {
            lock (TypeToLdapPropListMap)
            {
                if (!TypeToLdapPropListMap[this.MappingTableIndex].ContainsKey(principalType))
                {
                    TypeToLdapPropListMap[this.MappingTableIndex].Add(principalType, propertySet);
                }
            }
        }
    }
}

//#endif  // PAPI_AD
