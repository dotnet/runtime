// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;

namespace System.DirectoryServices.AccountManagement
{
    internal enum PrincipalAccessMask
    {
        ChangePassword
    }
    internal abstract class StoreCtx : IDisposable
    {
        //
        // StoreCtx information
        //

        // Retrieves the Path (ADsPath) of the object used as the base of the StoreCtx
        internal abstract string BasePath { get; }

        // The PrincipalContext object to which this StoreCtx belongs.  Initialized by PrincipalContext after it creates
        // this StoreCtx instance.
        private PrincipalContext _owningContext;
        internal PrincipalContext OwningContext
        {
            get
            {
                return _owningContext;
            }

            set
            {
                Debug.Assert(value != null);
                _owningContext = value;
            }
        }

        //
        // CRUD
        //

        // Used to perform the specified operation on the Principal.  They also make any needed security subsystem
        // calls to obtain digitial signatures (e..g, to sign the Principal Extension/GroupMember Relationship for
        // WinFS).
        //
        // Insert() and Update() must check to make sure no properties not supported by this StoreCtx
        // have been set, prior to persisting the Principal.
        internal abstract void Insert(Principal p);
        internal abstract void Update(Principal p);
        internal abstract void Delete(Principal p);
        internal abstract void Move(StoreCtx originalStore, Principal p);

        //
        // Native <--> Principal
        //

        // For modified object, pushes any changes (including IdentityClaim changes)
        // into the underlying store-specific object (e.g., DirectoryEntry) and returns the underlying object.
        // For unpersisted object, creates a  underlying object if one doesn't already exist (in
        // Principal.UnderlyingObject), then pushes any changes into the underlying object.
        internal abstract object PushChangesToNative(Principal p);

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
        internal abstract Principal GetAsPrincipal(object storeObject, object discriminant);

        // Loads the store values from p.UnderlyingObject into p, performing schema mapping as needed.
        internal abstract void Load(Principal p);
        // Loads only the psecified property into the principal object.  The object should have already been persisted or searched for this to happen.
        internal abstract void Load(Principal p, string principalPropertyName);

        // Performs store-specific resolution of an IdentityReference to a Principal
        // corresponding to the IdentityReference.  Returns null if no matching object found.
        // principalType can be used to scope the search to principals of a specified type, e.g., users or groups.
        // Specify typeof(Principal) to search all principal types.
        internal abstract Principal FindPrincipalByIdentRef(
                                    Type principalType, string urnScheme, string urnValue, DateTime referenceDate);

        // Returns a type indicating the type of object that would be returned as the wormhole for the specified
        // Principal.  For some StoreCtxs, this method may always return a constant (e.g., typeof(DirectoryEntry)
        // for ADStoreCtx).  For others, it may vary depending on the Principal passed in.
        internal abstract Type NativeType(Principal p);

        //
        // Special operations: the Principal classes delegate their implementation of many of the
        // special methods to their underlying StoreCtx
        //

        // methods for manipulating accounts
        internal abstract void InitializeUserAccountControl(AuthenticablePrincipal p);
        internal abstract bool IsLockedOut(AuthenticablePrincipal p);
        internal abstract void UnlockAccount(AuthenticablePrincipal p);

        // methods for manipulating passwords
        internal abstract void SetPassword(AuthenticablePrincipal p, string newPassword);
        internal abstract void ChangePassword(AuthenticablePrincipal p, string oldPassword, string newPassword);
        internal abstract void ExpirePassword(AuthenticablePrincipal p);
        internal abstract void UnexpirePassword(AuthenticablePrincipal p);

        internal abstract bool AccessCheck(Principal p, PrincipalAccessMask targetPermission);

