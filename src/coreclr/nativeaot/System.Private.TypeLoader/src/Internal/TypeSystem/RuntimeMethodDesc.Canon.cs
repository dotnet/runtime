// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;

namespace Internal.TypeSystem.NoMetadata
{
    // Implements runtime method canonicalization
    internal partial class RuntimeMethodDesc
    {
        public override bool IsCanonicalMethod(CanonicalFormKind policy)
        {
            return OwningType.HasInstantiation && OwningType.IsCanonicalSubtype(policy);
        }

        public override MethodDesc GetCanonMethodTarget(CanonicalFormKind kind)
        {
            if (!OwningType.HasInstantiation)
                return this;

            DefType canonicalizedTypeOfTargetMethod = (DefType)OwningType.ConvertToCanonForm(kind);
            if (canonicalizedTypeOfTargetMethod == OwningType)
                return this;

            return Context.ResolveRuntimeMethod(this.UnboxingStub, canonicalizedTypeOfTargetMethod, this.NameAndSignature, IntPtr.Zero, false);
        }
    }
}
