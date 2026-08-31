// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

            BlockSyntax block;
            if (fixedStatement.Statement.IsKind(SyntaxKind.Block))
            {
                block = (BlockSyntax)fixedStatement.Statement;
                if (block.Statements.Count == 0)
                {
                    return fixedStatement.WithStatement(childStatement);
                }
            }
            else
            {
                block = SyntaxFactory.Block(fixedStatement.Statement);
            }

            if (childStatement.IsKind(SyntaxKind.Block))
            {
                block = block.WithStatements(block.Statements.AddRange(((BlockSyntax)childStatement).Statements));
            }
            else
            {
                block = block.AddStatements(childStatement);
            }

            return fixedStatement.WithStatement(block);
        }

        public static StatementSyntax NestFixedStatements(this ImmutableArray<FixedStatementSyntax> fixedStatements, StatementSyntax innerStatement)
        {
            StatementSyntax nestedStatement = innerStatement;
            if (!fixedStatements.IsEmpty)
            {
                int i = fixedStatements.Length - 1;
                nestedStatement = fixedStatements[i].AddStatementWithoutEmptyStatements(WrapStatementInBlock(nestedStatement));
                i--;
                for (; i >= 0; i--)
                {
                    nestedStatement = fixedStatements[i].AddStatementWithoutEmptyStatements(nestedStatement);
                }
            }
            return nestedStatement;

            static StatementSyntax WrapStatementInBlock(StatementSyntax statement)
            {
                if (statement.IsKind(SyntaxKind.Block))
                {
                    return statement;
                }
                return SyntaxFactory.Block(statement);
            }
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

        public static SyntaxTokenList StripAccessibilityModifiers(this SyntaxTokenList tokenList)
        {
            List<SyntaxToken> strippedTokens = new();
            for (int i = 0; i < tokenList.Count; i++)
            {
                if (tokenList[i].Kind() is SyntaxKind.PublicKeyword or SyntaxKind.InternalKeyword or SyntaxKind.ProtectedKeyword or SyntaxKind.PrivateKeyword)
                {
                    continue;
                }
                strippedTokens.Add(tokenList[i]);
            }
            return new SyntaxTokenList(strippedTokens);
        }

        public static SyntaxTokenList AddToModifiers(this SyntaxTokenList modifiers, SyntaxKind modifierToAdd)
        {
            if (modifiers.IndexOf(modifierToAdd) >= 0)
            {
                return modifiers;
            }

            // https://github.com/dotnet/csharplang/blob/main/meetings/2018/LDM-2018-04-04.md#ordering-of-ref-and-partial-keywords
            int idxPartial = modifiers.IndexOf(SyntaxKind.PartialKeyword);
            int idxRef = modifiers.IndexOf(SyntaxKind.RefKeyword);

            int idxInsert = (idxPartial, idxRef) switch
            {
                (-1, -1) => modifiers.Count,
                (-1, _) => idxRef,
                (_, -1) => idxPartial,
                (_, _) => Math.Min(idxPartial, idxRef)
            };

            return modifiers.Insert(idxInsert, SyntaxFactory.Token(modifierToAdd));
        }

        public static bool IsInPartialContext(this TypeDeclarationSyntax syntax, [NotNullWhen(false)] out SyntaxToken? nonPartialIdentifier)
        {
            for (SyntaxNode? parentNode = syntax; parentNode is TypeDeclarationSyntax typeDecl; parentNode = parentNode.Parent)
            {
                if (!typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    nonPartialIdentifier = typeDecl.Identifier;
                    return false;
                }
            }
            nonPartialIdentifier = null;
            return true;
        }
    }
}
