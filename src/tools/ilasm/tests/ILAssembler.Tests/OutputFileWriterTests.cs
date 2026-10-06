// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;

namespace ILAssembler.Tests;

/// <summary>
/// Tests of <see cref="OutputFileWriter"/> on real files: the PDB path, the temporary file and rename, deletion,
/// and how paths that name the same file are recognized. The decisions it delegates to <see cref="OutputWriter"/>
/// are tested on streams in <see cref="OutputWriterTests"/>.
/// </summary>
public sealed class OutputFileWriterTests : IDisposable
{
    private static readonly byte[] s_image = [0x4D, 0x5A, 0x01, 0x02];
    private static readonly ImmutableArray<byte> s_pdb = [0x42, 0x53, 0x4A, 0x42, 0x03];
    private static readonly byte[] s_stale = [0xDE, 0xAD];

    // An image and the PDB its CodeView entry refers to.
    private static readonly (ImmutableArray<byte> Image, ImmutableArray<byte> Pdb) s_pair = DocumentCompilerTestHelpers.CompileImageAndPdb("A");

    private readonly string _directory = Directory.CreateTempSubdirectory("ilasm-output-").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private string PathOf(string fileName) => Path.Combine(_directory, fileName);

    private string[] FileNames() => Directory.EnumerateFileSystemEntries(_directory).Select(Path.GetFileName).Order().ToArray()!;

    private static void WriteImage(Stream stream) => stream.Write(s_image);

    [Theory]
    [InlineData("Min.dll", "Min.pdb")]
    [InlineData("Min", "Min.pdb")]
    [InlineData("a.b/Min", "a.b/Min.pdb")]
    public void GetPdbPath_ReplacesTheExtensionAndReturnsTheFullPath(string outputPath, string expectedPdbPath)
    {
        Assert.Equal(Path.GetFullPath(expectedPdbPath), OutputFileWriter.GetPdbPath(outputPath));
    }

    [Fact]
    public void Write_WithPdb_WritesTheImageAndThePdbBesideIt()
    {
        string outputPath = PathOf("Min.dll");
        string pdbPath = OutputFileWriter.GetPdbPath(outputPath);

        OutputWriteResult result = OutputFileWriter.Write(outputPath, pdbPath, WriteImage, s_pdb);

        Assert.Equal(OutputWriteResult.ImageAndPdbWritten, result);
        Assert.Equal(s_image, File.ReadAllBytes(outputPath));
        Assert.Equal(s_pdb.ToArray(), File.ReadAllBytes(pdbPath));
        Assert.Equal(new[] { "Min.dll", "Min.pdb" }, FileNames());
    }

    [Fact]
    public void Write_WithPdb_ReplacesAnExistingPdb()
    {
        string outputPath = PathOf("Min.dll");
        string pdbPath = OutputFileWriter.GetPdbPath(outputPath);
        File.WriteAllBytes(pdbPath, s_stale);

        OutputFileWriter.Write(outputPath, pdbPath, WriteImage, s_pdb);

        Assert.Equal(s_pdb.ToArray(), File.ReadAllBytes(pdbPath));
    }