        // the various FindBy* methods
        internal abstract ResultSet FindByLockoutTime(
            DateTime dt, MatchType matchType, Type principalType);
        internal abstract ResultSet FindByLogonTime(
            DateTime dt, MatchType matchType, Type principalType);
        internal abstract ResultSet FindByPasswordSetTime(
            DateTime dt, MatchType matchType, Type principalType);
        internal abstract ResultSet FindByBadPasswordAttempt(
            DateTime dt, MatchType matchType, Type principalType);
        internal abstract ResultSet FindByExpirationTime(
            DateTime dt, MatchType matchType, Type principalType);

        // Get groups of which p is a direct member
        internal abstract ResultSet GetGroupsMemberOf(Principal p);

        // Get groups from this ctx which contain a principal corresponding to foreignPrincipal
        // (which is a principal from foreignContext)
        internal abstract ResultSet GetGroupsMemberOf(Principal foreignPrincipal, StoreCtx foreignContext);

        // Get groups of which p is a member, using AuthZ S4U APIs for recursive membership
        internal abstract ResultSet GetGroupsMemberOfAZ(Principal p);

        // Get members of group g
        internal abstract BookmarkableResultSet GetGroupMembership(GroupPrincipal g, bool recursive);

        // Is p a member of g in the store?
        internal abstract bool SupportsNativeMembershipTest { get; }
        internal abstract bool IsMemberOfInStore(GroupPrincipal g, Principal p);

        // Can a Clear() operation be performed on the specified group?  If not, also returns
        // a string containing a human-readable explanation of why not, suitable for use in an exception.
        internal abstract bool CanGroupBeCleared(GroupPrincipal g, out string explanationForFailure);

        // Can the given member be removed from the specified group?  If not, also returns
        // a string containing a human-readable explanation of why not, suitable for use in an exception.
        internal abstract bool CanGroupMemberBeRemoved(GroupPrincipal g, Principal member, out string explanationForFailure);

        //
        // Query operations
        //

        // Returns true if this store has native support for search (and thus a wormhole).
        // Returns true for everything but SAM (both reg-SAM and MSAM).
        internal abstract bool SupportsSearchNatively { get; }

        // Returns a type indicating the type of object that would be returned as the wormhole for the specified
        // PrincipalSearcher.
        internal abstract Type SearcherNativeType();

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
        internal abstract object PushFilterToNativeSearcher(PrincipalSearcher ps);

        // The core query operation.
        // Given a PrincipalSearcher containg a query filter, transforms it into the store schema
        // and performs the query to get a collection of matching native objects (up to a maximum of sizeLimit,
        // or uses the sizelimit already set on the DirectorySearcher if sizeLimit == -1).
        // If the PrincipalSearcher does not have a query filter (PrincipalSearcher.QueryFilter == null),
        // matches all principals in the store.
        //
        // The collection may not be complete, i.e., paging - the returned ResultSet will automatically
        // page in additional results as needed.
        internal abstract ResultSet Query(PrincipalSearcher ps, int sizeLimit);

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
        internal abstract Principal ResolveCrossStoreRefToPrincipal(object o);

        //
        // Data Validation
        //

        // Validiate the passed property name to determine if it is valid for the store and Principal type.
        // used by the principal objects to determine if a property is valid in the property before
        // save is called.
        internal abstract bool IsValidProperty(Principal p, string propertyName);

        // Returns true if AccountInfo is supported for the specified principal, false otherwise.
        // Used when an application tries to access the AccountInfo property of a newly-inserted
        // (not yet persisted) AuthenticablePrincipal, to determine whether it should be allowed.
        internal abstract bool SupportsAccounts(AuthenticablePrincipal p);

        // Returns the set of credential types supported by this store for the specified principal.
        // Used when an application tries to access the PasswordInfo property of a newly-inserted
        // (not yet persisted) AuthenticablePrincipal, to determine whether it should be allowed.
        // Also used to implement AuthenticablePrincipal.SupportedCredentialTypes.
        internal abstract CredentialTypes SupportedCredTypes(AuthenticablePrincipal p);

