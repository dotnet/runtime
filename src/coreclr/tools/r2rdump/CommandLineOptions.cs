// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.IO;

namespace R2RDump
{
    public class R2RDumpRootCommand : RootCommand
    {
        public Option<FileInfo[]> In { get; } =
            new(new[] { "--in", "-i" }, "Input file(s) to dump. Expects them to by ReadyToRun images");
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

        public Option<FileInfo[]> Reference { get; } =
            new(new[] { "--reference", "-r" }, "Explicit reference assembly files");
        public Option<DirectoryInfo[]> ReferencePath { get; } =
            new(new[] { "--referencePath", "--rp" }, "Search paths for reference assemblies");

        public Option<bool> SignatureBinary { get; } =
            new(new[] { "--signatureBinary", "--sb" }, "Append signature binary to its textual representation");
        public Option<bool> InlineSignatureBinary { get; } =
            new(new[] { "--inlineSignatureBinary", "--isb" }, "Embed binary signature into its textual representation");

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
                context.ExitCode = new R2RDump(new DumpOptions(this, context.ParseResult)).Run());
        }
    }

    public partial class DumpOptions
    {
        public FileInfo[] In { get; }
        public FileInfo Out { get; }
        public bool Raw { get; }
        public bool Header { get; }
        public bool Disasm { get; }
        public bool Naked { get; }
        public bool HideOffsets { get; }

        public string[] Query { get; }
        public string[] Keyword { get; }
        public string[] RuntimeFunction { get; }
        public string[] Section { get; }

        public bool Unwind { get; }
        public bool GC { get; }
        public bool Pgo { get; }
        public bool SectionContents { get; }
        public bool EntryPoints { get; }
        public bool Normalize { get; }
        public bool HideTransitions { get; }
        public bool Verbose { get; }
        public bool Diff { get; }
        public bool DiffHideSameDisasm { get; }

        public bool CreatePDB { get; }
        public string PdbPath { get; }

        public bool CreatePerfmap { get; }
        public string PerfmapPath { get; }
        public int PerfmapFormatVersion { get; }

        public FileInfo[] Reference { get; }
        public DirectoryInfo[] ReferencePath { get; }

        public bool SignatureBinary { get; }
        public bool InlineSignatureBinary { get; }

        public DumpOptions(R2RDumpRootCommand cmd, ParseResult res)
        {
            In = res.GetValueForOption(cmd.In);
            Out = res.GetValueForOption(cmd.Out);
            Raw = res.GetValueForOption(cmd.Raw);
            Header = res.GetValueForOption(cmd.Header);
            Disasm = res.GetValueForOption(cmd.Disasm);
            Naked = res.GetValueForOption(cmd.Naked);
            HideOffsets = res.GetValueForOption(cmd.HideOffsets);

            Query = res.GetValueForOption(cmd.Query);
            Keyword = res.GetValueForOption(cmd.Keyword);
            RuntimeFunction = res.GetValueForOption(cmd.RuntimeFunction);
            Section = res.GetValueForOption(cmd.Section);

            Unwind = res.GetValueForOption(cmd.Unwind);
            GC = res.GetValueForOption(cmd.GC);
            Pgo = res.GetValueForOption(cmd.Pgo);
            SectionContents = res.GetValueForOption(cmd.SectionContents);
            EntryPoints = res.GetValueForOption(cmd.EntryPoints);
            Normalize = res.GetValueForOption(cmd.Normalize);
            HideTransitions = res.GetValueForOption(cmd.HideTransitions);
            Verbose = res.GetValueForOption(cmd.Verbose);
            Diff = res.GetValueForOption(cmd.Diff);
            DiffHideSameDisasm = res.GetValueForOption(cmd.DiffHideSameDisasm);

            CreatePDB = res.GetValueForOption(cmd.CreatePDB);
            PdbPath = res.GetValueForOption(cmd.PdbPath);

            CreatePerfmap = res.GetValueForOption(cmd.CreatePerfmap);
            PerfmapPath = res.GetValueForOption(cmd.PerfmapPath);
            PerfmapFormatVersion = res.GetValueForOption(cmd.PerfmapFormatVersion);

            Reference = res.GetValueForOption(cmd.Reference);
            ReferencePath = res.GetValueForOption(cmd.ReferencePath);

            SignatureBinary = res.GetValueForOption(cmd.SignatureBinary);
            InlineSignatureBinary = res.GetValueForOption(cmd.InlineSignatureBinary);

            if (Verbose)
            {
                Disasm = true;
                Unwind = true;
                GC = true;
                Pgo = true;
                SectionContents = true;
            }
        }
    }
}
