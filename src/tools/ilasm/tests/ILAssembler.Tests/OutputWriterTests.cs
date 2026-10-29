// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Xunit;

namespace ILAssembler.Tests;

/// <summary>
/// Tests of <see cref="OutputWriter"/> on in-memory streams, without the file system: the order of the writes,
/// which existing PDB is deleted, and the refusal to overwrite the output with its PDB.
/// </summary>
public class OutputWriterTests
{
    private const string CloseOutput = "CloseOutput";

    private static readonly byte[] s_image = [0x4D, 0x5A, 0x01, 0x02];
    private static readonly ImmutableArray<byte> s_pdb = [0x42, 0x53, 0x4A, 0x42, 0x03];
    private static readonly byte[] s_stale = [0xDE, 0xAD];

    // An image and the PDB its CodeView entry refers to, a second such pair with another PDB id, and an image
    // without a debug directory.
    private static readonly (ImmutableArray<byte> Image, ImmutableArray<byte> Pdb) s_pair = DocumentCompilerTestHelpers.CompileImageAndPdb("A");
    private static readonly (ImmutableArray<byte> Image, ImmutableArray<byte> Pdb) s_otherPair = DocumentCompilerTestHelpers.CompileImageAndPdb("B");
    private static readonly ImmutableArray<byte> s_imageWithoutPdb = DocumentCompilerTestHelpers.CompileImageWithoutPdb("A");

    private static void WriteImage(Stream stream) => stream.Write(s_image);

    // The previous output is the image of the PDB at the PDB path.
    private static MemoryOutputStreams ImageAndItsPdb() => new() { Output = s_pair.Image.ToArray(), Pdb = s_pair.Pdb.ToArray() };

    // The PDB id (GUID and stamp) that an image's CodeView entry refers to, and the id of a Portable PDB, read
    // without the writer's own readers.
    private static BlobContentId CodeViewPdbId(ImmutableArray<byte> image)
    {
        using var reader = new PEReader(image);
        DebugDirectoryEntry codeView = reader.ReadDebugDirectory().Single(entry => entry.Type == DebugDirectoryEntryType.CodeView);
        return new BlobContentId(reader.ReadCodeViewDebugDirectoryData(codeView).Guid, codeView.Stamp);
    }

    private static BlobContentId PortablePdbId(ImmutableArray<byte> pdb)
    {
        using MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbImage(pdb);
        return new BlobContentId(provider.GetMetadataReader().DebugMetadataHeader!.Id);
    }

    [Fact]
    public void Write_WithPdb_WritesTheImageAndThePdb()
    {
        var output = new MemoryOutputStreams();

        OutputWriteResult result = OutputWriter.Write(output, WriteImage, s_pdb);

        Assert.Equal(OutputWriteResult.ImageAndPdbWritten, result);
        Assert.Equal(s_image, output.Output);
        Assert.Equal(s_pdb.ToArray(), output.Pdb);
    }

    [Fact]
    public void Write_WithPdb_WritesThePdbAfterTheImageIsClosed()
    {
        var output = new MemoryOutputStreams();

        OutputWriter.Write(output, WriteImage, s_pdb);

        Assert.Equal(new[] { nameof(IOutputStreams.CreateOutput), CloseOutput, nameof(IOutputStreams.WritePdb) }, output.Calls);
    }

    [Fact]
    public void Write_WithPdb_DoesNotReadTheExistingOutput()
    {
        MemoryOutputStreams output = ImageAndItsPdb();

        OutputWriter.Write(output, WriteImage, s_pdb);

        Assert.DoesNotContain(nameof(IOutputStreams.OpenExistingOutput), output.Calls);
    }

    [Fact]
    public void Write_WithoutPdb_DeletesThePdbOfTheImageItReplaces()
    {
        MemoryOutputStreams output = ImageAndItsPdb();

        OutputWriteResult result = OutputWriter.Write(output, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWrittenStalePdbDeleted, result);
        Assert.Null(output.Pdb);
        Assert.Equal(s_image, output.Output);
    }

    [Fact]
    public void Write_WithoutPdb_ReadsTheReplacedImageBeforeCreatingTheOutput()
    {
        MemoryOutputStreams output = ImageAndItsPdb();

        OutputWriter.Write(output, WriteImage, portablePdb: null);

        int read = output.Calls.IndexOf(nameof(IOutputStreams.OpenExistingOutput));
        int create = output.Calls.IndexOf(nameof(IOutputStreams.CreateOutput));
        Assert.True(read >= 0 && read < create, string.Join(", ", output.Calls));
    }

