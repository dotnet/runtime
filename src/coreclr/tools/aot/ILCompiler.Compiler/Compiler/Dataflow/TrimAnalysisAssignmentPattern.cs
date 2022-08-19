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
        public MultiValue Source { init; get; }
        public MultiValue Target { init; get; }
        public MessageOrigin Origin { init; get; }
        internal Origin MemberWithRequirements { init; get; }

        internal TrimAnalysisAssignmentPattern(MultiValue source, MultiValue target, MessageOrigin origin, Origin memberWithRequirements)
        {
            Source = source.Clone();
            Target = target.Clone();
            Origin = origin;
            MemberWithRequirements = memberWithRequirements;
        }

        public TrimAnalysisAssignmentPattern Merge(ValueSetLattice<SingleValue> lattice, TrimAnalysisAssignmentPattern other)
        {
            Debug.Assert(Origin == other.Origin);

            return new TrimAnalysisAssignmentPattern(
                lattice.Meet(Source, other.Source),
                lattice.Meet(Target, other.Target),
                Origin,
                MemberWithRequirements);
        }

        public void MarkAndProduceDiagnostics(ReflectionMarker reflectionMarker, Logger logger)
        {
            var diagnosticContext = new DiagnosticContext(
                Origin,
                logger.ShouldSuppressAnalysisWarningsForRequires(Origin.MemberDefinition, DiagnosticUtilities.RequiresUnreferencedCodeAttribute),
                logger.ShouldSuppressAnalysisWarningsForRequires(Origin.MemberDefinition, DiagnosticUtilities.RequiresDynamicCodeAttribute),
                logger.ShouldSuppressAnalysisWarningsForRequires(Origin.MemberDefinition, DiagnosticUtilities.RequiresAssemblyFilesAttribute),
                logger);

            foreach (var sourceValue in Source)
            {
                foreach (var targetValue in Target)
                {
                    if (targetValue is FieldValue fieldValue)
                    {
                        // Once this is removed, please also cleanup ReflectionMethodBodyScanner.HandleStoreValueWithDynamicallyAccessedMembers
                        // which has to special case FieldValue right now, should not be needed after removal of this
                        ReflectionMethodBodyScanner.CheckAndReportRequires(diagnosticContext, fieldValue.Field, DiagnosticUtilities.RequiresUnreferencedCodeAttribute);
                        ReflectionMethodBodyScanner.CheckAndReportRequires(diagnosticContext, fieldValue.Field, DiagnosticUtilities.RequiresDynamicCodeAttribute);
                        // ?? Should this be enabled (was not so far)
                        //ReflectionMethodBodyScanner.CheckAndReportRequires(diagnosticContext, fieldValue.Field, DiagnosticUtilities.RequiresAssemblyFilesAttribute);
                    }

                    if (targetValue is not ValueWithDynamicallyAccessedMembers targetWithDynamicallyAccessedMembers)
                        throw new NotImplementedException();

                    var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction(reflectionMarker, diagnosticContext, MemberWithRequirements);
                    requireDynamicallyAccessedMembersAction.Invoke(sourceValue, targetWithDynamicallyAccessedMembers);
                }
            }
        }
    }
}
