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

        public static bool DataSectionAutoSplit = false;
        public static string DataSectionFile = "";
        public static int DataOffset = 0;

        static int Main(string[] args)
        {
            var files = ProcessArguments(args);
            var reader = new WasmRewriter(files[0], files[1]);
            reader.Parse();

            return 0;
        }

        static List<string> ProcessArguments(string[] args)
        {
            var help = false;
            var options = new OptionSet {
                $"Usage: wa-edit OPTIONS* source.wasm destination.wasm",
                "",
                "Modifies WebAssembly file (source.wasm) and writes updated file (destination.wasm)",
                "",
                "Copyright 2022 Microsoft Corporation",
                "",
                "Options:",
                { "a|data-auto-split",
                    "Split the data segment to avoid long empty chunks with zeroes",
                    v => DataSectionAutoSplit = true },
                { "d|data-section=",
                    "Replace the data section with content of the {FILE}",
                    v => DataSectionFile = v },
                { "o|data-offset=",
                    "Data section offset",
                    v => { if (!int.TryParse(v, out DataOffset))
                            Console.WriteLine("Specify number for data-offset option"); } },
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

            if (remaining.Count != 2)
            {
                Console.WriteLine("Provide 2 paths, source and destination");
                options.WriteOptionDescriptions(Console.Out);

                Environment.Exit(1);
            }

            return remaining;
        }
    }
}