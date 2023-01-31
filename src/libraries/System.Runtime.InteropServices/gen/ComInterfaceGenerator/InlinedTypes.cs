// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal static class InlinedTypes
    {
        /// <summary>
        /// The ClassDeclarationSyntax for the following
        /// <code>
        /// file abstract unsafe class IUnknownVTableComWrappers : ComWrappers
        /// {
        ///     public static void GetIUnknownImpl(out void* pQueryInterface, out void* pAddRef, out void* pRelease)
        ///     {
        ///         nint qi, addRef, release;
        ///         ComWrappers.GetIUnknownImpl(out qi, out addRef, out release);
        ///         pQueryInterface = (void*)qi;
        ///         pAddRef = (void*)addRef;
        ///         pRelease = (void*)release;
        ///     }
        /// }
        /// </code>
        /// </summary>
        public static ClassDeclarationSyntax IUnknownVTableComWrappersInstance { get; } = GetIUnknownVTableComWrappersInstance();

        private static ClassDeclarationSyntax GetIUnknownVTableComWrappersInstance()
        {
            const string pQueryInterface = nameof(pQueryInterface);
            const string pAddRef = nameof(pAddRef);
            const string pRelease = nameof(pRelease);
            const string ComWrappers = nameof(ComWrappers);
            const string GetIUnknownImpl = nameof(GetIUnknownImpl);

            return ClassDeclaration("IUnknownVTableComWrappers")
                .AddModifiers(Token(SyntaxKind.AbstractKeyword),
                              Token(SyntaxKind.UnsafeKeyword),
                              Token(SyntaxKind.FileKeyword))
                .AddBaseListTypes(SimpleBaseType(ParseTypeName(ComWrappers)))
                .AddMembers(
                    MethodDeclaration(
                        PredefinedType(Token(SyntaxKind.VoidKeyword)),
                        Identifier(GetIUnknownImpl))
                    .AddModifiers(Token(SyntaxKind.PublicKeyword),
                                  Token(SyntaxKind.StaticKeyword))
                    .AddParameterListParameters(
                        Parameter(Identifier(pQueryInterface))
                            .WithType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))))
                            .AddModifiers(Token(SyntaxKind.OutKeyword)),
                        Parameter(Identifier(pAddRef))
                            .WithType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))))
                            .AddModifiers(Token(SyntaxKind.OutKeyword)),
                        Parameter(Identifier(pRelease))
                            .WithType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))))
                            .AddModifiers(Token(SyntaxKind.OutKeyword)))
                    .WithBody(body: Body()));

            static BlockSyntax Body()
            {
                /// {
                ///     nint qi, addRef, release;
                ///     ComWrappers.GetIUnknownImpl(out qi, out addRef, out release);
                ///     pQueryInterface = (void*)qi;
                ///     pAddRef = (void*)addRef;
                ///     pRelease = (void*)release;
                /// }
                const string qi = nameof(qi);
                const string addRef = nameof(addRef);
                const string release = nameof(release);

                /// nint qi, addRef, release;
                var declarations = LocalDeclarationStatement(
                        VariableDeclaration(
                            IdentifierName("nint"))
                        .AddVariables(
                            VariableDeclarator(qi),
                            VariableDeclarator(addRef),
                            VariableDeclarator(release)));

                /// ComWrappers.GetIUnknownImpl(out qi, out addRef, out release);
                var invocation = ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(ComWrappers),
                                        IdentifierName("GetIUnknownImpl")))
                                .AddArgumentListArguments(
                                    Argument(
                                        null,
                                        Token(SyntaxKind.OutKeyword),
                                        IdentifierName(qi)),
                                    Argument(
                                        null,
                                        Token(SyntaxKind.OutKeyword),
                                        IdentifierName(addRef)),
                                    Argument(
                                        null,
                                        Token(SyntaxKind.OutKeyword),
                                        IdentifierName(release))));

                // pQueryInterface = (void*)qi;
                var pQueryInterfaceAssignment = GenerateCastToVoidPtrAndAssign(target: pQueryInterface, source: qi);

                // pAddRef = (void*)addRef;
                var pAddRefAssignment = GenerateCastToVoidPtrAndAssign(target: pAddRef, source: addRef);

                // pRelease = (void*)release;
                var pReleaseAssignment = GenerateCastToVoidPtrAndAssign(target: pRelease, source: release);

                return Block(
                        declarations,
                        invocation,
                        pQueryInterfaceAssignment,
                        pAddRefAssignment,
                        pReleaseAssignment);
            }
            static ExpressionStatementSyntax GenerateCastToVoidPtrAndAssign(string target, string source)
            {
                return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(target),
                        CastExpression(
                            PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                            IdentifierName(source))));
            }
        }

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
        public static ClassDeclarationSyntax ComWrappersUnwrapper { get; }

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
