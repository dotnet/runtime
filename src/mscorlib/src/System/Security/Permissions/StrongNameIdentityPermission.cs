// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    using System;
#if FEATURE_CAS_POLICY
    using SecurityElement = System.Security.SecurityElement;
#endif // FEATURE_CAS_POLICY
    using System.Security.Util;
    using System.IO;
    using String = System.String;
    using Version = System.Version;
    using System.Security.Policy;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    // The only difference between this class and System.Security.Policy.StrongName is that this one
    // allows m_name to be null.  We should merge this class with System.Security.Policy.StrongName
    [Serializable]
    sealed internal class StrongName2
    {
        public StrongNamePublicKeyBlob m_publicKeyBlob;
        public String m_name;
        public Version m_version;

        public StrongName2(StrongNamePublicKeyBlob publicKeyBlob, String name, Version version)
        {
            m_publicKeyBlob = publicKeyBlob;
            m_name = name;
            m_version = version;
        }

        public StrongName2 Copy()
        {
            return new StrongName2(m_publicKeyBlob, m_name, m_version);
        }

        public bool IsSubsetOf(StrongName2 target)
        {
            // This StrongName2 is a subset of the target if it's public key blob is null no matter what
            if (this.m_publicKeyBlob == null)
                return true;

            // Subsets are always false if the public key blobs do not match
            if (!this.m_publicKeyBlob.Equals( target.m_publicKeyBlob ))
                return false;

            // We use null in strings to represent the "Anything" state.
            // Therefore, the logic to detect an individual subset is:
            //
            // 1. If the this string is null ("Anything" is a subset of any other).
            // 2. If the this string and target string are the same (equality is sufficient for a subset).
            //
            // The logic is reversed here to discover things that are not subsets.
            if (this.m_name != null)
            {
                if (target.m_name == null || !System.Security.Policy.StrongName.CompareNames( target.m_name, this.m_name ))
                    return false;
            }

            if ((Object) this.m_version != null)
            {
                if ((Object) target.m_version == null ||
                    target.m_version.CompareTo( this.m_version ) != 0)
                {
                    return false;
                }
            }

            return true;
        }
        
        public StrongName2 Intersect(StrongName2 target)
        {
            if (target.IsSubsetOf( this ))
                return target.Copy();
            else if (this.IsSubsetOf( target ))
                return this.Copy();
            else
                return null;
        }

        public bool Equals(StrongName2 target)
        {
            if (!target.IsSubsetOf(this))
                return false;
            if (!this.IsSubsetOf(target))
                return false;
            return true;
        }
    }



