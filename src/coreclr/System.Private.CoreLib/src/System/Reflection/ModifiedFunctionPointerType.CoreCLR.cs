// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal partial class ModifiedFunctionPointerType
    {
        private MdSigCallingConvention GetCallingConvention()
        {
            Signature? signature = GetSignature();
            if (signature is not null)
            {
                return (MdSigCallingConvention)signature.GetCallingConventionFromFunctionPointer(RootSignatureParameterIndex, NestedSignatureIndex);
            }

            return MdSigCallingConvention.Default;
        }
    }
}
