// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    public static class SyntaxExtensions
    {
        private static FixedStatementSyntax AddStatementWithoutEmptyStatements(this FixedStatementSyntax fixedStatement, StatementSyntax childStatement)
        {
            if (fixedStatement.Statement.IsKind(SyntaxKind.EmptyStatement))
            {
                return fixedStatement.WithStatement(childStatement);
            }
            if (fixedStatement.Statement.IsKind(SyntaxKind.Block))
            {
                var block = (BlockSyntax)fixedStatement.Statement;
                if (block.Statements.Count == 0)
                {
                    return fixedStatement.WithStatement(childStatement);
                }
                return fixedStatement.WithStatement(block.AddStatements(childStatement));
            }
            return fixedStatement.WithStatement(SyntaxFactory.Block(fixedStatement.Statement, childStatement));
        }

        public static StatementSyntax NestFixedStatements(this ImmutableArray<FixedStatementSyntax> fixedStatements, StatementSyntax innerStatement)
        {
            StatementSyntax nestedStatement = innerStatement;
            if (!fixedStatements.IsEmpty)
            {
                int i = fixedStatements.Length - 1;
                nestedStatement = fixedStatements[i].AddStatementWithoutEmptyStatements(SyntaxFactory.Block(nestedStatement));
                i--;
                for (; i >= 0; i--)
                {
                    nestedStatement = fixedStatements[i].AddStatementWithoutEmptyStatements(nestedStatement);
                }
            }
            return nestedStatement;
        }

        public static SyntaxTokenList StripTriviaFromTokens(this SyntaxTokenList tokenList)
        {
            SyntaxToken[] strippedTokens = new SyntaxToken[tokenList.Count];
            for (int i = 0; i < tokenList.Count; i++)
            {
                strippedTokens[i] = tokenList[i].WithoutTrivia();
            }
            return new SyntaxTokenList(strippedTokens);
        }

        public static SyntaxTokenList AddToModifiers(this SyntaxTokenList modifiers, SyntaxKind modifierToAdd)
        {
            if (modifiers.IndexOf(modifierToAdd) >= 0)
                return modifiers;

            int idx = modifiers.IndexOf(SyntaxKind.PartialKeyword);
            return idx >= 0
                ? modifiers.Insert(idx, SyntaxFactory.Token(modifierToAdd))
                : modifiers.Add(SyntaxFactory.Token(modifierToAdd));
        }
    }
}
