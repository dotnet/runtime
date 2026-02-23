// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ILDisassembler;

/// <summary>
/// Main entry point for IL disassembly.
/// Reads a PE file and outputs IL assembly syntax.
/// </summary>
public sealed class Disassembler : IDisposable
{
    private readonly PEReader _peReader;
    private readonly MetadataReader _metadataReader;
    private readonly string _filePath;
    private readonly Options _options;

    public Disassembler(string filePath, Options? options = null)
    {
        _filePath = filePath;
        // TODO: Wire up options to control disassembly behavior (ShowBytes, ShowTokens, etc.)
        _options = options ?? new Options();
        var stream = File.OpenRead(filePath);
        // PEReader takes ownership of the stream and will dispose it when PEReader is disposed
        _peReader = new PEReader(stream);

        if (!_peReader.HasMetadata)
        {
            throw new InvalidOperationException("PE file does not contain metadata");
        }

        _metadataReader = _peReader.GetMetadataReader();
    }

    public Disassembler(Stream stream, Options? options = null)
    {
        _filePath = "<stream>";
        _options = options ?? new Options();
        _peReader = new PEReader(stream);

        if (!_peReader.HasMetadata)
        {
            throw new InvalidOperationException("PE file does not contain metadata");
        }

        _metadataReader = _peReader.GetMetadataReader();
    }

    /// <summary>
    /// Disassembles the PE file to the specified output.
    /// </summary>
    public void Disassemble(TextWriter output)
    {
        var writer = new ILWriter(output);

        // Write header comment
        writer.WriteComment($".NET IL Disassembler.  Version {typeof(Disassembler).Assembly.GetName().Version}");
        writer.WriteLine();
        writer.WriteComment($"Metadata version: {_metadataReader.MetadataVersion}");

        // TODO: Support --headers option to output PE headers (.imagebase, .file alignment, .stackreserve, .corflags, .subsystem)
        // TODO: Support --html option to wrap output in HTML tags
        // TODO: Support --rtf option to wrap output in RTF format

        // Disassemble in order:
        // 1. Assembly extern references
        WriteAssemblyRefs(writer);

        // 2. Assembly definition
        WriteAssemblyDef(writer);

        // 3. Module definition
        WriteModuleDef(writer);

        // TODO: Output manifest resources (.mresource)
        // TODO: Output file references (.file)
        // TODO: Output exported types (.class extern)

        // 4. Type definitions (classes, interfaces, etc.)
        WriteTypeDefs(writer);
    }

    private void WriteAssemblyRefs(ILWriter writer)
    {
        foreach (var handle in _metadataReader.AssemblyReferences)
        {
            var assemblyRef = _metadataReader.GetAssemblyReference(handle);
            var name = _metadataReader.GetString(assemblyRef.Name);
            var version = assemblyRef.Version;

            writer.WriteLine($".assembly extern {name}");
            writer.WriteLine("{");
            // TODO: Output .publickeytoken if present
            // TODO: Output .culture if present
            // TODO: Output .hash if present
            writer.WriteLine($"  .ver {version.Major}:{version.Minor}:{version.Build}:{version.Revision}");
            writer.WriteLine("}");
        }
    }

    private void WriteAssemblyDef(ILWriter writer)
    {
        if (!_metadataReader.IsAssembly)
        {
            return;
        }

        var assemblyDef = _metadataReader.GetAssemblyDefinition();
        var name = _metadataReader.GetString(assemblyDef.Name);
        var version = assemblyDef.Version;

        writer.WriteLine($".assembly {name}");
        writer.WriteLine("{");
        // TODO: Output .publickey if present
        // TODO: Output .culture if present
        // TODO: Output .hash algorithm if present
        // TODO: Output custom attributes on assembly
        writer.WriteLine($"  .ver {version.Major}:{version.Minor}:{version.Build}:{version.Revision}");
        writer.WriteLine("}");
    }

    private void WriteModuleDef(ILWriter writer)
    {
        var moduleDef = _metadataReader.GetModuleDefinition();
        var name = _metadataReader.GetString(moduleDef.Name);

        writer.WriteLine($".module {name}");
        writer.WriteComment($"MVID: {{{_metadataReader.GetGuid(moduleDef.Mvid)}}}");
        // TODO: Output custom attributes on module
    }

    private void WriteTypeDefs(ILWriter writer)
    {
        // TODO: Support --typelist option to output full type list for round-trip
        // TODO: Support --classlist option to output class list
        // TODO: Support --visibility/--pubonly options to filter types
        // TODO: Support --item option to disassemble specific item only
        foreach (var handle in _metadataReader.TypeDefinitions)
        {
            var typeDef = _metadataReader.GetTypeDefinition(handle);
            var typeWriter = new TypeDisassembler(_metadataReader, _peReader, typeDef);
            typeWriter.Write(writer);
        }
    }

    public void Dispose()
    {
        _peReader.Dispose();
    }
}
