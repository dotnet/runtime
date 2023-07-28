// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Holds code for canonicalization of metadata types
    public partial class MetadataType
    {
        public override bool IsCanonicalSubtype(CanonicalFormKind policy)
        {
            return false;
        }
    }
}
