using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal static class MarshallerHelpers
    {
        public static readonly ExpressionSyntax IsWindows = InvocationExpression(
                                                        MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            ParseTypeName("System.OperatingSystem"),
                                                            IdentifierName("IsWindows")));

        public static readonly TypeSyntax InteropServicesMarshalType = ParseTypeName(TypeNames.System_Runtime_InteropServices_Marshal);

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

        public static class StringMarshaller
        {
            public static ExpressionSyntax AllocationExpression(CharEncoding encoding, string managedIdentifier)
            {
                string methodName = encoding switch
                {
                    CharEncoding.Utf8 => "StringToCoTaskMemUTF8",
                    CharEncoding.Utf16 => "StringToCoTaskMemUni",
                    CharEncoding.Ansi => "StringToCoTaskMemAnsi",
                    _ => throw new System.ArgumentOutOfRangeException(nameof(encoding))
                };

                // Marshal.StringToCoTaskMemUTF8(<managed>)
                // or
                // Marshal.StringToCoTaskMemUni(<managed>)
                // or
                // Marshal.StringToCoTaskMemAnsi(<managed>)
                return InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        InteropServicesMarshalType,
                        IdentifierName(methodName)),
                    ArgumentList(
                        SingletonSeparatedList<ArgumentSyntax>(
                            Argument(IdentifierName(managedIdentifier)))));
            }

            public static ExpressionSyntax FreeExpression(string nativeIdentifier)
            {
                // Marshal.FreeCoTaskMem((IntPtr)<nativeIdentifier>)
                return InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        InteropServicesMarshalType,
                        IdentifierName("FreeCoTaskMem")),
                    ArgumentList(SingletonSeparatedList(
                        Argument(
                            CastExpression(
                                ParseTypeName("System.IntPtr"),
                                IdentifierName(nativeIdentifier))))));
            }
        }
    }
}