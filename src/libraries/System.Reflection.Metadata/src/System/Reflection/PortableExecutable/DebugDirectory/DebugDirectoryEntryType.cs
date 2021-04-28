// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.PortableExecutable
{
    public enum DebugDirectoryEntryType
    {
        /// <summary>
        /// An unknown value that is ignored by all tools.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The COFF debug information (line numbers, symbol table, and string table).
        /// This type of debug information is also pointed to by fields in the file headers.
        /// </summary>
        Coff = 1,

        /// <summary>
        /// Associated PDB file description.
        /// </summary>
        /// <remarks>
        /// See https://github.com/dotnet/runtime/blob/main/src/libraries/System.Reflection.Metadata/specs/PE-COFF.md#codeview-debug-directory-entry-type-2 for specification.
        /// </remarks>
        CodeView = 2,

        /// <summary>
        /// Presence of this entry indicates deterministic PE/COFF file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The tool that produced the deterministic PE/COFF file guarantees that the entire content of the file
        /// is based solely on documented inputs given to the tool (such as source files, resource files, compiler options, etc.)
        /// rather than ambient environment variables (such as the current time, the operating system,
        /// the bitness of the process running the tool, etc.).
        /// </para>
        /// <para>
        /// The value of field TimeDateStamp in COFF File Header of a deterministic PE/COFF file
        /// does not indicate the date and time when the file was produced and should not be interpreted that way.
        /// Instead the value of the field is derived from a hash of the file content. The algorithm to calculate
        /// this value is an implementation detail of the tool that produced the file.
        /// </para>
        /// <para>
        /// The debug directory entry of type <see cref="Reproducible"/> must have all fields, except for Type zeroed.
        /// </para>
        /// <para>
        /// See https://github.com/dotnet/runtime/blob/main/src/libraries/System.Reflection.Metadata/specs/PE-COFF.md#deterministic-debug-directory-entry-type-16 for specification.
        /// </para>
        /// </remarks>
        Reproducible = 16,

        /// <summary>
        /// The entry points to a blob containing Embedded Portable PDB.
        /// </summary>
        /// <remarks>
        /// The Embedded Portable PDB blob has the following format:
        ///
        /// blob ::= uncompressed-size data
        ///
        /// Data spans the remainder of the blob and contains a Deflate-compressed Portable PDB.
        ///
        /// See https://github.com/dotnet/runtime/blob/main/src/libraries/System.Reflection.Metadata/specs/PE-COFF.md#embedded-portable-pdb-debug-directory-entry-type-17 for specification.
        /// </remarks>
        EmbeddedPortablePdb = 17,

        /// <summary>
        /// The entry stores crypto hash of the content of the symbol file the PE/COFF file was built with.
        /// </summary>
        /// <remarks>
        /// The hash can be used to validate that a given PDB file was built with the PE/COFF file and not altered in any way.
        /// More than one entry can be present, in case multiple PDBs were produced during the build of the PE/COFF file (e.g. private and public symbols).
        ///
        /// See https://github.com/dotnet/runtime/blob/main/src/libraries/System.Reflection.Metadata/specs/PE-COFF.md#pdb-checksum-debug-directory-entry-type-19 for specification.
        /// </remarks>
        PdbChecksum = 19,
    }
}
