// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix.UnsafeContext
{
    internal enum UnsafeContextForm
    {
        Block,
        Expression,
    }

    /// <summary>
    /// One way of introducing an unsafe context: an immutable plan over the original tree. All edits of a validation
    /// round are applied together through one <see cref="SyntaxEditor"/>.
    /// </summary>
    internal abstract class UnsafeContextEdit
    {
        /// <summary>The original span this edit rewrites, used to attribute validation failures.</summary>
        public abstract TextSpan Span { get; }

        public abstract UnsafeContextForm Form { get; }

        /// <summary>Whether the context satisfies the coverage rule; only blocks can cover too much.</summary>
        public virtual bool IsProportionate => true;

        /// <summary>The leaves covered without needing the context, used to rank disproportionate blocks.</summary>
        public virtual int UnrelatedLeaves => 0;

        /// <summary>Applies the edit, annotating the nodes it creates with <paramref name="annotation"/>.</summary>
        public abstract void Apply(EditSession session, SyntaxAnnotation annotation);
    }

    /// <summary>State shared by the edits applied in one validation round.</summary>
    internal sealed record EditSession(SyntaxEditor Editor, UnsafeContextFormatting Formatting, CSharpParseOptions ParseOptions);

    /// <summary>
    /// Wraps an expression in <c>/* SAFETY: To be audited */ unsafe(...)</c>, merging the placeholder wrappers inside
    /// it. Failures are attributed to the whole <paramref name="clause"/> (e.g. the statement) the wrapper belongs to.
    /// </summary>
    internal sealed class ExpressionWrapEdit(ExpressionSyntax expression, SyntaxNode clause) : UnsafeContextEdit
    {
        public override TextSpan Span => clause.Span;

        public override UnsafeContextForm Form => UnsafeContextForm.Expression;

        public override void Apply(EditSession session, SyntaxAnnotation annotation)
        {
            // A placeholder marker on the token before the expression is outside it and stays, so it is not repeated.
            bool withMarker = !PlaceholderContexts.HasMarkerOnPreviousToken(expression);
            ExpressionSyntax merged = PlaceholderContexts.Unwrap(expression);
            session.Editor.ReplaceNode(expression, (current, _) =>
                UnsafeContextFacts.CreateUnsafeExpression(merged == expression ? (ExpressionSyntax)current : merged, session.ParseOptions, withMarker)
                    .WithAdditionalAnnotations(annotation));
            session.Editor.ReplaceNode(clause, (current, _) => current.WithAdditionalAnnotations(annotation));
        }
    }

    /// <summary>
    /// Converts an expression body (<c>=&gt; expr</c>) into a block body holding one unsafe block. Used when no wrapper
    /// is valid, typically because the body must be a statement expression.
    /// </summary>
    internal sealed class ExpressionBodyBlockEdit(SyntaxNode owner, ExpressionSyntax body, bool asStatement, string indentation) : UnsafeContextEdit
    {
        public override TextSpan Span => body.Span;

        public override UnsafeContextForm Form => UnsafeContextForm.Block;

        public static bool CanConvert(SyntaxNode owner) =>
            owner is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or AccessorDeclarationSyntax
                or LambdaExpressionSyntax or PropertyDeclarationSyntax or IndexerDeclarationSyntax;

        public override void Apply(EditSession session, SyntaxAnnotation annotation) =>
            session.Editor.ReplaceNode(owner, (current, _) => Convert(current, session.Formatting).WithAdditionalAnnotations(annotation));

        // Replaces the arrow clause with the block body; a property or indexer gets a `get` accessor.
        private SyntaxNode Convert(SyntaxNode current, UnsafeContextFormatting formatting)
        {
            // The token before the body (`)` of a parameter list, `=>` of a lambda, ...) now ends its line.
            SyntaxToken arrow = GetExpressionBody(current).GetFirstToken().GetPreviousToken();
            SyntaxToken previous = current is LambdaExpressionSyntax ? arrow : arrow.GetPreviousToken();
            current = current.ReplaceToken(previous, previous.WithTrailingTrivia(formatting.EndOfLine));

            ExpressionSyntax expression = GetExpressionBody(current);
            bool isProperty = current is BasePropertyDeclarationSyntax;
            BlockSyntax CreateBody(SyntaxTriviaList closeBraceTrailingTrivia) =>
                CreateUnsafeBody(formatting, isProperty ? indentation + formatting.IndentUnit : indentation, expression, closeBraceTrailingTrivia);

            SyntaxTriviaList endOfLine = [formatting.EndOfLine];
            return current switch
            {
                LambdaExpressionSyntax lambda => lambda.WithExpressionBody(null).WithBlock(CreateBody(default)),
                BaseMethodDeclarationSyntax method => method.WithExpressionBody(null).WithSemicolonToken(default).WithBody(CreateBody(method.SemicolonToken.TrailingTrivia)),
                LocalFunctionStatementSyntax local => local.WithExpressionBody(null).WithSemicolonToken(default).WithBody(CreateBody(local.SemicolonToken.TrailingTrivia)),
                AccessorDeclarationSyntax accessor => accessor.WithExpressionBody(null).WithSemicolonToken(default).WithBody(CreateBody(accessor.SemicolonToken.TrailingTrivia)),
                PropertyDeclarationSyntax property => property.WithExpressionBody(null).WithSemicolonToken(default)
                    .WithAccessorList(CreateGetter(formatting, CreateBody(endOfLine), property.SemicolonToken.TrailingTrivia)),
                IndexerDeclarationSyntax indexer => indexer.WithExpressionBody(null).WithSemicolonToken(default)
                    .WithAccessorList(CreateGetter(formatting, CreateBody(endOfLine), indexer.SemicolonToken.TrailingTrivia)),
                _ => current,
            };
        }

        // Builds `{ // SAFETY ... unsafe { return expr; } }`, keeping comments that preceded the expression.
        private BlockSyntax CreateUnsafeBody(UnsafeContextFormatting formatting, string blockIndentation, ExpressionSyntax expression, SyntaxTriviaList closeBraceTrailingTrivia)
        {
            string middle = blockIndentation + formatting.IndentUnit;
            string inner = middle + formatting.IndentUnit;
            ExpressionSyntax value = expression.WithoutLeadingTrivia().WithoutTrailingTrivia();
            StatementSyntax statement = asStatement
                ? SyntaxFactory.ExpressionStatement(value)
                : SyntaxFactory.ReturnStatement(SyntaxFactory.Token(SyntaxKind.ReturnKeyword).WithTrailingTrivia(SyntaxFactory.Space), value, SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            // Comments between `=>` and the expression stay above it.
            SyntaxTriviaList comments = [];
            foreach (SyntaxTrivia comment in expression.GetLeadingTrivia().Where(static t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia)))
                comments = [.. comments, .. UnsafeContextFormatting.Whitespace(inner), comment, formatting.EndOfLine];

            statement = formatting.Indent(
                statement.WithLeadingTrivia(comments.AddRange(UnsafeContextFormatting.Whitespace(inner))).WithTrailingTrivia(formatting.EndOfLine),
                formatting.IndentUnit,
                includeFirstLine: false);

            return BlockSyntaxFactory.Create(formatting, blockIndentation, [BlockSyntaxFactory.CreateUnsafe(formatting, middle, [statement])], closeBraceTrailingTrivia);
        }

        private AccessorListSyntax CreateGetter(UnsafeContextFormatting formatting, BlockSyntax body, SyntaxTriviaList closeBraceTrailingTrivia)
        {
            SyntaxTriviaList outer = UnsafeContextFormatting.Whitespace(indentation);
            AccessorDeclarationSyntax getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithKeyword(SyntaxFactory.Token(UnsafeContextFormatting.Whitespace(indentation + formatting.IndentUnit), SyntaxKind.GetKeyword, [formatting.EndOfLine]))
                .WithBody(body);
            return SyntaxFactory.AccessorList(
                SyntaxFactory.Token(outer, SyntaxKind.OpenBraceToken, [formatting.EndOfLine]),
                [getter],
                SyntaxFactory.Token(outer, SyntaxKind.CloseBraceToken, closeBraceTrailingTrivia));
        }

        private static ExpressionSyntax GetExpressionBody(SyntaxNode owner) =>
            owner switch
            {
                LambdaExpressionSyntax lambda => lambda.ExpressionBody!,
                BaseMethodDeclarationSyntax method => method.ExpressionBody!.Expression,
                LocalFunctionStatementSyntax localFunction => localFunction.ExpressionBody!.Expression,
                AccessorDeclarationSyntax accessor => accessor.ExpressionBody!.Expression,
                BasePropertyDeclarationSyntax property => GetArrowClause(property)!.Expression,
                _ => throw new InvalidOperationException(),
            };

        private static ArrowExpressionClauseSyntax? GetArrowClause(BasePropertyDeclarationSyntax property) =>
            property switch
            {
                PropertyDeclarationSyntax p => p.ExpressionBody,
                IndexerDeclarationSyntax i => i.ExpressionBody,
                _ => null,
            };
    }

    /// <summary>Creates blocks in the multi-line layout used for every generated context.</summary>
    internal static class BlockSyntaxFactory
    {
        public static BlockSyntax Create(UnsafeContextFormatting formatting, string indentation, IEnumerable<StatementSyntax> statements, SyntaxTriviaList closeBraceTrailingTrivia)
        {
            SyntaxTriviaList indent = UnsafeContextFormatting.Whitespace(indentation);
            return SyntaxFactory.Block(
                SyntaxFactory.Token(indent, SyntaxKind.OpenBraceToken, [formatting.EndOfLine]),
                [.. statements],
                SyntaxFactory.Token(indent, SyntaxKind.CloseBraceToken, closeBraceTrailingTrivia));
        }

        /// <summary>Creates <c>// SAFETY: To be audited</c> + <c>unsafe { statements }</c> at <paramref name="indentation"/>.</summary>
        public static UnsafeStatementSyntax CreateUnsafe(UnsafeContextFormatting formatting, string indentation, IEnumerable<StatementSyntax> statements, SyntaxTriviaList extraLeadingLines = default) =>
            SyntaxFactory.UnsafeStatement(
                SyntaxFactory.Token([.. extraLeadingLines, .. formatting.BlockMarkerLine(indentation)], SyntaxKind.UnsafeKeyword, [formatting.EndOfLine]),
                Create(formatting, indentation, statements, [formatting.EndOfLine]));
    }
}
