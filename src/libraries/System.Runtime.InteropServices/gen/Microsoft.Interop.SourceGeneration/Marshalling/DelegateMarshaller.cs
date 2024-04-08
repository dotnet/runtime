// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.Interop.SyntaxFactoryExtensions;

namespace Microsoft.Interop
{
    public sealed class DelegateMarshaller : IMarshallingGenerator
    {
        public ManagedTypeInfo AsNativeType(TypePositionInfo info)
        {
            return SpecialTypeInfo.IntPtr;
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
            MarshalDirection elementMarshalDirection = MarshallerHelpers.GetMarshalDirection(info, context);
            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            switch (context.CurrentStage)
            {
                case StubCodeContext.Stage.Setup:
                    break;
                case StubCodeContext.Stage.Marshal:
                    if (elementMarshalDirection is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
                    {
                        // <nativeIdentifier> = <managedIdentifier> != null ? Marshal.GetFunctionPointerForDelegate(<managedIdentifier>) : default;
                        yield return AssignmentStatement(
                                IdentifierName(nativeIdentifier),
                                ConditionalExpression(
                                    BinaryExpression(
                                        SyntaxKind.NotEqualsExpression,
                                        IdentifierName(managedIdentifier),
                                        LiteralExpression(SyntaxKind.NullLiteralExpression)
                                    ),
                                    MethodInvocation(
                                            TypeSyntaxes.System_Runtime_InteropServices_Marshal,
                                            IdentifierName("GetFunctionPointerForDelegate"),
                                        Argument(IdentifierName(managedIdentifier))),
                                    LiteralExpression(SyntaxKind.DefaultLiteralExpression)));
                    }
                    break;
                case StubCodeContext.Stage.Unmarshal:
                    if (elementMarshalDirection is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional)
                    {
                        // <managedIdentifier> = <nativeIdentifier> != default : Marshal.GetDelegateForFunctionPointer<<managedType>>(<nativeIdentifier>) : null;
                        yield return AssignmentStatement(
                                IdentifierName(managedIdentifier),
                                ConditionalExpression(
                                    BinaryExpression(
                                        SyntaxKind.NotEqualsExpression,
                                        IdentifierName(nativeIdentifier),
                                        LiteralExpression(SyntaxKind.DefaultLiteralExpression)),
                                    MethodInvocation(
                                            TypeSyntaxes.System_Runtime_InteropServices_Marshal,
                                            GenericName(Identifier("GetDelegateForFunctionPointer"))
                                            .WithTypeArgumentList(
                                                TypeArgumentList(
                                                    SingletonSeparatedList(
                                                        info.ManagedType.Syntax))),
                                        Argument(IdentifierName(nativeIdentifier))),
                                    LiteralExpression(SyntaxKind.NullLiteralExpression)));
                    }
                    break;
                case StubCodeContext.Stage.NotifyForSuccessfulInvoke:
                    if (elementMarshalDirection is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
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

        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, StubCodeContext context, out GeneratorDiagnostic? diagnostic)
            => ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, context, out diagnostic);
    }
}
