// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace SuperPmi.Mcs;

internal sealed class McsRootCommand : RootCommand
{
    public Option<string> Verbosity { get; } =
        new("--verbosity", "-verbosity", "-v") { Recursive = true, HelpName = "messagetypes", Description = "Message types to log: e(rrors), w(arnings), m(issing), n(otifications), v(erbose), d(ebug), a(ll), q(uiet). Default: ewmnv" };
    public Option<string> LogFile { get; } =
        new("--write-log-file", "-writeLogFile") { Recursive = true, HelpName = "logfile", Description = "Write log messages to the specified file" };

    public McsRootCommand()
        : base("MCS is a utility for examining and manipulating SuperPMI MC files.")
    {
        Options.Add(Verbosity);
        Options.Add(LogFile);

        {
            Operands operands = new("inputfile", "outputfile");
            Verb("ASMDump", "Dump out the asm file for each input methodContext.", operands)
                .SetAction(r => Run(r, () => VerbASMDump.DoWork(operands.File(r, 0), operands.File(r, 1), operands.Range(r))));
        }
        {
            Argument<string> file1 = File("file1");
            Argument<string> file2 = File("file2");
            Verb("concat", "Concatenate two files without regard to internal formatting. file2 is appended to file1.", file1, file2)
                .SetAction(r => Run(r, () => VerbConcat.DoWork(r.GetValue(file1), r.GetValue(file2))));
        }
        {
            Operands operands = new("file1", "file2");
            Option<bool> thin = Thin();
            Command copy = Verb("copy", "Copy methodContext numbers in range from file1 to file2.", operands, thin);
            copy.Validators.Add(result =>
            {
                if (operands.Range(result) is { Length: 0 })
                    result.AddError("-copy requires a range.");
            });
            copy.SetAction(r => Run(r, () => VerbStrip.DoWork(operands.File(r, 0), operands.File(r, 1), operands.Range(r), strip: false, r.GetValue(thin))));
        }
        {
            Operands operands = new("inputfile");
            Option<bool> simple = Flag("simple", "Don't display the function name/arguments in the header (useful for debugging mcs itself)");
            Verb("dump", "Dump details for each methodContext.", operands, simple)
                .SetAction(r => Run(r, () => VerbDump.DoWork(operands.File(r, 0), operands.Range(r), r.GetValue(simple))));
        }
        {
            Argument<string> input = File("inputfile");
            Verb("dumpMap", "Dump a map from MC index to function name to the console, in CSV format.", input)
                .SetAction(r => Run(r, () => VerbDumpMap.DoWork(r.GetValue(input))));
        }
        {
            Argument<string> input = File("inputfile");
            Verb("dumpToc", "Dump a TOC file.", input)
                .SetAction(r => Run(r, () => VerbDumpToc.DoWork(r.GetValue(input))));
        }
        {
            Argument<int[]> range = Range("range", "Number of method contexts in each output file");
            Argument<string> input = File("inputfile");
            Argument<string> output = File("outputfile");
            Option<bool> thin = Thin();
            Verb("fracture", "Break the input file into chunks sized by range: '-fracture 3 a.mch b' leads to b-00000.mch, b-00001.mch, etc.", range, input, output, thin)
                .SetAction(r => Run(r, () => VerbFracture.DoWork(r.GetValue(input), r.GetValue(output), r.GetValue(range), r.GetValue(thin))));
        }
        {
            Operands operands = new("inputfile");
            Verb("ildump", "Dump raw IL for each methodContext.", operands)
                .SetAction(r => Run(r, () => VerbILDump.DoWork(operands.File(r, 0), operands.Range(r))));
        }
        {
            Argument<string> input = File("inputfile");
            Verb("integ", "Check the integrity of each methodContext.", input)
                .SetAction(r => Run(r, () => VerbInteg.DoWork(r.GetValue(input))));
        }
        {
            Argument<string> input = File("inputfile");
            Verb("jitflags", "Summarize interesting jitflags for the method contexts.", input)
                .SetAction(r => Run(r, () => VerbJitFlags.DoWork(r.GetValue(input))));
        }
        {
            Argument<string> output = File("outputfile");
            Argument<string> pattern = new("pattern") { Description = "Files to merge, e.g. *.mc, relpath/*.mc, or a directory name" };
            Option<bool> recursive = Flag("recursive", "Also merge matching files in all child directories");
            Option<bool> dedup = Flag("dedup", "Skip duplicates when copying (like using -removeDup)");
            Option<bool> thin = Thin();
            Command merge = Verb("merge", "Merge all the input files matching the pattern.", output, pattern, recursive, dedup, thin);
            merge.Validators.Add(result =>
            {
                if (result.GetValue(thin) && !result.GetValue(dedup))
                    result.AddError("'-thin' in '-merge' requires -dedup.");
            });
            merge.SetAction(r => Run(r, () => VerbMerge.DoWork(r.GetValue(output), r.GetValue(pattern), r.GetValue(recursive), r.GetValue(dedup), r.GetValue(thin))));
        }
        {
            Verb("printJITEEVersion", "Print the JITEEVersion GUID with which this was built, in the form: a5eec3a4-4176-43a7-8c2b-a05b551d4f49")
                .SetAction(r => Run(r, VerbPrintJITEEVersion.DoWork));
        }
        {
            Argument<string> input = File("inputfile");
            Argument<string> output = File("outputfile");
            Option<bool> legacy = Flag("legacy", "Compare using the legacy method, which may take much longer");
            Option<bool> thin = Thin();
            Verb("removeDup", "Copy methodContexts from inputfile to outputfile, skipping duplicates.", input, output, legacy, thin)
                .SetAction(r => Run(r, () => VerbRemoveDup.DoWork(r.GetValue(input), r.GetValue(output), r.GetValue(thin), r.GetValue(legacy))));
        }
        {
            Operands operands = new("inputfile", "outputfile");
            Verb("stat", "Report various statistics per method context into outputfile, in CSV format.", operands)
                .SetAction(r => Run(r, () => VerbStat.DoWork(operands.File(r, 0), operands.File(r, 1), operands.Range(r))));
        }
        {
            Operands operands = new("inputfile", "outputfile");
            Option<bool> thin = Thin();
            Verb("strip", "Copy method contexts not in range from inputfile to outputfile. An empty range (e.g., an empty .mcl file from a clean replay) copies the file.", operands, thin)
                .SetAction(r => Run(r, () => VerbStrip.DoWork(operands.File(r, 0), operands.File(r, 1), operands.Range(r), strip: true, r.GetValue(thin))));
        }
        {
            Argument<string> input = File("inputfile");
            Verb("toc", "Create a Table of Contents file for inputfile to allow better random access: '-toc a.mch' creates a.mch.mct.", input)
                .SetAction(r => Run(r, () => VerbTOC.DoWork(r.GetValue(input))));
        }
    }

