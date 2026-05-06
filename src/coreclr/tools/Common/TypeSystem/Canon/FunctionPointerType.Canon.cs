// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Holds code for canonicalizing a function pointer type
    public partial class FunctionPointerType
    {
        public override bool IsCanonicalSubtype(CanonicalFormKind policy)
        {
            if (_signature.ReturnType.IsCanonicalSubtype(policy))
                return true;

            for (int i = 0; i < _signature.Length; i++)
                if (_signature[i].IsCanonicalSubtype(policy))
                    return true;

            return false;
        }

        protected override TypeDesc ConvertToCanonFormImpl(CanonicalFormKind kind)
        {
            MethodSignatureBuilder sigBuilder = new MethodSignatureBuilder(_signature);
            sigBuilder.ReturnType = Context.ConvertToCanon(_signature.ReturnType, kind);
            for (int i = 0; i < _signature.Length; i++)
                sigBuilder[i] = Context.ConvertToCanon(_signature[i], kind);

            MethodSignature canonSignature = sigBuilder.ToSignature();
            if (canonSignature != _signature)
                return Context.GetFunctionPointerType(canonSignature);

            return this;
        }
    }
}
