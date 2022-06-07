// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public static class MarshallingGeneratorExtensions
    {
        /// <summary>
        /// Gets the return type for the unmanaged signature that represents the provided <paramref name="info"/>.
        /// </summary>
        /// <param name="generator">The marshalling generator for this <paramref name="info"/></param>
        /// <param name="info">Object to marshal</param>
        public static TypeSyntax AsReturnType(this IMarshallingGenerator generator, TypePositionInfo info)
        {
            return generator.GetNativeSignatureBehavior(info) switch
            {
                SignatureBehavior.ManagedTypeAndAttributes => info.ManagedType.Syntax,
                SignatureBehavior.NativeType => generator.AsNativeType(info),
                SignatureBehavior.PointerToNativeType => PointerType(generator.AsNativeType(info)),
                _ => throw new InvalidOperationException()
            };
        }
        /// <summary>
        /// Gets any attributes that should be applied to the return type for this <paramref name="info"/>.
        /// </summary>
        /// <param name="generator">The marshalling generator for this <paramref name="info"/></param>
        /// <param name="info">Object to marshal</param>
        /// <returns>Attributes for the return type for this <paramref name="info"/>, or <c>null</c> if no attributes should be added.</returns>
        public static AttributeListSyntax? GenerateAttributesForReturnType(this IMarshallingGenerator generator, TypePositionInfo info)
        {
            if (generator.GetNativeSignatureBehavior(info) != SignatureBehavior.ManagedTypeAndAttributes)
            {
                return null;
            }

            if (!TryRehydrateMarshalAsAttribute(info, out AttributeSyntax marshalAsAttribute))
            {
                return null;
            }
            return AttributeList(SingletonSeparatedList(marshalAsAttribute));
        }

        /// <summary>
        /// Gets a parameter for the unmanaged signature that represents the provided <paramref name="info"/>.
        /// </summary>
        /// <param name="generator">The marshalling generator for this <paramref name="info"/></param>
        /// <param name="info">Object to marshal</param>
        public static ParameterSyntax AsParameter(this IMarshallingGenerator generator, TypePositionInfo info)
        {
            SignatureBehavior behavior = generator.GetNativeSignatureBehavior(info);
            if (behavior == SignatureBehavior.ManagedTypeAndAttributes)
            {
                return GenerateForwardingParameter(info);
            }
            return Parameter(Identifier(info.InstanceIdentifier))
                .WithType(behavior switch
                {
                    SignatureBehavior.NativeType => generator.AsNativeType(info),
                    SignatureBehavior.PointerToNativeType => PointerType(generator.AsNativeType(info)),
                    _ => throw new InvalidOperationException()
                });
        }

        private static ParameterSyntax GenerateForwardingParameter(TypePositionInfo info)
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

        private static bool TryRehydrateMarshalAsAttribute(TypePositionInfo info, out AttributeSyntax marshalAsAttribute)
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
                if (info.MarshallingAttributeInfo is NativeLinearCollectionMarshallingInfo collectionMarshalling
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

        /// <summary>
        /// Gets an argument expression for the unmanaged signature that can be used to pass a value of the provided <paramref name="info" /> in the specified <paramref name="context" />.
        /// </summary>
        /// <param name="generator">The marshalling generator for this <paramref name="info"/></param>
        /// <param name="info">Object to marshal</param>
        /// <param name="context">Marshalling context</param>
        public static ArgumentSyntax AsArgument(this IMarshallingGenerator generator, TypePositionInfo info, StubCodeContext context)
        {
            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            return generator.GetValueBoundaryBehavior(info, context) switch
            {
                ValueBoundaryBehavior.ManagedIdentifier when !info.IsByRef => Argument(IdentifierName(managedIdentifier)),
                ValueBoundaryBehavior.ManagedIdentifier when info.IsByRef => Argument(IdentifierName(managedIdentifier)).WithRefKindKeyword(Token(info.RefKindSyntax)),
                ValueBoundaryBehavior.NativeIdentifier => Argument(IdentifierName(nativeIdentifier)),
                ValueBoundaryBehavior.AddressOfNativeIdentifier => Argument(PrefixUnaryExpression(SyntaxKind.AddressOfExpression, IdentifierName(nativeIdentifier))),
                ValueBoundaryBehavior.CastNativeIdentifier => Argument(CastExpression(generator.AsParameter(info).Type, IdentifierName(nativeIdentifier))),
                _ => throw new InvalidOperationException()
            };
        }
    }
}
