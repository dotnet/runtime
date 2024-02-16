// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;

namespace ILVerify
{
    internal sealed class ILVerifyRootCommand : CliRootCommand
    {
        public CliArgument<Dictionary<string, string>> InputFilePath { get; } =
            new("input-file-path") { CustomParser = result => Helpers.BuildPathDictionary(result.Tokens, true), Description = "Input file(s)", Arity = ArgumentArity.OneOrMore };
        public CliOption<Dictionary<string, string>> Reference { get; } =
            new("--reference", "-r") { CustomParser = result => Helpers.BuildPathDictionary(result.Tokens, false), DefaultValueFactory = result => Helpers.BuildPathDictionary(result.Tokens, false), Description = "Reference metadata from the specified assembly" };
        public CliOption<string> SystemModule { get; } =
            new("--system-module", "-s") { Description = "System module name (default: mscorlib)" };
        public CliOption<bool> SanityChecks { get; } =
            new("--sanity-checks", "-c") { Description = "Check for valid constructs that are likely mistakes" };
        public CliOption<string[]> Include { get; } =
            new("--include", "-i") { Description = "Use only methods/types/namespaces, which match the given regular expression(s)" };
        public CliOption<FileInfo> IncludeFile { get; } =
            new CliOption<FileInfo>("--include-file") { Description = "Same as --include, but the regular expression(s) are declared line by line in the specified file." }.AcceptExistingOnly();
        public CliOption<string[]> Exclude { get; } =
            new("--exclude", "-e") { Description = "Skip methods/types/namespaces, which match the given regular expression(s)" };
        public CliOption<FileInfo> ExcludeFile { get; } =
            new CliOption<FileInfo>("--exclude-file") { Description = "Same as --exclude, but the regular expression(s) are declared line by line in the specified file." }.AcceptExistingOnly();
        public CliOption<string[]> IgnoreError { get; } =
            new("--ignore-error", "-g") { Description = "Ignore errors, which match the given regular expression(s)" };
        public CliOption<FileInfo> IgnoreErrorFile { get; } =
            new CliOption<FileInfo>("--ignore-error-file") { Description = "Same as --ignore-error, but the regular expression(s) are declared line by line in the specified file." }.AcceptExistingOnly();
        public CliOption<bool> Statistics { get; } =
            new("--statistics") { Description = "Print verification statistics" };
        public CliOption<bool> Verbose { get; } =
            new("--verbose") { Description = "Verbose output" };
        public CliOption<bool> Tokens { get; } =
            new("--tokens", "-t") { Description = "Include metadata tokens in error messages" };

        public ParseResult Result;

        public ILVerifyRootCommand()
            : base("Tool for verifying MSIL code based on ECMA-335.")
        {
            Arguments.Add(InputFilePath);
            Options.Add(Reference);
            Options.Add(SystemModule);
            Options.Add(SanityChecks);
            Options.Add(Include);
            Options.Add(IncludeFile);
            Options.Add(Exclude);
            Options.Add(ExcludeFile);
            Options.Add(IgnoreError);
            Options.Add(IgnoreErrorFile);
            Options.Add(Statistics);
            Options.Add(Verbose);
            Options.Add(Tokens);

            this.SetAction(result =>
            {
                Result = result;

                try
                {
                    return new Program(this).Run();
                }
                catch (Exception e)
                {
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Red;

                    Console.Error.WriteLine("Error: " + e.Message);
                    Console.Error.WriteLine(e.ToString());

                    Console.ResetColor();
                }

                return 1;
            });
        }
    }
}
