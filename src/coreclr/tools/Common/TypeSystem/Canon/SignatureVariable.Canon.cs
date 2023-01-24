// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    // Implements canonicalization of generic type parameters
    public partial class SignatureTypeVariable
    {
        public override bool IsCanonicalSubtype(CanonicalFormKind policy)
        {
            Debug.Fail("IsCanonicalSubtype of an indefinite type");
            return false;
        }

        protected override TypeDesc ConvertToCanonFormImpl(CanonicalFormKind kind)
        {
            Debug.Fail("ConvertToCanonFormImpl for an indefinite type");
            return this;
        }
    }

    // Implements canonicalization of generic method parameters
    public partial class SignatureMethodVariable
    {
        public override bool IsCanonicalSubtype(CanonicalFormKind policy)
        {
            Debug.Fail("IsCanonicalSubtype of an indefinite type");
            return false;
        }

        protected override TypeDesc ConvertToCanonFormImpl(CanonicalFormKind kind)
        {
            Debug.Fail("ConvertToCanonFormImpl for an indefinite type");
            return this;
        }
    }
}
