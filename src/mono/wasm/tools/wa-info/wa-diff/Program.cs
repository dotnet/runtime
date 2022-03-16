using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

using Mono.Options;

namespace WebAssemblyInfo
{
    public class Program
    {
        public static int VerboseLevel;
        static public bool Verbose { get { return VerboseLevel > 0; } }
        static public bool Verbose2 { get { return VerboseLevel > 1; } }

        static internal Regex? AssemblyFilter;
        static internal Regex? FunctionFilter;
        static internal Regex? TypeFilter;

        public static bool AotStats;
        public static bool Disassemble;
        public static bool PrintOffsets;

        static int Main(string[] args)
        {
            var files = ProcessArguments(args);

            if (files.Count != 2)
            {
                Console.WriteLine("Provide exactly 2 .wasm files");

                return -1;
            }

            var reader1 = new WasmDiffReader(files[0]);
            reader1.Parse();

            var reader2 = new WasmDiffReader(files[1]);
            reader2.Parse();

            if (!Disassemble)
                return reader1.CompareSummary(reader2);
            else
                return reader1.CompareDissasembledFunctions(reader2);
        }

        static List<string> ProcessArguments(string[] args)
        {
            var help = false;
            var options = new OptionSet {
                $"Usage: wa-diff OPTIONS* file.wasm [file2.wasm ...]",
                "",
                "Compares WebAssembly binary file(s)",
                "",
                "Copyright 2021 Microsoft Corporation",
                "",
                "Options:",
                { "d|disassemble",
                    "Show functions(s) disassembled code",
                    v => Disassemble = true },
                { "v|verbose",
                    "Output information about progress during the run of the tool",
                    v => VerboseLevel++ },
            };

            var remaining = options.Parse(args);

            if (help || args.Length < 1)
            {
                options.WriteOptionDescriptions(Console.Out);

                Environment.Exit(0);
            }

            return remaining;
        }
    }
}