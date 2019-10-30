// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;

namespace R2RDump
{
    internal static class CommandLineOptions
    {
        public static RootCommand RootCommand()
        {
            RootCommand command = new RootCommand();
            command.AddOption(new Option(new[] { "--in", "-i" }, "Input file(s) to dump. Expects them to by ReadyToRun images", new Argument<FileInfo[]>()));
            command.AddOption(new Option(new[] { "--out", "-o" }, "Output file path. Dumps everything to the specified file except for help message and exception messages", new Argument<FileInfo>()));
            command.AddOption(new Option(new[] { "--xml", "-x" }, "Output in XML format", new Argument<bool>()));
            command.AddOption(new Option(new[] { "--raw" }, "Dump the raw bytes of each section or runtime function", new Argument<bool>()));
            command.AddOption(new Option(new[] { "--header" }, "Dump R2R header", new Argument<bool>()));
            command.AddOption(new Option(new[] { "--disasm", "-d" }, "Show disassembly of methods or runtime functions", new Argument<bool>()));
            command.AddOption(new Option(new[] { "--naked" }, "Naked dump suppresses most compilation details like placement addresses", new Argument<bool>()));
            command.AddOption(new Option(new[] { "--query", "-q" }, "Query method by exact name, signature, row ID or token", new Argument<string[]>()));
            command.AddOption(new Option(new[] { "--keyword", "-k" }, "Search method by keyword", new Argument<string[]>()));
            command.AddOption(new Option(new[] { "--runtimefunction", "-f" }, "Get one runtime function by id or relative virtual address", new Argument<string[]>()));
            command.AddOption(new Option(new[] { "--section", "-s" }, "Get section by keyword", new Argument<string[]>()));
            command.AddOption(new Option(new[] { "--unwind" }, "Dump unwindInfo", new Argument<bool>()));
            command.AddOption(new Option(new[] { "--gc" }, "Dump gcInfo and slot table", new Argument<bool>()));
            command.AddOption(new Option(new[] { "--sectionContents", "--sc" }, "Dump section contents", new Argument<bool>()));
            command.AddOption(new Option(new[] { "--entrypoints", "-e" }, "Dump list of method / instance entrypoints in the R2R file", new Argument<bool>()));
            command.AddOption(new Option(new[] { "--normalize", "-n" }, "Normalize dump by sorting the various tables and methods (default = unsorted i.e. file order)", new Argument<bool>()));
            command.AddOption(new Option(new[] { "--verbose", "-v" }, "Dump disassembly, unwindInfo, gcInfo and sectionContents", new Argument<bool>()));
            command.AddOption(new Option(new[] { "--diff" }, "Compare two R2R images", new Argument<bool>()));
            command.AddOption(new Option(new[] { "--ignoreSensitive" }, "Ignore sensitive properties in xml dump to avoid failing tests", new Argument<bool>()));
            command.AddOption(new Option(new[] { "--reference", "-r" }, "Explicit reference assembly files", new Argument<FileInfo[]>()));
            command.AddOption(new Option(new[] { "--referencePath", "--rp" }, "Search paths for reference assemblies", new Argument<DirectoryInfo[]>()));
            command.AddOption(new Option(new[] { "--inlineSignatureBinary", "--isb" }, "Embed binary signature into its textual representation", new Argument<bool>()));
            command.AddOption(new Option(new[] { "--signatureBinary", "--sb" }, "Append signature binary to its textual representation", new Argument<bool>()));
            return command;
        }
    }
}
