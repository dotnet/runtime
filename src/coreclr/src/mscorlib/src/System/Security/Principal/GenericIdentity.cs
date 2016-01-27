// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

//
//
// A generic identity
//

namespace System.Security.Principal
{
    using System;
    using System.Diagnostics.Contracts;

// Claims feature is not available in Silverlight
#if !FEATURE_CORECLR                         
    using System.Security.Claims;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
#endif

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]

#if!FEATURE_CORECLR
    public class GenericIdentity  : ClaimsIdentity {
#else
    public class GenericIdentity : IIdentity {
#endif

        private string m_name;
        private string m_type;

#if !FEATURE_CORECLR
        [SecuritySafeCritical]
#endif
        public GenericIdentity (string name) {
            if (name == null)
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();

            m_name = name;
            m_type = "";

#if !FEATURE_CORECLR
            AddNameClaim();
#endif
        }

#if !FEATURE_CORECLR
        [SecuritySafeCritical]
#endif
        public GenericIdentity (string name, string type) {
            if (name == null)
                throw new ArgumentNullException("name");
            if (type == null)
                throw new ArgumentNullException("type");
            Contract.EndContractBlock();

            m_name = name;
            m_type = type;

#if !FEATURE_CORECLR
            AddNameClaim();
#endif
        }

#if !FEATURE_CORECLR
        GenericIdentity()
            : base()
        { }
#endif

#if !FEATURE_CORECLR

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

#endif

#if !FEATURE_CORECLR
        public override string Name {
#else
        public virtual string Name {
#endif
            get {
                return m_name;
            }
        }

#if !FEATURE_CORECLR
        public override string AuthenticationType {
#else
        public virtual string AuthenticationType {
#endif
            get {
                return m_type;
            }
        }

#if !FEATURE_CORECLR
        public override bool IsAuthenticated {
#else
        public virtual bool IsAuthenticated {
#endif
            get {
                return !m_name.Equals("");
            } 
        }

#if !FEATURE_CORECLR
        [OnDeserialized()]
        private void OnDeserializedMethod(StreamingContext context)
        {
            // GenericIdentities that have been deserialized from a .net 4.0 runtime, will not have any claims. 
            // In this case add a name claim, otherwise assume it was deserialized.
            bool claimFound = false;
            foreach (Claim c in base.Claims)
            {
                claimFound = true;
                break;
            }

            if (!claimFound)
            {
                AddNameClaim();
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
#endif // #if !FEATURE_CORECLR
    }
}
