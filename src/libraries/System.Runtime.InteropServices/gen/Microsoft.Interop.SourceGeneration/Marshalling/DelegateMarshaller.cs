// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public sealed class DelegateMarshaller : IMarshallingGenerator
    {
        public bool IsSupported(TargetFramework target, Version version) => true;

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return MarshallerHelpers.SystemIntPtrType;
        }

        public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info)
        {
            return info.IsByRef ? SignatureBehavior.PointerToNativeType : SignatureBehavior.NativeType;
        }

        public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context)
        {
            return info.IsByRef ? ValueBoundaryBehavior.AddressOfNativeIdentifier : ValueBoundaryBehavior.NativeIdentifier;
        }

        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            // [TODO] Handle byrefs in a more common place?
            // This pattern will become very common (arrays and strings will also use it)
            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            switch (context.CurrentStage)
            {
                case StubCodeContext.Stage.Setup:
                    break;
                case StubCodeContext.Stage.Marshal:
                    if (info.RefKind != RefKind.Out)
                    {
                        // <nativeIdentifier> = <managedIdentifier> != null ? Marshal.GetFunctionPointerForDelegate(<managedIdentifier>) : default;
                        yield return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(nativeIdentifier),
                                ConditionalExpression(
                                    BinaryExpression(
                                        SyntaxKind.NotEqualsExpression,
                                        IdentifierName(managedIdentifier),
                                        LiteralExpression(SyntaxKind.NullLiteralExpression)
                                    ),
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            ParseName(TypeNames.System_Runtime_InteropServices_Marshal),
                                            IdentifierName("GetFunctionPointerForDelegate")),
                                        ArgumentList(SingletonSeparatedList(Argument(IdentifierName(managedIdentifier))))),
                                    LiteralExpression(SyntaxKind.DefaultLiteralExpression))));
                    }
                    break;
                case StubCodeContext.Stage.Unmarshal:
                    if (info.IsManagedReturnPosition || (info.IsByRef && info.RefKind != RefKind.In))
                    {
                        // <managedIdentifier> = <nativeIdentifier> != default : Marshal.GetDelegateForFunctionPointer<<managedType>>(<nativeIdentifier>) : null;
                        yield return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(managedIdentifier),
                                ConditionalExpression(
                                    BinaryExpression(
                                        SyntaxKind.NotEqualsExpression,
                                        IdentifierName(nativeIdentifier),
                                        LiteralExpression(SyntaxKind.DefaultLiteralExpression)),
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            ParseName(TypeNames.System_Runtime_InteropServices_Marshal),
                                            GenericName(Identifier("GetDelegateForFunctionPointer"))
                                            .WithTypeArgumentList(
                                                TypeArgumentList(
                                                    SingletonSeparatedList(
                                                        info.ManagedType.Syntax)))),
                                        ArgumentList(SingletonSeparatedList(Argument(IdentifierName(nativeIdentifier))))),
                                    LiteralExpression(SyntaxKind.NullLiteralExpression))));
                    }
                    break;
                case StubCodeContext.Stage.NotifyForSuccessfulInvoke:
                    if (info.RefKind != RefKind.Out)
                    {
                        yield return ExpressionStatement(
                            InvocationExpression(
                                ParseName("global::System.GC.KeepAlive"),
                                ArgumentList(SingletonSeparatedList(Argument(IdentifierName(managedIdentifier))))));
                    }
                    break;
                default:
                    break;
            }
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;

        public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context) => false;
    }
}
