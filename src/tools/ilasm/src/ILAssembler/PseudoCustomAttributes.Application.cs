// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace ILAssembler;

internal static partial class PseudoCustomAttributes
{

    private static bool Apply(LoweringContext context, KnownAttribute known)
    {
        if ((known.Targets & GetTarget(context.Owner)) == 0)
        {
            return context.InvalidTarget();
        }

        if (!TryParseArguments(context, known, out CustomAttributeValue<SerializationTypeCode> arguments))
        {
            return false;
        }

        return known.Kind switch
        {
            KnownAttributeKind.DllImport => ApplyDllImport(context, arguments),
            KnownAttributeKind.Guid => ApplyGuid(context, arguments),
            KnownAttributeKind.ComImport => AddTypeFlags(context, TypeAttributes.Import),
            KnownAttributeKind.InterfaceType =>
                GetUInt16(arguments.FixedArguments[0].Value) < (ushort)ComInterfaceType.Last || context.InvalidValue(),
            KnownAttributeKind.ClassInterface =>
                GetUInt16(arguments.FixedArguments[0].Value) < (ushort)ClassInterfaceType.Last || context.InvalidValue(),
#pragma warning disable SYSLIB0050 // Formatter-based serialization APIs are obsolete.
            KnownAttributeKind.Serializable => AddTypeFlags(context, TypeAttributes.Serializable),
            KnownAttributeKind.NonSerialized => AddFieldFlags(context, FieldAttributes.NotSerialized),
#pragma warning restore SYSLIB0050 // Formatter-based serialization APIs are obsolete.
            KnownAttributeKind.MethodImpl1 or KnownAttributeKind.MethodImpl2 or KnownAttributeKind.MethodImpl3 =>
                ApplyMethodImpl(context, known.Kind, arguments),
            KnownAttributeKind.MarshalAs1 or KnownAttributeKind.MarshalAs2 => ApplyMarshalAs(context, arguments),
            KnownAttributeKind.PreserveSig => AddMethodImplFlags(context, MethodImplAttributes.PreserveSig),
            KnownAttributeKind.In => AddParameterFlags(context, ParameterAttributes.In),
            KnownAttributeKind.Out => AddParameterFlags(context, ParameterAttributes.Out),
            KnownAttributeKind.Optional => AddParameterFlags(context, ParameterAttributes.Optional),
            KnownAttributeKind.StructLayout1 or KnownAttributeKind.StructLayout2 =>
                ApplyStructLayout(context, known.Kind, arguments),
            KnownAttributeKind.FieldOffset => ApplyFieldOffset(context, arguments),
            KnownAttributeKind.TypeLibVersion or KnownAttributeKind.ComCompatibleVersion =>
                ValidateNonNegative(context, arguments.FixedArguments),
            KnownAttributeKind.SpecialName => ApplySpecialName(context),
            KnownAttributeKind.AllowPartiallyTrustedCallers => true,
            KnownAttributeKind.WindowsRuntimeImport => AddTypeFlags(context, TypeAttributes.WindowsRuntime),
            _ => true,
        };
    }

    private static bool AddTypeFlags(LoweringContext context, TypeAttributes flags)
    {
        ((EntityRegistry.TypeDefinitionEntity)context.Owner).Attributes |= flags;
        return true;
    }

    private static bool AddFieldFlags(LoweringContext context, FieldAttributes flags)
    {
        ((EntityRegistry.FieldDefinitionEntity)context.Owner).Attributes |= flags;
        return true;
    }

    private static bool AddParameterFlags(LoweringContext context, ParameterAttributes flags)
    {
        ((EntityRegistry.ParameterEntity)context.Owner).Attributes |= flags;
        return true;
    }

    private static bool AddMethodImplFlags(LoweringContext context, MethodImplAttributes flags)
    {
        ((EntityRegistry.MethodDefinitionEntity)context.Owner).ImplementationAttributes |= flags;
        return true;
    }

    private static bool ApplySpecialName(LoweringContext context)
    {
        switch (context.Owner)
        {
            case EntityRegistry.TypeDefinitionEntity type:
                type.Attributes |= TypeAttributes.SpecialName;
                return true;
            case EntityRegistry.MethodDefinitionEntity method:
                method.MethodAttributes |= MethodAttributes.SpecialName;
                return true;
            case EntityRegistry.FieldDefinitionEntity field:
                field.Attributes |= FieldAttributes.SpecialName;
                return true;
            case EntityRegistry.PropertyEntity property:
                property.Attributes |= PropertyAttributes.SpecialName;
                return true;
            case EntityRegistry.EventEntity @event:
                @event.Attributes |= EventAttributes.SpecialName;
                return true;
            default:
                return context.InvalidValue();
        }
    }

