// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    using System;
#if FEATURE_CAS_POLICY
    using SecurityElement = System.Security.SecurityElement;
#endif // FEATURE_CAS_POLICY
    using SiteString = System.Security.Util.SiteString;
    using System.Text;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.Serialization;

    [System.Runtime.InteropServices.ComVisible(true)]
#if FEATURE_SERIALIZATION
    [Serializable]
#endif
    sealed public class SiteIdentityPermission : CodeAccessPermission, IBuiltInPermission
    {
        //------------------------------------------------------
        //
        // PRIVATE STATE DATA
        //
        //------------------------------------------------------
        [OptionalField(VersionAdded = 2)]
        private bool m_unrestricted;
        [OptionalField(VersionAdded = 2)]        
        private SiteString[] m_sites;

#if FEATURE_REMOTING
        // This field will be populated only for non X-AD scenarios where we create a XML-ised string of the Permission
        [OptionalField(VersionAdded = 2)]
        private String m_serializedPermission; 

        //  This field is legacy info from v1.x and is never used in v2.0 and beyond: purely for serialization purposes
        private SiteString m_site;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            // v2.0 and beyond XML case
            if (m_serializedPermission != null)
            {
                FromXml(SecurityElement.FromString(m_serializedPermission));
                m_serializedPermission = null;
            }
            else if (m_site != null) //v1.x case where we read the m_site value
            {
                m_unrestricted = false;
                m_sites = new SiteString[1];
                m_sites[0] = m_site;
                m_site = null;
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext ctx)
        {

            if ((ctx.State & ~(StreamingContextStates.Clone|StreamingContextStates.CrossAppDomain)) != 0)
            {
                m_serializedPermission = ToXml().ToString(); //for the v2 and beyond case
                if (m_sites != null && m_sites.Length == 1) // for the v1.x case
                    m_site = m_sites[0];
                
            }
        }   
        [OnSerialized]
        private void OnSerialized(StreamingContext ctx)
        {
            if ((ctx.State & ~(StreamingContextStates.Clone|StreamingContextStates.CrossAppDomain)) != 0)
            {
                m_serializedPermission = null;
                m_site = null;
            }
        }
#endif // FEATURE_REMOTING

        //------------------------------------------------------
        //
        // PUBLIC CONSTRUCTORS
        //
        //------------------------------------------------------
        
       
        public SiteIdentityPermission(PermissionState state)
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
        
        public SiteIdentityPermission( String site )
        {
            Site = site;
        }
        
        //------------------------------------------------------
        //
        // PUBLIC ACCESSOR METHODS
        //
        //------------------------------------------------------

        public String Site
        {
            set
            {
                m_unrestricted = false;
                m_sites = new SiteString[1];
                m_sites[0] = new SiteString( value );
            }

            get
            {
                if(m_sites == null)
                    return "";
                if(m_sites.Length == 1)
                    return m_sites[0].ToString();
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_AmbiguousIdentity"));
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
            SiteIdentityPermission perm = new SiteIdentityPermission( PermissionState.None );
            perm.m_unrestricted = this.m_unrestricted;
            if (this.m_sites != null)
            {
                perm.m_sites = new SiteString[this.m_sites.Length];
                int n;
                for(n = 0; n < this.m_sites.Length; n++)
                    perm.m_sites[n] = (SiteString)this.m_sites[n].Copy();
            }
            return perm;
        }
        
        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
            {
                if(m_unrestricted)
                    return false;
                if(m_sites == null)
                    return true;
                if(m_sites.Length == 0)
                    return true;
                return false;
            }
            SiteIdentityPermission that = target as SiteIdentityPermission;
            if(that == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            if(that.m_unrestricted)
                return true;
            if(m_unrestricted)
                return false;
            if(this.m_sites != null)
            {
                foreach(SiteString ssThis in this.m_sites)
                {
                    bool bOK = false;
                    if(that.m_sites != null)
                    {
                        foreach(SiteString ssThat in that.m_sites)
                        {
                            if(ssThis.IsSubsetOf(ssThat))
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
            SiteIdentityPermission that = target as SiteIdentityPermission;
            if(that == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            if(this.m_unrestricted && that.m_unrestricted)
            {
                SiteIdentityPermission res = new SiteIdentityPermission(PermissionState.None);
                res.m_unrestricted = true;
                return res;
            }
            if(this.m_unrestricted)
                return that.Copy();
            if(that.m_unrestricted)
                return this.Copy();
            if(this.m_sites == null || that.m_sites == null || this.m_sites.Length == 0 || that.m_sites.Length == 0)
                return null;
            List<SiteString> alSites = new List<SiteString>();
            foreach(SiteString ssThis in this.m_sites)
            {
                foreach(SiteString ssThat in that.m_sites)
                {
                    SiteString ssInt = (SiteString)ssThis.Intersect(ssThat);
                    if(ssInt != null)
                        alSites.Add(ssInt);
                }
            }
            if(alSites.Count == 0)
                return null;
            SiteIdentityPermission result = new SiteIdentityPermission(PermissionState.None);
            result.m_sites = alSites.ToArray();
            return result;
        }
        
        public override IPermission Union(IPermission target)
        {
            if (target == null)
            {
                if((this.m_sites == null || this.m_sites.Length == 0) && !this.m_unrestricted)
                    return null;
                return this.Copy();
            }
            SiteIdentityPermission that = target as SiteIdentityPermission;
            if(that == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            if(this.m_unrestricted || that.m_unrestricted)
            {
                SiteIdentityPermission res = new SiteIdentityPermission(PermissionState.None);
                res.m_unrestricted = true;
                return res;
            }
            if (this.m_sites == null || this.m_sites.Length == 0)
            {
                if(that.m_sites == null || that.m_sites.Length == 0)
                    return null;
                return that.Copy();
            }
            if(that.m_sites == null || that.m_sites.Length == 0)
                return this.Copy();
            List<SiteString> alSites = new List<SiteString>();
            foreach(SiteString ssThis in this.m_sites)
                alSites.Add(ssThis);
            foreach(SiteString ssThat in that.m_sites)
            {
                bool bDupe = false;
                foreach(SiteString ss in alSites)
                {
                    if(ssThat.Equals(ss))
                    {
                        bDupe = true;
                        break;
                    }
                }
                if(!bDupe)
                    alSites.Add(ssThat);
            }
            SiteIdentityPermission result = new SiteIdentityPermission(PermissionState.None);
            result.m_sites = alSites.ToArray();
            return result;
        }

#if FEATURE_CAS_POLICY
        public override void FromXml(SecurityElement esd)
        {
            m_unrestricted = false;
            m_sites = null;
            CodeAccessPermission.ValidateElement( esd, this );
            String unr = esd.Attribute( "Unrestricted" );
            if(unr != null && String.Compare(unr, "true", StringComparison.OrdinalIgnoreCase) == 0)
            {
                m_unrestricted = true;
                return;
            }
            String elem = esd.Attribute( "Site" );
            List<SiteString> al = new List<SiteString>();
            if(elem != null)
                al.Add(new SiteString( elem ));
            ArrayList alChildren = esd.Children;
            if(alChildren != null)
            {
                foreach(SecurityElement child in alChildren)
                {
                    elem = child.Attribute( "Site" );
                    if(elem != null)
                        al.Add(new SiteString( elem ));
                }
            }
            if(al.Count != 0)
                m_sites = al.ToArray();
        }

        public override SecurityElement ToXml()
        {
            SecurityElement esd = CodeAccessPermission.CreatePermissionElement( this, "System.Security.Permissions.SiteIdentityPermission" );
            if (m_unrestricted)
                esd.AddAttribute( "Unrestricted", "true" );
            else if (m_sites != null)
            {
                if (m_sites.Length == 1)
                    esd.AddAttribute( "Site", m_sites[0].ToString() );
                else
                {
                    int n;
                    for(n = 0; n < m_sites.Length; n++)
                    {
                        SecurityElement child = new SecurityElement("Site");
                        child.AddAttribute( "Site", m_sites[n].ToString() );
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
            return SiteIdentityPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.SiteIdentityPermissionIndex;
        }
    }
}
