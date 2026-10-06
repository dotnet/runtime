// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

    /// <summary>
    /// A <c>.line</c> directive of a generated program, written in the full form
    /// <c>.line start,end : startColumn,endColumn ['file']</c>, which native ilasm also accepts.
    /// <see cref="FileName"/> is <see langword="null"/> for a directive without a file name, empty for <c>''</c>,
    /// and otherwise the file the directive names. A hidden directive is <c>.line 16707566,16707566 : 0,0</c>.
    /// </summary>
    public sealed record GeneratedLineDirective(int Line, bool Hidden, string? FileName)
    {
        public const int HiddenLine = 0xFEEFEE;

        public string ToSource() =>
            (Hidden ? $".line {HiddenLine},{HiddenLine} : 0,0" : $".line {Line},{Line} : 1,2") +
            (FileName is null ? "" : $" '{FileName}'");

        public override string ToString() => (Hidden ? "H" : Line.ToString()) + (FileName is null ? "" : $"'{FileName}'");
    }

    /// <summary>
    /// A method of a generated multi-document program: optionally preceded by a class-level <c>.language</c>
    /// directive and a class-level <c>.line</c> directive. A method with a body optionally declares locals and has
    /// <c>nop</c> instructions and a final <c>ret</c>; <see cref="Slots"/> holds the directives that precede each
    /// instruction, and instruction <c>i</c> is at IL offset <c>i</c>. A <see cref="Bodyless"/> method is abstract;
    /// its one slot holds the directives inside its braces, and it has no instructions.
    /// </summary>
    public sealed record GeneratedLineMethod(
        bool LanguageBefore,
        GeneratedLineDirective? ClassLevelDirective,
        bool Bodyless,
        string? Locals,
        ImmutableArray<ImmutableArray<GeneratedLineDirective>> Slots)
    {
        public override string ToString() =>
            (LanguageBefore ? "lang " : "") +
            (ClassLevelDirective is { } directive ? $"class:{directive} " : "") +
            (Bodyless ? "abstract " : "") +
            (Locals is null ? "" : $"locals({Locals}) ") +
            "[" + string.Join(" ", Slots.Select((slot, offset) => slot.IsEmpty ? "_" : $"{offset}:{string.Join(",", slot)}")) + "]";
    }

    /// <summary>A point that a generated method is expected to have, with the document a reader resolves for it.</summary>
    public sealed record ExpectedSequencePoint(int Offset, bool Hidden, string RecordedDocument, string Document);

    /// <summary>
    /// A generated program whose <c>.line</c> directives move between a few documents, in one input file or split
    /// across two, with the PDB documents and sequence points it is expected to produce, computed independently of
    /// the assembler. The model follows native ilasm's rules for <c>.line</c> directives and documents; it does not
    /// model the sequence points native ilasm adds for the lines of the <c>.il</c> source itself.
    /// </summary>
    /// <param name="Methods">The methods, named <c>M0</c>, <c>M1</c>, ... in order.</param>
    /// <param name="SecondInputStart">
    /// <see langword="null"/> for a single input file; otherwise the index of the first method that is in a second
    /// input file (<see cref="SecondInputDocument"/>), which may hold no method at all.
    /// </param>
    public sealed record GeneratedDocumentProgram(ImmutableArray<GeneratedLineMethod> Methods, int? SecondInputStart)
    {
        /// <summary>The name of the first input file, which is the first document.</summary>
        public const string InputDocument = "test.il";

        /// <summary>The name of the second input file, when there is one.</summary>
        public const string SecondInputDocument = "second.il";

        public static readonly Guid ILAssemblyLanguage = new("af046cd3-d0e1-11d2-977c-00a0c9b4d50c");

        public static readonly Guid CSharpLanguage = new("3f5162f8-07c6-11d3-9053-00c04fa302a1");

        /// <summary>The input files, in the order they are compiled.</summary>
        public ImmutableArray<SourceText> ToSources()
        {
            int split = SecondInputStart ?? Methods.Length;
            var first = new StringBuilder();
            first.AppendLine(".assembly extern System.Runtime { }");
            first.AppendLine(".assembly test { }");
            AppendClass(first, "Test", 0, split);
            if (SecondInputStart is null)
            {
                return [new SourceText(first.ToString(), InputDocument)];
            }

            var second = new StringBuilder();
            AppendClass(second, "Test2", split, Methods.Length);
            return [new SourceText(first.ToString(), InputDocument), new SourceText(second.ToString(), SecondInputDocument)];
        }

        private void AppendClass(StringBuilder source, string className, int start, int end)
        {
            source.AppendLine($".class public abstract auto ansi beforefieldinit {className}");
            source.AppendLine("{");
            for (int i = start; i < end; i++)
            {
                GeneratedLineMethod method = Methods[i];
                if (method.LanguageBefore)
                {
                    source.AppendLine($"    .language '{CSharpLanguage}'");
                }

                if (method.ClassLevelDirective is { } classLevelDirective)
                {
                    source.AppendLine("    " + classLevelDirective.ToSource());
                }

                if (method.Bodyless)
                {
                    source.AppendLine($"    .method public hidebysig newslot abstract virtual instance void M{i}() cil managed");
                    source.AppendLine("    {");
                    foreach (GeneratedLineDirective directive in method.Slots.SelectMany(slot => slot))
                    {
                        source.AppendLine("        " + directive.ToSource());
                    }

                    source.AppendLine("    }");
                    continue;
                }

                source.AppendLine($"    .method public static void M{i}() cil managed");
                source.AppendLine("    {");
                if (method.Locals is { } locals)
                {
                    source.AppendLine($"        .locals init ({locals})");
                }

                for (int offset = 0; offset < method.Slots.Length; offset++)
                {
                    foreach (GeneratedLineDirective directive in method.Slots[offset])
                    {
                        source.AppendLine("        " + directive.ToSource());
                    }

                    source.AppendLine(offset == method.Slots.Length - 1 ? "        ret" : "        nop");
                }

                source.AppendLine("    }");
            }

            source.AppendLine("}");
        }

        /// <summary>The documents, in the order they are first named, with the language current at that point.</summary>
        public ImmutableArray<(string Name, Guid Language)> ExpectedDocuments => Expect().Documents;

        /// <summary>The sequence points of each method, in IL offset order.</summary>
        public ImmutableArray<ImmutableArray<ExpectedSequencePoint>> ExpectedSequencePoints => Expect().Points;

        /// <summary>
        /// The document a method's MethodDebugInformation row names: the document of all of its points when they
        /// are recorded in one document, and <see langword="null"/> (nil) when they span several or there are none.
        /// </summary>
        public string? ExpectedMethodDocument(int method)
        {
            ImmutableArray<ExpectedSequencePoint> points = ExpectedSequencePoints[method];
            return !points.IsEmpty && points.All(point => point.RecordedDocument == points[0].RecordedDocument)
                ? points[0].RecordedDocument
                : null;
        }

        private (ImmutableArray<(string Name, Guid Language)> Documents, ImmutableArray<ImmutableArray<ExpectedSequencePoint>> Points) Expect()
        {
            // The rules: each input file is defined as a document when its parsing begins and becomes the current
            // document; the .language state carries over from one input file to the next. A directive with a
            // non-empty file name defines that file (once, with the language current then) and makes it current,
            // in a method with or without a body. Each directive in a method with a body records a point in the
            // current document; a directive at the offset of the previous point replaces it. A method without a body
            // has no points. A reader resolves a point's document from the blob, where a hidden point has no
            // document-record and so is in the current document of the encoding.
            // This model records points when the directive is applied. Native ilasm records them as it emits
            // instructions (a class-level .line can give a following method's first instruction a point, and a .line
            // after the last instruction gives none); the model changes when the assembler does.
            var documents = new List<(string Name, Guid Language)>();
            Guid language = ILAssemblyLanguage;
            string current = InputDocument;
            void Define(string name)
            {
                if (!documents.Any(document => document.Name == name))
                {
                    documents.Add((name, language));
                }

                current = name;
            }

            void Apply(GeneratedLineDirective directive)
            {
                if (!string.IsNullOrEmpty(directive.FileName))
                {
                    Define(directive.FileName);
                }
            }

            Define(InputDocument);
            var methods = ImmutableArray.CreateBuilder<ImmutableArray<ExpectedSequencePoint>>(Methods.Length);
            for (int i = 0; i < Methods.Length; i++)
            {
                GeneratedLineMethod method = Methods[i];
                if (i == SecondInputStart)
                {
                    Define(SecondInputDocument);
                }

                if (method.LanguageBefore)
                {
                    language = CSharpLanguage;
                }

                if (method.ClassLevelDirective is { } classLevelDirective)
                {
                    Apply(classLevelDirective);
                }

                var recorded = new List<(int Offset, bool Hidden, string Document)>();
                for (int offset = 0; offset < method.Slots.Length; offset++)
                {
                    foreach (GeneratedLineDirective directive in method.Slots[offset])
                    {
                        Apply(directive);
                        if (recorded.Count > 0 && recorded[^1].Offset == offset)
                        {
                            recorded[^1] = (offset, directive.Hidden, current);
                        }
                        else
                        {
                            recorded.Add((offset, directive.Hidden, current));
                        }
                    }
                }

                if (method.Bodyless)
                {
                    recorded.Clear();
                }

                var points = ImmutableArray.CreateBuilder<ExpectedSequencePoint>(recorded.Count);
                string encodingDocument = recorded.Count > 0 ? recorded[0].Document : InputDocument;
                foreach ((int offset, bool hidden, string document) in recorded)
                {
                    if (!hidden)
                    {
                        encodingDocument = document;
                    }

                    points.Add(new ExpectedSequencePoint(offset, hidden, document, encodingDocument));
                }

                methods.Add(points.MoveToImmutable());
            }

            if (SecondInputStart == Methods.Length)
            {
                Define(SecondInputDocument);
            }

            return (documents.ToImmutableArray(), methods.MoveToImmutable());
        }

        public override string ToString()
        {
            if (SecondInputStart is not int split)
            {
                return string.Join(" | ", Methods);
            }

            return string.Join(" | ", Methods.Take(split)) + $" || {SecondInputDocument} || " + string.Join(" | ", Methods.Skip(split));
        }
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

        private const int DocumentCaseCount = 200;

        // A small pool, so that methods return to documents they used before. '' and directives without a file
        // name keep the current document.
        private static readonly string?[] s_lineFileNames = ["a.cs", "b.cs", "dir/c.cs", "", null];
        private static readonly string?[] s_locals = [null, null, "int32 x", "int64 y", "int32 x, string s"];

        /// <summary>Programs with any options.</summary>
        public static ImmutableArray<CompileCase> AnyCases { get; } = Generate(new Random(Seed), requirePdb: false);

        /// <summary>Programs with options that request a PDB.</summary>
        public static ImmutableArray<CompileCase> PdbRequestedCases { get; } = Generate(new Random(Seed + 1), requirePdb: true);

        /// <summary>Programs whose methods move between documents with <c>.line</c> directives.</summary>
        public static ImmutableArray<GeneratedDocumentProgram> DocumentCases { get; } = GenerateDocumentPrograms(new Random(Seed + 2));

        public static TheoryData<int, string> AnyCaseData => ToTheoryData(AnyCases);

        public static TheoryData<int, string> PdbRequestedCaseData => ToTheoryData(PdbRequestedCases);

        public static TheoryData<int, string> DocumentCaseData
        {
            get
            {
                var data = new TheoryData<int, string>();
                for (int i = 0; i < DocumentCases.Length; i++)
                {
                    data.Add(i, DocumentCases[i].ToString());
                }

                return data;
            }
        }

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

        private static ImmutableArray<GeneratedDocumentProgram> GenerateDocumentPrograms(Random random)
        {
            GeneratedLineDirective NextDirective(bool allowHidden) => new(
                random.Next(1, 1000),
                allowHidden && random.Next(5) == 0,
                s_lineFileNames[random.Next(s_lineFileNames.Length)]);

            var programs = ImmutableArray.CreateBuilder<GeneratedDocumentProgram>(DocumentCaseCount);
            while (programs.Count < DocumentCaseCount)
            {
                var methods = ImmutableArray.CreateBuilder<GeneratedLineMethod>();
                int methodCount = random.Next(1, 4);
                for (int i = 0; i < methodCount; i++)
                {
                    // Sometimes an abstract method: its directives still name files, but it has no points.
                    bool bodyless = random.Next(8) == 0;
                    var slots = ImmutableArray.CreateBuilder<ImmutableArray<GeneratedLineDirective>>();
                    int instructionCount = bodyless ? 1 : random.Next(1, 6);
                    for (int offset = 0; offset < instructionCount; offset++)
                    {
                        // Mostly one directive before an instruction; sometimes none, sometimes two at one offset.
                        int directiveCount = random.Next(10) switch { < 3 => 0, < 9 => 1, _ => 2 };
                        var slot = ImmutableArray.CreateBuilder<GeneratedLineDirective>(directiveCount);
                        for (int d = 0; d < directiveCount; d++)
                        {
                            slot.Add(NextDirective(allowHidden: true));
                        }

                        slots.Add(slot.MoveToImmutable());
                    }

                    methods.Add(new GeneratedLineMethod(
                        LanguageBefore: random.Next(8) == 0,
                        ClassLevelDirective: random.Next(6) == 0 ? NextDirective(allowHidden: false) : null,
                        Bodyless: bodyless,
                        Locals: bodyless ? null : s_locals[random.Next(s_locals.Length)],
                        Slots: slots.ToImmutable()));
                }

                // Sometimes a second input file, holding the methods from a split point on (possibly none).
                int? secondInputStart = random.Next(3) == 0 ? random.Next(1, methodCount + 1) : null;
                programs.Add(new GeneratedDocumentProgram(methods.ToImmutable(), secondInputStart));
            }

            return programs.MoveToImmutable();
        }
    }
}