    [Fact]
    public void Write_WithPdb_RenamesANewFileOverTheExistingPdbInsteadOfRewritingIt()
    {
        // Renaming a completed temporary file over the PDB path is what keeps a failed write from leaving a
        // partial PDB. It is observable as a reader of the old PDB still seeing the old content afterwards.
        // Renaming over a file that is open depends on the Windows version, so this runs elsewhere.
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        string outputPath = PathOf("Min.dll");
        string pdbPath = OutputFileWriter.GetPdbPath(outputPath);
        File.WriteAllBytes(pdbPath, s_stale);
        using var oldPdbReader = new FileStream(pdbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

        OutputFileWriter.Write(outputPath, pdbPath, WriteImage, s_pdb);

        byte[] oldContent = new byte[s_stale.Length];
        oldPdbReader.ReadExactly(oldContent);
        Assert.Equal(s_stale, oldContent);
        Assert.Equal(s_pdb.ToArray(), File.ReadAllBytes(pdbPath));
    }

    [Fact]
    public void Write_WithoutPdb_DeletesThePdbOfTheImageItReplaces()
    {
        string outputPath = PathOf("Min.dll");
        string pdbPath = OutputFileWriter.GetPdbPath(outputPath);
        File.WriteAllBytes(outputPath, s_pair.Image.ToArray());
        File.WriteAllBytes(pdbPath, s_pair.Pdb.ToArray());

        OutputWriteResult result = OutputFileWriter.Write(outputPath, pdbPath, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWrittenStalePdbDeleted, result);
        Assert.Equal(new[] { "Min.dll" }, FileNames());
    }

    [Fact]
    public void Write_WhenThePdbCannotBeWritten_RemovesOnlyItsTemporaryFile()
    {
        string outputPath = PathOf("Min.dll");
        string pdbPath = OutputFileWriter.GetPdbPath(outputPath);

        // A directory at the PDB path makes the final rename fail after the temporary file is written.
        Directory.CreateDirectory(pdbPath);

        Assert.ThrowsAny<Exception>(() => OutputFileWriter.Write(outputPath, pdbPath, WriteImage, s_pdb));

        Assert.Equal(new[] { "Min.dll", "Min.pdb" }, FileNames());
        Assert.True(Directory.Exists(pdbPath));
    }

    [Fact]
    public void WritePdb_WhenItsTemporaryPathExists_LeavesThePdbUnchanged()
    {
        string pdbPath = PathOf("Min.pdb");
        string temporaryPath = PathOf("Min.pdb.existing.tmp");
        File.WriteAllBytes(pdbPath, s_stale);
        File.WriteAllBytes(temporaryPath, s_image);

        Assert.Throws<IOException>(() => OutputFileWriter.WritePdb(pdbPath, temporaryPath, s_pdb));

        Assert.Equal(s_stale, File.ReadAllBytes(pdbPath));
    }

    [Fact]
    public void WritePdb_WhenItsTemporaryPathExists_KeepsTheFileThere()
    {
        string pdbPath = PathOf("Min.pdb");
        string temporaryPath = PathOf("Min.pdb.existing.tmp");
        File.WriteAllBytes(temporaryPath, s_image);

        Assert.Throws<IOException>(() => OutputFileWriter.WritePdb(pdbPath, temporaryPath, s_pdb));

        Assert.Equal(s_image, File.ReadAllBytes(temporaryPath));
    }

    [Fact]
    public void Write_WithPdbNamedLikeTheOutput_WritesNothing()
    {
        string outputPath = PathOf("Min.pdb");
        string pdbPath = OutputFileWriter.GetPdbPath(outputPath);
        File.WriteAllBytes(outputPath, s_stale);

        OutputWriteResult result = OutputFileWriter.Write(outputPath, pdbPath, WriteImage, s_pdb);

        Assert.Equal(OutputWriteResult.PdbWouldOverwriteOutput, result);
        Assert.Equal(s_stale, File.ReadAllBytes(outputPath));
    }

    [Fact]
    public void Write_WithoutPdb_NeverDeletesTheOutputItWrote()
    {
        // The output is named like its PDB, the image it replaces refers to a PDB, and the bytes written are that
        // PDB's: the file at the PDB path then reads as the replaced image's PDB, but it is the new output.
        string outputPath = PathOf("Min.pdb");
        string pdbPath = OutputFileWriter.GetPdbPath(outputPath);
        File.WriteAllBytes(outputPath, s_pair.Image.ToArray());

        OutputWriteResult result = OutputFileWriter.Write(outputPath, pdbPath, stream => stream.Write(s_pair.Pdb.AsSpan()), portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.Equal(s_pair.Pdb.ToArray(), File.ReadAllBytes(outputPath));
    }

    [Fact]
    public void Write_WithPdb_TreatsAnOutputDifferingFromThePdbOnlyInCaseAsTheDefaultFileSystemDoes()
    {
        // Min.PDB and Min.pdb are one file on the default Windows and macOS file systems, and two elsewhere.
        bool caseInsensitive = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();
        string outputPath = PathOf("Min.PDB");
        string pdbPath = OutputFileWriter.GetPdbPath(outputPath);

        OutputWriteResult result = OutputFileWriter.Write(outputPath, pdbPath, WriteImage, s_pdb);

        Assert.Equal(caseInsensitive ? OutputWriteResult.PdbWouldOverwriteOutput : OutputWriteResult.ImageAndPdbWritten, result);
    }
}
