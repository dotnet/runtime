using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public sealed class DelegateMarshaller : IMarshallingGenerator
    {
        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return MarshallerHelpers.SystemIntPtrType;
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
                case StubCodeContext.Stage.KeepAlive:
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
