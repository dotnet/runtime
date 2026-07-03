// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Linq;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
    /// <summary>
    /// Flags <c>unsafe</c> modifiers that are unnecessary under the updated memory-safety rules and
    /// can be removed as part of migrating to the "unsafe evolution" feature. See IL5005.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnnecessaryUnsafeModifierAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.UnnecessaryUnsafeModifier);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

        private static readonly ImmutableArray<SyntaxKind> s_declarationKinds =
        [
            SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.InterfaceDeclaration,
            SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration, SyntaxKind.DelegateDeclaration,
            SyntaxKind.MethodDeclaration, SyntaxKind.LocalFunctionStatement,
            SyntaxKind.ConstructorDeclaration, SyntaxKind.DestructorDeclaration,
            SyntaxKind.OperatorDeclaration, SyntaxKind.ConversionOperatorDeclaration,
            SyntaxKind.PropertyDeclaration, SyntaxKind.IndexerDeclaration,
            SyntaxKind.EventDeclaration, SyntaxKind.EventFieldDeclaration, SyntaxKind.FieldDeclaration,
            SyntaxKind.GetAccessorDeclaration, SyntaxKind.SetAccessorDeclaration, SyntaxKind.InitAccessorDeclaration,
        ];

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(static context =>
            {
                if (!context.Options.IsMSBuildPropertyValueTrue(MSBuildPropertyOptionNames.EnableUnsafeAnalyzer))
                    return;

                context.RegisterSyntaxNodeAction(AnalyzeDeclaration, s_declarationKinds);
            });
        }

        private static void AnalyzeDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (!UnsafeMigrationFacts.UsesUpdatedMemorySafetyRules(context.Node.SyntaxTree))
                return;

            var node = context.Node;
            var modifiers = node.GetModifiers();
            var unsafeToken = modifiers.FirstOrDefault(static m => m.IsKind(SyntaxKind.UnsafeKeyword));
            if (unsafeToken == default)
                return;

            // 'extern' members must be explicitly 'safe' or 'unsafe' under the new rules, so leave them alone.
            if (modifiers.Any(SyntaxKind.ExternKeyword))
                return;

            if (!IsUnsafeModifierRemovable(node))
                return;

            var name = context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken)?.Name;
            if (string.IsNullOrEmpty(name))
                name = "member";

            context.ReportDiagnostic(Diagnostic.Create(s_rule, unsafeToken.GetLocation(), name));
        }

        private static bool IsUnsafeModifierRemovable(SyntaxNode node) => node switch
        {
            // Types and delegates can never be 'unsafe' under the new rules.
            BaseTypeDeclarationSyntax or DelegateDeclarationSyntax => true,
            // 'unsafe' has no meaning on static constructors or destructors.
            ConstructorDeclarationSyntax ctor when ctor.Modifiers.Any(SyntaxKind.StaticKeyword) => true,
            DestructorDeclarationSyntax => true,
            // For everything else, 'unsafe' stays only if an unmanaged pointer appears in the signature.
            _ => !SignatureContainsPointer(node),
        };

        private static bool SignatureContainsPointer(SyntaxNode node)
        {
            var signatureTypes = node switch
            {
                MethodDeclarationSyntax m => Types(m.ReturnType, m.ParameterList),
                LocalFunctionStatementSyntax lf => Types(lf.ReturnType, lf.ParameterList),
                OperatorDeclarationSyntax op => Types(op.ReturnType, op.ParameterList),
                ConversionOperatorDeclarationSyntax co => Types(co.Type, co.ParameterList),
                ConstructorDeclarationSyntax c => Types(null, c.ParameterList),
                PropertyDeclarationSyntax p => Types(p.Type, null),
                IndexerDeclarationSyntax i => Types(i.Type, i.ParameterList),
                EventDeclarationSyntax e => Types(e.Type, null),
                EventFieldDeclarationSyntax ef => Types(ef.Declaration.Type, null),
                FieldDeclarationSyntax f => Types(f.Declaration.Type, null),
                // An accessor is caller-unsafe only if its containing property/indexer exposes a pointer.
                AccessorDeclarationSyntax a => a.Parent?.Parent is { } member ? [member] : [],
                _ => [],
            };

            return signatureTypes.Any(static t => t is not null &&
                t.DescendantNodesAndSelf().Any(static n => n is PointerTypeSyntax or FunctionPointerTypeSyntax));

            static SyntaxNode?[] Types(TypeSyntax? returnType, BaseParameterListSyntax? parameters) => [returnType, parameters];
        }
    }
}
#endif
