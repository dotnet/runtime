// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public sealed class PinnableManagedValueMarshaller : IMarshallingGenerator
    {
        private readonly IMarshallingGenerator _manualMarshallingGenerator;

        public PinnableManagedValueMarshaller(IMarshallingGenerator manualMarshallingGenerator)
        {
            _manualMarshallingGenerator = manualMarshallingGenerator;
        }

        public bool IsSupported(TargetFramework target, Version version)
            => _manualMarshallingGenerator.IsSupported(target, version);

        public ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context)
        {
            if (IsPinningPathSupported(info, context))
            {
                string identifier = context.GetIdentifiers(info).native;
                return Argument(CastExpression(AsNativeType(info), IdentifierName(identifier)));
            }
            return _manualMarshallingGenerator.AsArgument(info, context);
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _manualMarshallingGenerator.AsNativeType(info);
        }

        public ParameterSyntax AsParameter(TypePositionInfo info)
        {
            return _manualMarshallingGenerator.AsParameter(info);
        }

        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            if (IsPinningPathSupported(info, context))
            {
                return GeneratePinningPath(info, context);
            }
            return _manualMarshallingGenerator.Generate(info, context);
        }

        public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context)
        {
            return _manualMarshallingGenerator.SupportsByValueMarshalKind(marshalKind, context);
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            if (IsPinningPathSupported(info, context))
            {
                return false;
            }
            return _manualMarshallingGenerator.UsesNativeIdentifier(info, context);
        }
        private static bool IsPinningPathSupported(TypePositionInfo info, StubCodeContext context)
        {
            return context.SingleFrameSpansNativeContext && !info.IsByRef && !info.IsManagedReturnPosition;
        }

        private IEnumerable<StatementSyntax> GeneratePinningPath(TypePositionInfo info, StubCodeContext context)
        {
            if (context.CurrentStage == StubCodeContext.Stage.Pin)
            {
                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
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
