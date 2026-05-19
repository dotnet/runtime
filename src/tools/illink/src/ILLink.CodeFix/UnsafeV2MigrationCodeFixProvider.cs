// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using ILLink.RoslynAnalyzer;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Mechanical migration to "unsafe-v2" semantics. Handles two diagnostics:
    ///   IL5005 — removes the 'unsafe' modifier from a type declaration.
    ///   IL5006 — for a member with an 'unsafe' modifier:
    ///            * removes the modifier if the signature has no pointer / function-pointer types,
    ///            * wraps the body in a single 'unsafe { ... }' block (prefixed with a SAFETY-TODO
    ///              comment) unless the member has no body, is a trivial field-forwarding accessor,
    ///              the body already contains an 'unsafe' block anywhere, the body is empty, or the
    ///              body contains 'yield' statements (iterators cannot contain unsafe code).
    /// Known limitations (best-effort migration, developer to fix fallout):
    ///   * Members that lacked an explicit 'unsafe' modifier but relied on a containing type's
    ///     'unsafe' modifier (implicit unsafe context) are not wrapped automatically.
    ///   * Pointer expressions in constructor initializers (':base(...)' / ':this(...)') are
    ///     outside the body and won't be wrapped.
    ///   * '#if' directives inside the body / signature are kept as-is.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnsafeV2MigrationCodeFixProvider)), Shared]
    public sealed class UnsafeV2MigrationCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        internal const string SafetyTodoComment = "// SAFETY-TODO: Audit this unsafe usage";

        private static readonly LocalizableString s_removeUnsafeFromTypeTitle =
            new LocalizableResourceString(nameof(Resources.UnsafeV2MigrationRemoveUnsafeFromTypeCodeFixTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString s_rewriteMemberTitle =
            new LocalizableResourceString(nameof(Resources.UnsafeV2MigrationRewriteMemberCodeFixTitle), Resources.ResourceManager, typeof(Resources));

        public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [
            DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.UnsafeModifierOnTypeDeclaration, diagnosticSeverity: DiagnosticSeverity.Info),
            DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.UnsafeModifierOnMemberDeclaration, diagnosticSeverity: DiagnosticSeverity.Info),
        ];

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            [.. SupportedDiagnostics.Select(static d => d.Id)];

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root)
                return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var unsafeToken = root.FindToken(diagnostic.Location.SourceSpan.Start);
                if (!unsafeToken.IsKind(SyntaxKind.UnsafeKeyword))
                    continue;

                if (diagnostic.Id == DiagnosticId.UnsafeModifierOnTypeDeclaration.AsString())
                {
                    var title = s_removeUnsafeFromTypeTitle.ToString();
                    context.RegisterCodeFix(CodeAction.Create(
                        title: title,
                        createChangedDocument: ct => RemoveUnsafeFromTypeAsync(context.Document, unsafeToken, ct),
                        equivalenceKey: title), diagnostic);
                }
                else if (diagnostic.Id == DiagnosticId.UnsafeModifierOnMemberDeclaration.AsString())
                {
                    var title = s_rewriteMemberTitle.ToString();
                    context.RegisterCodeFix(CodeAction.Create(
                        title: title,
                        createChangedDocument: ct => RewriteUnsafeMemberAsync(context.Document, unsafeToken, ct),
                        equivalenceKey: title), diagnostic);
                }
            }
        }

        // ----- IL5005 fix: remove 'unsafe' from a type declaration -----------------------------

        private static async Task<Document> RemoveUnsafeFromTypeAsync(Document document, SyntaxToken unsafeToken, CancellationToken cancellationToken)
        {
            var typeDecl = unsafeToken.Parent?.AncestorsAndSelf().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
            if (typeDecl is null)
                return document;

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            editor.ReplaceNode(typeDecl, RemoveUnsafeKeyword(typeDecl));
            return editor.GetChangedDocument();
        }

        // ----- IL5006 fix: migrate an 'unsafe' member ------------------------------------------

        private static async Task<Document> RewriteUnsafeMemberAsync(Document document, SyntaxToken unsafeToken, CancellationToken cancellationToken)
        {
            var memberNode = FindMemberDeclaration(unsafeToken);
            if (memberNode is null)
                return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var newMember = RewriteMember(memberNode, semanticModel);
            if (ReferenceEquals(newMember, memberNode))
                return document;

            // Tag the new member so we can scope the formatter to just the changed subtree.
            newMember = newMember.WithAdditionalAnnotations(Formatter.Annotation);

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            editor.ReplaceNode(memberNode, newMember);
            var changedDocument = editor.GetChangedDocument();

            // 'dotnet format analyzers' applies code-fix output verbatim and does not run a
            // subsequent whitespace pass, so we explicitly format the changed subtree here.
            // 'dotnet format' run later would also be idempotent on the result.
            return await Formatter.FormatAsync(changedDocument, Formatter.Annotation, options: null, cancellationToken).ConfigureAwait(false);
        }

        private static SyntaxNode? FindMemberDeclaration(SyntaxToken unsafeToken) =>
            unsafeToken.Parent?.AncestorsAndSelf().FirstOrDefault(static n =>
                n is MemberDeclarationSyntax or LocalFunctionStatementSyntax);

        private static SyntaxNode RewriteMember(SyntaxNode member, SemanticModel? semanticModel)
        {
            // 1. Remove the 'unsafe' modifier when the signature carries no pointer / function-pointer types.
            var current = UnsafeV2MigrationAnalyzer.CanRemoveUnsafeModifier(member, semanticModel)
                ? RemoveUnsafeKeyword(member)
                : member;

            // 2. Wrap the body / accessors in 'unsafe { ... }' if appropriate.
            return WrapBody(current);
        }

        // ----- Body wrapping -------------------------------------------------------------------

        private static SyntaxNode WrapBody(SyntaxNode member)
        {
            return member switch
            {
                MethodDeclarationSyntax m => WrapMethodLike(m, m.Body, m.ExpressionBody,
                    static (n, body) => n.WithBody(body).WithExpressionBody(null).WithSemicolonToken(default),
                    isVoid: IsVoid(m.ReturnType)),
                LocalFunctionStatementSyntax lf => WrapMethodLike(lf, lf.Body, lf.ExpressionBody,
                    static (n, body) => n.WithBody(body).WithExpressionBody(null).WithSemicolonToken(default),
                    isVoid: IsVoid(lf.ReturnType)),
                ConstructorDeclarationSyntax c => WrapMethodLike(c, c.Body, c.ExpressionBody,
                    static (n, body) => n.WithBody(body).WithExpressionBody(null).WithSemicolonToken(default),
                    isVoid: true),
                DestructorDeclarationSyntax d => WrapMethodLike(d, d.Body, d.ExpressionBody,
                    static (n, body) => n.WithBody(body).WithExpressionBody(null).WithSemicolonToken(default),
                    isVoid: true),
                OperatorDeclarationSyntax op => WrapMethodLike(op, op.Body, op.ExpressionBody,
                    static (n, body) => n.WithBody(body).WithExpressionBody(null).WithSemicolonToken(default),
                    isVoid: IsVoid(op.ReturnType)),
                ConversionOperatorDeclarationSyntax co => WrapMethodLike(co, co.Body, co.ExpressionBody,
                    static (n, body) => n.WithBody(body).WithExpressionBody(null).WithSemicolonToken(default),
                    isVoid: false),
                PropertyDeclarationSyntax p => WrapProperty(p),
                IndexerDeclarationSyntax i => WrapIndexer(i),
                EventDeclarationSyntax e when e.AccessorList is not null => e.WithAccessorList(WrapAccessorList(e.AccessorList)),
                _ => member,
            };
        }

        private static T WrapMethodLike<T>(
            T member,
            BlockSyntax? body,
            ArrowExpressionClauseSyntax? expressionBody,
            Func<T, BlockSyntax, T> withBody,
            bool isVoid) where T : SyntaxNode
        {
            if (body is not null)
            {
                if (!ShouldWrapBlock(body))
                    return member;

                return withBody(member, ReplaceWithUnsafeBlock(body));
            }

            if (expressionBody is not null)
            {
                if (!ShouldWrapExpression(expressionBody))
                    return member;

                StatementSyntax inner = isVoid
                    ? SyntaxFactory.ExpressionStatement(expressionBody.Expression.WithoutTrivia())
                    : SyntaxFactory.ReturnStatement(expressionBody.Expression.WithoutTrivia());
                var newBlock = SyntaxFactory.Block(BuildUnsafeStatement(inner));
                return withBody(member, newBlock);
            }

            return member;
        }

        private static PropertyDeclarationSyntax WrapProperty(PropertyDeclarationSyntax prop)
        {
            if (prop.ExpressionBody is { } expr)
            {
                if (IsTrivialFieldExpression(expr.Expression) || !ShouldWrapExpression(expr))
                    return prop;

                var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithBody(SyntaxFactory.Block(BuildUnsafeStatement(SyntaxFactory.ReturnStatement(expr.Expression.WithoutTrivia()))));
                return prop
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(getter)));
            }

            if (prop.AccessorList is { } list)
                return prop.WithAccessorList(WrapAccessorList(list));

            return prop;
        }

        private static IndexerDeclarationSyntax WrapIndexer(IndexerDeclarationSyntax indexer)
        {
            if (indexer.ExpressionBody is { } expr)
            {
                if (IsTrivialFieldExpression(expr.Expression) || !ShouldWrapExpression(expr))
                    return indexer;

                var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithBody(SyntaxFactory.Block(BuildUnsafeStatement(SyntaxFactory.ReturnStatement(expr.Expression.WithoutTrivia()))));
                return indexer
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(getter)));
            }

            if (indexer.AccessorList is { } list)
                return indexer.WithAccessorList(WrapAccessorList(list));

            return indexer;
        }

        private static AccessorListSyntax WrapAccessorList(AccessorListSyntax list)
        {
            var wrapped = SyntaxFactory.List(list.Accessors.Select(WrapAccessor));
            return list.WithAccessors(wrapped);
        }

        private static AccessorDeclarationSyntax WrapAccessor(AccessorDeclarationSyntax accessor)
        {
            bool isVoid = !accessor.IsKind(SyntaxKind.GetAccessorDeclaration);

            if (accessor.Body is { } body)
            {
                if (!ShouldWrapBlock(body))
                    return accessor;
                return accessor.WithBody(ReplaceWithUnsafeBlock(body));
            }

            if (accessor.ExpressionBody is { } expr)
            {
                if (IsTrivialAccessorExpression(accessor, expr.Expression) || !ShouldWrapExpression(expr))
                    return accessor;

                StatementSyntax inner = isVoid
                    ? SyntaxFactory.ExpressionStatement(expr.Expression.WithoutTrivia())
                    : SyntaxFactory.ReturnStatement(expr.Expression.WithoutTrivia());
                return accessor
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithBody(SyntaxFactory.Block(BuildUnsafeStatement(inner)));
            }

            // Auto-property accessor (no body, no expression body) — nothing to wrap.
            return accessor;
        }

        private static BlockSyntax ReplaceWithUnsafeBlock(BlockSyntax body)
        {
            // Strip the original leading whitespace from each statement so the formatter
            // re-indents them inside the new 'unsafe { }' block. We deliberately keep any
            // leading comments / directives the original statements had.
            var statements = body.Statements.Select(StripLeadingWhitespace).ToArray();
            var inner = SyntaxFactory.Block(statements);
            var unsafeStmt = SyntaxFactory.UnsafeStatement(inner)
                .WithLeadingTrivia(BuildSafetyTodoLeadingTrivia());
            return SyntaxFactory.Block(unsafeStmt)
                .WithLeadingTrivia(body.GetLeadingTrivia())
                .WithTrailingTrivia(body.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static UnsafeStatementSyntax BuildUnsafeStatement(StatementSyntax statement)
        {
            var inner = SyntaxFactory.Block(StripLeadingWhitespace(statement));
            return SyntaxFactory.UnsafeStatement(inner)
                .WithLeadingTrivia(BuildSafetyTodoLeadingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static StatementSyntax StripLeadingWhitespace(StatementSyntax statement)
        {
            // Strip only the indentation whitespace from the statement's leading trivia and
            // let the formatter (via Formatter.Annotation on the enclosing block) compute the
            // new indentation. We must preserve EndOfLineTrivia so that blank lines between
            // statements survive the wrap, and we must preserve any leading comments / pragmas
            // / directives the statement had.
            var leading = statement.GetLeadingTrivia();
            var stripped = SyntaxFactory.TriviaList(
                leading.Where(static t => !t.IsKind(SyntaxKind.WhitespaceTrivia)));
            return stripped.Count == leading.Count
                ? statement
                : statement.WithLeadingTrivia(stripped);
        }

        private static SyntaxTriviaList BuildSafetyTodoLeadingTrivia() =>
            SyntaxFactory.TriviaList(
                SyntaxFactory.Comment(SafetyTodoComment),
                SyntaxFactory.ElasticCarriageReturnLineFeed);

        // ----- Skip conditions -----------------------------------------------------------------

        private static bool ShouldWrapBlock(BlockSyntax body) =>
            body.Statements.Count > 0
            && !BodyAlreadyHasUnsafeContext(body)
            && !BodyHasYield(body);

        private static bool ShouldWrapExpression(ArrowExpressionClauseSyntax expr) =>
            !expr.DescendantNodesAndSelf().OfType<UnsafeStatementSyntax>().Any();

        private static bool BodyAlreadyHasUnsafeContext(BlockSyntax body) =>
            // Per spec: any 'unsafe' block anywhere in the body is sufficient — even nested in
            // lambdas/local functions — to skip wrapping ('best effort').
            body.DescendantNodes().OfType<UnsafeStatementSyntax>().Any();

        private static bool BodyHasYield(BlockSyntax body) =>
            // Iterators can never contain unsafe blocks (CS1629). Skip wrapping when we see one,
            // ignoring nested local functions/lambdas which have their own scope.
            body.DescendantNodes(static n => n is not LocalFunctionStatementSyntax and not AnonymousFunctionExpressionSyntax)
                .OfType<YieldStatementSyntax>()
                .Any();

        private static bool IsTrivialFieldExpression(ExpressionSyntax expr) => expr switch
        {
            IdentifierNameSyntax => true,
            MemberAccessExpressionSyntax mae => mae.Expression is ThisExpressionSyntax or BaseExpressionSyntax or IdentifierNameSyntax,
            ParenthesizedExpressionSyntax pe => IsTrivialFieldExpression(pe.Expression),
            _ => false,
        };

        private static bool IsTrivialAccessorExpression(AccessorDeclarationSyntax accessor, ExpressionSyntax expr)
        {
            if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                return IsTrivialFieldExpression(expr);

            if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration) || accessor.IsKind(SyntaxKind.InitAccessorDeclaration))
            {
                return expr is AssignmentExpressionSyntax { Right: IdentifierNameSyntax { Identifier.ValueText: "value" } } assign
                    && IsTrivialFieldExpression(assign.Left);
            }

            return false;
        }

        // ----- Modifier helpers ----------------------------------------------------------------

        /// <summary>
        /// Removes the 'unsafe' modifier from a node and preserves the leading trivia
        /// (typically indentation) of the removed token by transferring it onto whatever
        /// becomes the new "first" token of the declaration.
        /// </summary>
        private static T RemoveUnsafeKeyword<T>(T node) where T : SyntaxNode
        {
            var modifiers = GetModifiers(node);
            var index = modifiers.IndexOf(SyntaxKind.UnsafeKeyword);
            if (index < 0)
                return node;

            var unsafeToken = modifiers[index];
            var newModifiers = modifiers.RemoveAt(index);

            if (index == 0 && newModifiers.Count > 0)
            {
                var first = newModifiers[0];
                newModifiers = newModifiers.Replace(first, first.WithLeadingTrivia(unsafeToken.LeadingTrivia.AddRange(first.LeadingTrivia)));
            }

            var updated = (T)WithModifiers(node, newModifiers);

            // No modifiers left and the removed 'unsafe' carried indentation/newline trivia
            // that would otherwise be lost. Push it onto the first non-attribute token.
            if (index == 0 && newModifiers.Count == 0 && !unsafeToken.LeadingTrivia.All(static t => t.IsKind(SyntaxKind.WhitespaceTrivia) && t.Span.Length == 0))
            {
                updated = PrependLeadingTriviaToFirstNonAttributeToken(updated, unsafeToken.LeadingTrivia);
            }

            return updated;
        }

        private static T PrependLeadingTriviaToFirstNonAttributeToken<T>(T node, SyntaxTriviaList trivia) where T : SyntaxNode
        {
            SyntaxToken target = default;
            foreach (var token in node.DescendantTokens())
            {
                if (token.Parent is not null && token.Parent.AncestorsAndSelf().OfType<AttributeListSyntax>().Any())
                    continue;
                target = token;
                break;
            }

            if (target.IsKind(SyntaxKind.None))
                return node;

            return node.ReplaceToken(target, target.WithLeadingTrivia(trivia.AddRange(target.LeadingTrivia)));
        }

        private static SyntaxTokenList GetModifiers(SyntaxNode node) => UnsafeV2MigrationAnalyzer.GetModifiers(node);

        private static SyntaxNode WithModifiers(SyntaxNode node, SyntaxTokenList modifiers) => node switch
        {
            ClassDeclarationSyntax c => c.WithModifiers(modifiers),
            StructDeclarationSyntax s => s.WithModifiers(modifiers),
            InterfaceDeclarationSyntax i => i.WithModifiers(modifiers),
            RecordDeclarationSyntax r => r.WithModifiers(modifiers),
            MethodDeclarationSyntax m => m.WithModifiers(modifiers),
            ConstructorDeclarationSyntax ctor => ctor.WithModifiers(modifiers),
            DestructorDeclarationSyntax d => d.WithModifiers(modifiers),
            OperatorDeclarationSyntax op => op.WithModifiers(modifiers),
            ConversionOperatorDeclarationSyntax co => co.WithModifiers(modifiers),
            PropertyDeclarationSyntax p => p.WithModifiers(modifiers),
            IndexerDeclarationSyntax idx => idx.WithModifiers(modifiers),
            EventDeclarationSyntax e => e.WithModifiers(modifiers),
            EventFieldDeclarationSyntax ef => ef.WithModifiers(modifiers),
            LocalFunctionStatementSyntax lf => lf.WithModifiers(modifiers),
            _ => node,
        };

        private static bool IsVoid(TypeSyntax? type) =>
            type is PredefinedTypeSyntax pts && pts.Keyword.IsKind(SyntaxKind.VoidKeyword);
    }
}
#endif
