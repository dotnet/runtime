// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix.UnsafeContext
{
    /// <summary>
    /// Layout helpers for generated contexts. Edits only add line breaks and indentation, so the rest of the document
    /// keeps its formatting, comments, directives and disabled text.
    /// </summary>
    internal sealed class UnsafeContextFormatting
    {
        private const string DefaultIndentUnit = "    ";

        private readonly SourceText _text;

        public UnsafeContextFormatting(SyntaxNode root)
        {
            _text = root.SyntaxTree.GetText();
            string newLine = root.DescendantTrivia().FirstOrDefault(static t => t.IsKind(SyntaxKind.EndOfLineTrivia)).ToFullString();
            EndOfLine = SyntaxFactory.EndOfLine(newLine.Length > 0 ? newLine : Environment.NewLine);
            IndentUnit = DetectIndentUnit(root);
        }

        public SyntaxTrivia EndOfLine { get; }

        /// <summary>One level of indentation, inferred from the document.</summary>
        public string IndentUnit { get; }

        /// <summary>Returns the leading whitespace of the line containing <paramref name="position"/>.</summary>
        public string GetIndentation(int position)
        {
            TextLine line = _text.Lines.GetLineFromPosition(position);
            int end = line.Start;
            while (end < line.End && _text[end] is ' ' or '\t')
                end++;

            return _text.ToString(TextSpan.FromBounds(line.Start, end));
        }

        /// <summary>Whether only whitespace precedes <paramref name="token"/> on its line.</summary>
        public bool StartsLine(SyntaxToken token) =>
            GetIndentation(token.SpanStart).Length == token.SpanStart - _text.Lines.GetLineFromPosition(token.SpanStart).Start;

        public static SyntaxTriviaList Whitespace(string indentation) =>
            indentation.Length == 0 ? default : [SyntaxFactory.Whitespace(indentation)];

        /// <summary>Returns the block marker line followed by the indentation of the <c>unsafe</c> keyword below it.</summary>
        public SyntaxTriviaList BlockMarkerLine(string indentation) =>
            [.. Whitespace(indentation), SyntaxFactory.Comment(UnsafeContextFacts.BlockMarker), EndOfLine, .. Whitespace(indentation)];

        /// <summary>
        /// Splits leading trivia into the complete lines above a token (comments, blank lines, directives) and the
        /// non-whitespace trivia on the token's own line.
        /// </summary>
        public static (SyntaxTriviaList Lines, SyntaxTriviaList Inline) SplitLeadingTrivia(SyntaxTriviaList trivia)
        {
            int linesEnd = 0;
            for (int i = 0; i < trivia.Count; i++)
            {
                if (trivia[i].EndsLine)
                    linesEnd = i + 1;
            }

            return ([.. trivia.Take(linesEnd)], [.. trivia.Skip(linesEnd).SkipWhile(static t => t.IsKind(SyntaxKind.WhitespaceTrivia))]);
        }

        /// <summary>Removes the last placeholder block marker line from leading trivia lines.</summary>
        public static SyntaxTriviaList RemoveBlockMarker(SyntaxTriviaList lines)
        {
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (!lines[i].IsKind(SyntaxKind.SingleLineCommentTrivia) || lines[i].ToString() != UnsafeContextFacts.BlockMarker)
                    continue;

                int start = i > 0 && lines[i - 1].IsKind(SyntaxKind.WhitespaceTrivia) ? i - 1 : i;
                int end = i + 1 < lines.Count && lines[i + 1].IsKind(SyntaxKind.EndOfLineTrivia) ? i + 1 : i;
                return [.. lines.Take(start), .. lines.Skip(end + 1)];
            }

            return lines;
        }

        /// <summary>Adds <paramref name="addition"/> to the indentation of complete trivia lines.</summary>
        public SyntaxTriviaList IndentLines(SyntaxTriviaList lines, string addition) =>
            lines.Count == 0 ? lines : IndentTrivia(lines, atLineStart: true, addition, indentFollowingToken: false);

        /// <summary>Ensures that <paramref name="node"/> ends its line.</summary>
        public T WithLineBreak<T>(T node) where T : SyntaxNode
        {
            SyntaxToken last = node.GetLastToken();
            if (last.EndsLine)
                return node;

            // Trailing whitespace before the new line break is dropped.
            SyntaxTriviaList trailing = [.. last.TrailingTrivia.Reverse().SkipWhile(static t => t.IsKind(SyntaxKind.WhitespaceTrivia)).Reverse(), EndOfLine];
            return node.ReplaceToken(last, last.WithTrailingTrivia(trailing));
        }

        /// <summary>
        /// Adds <paramref name="addition"/> to the indentation of every line of <paramref name="node"/> (the first only
        /// with <paramref name="includeFirstLine"/>). Directive lines and multi-line token contents are unchanged.
        /// </summary>
        public T Indent<T>(T node, string addition, bool includeFirstLine) where T : SyntaxNode
        {
            if (addition.Length == 0)
                return node;

            var replacements = new Dictionary<SyntaxToken, SyntaxToken>();
            bool atLineStart = includeFirstLine;
            bool isFirst = true;
            foreach (SyntaxToken token in node.DescendantTokens())
            {
                if (!isFirst || includeFirstLine)
                    replacements[token] = token.WithLeadingTrivia(IndentTrivia(token.LeadingTrivia, atLineStart, addition, indentFollowingToken: true));

                isFirst = false;
                atLineStart = token.EndsLine;
            }

            return node.ReplaceTokens(replacements.Keys, (original, _) => replacements[original]);
        }

        private static SyntaxTriviaList IndentTrivia(SyntaxTriviaList trivia, bool atLineStart, string addition, bool indentFollowingToken)
        {
            List<SyntaxTrivia> result = new(trivia.Count + 1);
            for (int i = 0; i < trivia.Count; i++)
            {
                SyntaxTrivia current = trivia[i];
                if (atLineStart)
                {
                    atLineStart = false;
                    if (current.IsKind(SyntaxKind.WhitespaceTrivia))
                    {
                        bool isDirectiveLine = i + 1 < trivia.Count && trivia[i + 1].IsDirective;
                        result.Add(isDirectiveLine ? current : SyntaxFactory.Whitespace(addition + current.ToFullString()));
                        continue;
                    }

                    if (!current.EndsLine)
                        result.Add(SyntaxFactory.Whitespace(addition));
                }

                result.Add(current.IsKind(SyntaxKind.DisabledTextTrivia) ? IndentDisabledText(current, addition) : current);
                atLineStart = current.EndsLine;
            }

            if (atLineStart && indentFollowingToken)
                result.Add(SyntaxFactory.Whitespace(addition));

            return [.. result];
        }

        // Inactive code is not parsed, so a line may be inside a multi-line string literal; such text is left as is.
        private static SyntaxTrivia IndentDisabledText(SyntaxTrivia trivia, string addition)
        {
            string text = trivia.ToFullString();
            if (text.IndexOf('"') >= 0)
                return trivia;

            var builder = new StringBuilder(text.Length);
            for (int lineStart = 0, lineEnd; lineStart < text.Length; lineStart = lineEnd)
            {
                lineEnd = text.IndexOf('\n', lineStart) is int newLine and >= 0 ? newLine + 1 : text.Length;
                string line = text[lineStart..lineEnd];
                builder.Append(line.Trim().Length > 0 ? addition + line : line);
            }

            return SyntaxFactory.DisabledText(builder.ToString());
        }

        // Infers one level of indentation from the first block whose statements are on their own lines.
        private string DetectIndentUnit(SyntaxNode root)
        {
            foreach (BlockSyntax block in root.DescendantNodes().OfType<BlockSyntax>())
            {
                if (block.Statements.Count == 0 || !StartsLine(block.OpenBraceToken) || !StartsLine(block.Statements[0].GetFirstToken()))
                    continue;

                string outer = GetIndentation(block.OpenBraceToken.SpanStart);
                string inner = GetIndentation(block.Statements[0].SpanStart);
                if (inner.Length > outer.Length && inner.StartsWith(outer, StringComparison.Ordinal))
                    return inner[outer.Length..];
            }

            return DefaultIndentUnit;
        }
    }
}
