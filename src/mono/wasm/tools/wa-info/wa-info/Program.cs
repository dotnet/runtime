using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

using Mono.Options;

namespace WebAssemblyInfo
{
    public class Program
    {
        static internal Regex? AssemblyFilter;
        static internal Regex? TypeFilter;
        public static bool ListWitImports;
        public static bool ListWitExports;

        readonly static Dictionary<string, AssemblyReader> assemblies = new();

        static int Main(string[] args)
        {
            var context = new WasmContext();
            var files = ProcessArguments(context, args);

            foreach (var file in files)
            {
                var reader = new WasmReader(context, file);
                reader.Parse();

                if (!context.Disassemble && !context.AotStats && !ListWitImports && !ListWitExports)
                {
                    reader.PrintSummary();
                    continue;
                }

                if (ListWitExports)
                    reader.PrintWitExports();

                if (ListWitImports)
                    reader.PrintWitImports();

                if (context.Disassemble)
                    reader.PrintFunctions();

                if (!context.AotStats)
                    continue;

                reader.FindFunctionsCallingInterp();

                var dir = Path.GetDirectoryName(file);
                if (dir == null)
                    continue;

                var managedDir = Path.Combine(dir, "managed");
                if (!Directory.Exists(managedDir))
                    continue;

                foreach (var path in Directory.GetFiles(managedDir, "*.dll"))
                {
                    if (AssemblyFilter != null && !AssemblyFilter.Match(Path.GetFileName(path)).Success)
                        continue;

                    var ar = GetAssemblyReader(path);
                    ar.GetAllMethods();
                }
            }

            return 0;
        }

        static AssemblyReader GetAssemblyReader(string path)
        {
            if (assemblies.TryGetValue(path, out AssemblyReader? reader))
                return reader;

            reader = new AssemblyReader(path);
            assemblies[path] = reader;

            return reader;
        }

        static List<string> ProcessArguments(WasmContext context, string[] args)
        {
            var help = false;
            var options = new OptionSet {
                $"Usage: wa-info OPTIONS* file.wasm [file2.wasm ...]",
                "",
                "Provides information about WebAssembly file(s)",
                "",
                "Copyright 2021 Microsoft Corporation",
                "",
                "Options:",
                { "aot-stats",
                    "Show stats about methods",
                    v => context.AotStats = true },
                { "a|assembly-filter=",
                    "Filter assemblies and process only those matching {REGEX}",
                    v => AssemblyFilter = new Regex (v) },
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
                { "h|help|?",
                    "Show this message and exit",
                    v => help = v != null },
                { "o|instruction-offsets",
                    "Show instruction offsets",
                    v => context.PrintOffsets = true },
                { "t|type-filter=",
                    "Filter types and process only those matching {REGEX}",
                    v => TypeFilter = new Regex (v) },
                { "v|verbose",
                    "Output information about progress during the run of the tool. Use multiple times to increase verbosity, like -vv",
                    v => context.VerboseLevel++ },
                { "wit-imports",
                    "List WIT imports",
                    v => ListWitImports = true },
                { "wit-exports",
                    "List WIT exports",
                    v => ListWitExports = true },
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