[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    sealed public class StrongNameIdentityPermission : CodeAccessPermission, IBuiltInPermission
    {
        //------------------------------------------------------
        //
        // PRIVATE STATE DATA
        //
        //------------------------------------------------------

        private bool m_unrestricted;
        private StrongName2[] m_strongNames;

        //------------------------------------------------------
        //
        // PUBLIC CONSTRUCTORS
        //
        //------------------------------------------------------


        public StrongNameIdentityPermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
            {
                m_unrestricted = true;
            }
            else if (state == PermissionState.None)
            {
                m_unrestricted = false;
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
            }
        }

        public StrongNameIdentityPermission( StrongNamePublicKeyBlob blob, String name, Version version )
        {
            if (blob == null)
                throw new ArgumentNullException( "blob" );
            if (name != null && name.Equals( "" ))
                throw new ArgumentException( Environment.GetResourceString( "Argument_EmptyStrongName" ) );      
            Contract.EndContractBlock();
            m_unrestricted = false;
            m_strongNames = new StrongName2[1];
            m_strongNames[0] = new StrongName2(blob, name, version);
        }


        //------------------------------------------------------
        //
        // PUBLIC ACCESSOR METHODS
        //
        //------------------------------------------------------

        public StrongNamePublicKeyBlob PublicKey
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException( "PublicKey" );
                Contract.EndContractBlock();
                m_unrestricted = false;
                if(m_strongNames != null && m_strongNames.Length == 1)
                    m_strongNames[0].m_publicKeyBlob = value;
                else
                {
                    m_strongNames = new StrongName2[1];
                    m_strongNames[0] = new StrongName2(value, "", new Version());                   
                }
            }

            get
            {
                if(m_strongNames == null || m_strongNames.Length == 0)
                    return null;
                if(m_strongNames.Length > 1)
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_AmbiguousIdentity"));
                return m_strongNames[0].m_publicKeyBlob;
            }
        }

        public String Name
        {
            set
            {
                if (value != null && value.Length == 0)
                    throw new ArgumentException( Environment.GetResourceString("Argument_EmptyName" ));    
                Contract.EndContractBlock();
                m_unrestricted = false;
                if(m_strongNames != null && m_strongNames.Length == 1)
                    m_strongNames[0].m_name = value;
                else
                {
                    m_strongNames = new StrongName2[1];
                    m_strongNames[0] = new StrongName2(null, value, new Version());                 
                }
            }                    

            get
            {
                if(m_strongNames == null || m_strongNames.Length == 0)
                    return "";
                if(m_strongNames.Length > 1)
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_AmbiguousIdentity"));
                return m_strongNames[0].m_name;
            }
        }

        public Version Version
        {
            set
            {
                m_unrestricted = false;
                if(m_strongNames != null && m_strongNames.Length == 1)
                    m_strongNames[0].m_version = value;
                else
                {
                    m_strongNames = new StrongName2[1];
                    m_strongNames[0] = new StrongName2(null, "", value);
                }
            }
            
            get
            {
                if(m_strongNames == null || m_strongNames.Length == 0)
                    return new Version();
                if(m_strongNames.Length > 1)
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_AmbiguousIdentity"));
                return m_strongNames[0].m_version;
            }
        }

        //------------------------------------------------------
        //
        // PRIVATE AND PROTECTED HELPERS FOR ACCESSORS AND CONSTRUCTORS
        //
        //------------------------------------------------------
    
        //------------------------------------------------------
        //
        // CODEACCESSPERMISSION IMPLEMENTATION
        //
        //------------------------------------------------------
        
        //------------------------------------------------------
        //
        // IPERMISSION IMPLEMENTATION
        //
        //------------------------------------------------------


        public override IPermission Copy()
        {
            StrongNameIdentityPermission perm = new StrongNameIdentityPermission(PermissionState.None);
            perm.m_unrestricted = this.m_unrestricted;
            if(this.m_strongNames != null)
            {
                perm.m_strongNames = new StrongName2[this.m_strongNames.Length];
                int n;
                for(n = 0; n < this.m_strongNames.Length; n++)
                    perm.m_strongNames[n] = this.m_strongNames[n].Copy();
            }
            return perm;
        }

        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
            {
                if(m_unrestricted)
                    return false;
                if(m_strongNames == null)
                    return true;
                if(m_strongNames.Length == 0)
                    return true;
                return false;
            }
            StrongNameIdentityPermission that = target as StrongNameIdentityPermission;
            if(that == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            if(that.m_unrestricted)
                return true;
            if(m_unrestricted)
                return false;
            if(this.m_strongNames != null)
            {
                foreach(StrongName2 snThis in m_strongNames)
                {
                    bool bOK = false;
                    if(that.m_strongNames != null)
                    {
                        foreach(StrongName2 snThat in that.m_strongNames)
                        {
                            if(snThis.IsSubsetOf(snThat))
                            {
                                bOK = true;
                                break;
                            }
                        }
                    }
                    if(!bOK)
                        return false;           
                }
            }
            return true;
        }



        public override IPermission Intersect(IPermission target)
        {
            if (target == null)
                return null;
            StrongNameIdentityPermission that = target as StrongNameIdentityPermission;
            if(that == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            if(this.m_unrestricted && that.m_unrestricted)
            {
                StrongNameIdentityPermission res = new StrongNameIdentityPermission(PermissionState.None);
                res.m_unrestricted = true;
                return res;
            }
            if(this.m_unrestricted)
                return that.Copy();
            if(that.m_unrestricted)
                return this.Copy();
            if(this.m_strongNames == null || that.m_strongNames == null || this.m_strongNames.Length == 0 || that.m_strongNames.Length == 0)
                return null;
            List<StrongName2> alStrongNames = new List<StrongName2>();
            foreach(StrongName2 snThis in this.m_strongNames)
            {
                foreach(StrongName2 snThat in that.m_strongNames)
                {
                    StrongName2 snInt = (StrongName2)snThis.Intersect(snThat);
                    if(snInt != null)
                        alStrongNames.Add(snInt);
                }
            }
            if(alStrongNames.Count == 0)
                return null;
            StrongNameIdentityPermission result = new StrongNameIdentityPermission(PermissionState.None);
            result.m_strongNames = alStrongNames.ToArray();
            return result;
        }

        public override IPermission Union(IPermission target)
        {
            if (target == null)
            {
                if((this.m_strongNames == null || this.m_strongNames.Length == 0) && !this.m_unrestricted)
                    return null;
                return this.Copy();
            }
            StrongNameIdentityPermission that = target as StrongNameIdentityPermission;
            if(that == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            if(this.m_unrestricted || that.m_unrestricted)
            {
                StrongNameIdentityPermission res = new StrongNameIdentityPermission(PermissionState.None);
                res.m_unrestricted = true;
                return res;
            }
            if (this.m_strongNames == null || this.m_strongNames.Length == 0)
            {
                if(that.m_strongNames == null || that.m_strongNames.Length == 0)
                    return null;
                return that.Copy();
            }
            if(that.m_strongNames == null || that.m_strongNames.Length == 0)
                return this.Copy();
            List<StrongName2> alStrongNames = new List<StrongName2>();
            foreach(StrongName2 snThis in this.m_strongNames)
                alStrongNames.Add(snThis);
            foreach(StrongName2 snThat in that.m_strongNames)
            {
                bool bDupe = false;
                foreach(StrongName2 sn in alStrongNames)
                {
                    if(snThat.Equals(sn))
                    {
                        bDupe = true;
                        break;
                    }
                }
                if(!bDupe)
                    alStrongNames.Add(snThat);
            }
            StrongNameIdentityPermission result = new StrongNameIdentityPermission(PermissionState.None);
            result.m_strongNames = alStrongNames.ToArray();
            return result;
        }

#if FEATURE_CAS_POLICY
        public override void FromXml(SecurityElement e)
        {
            m_unrestricted = false;
            m_strongNames = null;
            CodeAccessPermission.ValidateElement( e, this );
            String unr = e.Attribute( "Unrestricted" );
            if(unr != null && String.Compare(unr, "true", StringComparison.OrdinalIgnoreCase) == 0)
            {
                m_unrestricted = true;
                return;
            }
            String elBlob = e.Attribute("PublicKeyBlob");
            String elName = e.Attribute("Name");
            String elVersion = e.Attribute("AssemblyVersion");
            StrongName2 sn;
            List<StrongName2> al = new List<StrongName2>();
            if(elBlob != null || elName != null || elVersion != null)
            {
                sn = new StrongName2(
                                    (elBlob == null ? null : new StrongNamePublicKeyBlob(elBlob)), 
                                    elName, 
                                    (elVersion == null ? null : new Version(elVersion)));
                al.Add(sn);
            }
            ArrayList alChildren = e.Children;
            if(alChildren != null)
            {
                foreach(SecurityElement child in alChildren)
                {
                    elBlob = child.Attribute("PublicKeyBlob");
                    elName = child.Attribute("Name");
                    elVersion = child.Attribute("AssemblyVersion");
                    if(elBlob != null || elName != null || elVersion != null)
                    {
                        sn = new StrongName2(
                                            (elBlob == null ? null : new StrongNamePublicKeyBlob(elBlob)), 
                                            elName, 
                                            (elVersion == null ? null : new Version(elVersion)));
                        al.Add(sn);
                    }
                }
            }
            if(al.Count != 0)
                m_strongNames = al.ToArray();
        }

        public override SecurityElement ToXml()
        {
            SecurityElement esd = CodeAccessPermission.CreatePermissionElement( this, "System.Security.Permissions.StrongNameIdentityPermission" );
            if (m_unrestricted)
                esd.AddAttribute( "Unrestricted", "true" );
            else if (m_strongNames != null)
            {
                if (m_strongNames.Length == 1)
                {
                    if (m_strongNames[0].m_publicKeyBlob != null)
                        esd.AddAttribute("PublicKeyBlob", Hex.EncodeHexString(m_strongNames[0].m_publicKeyBlob.PublicKey));
                    if (m_strongNames[0].m_name != null)
                        esd.AddAttribute("Name", m_strongNames[0].m_name);
                    if ((Object)m_strongNames[0].m_version != null)
                        esd.AddAttribute("AssemblyVersion", m_strongNames[0].m_version.ToString());
                }
                else
                {
                    int n;
                    for(n = 0; n < m_strongNames.Length; n++)
                    {
                        SecurityElement child = new SecurityElement("StrongName");
                        if (m_strongNames[n].m_publicKeyBlob != null)
                            child.AddAttribute("PublicKeyBlob", Hex.EncodeHexString(m_strongNames[n].m_publicKeyBlob.PublicKey));
                        if (m_strongNames[n].m_name != null)
                            child.AddAttribute("Name", m_strongNames[n].m_name);
                        if ((Object)m_strongNames[n].m_version != null)
                            child.AddAttribute("AssemblyVersion", m_strongNames[n].m_version.ToString());
                        esd.AddChild(child);
                    }
                }
            }
            return esd;
        }
#endif // FEATURE_CAS_POLICY

        /// <internalonly/>
        int IBuiltInPermission.GetTokenIndex()
        {
            return StrongNameIdentityPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.StrongNameIdentityPermissionIndex;
        }
            
    }
}
