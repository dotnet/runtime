// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
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
            KnownAttributeKind.ComImport => AddTypeFlags(context, TypeAttributes.Import),
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
            KnownAttributeKind.SpecialName => ApplySpecialName(context),
            KnownAttributeKind.WindowsRuntimeImport => AddTypeFlags(context, TypeAttributes.WindowsRuntime),
            KnownAttributeKind.DynamicSecurityMethod => AddMethodFlags(context, MethodAttributes.RequireSecObject),
            KnownAttributeKind.SuppressUnmanagedCodeSecurity => ApplySuppressUnmanagedCodeSecurity(context),
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

    private static bool AddMethodFlags(LoweringContext context, MethodAttributes flags)
    {
        ((EntityRegistry.MethodDefinitionEntity)context.Owner).MethodAttributes |= flags;
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
                return context.InvalidTarget();
        }
    }

    private static bool ApplySuppressUnmanagedCodeSecurity(LoweringContext context)
    {
        switch (context.Owner)
        {
            case EntityRegistry.TypeDefinitionEntity type:
                type.Attributes |= TypeAttributes.HasSecurity;
                return true;
            case EntityRegistry.MethodDefinitionEntity method:
                method.MethodAttributes |= MethodAttributes.HasSecurity;
                return true;
            default:
                return context.InvalidTarget();
        }
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

        var field = (EntityRegistry.FieldDefinitionEntity)context.Owner;
        // SetClassLayout follows field attributes, but precedes deferred attributes.
        if (!field.HasExplicitOffset || context.IsDeferred)
        {
            field.Offset = (int)offset;
        }

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
        MethodImplAttributes fixedAttributes = 0;

        if (kind is not KnownAttributeKind.MethodImpl1)
        {
            object? fixedValue = arguments.FixedArguments[0].Value;
            int value = kind is KnownAttributeKind.MethodImpl2
                ? unchecked((ushort)GetInt16(fixedValue))
                : GetInt32(fixedValue);
            // MethodCodeType owns the low bits. All other bits are available for runtime experiments,
            // provided the value fits the two-byte MethodDef.ImplFlags column (ECMA-335 II.22.26).
            if ((uint)value > ushort.MaxValue || ((MethodImplAttributes)value & MethodImplAttributes.CodeTypeMask) != 0)
            {
                return context.InvalidValue();
            }

            fixedAttributes = (MethodImplAttributes)value;
        }

        MethodCodeType codeType = codeTypeArgument is { } argument
            ? (MethodCodeType)GetInt32(argument.Value)
            : MethodCodeType.IL;
        if (codeType is < MethodCodeType.IL or > MethodCodeType.Runtime)
        {
            return context.InvalidValue();
        }

        method.ImplementationAttributes |= fixedAttributes;
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
        LayoutKind layoutKind = (LayoutKind)(kind is KnownAttributeKind.StructLayout1
            ? unchecked((ushort)GetInt16(fixedValue))
            : GetInt32(fixedValue));

        TypeAttributes layout = layoutKind switch
        {
            LayoutKind.Sequential => TypeAttributes.SequentialLayout,
            LayoutKind.Extended => TypeAttributes.ExtendedLayout,
            LayoutKind.Explicit => TypeAttributes.ExplicitLayout,
            LayoutKind.Auto => TypeAttributes.AutoLayout,
            _ => (TypeAttributes)(-1),
        };

        if (layout == (TypeAttributes)(-1))
        {
            return context.InvalidValue();
        }

        TypeAttributes attributes = (type.Attributes & ~TypeAttributes.LayoutMask) | layout;
        int? packingSize = null;
        int? classSize = null;

        if (FindNamedArgument(arguments, StructLayoutPack) is { } packArgument)
        {
            uint pack = GetUInt32(packArgument.Value);
            if (pack > 128 || (pack & (pack - 1)) != 0)
            {
                return context.InvalidValue();
            }

            packingSize = (int)pack;
        }

        if (FindNamedArgument(arguments, StructLayoutSize) is { } sizeArgument)
        {
            uint size = GetUInt32(sizeArgument.Value);
            if (size > int.MaxValue)
            {
                return context.InvalidValue();
            }

            classSize = (int)size;
        }

        if (FindNamedArgument(arguments, StructLayoutCharSet) is { } charSetArgument)
        {
            switch ((CharSet)GetUInt32(charSetArgument.Value))
            {
                case CharSet.None:
                case CharSet.Ansi:
                    attributes = (attributes & ~TypeAttributes.StringFormatMask) | TypeAttributes.AnsiClass;
                    break;
                case CharSet.Unicode:
                    attributes = (attributes & ~TypeAttributes.StringFormatMask) | TypeAttributes.UnicodeClass;
                    break;
                case CharSet.Auto:
                    attributes = (attributes & ~TypeAttributes.StringFormatMask) | TypeAttributes.AutoClass;
                    break;
                default:
                    return context.InvalidValue();
            }
        }

        type.Attributes = attributes;
        // Explicit directives are emitted after pseudo custom attributes by native ilasm, so they
        // win regardless of source order. Otherwise, later attributes overwrite earlier ones.
        if (packingSize is not null && !type.HasExplicitPackingSize)
        {
            type.PackingSize = packingSize;
        }
        if (classSize is not null && !type.HasExplicitClassSize)
        {
            type.ClassSize = classSize;
        }

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
            flags = (CallingConvention)GetUInt32(callingConventionArgument.Value) switch
            {
                CallingConvention.Winapi => flags | MethodImportAttributes.CallingConventionWinApi,
                CallingConvention.Cdecl => flags | MethodImportAttributes.CallingConventionCDecl,
                CallingConvention.StdCall => flags | MethodImportAttributes.CallingConventionStdCall,
                CallingConvention.ThisCall => flags | MethodImportAttributes.CallingConventionThisCall,
                CallingConvention.FastCall => flags | MethodImportAttributes.CallingConventionFastCall,
                _ => flags | MethodImportAttributes.CallingConventionWinApi,
            };
        }
        else
        {
            flags |= MethodImportAttributes.CallingConventionWinApi;
        }

        if (FindNamedArgument(arguments, DllImportCharSet) is { } charSetArgument)
        {
            flags = (CharSet)GetUInt32(charSetArgument.Value) switch
            {
                CharSet.None => flags,
                CharSet.Ansi => flags | MethodImportAttributes.CharSetAnsi,
                CharSet.Unicode => flags | MethodImportAttributes.CharSetUnicode,
                CharSet.Auto => flags | MethodImportAttributes.CharSetAuto,
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
