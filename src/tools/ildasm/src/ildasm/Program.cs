// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.IO;

namespace ILDisassembler;

internal sealed class Program
{
    private readonly IldasmRootCommand _command;
    private readonly ParseResult _result;

    public Program(IldasmRootCommand command, ParseResult result)
    {
        _command = command;
        _result = result;
    }

    public int Run()
    {
        bool quiet = Get(_command.Quiet);

        if (!Get(_command.NoLogo) && !quiet)
        {
            Console.WriteLine(IldasmRootCommand.ProductName);
            Console.WriteLine();
        }

        string? inputFile = Get(_command.InputFilePath);
        if (string.IsNullOrEmpty(inputFile))
        {
            Console.Error.WriteLine("Error: No input file specified");
            return 1;
        }

        if (!File.Exists(inputFile))
        {
            Console.Error.WriteLine($"Error: Input file not found: {inputFile}");
            return 1;
        }

        string? outputFile = Get(_command.OutputFilePath);
        string[] metadataFilters = Get(_command.MetadataFilters) ?? [];

        var options = new Options
        {
            ShowBytes = Get(_command.ShowBytes),
            RawExceptionHandling = Get(_command.RawExceptionHandling),
            ShowTokens = Get(_command.ShowTokens),
            ShowSource = Get(_command.ShowSource),
            ShowLineNumbers = Get(_command.ShowLineNumbers),
            Visibility = Get(_command.Visibility),
            PublicOnly = Get(_command.PublicOnly),
            QuoteAllNames = Get(_command.QuoteAllNames),
            NoCustomAttributes = Get(_command.NoCustomAttributes),
            CustomAttributesVerbal = Get(_command.CustomAttributesVerbal),
            R2RNativeMetadata = Get(_command.R2RNativeMetadata),
            NoIL = Get(_command.NoIL),
            ForwardDeclarations = Get(_command.ForwardDeclarations),
            TypeList = Get(_command.TypeList),
            Headers = Get(_command.Headers),
            Item = Get(_command.Item),
            Stats = Get(_command.Stats),
            ClassList = Get(_command.ClassList),
            Metadata = Get(_command.Metadata) || metadataFilters.Length > 0,
            MetadataFilters = metadataFilters,
            Html = Get(_command.Html),
            Rtf = Get(_command.Rtf),
            Utf8 = Get(_command.Utf8),
            Unicode = Get(_command.Unicode),
        };

        if (Get(_command.All))
        {
            options.Headers = true;
            options.ShowBytes = true;
            options.Stats = true;
            options.ClassList = true;
            options.ShowTokens = true;
        }

        try
        {
            using var disassembler = new Disassembler(inputFile, options);

            TextWriter output = outputFile is not null
                ? new StreamWriter(outputFile)
                : Console.Out;

            try
            {
                disassembler.Disassemble(output);

                if (!quiet)
                {
                    Console.WriteLine("// Disassembly complete");
                }
            }
            finally
            {
                if (outputFile is not null)
                {
                    output.Dispose();
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private T Get<T>(Argument<T> argument) => _result.GetValue(argument)!;

    private T Get<T>(Option<T> option) => _result.GetValue(option)!;

    private static int Main(string[] args)
    {
        IldasmRootCommand command = new();
        string[] normalizedArgs;
        try
        {
            normalizedArgs = NativeCommandLine.Normalize(args, command);
        }
        catch (ArgumentException e)
        {
            Console.Error.WriteLine($"Error: {e.Message}");
            return 1;
        }

        command.SetAction(result =>
        {
            if (result.GetValue(command.WaitForDebugger))
            {
                Console.WriteLine("Waiting for debugger to attach. Press ENTER to continue");
                Console.ReadLine();
            }

            try
            {
                return new Program(command, result).Run();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Error: " + e.Message);
                Console.ResetColor();
                return 1;
            }
        });

        return command
            .Parse(normalizedArgs)
            .Invoke();
    }
}
