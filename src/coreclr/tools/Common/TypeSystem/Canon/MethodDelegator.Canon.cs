// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public partial class MethodDelegator
    {
        public override bool IsCanonicalMethod(CanonicalFormKind policy)
        {
            return _wrappedMethod.IsCanonicalMethod(policy);
        }

        // For this method, delegating to the wrapped MethodDesc would likely be the wrong thing.
        public abstract override MethodDesc GetCanonMethodTarget(CanonicalFormKind kind);
    }
}
