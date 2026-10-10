// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;

namespace ILAssembler;

/// <summary>
/// Writes an assembled image and its Portable PDB to files: the output, and <c>&lt;output&gt;.pdb</c> beside it.
/// </summary>
/// <remarks>
/// <para>
/// The PDB is a separate file beside the output (<see cref="GetPdbPath"/>), as with native ilasm.
/// <see cref="Write"/> is called only when the assembly produced an image: when it succeeded, or despite errors with
/// the error-tolerant option (<c>--error</c>, <c>/ERR</c>), in which case the image and PDB are handled exactly as for
/// a successful assembly. When the assembly produces no image, the tool calls nothing here and leaves the existing
/// output and PDB as they are.
/// </para>
/// <para>
/// <see cref="OutputWriter.Write"/> orders the writes and decides which existing PDB is deleted; this type gives it
/// the two files as streams. An output or PDB that does not exist is opened as none.
/// </para>
/// <para>
/// The PDB is written to a new temporary file in the same directory and then renamed over <c>&lt;output&gt;.pdb</c>,
/// so the PDB path holds either the file that was there before or the complete new PDB. If writing or renaming the
/// temporary file fails, the run fails with the new image already written, and deleting the temporary file is
/// attempted; it can remain if that deletion fails or the process ends before the rename. Because the PDB is renamed
/// into place, a symbolic or hard link at <c>&lt;output&gt;.pdb</c> is replaced by a regular file and writing the PDB
/// leaves the link's target as it was; deleting a stale PDB likewise removes a symbolic link, not its target. A stale PDB
/// that cannot be deleted is left in place without failing the write.
/// </para>
/// <para>
/// The image, by contrast, is written through a symbolic link at the output path. An output that leads to
/// <c>&lt;output&gt;.pdb</c> through symbolic links is therefore treated like an output named <c>&lt;output&gt;.pdb</c>
/// (<see cref="IsOutputPath"/>): no PDB is written over it, and it is not deleted as a stale PDB.
/// </para>
/// </remarks>
internal static class OutputFileWriter
{
    /// <summary>
    /// The most symbolic links <see cref="IsOutputPath"/> follows from the output path, which is the limit Linux
    /// applies when it resolves a path. A cycle of links ends the walk here too.
    /// </summary>
    internal const int MaxSymbolicLinks = 40;

    /// <summary>
    /// Gets the full path of the PDB for an output: the output path with its extension replaced by <c>.pdb</c>.
    /// </summary>
    public static string GetPdbPath(string outputPath) => Path.GetFullPath(Path.ChangeExtension(outputPath, ".pdb"));

    /// <summary>
    /// Gets whether writing the image to <paramref name="outputPath"/> would write it at <paramref name="pdbPath"/>,
    /// where the PDB then replaces it: the output is named like its PDB (an output named <c>Min.pdb</c>), or it is a
    /// symbolic link that leads to the PDB path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The full output path is compared with the PDB path, and so is each target in the chain of symbolic links that
    /// starts at the output path, whether or not the last target exists. A relative link target is resolved against
    /// the directory of the link. Only the links at the output path and at its targets are followed; the directories
    /// in each path are compared as written.
    /// </para>
    /// <para>
    /// The walk ends at a path that is not a symbolic link, at a path that does not exist (a
    /// <see cref="FileNotFoundException"/> or <see cref="DirectoryNotFoundException"/> counts as no link), or after
    /// <see cref="MaxSymbolicLinks"/> links. Any other failure to read a link, such as an
    /// <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/>, propagates, so that an output that
    /// cannot be inspected is not written.
    /// </para>
    /// <para>
    /// A symbolic link at the PDB path needs no check, because the PDB is renamed into place and never written
    /// through a link. Nor does a hard link between the two names: the image is written through the output's name
    /// and the PDB then replaces the other name, which leaves the image under the output's name.
    /// </para>
    /// <para>
    /// The comparison ignores case on Windows and macOS, whose default file systems do, so that
    /// <c>Min.PDB</c> and <c>Min.pdb</c> are treated as the same file there; elsewhere it is exact.
    /// </para>
    /// </remarks>
    public static bool IsOutputPath(string pdbPath, string outputPath)
    {
        StringComparison comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        string? path = Path.GetFullPath(outputPath);
        for (int links = 0; path is not null && links <= MaxSymbolicLinks; links++)
        {
            if (string.Equals(pdbPath, path, comparison))
            {
                return true;
            }

            path = TryGetSymbolicLinkTarget(path);
        }

        return false;
    }

    /// <summary>
    /// Gets the full path of the target of the symbolic link at <paramref name="path"/>, or <see langword="null"/>
    /// when there is no link there or nothing exists at <paramref name="path"/>.
    /// </summary>
    /// <remarks>Any other failure to read the link propagates.</remarks>
    private static string? TryGetSymbolicLinkTarget(string path)
    {
        try
        {
            return File.ResolveLinkTarget(path, returnFinalTarget: false)?.FullName;
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Writes the image to <paramref name="outputPath"/>, then writes <paramref name="portablePdb"/> to
    /// <paramref name="pdbPath"/> or, when there is no PDB, deletes the PDB of the image that was replaced, as
    /// <see cref="OutputWriter.Write"/> decides.
    /// </summary>
    /// <param name="outputPath">The path of the image.</param>
    /// <param name="pdbPath">The path of the PDB, from <see cref="GetPdbPath"/>.</param>
    /// <param name="writeImage">Writes the image to the stream it is given.</param>
    /// <param name="portablePdb">The PDB, or <see langword="null"/> when none was produced.</param>
    /// <returns>What was written or deleted.</returns>
    public static OutputWriteResult Write(string outputPath, string pdbPath, Action<Stream> writeImage, ImmutableArray<byte>? portablePdb) =>
        OutputWriter.Write(new OutputFiles(outputPath, pdbPath), writeImage, portablePdb);

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

    private static FileStream? OpenReadIfExists(string path) => File.Exists(path) ? File.OpenRead(path) : null;

    /// <summary>The output file and the PDB file, as the streams <see cref="OutputWriter"/> writes through.</summary>
    private sealed class OutputFiles(string outputPath, string pdbPath) : IOutputStreams
    {
        public bool PdbPathIsOutputPath => IsOutputPath(pdbPath, outputPath);

        public Stream? OpenExistingOutput() => OpenReadIfExists(outputPath);

        public Stream CreateOutput() => File.Create(outputPath);

        public void WritePdb(ImmutableArray<byte> pdb) => OutputFileWriter.WritePdb(pdbPath, GetTemporaryPdbPath(pdbPath), pdb);

        public Stream? OpenExistingPdb() => OpenReadIfExists(pdbPath);

        public bool TryDeletePdb() => TryDelete(pdbPath);
    }
}
