// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using Xunit;

namespace ILAssembler.Tests;

/// <summary>
/// Runs the ilasm command line (<see cref="Program"/>) in process, end to end, and checks the documents of the PDB it
/// writes where the command line decides them: each input file is a document named by its full path, whatever path
/// it was given by, and the input files are documents in the order they are given. The assembler's own document
/// rules are checked on the library by <see cref="PdbDocumentTests"/>; the last test here runs one of them through
/// the command line. Each test has its own temporary directory, with the IL sources in <c>src</c> and the outputs in
/// <c>out</c>, and its own directory under the current directory for an input given by a relative path.
/// </summary>
public sealed class CommandLinePdbDocumentTests : IDisposable
{
    // Has no .line directive.
    private const string Source = """
        .assembly extern System.Runtime
        {
        }

        .assembly TestPdbDocuments
        {
        }

        .class public auto ansi beforefieldinit TestPdbDocumentsType
               extends [System.Runtime]System.Object
        {
          .method public hidebysig static int32 Foo() cil managed
          {
             ldc.i4.1
             ret
          }
        }
        """;

    // The first of two input files: the assembly, and a method without a .line directive.
    private const string FirstInput = """
        .assembly extern System.Runtime
        {
        }

        .assembly TestPdbTwoInputs
        {
        }

        .class public auto ansi beforefieldinit FirstType
               extends [System.Runtime]System.Object
        {
          .method public hidebysig static void Foo() cil managed
          {
             ret
          }
        }
        """;

    // The second of two input files: a method whose .line directive names the file Y.
    private const string SecondInput = """
        .class public auto ansi beforefieldinit SecondType
               extends [System.Runtime]System.Object
        {
          .method public hidebysig static void Bar() cil managed
          {
             .line 5,5 : 9,10 'Y'
             ret
          }
        }
        """;

    // Bar's first .line has an empty file name, as ildasm writes it when the file is the same as the previous .line's:
    // it keeps the document of Foo's .line. The last .line names the file again. The file name X has no directory
    // separator, so the source is the same on every platform.
    private const string SourceWithEmptyFileName = """
        .assembly extern System.Runtime
        {
        }

        .assembly TestDocuments
        {
        }

        .class public auto ansi beforefieldinit TestDocumentsType
               extends [System.Runtime]System.Object
        {
            .method public hidebysig instance void Foo() cil managed
            {
            .line 1,1 : 9,10 'X'
                ret
            }

            .method public hidebysig instance void Bar() cil managed
            {
            .line 2,2 : 9,10 ''
                nop
            .line 3,3 : 9,10 'X'
                ret
            }
        }
        """;

    private readonly string _directory = Directory.CreateTempSubdirectory("ilasm-cli-").FullName;

    // A directory of this test's own under the current directory, for the input given by a relative path.
    private readonly string _currentDirectorySubdirectory =
        Path.Combine(Environment.CurrentDirectory, "ilasm-cli-" + Path.GetRandomFileName());

    public CommandLinePdbDocumentTests()
    {
        Directory.CreateDirectory(Path.Combine(_directory, "src"));
        Directory.CreateDirectory(Path.Combine(_directory, "out"));
        Directory.CreateDirectory(_currentDirectorySubdirectory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
        Directory.Delete(_currentDirectorySubdirectory, recursive: true);
    }

    // Writes the source to src/<fileName> and returns its full path.
    private string WriteSource(string fileName, string source)
    {
        string path = Path.Combine(_directory, "src", fileName);
        File.WriteAllText(path, source);
        return path;
    }

    // Assembles the inputs, given by these paths, as "ilasm -nologo -quiet -dll -debug -output=<out/Output.dll>
    // <inputs>" does, asserts that it succeeds, and opens the image and the PDB it writes.
    private PortablePdbTestReader RunIlasm(params string[] inputs)
    {
        string dll = Path.Combine(_directory, "out", "Output.dll");
        string[] args = ["-nologo", "-quiet", "-dll", "-debug", $"-output={dll}", .. inputs];

        var command = new IlasmRootCommand();
        ParseResult result = command.Parse(NativeCommandLine.Normalize(args, command));
        Assert.Empty(result.Errors);
        Assert.Equal(0, new Program(command, result).Run());
        return PortablePdbTestReader.Open(dll, Path.Combine(_directory, "out", "Output.pdb"));
    }

    // Without any .line directive, the PDB's only document is the input file, named by its full path.
    [Fact]
    public void InputFile_IsTheOnlyDocument_NamedByItsFullPath()
    {
        string input = WriteSource("TestPdbDocuments.il", Source);
        Assert.True(Path.IsPathFullyQualified(input));

        using PortablePdbTestReader reader = RunIlasm(input);

        Assert.Equal(new[] { input }, reader.DocumentNames);
    }

    // An input file given by a path relative to the current directory is named by the full path that its relative
    // path resolves to, not by the path as given.
    [Fact]
    public void InputFileGivenByARelativePath_IsNamedByItsFullPath()
    {
        // The source is written under the current directory, so its path relative to the current directory is not
        // rooted. The current directory is read, never set: xunit runs test classes in parallel, and the current
        // directory is the whole process's.
        string input = Path.Combine(_currentDirectorySubdirectory, "TestPdbDocuments.il");
        File.WriteAllText(input, Source);
        string relativeInput = Path.GetRelativePath(Environment.CurrentDirectory, input);
        Assert.False(Path.IsPathRooted(relativeInput));

        using PortablePdbTestReader reader = RunIlasm(relativeInput);

        Assert.Equal(new[] { Path.GetFullPath(relativeInput) }, reader.DocumentNames);
    }

    // Two input files on one command line are two documents, each named by its full path, in the order they are
    // given, before the document of a .line directive in the second. The first input's name sorts after the
    // second's, so the order is not an order of names.
    [Fact]
    public void TwoInputFiles_AreTwoDocumentsInInputOrder_BeforeALineDirectivesDocument()
    {
        string first = WriteSource("Main.il", FirstInput);
        string second = WriteSource("Helpers.il", SecondInput);

        using PortablePdbTestReader reader = RunIlasm(first, second);

        Assert.Equal(new[] { first, second, "Y" }, reader.DocumentNames);
    }

    // The TestDocuments2 case of the runtime's PortablePdb tests, end to end: '' on Bar's first .line keeps the
    // document of Foo's .line, so both methods' points are in X, Bar does not span documents, and the documents
    // are the input file and X.
    [Fact]
    public void EmptyFileNameOnAMethodsFirstLineDirective_KeepsThePreviousDocument()
    {
        string input = WriteSource("TestDocuments2.il", SourceWithEmptyFileName);

        using PortablePdbTestReader reader = RunIlasm(input);

        Assert.Equal(new[] { input, "X" }, reader.DocumentNames);
        Assert.Equal(
            new[] { (0, 1, 1, 9, 10, "X") },
            reader.GetSequencePoints("Foo").Select(point => Describe(reader, point)));
        Assert.Equal("X", reader.GetMethodDocumentName("Foo"));
        Assert.Equal(
            new[] { (0, 2, 2, 9, 10, "X"), (1, 3, 3, 9, 10, "X") },
            reader.GetSequencePoints("Bar").Select(point => Describe(reader, point)));
        Assert.Equal("X", reader.GetMethodDocumentName("Bar"));
    }

    // A sequence point as (IL offset, start line, end line, start column, end column, document name).
    private static (int, int, int, int, int, string) Describe(PortablePdbTestReader reader, SequencePoint point)
        => (point.Offset, point.StartLine, point.EndLine, point.StartColumn, point.EndColumn, reader.GetDocumentName(point.Document));
}
