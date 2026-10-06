// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using Xunit;

namespace ILAssembler.Tests;

public sealed class OutputWriterTests : IDisposable
{
    private static readonly byte[] s_image = [0x4D, 0x5A, 0x01, 0x02];
    private static readonly ImmutableArray<byte> s_pdb = [0x42, 0x53, 0x4A, 0x42, 0x03];
    private static readonly byte[] s_stale = [0xDE, 0xAD];

    // An image and the PDB its CodeView entry refers to, a second such pair with another PDB id, and an image
    // without a debug directory.
    private static readonly (ImmutableArray<byte> Image, ImmutableArray<byte> Pdb) s_pair = DocumentCompilerTestHelpers.CompileImageAndPdb("A");
    private static readonly (ImmutableArray<byte> Image, ImmutableArray<byte> Pdb) s_otherPair = DocumentCompilerTestHelpers.CompileImageAndPdb("B");
    private static readonly ImmutableArray<byte> s_imageWithoutPdb = DocumentCompilerTestHelpers.CompileImageWithoutPdb("A");

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
        Assert.Equal(Path.GetFullPath(expectedPdbPath), OutputWriter.GetPdbPath(outputPath));
    }

    [Fact]
    public void Write_WithPdb_WritesTheImageAndThenThePdb()
    {
        string outputPath = PathOf("Min.dll");
        string pdbPath = OutputWriter.GetPdbPath(outputPath);
        bool pdbExistedDuringImageWrite = true;

        OutputWriteResult result = OutputWriter.Write(
            outputPath,
            pdbPath,
            stream =>
            {
                pdbExistedDuringImageWrite = File.Exists(pdbPath);
                WriteImage(stream);
            },
            s_pdb);

        Assert.Equal(OutputWriteResult.ImageAndPdbWritten, result);
        Assert.False(pdbExistedDuringImageWrite);
        Assert.Equal(s_image, File.ReadAllBytes(outputPath));
        Assert.Equal(s_pdb.ToArray(), File.ReadAllBytes(pdbPath));
        Assert.Equal(new[] { "Min.dll", "Min.pdb" }, FileNames());
    }

    [Fact]
    public void Write_WithPdb_ReplacesAnExistingPdb()
    {
        string outputPath = PathOf("Min.dll");
        string pdbPath = OutputWriter.GetPdbPath(outputPath);
        File.WriteAllBytes(pdbPath, s_stale);

        OutputWriter.Write(outputPath, pdbPath, WriteImage, s_pdb);

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
        string pdbPath = OutputWriter.GetPdbPath(outputPath);
        File.WriteAllBytes(pdbPath, s_stale);
        using var oldPdbReader = new FileStream(pdbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

        OutputWriter.Write(outputPath, pdbPath, WriteImage, s_pdb);

        byte[] oldContent = new byte[s_stale.Length];
        oldPdbReader.ReadExactly(oldContent);
        Assert.Equal(s_stale, oldContent);
        Assert.Equal(s_pdb.ToArray(), File.ReadAllBytes(pdbPath));
    }

    [Fact]
    public void Write_WithoutPdb_DeletesThePdbOfTheImageItReplaces()
    {
        string outputPath = PathOf("Min.dll");
        string pdbPath = OutputWriter.GetPdbPath(outputPath);
        File.WriteAllBytes(outputPath, s_pair.Image.ToArray());
        File.WriteAllBytes(pdbPath, s_pair.Pdb.ToArray());

        OutputWriteResult result = OutputWriter.Write(outputPath, pdbPath, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWrittenStalePdbDeleted, result);
        Assert.Equal(new[] { "Min.dll" }, FileNames());
    }

    [Fact]
    public void Write_WithoutPdb_KeepsAPdbWhenNoImageIsReplaced()
    {
        string outputPath = PathOf("Min.dll");
        string pdbPath = OutputWriter.GetPdbPath(outputPath);
        File.WriteAllBytes(pdbPath, s_pair.Pdb.ToArray());

        OutputWriteResult result = OutputWriter.Write(outputPath, pdbPath, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.Equal(s_pair.Pdb.ToArray(), File.ReadAllBytes(pdbPath));
    }

    [Fact]
    public void Write_WithoutPdb_KeepsAPdbThatTheReplacedImageDoesNotReference()
    {
        string outputPath = PathOf("Min.dll");
        string pdbPath = OutputWriter.GetPdbPath(outputPath);
        File.WriteAllBytes(outputPath, s_otherPair.Image.ToArray());
        File.WriteAllBytes(pdbPath, s_pair.Pdb.ToArray());
        Assert.NotEqual(OutputWriter.TryReadPortablePdbId(pdbPath), OutputWriter.TryReadCodeViewPdbId(outputPath));

        OutputWriteResult result = OutputWriter.Write(outputPath, pdbPath, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.Equal(s_pair.Pdb.ToArray(), File.ReadAllBytes(pdbPath));
    }

    [Fact]
    public void Write_WithoutPdb_KeepsAPdbWhenTheReplacedImageHasNoDebugDirectory()
    {
        string outputPath = PathOf("Min.dll");
        string pdbPath = OutputWriter.GetPdbPath(outputPath);
        File.WriteAllBytes(outputPath, s_imageWithoutPdb.ToArray());
        File.WriteAllBytes(pdbPath, s_pair.Pdb.ToArray());

        OutputWriteResult result = OutputWriter.Write(outputPath, pdbPath, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.Equal(s_pair.Pdb.ToArray(), File.ReadAllBytes(pdbPath));
    }

    [Fact]
    public void Write_WithoutPdb_KeepsAPdbWhenTheReplacedFileIsNotAnImage()
    {
        string outputPath = PathOf("Min.dll");
        string pdbPath = OutputWriter.GetPdbPath(outputPath);
        File.WriteAllBytes(outputPath, s_stale);
        File.WriteAllBytes(pdbPath, s_pair.Pdb.ToArray());

        OutputWriteResult result = OutputWriter.Write(outputPath, pdbPath, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.Equal(s_pair.Pdb.ToArray(), File.ReadAllBytes(pdbPath));
    }

    [Fact]
    public void Write_WithoutPdb_KeepsAPdbWhenTheReplacedFileIsACoffObject()
    {
        // System.Reflection.Metadata reads a file that does not start with "MZ" as a COFF object file, which has no
        // PE header and so no debug directory. Twenty zero bytes read as one with no sections.
        string outputPath = PathOf("Min.dll");
        string pdbPath = OutputWriter.GetPdbPath(outputPath);
        File.WriteAllBytes(outputPath, new byte[20]);
        File.WriteAllBytes(pdbPath, s_pair.Pdb.ToArray());
        using (var coff = new PEReader(File.ReadAllBytes(outputPath).ToImmutableArray()))
        {
            Assert.True(coff.PEHeaders.IsCoffOnly);
        }

        OutputWriteResult result = OutputWriter.Write(outputPath, pdbPath, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.Equal(s_image, File.ReadAllBytes(outputPath));
        Assert.Equal(s_pair.Pdb.ToArray(), File.ReadAllBytes(pdbPath));
    }

    [Fact]
    public void TryReadCodeViewPdbId_ReturnsNullForAFileLongerThanAnImageCanBe()
    {
        // System.Reflection.Metadata rejects a stream longer than int.MaxValue bytes with an ArgumentException.
        using var stream = new LengthOnlyStream(int.MaxValue + 1L);

        Assert.Null(OutputWriter.TryReadCodeViewPdbId(stream));
    }

    [Fact]
    public void TryReadPortablePdbId_ReturnsNullForAFileLongerThanAPdbCanBe()
    {
        using var stream = new LengthOnlyStream(int.MaxValue + 1L);

        Assert.Null(OutputWriter.TryReadPortablePdbId(stream));
    }

    [Fact]
    public void Write_WithoutPdb_KeepsAFileThatIsNotAPortablePdb()
    {
        string outputPath = PathOf("Min.dll");
        string pdbPath = OutputWriter.GetPdbPath(outputPath);
        File.WriteAllBytes(outputPath, s_pair.Image.ToArray());
        File.WriteAllBytes(pdbPath, s_stale);

        OutputWriteResult result = OutputWriter.Write(outputPath, pdbPath, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.Equal(s_stale, File.ReadAllBytes(pdbPath));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Write_WhenTheImageWriteFails_LeavesTheExistingPdbUnchanged(bool withPdb)
    {
        string outputPath = PathOf("Min.dll");
        string pdbPath = OutputWriter.GetPdbPath(outputPath);
        File.WriteAllBytes(pdbPath, s_stale);

        Assert.Throws<IOException>(() => OutputWriter.Write(
            outputPath,
            pdbPath,
            stream =>
            {
                stream.WriteByte(0x4D);
                throw new IOException("Injected image write failure");
            },
            withPdb ? s_pdb : null));

        Assert.Equal(s_stale, File.ReadAllBytes(pdbPath));
        Assert.Equal(new[] { "Min.dll", "Min.pdb" }, FileNames());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Write_WhenClosingTheImageFails_LeavesTheExistingPdbUnchanged(bool withPdb)
    {
        // The image is closed before the PDB is touched: a PDB written, or the replaced image's PDB deleted,
        // before the image stream is disposed would show here.
        string outputPath = PathOf("Min.dll");
        string pdbPath = OutputWriter.GetPdbPath(outputPath);
        File.WriteAllBytes(outputPath, s_pair.Image.ToArray());
        File.WriteAllBytes(pdbPath, s_pair.Pdb.ToArray());

        Assert.Throws<IOException>(() => OutputWriter.Write(
            outputPath,
            pdbPath,
            WriteImage,
            withPdb ? s_pdb : null,
            path => new ThrowOnDisposeStream(File.Create(path))));

        Assert.Equal(s_pair.Pdb.ToArray(), File.ReadAllBytes(pdbPath));
    }

    [Fact]
    public void Write_WhenThePdbCannotBeWritten_RemovesOnlyItsTemporaryFile()
    {
        string outputPath = PathOf("Min.dll");
        string pdbPath = OutputWriter.GetPdbPath(outputPath);

        // A directory at the PDB path makes the final rename fail after the temporary file is written.
        Directory.CreateDirectory(pdbPath);

        Assert.ThrowsAny<Exception>(() => OutputWriter.Write(outputPath, pdbPath, WriteImage, s_pdb));

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

        Assert.Throws<IOException>(() => OutputWriter.WritePdb(pdbPath, temporaryPath, s_pdb));

        Assert.Equal(s_stale, File.ReadAllBytes(pdbPath));
    }

    [Fact]
    public void WritePdb_WhenItsTemporaryPathExists_KeepsTheFileThere()
    {
        string pdbPath = PathOf("Min.pdb");
        string temporaryPath = PathOf("Min.pdb.existing.tmp");
        File.WriteAllBytes(temporaryPath, s_image);

        Assert.Throws<IOException>(() => OutputWriter.WritePdb(pdbPath, temporaryPath, s_pdb));

        Assert.Equal(s_image, File.ReadAllBytes(temporaryPath));
    }

    [Fact]
    public void Write_WithPdbNamedLikeTheOutput_WritesNothing()
    {
        string outputPath = PathOf("Min.pdb");
        string pdbPath = OutputWriter.GetPdbPath(outputPath);
        File.WriteAllBytes(outputPath, s_stale);

        OutputWriteResult result = OutputWriter.Write(outputPath, pdbPath, WriteImage, s_pdb);

        Assert.Equal(OutputWriteResult.PdbWouldOverwriteOutput, result);
        Assert.Equal(s_stale, File.ReadAllBytes(outputPath));
    }

    [Fact]
    public void Write_WithoutPdb_KeepsAnOutputNamedLikeThePdb()
    {
        string outputPath = PathOf("Min.pdb");
        string pdbPath = OutputWriter.GetPdbPath(outputPath);

        OutputWriteResult result = OutputWriter.Write(outputPath, pdbPath, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.Equal(s_image, File.ReadAllBytes(outputPath));
    }

    [Fact]
    public void Write_WithoutPdb_NeverDeletesTheOutputItWrote()
    {
        // The output is named like its PDB, the image it replaces refers to a PDB, and the bytes written are that
        // PDB's: the file at the PDB path then reads as the replaced image's PDB, but it is the new output.
        string outputPath = PathOf("Min.pdb");
        string pdbPath = OutputWriter.GetPdbPath(outputPath);
        File.WriteAllBytes(outputPath, s_pair.Image.ToArray());

        OutputWriteResult result = OutputWriter.Write(outputPath, pdbPath, stream => stream.Write(s_pair.Pdb.AsSpan()), portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.Equal(s_pair.Pdb.ToArray(), File.ReadAllBytes(outputPath));
    }

    [Fact]
    public void Write_WithPdb_TreatsAnOutputDifferingFromThePdbOnlyInCaseAsTheDefaultFileSystemDoes()
    {
        // Min.PDB and Min.pdb are one file on the default Windows and macOS file systems, and two elsewhere.
        bool caseInsensitive = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();
        string outputPath = PathOf("Min.PDB");
        string pdbPath = OutputWriter.GetPdbPath(outputPath);

        OutputWriteResult result = OutputWriter.Write(outputPath, pdbPath, WriteImage, s_pdb);

        Assert.Equal(caseInsensitive ? OutputWriteResult.PdbWouldOverwriteOutput : OutputWriteResult.ImageAndPdbWritten, result);
    }

    // A stream that writes through to another and fails when it is closed.
    private sealed class ThrowOnDisposeStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            inner.Dispose();
            throw new IOException("Injected image close failure");
        }
    }

    // A readable, seekable stream that reports a length and holds no data, so a length check can be tested without
    // a file of that size.
    private sealed class LengthOnlyStream(long length) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => length;
        public override long Position { get; set; }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException("The stream was read.");
        public override long Seek(long offset, SeekOrigin origin) => Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            _ => length + offset,
        };
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
