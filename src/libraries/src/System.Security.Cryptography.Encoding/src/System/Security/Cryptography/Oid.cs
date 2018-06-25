// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public sealed class Oid
    {
        public Oid() { }

        public Oid(string oid)
        {
            // If we were passed the friendly name, retrieve the value String.
            string oidValue = OidLookup.ToOid(oid, OidGroup.All, fallBackToAllGroups: false);
            if (oidValue == null)
            {
                oidValue = oid;
            }
            this.Value = oidValue;

            _group = OidGroup.All;
        }

        public Oid(string value, string friendlyName)
        {
            _value = value;
            _friendlyName = friendlyName;
        }

        public Oid(Oid oid)
        {
            if (oid == null)
                throw new ArgumentNullException(nameof(oid));
            _value = oid._value;
            _friendlyName = oid._friendlyName;
            _group = oid._group;
        }

        public static Oid FromFriendlyName(string friendlyName, OidGroup group)
        {
            if (friendlyName == null)
            {
                throw new ArgumentNullException(nameof(friendlyName));
            }

            string oidValue = OidLookup.ToOid(friendlyName, group, fallBackToAllGroups: false);
            if (oidValue == null)
                throw new CryptographicException(SR.Cryptography_Oid_InvalidName);

            return new Oid(oidValue, friendlyName, group);
        }

        public static Oid FromOidValue(string oidValue, OidGroup group)
        {
            if (oidValue == null)
                throw new ArgumentNullException(nameof(oidValue));

            string friendlyName = OidLookup.ToFriendlyName(oidValue, group, fallBackToAllGroups: false);
            if (friendlyName == null)
                throw new CryptographicException(SR.Cryptography_Oid_InvalidValue);

            return new Oid(oidValue, friendlyName, group);
        }

        public string Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public string FriendlyName
        {
            get
            {
                if (_friendlyName == null && _value != null)
                {
                    _friendlyName = OidLookup.ToFriendlyName(_value, _group, fallBackToAllGroups: true);
                }

                return _friendlyName;
            }
            set
            {
                _friendlyName = value;
                // If we can find the matching OID value, then update it as well
                if (_friendlyName != null)
                {
                    // If FindOidInfo fails, we return a null String
                    string oidValue = OidLookup.ToOid(_friendlyName, _group, fallBackToAllGroups: true);
                    if (oidValue != null)
                    {
                        _value = oidValue;
                    }
                }
            }
        }

        private Oid(string value, string friendlyName, OidGroup group)
        {
            Debug.Assert(value != null);
            Debug.Assert(friendlyName != null);

            _value = value;
            _friendlyName = friendlyName;
            _group = group;
        }

        private string _value = null;
        private string _friendlyName = null;
        private OidGroup _group = OidGroup.All;
    }
}
