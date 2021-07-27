// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.IO;

namespace R2RDump
{
    internal static class CommandLineOptions
    {
        public static RootCommand RootCommand()
        {
            RootCommand command = new RootCommand();
            command.AddOption(new Option<FileInfo[]>(new[] { "--in", "-i" }, "Input file(s) to dump. Expects them to by ReadyToRun images"));
            command.AddOption(new Option<FileInfo>(new[] { "--out", "-o" }, "Output file path. Dumps everything to the specified file except for help message and exception messages"));
            command.AddOption(new Option<bool>(new[] { "--raw" }, "Dump the raw bytes of each section or runtime function"));
            command.AddOption(new Option<bool>(new[] { "--header" }, "Dump R2R header"));
            command.AddOption(new Option<bool>(new[] { "--disasm", "-d" }, "Show disassembly of methods or runtime functions"));
            command.AddOption(new Option<bool>(new[] { "--naked" }, "Naked dump suppresses most compilation details like placement addresses"));
            command.AddOption(new Option<bool>(new[] { "--hide-offsets", "--ho" }, "Hide offsets in naked disassembly"));
            command.AddOption(new Option<string[]>(new[] { "--query", "-q" }, "Query method by exact name, signature, row ID or token"));
            command.AddOption(new Option<string[]>(new[] { "--keyword", "-k" }, "Search method by keyword"));
            command.AddOption(new Option<string[]>(new[] { "--runtimefunction", "-f" }, "Get one runtime function by id or relative virtual address"));
            command.AddOption(new Option<string[]>(new[] { "--section", "-s" }, "Get section by keyword"));
            command.AddOption(new Option<bool>(new[] { "--unwind" }, "Dump unwindInfo"));
            command.AddOption(new Option<bool>(new[] { "--gc" }, "Dump gcInfo and slot table"));
            command.AddOption(new Option<bool>(new[] { "--pgo" }, "Dump embedded pgo instrumentation data"));
            command.AddOption(new Option<bool>(new[] { "--sectionContents", "--sc" }, "Dump section contents"));
            command.AddOption(new Option<bool>(new[] { "--entrypoints", "-e" }, "Dump list of method / instance entrypoints in the R2R file"));
            command.AddOption(new Option<bool>(new[] { "--normalize", "-n" }, "Normalize dump by sorting the various tables and methods (default = unsorted i.e. file order)"));
            command.AddOption(new Option<bool>(new[] { "--hide-transitions", "--ht" }, "Don't include GC transitions in disassembly output"));
            command.AddOption(new Option<bool>(new[] { "--verbose", "-v" }, "Dump disassembly, unwindInfo, gcInfo and sectionContents"));
            command.AddOption(new Option<bool>(new[] { "--diff" }, "Compare two R2R images"));
            command.AddOption(new Option<bool>(new[] { "--diff-hide-same-disasm" }, "In matching method diff dump, hide functions with identical disassembly"));
            command.AddOption(new Option<FileInfo[]>(new[] { "--reference", "-r" }, "Explicit reference assembly files"));
            command.AddOption(new Option<DirectoryInfo[]>(new[] { "--referencePath", "--rp" }, "Search paths for reference assemblies"));
            command.AddOption(new Option<bool>(new[] { "--inlineSignatureBinary", "--isb" }, "Embed binary signature into its textual representation"));
            command.AddOption(new Option<bool>(new[] { "--signatureBinary", "--sb" }, "Append signature binary to its textual representation"));
            command.AddOption(new Option<bool>(new[] { "--create-pdb" }, "Create PDB"));
            command.AddOption(new Option<string>(new[] { "--pdb-path" }, "PDB output path for --create-pdb"));
            command.AddOption(new Option<bool>(new[] { "--create-perfmap" }, "Create PerfMap"));
            command.AddOption(new Option<string>(new[] { "--perfmap-path" }, "PerfMap output path for --create-perfmap"));
            command.AddOption(new Option<int>(new[] { "--perfmap-format-version" }, "PerfMap format version for --create-perfmap"));
            return command;
        }
    }
}
