// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL
{
    public abstract class PInvokeILEmitterConfiguration
    {
        public abstract bool GenerateDirectCall(MethodDesc method, out string externName);
    }
}
