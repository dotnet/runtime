// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.CommandLine;
using System.IO;
using System.Reflection.PortableExecutable;
using Xunit;

namespace ILAssembler.Tests;

/// <summary>
/// Runs the ilasm command line (<see cref="Program"/>) in process, end to end, with <c>--pathmap</c>: the paths the PDB
/// and the image's CodeView entry record are mapped, while the files are read and written at their real paths. The
/// mapping rules themselves are checked by <see cref="PathMapTests"/>, and the library's use of them by
/// <see cref="PdbDocumentTests"/> and <see cref="CompilerOptionsTests"/>. Each test has its own temporary directories,
/// each standing for a clone of a source tree, with the IL source in <c>src</c> and the outputs in <c>out</c>. As in
/// <see cref="CommandLinePdbFileTests"/>, a failing run is checked by its exit code and by what it did not write; the
/// text of the error is checked by <see cref="PathMapTests"/>.
/// </summary>
public sealed class CommandLinePathMapTests : IDisposable
{
    // Has no .line directive, so the PDB's only document is the input file.
    private const string Source = """
        .assembly extern System.Runtime
        {
        }

        .assembly TestPathMap
        {
        }

        .class public auto ansi beforefieldinit TestPathMapType
               extends [System.Runtime]System.Object
        {
          .method public hidebysig static int32 Foo() cil managed
          {
             ldc.i4.1
             ret
          }
        }
        """;

    private readonly string _first = CreateTree();
    private readonly string _second = CreateTree();

    public void Dispose()
    {
        Directory.Delete(_first, recursive: true);
        Directory.Delete(_second, recursive: true);
    }

    // Creates a temporary directory with the source in src/T.il and an empty out directory, and returns its full path.
    private static string CreateTree()
    {
        string tree = Directory.CreateTempSubdirectory("ilasm-pathmap-").FullName;
        Directory.CreateDirectory(Path.Combine(tree, "src"));
        Directory.CreateDirectory(Path.Combine(tree, "out"));
        File.WriteAllText(Path.Combine(tree, "src", "T.il"), Source);
        return tree;
    }

    private static string Input(string tree) => Path.Combine(tree, "src", "T.il");

    private static string Output(string tree) => Path.Combine(tree, "out", "T.dll");

    private static string Pdb(string tree) => Path.Combine(tree, "out", "T.pdb");

    // Runs "ilasm -nologo -quiet -dll <switches> -output=<tree>/out/T.dll <tree>/src/T.il" and returns its exit code.
    private static int RunIlasm(string tree, params string[] switches)
    {
        string[] args = ["-nologo", "-quiet", "-dll", .. switches, $"-output={Output(tree)}", Input(tree)];
        var command = new IlasmRootCommand();
        ParseResult result = command.Parse(NativeCommandLine.Normalize(args, command));
        Assert.Empty(result.Errors);
        return new Program(command, result).Run();
    }

    private static void AssertSucceeds(int exitCode) => Assert.Equal(0, exitCode);

    private static string ReadCodeViewPath(string imagePath)
    {
        using var pe = new PEReader(ImmutableArray.Create(File.ReadAllBytes(imagePath)));
        DebugDirectoryEntry codeView = Assert.Single(pe.ReadDebugDirectory(), entry => entry.Type == DebugDirectoryEntryType.CodeView);
        return pe.ReadCodeViewDebugDirectoryData(codeView).Path;
    }

    // One source in two clones of a tree, each mapped to /_/, gives the same files: the documents through the map,
    // the CodeView entry because -DET records only the PDB's file name, which no entry of the map matches.
    [Fact]
    public void Deterministic_TheSameSourceInTwoTreesMappedToOnePath_GivesIdenticalImageAndPdb()
    {
        AssertSucceeds(RunIlasm(_first, "-DET", "-DEBUG", $"-pathmap={_first}=/_/"));
        AssertSucceeds(RunIlasm(_second, "-DET", "-DEBUG", $"-pathmap={_second}=/_/"));

        Assert.Equal(File.ReadAllBytes(Output(_first)), File.ReadAllBytes(Output(_second)));
        Assert.Equal(File.ReadAllBytes(Pdb(_first)), File.ReadAllBytes(Pdb(_second)));
        using PortablePdbTestReader reader = PortablePdbTestReader.Open(Output(_first), Pdb(_first));
        Assert.Equal(new[] { "/_/src/T.il" }, reader.DocumentNames);
        Assert.Equal("T.pdb", ReadCodeViewPath(Output(_first)));
    }

    // The native form, with either of its separators, as native options take a value.
    [Theory]
    [InlineData('=')]
    [InlineData(':')]
    public void NativePathMapOption_IsAccepted(char separator)
    {
        AssertSucceeds(RunIlasm(_first, "-DEBUG", $"-PATHMAP{separator}{_first}=/_/"));

        using PortablePdbTestReader reader = PortablePdbTestReader.Open(Output(_first), Pdb(_first));
        Assert.Equal(new[] { "/_/src/T.il" }, reader.DocumentNames);
    }

    // Two --pathmap options: the entries of both apply, those of the first before those of the second. Only the
    // second's entry matches the PDB path, and both match the input, where the first's wins.
    [Fact]
    public void ModernPathMapOption_GivenTwice_AppliesBothInOrder()
    {
        string src = Path.Combine(_first, "src");
        AssertSucceeds(RunIlasm(_first, "-DEBUG", "--pathmap", $"{src}=/first/", "--pathmap", $"{_first}=/second/"));

        using PortablePdbTestReader reader = PortablePdbTestReader.Open(Output(_first), Pdb(_first));
        Assert.Equal(new[] { "/first/T.il" }, reader.DocumentNames);
        Assert.Equal("/second/out/T.pdb", ReadCodeViewPath(Output(_first)));
    }

    // An entry without '=' fails the run before anything is written.
    [Fact]
    public void MalformedPathMap_FailsAndWritesNothing()
    {
        int exitCode = RunIlasm(_first, "-DEBUG", $"-PATHMAP={_first}");

        Assert.Equal(1, exitCode);
        Assert.Empty(Directory.EnumerateFileSystemEntries(Path.Combine(_first, "out")));
    }

    // The map changes the path the image records, not where the PDB is written.
    [Fact]
    public void PathMap_PdbIsWrittenAtItsRealPath_WhileTheCodeViewEntryNamesTheMappedPath()
    {
        AssertSucceeds(RunIlasm(_first, "-DEBUG", $"-pathmap={_first}=/_/"));

        Assert.True(File.Exists(Pdb(_first)));
        Assert.Equal("/_/out/T.pdb", ReadCodeViewPath(Output(_first)));
    }
}
