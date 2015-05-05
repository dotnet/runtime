// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
