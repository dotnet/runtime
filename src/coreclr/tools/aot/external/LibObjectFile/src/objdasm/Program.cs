using System;
using System.IO;
using System.Text.RegularExpressions;
using Mono.Options;

namespace LibObjectFile.Disasm
{
    /// <summary>
    /// A minimalistic program to disassemble functions in .a and ELF files
    /// TODO: handle correctly RIP, labels, relocation...etc.
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {

            var exeName = Path.GetFileNameWithoutExtension(typeof(Program).Assembly.Location);
            bool showHelp = false;


            var objDisasmApp = new ObjDisasmApp();
            
            var _ = string.Empty;
            var options = new OptionSet
            {
                "Copyright (C) 2019 Alexandre Mutel. All Rights Reserved",
                $"{exeName} - Version: "
                +
                $"{typeof(Program).Assembly.GetName().Version.Major}.{typeof(Program).Assembly.GetName().Version.Minor}.{typeof(Program).Assembly.GetName().Version.Build}" + string.Empty,
                _,
                $"Usage: {exeName} [options]+ [.o|.a files]",
                _,
                "Disassemble the global functions found in a list of ELF object .o files or archive `ar` files.",
                _,
                "## Options",
                _,
                {"f|func=", "Add a regex filtering for function symbols. Can add multiples.", v=> objDisasmApp.FunctionRegexFilters.Add(new Regex(v)) },
                {"l|list=", "List functions that can be decompiled.", v=> objDisasmApp.Listing = true},
                _,
                {"h|help", "Show this message and exit", v => showHelp = true },
                {"v|verbose", "Show more verbose progress logs", v => objDisasmApp.Verbose = true },
            };

            try
            {
                var files = options.Parse(args);

                if (showHelp)
                {
                    options.WriteOptionDescriptions(Console.Out);
                    return 0;
                }
                
                foreach (var file in files)
                {
                    var filePath = Path.Combine(Environment.CurrentDirectory, file);
                    if (!File.Exists(filePath))
                    {
                        throw new OptionException($"The file {file} does not exist", "[files]");
                    }

                    objDisasmApp.Files.Add(filePath);
                }

                objDisasmApp.Run();
            }
            catch (Exception exception)
            {
                if (exception is OptionException || exception is ObjectFileException)
                {
                    var backColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(exception.Message);
                    Console.ForegroundColor = backColor;
                    Console.WriteLine("See --help for usage");
                    return 1;
                }
                else
                {
                    throw;
                }
            }

            return 0;
        }
    }
}
