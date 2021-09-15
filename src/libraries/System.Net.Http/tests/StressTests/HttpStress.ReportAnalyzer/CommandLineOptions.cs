using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.IO;
using System.CommandLine.Parsing;

namespace HttpStress.ReportAnalyzer
{
    internal class CommandLineOptions
    {
        public bool Test { get; set; }

        public static bool TryParse(string[] args, out CommandLineOptions? options)
        {
            var cmd = new RootCommand();
            cmd.AddOption(new Option<bool>("-test", () => false, "Run tests."));

            ParseResult? cmdline = cmd.Parse(args);
            if (cmdline.Errors.Count > 0)
            {
                foreach (ParseError error in cmdline.Errors)
                {
                    Console.WriteLine(error);
                }
                Console.WriteLine();
                new HelpBuilder(new SystemConsole()).Write(cmd);
                options = null;
                return false;
            }

            options = new CommandLineOptions()
            {
                Test = cmdline.ValueForOption<bool>("-test")
            };
            return true;
        }
    }
}
