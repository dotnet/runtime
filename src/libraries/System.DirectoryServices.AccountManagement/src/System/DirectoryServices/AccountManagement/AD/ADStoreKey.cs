// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;

namespace System.DirectoryServices.AccountManagement
{
    internal class ADStoreKey : StoreKey
    {
        // For regular ADStoreKeys
        private System.Guid _objectGuid;

        // For ADStoreKeys corresponding to well-known SIDs
        private bool _wellKnownSid;
        private string _domainName;
        private byte[] _sid;

        public ADStoreKey(Guid guid)
        {
            Debug.Assert(guid != Guid.Empty);

            _objectGuid = guid;
            _wellKnownSid = false;

            GlobalDebug.WriteLineIf(
                            GlobalDebug.Info,
                            "ADStoreKey",
                            "creating GUID key for GUID={0}",
                            guid);
        }

        public ADStoreKey(string domainName, byte[] sid)
        {
            Debug.Assert(domainName != null && domainName.Length > 0);
            Debug.Assert(sid != null && sid.Length != 0);

            // Make a copy of the SID, since a byte[] is mutable
            _sid = new byte[sid.Length];
            Array.Copy(sid, _sid, sid.Length);

            _domainName = domainName;
            _wellKnownSid = true;

            GlobalDebug.WriteLineIf(
                            GlobalDebug.Info,
                            "ADStoreKey",
                            "creating SID key for domainName={0}, sid={1}",
                            domainName,
                            Utils.ByteArrayToString(sid));
        }

        override public bool Equals(object o)
        {
            if (!(o is ADStoreKey))
                return false;

            ADStoreKey that = (ADStoreKey)o;

            if (_wellKnownSid != that._wellKnownSid)
                return false;

            if (!_wellKnownSid)
            {
                if (_objectGuid == that._objectGuid)
                    return true;
            }
            else
            {
                if ((String.Compare(_domainName, that._domainName, StringComparison.OrdinalIgnoreCase) == 0) &&
                     (Utils.AreBytesEqual(_sid, that._sid)))
                    return true;
            }

            return false;
        }

        override public int GetHashCode()
        {
            return (_wellKnownSid == false) ?
                        _objectGuid.GetHashCode() :
                        (_domainName.GetHashCode() ^ _sid.GetHashCode());
        }
    }
}
