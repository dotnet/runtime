﻿// Licensed to the .NET Foundation under one or more agreements.
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
        public static TypeSyntax AsReturnType(this IBoundMarshallingGenerator generator)
        {
            return generator.NativeSignatureBehavior switch
            {
                SignatureBehavior.ManagedTypeAndAttributes => generator.TypeInfo.ManagedType.Syntax,
                SignatureBehavior.NativeType => generator.NativeType.Syntax,
                SignatureBehavior.PointerToNativeType => PointerType(generator.NativeType.Syntax),
                _ => throw new InvalidOperationException()
            };
        }
        /// <summary>
        /// Gets any attributes that should be applied to the return type for this <paramref name="info"/>.
        /// </summary>
        /// <param name="generator">The marshalling generator for this <paramref name="info"/></param>
        /// <param name="info">Object to marshal</param>
        /// <returns>Attributes for the return type for this <paramref name="info"/>, or <c>null</c> if no attributes should be added.</returns>
        public static AttributeListSyntax? GenerateAttributesForReturnType(this IBoundMarshallingGenerator generator)
        {
            if (generator.NativeSignatureBehavior != SignatureBehavior.ManagedTypeAndAttributes)
            {
                return null;
            }

            if (generator.TypeInfo.MarshallingAttributeInfo is IForwardedMarshallingInfo forwarded
                && forwarded.TryCreateAttributeSyntax(out AttributeSyntax forwardedAttribute))
            {
                return AttributeList(SingletonSeparatedList(forwardedAttribute));
            }

            return null;
        }

        private const string ParameterIdentifierSuffix = "param";

        /// <summary>
        /// Gets a parameter for the unmanaged signature that represents the provided <paramref name="info"/> in the given <paramref name="context"/>.
        /// </summary>
        /// <param name="generator">The marshalling generator for this <paramref name="info"/></param>
        /// <param name="context">The stub marshalling context</param>
        public static ParameterSyntax AsParameter(this IBoundMarshallingGenerator generator, StubIdentifierContext context)
        {
            SignatureBehavior behavior = generator.NativeSignatureBehavior;
            if (behavior == SignatureBehavior.ManagedTypeAndAttributes)
            {
                return GenerateForwardingParameter(generator.TypeInfo, context.GetIdentifiers(generator.TypeInfo).managed);
            }
            string identifierName;
            if (generator.CodeContext.Direction == MarshalDirection.ManagedToUnmanaged)
            {
                // This name doesn't get introduced into the stub's scope, so we can make it pretty
                // and reuse the native identifier
                identifierName = context.GetIdentifiers(generator.TypeInfo).native;
            }
            else if (generator.CodeContext.Direction == MarshalDirection.UnmanagedToManaged)
            {
                // This name is introduced into the stub's scope.
                // When we are passing the managed identifier as-is, we can just use that name everywhere.
                // When we're passing the native identifier as-is or casting the value to the native type in managed->unmanaged cases,
                // we can use the native identifier.
                // When we're passing the address of the native identifier, we need to introduce a new name to hold this value
                // before we assign it to the managed value.
                (string managed, string native) = context.GetIdentifiers(generator.TypeInfo);
                string param = context.GetAdditionalIdentifier(generator.TypeInfo, ParameterIdentifierSuffix);
                identifierName = generator.ValueBoundaryBehavior switch
                {
                    ValueBoundaryBehavior.ManagedIdentifier => generator.TypeInfo.IsByRef ? param : managed,
                    ValueBoundaryBehavior.NativeIdentifier or ValueBoundaryBehavior.CastNativeIdentifier => native,
                    ValueBoundaryBehavior.AddressOfNativeIdentifier => param,
                    _ => throw new UnreachableException()
                };
            }
            else
            {
                throw new ArgumentException("Context direction must be ManagedToUnmanaged or UnmanagedToManaged");
            }
            return Parameter(Identifier(identifierName))
                .WithType(behavior switch
                {
                    SignatureBehavior.NativeType => generator.NativeType.Syntax,
                    SignatureBehavior.PointerToNativeType => PointerType(generator.NativeType.Syntax),
                    _ => throw new InvalidOperationException()
                });
        }

        private static ParameterSyntax GenerateForwardingParameter(TypePositionInfo info, string identifier)
        {
            ParameterSyntax param = Parameter(Identifier(identifier))
                .WithModifiers(MarshallerHelpers.GetManagedParameterModifiers(info))
                .WithType(info.ManagedType.Syntax);

            List<AttributeSyntax> rehydratedAttributes = new();
            if (info.MarshallingAttributeInfo is IForwardedMarshallingInfo forwardedMarshallingInfo
                && forwardedMarshallingInfo.TryCreateAttributeSyntax(out AttributeSyntax forwardedAttribute))
            {
                rehydratedAttributes.Add(forwardedAttribute);
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

        /// <summary>
        /// Gets an argument expression for the unmanaged signature that can be used to pass a value of the provided <paramref name="info" /> in the specified <paramref name="context" />.
        /// </summary>
        /// <param name="generator">The marshalling generator for this <paramref name="info"/></param>
        /// <param name="info">Object to marshal</param>
        /// <param name="context">Marshalling context</param>
        public static ArgumentSyntax AsArgument(this IBoundMarshallingGenerator generator, StubIdentifierContext context)
        {
            TypePositionInfo info = generator.TypeInfo;
            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            return generator.ValueBoundaryBehavior switch
            {
                ValueBoundaryBehavior.ManagedIdentifier when !info.IsByRef => Argument(IdentifierName(managedIdentifier)),
                ValueBoundaryBehavior.ManagedIdentifier when info.IsByRef => Argument(IdentifierName(managedIdentifier)).WithRefKindKeyword(MarshallerHelpers.GetManagedArgumentRefKindKeyword(info)),
                ValueBoundaryBehavior.NativeIdentifier => Argument(IdentifierName(nativeIdentifier)),
                ValueBoundaryBehavior.AddressOfNativeIdentifier => Argument(PrefixUnaryExpression(SyntaxKind.AddressOfExpression, IdentifierName(nativeIdentifier))),
                ValueBoundaryBehavior.CastNativeIdentifier => Argument(CastExpression(generator.AsParameter(context).Type, IdentifierName(nativeIdentifier))),
                _ => throw new InvalidOperationException()
            };
        }

        public static ArgumentSyntax AsManagedArgument(this IBoundMarshallingGenerator generator, StubIdentifierContext context)
        {
            TypePositionInfo info = generator.TypeInfo;
            var (managedIdentifier, _) = context.GetIdentifiers(info);
            if (info.IsByRef)
            {
                return Argument(IdentifierName(managedIdentifier)).WithRefKindKeyword(MarshallerHelpers.GetManagedArgumentRefKindKeyword(info));
            }
            return Argument(IdentifierName(managedIdentifier));
        }

        public static ExpressionSyntax GenerateNativeByRefInitialization(this IBoundMarshallingGenerator generator, StubIdentifierContext context)
        {
            TypePositionInfo info = generator.TypeInfo;
            string paramIdentifier = context.GetAdditionalIdentifier(info, ParameterIdentifierSuffix);
            return RefExpression(PrefixUnaryExpression(SyntaxKind.PointerIndirectionExpression, IdentifierName(paramIdentifier)));
        }

        public static bool IsForwarder(this IBoundMarshallingGenerator generator) => generator is BoundMarshallingGenerator { IsForwarder: true };

        public static bool IsBlittable(this IBoundMarshallingGenerator generator) => generator is BoundMarshallingGenerator { IsBlittable: true };
    }
}
