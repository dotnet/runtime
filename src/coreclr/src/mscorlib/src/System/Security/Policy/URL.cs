// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

//
//
//  Url is an IIdentity representing url internet sites.
//

namespace System.Security.Policy {
    using System.IO;
    using System.Security.Util;
    using UrlIdentityPermission = System.Security.Permissions.UrlIdentityPermission;
    using System.Runtime.Serialization;
    using System.Diagnostics.Contracts;

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class Url : EvidenceBase, IIdentityPermissionFactory
    {
        private URLString m_url;

        internal Url( String name, bool parsed )
        {
            if (name == null)
                throw new ArgumentNullException( "name" );
            Contract.EndContractBlock();

            m_url = new URLString( name, parsed );
        }

        public Url( String name )
        {
            if (name == null)
                throw new ArgumentNullException( "name" );
            Contract.EndContractBlock();

            m_url = new URLString( name );
        }

        private Url(Url url)
        {
            Contract.Assert(url != null);
            m_url = url.m_url;
        }

        public String Value
        {
            get { return m_url.ToString(); }
        }

        internal URLString GetURLString()
        {
            return m_url;
        }

        public IPermission CreateIdentityPermission( Evidence evidence )
        {
            return new UrlIdentityPermission( m_url );
        }

        public override bool Equals(Object o)
        {
            Url other = o as Url;
            if (other == null)
            {
                return false;
            }

            return other.m_url.Equals(m_url);
        }

        public override int GetHashCode()
        {
            return this.m_url.GetHashCode();
        }

        public override EvidenceBase Clone()
        {
            return new Url(this);
        }

        public Object Copy()
        {
            return Clone();
        }

#if FEATURE_CAS_POLICY
        internal SecurityElement ToXml()
        {
            SecurityElement root = new SecurityElement( "System.Security.Policy.Url" );
            // If you hit this assert then most likely you are trying to change the name of this class. 
            // This is ok as long as you change the hard coded string above and change the assert below.
            Contract.Assert( this.GetType().FullName.Equals( "System.Security.Policy.Url" ), "Class name changed!" );

            root.AddAttribute( "version", "1" );

            if (m_url != null)
                root.AddChild( new SecurityElement( "Url", m_url.ToString() ) );

            return root;
        }

        public override String ToString()
        {
            return ToXml().ToString();
        }
#endif // FEATURE_CAS_POLICY

        // INormalizeForIsolatedStorage is not implemented for startup perf
        // equivalent to INormalizeForIsolatedStorage.Normalize()
        internal Object Normalize()
        {
            return m_url.NormalizeUrl();
        }
    }
}
