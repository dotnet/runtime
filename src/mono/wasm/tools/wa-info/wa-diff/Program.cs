using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

using Mono.Options;

namespace WebAssemblyInfo
{
    public class Program
    {
        public static int VerboseLevel;
        static public bool Verbose { get { return VerboseLevel > 0; } }
        static public bool Verbose2 { get { return VerboseLevel > 1; } }

        public static Regex? AssemblyFilter;
        public static Regex? FunctionFilter;
        public static long FunctionOffset = -1;
        public static bool ShowFunctionSize = false;
        public static bool ShowConstLoad = true;
        public static Regex? TypeFilter;

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
            {
                if (Program.ShowFunctionSize)
                    reader1.CompareFunctions(reader2);

                return reader1.CompareSummary(reader2);
            }
            else
                return reader1.CompareDissasembledFunctions(reader2);
        }

        static List<string> ProcessArguments(string[] args)
        {
            var help = false;
            var options = new OptionSet {
                $"Usage: wa-diff OPTIONS* file1.wasm file2.wasm",
                "",
                "Compares WebAssembly binary file(s)",
                "",
                "Copyright 2021 Microsoft Corporation",
                "",
                "Options:",
                { "d|disassemble",
                    "Show functions(s) disassembled code",
                    v => Disassemble = true },
                { "f|function-filter=",
                    "Filter wasm functions {REGEX}",
                    v => FunctionFilter = new Regex (v) },
                { "function-offset=",
                    "Filter wasm functions {REGEX}",
                    v => {
                            if (long.TryParse(v, out var offset))
                                FunctionOffset = offset;
                            else if (v.StartsWith("0x") && long.TryParse(v[2..], NumberStyles.AllowHexSpecifier, null, out offset))
                                FunctionOffset = offset;
                    } },
                { "h|hide-const-loads",
                    "Hide const loads values",
                    v => {
                        ShowConstLoad = false;
                    } },
                { "s|function-size",
                    "Compare function code sizes",
                    v => {
                        ShowFunctionSize = true;
                    } },
                { "v|verbose",
                    "Output information about progress during the run of the tool. Use multiple times to increase verbosity, like -vv",
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