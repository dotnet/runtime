// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using ILCompiler.Logging;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;

using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

#nullable enable

namespace ILCompiler.Dataflow
{
    public readonly record struct TrimAnalysisAssignmentPattern
    {
        public MultiValue Source { get; init; }
        public MultiValue Target { get; init; }
        public MessageOrigin Origin { get; init; }
        internal string Reason { get; init; }

        internal TrimAnalysisAssignmentPattern(MultiValue source, MultiValue target, MessageOrigin origin, string reason)
        {
            Source = source.DeepCopy();
            Target = target.DeepCopy();
            Origin = origin;
            Reason = reason;
        }

        public TrimAnalysisAssignmentPattern Merge(ValueSetLattice<SingleValue> lattice, TrimAnalysisAssignmentPattern other)
        {
            Debug.Assert(Origin == other.Origin);

            return new TrimAnalysisAssignmentPattern(
                lattice.Meet(Source, other.Source),
                lattice.Meet(Target, other.Target),
                Origin,
                Reason);
        }

        public void MarkAndProduceDiagnostics(ReflectionMarker reflectionMarker, Logger logger)
        {
            var diagnosticContext = new DiagnosticContext(
                Origin,
                logger.ShouldSuppressAnalysisWarningsForRequires(Origin.MemberDefinition, DiagnosticUtilities.RequiresUnreferencedCodeAttribute),
                logger.ShouldSuppressAnalysisWarningsForRequires(Origin.MemberDefinition, DiagnosticUtilities.RequiresDynamicCodeAttribute),
                logger.ShouldSuppressAnalysisWarningsForRequires(Origin.MemberDefinition, DiagnosticUtilities.RequiresAssemblyFilesAttribute),
                logger);

            foreach (var sourceValue in Source.AsEnumerable ())
            {
                foreach (var targetValue in Target.AsEnumerable ())
                {
                    if (targetValue is not ValueWithDynamicallyAccessedMembers targetWithDynamicallyAccessedMembers)
                        throw new NotImplementedException();

                    var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction(reflectionMarker, diagnosticContext, Reason);
                    requireDynamicallyAccessedMembersAction.Invoke(sourceValue, targetWithDynamicallyAccessedMembers);
                }
            }
        }
    }
}
