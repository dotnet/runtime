// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace ILAssembler.Tests;

/// <summary>
/// Runs the ilasm command line (<see cref="Program"/>) in process, end to end, and checks the files it writes: the PDB
/// beside the output, the image's debug directory, the deletion of a stale PDB, failed and error-tolerant builds, and
/// the refusal of an output named like its PDB. Each test has its own temporary directory, with the IL sources in
/// <c>src</c> and the outputs in <c>out</c>.
/// </summary>
public sealed class CommandLinePdbFileTests : IDisposable
{
    private const string Source = """
        .assembly extern System.Runtime
        {
        }

        .assembly TestPdbFile1
        {
        }

        .class public auto ansi beforefieldinit TestPdbFileType
               extends [System.Runtime]System.Object
        {
          .method public hidebysig static int32 Foo() cil managed
          {
             ldc.i4.1
             ret
          }
        }
        """;

    private const string SourceWithLineDirective = """
        .assembly extern System.Runtime
        {
        }

        .assembly TestPdbFileLine
        {
        }

        .class public auto ansi beforefieldinit TestPdbFileLineType
               extends [System.Runtime]System.Object
        {
          .method public hidebysig static int32 Foo() cil managed
          {
             .line 10,10 : 9,10 'TestPdbFileLine.cs'
             ldc.i4.1
             ret
          }
        }
        """;

    // Does not assemble: the branch target is not defined.
    private const string SourceWithError = """
        .assembly extern System.Runtime
        {
        }

        .assembly TestPdbFileError
        {
        }

        .class public auto ansi beforefieldinit TestPdbFileErrorType
               extends [System.Runtime]System.Object
        {
          .method public hidebysig static void Foo() cil managed
          {
             br UNDEFINED_LABEL
             ret
          }
        }
        """;

    // Does not assemble: the included file does not exist, so assembly ends with an exception.
    private const string SourceWithMissingInclude = """
        #include "TestPdbFileMissingInclude.inc"

        .assembly extern System.Runtime
        {
        }

        .assembly TestPdbFileMissingInclude
        {
        }
        """;

    // Has an error that the error-tolerant option (-ERR) assembles past: the exported type names an assembly
    // reference that is not declared.
    private const string SourceWithRecoverableError = """
        .assembly extern System.Runtime
        {
        }

        .assembly TestPdbFileRecoverableError
        {
        }

        .class extern public MissingType
        {
          .assembly extern MissingAssembly
        }

        .class public auto ansi beforefieldinit TestPdbFileRecoverableErrorType
               extends [System.Runtime]System.Object
        {
          .method public hidebysig static void Foo() cil managed
          {
             ret
          }
        }
        """;

    private readonly string _directory = Directory.CreateTempSubdirectory("ilasm-cli-").FullName;

    /// <summary>
    /// Whether a relative path leads from the working directory to the temporary directory, where each test's files
    /// are. On Windows there is none when the two are on different drives.
    /// </summary>
    public static bool TemporaryDirectoryHasARelativePath =>
        !Path.IsPathRooted(Path.GetRelativePath(Environment.CurrentDirectory, Path.GetTempPath()));

    public CommandLinePdbFileTests()
    {
        Directory.CreateDirectory(Path.Combine(_directory, "src"));
        Directory.CreateDirectory(Path.Combine(_directory, "out"));
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private string OutputPath(string fileName) => Path.Combine(_directory, "out", fileName);

    private string[] OutputFileNames() => Directory.EnumerateFileSystemEntries(Path.Combine(_directory, "out")).Select(Path.GetFileName).Order().ToArray()!;

    // Writes the source to src/<sourceName>.il and assembles it as "ilasm -nologo -quiet -dll -output=<outputPath>
    // <switches> <source>" does, returning the exit code.
    private int RunIlasm(string source, string sourceName, string outputPath, string switches)
    {
        string sourcePath = Path.Combine(_directory, "src", sourceName + ".il");
        File.WriteAllText(sourcePath, source);
        string[] args = ["-nologo", "-quiet", "-dll", $"-output={outputPath}", .. switches.Split(' ', StringSplitOptions.RemoveEmptyEntries), sourcePath];

        var command = new IlasmRootCommand();
        ParseResult result = command.Parse(NativeCommandLine.Normalize(args, command));
        Assert.Empty(result.Errors);
        return new Program(command, result).Run();
    }

    private int RunIlasm(string outputPath, string switches) => RunIlasm(Source, "TestPdbFile1", outputPath, switches);

    private static PEReader ReadImage(string path) => new(ImmutableArray.Create(File.ReadAllBytes(path)));

    private static BlobContentId ReadPdbId(string pdbPath)
    {
        using MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(File.ReadAllBytes(pdbPath)));
        return new BlobContentId(provider.GetMetadataReader().DebugMetadataHeader!.Id);
    }

