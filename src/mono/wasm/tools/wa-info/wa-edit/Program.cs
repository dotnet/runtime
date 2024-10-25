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
        static int Main(string[] args)
        {
            var context = new WasmEditContext();
            var files = ProcessArguments(context, args);
            var reader = new WasmRewriter(context, files[0], files[1]);
            reader.Parse();

            return 0;
        }

        static List<string> ProcessArguments(WasmEditContext context, string[] args)
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
                    v => context.DataSectionAutoSplit = true },
                { "d|data-section=",
                    "Replace the data section with content of the {FILE}",
                    v => context.DataSectionFile = v },
                { "m|data-section-mode=",
                    "Set the data section replacement {MODE}. Possible values: Active, Passive",
                    v => context.DataSectionMode = (string.Equals(v, "Passive", StringComparison.InvariantCultureIgnoreCase)) ? DataMode.Passive : DataMode.Active },
                { "o|data-offset=",
                    "Data section offset",
                    v => { if (!int.TryParse(v, out context.DataOffset))
                            Console.WriteLine("Specify number for data-offset option"); } },
                { "h|help|?",
                    "Show this message and exit",
                    v => help = v != null },
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