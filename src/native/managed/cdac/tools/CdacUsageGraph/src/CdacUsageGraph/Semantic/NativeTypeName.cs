// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace CdacUsageGraph.Semantic;

internal static class NativeTypeName
{
    public static string FromType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol
            {
                OriginalDefinition.SpecialType: SpecialType.System_Nullable_T,
                TypeArguments: [ITypeSymbol underlying],
            })
        {
            type = underlying;
        }

        if (type.TypeKind == TypeKind.Enum &&
            type is INamedTypeSymbol { EnumUnderlyingType: ITypeSymbol underlyingType })
        {
            type = underlyingType;
        }

        return type.SpecialType switch
        {
            SpecialType.System_Boolean => "uint8",
            SpecialType.System_SByte => "int8",
            SpecialType.System_Byte => "uint8",
            SpecialType.System_Char => "uint16",
            SpecialType.System_Int16 => "int16",
            SpecialType.System_UInt16 => "uint16",
            SpecialType.System_Int32 => "int32",
            SpecialType.System_UInt32 => "uint32",
            SpecialType.System_Int64 => "int64",
            SpecialType.System_UInt64 => "uint64",
            SpecialType.System_IntPtr => "nint",
            SpecialType.System_UIntPtr => "nuint",
            SpecialType.System_String => "string",
            _ => FromNamedType(type),
        };
    }

    private static string FromNamedType(ITypeSymbol type) =>
        type.ToDisplayString() switch
        {
            "Microsoft.Diagnostics.DataContractReader.TargetPointer" => "pointer",
            "Microsoft.Diagnostics.DataContractReader.TargetNUInt" => "nuint",
            "Microsoft.Diagnostics.DataContractReader.TargetNInt" => "nint",
            "Microsoft.Diagnostics.DataContractReader.TargetCodePointer" => "CodePointer",
            _ => type.Name,
        };
}
