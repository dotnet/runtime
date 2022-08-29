// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Interop.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class ShapeBreakingDiagnosticSuppressor : DiagnosticSuppressor
    {
        public static readonly SuppressionDescriptor MarkMethodsAsStaticSuppression = new SuppressionDescriptor("SYSLIBSUPPRESS0001", "CA1822", "Do not offer to make methods static when the methods need to be instance methods for a custom marshaller shape.");

        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions =>
            ImmutableArray.Create(MarkMethodsAsStaticSuppression);

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            foreach (var diagnostic in context.ReportedDiagnostics)
            {
                if (diagnostic.Id == MarkMethodsAsStaticSuppression.SuppressedDiagnosticId)
                {
                    SuppressMarkMethodsAsStaticDiagnosticIfNeeded(context, diagnostic);
                }
            }
        }

        private static void SuppressMarkMethodsAsStaticDiagnosticIfNeeded(SuppressionAnalysisContext context, Diagnostic diagnostic)
        {
            SemanticModel model = context.GetSemanticModel(diagnostic.Location.SourceTree);
            ISymbol diagnosedSymbol = model.GetDeclaredSymbol(diagnostic.Location.SourceTree.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan), context.CancellationToken);

            if (diagnosedSymbol.Kind != SymbolKind.Method)
            {
                return;
            }

            if (FindContainingEntryPointTypeAndManagedType(diagnosedSymbol.ContainingType) is (INamedTypeSymbol entryPointMarshallerType, INamedTypeSymbol managedType))
            {
                bool isLinearCollectionMarshaller = ManualTypeMarshallingHelper.IsLinearCollectionEntryPoint(entryPointMarshallerType);
                (MarshallerShape _, StatefulMarshallerShapeHelper.MarshallerMethods methods) = StatefulMarshallerShapeHelper.GetShapeForType(diagnosedSymbol.ContainingType, managedType, isLinearCollectionMarshaller, context.Compilation);
                if (methods.IsShapeMethod((IMethodSymbol)diagnosedSymbol))
                {
                    // If we are a method of the shape on the stateful marshaller shape, then we need to be our current shape.
                    // So, suppress the diagnostic to make this method static, as that would break the shape.
                    context.ReportSuppression(Suppression.Create(MarkMethodsAsStaticSuppression, diagnostic));
                }
            }
        }

        private static (INamedTypeSymbol EntryPointType, INamedTypeSymbol ManagedType)? FindContainingEntryPointTypeAndManagedType(INamedTypeSymbol marshallerType)
        {
            for (INamedTypeSymbol containingType = marshallerType; containingType is not null; containingType = containingType.ContainingType)
            {
                AttributeData? attrData = containingType.GetAttributes().FirstOrDefault(
                        attr => attr.AttributeClass?.ToDisplayString() == TypeNames.CustomMarshallerAttribute
                            && attr.AttributeConstructor is not null
                            && !attr.ConstructorArguments[0].IsNull
                            && attr.ConstructorArguments[2].Value is INamedTypeSymbol marshallerTypeInAttribute
                            && ManualTypeMarshallingHelper.TryResolveMarshallerType(containingType, marshallerTypeInAttribute, (_, _) => { }, out ITypeSymbol constructedMarshallerType)
                            && SymbolEqualityComparer.Default.Equals(constructedMarshallerType, marshallerType));
                if (attrData is not null)
                {
                    return (containingType, (INamedTypeSymbol)attrData.ConstructorArguments[0].Value);
                }
            }
            return null;
        }
    }
}
