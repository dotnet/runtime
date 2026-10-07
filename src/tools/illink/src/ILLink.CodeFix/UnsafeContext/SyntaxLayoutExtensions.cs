// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ILLink.CodeFix.UnsafeContext
{
    /// <summary>Layout and declaration queries over syntax, shared by the planners and edits.</summary>
    internal static class SyntaxLayoutExtensions
    {
        extension(SyntaxTrivia trivia)
        {
            /// <summary>Whitespace or a line break.</summary>
            public bool IsLayout => trivia.IsKind(SyntaxKind.WhitespaceTrivia) || trivia.IsKind(SyntaxKind.EndOfLineTrivia);

            /// <summary>Line breaks, directives, disabled text and doc comments, which all end their line.</summary>
            public bool EndsLine =>
                trivia.IsKind(SyntaxKind.EndOfLineTrivia)
                || trivia.IsKind(SyntaxKind.DisabledTextTrivia)
                || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                || trivia.IsDirective;
        }

        extension(SyntaxTriviaList trivia)
        {
            public bool IsLayoutOnly => trivia.All(static t => t.IsLayout);

            /// <summary>The last trivia that is not whitespace or a line break, e.g. the comment directly above a token.</summary>
            public SyntaxTrivia LastNonLayout => trivia.LastOrDefault(static t => !t.IsLayout);
        }

        extension(SyntaxToken token)
        {
            /// <summary>Whether the token is the last one on its line.</summary>
            public bool EndsLine => token.TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia);
        }

        extension(SyntaxNode node)
        {
            /// <summary>The first token of a declaration after its attribute lists, e.g. the first modifier.</summary>
            public SyntaxToken FirstTokenAfterAttributes =>
                (node switch
                {
                    MemberDeclarationSyntax member => member.AttributeLists,
                    AccessorDeclarationSyntax accessor => accessor.AttributeLists,
                    LocalFunctionStatementSyntax localFunction => localFunction.AttributeLists,
                    _ => default,
                }) is { Count: > 0 } attributeLists
                    ? attributeLists[^1].GetLastToken().GetNextToken()
                    : node.GetFirstToken();
        }
    }
}
