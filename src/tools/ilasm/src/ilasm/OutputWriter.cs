// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ILAssembler;

/// <summary>
/// Writes an assembled image and its Portable PDB to disk.
/// </summary>
/// <remarks>
/// <para>
/// The PDB is a separate file beside the output (<see cref="GetPdbPath"/>), as with native ilasm.
/// <see cref="Write(string, string, Action{Stream}, ImmutableArray{byte}?)"/> is called only when the assembly
/// produced an image: when it succeeded, or despite errors with the error-tolerant option (<c>--error</c>,
/// <c>/ERR</c>), in which case the image and PDB are handled exactly as for a successful assembly. When the assembly
/// produces no image, the tool calls nothing here and leaves the existing output and PDB as they are.
/// </para>
/// <para>
/// The image is written and closed before the PDB is touched, so a failure while writing or closing the image
/// leaves the existing PDB as it was; the image itself may then be partial. The PDB is written to a new temporary
/// file in the same directory and then renamed over <c>&lt;output&gt;.pdb</c>, so the PDB path holds either the
/// file that was there before or the complete new PDB. If writing or renaming the temporary file fails, the run
/// fails with the new image already written, and deleting the temporary file is attempted; it can remain if that
/// deletion fails or the process ends before the rename. Because the PDB is renamed into place, a symbolic or hard
/// link at <c>&lt;output&gt;.pdb</c> is replaced by a regular file and the link's target keeps its old content;
/// deleting a PDB (below) likewise removes a symbolic link, not its target.
/// </para>
/// <para>
/// When no PDB is produced, an existing <c>&lt;output&gt;.pdb</c> is deleted after the image is written only if it
/// belongs to the image being replaced: the image previously at the output path has a CodeView entry whose PDB id
/// (GUID and stamp) equals the id of that Portable PDB. Any other file at the PDB path is left in place, including
/// a PDB beside an output that did not exist before, a PDB of another image, and a file that is not a Portable PDB.
/// </para>
/// </remarks>
internal static class OutputWriter
{
    /// <summary>
    /// Gets the full path of the PDB for an output: the output path with its extension replaced by <c>.pdb</c>.
    /// </summary>
    public static string GetPdbPath(string outputPath) => Path.GetFullPath(Path.ChangeExtension(outputPath, ".pdb"));

    /// <summary>
    /// Gets whether <paramref name="pdbPath"/> names the output file itself, as it does for an output named
    /// <c>Min.pdb</c>.
    /// </summary>
    /// <remarks>
    /// The comparison ignores case on Windows and macOS, whose default file systems do, so that
    /// <c>Min.PDB</c> and <c>Min.pdb</c> are treated as the same file there; elsewhere it is exact.
    /// </remarks>
    public static bool IsOutputPath(string pdbPath, string outputPath) =>
        string.Equals(
            pdbPath,
            Path.GetFullPath(outputPath),
            OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    /// <summary>
    /// Writes the image to <paramref name="outputPath"/>, then writes <paramref name="portablePdb"/> to
    /// <paramref name="pdbPath"/> or, when there is no PDB, deletes the PDB of the image that was replaced.
    /// </summary>
    /// <param name="outputPath">The path of the image.</param>
    /// <param name="pdbPath">The path of the PDB, from <see cref="GetPdbPath"/>.</param>
    /// <param name="writeImage">Writes the image to the stream it is given.</param>
    /// <param name="portablePdb">The PDB, or <see langword="null"/> when none was produced.</param>
    /// <returns>What was written or deleted.</returns>
    /// <remarks>
    /// When a PDB is produced and <paramref name="pdbPath"/> names the output file itself, nothing is written
    /// and <see cref="OutputWriteResult.PdbWouldOverwriteOutput"/> is returned. When no PDB is produced and
    /// <paramref name="pdbPath"/> names the output file, that file is the new image and is not deleted.
    /// A PDB that belongs to the replaced image but cannot be deleted is left in place without failing the write.
    /// </remarks>
    public static OutputWriteResult Write(string outputPath, string pdbPath, Action<Stream> writeImage, ImmutableArray<byte>? portablePdb) =>
        Write(outputPath, pdbPath, writeImage, portablePdb, File.Create);

    /// <summary>
    /// <see cref="Write(string, string, Action{Stream}, ImmutableArray{byte}?)"/>, with the stream the image is
    /// written to created by <paramref name="createImageStream"/>.
    /// </summary>
    internal static OutputWriteResult Write(
        string outputPath,
        string pdbPath,
        Action<Stream> writeImage,
        ImmutableArray<byte>? portablePdb,
        Func<string, Stream> createImageStream)
    {
        bool pdbPathIsOutputPath = IsOutputPath(pdbPath, outputPath);
        if (portablePdb is not null && pdbPathIsOutputPath)
        {
            return OutputWriteResult.PdbWouldOverwriteOutput;
        }

        // Which PDB the image being replaced refers to, read before the image is overwritten. Without a new
        // PDB, only that PDB is deleted.
        BlobContentId? replacedImagePdbId = portablePdb is null && !pdbPathIsOutputPath ? TryReadCodeViewPdbId(outputPath) : null;

        // Write and close the image before touching the PDB. If this throws, the PDB is left as it was.
        using (Stream imageStream = createImageStream(outputPath))
        {
            writeImage(imageStream);
        }

        if (portablePdb is { } pdb)
        {
            WritePdb(pdbPath, GetTemporaryPdbPath(pdbPath), pdb);
            return OutputWriteResult.ImageAndPdbWritten;
        }

        if (replacedImagePdbId is { } pdbId && TryReadPortablePdbId(pdbPath) == pdbId && TryDelete(pdbPath))
        {
            return OutputWriteResult.ImageWrittenStalePdbDeleted;
        }

        return OutputWriteResult.ImageWritten;
    }

    /// <summary>
    /// Gets the id (GUID and stamp) of the PDB that the image at <paramref name="imagePath"/> refers to in its
    /// first CodeView entry, or <see langword="null"/> when there is no such file, it cannot be read, or
    /// <see cref="TryReadCodeViewPdbId(Stream)"/> finds no id in it.
    /// </summary>
    internal static BlobContentId? TryReadCodeViewPdbId(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath))
            {
                return null;
            }

