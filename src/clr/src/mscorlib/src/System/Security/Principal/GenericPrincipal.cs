// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

//
//

namespace System.Security.Principal
{
    using System;
    using System.Diagnostics.Contracts;

#if !FEATURE_CORECLR
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Security.Claims;
#endif

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]

#if !FEATURE_CORECLR
    public class GenericPrincipal : ClaimsPrincipal {        
#else
    public class GenericPrincipal : IPrincipal {
#endif
        private IIdentity m_identity;
        private string[] m_roles;

        public GenericPrincipal(IIdentity identity, string[] roles) {
            if (identity == null)
                throw new ArgumentNullException("identity");
            Contract.EndContractBlock();

            m_identity = identity;
            if (roles != null) {
                m_roles = new string[roles.Length];
                for (int i = 0; i < roles.Length; ++i) {
                    m_roles[i] = roles[i];
                }
            }
            else {
                m_roles = null;
            }

#if !FEATURE_CORECLR
            AddIdentityWithRoles(m_identity, m_roles);
        }

        [OnDeserialized()]
        private void OnDeserializedMethod(StreamingContext context)
        {
            // Here it the matrix of possible serializations
            //
            // Version From  |  Version To | ClaimsIdentities | Roles
            // ============     ==========   ================   ========================================================
            // 4.0               4.5         None               We always need to add a ClaimsIdentity, if Roles add them
            //
            // 4.5               4.5         Yes                There should be a ClaimsIdentity, DebugAssert if this is not the case
            //                                                  If there are roles, attach them to the first ClaimsIdentity.
            //                                                  If there is no non-null ClaimsIdentity, add one.  However, this is unusual and may be an issue.

            ClaimsIdentity firstNonNullIdentity = null;
            foreach (var identity in base.Identities)
            {
                if (identity != null)
                {
                    firstNonNullIdentity = identity;
                    break;
                }
            }

            if (m_roles != null && m_roles.Length > 0 && firstNonNullIdentity != null)
            {
                firstNonNullIdentity.ExternalClaims.Add(new RoleClaimProvider(ClaimsIdentity.DefaultIssuer, m_roles, firstNonNullIdentity).Claims);
            }
            else if (firstNonNullIdentity == null)
            {
                AddIdentityWithRoles(m_identity, m_roles);
            }
        }

        /// <summary>
        /// helper method to add roles 
        /// </summary>
        [SecuritySafeCritical]
        void AddIdentityWithRoles(IIdentity identity, string[] roles)
        {
            ClaimsIdentity claimsIdentity = identity as ClaimsIdentity;

            if (claimsIdentity != null)
            {
                claimsIdentity = claimsIdentity.Clone();
            }
            else
            {
                claimsIdentity = new ClaimsIdentity(identity);
            }

            // if roles are not null then we need to add a provider
            if (roles != null && roles.Length > 0)
            {
                claimsIdentity.ExternalClaims.Add(new RoleClaimProvider(ClaimsIdentity.DefaultIssuer, roles, claimsIdentity).Claims);
            }

            base.AddIdentity(claimsIdentity);
        }
#else
        }
#endif


#if !FEATURE_CORECLR
        public override IIdentity Identity {
#else
        public virtual IIdentity Identity {
#endif
            get { return m_identity; }
        }

#if !FEATURE_CORECLR
        public override bool IsInRole(string role) {
#else
        public virtual bool IsInRole (string role) {
#endif
            if (role == null || m_roles == null)
                return false;

            for (int i = 0; i < m_roles.Length; ++i) {
                if (m_roles[i] != null && String.Compare(m_roles[i], role, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
            }

#if !FEATURE_CORECLR
            // it may be the case a ClaimsIdentity was passed in as the IIdentity which may have contained claims, they need to be checked.
            return base.IsInRole(role);
#else
            return false;
#endif
        }
    }
}