    // The path of the PDB that System.Reflection.Metadata finds for the image, as a debugger would find it.
    private static string? FindAssociatedPdb(string imagePath)
    {
        using PEReader pe = ReadImage(imagePath);
        if (!pe.TryOpenAssociatedPortablePdb(imagePath, File.OpenRead, out MetadataReaderProvider? provider, out string? pdbPath))
        {
            return null;
        }

        provider?.Dispose();
        return pdbPath;
    }

    private static bool HasDebuggableAttribute(string imagePath)
    {
        using PEReader pe = ReadImage(imagePath);
        MetadataReader reader = pe.GetMetadataReader();
        foreach (CustomAttributeHandle handle in reader.GetAssemblyDefinition().GetCustomAttributes())
        {
            CustomAttribute attribute = reader.GetCustomAttribute(handle);
            if (attribute.Constructor.Kind == HandleKind.MemberReference)
            {
                MemberReference constructor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                if (constructor.Parent.Kind == HandleKind.TypeReference &&
                    reader.GetString(reader.GetTypeReference((TypeReferenceHandle)constructor.Parent).Name) == "DebuggableAttribute")
                {
                    return true;
                }
            }
        }

        return false;
    }

    // With -DEBUG or -PDB the PDB is a file beside the output, the CodeView entry names its full path (without -DET)
    // and carries its id, and nothing is embedded in the image.
    [Theory]
    [InlineData("-debug")]
    [InlineData("-pdb")]
    public void PdbSwitch_WritesThePdbBesideTheOutputAndReferencesIt(string pdbSwitch)
    {
        string dll = OutputPath("TestPdbFile1.dll");
        string pdb = OutputPath("TestPdbFile1.pdb");

        Assert.Equal(0, RunIlasm(dll, pdbSwitch));

        Assert.Equal(new[] { "TestPdbFile1.dll", "TestPdbFile1.pdb" }, OutputFileNames());
        using PEReader pe = ReadImage(dll);
        ImmutableArray<DebugDirectoryEntry> entries = pe.ReadDebugDirectory();
        Assert.DoesNotContain(entries, entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
        DebugDirectoryEntry codeViewEntry = Assert.Single(entries, entry => entry.Type == DebugDirectoryEntryType.CodeView);
        CodeViewDebugDirectoryData codeView = pe.ReadCodeViewDebugDirectoryData(codeViewEntry);
        Assert.Equal(pdb, codeView.Path);
        BlobContentId pdbId = ReadPdbId(pdb);
        Assert.Equal((pdbId.Guid, pdbId.Stamp), (codeView.Guid, codeViewEntry.Stamp));
    }

    // With -DET the PDB is still written beside the output, but the CodeView entry names only its file name and
    // extension, so the image does not depend on the directory it is written to; it still carries the PDB's id.
    [Theory]
    [InlineData("-debug -det")]
    [InlineData("-pdb -det")]
    public void Deterministic_CodeViewEntryNamesThePdbFileNameOnly(string switches)
    {
        string dll = OutputPath("TestPdbFile1.dll");
        string pdb = OutputPath("TestPdbFile1.pdb");

        Assert.Equal(0, RunIlasm(dll, switches));

        Assert.Equal(new[] { "TestPdbFile1.dll", "TestPdbFile1.pdb" }, OutputFileNames());
        using PEReader pe = ReadImage(dll);
        DebugDirectoryEntry codeViewEntry = Assert.Single(pe.ReadDebugDirectory(), entry => entry.Type == DebugDirectoryEntryType.CodeView);
        CodeViewDebugDirectoryData codeView = pe.ReadCodeViewDebugDirectoryData(codeViewEntry);
        Assert.Equal("TestPdbFile1.pdb", codeView.Path);
        BlobContentId pdbId = ReadPdbId(pdb);
        Assert.Equal((pdbId.Guid, pdbId.Stamp), (codeView.Guid, codeViewEntry.Stamp));
    }

    // With -DET a relative output path still gives a CodeView entry with no directory part, and the PDB is written
    // beside the output. The output path is relative to the working directory of the test process, which is not the
    // output's directory. Where no relative path leads there, the test is skipped rather than run with an absolute path.
    [ConditionalFact(typeof(CommandLinePdbFileTests), nameof(TemporaryDirectoryHasARelativePath))]
    public void Deterministic_RelativeOutputPath_CodeViewEntryNamesThePdbFileNameOnly()
    {
        string directory = OutputPath("sub");
        Directory.CreateDirectory(directory);
        string relativeDll = Path.GetRelativePath(Environment.CurrentDirectory, Path.Combine(directory, "TestPdbFile1.dll"));
        Assert.False(Path.IsPathRooted(relativeDll), relativeDll);

        Assert.Equal(0, RunIlasm(relativeDll, "-debug -det"));

        Assert.Equal(new[] { "TestPdbFile1.dll", "TestPdbFile1.pdb" }, Directory.EnumerateFiles(directory).Select(Path.GetFileName).Order().ToArray());
        using PEReader pe = ReadImage(Path.Combine(directory, "TestPdbFile1.dll"));
        DebugDirectoryEntry codeViewEntry = Assert.Single(pe.ReadDebugDirectory(), entry => entry.Type == DebugDirectoryEntryType.CodeView);
        Assert.Equal("TestPdbFile1.pdb", pe.ReadCodeViewDebugDirectoryData(codeViewEntry).Path);
    }

    // With -DET a debugger still finds the PDB from the bare CodeView name: System.Reflection.Metadata probes the
    // image's directory for that name, and the PDB it opens has the id that the CodeView entry carries.
    [Fact]
    public void Deterministic_TheAssociatedPdbIsFoundBesideTheImage()
    {
        string directory = OutputPath("sub");
        Directory.CreateDirectory(directory);
        string dll = Path.Combine(directory, "TestPdbFile1.dll");

        Assert.Equal(0, RunIlasm(dll, "-debug -det"));

        using PEReader pe = ReadImage(dll);
        Assert.True(pe.TryOpenAssociatedPortablePdb(dll, File.OpenRead, out MetadataReaderProvider? provider, out string? pdbPath));
        using (provider)
        {
            Assert.Equal(Path.Combine(directory, "TestPdbFile1.pdb"), pdbPath);
            DebugDirectoryEntry codeViewEntry = Assert.Single(pe.ReadDebugDirectory(), entry => entry.Type == DebugDirectoryEntryType.CodeView);
            BlobContentId openedPdbId = new(provider!.GetMetadataReader().DebugMetadataHeader!.Id);
            Assert.Equal((pe.ReadCodeViewDebugDirectoryData(codeViewEntry).Guid, codeViewEntry.Stamp), (openedPdbId.Guid, openedPdbId.Stamp));
        }
    }

    // -DET gives byte-identical image and PDB files when the same source is assembled to two different directories.
    [Fact]
    public void Deterministic_AssemblingToTwoDirectories_WritesIdenticalImageAndPdb()
    {
        string firstDll = OutputPath(Path.Combine("first", "TestPdbFile1.dll"));
        string secondDll = OutputPath(Path.Combine("second", "TestPdbFile1.dll"));
        Directory.CreateDirectory(Path.GetDirectoryName(firstDll)!);
        Directory.CreateDirectory(Path.GetDirectoryName(secondDll)!);

        Assert.Equal(0, RunIlasm(firstDll, "-debug -det"));
        Assert.Equal(0, RunIlasm(secondDll, "-debug -det"));

        Assert.Equal(File.ReadAllBytes(firstDll), File.ReadAllBytes(secondDll));
        Assert.Equal(File.ReadAllBytes(Path.ChangeExtension(firstDll, ".pdb")), File.ReadAllBytes(Path.ChangeExtension(secondDll, ".pdb")));
    }

    // The debug directory is CodeView then PdbChecksum, and under -DET a Reproducible entry follows.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PdbSwitch_DebugDirectoryIsCodeViewThenPdbChecksumThenReproducibleWhenDeterministic(bool deterministic)
    {
        string dll = OutputPath("TestPdbFile1.dll");

        Assert.Equal(0, RunIlasm(dll, deterministic ? "-debug -det" : "-debug"));

        DebugDirectoryEntryType[] expected = deterministic
            ? [DebugDirectoryEntryType.CodeView, DebugDirectoryEntryType.PdbChecksum, DebugDirectoryEntryType.Reproducible]
            : [DebugDirectoryEntryType.CodeView, DebugDirectoryEntryType.PdbChecksum];
        using PEReader pe = ReadImage(dll);
        Assert.Equal(expected, pe.ReadDebugDirectory().Select(entry => entry.Type));
    }

    // The PdbChecksum entry is SHA-256 of the PDB file with its 20-byte id zeroed (PE-COFF.md).
    [Theory]
    [InlineData("-debug")]
    [InlineData("-debug -det")]
    public void PdbSwitch_PdbChecksumIsSha256OfThePdbFileWithItsIdZeroed(string switches)
    {
        string dll = OutputPath("TestPdbFile1.dll");
        string pdb = OutputPath("TestPdbFile1.pdb");

        Assert.Equal(0, RunIlasm(dll, switches));

        using PEReader pe = ReadImage(dll);
        DebugDirectoryEntry checksumEntry = Assert.Single(pe.ReadDebugDirectory(), entry => entry.Type == DebugDirectoryEntryType.PdbChecksum);
        PdbChecksumDebugDirectoryData checksum = pe.ReadPdbChecksumDebugDirectoryData(checksumEntry);
        byte[] pdbBytes = File.ReadAllBytes(pdb);
        using (MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(pdbBytes)))
        {
            DebugMetadataHeader header = provider.GetMetadataReader().DebugMetadataHeader!;
            Array.Clear(pdbBytes, header.IdStartOffset, header.Id.Length);
        }

        Assert.Equal("SHA256", checksum.AlgorithmName);
        Assert.Equal(SHA256.HashData(pdbBytes), checksum.Checksum.ToArray());
    }

