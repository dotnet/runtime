// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;
using System.IO;
using System.Resources;
using Internal.TypeSystem;

namespace ILVerify
{
    internal sealed class ILVerifyRootCommand : RootCommand
    {
        public Argument<Dictionary<string, string>> InputFilePath { get; } =
            new("input-file-path") { CustomParser = result => Helpers.BuildPathDictionary(result.Tokens, true), Description = "Input file(s)", Arity = ArgumentArity.OneOrMore };
        public Option<Dictionary<string, string>> Reference { get; } =
            new("--reference", "-r") { CustomParser = result => Helpers.BuildPathDictionary(result.Tokens, false), DefaultValueFactory = result => Helpers.BuildPathDictionary(result.Tokens, false), Description = "Reference metadata from the specified assembly" };
        public Option<string> SystemModule { get; } =
            new("--system-module", "-s") { Description = "System module name (default: mscorlib)" };
        public Option<bool> SanityChecks { get; } =
            new("--sanity-checks", "-c") { Description = "Check for valid constructs that are likely mistakes" };
        public Option<string[]> Include { get; } =
            new("--include", "-i") { Description = "Use only methods/types/namespaces, which match the given regular expression(s)" };
        public Option<FileInfo> IncludeFile { get; } =
            new Option<FileInfo>("--include-file") { Description = "Same as --include, but the regular expression(s) are declared line by line in the specified file." }.AcceptExistingOnly();
        public Option<string[]> Exclude { get; } =
            new("--exclude", "-e") { Description = "Skip methods/types/namespaces, which match the given regular expression(s)" };
        public Option<FileInfo> ExcludeFile { get; } =
            new Option<FileInfo>("--exclude-file") { Description = "Same as --exclude, but the regular expression(s) are declared line by line in the specified file." }.AcceptExistingOnly();
        public Option<string[]> IgnoreError { get; } =
            new("--ignore-error", "-g") { Description = "Ignore errors, which match the given regular expression(s)" };
        public Option<FileInfo> IgnoreErrorFile { get; } =
            new Option<FileInfo>("--ignore-error-file") { Description = "Same as --ignore-error, but the regular expression(s) are declared line by line in the specified file." }.AcceptExistingOnly();
        public Option<bool> ListErrors { get; } =
            new("--list-errors") { Description = "List the error names that --ignore-error matches", Action = new ListErrorsAction() };
        public Option<bool> Statistics { get; } =
            new("--statistics") { Description = "Print verification statistics" };
        public Option<bool> Verbose { get; } =
            new("--verbose") { Description = "Verbose output" };
        public Option<bool> Tokens { get; } =
            new("--tokens", "-t") { Description = "Include metadata tokens in error messages" };
        public Option<bool> MetadataReferencesOnly { get; } =
            new("--metadata-references-only") { Description = "Only validate metadata references" };
        public Option<bool> AllowIgnoresAccessChecks { get; } =
            new("--allow-ignores-access-checks") { Description = "Respect IgnoresAccessChecksToAttribute" };

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
            Options.Add(ListErrors);
            Options.Add(Statistics);
            Options.Add(Verbose);
            Options.Add(Tokens);
            Options.Add(MetadataReferencesOnly);
            Options.Add(AllowIgnoresAccessChecks);

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

        // Like --help, --list-errors works without input files and does not verify any that are given.
        private sealed class ListErrorsAction : SynchronousCommandLineAction
        {
            public override int Invoke(ParseResult parseResult)
            {
                ResourceManager messages = new ResourceManager("ILVerification.Strings", typeof(Verifier).Assembly);
                Console.WriteLine("Verification errors (--ignore-error matches these names):");
                foreach (VerifierError code in Enum.GetValues<VerifierError>())
                {
                    if (code != VerifierError.None)
                    {
                        Console.WriteLine($"  {code}: {messages.GetString(code.ToString(), CultureInfo.InvariantCulture)}");
                    }
                }
                Console.WriteLine();

                Console.WriteLine("Type system errors (--ignore-error also matches these names):");
                foreach (ExceptionStringID id in Enum.GetValues<ExceptionStringID>())
                {
                    Console.WriteLine($"  {id}");
                }
                Console.WriteLine();

                Console.WriteLine("An error with neither kind of name, such as an exception while verifying a method, matches None.");
                return 0;
            }
        }
    }
}
