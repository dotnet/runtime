// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Specialized;
using System.Text;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using System.Net;
using System.Security.Principal;

using System.DirectoryServices;

using MACLPrinc = System.Security.Principal;
using System.Security.AccessControl;

namespace System.DirectoryServices.AccountManagement
{
    internal partial class ADStoreCtx : StoreCtx
    {
        //
        // Native <--> Principal
        //

        // For modified object, pushes any changes (including IdentityClaim changes) 
        // into the underlying store-specific object (e.g., DirectoryEntry) and returns the underlying object.
        // For unpersisted object, creates a  underlying object if one doesn't already exist (in
        // Principal.UnderlyingObject), then pushes any changes into the underlying object.
        internal override object PushChangesToNative(Principal p)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "Entering PushChangesToNative, type={0}", p.GetType());

            try
            {
                DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;

                if (de == null)
                {
                    // Must be a newly-inserted Principal for which PushChangesToNative has not yet
                    // been called.

                    // Determine the objectClass of the AD entry we'll be creating
                    string objectClass;
                    Type principalType = p.GetType();
                    // read the rdnPrefix off of the class attribute.  This defaults to CN for the internal classes
                    string rdnPrefix = p.ExtensionHelper.RdnPrefix;
                    string rdnValue = null;
                    string baseObjectRdnPrefix = null;

                    if (principalType == typeof(UserPrincipal))
                        objectClass = "user";
                    else if (principalType == typeof(GroupPrincipal))
                        objectClass = "group";
                    else if (principalType == typeof(ComputerPrincipal))
                        objectClass = "computer";
                    else
                    {
                        objectClass = p.ExtensionHelper.StructuralObjectClass;

                        if (null == objectClass || null == rdnPrefix)
                        {
                            throw new InvalidOperationException(SR.ExtensionInvalidClassAttributes);
                        }

                        // We need to determine if this class is dervived from one of the base classes but has a different RdnPrefix
                        // For the base objects ( User, Computer and Group ) Their RDNPrefix is a required field along with the RDNPrefix for the
                        // derived object.  This is only done for classes that derive from User, Computer or Group.  If a user derives his own class from AuthPrincipal
                        // they are responsible for setting all required base class properties.

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

                            // If the base objects RDN prefix is not the same as the dervied class then we need to set both
                            if (defaultRdn != rdnPrefix)
                            {
                                baseObjectRdnPrefix = defaultRdn;
                            }
                        }
                    }

                    // Determine a RDN for the AD entry we'll be creating.
                    // In order, try:
                    //  (1) The NAme is they set one.
                    //  (2) The SAM account name if they set one
                    string rdn = null;

                    if (p.GetChangeStatusForProperty(PropertyNames.PrincipalName))
                    {
                        // They set a display name.
                        string name = (string)p.GetValueForProperty(PropertyNames.PrincipalName);

                        if ((name != null) && (name.Length > 0))
                            rdnValue = ADUtils.EscapeDNComponent(name);
                    }

                    if (rdnValue == null)
                    {
                        if (p.GetChangeStatusForProperty(PropertyNames.PrincipalSamAccountName))
                        {
                            // They set a sAMAccountName.  If it's an invalid claim, we'll just ignore it here.
                            // The error will get picked up in IdentClaimToLdapConverter.
                            string samAccountName = (string)p.GetValueForProperty(PropertyNames.PrincipalSamAccountName);

                            int index = samAccountName.IndexOf('\\');

                            if (index != samAccountName.Length - 1)
                            {
                                samAccountName = (index != -1) ? samAccountName.Substring(index + 1) :    // +1 to skip the '/'
                                                                     samAccountName;
                            }

                            if ((samAccountName != null) && (samAccountName.Length > 0))
                            {
                                rdnValue = ADUtils.EscapeDNComponent(samAccountName);
                            }
                        }
                    }

                    if (rdnPrefix == null)
                    {
                        // There was no rdn prefix attribute set on the principal class
                        throw new InvalidOperationException(SR.ExtensionInvalidClassAttributes);
                    }

                    if (rdnValue == null)
                    {
                        // They didn't set a display name or SAM IdentityClaim (or set an empty/invalid value for those).
                        throw new InvalidOperationException(SR.NameMustBeSetToPersistPrincipal);
                    }

                    rdn = rdnPrefix + "=" + rdnValue;

                    lock (_ctxBaseLock)
                    {
                        de = this.ctxBase.Children.Add(rdn, objectClass);
                    }

                    if (null != baseObjectRdnPrefix)
                    {
                        de.Properties[baseObjectRdnPrefix].Value = rdnValue;
                    }

                    InitializeNewDirectoryOptions(de);

                    p.UnderlyingObject = de;

                    // set the default user account control bits for authenticable principals
                    if (principalType.IsSubclassOf(typeof(AuthenticablePrincipal)))
                    {
                        InitializeUserAccountControl((AuthenticablePrincipal)p);
                    }

                    GlobalDebug.WriteLineIf(
                        GlobalDebug.Info, "ADStoreCtx", "PushChangesToNative: created fresh DE, oc={0}, path={1}",
                        objectClass, de.Path);
                }

                // propertyMappingTableByProperty only has entries for writable properties,
                // including writable properties which we don't support in AD.
                // If we don't support the property, the PropertyMappingTableEntry will map
                // it to a converter which will throw an appropriate exception.
                Hashtable mappingTableByProp = (Hashtable)s_propertyMappingTableByProperty[this.MappingTableIndex];

