// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
    [DiagnosticAnalyzer (LanguageNames.CSharp)]
    public sealed class UnsafeMethodMissingRequiresUnsafeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.UnsafeMethodMissingRequiresUnsafe);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (s_rule);

        public override void Initialize (AnalysisContext context)
        {
            context.EnableConcurrentExecution ();
            context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction (context => {
                if (!context.Options.IsMSBuildPropertyValueTrue (MSBuildPropertyOptionNames.EnableUnsafeAnalyzer))
                    return;

                if (context.Compilation.GetTypeByMetadataName (RequiresUnsafeAnalyzer.FullyQualifiedRequiresUnsafeAttribute) is null)
                    return;

                context.RegisterSyntaxNodeAction (
                    AnalyzeNode,
                    SyntaxKind.MethodDeclaration,
                    SyntaxKind.ConstructorDeclaration,
                    SyntaxKind.LocalFunctionStatement);
            });
        }

        private static void AnalyzeNode (SyntaxNodeAnalysisContext context)
        {
            if (!HasUnsafeModifier (context.Node))
                return;

            var symbol = context.SemanticModel.GetDeclaredSymbol (context.Node, context.CancellationToken);
            if (symbol is null)
                return;

            if (symbol.HasAttribute (RequiresUnsafeAnalyzer.RequiresUnsafeAttributeName))
                return;

            var location = GetDiagnosticLocation (context.Node);
            context.ReportDiagnostic (Diagnostic.Create (s_rule, location, symbol.GetDisplayName ()));
        }

        private static bool HasUnsafeModifier (SyntaxNode node) => node switch {
            MethodDeclarationSyntax method => method.Modifiers.Any (SyntaxKind.UnsafeKeyword),
            ConstructorDeclarationSyntax ctor => ctor.Modifiers.Any (SyntaxKind.UnsafeKeyword),
            LocalFunctionStatementSyntax localFunc => localFunc.Modifiers.Any (SyntaxKind.UnsafeKeyword),
            _ => false,
        };

        private static Location GetDiagnosticLocation (SyntaxNode node) => node switch {
            MethodDeclarationSyntax method => method.Identifier.GetLocation (),
            ConstructorDeclarationSyntax ctor => ctor.Identifier.GetLocation (),
            LocalFunctionStatementSyntax localFunc => localFunc.Identifier.GetLocation (),
            _ => node.GetLocation (),
        };
    }
}
#endif
