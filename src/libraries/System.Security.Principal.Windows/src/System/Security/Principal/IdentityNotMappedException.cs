// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Principal
{
    public sealed class IdentityNotMappedException : Exception
    {
        private IdentityReferenceCollection _unmappedIdentities;

        public IdentityNotMappedException()
            : base(SR.IdentityReference_IdentityNotMapped)
        {
        }

        public IdentityNotMappedException(string message)
            : base(message)
        {
        }

        public IdentityNotMappedException(String message, Exception inner)
            : base(message, inner)
        {
        }

        internal IdentityNotMappedException(string message, IdentityReferenceCollection unmappedIdentities)
            : this(message)
        {
            _unmappedIdentities = unmappedIdentities;
        }

        public IdentityReferenceCollection UnmappedIdentities
        {
            get
            {
                if (_unmappedIdentities == null)
                {
                    _unmappedIdentities = new IdentityReferenceCollection();
                }
                return _unmappedIdentities;
            }
        }
    }
}
