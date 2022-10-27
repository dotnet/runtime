// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace Mono.Linker.Dataflow
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

        public TrimAnalysisAssignmentPattern Merge(ValueSetLattice<SingleValue> lattice, TrimAnalysisAssignmentPattern other)
        {
            Debug.Assert(Origin == other.Origin);

            return new TrimAnalysisAssignmentPattern(
                lattice.Meet(Source, other.Source),
                lattice.Meet(Target, other.Target),
                Origin);
        }

        public void MarkAndProduceDiagnostics(ReflectionMarker reflectionMarker, LinkContext context)
        {
            bool diagnosticsEnabled = !context.Annotations.ShouldSuppressAnalysisWarningsForRequiresUnreferencedCode(Origin.Provider);
            var diagnosticContext = new DiagnosticContext(Origin, diagnosticsEnabled, context);

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
