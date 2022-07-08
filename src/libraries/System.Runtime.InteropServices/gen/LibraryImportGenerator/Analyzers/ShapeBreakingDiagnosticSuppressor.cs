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
        public static readonly SuppressionDescriptor MarkMethodsAsStaticSuppression = new SuppressionDescriptor("SYSLIBSUPPRESS1001", "CA1822", "Do not offer to make methods static when they need to be instance methods.");

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
            ISymbol symbol = model.GetDeclaredSymbol(diagnostic.Location.SourceTree.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan), context.CancellationToken);
            if (symbol.Name == "Free" && symbol.Kind == SymbolKind.Method) // TODO: Extend to all names recognized in the shape
            {
                if (symbol.ContainingType is { TypeKind: TypeKind.Struct })
                {
                    bool isCustomTypeMarshaller = GetAllContainingTypes(symbol).Any(type => type.GetAttributes().Any(
                        attr => attr.AttributeClass?.ToDisplayString() == TypeNames.CustomMarshallerAttribute
                            && attr.AttributeConstructor is not null
                            && attr.ConstructorArguments[2].Value is INamedTypeSymbol marshallerType
                            && SymbolEqualityComparer.Default.Equals(marshallerType, symbol.ContainingType)));
                    context.ReportSuppression(Suppression.Create(MarkMethodsAsStaticSuppression, diagnostic));
                }
            }
        }

        private static IEnumerable<ITypeSymbol> GetAllContainingTypes(ISymbol symbol)
        {
            for (INamedTypeSymbol containingType = symbol.ContainingType; containingType is not null; containingType = containingType.ContainingType)
            {
                yield return containingType;
            }
        }
    }
}
