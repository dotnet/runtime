// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix.UnsafeContext
{
    /// <summary>
    /// Wraps a contiguous range of a statement list in a new unsafe block. Declarations used after the range are split
    /// out before the block, and a leading label stays outside so that jumps to it remain valid.
    /// </summary>
    internal sealed class StatementBlockEdit(
        StatementList list,
        int first,
        int last,
        ImmutableDictionary<int, DeclarationSplit> splits,
        Coverage coverage,
        bool isProportionate) : UnsafeContextEdit
    {
        public bool HasSplits => !splits.IsEmpty;

        public override bool IsProportionate => isProportionate;

        public override int UnrelatedLeaves => coverage.Unrelated;

        public override TextSpan Span => TextSpan.FromBounds(list[first].SpanStart, list[last].Span.End);

        public override UnsafeContextForm Form => UnsafeContextForm.Block;

        public static StatementSyntax GetFirstWrappedStatement(StatementSyntax statement) =>
            statement is LabeledStatementSyntax labeled ? labeled.Statement : statement;

        public override void Apply(EditSession session, SyntaxAnnotation annotation)
        {
            UnsafeContextFormatting formatting = session.Formatting;
            StatementSyntax firstStatement = list[first];
            string indentation = formatting.GetIndentation(GetFirstWrappedStatement(firstStatement).SpanStart);
            var (outerLines, declarations, body) = BuildContents(formatting, indentation);

            // Comments and directives above the first statement stay above the split declarations and the marker.
            if (declarations.Count > 0)
                declarations[0] = declarations[0].WithLeadingTrivia(outerLines.AddRange(declarations[0].GetLeadingTrivia()));

            StatementSyntax unsafeStatement = BlockSyntaxFactory
                .CreateUnsafe(formatting, indentation, body, declarations.Count == 0 ? outerLines : default)
                .WithAdditionalAnnotations(annotation);

            if (declarations.Count > 0)
                session.Editor.InsertBefore(firstStatement, declarations.ConvertAll(d => d.WithAdditionalAnnotations(annotation)));

            session.Editor.ReplaceNode(firstStatement, firstStatement is LabeledStatementSyntax labeled ? labeled.WithStatement(unsafeStatement) : unsafeStatement);
            foreach (StatementSyntax statement in list.Statements.Skip(first + 1).Take(last - first))
                session.Editor.RemoveNode(statement, SyntaxRemoveOptions.KeepNoTrivia);
        }

        /// <summary>Returns the trivia lines kept above the block, the split declarations and the block's statements.</summary>
        private (SyntaxTriviaList OuterLines, List<StatementSyntax> Declarations, List<StatementSyntax> Body) BuildContents(UnsafeContextFormatting formatting, string indentation)
        {
            string inner = indentation + formatting.IndentUnit;
            StatementSyntax firstWrapped = MergePlaceholderWrappers(GetFirstWrappedStatement(list[first]));
            var (outerLines, firstInline) = UnsafeContextFormatting.SplitLeadingTrivia(firstWrapped.GetLeadingTrivia());

            List<StatementSyntax> declarations = [], body = [];
            for (int i = first; i <= last; i++)
            {
                StatementSyntax statement = i == first ? firstWrapped : MergePlaceholderWrappers(list[i]);
                var (lines, inline) = i == first ? (default, firstInline) : UnsafeContextFormatting.SplitLeadingTrivia(statement.GetLeadingTrivia());
                if (statement is UnsafeStatementSyntax placeholder && PlaceholderContexts.IsExtendableBlock(placeholder))
                {
                    // An extended placeholder block is unwrapped; its comments other than the marker are kept.
                    if (i == first)
                        outerLines = UnsafeContextFormatting.RemoveBlockMarker(outerLines);

                    body.AddRange(Unwrap(placeholder, formatting.IndentLines(UnsafeContextFormatting.RemoveBlockMarker(lines), formatting.IndentUnit)));
                }
                else if (splits.TryGetValue(i, out DeclarationSplit? split))
                {
                    // Comments before a split declaration, above it or on its line, stay with the declaration.
                    declarations.AddRange(split.Declarations.Select((declaration, index) => declaration
                        .WithLeadingTrivia(index == 0 ? lines.AddRange(UnsafeContextFormatting.Whitespace(indentation)).AddRange(inline) : UnsafeContextFormatting.Whitespace(indentation))
                        .WithTrailingTrivia(formatting.EndOfLine)));
                    body.AddRange(split.Remaining.Select(remaining => IndentFirstLine(formatting, remaining, inner, default)));
                }
                else
                {
                    body.Add(i == first
                        ? IndentFirstLine(formatting, statement, inner, firstInline)
                        : formatting.Indent(statement, formatting.IndentUnit, includeFirstLine: true));
                }
            }

            body[^1] = formatting.WithLineBreak(body[^1]);
            return (outerLines, declarations, body);
        }

        // Placeholder wrappers inside a statement the block now covers are merged into the block.
        private static StatementSyntax MergePlaceholderWrappers(StatementSyntax statement) =>
            PlaceholderContexts.IsExtendableBlock(statement) ? statement : PlaceholderContexts.Unwrap(statement);

        private static StatementSyntax IndentFirstLine(UnsafeContextFormatting formatting, StatementSyntax statement, string indentation, SyntaxTriviaList inline) =>
            formatting.Indent(statement.WithLeadingTrivia(UnsafeContextFormatting.Whitespace(indentation).AddRange(inline)), formatting.IndentUnit, includeFirstLine: false);

        // The statements of a placeholder block are already at the new block's indentation.
        private static IEnumerable<StatementSyntax> Unwrap(UnsafeStatementSyntax placeholder, SyntaxTriviaList commentLines) =>
            placeholder.Block.Statements.Select((statement, index) =>
                index == 0 ? statement.WithLeadingTrivia(commentLines.AddRange(statement.GetLeadingTrivia())) : statement);
    }

    /// <summary>
    /// Wraps all statements of a single-line block such as <c>void M() { Write(); }</c>, laying it out on multiple
    /// lines with the standard marker.
    /// </summary>
    internal sealed class SingleLineBlockEdit(BlockSyntax block, Coverage coverage, bool isProportionate) : UnsafeContextEdit
    {
        public override TextSpan Span => block.Span;

        public override UnsafeContextForm Form => UnsafeContextForm.Block;

        public override bool IsProportionate => isProportionate;

        public override int UnrelatedLeaves => coverage.Unrelated;

        /// <summary>Creates the edit when the block and its owner share one line and the block holds no comments.</summary>
        public static SingleLineBlockEdit? TryCreate(BlockSyntax block, UnsafeContextFormatting formatting, Coverage coverage, bool isProportionate)
        {
            if (block.Parent is not { } owner || owner is BlockSyntax or SwitchSectionSyntax)
                return null;

            SyntaxToken firstToken = owner.FirstTokenAfterAttributes;
            bool isOwnLine = formatting.StartsLine(firstToken)
                && block.CloseBraceToken.EndsLine
                && !owner.DescendantTokens().SkipWhile(t => t != firstToken).TakeWhile(t => t != block.CloseBraceToken).Any(static t => t.EndsLine);
            bool hasOnlyCode = block.DescendantTrivia(TextSpan.FromBounds(block.OpenBraceToken.Span.End, block.CloseBraceToken.SpanStart))
                .All(static t => t.IsKind(SyntaxKind.WhitespaceTrivia));
            return isOwnLine && hasOnlyCode ? new SingleLineBlockEdit(block, coverage, isProportionate) : null;
        }

        public override void Apply(EditSession session, SyntaxAnnotation annotation)
        {
            UnsafeContextFormatting formatting = session.Formatting;
            string indentation = formatting.GetIndentation(block.SpanStart);
            string middle = indentation + formatting.IndentUnit;

            var statements = block.Statements.Select(s => s
                .WithLeadingTrivia(UnsafeContextFormatting.Whitespace(middle + formatting.IndentUnit))
                .WithTrailingTrivia(formatting.EndOfLine));
            BlockSyntax expanded = BlockSyntaxFactory
                .Create(formatting, indentation, [BlockSyntaxFactory.CreateUnsafe(formatting, middle, statements)], block.CloseBraceToken.TrailingTrivia)
                .WithAdditionalAnnotations(annotation);
            session.Editor.ReplaceNode(block, expanded);

            // The owner's line now ends before the opening brace.
            session.Editor.ReplaceNode(block.Parent!, (current, _) =>
            {
                SyntaxToken previous = current.GetAnnotatedNodes(annotation).First().GetFirstToken().GetPreviousToken();
                SyntaxTriviaList trailing = [.. previous.TrailingTrivia.Where(static t => !t.IsKind(SyntaxKind.WhitespaceTrivia)), formatting.EndOfLine];
                return current.ReplaceToken(previous, previous.WithTrailingTrivia(trailing));
            });
        }
    }

    /// <summary>
    /// Wraps the brace-less body of an <c>if</c>, <c>else</c>, loop, <c>using</c>, <c>lock</c> or <c>fixed</c> in
    /// braces holding a new unsafe block, rather than emitting <c>if (c) unsafe { ... }</c>.
    /// </summary>
    /// <param name="contentIndentation">The statement's indentation when it starts its own line; otherwise it follows its header.</param>
    internal sealed class EmbeddedBlockEdit(StatementSyntax statement, string outer, string? contentIndentation, Coverage coverage, bool isProportionate) : UnsafeContextEdit
    {
        public override TextSpan Span => statement.Span;

        public override UnsafeContextForm Form => UnsafeContextForm.Block;

        public override bool IsProportionate => isProportionate;

        public override int UnrelatedLeaves => coverage.Unrelated;

        /// <summary>Creates the edit when the statement starts its own line or is a one-liner after its header.</summary>
        public static EmbeddedBlockEdit? TryCreate(StatementSyntax statement, UnsafeContextFormatting formatting, Coverage coverage, bool isProportionate)
        {
            string outer = formatting.GetIndentation(statement.GetFirstToken().GetPreviousToken().SpanStart);
            if (formatting.StartsLine(statement.GetFirstToken()))
            {
                string current = formatting.GetIndentation(statement.SpanStart);
                return (outer + formatting.IndentUnit + formatting.IndentUnit).StartsWith(current, StringComparison.Ordinal)
                    ? new EmbeddedBlockEdit(statement, outer, current, coverage, isProportionate)
                    : null;
            }

            ImmutableArray<SyntaxToken> tokens = [.. statement.DescendantTokens()];
            return tokens[..^1].Any(static t => t.EndsLine) ? null : new EmbeddedBlockEdit(statement, outer, contentIndentation: null, coverage, isProportionate);
        }

        public override void Apply(EditSession session, SyntaxAnnotation annotation)
        {
            UnsafeContextFormatting formatting = session.Formatting;
            string middle = outer + formatting.IndentUnit;
            string inner = middle + formatting.IndentUnit;

            StatementSyntax source = PlaceholderContexts.Unwrap(statement);
            var (lines, inline) = UnsafeContextFormatting.SplitLeadingTrivia(source.GetLeadingTrivia());
            StatementSyntax content = source.WithLeadingTrivia(UnsafeContextFormatting.Whitespace(inner).AddRange(inline));
            if (contentIndentation is not null)
                content = formatting.Indent(content, inner[contentIndentation.Length..], includeFirstLine: false);

            BlockSyntax block = BlockSyntaxFactory
                .Create(formatting, outer, [BlockSyntaxFactory.CreateUnsafe(formatting, middle, [formatting.WithLineBreak(content)], lines)], [formatting.EndOfLine])
                .WithAdditionalAnnotations(annotation);

            if (contentIndentation is not null)
            {
                session.Editor.ReplaceNode(statement, block);
                return;
            }

            // The statement shares its header's line, which now ends before the new brace.
            session.Editor.ReplaceNode(statement.Parent!, (current, _) =>
            {
                SyntaxToken previous = GetEmbeddedStatement(current).GetFirstToken().GetPreviousToken();
                current = current.ReplaceToken(previous, previous.WithTrailingTrivia(formatting.EndOfLine));
                return current.ReplaceNode(GetEmbeddedStatement(current), block);
            });
        }

        private static StatementSyntax GetEmbeddedStatement(SyntaxNode parent) =>
            parent switch
            {
                IfStatementSyntax ifStatement => ifStatement.Statement,
                ElseClauseSyntax elseClause => elseClause.Statement,
                WhileStatementSyntax whileStatement => whileStatement.Statement,
                DoStatementSyntax doStatement => doStatement.Statement,
                ForStatementSyntax forStatement => forStatement.Statement,
                CommonForEachStatementSyntax forEachStatement => forEachStatement.Statement,
                UsingStatementSyntax usingStatement => usingStatement.Statement,
                LockStatementSyntax lockStatement => lockStatement.Statement,
                FixedStatementSyntax fixedStatement => fixedStatement.Statement,
                _ => throw new InvalidOperationException(),
            };
    }
}
