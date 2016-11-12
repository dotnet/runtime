// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Security.Claims;

namespace System.Security.Principal
{
    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class GenericPrincipal : ClaimsPrincipal
    {
        private IIdentity m_identity;
        private string[] m_roles;

        public GenericPrincipal(IIdentity identity, string[] roles)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));
            Contract.EndContractBlock();

            m_identity = identity;
            if (roles != null)
            {
                m_roles = new string[roles.Length];
                for (int i = 0; i < roles.Length; ++i)
                {
                    m_roles[i] = roles[i];
                }
            }
            else
            {
                m_roles = null;
            }

            AddIdentityWithRoles(m_identity, m_roles);
        }

        /// <summary>
        /// helper method to add roles 
        /// </summary>
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

            // Add 'roles' as external claims so they are not serialized
            // TODO - brentsch, we should be able to replace GenericPrincipal and GenericIdentity with ClaimsPrincipal and ClaimsIdentity
            // hence I am not too concerned about perf.
            List<Claim> roleClaims = new List<Claim>();
            if (roles != null && roles.Length > 0)
            {
                foreach (string role in roles)
                {
                    if (!string.IsNullOrWhiteSpace(role))
                    {
                        roleClaims.Add(new Claim(claimsIdentity.RoleClaimType, role, ClaimValueTypes.String, ClaimsIdentity.DefaultIssuer, ClaimsIdentity.DefaultIssuer, claimsIdentity));
                    }
                }

                claimsIdentity.ExternalClaims.Add(roleClaims);
            }

            base.AddIdentity(claimsIdentity);
        }

        public override IIdentity Identity
        {
            get { return m_identity; }
        }

        public override bool IsInRole(string role)
        {
            if (role == null || m_roles == null)
                return false;

            for (int i = 0; i < m_roles.Length; ++i)
            {
                if (m_roles[i] != null && String.Compare(m_roles[i], role, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
            }

            // it may be the case a ClaimsIdentity was passed in as the IIdentity which may have contained claims, they need to be checked.
            return base.IsInRole(role);
        }
    }
}
