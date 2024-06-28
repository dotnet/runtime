// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace System.Formats.Nrbf.Utils;

internal static class TypeNameHelpers
{
    // PrimitiveType does not define Object, IntPtr or UIntPtr
    internal const PrimitiveType ObjectPrimitiveType = (PrimitiveType)19;
    internal const PrimitiveType IntPtrPrimitiveType = (PrimitiveType)20;
    internal const PrimitiveType UIntPtrPrimitiveType = (PrimitiveType)21;
    private static readonly TypeName?[] s_PrimitiveTypeNames = new TypeName?[(int)UIntPtrPrimitiveType + 1];
    private static readonly TypeName?[] s_PrimitiveSZArrayTypeNames = new TypeName?[(int)UIntPtrPrimitiveType + 1];
    private static AssemblyNameInfo? s_CoreLibAssemblyName;

    internal static TypeName GetPrimitiveTypeName(PrimitiveType primitiveType)
    {
        Debug.Assert(primitiveType is not (PrimitiveType.None or PrimitiveType.Null));

        TypeName? typeName = s_PrimitiveTypeNames[(int)primitiveType];
        if (typeName is null)
        {
            string fullName = primitiveType switch
            {
                PrimitiveType.Boolean => "System.Boolean",
                PrimitiveType.Byte => "System.Byte",
                PrimitiveType.SByte => "System.SByte",
                PrimitiveType.Char => "System.Char",
                PrimitiveType.Int16 => "System.Int16",
                PrimitiveType.UInt16 => "System.UInt16",
                PrimitiveType.Int32 => "System.Int32",
                PrimitiveType.UInt32 => "System.UInt32",
                PrimitiveType.Int64 => "System.Int64",
                PrimitiveType.Single => "System.Single",
                PrimitiveType.Double => "System.Double",
                PrimitiveType.Decimal => "System.Decimal",
                PrimitiveType.TimeSpan => "System.TimeSpan",
                PrimitiveType.DateTime => "System.DateTime",
                PrimitiveType.String => "System.String",
                ObjectPrimitiveType => "System.Object",
                IntPtrPrimitiveType => "System.IntPtr",
                UIntPtrPrimitiveType => "System.UIntPtr",
                _ => "System.UInt64",
            };

            s_PrimitiveTypeNames[(int)primitiveType] = typeName = TypeName.Parse(fullName.AsSpan()).WithCoreLibAssemblyName();
        }
        return typeName;
    }

    internal static TypeName GetPrimitiveSZArrayTypeName(PrimitiveType primitiveType)
    {
        TypeName? typeName = s_PrimitiveSZArrayTypeNames[(int)primitiveType];
        if (typeName is null)
        {
            s_PrimitiveSZArrayTypeNames[(int)primitiveType] = typeName = GetPrimitiveTypeName(primitiveType).MakeArrayTypeName();
        }
        return typeName;
    }

    internal static PrimitiveType GetPrimitiveType<T>()
    {
        if (typeof(T) == typeof(bool)) return PrimitiveType.Boolean;
        else if (typeof(T) == typeof(byte)) return PrimitiveType.Byte;
        else if (typeof(T) == typeof(sbyte)) return PrimitiveType.SByte;
        else if (typeof(T) == typeof(char)) return PrimitiveType.Char;
        else if (typeof(T) == typeof(short)) return PrimitiveType.Int16;
        else if (typeof(T) == typeof(ushort)) return PrimitiveType.UInt16;
        else if (typeof(T) == typeof(int)) return PrimitiveType.Int32;
        else if (typeof(T) == typeof(uint)) return PrimitiveType.UInt32;
        else if (typeof(T) == typeof(long)) return PrimitiveType.Int64;
        else if (typeof(T) == typeof(ulong)) return PrimitiveType.UInt64;
        else if (typeof(T) == typeof(float)) return PrimitiveType.Single;
        else if (typeof(T) == typeof(double)) return PrimitiveType.Double;
        else if (typeof(T) == typeof(decimal)) return PrimitiveType.Decimal;
        else if (typeof(T) == typeof(DateTime)) return PrimitiveType.DateTime;
        else if (typeof(T) == typeof(TimeSpan)) return PrimitiveType.TimeSpan;
        else if (typeof(T) == typeof(IntPtr)) return IntPtrPrimitiveType;
        else if (typeof(T) == typeof(UIntPtr)) return UIntPtrPrimitiveType;
        else
        {
            Debug.Assert(typeof(T) == typeof(string));
            return PrimitiveType.String;
        }
    }

    internal static TypeName ParseNonSystemClassRecordTypeName(this string rawName, BinaryLibraryRecord libraryRecord, PayloadOptions payloadOptions)
    {
        if (libraryRecord.LibraryName is not null)
        {
            return ParseWithoutAssemblyName(rawName, payloadOptions).WithAssemblyName(libraryRecord.LibraryName);
        }

        Debug.Assert(payloadOptions.UndoTruncatedTypeNames);
        Debug.Assert(libraryRecord.RawLibraryName is not null);

        // Combining type and library allows us for handling truncated generic type names that may be present in resources.
        ArraySegment<char> assemblyQualifiedName = GetAssemblyQualifiedName(rawName, libraryRecord.RawLibraryName);
        TypeName.TryParse(assemblyQualifiedName.AsSpan(), out TypeName? typeName, payloadOptions.TypeNameParseOptions);
        ArrayPool<char>.Shared.Return(assemblyQualifiedName.Array!);

        if (typeName is null)
        {
            throw new SerializationException(SR.Format(SR.Serialization_InvalidTypeOrAssemblyName, rawName, libraryRecord.RawLibraryName));
        }

        if (typeName.AssemblyName is null)
        {
            // Sample invalid input that could lead us here:
            // TypeName: System.Collections.Generic.List`1[[System.String
            // LibraryName: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]
            // Since the flag is ON, we know it's mangling and we provide missing information.
            typeName = typeName.WithCoreLibAssemblyName();
        }

        return typeName;
    }

    internal static TypeName ParseSystemRecordTypeName(this string rawName, PayloadOptions payloadOptions)
        => ParseWithoutAssemblyName(rawName, payloadOptions)
                .WithCoreLibAssemblyName(); // We know it's a System Record, so we set the LibraryName to CoreLib

    internal static TypeName WithCoreLibAssemblyName(this TypeName systemType)
        => systemType.WithAssemblyName(s_CoreLibAssemblyName ??= AssemblyNameInfo.Parse("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089".AsSpan()));

    private static TypeName ParseWithoutAssemblyName(string rawName, PayloadOptions payloadOptions)
    {
        if (!TypeName.TryParse(rawName.AsSpan(), out TypeName? typeName, payloadOptions.TypeNameParseOptions)
            || typeName.AssemblyName is not null) // the type and library names should be provided separately
        {
            throw new SerializationException(SR.Format(SR.Serialization_InvalidTypeName, rawName));
        }

        return typeName;
    }

    private static ArraySegment<char> GetAssemblyQualifiedName(string typeName, string libraryName)
    {
        int length = typeName.Length + 1 + libraryName.Length;

        char[] rented = ArrayPool<char>.Shared.Rent(length);

        typeName.AsSpan().CopyTo(rented);
        rented[typeName.Length] = ',';
        libraryName.AsSpan().CopyTo(rented.AsSpan(typeName.Length + 1));

        return new ArraySegment<char>(rented, 0, length);
    }
}
