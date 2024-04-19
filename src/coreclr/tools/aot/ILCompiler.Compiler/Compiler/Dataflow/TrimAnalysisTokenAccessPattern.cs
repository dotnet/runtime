// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using ILCompiler.Logging;
using Internal.TypeSystem;

#nullable enable

namespace ILCompiler.Dataflow
{
    public readonly record struct TrimAnalysisTokenAccessPattern
    {
        public TypeSystemEntity Entity { get; init; }
        public MessageOrigin Origin { get; init; }

        internal TrimAnalysisTokenAccessPattern(TypeSystemEntity entity, MessageOrigin origin)
        {
            Entity = entity;
            Origin = origin;
        }

        // No Merge - there's nothing to merge since this pattern is uniquely identified by both the origin and the entity
        // and there's only one way to access entity by its handle.

        public void MarkAndProduceDiagnostics(ReflectionMarker reflectionMarker, Logger logger)
        {
            switch (Entity)
            {
                case MethodDesc method:
                    reflectionMarker.CheckAndWarnOnReflectionAccess(Origin, method, ReflectionMarker.AccessKind.TokenAccess);
                    break;

                case FieldDesc field:
                    reflectionMarker.CheckAndWarnOnReflectionAccess(Origin, field, ReflectionMarker.AccessKind.TokenAccess);
                    break;

                default:
                    Debug.Fail($"Unsupported entity for reflection access pattern: {Entity}");
                    break;
            }
        }
    }
}
