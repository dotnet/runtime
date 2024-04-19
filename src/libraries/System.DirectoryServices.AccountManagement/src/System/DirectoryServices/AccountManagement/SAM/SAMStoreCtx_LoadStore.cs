// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;

namespace System.DirectoryServices.AccountManagement
{
    internal sealed partial class SAMStoreCtx : StoreCtx
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
            try
            {
                DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;
                Type principalType = p.GetType();

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "Entering PushChangesToNative, type={0}", p.GetType());

                if (de == null)
                {
                    // Must be a newly-inserted Principal for which PushChangesToNative has not yet
                    // been called.

                    // Determine the objectClass of the SAM entry we'll be creating
                    string objectClass;

                    if (principalType == typeof(UserPrincipal) || principalType.IsSubclassOf(typeof(UserPrincipal)))
                        objectClass = "user";
                    else if (principalType == typeof(GroupPrincipal) || principalType.IsSubclassOf(typeof(GroupPrincipal)))
                        objectClass = "group";
                    else
                    {
                        throw new InvalidOperationException(
                                        SR.Format(SR.StoreCtxUnsupportedPrincipalTypeForSave, principalType));
                    }

                    // Determine the SAM account name for the entry we'll be creating.  Use the name from the NT4 IdentityClaim.
                    string samAccountName = GetSamAccountName(p);

                    if (samAccountName == null)
                    {
                        // They didn't set a NT4 IdentityClaim.
                        throw new InvalidOperationException(SR.NameMustBeSetToPersistPrincipal);
                    }

                    lock (_ctxBaseLock)
                    {
                        de = _ctxBase.Children.Add(samAccountName, objectClass);
                    }

                    GlobalDebug.WriteLineIf(
                        GlobalDebug.Info, "SAMStoreCtx", "PushChangesToNative: created fresh DE, oc={0}, name={1}, path={2}",
                        objectClass, samAccountName, de.Path);

                    p.UnderlyingObject = de;

                    // set the default user account control bits for authenticable principals
                    if (principalType.IsSubclassOf(typeof(AuthenticablePrincipal)))
                    {
                        InitializeUserAccountControl((AuthenticablePrincipal)p);
                    }
                }

                // Determine the mapping table to use, based on the principal type
                Hashtable propertyMappingTableByProperty;

                if (principalType == typeof(UserPrincipal))
                {
                    propertyMappingTableByProperty = s_userPropertyMappingTableByProperty;
                }
                else if (principalType == typeof(GroupPrincipal))
                {
                    propertyMappingTableByProperty = s_groupPropertyMappingTableByProperty;
                }
                else
                {
                    Debug.Assert(principalType == typeof(ComputerPrincipal));
                    propertyMappingTableByProperty = s_computerPropertyMappingTableByProperty;
                }

