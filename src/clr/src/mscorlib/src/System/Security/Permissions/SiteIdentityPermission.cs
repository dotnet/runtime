// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    using System;
    using SiteString = System.Security.Util.SiteString;
    using System.Text;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.Serialization;

    [System.Runtime.InteropServices.ComVisible(true)]
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