    private static bool ValidateNonNegative(
        LoweringContext context,
        ImmutableArray<CustomAttributeTypedArgument<SerializationTypeCode>> arguments)
    {
        foreach (CustomAttributeTypedArgument<SerializationTypeCode> argument in arguments)
        {
            if (GetInt32(argument.Value) < 0)
            {
                return context.InvalidValue();
            }
        }

        return true;
    }

    private static bool ApplyGuid(
        LoweringContext context,
        CustomAttributeValue<SerializationTypeCode> arguments)
    {
        // The value is only validated; the attribute itself is still emitted.
        string guid = GetString(arguments.FixedArguments[0].Value);
        return guid.Length == 36 && Guid.TryParseExact(guid, "D", out _)
            ? true
            : context.InvalidGuid();
    }

    private static bool ApplyFieldOffset(
        LoweringContext context,
        CustomAttributeValue<SerializationTypeCode> arguments)
    {
        uint offset = GetUInt32(arguments.FixedArguments[0].Value);
        if (offset > int.MaxValue)
        {
            return context.InvalidValue();
        }

        ((EntityRegistry.FieldDefinitionEntity)context.Owner).Offset = (int)offset;
        return true;
    }

    private static bool ApplyMethodImpl(
        LoweringContext context,
        KnownAttributeKind kind,
        CustomAttributeValue<SerializationTypeCode> arguments)
    {
        var method = (EntityRegistry.MethodDefinitionEntity)context.Owner;
        CustomAttributeNamedArgument<SerializationTypeCode>? codeTypeArgument =
            FindNamedArgument(arguments, MethodImplCodeType);

        if (kind is not KnownAttributeKind.MethodImpl1)
        {
            // The I2 overload is widened before validation, matching the native emitter.
            object? fixedValue = arguments.FixedArguments[0].Value;
            ushort value = kind is KnownAttributeKind.MethodImpl2
                ? unchecked((ushort)GetInt16(fixedValue))
                : GetUInt16(fixedValue);
            if (((MethodImplAttributes)value & ~MethodImplAttributes.UserMask) != 0)
            {
                return context.InvalidValue();
            }

            method.ImplementationAttributes |= (MethodImplAttributes)value;

            if (codeTypeArgument is null)
            {
                return true;
            }
        }

        ushort codeType = codeTypeArgument is { } argument ? GetUInt16(argument.Value) : (ushort)0;
        if ((codeType & ~(ushort)MethodImplAttributes.CodeTypeMask) != 0)
        {
            return context.InvalidValue();
        }

        method.ImplementationAttributes =
            (method.ImplementationAttributes & ~MethodImplAttributes.CodeTypeMask) | (MethodImplAttributes)codeType;
        return true;
    }

    private static bool ApplyStructLayout(
        LoweringContext context,
        KnownAttributeKind kind,
        CustomAttributeValue<SerializationTypeCode> arguments)
    {
        var type = (EntityRegistry.TypeDefinitionEntity)context.Owner;

        // The I2 overload is zero-extended through 16 bits before the layout kind is read.
        object? fixedValue = arguments.FixedArguments[0].Value;
        int layoutKind = kind is KnownAttributeKind.StructLayout1
            ? unchecked((ushort)GetInt16(fixedValue))
            : GetInt32(fixedValue);

        TypeAttributes layout = layoutKind switch
        {
            0 => TypeAttributes.SequentialLayout,
            1 => TypeAttributes.ExtendedLayout,
            2 => TypeAttributes.ExplicitLayout,
            3 => TypeAttributes.AutoLayout,
            _ => (TypeAttributes)(-1),
        };

        if (layout == (TypeAttributes)(-1))
        {
            return context.InvalidValue();
        }

        TypeAttributes attributes = (type.Attributes & ~TypeAttributes.LayoutMask) | layout;

        if (FindNamedArgument(arguments, StructLayoutPack) is { } packArgument)
        {
            uint pack = GetUInt32(packArgument.Value);
            if (pack > 128 || (pack & (pack - 1)) != 0)
            {
                return context.InvalidValue();
            }

            // An explicit .pack directive wins: the native assembler emits the ClassLayout row for
            // explicit directives in a later phase than the one that applies this attribute.
            type.PackingSize ??= (int)pack;
        }

        if (FindNamedArgument(arguments, StructLayoutSize) is { } sizeArgument)
        {
            uint size = GetUInt32(sizeArgument.Value);
            if (size > int.MaxValue)
            {
                return context.InvalidValue();
            }

            // An explicit .size directive wins, for the same reason as .pack above.
            type.ClassSize ??= (int)size;
        }

        if (FindNamedArgument(arguments, StructLayoutCharSet) is { } charSetArgument)
        {
            switch (GetUInt32(charSetArgument.Value))
            {
                case 2:
                    attributes = (attributes & ~TypeAttributes.StringFormatMask) | TypeAttributes.AnsiClass;
                    break;
                case 3:
                    attributes = (attributes & ~TypeAttributes.StringFormatMask) | TypeAttributes.UnicodeClass;
                    break;
                case 4:
                    attributes = (attributes & ~TypeAttributes.StringFormatMask) | TypeAttributes.AutoClass;
                    break;
                default:
                    return context.InvalidValue();
            }
        }

        type.Attributes = attributes;
        return true;
    }

