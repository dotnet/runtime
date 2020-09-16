using System.Collections.Generic;
using System.Runtime.InteropServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal class BoolMarshaller : IMarshallingGenerator
    {
        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            var syntax = SyntaxKind.ByteKeyword;
            if (info.MarshalAsInfo != null)
            {
                syntax = info.MarshalAsInfo.UnmanagedType switch
                {
                    UnmanagedType.Bool => SyntaxKind.IntKeyword,
                    UnmanagedType.U1 => SyntaxKind.ByteKeyword,
                    UnmanagedType.I1 => SyntaxKind.SByteKeyword,
                    UnmanagedType.VariantBool => SyntaxKind.ShortKeyword,
                    _ => SyntaxKind.ByteKeyword
                };
            }

            return PredefinedType(Token(syntax));
        }

        public ParameterSyntax AsParameter(TypePositionInfo info)
        {
            var type = info.IsByRef
                ? PointerType(AsNativeType(info))
                : AsNativeType(info);
            return Parameter(Identifier(info.InstanceIdentifier))
                .WithType(type);
        }

        public ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context)
        {
            string identifier = context.GetIdentifiers(info).native;
            if (info.IsByRef)
            {
                return Argument(
                    PrefixUnaryExpression(
                        SyntaxKind.AddressOfExpression,
                        IdentifierName(identifier)));
            }

            return Argument(IdentifierName(identifier));
        }

        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            switch (context.CurrentStage)
            {
                case StubCodeContext.Stage.Setup:
                    if (info.IsManagedReturnPosition)
                        nativeIdentifier = context.GenerateReturnNativeIdentifier();

                    yield return LocalDeclarationStatement(
                        VariableDeclaration(
                            AsNativeType(info),
                            SingletonSeparatedList(VariableDeclarator(nativeIdentifier))));

                    break;
                case StubCodeContext.Stage.Marshal:
                    // <nativeIdentifier> = (<nativeType>)(<managedIdentifier> ? 1 : 0);
                    if (info.RefKind != RefKind.Out)
                    {
                        yield return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(nativeIdentifier),
                                CastExpression(
                                    AsNativeType(info),
                                    ParenthesizedExpression(
                                        ConditionalExpression(IdentifierName(managedIdentifier),
                                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)),
                                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))))));
                    }

                    break;
                case StubCodeContext.Stage.Unmarshal:
                    if (info.IsManagedReturnPosition || info.IsByRef)
                    {
                        // <managedIdentifier> = <nativeIdentifier> != 0;
                        yield return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(managedIdentifier),
                                BinaryExpression(
                                    SyntaxKind.NotEqualsExpression,
                                    IdentifierName(nativeIdentifier),
                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))));
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
