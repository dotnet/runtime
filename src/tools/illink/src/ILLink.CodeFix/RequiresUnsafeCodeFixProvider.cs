// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Generic;
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

namespace ILLink.CodeFix
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RequiresUnsafeCodeFixProvider)), Shared]
    public sealed class RequiresUnsafeCodeFixProvider : BaseAttributeCodeFixProvider
    {
        private const string WrapInUnsafeBlockTitle = "Wrap in unsafe block";

        public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.RequiresUnsafe));

        public sealed override ImmutableArray<string> FixableDiagnosticIds => SupportedDiagnostics.Select(dd => dd.Id).ToImmutableArray();

        private protected override LocalizableString CodeFixTitle => new LocalizableResourceString(nameof(Resources.RequiresUnsafeCodeFixTitle), Resources.ResourceManager, typeof(Resources));

        private protected override string FullyQualifiedAttributeName => RequiresUnsafeAnalyzer.FullyQualifiedRequiresUnsafeAttribute;

        private protected override AttributeableParentTargets AttributableParentTargets => AttributeableParentTargets.MethodOrConstructor;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Register the base code fix (add RequiresUnsafe attribute)
            await BaseRegisterCodeFixesAsync(context).ConfigureAwait(false);

            // Register the "wrap in unsafe block" code fix
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();

            if (await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root)
                return;

            SyntaxNode targetNode = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            // Find the statement containing the unsafe call
            var containingStatement = targetNode.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();

            // Check if this is a local function with expression body - treat it like expression-bodied member
            if (containingStatement is LocalFunctionStatementSyntax localFunc && localFunc.ExpressionBody != null)
            {
                if (!HasDirectiveTrivia(localFunc.ExpressionBody))
                {
                    context.RegisterCodeFix(CodeAction.Create(
                        title: WrapInUnsafeBlockTitle,
                        createChangedDocument: ct => ConvertExpressionBodyToUnsafeBlockAsync(document, localFunc.ExpressionBody, ct),
                        equivalenceKey: WrapInUnsafeBlockTitle), diagnostic);
                }
                return;
            }

            if (containingStatement is null || containingStatement is BlockSyntax)
            {
                // Try expression-bodied member
                var arrowExpr = targetNode.AncestorsAndSelf().OfType<ArrowExpressionClauseSyntax>().FirstOrDefault();
                if (arrowExpr != null && !HasDirectiveTrivia(arrowExpr))
                {
                    context.RegisterCodeFix(CodeAction.Create(
                        title: WrapInUnsafeBlockTitle,
                        createChangedDocument: ct => ConvertExpressionBodyToUnsafeBlockAsync(document, arrowExpr, ct),
                        equivalenceKey: WrapInUnsafeBlockTitle), diagnostic);
                }
                return;
            }

            // Find the parent block containing this statement
            var parentBlock = containingStatement.Parent as BlockSyntax;
            if (parentBlock != null)
            {
                context.RegisterCodeFix(CodeAction.Create(
                    title: WrapInUnsafeBlockTitle,
                    createChangedDocument: ct => WrapStatementsInUnsafeBlockAsync(document, parentBlock, containingStatement, ct),
                    equivalenceKey: WrapInUnsafeBlockTitle), diagnostic);
                return;
            }

            // Handle switch case sections
            var switchSection = containingStatement.Parent as SwitchSectionSyntax;
            if (switchSection != null)
            {
                context.RegisterCodeFix(CodeAction.Create(
                    title: WrapInUnsafeBlockTitle,
                    createChangedDocument: ct => WrapSwitchSectionStatementInUnsafeBlockAsync(document, switchSection, containingStatement, ct),
                    equivalenceKey: WrapInUnsafeBlockTitle), diagnostic);
                return;
            }

            // Handle embedded statements (if/else/while/for without braces)
            if (IsEmbeddedStatement(containingStatement))
            {
                context.RegisterCodeFix(CodeAction.Create(
                    title: WrapInUnsafeBlockTitle,
                    createChangedDocument: ct => WrapEmbeddedStatementInUnsafeBlockAsync(document, containingStatement, ct),
                    equivalenceKey: WrapInUnsafeBlockTitle), diagnostic);
                return;
            }
        }

        private static bool IsEmbeddedStatement(StatementSyntax statement)
        {
            // An embedded statement is a statement that is the direct child of a control flow statement
            // without being wrapped in a block (e.g., "if (x) return;" instead of "if (x) { return; }")
            return statement.Parent is IfStatementSyntax
                || statement.Parent is ElseClauseSyntax
                || statement.Parent is WhileStatementSyntax
                || statement.Parent is ForStatementSyntax
                || statement.Parent is ForEachStatementSyntax
                || statement.Parent is DoStatementSyntax
                || statement.Parent is UsingStatementSyntax
                || statement.Parent is LockStatementSyntax
                || statement.Parent is FixedStatementSyntax;
        }

        private static async Task<Document> WrapStatementsInUnsafeBlockAsync(
            Document document,
            BlockSyntax parentBlock,
            StatementSyntax triggerStatement,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
                return document;

            // Find the trigger statement in the block
            var statements = parentBlock.Statements;
            int triggerIndex = statements.IndexOf(triggerStatement);
            if (triggerIndex < 0)
                return document;

            // Check if any statement has directive trivia that would be lost or mangled
            var leadingTrivia = triggerStatement.GetLeadingTrivia();
            if (leadingTrivia.Any(t => t.IsDirective))
            {
                // Skip the fix - directives in statement trivia would be lost or malformed
                return document;
            }

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // Create the TODO comment
            var todoComment = SyntaxFactory.Comment("// TODO(unsafe): Baselining unsafe usage");
            var newLine = SyntaxFactory.ElasticCarriageReturnLineFeed;

            // Check if we can use forward declaration strategy
            // This applies when the trigger is a local declaration with a single variable
            // Ref locals (including ref readonly and scoped ref) cannot be forward-declared
            LocalDeclarationStatementSyntax? forwardDecl = null;
            StatementSyntax statementToWrap = triggerStatement;
            int endIndex = triggerIndex;  // For block expansion when forward decl not possible

            bool isRefOrScopedLocal = triggerStatement is LocalDeclarationStatementSyntax localDeclCheck &&
                (localDeclCheck.Declaration.Type is RefTypeSyntax || localDeclCheck.Declaration.Type is ScopedTypeSyntax);

            if (triggerStatement is LocalDeclarationStatementSyntax localDecl &&
                !localDecl.IsConst &&
                !isRefOrScopedLocal &&
                localDecl.Declaration.Variables.Count == 1 &&
                localDecl.Declaration.Variables[0].Initializer != null)
            {
                var variable = localDecl.Declaration.Variables[0];
                TypeSyntax? typeSyntax = localDecl.Declaration.Type;

                // If using 'var', resolve to explicit type
                if (typeSyntax.IsVar)
                {
                    var typeInfo = semanticModel.GetTypeInfo(typeSyntax, cancellationToken);
                    if (typeInfo.Type is not null and not IErrorTypeSymbol)
                    {
                        typeSyntax = SyntaxFactory.ParseTypeName(typeInfo.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat))
                            .WithTrailingTrivia(SyntaxFactory.Space);
                    }
                    else
                    {
                        // Can't resolve type, fall back to wrapping the whole declaration
                        typeSyntax = null;
                    }
                }

                if (typeSyntax != null)
                {
                    // Create forward declaration: Type varName;
                    var forwardDeclVariable = SyntaxFactory.VariableDeclarator(variable.Identifier);
                    var forwardDeclDeclaration = SyntaxFactory.VariableDeclaration(typeSyntax)
                        .AddVariables(forwardDeclVariable);
                    forwardDecl = SyntaxFactory.LocalDeclarationStatement(forwardDeclDeclaration)
                        .WithLeadingTrivia(triggerStatement.GetLeadingTrivia())
                        .WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed));

                    // Create assignment: varName = initializer;
                    var assignment = SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(variable.Identifier),
                        variable.Initializer!.Value);
                    statementToWrap = SyntaxFactory.ExpressionStatement(assignment)
                        .WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed));
                }
            }
            else if (isRefOrScopedLocal)
            {
                // For ref/scoped locals, we must expand the block until no variables
                // declared inside are used outside. This handles chains of dependencies
                // where ref locals lead to regular locals that are also used later.
                var localDeclStmt = (LocalDeclarationStatementSyntax)triggerStatement;
                if (localDeclStmt.Declaration.Variables.Count == 1)
                {
                    // Iteratively expand until no variables declared inside escape outside
                    bool expanded = true;
                    while (expanded && endIndex < statements.Count - 1)
                    {
                        expanded = false;

                        // Collect all variables declared in the current range (using symbols for correct scoping)
                        var declaredVariableSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
                        for (int i = triggerIndex; i <= endIndex; i++)
                        {
                            if (statements[i] is LocalDeclarationStatementSyntax rangeLocalDecl)
                            {
                                foreach (var variable in rangeLocalDecl.Declaration.Variables)
                                {
                                    var symbol = semanticModel.GetDeclaredSymbol(variable, cancellationToken);
                                    if (symbol is not null)
                                        declaredVariableSymbols.Add(symbol);
                                }
                            }
                        }

                        // Check ALL remaining statements - does any use a declared variable?
                        for (int nextIndex = endIndex + 1; nextIndex < statements.Count; nextIndex++)
                        {
                            var stmt = statements[nextIndex];

                            bool usesAnyDeclaredVariable = stmt.DescendantNodes()
                                .OfType<IdentifierNameSyntax>()
                                .Any(id =>
                                {
                                    var symbolInfo = semanticModel.GetSymbolInfo(id, cancellationToken);
                                    return symbolInfo.Symbol is not null && declaredVariableSymbols.Contains(symbolInfo.Symbol);
                                });

                            if (usesAnyDeclaredVariable)
                            {
                                // Check for directive trivia that would be mangled
                                if (stmt.GetLeadingTrivia().Any(t => t.IsDirective))
                                {
                                    // Stop expansion here - don't include statements with directives
                                    break;
                                }

                                // Expand to include this statement
                                endIndex = nextIndex;
                                expanded = true;
                                break; // Restart the outer loop to recollect variables
                            }
                        }
                    }
                }
            }

            // Build the statements to wrap
            List<StatementSyntax> statementsToWrap;
            if (forwardDecl != null)
            {
                statementsToWrap = new List<StatementSyntax> { statementToWrap };
            }
            else
            {
                statementsToWrap = statements.Skip(triggerIndex).Take(endIndex - triggerIndex + 1).ToList();
            }

            // Create the unsafe block
            var wrappedStatements = statementsToWrap.Select(s =>
                s.WithoutTrivia()
                 .WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed)));
            var unsafeBlock = SyntaxFactory.UnsafeStatement(
                SyntaxFactory.Block(wrappedStatements))
                .WithLeadingTrivia(forwardDecl != null
                    ? SyntaxFactory.TriviaList(todoComment, newLine)
                    : triggerStatement.GetLeadingTrivia().InsertRange(0, new[] { todoComment, newLine }))
                .WithTrailingTrivia(forwardDecl != null
                    ? triggerStatement.GetTrailingTrivia()
                    : statementsToWrap.Last().GetTrailingTrivia());

            // Build the new list of statements
            var newStatements = new List<StatementSyntax>();
            for (int i = 0; i < statements.Count; i++)
            {
                if (i == triggerIndex)
                {
                    if (forwardDecl != null)
                    {
                        newStatements.Add(forwardDecl);
                    }
                    newStatements.Add(unsafeBlock);
                }
                else if (i > triggerIndex && i <= endIndex)
                {
                    // Skip - already included in unsafe block
                }
                else
                {
                    newStatements.Add(statements[i]);
                }
            }

            var newBlock = parentBlock.WithStatements(SyntaxFactory.List(newStatements));
            editor.ReplaceNode(parentBlock, newBlock);

            return editor.GetChangedDocument();
        }

        private static async Task<Document> WrapSwitchSectionStatementInUnsafeBlockAsync(
            Document document,
            SwitchSectionSyntax _,
            StatementSyntax triggerStatement,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // Create the TODO comment
            var todoComment = SyntaxFactory.Comment("// TODO(unsafe): Baselining unsafe usage");
            var newLine = SyntaxFactory.CarriageReturnLineFeed;

            // Create the unsafe block wrapping just the trigger statement
            var unsafeBlock = SyntaxFactory.UnsafeStatement(
                SyntaxFactory.Block(triggerStatement.WithoutTrivia()))
                .WithLeadingTrivia(triggerStatement.GetLeadingTrivia().InsertRange(0, new[] { todoComment, newLine }))
                .WithTrailingTrivia(triggerStatement.GetTrailingTrivia());

            // Replace the trigger statement with the unsafe block
            editor.ReplaceNode(triggerStatement, unsafeBlock);

            return editor.GetChangedDocument();
        }

        private static async Task<Document> WrapEmbeddedStatementInUnsafeBlockAsync(
            Document document,
            StatementSyntax embeddedStatement,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // Create the TODO comment
            var todoComment = SyntaxFactory.Comment("// TODO(unsafe): Baselining unsafe usage");
            var newLine = SyntaxFactory.CarriageReturnLineFeed;

            // Create the unsafe block wrapping the statement
            var unsafeBlock = SyntaxFactory.UnsafeStatement(
                SyntaxFactory.Block(embeddedStatement.WithoutTrivia()))
                .WithLeadingTrivia(todoComment, newLine);

            // Wrap in a block to replace the embedded statement
            var wrappingBlock = SyntaxFactory.Block(unsafeBlock)
                .WithLeadingTrivia(embeddedStatement.GetLeadingTrivia())
                .WithTrailingTrivia(embeddedStatement.GetTrailingTrivia());

            editor.ReplaceNode(embeddedStatement, wrappingBlock);

            return editor.GetChangedDocument();
        }

        private static async Task<Document> ConvertExpressionBodyToUnsafeBlockAsync(
            Document document,
            ArrowExpressionClauseSyntax arrowExpr,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // Create the TODO comment
            var todoComment = SyntaxFactory.Comment("// TODO(unsafe): Baselining unsafe usage");
            var newLine = SyntaxFactory.CarriageReturnLineFeed;

            // Get the parent member to determine the return type
            var parent = arrowExpr.Parent;
            bool isVoid = false;

            if (parent is MethodDeclarationSyntax method)
            {
                isVoid = method.ReturnType is PredefinedTypeSyntax pts && pts.Keyword.IsKind(SyntaxKind.VoidKeyword);
            }
            else if (parent is LocalFunctionStatementSyntax localFunc)
            {
                isVoid = localFunc.ReturnType is PredefinedTypeSyntax pts && pts.Keyword.IsKind(SyntaxKind.VoidKeyword);
            }
            else if (parent is DestructorDeclarationSyntax)
            {
                isVoid = true;
            }
            else if (parent is AccessorDeclarationSyntax or PropertyDeclarationSyntax or IndexerDeclarationSyntax)
            {
                isVoid = false;
            }

            // Create the statement that goes inside the unsafe block
            StatementSyntax innerStatement = isVoid
                ? SyntaxFactory.ExpressionStatement(arrowExpr.Expression.WithoutTrivia())
                : SyntaxFactory.ReturnStatement(arrowExpr.Expression.WithoutTrivia());

            // Create the unsafe block
            var unsafeBlock = SyntaxFactory.UnsafeStatement(
                SyntaxFactory.Block(innerStatement))
                .WithLeadingTrivia(todoComment, newLine);

            // Create the block body
            var blockBody = SyntaxFactory.Block(unsafeBlock);

            // Replace based on parent type
            switch (parent)
            {
                case MethodDeclarationSyntax methodDecl:
                    var newMethod = methodDecl
                        .WithExpressionBody(null)
                        .WithSemicolonToken(default)
                        .WithBody(blockBody);
                    editor.ReplaceNode(methodDecl, newMethod);
                    break;

                case LocalFunctionStatementSyntax localFunc:
                    var newLocalFunc = localFunc
                        .WithExpressionBody(null)
                        .WithSemicolonToken(default)
                        .WithBody(blockBody);
                    editor.ReplaceNode(localFunc, newLocalFunc);
                    break;

                case DestructorDeclarationSyntax destructorDecl:
                    var newDestructor = destructorDecl
                        .WithExpressionBody(null)
                        .WithSemicolonToken(default)
                        .WithBody(blockBody);
                    editor.ReplaceNode(destructorDecl, newDestructor);
                    break;

                case PropertyDeclarationSyntax propDecl:
                    var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithBody(SyntaxFactory.Block(unsafeBlock));
                    var accessorList = SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(getter));
                    var newProp = propDecl
                        .WithExpressionBody(null)
                        .WithSemicolonToken(default)
                        .WithAccessorList(accessorList);
                    editor.ReplaceNode(propDecl, newProp);
                    break;

                case AccessorDeclarationSyntax accessor:
                    var newAccessor = accessor
                        .WithExpressionBody(null)
                        .WithSemicolonToken(default)
                        .WithBody(SyntaxFactory.Block(unsafeBlock));
                    editor.ReplaceNode(accessor, newAccessor);
                    break;

                case IndexerDeclarationSyntax indexerDecl:
                    var indexerGetter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithBody(SyntaxFactory.Block(unsafeBlock));
                    var indexerAccessorList = SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(indexerGetter));
                    var newIndexer = indexerDecl
                        .WithExpressionBody(null)
                        .WithSemicolonToken(default)
                        .WithAccessorList(indexerAccessorList);
                    editor.ReplaceNode(indexerDecl, newIndexer);
                    break;
            }

            return editor.GetChangedDocument();
        }

        protected override SyntaxNode[] GetAttributeArguments(ISymbol? attributableSymbol, ISymbol targetSymbol, SyntaxGenerator syntaxGenerator, Diagnostic diagnostic) =>
            RequiresHelpers.GetAttributeArgumentsForRequires(targetSymbol, syntaxGenerator, HasPublicAccessibility(attributableSymbol));

        /// <summary>
        /// Checks if the arrow expression clause or its expression has preprocessor directive trivia.
        /// Converting expression bodies with directives to block bodies is error-prone, so we skip the fix.
        /// </summary>
        private static bool HasDirectiveTrivia(ArrowExpressionClauseSyntax arrowExpr)
        {
            // Check the arrow expression clause's leading trivia (e.g., #if or #else before =>)
            if (arrowExpr.GetLeadingTrivia().Any(t => t.IsDirective))
                return true;

            // Check the arrow token's trailing trivia (e.g., #if right after =>)
            if (arrowExpr.ArrowToken.TrailingTrivia.Any(t => t.IsDirective))
                return true;

            // Check the expression's leading trivia (e.g., #if before the expression)
            if (arrowExpr.Expression.GetLeadingTrivia().Any(t => t.IsDirective))
                return true;

            // Check the expression's trailing trivia (e.g., #endif after the expression)
            if (arrowExpr.Expression.GetTrailingTrivia().Any(t => t.IsDirective))
                return true;

            // Check the arrow expression clause's trailing trivia
            if (arrowExpr.GetTrailingTrivia().Any(t => t.IsDirective))
                return true;

            // Check the parent member for directives between the signature and the arrow
            // e.g., method() #if FOO => expr1; #else => expr2; #endif
            if (arrowExpr.Parent is MethodDeclarationSyntax method)
            {
                // Check trivia after the parameter list (where #if might appear)
                if (method.ParameterList.GetTrailingTrivia().Any(t => t.IsDirective))
                    return true;
                // Check constraint clauses if present
                foreach (var constraint in method.ConstraintClauses)
                {
                    if (constraint.GetTrailingTrivia().Any(t => t.IsDirective))
                        return true;
                }
            }
            else if (arrowExpr.Parent is LocalFunctionStatementSyntax localFunc)
            {
                if (localFunc.ParameterList.GetTrailingTrivia().Any(t => t.IsDirective))
                    return true;
            }
            else if (arrowExpr.Parent is PropertyDeclarationSyntax prop)
            {
                if (prop.Identifier.TrailingTrivia.Any(t => t.IsDirective))
                    return true;
            }

            return false;
        }
    }
}
#endif
