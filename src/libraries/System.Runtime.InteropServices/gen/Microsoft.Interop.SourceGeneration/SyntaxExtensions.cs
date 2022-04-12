// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    public static class SyntaxExtensions
    {
        public static FixedStatementSyntax AddStatementWithoutEmptyStatements(this FixedStatementSyntax fixedStatement, StatementSyntax childStatement)
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
