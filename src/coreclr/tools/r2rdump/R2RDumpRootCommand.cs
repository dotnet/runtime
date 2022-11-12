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
            new(new[] { "--in", "-i" }, result => Helpers.BuildPathList(result.Tokens), true, "Input file(s) to dump. Expects them to by ReadyToRun images");
        public Option<FileInfo> Out { get; } =
            new(new[] { "--out", "-o" }, "Output file path. Dumps everything to the specified file except for help message and exception messages");
        public Option<bool> Raw { get; } =
            new(new[] { "--raw" }, "Dump the raw bytes of each section or runtime function");
        public Option<bool> Header { get; } =
            new(new[] { "--header" }, "Dump R2R header");
        public Option<bool> Disasm { get; } =
            new(new[] { "--disasm", "-d" }, "Show disassembly of methods or runtime functions");
        public Option<bool> Naked { get; } =
            new(new[] { "--naked" }, "Naked dump suppresses most compilation details like placement addresses");
        public Option<bool> HideOffsets { get; } =
            new(new[] { "--hide-offsets", "--ho" }, "Hide offsets in naked disassembly");

        public Option<string[]> Query { get; } =
            new(new[] { "--query", "-q" }, "Query method by exact name, signature, row ID or token");
        public Option<string[]> Keyword { get; } =
            new(new[] { "--keyword", "-k" }, "Search method by keyword");
        public Option<string[]> RuntimeFunction { get; } =
            new(new[] { "--runtimefunction", "-f" }, "Get one runtime function by id or relative virtual address");
        public Option<string[]> Section { get; } =
            new(new[] { "--section", "-s" }, "Get section by keyword");

        public Option<bool> Unwind { get; } =
            new(new[] { "--unwind" }, "Dump unwindInfo");
        public Option<bool> GC { get; } =
            new(new[] { "--gc" }, "Dump gcInfo and slot table");
        public Option<bool> Pgo { get; } =
            new(new[] { "--pgo" }, "Dump embedded pgo instrumentation data");
        public Option<bool> SectionContents { get; } =
            new(new[] { "--sectionContents", "--sc" }, "Dump section contents");
        public Option<bool> EntryPoints { get; } =
            new(new[] { "--entrypoints", "-e" }, "Dump list of method / instance entrypoints in the R2R file");
        public Option<bool> Normalize { get; } =
            new(new[] { "--normalize", "-n" }, "Normalize dump by sorting the various tables and methods (default = unsorted i.e. file order)");
        public Option<bool> HideTransitions { get; } =
            new(new[] { "--hide-transitions", "--ht" }, "Don't include GC transitions in disassembly output");
        public Option<bool> Verbose { get; } =
            new(new[] { "--verbose", "-v" }, "Dump disassembly, unwindInfo, gcInfo and sectionContents");
        public Option<bool> Diff { get; } =
            new(new[] { "--diff" }, "Compare two R2R images");
        public Option<bool> DiffHideSameDisasm { get; } =
            new(new[] { "--diff-hide-same-disasm" }, "In matching method diff dump, hide functions with identical disassembly");

        public Option<bool> CreatePDB { get; } =
            new(new[] { "--create-pdb" }, "Create PDB");
        public Option<string> PdbPath { get; } =
            new(new[] { "--pdb-path" }, "PDB output path for --create-pdb");

        public Option<bool> CreatePerfmap { get; } =
            new(new[] { "--create-perfmap" }, "Create PerfMap");
        public Option<string> PerfmapPath { get; } =
            new(new[] { "--perfmap-path" }, "PerfMap output path for --create-perfmap");
        public Option<int> PerfmapFormatVersion { get; } =
            new(new[] { "--perfmap-format-version" }, () => ILCompiler.Diagnostics.PerfMapWriter.CurrentFormatVersion, "PerfMap format version for --create-perfmap");

        public Option<List<string>> Reference { get; } =
            new(new[] { "--reference", "-r" }, result => Helpers.BuildPathList(result.Tokens), true, "Explicit reference assembly files");
        public Option<DirectoryInfo[]> ReferencePath { get; } =
            new(new[] { "--referencePath", "--rp" }, "Search paths for reference assemblies");

        public Option<bool> SignatureBinary { get; } =
            new(new[] { "--signatureBinary", "--sb" }, "Append signature binary to its textual representation");
        public Option<bool> InlineSignatureBinary { get; } =
            new(new[] { "--inlineSignatureBinary", "--isb" }, "Embed binary signature into its textual representation");

        public ParseResult Result;

        public R2RDumpRootCommand()
            : base("Parses and outputs the contents of a ReadyToRun image")
        {
            AddOption(In);
            AddOption(Out);
            AddOption(Raw);
            AddOption(Header);
            AddOption(Disasm);
            AddOption(Naked);
            AddOption(HideOffsets);

            AddOption(Query);
            AddOption(Keyword);
            AddOption(RuntimeFunction);
            AddOption(Section);

            AddOption(Unwind);
            AddOption(GC);
            AddOption(Pgo);
            AddOption(SectionContents);
            AddOption(EntryPoints);
            AddOption(Normalize);
            AddOption(HideTransitions);
            AddOption(Verbose);
            AddOption(Diff);
            AddOption(DiffHideSameDisasm);

            AddOption(CreatePDB);
            AddOption(PdbPath);

            AddOption(CreatePerfmap);
            AddOption(PerfmapPath);
            AddOption(PerfmapFormatVersion);

            AddOption(Reference);
            AddOption(ReferencePath);

            AddOption(SignatureBinary);
            AddOption(InlineSignatureBinary);

            this.SetHandler(context =>
            {
                Result = context.ParseResult;

                try
                {
                    context.ExitCode = new Program(this).Run();
                }
                catch (Exception e)
                {
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Red;

                    Console.Error.WriteLine("Error: " + e.Message);
                    Console.Error.WriteLine(e.ToString());

                    Console.ResetColor();

                    context.ExitCode = 1;
                }
            });
        }
    }
}