        //
        // Construct a fake Principal to represent a well-known SID like
        // "\Everyone" or "NT AUTHORITY\NETWORK SERVICE"
        //
        internal abstract Principal ConstructFakePrincipalFromSID(byte[] sid);

        //
        // IDisposable implementation
        //

        // Disposes of this instance of a StoreCtx.  Calling this method more than once is allowed, and all but
        // the first call should be ignored.
        public virtual void Dispose()
        {
            // Nothing to do in the base class
        }

        //
        // QBE Filter parsing
        //

        // These property sets include only the properties used to build QBE filters,
        // e.g., the Group.Members property is not included

        internal static string[] principalProperties = new string[]
        {
            PropertyNames.PrincipalDisplayName,
            PropertyNames.PrincipalDescription,
            PropertyNames.PrincipalSamAccountName,
            PropertyNames.PrincipalUserPrincipalName,
            PropertyNames.PrincipalGuid,
            PropertyNames.PrincipalSid,
            PropertyNames.PrincipalStructuralObjectClass,
            PropertyNames.PrincipalName,
            PropertyNames.PrincipalDistinguishedName,
            PropertyNames.PrincipalExtensionCache
        };

        internal static string[] authenticablePrincipalProperties = new string[]
        {
            PropertyNames.AuthenticablePrincipalEnabled,
            PropertyNames.AuthenticablePrincipalCertificates,
            PropertyNames.PwdInfoLastBadPasswordAttempt,
            PropertyNames.AcctInfoExpirationDate,
            PropertyNames.AcctInfoExpiredAccount,
            PropertyNames.AcctInfoLastLogon,
            PropertyNames.AcctInfoAcctLockoutTime,
            PropertyNames.AcctInfoBadLogonCount,
            PropertyNames.PwdInfoLastPasswordSet
        };

        // includes AccountInfo and PasswordInfo
        internal static string[] userProperties = new string[]
        {
            PropertyNames.UserGivenName,
            PropertyNames.UserMiddleName,
            PropertyNames.UserSurname,
            PropertyNames.UserEmailAddress,
            PropertyNames.UserVoiceTelephoneNumber,
            PropertyNames.UserEmployeeID,

            PropertyNames.AcctInfoPermittedWorkstations,
            PropertyNames.AcctInfoPermittedLogonTimes,
            PropertyNames.AcctInfoSmartcardRequired,
            PropertyNames.AcctInfoDelegationPermitted,
            PropertyNames.AcctInfoHomeDirectory,
            PropertyNames.AcctInfoHomeDrive,
            PropertyNames.AcctInfoScriptPath,

            PropertyNames.PwdInfoPasswordNotRequired,
            PropertyNames.PwdInfoPasswordNeverExpires,
            PropertyNames.PwdInfoCannotChangePassword,
            PropertyNames.PwdInfoAllowReversiblePasswordEncryption
        };

        internal static string[] groupProperties = new string[]
        {
            PropertyNames.GroupIsSecurityGroup,
            PropertyNames.GroupGroupScope
        };

        internal static string[] computerProperties = new string[]
        {
            PropertyNames.ComputerServicePrincipalNames
        };

        protected QbeFilterDescription BuildQbeFilterDescription(Principal p)
        {
            QbeFilterDescription qbeFilterDescription = new QbeFilterDescription();

            // We don't have to check to make sure the application didn't try to set any
            // disallowed properties (i..e, referential properties, such as Group.Members),
            // because that check was enforced by the PrincipalSearcher in its
            // FindAll() and GetUnderlyingSearcher() methods, by calling
            // PrincipalSearcher.HasReferentialPropertiesSet().

            if (p is Principal)
                BuildFilterSet(p, principalProperties, qbeFilterDescription);

            if (p is AuthenticablePrincipal)
                BuildFilterSet(p, authenticablePrincipalProperties, qbeFilterDescription);

            if (p is UserPrincipal)  // includes AccountInfo and PasswordInfo
            {
                // AcctInfoExpirationDate and AcctInfoExpiredAccount represent filters on the same property
                // check that only one is specified
                if (p.GetChangeStatusForProperty(PropertyNames.AcctInfoExpirationDate) &&
                        p.GetChangeStatusForProperty(PropertyNames.AcctInfoExpiredAccount))
                {
                    throw new InvalidOperationException(
                                       SR.Format(
                                           SR.StoreCtxMultipleFiltersForPropertyUnsupported,
                                           PropertyNamesExternal.GetExternalForm(ExpirationDateFilter.PropertyNameStatic)));
                }

                BuildFilterSet(p, userProperties, qbeFilterDescription);
            }

            if (p is GroupPrincipal)
                BuildFilterSet(p, groupProperties, qbeFilterDescription);

            if (p is ComputerPrincipal)
                BuildFilterSet(p, computerProperties, qbeFilterDescription);

            return qbeFilterDescription;
        }

