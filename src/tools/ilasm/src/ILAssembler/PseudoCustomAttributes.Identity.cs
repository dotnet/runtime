// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace ILAssembler;

internal static partial class PseudoCustomAttributes
{

    private static bool TryGetAttributeTypeName(EntityRegistry.EntityBase constructor, out string @namespace, out string name)
    {
        switch (constructor)
        {
            case EntityRegistry.MemberReferenceEntity memberReference:
                switch (memberReference.Parent)
                {
                    case EntityRegistry.TypeReferenceEntity typeReference:
                        @namespace = typeReference.Namespace;
                        name = typeReference.Name;
                        return true;
                    case EntityRegistry.TypeDefinitionEntity typeDefinition:
                        @namespace = typeDefinition.Namespace;
                        name = typeDefinition.Name;
                        return true;
                }
                break;

            case EntityRegistry.MethodDefinitionEntity methodDefinition:
                @namespace = methodDefinition.ContainingType.Namespace;
                name = methodDefinition.ContainingType.Name;
                return true;
        }

        @namespace = "";
        name = "";
        return false;
    }

    private static BlobBuilder? GetConstructorSignature(EntityRegistry.EntityBase constructor) => constructor switch
    {
        EntityRegistry.MemberReferenceEntity memberReference => memberReference.Signature,
        EntityRegistry.MethodDefinitionEntity methodDefinition => methodDefinition.MethodSignature,
        _ => null
    };

    /// <summary>
    /// Resolves a type or member reference that designates an entity defined in this module to that
    /// entity. References are otherwise only resolved while metadata rows are written, which happens
    /// after this pass runs. Anything that does not designate a local entity is returned unchanged.
    /// </summary>
    private static EntityRegistry.EntityBase ResolveOwner(EntityRegistry registry, EntityRegistry.EntityBase owner)
    {
        switch (owner)
        {
            case EntityRegistry.TypeReferenceEntity typeReference:
                return registry.FindLocalTypeDefinition(typeReference) ?? owner;

            case EntityRegistry.MemberReferenceEntity memberReference:
                if (ResolveDeclaringType(registry, memberReference.Parent) is not { } declaringType)
                {
                    return owner;
                }

                byte[] signature = memberReference.Signature.ToArray();
                if (signature.Length == 0)
                {
                    return owner;
                }

                switch (new SignatureHeader(signature[0]).Kind)
                {
                    case SignatureKind.Method:
                        foreach (EntityRegistry.MethodDefinitionEntity method in declaringType.Methods)
                        {
                            if (method.Name == memberReference.Name
                                && method.MethodSignature is { } methodSignature
                                && methodSignature.ContentEquals(memberReference.Signature))
                            {
                                return method;
                            }
                        }
                        break;

                    case SignatureKind.Field:
                        foreach (EntityRegistry.FieldDefinitionEntity field in declaringType.Fields)
                        {
                            if (field.Name == memberReference.Name
                                && field.Signature.ContentEquals(memberReference.Signature))
                            {
                                return field;
                            }
                        }
                        break;
                }

                return owner;

            default:
                return owner;
        }
    }

    private static EntityRegistry.TypeDefinitionEntity? ResolveDeclaringType(
        EntityRegistry registry,
        EntityRegistry.EntityBase? declaringType) => declaringType switch
        {
            EntityRegistry.TypeDefinitionEntity typeDefinition => typeDefinition,
            EntityRegistry.TypeReferenceEntity typeReference => registry.FindLocalTypeDefinition(typeReference),
            _ => null,
        };

    private static KnownAttribute? TryFindKnownAttribute(EntityRegistry.EntityBase constructor, string @namespace, string name)
    {
        foreach (KnownAttribute candidate in s_knownAttributes)
        {
            if (candidate.Name != name || candidate.Namespace != @namespace)
            {
                continue;
            }

            if (candidate.MatchBySignature && !SignatureMatches(constructor, candidate))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    /// <summary>
    /// Discriminates between overloads of a known attribute by comparing the constructor signature's
    /// parameter count and element types against the descriptor's fixed arguments. The serialization
    /// type tags of the primitive types used by the known attributes coincide with their
    /// <c>ELEMENT_TYPE</c> values, so the tags can be compared directly.
    /// </summary>
    private static unsafe bool SignatureMatches(EntityRegistry.EntityBase constructor, KnownAttribute candidate)
    {
        BlobBuilder? signature = GetConstructorSignature(constructor);
        if (signature is null)
        {
            return false;
        }

        byte[] signatureBytes = signature.ToArray();
        fixed (byte* signaturePointer = signatureBytes)
        {
            var reader = new BlobReader(signaturePointer, signatureBytes.Length);
            try
            {
                // Calling convention, then parameter count.
                if (!reader.TryReadCompressedInteger(out _)
                    || !reader.TryReadCompressedInteger(out int parameterCount)
                    || parameterCount != candidate.FixedArguments.Length
                    // Skip the return type.
                    || !reader.TryReadCompressedInteger(out _))
                {
                    return false;
                }

                for (int i = 0; i < parameterCount; i++)
                {
                    if (!reader.TryReadCompressedInteger(out int element)
                        || (SerializationTypeCode)element != candidate.FixedArguments[i])
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }
    }
}
