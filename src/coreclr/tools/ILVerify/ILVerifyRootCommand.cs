// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;

namespace ILVerify
{
    internal sealed class ILVerifyRootCommand : RootCommand
    {
        public Argument<Dictionary<string, string>> InputFilePath { get; } =
            new("input-file-path", result => Helpers.BuildPathDictionary(result.Tokens, true), false, "Input file(s)") { Arity = ArgumentArity.OneOrMore };
        public Option<Dictionary<string, string>> Reference { get; } =
            new(new[] { "--reference", "-r" }, result => Helpers.BuildPathDictionary(result.Tokens, false), false, "Reference metadata from the specified assembly");
        public Option<string> SystemModule { get; } =
            new(new[] { "--system-module", "-s" }, "System module name (default: mscorlib)");
        public Option<bool> SanityChecks { get; } =
            new(new[] { "--sanity-checks", "-c" }, "Check for valid constructs that are likely mistakes");
        public Option<string[]> Include { get; } =
            new(new[] { "--include", "-i" }, "Use only methods/types/namespaces, which match the given regular expression(s)");
        public Option<FileInfo> IncludeFile { get; } =
            new Option<FileInfo>(new[] { "--include-file" }, "Same as --include, but the regular expression(s) are declared line by line in the specified file.").ExistingOnly();
        public Option<string[]> Exclude { get; } =
            new(new[] { "--exclude", "-e" }, "Skip methods/types/namespaces, which match the given regular expression(s)");
        public Option<FileInfo> ExcludeFile { get; } =
            new Option<FileInfo>(new[] { "--exclude-file" }, "Same as --exclude, but the regular expression(s) are declared line by line in the specified file.").ExistingOnly();
        public Option<string[]> IgnoreError { get; } =
            new(new[] { "--ignore-error", "-g" }, "Ignore errors, which match the given regular expression(s)");
        public Option<FileInfo> IgnoreErrorFile { get; } =
            new Option<FileInfo>(new[] { "--ignore-error-file" }, "Same as --ignore-error, but the regular expression(s) are declared line by line in the specified file.").ExistingOnly();
        public Option<bool> Statistics { get; } =
            new(new[] { "--statistics" }, "Print verification statistics");
        public Option<bool> Verbose { get; } =
            new(new[] { "--verbose", "-v" }, "Verbose output");
        public Option<bool> Tokens { get; } =
            new(new[] { "--tokens", "-t" }, "Include metadata tokens in error messages");

        public ParseResult Result;

        public ILVerifyRootCommand()
            : base("Tool for verifying MSIL code based on ECMA-335.")
        {
            AddArgument(InputFilePath);
            AddOption(Reference);
            AddOption(SystemModule);
            AddOption(SanityChecks);
            AddOption(Include);
            AddOption(IncludeFile);
            AddOption(Exclude);
            AddOption(ExcludeFile);
            AddOption(IgnoreError);
            AddOption(IgnoreErrorFile);
            AddOption(Statistics);
            AddOption(Verbose);
            AddOption(Tokens);

            this.SetHandler(context =>
            {
                Result = context.ParseResult;

                try
                {
                    context.ExitCode = new Program(this).Run();
                }
                catch (Exception e)
                {
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Red;

                    Console.Error.WriteLine("Error: " + e.Message);
                    Console.Error.WriteLine(e.ToString());

                    Console.ResetColor();

                    context.ExitCode = 1;
                }
            });
        }
    }
}