    private static bool ApplyDllImport(
        LoweringContext context,
        CustomAttributeValue<SerializationTypeCode> arguments)
    {
        var method = (EntityRegistry.MethodDefinitionEntity)context.Owner;

        if (arguments.FixedArguments[0].Value is not string moduleName || moduleName.Length == 0)
        {
            return context.InvalidValue();
        }

        MethodImportAttributes flags = MethodImportAttributes.None;

        if (FindNamedArgument(arguments, DllImportCallingConvention) is { } callingConventionArgument)
        {
            flags = GetUInt32(callingConventionArgument.Value) switch
            {
                0 => flags,
                1 => flags | MethodImportAttributes.CallingConventionWinApi,
                2 => flags | MethodImportAttributes.CallingConventionCDecl,
                3 => flags | MethodImportAttributes.CallingConventionStdCall,
                4 => flags | MethodImportAttributes.CallingConventionThisCall,
                5 => flags | MethodImportAttributes.CallingConventionFastCall,
                _ => flags,
            };
        }
        else
        {
            flags |= MethodImportAttributes.CallingConventionWinApi;
        }

        if (FindNamedArgument(arguments, DllImportCharSet) is { } charSetArgument)
        {
            flags = GetUInt32(charSetArgument.Value) switch
            {
                // 0 means "do nothing" and 1 is "not specified", which is the zero bit pattern.
                0 or 1 => flags,
                2 => flags | MethodImportAttributes.CharSetAnsi,
                3 => flags | MethodImportAttributes.CharSetUnicode,
                4 => flags | MethodImportAttributes.CharSetAuto,
                _ => flags,
            };
        }

        if (FindNamedArgument(arguments, DllImportExactSpelling) is { } exactSpellingArgument
            && GetBoolean(exactSpellingArgument.Value))
        {
            flags |= MethodImportAttributes.ExactSpelling;
        }

        if (FindNamedArgument(arguments, DllImportSetLastError) is { } setLastErrorArgument
            && GetBoolean(setLastErrorArgument.Value))
        {
            flags |= MethodImportAttributes.SetLastError;
        }

        if (FindNamedArgument(arguments, DllImportBestFitMapping) is { } bestFitMappingArgument)
        {
            flags |= GetBoolean(bestFitMappingArgument.Value)
                ? MethodImportAttributes.BestFitMappingEnable
                : MethodImportAttributes.BestFitMappingDisable;
        }

        if (FindNamedArgument(arguments, DllImportThrowOnUnmappableChar) is { } throwOnUnmappableCharArgument)
        {
            flags |= GetBoolean(throwOnUnmappableCharArgument.Value)
                ? MethodImportAttributes.ThrowOnUnmappableCharEnable
                : MethodImportAttributes.ThrowOnUnmappableCharDisable;
        }

        // PreserveSig defaults to set, and is only cleared by an explicit false value.
        if (FindNamedArgument(arguments, DllImportPreserveSig) is { } preserveSigArgument
            && !GetBoolean(preserveSigArgument.Value))
        {
            method.ImplementationAttributes &= ~MethodImplAttributes.PreserveSig;
        }
        else
        {
            method.ImplementationAttributes |= MethodImplAttributes.PreserveSig;
        }

        string entryPoint = FindNamedArgument(arguments, DllImportEntryPoint) is { } entryPointArgument
            ? GetString(entryPointArgument.Value)
            : method.Name;

        // The module reference is created even when an explicit pinvokeimpl clause takes precedence,
        // because the native emitter resolves it before it discovers the existing ImplMap row.
        var moduleReference = context.Registry.GetOrCreateModuleReference(moduleName, _ => { });

        // An explicit pinvokeimpl clause wins: the native assembler emits the ImplMap row for
        // explicit clauses in a later phase than the one that applies this attribute.
        method.MethodImportInformation ??= (moduleReference, entryPoint, flags);

        return true;
    }
}
