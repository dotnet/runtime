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

            return SyntaxFactory.ClassDeclaration("IUnknownVTableComWrappers")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.AbstractKeyword),
                              SyntaxFactory.Token(SyntaxKind.UnsafeKeyword),
                              SyntaxFactory.Token(SyntaxKind.FileKeyword))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(ComWrappers)))
                .AddMembers(
                    SyntaxFactory.MethodDeclaration(
                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                        SyntaxFactory.Identifier(GetIUnknownImpl))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                  SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier(pQueryInterface))
                            .WithType(SyntaxFactory.PointerType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword))))
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.OutKeyword)),
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier(pAddRef))
                            .WithType(SyntaxFactory.PointerType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword))))
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.OutKeyword)),
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier(pRelease))
                            .WithType(SyntaxFactory.PointerType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword))))
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.OutKeyword)))
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
                var declarations = SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(
                            SyntaxFactory.IdentifierName("nint"))
                        .AddVariables(
                            SyntaxFactory.VariableDeclarator(qi),
                            SyntaxFactory.VariableDeclarator(addRef),
                            SyntaxFactory.VariableDeclarator(release)));

                /// ComWrappers.GetIUnknownImpl(out qi, out addRef, out release);
                var invocation = SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName(ComWrappers),
                                        SyntaxFactory.IdentifierName("GetIUnknownImpl")))
                                .AddArgumentListArguments(
                                    SyntaxFactory.Argument(
                                        null,
                                        SyntaxFactory.Token(SyntaxKind.OutKeyword),
                                        SyntaxFactory.IdentifierName(qi)),
                                    SyntaxFactory.Argument(
                                        null,
                                        SyntaxFactory.Token(SyntaxKind.OutKeyword),
                                        SyntaxFactory.IdentifierName(addRef)),
                                    SyntaxFactory.Argument(
                                        null,
                                        SyntaxFactory.Token(SyntaxKind.OutKeyword),
                                        SyntaxFactory.IdentifierName(release))));

                /// pQueryInterface = (void*)qi;
                var pQueryInterfaceAssignment = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(pQueryInterface),
                        SyntaxFactory.CastExpression(
                            SyntaxFactory.PointerType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword))),
                            SyntaxFactory.IdentifierName(qi))));

                /// pAddRef = (void*)addRef;
                var pAddRefAssignment = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(pAddRef),
                        SyntaxFactory.CastExpression(
                            SyntaxFactory.PointerType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword))),
                            SyntaxFactory.IdentifierName(addRef))));

                /// pRelease = (void*)release;
                var pReleaseAssignment = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(pRelease),
                        SyntaxFactory.CastExpression(
                            SyntaxFactory.PointerType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword))),
                            SyntaxFactory.IdentifierName(release))));

                return SyntaxFactory.Block(
                        declarations,
                        invocation,
                        pQueryInterfaceAssignment,
                        pAddRefAssignment,
                        pReleaseAssignment);
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
            return SyntaxFactory.ClassDeclaration("ComWrappersUnwrapper")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.SealedKeyword),
                              SyntaxFactory.Token(SyntaxKind.UnsafeKeyword),
                              SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                              SyntaxFactory.Token(SyntaxKind.FileKeyword))
                .AddMembers(
                    SyntaxFactory.MethodDeclaration(
                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                        SyntaxFactory.Identifier("GetComObjectForUnmanagedWrapper"))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                  SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("ptr"))
                            .WithType(SyntaxFactory.PointerType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)))))
                    .WithBody(body: Body()));

            static BlockSyntax Body()
            {
                var invocation = SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName("ComWrappers"),
                                            SyntaxFactory.IdentifierName("ComInterfaceDispatch")),
                                        SyntaxFactory.GenericName(
                                            SyntaxFactory.Identifier("GetInstance"),
                                            SyntaxFactory.TypeArgumentList(
                                                SyntaxFactory.SeparatedList<SyntaxNode>(
                                                    new[] { SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)) })))))
                                .AddArgumentListArguments(
                                    SyntaxFactory.Argument(
                                        null,
                                        SyntaxFactory.Token(SyntaxKind.None),
                                        SyntaxFactory.CastExpression(
                                            SyntaxFactory.PointerType(
                                                SyntaxFactory.QualifiedName(
                                                    SyntaxFactory.IdentifierName("ComWrappers"),
                                                    SyntaxFactory.IdentifierName("ComInterfaceDispatch"))),
                                            SyntaxFactory.IdentifierName("ptr"))));

                return SyntaxFactory.Block(SyntaxFactory.ReturnStatement(invocation));
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
            return SyntaxFactory.ClassDeclaration("UnmanagedObjectUnwrapper")
                  .AddModifiers(SyntaxFactory.Token(SyntaxKind.FileKeyword),
                                SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                  .AddMembers(
                      SyntaxFactory.MethodDeclaration(
                          SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                          SyntaxFactory.Identifier("GetObjectForUnmanagedWrapper"))
                      .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                    SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                      .AddTypeParameterListParameters(
                          SyntaxFactory.TypeParameter(SyntaxFactory.Identifier(tUnwrapper)))
                      .AddParameterListParameters(
                          SyntaxFactory.Parameter(SyntaxFactory.Identifier("ptr"))
                              .WithType(SyntaxFactory.PointerType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)))))
                      .AddConstraintClauses(SyntaxFactory.TypeParameterConstraintClause(SyntaxFactory.IdentifierName(tUnwrapper))
                           .AddConstraints(SyntaxFactory.TypeConstraint(SyntaxFactory.ParseTypeName(TypeNames.IUnmanagedObjectUnwrapper))))
                      .WithBody(body: Body()));

            static BlockSyntax Body()
            {
                var invocation = SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("T"),
                                        SyntaxFactory.IdentifierName("GetObjectForUnmanagedWrapper")))
                                .AddArgumentListArguments(
                                    SyntaxFactory.Argument(
                                        null,
                                        SyntaxFactory.Token(SyntaxKind.None),
                                        SyntaxFactory.IdentifierName("ptr")));

                return SyntaxFactory.Block(SyntaxFactory.ReturnStatement(invocation));
            }

        }
    }
}
