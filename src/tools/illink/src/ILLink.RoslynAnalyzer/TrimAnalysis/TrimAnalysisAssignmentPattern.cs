// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
    internal readonly record struct TrimAnalysisAssignmentPattern
    {
        public MultiValue Source { get; init; }
        public MultiValue Target { get; init; }
        public IOperation Operation { get; init; }
        public ISymbol OwningSymbol { get; init; }
        public FeatureContext FeatureContext { get; init; }

        public TrimAnalysisAssignmentPattern(
            MultiValue source,
            MultiValue target,
            IOperation operation,
            ISymbol owningSymbol,
            FeatureContext featureContext)
        {
            Source = source.DeepCopy();
            Target = target.DeepCopy();
            Operation = operation;
            OwningSymbol = owningSymbol;
            FeatureContext = featureContext;
        }

        public TrimAnalysisAssignmentPattern Merge(
            ValueSetLattice<SingleValue> lattice,
            FeatureContextLattice featureContextLattice,
            MultiValue source,
            MultiValue target,
            FeatureContext featureContext)
        {
            return new TrimAnalysisAssignmentPattern
            {
                Source = lattice.Meet(source, Source),
                Target = lattice.Meet(target, Target),
                Operation = Operation,
                OwningSymbol = OwningSymbol,
                FeatureContext = featureContextLattice.Meet(featureContext, FeatureContext)
            };
        }

        public void ReportDiagnostics(DataFlowAnalyzerContext context, Action<Diagnostic> reportDiagnostic)
        {
            var location = Operation.Syntax.GetLocation();
            if (context.TrimAnalyzer is not null &&
                !OwningSymbol.IsInRequiresUnreferencedCodeAttributeScope(out _) &&
                !FeatureContext.IsEnabled(RequiresUnreferencedCodeAnalyzer.FullyQualifiedRequiresUnreferencedCodeAttribute))
            {
                foreach (var sourceValue in Source.AsEnumerable())
                {
                    foreach (var targetValue in Target.AsEnumerable())
                    {
                        // The target should always be an annotated value, but the visitor design currently prevents
                        // declaring this in the type system.
                        if (targetValue is not ValueWithDynamicallyAccessedMembers targetWithDynamicallyAccessedMembers)
                            throw new NotImplementedException();

                        var typeNameResolver = new TypeNameResolver(context.Compilation);
                        var reflectionAccessAnalyzer = new ReflectionAccessAnalyzer(reportDiagnostic, typeNameResolver, typeHierarchyType: null);
                        var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction(context.TrimAnalyzer, FeatureContext, typeNameResolver, location, reportDiagnostic, reflectionAccessAnalyzer, OwningSymbol);
                        requireDynamicallyAccessedMembersAction.Invoke(sourceValue, targetWithDynamicallyAccessedMembers);
                    }
                }
            }
        }
    }
}
