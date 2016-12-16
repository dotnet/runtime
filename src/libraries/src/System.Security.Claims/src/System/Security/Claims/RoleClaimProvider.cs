// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// 
//
// RoleClaimProvider.cs
//

using System.Collections.Generic;

namespace System.Security.Claims
{
    /// <summary>
    /// This internal class is used to wrap role claims that can be set on GenericPrincipal.  They need to be kept distinct from other claims.
    /// ClaimsIdentity has a property the holds this type.  Since it is internal, few checks are
    /// made on parameters.
    /// </summary>    

    internal class RoleClaimProvider
    {
        string m_issuer;
        string[] m_roles;
        ClaimsIdentity m_subject;

        public RoleClaimProvider(string issuer, string[] roles, ClaimsIdentity subject)
        {
            m_issuer = issuer;
            m_roles = roles;
            m_subject = subject;
        }

        public IEnumerable<Claim> Claims
        {
            get
            {
                for (int i = 0; i < m_roles.Length; i++)
                {
                    if (m_roles[i] != null)
                    {
                        yield return new Claim(m_subject.RoleClaimType, m_roles[i], ClaimValueTypes.String, m_issuer, m_issuer, m_subject);
                    }
                }
            }
        }
    }
}
