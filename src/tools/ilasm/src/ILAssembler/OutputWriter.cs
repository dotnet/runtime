// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ILAssembler;

/// <summary>
/// Writes an assembled image and its Portable PDB through the streams of an <see cref="IOutputStreams"/>, and
/// decides what happens to an existing PDB when no PDB is produced.
/// </summary>
/// <remarks>
/// <para>
/// This type orders the writes and makes the decisions; it does not touch the file system. The caller supplies the
/// output and the PDB as streams: the ilasm tool backs them with the output file and <c>&lt;output&gt;.pdb</c>
/// beside it.
/// </para>
/// <para>
/// The image is written and its stream closed before the PDB is written, read or deleted, so a failure while
/// writing or closing the image leaves the existing PDB as it was; the image itself may then be partial.
/// </para>
/// <para>
/// When no PDB is produced, the existing PDB is deleted after the image is written only if it belongs to the image
/// being replaced: the output as it was before the new image was written is a PE image whose first CodeView entry
/// has a PDB id (GUID and stamp) equal to the id of that Portable PDB. Anything else at the PDB path is left in
/// place, including a PDB when there was no previous output, a PDB of another image, and a file that is not a
/// Portable PDB. A previous output or PDB that cannot be opened or read counts as having no id, so it never causes a
/// deletion and never fails the write.
/// </para>
/// <para>
/// When a PDB is produced and the PDB path names the output itself (<see cref="IOutputStreams.PdbPathIsOutputPath"/>),
/// nothing is written and <see cref="OutputWriteResult.PdbWouldOverwriteOutput"/> is returned. When no PDB is
/// produced and the PDB path names the output, the file there is the new image, so it is neither read as a PDB nor
/// deleted.
/// </para>
/// </remarks>
public static class OutputWriter
{
    /// <summary>
    /// Writes the image to <see cref="IOutputStreams.CreateOutput"/>, then writes <paramref name="portablePdb"/>
    /// with <see cref="IOutputStreams.WritePdb"/> or, when there is no PDB, deletes the PDB of the image that was
    /// replaced.
    /// </summary>
    /// <param name="output">The output and its PDB.</param>
    /// <param name="writeImage">Writes the image to the stream it is given.</param>
    /// <param name="portablePdb">The PDB, or <see langword="null"/> when none was produced.</param>
    /// <returns>What was written or deleted.</returns>
    /// <remarks>
    /// An exception from <see cref="IOutputStreams.PdbPathIsOutputPath"/> propagates to the caller before anything is
    /// written. An exception from <paramref name="writeImage"/>, from creating or closing the output, or from
    /// <see cref="IOutputStreams.WritePdb"/> propagates to the caller. A PDB that belongs to the replaced image but
    /// cannot be deleted (<see cref="IOutputStreams.TryDeletePdb"/> returns <see langword="false"/>) is left in place
    /// without failing the write.
    /// </remarks>
    public static OutputWriteResult Write(IOutputStreams output, Action<Stream> writeImage, ImmutableArray<byte>? portablePdb)
    {
        bool pdbPathIsOutputPath = output.PdbPathIsOutputPath;
        if (portablePdb is not null && pdbPathIsOutputPath)
        {
            return OutputWriteResult.PdbWouldOverwriteOutput;
        }

        // Which PDB the image being replaced refers to, read before the image is overwritten. Without a new
        // PDB, only that PDB is deleted.
        BlobContentId? replacedImagePdbId = portablePdb is null && !pdbPathIsOutputPath ? TryReadPdbIdOfExistingOutput(output) : null;

        // Write and close the image before touching the PDB. If this throws, the PDB is left as it was.
        using (Stream imageStream = output.CreateOutput())
        {
            writeImage(imageStream);
        }

        if (portablePdb is { } pdb)
        {
            output.WritePdb(pdb);
            return OutputWriteResult.ImageAndPdbWritten;
        }

        if (replacedImagePdbId is { } pdbId && TryReadIdOfExistingPdb(output) == pdbId && output.TryDeletePdb())
        {
            return OutputWriteResult.ImageWrittenStalePdbDeleted;
        }

        return OutputWriteResult.ImageWritten;
    }

