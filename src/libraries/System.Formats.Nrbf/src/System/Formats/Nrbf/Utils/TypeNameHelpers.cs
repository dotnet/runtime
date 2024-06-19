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
    private static AssemblyNameInfo? s_CoreLibAssemblyName;

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

    internal static TypeName BuildCoreLibArrayTypeName(this Type type, int arrayRank)
        => WithCoreLibAssemblyName(BuildArrayTypeName(TypeName.Parse(type.FullName.AsSpan()), arrayRank));

    internal static TypeName BuildArrayTypeName(this TypeName typeName, int arrayRank)
    {
        // In general, arrayRank == 1 may have two different meanings:
        // - [] is a single dimension and zero-indexed array (SZArray)
        // - [*] is single dimension, custom offset array.
        // Custom offset arrays are not supported by design.
        // That is why we don't call TypeName.MakeArrayTypeName(1) because it would create [*] instead of [] name.
        return arrayRank == 1 ? typeName.MakeArrayTypeName() : typeName.MakeArrayTypeName(arrayRank);
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
