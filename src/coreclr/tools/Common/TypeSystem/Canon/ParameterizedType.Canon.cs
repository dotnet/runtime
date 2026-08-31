// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Holds code for canonicalizing a parameterized type
    public partial class ParameterizedType
    {
        public sealed override bool IsCanonicalSubtype(CanonicalFormKind policy)
        {
            return ParameterType.IsCanonicalSubtype(policy);
        }
    }
}
