// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.DirectoryServices;

using System.Runtime.InteropServices;

namespace System.DirectoryServices.AccountManagement
{
    internal sealed class SAMGroupsSet : ResultSet
    {
        internal SAMGroupsSet(UnsafeNativeMethods.IADsMembers iADsMembers, SAMStoreCtx storeCtx, DirectoryEntry ctxBase)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMGroupsSet", "SAMGroupsSet: creating for path={0}", ctxBase.Path);

            _groupsEnumerator = ((IEnumerable)iADsMembers).GetEnumerator();

            _storeCtx = storeCtx;
            _ctxBase = ctxBase;
        }

        // Return the principal we're positioned at as a Principal object.
        // Need to use our StoreCtx's GetAsPrincipal to convert the native object to a Principal
        internal override object CurrentAsPrincipal
        {
            get
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMGroupsSet", "CurrentAsPrincipal");

                Debug.Assert(_current != null);

                return SAMUtils.DirectoryEntryAsPrincipal(_current, _storeCtx);
            }
        }

        // Advance the enumerator to the next principal in the result set, pulling in additional pages
        // of results (or ranges of attribute values) as needed.
        // Returns true if successful, false if no more results to return.
        internal override bool MoveNext()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMGroupsSet", "MoveNext");

            _atBeginning = false;

            bool f = _groupsEnumerator.MoveNext();

            if (f)
            {
                // Got a group.  Create a DirectoryEntry for it.
                // Clone the ctxBase to pick up its credentials, then build an appropriate path.
                UnsafeNativeMethods.IADs nativeMember = (UnsafeNativeMethods.IADs)_groupsEnumerator.Current;

                // We do this, rather than using the DirectoryEntry constructor that takes a native IADs object,
                // is so the credentials get transferred to the new DirectoryEntry.  If we just use the native
                // object constructor, the native object will have the right credentials, but the DirectoryEntry
                // will have default (null) credentials, which it'll use anytime it needs to use credentials.
                DirectoryEntry de = SDSUtils.BuildDirectoryEntry(
                                                nativeMember.ADsPath,
                                                _storeCtx.Credentials,
                                                _storeCtx.AuthTypes);

                _current = de;
            }

            return f;
        }

        // Resets the enumerator to before the first result in the set.  This potentially can be an expensive
        // operation, e.g., if doing a paged search, may need to re-retrieve the first page of results.
        // As a special case, if the ResultSet is already at the very beginning, this is guaranteed to be
        // a no-op.
        internal override void Reset()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMGroupsSet", "Reset");

            if (!_atBeginning)
            {
                _groupsEnumerator.Reset();
                _current = null;

                _atBeginning = true;
            }
        }

        //
        // Private fields
        //

        private readonly IEnumerator _groupsEnumerator;
        private readonly SAMStoreCtx _storeCtx;
        private readonly DirectoryEntry _ctxBase;

        private bool _atBeginning = true;

        private DirectoryEntry _current;
    }
}

// #endif
