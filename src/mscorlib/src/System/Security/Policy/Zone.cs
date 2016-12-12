// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

//
//
//  Zone is an IIdentity representing Internet/Intranet/MyComputer etc.
//

namespace System.Security.Policy
{
    using System.Security.Util;
    using ZoneIdentityPermission = System.Security.Permissions.ZoneIdentityPermission;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Runtime.Serialization;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class Zone : EvidenceBase, IIdentityPermissionFactory
    {
        private SecurityZone m_zone;

        private static readonly String[] s_names =
            {"MyComputer", "Intranet", "Trusted", "Internet", "Untrusted", "NoZone"};

        public Zone(SecurityZone zone)
        {
            if (zone < SecurityZone.NoZone || zone > SecurityZone.Untrusted)
                throw new ArgumentException( Environment.GetResourceString( "Argument_IllegalZone" ) );
            Contract.EndContractBlock();

            m_zone = zone;
        }

        private Zone(Zone zone)
        {
            Debug.Assert(zone != null);
            m_zone = zone.m_zone;
        }

        public IPermission CreateIdentityPermission( Evidence evidence )
        {
            return new ZoneIdentityPermission( SecurityZone );
        }

        public SecurityZone SecurityZone
        {
            get
            {
                return m_zone;
            }
        }

        public override bool Equals(Object o)
        {
            Zone other = o as Zone;
            if (other == null)
            {
                return false;
            }

            return SecurityZone == other.SecurityZone;
        }

        public override int GetHashCode()
        {
            return (int)SecurityZone;
        }

        public override EvidenceBase Clone()
        {
            return new Zone(this);
        }

        public Object Copy()
        {
            return Clone();
        }

        // INormalizeForIsolatedStorage is not implemented for startup perf
        // equivalent to INormalizeForIsolatedStorage.Normalize()
        internal Object Normalize()
        {
            return s_names[(int)SecurityZone];
        }
    }
}
