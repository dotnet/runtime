// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.IO;
using System.Text;

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
        // Nothing but the disassembly goes to the console; there is no logo or progress output.
        string? inputFile = Get(_command.InputFilePath);
        if (string.IsNullOrEmpty(inputFile))
        {
            Console.Error.WriteLine("Error: No input file specified");
            return 1;
        }

        if (!File.Exists(inputFile))
        {
            Console.Error.WriteLine($"error : File '{inputFile}' not found or not a PE file");
            return 1;
        }

        if (Get(_command.ShowSource))
        {
            Console.WriteLine("Warning: 'SOURCE' option is ignored for ildasm on CoreCLR.");
        }

        string? outputFile = Get(_command.OutputFilePath);
        if (outputFile is not null && outputFile.Equals("con", StringComparison.OrdinalIgnoreCase))
        {
            outputFile = null;
        }

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
            Metadata = Get(_command.Metadata) || metadataFilters.Length > 0 || Get(_command.R2RNativeMetadata),
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
            bool ok;
            if (outputFile is not null)
            {
                ok = disassembler.Disassemble(outputFile, Console.Error);
            }
            else
            {
                using var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = false };
                ok = disassembler.Disassemble(stdout);
            }

            return ok ? 0 : 1;
        }
        catch (Exception ex) when (ex is BadImageFormatException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Console.Error.WriteLine($"error : File '{inputFile}' not found or not a PE file ({ex.Message})");
            if (Environment.GetEnvironmentVariable("ILDASM_DEBUG") == "1")
            {
                Console.Error.WriteLine(ex);
            }
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
