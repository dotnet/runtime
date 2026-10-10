// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace ILAssembler.Tests.GeneratedCases
{
    /// <summary>A method of a generated program, with an optional <c>.line</c> directive.</summary>
    public sealed record GeneratedMethod(string? Document, int Line)
    {
        public override string ToString() => Document is null ? "-" : $"{Document}@{Line}";
    }

    /// <summary>A small valid IL program: one class with one or more methods.</summary>
    public sealed record GeneratedProgram(ImmutableArray<GeneratedMethod> Methods)
    {
        public GeneratedProgram WithExtraMethod() => new(Methods.Add(new GeneratedMethod(null, 0)));

        public string ToSource()
        {
            var source = new StringBuilder();
            source.AppendLine(".assembly extern System.Runtime { }");
            source.AppendLine(".assembly test { }");
            source.AppendLine(".class public auto ansi beforefieldinit Test");
            source.AppendLine("{");
            for (int i = 0; i < Methods.Length; i++)
            {
                source.AppendLine($"    .method public static void M{i}() cil managed");
                source.AppendLine("    {");
                if (Methods[i].Document is { } document)
                {
                    source.AppendLine($"        .line {Methods[i].Line} '{document}'");
                }

                source.AppendLine("        nop");
                source.AppendLine("        ret");
                source.AppendLine("    }");
            }

            source.AppendLine("}");
            return source.ToString();
        }

        public override string ToString() => string.Join(" ", Methods);
    }

    /// <summary>The PDB-related assembler options of a generated compilation.</summary>
    public sealed record GeneratedOptions(
        bool Debug,
        DebugMode? DebugMode,
        bool Pdb,
        bool Deterministic,
        string? OutputFileName,
        string? PdbFilePath)
    {
        /// <summary>Whether a switch that produces a PDB is set (<c>/DEBUG</c> in any mode, or <c>/PDB</c>).</summary>
        public bool RequestsPdb => Debug || DebugMode is not null || Pdb;

        /// <summary>The PDB path the CodeView entry is expected to name.</summary>
        public string ExpectedCodeViewPath =>
            PdbFilePath ?? (OutputFileName is not null ? Path.ChangeExtension(OutputFileName, ".pdb") : "assembly.pdb");

        public Options ToOptions() => new()
        {
            Debug = Debug,
            DebugMode = DebugMode,
            Pdb = Pdb,
            Deterministic = Deterministic,
            OutputFileName = OutputFileName,
            PdbFilePath = PdbFilePath,
        };

        public override string ToString() =>
            $"{(Debug ? "debug " : "")}{(DebugMode is { } mode ? $"mode={mode} " : "")}{(Pdb ? "pdb " : "")}{(Deterministic ? "det " : "")}" +
            $"out={OutputFileName ?? "-"} pdbpath={PdbFilePath ?? "-"}";
    }

    /// <summary>A generated program and options.</summary>
    public sealed record CompileCase(GeneratedProgram Program, GeneratedOptions Options)
    {
        public override string ToString() => $"{Options} | {Program}";
    }

    /// <summary>A file system state at the PDB path before the output is written.</summary>
    public enum ExistingPdb
    {
        Absent,

        /// <summary>A Portable PDB (<see cref="PdbCaseGenerator.ExistingPair"/>'s).</summary>
        File,

        Directory,
    }

    /// <summary>A file system state at the output path before the output is written.</summary>
    public enum ExistingOutput
    {
        Absent,

        /// <summary>The image whose CodeView entry refers to the PDB at the PDB path (<see cref="ExistingPdb.File"/>).</summary>
        ImageOfThePdb,

        /// <summary>An image whose CodeView entry refers to another PDB.</summary>
        ImageOfAnotherPdb,

        /// <summary>A file that is not an image.</summary>
        NotAnImage,

        /// <summary>A file that reads as a COFF object file: no PE header, so no debug directory.</summary>
        CoffObject,
    }

    /// <summary>
    /// The inputs of the generated-case theories. Compilation cases come from a <see cref="Random"/> with a fixed
    /// seed, so every run builds the same cases and a failing case is reproduced by its index; the display name
    /// also describes it. The output file writer's states are few, so they are enumerated exhaustively.
    /// </summary>
    public static class PdbCaseGenerator
    {
        private const int Seed = 20261006;
        private const int CaseCount = 100;

        private static readonly string[] s_documents = ["a.cs", "b.cs", "dir/c.cs"];
        private static readonly DebugMode?[] s_debugModes = [null, ILAssembler.DebugMode.Impl, ILAssembler.DebugMode.Opt];
        private static readonly string?[] s_outputFileNames = [null, "Output.dll", "Output.exe", "Output", "Out.put.dll"];
        private static readonly string?[] s_pdbFilePaths = [null, "/out/Output.pdb", "C:\\out\\Output.pdb", "Output.pdb"];

        /// <summary>Programs with any options.</summary>
        public static ImmutableArray<CompileCase> AnyCases { get; } = Generate(new Random(Seed), requirePdb: false);

        /// <summary>Programs with options that request a PDB.</summary>
        public static ImmutableArray<CompileCase> PdbRequestedCases { get; } = Generate(new Random(Seed + 1), requirePdb: true);

        public static TheoryData<int, string> AnyCaseData => ToTheoryData(AnyCases);

        public static TheoryData<int, string> PdbRequestedCaseData => ToTheoryData(PdbRequestedCases);

        /// <summary>
        /// An image and the PDB its CodeView entry refers to, which the output file writer states place at the output and
        /// PDB paths, and an image of another PDB.
        /// </summary>
        public static (ImmutableArray<byte> Image, ImmutableArray<byte> Pdb) ExistingPair { get; } = DocumentCompilerTestHelpers.CompileImageAndPdb("A");

        public static ImmutableArray<byte> ImageOfAnotherPdb { get; } = DocumentCompilerTestHelpers.CompileImageAndPdb("B").Image;

        /// <summary>Every output name, pre-existing output state, pre-existing PDB path state and PDB presence.</summary>
        public static TheoryData<string, ExistingOutput, ExistingPdb, bool> OutputWriteStates
        {
            get
            {
                var data = new TheoryData<string, ExistingOutput, ExistingPdb, bool>();
                foreach (string outputFileName in new[] { "Min.dll", "Min.exe", "Min" })
                {
                    foreach (ExistingOutput existingOutput in Enum.GetValues<ExistingOutput>())
                    {
                        foreach (ExistingPdb existingPdb in Enum.GetValues<ExistingPdb>())
                        {
                            foreach (bool withPdb in new[] { false, true })
                            {
                                data.Add(outputFileName, existingOutput, existingPdb, withPdb);
                            }
                        }
                    }
                }

                return data;
            }
        }

        private static TheoryData<int, string> ToTheoryData(ImmutableArray<CompileCase> cases)
        {
            var data = new TheoryData<int, string>();
            for (int i = 0; i < cases.Length; i++)
            {
                data.Add(i, cases[i].ToString());
            }

            return data;
        }

        private static ImmutableArray<CompileCase> Generate(Random random, bool requirePdb)
        {
            var cases = ImmutableArray.CreateBuilder<CompileCase>(CaseCount);
            while (cases.Count < CaseCount)
            {
                var methods = ImmutableArray.CreateBuilder<GeneratedMethod>();
                int methodCount = random.Next(1, 6);
                for (int i = 0; i < methodCount; i++)
                {
                    methods.Add(random.Next(2) == 0
                        ? new GeneratedMethod(null, 0)
                        : new GeneratedMethod(s_documents[random.Next(s_documents.Length)], random.Next(1, 100_000)));
                }

                var options = new GeneratedOptions(
                    Debug: random.Next(2) == 0,
                    DebugMode: s_debugModes[random.Next(s_debugModes.Length)],
                    Pdb: random.Next(2) == 0,
                    Deterministic: random.Next(2) == 0,
                    OutputFileName: s_outputFileNames[random.Next(s_outputFileNames.Length)],
                    PdbFilePath: s_pdbFilePaths[random.Next(s_pdbFilePaths.Length)]);

                if (!requirePdb || options.RequestsPdb)
                {
                    cases.Add(new CompileCase(new GeneratedProgram(methods.ToImmutable()), options));
                }
            }

            return cases.MoveToImmutable();
        }
    }
}
