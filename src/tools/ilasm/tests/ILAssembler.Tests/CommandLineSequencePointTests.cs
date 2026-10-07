// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using Xunit;

namespace ILAssembler.Tests;

/// <summary>
/// Runs the ilasm command line (<see cref="Program"/>) in process with -DEBUG and reads back the sequence points of
/// the PDB it writes, for what only the command line establishes: the documents of the input file and of an
/// <c>#include</c>d file are named by their full paths, whatever paths the command line and the <c>#include</c> give.
/// How points are recorded is in <see cref="SequencePointTests"/>. Each test has its own temporary directory, with
/// the IL sources in <c>src</c> and the outputs in <c>out</c>, and its own subdirectory of the current directory for
/// the IL sources named by a path relative to the current directory.
/// </summary>
public sealed class CommandLineSequencePointTests : IDisposable
{
    // No .line directive: each instruction's point is on its own line of this file, columns 1 to 2, and ldc.i4.1 and
    // add, on one line, share one.
    private const string SourceWithoutLineDirectives = """
        .assembly extern System.Runtime
        {
        }

        .assembly TestImplicitLines
        {
        }

        .class public abstract auto ansi sealed beforefieldinit TestImplicitLinesType
               extends [System.Runtime]System.Object
        {
          .method public hidebysig static int32 Foo(int32 a) cil managed
          {
             .maxstack 2
             ldarg.0 // first

             // a comment line between two instructions
             ldc.i4.1 add // second and third
             ret // fourth
          }
        }
        """;

    // The .line directive applies to the instructions after it, so Bar spans two documents.
    private const string SourceWithLineDirectiveInABody = """
        .assembly extern System.Runtime
        {
        }

        .assembly TestLineDirective
        {
        }

        .class public abstract auto ansi sealed beforefieldinit TestLineDirectiveType
               extends [System.Runtime]System.Object
        {
          .method public hidebysig static int32 Bar(int32 a) cil managed
          {
             ldarg.0 // before the directive
             .line 5,5 : 9,10 'implicit_lines.cs'
             ldc.i4.2
             mul
             ret
          }
        }
        """;

    // The first instructions of TestIncludeType::Foo, written to src/TestInclude.inc.
    private const string IncludedBody = """
        // The first instructions of TestIncludeType::Foo, included by TestInclude.il.
             ldarg.0 // included first
             ldc.i4.1 // included second

             add // included third

        """;

    private static string SourceWithInclude(string includePath) => $$"""
        .assembly extern System.Runtime
        {
        }

        .assembly TestInclude
        {
        }

        .class public abstract auto ansi sealed beforefieldinit TestIncludeType
               extends [System.Runtime]System.Object
        {
          .method public hidebysig static int32 Foo(int32 a) cil managed
          {
             .maxstack 2
        #include "{{includePath}}"
             ret // after the include
          }
        }
        """;

    private readonly string _directory = Directory.CreateTempSubdirectory("ilasm-cli-").FullName;

    // The name of this test's subdirectory of the current directory, which is read, never set: it is process-wide.
    // A path into it is relative to the current directory on every platform, which a relative path from the current
    // directory to the temporary directory is not where the two are on different drives.
    private readonly string _currentDirectorySubdirectoryName = "ilasm-cli-" + Path.GetRandomFileName();

