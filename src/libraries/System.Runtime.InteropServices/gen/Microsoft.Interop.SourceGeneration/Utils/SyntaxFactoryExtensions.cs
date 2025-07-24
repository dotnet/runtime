// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public static class SyntaxFactoryExtensions
    {
        /// <summary>
        /// <code><paramref name="typeSyntax"/> <paramref name="identifier"/> = default;</code>
        /// or
        /// <code><paramref name="typeSyntax"/> <paramref name="identifier"/>;</code>
        /// </summary>
        public static LocalDeclarationStatementSyntax Declare(TypeSyntax typeSyntax, string identifier, bool initializeToDefault)
        {
            return Declare(typeSyntax, identifier, initializeToDefault ? LiteralExpression(SyntaxKind.DefaultLiteralExpression) : null);
        }

        /// <summary>
        /// <code><paramref name="typeSyntax"/> <paramref name="identifier"/> = <paramref name="identifier"/>;</code>
        /// or
        /// <code><paramref name="typeSyntax"/> <paramref name="identifier"/>;</code>
        /// </summary>
        public static LocalDeclarationStatementSyntax Declare(TypeSyntax typeSyntax, string identifier, ExpressionSyntax? initializer)
        {
            VariableDeclaratorSyntax decl = VariableDeclarator(identifier);
            if (initializer is not null)
            {
                decl = decl.WithInitializer(
                    EqualsValueClause(
                        initializer));
            }

            // <type> <identifier>;
            // or
            // <type> <identifier> = <initializer>;
            return LocalDeclarationStatement(
                VariableDeclaration(
                    typeSyntax,
                    SingletonSeparatedList(decl)));
        }

        public static InvocationExpressionSyntax MethodInvocation(ExpressionSyntax objectOrClass, SimpleNameSyntax methodName)
            => InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        objectOrClass,
                        methodName),
                    ArgumentList(SeparatedList<ArgumentSyntax>()));

        public static InvocationExpressionSyntax MethodInvocation(ExpressionSyntax objectOrClass, SimpleNameSyntax methodName, params ArgumentSyntax[] arguments)
            => InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        objectOrClass,
                        methodName),
                    ArgumentList(SeparatedList(arguments)));

        /// <summary>
        /// <code>
        /// <paramref name="objectOrClass"/>.<paramref name="methodName"/>(<paramref name="arguments"/>);
        /// </code>
        /// </summary>
        public static ExpressionStatementSyntax MethodInvocationStatement(ExpressionSyntax objectOrClass, SimpleNameSyntax methodName, params ArgumentSyntax[] arguments)
            => ExpressionStatement(MethodInvocation(objectOrClass, methodName, arguments));

        public static ArgumentSyntax RefArgument(ExpressionSyntax expression)
            => Argument(null, Token(SyntaxKind.RefKeyword), expression);

        public static ArgumentSyntax InArgument(ExpressionSyntax expression)
            => Argument(null, Token(SyntaxKind.InKeyword), expression);

        public static ArgumentSyntax OutArgument(ExpressionSyntax expression)
            => Argument(null, Token(SyntaxKind.OutKeyword), expression);

        private static readonly SyntaxToken _span = Identifier(TypeNames.System_Span);
        public static GenericNameSyntax SpanOf(TypeSyntax type) => GenericName(_span, TypeArgumentList(SingletonSeparatedList(type)));

        private static readonly SyntaxToken _readonlySpan = Identifier(TypeNames.System_ReadOnlySpan);
        public static GenericNameSyntax ReadOnlySpanOf(TypeSyntax type) => GenericName(_readonlySpan, TypeArgumentList(SingletonSeparatedList(type)));

        public static MemberAccessExpressionSyntax Dot(this ExpressionSyntax expression, SimpleNameSyntax member) =>
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        expression,
                        member);

        public static ElementAccessExpressionSyntax IndexExpression(ExpressionSyntax indexed, ArgumentSyntax argument)
            => ElementAccessExpression(
                indexed,
                BracketedArgumentList(SingletonSeparatedList(argument)));

        public static LiteralExpressionSyntax IntLiteral(int number) => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(number));

        /// <summary>
        /// <code>
        /// <paramref name="left"/> = <paramref name="right"/>;
        /// </code>
        /// </summary>
        public static ExpressionStatementSyntax AssignmentStatement(ExpressionSyntax left, ExpressionSyntax right)
            => ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, right));

        /// <summary>
        /// Returns a for loop with no body:
        /// <code>
        /// for (int <paramref name="indexerIdentifier"/>; <paramref name="indexerIdentifier"/> &lt; <paramref name="lengthExpression"/>; ++<paramref name="indexerIdentifier"/>)
        /// ;
        /// </code>
        /// </summary>
        public static ForStatementSyntax ForLoop(string indexerIdentifier, ExpressionSyntax lengthExpression)
        {
            // for (int <indexerIdentifier> = 0; <indexerIdentifier> < <lengthIdentifier>; ++<indexerIdentifier>)
            //      ;
            return ForStatement(EmptyStatement())
            .WithDeclaration(
                VariableDeclaration(
                    PredefinedType(
                        Token(SyntaxKind.IntKeyword)))
                .WithVariables(
                    SingletonSeparatedList(
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
                    lengthExpression))
            .WithIncrementors(
                SingletonSeparatedList<ExpressionSyntax>(
                    PrefixUnaryExpression(
                        SyntaxKind.PreIncrementExpression,
                        IdentifierName(indexerIdentifier))));
        }


    }
}
