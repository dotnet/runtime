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
    /// Mechanical migration to "unsafe-v2" semantics (see csharplang's unsafe-evolution proposal).
    /// Handles two diagnostics:
    ///   IL5005 — removes the 'unsafe' modifier from a type or delegate declaration.
    ///            Per the spec, 'unsafe' on those has no meaning under the updated rules
    ///            and is removed unconditionally.
    ///   IL5006 — for a non-type member with an 'unsafe' modifier:
    ///            * For static constructors and destructors, removes the modifier (spec:
    ///              'unsafe' has no meaning on either) and still wraps the body in
    ///              'unsafe { ... }' if it has body content — under the legacy rules the
    ///              modifier opened an unsafe context for the body, so we preserve that
    ///              context for any pointer operations the body may contain.
    ///            * For instance constructors with an initializer ('base(...)' /
    ///              'this(...)'), keeps the modifier (under the updated rules 'unsafe' on
    ///              a ctor opens an unsafe context for its initializer — removing it
    ///              could break a base call that needs that context).
    ///            * Otherwise, removes the modifier when the signature carries no pointer
    ///              / function-pointer types, and wraps the body in a single
    ///              'unsafe { ... }' block whose first inner line is a SAFETY-TODO
    ///              comment, unless the body is empty, the member has no body, it already
    ///              contains an 'unsafe' block, the body contains conditional-compilation
    ///              directives (multi-TFM 'dotnet format' merge-conflict hazard — see
    ///              BlockNeedsWrap comment in the analyzer), or it's a trivial
    ///              field-forwarding accessor.
    /// Known limitations (best-effort migration, developer to fix fallout):
    ///   * Members that lacked an explicit 'unsafe' modifier but relied on a containing
    ///     type's 'unsafe' modifier (implicit unsafe context) are not wrapped automatically.
    ///   * Iterator bodies ('yield' inside) are wrapped even though that produces CS1629;
    ///     the developer manually moves the 'unsafe' inwards.
    ///   * Lambdas and anonymous methods are not handled — current Roslyn parsers reject
    ///     'unsafe' as a lambda modifier, so the analyzer never sees one.
    ///   * Wrapping the whole body in 'unsafe { ... }' moves every body-declared local into
    ///     a narrower scope than the enclosing method's parameters. A ref-like local
    ///     (e.g. a 'Span&lt;T&gt;' from 'stackalloc') that gets assigned to an outer
    ///     'scoped' parameter or returned by ref will now trigger CS9080 ("use of variable
    ///     '...' in this context may expose referenced variables outside of their
    ///     declaration scope"), even though the original code compiled. We don't try to
    ///     detect this — the developer either moves the 'unsafe' block tighter around
    ///     just the pointer operation, or hoists the local above the wrap.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnsafeV2MigrationCodeFixProvider)), Shared]
    public sealed class UnsafeV2MigrationCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        internal const string SafetyTodoComment = "// SAFETY-TODO: Audit this unsafe usage";

        private static readonly LocalizableString s_removeUnsafeFromTypeTitle =
            new LocalizableResourceString(nameof(Resources.UnsafeV2MigrationRemoveUnsafeFromTypeCodeFixTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString s_rewriteMemberTitle =
            new LocalizableResourceString(nameof(Resources.UnsafeV2MigrationRewriteMemberCodeFixTitle), Resources.ResourceManager, typeof(Resources));

        // Diagnostics this fixer can address — IL5005 (type/delegate) and IL5006 (member).
        public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [
            DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.UnsafeModifierOnTypeDeclaration, diagnosticSeverity: DiagnosticSeverity.Info),
            DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.UnsafeModifierOnMemberDeclaration, diagnosticSeverity: DiagnosticSeverity.Info),
        ];

        // Roslyn-required mapping from descriptors to diagnostic IDs.
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            [.. SupportedDiagnostics.Select(static d => d.Id)];

        // BatchFixer is sufficient — each diagnostic's fix is local to its declaration.
        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        // Per-diagnostic dispatch: type/delegate goes through RemoveUnsafeFromTypeAsync,
        // member goes through RewriteUnsafeMemberAsync.
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

        // IL5005 fix: locate the enclosing type/delegate decl and strip the `unsafe` token.
        private static async Task<Document> RemoveUnsafeFromTypeAsync(Document document, SyntaxToken unsafeToken, CancellationToken cancellationToken)
        {
            var typeDecl = unsafeToken.Parent?.AncestorsAndSelf().FirstOrDefault(static n =>
                n is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax);
            if (typeDecl is null)
                return document;

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            editor.ReplaceNode(typeDecl, RemoveUnsafeKeyword(typeDecl));
            return editor.GetChangedDocument();
        }

        // IL5006 fix: locate the enclosing member, rewrite it (modifier + body wrap), and
        // explicitly run Formatter.FormatAsync to re-indent the new `unsafe { ... }` block.
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

        // Walks ancestors from `unsafeToken` until it hits the nearest member declaration
        // (method, ctor, property, local function, etc.).
        private static SyntaxNode? FindMemberDeclaration(SyntaxToken unsafeToken) =>
            unsafeToken.Parent?.AncestorsAndSelf().FirstOrDefault(static n =>
                n is MemberDeclarationSyntax or LocalFunctionStatementSyntax);

        // Applies the IL5006 transformations in order: (1) optionally remove the `unsafe`
        // modifier, then (2) optionally wrap the body in `unsafe { ... }`.
        private static SyntaxNode RewriteMember(SyntaxNode member, SemanticModel? semanticModel)
        {
            // 1. Remove the 'unsafe' modifier if the analyzer says it's removable
            //    (signature has no pointer/function-pointer types, OR it's a static
            //    ctor / destructor where the spec says the modifier is meaningless).
            //    Non-static ctors with `: base(...)` / `: this(...)` keep the modifier.
            var current = UnsafeV2MigrationAnalyzer.CanRemoveUnsafeModifier(member, semanticModel)
                ? RemoveUnsafeKeyword(member)
                : member;

            // 2. Wrap the body / accessors in 'unsafe { ... }' if appropriate.
            return WrapBody(current);
        }

        // ----- Body wrapping -------------------------------------------------------------------

        // Dispatches a member to the right wrapping helper based on its syntactic shape.
        private static SyntaxNode WrapBody(SyntaxNode member) =>
            member switch
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
                EventDeclarationSyntax { AccessorList: not null } e => e.WithAccessorList(WrapAccessorList(e.AccessorList, isTrivialAllowed: false)),
                _ => member,
            };

        // Generic wrapper for member shapes that have a (block-or-expression) body and
        // know their own return-void-ness — methods, local functions, ctors, dtors,
        // operators, conversion operators.
        private static T WrapMethodLike<T>(
            T member,
            BlockSyntax? body,
            ArrowExpressionClauseSyntax? expressionBody,
            Func<T, BlockSyntax, T> withBody,
            bool isVoid) where T : SyntaxNode
        {
            if (body is not null)
            {
                if (!UnsafeV2MigrationAnalyzer.BlockNeedsWrap(body))
                    return member;

                return withBody(member, ReplaceWithUnsafeBlock(body));
            }

            if (expressionBody is not null)
            {
                if (!UnsafeV2MigrationAnalyzer.ExpressionBodyNeedsWrap(expressionBody))
                    return member;

                StatementSyntax inner = isVoid
                    ? SyntaxFactory.ExpressionStatement(expressionBody.Expression.WithoutTrivia())
                    : SyntaxFactory.ReturnStatement(expressionBody.Expression.WithoutTrivia());
                var newBlock = SyntaxFactory.Block(BuildUnsafeStatement(inner));
                return withBody(member, newBlock);
            }

            return member;
        }

        // Wraps a property: expression body becomes a `get` accessor; accessor list is
        // wrapped per-accessor with trivial-field-forwarders left alone.
        private static PropertyDeclarationSyntax WrapProperty(PropertyDeclarationSyntax prop)
        {
            if (prop.ExpressionBody is { } expr)
            {
                if (UnsafeV2MigrationAnalyzer.IsTrivialFieldExpression(expr.Expression)
                    || !UnsafeV2MigrationAnalyzer.ExpressionBodyNeedsWrap(expr))
                    return prop;

                var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithBody(SyntaxFactory.Block(BuildUnsafeStatement(SyntaxFactory.ReturnStatement(expr.Expression.WithoutTrivia()))));
                return prop
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(getter)));
            }

            if (prop.AccessorList is { } list)
                return prop.WithAccessorList(WrapAccessorList(list, isTrivialAllowed: true));

            return prop;
        }

        // Same as WrapProperty but for indexers (signature has a parameter list that we
        // preserve when converting an expression body to an accessor list).
        private static IndexerDeclarationSyntax WrapIndexer(IndexerDeclarationSyntax indexer)
        {
            if (indexer.ExpressionBody is { } expr)
            {
                if (UnsafeV2MigrationAnalyzer.IsTrivialFieldExpression(expr.Expression)
                    || !UnsafeV2MigrationAnalyzer.ExpressionBodyNeedsWrap(expr))
                    return indexer;

                var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithBody(SyntaxFactory.Block(BuildUnsafeStatement(SyntaxFactory.ReturnStatement(expr.Expression.WithoutTrivia()))));
                return indexer
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(getter)));
            }

            if (indexer.AccessorList is { } list)
                return indexer.WithAccessorList(WrapAccessorList(list, isTrivialAllowed: true));

            return indexer;
        }

        // Maps WrapAccessor over each accessor, passing the trivial-allowed flag
        // (true for property/indexer, false for event).
        private static AccessorListSyntax WrapAccessorList(AccessorListSyntax list, bool isTrivialAllowed) =>
            list.WithAccessors(SyntaxFactory.List(list.Accessors.Select(a => WrapAccessor(a, isTrivialAllowed))));

        // Wraps a single get/set/init/add/remove accessor: block body → wrap statements,
        // expression body → convert to block with `return` (get) or expression-statement
        // (set/init/add/remove). Leaves the accessor alone if the analyzer says no wrap.
        private static AccessorDeclarationSyntax WrapAccessor(AccessorDeclarationSyntax accessor, bool isTrivialAllowed)
        {
            // Decide whether to wrap via the analyzer's single source of truth, so the
            // fix can never disagree with the diagnostic.
            if (!UnsafeV2MigrationAnalyzer.AccessorNeedsWrap(accessor, isTrivialAllowed))
                return accessor;

            if (accessor.Body is { } body)
                return accessor.WithBody(ReplaceWithUnsafeBlock(body));

            // Expression-bodied accessor — convert to block. Getters wrap as 'return expr;',
            // every other accessor kind (set/init/add/remove) wraps as 'expr;'.
            var expr = accessor.ExpressionBody!.Expression;
            bool isVoid = !accessor.IsKind(SyntaxKind.GetAccessorDeclaration);
            StatementSyntax inner = isVoid
                ? SyntaxFactory.ExpressionStatement(expr.WithoutTrivia())
                : SyntaxFactory.ReturnStatement(expr.WithoutTrivia());
            return accessor
                .WithExpressionBody(null)
                .WithSemicolonToken(default)
                .WithBody(SyntaxFactory.Block(BuildUnsafeStatement(inner)));
        }

        // Rebuilds a block body so its statements live inside a single inner `unsafe { }`
        // statement, with the SAFETY-TODO comment placed inside the block on its own line
        // above the original body's first statement.
        private static BlockSyntax ReplaceWithUnsafeBlock(BlockSyntax body)
        {
            // Strip the original leading whitespace from each statement so the formatter
            // re-indents them inside the new 'unsafe { }' block. We deliberately keep any
            // leading comments / directives the original statements had.
            var statements = body.Statements.Select(StripLeadingWhitespace).ToArray();
            if (statements.Length > 0)
                statements[0] = PrefixWithSafetyTodo(statements[0]);
            var inner = SyntaxFactory.Block(statements);
            return SyntaxFactory.Block(SyntaxFactory.UnsafeStatement(inner))
                .WithLeadingTrivia(body.GetLeadingTrivia())
                .WithTrailingTrivia(body.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        // Builds a fresh `unsafe { statement }` with the SAFETY-TODO comment placed inside
        // the block on its own line above `statement`.
        private static UnsafeStatementSyntax BuildUnsafeStatement(StatementSyntax statement)
        {
            var inner = SyntaxFactory.Block(PrefixWithSafetyTodo(StripLeadingWhitespace(statement)));
            return SyntaxFactory.UnsafeStatement(inner)
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        // Returns `statement` with the SAFETY-TODO comment prepended to its leading trivia,
        // so the comment sits on its own line directly above the statement.
        private static StatementSyntax PrefixWithSafetyTodo(StatementSyntax statement) =>
            statement.WithLeadingTrivia(BuildSafetyTodoLeadingTrivia().AddRange(statement.GetLeadingTrivia()));

        // Removes leading indentation whitespace from a statement (preserving comments,
        // directives, and end-of-line trivia so blank lines / pragmas survive the wrap).
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

        // Builds the leading trivia placed before the first statement inside each emitted
        // `unsafe { ... }` block: the SAFETY-TODO single-line comment followed by an
        // elastic newline so the formatter puts the comment on its own indented line.
        private static SyntaxTriviaList BuildSafetyTodoLeadingTrivia() =>
            SyntaxFactory.TriviaList(
                SyntaxFactory.Comment(SafetyTodoComment),
                SyntaxFactory.ElasticCarriageReturnLineFeed);

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

            // No modifiers left and the removed 'unsafe' carried leading trivia
            // (typically indentation / newlines) that would otherwise be lost. Transfer
            // it onto the first non-attribute token of the declaration so indentation,
            // doc comments, etc. survive the modifier removal.
            if (index == 0 && newModifiers.Count == 0 && unsafeToken.LeadingTrivia.Count > 0)
            {
                updated = PrependLeadingTriviaToFirstNonAttributeToken(updated, unsafeToken.LeadingTrivia);
            }

            return updated;
        }

        // Pushes leading trivia from `unsafeToken` onto the first non-attribute token of
        // the declaration so indentation / doc-comments don't get lost when the modifier
        // was the very first token.
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

        // Local-namespace alias for the analyzer's GetModifiers helper.
        private static SyntaxTokenList GetModifiers(SyntaxNode node) => UnsafeV2MigrationAnalyzer.GetModifiers(node);

        // Inverse of GetModifiers — rebuilds a declaration node with new modifiers. Covers
        // every type / member kind the analyzer reports on.
        private static SyntaxNode WithModifiers(SyntaxNode node, SyntaxTokenList modifiers) => node switch
        {
            ClassDeclarationSyntax c => c.WithModifiers(modifiers),
            StructDeclarationSyntax s => s.WithModifiers(modifiers),
            InterfaceDeclarationSyntax i => i.WithModifiers(modifiers),
            RecordDeclarationSyntax r => r.WithModifiers(modifiers),
            DelegateDeclarationSyntax d => d.WithModifiers(modifiers),
            MethodDeclarationSyntax m => m.WithModifiers(modifiers),
            ConstructorDeclarationSyntax ctor => ctor.WithModifiers(modifiers),
            DestructorDeclarationSyntax dt => dt.WithModifiers(modifiers),
            OperatorDeclarationSyntax op => op.WithModifiers(modifiers),
            ConversionOperatorDeclarationSyntax co => co.WithModifiers(modifiers),
            PropertyDeclarationSyntax p => p.WithModifiers(modifiers),
            IndexerDeclarationSyntax idx => idx.WithModifiers(modifiers),
            EventDeclarationSyntax e => e.WithModifiers(modifiers),
            EventFieldDeclarationSyntax ef => ef.WithModifiers(modifiers),
            LocalFunctionStatementSyntax lf => lf.WithModifiers(modifiers),
            _ => node,
        };

        // True iff `type` is the `void` keyword (used to choose return vs. expression-statement
        // when converting an expression body to a block).
        private static bool IsVoid(TypeSyntax? type) =>
            type is PredefinedTypeSyntax pts && pts.Keyword.IsKind(SyntaxKind.VoidKeyword);
    }
}
#endif
