using System;
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

        readonly static Dictionary<string, AssemblyReader> assemblies = new();

        static int Main(string[] args)
        {
            var files = ProcessArguments(args);

            foreach (var file in files)
            {
                var reader = new WasmReader(file);
                reader.Parse();

                var dir = Path.GetDirectoryName(file);
                if (dir == null)
                    continue;

                foreach (var path in Directory.GetFiles(Path.Combine(dir, "managed"), "*.dll"))
                {
                    Console.WriteLine($"path {path}");
                    var ar = GetAssemblyReader(path);
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
                $"Usage: wa-info.exe OPTIONS* file.wasm [file2.wasm ...]",
                "",
                "Provides information about WebAssembly file(s)",
                "",
                "Copyright 2021 Microsoft Corporation",
                "",
                "Options:",
                { "h|help|?",
                    "Show this message and exit",
                    v => help = v != null },
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