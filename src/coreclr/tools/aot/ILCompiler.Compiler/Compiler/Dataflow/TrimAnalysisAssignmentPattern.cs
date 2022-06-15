// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using ILCompiler.Logging;
using ILLink.Shared.TrimAnalysis;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

#nullable enable

namespace ILCompiler.Dataflow
{
    public readonly record struct TrimAnalysisAssignmentPattern
    {
        public MultiValue Source { init; get; }
        public MultiValue Target { init; get; }
        public MessageOrigin Origin { init; get; }

        public TrimAnalysisAssignmentPattern(MultiValue source, MultiValue target, MessageOrigin origin)
        {
            Source = source.Clone();
            Target = target.Clone();
            Origin = origin;
        }

        public void MarkAndProduceDiagnostics(ReflectionMarker reflectionMarker, Logger logger)
        {
            bool diagnosticsEnabled = !context.Annotations.ShouldSuppressAnalysisWarningsForRequiresUnreferencedCode(Origin.MemberDefinition);
            var diagnosticContext = new DiagnosticContext(Origin, diagnosticsEnabled, logger);

            foreach (var sourceValue in Source)
            {
                foreach (var targetValue in Target)
                {
                    if (targetValue is not ValueWithDynamicallyAccessedMembers targetWithDynamicallyAccessedMembers)
                        throw new NotImplementedException();

                    var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction(reflectionMarker, diagnosticContext);
                    requireDynamicallyAccessedMembersAction.Invoke(sourceValue, targetWithDynamicallyAccessedMembers);
                }
            }
        }
    }
}