                foreach (DictionaryEntry dictEntry in mappingTableByProp)
                {
                    ArrayList propertyEntries = (ArrayList)dictEntry.Value;

                    foreach (PropertyMappingTableEntry propertyEntry in propertyEntries)
                    {
                        if (p.GetChangeStatusForProperty(propertyEntry.propertyName))
                        {
                            // The property was set.  Write it to the DirectoryEntry.
                            Debug.Assert(propertyEntry.propertyName == (string)dictEntry.Key);

                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "PushChangesToNative: pushing {0}", propertyEntry.propertyName);

                            propertyEntry.papiToLdapConverter(p, propertyEntry.propertyName, de, propertyEntry.suggestedADPropertyName);
                        }
                    }
                }

                return de;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
        }

        // Given a underlying store object (e.g., DirectoryEntry), further narrowed down a discriminant
        // (if applicable for the StoreCtx type), returns a fresh instance of a Principal 
        // object based on it.  The WinFX Principal API follows ADSI-style semantics, where you get multiple
        // in-memory objects all referring to the same store pricipal, rather than WinFS semantics, where
        // multiple searches all return references to the same in-memory object.
        // Used to implement the reverse wormhole.  Also, used internally by FindResultEnumerator
        // to construct Principals from the store objects returned by a store query.
        //
        // The Principal object produced by this method does not have all the properties
        // loaded.  The Principal object will call the Load method on demand to load its properties
        // from its Principal.UnderlyingObject.
        //
        //
        // This method works for native objects from the store corresponding to _this_ StoreCtx.
        // Each StoreCtx will also have its own internal algorithms used for dealing with cross-store objects, e.g., 
        // for use when iterating over group membership.  These routines are exposed as 
        // ResolveCrossStoreRefToPrincipal, and will be called by the StoreCtx's associated ResultSet
        // classes when iterating over a representation of a "foreign" principal.

        // This method will either be passed a DirectoryEntry or SearchResult object if this is the result of a search.  
        // We need to determine the type and then use the appropriate object.
        internal override Principal GetAsPrincipal(object storeObject, object discriminant)
        {
            Debug.Assert(storeObject != null);

            DirectoryEntry de = null;
            SearchResult sr = null;
            string path;
            string distinguishedName;

            if (storeObject is DirectoryEntry)
            {
                de = (DirectoryEntry)storeObject;
                path = de.Path;
                distinguishedName = (string)de.Properties["distinguishedName"].Value;
            }
            else
            {
                Debug.Assert(storeObject is SearchResult);
                sr = (SearchResult)storeObject;
                path = sr.Path;
                distinguishedName = (string)sr.Properties["distinguishedName"][0];
            }

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "GetAsPrincipal: using path={0}", (null != de ? de.Path : "searchResult"));

            // Construct an appropriate Principal object.

            bool targetIsFromGC = SDSUtils.IsObjectFromGC(path);
            try
            {
                Principal p;

                DirectoryEntry dcEntry = null;
                PrincipalContext constructedContext = null;

                // if the object was obtained from a GC, we have to construct a new context, and build a new DirectoryEntry
                // if the object is not from a GC but belongs to another domain, we just have to construct a new context. We can still use the storeObject or searchresult's DirectoryEntry.
                // if our context is not a domain (that is, it's ADLDS) we don't build a new context unless the object was obtained from a GC. 
                if (targetIsFromGC || OwningContext.ContextType == ContextType.Domain)
                {
                    string dnsDomainName = SDSUtils.ConstructDnsDomainNameFromDn(distinguishedName);
                    if (targetIsFromGC ||
                        (!string.IsNullOrEmpty(this.domainDnsName) && !string.Equals(this.DnsDomainName, dnsDomainName, StringComparison.OrdinalIgnoreCase)))
                    {
                        constructedContext = SDSCache.Domain.GetContext(dnsDomainName, this.Credentials, this.OwningContext.Options);
                    }

                    if (targetIsFromGC)
                    {
                        dcEntry = SDSUtils.BuildDirectoryEntry("LDAP://" + dnsDomainName + "/" + GetEscapedDN(distinguishedName), this.Credentials, this.authTypes);
                        this.InitializeNewDirectoryOptions(dcEntry);
                    }
                }

                if (de != null)
                {
                    //NOTE: If target is GC then we do NOT use variable "de". So, need to dispose this at the end.
                    p = SDSUtils.DirectoryEntryToPrincipal((targetIsFromGC ? dcEntry : de), constructedContext ?? this.OwningContext, (Type)discriminant);
                }
                else
                {
                    //NOTE: If input storeObject is searchResult then variable "de" gets used in all cases.
                    p = SDSUtils.SearchResultToPrincipal(sr, constructedContext ?? this.OwningContext, (Type)discriminant);
                    p.UnderlyingObject = (targetIsFromGC ? dcEntry : sr.GetDirectoryEntry());
                }

                Debug.Assert(p != null);

                Guid g;

                if (de != null)
                    g = de.Guid;
                else
                {
                    byte[] guid = (byte[])sr.Properties["objectGuid"][0];
                    g = new Guid(guid);
                }

                ADStoreKey key = new ADStoreKey(g);
                p.Key = key;

                return p;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
            finally
            {
                if (targetIsFromGC && de != null)
                {
                    //Since target is GC, variable "de" is not getting used. Disposing it.
                    de.Dispose();
                }
            }
        }

        /// <summary>
        /// This Calls the Native API to Escape the DN
        /// </summary>
        /// <param name="dn"></param>
        /// <returns>Escaped DN</returns>
        private string GetEscapedDN(string dn)
        {
            UnsafeNativeMethods.Pathname pathNameObj = new UnsafeNativeMethods.Pathname();
            UnsafeNativeMethods.IADsPathname pathCracker = (UnsafeNativeMethods.IADsPathname)pathNameObj;

            // Set the Escape mode On
            pathCracker.EscapedMode = 2 /* ADS_ESCAPEDMODE_ON */;

            // Set the type of path to DN
            pathCracker.Set(dn, 4 /* ADS_SETTYPE_DN */);

            // Get back the escaped DN
            return pathCracker.Retrieve(7 /* ADS_FORMAT_X500_DN */);
        }

        internal override void Load(Principal p, string principalPropertyName)
        {
            Debug.Assert(p != null);
            Debug.Assert(p.UnderlyingObject != null);
            Debug.Assert(p.UnderlyingObject is DirectoryEntry);

            dSPropertyCollection props;

            SearchResult sr = (SearchResult)p.UnderlyingSearchObject;
            if (null == sr)
            {
                DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;
                Debug.Assert(de != null);
                Debug.Assert(p.GetUnderlyingObjectType() == typeof(DirectoryEntry));

                props = new dSPropertyCollection(de.Properties);
            }
            else
            {
                props = new dSPropertyCollection(sr.Properties);
            }

            Hashtable propertyMappingTable = (Hashtable)s_propertyMappingTableByPropertyFull[this.MappingTableIndex];

            ArrayList entries = (ArrayList)propertyMappingTable[principalPropertyName];

            // We don't support this property and cannot load it.  To maintain backward compatibility with the old code just return.
            if (entries == null)
                return;

            Debug.Assert(null != entries);

            try
            {
                foreach (PropertyMappingTableEntry entry in entries)
                {
                    if (null != entry.ldapToPapiConverter)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "Load_PropertyName: loading {0}", entry.propertyName);
                        entry.ldapToPapiConverter(props, entry.suggestedADPropertyName, p, entry.propertyName);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
        }

        // Loads the store values from p.UnderlyingObject into p, performing schema mapping as needed.
        internal override void Load(Principal p)
        {
            Debug.Assert(p != null);
            Debug.Assert(p.UnderlyingObject != null);
            Debug.Assert(p.UnderlyingObject is DirectoryEntry);

            DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;
            Debug.Assert(de != null);

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "Entering Load, type={0}, path={1}", p.GetType(), de.Path);

            Hashtable ldapMappingTable = (Hashtable)s_propertyMappingTableByLDAP[this.MappingTableIndex];
            foreach (DictionaryEntry Dictentry in ldapMappingTable)
            {
                ArrayList entries = (ArrayList)Dictentry.Value;
                try
                {
                    foreach (PropertyMappingTableEntry entry in entries)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "Load: loading {0}", entry.propertyName);
                        entry.ldapToPapiConverter(new dSPropertyCollection(de.Properties), entry.suggestedADPropertyName, p, entry.propertyName);
                    }
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(e);
                }
            }

            p.Loaded = true;
        }

        // Performs store-specific resolution of an IdentityReference to a Principal
        // corresponding to the IdentityReference.  Returns null if no matching object found.
        // principalType can be used to scope the search to principals of a specified type, e.g., users or groups.
        // Specify typeof(Principal) to search all principal types.
        internal override Principal FindPrincipalByIdentRef(
                                    Type principalType, string urnScheme, string urnValue, DateTime referenceDate)
        {
            return FindPrincipalByIdentRefHelper(principalType, urnScheme, urnValue, referenceDate, false);
        }

        // Normally, doing a FindByIdentity for a SID doesn't include the SID History.  This method provides a
        // means to include the sidHistory as well as the objectSid in the search.
        internal Principal FindPrincipalBySID(
                                    Type principalType, IdentityReference ir, bool useSidHistory)
        {
            return FindPrincipalByIdentRefHelper(principalType,
                                                 ir.UrnScheme,
                                                 ir.UrnValue,
                                                 DateTime.UtcNow,
                                                 useSidHistory);
        }

        // Handles all the work required to implement FindPrincipalByIdentRef (and FindPrincipalBySID).
        private Principal FindPrincipalByIdentRefHelper(
                                    Type principalType,
                                    string urnScheme,
                                    string urnValue,
                                    DateTime referenceDate,
                                    bool useSidHistory)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                    "ADStoreCtx",
                                    "FindPrincipalByIdentRefHelper: type={0}, scheme={1}, value={2}, useSidHistory={3}",
                                    principalType.ToString(),
                                    (urnScheme != null ? urnScheme : "NULL"),
                                    (urnValue != null ? urnValue : "NULL"),
                                    useSidHistory);

            //
            // Set up a DirectorySearcher
            //
            DirectorySearcher ds = new DirectorySearcher(this.ctxBase);
            SearchResultCollection src = null;

            try
            {
                ds.SizeLimit = 2;   // so we can efficiently check for duplicates

                // If we are searching for AuthPrincpal or Principal in the end we will construct the actual type
                // i.e. if the objects objectClass is User we will construct a UserPrincipal even though they searched for Principal.FindByIdentity
                // At this time we don't know the actual object type so we have to ask AD for all the attributes of the derived types so they are there
                // when we go to load the principal.
                if (principalType == typeof(Principal) || principalType == typeof(AuthenticablePrincipal))
                {
                    BuildPropertySet(typeof(UserPrincipal), ds.PropertiesToLoad);
                    BuildPropertySet(typeof(GroupPrincipal), ds.PropertiesToLoad);
                    BuildPropertySet(typeof(ComputerPrincipal), ds.PropertiesToLoad);

                    if (principalType == typeof(Principal))
                    {
                        BuildPropertySet(typeof(AuthenticablePrincipal), ds.PropertiesToLoad);
                    }
                }

                BuildPropertySet(principalType, ds.PropertiesToLoad);

                //
                // Build an appropriate filter
                //

                StringBuilder ldapFilter = new StringBuilder();

                // Limit the results returned to principalType by specifying the corresponding objectClass/objectCategory
                ldapFilter.Append(GetObjectClassPortion(principalType));

                // Build the rest of the filter based off of the user's specified IdentityReference.
                if (urnScheme != null)
                {
                    // If they're searching by SID for a SID corresponding to a fake group, construct
                    // and return the fake group
                    if ((urnScheme == UrnScheme.SidScheme) &&
                         (principalType == typeof(Principal) || principalType == typeof(GroupPrincipal) || principalType.IsSubclassOf(typeof(GroupPrincipal))))
                    {
                        SecurityIdentifier sid = new SecurityIdentifier(urnValue);
                        byte[] sidb = new byte[sid.BinaryLength];
                        sid.GetBinaryForm(sidb, 0);
                        //                        = Utils.StringToByteArray(urnValue);

                        if (sid == null)
                            throw new ArgumentException(SR.StoreCtxSecurityIdentityClaimBadFormat);

                        IntPtr pSid = IntPtr.Zero;

                        try
                        {
                            pSid = Utils.ConvertByteArrayToIntPtr(sidb);

                            if (UnsafeNativeMethods.IsValidSid(pSid) && (Utils.ClassifySID(pSid) == SidType.FakeObject))
                            {
                                GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                                        "ADStoreCtx",
                                                        "FindPrincipalByIdentRefHelper: fake principal, SID Scheme, {0}",
                                                        sid.ToString());

                                return ConstructFakePrincipalFromSID(sidb);
                            }
                        }
                        finally
                        {
                            if (pSid != IntPtr.Zero)
                                Marshal.FreeHGlobal(pSid);
                        }
                    }

                    // This is the simple case --- we got a specific UrnScheme, so we'll just build
                    // a filter for it.

                    // Ignore referenceDate --- all IdentityClaims in AD are forever
                    string innerLdapFilter = null;
                    BuildLdapFilterFromIdentityClaim(urnValue, urnScheme, ref innerLdapFilter, useSidHistory, true);

                    ldapFilter.Append(innerLdapFilter);
                }
                else
                {
                    // Are they perhaps searching for a fake group?
                    // If they passed in a valid SID for a fake group, construct and return the fake
                    // group.                
                    if (principalType == typeof(Principal) || principalType == typeof(GroupPrincipal) || principalType.IsSubclassOf(typeof(GroupPrincipal)))
                    {
                        SecurityIdentifier sid = null;
                        byte[] sidb = null;
                        try
                        {
                            sid = new SecurityIdentifier(urnValue);
                            sidb = new byte[sid.BinaryLength];
                            sid.GetBinaryForm(sidb, 0);
                        }
                        catch (ArgumentException)
                        {
                        }

                        if (sidb != null)
                        {
                            // They passed in a hex string, is it a valid SID, and if so, does it correspond to a fake
                            // principal?
                            IntPtr pSid = IntPtr.Zero;

                            try
                            {
                                pSid = Utils.ConvertByteArrayToIntPtr(sidb);

                                if (UnsafeNativeMethods.IsValidSid(pSid) && (Utils.ClassifySID(pSid) == SidType.FakeObject))
                                {
                                    GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                                            "ADStoreCtx",
                                                            "FindPrincipalByIdentRefHelper: fake principal, null Scheme, {0}",
                                                            sid.ToString());

                                    return ConstructFakePrincipalFromSID(sidb);
                                }
                            }
                            finally
                            {
                                if (pSid != IntPtr.Zero)
                                    Marshal.FreeHGlobal(pSid);
                            }
                        }
                    }

                    // This is the tricky case.  They didn't specify a UrnScheme, so we need to
                    // try all of them.

                    string[] urnSchemesToTry = new string[]
                    {
                        UrnScheme.SamAccountScheme,
                        UrnScheme.UpnScheme,
                        UrnScheme.DistinguishedNameScheme,
                        UrnScheme.SidScheme,
                        UrnScheme.GuidScheme,
                        UrnScheme.NameScheme
                    };

                    StringBuilder innerLdapFilter = new StringBuilder();

                    innerLdapFilter.Append("(|");

                    string filterVal = null;

                    foreach (string urnSchemeToTry in urnSchemesToTry)
                    {
                        if (BuildLdapFilterFromIdentityClaim(urnValue, urnSchemeToTry, ref filterVal, useSidHistory, false))
                            if (null != filterVal)
                                innerLdapFilter.Append(filterVal);
                    }

                    innerLdapFilter.Append(")");

                    ldapFilter.Append(innerLdapFilter.ToString());
                }

                // Wrap off the filter
                ldapFilter.Append(")");

                ds.Filter = ldapFilter.ToString();
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "FindPrincipalByIdentRefHelper: using LDAP filter {0}", ds.Filter);

                //
                // Perform the actual search
                //
                src = ds.FindAll();

                Debug.Assert(src != null);

                if (src == null)
                    return null;

                // Did we find a match?
                int count = src.Count;

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "FindPrincipalByIdentRefHelper: found {0} matches", count);

                // Did we find more than one match?
                if (count > 1)
                    throw new MultipleMatchesException(SR.MultipleMatchingPrincipals);

                if (count == 0)
                    return null;

                return GetAsPrincipal(src[0], principalType);
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
            finally
            {
                ds.Dispose();
                if (src != null)
                {
                    src.Dispose();
                }
            }
        }

        // Returns a type indicating the type of object that would be returned as the wormhole for the specified
        // Principal.  For some StoreCtxs, this method may always return a constant (e.g., typeof(DirectoryEntry)
        // for ADStoreCtx).  For others, it may vary depending on the Principal passed in.
        internal override Type NativeType(Principal p)
        {
            Debug.Assert(p != null);

            return typeof(DirectoryEntry);
        }

        //
        // Property mapping tables
        //

        // We only list properties we map in this table.  At run-time, if we detect they set a
        // property that's not listed here when writing to AD, we throw an exception.
        //
        // When updating this table, be sure to also update LoadDirectoryEntryAttributes() to load
        // in any newly-added attributes.

        private static object[,] s_propertyMappingTableRaw =
        {
            // PropertyName                          AD property             Converter(LDAP->PAPI)                                    Converter(PAPI->LDAP)
            {PropertyNames.PrincipalDisplayName,     "displayname",          new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.PrincipalDescription,     "description",          new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.PrincipalDistinguishedName,  "distinguishedname",    new FromLdapConverterDelegate(StringFromLdapConverter), new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.PrincipalSid,  "objectsid",            new FromLdapConverterDelegate(SidFromLdapConverter),  null},
            {PropertyNames.PrincipalSamAccountName,  "samaccountname",       new FromLdapConverterDelegate(StringFromLdapConverter), new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.PrincipalUserPrincipalName,  "userprincipalname",    new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.PrincipalGuid,  "objectguid",           new FromLdapConverterDelegate(GuidFromLdapConverter),   null},
            {PropertyNames.PrincipalStructuralObjectClass,  "objectclass",           new FromLdapConverterDelegate(ObjectClassFromLdapConverter),   null},
            {PropertyNames.PrincipalName,  "name",           new FromLdapConverterDelegate(StringFromLdapConverter), new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.PrincipalExtensionCache,  null,  null, new ToLdapConverterDelegate(ExtensionCacheToLdapConverter)},

            {PropertyNames.AuthenticablePrincipalEnabled,      "useraccountcontrol", new FromLdapConverterDelegate(UACFromLdapConverter),  new ToLdapConverterDelegate(UACToLdapConverter)},
            {PropertyNames.AuthenticablePrincipalCertificates, "usercertificate",    new FromLdapConverterDelegate(CertFromLdapConverter), new ToLdapConverterDelegate(CertToLdap)},

            {PropertyNames.GroupIsSecurityGroup,   "grouptype", new FromLdapConverterDelegate(GroupTypeFromLdapConverter), new ToLdapConverterDelegate(GroupTypeToLdapConverter)},
            {PropertyNames.GroupGroupScope, "grouptype", new FromLdapConverterDelegate(GroupTypeFromLdapConverter), new ToLdapConverterDelegate(GroupTypeToLdapConverter)},

            {PropertyNames.UserGivenName,             "givenname",        new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.UserMiddleName,            "middlename",       new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.UserSurname,               "sn",               new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.UserEmailAddress,          "mail",             new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.UserVoiceTelephoneNumber,  "telephonenumber",  new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.UserEmployeeID,            "employeeid",       new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},

            {PropertyNames.ComputerServicePrincipalNames, "serviceprincipalname", new FromLdapConverterDelegate(MultiStringFromLdapConverter), new ToLdapConverterDelegate(MultiStringToLdapConverter)},

            {PropertyNames.AcctInfoAcctLockoutTime,       "lockouttime",        new FromLdapConverterDelegate(GenericDateTimeFromLdapConverter), null},
            {PropertyNames.AcctInfoLastLogon,             "lastlogon",          new FromLdapConverterDelegate(LastLogonFromLdapConverter),       null},
            {PropertyNames.AcctInfoLastLogon,             "lastlogontimestamp", new FromLdapConverterDelegate(LastLogonFromLdapConverter),       null},
            {PropertyNames.AcctInfoPermittedWorkstations, "userworkstations",   new FromLdapConverterDelegate(CommaStringFromLdapConverter),     new ToLdapConverterDelegate(CommaStringToLdapConverter)},
            {PropertyNames.AcctInfoPermittedLogonTimes,   "logonhours",         new FromLdapConverterDelegate(BinaryFromLdapConverter),          new ToLdapConverterDelegate(BinaryToLdapConverter)},
            {PropertyNames.AcctInfoExpirationDate,        "accountexpires",     new FromLdapConverterDelegate(AcctExpirFromLdapConverter),       new ToLdapConverterDelegate(AcctExpirToLdapConverter)},
            {PropertyNames.AcctInfoSmartcardRequired,     "useraccountcontrol", new FromLdapConverterDelegate(UACFromLdapConverter),             new ToLdapConverterDelegate(UACToLdapConverter)},
            {PropertyNames.AcctInfoDelegationPermitted,   "useraccountcontrol", new FromLdapConverterDelegate(UACFromLdapConverter),             new ToLdapConverterDelegate(UACToLdapConverter)},
            {PropertyNames.AcctInfoBadLogonCount,         "badpwdcount",        new FromLdapConverterDelegate(IntFromLdapConverter),             null},
            {PropertyNames.AcctInfoHomeDirectory,         "homedirectory",      new FromLdapConverterDelegate(StringFromLdapConverter),          new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.AcctInfoHomeDrive,             "homedrive",          new FromLdapConverterDelegate(StringFromLdapConverter),          new ToLdapConverterDelegate(StringToLdapConverter)},
            {PropertyNames.AcctInfoScriptPath,            "scriptpath",         new FromLdapConverterDelegate(StringFromLdapConverter),          new ToLdapConverterDelegate(StringToLdapConverter)},

            {PropertyNames.PwdInfoLastPasswordSet,        "pwdlastset",           new FromLdapConverterDelegate(GenericDateTimeFromLdapConverter),     null},
            {PropertyNames.PwdInfoLastBadPasswordAttempt, "badpasswordtime",      new FromLdapConverterDelegate(GenericDateTimeFromLdapConverter),     null},
            {PropertyNames.PwdInfoPasswordNotRequired,    "useraccountcontrol",   new FromLdapConverterDelegate(UACFromLdapConverter),                 new ToLdapConverterDelegate(UACToLdapConverter)},
            {PropertyNames.PwdInfoPasswordNeverExpires,   "useraccountcontrol",   new FromLdapConverterDelegate(UACFromLdapConverter),                 new ToLdapConverterDelegate(UACToLdapConverter)},
            {PropertyNames.PwdInfoCannotChangePassword,   null, null,     new ToLdapConverterDelegate(CannotChangePwdToLdapConverter)},
            {PropertyNames.PwdInfoAllowReversiblePasswordEncryption,     "useraccountcontrol",    new FromLdapConverterDelegate(UACFromLdapConverter), new ToLdapConverterDelegate(UACToLdapConverter)}
        };
        /*/
        ///*******  Mapping table for perf testing...
                static object[,] propertyMappingTableRaw = 
                {
                    // PropertyName                          AD property             Converter(LDAP->PAPI)                                    Converter(PAPI->LDAP)
                    {PropertyNames.PrincipalDisplayName,     "displayName",          new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},
                    {PropertyNames.PrincipalDescription,     "description",          new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},  
        //            {PropertyNames.PrincipalDistinguishedName,  "distinguishedName",    null, null},
                    {PropertyNames.PrincipalSid,  "objectSid",            new FromLdapConverterDelegate(SidFromLdapConverter),  null},
                    {PropertyNames.PrincipalSamAccountName,  "samAccountName",       new FromLdapConverterDelegate(StringFromLdapConverter), new ToLdapConverterDelegate(StringToLdapConverter)},
                    {PropertyNames.PrincipalUserPrincipalName,  "userPrincipalName",    new FromLdapConverterDelegate(StringFromLdapConverter),  new ToLdapConverterDelegate(StringToLdapConverter)},
                    {PropertyNames.PrincipalGuid,  "objectGuid",           new FromLdapConverterDelegate(GuidFromLdapConverter),   null},
                    {PropertyNames.PrincipalStructuralObjectClass,  "objectClass",           null, null},
                    {PropertyNames.PrincipalName,  "name",           new FromLdapConverterDelegate(StringFromLdapConverter), new ToLdapConverterDelegate(StringToLdapConverter)},
                    {PropertyNames.PrincipalExtensionCache,  null,  null, null},                

                    {PropertyNames.AuthenticablePrincipalEnabled,      "userAccountControl", null, null},
                  {PropertyNames.AuthenticablePrincipalCertificates, "userCertificate",    null, null},

                    {PropertyNames.GroupIsSecurityGroup,   "groupType", null, null},
                                {PropertyNames.GroupGroupScope, "groupType", null, null},
                   // 
                              {PropertyNames.UserGivenName,             "givenName",        null, null},
                               {PropertyNames.UserMiddleName,            "middleName",       null, null},
                                {PropertyNames.UserSurname,               "sn",               null, null},
                                {PropertyNames.UserEmailAddress,          "mail",             null, null},
                                {PropertyNames.UserVoiceTelephoneNumber,  "telephoneNumber",  null, null},
                                {PropertyNames.UserEmployeeID,            "employeeID",       null, null},

                                {PropertyNames.ComputerServicePrincipalNames, "servicePrincipalName", null, null},
                                {PropertyNames.AcctInfoAcctLockoutTime,       "lockoutTime",        null, null},
                                {PropertyNames.AcctInfoLastLogon,             "lastLogon",          null, null},
                                {PropertyNames.AcctInfoLastLogon,             "lastLogonTimestamp", null, null},
                                {PropertyNames.AcctInfoPermittedWorkstations, "userWorkstations",   null, null},
                                {PropertyNames.AcctInfoPermittedLogonTimes,   "logonHours",         null, null},
                    {PropertyNames.AcctInfoExpirationDate,        "accountExpires",     new FromLdapConverterDelegate(AcctExpirFromLdapConverter),       new ToLdapConverterDelegate(AcctExpirToLdapConverter)},
                                {PropertyNames.AcctInfoSmartcardRequired,     "userAccountControl", null, null},
                                {PropertyNames.AcctInfoDelegationPermitted,   "userAccountControl", null, null},
                                {PropertyNames.AcctInfoBadLogonCount,         "badPwdCount",        null, null},
                                {PropertyNames.AcctInfoHomeDirectory,         "homeDirectory",      null, null},
                                {PropertyNames.AcctInfoHomeDrive,             "homeDrive",          null, null},
                                {PropertyNames.AcctInfoScriptPath,            "scriptPath",         null, null},

                                {PropertyNames.PwdInfoLastPasswordSet,        "pwdLastSet",           null, null},
                                {PropertyNames.PwdInfoLastBadPasswordAttempt, "badPasswordTime",      null, null},
                                {PropertyNames.PwdInfoPasswordNotRequired,    "userAccountControl",   null, null},
                                {PropertyNames.PwdInfoPasswordNeverExpires,   "userAccountControl",   null, null},
        //            {PropertyNames.PwdInfoCannotChangePassword,   "ntSecurityDescriptor", null,     new ToLdapConverterDelegate(CannotChangePwdToLdapConverter)},
                                {PropertyNames.PwdInfoAllowReversiblePasswordEncryption,     "userAccountControl",    null, null}
                };

        /************************/
        // This table only includes properties that are writeable.
        private static Hashtable s_propertyMappingTableByProperty = null;
        private static Hashtable s_propertyMappingTableByLDAP = null;
        protected static Dictionary<string, bool> NonPresentAttrDefaultStateMapping = null;
        private static Hashtable s_propertyMappingTableByPropertyFull = null;

        protected static Dictionary<int, Dictionary<Type, StringCollection>> TypeToLdapPropListMap = null;

        private class PropertyMappingTableEntry
        {
            internal string propertyName;               // PAPI name
            internal string suggestedADPropertyName;    // LDAP attribute name
            internal FromLdapConverterDelegate ldapToPapiConverter;
            internal ToLdapConverterDelegate papiToLdapConverter;
        }

        //
        // Conversion routines
        //

        // Loads the specified attribute of the DirectoryEntry into the specified property of the Principal
        protected delegate void FromLdapConverterDelegate(dSPropertyCollection properties, string suggestedAdProperty, Principal p, string propertyName);

        // Loads the specified property of the Principal into the specified attribute of the DirectoryEntry.
        // For multivalued attributes, must test to make sure the value hasn't already been loaded into the DirectoryEntry
        // (to maintain idempotency when PushChangesToNative is called multiple times).
        protected delegate void ToLdapConverterDelegate(Principal p, string propertyName, DirectoryEntry de, string suggestedAdProperty);

        //
        // Conversion: LDAP --> PAPI
        //
        protected static void SidFromLdapConverter(dSPropertyCollection properties, string suggestedAdProperty, Principal p, string propertyName)
        {
            if (properties["objectSid"].Count > 0)
            {
                byte[] sid = (byte[])properties["objectSid"][0];

                SecurityIdentifier SecurityId = new SecurityIdentifier(sid, 0);

                p.LoadValueIntoProperty(propertyName, (object)SecurityId);
            }
            else
            {
                p.LoadValueIntoProperty(propertyName, null);
            }
        }

        protected static void GuidFromLdapConverter(dSPropertyCollection properties, string suggestedAdProperty, Principal p, string propertyName)
        {
            Debug.Assert(properties["objectGuid"].Count == 1);

            if (properties["objectGuid"].Count == 1)
            {
                byte[] guid = (byte[])properties["objectGuid"][0];

                Guid g = new Guid(guid);

                p.LoadValueIntoProperty(propertyName, g);
            }
            else
            {
                p.LoadValueIntoProperty(propertyName, null);
            }
        }

        protected static void StringFromLdapConverter(dSPropertyCollection properties, string suggestedAdProperty, Principal p, string propertyName)
        {
            SDSUtils.SingleScalarFromDirectoryEntry<string>(properties, suggestedAdProperty, p, propertyName);
        }

        protected static void MultiStringFromLdapConverter(dSPropertyCollection properties, string suggestedAdProperty, Principal p, string propertyName)
        {
            // ValueCollection<string> is Load'ed from a List<string>
            SDSUtils.MultiScalarFromDirectoryEntry<string>(properties, suggestedAdProperty, p, propertyName);
        }

        protected static void BoolFromLdapConverter(dSPropertyCollection properties, string suggestedAdProperty, Principal p, string propertyName)
        {
            SDSUtils.SingleScalarFromDirectoryEntry<bool>(properties, suggestedAdProperty, p, propertyName);
        }

        // This function is converting the disabled directory status into the enabled principal property
        // the boolan value needs to be negated
        protected static void AcctDisabledFromLdapConverter(dSPropertyCollection properties, string suggestedAdProperty, Principal p, string propertyName)
        {
            if (properties[suggestedAdProperty].Count > 0)
            {
                // We're intended to handle single-valued scalar properties
                Debug.Assert(properties[suggestedAdProperty].Count == 1);
                Debug.Assert(properties[suggestedAdProperty][0] is bool);

                p.LoadValueIntoProperty(propertyName, !(bool)properties[suggestedAdProperty][0]);
            }
        }

        protected static void CommaStringFromLdapConverter(dSPropertyCollection properties, string suggestedAdProperty, Principal p, string propertyName)
        {
            Debug.Assert(string.Equals(suggestedAdProperty, "userWorkstations", StringComparison.OrdinalIgnoreCase));

            // The userWorkstations attribute is odd.  Rather than being a multivalued string attribute, it's a single-valued
            // string of comma-separated values.

            dSPropertyValueCollection values = properties[suggestedAdProperty];

            if (values.Count != 0)
            {
                Debug.Assert(values.Count == 1);
                Debug.Assert(values[0] is string);

                string commaSeparatedValues = (string)values[0];
                string[] individualValues = commaSeparatedValues.Split(new char[] { ',' });

                // ValueCollection<string> is Load'ed from a List<string>
                List<string> list = new List<string>(individualValues.Length);

                foreach (string s in individualValues)
                {
                    list.Add(s);
                }

                p.LoadValueIntoProperty(propertyName, list);
            }
        }

        protected static void IntFromLdapConverter(dSPropertyCollection properties, string suggestedAdProperty, Principal p, string propertyName)
        {
            SDSUtils.SingleScalarFromDirectoryEntry<int>(properties, suggestedAdProperty, p, propertyName);
        }

        protected static void BinaryFromLdapConverter(dSPropertyCollection properties, string suggestedAdProperty, Principal p, string propertyName)
        {
            SDSUtils.SingleScalarFromDirectoryEntry<byte[]>(properties, suggestedAdProperty, p, propertyName);
        }

        protected static void CertFromLdapConverter(dSPropertyCollection properties, string suggestedAdProperty, Principal p, string propertyName)
        {
            // Cert collection is Load'ed via a list of byte[], each representing a cert
            SDSUtils.MultiScalarFromDirectoryEntry<byte[]>(properties, suggestedAdProperty, p, propertyName);
        }

        protected static void UACFromLdapConverter(dSPropertyCollection properties, string suggestedAdProperty, Principal p, string propertyName)
        {
            Debug.Assert(string.Equals(suggestedAdProperty, "userAccountControl", StringComparison.OrdinalIgnoreCase));

            SDSUtils.AccountControlFromDirectoryEntry(properties, suggestedAdProperty, p, propertyName, false);
        }

        protected static void GenericDateTimeFromLdapConverter(dSPropertyCollection properties, string suggestedAdProperty, Principal p, string propertyName)
        {
            DateTimeFromLdapConverter(properties, suggestedAdProperty, p, propertyName, false);
        }

        protected static void ObjectClassFromLdapConverter(dSPropertyCollection properties, string suggestedAdProperty, Principal p, string propertyName)
        {
            dSPropertyValueCollection values = properties[suggestedAdProperty];

            if (values.Count > 0)
            {
                // This is a multivalued attribute and we want the last element.  The most specialized object class...
                Debug.Assert(values[values.Count - 1] is string);
                p.LoadValueIntoProperty(propertyName, (string)values[values.Count - 1]);
            }
        }

        protected static void LastLogonFromLdapConverter(dSPropertyCollection properties, string suggestedAdProperty, Principal p, string propertyName)
        {
            // W2k DCs support just "lastLogon".  W2k3 DCs also support "lastLogonTimestamp".  The latter is replicated, and
            // preferred over the former.
            if (string.Equals(suggestedAdProperty, "lastLogon", StringComparison.OrdinalIgnoreCase))
            {
                // Is "lastLogonTimestamp" available instead?

                if (properties["lastLogonTimestamp"].Count != 0)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "LastLogonFromLdapConverter: found lastLogonTimestamp");

                    // "lastLogonTimestamp" is available.  So ignore "lastLogon" and do nothing.  We'll be called again
                    // when Load() gets to the "lastLogonTimestamp" attribute (or it may already have).
                    return;
                }
            }

            // Either we're processing "lastLogonTimestamp", or there is no "lastLogonTimestamp" and we're processing "lastLogon".
            // Either way, it's handled like a generic date.
            DateTimeFromLdapConverter(properties, suggestedAdProperty, p, propertyName, false);
        }

        protected static void AcctExpirFromLdapConverter(dSPropertyCollection properties, string suggestedAdProperty, Principal p, string propertyName)
        {
            DateTimeFromLdapConverter(properties, suggestedAdProperty, p, propertyName, true);
        }

        protected static void DateTimeFromLdapConverter(dSPropertyCollection properties, string suggestedAdProperty, Principal p, string propertyName, bool useAcctExpLogic)
        {
            dSPropertyValueCollection values = properties[suggestedAdProperty];

            if (values.Count != 0)
            {
                Debug.Assert(values.Count == 1);

                long filetime;

                if (values[0] is long)
                    filetime = (long)values[0];
                else
                    filetime = ADUtils.LargeIntToInt64((UnsafeNativeMethods.IADsLargeInteger)values[0]);

                Nullable<DateTime> dt;

                // Filetimes use "0" to mean "no value, except for accountExpires,
                // which uses both "0" and "0x7fffffffffffffff"  to mean "no expiration date"
                if ((!useAcctExpLogic) && (filetime == 0x0))
                    dt = null;
                else if ((useAcctExpLogic) && (filetime == 0x0 || filetime == 0x7fffffffffffffff))
                    dt = null;
                else
                    dt = ADUtils.ADFileTimeToDateTime(filetime);

                p.LoadValueIntoProperty(propertyName, dt);
            }
        }

        protected static void GroupTypeFromLdapConverter(dSPropertyCollection properties, string suggestedAdProperty, Principal p, string propertyName)
        {
            Debug.Assert(string.Equals(suggestedAdProperty, "groupType", StringComparison.OrdinalIgnoreCase));

            dSPropertyValueCollection values = properties[suggestedAdProperty];

            if (values.Count != 0)
            {
                Debug.Assert(values.Count == 1);
                Debug.Assert(values[0] is int);

                int groupTypeCombined = (int)values[0];

                switch (propertyName)
                {
                    case PropertyNames.GroupIsSecurityGroup:

                        bool isSecurityEnabled = false;

                        // GROUP_TYPE_SECURITY_ENABLED
                        if ((groupTypeCombined & 0x80000000) != 0)
                            isSecurityEnabled = true;

                        // isSecurityEnabled --> group IS enabled
                        p.LoadValueIntoProperty(propertyName, isSecurityEnabled);
                        break;

                    case PropertyNames.GroupGroupScope:

                        GroupScope groupType = GroupScope.Universal;

                        if ((groupTypeCombined & ADGroupScope.Global) != 0) // ADS_GROUP_TYPE_GLOBAL_GROUP
                            groupType = GroupScope.Global;
                        else if ((groupTypeCombined & ADGroupScope.Local) != 0) // ADS_GROUP_TYPE_DOMAIN_LOCAL_GROUP
                            groupType = GroupScope.Local;
                        else
                            Debug.Assert((groupTypeCombined & ADGroupScope.Universal) != 0); // ADS_GROUP_TYPE_UNIVERSAL_GROUP

                        p.LoadValueIntoProperty(propertyName, groupType);
                        break;

                    default:
                        Debug.Fail("ADStoreCtx.GroupTypeFromLdapConverter: Fell off end looking for " + propertyName);
                        break;
                }
            }
        }

        protected static bool CannotChangePwdFromLdapConverter(DirectoryEntry de)
        {
            // We use the same logic as the AD Users & Computers snapin.  We scan the DACL,
            // looking for ALLOW or DENY ACEs for the "user can change password" right.  We scan
            // for both self and world ACEs.  If we find neither explitic allow nor explicit deny ACEs,
            // we (like the AD U&C snapin) default to assuming the user can change his or her password.

            // *******************************
            // retrieving ObjectSecurity after
            // previously modifying the ACL will return null unless we force a cache refresh.  We have to do this always,
            // even before we call ObjectSecurity to see if it would return null, because once ObjectSecurity returns null the
            // first time, it'll keep returning null even if we refresh the cache.

            if (!de.Properties.Contains("nTSecurityDescriptor"))
                de.RefreshCache(new string[] { "nTSecurityDescriptor" });

            ActiveDirectorySecurity adsSecurity = de.ObjectSecurity;

            bool denySelfFound;
            bool denyWorldFound;
            bool allowSelfFound;
            bool allowWorldFound;

            // Perform the scan
            ScanACLForChangePasswordRight(adsSecurity, out denySelfFound, out denyWorldFound, out allowSelfFound, out allowWorldFound);

            // Map the ACEs found to "user cannot change password" status
            bool userCannotChangePassword;

            if (denySelfFound || denyWorldFound)
            {
                // Explicit deny --> user can't change password
                userCannotChangePassword = true;
            }
            else if ((!denySelfFound && !denyWorldFound) && (allowSelfFound || allowWorldFound))
            {
                // Explitic allow --> user can change password
                userCannotChangePassword = false;
            }
            else
            {
                // As with the AD U&C snapin, if the explitic ACEs don't tell us anything, we
                // fallback to assuming the user can change their password
                userCannotChangePassword = false;
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADStoreCtx", "CannotChangePwdFromLdapConverter: fallback, assume user can change pwd");
            }

            return userCannotChangePassword;
        }

        private const string SelfSddl = "S-1-5-10";  // NT AUTHORITY\SELF
        private const string WorldSddl = "S-1-1-0";   // EVERYONE
        private static readonly Guid s_changePasswordGuid = new Guid("{ab721a53-1e2f-11d0-9819-00aa0040529b}");

        protected static void ScanACLForChangePasswordRight(
                                        ActiveDirectorySecurity adsSecurity,
                                        out bool denySelfFound,
                                        out bool denyWorldFound,
                                        out bool allowSelfFound,
                                        out bool allowWorldFound)
        {
            denySelfFound = false;
            denyWorldFound = false;
            allowSelfFound = false;
            allowWorldFound = false;

            MACLPrinc.SecurityIdentifier trustee;

            foreach (ActiveDirectoryAccessRule rule in adsSecurity.GetAccessRules(true, true, typeof(MACLPrinc.SecurityIdentifier)))
            {
                trustee = (MACLPrinc.SecurityIdentifier)rule.IdentityReference;
                string sidSddl = trustee.Value;

                if (rule.ObjectType == s_changePasswordGuid)
                {
                    if (rule.AccessControlType == AccessControlType.Deny)
                    {
                        if (sidSddl == SelfSddl)
                        {
                            denySelfFound = true;
                        }
                        else if (sidSddl == WorldSddl)
                        {
                            denyWorldFound = true;
                        }
                    }
                    else if (rule.AccessControlType == AccessControlType.Allow)
                    {
                        if (sidSddl == SelfSddl)
                        {
                            allowSelfFound = true;
                        }
                        else if (sidSddl == WorldSddl)
                        {
                            allowWorldFound = true;
                        }
                    }
                }
            }
        }

        //
        // Conversion: PAPI --> LDAP
        //

        protected static void StringToLdapConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedAdProperty)
        {
            string value = (string)p.GetValueForProperty(propertyName);

            if (p.unpersisted && value == null)
                return;

            if ((value == null) || (value.Length > 0))
                de.Properties[suggestedAdProperty].Value = value;
            else
                throw new ArgumentException(SR.Format(SR.InvalidStringValueForStore, propertyName));
        }

        protected static void BinaryToLdapConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedAdProperty)
        {
            byte[] value = (byte[])p.GetValueForProperty(propertyName);

            if (p.unpersisted && value == null)
                return;

            if (value != null && value.Length != 0)
                de.Properties[suggestedAdProperty].Value = value;
            else
                de.Properties[suggestedAdProperty].Value = null;
        }

        protected static void MultiStringToLdapConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedAdProperty)
        {
            SDSUtils.MultiStringToDirectoryEntryConverter(p, propertyName, de, suggestedAdProperty);
        }

        protected static void BoolToLdapConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedAdProperty)
        {
            object value = (bool)p.GetValueForProperty(propertyName);

            if (p.unpersisted && value == null)
                return;

            if (value != null)
                de.Properties[suggestedAdProperty].Value = (bool)value;
            else
                de.Properties[suggestedAdProperty].Value = null;
        }

        // This function is converting the disabled directory status into the enabled principal property
        // the boolan value needs to be negated
        protected static void AcctDisabledToLdapConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedAdProperty)
        {
            // Only modify disabled property if we are talking to an already persisted user.
            // We need to set the password before we can enable the user on new objects.
            if (!p.unpersisted)
            {
                object value = (bool)p.GetValueForProperty(propertyName);
                if (value != null)
                    de.Properties[suggestedAdProperty].Value = !(bool)value;
                else
                    de.Properties[suggestedAdProperty].Value = null;
            }
        }

        protected static void CommaStringToLdapConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedAdProperty)
        {
            PrincipalValueCollection<string> trackingList = (PrincipalValueCollection<string>)p.GetValueForProperty(propertyName);

            StringBuilder sb = new StringBuilder();

            foreach (string value in trackingList)
            {
                // Preexisting values that have not been removed.
                // This also includes inserted values.
                sb.Append(value);
                sb.Append(",");
            }

            // We have an extra comma at the end (assuming we added any values to the string).  Remove it.
            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);

            string s = (sb.Length > 0) ? sb.ToString() : null;

            if (p.unpersisted && s == null)
                return;

            de.Properties[suggestedAdProperty].Value = s;
        }

        protected static void CertToLdap(Principal p, string propertyName, DirectoryEntry de, string suggestedAdProperty)
        {
            X509Certificate2Collection certificates = (X509Certificate2Collection)p.GetValueForProperty(propertyName);

            if (certificates.Count == 0)
            {
                // Clear out the certificates
                de.Properties[suggestedAdProperty].Value = null;
            }
            else
            {
                // Replace the existing certs with the modified certs.  Note that this replaces all the certs ---
                // X509CertificateExCollection doesn't expose a finer-grained change-tracking mechanism
                byte[][] rawCerts = new byte[certificates.Count][];

                for (int i = 0; i < certificates.Count; i++)
                {
                    rawCerts[i] = certificates[i].RawData;
                }

                de.Properties[suggestedAdProperty].Value = null;        // remove the old
                de.Properties[suggestedAdProperty].Value = rawCerts;    // and put in the new
            }
        }

        protected static void UACToLdapConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedAdProperty)
        {
            Debug.Assert(string.Equals(suggestedAdProperty, "userAccountControl", StringComparison.OrdinalIgnoreCase));

            SDSUtils.AccountControlToDirectoryEntry(p, propertyName, de, suggestedAdProperty, false, p.unpersisted);
        }

        protected static void AcctExpirToLdapConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedAdProperty)
        {
            Nullable<DateTime> dt = (Nullable<DateTime>)p.GetValueForProperty(propertyName);

            if (p.unpersisted && dt == null)
                return;

            UnsafeNativeMethods.ADsLargeInteger largeIntObj = new UnsafeNativeMethods.ADsLargeInteger();
            UnsafeNativeMethods.IADsLargeInteger largeInt = (UnsafeNativeMethods.IADsLargeInteger)largeIntObj;

            if (!dt.HasValue)
            {
                // no expiration date
                largeInt.LowPart = unchecked((int)0xffffffff);
                largeInt.HighPart = (int)0x7fffffff;
            }
            else
            {
                long filetime = ADUtils.DateTimeToADFileTime(dt.Value);

                uint lowPart = (uint)(((ulong)filetime) & ((ulong)0x00000000ffffffff));
                uint highPart = (uint)((((ulong)filetime) & ((ulong)0xffffffff00000000)) >> 32);

                largeInt.LowPart = (int)lowPart;
                largeInt.HighPart = (int)highPart;
            }

            de.Properties[suggestedAdProperty].Value = largeInt;
        }

        // Supported types
        // ICollection where an item of the collection is not an ICollection or IList
        // object[]
        // object
        protected static void ExtensionCacheToLdapConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedAdProperty)
        {
            ExtensionCache cacheValues = (ExtensionCache)p.GetValueForProperty(propertyName);
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "ExtensionCacheToLdapConverter");

            foreach (KeyValuePair<string, ExtensionCacheValue> kvp in cacheValues.properties)
            {
                if (!kvp.Value.Filter && null != kvp.Value.Value && kvp.Value.Value.Length != 0)
                {
                    // array of objects   ( .Length > 1 && typeof(array[0] != ICollection or IList )
                    // Single collection ( .Length == 1 ) &&  typeof(array[0] == ICollection ) && typeof(array[0][0] != ICollection or IList )

                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "ExtensionCacheToLdapConverter - Value Type " + kvp.Value.Value.GetType().ToString());

                    if ((kvp.Value.Value.Length == 1 && kvp.Value.Value[0] is ICollection) || (kvp.Value.Value.Length > 1))
                    {
                        if (kvp.Value.Value.Length > 1 && (kvp.Value.Value[0] is ICollection))
                            throw new ArgumentException(SR.InvalidExtensionCollectionType);

                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "ExtensionCacheToLdapConverter - Value implements ICollection");

                        ICollection valueCollection;

                        // byte[] gets special treatment (following S.DS and ADSI) - we don't treat it as ICollection but rather as a whole
                        if (kvp.Value.Value.Length == 1 && kvp.Value.Value[0] is ICollection && !(kvp.Value.Value[0] is byte[]))
                        {
                            valueCollection = (ICollection)kvp.Value.Value[0];
                        }
                        else
                        {
                            valueCollection = (ICollection)kvp.Value.Value;
                        }

                        foreach (object oVal in valueCollection)
                        {
                            if (null != oVal)
                            {
                                if ((oVal is ICollection || oVal is IList) && !(oVal is byte[]))
                                    throw new ArgumentException(SR.InvalidExtensionCollectionType);

                                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "ExtensionCacheToLdapConverter - Element Value Type " + oVal.GetType().ToString());
                                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "ExtensionCacheToLdapConverter - Adding  Element " + oVal.ToString());
                            }

                            // Do nothing if we are not persisted and the value is null.  We can't delete a property that
                            // has not already been set.
                            if (p.unpersisted && null == oVal)
                                continue;

                            de.Properties[kvp.Key].Add(oVal);
                        }
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "ExtensionCacheToLdapConverter - Collection complete");
                    }
                    else
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "ExtensionCacheToLdapConverter - Adding " + kvp.Value.Value.ToString());
                        // Do nothing if we are not persisted and the value is null.  We can't delete a property that
                        // has not already been set.
                        if (p.unpersisted && (null == kvp.Value.Value[0]))
                            continue;

                        de.Properties[kvp.Key].Value = kvp.Value.Value[0];
                    }
                }
            }
        }

        protected static void GroupTypeToLdapConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedAdProperty)
        {
            Debug.Assert(propertyName == PropertyNames.GroupIsSecurityGroup || propertyName == PropertyNames.GroupGroupScope);

            int groupTypeCombined;

            // We want to get the current value, so we can flip the appropriate bit while leaving the other bits as-is.
            // If this is a to-be-inserted Principal, we may not have an existing groupType, so we'll use AD's default value
            // for new groups.
            if (de.Properties[suggestedAdProperty].Count > 0)
            {
                Debug.Assert(de.Properties[suggestedAdProperty].Count == 1);

                groupTypeCombined = (int)de.Properties[suggestedAdProperty][0];
            }
            else
            {
                if (!p.unpersisted)
                {
                    // It's not an unpersisted principal, so we should have the property.  Perhaps we don't have access
                    // to it.  In that case, we don't want to blindly overwrite whatever other bits might be there.
                    throw new PrincipalOperationException(
                                    SR.ADStoreCtxUnableToReadExistingGroupTypeFlagsForUpdate);
                }

                // initial default value
                groupTypeCombined = unchecked((int)(0x80000000 | 0x00000002));  //  GROUP_TYPE_SECURITY_ENABLED | GROUP_TYPE_ACCOUNT_GROUP
            }

            switch (propertyName)
            {
                case PropertyNames.GroupIsSecurityGroup:
                    bool groupEnabled = (bool)p.GetValueForProperty(propertyName);

                    // Flip the bit without touching the other bits.
                    if (!groupEnabled)
                        Utils.ClearBit(ref groupTypeCombined, 0x80000000);   // disabled --> clear GROUP_TYPE_SECURITY_ENABLED
                    else
                        Utils.SetBit(ref groupTypeCombined, 0x80000000);     // enabled --> set GROUP_TYPE_SECURITY_ENABLED

                    break;

                case PropertyNames.GroupGroupScope:

                    GroupScope groupType = (GroupScope)p.GetValueForProperty(propertyName);

                    // Remove the bits indicating the group type it currently is...
                    Utils.ClearBit(ref groupTypeCombined, ADGroupScope.Local);
                    Utils.ClearBit(ref groupTypeCombined, ADGroupScope.Global);
                    Utils.ClearBit(ref groupTypeCombined, ADGroupScope.Universal);

                    // ...and set the bit for the group type we want it to be
                    if (groupType == GroupScope.Local)
                    {
                        Utils.SetBit(ref groupTypeCombined, ADGroupScope.Local);
                    }
                    else if (groupType == GroupScope.Global)
                    {
                        Utils.SetBit(ref groupTypeCombined, ADGroupScope.Global);
                    }
                    else
                    {
                        Debug.Assert(groupType == GroupScope.Universal);
                        Utils.SetBit(ref groupTypeCombined, ADGroupScope.Universal);
                    }

                    break;

                default:
                    Debug.Fail("ADStoreCtx.GroupTypeToLdapConverter: Fell off end looking for " + propertyName);
                    break;
            }

            de.Properties[suggestedAdProperty].Value = groupTypeCombined;
        }

        // {PropertyNames.GroupMembers,  "members",   null,                                                      new ToLdapConverterDelegate(GroupMembersToLdapConverter)},

        protected static void UpdateGroupMembership(Principal group, DirectoryEntry de, NetCred credentials, AuthenticationTypes authTypes)
        {
            Debug.Assert(group.fakePrincipal == false);

            PrincipalCollection members = (PrincipalCollection)group.GetValueForProperty(PropertyNames.GroupMembers);

            DirectoryEntry groupDe = null;

            try
            {
                //
                // Process clear
                //
                if (members.Cleared)
                {
                    DirectoryEntry copyOfDe = null;

                    try
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "UpdateGroupMembership: clearing {0}", de.Path);

                        copyOfDe = SDSUtils.BuildDirectoryEntry(
                                                de.Path,
                                                credentials,
                                                authTypes);

                        Debug.Assert(copyOfDe != null);
                        copyOfDe.Properties["member"].Clear();
                        copyOfDe.CommitChanges();
                    }
                    finally
                    {
                        if (copyOfDe != null)
                            copyOfDe.Dispose();
                    }
                }

                //
                // Process inserted members
                //

                List<Principal> insertedMembers = members.Inserted;
                List<Principal> removedMembers = members.Removed;

                if (insertedMembers.Count > 0 || removedMembers.Count > 0)
                {
                    groupDe = SDSUtils.BuildDirectoryEntry(
                                            de.Path,
                                            credentials,
                                            authTypes);
                }

                // First, validate the members to be added
                foreach (Principal member in insertedMembers)
                {
                    Type memberType = member.GetType();
                    if ((memberType != typeof(UserPrincipal)) && (!memberType.IsSubclassOf(typeof(UserPrincipal))) &&
                        (memberType != typeof(ComputerPrincipal)) && (!memberType.IsSubclassOf(typeof(ComputerPrincipal))) &&
                        (memberType != typeof(GroupPrincipal)) && (!memberType.IsSubclassOf(typeof(GroupPrincipal))) &&
                        (!memberType.IsSubclassOf(typeof(AuthenticablePrincipal))))
                    {
                        throw new InvalidOperationException(
                                        SR.Format(SR.StoreCtxUnsupportedPrincipalTypeForGroupInsert, memberType));
                    }
                    // Can't inserted unpersisted principal
                    if (member.unpersisted)
                        throw new InvalidOperationException(SR.StoreCtxGroupHasUnpersistedInsertedPrincipal);

                    Debug.Assert(member.Context != null);

                    // Can only insert AD principals (no reg-SAM/MSAM principals)
                    if (member.ContextType == ContextType.Machine)
                        throw new InvalidOperationException(SR.ADStoreCtxUnsupportedPrincipalContextForGroupInsert);
                }

                // Now add each member to the group
                foreach (Principal member in insertedMembers)
                {
                    // For objects in the current domain or any other domains in the forest we need to use the objects DN
                    // SID path would work for current domain but would not work for child or parent domains.
                    // For foreign objects we must use SID path e.g.  "<SID=...>" so that the necessary FPO gets autocreated by AD.
                    // It also works in the "fake principal" case (which are always represented as FPOs).
                    if (!member.fakePrincipal && ADUtils.ArePrincipalsInSameForest(group, member))
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "UpdateGroupMembership: add {0}", member.DistinguishedName);

                        groupDe.Properties["member"].Add(member.DistinguishedName);
                    }
                    else
                    {
                        // Build a SID DN.  This needs to be a DN path not an ADSI sid path with the LDAP prefix.
                        string memberSidDN = GetSidPathFromPrincipal(member);

                        if (memberSidDN == null)
                            throw new PrincipalOperationException(SR.ADStoreCtxCouldntGetSIDForGroupMember);

                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "UpdateGroupMembership: add {0}", memberSidDN);

                        // Add the member to the group
                        groupDe.Properties["member"].Add(memberSidDN);
                    }
                }

                // If we had any members then commit them.
                if (insertedMembers.Count > 0)
                    groupDe.CommitChanges();

                //
                // Process removed members
                //

                foreach (Principal member in removedMembers)
                {
                    // Since we don't allow any of these to be inserted, none of them should ever
                    // show up in the removal list
                    Debug.Assert(member.unpersisted == false);
                    Debug.Assert(member.ContextType == ContextType.Domain || member.ContextType == ContextType.ApplicationDirectory);

                    // If the collection was cleared, there should be no original members to remove
                    Debug.Assert(members.Cleared == false);

                    // Since we are using PropertyValueCollection to do the item removal we are constrainted to items that are in the collection
                    // For principals that are in the same forest just use their DN to do the removal.  This is how they are represented in the member attr.
                    // For foreign principals we must represent them with their SID binding string since they are locally represented by an FSP object.
                    if (!member.fakePrincipal && ADUtils.ArePrincipalsInSameForest(group, member))
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "UpdateGroupMembership: remove via DN {0}", member.DistinguishedName);

                        // Remove the member from the group

                        groupDe.Properties["member"].Remove(member.DistinguishedName);
                    }
                    else
                    {
                        // SID DN case

                        // Build a SID DN.
                        string memberSidDN = GetSidPathFromPrincipal(member);

                        if (memberSidDN == null)
                            throw new PrincipalOperationException(SR.ADStoreCtxCouldntGetSIDForGroupMember);

                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "ADStoreCtx", "UpdateGroupMembership: remove via SID {0}", memberSidDN);

                        // Remove the member from the group
                        groupDe.Properties["member"].Remove(memberSidDN);
                    }
                }

                // If we used the collection to do a modification then commit it.
                if (removedMembers.Count > 0)
                    groupDe.CommitChanges();
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
            finally
            {
                if (null != groupDe)
                    groupDe.Dispose();
            }
        }

        // Builds a SID dn for the principal <SID=...>
        protected static string GetSidPathFromPrincipal(Principal p)
        {
            Debug.Assert(p.unpersisted == false);

            if (p.fakePrincipal)
            {
                SecurityIdentifier Sid = p.Sid;
                if (Sid == null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADStoreCtx", "GetSidADsPathFromPrincipal: no SID IC (fake principal)");
                    throw new InvalidOperationException(SR.StoreCtxNeedValueSecurityIdentityClaimToQuery);
                }

                return @"<SID=" + Utils.SecurityIdentifierToLdapHexBindingString(Sid) + ">";
            }
            else
            {
                // Retrieve the member's SID
                DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;
                Debug.Assert(de != null); // since member can't be unpersisted

                // Force it to load if it hasn't been already loaded
                if (!de.Properties.Contains("objectSid"))
                    de.RefreshCache(new string[] { "objectSid" });

                byte[] sid = (byte[])de.Properties["objectSid"].Value;

                if (sid == null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "ADStoreCtx", "GetSidADsPathFromPrincipal: no SID");
                    return null;
                }

                // Build a SID DN
                return @"<SID=" + Utils.ByteArrayToString(sid) + ">";
            }
        }

        protected static void CannotChangePwdToLdapConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedAdProperty)
        {
            Debug.Assert(propertyName == PropertyNames.PwdInfoCannotChangePassword);

            // Only modify disabled property if we are talking to an already persisted user.
            // We need to set the password before we can enable the user on new objects.
            // We can't read the sec desc of an unpersisted object so just return here.  this will be set later.
            if (p.unpersisted)
            {
                return;
            }

            SetCannotChangePasswordStatus(p, (bool)p.GetValueForProperty(propertyName), false);
        }

        protected bool BuildLdapFilterFromIdentityClaim(string urnValue, string urnScheme, ref string filter, bool useSidHistory, bool throwOnFail)
        {
            // To build the filter, we'll use the same IdentityClaimConverter routine as the QBE mechanism.
            // This routine takes an IdentityClaimFilter as input, which in turn wraps an IdentityClaim.
            // So we'll build an IdentityClaim from the user's parameters.

            IdentityClaim ic = new IdentityClaim();
            ic.UrnValue = urnValue;
            ic.UrnScheme = urnScheme;

            IdentityClaimFilter icFilterBase = new IdentityClaimFilter();
            icFilterBase.Value = ic;

            if (useSidHistory)
            {
                // Special handling if we want to include the SID History in the search
                Debug.Assert(urnScheme == UrnScheme.SidScheme);
                StringBuilder sb = new StringBuilder();
                if (false == SecurityIdentityClaimConverterHelper(urnValue, useSidHistory, sb, throwOnFail))
                {
                    return false;
                }
                filter = sb.ToString();
            }
            else
            {
                if (false == IdentityClaimToFilter(urnValue, urnScheme, ref filter, throwOnFail))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

// #endif   // PAPI_AD
