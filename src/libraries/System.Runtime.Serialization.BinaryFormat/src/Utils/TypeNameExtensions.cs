// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Reflection.Metadata;

namespace System.Runtime.Serialization.BinaryFormat;

internal static class TypeNameExtensions
{
    internal static TypeName WithAssemblyName(this TypeName typeName, string assemblyName)
    {
        // For ClassWithMembersAndTypesRecord, the TypeName and LibraryName and provided separately,
        // and the LibraryName may not be known when parsing TypeName.
        // For SystemClassWithMembersAndTypesRecord, the LibraryName is not provided, it's always mscorlib.
        // Ideally, we would just create TypeName with new AssemblyNameInfo.
        // This will be possible once https://github.com/dotnet/runtime/issues/102263 is done.

        int length = typeName.FullName.Length + 1 + assemblyName.Length;
        char[] rented = ArrayPool<char>.Shared.Rent(length);

        try
        {
            typeName.FullName.AsSpan().CopyTo(rented);
            rented[typeName.FullName.Length] = ',';
            assemblyName.AsSpan().CopyTo(rented.AsSpan(typeName.FullName.Length + 1));

            return TypeName.Parse(rented.AsSpan(0, length));
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rented);
        }
    }
}