        // Applies to supplied propertySet to the supplied Principal, and adds any resulting filters
        // to qbeFilterDescription.
        private void BuildFilterSet(Principal p, string[] propertySet, QbeFilterDescription qbeFilterDescription)
        {
            foreach (string propertyName in propertySet)
            {
                if (p.GetChangeStatusForProperty(propertyName))
                {
                    // Property has changed.  Add it to the filter set.
                    object value = p.GetValueForProperty(propertyName);

                    GlobalDebug.WriteLineIf(
                            GlobalDebug.Info,
                            "StoreCtx",
                            "BuildFilterSet: type={0}, property name={1}, property value={2} of type {3}",
                            p.GetType().ToString(),
                            propertyName,
                            value.ToString(),
                            value.GetType().ToString());

                    // Build the right filter based on type of the property value
                    if (value is PrincipalValueCollection<string> trackingList)
                    {
                        foreach (string s in trackingList.Inserted)
                        {
                            object filter = FilterFactory.CreateFilter(propertyName);
                            ((FilterBase)filter).Value = (string)s;
                            qbeFilterDescription.FiltersToApply.Add(filter);
                        }
                    }
                    else if (value is X509Certificate2Collection certCollection)
                    {
                        // Since QBE filter objects are always unpersisted, any certs in the collection
                        // must have been inserted by the application.
                        foreach (X509Certificate2 cert in certCollection)
                        {
                            object filter = FilterFactory.CreateFilter(propertyName);
                            ((FilterBase)filter).Value = (X509Certificate2)cert;
                            qbeFilterDescription.FiltersToApply.Add(filter);
                        }
                    }
                    else
                    {
                        // It's not one of the multivalued cases.  Try the scalar cases.

                        object filter = FilterFactory.CreateFilter(propertyName);

                        if (value == null)
                        {
                            ((FilterBase)filter).Value = null;
                        }
                        else if (value is bool)
                        {
                            ((FilterBase)filter).Value = (bool)value;
                        }
                        else if (value is string)
                        {
                            ((FilterBase)filter).Value = (string)value;
                        }
                        else if (value is GroupScope)
                        {
                            ((FilterBase)filter).Value = (GroupScope)value;
                        }
                        else if (value is byte[])
                        {
                            ((FilterBase)filter).Value = (byte[])value;
                        }
                        else if (value is Nullable<DateTime>)
                        {
                            ((FilterBase)filter).Value = (Nullable<DateTime>)value;
                        }
                        else if (value is ExtensionCache)
                        {
                            ((FilterBase)filter).Value = (ExtensionCache)value;
                        }
                        else if (value is QbeMatchType)
                        {
                            ((FilterBase)filter).Value = (QbeMatchType)value;
                        }
                        else
                        {
                            // Internal error.  Didn't match either the known multivalued or scalar cases.
                            Debug.Fail($"StoreCtx.BuildFilterSet: fell off end looking for {propertyName} of type {value.GetType()}");
                        }

                        qbeFilterDescription.FiltersToApply.Add(filter);
                    }
                }
            }
        }
    }
}
