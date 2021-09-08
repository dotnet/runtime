using System;
using System.Diagnostics;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal sealed class HResultExceptionMarshaller : IMarshallingGenerator
    {
        private static readonly TypeSyntax NativeType = PredefinedType(Token(SyntaxKind.IntKeyword));

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            Debug.Assert(info.ManagedType is SpecialTypeInfo(_, _, SpecialType.System_Int32));
            return NativeType;
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
