// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Implements canonicalizing ByRefs
    public partial class ByRefType
    {
        protected override TypeDesc ConvertToCanonFormImpl(CanonicalFormKind kind)
        {
            TypeDesc paramTypeConverted = Context.ConvertToCanon(ParameterType, kind);
            if (paramTypeConverted != ParameterType)
                return Context.GetByRefType(paramTypeConverted);

            return this;
        }
    }
}
