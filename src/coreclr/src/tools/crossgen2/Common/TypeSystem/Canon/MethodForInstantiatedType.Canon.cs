// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Internal.TypeSystem
{
    // Implements canonicalization for methods on instantiated types
    partial class MethodForInstantiatedType
    {
        public override bool IsCanonicalMethod(CanonicalFormKind policy)
        {
            return OwningType.IsCanonicalSubtype(policy);
        }

        public override MethodDesc GetCanonMethodTarget(CanonicalFormKind kind)
        {
            TypeDesc canonicalizedTypeOfTargetMethod = OwningType.ConvertToCanonForm(kind);
            if (canonicalizedTypeOfTargetMethod == OwningType)
                return this;

            return Context.GetMethodForInstantiatedType(GetTypicalMethodDefinition(), (InstantiatedType)canonicalizedTypeOfTargetMethod);
        }
    }
}
