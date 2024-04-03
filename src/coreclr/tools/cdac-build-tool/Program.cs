// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

public class Program
{
    internal class ComposeCommand : CliCommand
    {
        private readonly CliArgument<string[]> inputFiles = new ("INPUT [INPUTS...]") { Arity = ArgumentArity.OneOrMore, Description="One or more input files" };
        private readonly CliOption<string> outputFile = new ("-o") { Arity = ArgumentArity.ExactlyOne, HelpName="OUTPUT", Required = true, Description = "Output file" };
        private readonly CliOption<bool> _verboseOption;
        public ComposeCommand (CliOption<bool> verboseOption) : base("compose")
        {
            _verboseOption = verboseOption;
            Add(inputFiles);
            Add(outputFile);
            SetAction(Run);
        }


        private async Task<int> Run(ParseResult parse, CancellationToken token = default)
        {
            var inputs = parse.GetValue(inputFiles);
            if (inputs == null || inputs.Length == 0)
            {
                Console.Error.WriteLine("No input files specified");
                return 1;
            }
            var output = parse.GetValue(outputFile);
            if (output == null)
            {
                Console.Error.WriteLine("No output file specified");
                return 1;
            }
            var verbose = parse.GetValue(_verboseOption);
            var builder = new DataDescriptorModel.Builder();
            var scraper = new ObjectFileScraper(verbose, builder);
            foreach (var input in inputs) {
                token.ThrowIfCancellationRequested();
                if (!await scraper.ScrapeInput(input, token)) {
                    Console.Error.WriteLine ($"could not scrape payload in {input}");
                    return 1;
                }
            }

            var model = builder.Build();
            if (verbose)
            {
                model.DumpModel();
            }
            using var writer = new System.IO.StreamWriter(output);
            var emitter = new ContractDescriptorSourceFileEmitter();
            emitter.JsonDescriptor = "{version: 0}"; // model.ToJson();
            emitter.Emit(writer);
            await writer.FlushAsync(token);
            return 0;
        }
    }

    public static async Task<int> Main(string[] args)
    {
        CliRootCommand rootCommand = new ();
        var verboseOption = new CliOption<bool>("-v", "--verbose") {Recursive = true, Description = "Verbose"};
        rootCommand.Add(verboseOption);
        rootCommand.Add(new DiagramDirective());
        rootCommand.Add(new ComposeCommand(verboseOption));
        return await rootCommand.Parse(args).InvokeAsync();
    }
}
