// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace ILAssembler;

/// <summary>
/// The <see cref="ManagedPEBuilder"/> for every image the assembler writes. The image has a debug
/// directory only when the assembler supplies one.
/// </summary>
/// <remarks>
/// Given no <see cref="DebugDirectoryBuilder"/>, a deterministic <see cref="ManagedPEBuilder"/> adds a debug
/// directory with a Reproducible entry of its own. Native ilasm writes a debug directory only with a PDB, so
/// this builder passes an empty <see cref="DebugDirectoryBuilder"/> instead, and it clears the PE header's debug
/// directory entry whenever the directory has no entries, which an empty builder would otherwise leave with a
/// non-zero address and size 0: an image built without a PDB has no debug directory, deterministic or not.
/// </remarks>
internal class ILAssemblerPEBuilder : ManagedPEBuilder
{
    public ILAssemblerPEBuilder(
        PEHeaderBuilder header,
        MetadataRootBuilder metadataRootBuilder,
        BlobBuilder ilStream,
        BlobBuilder? mappedFieldData = null,
        BlobBuilder? managedResources = null,
        ResourceSectionBuilder? nativeResources = null,
        DebugDirectoryBuilder? debugDirectoryBuilder = null,
        int strongNameSignatureSize = 128,
        MethodDefinitionHandle entryPoint = default,
        CorFlags flags = CorFlags.ILOnly,
        Func<IEnumerable<Blob>, BlobContentId>? deterministicIdProvider = null)
        : base(
            header,
            metadataRootBuilder,
            ilStream,
            mappedFieldData,
            managedResources,
            nativeResources,
            debugDirectoryBuilder ?? new DebugDirectoryBuilder(),
            strongNameSignatureSize,
            entryPoint,
            flags,
            deterministicIdProvider)
    {
    }

    protected override PEDirectoriesBuilder GetDirectories()
    {
        PEDirectoriesBuilder directories = base.GetDirectories();

        // A debug directory without entries is no debug directory. Clearing it is idempotent, so a later call
        // (VTableExportPEBuilder calls this again while serializing .reloc) sees the same directories.
        if (directories.DebugTable.Size == 0)
        {
            directories.DebugTable = default;
        }

        return directories;
    }
}
