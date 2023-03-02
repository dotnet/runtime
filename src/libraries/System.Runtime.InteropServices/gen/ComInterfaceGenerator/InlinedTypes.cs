// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        /// <summary>
        /// <code>
        /// file abstract unsafe class IUnknownVTableComWrappers : ComWrappers
        /// {
        ///     public static void GetIUnknownImpl(out void* pQueryInterface, out void* pAddRef, out void* pRelease)
        ///     {
        ///         System.IntPtr qi, addRef, release;
        ///         ComWrappers.GetIUnknownImpl(out qi, out addRef, out release);
        ///         pQueryInterface = (void*)qi;
        ///         pAddRef = (void*)addRef;
        ///         pRelease = (void*)release;
        ///     }
        /// }
        /// </code>
        /// </summary>
        public static ClassDeclarationSyntax IUnknownVTableComWrappers { get; } = GetIUnknownVTableComWrappers();

        private static ClassDeclarationSyntax GetIUnknownVTableComWrappers()
        {
            return ClassDeclaration("IUnknownVTableComWrappers")
                .AddModifiers(Token(SyntaxKind.AbstractKeyword),
                              Token(SyntaxKind.UnsafeKeyword),
                              Token(SyntaxKind.PublicKeyword)
                              //,Token(SyntaxKind.FileKeyword)
                              )
                .AddBaseListTypes(SimpleBaseType(ParseTypeName(TypeNames.ComWrappers)))
                .AddMembers(
                    MethodDeclaration(
                        PredefinedType(Token(SyntaxKind.VoidKeyword)),
                        Identifier("GetIUnknownImpl"))
                    .AddModifiers(Token(SyntaxKind.PublicKeyword),
                                  Token(SyntaxKind.StaticKeyword))
                    .AddParameterListParameters(
                        Parameter(Identifier("pQueryInterface"))
                            .WithType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)))),
                        Parameter(Identifier("pAddRef"))
                            .WithType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)))),
                        Parameter(Identifier("pRelease"))
                            .WithType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)))))
                    .WithBody(body: Body()));
            /// <summary>
            /// <code>
            ///     {
            ///         nint qi, addRef, release;
            ///         ComWrappers.GetIUnknownImpl(out qi, out addRef, out release);
            ///         pQueryInterface = (void*)qi;
            ///         pAddRef = (void*)addRef;
            ///         pRelease = (void*)release;
            ///     }
            /// </code>
            /// </summary>
            static BlockSyntax Body()
            {
                /// nint qi, addRef, release;
                var variableDeclarations = VariableDeclaration(ParseTypeName("nint"))
                    .AddVariables(
                        VariableDeclarator(Identifier("qi")),
                        VariableDeclarator(Identifier("addRef")),
                        VariableDeclarator(Identifier("release")));
                /// ComWrappers.GetIUnknownImpl(out qi, out addRef, out release);
                var invocation = InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        ParseTypeName(TypeNames.ComWrappers),
                                        IdentifierName("GetIUnknownImpl")))
                                .AddArgumentListArguments(
                                    Argument(IdentifierName("qi")).WithRefKindKeyword(Token(SyntaxKind.OutKeyword)),
                                    Argument(IdentifierName("addRef")).WithRefKindKeyword(Token(SyntaxKind.OutKeyword)),
                                    Argument(IdentifierName("release")).WithRefKindKeyword(Token(SyntaxKind.OutKeyword)));

                /// pQueryInterface = (void*)qi;
                var pQueryAssignmentAssignment = AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName("pQueryInterface"),
                    CastExpression(
                        PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                        IdentifierName("qi")));
                /// pAddRef = (void*)addRef;
                var pAddRefAssignment = AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName("pAddRef"),
                    CastExpression(
                        PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                        IdentifierName("addRef")));
                /// pRelease = (void*)release;
                var pReleaseAssignment = AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName("pRelease"),
                    CastExpression(
                        PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                        IdentifierName("release")));
                return Block(
                    LocalDeclarationStatement(variableDeclarations),
                    ExpressionStatement(invocation),
                    ExpressionStatement(pQueryAssignmentAssignment),
                    ExpressionStatement(pAddRefAssignment),
                    ExpressionStatement(pReleaseAssignment));
            }
        }

    }
}
