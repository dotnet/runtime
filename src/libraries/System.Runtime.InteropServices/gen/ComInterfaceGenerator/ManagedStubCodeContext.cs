// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Interop
{
    /// <summary>
    /// Stub code context for generating code that does not cross a native/managed boundary
    /// </summary>
    internal sealed record ManagedStubCodeContext : StubCodeContext
    {
        public override bool SingleFrameSpansNativeContext => throw new NotImplementedException();

        public override bool AdditionalTemporaryStateLivesAcrossStages => throw new NotImplementedException();

        public override (TargetFramework framework, Version version) GetTargetFramework() => throw new NotImplementedException();
    }
}
