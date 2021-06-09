using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal sealed class PinnableManagedValueMarshaller : IMarshallingGenerator
    {
        private readonly IMarshallingGenerator manualMarshallingGenerator;

        public PinnableManagedValueMarshaller(IMarshallingGenerator manualMarshallingGenerator)
        {
            this.manualMarshallingGenerator = manualMarshallingGenerator;
        }

        public ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context)
        {
            if (IsPinningPathSupported(info, context))
            {
                string identifier = context.GetIdentifiers(info).native;
                return Argument(CastExpression(AsNativeType(info), IdentifierName(identifier)));
            }
            return manualMarshallingGenerator.AsArgument(info, context);
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return manualMarshallingGenerator.AsNativeType(info);
        }

        public ParameterSyntax AsParameter(TypePositionInfo info)
        {
            return manualMarshallingGenerator.AsParameter(info);
        }

        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            if (IsPinningPathSupported(info, context))
            {
                return GeneratePinningPath(info, context);
            }
            return manualMarshallingGenerator.Generate(info, context);
        }

        public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context)
        {
            return manualMarshallingGenerator.SupportsByValueMarshalKind(marshalKind, context);
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            if (IsPinningPathSupported(info, context))
            {
                return false;
            }
            return manualMarshallingGenerator.UsesNativeIdentifier(info, context);
        }
        private static bool IsPinningPathSupported(TypePositionInfo info, StubCodeContext context)
        {
            return context.PinningSupported && !info.IsByRef && !info.IsManagedReturnPosition;
        }

        private IEnumerable<StatementSyntax> GeneratePinningPath(TypePositionInfo info, StubCodeContext context)
        {
            if (context.CurrentStage == StubCodeContext.Stage.Pin)
            {
                var (managedIdentifier, nativeIdentifier) = context.GetIdentifiers(info);
                yield return FixedStatement(
                    VariableDeclaration(
                        PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                        SingletonSeparatedList(
                            VariableDeclarator(Identifier(nativeIdentifier))
                                .WithInitializer(EqualsValueClause(
                                    IdentifierName(managedIdentifier)
                                ))
                        )
                    ),
                    EmptyStatement()
                );
            }
        }
    }
}
