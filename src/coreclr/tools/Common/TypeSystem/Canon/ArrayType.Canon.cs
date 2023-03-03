// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Implements canonicalization for arrays
    public partial class ArrayType
    {
        protected override TypeDesc ConvertToCanonFormImpl(CanonicalFormKind kind)
        {
            TypeDesc paramTypeConverted = Context.ConvertToCanon(ParameterType, kind);
            if (paramTypeConverted != ParameterType)
            {
                // Note: don't use the Rank property here, as that hides the difference
                // between a single dimensional MD array and an SZArray.
                return Context.GetArrayType(paramTypeConverted, _rank);
            }

            return this;
        }
    }

    // Implements canonicalization for array methods
    public partial class ArrayMethod
    {
        public override bool IsCanonicalMethod(CanonicalFormKind policy)
        {
            return _owningType.IsCanonicalSubtype(policy);
        }

        public override MethodDesc GetCanonMethodTarget(CanonicalFormKind kind)
        {
            TypeDesc canonicalizedTypeOfTargetMethod = _owningType.ConvertToCanonForm(kind);
            if (canonicalizedTypeOfTargetMethod == _owningType)
                return this;

            return ((ArrayType)canonicalizedTypeOfTargetMethod).GetArrayMethod(_kind);
        }
    }
}
