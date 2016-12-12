// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

//
//
//  Site is an IIdentity representing internet sites.
//

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Security.Permissions;
using System.Security.Util;

namespace System.Security.Policy
{
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class Site : EvidenceBase, IIdentityPermissionFactory
    {
        private SiteString m_name;

        public Site(String name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            Contract.EndContractBlock();

            m_name = new SiteString( name );
        }

        private Site(SiteString name)
        {
            Debug.Assert(name != null);
            m_name = name;
        }

        public static Site CreateFromUrl( String url )
        {
            return new Site(ParseSiteFromUrl(url));
        }

        private static SiteString ParseSiteFromUrl( String name )
        {
            URLString urlString = new URLString( name );

            if (String.Compare( urlString.Scheme, "file", StringComparison.OrdinalIgnoreCase) == 0)
                throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidSite" ) );

            return new SiteString( new URLString( name ).Host );
        }

        public String Name
        {
            get { return m_name.ToString(); }
        }

        internal SiteString GetSiteString()
        {
            return m_name;
        }

        public IPermission CreateIdentityPermission( Evidence evidence )
        {
            return new SiteIdentityPermission( Name );
        }

        public override bool Equals(Object o)
        {
            Site other = o as Site;
            if (other == null)
            {
                return false;
            }

            return String.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override EvidenceBase Clone()
        {
            return new Site(m_name);
        }

        public Object Copy()
        {
            return Clone();
        }

        // INormalizeForIsolatedStorage is not implemented for startup perf
        // equivalent to INormalizeForIsolatedStorage.Normalize()
        internal Object Normalize()
        {
            return m_name.ToString().ToUpper(CultureInfo.InvariantCulture);
        }
    }
}
