using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

using Mono.Options;

namespace WebAssemblyInfo
{
    public class Program
    {
        static int Main(string[] args)
        {
            var context = new WasmContext();
            var files = ProcessArguments(context, args);

            if (files.Count != 2)
            {
                Console.WriteLine("Provide exactly 2 .wasm files");

                return -1;
            }

            var reader1 = new WasmDiffReader(context, files[0]);
            reader1.Parse();

            var reader2 = new WasmDiffReader(context, files[1]);
            reader2.Parse();

            return CompareReaders(context, reader1, reader2);
        }

        static int CompareReaders(WasmContext context, WasmDiffReader reader1, WasmDiffReader reader2)
        {
            int rv = 0;

            if (!context.Disassemble)
            {
                if (context.ShowFunctionSize)
                    reader1.CompareFunctions(reader2);

                rv |= reader1.CompareSummary(reader2);
            }
            else
                rv |= reader1.CompareDissasembledFunctions(reader2);

            if (reader1.ModuleReaders != null && reader2.ModuleReaders != null && reader1.ModuleReaders.Count == reader2.ModuleReaders.Count) {
                for(int i = 0; i < reader1.ModuleReaders.Count; i++) {
                    var module1 = reader1.ModuleReaders[i] as WasmDiffReader;
                    var module2 = reader2.ModuleReaders[i] as WasmDiffReader;

                    if (module1 == null || module2 == null)
                        continue;

                    Console.WriteLine($"Comparing {module1} and {module2}");

                    rv |= CompareReaders(context, module1, module2);
                }
            }

            return rv;
        }

        static List<string> ProcessArguments(WasmContext context, string[] args)
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
                    v => context.Disassemble = true },
                { "f|function-filter=",
                    "Filter wasm functions {REGEX}",
                    v => context.FunctionFilter = new Regex (v) },
                { "function-offset=",
                    "Filter wasm functions {REGEX}",
                    v => {
                            if (long.TryParse(v, out var offset))
                                context.FunctionOffset = offset;
                            else if (v.StartsWith("0x") && long.TryParse(v[2..], NumberStyles.AllowHexSpecifier, null, out offset))
                                context.FunctionOffset = offset;
                    } },
                { "h|hide-const-loads",
                    "Hide const loads values",
                    v => {
                        context.ShowConstLoad = false;
                    } },
                { "s|function-size",
                    "Compare function code sizes",
                    v => {
                        context.ShowFunctionSize = true;
                    } },
                { "v|verbose",
                    "Output information about progress during the run of the tool. Use multiple times to increase verbosity, like -vv",
                    v => context.VerboseLevel++ },
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