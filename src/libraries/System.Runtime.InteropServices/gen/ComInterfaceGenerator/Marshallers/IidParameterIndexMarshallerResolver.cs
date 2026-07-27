// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.Interop.SyntaxFactoryExtensions;

namespace Microsoft.Interop
{
    internal sealed class IidParameterIndexMarshallerResolver : IMarshallingGeneratorResolver
    {
        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            if (info.MarshallingAttributeInfo is not IidParameterIndexNativeMarshallingInfo iidInfo
                || context.Direction != MarshalDirection.UnmanagedToManaged)
            {
                return ResolvedGenerator.UnresolvedGenerator;
            }

            return ResolvedGenerator.Resolved(new Marshaller(iidInfo.IidParameterIndexInfo).Bind(info, context));
        }

        private sealed class Marshaller(TypePositionInfo iidParameterIndexInfo) : IUnboundMarshallingGenerator
        {
            public ManagedTypeInfo AsNativeType(TypePositionInfo info) => new PointerTypeInfo("void*", "void*", false);

            public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info) => info.IsByRef ? SignatureBehavior.PointerToNativeType : SignatureBehavior.NativeType;

            public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context)
                => info.IsByRef ? ValueBoundaryBehavior.AddressOfNativeIdentifier : ValueBoundaryBehavior.NativeIdentifier;

            public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, out GeneratorDiagnostic? diagnostic)
                => ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, out diagnostic);

            public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;

            public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext codeContext, StubIdentifierContext context)
            {
                if (context.CurrentStage != StubIdentifierContext.Stage.Marshal)
                {
                    yield break;
                }

                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
                string unknownIdentifier = context.GetAdditionalIdentifier(info, "unknown");
                string queryInterfaceHResultIdentifier = context.GetAdditionalIdentifier(info, "queryInterfaceHResult");
                string queriedInterfaceIdentifier = context.GetAdditionalIdentifier(info, "queriedInterface");

                ExpressionSyntax iidExpression = MarshallerHelpers.GetIndexedManagedElementExpression(iidParameterIndexInfo, codeContext, context);
                yield return LocalDeclarationStatement(
                    VariableDeclaration(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))))
                        .AddVariables(
                            VariableDeclarator(Identifier(unknownIdentifier))
                                .WithInitializer(
                                    EqualsValueClause(
                                        CastExpression(
                                            PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                                            InvocationExpression(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        ParseTypeName("global::System.Runtime.InteropServices.Marshalling.ComInterfaceMarshaller<object>"),
                                                        IdentifierName("ConvertToUnmanaged")))
                                                .AddArgumentListArguments(
                                                    Argument(IdentifierName(managedIdentifier))))))));

                yield return IfStatement(
                    BinaryExpression(
                        SyntaxKind.NotEqualsExpression,
                        IdentifierName(unknownIdentifier),
                        LiteralExpression(SyntaxKind.NullLiteralExpression)),
                    Block(
                        LocalDeclarationStatement(
                            VariableDeclaration(TypeSyntaxes.System_IntPtr)
                                .AddVariables(
                                    VariableDeclarator(Identifier(queriedInterfaceIdentifier))
                                        .WithInitializer(
                                            EqualsValueClause(
                                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))))),
                        LocalDeclarationStatement(
                            VariableDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)))
                                .AddVariables(
                                    VariableDeclarator(Identifier(queryInterfaceHResultIdentifier))
                                        .WithInitializer(
                                            EqualsValueClause(
                                                InvocationExpression(
                                                        MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            TypeSyntaxes.System_Runtime_InteropServices_Marshal,
                                                            IdentifierName("QueryInterface")))
                                                    .AddArgumentListArguments(
                                                        Argument(CastExpression(TypeSyntaxes.System_IntPtr, IdentifierName(unknownIdentifier))),
                                                        Argument(iidExpression).WithRefKindKeyword(Token(SyntaxKind.InKeyword)),
                                                        Argument(IdentifierName(queriedInterfaceIdentifier)).WithRefKindKeyword(Token(SyntaxKind.OutKeyword))))))),
                        ExpressionStatement(
                            InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        ParseTypeName("global::System.Runtime.InteropServices.Marshalling.ComInterfaceMarshaller<object>"),
                                        IdentifierName("Free")))
                                .AddArgumentListArguments(
                                    Argument(IdentifierName(unknownIdentifier)))),
                        IfStatement(
                            BinaryExpression(
                                SyntaxKind.LessThanExpression,
                                IdentifierName(queryInterfaceHResultIdentifier),
                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))),
                            Block(
                                IfStatement(
                                    BinaryExpression(
                                        SyntaxKind.NotEqualsExpression,
                                        IdentifierName(queriedInterfaceIdentifier),
                                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))),
                                    ExpressionStatement(
                                        InvocationExpression(
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    TypeSyntaxes.System_Runtime_InteropServices_Marshal,
                                                    IdentifierName("Release")))
                                            .AddArgumentListArguments(
                                                Argument(IdentifierName(queriedInterfaceIdentifier))))),
                                ExpressionStatement(
                                    AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName(nativeIdentifier),
                                        LiteralExpression(SyntaxKind.NullLiteralExpression))),
                                // Throw a managed exception derived from the failing HRESULT. The stub's
                                // existing exception-to-HRESULT infrastructure (see
                                // ManagedHResultExceptionGeneratorResolver) catches this on the way out
                                // and returns the HRESULT to the unmanaged caller while also running
                                // the normal cleanup stages.
                                MethodInvocationStatement(
                                    TypeSyntaxes.System_Runtime_InteropServices_Marshal,
                                    IdentifierName("ThrowExceptionForHR"),
                                    Argument(IdentifierName(queryInterfaceHResultIdentifier))))),
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(nativeIdentifier),
                                CastExpression(
                                    PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                                    IdentifierName(queriedInterfaceIdentifier))))),
                    ElseClause(
                        Block(
                            ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName(nativeIdentifier),
                                    LiteralExpression(SyntaxKind.NullLiteralExpression))))));
            }
        }
    }
}
