// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public sealed class Forwarder : IMarshallingGenerator, IAttributedReturnTypeMarshallingGenerator
    {
        public bool IsSupported(TargetFramework target, Version version) => true;

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

            if (info.ManagedType is SzArrayType)
            {
                CountInfo countInfo;
                MarshallingInfo elementMarshallingInfo;
                if (info.MarshallingAttributeInfo is NativeContiguousCollectionMarshallingInfo collectionMarshalling
                    && collectionMarshalling.UseDefaultMarshalling
                    && collectionMarshalling.ElementCountInfo is NoCountInfo or SizeAndParamIndexInfo
                    && collectionMarshalling.ElementMarshallingInfo is NoMarshallingInfo or MarshalAsInfo { UnmanagedType: not UnmanagedType.CustomMarshaler }
                    )
                {
                    countInfo = collectionMarshalling.ElementCountInfo;
                    elementMarshallingInfo = collectionMarshalling.ElementMarshallingInfo;
                }
                else if (info.MarshallingAttributeInfo is MissingSupportCollectionMarshallingInfo missingSupport)
                {
                    countInfo = missingSupport.CountInfo;
                    elementMarshallingInfo = missingSupport.ElementMarshallingInfo;
                }
                else
                {
                    // This condition can be hit in two ways:
                    // 1. User uses the MarshalUsing attribute to provide count info or element marshalling information.
                    // Since the MarshalUsing attribute doesn't exist on downlevel platforms where we don't support arrays,
                    // this case is unlikely to come in supported scenarios, but could come up with a custom CoreLib implementation
                    // 2. User provides a MarsalAs attribute with the ArraySubType field set to UnmanagedType.CustomMarshaler
                    // As mentioned above, we don't support ICustomMarshaler in the generator so we fail to forward the attribute instead of partially fowarding it.
                    return false;
                }

                List<AttributeArgumentSyntax> marshalAsArguments = new List<AttributeArgumentSyntax>
                {
                    AttributeArgument(
                        CastExpression(ParseTypeName(TypeNames.System_Runtime_InteropServices_UnmanagedType),
                        LiteralExpression(SyntaxKind.NumericLiteralExpression,
                            Literal((int)UnmanagedType.LPArray))))
                };

                if (countInfo is SizeAndParamIndexInfo sizeParamIndex)
                {
                    if (sizeParamIndex.ConstSize != SizeAndParamIndexInfo.UnspecifiedConstSize)
                    {
                        marshalAsArguments.Add(
                            AttributeArgument(NameEquals("SizeConst"), null,
                                LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                    Literal(sizeParamIndex.ConstSize)))
                        );
                    }
                    if (sizeParamIndex.ParamAtIndex is { ManagedIndex: int paramIndex })
                    {
                        marshalAsArguments.Add(
                            AttributeArgument(NameEquals("SizeParamIndex"), null,
                                LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                    Literal(paramIndex)))
                        );
                    }
                }

                if (elementMarshallingInfo is MarshalAsInfo elementMarshalAs)
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

            List<AttributeSyntax> rehydratedAttributes = new();
            if (TryRehydrateMarshalAsAttribute(info, out AttributeSyntax marshalAsAttribute))
            {
                rehydratedAttributes.Add(marshalAsAttribute);
            }
            if (info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.In))
            {
                rehydratedAttributes.Add(Attribute(IdentifierName(TypeNames.System_Runtime_InteropServices_InAttribute)));
            }
            if (info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                rehydratedAttributes.Add(Attribute(IdentifierName(TypeNames.System_Runtime_InteropServices_OutAttribute)));
            }

            if (rehydratedAttributes.Count > 0)
            {
                param = param.AddAttributeLists(AttributeList(SeparatedList(rehydratedAttributes)));
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
