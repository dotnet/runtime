// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop
{
    /// <summary>
    /// Contains the data related to a LibraryImportAttribute, without references to Roslyn symbols.
    /// See <seealso cref="LibraryImportCompilationData"/> for a type with a reference to the StringMarshallingCustomType
    /// </summary>
    internal sealed record LibraryImportData(string ModuleName) : InteropAttributeData
    {
        public string EntryPoint { get; init; }

        public static LibraryImportData From(LibraryImportCompilationData libraryImport)
            => new LibraryImportData(libraryImport.ModuleName) with
            {
                EntryPoint = libraryImport.EntryPoint,
                IsUserDefined = libraryImport.IsUserDefined,
                SetLastError = libraryImport.SetLastError,
                StringMarshalling = libraryImport.StringMarshalling,
                StringMarshallingCustomType = libraryImport.StringMarshallingCustomType is not null
                    ? ManagedTypeInfo.CreateTypeInfoForTypeSymbol(libraryImport.StringMarshallingCustomType)
                    : null,
            };
    }

    /// <summary>
    /// Contains the data related to a LibraryImportAttribute, with references to Roslyn symbols.
    /// Use <seealso cref="LibraryImportData"/> instead when using for incremental compilation state to avoid keeping a compilation alive
    /// </summary>
    internal sealed record LibraryImportCompilationData(string ModuleName) : InteropAttributeCompilationData
    {
        public string EntryPoint { get; init; }
    }
}
