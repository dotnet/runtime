// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    partial class PInvokeTargetNativeMethod
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
