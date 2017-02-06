// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

//
//
// StrongName is an IIdentity representing strong names.
//

namespace System.Security.Policy {
    using System.IO;
    using System.Reflection;
    using System.Security.Util;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;
    using CultureInfo = System.Globalization.CultureInfo;

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    internal sealed class StrongName : EvidenceBase
    {
        private StrongNamePublicKeyBlob m_publicKeyBlob;
        private String m_name;
        private Version m_version;

        // Delay evaluated evidence is for policy resolution only, so it doesn't make sense to save that
        // state away and then try to evaluate the strong name later.
        [NonSerialized]
        private RuntimeAssembly m_assembly = null;

        [NonSerialized]
        private bool m_wasUsed = false;

        internal StrongName() {}

        public override bool Equals( Object o )
        {
            StrongName that = (o as StrongName);
            return (that != null) &&
                   Equals( this.m_publicKeyBlob, that.m_publicKeyBlob ) &&
                   Equals( this.m_name, that.m_name ) &&
                   Equals( this.m_version, that.m_version );
        }

        public override int GetHashCode()
        {
            if (m_publicKeyBlob != null)
            {
                return m_publicKeyBlob.GetHashCode();
            }
            else if (m_name != null || m_version != null)
            {
                return (m_name == null ? 0 : m_name.GetHashCode()) + (m_version == null ? 0 : m_version.GetHashCode());
            }
            else
            {
                return typeof( StrongName ).GetHashCode();
            }
        }
    }
}
