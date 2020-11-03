using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal static class MarshallerHelpers
    {
        public static ForStatementSyntax GetForLoop(string collectionIdentifier, string indexerIdentifier)
        {
            // for(int <indexerIdentifier> = 0; <indexerIdentifier> < <collectionIdentifier>.Length; ++<indexerIdentifier>)
            //      ;
            return ForStatement(EmptyStatement())
            .WithDeclaration(
                VariableDeclaration(
                    PredefinedType(
                        Token(SyntaxKind.IntKeyword)))
                .WithVariables(
                    SingletonSeparatedList<VariableDeclaratorSyntax>(
                        VariableDeclarator(
                            Identifier(indexerIdentifier))
                        .WithInitializer(
                            EqualsValueClause(
                                LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    Literal(0)))))))
            .WithCondition(
                BinaryExpression(
                    SyntaxKind.LessThanExpression,
                    IdentifierName(indexerIdentifier),
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(collectionIdentifier),
                        IdentifierName("Length"))))
            .WithIncrementors(
                SingletonSeparatedList<ExpressionSyntax>(
                    PrefixUnaryExpression(
                        SyntaxKind.PreIncrementExpression,
                        IdentifierName(indexerIdentifier))));
        }
    }
}