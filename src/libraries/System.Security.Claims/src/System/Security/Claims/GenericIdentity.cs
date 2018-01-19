// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Claims;

namespace System.Security.Principal
{
    public class GenericIdentity : ClaimsIdentity
    {
        private readonly string m_name;
        private readonly string m_type;

        public GenericIdentity(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            m_name = name;
            m_type = "";

            AddNameClaim();
        }

        public GenericIdentity(string name, string type)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            m_name = name;
            m_type = type;

            AddNameClaim();
        }

        GenericIdentity()
            : base()
        { }


        protected GenericIdentity(GenericIdentity identity)
            : base(identity)
        {
            m_name = identity.m_name;
            m_type = identity.m_type;
        }

        /// <summary>
        /// Returns a new instance of <see cref="GenericIdentity"/> with values copied from this object.
        /// </summary>
        public override ClaimsIdentity Clone()
        {
            return new GenericIdentity(this);
        }

        public override IEnumerable<Claim> Claims
        {
            get
            {
                return base.Claims;
            }
        }

        public override string Name
        {
            get
            {
                return m_name;
            }
        }

        public override string AuthenticationType
        {
            get
            {
                return m_type;
            }
        }

        public override bool IsAuthenticated
        {
            get
            {
                return !m_name.Equals("");
            }
        }

        private void AddNameClaim()
        {
            if (m_name != null)
            {
                base.AddClaim(new Claim(base.NameClaimType, m_name, ClaimValueTypes.String, ClaimsIdentity.DefaultIssuer, ClaimsIdentity.DefaultIssuer, this));
            }
        }
    }
}