    [Fact]
    public void Write_WithoutPdb_KeepsAPdbWhenNoImageIsReplaced()
    {
        var output = new MemoryOutputStreams { Pdb = s_pair.Pdb.ToArray() };

        OutputWriteResult result = OutputWriter.Write(output, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.Equal(s_pair.Pdb.ToArray(), output.Pdb);
    }

    [Fact]
    public void Write_WithoutPdb_KeepsAPdbThatTheReplacedImageDoesNotReference()
    {
        // The replaced image refers to a PDB, but not to the one at the PDB path.
        Assert.NotEqual(PortablePdbId(s_pair.Pdb), CodeViewPdbId(s_otherPair.Image));
        var output = new MemoryOutputStreams { Output = s_otherPair.Image.ToArray(), Pdb = s_pair.Pdb.ToArray() };

        OutputWriteResult result = OutputWriter.Write(output, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.Equal(s_pair.Pdb.ToArray(), output.Pdb);
    }

    [Fact]
    public void Write_WithoutPdb_KeepsAPdbWhenTheReplacedImageHasNoDebugDirectory()
    {
        var output = new MemoryOutputStreams { Output = s_imageWithoutPdb.ToArray(), Pdb = s_pair.Pdb.ToArray() };

        OutputWriteResult result = OutputWriter.Write(output, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.Equal(s_pair.Pdb.ToArray(), output.Pdb);
    }

    [Fact]
    public void Write_WithoutPdb_KeepsAPdbWhenTheReplacedOutputIsNotAnImage()
    {
        var output = new MemoryOutputStreams { Output = s_stale, Pdb = s_pair.Pdb.ToArray() };

        OutputWriteResult result = OutputWriter.Write(output, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.Equal(s_pair.Pdb.ToArray(), output.Pdb);
    }

    [Fact]
    public void Write_WithoutPdb_KeepsAPdbWhenTheReplacedOutputIsACoffObject()
    {
        // System.Reflection.Metadata reads bytes that do not start with "MZ" as a COFF object file, which has no
        // PE header and so no debug directory. Twenty zero bytes read as one with no sections.
        byte[] coff = new byte[20];
        using (var reader = new PEReader(coff.ToImmutableArray()))
        {
            Assert.True(reader.PEHeaders.IsCoffOnly);
        }

        var output = new MemoryOutputStreams { Output = coff, Pdb = s_pair.Pdb.ToArray() };

        OutputWriteResult result = OutputWriter.Write(output, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.Equal(s_pair.Pdb.ToArray(), output.Pdb);
    }

    [Fact]
    public void Write_WithoutPdb_KeepsAPdbWhenTheReplacedOutputIsLongerThanAnImageCanBe()
    {
        // System.Reflection.Metadata rejects a stream longer than int.MaxValue bytes with an ArgumentException.
        var output = new MemoryOutputStreams
        {
            Pdb = s_pair.Pdb.ToArray(),
            OpenExistingOutputOverride = () => new LengthOnlyStream(int.MaxValue + 1L),
        };

        OutputWriteResult result = OutputWriter.Write(output, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.Equal(s_pair.Pdb.ToArray(), output.Pdb);
    }

    [Fact]
    public void Write_WithoutPdb_KeepsAFileLongerThanAPdbCanBe()
    {
        var output = new MemoryOutputStreams
        {
            Output = s_pair.Image.ToArray(),
            OpenExistingPdbOverride = () => new LengthOnlyStream(int.MaxValue + 1L),
        };

        OutputWriteResult result = OutputWriter.Write(output, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.DoesNotContain(nameof(IOutputStreams.TryDeletePdb), output.Calls);
    }

    [Fact]
    public void Write_WithoutPdb_KeepsAFileThatIsNotAPortablePdb()
    {
        var output = new MemoryOutputStreams { Output = s_pair.Image.ToArray(), Pdb = s_stale };

        OutputWriteResult result = OutputWriter.Write(output, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.Equal(s_stale, output.Pdb);
    }

    [Fact]
    public void Write_WithoutPdb_DeletesNothingWhenThereIsNoPdb()
    {
        var output = new MemoryOutputStreams { Output = s_pair.Image.ToArray() };

        OutputWriteResult result = OutputWriter.Write(output, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.DoesNotContain(nameof(IOutputStreams.TryDeletePdb), output.Calls);
    }

    public static TheoryData<Type> OpenFailures { get; } = new() { typeof(IOException), typeof(UnauthorizedAccessException) };

    [Theory]
    [MemberData(nameof(OpenFailures))]
    public void Write_WithoutPdb_KeepsAPdbWhenTheReplacedOutputCannotBeOpened(Type exceptionType)
    {
        var output = new MemoryOutputStreams
        {
            Pdb = s_pair.Pdb.ToArray(),
            OpenExistingOutputOverride = () => throw (Exception)Activator.CreateInstance(exceptionType)!,
        };

        OutputWriteResult result = OutputWriter.Write(output, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.Equal(s_pair.Pdb.ToArray(), output.Pdb);
    }

    [Theory]
    [MemberData(nameof(OpenFailures))]
    public void Write_WithoutPdb_KeepsAPdbThatCannotBeOpened(Type exceptionType)
    {
        var output = new MemoryOutputStreams
        {
            Output = s_pair.Image.ToArray(),
            OpenExistingPdbOverride = () => throw (Exception)Activator.CreateInstance(exceptionType)!,
        };

        OutputWriteResult result = OutputWriter.Write(output, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.DoesNotContain(nameof(IOutputStreams.TryDeletePdb), output.Calls);
    }

    [Fact]
    public void Write_WithoutPdb_ReportsAPdbThatCannotBeDeletedAsNotDeleted()
    {
        MemoryOutputStreams output = ImageAndItsPdb();
        output.PdbDeleteFails = true;

        OutputWriteResult result = OutputWriter.Write(output, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.Equal(s_pair.Pdb.ToArray(), output.Pdb);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Write_WhenTheImageWriteFails_LeavesTheExistingPdbUnchanged(bool withPdb)
    {
        MemoryOutputStreams output = ImageAndItsPdb();

        Assert.Throws<IOException>(() => OutputWriter.Write(
            output,
            stream =>
            {
                stream.WriteByte(0x4D);
                throw new IOException("Injected image write failure");
            },
            withPdb ? s_pdb : null));

        Assert.Equal(s_pair.Pdb.ToArray(), output.Pdb);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Write_WhenClosingTheImageFails_LeavesTheExistingPdbUnchanged(bool withPdb)
    {
        // The image is closed before the PDB is touched: a PDB written, or the replaced image's PDB deleted,
        // before the image stream is disposed would show here.
        MemoryOutputStreams output = ImageAndItsPdb();
        output.OutputCloseFails = true;

        Assert.Throws<IOException>(() => OutputWriter.Write(output, WriteImage, withPdb ? s_pdb : null));

        Assert.Equal(s_pair.Pdb.ToArray(), output.Pdb);
    }

    [Fact]
    public void Write_WithPdbWhenThePdbPathNamesTheOutput_WritesNothing()
    {
        var output = new MemoryOutputStreams { Output = s_stale, PdbPathIsOutputPath = true };

        OutputWriteResult result = OutputWriter.Write(output, WriteImage, s_pdb);

        Assert.Equal(OutputWriteResult.PdbWouldOverwriteOutput, result);
        Assert.Equal(s_stale, output.Output);
        Assert.Empty(output.Calls);
    }

    [Fact]
    public void Write_WithoutPdbWhenThePdbPathNamesTheOutput_WritesTheImageAndDoesNotReadTheReplacedOutput()
    {
        // The file at the PDB path is the new image, so the replaced output's PDB id must not be compared with it.
        var output = new MemoryOutputStreams { Output = s_pair.Image.ToArray(), PdbPathIsOutputPath = true };

        OutputWriteResult result = OutputWriter.Write(output, WriteImage, portablePdb: null);

        Assert.Equal(OutputWriteResult.ImageWritten, result);
        Assert.Equal(s_image, output.Output);
        Assert.DoesNotContain(nameof(IOutputStreams.OpenExistingOutput), output.Calls);
    }

    // The output and the PDB in memory, recording the calls made on it in order, with CloseOutput when the output
    // stream is disposed. Output and Pdb are the bytes at each path, or null when there is no file; creating the
    // output empties it, as creating a file does, and it holds what was written once the stream is closed.
    private sealed class MemoryOutputStreams : IOutputStreams
    {
        public byte[]? Output { get; set; }

        public byte[]? Pdb { get; set; }

        public bool PdbPathIsOutputPath { get; init; }

        public bool OutputCloseFails { get; set; }

        public bool PdbDeleteFails { get; set; }

        // Called instead of opening Output or Pdb, to supply a stream that cannot be held in memory or to fail.
        public Func<Stream>? OpenExistingOutputOverride { get; init; }

        public Func<Stream>? OpenExistingPdbOverride { get; init; }

        public List<string> Calls { get; } = [];

        public Stream? OpenExistingOutput()
        {
            Calls.Add(nameof(OpenExistingOutput));
            return OpenExistingOutputOverride is { } open ? open() : Output is null ? null : new MemoryStream(Output, writable: false);
        }

        public Stream CreateOutput()
        {
            Calls.Add(nameof(CreateOutput));
            Output = [];
            return new OutputStream(this);
        }

        public void WritePdb(ImmutableArray<byte> pdb)
        {
            Calls.Add(nameof(WritePdb));
            Pdb = pdb.ToArray();
        }

        public Stream? OpenExistingPdb()
        {
            Calls.Add(nameof(OpenExistingPdb));
            return OpenExistingPdbOverride is { } open ? open() : Pdb is null ? null : new MemoryStream(Pdb, writable: false);
        }

        public bool TryDeletePdb()
        {
            Calls.Add(nameof(TryDeletePdb));
            if (PdbDeleteFails)
            {
                return false;
            }

            Pdb = null;
            return true;
        }

        private sealed class OutputStream(MemoryOutputStreams owner) : MemoryStream
        {
            private bool _closed;

            protected override void Dispose(bool disposing)
            {
                if (disposing && !_closed)
                {
                    _closed = true;
                    owner.Output = ToArray();
                    owner.Calls.Add(CloseOutput);
                    base.Dispose(disposing);
                    if (owner.OutputCloseFails)
                    {
                        throw new IOException("Injected image close failure");
                    }

                    return;
                }

                base.Dispose(disposing);
            }
        }
    }

    // A readable, seekable stream that reports a length and holds no data, so a length check can be tested without
    // that many bytes.
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
