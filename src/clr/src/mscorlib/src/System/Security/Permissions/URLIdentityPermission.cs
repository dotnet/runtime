// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    using System;
    using System.Security.Util;
    using System.IO;
    using System.Text;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Diagnostics.Contracts;

    [System.Runtime.InteropServices.ComVisible(true)]
    sealed public class UrlIdentityPermission : CodeAccessPermission, IBuiltInPermission
    {
        //------------------------------------------------------
        //
        // PRIVATE STATE DATA
        //
        //------------------------------------------------------

        [OptionalField(VersionAdded = 2)]
        private bool m_unrestricted;
        [OptionalField(VersionAdded = 2)]
        private URLString[] m_urls;

        //------------------------------------------------------
        //
        // PUBLIC CONSTRUCTORS
        //
        //------------------------------------------------------


        public UrlIdentityPermission(PermissionState state)
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

        public UrlIdentityPermission( String site )
        {
            if (site == null)
                throw new ArgumentNullException( nameof(site) );
            Contract.EndContractBlock();
            Url = site;
        }

        internal UrlIdentityPermission( URLString site )
        {
            m_unrestricted = false;
            m_urls = new URLString[1];
            m_urls[0] = site;
        }

        // Internal function to append all the urls in m_urls to the input originList
        internal void AppendOrigin(ArrayList originList)
        {
            if (m_urls == null)
                originList.Add("");
            else
            {
                int n;
                for(n = 0; n < this.m_urls.Length; n++)        
                    originList.Add(m_urls[n].ToString());
            }
        }

        //------------------------------------------------------
        //
        // PUBLIC ACCESSOR METHODS
        //
        //------------------------------------------------------

        public String Url
        {
            set
            {
                m_unrestricted = false;
                if(value == null || value.Length == 0)
                    m_urls = null;
                else
                {
                    m_urls = new URLString[1];
                    m_urls[0] = new URLString( value );
                }
            }

            get
            {
                if(m_urls == null)
                    return "";
                if(m_urls.Length == 1)
                    return m_urls[0].ToString();
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
            UrlIdentityPermission perm = new UrlIdentityPermission( PermissionState.None );
            perm.m_unrestricted = this.m_unrestricted;
            if (this.m_urls != null)
            {
                perm.m_urls = new URLString[this.m_urls.Length];
                int n;
                for(n = 0; n < this.m_urls.Length; n++)
                    perm.m_urls[n] = (URLString)this.m_urls[n].Copy();
            }
            return perm;
        }

        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
            {
                if(m_unrestricted)
                    return false;
                if(m_urls == null)
                    return true;
                if(m_urls.Length == 0)
                    return true;
                return false;
            }
            UrlIdentityPermission that = target as UrlIdentityPermission;
            if(that == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            if(that.m_unrestricted)
                return true;
            if(m_unrestricted)
                return false;
            if(this.m_urls != null)
            {
                foreach(URLString usThis in this.m_urls)
                {
                    bool bOK = false;
                    if(that.m_urls != null)
                    {
                        foreach(URLString usThat in that.m_urls)
                        {
                            if(usThis.IsSubsetOf(usThat))
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
            UrlIdentityPermission that = target as UrlIdentityPermission;
            if(that == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            if(this.m_unrestricted && that.m_unrestricted)
            {
                UrlIdentityPermission res = new UrlIdentityPermission(PermissionState.None);
                res.m_unrestricted = true;
                return res;
            }
            if(this.m_unrestricted)
                return that.Copy();
            if(that.m_unrestricted)
                return this.Copy();
            if(this.m_urls == null || that.m_urls == null || this.m_urls.Length == 0 || that.m_urls.Length == 0)
                return null;
            List<URLString> alUrls = new List<URLString>();
            foreach(URLString usThis in this.m_urls)
            {
                foreach(URLString usThat in that.m_urls)
                {
                    URLString usInt = (URLString)usThis.Intersect(usThat);
                    if(usInt != null)
                        alUrls.Add(usInt);
                }
            }
            if(alUrls.Count == 0)
                return null;
            UrlIdentityPermission result = new UrlIdentityPermission(PermissionState.None);
            result.m_urls = alUrls.ToArray();
            return result;
        }

        public override IPermission Union(IPermission target)
        {
            if (target == null)
            {
                if((this.m_urls == null || this.m_urls.Length == 0) && !this.m_unrestricted)
                    return null;
                return this.Copy();
            }
            UrlIdentityPermission that = target as UrlIdentityPermission;
            if(that == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            if(this.m_unrestricted || that.m_unrestricted)
            {
                UrlIdentityPermission res = new UrlIdentityPermission(PermissionState.None);
                res.m_unrestricted = true;
                return res;
            }
            if (this.m_urls == null || this.m_urls.Length == 0)
            {
                if(that.m_urls == null || that.m_urls.Length == 0)
                    return null;
                return that.Copy();
            }
            if(that.m_urls == null || that.m_urls.Length == 0)
                return this.Copy();
            List<URLString> alUrls = new List<URLString>();
            foreach(URLString usThis in this.m_urls)
                alUrls.Add(usThis);
            foreach(URLString usThat in that.m_urls)
            {
                bool bDupe = false;
                foreach(URLString us in alUrls)
                {
                    if(usThat.Equals(us))
                    {
                        bDupe = true;
                        break;
                    }
                }
                if(!bDupe)
                    alUrls.Add(usThat);
            }
            UrlIdentityPermission result = new UrlIdentityPermission(PermissionState.None);
            result.m_urls = alUrls.ToArray();
            return result;
        }

        /// <internalonly/>
        int IBuiltInPermission.GetTokenIndex()
        {
            return UrlIdentityPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.UrlIdentityPermissionIndex;
        }        
    }
}