            using FileStream stream = File.OpenRead(imagePath);
            return TryReadCodeViewPdbId(stream);
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
    /// Gets the id of the Portable PDB at <paramref name="pdbPath"/>, or <see langword="null"/> when there is no
    /// such file, it cannot be read, or <see cref="TryReadPortablePdbId(Stream)"/> finds no id in it.
    /// </summary>
    internal static BlobContentId? TryReadPortablePdbId(string pdbPath)
    {
        try
        {
            if (!File.Exists(pdbPath))
            {
                return null;
            }

            using FileStream stream = File.OpenRead(pdbPath);
            return TryReadPortablePdbId(stream);
        }
        catch (Exception ex) when (ex is BadImageFormatException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
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

    /// <summary>
    /// Gets a path in the PDB's directory for the temporary file the PDB is written to before it is renamed.
    /// </summary>
    internal static string GetTemporaryPdbPath(string pdbPath) =>
        Path.Combine(Path.GetDirectoryName(pdbPath)!, $"{Path.GetFileName(pdbPath)}.{Path.GetRandomFileName()}.tmp");

    /// <summary>
    /// Writes <paramref name="pdb"/> to a new file at <paramref name="temporaryPath"/> and renames it over
    /// <paramref name="pdbPath"/>.
    /// </summary>
    /// <remarks>
    /// The temporary file is created only if no file exists at <paramref name="temporaryPath"/>. If the write or
    /// the rename fails, deleting the temporary file is attempted only when this call created it, and
    /// <paramref name="pdbPath"/> is left as it was.
    /// </remarks>
    internal static void WritePdb(string pdbPath, string temporaryPath, ImmutableArray<byte> pdb)
    {
        bool created = false;
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                created = true;
                stream.Write(pdb.AsSpan());
            }

            File.Move(temporaryPath, pdbPath, overwrite: true);
        }
        catch
        {
            if (created)
            {
                TryDelete(temporaryPath);
            }

            throw;
        }
    }

    private static bool TryDelete(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}

/// <summary>
/// What <see cref="OutputWriter.Write(string, string, Action{Stream}, ImmutableArray{byte}?)"/> wrote or deleted.
/// </summary>
internal enum OutputWriteResult
{
    /// <summary>The image was written. No PDB was produced, and no PDB was deleted.</summary>
    ImageWritten,

    /// <summary>
    /// The image was written. No PDB was produced, and the PDB of the image it replaced was deleted.
    /// </summary>
    ImageWrittenStalePdbDeleted,

    /// <summary>The image and then its PDB were written.</summary>
    ImageAndPdbWritten,

    /// <summary>Nothing was written: the PDB path names the output file itself.</summary>
    PdbWouldOverwriteOutput,
}
