// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;

namespace ILAssembler;

internal static partial class PseudoCustomAttributes
{

    private static bool ApplyMarshalAs(
        LoweringContext context,
        CustomAttributeValue<SerializationTypeCode> arguments)
    {
        var descriptor = new BlobBuilder();
        if (!TryBuildMarshallingDescriptor(context, arguments, descriptor))
        {
            return false;
        }

        switch (context.Owner)
        {
            case EntityRegistry.FieldDefinitionEntity field:
                field.MarshallingDescriptor = descriptor;
                return true;

            case EntityRegistry.ParameterEntity parameter:
                parameter.MarshallingDescriptor = descriptor;
                return true;

            case EntityRegistry.PropertyEntity property:
                ApplyMarshalAsToProperty(context.Registry, property, descriptor);
                return true;

            default:
                return context.InvalidTarget();
        }
    }

    /// <summary>
    /// Applies a property's marshalling descriptor to the getter's return parameter and to the
    /// setter's last parameter, matching the native emitter.
    /// </summary>
    private static void ApplyMarshalAsToProperty(
        EntityRegistry registry,
        EntityRegistry.PropertyEntity property,
        BlobBuilder descriptor)
    {
        EntityRegistry.TypeDefinitionEntity? containingType = FindContainingType(registry, property);

        foreach ((MethodSemanticsAttributes semantic, EntityRegistry.EntityBase accessor) in property.Accessors)
        {
            if (ResolveAccessor(registry, accessor, containingType) is not { } method)
            {
                continue;
            }

            int targetSequence;
            if (semantic == MethodSemanticsAttributes.Getter)
            {
                targetSequence = 0;
            }
            else if (semantic == MethodSemanticsAttributes.Setter)
            {
                if (!TryGetSignatureParameterCount(method.MethodSignature, out targetSequence) || targetSequence == 0)
                {
                    continue;
                }
            }
            else
            {
                continue;
            }

            foreach (EntityRegistry.ParameterEntity parameter in method.Parameters)
            {
                if (parameter.Sequence == targetSequence)
                {
                    // Each target gets its own builder so that the blobs stay independent.
                    var copy = new BlobBuilder();
                    descriptor.WriteContentTo(copy);
                    parameter.MarshallingDescriptor = copy;
                    break;
                }
            }
        }
    }

    private static EntityRegistry.TypeDefinitionEntity? FindContainingType(
        EntityRegistry registry,
        EntityRegistry.PropertyEntity property)
    {
        foreach (var entity in registry.GetSeenEntities(TableIndex.TypeDef))
        {
            if (entity is EntityRegistry.TypeDefinitionEntity type && type.Properties.Contains(property))
            {
                return type;
            }
        }

        return null;
    }

    /// <summary>
    /// Property accessors are recorded as member references, so they are resolved to their local
    /// method definitions the same way attribute owners are, falling back to a lookup in the
    /// declaring type when the reference does not carry one.
    /// </summary>
    private static EntityRegistry.MethodDefinitionEntity? ResolveAccessor(
        EntityRegistry registry,
        EntityRegistry.EntityBase accessor,
        EntityRegistry.TypeDefinitionEntity? containingType)
    {
        if (ResolveOwner(registry, accessor) is EntityRegistry.MethodDefinitionEntity resolved)
        {
            return resolved;
        }

        if (accessor is not EntityRegistry.MemberReferenceEntity memberReference || containingType is null)
        {
            return null;
        }

        foreach (EntityRegistry.MethodDefinitionEntity candidate in containingType.Methods)
        {
            if (candidate.Name == memberReference.Name
                && candidate.MethodSignature is { } signature
                && signature.ContentEquals(memberReference.Signature))
            {
                return candidate;
            }
        }

        return null;
    }

