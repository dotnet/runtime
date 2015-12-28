// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


// 

//
// GenericIdentity.cs
//
// A generic identity
//

using System;
using System.Diagnostics.Contracts;
using System.Security.Claims;
using System.Collections.Generic;

namespace System.Security.Principal
{
    [System.Runtime.InteropServices.ComVisible(true)]

    public class GenericIdentity : ClaimsIdentity
    {
        private string m_name;
        private string m_type;

        [SecuritySafeCritical]
        public GenericIdentity(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();

            m_name = name;
            m_type = "";

            AddNameClaim();
        }

        [SecuritySafeCritical]
        public GenericIdentity(string name, string type)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (type == null)
                throw new ArgumentNullException("type");
            Contract.EndContractBlock();

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


        [SecuritySafeCritical]
        private void AddNameClaim()
        {
            if (m_name != null)
            {
                base.AddClaim(new Claim(base.NameClaimType, m_name, ClaimValueTypes.String, ClaimsIdentity.DefaultIssuer, ClaimsIdentity.DefaultIssuer, this));
            }
        }
    }
}
