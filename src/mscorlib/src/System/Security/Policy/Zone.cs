// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

//
//
//  Zone is an IIdentity representing Internet/Intranet/MyComputer etc.
//

namespace System.Security.Policy {
    using System.Security.Util;
    using ZoneIdentityPermission = System.Security.Permissions.ZoneIdentityPermission;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Runtime.Serialization;
    using System.Diagnostics.Contracts;

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class Zone : EvidenceBase, IIdentityPermissionFactory
    {
#if FEATURE_CAS_POLICY
        [OptionalField(VersionAdded = 2)]
        private String m_url;
#endif // FEATURE_CAS_POLICY
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
            Contract.Assert(zone != null);

#if FEATURE_CAS_POLICY
            m_url = zone.m_url;
#endif // FEATURE_CAS_POLICY
            m_zone = zone.m_zone;
        }

#if FEATURE_CAS_POLICY
        private Zone(String url)
        {
            m_url = url;
            m_zone = SecurityZone.NoZone;
        }

        public static Zone CreateFromUrl( String url )
        {
            if (url == null)
                throw new ArgumentNullException( "url" );
            Contract.EndContractBlock();

            return new Zone( url );
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static SecurityZone _CreateFromUrl( String url );
#endif // FEATURE_CAS_POLICY

        public IPermission CreateIdentityPermission( Evidence evidence )
        {
            return new ZoneIdentityPermission( SecurityZone );
        }

        public SecurityZone SecurityZone
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
#if FEATURE_CAS_POLICY
                if (m_url != null)
                    m_zone = _CreateFromUrl( m_url );
#endif // FEATURE_CAS_POLICY

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

#if FEATURE_CAS_POLICY
        internal SecurityElement ToXml()
        {
            SecurityElement elem = new SecurityElement( "System.Security.Policy.Zone" );
            // If you hit this assert then most likely you are trying to change the name of this class. 
            // This is ok as long as you change the hard coded string above and change the assert below.
            Contract.Assert( this.GetType().FullName.Equals( "System.Security.Policy.Zone" ), "Class name changed!" );

            elem.AddAttribute( "version", "1" );
            if (SecurityZone != SecurityZone.NoZone)
                elem.AddChild( new SecurityElement( "Zone", s_names[(int)SecurityZone] ) );
            else
                elem.AddChild( new SecurityElement( "Zone", s_names[s_names.Length-1] ) );
            return elem;
        }
#endif // FEATURE_CAS_POLICY

#if FEATURE_CAS_POLICY
        public override String ToString()
        {
            return ToXml().ToString();
        }
#endif // FEATURE_CAS_POLICY

        // INormalizeForIsolatedStorage is not implemented for startup perf
        // equivalent to INormalizeForIsolatedStorage.Normalize()
        internal Object Normalize()
        {
            return s_names[(int)SecurityZone];
        }
    }
}
