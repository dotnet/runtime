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
/// into place, a symbolic or hard link at <c>&lt;output&gt;.pdb</c> is replaced by a regular file and the link's
/// target keeps its old content; deleting a stale PDB likewise removes a symbolic link, not its target. A stale PDB
/// that cannot be deleted is left in place without failing the write.
/// </para>
/// </remarks>
internal static class OutputFileWriter
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
