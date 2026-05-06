// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.Security.Principal;
using System.Text;

namespace System.DirectoryServices.AccountManagement
{
    internal sealed class TokenGroupSet : ResultSet
    {
        internal TokenGroupSet(
                             string userDN,
                             ADStoreCtx storeCtx,
                             bool readDomainGroups)
        {
            _principalDN = userDN;
            _storeCtx = storeCtx;
            _attributeToQuery = readDomainGroups ? "tokenGroups" : "tokenGroupsGlobalAndUniversal";

            GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                    "TokenGroupSet",
                                    "TokenGroupSet: userDN={0}",
                                    userDN);
        }

        private readonly string _principalDN;
        private readonly ADStoreCtx _storeCtx;

        private bool _atBeginning = true;
        private DirectoryEntry _current; // current member of the group (or current group of the user)

        private IEnumerator _tokenGroupsEnum;

        private SecurityIdentifier _currentSID;
        private bool _disposed;

        private readonly string _attributeToQuery;

        // Return the principal we're positioned at as a Principal object.
        // Need to use our StoreCtx's GetAsPrincipal to convert the native object to a Principal
        internal override object CurrentAsPrincipal
        {
            get
            {
                if (_currentSID != null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "TokenGroupSet", "CurrentAsPrincipal: using current");

                    string SidBindingString = $"<SID={Utils.SecurityIdentifierToLdapHexBindingString(_currentSID)}>";

                    DirectoryEntry currentDE = SDSUtils.BuildDirectoryEntry(
                                                BuildPathFromDN(SidBindingString),
                                                _storeCtx.Credentials,
                                                _storeCtx.AuthTypes);

                    _storeCtx.InitializeNewDirectoryOptions(currentDE);
                    _storeCtx.LoadDirectoryEntryAttributes(currentDE);

                    return ADUtils.DirectoryEntryAsPrincipal(currentDE, _storeCtx);
                }

                return null;
            }
        }

        // Advance the enumerator to the next principal in the result set, pulling in additional pages
        // of results (or ranges of attribute values) as needed.
        // Returns true if successful, false if no more results to return.
        internal override bool MoveNext()
        {
            if (_atBeginning)
            {
                Debug.Assert(_principalDN != null);

                _current = SDSUtils.BuildDirectoryEntry(
                                            BuildPathFromDN(_principalDN),
                                            _storeCtx.Credentials,
                                            _storeCtx.AuthTypes);

                _current.RefreshCache(new string[] { _attributeToQuery });

                _tokenGroupsEnum = _current.Properties[_attributeToQuery].GetEnumerator();

                _atBeginning = false;
            }

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "TokenGroupSet", "MoveNextLocal: returning primary group {0}", _current.Path);

            if (_tokenGroupsEnum.MoveNext())
            {
                // Got a member from this group (or, got a group of which we're a member).
                // Create a DirectoryEntry for it.

                byte[] sid = (byte[])_tokenGroupsEnum.Current;
                _currentSID = new SecurityIdentifier(sid, 0);
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "TokenGroupSet", "MoveNextLocal: got a value from the enumerator: {0}", _currentSID.ToString());

                return true;
            }

            return false;
        }

        // Resets the enumerator to before the first result in the set.  This potentially can be an expensive
        // operation, e.g., if doing a paged search, may need to re-retrieve the first page of results.
        // As a special case, if the ResultSet is already at the very beginning, this is guaranteed to be
        // a no-op.
        internal override void Reset()
        {
            if (_atBeginning)
                return;

            _tokenGroupsEnum.Reset();
        }

        // IDisposable implementation
        public override void Dispose()
        {
            try
            {
                if (!_disposed)
                {
                    _current?.Dispose();

                    _disposed = true;
                }
            }
            finally
            {
                base.Dispose();
            }
        }

        private string BuildPathFromDN(string dn)
        {
            string userSuppliedServername = _storeCtx.UserSuppliedServerName;

            UnsafeNativeMethods.Pathname pathCracker = new UnsafeNativeMethods.Pathname();
            UnsafeNativeMethods.IADsPathname pathName = (UnsafeNativeMethods.IADsPathname)pathCracker;
            pathName.EscapedMode = 2 /* ADS_ESCAPEDMODE_ON */;
            pathName.Set(dn, 4 /* ADS_SETTYPE_DN */);
            string escapedDn = pathName.Retrieve(7 /* ADS_FORMAT_X500_DN */);

            if (userSuppliedServername.Length > 0)
                return "LDAP://" + _storeCtx.UserSuppliedServerName + "/" + escapedDn;
            else
                return "LDAP://" + escapedDn;
        }
    }
}
