// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.Logging;
using ILLink.Shared.TrimAnalysis;
using Internal.TypeSystem;

#nullable enable

namespace ILCompiler.Dataflow
{
    public readonly record struct TrimAnalysisGenericInstantiationAccessPattern
    {
        public TypeSystemEntity Entity { get; init; }
        public MessageOrigin Origin { get; init; }

        internal TrimAnalysisGenericInstantiationAccessPattern(TypeSystemEntity entity, MessageOrigin origin)
        {
            Entity = entity;
            Origin = origin;
        }

        // No Merge - there's nothing to merge since this pattern is uniquely identified by both the origin and the entity
        // and there's only one way to "access" a generic instantiation.

        public void MarkAndProduceDiagnostics(ReflectionMarker reflectionMarker, Logger logger)
        {
            var diagnosticContext = new DiagnosticContext(
                Origin,
                logger.ShouldSuppressAnalysisWarningsForRequires(Origin.MemberDefinition, DiagnosticUtilities.RequiresUnreferencedCodeAttribute),
                logger.ShouldSuppressAnalysisWarningsForRequires(Origin.MemberDefinition, DiagnosticUtilities.RequiresDynamicCodeAttribute),
                logger.ShouldSuppressAnalysisWarningsForRequires(Origin.MemberDefinition, DiagnosticUtilities.RequiresAssemblyFilesAttribute),
                logger);

            switch (Entity)
            {
                case TypeDesc type:
                    GenericArgumentDataFlow.ProcessGenericArgumentDataFlow(diagnosticContext, reflectionMarker, type);
                    break;

                case MethodDesc method:
                    GenericArgumentDataFlow.ProcessGenericArgumentDataFlow(diagnosticContext, reflectionMarker, method);
                    break;

                case FieldDesc field:
                    GenericArgumentDataFlow.ProcessGenericArgumentDataFlow(diagnosticContext, reflectionMarker, field);
                    break;
            }
        }
    }
}
