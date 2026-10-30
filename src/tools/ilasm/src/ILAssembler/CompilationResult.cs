// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ILAssembler;

/// <summary>Represents a compiled portable executable image.</summary>
public sealed class CompilationResult
{
    private readonly PEBuilder _peBuilder;
    private readonly Blob _mvidFixup;

    internal CompilationResult(PEBuilder peBuilder, Blob mvidFixup, ImmutableArray<byte>? portablePdb)
    {
        _peBuilder = peBuilder;
        _mvidFixup = mvidFixup;
        PortablePdb = portablePdb;
    }

    /// <summary>
    /// Gets the serialized Portable PDB for the image, or <see langword="null"/> when no PDB was requested.
    /// </summary>
    /// <remarks>
    /// A PDB is produced when <see cref="Options.Debug"/>, <see cref="Options.DebugMode"/> or
    /// <see cref="Options.Pdb"/> is set; <c>.line</c> directives alone do not produce one.
    /// It is not embedded in the image: the image's debug directory holds a CodeView entry that names the PDB path and carries this PDB's id, followed by a PdbChecksum
    /// entry with the SHA-256 hash of these bytes with the 20-byte PDB id zeroed, and, with
    /// <see cref="Options.Deterministic"/>, a Reproducible entry. The PDB path is
    /// <see cref="Options.PdbFilePath"/> as given when set; otherwise it is <see cref="Options.OutputFileName"/> with its
    /// extension replaced by <c>.pdb</c>, or <c>assembly.pdb</c> when no output file name is set.
    /// The caller writes these bytes to the file that path names; a file name alone names a file beside the image.
    /// </remarks>
    public ImmutableArray<byte>? PortablePdb { get; }

    /// <summary>Serializes the compiled image into the specified builder.</summary>
    /// <param name="builder">The builder that receives the serialized image.</param>
    /// <returns>The content identifier of the serialized image.</returns>
    public BlobContentId Serialize(BlobBuilder builder)
    {
        BlobContentId contentId = _peBuilder.Serialize(builder);
        if (!_mvidFixup.IsDefault)
        {
            new BlobWriter(_mvidFixup).WriteGuid(contentId.Guid);
        }

        return contentId;
    }
}
