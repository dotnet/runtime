﻿// Licensed to the .NET Foundation under one or more agreements.
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
    // PrimitiveType does not define Object, IntPtr or UIntPtr.
    internal const PrimitiveType StringPrimitiveType = (PrimitiveType)18;
    internal const PrimitiveType ObjectPrimitiveType = (PrimitiveType)19;
    internal const PrimitiveType IntPtrPrimitiveType = (PrimitiveType)20;
    internal const PrimitiveType UIntPtrPrimitiveType = (PrimitiveType)21;
    private static readonly TypeName?[] s_primitiveTypeNames = new TypeName?[(int)UIntPtrPrimitiveType + 1];
    private static readonly TypeName?[] s_primitiveSZArrayTypeNames = new TypeName?[(int)UIntPtrPrimitiveType + 1];
    private static AssemblyNameInfo? s_coreLibAssemblyName;

    internal static TypeName GetPrimitiveTypeName(PrimitiveType primitiveType)
    {
        TypeName? typeName = s_primitiveTypeNames[(int)primitiveType];
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
                PrimitiveType.UInt64 => "System.UInt64",
                PrimitiveType.Single => "System.Single",
                PrimitiveType.Double => "System.Double",
                PrimitiveType.Decimal => "System.Decimal",
                PrimitiveType.TimeSpan => "System.TimeSpan",
                PrimitiveType.DateTime => "System.DateTime",
                StringPrimitiveType => "System.String",
                ObjectPrimitiveType => "System.Object",
                IntPtrPrimitiveType => "System.IntPtr",
                UIntPtrPrimitiveType => "System.UIntPtr",
                _ => throw new InvalidOperationException()
            };

            s_primitiveTypeNames[(int)primitiveType] = typeName = TypeName.Parse(fullName.AsSpan()).WithCoreLibAssemblyName();
        }
        return typeName;
    }

    internal static TypeName GetPrimitiveSZArrayTypeName(PrimitiveType primitiveType)
    {
        TypeName? typeName = s_primitiveSZArrayTypeNames[(int)primitiveType];
        if (typeName is null)
        {
            s_primitiveSZArrayTypeNames[(int)primitiveType] = typeName = GetPrimitiveTypeName(primitiveType).MakeSZArrayTypeName();
        }
        return typeName;
    }

    internal static PrimitiveType GetPrimitiveType<T>()
    {
        if (typeof(T) == typeof(bool))
            return PrimitiveType.Boolean;
        else if (typeof(T) == typeof(byte))
            return PrimitiveType.Byte;
        else if (typeof(T) == typeof(sbyte))
            return PrimitiveType.SByte;
        else if (typeof(T) == typeof(char))
            return PrimitiveType.Char;
        else if (typeof(T) == typeof(short))
            return PrimitiveType.Int16;
        else if (typeof(T) == typeof(ushort))
            return PrimitiveType.UInt16;
        else if (typeof(T) == typeof(int))
            return PrimitiveType.Int32;
        else if (typeof(T) == typeof(uint))
            return PrimitiveType.UInt32;
        else if (typeof(T) == typeof(long))
            return PrimitiveType.Int64;
        else if (typeof(T) == typeof(ulong))
            return PrimitiveType.UInt64;
        else if (typeof(T) == typeof(float))
            return PrimitiveType.Single;
        else if (typeof(T) == typeof(double))
            return PrimitiveType.Double;
        else if (typeof(T) == typeof(decimal))
            return PrimitiveType.Decimal;
        else if (typeof(T) == typeof(DateTime))
            return PrimitiveType.DateTime;
        else if (typeof(T) == typeof(TimeSpan))
            return PrimitiveType.TimeSpan;
        else if (typeof(T) == typeof(string))
            return StringPrimitiveType;
        else if (typeof(T) == typeof(IntPtr))
            return IntPtrPrimitiveType;
        else if (typeof(T) == typeof(UIntPtr))
            return UIntPtrPrimitiveType;
        else
            throw new InvalidOperationException();
    }

    internal static TypeName ParseNonSystemClassRecordTypeName(this string rawName, BinaryLibraryRecord libraryRecord, PayloadOptions payloadOptions)
    {
        if (libraryRecord.LibraryName is not null)
        {
            return ParseWithoutAssemblyName(rawName, payloadOptions).With(libraryRecord.LibraryName);
        }

        Debug.Assert(payloadOptions.UndoTruncatedTypeNames);
        Debug.Assert(libraryRecord.RawLibraryName is not null);

        // This is potentially a DoS vector, as somebody could submit:
        // [1] BinaryLibraryRecord = <really long string>
        // [2] ClassRecord (lib = [1])
        // [3] ClassRecord (lib = [1])
        // ...
        // [n] ClassRecord (lib = [1])
        //
        // Which means somebody submits a payload of length O(long + n) and tricks us into
        // performing O(long * n) work. For this reason, we have marked the UndoTruncatedTypeNames
        // property as "keep this disabled unless you trust the input."

        // Combining type and library allows us for handling truncated generic type names that may be present in resources.
        ArraySegment<char> assemblyQualifiedName = RentAssemblyQualifiedName(rawName, libraryRecord.RawLibraryName);
        TypeName.TryParse(assemblyQualifiedName.AsSpan(), out TypeName? typeName, payloadOptions.TypeNameParseOptions);
        ArrayPool<char>.Shared.Return(assemblyQualifiedName.Array!);

        if (typeName is null)
        {
            throw new SerializationException(SR.Serialization_InvalidTypeOrAssemblyName);
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
        => systemType.With(s_coreLibAssemblyName ??= AssemblyNameInfo.Parse("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089".AsSpan()));

    private static TypeName With(this TypeName typeName, AssemblyNameInfo assemblyName)
    {
        // This is a recursive method over potentially hostile TypeName arguments.
        // We assume the complexity of the TypeName arg was appropriately bounded.
        // See comment in TypeName.FullName property getter for more info.

        if (!typeName.IsSimple)
        {
            if (typeName.IsArray)
            {
                TypeName newElementType = typeName.GetElementType().With(assemblyName);

                return typeName.IsSZArray
                    ? newElementType.MakeSZArrayTypeName()
                    : newElementType.MakeArrayTypeName(typeName.GetArrayRank());
            }
            else if (typeName.IsConstructedGenericType)
            {
                TypeName newGenericTypeDefinition = typeName.GetGenericTypeDefinition().With(assemblyName);

                // We don't change the assembly name of generic arguments on purpose.
                return newGenericTypeDefinition.MakeGenericTypeName(typeName.GetGenericArguments());
            }
            else
            {
                // BinaryFormatter can not serialize pointers or references.
                ThrowHelper.ThrowInvalidTypeName();
            }
        }

        return typeName.WithAssemblyName(assemblyName);
    }

    private static TypeName ParseWithoutAssemblyName(string rawName, PayloadOptions payloadOptions)
    {
        if (!TypeName.TryParse(rawName.AsSpan(), out TypeName? typeName, payloadOptions.TypeNameParseOptions)
            || typeName.AssemblyName is not null) // the type and library names should be provided separately
        {
            throw new SerializationException(SR.Format(SR.Serialization_InvalidTypeName, rawName));
        }

        return typeName;
    }

    // Complexity is O(typeName.Length + libraryName.Length)
    private static ArraySegment<char> RentAssemblyQualifiedName(string typeName, string libraryName)
    {
        int length = typeName.Length + 1 + libraryName.Length;

        char[] rented = ArrayPool<char>.Shared.Rent(length);

        typeName.AsSpan().CopyTo(rented);
        rented[typeName.Length] = ',';
        libraryName.AsSpan().CopyTo(rented.AsSpan(typeName.Length + 1));

        return new ArraySegment<char>(rented, 0, length);
    }
}
