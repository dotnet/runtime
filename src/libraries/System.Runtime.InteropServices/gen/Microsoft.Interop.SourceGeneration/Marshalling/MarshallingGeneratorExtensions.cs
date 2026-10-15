// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Interop
{
    public static class MarshallingGeneratorExtensions
    {
        private const string ParameterIdentifierSuffix = "param";

        /// <summary>
        /// Gets the return type for the unmanaged signature.
        /// </summary>
        public static string AsReturnType(this IBoundMarshallingGenerator generator)
        {
            return generator.NativeSignatureBehavior switch
            {
                SignatureBehavior.ManagedTypeAndAttributes => generator.TypeInfo.ManagedType.FullTypeName,
                SignatureBehavior.NativeType => generator.NativeType.FullTypeName,
                SignatureBehavior.PointerToNativeType => $"{generator.NativeType.FullTypeName}*",
                _ => throw new InvalidOperationException()
            };
        }

        /// <summary>
        /// Gets the attribute bodies for the unmanaged return type, without brackets or a return target.
        /// </summary>
        public static string? GenerateAttributesForReturnType(this IBoundMarshallingGenerator generator)
        {
            if (generator.NativeSignatureBehavior == SignatureBehavior.ManagedTypeAndAttributes
                && generator.TypeInfo.MarshallingAttributeInfo is IForwardedMarshallingInfo forwarded
                && forwarded.TryCreateAttribute(out string? attribute))
            {
                return attribute;
            }

            return null;
        }

        /// <summary>
        /// Gets a parameter for the unmanaged signature.
        /// </summary>
        public static GeneratedParameter AsParameter(this IBoundMarshallingGenerator generator, StubIdentifierContext context)
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

            string type = behavior switch
            {
                SignatureBehavior.NativeType => generator.NativeType.FullTypeName,
                SignatureBehavior.PointerToNativeType => $"{generator.NativeType.FullTypeName}*",
                _ => throw new InvalidOperationException()
            };
            return new GeneratedParameter(type, identifierName);
        }

        private static GeneratedParameter GenerateForwardingParameter(TypePositionInfo info, string identifier)
        {
            List<string> attributes = [];
            if (info.MarshallingAttributeInfo is IForwardedMarshallingInfo forwarded
                && forwarded.TryCreateAttribute(out string? attribute))
            {
                attributes.Add(attribute);
            }
            if (info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.In))
            {
                attributes.Add(TypeNames.GlobalAlias + TypeNames.System_Runtime_InteropServices_InAttribute);
            }
            if (info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                attributes.Add(TypeNames.GlobalAlias + TypeNames.System_Runtime_InteropServices_OutAttribute);
            }

            return new GeneratedParameter(
                info.ManagedType.FullTypeName,
                identifier,
                MarshallerHelpers.GetManagedParameterModifiers(info),
                attributes.Count == 0 ? null : string.Join(", ", attributes));
        }

        /// <summary>
        /// Gets an argument for passing a value across the managed/native boundary.
        /// </summary>
        public static string AsArgument(this IBoundMarshallingGenerator generator, StubIdentifierContext context)
        {
            TypePositionInfo info = generator.TypeInfo;
            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            return generator.ValueBoundaryBehavior switch
            {
                ValueBoundaryBehavior.ManagedIdentifier when !info.IsByRef => managedIdentifier,
                ValueBoundaryBehavior.ManagedIdentifier => $"{MarshallerHelpers.GetManagedArgumentRefKindKeyword(info)} {managedIdentifier}",
                ValueBoundaryBehavior.NativeIdentifier => nativeIdentifier,
                ValueBoundaryBehavior.AddressOfNativeIdentifier => $"&{nativeIdentifier}",
                ValueBoundaryBehavior.CastNativeIdentifier => $"({generator.AsParameter(context).Type}){nativeIdentifier}",
                _ => throw new InvalidOperationException()
            };
        }

        public static string AsManagedArgument(this IBoundMarshallingGenerator generator, StubIdentifierContext context)
        {
            TypePositionInfo info = generator.TypeInfo;
            string managedIdentifier = context.GetIdentifiers(info).managed;
            return info.IsByRef
                ? $"{MarshallerHelpers.GetManagedArgumentRefKindKeyword(info)} {managedIdentifier}"
                : managedIdentifier;
        }

        public static string GenerateNativeByRefInitialization(this IBoundMarshallingGenerator generator, StubIdentifierContext context)
        {
            string paramIdentifier = context.GetAdditionalIdentifier(generator.TypeInfo, ParameterIdentifierSuffix);
            return $"ref *{paramIdentifier}";
        }

        public static bool IsForwarder(this IBoundMarshallingGenerator generator) => generator is BoundMarshallingGenerator { IsForwarder: true };

        public static bool IsBlittable(this IBoundMarshallingGenerator generator) => generator is BoundMarshallingGenerator { IsBlittable: true };
    }
}
