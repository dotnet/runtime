// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.Interop;

public static class IndentedTextWriterExtensions
{
    public static void WriteMultilineNode(this IndentedTextWriter writer, SyntaxNode node)
    {
        foreach (SyntaxToken token in node.DescendantTokens())
        {
            foreach (SyntaxTrivia leadingTrivia in token.LeadingTrivia)
            {
                if (leadingTrivia.IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    writer.WriteLine();
                    continue;
                }

                writer.Write(leadingTrivia.ToString());
            }

            writer.Write(token.Text);

            foreach (SyntaxTrivia trailingTrivia in token.TrailingTrivia)
            {
                if (trailingTrivia.IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    writer.WriteLine();
                    continue;
                }

                writer.Write(trailingTrivia.ToString());
            }
        }

        writer.WriteLine();
    }
}
