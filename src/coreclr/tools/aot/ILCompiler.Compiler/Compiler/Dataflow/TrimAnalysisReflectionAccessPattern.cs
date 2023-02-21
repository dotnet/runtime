// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.Logging;
using Internal.TypeSystem;

namespace ILCompiler.Dataflow
{
    public readonly record struct TrimAnalysisReflectionAccessPattern
    {
        public TypeSystemEntity Entity { init; get; }
        public MessageOrigin Origin { init; get; }

        internal TrimAnalysisReflectionAccessPattern(TypeSystemEntity entity, MessageOrigin origin)
        {
            Entity = entity;
            Origin = origin;
        }

        // No Merge - there's nothing to merge since this pattern is unequily identified by both the origin and the entity
        // and there's only one way to "reflection access" an entity.

        public void MarkAndProduceDiagnostics(ReflectionMarker reflectionMarker, Logger logger)
        {
            reflectionMarker.CheckAndWarnOnReflectionAccess(Origin, Entity);
        }
    }
}
