// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace ILLink.CodeFix.UnsafeEvolution
{
    /// <summary>
    /// Introduces <c>unsafe { ... }</c> blocks around code that the compiler now considers
    /// to require an unsafe context under the updated memory-safety rules.
    /// </summary>
    /// <remarks>
    /// <para>Handles:</para>
    /// <list type="bullet">
    ///   <item>CS0214 - legacy pointer-in-non-unsafe-context error.</item>
    ///   <item>CS9360 - unsafe operation (pointer dereference, function-pointer invocation, etc.).</item>
    ///   <item>CS9361 - uninitialized <c>stackalloc</c> to <c>Span&lt;T&gt;</c> under <c>SkipLocalsInit</c>.</item>
    ///   <item>CS9362 - call of a requires-unsafe member outside an unsafe context.</item>
    ///   <item>CS9363 - compat-mode call of a member with pointers in its signature.</item>
    /// </list>
    /// <para>Behavior:</para>
    /// <list type="bullet">
    ///   <item>
    ///     If many unsafe diagnostics cluster inside a single member body
    ///     (see <see cref="UnsafeBlockHelpers.ShouldWrapEntireBody"/>), the whole body is wrapped once.
    ///   </item>
    ///   <item>
    ///     Otherwise the smallest containing statement is wrapped. Local declarations whose
    ///     initializer is unsafe but whose declared local is referenced later are split into
    ///     a forward declaration plus assignment so the wrap stays minimal (with <c>scoped</c>
    ///     added for ref-struct types).
    ///   </item>
    ///   <item>The first line of every generated block is <c>// SAFETY-TODO: Audit</c>.</item>
    ///   <item>
    ///     Statements that have preprocessor directives strictly BETWEEN their tokens are left
    ///     alone (handled manually). Enclosing <c>#if/#endif</c> blocks are fine - the wrap
    ///     simply lands inside them.
    ///   </item>
    /// </list>
    /// </remarks>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(IntroduceUnsafeBlockCodeFixProvider)), Shared]
    public sealed class IntroduceUnsafeBlockCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        private const string WrapStatementTitle = "Wrap in 'unsafe' block (SAFETY-TODO)";
        private const string WrapBodyTitle = "Wrap entire member body in 'unsafe' block (SAFETY-TODO)";

        /// <summary>
        /// Compiler diagnostic IDs that this fixer can introduce an <c>unsafe { }</c> block to resolve.
        /// </summary>
        private static readonly ImmutableArray<string> UnsafeDiagnosticIds =
        [
            UnsafeEvolutionDescriptors.PointersAndFixedBuffersUnsafe,
            UnsafeEvolutionDescriptors.UnsafeOperation,
            UnsafeEvolutionDescriptors.UnsafeUninitializedStackAlloc,
            UnsafeEvolutionDescriptors.UnsafeMemberOperation,
            UnsafeEvolutionDescriptors.UnsafeMemberOperationCompat,
        ];

        public override ImmutableArray<string> FixableDiagnosticIds => UnsafeDiagnosticIds;

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();

            if (await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root)
                return;

            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            // Lambdas / anonymous functions do not inherit unsafe context from an enclosing
            // member, so we cannot fix a diagnostic that lives in one by wrapping an outer
            // statement.
            var containingStatement = FindContainingStatement(node);
            if (containingStatement is null)
            {
                // Expression-bodied members (e.g. 'int M() => Helper();') have no enclosing
                // statement. If we can rewrite the arrow body into a block body, offer that fix.
                var arrow = FindContainingArrowBody(node);
                if (arrow is null)
                    return;

                var semanticModelForArrow = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: WrapStatementTitle,
                        createChangedDocument: ct => WrapArrowBodyAsync(document, arrow, semanticModelForArrow, ct),
                        equivalenceKey: WrapStatementTitle),
                    diagnostic);
                return;
            }

            // Skip statements whose tokens enclose a preprocessor directive (e.g. an argument
            // list with #if/#else/#endif between commas). Wrapping such a statement would
            // corrupt the directive region.
            if (UnsafeBlockHelpers.ContainsInternalDirectiveTrivia(containingStatement))
                return;

            var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            // Decide the wrap strategy: split a local declaration into forward-decl + unsafe block
            // when its declared locals (or pattern variables in its initializer) are used after the
            // wrap, otherwise wrap the statement as-is.
            var strategy = ChooseFixStrategy(containingStatement, semanticModel);
            if (strategy is FixStrategy.Skip)
                return;

            // If the diagnostic clusters densely inside a member body, prefer one big wrap.
            var containingMember = FindContainingMember(node);
            BlockSyntax? memberBody = GetMemberBody(containingMember);
            if (memberBody is not null && ShouldWrapEntireBody(memberBody, semanticModel, context.CancellationToken))
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: WrapBodyTitle,
                        createChangedDocument: ct => WrapMemberBodyAsync(document, memberBody, ct),
                        equivalenceKey: WrapBodyTitle),
                    diagnostic);
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: WrapStatementTitle,
                    createChangedDocument: ct => WrapSingleStatementAsync(document, containingStatement, strategy, ct),
                    equivalenceKey: WrapStatementTitle),
                diagnostic);
        }

        // ---- Diagnostic-ID classification ----

        internal static bool IsUnsafeDiagnosticId(string id) => UnsafeDiagnosticIds.Contains(id);

        // ---- Boundary-aware ancestor walks ----

        /// <summary>
        /// Finds the smallest containing <see cref="StatementSyntax"/>, but stops at lambda /
        /// anonymous-function boundaries: diagnostics inside an expression-bodied lambda
        /// produce no enclosing statement we are allowed to wrap.
        /// </summary>
        private static StatementSyntax? FindContainingStatement(SyntaxNode node)
        {
            for (var n = node; n is not null; n = n.Parent)
            {
                if (n is StatementSyntax statement)
                    return statement;
                if (n is AnonymousFunctionExpressionSyntax)
                    return null;
            }
            return null;
        }

        /// <summary>
        /// Finds the smallest enclosing <see cref="ArrowExpressionClauseSyntax"/> (the body
        /// of an expression-bodied member or accessor), stopping at lambda / anonymous
        /// function boundaries: an expression-bodied lambda is not a member body we can
        /// rewrite.
        /// </summary>
        private static ArrowExpressionClauseSyntax? FindContainingArrowBody(SyntaxNode node)
        {
            for (var n = node; n is not null; n = n.Parent)
            {
                if (n is ArrowExpressionClauseSyntax arrow)
                    return arrow;
                if (n is AnonymousFunctionExpressionSyntax)
                    return null;
            }
            return null;
        }

        /// <summary>
        /// Finds the smallest method-like declaration that contains the diagnostic, stopping
        /// at lambda boundaries (unsafe context does not flow into lambdas, so wrapping the
        /// outer member's body would not help a diagnostic that lives inside one).
        /// </summary>
        private static SyntaxNode? FindContainingMember(SyntaxNode node)
        {
            for (var n = node; n is not null; n = n.Parent)
            {
                if (n is LocalFunctionStatementSyntax lf)
                    return lf;
                if (n is AnonymousFunctionExpressionSyntax)
                    return null;
                if (IsMemberWithBlockBody(n))
                    return n;
            }
            return null;
        }

        private static bool IsMemberWithBlockBody(SyntaxNode n) => n is
            MethodDeclarationSyntax
            or ConstructorDeclarationSyntax
            or DestructorDeclarationSyntax
            or OperatorDeclarationSyntax
            or ConversionOperatorDeclarationSyntax
            or LocalFunctionStatementSyntax
            or AccessorDeclarationSyntax;

        private static BlockSyntax? GetMemberBody(SyntaxNode? member) => member switch
        {
            MethodDeclarationSyntax m => m.Body,
            ConstructorDeclarationSyntax c => c.Body,
            DestructorDeclarationSyntax d => d.Body,
            OperatorDeclarationSyntax op => op.Body,
            ConversionOperatorDeclarationSyntax co => co.Body,
            LocalFunctionStatementSyntax lf => lf.Body,
            AccessorDeclarationSyntax a => a.Body,
            _ => null,
        };

        // ---- Whole-body wrap decision ----

        private static bool ShouldWrapEntireBody(BlockSyntax body, SemanticModel? semanticModel, CancellationToken ct)
        {
            if (semanticModel is null)
                return false;
            int unsafeCount = CountUnsafeDiagnosticsInBody(body, semanticModel, ct);
            int statementCount = CountStatementsOutsideLambdas(body);
            return UnsafeBlockHelpers.ShouldWrapEntireBody(unsafeCount, statementCount);
        }

        private static int CountStatementsOutsideLambdas(BlockSyntax body)
        {
            int count = 0;
            foreach (var s in body.DescendantNodes(descendIntoChildren: static n => !IsLambdaOrLocalFunction(n))
                                  .OfType<StatementSyntax>())
            {
                if (s is not BlockSyntax)
                    count++;
            }
            return count;
        }

        private static int CountUnsafeDiagnosticsInBody(BlockSyntax body, SemanticModel semanticModel, CancellationToken ct)
        {
            int count = 0;
            foreach (var d in semanticModel.GetDiagnostics(body.Span, ct))
            {
                if (!IsUnsafeDiagnosticId(d.Id))
                    continue;
                // Wrapping the outer body doesn't establish unsafe context inside nested lambdas.
                var diagNode = body.FindNode(d.Location.SourceSpan, getInnermostNodeForTie: true);
                if (IsInsideLambdaWithinBody(diagNode, body))
                    continue;
                count++;
            }
            return count;
        }

        private static bool IsLambdaOrLocalFunction(SyntaxNode n) =>
            n is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax;

        private static bool IsInsideLambdaWithinBody(SyntaxNode node, BlockSyntax body)
        {
            for (var n = node; n is not null && n != body; n = n.Parent)
            {
                if (IsLambdaOrLocalFunction(n))
                    return true;
            }
            return false;
        }

        // ---- Fix strategy: wrap as-is vs split into forward-decl + assignment ----

        private enum FixStrategy
        {
            Skip,
            WrapAsIs,
            ForwardDeclare,
        }

        private static FixStrategy ChooseFixStrategy(StatementSyntax statement, SemanticModel? semanticModel)
        {
            if (statement is not LocalDeclarationStatementSyntax local)
                return FixStrategy.WrapAsIs;

            // Init-declared variables (out var, pattern variables) cannot follow the local out
            // of the unsafe block; if any are referenced after, neither strategy is safe.
            if (HasInitializerDeclaredVariablesUsedAfter(local, semanticModel))
                return FixStrategy.Skip;

            // If the declared locals themselves don't escape past this statement, we can safely
            // wrap the whole declaration in unsafe.
            if (!DeclaredLocalsUsedAfter(local, semanticModel))
                return FixStrategy.WrapAsIs;

            // Locals escape - we must keep them visible after the unsafe block. Try to split
            // the declaration into a forward-decl + assignment; if we cannot, give up.
            return TryRewriteAsForwardDeclaration(local, semanticModel) is not null
                ? FixStrategy.ForwardDeclare
                : FixStrategy.Skip;
        }

        private static bool DeclaredLocalsUsedAfter(LocalDeclarationStatementSyntax local, SemanticModel? semanticModel)
        {
            if (semanticModel is null)
                return true; // conservative: assume escape

            var declared = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            foreach (var v in local.Declaration.Variables)
            {
                if (semanticModel.GetDeclaredSymbol(v) is { } s)
                    declared.Add(s);
            }
            return declared.Count > 0 && AnyReferenceAfter(local, declared, semanticModel);
        }

        private static bool HasInitializerDeclaredVariablesUsedAfter(LocalDeclarationStatementSyntax local, SemanticModel? semanticModel)
        {
            if (semanticModel is null || local.Declaration.Variables.Count != 1)
                return false;

            var initializer = local.Declaration.Variables[0].Initializer;
            if (initializer is null)
                return false;

            var initVars = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            // Both 'M(out var x)' and 'switch (e) { _ when e is T t => ... }' surface as
            // SingleVariableDesignationSyntax; one walk covers both.
            foreach (var designation in initializer.DescendantNodes().OfType<SingleVariableDesignationSyntax>())
            {
                if (semanticModel.GetDeclaredSymbol(designation) is { } s)
                    initVars.Add(s);
            }
            return initVars.Count > 0 && AnyReferenceAfter(local, initVars, semanticModel);
        }

        private static bool AnyReferenceAfter(StatementSyntax statement, HashSet<ISymbol> symbols, SemanticModel semanticModel)
        {
            // Look at the containing statement list. Both BlockSyntax and SwitchSectionSyntax
            // hold sibling statements in which the declared local is in scope; embedded
            // statements (if/while/etc.) have neither parent kind and locals don't escape.
            SyntaxList<StatementSyntax> siblings;
            switch (statement.Parent)
            {
                case BlockSyntax block:
                    siblings = block.Statements;
                    break;
                case SwitchSectionSyntax section:
                    siblings = section.Statements;
                    break;
                default:
                    return false;
            }

            int idx = siblings.IndexOf(statement);
            for (int i = idx + 1; i < siblings.Count; i++)
            {
                foreach (var id in siblings[i].DescendantNodes().OfType<IdentifierNameSyntax>())
                {
                    if (semanticModel.GetSymbolInfo(id).Symbol is { } sym && symbols.Contains(sym))
                        return true;
                }
            }
            return false;
        }

        // ---- Wrapping the whole member body ----

        private static async Task<Document> WrapMemberBodyAsync(Document document, BlockSyntax body, CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            if (root is null || body.Statements.Count == 0)
                return document;

            // Preserve each statement's existing leading trivia (comments, blank lines).
            // BuildUnsafeBlock prepends the SAFETY-TODO comment to the first statement's
            // existing trivia so user-authored comments survive.
            var unsafeBlock = BuildUnsafeBlock([.. body.Statements]);
            var newBody = SyntaxFactory.Block(unsafeBlock)
                .WithOpenBraceToken(body.OpenBraceToken)
                .WithCloseBraceToken(body.CloseBraceToken)
                .WithAdditionalAnnotations(Formatter.Annotation);

            return document.WithSyntaxRoot(root.ReplaceNode(body, newBody));
        }

        // ---- Wrapping an expression-bodied member ----

        private static async Task<Document> WrapArrowBodyAsync(
            Document document, ArrowExpressionClauseSyntax arrow, SemanticModel? semanticModel, CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            if (root is null || arrow.Parent is not { } member)
                return document;

            bool requiresReturn = ArrowBodyRequiresReturn(member, semanticModel);

            // Build 'return <expr>;' or '<expr>;' depending on the member's effective return type.
            // Preserve the original expression's trivia inside the new statement so any inline
            // comments authored on the arrow expression survive.
            StatementSyntax inner = requiresReturn
                ? SyntaxFactory.ReturnStatement(arrow.Expression.WithoutTrivia())
                : SyntaxFactory.ExpressionStatement(arrow.Expression.WithoutTrivia());
            var unsafeBlock = BuildUnsafeBlock([inner]);
            var newBody = SyntaxFactory.Block(unsafeBlock).WithAdditionalAnnotations(Formatter.Annotation);

            // Replace the member's '=> expr;' with '{ unsafe { ... } }'.
            var newMember = ReplaceArrowWithBlock(member, newBody);
            if (newMember is null)
                return document;

            return document.WithSyntaxRoot(root.ReplaceNode(member, newMember));
        }

        /// <summary>
        /// True if rewriting <paramref name="memberWithArrowBody"/>'s arrow body into a block
        /// body requires a <c>return</c> statement around the original expression. False when
        /// the member produces no value (void method/local-function, set/init/add/remove
        /// accessor, constructor/destructor, or an async method whose only result is the Task).
        /// </summary>
        private static bool ArrowBodyRequiresReturn(SyntaxNode memberWithArrowBody, SemanticModel? semanticModel)
        {
            switch (memberWithArrowBody)
            {
                case MethodDeclarationSyntax m:
                    return !IsVoidLikeMethod(m.ReturnType, m.Modifiers, semanticModel, m);
                case LocalFunctionStatementSyntax lf:
                    return !IsVoidLikeMethod(lf.ReturnType, lf.Modifiers, semanticModel, lf);
                case OperatorDeclarationSyntax:
                case ConversionOperatorDeclarationSyntax:
                case PropertyDeclarationSyntax:
                case IndexerDeclarationSyntax:
                    return true;
                case AccessorDeclarationSyntax a:
                    return a.Keyword.IsKind(SyntaxKind.GetKeyword);
                case ConstructorDeclarationSyntax:
                case DestructorDeclarationSyntax:
                    return false;
                default:
                    // Unknown member kind - rewriting could produce broken syntax; assume no return.
                    return false;
            }
        }

        private static bool IsVoidLikeMethod(TypeSyntax returnType, SyntaxTokenList modifiers, SemanticModel? semanticModel, SyntaxNode declaration)
        {
            if (returnType is PredefinedTypeSyntax p && p.Keyword.IsKind(SyntaxKind.VoidKeyword))
                return true;

            // 'async Task' / 'async ValueTask' bodies do not return a value; the await/return
            // expression is just an expression statement when in block form.
            if (modifiers.Any(SyntaxKind.AsyncKeyword) && semanticModel is not null
                && semanticModel.GetDeclaredSymbol(declaration) is IMethodSymbol m
                && m.ReturnType is INamedTypeSymbol rt
                && rt.TypeArguments.Length == 0
                && rt.Name is "Task" or "ValueTask")
            {
                return true;
            }

            return false;
        }

        private static SyntaxNode? ReplaceArrowWithBlock(SyntaxNode member, BlockSyntax newBody) => member switch
        {
            MethodDeclarationSyntax m => m.WithExpressionBody(null).WithBody(newBody).WithSemicolonToken(default),
            LocalFunctionStatementSyntax lf => lf.WithExpressionBody(null).WithBody(newBody).WithSemicolonToken(default),
            OperatorDeclarationSyntax op => op.WithExpressionBody(null).WithBody(newBody).WithSemicolonToken(default),
            ConversionOperatorDeclarationSyntax co => co.WithExpressionBody(null).WithBody(newBody).WithSemicolonToken(default),
            ConstructorDeclarationSyntax c => c.WithExpressionBody(null).WithBody(newBody).WithSemicolonToken(default),
            DestructorDeclarationSyntax d => d.WithExpressionBody(null).WithBody(newBody).WithSemicolonToken(default),
            AccessorDeclarationSyntax a => a.WithExpressionBody(null).WithBody(newBody).WithSemicolonToken(default),
            // For arrow-bodied property/indexer ('int P => expr;'), turn it into a block with a
            // single get accessor whose body is the new unsafe block.
            PropertyDeclarationSyntax prop => prop
                .WithExpressionBody(null)
                .WithSemicolonToken(default)
                .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithBody(newBody)))),
            IndexerDeclarationSyntax idx => idx
                .WithExpressionBody(null)
                .WithSemicolonToken(default)
                .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithBody(newBody)))),
            _ => null,
        };

        // ---- Wrapping a single statement ----

        private static async Task<Document> WrapSingleStatementAsync(
            Document document, StatementSyntax statement, FixStrategy strategy, CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            if (root is null)
                return document;

            SyntaxNode newRoot;
            // Only attach the original statement's outer trivia to the inner pieces when we
            // are going to splice them into an existing statement list. For embedded
            // statements (parent is not Block/SwitchSection) ReplaceStatementWithStatements
            // wraps the result in a fresh block and applies the trivia there - applying it
            // here too would duplicate comments, blank lines, and indentation.
            bool willSplice = statement.Parent is BlockSyntax or SwitchSectionSyntax;
            if (strategy is FixStrategy.ForwardDeclare)
            {
                var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
                var rewrite = TryRewriteAsForwardDeclaration((LocalDeclarationStatementSyntax)statement, semanticModel);
                if (rewrite is null)
                {
                    // Defensive: ChooseFixStrategy already verified the rewrite is possible by
                    // calling TryRewriteAsForwardDeclaration, so this branch should be unreachable.
                    // If it is reached (e.g. a future semantic-model change makes the rewrite fail
                    // here but not at strategy-decision time), bail out unchanged rather than
                    // producing a broken fix; wrap-as-is would not be safe because the local is
                    // referenced after the wrap point (that is why we picked ForwardDeclare).
                    return document;
                }

                var forwardDecl = rewrite.Value.ForwardDeclaration;
                var unsafeBlock = BuildUnsafeBlock([rewrite.Value.AssignmentStatement])
                    .WithAdditionalAnnotations(Formatter.Annotation);
                if (willSplice)
                {
                    forwardDecl = forwardDecl.WithLeadingTrivia(statement.GetLeadingTrivia());
                    unsafeBlock = unsafeBlock.WithTrailingTrivia(statement.GetTrailingTrivia());
                }

                newRoot = ReplaceStatementWithStatements(root, statement, [forwardDecl, unsafeBlock]);
            }
            else
            {
                var inner = statement.WithoutLeadingTrivia().WithoutTrailingTrivia();
                var unsafeBlock = BuildUnsafeBlock([inner])
                    .WithAdditionalAnnotations(Formatter.Annotation);
                if (willSplice)
                {
                    unsafeBlock = unsafeBlock
                        .WithLeadingTrivia(statement.GetLeadingTrivia())
                        .WithTrailingTrivia(statement.GetTrailingTrivia());
                }

                newRoot = ReplaceStatementWithStatements(root, statement, [unsafeBlock]);
            }

            return document.WithSyntaxRoot(newRoot);
        }

        // ---- Forward-declaration rewrite ----

        private readonly record struct ForwardDeclarationRewrite(
            LocalDeclarationStatementSyntax ForwardDeclaration,
            StatementSyntax AssignmentStatement);

        /// <summary>
        /// Attempts to split <paramref name="local"/> into a bare forward declaration
        /// (<c>scoped T x;</c> or <c>T x;</c>) plus an assignment statement that can be
        /// wrapped in an <c>unsafe { }</c> block. Returns <c>null</c> when the local
        /// cannot be split without changing semantics or producing uncompilable syntax.
        /// </summary>
        private static ForwardDeclarationRewrite? TryRewriteAsForwardDeclaration(
            LocalDeclarationStatementSyntax local,
            SemanticModel? semanticModel)
        {
            if (local.IsConst)
                return null;
            if (!local.UsingKeyword.IsKind(SyntaxKind.None))
                return null;
            if (local.Declaration.Variables.Count != 1)
                return null;
            if (local.Declaration.Type is RefTypeSyntax or ScopedTypeSyntax)
                return null;

            var variable = local.Declaration.Variables[0];
            if (variable.Initializer is null)
                return null;

            TypeSyntax typeSyntax = local.Declaration.Type;
            ITypeSymbol? typeSymbol = null;
            if (typeSyntax.IsVar)
            {
                if (semanticModel is null)
                    return null;
                typeSymbol = semanticModel.GetTypeInfo(typeSyntax).Type;
                if (typeSymbol is null or IErrorTypeSymbol)
                    return null;
                // Anonymous / tuple-element types cannot be named in user code.
                if (typeSymbol.IsAnonymousType || typeSymbol.Name.Length == 0)
                    return null;

                string typeName = typeSymbol.ToMinimalDisplayString(semanticModel, local.SpanStart);
                typeSyntax = SyntaxFactory.ParseTypeName(typeName);
                if (typeSyntax.ContainsDiagnostics)
                    return null;
            }
            else if (semanticModel is not null)
            {
                typeSymbol = semanticModel.GetTypeInfo(typeSyntax).Type;
            }

            // ref structs (e.g. Span<T>) need explicit 'scoped' so `x = stackalloc ...` compiles.
            TypeSyntax bareType = typeSyntax.WithoutTrivia().WithTrailingTrivia(SyntaxFactory.Space);
            TypeSyntax declType = typeSymbol is { IsRefLikeType: true }
                ? SyntaxFactory.ScopedType(
                    SyntaxFactory.Token(SyntaxKind.ScopedKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                    bareType)
                : bareType;

            var forwardVarDecl = SyntaxFactory.VariableDeclaration(
                declType,
                [SyntaxFactory.VariableDeclarator(variable.Identifier.WithoutTrivia())]);

            var forwardDecl = SyntaxFactory.LocalDeclarationStatement(forwardVarDecl)
                .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

            var assignment = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(variable.Identifier.WithoutTrivia()),
                    variable.Initializer.Value.WithoutTrivia()));

            return new ForwardDeclarationRewrite(forwardDecl, assignment);
        }

        // ---- Block construction ----

        private static UnsafeStatementSyntax BuildUnsafeBlock(IReadOnlyList<StatementSyntax> innerStatements)
        {
            var safetyComment = SyntaxFactory.Comment(UnsafeEvolutionDescriptors.SafetyTodoCommentText);
            var newline = SyntaxFactory.ElasticCarriageReturnLineFeed;

            // Attach the SAFETY-TODO comment as leading trivia of the first inner statement so
            // it lands inside the braces of the produced block (and any pre-existing leading
            // trivia of that statement, such as user comments, is preserved after the comment).
            var first = innerStatements[0];
            var commentedFirst = first.WithLeadingTrivia(first.GetLeadingTrivia().InsertRange(0, [safetyComment, newline]));

            var newStatements = new List<StatementSyntax>(innerStatements.Count) { commentedFirst };
            for (int i = 1; i < innerStatements.Count; i++)
                newStatements.Add(innerStatements[i]);

            return SyntaxFactory.UnsafeStatement(SyntaxFactory.Block(newStatements))
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        // ---- Replacement helpers ----

        private static SyntaxNode ReplaceStatementWithStatements(
            SyntaxNode root, StatementSyntax oldStatement, IReadOnlyList<StatementSyntax> newStatements)
        {
            // Splice into the containing block or switch-section's statement list when possible,
            // so we don't introduce an extra brace level. Embedded statements (e.g. the body of
            // 'if (cond) M();') have neither parent kind, so we wrap them in a fresh block.
            switch (oldStatement.Parent)
            {
                case BlockSyntax block:
                    return root.ReplaceNode(block, block.WithStatements(SpliceStatements(block.Statements, oldStatement, newStatements)));
                case SwitchSectionSyntax section:
                    return root.ReplaceNode(section, section.WithStatements(SpliceStatements(section.Statements, oldStatement, newStatements)));
                default:
                    var wrapping = SyntaxFactory.Block(newStatements)
                        .WithLeadingTrivia(oldStatement.GetLeadingTrivia())
                        .WithTrailingTrivia(oldStatement.GetTrailingTrivia())
                        .WithAdditionalAnnotations(Formatter.Annotation);
                    return root.ReplaceNode(oldStatement, wrapping);
            }
        }

        private static SyntaxList<StatementSyntax> SpliceStatements(
            SyntaxList<StatementSyntax> existing, StatementSyntax target, IReadOnlyList<StatementSyntax> replacement)
        {
            int idx = existing.IndexOf(target);
            var updated = existing.RemoveAt(idx);
            for (int i = replacement.Count - 1; i >= 0; i--)
                updated = updated.Insert(idx, replacement[i]);
            return updated;
        }
    }
}
#endif