    // -DET gives byte-identical image and PDB files when the same source is assembled twice to the same output.
    [Fact]
    public void Deterministic_AssemblingTwice_WritesIdenticalImageAndPdb()
    {
        string dll = OutputPath("TestPdbFile1.dll");
        string pdb = OutputPath("TestPdbFile1.pdb");
        Assert.Equal(0, RunIlasm(dll, "-debug -det"));
        byte[] firstImage = File.ReadAllBytes(dll);
        byte[] firstPdb = File.ReadAllBytes(pdb);

        Assert.Equal(0, RunIlasm(dll, "-debug -det"));

        Assert.Equal(firstImage, File.ReadAllBytes(dll));
        Assert.Equal(firstPdb, File.ReadAllBytes(pdb));
    }

    // Without -DEBUG or -PDB there is no PDB file and no debug directory, even with .line directives or -DET.
    [Theory]
    [InlineData(false, "")]
    [InlineData(false, "-det")]
    [InlineData(true, "")]
    [InlineData(true, "-det")]
    public void NoPdbSwitch_WritesNoPdbAndNoDebugDirectory(bool withLineDirective, string switches)
    {
        string dll = OutputPath("Output.dll");

        Assert.Equal(0, withLineDirective
            ? RunIlasm(SourceWithLineDirective, "TestPdbFileLine", dll, switches)
            : RunIlasm(dll, switches));

        Assert.Equal(new[] { "Output.dll" }, OutputFileNames());
        using PEReader pe = ReadImage(dll);
        DirectoryEntry debugTable = pe.PEHeaders.PEHeader!.DebugTableDirectory;
        Assert.Equal((0, 0), (debugTable.RelativeVirtualAddress, debugTable.Size));
    }

