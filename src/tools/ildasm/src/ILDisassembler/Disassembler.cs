// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILDisassembler;

/// <summary>
/// Disassembles a PE file into IL assembler source that ilasm can reassemble.
/// The output follows the format of the native ildasm.
/// </summary>
public sealed class Disassembler : IDisposable
{
    private readonly PEReader _peReader;
    private readonly string _filePath;
    private readonly Options _options;

    public Disassembler(string filePath, Options? options = null)
    {
        _filePath = filePath;
        _options = options ?? new Options();
        // PEReader takes ownership of the stream and disposes it.
        _peReader = new PEReader(File.OpenRead(filePath));
    }

    public Disassembler(Stream stream, Options? options = null)
    {
        _filePath = "<stream>";
        _options = options ?? new Options();
        _peReader = new PEReader(stream);
    }

    /// <summary>
    /// Disassembles the PE file to the specified output, as the native ildasm does for console output.
    /// </summary>
    /// <returns>true on success.</returns>
    public bool Disassemble(TextWriter output) => Disassemble(output, Console.Error, outputFilePath: null);

    /// <summary>
    /// Disassembles the PE file. When <paramref name="outputFilePath"/> is set, the output is treated as going
    /// to that file: HTML/RTF formatting applies and embedded managed resources are saved next to it.
    /// </summary>
    /// <returns>true on success.</returns>
    public bool Disassemble(TextWriter output, TextWriter error, string? outputFilePath)
    {
        if (!_peReader.HasMetadata && !_options.Headers)
        {
            output.Write("\n" + "//  .NET IL Disassembler.  Version " + Dasm.ProductVersion + "\n\n");
            error.WriteLine($"error : '{_filePath}' has no valid CLR header and cannot be disassembled");
            return false;
        }

        Dasm dasm = new(_peReader, _options, output, error, outputFilePath is not null, outputFilePath ?? "",
            newLine: outputFilePath is not null && _options.Unicode ? "\r\n" : "\n");
        return dasm.DumpFile(_filePath);
    }

    /// <summary>
    /// Disassembles the PE file into the file at <paramref name="outputFilePath"/>, using the encoding selected by the options.
    /// </summary>
    /// <returns>true on success.</returns>
    public bool Disassemble(string outputFilePath, TextWriter? error = null)
    {
        Encoding encoding = _options.Unicode
            ? new UnicodeEncoding(bigEndian: false, byteOrderMark: true)
            : new UTF8Encoding(encoderShouldEmitUTF8Identifier: _options.Utf8);
        using StreamWriter writer = new(outputFilePath, append: false, encoding);
        return Disassemble(writer, error ?? Console.Error, outputFilePath);
    }

    public void Dispose()
    {
        _peReader.Dispose();
    }
}
