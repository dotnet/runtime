// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Runtime.Serialization.BinaryFormat.Utils;

internal static class TypeNameExtensions
{
    internal const string CoreLibAssemblyName = "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

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
        ArraySegment<char> assemblyQualifiedName = GetAssemblyQualifiedName(rawName,
            CoreLibAssemblyName); // We know it's a System Record, so we set the LibraryName to CoreLib

        TypeName.TryParse(assemblyQualifiedName.AsSpan(), out TypeName? typeName, payloadOptions.TypeNameParseOptions);
        ArrayPool<char>.Shared.Return(assemblyQualifiedName.Array!);

        return typeName ?? throw new SerializationException(SR.Format(SR.Serialization_InvalidTypeName, rawName));
    }

    internal static TypeName WithCoreLibAssemblyName(this TypeName systemType)
        => systemType.WithAssemblyName(CoreLibAssemblyName);

    internal static TypeName WithAssemblyName(this TypeName typeName, string assemblyName)
    {
        // For ClassWithMembersAndTypesRecord, the TypeName and LibraryName and provided separately,
        // and the LibraryName may not be known when parsing TypeName.
        // For SystemClassWithMembersAndTypesRecord, the LibraryName is not provided, it's always mscorlib.
        // Ideally, we would just create TypeName with new AssemblyNameInfo.
        // This will be possible once https://github.com/dotnet/runtime/issues/102263 is done.

        ArraySegment<char> assemblyQualifiedName = GetAssemblyQualifiedName(typeName.FullName, assemblyName);
        TypeName result = TypeName.Parse(assemblyQualifiedName.AsSpan());
        ArrayPool<char>.Shared.Return(assemblyQualifiedName.Array!);

        return result;
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
