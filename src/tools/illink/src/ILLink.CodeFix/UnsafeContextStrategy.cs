// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Builds the syntax that introduces an <c>unsafe</c> context (block or <c>unsafe(...)</c> expression) for a
    /// single operation flagged by IL5006, choosing the minimal-but-safe form.
    /// </summary>
    internal static class UnsafeContextStrategy
    {
        internal const string BlockComment = "// SAFETY: Audit";
        internal const string ExpressionComment = "/* SAFETY: Audit */";

        private static readonly CSharpParseOptions s_previewOptions = new CSharpParseOptions(LanguageVersion.Preview)
            .WithFeatures([new KeyValuePair<string, string>("updated-memory-safety-rules", "")]);

        /// <summary>Applies the fix for a single flagged operation, returning the rewritten root, or null if unfixable.</summary>
        internal static SyntaxNode? Fix(SyntaxNode root, SemanticModel semanticModel, TextSpan span)
        {
            var operation = GetOperationExpression(root.FindNode(span, getInnermostNodeForTie: true));
            if (operation is null)
                return null;

            var statement = operation.FirstAncestorOrSelf<StatementSyntax>();

            if (UseExpressionForm(operation, statement, semanticModel))
            {
                // A void operation can't be wrapped as an 'unsafe(...)' expression. If it is the body of an
                // expression-bodied member, convert that member to a block body with an 'unsafe' block instead.
                if (ReturnsVoid(operation, semanticModel))
                    return TryConvertExpressionBodyToBlock(root, operation);

                return WrapExpression(root, operation, semanticModel);
            }

            // A block is only usable if we have a statement to wrap.
            if (statement is null)
                return WrapExpression(root, operation, semanticModel);

            return WrapStatement(root, statement);
        }

        /// <summary>Finds the smallest expression that fully represents the unsafe operation (whole call, deref, etc.).</summary>
        private static ExpressionSyntax? GetOperationExpression(SyntaxNode node)
        {
            if (node.AncestorsAndSelf().OfType<ExpressionSyntax>().FirstOrDefault() is not { } expr)
                return null;

            while (true)
            {
                switch (expr.Parent)
                {
                    case MemberAccessExpressionSyntax member when member.Name == expr:
                        expr = member; continue;
                    case MemberBindingExpressionSyntax binding when binding.Name == expr:
                        expr = binding; continue;
                    case InvocationExpressionSyntax invocation when invocation.Expression == expr:
                        expr = invocation; continue;
                    case ElementAccessExpressionSyntax element when element.Expression == expr:
                        expr = element; continue;
                    case PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.PointerIndirectionExpression } deref when deref.Operand == expr:
                        expr = deref; continue;
                    default:
                        return expr;
                }
            }
        }

        private static bool UseExpressionForm(ExpressionSyntax operation, StatementSyntax? statement, SemanticModel semanticModel)
        {
            // No statement to wrap (field/property/constructor initializer).
            if (statement is null)
                return true;

            // Structural positions where an unsafe block cannot appear.
            foreach (var ancestor in operation.AncestorsAndSelf())
            {
                if (ancestor == statement)
                    break;
                switch (ancestor)
                {
                    case AwaitExpressionSyntax:
                    case CatchFilterClauseSyntax:
                    case ConstructorInitializerSyntax:
                    case LambdaExpressionSyntax:
                    case AnonymousMethodExpressionSyntax:
                    case QueryExpressionSyntax:
                        return true;
                }
            }

            // Wrapping the statement in a block would be illegal or would shorten a scope.
            if (statement.DescendantNodes().Any(static n => n is AwaitExpressionSyntax or YieldStatementSyntax))
                return true;
            if (statement is LocalDeclarationStatementSyntax { UsingKeyword.RawKind: not (int)SyntaxKind.None })
                return true;
            if (statement is LocalDeclarationStatementSyntax { Declaration.Type: RefTypeSyntax or ScopedTypeSyntax })
                return true;
            if (statement.GetLeadingTrivia().Any(static t => t.IsDirective))
                return true;

            // Declaration expressions ('out var x') and pattern variables ('is T y') leak into the enclosing
            // scope; moving the statement into a block would hide them from later references. The expression
            // form keeps them in scope. Don't descend into nested functions, whose declarations don't leak.
            if (statement.DescendantNodes(static n => n is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax))
                    .Any(static n => n is DeclarationExpressionSyntax or SingleVariableDesignationSyntax))
                return true;

            // The operation is a value embedded inside the statement: an expression keeps the scope minimal.
            // If it sits at the very start of the statement, an 'unsafe(...)' expression cannot be used there
            // (the parser reads 'unsafe' as a block), so fall through to the block form.
            return operation.SpanStart != statement.SpanStart && !ReturnsVoid(operation, semanticModel);
        }

        private static bool ReturnsVoid(ExpressionSyntax operation, SemanticModel semanticModel) =>
            semanticModel.GetTypeInfo(operation).Type is { SpecialType: SpecialType.System_Void };

        private static SyntaxNode WrapExpression(SyntaxNode root, ExpressionSyntax operation, SemanticModel semanticModel)
        {
            if (ReturnsVoid(operation, semanticModel))
                return root; // Cannot wrap a void value in an unsafe expression.

            return MakeUnsafeExpression(operation) is { } wrapped ? root.ReplaceNode(operation, wrapped) : root;
        }

        private static SyntaxNode WrapStatement(SyntaxNode root, StatementSyntax statement)
        {
            var wrapped = MakeUnsafeBlock([statement.WithoutLeadingTrivia().WithoutTrailingTrivia()])
                .WithLeadingTrivia(statement.GetLeadingTrivia())
                .WithTrailingTrivia(statement.GetTrailingTrivia());
            return root.ReplaceNode(statement, wrapped);
        }

        // Converts a void expression-bodied member ('void M() => VoidCall();') to a block body wrapping the call
        // in an 'unsafe' block. Returns the original root unchanged if the shape isn't a supported member body.
        private static SyntaxNode TryConvertExpressionBodyToBlock(SyntaxNode root, ExpressionSyntax operation)
        {
            if (operation.FirstAncestorOrSelf<ArrowExpressionClauseSyntax>() is not { Parent: { } member } arrow)
                return root;

            var block = SyntaxFactory.Block(MakeUnsafeBlock([SyntaxFactory.ExpressionStatement(arrow.Expression.WithoutTrivia())]));

            SyntaxNode? converted = member switch
            {
                MethodDeclarationSyntax m => m.WithExpressionBody(null).WithSemicolonToken(default).WithBody(block),
                LocalFunctionStatementSyntax lf => lf.WithExpressionBody(null).WithSemicolonToken(default).WithBody(block),
                AccessorDeclarationSyntax a => a.WithExpressionBody(null).WithSemicolonToken(default).WithBody(block),
                OperatorDeclarationSyntax op => op.WithExpressionBody(null).WithSemicolonToken(default).WithBody(block),
                DestructorDeclarationSyntax d => d.WithExpressionBody(null).WithSemicolonToken(default).WithBody(block),
                _ => null,
            };

            return converted is null ? root : root.ReplaceNode(member, converted.WithAdditionalAnnotations(Formatter.Annotation));
        }

        private static ExpressionSyntax? MakeUnsafeExpression(ExpressionSyntax inner)
        {
            var text = inner.WithoutLeadingTrivia().WithoutTrailingTrivia().ToFullString();
            var wrapped = SyntaxFactory.ParseExpression($"unsafe({text})", options: s_previewOptions);

            // Bail out rather than corrupt the source if the 'unsafe(...)' expression did not round-trip.
            if (wrapped.ContainsDiagnostics)
                return null;

            // No Formatter.Annotation: the expression is replaced in place and formatting the new 'unsafe'
            // keyword would insert an unwanted space before '(' ("unsafe (x)").
            return wrapped
                .WithLeadingTrivia(inner.GetLeadingTrivia().Add(SyntaxFactory.Comment(ExpressionComment)).Add(SyntaxFactory.Space))
                .WithTrailingTrivia(inner.GetTrailingTrivia());
        }

        private static UnsafeStatementSyntax MakeUnsafeBlock(IEnumerable<StatementSyntax> statements)
        {
            var inner = statements.Select(static s => s.WithoutLeadingTrivia()).ToList();
            inner[0] = inner[0].WithLeadingTrivia(SyntaxFactory.Comment(BlockComment), SyntaxFactory.ElasticCarriageReturnLineFeed);
            return SyntaxFactory.UnsafeStatement(SyntaxFactory.Block(inner)).WithAdditionalAnnotations(Formatter.Annotation);
        }
    }
}
#endif
