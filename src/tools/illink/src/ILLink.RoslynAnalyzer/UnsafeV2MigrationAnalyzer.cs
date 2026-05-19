// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Generic;
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
    /// Reports diagnostics that drive the mechanical "unsafe-v2" migration code fixer:
    ///   IL5005 — 'unsafe' modifier on a class/struct/interface/record/delegate declaration.
    ///   IL5006 — 'unsafe' modifier on a member declaration (method, ctor, dtor, operator,
    ///            conversion operator, property, indexer, event, event field, local function).
    /// Both diagnostics are gated behind <see cref="MSBuildPropertyOptionNames.EnableUnsafeV2MigrationAnalyzer"/>
    /// so the migration is opt-in and only runs when a developer explicitly requests it
    /// (typically through <c>dotnet format analyzers</c>).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnsafeV2MigrationAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_unsafeOnTypeRule =
            DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.UnsafeModifierOnTypeDeclaration, diagnosticSeverity: DiagnosticSeverity.Info);

        private static readonly DiagnosticDescriptor s_unsafeOnMemberRule =
            DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.UnsafeModifierOnMemberDeclaration, diagnosticSeverity: DiagnosticSeverity.Info);

        private static readonly ImmutableArray<SyntaxKind> s_typeKinds = [
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.InterfaceDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration,
            SyntaxKind.DelegateDeclaration,
        ];

        private static readonly ImmutableArray<SyntaxKind> s_memberKinds = [
            SyntaxKind.MethodDeclaration,
            SyntaxKind.ConstructorDeclaration,
            SyntaxKind.DestructorDeclaration,
            SyntaxKind.OperatorDeclaration,
            SyntaxKind.ConversionOperatorDeclaration,
            SyntaxKind.PropertyDeclaration,
            SyntaxKind.IndexerDeclaration,
            SyntaxKind.EventDeclaration,
            SyntaxKind.EventFieldDeclaration,
            SyntaxKind.LocalFunctionStatement,
        ];

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            [s_unsafeOnTypeRule, s_unsafeOnMemberRule];

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(compilationContext =>
            {
                if (!compilationContext.Options.IsMSBuildPropertyValueTrue(MSBuildPropertyOptionNames.EnableUnsafeV2MigrationAnalyzer))
                    return;

                compilationContext.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, s_typeKinds);
                compilationContext.RegisterSyntaxNodeAction(AnalyzeMemberDeclaration, s_memberKinds);
            });
        }

        private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
        {
            var unsafeToken = TryGetUnsafeToken(context.Node);
            if (unsafeToken is null)
                return;

            // For delegates (which are not BaseTypeDeclarationSyntax) only report if the
            // delegate's signature has no pointer types — otherwise the 'unsafe' modifier
            // is still required by callers under unsafe-v2 semantics. For ordinary type
            // declarations the modifier is never allowed, so we always report.
            if (context.Node is DelegateDeclarationSyntax && !CanRemoveUnsafeModifier(context.Node, context.SemanticModel))
                return;

            var displayName = context.SemanticModel.GetDeclaredSymbol(context.Node, context.CancellationToken)?.GetDisplayName()
                ?? context.Node.ToString();
            context.ReportDiagnostic(Diagnostic.Create(s_unsafeOnTypeRule, unsafeToken.Value.GetLocation(), displayName));
        }

        private static void AnalyzeMemberDeclaration(SyntaxNodeAnalysisContext context)
        {
            var unsafeToken = TryGetUnsafeToken(context.Node);
            if (unsafeToken is null)
                return;

            // Suppress the diagnostic when the code fix would be a no-op so the analyzer is
            // idempotent under repeated 'dotnet format analyzers' runs. The fix is a no-op when:
            //   1. the signature carries a pointer (so we can't drop the modifier), AND
            //   2. the body is in a state where we wouldn't wrap it.
            if (!CanRemoveUnsafeModifier(context.Node, context.SemanticModel) &&
                !MemberNeedsBodyWrap(context.Node))
            {
                return;
            }

            // EventFieldDeclarationSyntax has no symbol of its own; the symbols are on the
            // variable declarators. Use the first one for the display name.
            var symbolNode = context.Node is EventFieldDeclarationSyntax ef && ef.Declaration.Variables.Count > 0
                ? (SyntaxNode)ef.Declaration.Variables[0]
                : context.Node;
            var displayName = context.SemanticModel.GetDeclaredSymbol(symbolNode, context.CancellationToken)?.GetDisplayName()
                ?? context.Node.ToString();
            context.ReportDiagnostic(Diagnostic.Create(s_unsafeOnMemberRule, unsafeToken.Value.GetLocation(), displayName));
        }

        private static SyntaxToken? TryGetUnsafeToken(SyntaxNode node)
        {
            var modifiers = GetModifiers(node);
            foreach (var m in modifiers)
            {
                if (m.IsKind(SyntaxKind.UnsafeKeyword))
                    return m;
            }
            return null;
        }

        public static bool CanRemoveUnsafeModifier(SyntaxNode member, SemanticModel? semanticModel)
        {
            if (semanticModel?.GetDeclaredSymbol(member) is { } symbol)
                return !SymbolSignatureContainsPointer(symbol);

            return !SyntaxSignatureContainsPointer(member);
        }

        public static bool MemberNeedsBodyWrap(SyntaxNode member)
        {
            // True if the member has a non-empty body (or expression body) that does not yet
            // contain an 'unsafe { ... }' block and isn't a trivial field-forwarding accessor.
            return member switch
            {
                MethodDeclarationSyntax m => BlockNeedsWrap(m.Body) || ExpressionBodyNeedsWrap(m.ExpressionBody),
                LocalFunctionStatementSyntax lf => BlockNeedsWrap(lf.Body) || ExpressionBodyNeedsWrap(lf.ExpressionBody),
                ConstructorDeclarationSyntax c => BlockNeedsWrap(c.Body) || ExpressionBodyNeedsWrap(c.ExpressionBody),
                DestructorDeclarationSyntax d => BlockNeedsWrap(d.Body) || ExpressionBodyNeedsWrap(d.ExpressionBody),
                OperatorDeclarationSyntax op => BlockNeedsWrap(op.Body) || ExpressionBodyNeedsWrap(op.ExpressionBody),
                ConversionOperatorDeclarationSyntax co => BlockNeedsWrap(co.Body) || ExpressionBodyNeedsWrap(co.ExpressionBody),
                PropertyDeclarationSyntax p => PropertyNeedsWrap(p.ExpressionBody, p.AccessorList),
                IndexerDeclarationSyntax i => PropertyNeedsWrap(i.ExpressionBody, i.AccessorList),
                EventDeclarationSyntax e when e.AccessorList is not null => AccessorListNeedsWrap(e.AccessorList, isTrivialAllowed: false),
                _ => false,
            };
        }

        public static bool BlockNeedsWrap(BlockSyntax? body)
        {
            if (body is null || body.Statements.Count == 0)
                return false;
            if (body.DescendantNodes().OfType<UnsafeStatementSyntax>().Any())
                return false;
            // Skip wrapping when the body contains conditional-compilation directives.
            // Every structural / textual wrap we tried produced different output per TFM,
            // which makes `dotnet format` emit "<<<<<<< TODO: Unmerged change" markers when
            // stitching the per-TFM fixer outputs back together. Developer wraps manually.
            // Other directives (#pragma / #region / #nullable) are fine — they don't change
            // the body shape between TFMs.
            if (body.DescendantTrivia(descendIntoTrivia: true).Any(static t => IsConditionalCompilationDirective(t)))
                return false;
            return true;
        }

        private static bool IsConditionalCompilationDirective(SyntaxTrivia trivia) =>
            trivia.IsKind(SyntaxKind.IfDirectiveTrivia)
            || trivia.IsKind(SyntaxKind.ElifDirectiveTrivia)
            || trivia.IsKind(SyntaxKind.ElseDirectiveTrivia)
            || trivia.IsKind(SyntaxKind.EndIfDirectiveTrivia);

        private static bool ExpressionBodyNeedsWrap(ArrowExpressionClauseSyntax? expr) =>
            expr is not null && !expr.DescendantNodesAndSelf().OfType<UnsafeStatementSyntax>().Any();

        private static bool PropertyNeedsWrap(ArrowExpressionClauseSyntax? expr, AccessorListSyntax? accessors)
        {
            if (expr is not null)
                return ExpressionBodyNeedsWrap(expr) && !IsTrivialFieldExpression(expr.Expression);

            return accessors is not null && AccessorListNeedsWrap(accessors, isTrivialAllowed: true);
        }

        private static bool AccessorListNeedsWrap(AccessorListSyntax accessors, bool isTrivialAllowed)
        {
            foreach (var accessor in accessors.Accessors)
            {
                if (accessor.Body is { } body && BlockNeedsWrap(body))
                    return true;

                if (accessor.ExpressionBody is { } expr && ExpressionBodyNeedsWrap(expr))
                {
                    if (!(isTrivialAllowed && IsTrivialAccessorExpression(accessor, expr.Expression)))
                        return true;
                }
            }

            return false;
        }

        public static bool IsTrivialFieldExpression(ExpressionSyntax expr) => expr switch
        {
            IdentifierNameSyntax => true,
            MemberAccessExpressionSyntax mae => mae.Expression is ThisExpressionSyntax or BaseExpressionSyntax or IdentifierNameSyntax,
            ParenthesizedExpressionSyntax pe => IsTrivialFieldExpression(pe.Expression),
            _ => false,
        };

        public static bool IsTrivialAccessorExpression(AccessorDeclarationSyntax accessor, ExpressionSyntax expr)
        {
            if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                return IsTrivialFieldExpression(expr);

            if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration) || accessor.IsKind(SyntaxKind.InitAccessorDeclaration))
            {
                return expr is AssignmentExpressionSyntax assign
                    && assign.Right is IdentifierNameSyntax { Identifier.ValueText: "value" }
                    && IsTrivialFieldExpression(assign.Left);
            }

            return false;
        }

        private static bool SymbolSignatureContainsPointer(ISymbol symbol) => symbol switch
        {
            IMethodSymbol m => ContainsPointer(m.ReturnType) || m.Parameters.Any(static p => ContainsPointer(p.Type)),
            IPropertySymbol p => ContainsPointer(p.Type) || p.Parameters.Any(static pp => ContainsPointer(pp.Type)),
            IEventSymbol e => ContainsPointer(e.Type),
            INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: { } invoke } =>
                ContainsPointer(invoke.ReturnType) || invoke.Parameters.Any(static p => ContainsPointer(p.Type)),
            _ => false,
        };

        private static bool ContainsPointer(ITypeSymbol? type) => type switch
        {
            null => false,
            IPointerTypeSymbol or IFunctionPointerTypeSymbol => true,
            IArrayTypeSymbol arr => ContainsPointer(arr.ElementType),
            INamedTypeSymbol named when named.IsGenericType => named.TypeArguments.Any(ContainsPointer),
            _ => false,
        };

        private static bool SyntaxSignatureContainsPointer(SyntaxNode member)
        {
            IEnumerable<TypeSyntax?> types = member switch
            {
                MethodDeclarationSyntax m => new[] { m.ReturnType }.Concat(m.ParameterList.Parameters.Select(static p => p.Type)),
                ConstructorDeclarationSyntax c => c.ParameterList.Parameters.Select(static p => p.Type),
                DestructorDeclarationSyntax => [],
                OperatorDeclarationSyntax op => new[] { op.ReturnType }.Concat(op.ParameterList.Parameters.Select(static p => p.Type)),
                ConversionOperatorDeclarationSyntax co => new[] { co.Type }.Concat(co.ParameterList.Parameters.Select(static p => p.Type)),
                PropertyDeclarationSyntax p => [p.Type],
                IndexerDeclarationSyntax i => new[] { i.Type }.Concat(i.ParameterList.Parameters.Select(static p => p.Type)),
                EventDeclarationSyntax e => [e.Type],
                EventFieldDeclarationSyntax ef => [ef.Declaration.Type],
                LocalFunctionStatementSyntax lf => new[] { lf.ReturnType }.Concat(lf.ParameterList.Parameters.Select(static p => p.Type)),
                DelegateDeclarationSyntax d => new[] { d.ReturnType }.Concat(d.ParameterList.Parameters.Select(static p => p.Type)),
                _ => [],
            };

            return types.Where(static t => t is not null)
                .Any(static t => t!.DescendantNodesAndSelf().Any(static n => n is PointerTypeSyntax or FunctionPointerTypeSyntax));
        }

        public static SyntaxTokenList GetModifiers(SyntaxNode node) => node switch
        {
            MemberDeclarationSyntax m => m.Modifiers,
            LocalFunctionStatementSyntax lf => lf.Modifiers,
            AccessorDeclarationSyntax acc => acc.Modifiers,
            _ => default,
        };
    }
}
#endif
