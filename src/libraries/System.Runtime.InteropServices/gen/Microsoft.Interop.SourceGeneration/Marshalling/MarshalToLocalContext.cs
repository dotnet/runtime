// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Interop
{
    internal sealed record MarshalToLocalContext(StubCodeContext inner) : StubCodeContext
    {
        public override bool SingleFrameSpansNativeContext => inner.SingleFrameSpansNativeContext;

        public override bool AdditionalTemporaryStateLivesAcrossStages => inner.AdditionalTemporaryStateLivesAcrossStages;

        public override (TargetFramework framework, Version version) GetTargetFramework() => inner.GetTargetFramework();
        public override (string managed, string native) GetIdentifiers(TypePositionInfo info)
            => inner.GetIdentifiers(info);
        //=> (inner.GetIdentifiers(info).managed, inner.GetAdditionalIdentifier(info, "out"));
    }
}
