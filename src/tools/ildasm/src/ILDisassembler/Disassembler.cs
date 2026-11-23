// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection.PortableExecutable;

namespace ILDisassembler;

/// <summary>
/// Disassembles a PE image into IL assembler source that ilasm can reassemble.
/// </summary>
public sealed class Disassembler : IDisposable
{
    private readonly PEReader _peReader;
    private readonly Options _options;

    /// <param name="peStream">The PE image. The disassembler takes ownership of the stream.</param>
    /// <param name="options">What to disassemble and how.</param>
    public Disassembler(Stream peStream, Options? options = null)
    {
        _options = options ?? new Options();
        _peReader = new PEReader(peStream);
    }

    /// <summary>
    /// Disassembles the image to the specified output, writing error messages to the console.
    /// </summary>
    /// <returns>true on success.</returns>
    public bool Disassemble(TextWriter output) => Disassemble(output, Console.Error);

    /// <summary>
    /// Disassembles the image to the specified output.
    /// </summary>
    /// <returns>true on success.</returns>
    public bool Disassemble(TextWriter output, TextWriter error)
    {
        bool headers = false;
        foreach (IMetadataDisassembler d in _options.MetadataDisassemblers)
        {
            headers |= d.Phase == DisassemblyPhase.Headers;
        }

        // An image without metadata only has headers to show.
        if (!_peReader.HasMetadata && !headers)
        {
            output.Write("\n" + "//  .NET IL Disassembler.  Version " + Dasm.ProductVersion + "\n\n");
            error.WriteLine($"error : '{_options.InputFileName ?? "<stream>"}' has no valid CLR header and cannot be disassembled");
            return false;
        }

        return new Dasm(_peReader, _options, output, error).DumpFile();
    }

    public void Dispose()
    {
        _peReader.Dispose();
    }
}
