using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal class Forwarder : IMarshallingGenerator
    {
        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return info.ManagedType.AsTypeSyntax();
        }

        public ParameterSyntax AsParameter(TypePositionInfo info)
        {
            return Parameter(Identifier(info.InstanceIdentifier))
                .WithModifiers(TokenList(Token(info.RefKindSyntax)))
                .WithType(info.ManagedType.AsTypeSyntax());
        }

        public ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context)
        {
            return Argument(IdentifierName(info.InstanceIdentifier))
                .WithRefKindKeyword(Token(info.RefKindSyntax));
        }

        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => false;
    }
}
