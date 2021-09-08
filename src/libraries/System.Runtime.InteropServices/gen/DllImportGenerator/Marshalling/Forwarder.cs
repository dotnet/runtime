using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal class Forwarder : IMarshallingGenerator, IAttributedReturnTypeMarshallingGenerator
    {
        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return info.ManagedType.Syntax;
        }

        private bool TryRehydrateMarshalAsAttribute(TypePositionInfo info, out AttributeSyntax marshalAsAttribute)
        {
            marshalAsAttribute = null!;
            // If the parameter has [MarshalAs] marshalling, we resurface that
            // in the forwarding target since the built-in system understands it.
            // ICustomMarshaller marshalling requires additional information that we throw away earlier since it's unsupported,
            // so explicitly do not resurface a [MarshalAs(UnmanagdType.CustomMarshaler)] attribute.
            if (info.MarshallingAttributeInfo is MarshalAsInfo { UnmanagedType: not UnmanagedType.CustomMarshaler } marshalAs)
            {
                marshalAsAttribute = Attribute(ParseName(TypeNames.System_Runtime_InteropServices_MarshalAsAttribute))
                        .WithArgumentList(AttributeArgumentList(SingletonSeparatedList(AttributeArgument(
                        CastExpression(ParseTypeName(TypeNames.System_Runtime_InteropServices_UnmanagedType),
                        LiteralExpression(SyntaxKind.NumericLiteralExpression,
                            Literal((int)marshalAs.UnmanagedType)))))));
                return true;
            }
            
            if (info.MarshallingAttributeInfo is NativeContiguousCollectionMarshallingInfo collectionMarshalling
                && collectionMarshalling.UseDefaultMarshalling
                && collectionMarshalling.ElementCountInfo is NoCountInfo or SizeAndParamIndexInfo
                && collectionMarshalling.ElementMarshallingInfo is NoMarshallingInfo or MarshalAsInfo { UnmanagedType: not UnmanagedType.CustomMarshaler }
                && info.ManagedType is IArrayTypeSymbol)
            {
                List<AttributeArgumentSyntax> marshalAsArguments = new List<AttributeArgumentSyntax>();
                marshalAsArguments.Add(
                    AttributeArgument(
                        CastExpression(ParseTypeName(TypeNames.System_Runtime_InteropServices_UnmanagedType),
                        LiteralExpression(SyntaxKind.NumericLiteralExpression,
                            Literal((int)UnmanagedType.LPArray))))
                    );

                if (collectionMarshalling.ElementCountInfo is SizeAndParamIndexInfo countInfo)
                {
                    if (countInfo.ConstSize != SizeAndParamIndexInfo.UnspecifiedConstSize)
                    {
                        marshalAsArguments.Add(
                            AttributeArgument(NameEquals("SizeConst"), null,
                                LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                    Literal(countInfo.ConstSize)))
                        );
                    }
                    if (countInfo.ParamAtIndex is { ManagedIndex: int paramIndex })
                    {
                        marshalAsArguments.Add(
                            AttributeArgument(NameEquals("SizeParamIndex"), null,
                                LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                    Literal(paramIndex)))
                        );
                    }
                }

                if (collectionMarshalling.ElementMarshallingInfo is MarshalAsInfo elementMarshalAs)
                {
                    marshalAsArguments.Add(
                        AttributeArgument(NameEquals("ArraySubType"), null,
                            CastExpression(ParseTypeName(TypeNames.System_Runtime_InteropServices_UnmanagedType),
                            LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                Literal((int)elementMarshalAs.UnmanagedType))))
                        );
                }
                marshalAsAttribute = Attribute(ParseName(TypeNames.System_Runtime_InteropServices_MarshalAsAttribute))
                        .WithArgumentList(AttributeArgumentList(SeparatedList(marshalAsArguments)));
                return true;
            }

            return false;
        }

        public ParameterSyntax AsParameter(TypePositionInfo info)
        {
            ParameterSyntax param = Parameter(Identifier(info.InstanceIdentifier))
                .WithModifiers(TokenList(Token(info.RefKindSyntax)))
                .WithType(info.ManagedType.Syntax);

            if (TryRehydrateMarshalAsAttribute(info, out AttributeSyntax marshalAsAttribute))
            {
                param = param.AddAttributeLists(AttributeList(SingletonSeparatedList(marshalAsAttribute)));
            }

            return param;
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
        
        public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context) => true;

        public AttributeListSyntax? GenerateAttributesForReturnType(TypePositionInfo info)
        {
            if (!TryRehydrateMarshalAsAttribute(info, out AttributeSyntax marshalAsAttribute))
            {
                return null;
            }
            return AttributeList(SingletonSeparatedList(marshalAsAttribute));
        }
    }
}
