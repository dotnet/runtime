// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
        bool all = Get(_command.All);
        bool unicode = Get(_command.Unicode);

        var options = new Options
        {
            ShowBytes = Get(_command.ShowBytes) || all,
            RawExceptionHandling = Get(_command.RawExceptionHandling),
            ShowTokens = Get(_command.ShowTokens) || all,
            ShowSource = Get(_command.ShowSource),
            ShowLineNumbers = Get(_command.ShowLineNumbers),
            Visibility = Get(_command.Visibility),
            PublicOnly = Get(_command.PublicOnly),
            QuoteAllNames = Get(_command.QuoteAllNames),
            NoCustomAttributes = Get(_command.NoCustomAttributes),
            R2RNativeMetadata = Get(_command.R2RNativeMetadata),
            NoIL = Get(_command.NoIL),
            ForwardDeclarations = Get(_command.ForwardDeclarations),
            Item = Get(_command.Item),
            InputFileName = inputFile,
            PdbFileStreamProvider = path => File.Exists(path) ? File.OpenRead(path) : null,
            CustomAttributeDisassembler = Get(_command.CustomAttributesVerbal)
                ? VerbalCustomAttributeDisassembler.Instance
                : ByteArrayCustomAttributeDisassembler.Instance,
            PermissionSetDisassembler = VerbalCustomAttributeDisassembler.Instance,
        };

        // The output order within a phase is the order of the list.
        List<IMetadataDisassembler> metadataDisassemblers = new();
        if (Get(_command.Headers) || all)
        {
            metadataDisassemblers.Add(new HeadersDisassembler());
        }

        if (Get(_command.Stats) || all)
        {
            metadataDisassemblers.Add(new StatisticsDisassembler());
        }

        if (Get(_command.ClassList) || all)
        {
            metadataDisassemblers.Add(new ClassListDisassembler());
        }

        if (Get(_command.Metadata) || metadataFilters.Length > 0 || options.R2RNativeMetadata)
        {
            metadataDisassemblers.Add(new MetaInfoDisassembler(metadataFilters));
        }

        if (Get(_command.TypeList))
        {
            metadataDisassemblers.Add(new TypeListDisassembler());
        }

        // Only file output is formatted as HTML or RTF, has the error messages in it, and has the resources saved next to it.
        if (outputFile is not null)
        {
            options.Formatter = Get(_command.Rtf) ? RtfFormatter.Instance : Get(_command.Html) ? HtmlFormatter.Instance : PlainTextFormatter.Instance;
            options.WriteErrorsToOutput = true;
            if (unicode)
            {
                options.NewLine = "\r\n";
            }

            metadataDisassemblers.Add(new ResourceExtractor(outputFile, path => new FileStream(path, FileMode.Create, FileAccess.Write)));
        }

        options.MetadataDisassemblers = metadataDisassemblers;

        try
        {
            using var disassembler = new Disassembler(File.OpenRead(inputFile), options);
            if (outputFile is not null)
            {
                Encoding encoding = unicode
                    ? new UnicodeEncoding(bigEndian: false, byteOrderMark: true)
                    : new UTF8Encoding(encoderShouldEmitUTF8Identifier: Get(_command.Utf8));
                using StreamWriter writer = new(outputFile, append: false, encoding);
                return disassembler.Disassemble(writer, Console.Error) ? 0 : 1;
            }

            using var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = false };
            return disassembler.Disassemble(stdout, Console.Error) ? 0 : 1;
        }
        catch (Exception ex) when (ex is BadImageFormatException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Console.Error.WriteLine($"error : File '{inputFile}' not found or not a PE file ({ex.Message})");
            Console.Error.WriteLine(ex);
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
