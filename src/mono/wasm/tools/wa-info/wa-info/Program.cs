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

        public static int VerboseLevel;
        static public bool Verbose { get { return VerboseLevel > 0; } }
        static public bool Verbose2 { get { return VerboseLevel > 1; } }

        static internal Regex? AssemblyFilter;
        static internal Regex? FunctionFilter;
        static internal long FunctionOffset = -1;
        static internal Regex? TypeFilter;
        public static bool AotStats;
        public static bool Disassemble;
        public static bool PrintOffsets;

        readonly static Dictionary<string, AssemblyReader> assemblies = new();

        static int Main(string[] args)
        {
            var files = ProcessArguments(args);

            foreach (var file in files)
            {
                var reader = new WasmReader(file);
                reader.Parse();

                if (!Disassemble && !AotStats)
                {
                    reader.PrintSummary();
                    continue;
                }

                if (Disassemble)
                    reader.PrintFunctions();

                if (!AotStats)
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

                    //Console.WriteLine($"path {path}");
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

        static List<string> ProcessArguments(string[] args)
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
                    v => AotStats = true },
                { "a|assembly-filter=",
                    "Filter assemblies and process only those matching {REGEX}",
                    v => AssemblyFilter = new Regex (v) },
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
                { "h|help|?",
                    "Show this message and exit",
                    v => help = v != null },
                { "o|instruction-offsets",
                    "Show instruction offsets",
                    v => PrintOffsets = true },
                { "t|type-filter=",
                    "Filter types and process only those matching {REGEX}",
                    v => TypeFilter = new Regex (v) },
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