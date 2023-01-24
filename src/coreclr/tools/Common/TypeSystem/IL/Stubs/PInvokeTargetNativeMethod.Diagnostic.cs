// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.IL.Stubs
{
    public partial class PInvokeTargetNativeMethod
    {
        public override string DiagnosticName
        {
            get
            {
                return _declMethod.DiagnosticName;
            }
        }
    }
}