    // -PDB produces the PDB without adding a DebuggableAttribute; -DEBUG adds one.
    [Theory]
    [InlineData("-pdb", false)]
    [InlineData("-debug", true)]
    public void PdbSwitch_AddsADebuggableAttributeOnlyWithDebug(string pdbSwitch, bool expectAttribute)
    {
        string dll = OutputPath("TestPdbFile1.dll");

        Assert.Equal(0, RunIlasm(dll, pdbSwitch));

        Assert.Equal(expectAttribute, HasDebuggableAttribute(dll));
    }

    // A PDB left by an earlier build is replaced when ilasm writes a new one.
    [Fact]
    public void StalePdb_IsReplacedWhenAPdbIsWritten()
    {
        string dll = OutputPath("TestPdbFile1.dll");
        string pdb = OutputPath("TestPdbFile1.pdb");
        File.WriteAllBytes(pdb, [0xDE, 0xAD]);

        Assert.Equal(0, RunIlasm(dll, "-debug"));

        Assert.Equal(pdb, FindAssociatedPdb(dll));
    }

    // Without a PDB switch, a successful build deletes the PDB of the image it replaces.
    [Fact]
    public void StalePdb_IsDeletedWhenItsImageIsReplacedWithoutAPdb()
    {
        string dll = OutputPath("TestPdbFile1.dll");
        Assert.Equal(0, RunIlasm(dll, "-debug"));
        Assert.Equal(new[] { "TestPdbFile1.dll", "TestPdbFile1.pdb" }, OutputFileNames());

        Assert.Equal(0, RunIlasm(dll, string.Empty));

        Assert.Equal(new[] { "TestPdbFile1.dll" }, OutputFileNames());
    }

