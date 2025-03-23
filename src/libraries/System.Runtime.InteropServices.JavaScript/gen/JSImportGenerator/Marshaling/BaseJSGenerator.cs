// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.Interop.SyntaxFactoryExtensions;


namespace Microsoft.Interop.JavaScript
{
    internal abstract class BaseJSGenerator(TypePositionInfo info, StubCodeContext codeContext) : IBoundMarshallingGenerator
    {
        private static ValueTypeInfo JSMarshalerArgument = new ValueTypeInfo(Constants.JSMarshalerArgumentGlobal, Constants.JSMarshalerArgument, IsByRefLike: false);

        public TypePositionInfo TypeInfo => info;

        public StubCodeContext CodeContext => codeContext;

        public ManagedTypeInfo NativeType => JSMarshalerArgument;

        public SignatureBehavior NativeSignatureBehavior => TypeInfo.IsByRef ? SignatureBehavior.PointerToNativeType : SignatureBehavior.NativeType;

        public ValueBoundaryBehavior ValueBoundaryBehavior => TypeInfo.IsByRef ? ValueBoundaryBehavior.AddressOfNativeIdentifier : ValueBoundaryBehavior.NativeIdentifier;

        public virtual bool UsesNativeIdentifier => true;

        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, out GeneratorDiagnostic? diagnostic)
        {
            diagnostic = null;
            return ByValueMarshalKindSupport.NotSupported;
        }

        public virtual IEnumerable<StatementSyntax> Generate(StubIdentifierContext context)
        {
            MarshalDirection marshalDirection = MarshallerHelpers.GetMarshalDirection(TypeInfo, CodeContext);
            if (context.CurrentStage == StubIdentifierContext.Stage.Setup
                && marshalDirection == MarshalDirection.ManagedToUnmanaged
                && !TypeInfo.IsManagedReturnPosition)
            {
                var (_, js) = context.GetIdentifiers(TypeInfo);
                return [
                    ExpressionStatement(
                        MethodInvocation(TypeSyntaxes.System_Runtime_CompilerServices_Unsafe, IdentifierName("SkipInit"),
                                    Argument(IdentifierName(js))
                                    .WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))))
                ];
            }

            return [];
        }

        protected static IdentifierNameSyntax GetToManagedMethod(MarshalerType marshalerType)
        {
            switch (marshalerType)
            {
                case MarshalerType.BigInt64:
                    return IdentifierName(Constants.ToManagedBigMethod);
                default:
                    return IdentifierName(Constants.ToManagedMethod);
            }
        }

        protected static IdentifierNameSyntax GetToJSMethod(MarshalerType marshalerType)
        {
            switch (marshalerType)
            {
                case MarshalerType.BigInt64:
                    return IdentifierName(Constants.ToJSBigMethod);
                default:
                    return IdentifierName(Constants.ToJSMethod);
            }
        }
    }
}
