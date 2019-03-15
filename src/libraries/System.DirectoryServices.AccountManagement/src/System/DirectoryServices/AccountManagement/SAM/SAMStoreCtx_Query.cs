// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

using System.DirectoryServices;

namespace System.DirectoryServices.AccountManagement
{
    internal partial class SAMStoreCtx : StoreCtx
    {
        //
        // Query operations
        //

        // Returns true if this store has native support for search (and thus a wormhole).
        // Returns true for everything but SAM (both reg-SAM and MSAM).
        internal override bool SupportsSearchNatively { get { return false; } }

        // Returns a type indicating the type of object that would be returned as the wormhole for the specified
        // PrincipalSearcher.
        internal override Type SearcherNativeType()
        {
            Debug.Fail("SAMStoreCtx: SearcherNativeType: There is no native searcher type.");
            throw new InvalidOperationException(SR.PrincipalSearcherNoUnderlying);
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
            // There's no underlying searcher for SAM
            Debug.Assert(ps.UnderlyingSearcher == null);

            Principal qbeFilter = ps.QueryFilter;
            QbeFilterDescription filters;

            // If they specified a filter object, extract the set properties from it.
            // Otherwise, use an empty set.  Note that we don't worry about filtering by
            // the type of the qbeFilter object (e.g., restricting the returned principals
            // to only Users, or only Groups) --- that's handled in Query().
            if (qbeFilter != null)
                filters = BuildQbeFilterDescription(qbeFilter);
            else
                filters = new QbeFilterDescription();

            return filters;
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
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMStoreCtx", "Query");

            Debug.Assert(sizeLimit >= -1);

            // Build the description of the properties we'll filter by.  In SAMStoreCtx, the "native" searcher
            // is simply the QbeFilterDescription, which will be passed to the SAMQuerySet to use to
            // manually filter out non-matching results.
            QbeFilterDescription propertiesToMatch = (QbeFilterDescription)PushFilterToNativeSearcher(ps);

            // Get the entries we'll iterate over.  Write access to Children is controlled through the
            // ctxBaseLock, but we don't want to have to hold that lock while we're iterating over all
            // the child entries.  So we have to clone the ctxBase --- not ideal, but it prevents
            // multithreading issues.
            DirectoryEntries entries = SDSUtils.BuildDirectoryEntry(_ctxBase.Path, _credentials, _authTypes).Children;
            Debug.Assert(entries != null);

            // Determine the principal types of interest.  The SAMQuerySet will use this to restrict
            // the types of DirectoryEntry objects returned.
            Type qbeFilterType = typeof(Principal);
            if (ps.QueryFilter != null)
                qbeFilterType = ps.QueryFilter.GetType();

            List<string> schemaTypes = GetSchemaFilter(qbeFilterType);

            // Create the ResultSet that will perform the client-side filtering
            SAMQuerySet resultSet = new SAMQuerySet(
                                                schemaTypes,
                                                entries,
                                                _ctxBase,
                                                sizeLimit,
                                                this,
                                                new QbeMatcher(propertiesToMatch));

            return resultSet;
        }

        private List<string> GetSchemaFilter(Type principalType)
        {
            List<string> schemaTypes = new List<string>();

            if (principalType == typeof(UserPrincipal) || principalType.IsSubclassOf(typeof(UserPrincipal)))
            {
                schemaTypes.Add("User");
            }
            else if (principalType == typeof(GroupPrincipal) || principalType.IsSubclassOf(typeof(GroupPrincipal)))
            {
                schemaTypes.Add("Group");
            }
            else if (principalType == typeof(ComputerPrincipal) || principalType.IsSubclassOf(typeof(ComputerPrincipal)))
            {
                schemaTypes.Add("Computer");
            }
            else if (principalType == typeof(Principal))
            {
                schemaTypes.Add("User");
                schemaTypes.Add("Group");
                schemaTypes.Add("Computer");
            }
            else if (principalType == typeof(AuthenticablePrincipal))
            {
                schemaTypes.Add("User");
                schemaTypes.Add("Computer");
            }
            else
            {
                Debug.Fail($"SAMStoreCtx.GetSchemaFilter: fell off end looking for {principalType}");
                throw new InvalidOperationException(
                                SR.Format(SR.StoreCtxUnsupportedPrincipalTypeForQuery, principalType));
            }

            return schemaTypes;
        }
    }
}

//#endif  // PAPI_REGSAM
