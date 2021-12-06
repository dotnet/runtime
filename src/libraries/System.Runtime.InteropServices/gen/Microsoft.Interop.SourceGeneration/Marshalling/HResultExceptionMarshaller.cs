// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public sealed class HResultExceptionMarshaller : IMarshallingGenerator
    {
        private static readonly TypeSyntax s_nativeType = PredefinedType(Token(SyntaxKind.IntKeyword));

        public bool IsSupported(TargetFramework target, Version version) => true;

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            Debug.Assert(info.ManagedType is SpecialTypeInfo(_, _, SpecialType.System_Int32));
            return s_nativeType;
        }

        // Should only be used for return value
        public ParameterSyntax AsParameter(TypePositionInfo info) => throw new InvalidOperationException();
        public ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context) => throw new InvalidOperationException();

        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            if (context.CurrentStage != StubCodeContext.Stage.Unmarshal)
                yield break;

            // Marshal.ThrowExceptionForHR(<managed>)
            string identifier = context.GetIdentifiers(info).managed;
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        MarshallerHelpers.InteropServicesMarshalType,
                        IdentifierName(nameof(System.Runtime.InteropServices.Marshal.ThrowExceptionForHR))),
                    ArgumentList(SingletonSeparatedList(
                        Argument(IdentifierName(identifier))))));
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => false;

        public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context) => false;
    }

}
