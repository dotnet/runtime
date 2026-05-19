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
    /// Reports diagnostics that drive the mechanical "unsafe-v2" migration code fixer:
    ///   IL5005 — 'unsafe' modifier on a type declaration (class, struct, interface,
    ///            record, record struct) or on a delegate declaration.
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

        // The two info-severity diagnostics this analyzer emits.
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            [s_unsafeOnTypeRule, s_unsafeOnMemberRule];

        // Wires up the syntax-node actions, gated on the EnableUnsafeV2MigrationAnalyzer MSBuild property.
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

        // Reports IL5005 when an `unsafe` token is present on a type/delegate declaration.
        private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
        {
            var unsafeToken = TryGetUnsafeToken(context.Node);
            if (unsafeToken is null)
                return;

            // Per the unsafe-evolution spec: `unsafe` on type declarations (class, struct,
            // interface, record, record struct) and on `delegate` declarations is an
            // error / has no meaning under the updated rules. Always removable.
            var displayName = context.SemanticModel.GetDeclaredSymbol(context.Node, context.CancellationToken)?.GetDisplayName()
                ?? context.Node.ToString();
            context.ReportDiagnostic(Diagnostic.Create(s_unsafeOnTypeRule, unsafeToken.Value.GetLocation(), displayName));
        }

        // Reports IL5006 when an `unsafe` token is present on a member declaration and the
        // fix would actually change something (the analyzer is idempotent).
        private static void AnalyzeMemberDeclaration(SyntaxNodeAnalysisContext context)
        {
            var unsafeToken = TryGetUnsafeToken(context.Node);
            if (unsafeToken is null)
                return;

            // Suppress the diagnostic when the code fix would be a no-op so the analyzer is
            // idempotent under repeated 'dotnet format analyzers' runs. The fix is a no-op
            // when BOTH of the following are true:
            //   1. CanRemoveUnsafeModifier returns false (signature has pointers, OR it's a
            //      non-static ctor with a `: base(...)` / `: this(...)` initializer), AND
            //   2. MemberNeedsBodyWrap returns false (no body, body already wrapped, body
            //      contains conditional-compilation directives, or body is a trivial
            //      field-forwarder accessor on a property/indexer).
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

        // Returns the first `unsafe` keyword among `node`'s modifiers, or null if there isn't one.
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

        // True iff the `unsafe` modifier can be safely dropped from `member` under unsafe-v2.
        public static bool CanRemoveUnsafeModifier(SyntaxNode member, SemanticModel? semanticModel)
        {
            // Per the unsafe-evolution spec: `unsafe` on static constructors and
            // destructors has no meaning under the updated rules — always remove.
            if (member is DestructorDeclarationSyntax)
                return true;
            if (member is ConstructorDeclarationSyntax ctor)
            {
                if (ctor.Modifiers.Any(SyntaxKind.StaticKeyword))
                    return true;

                // `unsafe` on a (non-static) constructor introduces an unsafe context in
                // its initializer (`: base(...)` / `: this(...)`), so removing the modifier
                // would break a `: base(unsafeMember(...))`-style call. Best-effort: keep
                // the modifier whenever the ctor has an initializer at all — developer can
                // remove it manually if the initializer is safe.
                if (ctor.Initializer is not null)
                    return false;
            }

            if (semanticModel?.GetDeclaredSymbol(member) is { } symbol)
                return !SymbolSignatureContainsPointer(symbol);

            return !SyntaxSignatureContainsPointer(member);
        }

        // True iff the member has some body content that should be wrapped in `unsafe { ... }`.
        public static bool MemberNeedsBodyWrap(SyntaxNode member)
        {
            // True if the member's body (or expression body, or accessors) would benefit
            // from being wrapped in an 'unsafe { ... }' block. See BlockNeedsWrap /
            // ExpressionBodyNeedsWrap / AccessorNeedsWrap for the per-shape rules — in
            // short: empty / already-wrapped / contains-conditional-compilation-directives
            // bodies are skipped; for property and indexer accessors, trivial
            // field-forwarders (`=> _x`, `=> _x = value`) are also skipped.
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

        // True iff a block body has statements worth wrapping (non-empty, no existing
        // unsafe block, no conditional-compilation directives).
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

        // True iff `trivia` is one of the conditional-compilation directive kinds
        // (#if / #elif / #else / #endif) — those are the ones that change body shape per TFM.
        private static bool IsConditionalCompilationDirective(SyntaxTrivia trivia) =>
            trivia.IsKind(SyntaxKind.IfDirectiveTrivia)
            || trivia.IsKind(SyntaxKind.ElifDirectiveTrivia)
            || trivia.IsKind(SyntaxKind.ElseDirectiveTrivia)
            || trivia.IsKind(SyntaxKind.EndIfDirectiveTrivia);

        // True iff an `=> expr` body is worth wrapping (has no unsafe statement already).
        public static bool ExpressionBodyNeedsWrap(ArrowExpressionClauseSyntax? expr) =>
            expr is not null && !expr.DescendantNodesAndSelf().OfType<UnsafeStatementSyntax>().Any();

        // True iff a property/indexer body (expression or accessor list) needs wrapping,
        // skipping trivial field-forwarders.
        private static bool PropertyNeedsWrap(ArrowExpressionClauseSyntax? expr, AccessorListSyntax? accessors)
        {
            if (expr is not null)
                return ExpressionBodyNeedsWrap(expr) && !IsTrivialFieldExpression(expr.Expression);

            return accessors is not null && AccessorListNeedsWrap(accessors, isTrivialAllowed: true);
        }

        // True iff at least one accessor in the list needs wrapping (see AccessorNeedsWrap).
        public static bool AccessorListNeedsWrap(AccessorListSyntax accessors, bool isTrivialAllowed) =>
            accessors.Accessors.Any(a => AccessorNeedsWrap(a, isTrivialAllowed));

        /// <summary>
        /// Decides whether <paramref name="accessor"/>'s body needs wrapping in
        /// <c>unsafe { ... }</c>. <paramref name="isTrivialAllowed"/> should be
        /// <c>true</c> for property / indexer accessors (where a trivial
        /// <c>=&gt; _field</c> getter or <c>=&gt; _field = value</c> setter never needs an
        /// unsafe context) and <c>false</c> for event accessors (no such shortcut applies).
        /// </summary>
        public static bool AccessorNeedsWrap(AccessorDeclarationSyntax accessor, bool isTrivialAllowed)
        {
            if (accessor.Body is { } body && BlockNeedsWrap(body))
                return true;

            if (accessor.ExpressionBody is { } expr && ExpressionBodyNeedsWrap(expr))
            {
                if (!(isTrivialAllowed && IsTrivialAccessorExpression(accessor, expr.Expression)))
                    return true;
            }

            return false;
        }

        // True iff `expr` is a pure field/property identifier reference like `_x`,
        // `this._x`, `base._x`, or `obj._x` — i.e. something that cannot do unsafe work.
        public static bool IsTrivialFieldExpression(ExpressionSyntax expr) => expr switch
        {
            IdentifierNameSyntax => true,
            MemberAccessExpressionSyntax mae => mae.Expression is ThisExpressionSyntax or BaseExpressionSyntax or IdentifierNameSyntax,
            ParenthesizedExpressionSyntax pe => IsTrivialFieldExpression(pe.Expression),
            _ => false,
        };

        // True iff `accessor`'s expression body is a trivial field forwarder
        // (`get => _x;` or `set/init => _x = value;`).
        private static bool IsTrivialAccessorExpression(AccessorDeclarationSyntax accessor, ExpressionSyntax expr)
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

        // True iff a method/property/event/delegate symbol's signature exposes any
        // pointer or function-pointer type (recursively through arrays + generics).
        private static bool SymbolSignatureContainsPointer(ISymbol symbol) => symbol switch
        {
            IMethodSymbol m => ContainsPointer(m.ReturnType) || m.Parameters.Any(static p => ContainsPointer(p.Type)),
            IPropertySymbol p => ContainsPointer(p.Type) || p.Parameters.Any(static pp => ContainsPointer(pp.Type)),
            IEventSymbol e => ContainsPointer(e.Type),
            INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: { } invoke } =>
                ContainsPointer(invoke.ReturnType) || invoke.Parameters.Any(static p => ContainsPointer(p.Type)),
            _ => false,
        };

        // True iff `type` is, or recursively contains, a pointer/function-pointer type
        // (peeking through arrays and generic type arguments).
        private static bool ContainsPointer(ITypeSymbol? type) => type switch
        {
            null => false,
            IPointerTypeSymbol or IFunctionPointerTypeSymbol => true,
            IArrayTypeSymbol arr => ContainsPointer(arr.ElementType),
            INamedTypeSymbol named when named.IsGenericType => named.TypeArguments.Any(ContainsPointer),
            _ => false,
        };

        // Pure-syntax fallback for SymbolSignatureContainsPointer when no semantic model
        // is available — scans every TypeSyntax in the member's signature.
        private static bool SyntaxSignatureContainsPointer(SyntaxNode member)
        {
            static bool HasPtr(TypeSyntax? t) =>
                t is not null && t.DescendantNodesAndSelf().Any(static n => n is PointerTypeSyntax or FunctionPointerTypeSyntax);

            static bool ParamsHavePtr(BaseParameterListSyntax? ps) =>
                ps is not null && ps.Parameters.Any(static p => HasPtr(p.Type));

            return member switch
            {
                MethodDeclarationSyntax m => HasPtr(m.ReturnType) || ParamsHavePtr(m.ParameterList),
                ConstructorDeclarationSyntax c => ParamsHavePtr(c.ParameterList),
                DestructorDeclarationSyntax => false,
                OperatorDeclarationSyntax op => HasPtr(op.ReturnType) || ParamsHavePtr(op.ParameterList),
                ConversionOperatorDeclarationSyntax co => HasPtr(co.Type) || ParamsHavePtr(co.ParameterList),
                PropertyDeclarationSyntax p => HasPtr(p.Type),
                IndexerDeclarationSyntax i => HasPtr(i.Type) || ParamsHavePtr(i.ParameterList),
                EventDeclarationSyntax e => HasPtr(e.Type),
                EventFieldDeclarationSyntax ef => HasPtr(ef.Declaration.Type),
                LocalFunctionStatementSyntax lf => HasPtr(lf.ReturnType) || ParamsHavePtr(lf.ParameterList),
                DelegateDeclarationSyntax d => HasPtr(d.ReturnType) || ParamsHavePtr(d.ParameterList),
                _ => false,
            };
        }

        // Returns the modifier list for any declaration-shaped node, or an empty list.
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
