// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal static class InlinedTypes
    {
        /// <summary>
        /// Returns the ClassDeclarationSyntax for:
        /// <code>
        /// public sealed unsafe class ComWrappersUnwrapper : IUnmanagedObjectUnwrapper
        /// {
        ///     public static object GetObjectForUnmanagedWrapper(void* ptr)
        ///     {
        ///         return ComWrappers.ComInterfaceDispatch.GetInstance<object>((ComWrappers.ComInterfaceDispatch*)ptr);
        ///     }
        /// }
        /// </code>
        /// </summary>
        public static ClassDeclarationSyntax ComWrappersUnwrapper { get; } = GetComWrappersUnwrapper();

        public static ClassDeclarationSyntax GetComWrappersUnwrapper()
        {
            return ClassDeclaration("ComWrappersUnwrapper")
                .AddModifiers(Token(SyntaxKind.SealedKeyword),
                              Token(SyntaxKind.UnsafeKeyword),
                              Token(SyntaxKind.StaticKeyword),
                              Token(SyntaxKind.FileKeyword))
                .AddMembers(
                    MethodDeclaration(
                        PredefinedType(Token(SyntaxKind.ObjectKeyword)),
                        Identifier("GetComObjectForUnmanagedWrapper"))
                    .AddModifiers(Token(SyntaxKind.PublicKeyword),
                                  Token(SyntaxKind.StaticKeyword))
                    .AddParameterListParameters(
                        Parameter(Identifier("ptr"))
                            .WithType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)))))
                    .WithBody(body: Body()));

            static BlockSyntax Body()
            {
                var invocation = InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("ComWrappers"),
                                            IdentifierName("ComInterfaceDispatch")),
                                        GenericName(
                                            Identifier("GetInstance"),
                                            TypeArgumentList(
                                                SeparatedList<SyntaxNode>(
                                                    new[] { PredefinedType(Token(SyntaxKind.ObjectKeyword)) })))))
                                .AddArgumentListArguments(
                                    Argument(
                                        null,
                                        Token(SyntaxKind.None),
                                        CastExpression(
                                            PointerType(
                                                QualifiedName(
                                                    IdentifierName("ComWrappers"),
                                                    IdentifierName("ComInterfaceDispatch"))),
                                            IdentifierName("ptr"))));

                return Block(ReturnStatement(invocation));
            }
        }

        /// <summary>
        /// <code>
        /// file static class UnmanagedObjectUnwrapper
        /// {
        ///     public static object GetObjectForUnmanagedWrapper<T>(void* ptr) where T : IUnmanagedObjectUnwrapper
        ///     {
        ///         return T.GetObjectForUnmanagedWrapper(ptr);
        ///     }
        /// }
        /// </code>
        /// </summary>
        public static ClassDeclarationSyntax UnmanagedObjectUnwrapper { get; } = GetUnmanagedObjectUnwrapper();

        private static ClassDeclarationSyntax GetUnmanagedObjectUnwrapper()
        {
            const string tUnwrapper = "TUnwrapper";
            return ClassDeclaration("UnmanagedObjectUnwrapper")
                  .AddModifiers(Token(SyntaxKind.FileKeyword),
                                Token(SyntaxKind.StaticKeyword))
                  .AddMembers(
                      MethodDeclaration(
                          PredefinedType(Token(SyntaxKind.ObjectKeyword)),
                          Identifier("GetObjectForUnmanagedWrapper"))
                      .AddModifiers(Token(SyntaxKind.PublicKeyword),
                                    Token(SyntaxKind.StaticKeyword))
                      .AddTypeParameterListParameters(
                          TypeParameter(Identifier(tUnwrapper)))
                      .AddParameterListParameters(
                          Parameter(Identifier("ptr"))
                              .WithType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)))))
                      .AddConstraintClauses(TypeParameterConstraintClause(IdentifierName(tUnwrapper))
                           .AddConstraints(TypeConstraint(ParseTypeName(TypeNames.IUnmanagedObjectUnwrapper))))
                      .WithBody(body: Body()));

            static BlockSyntax Body()
            {
                var invocation = InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("T"),
                                        IdentifierName("GetObjectForUnmanagedWrapper")))
                                .AddArgumentListArguments(
                                    Argument(
                                        null,
                                        Token(SyntaxKind.None),
                                        IdentifierName("ptr")));

                return Block(ReturnStatement(invocation));
            }

        }
    }
}
