// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Interop
{
    /// <summary>
    /// A <see cref="StubCodeContext"/> that uses the "out" postfixed local variable as the native identifier.
    /// Meant to be used during the Marshal and PinnedMarshal stages for pairs of (<see cref="TypePositionInfo"/>, <see cref="StubCodeContext"/>) that return true from <see cref="MarshallerHelpers.MarshalsOut(TypePositionInfo, StubCodeContext)"/>
    /// </summary>
    internal sealed record MarshalOutContext : StubCodeContext
    {
        internal StubCodeContext InnerContext { get; init; }

        internal MarshalOutContext(StubCodeContext inner)
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
            => (InnerContext.GetIdentifiers(info).managed, InnerContext.GetAdditionalIdentifier(info, "out"));

        public override string GetAdditionalIdentifier(TypePositionInfo info, string name) => InnerContext.GetAdditionalIdentifier(info, name);
    }
}
