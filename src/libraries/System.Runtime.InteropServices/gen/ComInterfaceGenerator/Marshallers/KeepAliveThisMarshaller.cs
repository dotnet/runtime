// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.Interop.SyntaxFactoryExtensions;

namespace Microsoft.Interop
{
    internal sealed class KeepAliveThisMarshaller : IUnboundMarshallingGenerator
    {
        public static readonly KeepAliveThisMarshaller Instance = new();

        public ManagedTypeInfo AsNativeType(TypePositionInfo info) => info.ManagedType;
        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            if (context.CurrentStage != StubCodeContext.Stage.NotifyForSuccessfulInvoke)
            {
                return [];
            }

            return [
                MethodInvocationStatement(
                    TypeSyntaxes.System_GC,
                    IdentifierName("KeepAlive"),
                    Argument(ThisExpression()))
                ];
        }

        public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info) => SignatureBehavior.NativeType;
        public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context) => ValueBoundaryBehavior.ManagedIdentifier;
        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, StubCodeContext context, out GeneratorDiagnostic? diagnostic)
            => ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, context, out diagnostic);

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => false;
    }
}
