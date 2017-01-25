// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using System.Security.Claims;

namespace System.Security.Principal
{
    [Serializable]
    public class GenericPrincipal : ClaimsPrincipal
    {
        private readonly IIdentity m_identity;
        private readonly string[] m_roles;

        public GenericPrincipal(IIdentity identity, string[] roles)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));
            Contract.EndContractBlock();

            m_identity = identity;
            if (roles != null)
            {
                m_roles = (string[])roles.Clone();
            }
            else
            {
                m_roles = null;
            }

            AddIdentityWithRoles(m_identity, m_roles);
        }

        [OnDeserialized]
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
            //                                                  If there is no non-null ClaimsIdentity, add one.  However, this is unusual and may be a 

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
                List<Claim> roleClaims = new List<Claim>(m_roles.Length);

                foreach (string role in m_roles)
                {
                    if (!string.IsNullOrWhiteSpace(role))
                    {
                        roleClaims.Add(new Claim(firstNonNullIdentity.RoleClaimType, role, ClaimValueTypes.String, ClaimsIdentity.DefaultIssuer, ClaimsIdentity.DefaultIssuer, firstNonNullIdentity));
                    }
                }

                firstNonNullIdentity.ExternalClaims.Add(roleClaims);
            }
            else if (firstNonNullIdentity == null)
            {
                AddIdentityWithRoles(m_identity, m_roles);
            }
        }

        /// <summary>
        /// helper method to add roles 
        /// </summary>
        private void AddIdentityWithRoles(IIdentity identity, string[] roles)
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
            if (roles != null && roles.Length > 0)
            {
                List<Claim> roleClaims = new List<Claim>(roles.Length);

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
                if (string.Equals(m_roles[i], role, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // it may be the case a ClaimsIdentity was passed in as the IIdentity which may have contained claims, they need to be checked.
            return base.IsInRole(role);
        }
    }
}
