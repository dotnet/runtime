// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Holds code for canonicalizing pointers
    public partial class PointerType
    {
        protected override TypeDesc ConvertToCanonFormImpl(CanonicalFormKind kind)
        {
            TypeDesc paramTypeConverted = Context.ConvertToCanon(ParameterType, kind);
            if (paramTypeConverted != ParameterType)
                return Context.GetPointerType(paramTypeConverted);

            return this;
        }
    }
}
