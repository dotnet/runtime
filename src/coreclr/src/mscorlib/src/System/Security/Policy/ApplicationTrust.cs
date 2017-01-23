// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This class encapsulates security decisions about an application.
//

namespace System.Security.Policy
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Security.Permissions;
    using System.Security.Util;
    using System.Text;
    using System.Threading;
    using System.Diagnostics.Contracts;

    [System.Runtime.InteropServices.ComVisible(true)]
    public enum ApplicationVersionMatch {
        MatchExactVersion,
        MatchAllVersions
    }

    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public sealed class ApplicationTrust : EvidenceBase, ISecurityEncodable
    {
        private PolicyStatement m_psDefaultGrant;
        private IList<StrongName> m_fullTrustAssemblies;

        // Permission special flags for the default grant set in this ApplicationTrust.  This should be
        // updated in sync with any updates to the default grant set.
        // 
        // In the general case, these values cannot be trusted - we only store a reference to the
        // DefaultGrantSet, and return the reference directly, which means that code can update the
        // permission set without our knowledge.  That would lead to the flags getting out of sync with the
        // grant set.
        // 
        // However, we only care about these flags when we're creating a homogenous AppDomain, and in that
        // case we control the ApplicationTrust object end-to-end, and know that the permission set will not
        // change after the flags are calculated.
        [NonSerialized]
        private int m_grantSetSpecialFlags;

        public ApplicationTrust () : this (new PermissionSet(PermissionState.None))
        {
        }

        internal ApplicationTrust (PermissionSet defaultGrantSet)
        {
            InitDefaultGrantSet(defaultGrantSet);

            m_fullTrustAssemblies = new List<StrongName>().AsReadOnly();
        }

        public ApplicationTrust(PermissionSet defaultGrantSet, IEnumerable<StrongName> fullTrustAssemblies) {
            if (fullTrustAssemblies == null) {
                throw new ArgumentNullException(nameof(fullTrustAssemblies));
            }

            InitDefaultGrantSet(defaultGrantSet);

            List<StrongName> fullTrustList = new List<StrongName>();
            foreach (StrongName strongName in fullTrustAssemblies) {
                if (strongName == null) {
                    throw new ArgumentException(Environment.GetResourceString("Argument_NullFullTrustAssembly"), nameof(fullTrustAssemblies));
                }

                fullTrustList.Add(new StrongName(strongName.PublicKey, strongName.Name, strongName.Version));
            }

            m_fullTrustAssemblies = fullTrustList.AsReadOnly();
        }

        // Sets up the default grant set for all constructors. Extracted to avoid the cost of
        // IEnumerable virtual dispatches on startup when there are no fullTrustAssemblies (CoreCLR)
        private void InitDefaultGrantSet(PermissionSet defaultGrantSet) {
            if (defaultGrantSet == null) {
                throw new ArgumentNullException(nameof(defaultGrantSet));
            }

            // Creating a PolicyStatement copies the incoming permission set, so we don't have to worry
            // about the PermissionSet parameter changing underneath us after we've calculated the
            // permisison flags in the DefaultGrantSet setter.
            DefaultGrantSet = new PolicyStatement(defaultGrantSet);
        }

        public PolicyStatement DefaultGrantSet {
            get {
                if (m_psDefaultGrant == null)
                    return new PolicyStatement(new PermissionSet(PermissionState.None));
                return m_psDefaultGrant;
            }
            set {
                if (value == null) {
                    m_psDefaultGrant = null;
                    m_grantSetSpecialFlags = 0;
                }
                else {
                    m_psDefaultGrant = value;
                    m_grantSetSpecialFlags = SecurityManager.GetSpecialFlags(m_psDefaultGrant.PermissionSet, null);
                }
            }
        }

        public IList<StrongName> FullTrustAssemblies {
            get {
                return m_fullTrustAssemblies;
            }
        }

        public override EvidenceBase Clone()
        {
            return base.Clone();
        }
    }
}