    private static unsafe bool TryGetSignatureParameterCount(BlobBuilder? signature, out int count)
    {
        count = 0;
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
                _ = reader.ReadByte();
                return reader.TryReadCompressedInteger(out count);
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }
    }

    private static bool TryBuildMarshallingDescriptor(
        LoweringContext context,
        CustomAttributeValue<SerializationTypeCode> arguments,
        BlobBuilder descriptor)
    {
        CustomAttributeNamedArgument<SerializationTypeCode>? arraySubTypeArgument =
            FindNamedArgument(arguments, MarshalArraySubType);
        CustomAttributeNamedArgument<SerializationTypeCode>? safeArraySubTypeArgument =
            FindNamedArgument(arguments, MarshalSafeArraySubType);
        CustomAttributeNamedArgument<SerializationTypeCode>? safeArrayUserDefinedSubTypeArgument =
            FindNamedArgument(arguments, MarshalSafeArrayUserDefinedSubType);
        CustomAttributeNamedArgument<SerializationTypeCode>? sizeParamIndexArgument =
            FindNamedArgument(arguments, MarshalSizeParamIndex);
        CustomAttributeNamedArgument<SerializationTypeCode>? sizeConstArgument =
            FindNamedArgument(arguments, MarshalSizeConst);
        CustomAttributeNamedArgument<SerializationTypeCode>? marshalTypeArgument =
            FindNamedArgument(arguments, MarshalType);
        CustomAttributeNamedArgument<SerializationTypeCode>? marshalTypeRefArgument =
            FindNamedArgument(arguments, MarshalTypeRef);
        CustomAttributeNamedArgument<SerializationTypeCode>? marshalCookieArgument =
            FindNamedArgument(arguments, MarshalCookie);
        CustomAttributeNamedArgument<SerializationTypeCode>? iidParameterIndexArgument =
            FindNamedArgument(arguments, MarshalIidParameterIndex);

        // For the I2 overload the value was read as a 16-bit quantity and is zero-extended here,
        // matching the native emitter which widens the I2 into a U4 before building the descriptor.
        int nativeType = GetInt32(arguments.FixedArguments[0].Value);
        if (!TryWriteCompressed(context, descriptor, nativeType))
        {
            return false;
        }

        switch (nativeType)
        {
            case (int)UnmanagedType.Interface:
            case (int)UnmanagedType.IUnknown:
            case (int)UnmanagedType.IDispatch:
                if (iidParameterIndexArgument is { } iidParameterIndex)
                {
                    int iidParameterIndexValue = GetInt32(iidParameterIndex.Value);
                    if (iidParameterIndexValue < 0)
                    {
                        return context.InvalidValue();
                    }

                    if (!TryWriteCompressed(context, descriptor, iidParameterIndexValue))
                    {
                        return false;
                    }
                }
                break;

            case (int)UnmanagedType.ByValArray:
                if (safeArraySubTypeArgument is not null || sizeParamIndexArgument is not null)
                {
                    return context.InvalidValue();
                }

                if (GetTarget(context.Owner) != CaTargets.FieldDef)
                {
                    return context.InvalidTarget();
                }

                if (sizeConstArgument is { } sizeConst)
                {
                    int sizeConstValue = GetInt32(sizeConst.Value);
                    if (sizeConstValue < 0)
                    {
                        return context.InvalidValue();
                    }

                    if (!TryWriteCompressed(context, descriptor, sizeConstValue))
                    {
                        return false;
                    }
                }
                else if (!TryWriteCompressed(context, descriptor, 1))
                {
                    return false;
                }

                if (arraySubTypeArgument is { } arraySubType
                    && !TryWriteCompressed(context, descriptor, GetInt32(arraySubType.Value)))
                {
                    return false;
                }
                break;

            case (int)UnmanagedType.ByValTStr:
                if (sizeConstArgument is not { } fixedStringSize)
                {
                    return context.InvalidValue();
                }

                if (arraySubTypeArgument is not null
                    || sizeParamIndexArgument is not null
                    || safeArraySubTypeArgument is not null)
                {
                    return context.InvalidValue();
                }

                if (GetTarget(context.Owner) != CaTargets.FieldDef)
                {
                    return context.InvalidTarget();
                }

                if (!TryWriteCompressed(context, descriptor, GetInt32(fixedStringSize.Value)))
                {
                    return false;
                }
                break;

            case int value when value == (int)UnmanagedType.ByValStr:
                if (GetTarget(context.Owner) != CaTargets.ParamDef)
                {
                    return context.InvalidTarget();
                }
                break;

            case (int)UnmanagedType.SafeArray:
                if (arraySubTypeArgument is not null
                    || sizeParamIndexArgument is not null
                    || sizeConstArgument is not null)
                {
                    return context.InvalidValue();
                }

                if (safeArraySubTypeArgument is { } safeArraySubType)
                {
                    int safeArraySubTypeValue = GetInt32(safeArraySubType.Value);
                    if (!TryWriteCompressed(context, descriptor, safeArraySubTypeValue))
                    {
                        return false;
                    }

                    if (safeArrayUserDefinedSubTypeArgument is { } userDefinedSubType)
                    {
                        if (safeArraySubTypeValue != (int)VarEnum.VT_RECORD
                            && safeArraySubTypeValue != (int)VarEnum.VT_DISPATCH
                            && safeArraySubTypeValue != (int)VarEnum.VT_UNKNOWN)
                        {
                            return context.InvalidValue();
                        }

                        if (!TryWriteCountedString(context, descriptor, GetString(userDefinedSubType.Value)))
                        {
                            return false;
                        }
                    }
                }
                break;

            case (int)UnmanagedType.LPArray:
                if (safeArraySubTypeArgument is not null)
                {
                    return context.InvalidValue();
                }

                if (arraySubTypeArgument is { } lpArraySubType)
                {
                    int arraySubTypeValue = GetInt32(lpArraySubType.Value);
                    if (arraySubTypeValue == (int)UnmanagedType.CustomMarshaler)
                    {
                        return context.InvalidValue();
                    }

                    if (!TryWriteCompressed(context, descriptor, arraySubTypeValue))
                    {
                        return false;
                    }
                }
                else if (!TryWriteCompressed(context, descriptor, (int)UnmanagedType.Max))
                {
                    return false;
                }

                if (sizeParamIndexArgument is { } sizeParamIndex)
                {
                    int sizeParamIndexValue = GetInt32(sizeParamIndex.Value);
                    if (sizeParamIndexValue < 0)
                    {
                        return context.InvalidValue();
                    }

                    if (!TryWriteCompressed(context, descriptor, GetInt16(sizeParamIndex.Value)))
                    {
                        return false;
                    }

                    if (sizeConstArgument is { } indexedSizeConst)
                    {
                        int sizeConstValue = GetInt32(indexedSizeConst.Value);
                        if (sizeConstValue < 0)
                        {
                            return context.InvalidValue();
                        }

                        if (!TryWriteCompressed(context, descriptor, sizeConstValue)
                            || !TryWriteCompressed(context, descriptor, UnmanagedType.ArraySizeParamIndexSpecified))
                        {
                            return false;
                        }
                    }
                }
                else if (sizeConstArgument is { } unindexedSizeConst)
                {
                    if (!TryWriteCompressed(context, descriptor, 0)
                        || !TryWriteCompressed(context, descriptor, GetInt32(unindexedSizeConst.Value))
                        || !TryWriteCompressed(context, descriptor, 0))
                    {
                        return false;
                    }
                }
                break;

            case (int)UnmanagedType.CustomMarshaler:
                if (marshalTypeArgument is null && marshalTypeRefArgument is null)
                {
                    return context.InvalidValue();
                }

                // Placeholders for the unmanaged type library GUID and the unmanaged type name.
                if (!TryWriteCompressed(context, descriptor, 0) || !TryWriteCompressed(context, descriptor, 0))
                {
                    return false;
                }

                string marshaler = marshalTypeArgument is { } marshalType
                    ? GetString(marshalType.Value)
                    : GetString(marshalTypeRefArgument!.Value.Value);
                string cookie = marshalCookieArgument is { } marshalCookie
                    ? GetString(marshalCookie.Value)
                    : "";

                if (!TryWriteCountedString(context, descriptor, marshaler)
                    || !TryWriteCountedString(context, descriptor, cookie))
                {
                    return false;
                }
                break;
        }

        return true;
    }

    private static bool TryWriteCompressed(LoweringContext context, BlobBuilder builder, int value)
    {
        if (value < 0 || value > 0x1FFFFFFF)
        {
            return context.InvalidBlob();
        }

        builder.WriteCompressedInteger(value);
        return true;
    }

    private static bool TryWriteCountedString(LoweringContext context, BlobBuilder builder, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        if (!TryWriteCompressed(context, builder, bytes.Length))
        {
            return false;
        }

        builder.WriteBytes(bytes);

        return true;
    }
}
