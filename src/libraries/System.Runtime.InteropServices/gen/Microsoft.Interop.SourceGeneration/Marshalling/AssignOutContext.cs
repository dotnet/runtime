// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Interop
{
    public sealed record AssignOutContext : StubCodeContext
    {
        internal StubCodeContext InnerContext { get; init; }
        public string ParameterIdentifier { get; init; }

        internal AssignOutContext(StubCodeContext inner, string parameterIdentifier)
        {
            InnerContext = inner;
            Debug.Assert(inner.CurrentStage == Stage.AssignOut);
            CurrentStage = Stage.AssignOut;
            Direction = inner.Direction;
            ParentContext = inner.ParentContext;
            ParameterIdentifier = parameterIdentifier;
        }

        public override (TargetFramework framework, Version version) GetTargetFramework() => InnerContext.GetTargetFramework();

        public override bool SingleFrameSpansNativeContext => InnerContext.SingleFrameSpansNativeContext;

        public override bool AdditionalTemporaryStateLivesAcrossStages => InnerContext.AdditionalTemporaryStateLivesAcrossStages;

        public override (string managed, string native) GetIdentifiers(TypePositionInfo info)
            => (InnerContext.GetIdentifiers(info).managed, InnerContext.GetAdditionalIdentifier(info, "out"));

        public override string GetAdditionalIdentifier(TypePositionInfo info, string name) => InnerContext.GetAdditionalIdentifier(info, name);
    }
}