    // Verbs are subcommands; the '-verb' spellings are aliases.
    private Command Verb(string name, string description, params object[] symbols)
    {
        var command = new Command(name.ToLowerInvariant(), description);
        command.Aliases.Add("-" + name);
        if (name != name.ToLowerInvariant())
            command.Aliases.Add("-" + name.ToLowerInvariant());

        foreach (object symbol in symbols)
        {
            switch (symbol)
            {
                case Option option:
                    command.Options.Add(option);
                    break;
                case Argument argument:
                    command.Arguments.Add(argument);
                    break;
                case Operands operands:
                    command.Arguments.Add(operands.Argument);
                    command.Validators.Add(operands.Validate);
                    break;
            }
        }

        Subcommands.Add(command);
        return command;
    }

    private static Argument<string> File(string name) => new(name);

    private static Option<bool> Flag(string name, string description) => new("--" + name, "-" + name) { Description = description };

    private static Option<bool> Thin() => Flag("thin", "Strip CompileResults from the method contexts that are written");

    private static Argument<int[]> Range(string name, string description) => new(name)
    {
        Description = description,
        Arity = ArgumentArity.ExactlyOne,
        CustomParser = result =>
        {
            if (!MCList.TryParse(result.Tokens[0].Value, out int[] list, out string error))
                result.AddError(error ?? $"'{result.Tokens[0].Value}' is not a method context list");
            return list;
        },
    };

    private int Run(ParseResult result, Func<int> verb)
    {
        try
        {
            string verbosity = result.GetValue(Verbosity);
            if (verbosity != null)
                Logger.SetLogLevel(Logger.ParseLogLevelString(verbosity));
            Logger.OpenLogFile(result.GetValue(LogFile));

            return verb();
        }
        catch (Exception) when (FlushBeforeCrash())
        {
            throw;
        }
        finally
        {
            Logger.Shutdown();
        }
    }

    // Runs as an exception filter, before an unhandled exception terminates the process without running
    // finally blocks, so the output written so far isn't lost.
    private static bool FlushBeforeCrash()
    {
        Logger.Flush();
        return false;
    }

    // File operands optionally preceded by a range: a method context number, a list such as 1-5,9,
    // or an .mcl file. With one operand more than the number of files, the first one is the range.
    private sealed class Operands
    {
        private readonly string[] _fileNames;

        public Operands(params string[] fileNames)
        {
            _fileNames = fileNames;
            Argument = new Argument<string[]>("operands")
            {
                Arity = new ArgumentArity(fileNames.Length, fileNames.Length + 1),
                HelpName = "[range] " + string.Join(' ', fileNames),
                Description = "Files, optionally preceded by a method context number, a range list such as 1-5,9, or an .mcl file",
            };
        }

        public Argument<string[]> Argument { get; }

        private bool HasRange(string[] tokens) => tokens.Length > _fileNames.Length;

        public string File(ParseResult result, int index)
        {
            string[] tokens = result.GetValue(Argument);
            return tokens[index + (HasRange(tokens) ? 1 : 0)];
        }

        public int[] Range(ParseResult result) => ParseRange(result.GetValue(Argument), out _);

        public int[] Range(CommandResult result) => ParseRange(result.GetValue(Argument), out _);

        private int[] ParseRange(string[] tokens, out string error)
        {
            error = null;
            if (tokens == null || !HasRange(tokens))
                return null;
            if (MCList.TryParse(tokens[0], out int[] list, out error))
                return list;
            error ??= "not a method context list";
            return null;
        }

        public void Validate(CommandResult result)
        {
            string[] tokens = result.GetValue(Argument);
            if (tokens != null && HasRange(tokens))
            {
                ParseRange(tokens, out string error);
                if (error != null)
                    result.AddError($"Invalid range '{tokens[0]}': {error}");
            }
        }
    }
}
