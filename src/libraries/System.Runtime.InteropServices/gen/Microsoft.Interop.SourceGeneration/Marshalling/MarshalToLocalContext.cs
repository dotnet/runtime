// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Interop
{
    internal sealed record MarshalToLocalContext : StubCodeContext
    {
        internal StubCodeContext InnerContext { get; init; }

        internal MarshalToLocalContext(StubCodeContext inner)
        {
            InnerContext = inner;
            CurrentStage = inner.CurrentStage;
            Direction = inner.Direction;
            ParentContext = inner.ParentContext;
        }

        public override (TargetFramework framework, Version version) GetTargetFramework() => InnerContext.GetTargetFramework();

        public override bool SingleFrameSpansNativeContext => InnerContext.SingleFrameSpansNativeContext;

        public override bool AdditionalTemporaryStateLivesAcrossStages => InnerContext.AdditionalTemporaryStateLivesAcrossStages;

        public override (string managed, string native) GetIdentifiers(TypePositionInfo info)
            => InnerContext.GetIdentifiers(info);
        //=> (inner.GetIdentifiers(info).managed, inner.GetAdditionalIdentifier(info, "out"));

        public override string GetAdditionalIdentifier(TypePositionInfo info, string name) => InnerContext.GetAdditionalIdentifier(info, name);
    }
}