    public CommandLineSequencePointTests()
    {
        Directory.CreateDirectory(Path.Combine(_directory, "src"));
        Directory.CreateDirectory(Path.Combine(_directory, "out"));
        Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, _currentDirectorySubdirectoryName));
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
        Directory.Delete(Path.Combine(Environment.CurrentDirectory, _currentDirectorySubdirectoryName), recursive: true);
    }

    // Writes src/<fileName> and returns its full path.
    private string WriteSource(string fileName, string text)
    {
        string path = Path.Combine(_directory, "src", fileName);
        File.WriteAllText(path, text);
        return path;
    }

    // Writes <fileName> in this test's subdirectory of the current directory and returns its path relative to the
    // current directory.
    private string WriteSourceRelativeToTheCurrentDirectory(string fileName, string text)
    {
        string path = Path.Combine(_currentDirectorySubdirectoryName, fileName);
        File.WriteAllText(Path.Combine(Environment.CurrentDirectory, path), text);
        return path;
    }

    // Assembles the input as "ilasm -nologo -quiet -dll -debug -output=<out/Test.dll> <input>" does and opens the image
    // and the PDB written beside it.
    private PortablePdbTestReader RunIlasm(string input)
    {
        string dll = Path.Combine(_directory, "out", "Test.dll");
        string[] args = ["-nologo", "-quiet", "-dll", "-debug", $"-output={dll}", input];

        var command = new IlasmRootCommand();
        ParseResult result = command.Parse(NativeCommandLine.Normalize(args, command));
        Assert.Empty(result.Errors);
        Assert.Equal(0, new Program(command, result).Run());
        return PortablePdbTestReader.Open(dll, Path.Combine(_directory, "out", "Test.pdb"));
    }

    /// <summary>Gets the 1-based number of the only line of <paramref name="text"/> that contains <paramref name="marker"/>.</summary>
    private static int LineOf(string text, string marker)
    {
        string[] lines = text.Split('\n');
        int index = Array.FindIndex(lines, line => line.Contains(marker, StringComparison.Ordinal));
        Assert.True(index >= 0, $"No line contains '{marker}'.");
        Assert.Equal(index, Array.FindLastIndex(lines, line => line.Contains(marker, StringComparison.Ordinal)));
        return index + 1;
    }

    private static (int Offset, int StartLine, int StartColumn, int EndLine, int EndColumn, string Document)[] Points(PortablePdbTestReader pdb, string method)
        => pdb.GetSequencePoints(method)
            .Select(point => (point.Offset, point.StartLine, point.StartColumn, point.EndLine, point.EndColumn, pdb.GetDocumentName(point.Document)))
            .ToArray();

    // The point of an instruction on that line of that document with no .line directive in effect.
    private static (int, int, int, int, int, string) OnLine(int offset, int line, string document)
        => (offset, line, 1, line, 2, document);

    // The input's implicit points are in a document named by the input's full path, whatever path the command line
    // gives: a path relative to the current directory (read, never set: it is process-wide), or a full path with a
    // ".." segment.
    [Theory]
    [InlineData("RelativeToTheCurrentDirectory")]
    [InlineData("WithADotDotSegment")]
    public void ImplicitPoints_AreOnTheirLinesOfTheInputInADocumentNamedByItsFullPath(string inputPathShape)
    {
        string sourcePath;
        string input;
        if (inputPathShape == "RelativeToTheCurrentDirectory")
        {
            input = WriteSourceRelativeToTheCurrentDirectory("TestImplicitLines.il", SourceWithoutLineDirectives);
            Assert.False(Path.IsPathRooted(input));
            sourcePath = Path.Combine(Environment.CurrentDirectory, input);
        }
        else
        {
            sourcePath = WriteSource("TestImplicitLines.il", SourceWithoutLineDirectives);
            input = Path.Combine(_directory, "src", "..", "src", "TestImplicitLines.il");
        }

        using PortablePdbTestReader pdb = RunIlasm(input);

        string source = SourceWithoutLineDirectives;
        Assert.Equal(new[] { sourcePath }, pdb.DocumentNames);
        Assert.Equal(sourcePath, pdb.GetMethodDocumentName("Foo"));
        Assert.Equal(
            new[]
            {
                OnLine(0x0, LineOf(source, "// first"), sourcePath),
                OnLine(0x1, LineOf(source, "// second and third"), sourcePath),
                OnLine(0x3, LineOf(source, "// fourth"), sourcePath),
            },
            Points(pdb, "Foo"));
    }

    // A .line directive after an implicit point moves the rest of the method to the file it names, so the method spans
    // the input's document and that file's, and its MethodDebugInformation row names no document.
    [Fact]
    public void LineDirectiveAfterAnImplicitPoint_MakesTheMethodSpanTheInputDocumentAndTheNamedFile()
    {
        string sourcePath = WriteSource("TestLineDirective.il", SourceWithLineDirectiveInABody);

        using PortablePdbTestReader pdb = RunIlasm(sourcePath);

        Assert.Equal(new[] { sourcePath, "implicit_lines.cs" }, pdb.DocumentNames);
        Assert.Null(pdb.GetMethodDocumentName("Bar"));
        Assert.Equal(
            new[]
            {
                OnLine(0x0, LineOf(SourceWithLineDirectiveInABody, "// before the directive"), sourcePath),
                (0x1, 5, 9, 5, 10, "implicit_lines.cs"),
            },
            Points(pdb, "Bar"));
    }

    // An #include'd file's instructions have points on their lines of that file, in a document named by its full path,
    // and the including file's later instructions are back in the input's document. The command line tries an #include
    // path as given, which is relative to the current directory as in native ilasm, then relative to the directory of
    // the first input file, then relative to -INCLUDE. The #include here gives a path relative to the current directory
    // (read, never set: it is process-wide), which the first step resolves; no file is at that path relative to the
    // input's directory, so the test does not depend on the later steps.
    [Fact]
    public void IncludedInstructions_FromAPathRelativeToTheCurrentDirectory_AreInADocumentNamedByTheIncludedFilesFullPath()
    {
        string includePath = WriteSourceRelativeToTheCurrentDirectory("TestInclude.inc", IncludedBody).Replace('\\', '/');
        Assert.False(Path.IsPathRooted(includePath));
        string includedPath = Path.Combine(Environment.CurrentDirectory, _currentDirectorySubdirectoryName, "TestInclude.inc");
        string source = SourceWithInclude(includePath);
        string sourcePath = Path.Combine(Environment.CurrentDirectory, WriteSourceRelativeToTheCurrentDirectory("TestInclude.il", source));

        using PortablePdbTestReader pdb = RunIlasm(sourcePath);

        Assert.Null(pdb.GetMethodDocumentName("Foo"));
        Assert.Equal(
            new[]
            {
                OnLine(0x0, LineOf(IncludedBody, "// included first"), includedPath),
                OnLine(0x1, LineOf(IncludedBody, "// included second"), includedPath),
                OnLine(0x2, LineOf(IncludedBody, "// included third"), includedPath),
                OnLine(0x3, LineOf(source, "// after the include"), sourcePath),
            },
            Points(pdb, "Foo"));
    }
}
