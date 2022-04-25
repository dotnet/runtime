// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            string? oidValue = OidLookup.ToOid(oid, OidGroup.All, fallBackToAllGroups: false);
            if (oidValue == null)
            {
                oidValue = oid;
            }
            this.Value = oidValue;

            _group = OidGroup.All;
        }

        public Oid(string? value, string? friendlyName)
        {
            _value = value;
            _friendlyName = friendlyName;
            _hasInitializedFriendlyName = friendlyName != null;
        }

        public Oid(Oid oid)
        {
            ArgumentNullException.ThrowIfNull(oid);

            _value = oid._value;
            _friendlyName = oid._friendlyName;
            _group = oid._group;
            _hasInitializedFriendlyName = oid._hasInitializedFriendlyName;
        }

        public static Oid FromFriendlyName(string friendlyName, OidGroup group)
        {
            ArgumentNullException.ThrowIfNull(friendlyName);

            string? oidValue = OidLookup.ToOid(friendlyName, group, fallBackToAllGroups: false);
            if (oidValue == null)
                throw new CryptographicException(SR.Cryptography_Oid_InvalidName);

            return new Oid(oidValue, friendlyName, group);
        }

        public static Oid FromOidValue(string oidValue, OidGroup group)
        {
            ArgumentNullException.ThrowIfNull(oidValue);

            string? friendlyName = OidLookup.ToFriendlyName(oidValue, group, fallBackToAllGroups: false);
            if (friendlyName == null)
                throw new CryptographicException(SR.Cryptography_Oid_InvalidValue);

            return new Oid(oidValue, friendlyName, group);
        }

        public string? Value
        {
            get => _value;
            set
            {
                // If _value has not been set, permit it to be set once, or to
                // the same value for "initialize once" behavior.
                if (_value != null && !_value.Equals(value, StringComparison.Ordinal))
                {
                    throw new PlatformNotSupportedException(SR.Cryptography_Oid_SetOnceValue);
                }

                _value = value;
            }
        }

        public string? FriendlyName
        {
            get
            {
                if (!_hasInitializedFriendlyName && _value != null)
                {
                    _friendlyName = OidLookup.ToFriendlyName(_value, _group, fallBackToAllGroups: true);
                    _hasInitializedFriendlyName = true;
                }

                return _friendlyName;
            }
            set
            {
                // If _friendlyName has not been set, permit it to be set once, or to
                // the same value for "initialize once" behavior.
                if (_hasInitializedFriendlyName)
                {
                    if ((_friendlyName != null && !_friendlyName.Equals(value, StringComparison.Ordinal)) ||
                        (_friendlyName is null && value != null))
                    {
                        throw new PlatformNotSupportedException(SR.Cryptography_Oid_SetOnceFriendlyName);
                    }

                    // Already initialized, no meaningful mutation, we so we can exit early.
                    return;
                }

                // If we can find the matching OID value, then update it as well
                if (value != null)
                {
                    // If FindOidInfo fails, we return a null String
                    string? oidValue = OidLookup.ToOid(value, _group, fallBackToAllGroups: true);

                    if (oidValue != null)
                    {
                        // If the OID value has not been initialized, set it
                        // to the lookup value.
                        if (_value == null)
                        {
                            _value = oidValue;
                        }

                        // The friendly name resolves to an OID value other than the
                        // current one, which is not permitted under "initialize once"
                        // behavior.
                        else if (!_value.Equals(oidValue, StringComparison.Ordinal))
                        {
                            throw new PlatformNotSupportedException(SR.Cryptography_Oid_SetOnceValue);
                        }
                    }
                }

                // Ensure we don't mutate _friendlyName until we are sure we can
                // set _value if we are going to.
                _friendlyName = value;
                _hasInitializedFriendlyName = true;
            }
        }

        private Oid(string value, string friendlyName, OidGroup group)
        {
            Debug.Assert(value != null);
            Debug.Assert(friendlyName != null);

            _value = value;
            _friendlyName = friendlyName;
            _group = group;
            _hasInitializedFriendlyName = true;
        }

        private string? _value;
        private string? _friendlyName;
        private bool _hasInitializedFriendlyName;
        private readonly OidGroup _group = OidGroup.All;
    }
}
