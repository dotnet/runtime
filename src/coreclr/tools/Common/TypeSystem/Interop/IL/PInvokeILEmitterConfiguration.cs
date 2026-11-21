// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL
{
    public abstract class PInvokeILEmitterConfiguration
    {
        /// <summary>
        /// Returns linkage metadata for a direct call, or null for a runtime-resolved call.
        /// </summary>
        public abstract LinkageMetadata GetLinkageMetadata(MethodDesc method);
    }
}
