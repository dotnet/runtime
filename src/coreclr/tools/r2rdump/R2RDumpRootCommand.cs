// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;

namespace R2RDump
{
    internal sealed class R2RDumpRootCommand : RootCommand
    {
        public Option<List<string>> In { get; } =
            new("--in", "-i") { CustomParser = result => Helpers.BuildPathList(result.Tokens), DefaultValueFactory = result => Helpers.BuildPathList(result.Tokens), Description = "Input file(s) to dump. Expects them to by ReadyToRun images" };
        public Option<FileInfo> Out { get; } =
            new("--out", "-o") { Description = "Output file path. Dumps everything to the specified file except for help message and exception messages" };
        public Option<bool> Raw { get; } =
            new("--raw") { Description = "Dump the raw bytes of each section or runtime function" };
        public Option<bool> Header { get; } =
            new("--header") { Description = "Dump R2R header" };
        public Option<bool> Disasm { get; } =
            new("--disasm", "-d") { Description = "Show disassembly of methods or runtime functions" };
        public Option<bool> Naked { get; } =
            new("--naked") { Description = "Naked dump suppresses most compilation details like placement addresses" };
        public Option<bool> HideOffsets { get; } =
            new("--hide-offsets", "--ho") { Description = "Hide offsets in naked disassembly" };

        public Option<string[]> Query { get; } =
            new("--query", "-q") { Description = "Query method by exact name, signature, row ID or token" };
        public Option<string[]> Keyword { get; } =
            new("--keyword", "-k") { Description = "Search method by keyword" };
        public Option<string[]> RuntimeFunction { get; } =
            new("--runtimefunction", "-f") { Description = "Get one runtime function by id or relative virtual address" };
        public Option<string[]> Section { get; } =
            new("--section", "-s") { Description = "Get section by keyword" };

        public Option<bool> Unwind { get; } =
            new("--unwind") { Description = "Dump unwindInfo" };
        public Option<bool> GC { get; } =
            new("--gc") { Description = "Dump gcInfo and slot table" };
        public Option<bool> Pgo { get; } =
            new("--pgo") { Description = "Dump embedded pgo instrumentation data" };
        public Option<bool> SectionContents { get; } =
            new("--sectionContents", "--sc") { Description = "Dump section contents" };
        public Option<bool> EntryPoints { get; } =
            new("--entrypoints", "-e") { Description = "Dump list of method / instance entrypoints in the R2R file" };
        public Option<bool> Normalize { get; } =
            new("--normalize", "-n") { Description = "Normalize dump by sorting the various tables and methods (default = unsorted i.e. file order)" };
        public Option<bool> HideTransitions { get; } =
            new("--hide-transitions", "--ht") { Description = "Don't include GC transitions in disassembly output" };
        public Option<bool> Verbose { get; } =
            new("--verbose") { Description = "Dump disassembly, unwindInfo, gcInfo and sectionContents" };
        public Option<bool> Diff { get; } =
            new("--diff") { Description = "Compare two R2R images" };
        public Option<bool> DiffHideSameDisasm { get; } =
            new("--diff-hide-same-disasm") { Description = "In matching method diff dump, hide functions with identical disassembly" };

        public Option<bool> CreatePDB { get; } =
            new("--create-pdb") { Description = "Create PDB" };
        public Option<string> PdbPath { get; } =
            new("--pdb-path") { Description = "PDB output path for --create-pdb" };

        public Option<bool> CreatePerfmap { get; } =
            new("--create-perfmap") { Description = "Create PerfMap" };
        public Option<string> PerfmapPath { get; } =
            new("--perfmap-path") { Description = "PerfMap output path for --create-perfmap" };
        public Option<int> PerfmapFormatVersion { get; } =
            new("--perfmap-format-version") { DefaultValueFactory = _ => ILCompiler.Diagnostics.PerfMapWriter.CurrentFormatVersion, Description = "PerfMap format version for --create-perfmap" };

        public Option<List<string>> Reference { get; } =
            new("--reference", "-r") { CustomParser = result => Helpers.BuildPathList(result.Tokens), DefaultValueFactory = result => Helpers.BuildPathList(result.Tokens), Description = "Explicit reference assembly files" };
        public Option<DirectoryInfo[]> ReferencePath { get; } =
            new("--referencePath", "--rp") { Description = "Search paths for reference assemblies" };

        public Option<bool> SignatureBinary { get; } =
            new("--signatureBinary", "--sb") { Description = "Append signature binary to its textual representation" };
        public Option<bool> InlineSignatureBinary { get; } =
            new("--inlineSignatureBinary", "--isb") { Description = "Embed binary signature into its textual representation" };
        public Option<bool> ValidateDebugInfo { get; } =
            new("--validateDebugInfo", "--val") { Description = "Validate functions reported debug info." };

        public ParseResult Result;

        public R2RDumpRootCommand()
            : base("Parses and outputs the contents of a ReadyToRun image")
        {
            Options.Add(In);
            Options.Add(Out);
            Options.Add(Raw);
            Options.Add(Header);
            Options.Add(Disasm);
            Options.Add(Naked);
            Options.Add(HideOffsets);

            Options.Add(Query);
            Options.Add(Keyword);
            Options.Add(RuntimeFunction);
            Options.Add(Section);

            Options.Add(Unwind);
            Options.Add(GC);
            Options.Add(Pgo);
            Options.Add(SectionContents);
            Options.Add(EntryPoints);
            Options.Add(Normalize);
            Options.Add(HideTransitions);
            Options.Add(Verbose);
            Options.Add(Diff);
            Options.Add(DiffHideSameDisasm);

            Options.Add(CreatePDB);
            Options.Add(PdbPath);

            Options.Add(CreatePerfmap);
            Options.Add(PerfmapPath);
            Options.Add(PerfmapFormatVersion);

            Options.Add(Reference);
            Options.Add(ReferencePath);

            Options.Add(SignatureBinary);
            Options.Add(InlineSignatureBinary);
            Options.Add(ValidateDebugInfo);

            SetAction(parseResult =>
            {
                Result = parseResult;

                try
                {
                    return new Program(this).Run();
                }
                catch (Exception e)
                {
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Red;

                    Console.Error.WriteLine("Error: " + e.Message);
                    Console.Error.WriteLine(e.ToString());

                    Console.ResetColor();

                    return 1;
                }
            });
        }
    }
}
