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
    public sealed class StrongName : EvidenceBase, IIdentityPermissionFactory, IDelayEvaluatedEvidence
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

        public StrongName( StrongNamePublicKeyBlob blob, String name, Version version ) : this(blob, name, version, null)
        {
        }

        internal StrongName(StrongNamePublicKeyBlob blob, String name, Version version, Assembly assembly)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyStrongName"));

            if (blob == null)
                throw new ArgumentNullException("blob");

            if (version == null)
                throw new ArgumentNullException("version");
            Contract.EndContractBlock();

            RuntimeAssembly rtAssembly = assembly as RuntimeAssembly;
            if (assembly != null && rtAssembly == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"), "assembly");

            m_publicKeyBlob = blob;
            m_name = name;
            m_version = version;
            m_assembly = rtAssembly;
        }

        public StrongNamePublicKeyBlob PublicKey
        {
            get
            {
                return m_publicKeyBlob;
            }
        }

        public String Name
        {
            get
            {
                return m_name;
            }
        }

        public Version Version
        {
            get
            {
                return m_version;
            }
        }

        bool IDelayEvaluatedEvidence.IsVerified
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
#if FEATURE_CAS_POLICY
                return m_assembly != null ? m_assembly.IsStrongNameVerified : true;
#else // !FEATURE_CAS_POLICY
                return true;
#endif // FEATURE_CAS_POLICY
            }
        }

        bool IDelayEvaluatedEvidence.WasUsed
        {
            get { return m_wasUsed; }
        }

        void IDelayEvaluatedEvidence.MarkUsed()
        {
            m_wasUsed = true;
        }

        internal static bool CompareNames( String asmName, String mcName )
        {
            if (mcName.Length > 0 && mcName[mcName.Length-1] == '*' && mcName.Length - 1 <= asmName.Length)
                return String.Compare( mcName, 0, asmName, 0, mcName.Length - 1, StringComparison.OrdinalIgnoreCase) == 0;
            else
                return String.Compare( mcName, asmName, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public IPermission CreateIdentityPermission( Evidence evidence )
        {
            return new StrongNameIdentityPermission( m_publicKeyBlob, m_name, m_version );
        }

        public override EvidenceBase Clone()
        {
            return new StrongName(m_publicKeyBlob, m_name, m_version);
        }

        public Object Copy()
        {
            return Clone();
        }

#if FEATURE_CAS_POLICY
        internal SecurityElement ToXml()
        {
            SecurityElement root = new SecurityElement( "StrongName" );
            root.AddAttribute( "version", "1" );

            if (m_publicKeyBlob != null)
                root.AddAttribute( "Key", System.Security.Util.Hex.EncodeHexString( m_publicKeyBlob.PublicKey ) );

            if (m_name != null)
                root.AddAttribute( "Name", m_name );

            if (m_version != null)
                root.AddAttribute( "Version", m_version.ToString() );

            return root;
        }

        internal void FromXml (SecurityElement element)
        {
            if (element == null)
                throw new ArgumentNullException("element");
            if (String.Compare(element.Tag, "StrongName", StringComparison.Ordinal) != 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidXML"));
            Contract.EndContractBlock();

            m_publicKeyBlob = null;
            m_version = null;

            string key = element.Attribute("Key");
            if (key != null)
                m_publicKeyBlob = new StrongNamePublicKeyBlob(System.Security.Util.Hex.DecodeHexString(key));

            m_name = element.Attribute("Name");

            string version = element.Attribute("Version");
            if (version != null)
                m_version = new Version(version);
        }

        public override String ToString()
        {
            return ToXml().ToString();
        }
#endif // FEATURE_CAS_POLICY

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

        // INormalizeForIsolatedStorage is not implemented for startup perf
        // equivalent to INormalizeForIsolatedStorage.Normalize()
        internal Object Normalize()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);

            bw.Write(m_publicKeyBlob.PublicKey);
            bw.Write(m_version.Major);
            bw.Write(m_name);

            ms.Position = 0;
            return ms;
        }
    }
}
