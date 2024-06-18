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
    internal static readonly AssemblyNameInfo s_CoreLibAssemblyName = AssemblyNameInfo.Parse("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089".AsSpan());

    internal static TypeName ParseNonSystemClassRecordTypeName(this string rawName, BinaryLibraryRecord libraryRecord, PayloadOptions payloadOptions)
    {
        // Combining type and library name has two goals:
        // 1. Handle truncated generic type names that may be present in resources.
        // 2. Improve perf by parsing only once.
        ArraySegment<char> assemblyQualifiedName = GetAssemblyQualifiedName(rawName, libraryRecord.LibraryName);
        TypeName.TryParse(assemblyQualifiedName.AsSpan(), out TypeName? typeName, payloadOptions.TypeNameParseOptions);
        ArrayPool<char>.Shared.Return(assemblyQualifiedName.Array!);

        if (typeName is null || (typeName.AssemblyName is null && !payloadOptions.UndoTruncatedTypeNames))
        {
            throw new SerializationException(SR.Format(SR.Serialization_InvalidTypeOrAssemblyName, rawName, libraryRecord.LibraryName));
        }

        if (typeName.AssemblyName is null && payloadOptions.UndoTruncatedTypeNames)
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
    {
        if (!TypeName.TryParse(rawName.AsSpan(), out TypeName? typeName, payloadOptions.TypeNameParseOptions))
        {
            throw new SerializationException(SR.Format(SR.Serialization_InvalidTypeName, rawName));
        }

        // We know it's a System Record, so we set the LibraryName to CoreLib
        return WithCoreLibAssemblyName(typeName);
    }

    internal static TypeName WithCoreLibAssemblyName(this TypeName systemType)
        => systemType.WithAssemblyName(s_CoreLibAssemblyName);

    internal static TypeName BuildCoreLibArrayTypeName(this Type type, int arrayRank)
        => WithCoreLibAssemblyName(BuildArrayTypeName(TypeName.Parse(type.FullName.AsSpan()), arrayRank));

    internal static TypeName BuildArrayTypeName(this TypeName typeName, int arrayRank)
    {
        // In this particular context, arrayRank == 1 means SZArray (custom offset arrays are not supported by design).
        // That is why we don't call typeName.MakeArrayTypeName(1) because it would create [*] instead of [] name.
        return arrayRank == 1 ? typeName.MakeArrayTypeName() : typeName.MakeArrayTypeName(arrayRank);
    }

    private static ArraySegment<char> GetAssemblyQualifiedName(string typeName, string libraryName, int arrayRank = 0)
    {
        int arrayLength = arrayRank != 0 ? 2 + arrayRank - 1 : 0;
        int length = typeName.Length + arrayLength + 1 + libraryName.Length;

        char[] rented = ArrayPool<char>.Shared.Rent(length);

        typeName.AsSpan().CopyTo(rented);
        if (arrayRank != 0)
        {
            rented[typeName.Length] = '[';
            for (int i = 1; i < arrayRank; i++)
            {
                rented[typeName.Length + i] = ',';
            }
            rented[typeName.Length + arrayLength - 1] = ']';
        }
        rented[typeName.Length + arrayLength] = ',';
        libraryName.AsSpan().CopyTo(rented.AsSpan(typeName.Length + arrayLength + 1));

        return new ArraySegment<char>(rented, 0, length);
    }
}
