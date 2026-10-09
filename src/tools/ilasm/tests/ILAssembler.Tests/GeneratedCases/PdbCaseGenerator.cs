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

        /// <summary>The start line, start column, end line and end column of the points the directive gives.</summary>
        public (int StartLine, int StartColumn, int EndLine, int EndColumn) Span =>
            Hidden ? (HiddenLine, 0, HiddenLine, 0) : (Line, 1, Line, 2);

        public override string ToString() => (Hidden ? "H" : Line.ToString()) + (FileName is null ? "" : $"'{FileName}'");
    }

    /// <summary>Where the text of a generated method is.</summary>
    public enum GeneratedInclusion
    {
        /// <summary>In the input file.</summary>
        None,

        /// <summary>The whole method declaration is in a file that the input file <c>#include</c>s at class level.</summary>
        Method,

        /// <summary>
        /// A run of the method's instructions, with their directives and filler lines, is in a file that the method
        /// body <c>#include</c>s.
        /// </summary>
        Body,
    }

    /// <summary>
    /// A method of a generated multi-document program, optionally preceded by a class-level <c>.language</c>
    /// directive and a class-level <c>.line</c> directive.
    /// </summary>
    /// <remarks>
    /// A method with a body optionally declares locals and has one-byte <c>nop</c> or, where <see cref="TwoByte"/>[i]
    /// is set, two-byte <c>ldc.i4.s</c> instructions and a final <c>ret</c>; instruction <c>i</c> is at IL offset
    /// <see cref="OffsetOf"/>(i). Before instruction <c>i</c> come <see cref="Fillers"/>[i] blank
    /// or comment lines, then the directives of <see cref="Slots"/>[i], each on its own line; the instruction is on
    /// a line of its own, or, when <see cref="SameLine"/>[i] is set, on the line of the previous instruction (it then
    /// has no fillers and no directives). <see cref="Trailing"/> holds the directives after the last instruction.
    /// A <see cref="Bodyless"/> method is abstract; its one slot holds the directives inside its braces, and it has
    /// no instructions. With <see cref="GeneratedInclusion.Body"/>, instructions <see cref="IncludeStart"/> to
    /// <see cref="IncludeEnd"/> (exclusive) and what precedes each of them are in the included file.
    /// </remarks>
    public sealed record GeneratedLineMethod(
        bool LanguageBefore,
        GeneratedLineDirective? ClassLevelDirective,
        bool Bodyless,
        string? Locals,
        ImmutableArray<ImmutableArray<GeneratedLineDirective>> Slots,
        ImmutableArray<int> Fillers,
        ImmutableArray<bool> SameLine,
        ImmutableArray<bool> TwoByte,
        ImmutableArray<GeneratedLineDirective> Trailing,
        GeneratedInclusion Inclusion,
        int IncludeStart,
        int IncludeEnd)
    {
        /// <summary>Whether instruction <paramref name="index"/> is in the file the method body includes.</summary>
        public bool IsInBodyInclude(int index) => Inclusion == GeneratedInclusion.Body && index >= IncludeStart && index < IncludeEnd;

        /// <summary>The IL offset of instruction <paramref name="index"/>: one byte per instruction before it, two for <c>ldc.i4.s</c>.</summary>
        public int OffsetOf(int index) => index + TwoByte.Take(index).Count(twoByte => twoByte);

        /// <summary>The text of instruction <paramref name="index"/>.</summary>
        public string InstructionText(int index) =>
            index == Slots.Length - 1 ? "ret" : TwoByte[index] ? $"ldc.i4.s {index + 1}" : "nop";

        public override string ToString() =>
            (LanguageBefore ? "lang " : "") +
            (ClassLevelDirective is { } directive ? $"class:{directive} " : "") +
            (Bodyless ? "abstract " : "") +
            (Locals is null ? "" : $"locals({Locals}) ") +
            (Inclusion switch { GeneratedInclusion.Method => "inc ", GeneratedInclusion.Body => $"inc{IncludeStart}-{IncludeEnd} ", _ => "" }) +
            "[" + string.Join(" ", Slots.Select((slot, offset) =>
                (Fillers[offset] > 0 ? "~" : "") + (SameLine[offset] ? "=" : "") + (TwoByte[offset] ? "2" : "") +
                (slot.IsEmpty ? "_" : $"{offset}:{string.Join(",", slot)}"))) + "]" +
            (Trailing.IsEmpty ? "" : " trail:" + string.Join(",", Trailing));
    }

    /// <summary>
    /// A point that a generated method is expected to have: its IL offset, whether it is hidden, its span, the
    /// document it is recorded in, the document a reader resolves for it from the blob (a hidden point has no
    /// document-record of its own), and whether it is an implicit point on a line of the <c>.il</c> source.
    /// </summary>
    public sealed record ExpectedSequencePoint(
        int Offset,
        bool Hidden,
        int StartLine,
        int StartColumn,
        int EndLine,
        int EndColumn,
        string RecordedDocument,
        string Document,
        bool Implicit);

    /// <summary>
    /// A generated program whose instructions are on lines of the <c>.il</c> source and whose <c>.line</c> directives
    /// move between a few documents, in one input file or split across two, with methods or runs of instructions in
    /// <c>#include</c>d files; and the PDB documents and sequence points it is expected to produce, computed
    /// from the source the program writes. The model follows native ilasm's rules with this assembler's declared
    /// differences (see <see cref="Expect"/>); it mirrors the rules the assembler implements, so it checks them
    /// against the layout of generated sources, not against native ilasm.
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

        /// <summary>The name, which is also the path, of the file that method <paramref name="method"/> includes or is in.</summary>
        public static string IncludeName(int method) => $"inc{method}.il";

        /// <summary>The input files, in the order they are compiled.</summary>
        public ImmutableArray<SourceText> ToSources() => Layout().Inputs;

        /// <summary>The included files, by the name the <c>#include</c> directives give, which is also their path.</summary>
        public ImmutableDictionary<string, SourceText> IncludedSources => Layout().Includes;

        /// <summary>Lines of a source being written; the first line is line 1, as ilasm counts them.</summary>
        private sealed class SourceLines
        {
            private readonly List<string> _lines = new();

            /// <summary>Adds a line and returns its number.</summary>
            public int Add(string line)
            {
                _lines.Add(line);
                return _lines.Count;
            }

            /// <summary>Appends text to the last line and returns its number.</summary>
            public int AppendToLast(string text)
            {
                _lines[^1] += text;
                return _lines.Count;
            }

            public SourceText ToSourceText(string path) => new(string.Join("\n", _lines) + "\n", path);
        }

        /// <summary>
        /// Writes the input files and the included files, and returns, for each method, the line of each of its
        /// instructions in the file the instruction is in.
        /// </summary>
        private (ImmutableArray<SourceText> Inputs, ImmutableDictionary<string, SourceText> Includes, ImmutableArray<ImmutableArray<int>> InstructionLines) Layout()
        {
            int split = SecondInputStart ?? Methods.Length;
            var includes = ImmutableDictionary.CreateBuilder<string, SourceText>();
            var instructionLines = ImmutableArray.CreateBuilder<ImmutableArray<int>>(Methods.Length);

            var first = new SourceLines();
            first.Add(".assembly extern System.Runtime { }");
            first.Add(".assembly test { }");
            AppendClass(first, "Test", 0, split);
            if (SecondInputStart is null)
            {
                return ([first.ToSourceText(InputDocument)], includes.ToImmutable(), instructionLines.MoveToImmutable());
            }

            var second = new SourceLines();
            AppendClass(second, "Test2", split, Methods.Length);
            return ([first.ToSourceText(InputDocument), second.ToSourceText(SecondInputDocument)], includes.ToImmutable(), instructionLines.MoveToImmutable());

            void AppendClass(SourceLines source, string className, int start, int end)
            {
                source.Add($".class public abstract auto ansi beforefieldinit {className}");
                source.Add("{");
                for (int i = start; i < end; i++)
                {
                    instructionLines.Add(AppendMethod(source, i));
                }

                source.Add("}");
            }

            ImmutableArray<int> AppendMethod(SourceLines outer, int i)
            {
                GeneratedLineMethod method = Methods[i];
                if (method.LanguageBefore)
                {
                    outer.Add($"    .language '{CSharpLanguage}'");
                }

                if (method.ClassLevelDirective is { } classLevelDirective)
                {
                    outer.Add("    " + classLevelDirective.ToSource());
                }

                SourceLines target = outer;
                if (method.Inclusion == GeneratedInclusion.Method)
                {
                    outer.Add($"#include \"{IncludeName(i)}\"");
                    target = new SourceLines();
                }

                var lines = ImmutableArray.CreateBuilder<int>();
                if (method.Bodyless)
                {
                    target.Add($"    .method public hidebysig newslot abstract virtual instance void M{i}() cil managed");
                    target.Add("    {");
                    foreach (GeneratedLineDirective directive in method.Slots.SelectMany(slot => slot))
                    {
                        target.Add("        " + directive.ToSource());
                    }

                    target.Add("    }");
                }
                else
                {
                    target.Add($"    .method public static void M{i}() cil managed");
                    target.Add("    {");
                    if (method.Locals is { } locals)
                    {
                        target.Add($"        .locals init ({locals})");
                    }

                    SourceLines body = target;
                    for (int offset = 0; offset < method.Slots.Length; offset++)
                    {
                        if (method.Inclusion == GeneratedInclusion.Body && offset == method.IncludeStart)
                        {
                            target.Add($"#include \"{IncludeName(i)}\"");
                            body = new SourceLines();
                        }

                        for (int filler = 0; filler < method.Fillers[offset]; filler++)
                        {
                            body.Add(filler % 2 == 0 ? "" : "        // a comment line");
                        }

                        foreach (GeneratedLineDirective directive in method.Slots[offset])
                        {
                            body.Add("        " + directive.ToSource());
                        }

                        string instruction = method.InstructionText(offset);
                        lines.Add(method.SameLine[offset] ? body.AppendToLast(" " + instruction) : body.Add("        " + instruction));

                        if (method.Inclusion == GeneratedInclusion.Body && offset == method.IncludeEnd - 1)
                        {
                            includes.Add(IncludeName(i), body.ToSourceText(IncludeName(i)));
                            body = target;
                        }
                    }

                    foreach (GeneratedLineDirective directive in method.Trailing)
                    {
                        target.Add("        " + directive.ToSource());
                    }

                    target.Add("    }");
                }

                if (method.Inclusion == GeneratedInclusion.Method)
                {
                    includes.Add(IncludeName(i), target.ToSourceText(IncludeName(i)));
                }

                return lines.ToImmutable();
            }
        }

        /// <summary>The documents, in the order they are first defined, with the language current at that point.</summary>
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

        /// <summary>The model's state for one source: an input file, or one inclusion of a file.</summary>
        private sealed class SourceState(string name)
        {
            public string Name { get; } = name;

            /// <summary>The source's current document; <see langword="null"/> while it is the source itself.</summary>
            public string? Document { get; set; }

            /// <summary>The last directive applied in the source, or <see langword="null"/>.</summary>
            public GeneratedLineDirective? Directive { get; set; }
        }

        /// <summary>
        /// Computes the expected documents and points. The rules are native ilasm's, except where marked (the
        /// differences listed in MANAGED-ILASM-FIXES.md): each input file is defined
        /// as a document when its parsing begins, and the <c>.language</c> state carries over from one input file to
        /// the next. Each input file and each inclusion is a source with its own state. A directive with a non-empty
        /// file name defines that file (once, with the language current then) and makes it the source's current
        /// document; every directive becomes the source's directive in effect, wherever it is (method body, class
        /// level, a method without a body, after the last instruction). Each instruction, when emitted, has a span:
        /// the span of its source's directive in effect, or, with none, its own line of its source, columns 1 to 2;
        /// and a document: its source's current document, which is the source itself until a directive in it names
        /// another file (native: after an include, the including file again). The instruction gets a point when its
        /// span and document (native: span only) differ from those of the last point recorded, in any method or input
        /// file, or a directive has been applied since. An included file becomes a document when a point first needs
        /// it, with the language current then (native: at the include, with the IL language). A reader resolves a
        /// point's document from the blob, where a hidden point has no document-record and so is in the current
        /// document of the encoding.
        /// </summary>
        private (ImmutableArray<(string Name, Guid Language)> Documents, ImmutableArray<ImmutableArray<ExpectedSequencePoint>> Points) Expect()
        {
            ImmutableArray<ImmutableArray<int>> instructionLines = Layout().InstructionLines;
            var documents = new List<(string Name, Guid Language)>();
            Guid language = ILAssemblyLanguage;
            (string Document, int StartLine, int StartColumn, int EndLine, int EndColumn)? last = null;

            string Define(string name)
            {
                if (!documents.Any(document => document.Name == name))
                {
                    documents.Add((name, language));
                }

                return name;
            }

            void Apply(SourceState source, GeneratedLineDirective directive)
            {
                if (!string.IsNullOrEmpty(directive.FileName))
                {
                    source.Document = Define(directive.FileName);
                }

                source.Directive = directive;
                last = null;
            }

            Define(InputDocument);
            var input = new SourceState(InputDocument);
            var methods = ImmutableArray.CreateBuilder<ImmutableArray<ExpectedSequencePoint>>(Methods.Length);
            for (int i = 0; i < Methods.Length; i++)
            {
                GeneratedLineMethod method = Methods[i];
                if (i == SecondInputStart)
                {
                    Define(SecondInputDocument);
                    input = new SourceState(SecondInputDocument);
                }

                if (method.LanguageBefore)
                {
                    language = CSharpLanguage;
                }

                if (method.ClassLevelDirective is { } classLevelDirective)
                {
                    Apply(input, classLevelDirective);
                }

                SourceState methodSource = method.Inclusion == GeneratedInclusion.Method ? new SourceState(IncludeName(i)) : input;
                SourceState? bodyInclude = method.Inclusion == GeneratedInclusion.Body ? new SourceState(IncludeName(i)) : null;
                var recorded = new List<(int Offset, GeneratedLineDirective? Directive, int StartLine, int StartColumn, int EndLine, int EndColumn, string Document)>();
                if (method.Bodyless)
                {
                    foreach (GeneratedLineDirective directive in method.Slots.SelectMany(slot => slot))
                    {
                        Apply(methodSource, directive);
                    }
                }
                else
                {
                    for (int offset = 0; offset < method.Slots.Length; offset++)
                    {
                        SourceState source = method.IsInBodyInclude(offset) ? bodyInclude! : methodSource;
                        foreach (GeneratedLineDirective directive in method.Slots[offset])
                        {
                            Apply(source, directive);
                        }

                        string document = source.Document ??= Define(source.Name);
                        int line = instructionLines[i][offset];
                        (int startLine, int startColumn, int endLine, int endColumn) = source.Directive?.Span ?? (line, 1, line, 2);
                        if (last != (document, startLine, startColumn, endLine, endColumn))
                        {
                            last = (document, startLine, startColumn, endLine, endColumn);
                            recorded.Add((method.OffsetOf(offset), source.Directive, startLine, startColumn, endLine, endColumn, document));
                        }
                    }

                    foreach (GeneratedLineDirective directive in method.Trailing)
                    {
                        Apply(methodSource, directive);
                    }
                }

                var points = ImmutableArray.CreateBuilder<ExpectedSequencePoint>(recorded.Count);
                string encodingDocument = recorded.Count > 0 ? recorded[0].Document : InputDocument;
                foreach (var point in recorded)
                {
                    bool hidden = point.Directive?.Hidden == true;
                    if (!hidden)
                    {
                        encodingDocument = point.Document;
                    }

                    points.Add(new ExpectedSequencePoint(
                        point.Offset, hidden, point.StartLine, point.StartColumn, point.EndLine, point.EndColumn,
                        point.Document, encodingDocument, Implicit: point.Directive is null));
                }

                methods.Add(points.MoveToImmutable());
            }

            if (SecondInputStart == Methods.Length)
            {
                Define(SecondInputDocument);
            }

            return (documents.ToImmutableArray(), methods.MoveToImmutable());
        }

        /// <summary>How many times the program has each generated shape, to check that the cases cover them.</summary>
        public ImmutableSortedDictionary<string, int> Shapes
        {
            get
            {
                ImmutableArray<ImmutableArray<ExpectedSequencePoint>> points = ExpectedSequencePoints;
                var shapes = new SortedDictionary<string, int>();
                void Count(string shape, int count)
                {
                    shapes[shape] = shapes.GetValueOrDefault(shape) + count;
                }

                for (int i = 0; i < Methods.Length; i++)
                {
                    GeneratedLineMethod method = Methods[i];
                    Count("class-level directive", method.ClassLevelDirective is null ? 0 : 1);
                    Count("trailing directive", method.Trailing.IsEmpty ? 0 : 1);
                    Count("method in an included file", method.Inclusion == GeneratedInclusion.Method ? 1 : 0);
                    Count("instructions included in a body", method.Inclusion == GeneratedInclusion.Body ? 1 : 0);
                    Count("blank or comment lines", method.Fillers.Count(count => count > 0));
                    Count("instruction on the previous instruction's line", method.SameLine.Count(sameLine => sameLine));
                    Count("two-byte instruction", method.TwoByte.Count(twoByte => twoByte));
                    Count("implicit point", points[i].Count(point => point.Implicit));
                    Count("directive point", points[i].Count(point => !point.Implicit));
                    Count("point in an included file", points[i].Count(point => point.RecordedDocument.StartsWith("inc", StringComparison.Ordinal)));
                    Count("method with a body and no point", !method.Bodyless && points[i].IsEmpty ? 1 : 0);
                    Count("instruction without a point of its own", method.Bodyless ? 0 : method.Slots.Length - points[i].Length);
                    Count("method whose points mix implicit and directive points",
                        points[i].Any(point => point.Implicit) && points[i].Any(point => !point.Implicit) ? 1 : 0);
                }

                Count("second input file", SecondInputStart is null ? 0 : 1);
                return shapes.ToImmutableSortedDictionary();
            }
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
                int methodCount = random.Next(1, 5);
                for (int i = 0; i < methodCount; i++)
                {
                    // Sometimes an abstract method: its directives still name files, but it has no points.
                    bool bodyless = random.Next(8) == 0;
                    int instructionCount = bodyless ? 1 : random.Next(1, 7);

                    // Sometimes the whole method, or a run of its instructions, is in an included file.
                    GeneratedInclusion inclusion = random.Next(8) switch
                    {
                        0 => GeneratedInclusion.Method,
                        1 when !bodyless => GeneratedInclusion.Body,
                        _ => GeneratedInclusion.None,
                    };
                    int includeStart = inclusion == GeneratedInclusion.Body ? random.Next(instructionCount) : 0;
                    int includeEnd = inclusion == GeneratedInclusion.Body ? random.Next(includeStart + 1, instructionCount + 1) : 0;

                    // Many methods have no directive of their own, so their points come from the lines of the source
                    // or from a directive still in effect, which may give them no point at all.
                    bool withDirectives = random.Next(5) >= 2;
                    var slots = ImmutableArray.CreateBuilder<ImmutableArray<GeneratedLineDirective>>(instructionCount);
                    var fillers = ImmutableArray.CreateBuilder<int>(instructionCount);
                    var sameLine = ImmutableArray.CreateBuilder<bool>(instructionCount);
                    var twoByte = ImmutableArray.CreateBuilder<bool>(instructionCount);
                    for (int offset = 0; offset < instructionCount; offset++)
                    {
                        // Mostly no directive or one before an instruction; sometimes two at one offset.
                        int directiveCount = withDirectives ? random.Next(10) switch { < 4 => 0, < 9 => 1, _ => 2 } : 0;
                        var slot = ImmutableArray.CreateBuilder<GeneratedLineDirective>(directiveCount);
                        for (int d = 0; d < directiveCount; d++)
                        {
                            slot.Add(NextDirective(allowHidden: true));
                        }

                        int fillerCount = random.Next(4) == 0 ? random.Next(1, 3) : 0;

                        // An instruction can share the previous instruction's line when nothing comes between them,
                        // including an #include boundary.
                        bool atIncludeBoundary = inclusion == GeneratedInclusion.Body && (offset == includeStart || offset == includeEnd);
                        bool onPreviousLine = !bodyless && offset > 0 && directiveCount == 0 && fillerCount == 0 && !atIncludeBoundary &&
                            random.Next(4) == 0;

                        // Sometimes a two-byte instruction, so that offsets differ from instruction indices.
                        bool twoByteInstruction = !bodyless && offset < instructionCount - 1 && random.Next(3) == 0;

                        slots.Add(slot.MoveToImmutable());
                        fillers.Add(bodyless ? 0 : fillerCount);
                        sameLine.Add(onPreviousLine);
                        twoByte.Add(twoByteInstruction);
                    }

                    // Sometimes a directive after the last instruction: it gives this method no point, and applies to
                    // the next instruction, which is in a later method.
                    ImmutableArray<GeneratedLineDirective> trailing = !bodyless && random.Next(6) == 0
                        ? [NextDirective(allowHidden: true)]
                        : [];

                    methods.Add(new GeneratedLineMethod(
                        LanguageBefore: random.Next(8) == 0,
                        ClassLevelDirective: random.Next(6) == 0 ? NextDirective(allowHidden: false) : null,
                        Bodyless: bodyless,
                        Locals: bodyless ? null : s_locals[random.Next(s_locals.Length)],
                        Slots: slots.MoveToImmutable(),
                        Fillers: fillers.MoveToImmutable(),
                        SameLine: sameLine.MoveToImmutable(),
                        TwoByte: twoByte.MoveToImmutable(),
                        Trailing: trailing,
                        Inclusion: inclusion,
                        IncludeStart: includeStart,
                        IncludeEnd: includeEnd));
                }

                // Sometimes a second input file, holding the methods from a split point on (possibly none).
                int? secondInputStart = random.Next(3) == 0 ? random.Next(1, methodCount + 1) : null;
                programs.Add(new GeneratedDocumentProgram(methods.ToImmutable(), secondInputStart));
            }

            return programs.MoveToImmutable();
        }
    }
}