                // propertyMappingTableByProperty has entries for all writable properties,
                // including writable properties which we don't support in SAM for some or
                // all principal types.
                // If we don't support the property, the PropertyMappingTableEntry will map
                // it to a converter which will throw an appropriate exception.
                foreach (DictionaryEntry dictEntry in propertyMappingTableByProperty)
                {
                    ArrayList propertyEntries = (ArrayList)dictEntry.Value;

                    foreach (PropertyMappingTableEntry propertyEntry in propertyEntries)
                    {
                        if (null != propertyEntry.papiToWinNTConverter)
                        {
                            if (p.GetChangeStatusForProperty(propertyEntry.propertyName))
                            {
                                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "PushChangesToNative: pushing {0}", propertyEntry.propertyName);

                                // The property was set.  Write it to the DirectoryEntry.
                                Debug.Assert(propertyEntry.propertyName == (string)dictEntry.Key);
                                propertyEntry.papiToWinNTConverter(
                                                    p,
                                                    propertyEntry.propertyName,
                                                    de,
                                                    propertyEntry.suggestedWinNTPropertyName,
                                                    this.IsLSAM
                                                    );
                            }
                        }
                    }
                }

                // Unlike AD, where password sets on newly-created principals must be handled after persisting the principal,
                // in SAM they get set before persisting the principal, and ADSI's WinNT provider saves off the operation
                // until the SetInfo() is performed.

                if (p.GetChangeStatusForProperty(PropertyNames.PwdInfoPassword))
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "PushChangesToNative: setting password");

                    // Only AuthenticablePrincipals can have PasswordInfo
                    Debug.Assert(p is AuthenticablePrincipal);

                    string password = (string)p.GetValueForProperty(PropertyNames.PwdInfoPassword);
                    Debug.Assert(password != null); // if null, PasswordInfo should not have indicated it was changed

                    SDSUtils.SetPassword(de, password);
                }

                return de;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
        }

        private string GetSamAccountName(Principal p)
        {
            // They didn't set any IdentityClaims, so they certainly didn't set a NT4 IdentityClaim
            if (!p.GetChangeStatusForProperty(PropertyNames.PrincipalSamAccountName))
                return null;

            string Name = p.SamAccountName;

            if (Name == null)
                return null;

            // Split the SAM account name out of the UrnValue
            // We accept both "host\user" and "user"
            int index = Name.IndexOf('\\');

            if (index == Name.Length - 1)
                return null;

            return (index != -1) ? Name.Substring(index + 1) :    // +1 to skip the '/'
                                   Name;
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
        internal override Principal GetAsPrincipal(object storeObject, object discriminant)
        {
            // SAM doesn't use discriminant, should always be null.
            Debug.Assert(discriminant == null);

            Debug.Assert(storeObject != null);
            Debug.Assert(storeObject is DirectoryEntry);

            DirectoryEntry de = (DirectoryEntry)storeObject;

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "GetAsPrincipal: using path={0}", de.Path);

            // Construct an appropriate Principal object.
            Principal p = SDSUtils.DirectoryEntryToPrincipal(de, this.OwningContext, null);
            Debug.Assert(p != null);

            // Assign a SAMStoreKey to the newly-constructed Principal.

            // If it doesn't have an objectSid, it's not a principal and we shouldn't be here.
            Debug.Assert((de.Properties["objectSid"] != null) && (de.Properties["objectSid"].Count == 1));

            SAMStoreKey key = new SAMStoreKey(this.MachineFlatName, (byte[])de.Properties["objectSid"].Value);
            p.Key = key;

            return p;
        }

        internal override void Load(Principal p, string principalPropertyName)
        {
            Debug.Assert(p != null);
            Debug.Assert(p.UnderlyingObject != null);
            Debug.Assert(p.UnderlyingObject is DirectoryEntry);

            DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;

            Type principalType = p.GetType();
            Hashtable propertyMappingTableByProperty;

            if (principalType == typeof(UserPrincipal))
            {
                propertyMappingTableByProperty = s_userPropertyMappingTableByProperty;
            }
            else if (principalType == typeof(GroupPrincipal))
            {
                propertyMappingTableByProperty = s_groupPropertyMappingTableByProperty;
            }
            else
            {
                Debug.Assert(principalType == typeof(ComputerPrincipal));
                propertyMappingTableByProperty = s_computerPropertyMappingTableByProperty;
            }

            ArrayList entries = (ArrayList)propertyMappingTableByProperty[principalPropertyName];

            // We don't support this property and cannot load it.  To maintain backward compatibility with the old code just return.
            if (entries == null)
                return;

            try
            {
                foreach (PropertyMappingTableEntry entry in entries)
                {
                    if (null != entry.winNTToPapiConverter)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "Load_PropertyName: loading {0}", entry.propertyName);
                        entry.winNTToPapiConverter(de, entry.suggestedWinNTPropertyName, p, entry.propertyName);
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
            try
            {
                Debug.Assert(p != null);
                Debug.Assert(p.UnderlyingObject != null);
                Debug.Assert(p.UnderlyingObject is DirectoryEntry);

                DirectoryEntry de = (DirectoryEntry)p.UnderlyingObject;
                Debug.Assert(de != null);

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "Entering Load, type={0}, path={1}", p.GetType(), de.Path);

                // The list of all the SAM attributes in the DirectoryEntry
                ICollection samAttributes = de.Properties.PropertyNames;

                // Determine the mapping table to use, based on the principal type
                Type principalType = p.GetType();
                Hashtable propertyMappingTableByWinNT;

                if (principalType == typeof(UserPrincipal))
                {
                    propertyMappingTableByWinNT = s_userPropertyMappingTableByWinNT;
                }
                else if (principalType == typeof(GroupPrincipal))
                {
                    propertyMappingTableByWinNT = s_groupPropertyMappingTableByWinNT;
                }
                else
                {
                    Debug.Assert(principalType == typeof(ComputerPrincipal));
                    propertyMappingTableByWinNT = s_computerPropertyMappingTableByWinNT;
                }

                // Map each SAM attribute into the Principal in turn
                foreach (string samAttribute in samAttributes)
                {
                    ArrayList entries = (ArrayList)propertyMappingTableByWinNT[samAttribute.ToLowerInvariant()];

                    // If it's not in the table, it's not an SAM attribute we care about
                    if (entries == null)
                        continue;

                    // Load it into the Principal.  Some LDAP attributes (such as userAccountControl)
                    // map to more than one Principal property.
                    foreach (PropertyMappingTableEntry entry in entries)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "Load: loading {0}", entry.propertyName);
                        entry.winNTToPapiConverter(de, entry.suggestedWinNTPropertyName, p, entry.propertyName);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
        }

        // Performs store-specific resolution of an IdentityReference to a Principal
        // corresponding to the IdentityReference.  Returns null if no matching object found.
        // principalType can be used to scope the search to principals of a specified type, e.g., users or groups.
        // Specify typeof(Principal) to search all principal types.
        internal override Principal FindPrincipalByIdentRef(
                                    Type principalType, string urnScheme, string urnValue, DateTime referenceDate)
        {
            // Perform the appropriate action based on the type of the UrnScheme
            if (urnScheme == UrnScheme.SidScheme)
            {
                // Get the SID from the UrnValue
                SecurityIdentifier sidObj = new SecurityIdentifier(urnValue);
                byte[] sid = new byte[sidObj.BinaryLength];
                sidObj.GetBinaryForm(sid, 0);

                if (sid == null)
                    throw new ArgumentException(SR.StoreCtxSecurityIdentityClaimBadFormat);

                // If they're searching by SID for a SID corresponding to a fake group, construct
                // and return the fake group
                IntPtr pSid = IntPtr.Zero;

                try
                {
                    pSid = Utils.ConvertByteArrayToIntPtr(sid);

                    if (Interop.Advapi32.IsValidSid(pSid) && (Utils.ClassifySID(pSid) == SidType.FakeObject))
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                                "SAMStoreCtx",
                                                "FindPrincipalByIdentRef: fake principal {0}",
                                                sidObj.ToString());

                        return ConstructFakePrincipalFromSID(sid);
                    }
                }
                finally
                {
                    if (pSid != IntPtr.Zero)
                        Marshal.FreeHGlobal(pSid);
                }

                // Not a fake group.  Search for the real group.
                object o = FindNativeBySIDIdentRef(principalType, sid);
                return (o != null) ? GetAsPrincipal(o, null) : null;
            }
            else if (urnScheme == UrnScheme.SamAccountScheme || urnScheme == UrnScheme.NameScheme)
            {
                object o = FindNativeByNT4IdentRef(principalType, urnValue);
                return (o != null) ? GetAsPrincipal(o, null) : null;
            }
            else if (urnScheme == null)
            {
                object sidPrincipal = null;
                object nt4Principal = null;

                //
                // Try UrnValue as a SID IdentityClaim
                //

                // Get the SID from the UrnValue
                byte[] sid = null;

                try
                {
                    SecurityIdentifier sidObj = new SecurityIdentifier(urnValue);
                    sid = new byte[sidObj.BinaryLength];
                    sidObj.GetBinaryForm(sid, 0);
                }
                catch (ArgumentException)
                {
                    // must not have been a valid sid claim ignore it.
                }

                // If null, must have been a non-SID UrnValue.  Ignore it, and
                // continue on to try NT4 Account IdentityClaim.
                if (sid != null)
                {
                    // Are they perhaps searching for a fake group?
                    // If they passed in a valid SID for a fake group, construct and return the fake
                    // group.
                    if (principalType == typeof(Principal) || principalType == typeof(GroupPrincipal) || principalType.IsSubclassOf(typeof(GroupPrincipal)))
                    {
                        // They passed in a hex string, is it a valid SID, and if so, does it correspond to a fake
                        // principal?
                        IntPtr pSid = IntPtr.Zero;

                        try
                        {
                            pSid = Utils.ConvertByteArrayToIntPtr(sid);

                            if (Interop.Advapi32.IsValidSid(pSid) && (Utils.ClassifySID(pSid) == SidType.FakeObject))
                            {
                                GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                                        "SAMStoreCtx",
                                                        "FindPrincipalByIdentRef: fake principal {0} (scheme==null)",
                                                        Utils.ByteArrayToString(sid));

                                return ConstructFakePrincipalFromSID(sid);
                            }
                        }
                        finally
                        {
                            if (pSid != IntPtr.Zero)
                                Marshal.FreeHGlobal(pSid);
                        }
                    }

                    sidPrincipal = FindNativeBySIDIdentRef(principalType, sid);
                }

                //
                // Try UrnValue as a NT4 IdentityClaim
                //

                try
                {
                    nt4Principal = FindNativeByNT4IdentRef(principalType, urnValue);
                }
                catch (ArgumentException)
                {
                    // Must have been a non-NT4 Account UrnValue.  Ignore it.
                }

                GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                        "SAMStoreCtx",
                                        "FindPrincipalByIdentRef: scheme==null, found nt4={0}, found sid={1}",
                                        (nt4Principal != null),
                                        (sidPrincipal != null));

                // If they both succeeded in finding a match, we have too many matches.
                // Throw an exception.
                if ((sidPrincipal != null) && (nt4Principal != null))
                    throw new MultipleMatchesException(SR.MultipleMatchingPrincipals);

                // Return whichever one matched.  If neither matched, this will return null.
                return (sidPrincipal != null) ? GetAsPrincipal(sidPrincipal, null) :
                            ((nt4Principal != null) ? GetAsPrincipal(nt4Principal, null) :
                                null);
            }
            else
            {
                // Unsupported type of IdentityClaim
                throw new ArgumentException(SR.StoreCtxUnsupportedIdentityClaimForQuery);
            }
        }

        private object FindNativeBySIDIdentRef(Type principalType, byte[] sid)
        {
            // We can't lookup directly by SID, so we transform the SID --> SAM account name,
            // and do a lookup by that.

            string samUrnValue;

            string name;
            string domainName;

            // Map the SID to a machine and account name
            // If this fails, there's no match
            int err = Utils.LookupSid(this.MachineUserSuppliedName, _credentials, sid, out name, out domainName, out _);

            if (err != 0)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Error,
                                        "SAMStoreCtx",
                                        "FindNativeBySIDIdentRef:LookupSid on {0} failed, err={1}",
                                        this.MachineUserSuppliedName,
                                        err);
                return null;
            }

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "FindNativeBySIDIdentRef: mapped to {0}\\{1}", domainName, name);

            // Fixup the domainName for BUILTIN principals
            if (Utils.ClassifySID(sid) == SidType.RealObjectFakeDomain)
            {
                // BUILTIN principal ---> Issuer is actually our machine, not "BUILTIN" domain.
                domainName = this.MachineFlatName;
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "FindNativeBySIDIdentRef: using {0} for domainName", domainName);
            }

            // Valid SID, but not for our context (machine).  No match.
            if (!string.Equals(domainName, this.MachineFlatName, StringComparison.OrdinalIgnoreCase))
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "SAMStoreCtx", "FindNativeBySIDIdentRef: {0} != {1}, no match", domainName, this.MachineFlatName);
                return null;
            }

            // Build the NT4 UrnValue
            samUrnValue = domainName + "\\" + name;

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "FindNativeBySIDIdentRef: searching for {0}", samUrnValue);

            return FindNativeByNT4IdentRef(principalType, samUrnValue);
        }

        private object FindNativeByNT4IdentRef(Type principalType, string urnValue)
        {
            // Extract the SAM account name from the UrnValue.
            // We'll accept both "host\user" and "user".
            int index = urnValue.IndexOf('\\');

            if (index == urnValue.Length - 1)
                throw new ArgumentException(SR.StoreCtxNT4IdentityClaimWrongForm);

            string samAccountName = (index != -1) ? urnValue.Substring(index + 1) :    // +1 to skip the '/'
                                                     urnValue;

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "FindNativeByNT4IdentRef: searching for {0}", samAccountName);

            // If they specified a specific type of principal, use that as a hint to speed up
            // the lookup.
            string principalHint = "";

            if (principalType == typeof(UserPrincipal) || principalType.IsSubclassOf(typeof(UserPrincipal)))
            {
                principalHint = ",user";
                principalType = typeof(UserPrincipal);
            }
            else if (principalType == typeof(GroupPrincipal) || principalType.IsSubclassOf(typeof(GroupPrincipal)))
            {
                principalHint = ",group";
                principalType = typeof(GroupPrincipal);
            }
            else if (principalType == typeof(ComputerPrincipal) || principalType.IsSubclassOf(typeof(ComputerPrincipal)))
            {
                principalHint = ",computer";
                principalType = typeof(ComputerPrincipal);
            }

            // Retrieve the object from SAM
            string adsPath = "WinNT://" + this.MachineUserSuppliedName + "/" + samAccountName + principalHint;

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "FindNativeByNT4IdentRef: adsPath={0}", adsPath);

            DirectoryEntry de = SDSUtils.BuildDirectoryEntry(_credentials, _authTypes);

            try
            {
                de.Path = adsPath;

                // If it has no SID, it's not a security principal, and we're not interested in it.
                // This also has the side effect of forcing the object to load, thereby testing
                // whether or not it exists.
                if (de.Properties["objectSid"] == null || de.Properties["objectSid"].Count == 0)
                    return false;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Error,
                                        "SAMStoreCtx",
                                        "FindNativeByNT4IdentRef: caught COMException, message={0}, code={1}",
                                        e.Message,
                                        e.ErrorCode);

                // Failed to find it
                if ((e.ErrorCode == unchecked((int)0x800708AB)) ||     // unknown user
                     (e.ErrorCode == unchecked((int)0x800708AC)) ||     // unknown group
                     (e.ErrorCode == unchecked((int)0x80070035)) ||     // bad net path
                     (e.ErrorCode == unchecked((int)0x800708AD)))
                {
                    return null;
                }

                // Unknown error, don't suppress it
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }

            // Make sure it's of the correct type
            bool fMatch = false;

            if ((principalType == typeof(UserPrincipal)) && SAMUtils.IsOfObjectClass(de, "User"))
                fMatch = true;
            else if ((principalType == typeof(GroupPrincipal)) && SAMUtils.IsOfObjectClass(de, "Group"))
                fMatch = true;
            else if ((principalType == typeof(ComputerPrincipal)) && SAMUtils.IsOfObjectClass(de, "Computer"))
                fMatch = true;
            else if ((principalType == typeof(AuthenticablePrincipal)) &&
                      (SAMUtils.IsOfObjectClass(de, "User") || SAMUtils.IsOfObjectClass(de, "Computer")))
                fMatch = true;
            else
            {
                Debug.Assert(principalType == typeof(Principal));

                if (SAMUtils.IsOfObjectClass(de, "User") || SAMUtils.IsOfObjectClass(de, "Group") || SAMUtils.IsOfObjectClass(de, "Computer"))
                    fMatch = true;
            }

            if (fMatch)
                return de;

            return null;
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
        // property that's not listed here, or is listed here but not for the correct principal type,
        // when writing to SAM, we throw an exception.
        private static readonly object[,] s_propertyMappingTableRaw =
        {
            // PropertyName                          Principal Type     SAM property            Converter(WinNT->PAPI)                                    Converter(PAPI->WinNT)
            {PropertyNames.PrincipalDisplayName,     typeof(UserPrincipal),      "FullName",             new FromWinNTConverterDelegate(StringFromWinNTConverter),  new ToWinNTConverterDelegate(StringToWinNTConverter)},
            {PropertyNames.PrincipalDescription,     typeof(UserPrincipal),      "Description",          new FromWinNTConverterDelegate(StringFromWinNTConverter),  new ToWinNTConverterDelegate(StringToWinNTConverter)},
            {PropertyNames.PrincipalDescription,     typeof(GroupPrincipal),     "Description",          new FromWinNTConverterDelegate(StringFromWinNTConverter),  new ToWinNTConverterDelegate(StringToWinNTConverter)},
            {PropertyNames.PrincipalSamAccountName,                  typeof(Principal), "Name",            new FromWinNTConverterDelegate(SamAccountNameFromWinNTConverter),  null},
            {PropertyNames.PrincipalSid,                        typeof(Principal), "objectSid",            new FromWinNTConverterDelegate(SidFromWinNTConverter), null },
            {PropertyNames.PrincipalDistinguishedName, typeof(UserPrincipal), null,          null,  new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},
            {PropertyNames.PrincipalGuid,                      typeof(UserPrincipal), null,         null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},
            {PropertyNames.PrincipalUserPrincipalName,  typeof(UserPrincipal), null,         null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},
            // Name and SamAccountNAme properties are currently routed to the same underlying Name property.
            {PropertyNames.PrincipalName,                  typeof(Principal), "Name",            new FromWinNTConverterDelegate(SamAccountNameFromWinNTConverter),  null},



            //
            {PropertyNames.AuthenticablePrincipalEnabled,     typeof(UserPrincipal),  "UserFlags",     new FromWinNTConverterDelegate(UserFlagsFromWinNTConverter),  new ToWinNTConverterDelegate(UserFlagsToWinNTConverter)},
            {PropertyNames.AuthenticablePrincipalCertificates, typeof(UserPrincipal),  "*******",       new FromWinNTConverterDelegate(CertFromWinNTConverter),       new ToWinNTConverterDelegate(CertToWinNT)},

            //
            {PropertyNames.GroupIsSecurityGroup,     typeof(GroupPrincipal), "*******",    new FromWinNTConverterDelegate(GroupTypeFromWinNTConverter), new ToWinNTConverterDelegate(GroupTypeToWinNTConverter)},
            {PropertyNames.GroupGroupScope,  typeof(GroupPrincipal), "groupType",  new FromWinNTConverterDelegate(GroupTypeFromWinNTConverter), new ToWinNTConverterDelegate(GroupTypeToWinNTConverter)},

            //
            {PropertyNames.UserEmailAddress, typeof(UserPrincipal),  "*******",  new FromWinNTConverterDelegate(EmailFromWinNTConverter),    new ToWinNTConverterDelegate(EmailToWinNTConverter)},

            //
            {PropertyNames.AcctInfoLastLogon,             typeof(UserPrincipal), "LastLogin",              new FromWinNTConverterDelegate(DateFromWinNTConverter),            null},
            {PropertyNames.AcctInfoPermittedWorkstations, typeof(UserPrincipal), "LoginWorkstations",      new FromWinNTConverterDelegate(MultiStringFromWinNTConverter),     new ToWinNTConverterDelegate(MultiStringToWinNTConverter)},
            {PropertyNames.AcctInfoPermittedLogonTimes,   typeof(UserPrincipal), "LoginHours",             new FromWinNTConverterDelegate(BinaryFromWinNTConverter),          new ToWinNTConverterDelegate(LogonHoursToWinNTConverter)},
            {PropertyNames.AcctInfoExpirationDate,        typeof(UserPrincipal), "AccountExpirationDate",  new FromWinNTConverterDelegate(DateFromWinNTConverter),            new ToWinNTConverterDelegate(AcctExpirDateToNTConverter)},
            {PropertyNames.AcctInfoSmartcardRequired,     typeof(UserPrincipal), "UserFlags",              new FromWinNTConverterDelegate(UserFlagsFromWinNTConverter),       new ToWinNTConverterDelegate(UserFlagsToWinNTConverter)},
            {PropertyNames.AcctInfoDelegationPermitted,   typeof(UserPrincipal), "UserFlags",              new FromWinNTConverterDelegate(UserFlagsFromWinNTConverter),       new ToWinNTConverterDelegate(UserFlagsToWinNTConverter)},
            {PropertyNames.AcctInfoBadLogonCount,         typeof(UserPrincipal), "BadPasswordAttempts",    new FromWinNTConverterDelegate(IntFromWinNTConverter),             null},
            {PropertyNames.AcctInfoHomeDirectory,         typeof(UserPrincipal), "HomeDirectory",          new FromWinNTConverterDelegate(StringFromWinNTConverter),          new ToWinNTConverterDelegate(StringToWinNTConverter)},
            {PropertyNames.AcctInfoHomeDrive,             typeof(UserPrincipal), "HomeDirDrive",           new FromWinNTConverterDelegate(StringFromWinNTConverter),          new ToWinNTConverterDelegate(StringToWinNTConverter)},
            {PropertyNames.AcctInfoScriptPath,            typeof(UserPrincipal), "LoginScript",            new FromWinNTConverterDelegate(StringFromWinNTConverter),          new ToWinNTConverterDelegate(StringToWinNTConverter)},

            //
            {PropertyNames.PwdInfoLastPasswordSet,                   typeof(UserPrincipal),  "PasswordAge",   new FromWinNTConverterDelegate(ElapsedTimeFromWinNTConverter),       null},
            {PropertyNames.PwdInfoLastBadPasswordAttempt,            typeof(UserPrincipal),  "*******",       new FromWinNTConverterDelegate(LastBadPwdAttemptFromWinNTConverter), null},
            {PropertyNames.PwdInfoPasswordNotRequired,               typeof(UserPrincipal),  "UserFlags",     new FromWinNTConverterDelegate(UserFlagsFromWinNTConverter),    new ToWinNTConverterDelegate(UserFlagsToWinNTConverter)},
            {PropertyNames.PwdInfoPasswordNeverExpires,              typeof(UserPrincipal),  "UserFlags",     new FromWinNTConverterDelegate(UserFlagsFromWinNTConverter),    new ToWinNTConverterDelegate(UserFlagsToWinNTConverter)},
            {PropertyNames.PwdInfoCannotChangePassword,              typeof(UserPrincipal),  "UserFlags",     new FromWinNTConverterDelegate(UserFlagsFromWinNTConverter),    new ToWinNTConverterDelegate(UserFlagsToWinNTConverter)},
            {PropertyNames.PwdInfoAllowReversiblePasswordEncryption, typeof(UserPrincipal),  "UserFlags",     new FromWinNTConverterDelegate(UserFlagsFromWinNTConverter),    new ToWinNTConverterDelegate(UserFlagsToWinNTConverter)},

            //
            // Writable properties we don't support writing in SAM
            //
            {PropertyNames.UserGivenName,            typeof(UserPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},
            {PropertyNames.UserMiddleName,           typeof(UserPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},
            {PropertyNames.UserSurname,              typeof(UserPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},
            {PropertyNames.UserVoiceTelephoneNumber, typeof(UserPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},
            {PropertyNames.UserEmployeeID,           typeof(UserPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},

            {PropertyNames.PrincipalDisplayName,     typeof(GroupPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},
            {PropertyNames.PrincipalDisplayName,     typeof(ComputerPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},

            {PropertyNames.PrincipalDescription,     typeof(ComputerPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},

            {PropertyNames.AuthenticablePrincipalEnabled,        typeof(ComputerPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},

            {PropertyNames.AuthenticablePrincipalCertificates,   typeof(ComputerPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},

            {PropertyNames.ComputerServicePrincipalNames,        typeof(ComputerPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},

            {PropertyNames.AcctInfoPermittedWorkstations, typeof(ComputerPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},
            {PropertyNames.AcctInfoPermittedLogonTimes,   typeof(ComputerPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},
            {PropertyNames.AcctInfoExpirationDate,        typeof(ComputerPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},
            {PropertyNames.AcctInfoSmartcardRequired,     typeof(ComputerPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},
            {PropertyNames.AcctInfoDelegationPermitted,   typeof(ComputerPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},
            {PropertyNames.AcctInfoHomeDirectory,         typeof(ComputerPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},
            {PropertyNames.AcctInfoHomeDrive,             typeof(ComputerPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},
            {PropertyNames.AcctInfoScriptPath,            typeof(ComputerPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},

            {PropertyNames.PwdInfoPasswordNotRequired,               typeof(ComputerPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},
            {PropertyNames.PwdInfoPasswordNeverExpires,              typeof(ComputerPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},
            {PropertyNames.PwdInfoCannotChangePassword,              typeof(ComputerPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)},
            {PropertyNames.PwdInfoAllowReversiblePasswordEncryption, typeof(ComputerPrincipal),       null,   null,   new ToWinNTConverterDelegate(ExceptionToWinNTConverter)}
        };

        private static readonly Hashtable s_userPropertyMappingTableByProperty;
        private static readonly Hashtable s_userPropertyMappingTableByWinNT;

        private static readonly Hashtable s_groupPropertyMappingTableByProperty;
        private static readonly Hashtable s_groupPropertyMappingTableByWinNT;

        private static readonly Hashtable s_computerPropertyMappingTableByProperty;
        private static readonly Hashtable s_computerPropertyMappingTableByWinNT;

        private static readonly Dictionary<string, ObjectMask> s_validPropertyMap;
        private static readonly Dictionary<Type, ObjectMask> s_maskMap;

        [Flags]
        private enum ObjectMask
        {
            None = 0,
            User = 0x1,
            Computer = 0x2,
            Group = 0x4,
            Principal = User | Computer | Group
        }

        private sealed class PropertyMappingTableEntry
        {
            internal string propertyName;                  // PAPI name
            internal string suggestedWinNTPropertyName;    // WinNT attribute name
            internal FromWinNTConverterDelegate winNTToPapiConverter;
            internal ToWinNTConverterDelegate papiToWinNTConverter;
        }

        //
        // Conversion routines
        //

        // Loads the specified attribute of the DirectoryEntry into the specified property of the Principal
        private delegate void FromWinNTConverterDelegate(DirectoryEntry de, string suggestedWinNTProperty, Principal p, string propertyName);

        // Loads the specified property of the Principal into the specified attribute of the DirectoryEntry.
        // For multivalued attributes, must test to make sure the value hasn't already been loaded into the DirectoryEntry
        // (to maintain idempotency when PushChangesToNative is called multiple times).
        private delegate void ToWinNTConverterDelegate(Principal p, string propertyName, DirectoryEntry de, string suggestedWinNTProperty, bool isLSAM);

        //
        // WinNT --> PAPI
        //
        private static void StringFromWinNTConverter(DirectoryEntry de, string suggestedWinNTProperty, Principal p, string propertyName)
        {
            PropertyValueCollection values = de.Properties[suggestedWinNTProperty];

            // The WinNT provider represents some string attributes that haven't been set as an empty string.
            // Map them to null (not set), to maintain consistency with the LDAP context.
            if ((values.Count > 0) && (((string)values[0]).Length == 0))
                return;

            SDSUtils.SingleScalarFromDirectoryEntry<string>(new dSPropertyCollection(de.Properties), suggestedWinNTProperty, p, propertyName);
        }

        private static void SidFromWinNTConverter(DirectoryEntry de, string suggestedWinNTProperty, Principal p, string propertyName)
        {
            byte[] sid = (byte[])de.Properties["objectSid"][0];

            string sddlSid = Utils.ConvertSidToSDDL(sid);
            SecurityIdentifier SidObj = new SecurityIdentifier(sddlSid);
            p.LoadValueIntoProperty(propertyName, (object)SidObj);
        }

        private static void SamAccountNameFromWinNTConverter(DirectoryEntry de, string suggestedWinNTProperty, Principal p, string propertyName)
        {
            Debug.Assert(de.Properties["Name"].Count == 1);

            string samAccountName = (string)de.Properties["Name"][0];
            //                string urnValue = p.Context.ConnectedServer + @"\" + samAccountName;

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "SamAccountNameFromWinNTConverter: loading SAM{0}", samAccountName);
            p.LoadValueIntoProperty(propertyName, (object)samAccountName);
        }

        private static void MultiStringFromWinNTConverter(DirectoryEntry de, string suggestedWinNTProperty, Principal p, string propertyName)
        {
            // ValueCollection<string> is Load'ed from a List<string>
            SDSUtils.MultiScalarFromDirectoryEntry<string>(new dSPropertyCollection(de.Properties), suggestedWinNTProperty, p, propertyName);
        }

        private static void IntFromWinNTConverter(DirectoryEntry de, string suggestedWinNTProperty, Principal p, string propertyName)
        {
            SDSUtils.SingleScalarFromDirectoryEntry<int>(new dSPropertyCollection(de.Properties), suggestedWinNTProperty, p, propertyName);
        }

        private static void BinaryFromWinNTConverter(DirectoryEntry de, string suggestedWinNTProperty, Principal p, string propertyName)
        {
            SDSUtils.SingleScalarFromDirectoryEntry<byte[]>(new dSPropertyCollection(de.Properties), suggestedWinNTProperty, p, propertyName);
        }

        private static void CertFromWinNTConverter(DirectoryEntry de, string suggestedWinNTProperty, Principal p, string propertyName)
        {
        }

        private static void GroupTypeFromWinNTConverter(DirectoryEntry de, string suggestedWinNTProperty, Principal p, string propertyName)
        {
            // Groups are always enabled in SAM.  There is no such thing as a distribution group.
            p.LoadValueIntoProperty(PropertyNames.GroupIsSecurityGroup, (object)true);

            if (propertyName == PropertyNames.GroupIsSecurityGroup)
            {
                // Do nothing for this property.
            }
            else
            {
                Debug.Assert(propertyName == PropertyNames.GroupGroupScope);

                // All local machine SAM groups are local
#if DEBUG
                PropertyValueCollection values = de.Properties[suggestedWinNTProperty];
                if (values.Count != 0)
                    Debug.Assert(((int)values[0]) == 4);
#endif

                p.LoadValueIntoProperty(propertyName, GroupScope.Local);
            }
        }

        private static void EmailFromWinNTConverter(DirectoryEntry de, string suggestedWinNTProperty, Principal p, string propertyName)
        {
        }

        private static void LastBadPwdAttemptFromWinNTConverter(DirectoryEntry de, string suggestedWinNTProperty, Principal p, string propertyName)
        {
        }

        private static void ElapsedTimeFromWinNTConverter(DirectoryEntry de, string suggestedWinNTProperty, Principal p, string propertyName)
        {
            // These properties are expressed as "seconds passed since the event of interest".  So to convert
            // to a DateTime, we subtract them from DateTime.UtcNow.

            PropertyValueCollection values = de.Properties[suggestedWinNTProperty];

            if (values.Count != 0)
            {
                // We're intended to handle single-valued scalar properties
                Debug.Assert(values.Count == 1);
                Debug.Assert(values[0] is int);

                int secondsLapsed = (int)values[0];

                Nullable<DateTime> dt = DateTime.UtcNow - new TimeSpan(0, 0, secondsLapsed);

                p.LoadValueIntoProperty(propertyName, dt);
            }
        }

        private static void DateFromWinNTConverter(DirectoryEntry de, string suggestedWinNTProperty, Principal p, string propertyName)
        {
            PropertyValueCollection values = de.Properties[suggestedWinNTProperty];

            if (values.Count != 0)
            {
                Debug.Assert(values.Count == 1);

                // Unlike with the ADSI LDAP provider, these come back as a DateTime (expressed in UTC), instead
                // of a IADsLargeInteger.
                Nullable<DateTime> dt = (Nullable<DateTime>)values[0];

                p.LoadValueIntoProperty(propertyName, dt);
            }
        }

        private static void UserFlagsFromWinNTConverter(DirectoryEntry de, string suggestedWinNTProperty, Principal p, string propertyName)
        {
            Debug.Assert(string.Equals(suggestedWinNTProperty, "UserFlags", StringComparison.OrdinalIgnoreCase));

            SDSUtils.AccountControlFromDirectoryEntry(new dSPropertyCollection(de.Properties), suggestedWinNTProperty, p, propertyName, true);
        }

        //
        // PAPI --> WinNT
        //

        private static void ExceptionToWinNTConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedWinNTProperty, bool isLSAM)
        {
            throw new InvalidOperationException(
                            SR.Format(SR.PrincipalUnsupportPropertyForType,
                                      p.GetType(),
                                      PropertyNamesExternal.GetExternalForm(propertyName)));
        }

        private static void StringToWinNTConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedWinNTProperty, bool isLSAM)
        {
            string value = (string)p.GetValueForProperty(propertyName);

            if (p.unpersisted && value == null)
                return;

            if (value != null && value.Length > 0)
                de.Properties[suggestedWinNTProperty].Value = value;
            else
                de.Properties[suggestedWinNTProperty].Value = "";
        }

        private static void MultiStringToWinNTConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedWinNTProperty, bool isLSAM)
        {
            SDSUtils.MultiStringToDirectoryEntryConverter(p, propertyName, de, suggestedWinNTProperty);
        }

        private static void LogonHoursToWinNTConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedWinNTProperty, bool isLSAM)
        {
            Debug.Assert(propertyName == PropertyNames.AcctInfoPermittedLogonTimes);

            byte[] value = (byte[])p.GetValueForProperty(propertyName);

            if (p.unpersisted && value == null)
                return;

            if (value != null && value.Length != 0)
            {
                de.Properties[suggestedWinNTProperty].Value = value;
            }
            else
            {
                byte[] allHours = new byte[]{0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                                             0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                                             0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                                             0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                                             0xff};

                de.Properties[suggestedWinNTProperty].Value = allHours;
            }
        }

        private static void CertToWinNT(Principal p, string propertyName, DirectoryEntry de, string suggestedWinNTProperty, bool isLSAM)
        {
            if (!isLSAM)
                throw new InvalidOperationException(
                    SR.Format(SR.PrincipalUnsupportPropertyForPlatform, PropertyNamesExternal.GetExternalForm(propertyName)));
        }

        private static void GroupTypeToWinNTConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedWinNTProperty, bool isLSAM)
        {
            if (propertyName == PropertyNames.GroupIsSecurityGroup)
            {
                if (!isLSAM)
                    throw new InvalidOperationException(
                        SR.Format(SR.PrincipalUnsupportPropertyForPlatform, PropertyNamesExternal.GetExternalForm(propertyName)));
            }
            else
            {
                Debug.Assert(propertyName == PropertyNames.GroupGroupScope);

                // local machine SAM only supports local groups
                GroupScope value = (GroupScope)p.GetValueForProperty(propertyName);

                if (value != GroupScope.Local)
                    throw new InvalidOperationException(SR.SAMStoreCtxLocalGroupsOnly);
            }
        }

        private static void EmailToWinNTConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedWinNTProperty, bool isLSAM)
        {
            if (!isLSAM)
                throw new InvalidOperationException(
                    SR.Format(SR.PrincipalUnsupportPropertyForPlatform, PropertyNamesExternal.GetExternalForm(propertyName)));
        }

        private static void AcctExpirDateToNTConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedWinNTProperty, bool isLSAM)
        {
            Nullable<DateTime> value = (Nullable<DateTime>)p.GetValueForProperty(propertyName);

            if (p.unpersisted && value == null)
                return;

            // ADSI's WinNT provider uses 1/1/1970 to represent "never expires" when setting the property
            if (value.HasValue)
                de.Properties[suggestedWinNTProperty].Value = (DateTime)value;
            else
                de.Properties[suggestedWinNTProperty].Value = new DateTime(1970, 1, 1);
        }

        private static void UserFlagsToWinNTConverter(Principal p, string propertyName, DirectoryEntry de, string suggestedWinNTProperty, bool isLSAM)
        {
            Debug.Assert(string.Equals(suggestedWinNTProperty, "UserFlags", StringComparison.OrdinalIgnoreCase));

            SDSUtils.AccountControlToDirectoryEntry(p, propertyName, de, suggestedWinNTProperty, true, p.unpersisted);
        }

        private static void UpdateGroupMembership(Principal group, DirectoryEntry de, NetCred credentials, AuthenticationTypes authTypes)
        {
            Debug.Assert(group.fakePrincipal == false);

            PrincipalCollection members = (PrincipalCollection)group.GetValueForProperty(PropertyNames.GroupMembers);

            UnsafeNativeMethods.IADsGroup iADsGroup = (UnsafeNativeMethods.IADsGroup)de.NativeObject;

            try
            {
                //
                // Process clear
                //
                if (members.Cleared)
                {
                    // Unfortunately, there's no quick way to clear a group's membership in SAM.
                    // So we remove each member in turn, by enumerating over the group membership
                    // and calling remove on each.

                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "UpdateGroupMembership: clearing {0}", de.Path);

                    // Prepare the COM Interopt enumeration
                    UnsafeNativeMethods.IADsMembers iADsMembers = iADsGroup.Members();
                    IEnumVARIANT enumerator = (IEnumVARIANT)iADsMembers._NewEnum;

                    object[] nativeMembers = new object[1];
                    int hr;

                    do
                    {
                        hr = enumerator.Next(1, nativeMembers, IntPtr.Zero);

                        if (hr == 0) // S_OK
                        {
                            // Found a member, remove it.
                            UnsafeNativeMethods.IADs iADs = (UnsafeNativeMethods.IADs)nativeMembers[0];
                            iADsGroup.Remove(iADs.ADsPath);
                        }
                    }
                    while (hr == 0);

                    // Either hr == S_FALSE (1), which means we completed the enumerator,
                    // or we encountered an error
                    if (hr != 1)
                    {
                        // Error occurred.
                        GlobalDebug.WriteLineIf(GlobalDebug.Warn, "SAMStoreCtx", "UpdateGroupMembership: error while clearing, hr={0}", hr);

                        throw new PrincipalOperationException(
                                        SR.Format(
                                                SR.SAMStoreCtxFailedToClearGroup,
                                                hr.ToString(CultureInfo.InvariantCulture)));
                    }
                }

                //
                // Process inserted members
                //
                List<Principal> insertedMembers = members.Inserted;

                // First, validate the members to be added
                foreach (Principal member in insertedMembers)
                {
                    Type memberType = member.GetType();

                    if ((memberType != typeof(UserPrincipal)) && (!memberType.IsSubclassOf(typeof(UserPrincipal))) &&
                         (memberType != typeof(ComputerPrincipal)) && (!memberType.IsSubclassOf(typeof(ComputerPrincipal))) &&
                         (memberType != typeof(GroupPrincipal)) && (!memberType.IsSubclassOf(typeof(GroupPrincipal))))
                    {
                        throw new InvalidOperationException(
                                        SR.Format(SR.StoreCtxUnsupportedPrincipalTypeForGroupInsert, memberType));
                    }

                    // Can't inserted unpersisted principal
                    if (member.unpersisted)
                        throw new InvalidOperationException(SR.StoreCtxGroupHasUnpersistedInsertedPrincipal);

                    Debug.Assert(member.Context != null);

                    // There's no restriction on the type of principal to be inserted (AD/reg-SAM/MSAM), but it needs
                    // to have a SID IdentityClaim.  We'll check that as we go.
                }

                // Now add each member to the group
                foreach (Principal member in insertedMembers)
                {
                    // We'll add the member via its SID.  This works for both MSAM members and AD members.

                    // Build a SID ADsPath
                    string memberSidPath = GetSidADsPathFromPrincipal(member);

                    if (memberSidPath == null)
                        throw new InvalidOperationException(SR.SAMStoreCtxCouldntGetSIDForGroupMember);

                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "UpdateGroupMembership: inserting {0}", memberSidPath);

                    // Add the member to the group
                    iADsGroup.Add(memberSidPath);
                }

                //
                // Process removed members
                //
                List<Principal> removedMembers = members.Removed;

                foreach (Principal member in removedMembers)
                {
                    // Since we don't allow any of these to be inserted, none of them should ever
                    // show up in the removal list
                    Debug.Assert(member.unpersisted == false);

                    // If the collection was cleared, there should be no original members to remove
                    Debug.Assert(members.Cleared == false);

                    // Like insertion, we'll remove by SID.

                    // Build a SID ADsPath
                    string memberSidPath = GetSidADsPathFromPrincipal(member);

                    if (memberSidPath == null)
                        throw new InvalidOperationException(SR.SAMStoreCtxCouldntGetSIDForGroupMember);

                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "UpdateGroupMembership: removing {0}", memberSidPath);

                    // Remove the member from the group
                    iADsGroup.Remove(memberSidPath);
                }
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Error,
                                        "SAMStoreCtx",
                                        "UpdateGroupMembership: caught COMException, message={0}, code={1}",
                                        e.Message,
                                        e.ErrorCode);

                // ADSI threw an exception trying to update the group membership
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
        }

        private static string GetSidADsPathFromPrincipal(Principal p)
        {
            Debug.Assert(p.unpersisted == false);

            // Get the member's SID from its Security IdentityClaim

            SecurityIdentifier Sid = p.Sid;

            if (Sid == null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "SAMStoreCtx", "GetSidADsPathFromPrincipal: no SID");
                return null;
            }

            string sddlSid = Sid.ToString();

            if (sddlSid == null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn,
                                        "SAMStoreCtx",
                                        "GetSidADsPathFromPrincipal: Couldn't convert to SDDL ({0})",
                                        Sid.ToString());
                return null;
            }

            // Build a SID ADsPath
            return @"WinNT://" + sddlSid;
        }
    }
}