    /// <summary>
    /// Gets the PDB id that the existing output refers to, or <see langword="null"/> when there is no existing
    /// output, it cannot be opened or read, or <see cref="TryReadCodeViewPdbId(Stream)"/> finds no id in it.
    /// </summary>
    private static BlobContentId? TryReadPdbIdOfExistingOutput(IOutputStreams output)
    {
        try
        {
            using Stream? stream = output.OpenExistingOutput();
            return stream is null ? null : TryReadCodeViewPdbId(stream);
        }
        catch (Exception ex) when (ex is BadImageFormatException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the id of the existing PDB, or <see langword="null"/> when there is no existing PDB, it cannot be opened
    /// or read, or <see cref="TryReadPortablePdbId(Stream)"/> finds no id in it.
    /// </summary>
    private static BlobContentId? TryReadIdOfExistingPdb(IOutputStreams output)
    {
        try
        {
            using Stream? stream = output.OpenExistingPdb();
            return stream is null ? null : TryReadPortablePdbId(stream);
        }
        catch (Exception ex) when (ex is BadImageFormatException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the id (GUID and stamp) of the PDB that the image in <paramref name="stream"/> refers to in its first
    /// CodeView entry, or <see langword="null"/> when the stream is longer than <see cref="int.MaxValue"/> bytes,
    /// holds a COFF object file rather than a PE image, or the image has no CodeView entry.
    /// </summary>
    /// <exception cref="BadImageFormatException">The stream holds neither a PE image nor a COFF object file.</exception>
    internal static BlobContentId? TryReadCodeViewPdbId(Stream stream)
    {
        // System.Reflection.Metadata reads at most int.MaxValue bytes and throws ArgumentException for a longer stream.
        if (stream.Length > int.MaxValue)
        {
            return null;
        }

        using var peReader = new PEReader(stream, PEStreamOptions.PrefetchEntireImage | PEStreamOptions.LeaveOpen);

        // A file that does not start with "MZ" is read as a COFF object file (twenty zero bytes read as one). It has
        // no PE header and so no debug directory, and ReadDebugDirectory requires the PE header.
        if (peReader.PEHeaders.IsCoffOnly)
        {
            return null;
        }

        foreach (DebugDirectoryEntry entry in peReader.ReadDebugDirectory())
        {
            if (entry.Type == DebugDirectoryEntryType.CodeView)
            {
                return new BlobContentId(peReader.ReadCodeViewDebugDirectoryData(entry).Guid, entry.Stamp);
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the id of the Portable PDB in <paramref name="stream"/>, or <see langword="null"/> when the stream is
    /// longer than <see cref="int.MaxValue"/> bytes or holds metadata that is not a Portable PDB.
    /// </summary>
    /// <exception cref="BadImageFormatException">The stream does not hold metadata.</exception>
    internal static BlobContentId? TryReadPortablePdbId(Stream stream)
    {
        // System.Reflection.Metadata reads at most int.MaxValue bytes and throws ArgumentException for a longer stream.
        if (stream.Length > int.MaxValue)
        {
            return null;
        }

        using MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(
            stream,
            MetadataStreamOptions.PrefetchMetadata | MetadataStreamOptions.LeaveOpen);
        DebugMetadataHeader? header = provider.GetMetadataReader().DebugMetadataHeader;
        return header is null ? null : new BlobContentId(header.Id);
    }
}

/// <summary>
/// The output and its PDB, as streams, for <see cref="OutputWriter.Write"/>.
/// </summary>
/// <remarks>
/// <see cref="OutputWriter.Write"/> uses these members in this order, each at most once:
/// <see cref="PdbPathIsOutputPath"/>; <see cref="OpenExistingOutput"/>, only when no PDB is produced and
/// <see cref="PdbPathIsOutputPath"/> is <see langword="false"/>; <see cref="CreateOutput"/>, unless the PDB would
/// overwrite the output; then either <see cref="WritePdb"/>, when a PDB is produced, or
/// <see cref="OpenExistingPdb"/>, only when the replaced output yielded a PDB id, followed by
/// <see cref="TryDeletePdb"/>, only when that id equals the id of the existing PDB. It disposes every stream it is
/// given.
/// </remarks>
public interface IOutputStreams
{
    /// <summary>
    /// Gets whether the PDB path names the output itself (an output named <c>Min.pdb</c>, for example, or an output
    /// that is a symbolic link to the PDB path), so that writing a PDB would overwrite the image.
    /// </summary>
    bool PdbPathIsOutputPath { get; }

    /// <summary>
    /// Opens the output as it is before the new image is written, for reading, or returns <see langword="null"/>
    /// when there is no output yet.
    /// </summary>
    /// <remarks>
    /// An <see cref="IOException"/>, <see cref="UnauthorizedAccessException"/> or
    /// <see cref="BadImageFormatException"/> from this method or from reading the stream counts as an output with no
    /// PDB id.
    /// </remarks>
    Stream? OpenExistingOutput();

    /// <summary>
    /// Creates the output, replacing any existing one, and returns the stream the image is written to.
    /// </summary>
    Stream CreateOutput();

    /// <summary>
    /// Writes <paramref name="pdb"/> to the PDB path, replacing what is there.
    /// </summary>
    void WritePdb(ImmutableArray<byte> pdb);

    /// <summary>
    /// Opens the file at the PDB path for reading, or returns <see langword="null"/> when there is none.
    /// </summary>
    /// <remarks>
    /// An <see cref="IOException"/>, <see cref="UnauthorizedAccessException"/> or
    /// <see cref="BadImageFormatException"/> from this method or from reading the stream counts as a file with no
    /// PDB id.
    /// </remarks>
    Stream? OpenExistingPdb();

    /// <summary>
    /// Deletes the file at the PDB path.
    /// </summary>
    /// <returns><see langword="true"/> if it was deleted; <see langword="false"/> if it could not be.</returns>
    bool TryDeletePdb();
}

/// <summary>
/// What <see cref="OutputWriter.Write"/> wrote or deleted.
/// </summary>
public enum OutputWriteResult
{
    /// <summary>The image was written. No PDB was produced, and no PDB was deleted.</summary>
    ImageWritten,

    /// <summary>
    /// The image was written. No PDB was produced, and the PDB of the image it replaced was deleted.
    /// </summary>
    ImageWrittenStalePdbDeleted,

    /// <summary>The image and then its PDB were written.</summary>
    ImageAndPdbWritten,

    /// <summary>Nothing was written: the PDB path names the output itself.</summary>
    PdbWouldOverwriteOutput,
}
