// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public partial class MethodDelegator
    {
        public override bool IsPInvoke => _wrappedMethod.IsPInvoke;

        public override PInvokeMetadata GetPInvokeMethodMetadata()
        {
            return _wrappedMethod.GetPInvokeMethodMetadata();
        }

        public override ParameterMetadata[] GetParameterMetadata()
        {
            return _wrappedMethod.GetParameterMetadata();
        }
    }
}