    // Without a PDB switch, a PDB beside an output that did not exist before belongs to no image ilasm replaced, so it
    // is kept.
    [Fact]
    public void UnrelatedPdb_IsKeptWhenNoImageIsReplaced()
    {
        string pdb = OutputPath("TestPdbFile1.pdb");
        Assert.Equal(0, RunIlasm(OutputPath("Other.dll"), "-debug"));
        File.Move(OutputPath("Other.pdb"), pdb);
        byte[] unrelatedPdb = File.ReadAllBytes(pdb);

        Assert.Equal(0, RunIlasm(OutputPath("TestPdbFile1.dll"), string.Empty));

        Assert.Equal(unrelatedPdb, File.ReadAllBytes(pdb));
    }

    // When assembly fails, the existing output and PDB are left as they were: after an error in the source and after
    // an exception (an include that does not exist).
    [Theory]
    [InlineData("Error", "")]
    [InlineData("Error", "-debug")]
    [InlineData("MissingInclude", "-debug")]
    public void FailedBuild_LeavesTheExistingOutputAndPdbUnchanged(string failure, string switches)
    {
        string source = failure == "Error" ? SourceWithError : SourceWithMissingInclude;
        string dll = OutputPath("Output.dll");
        string pdb = OutputPath("Output.pdb");
        byte[] existingDll = [0x4D, 0x5A];
        byte[] existingPdb = [0xDE, 0xAD];
        File.WriteAllBytes(dll, existingDll);
        File.WriteAllBytes(pdb, existingPdb);

        Assert.NotEqual(0, RunIlasm(source, "TestPdbFile" + failure, dll, switches));

        Assert.Equal(existingDll, File.ReadAllBytes(dll));
        Assert.Equal(existingPdb, File.ReadAllBytes(pdb));
    }

    // With -ERR, an image written despite errors is handled as after a successful build: with -DEBUG its PDB is
    // written beside it, and without a PDB switch the PDB of the image it replaces is deleted.
    [Theory]
    [InlineData("-err -debug")]
    [InlineData("-err")]
    public void ErrorTolerantBuildWithErrors_HandlesThePdbAsASuccessfulBuildDoes(string switches)
    {
        string dll = OutputPath("TestPdbFileRecoverableError.dll");
        string pdb = OutputPath("TestPdbFileRecoverableError.pdb");
        Assert.Equal(0, RunIlasm(dll, "-debug"));
        Assert.NotEqual(0, RunIlasm(SourceWithRecoverableError, "TestPdbFileRecoverableError", dll, "-debug"));

        Assert.Equal(0, RunIlasm(SourceWithRecoverableError, "TestPdbFileRecoverableError", dll, switches));

        using (PEReader pe = ReadImage(dll))
        {
            MetadataReader reader = pe.GetMetadataReader();
            Assert.Equal("TestPdbFileRecoverableError", reader.GetString(reader.GetAssemblyDefinition().Name));
        }

        if (switches.Contains("-debug", StringComparison.Ordinal))
        {
            Assert.Equal(pdb, FindAssociatedPdb(dll));
        }
        else
        {
            Assert.False(File.Exists(pdb));
        }
    }

    // An output named like its PDB is refused rather than overwritten by the PDB.
    [Fact]
    public void OutputNamedLikeItsPdb_IsRefused()
    {
        string output = OutputPath("TestPdbFile1.pdb");

        Assert.NotEqual(0, RunIlasm(output, "-debug"));

        Assert.Empty(OutputFileNames());
    }
}
