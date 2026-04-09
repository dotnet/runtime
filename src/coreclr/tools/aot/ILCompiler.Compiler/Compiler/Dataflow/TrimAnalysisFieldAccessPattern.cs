// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.Logging;
using ILLink.Shared.TrimAnalysis;
using Internal.TypeSystem;

namespace ILCompiler.Dataflow
{
    public readonly record struct TrimAnalysisFieldAccessPattern
    {
        public FieldDesc Field { get; init; }
        public MessageOrigin Origin { get; init; }

        public TrimAnalysisFieldAccessPattern(FieldDesc field, MessageOrigin origin)
        {
            Field = field;
            Origin = origin;
        }

        // No Merge - there's nothing to merge since this pattern is uniquely identified by both the origin and the entity
        // and there's only one way to "access" a field.

        public void MarkAndProduceDiagnostics(ReflectionMarker reflectionMarker, Logger logger)
        {
            var diagnosticContext = new DiagnosticContext(
                Origin,
                logger.ShouldSuppressAnalysisWarningsForRequires(Origin.MemberDefinition, DiagnosticUtilities.RequiresUnreferencedCodeAttribute),
                logger.ShouldSuppressAnalysisWarningsForRequires(Origin.MemberDefinition, DiagnosticUtilities.RequiresDynamicCodeAttribute),
                logger.ShouldSuppressAnalysisWarningsForRequires(Origin.MemberDefinition, DiagnosticUtilities.RequiresAssemblyFilesAttribute),
                logger);

            ReflectionMethodBodyScanner.CheckAndReportAllRequires(diagnosticContext, Field);
        }
    }
}
