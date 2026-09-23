// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.NET.WebAssembly.Webcil;

internal sealed class WasmModuleInfo
{
    public uint DefinedFunctionCount { get; internal set; }
    public uint ImportedFunctionCount { get; internal set; }
    public IReadOnlyDictionary<string, (byte Kind, uint Index)> Exports => _exports;
    public IReadOnlyList<int?> DefinedI32Functions => _definedI32Functions;
    public IReadOnlyList<(bool Active, int? Offset, long PayloadOffset, uint Size)> DataSegments => _dataSegments;
    public IReadOnlyList<(bool Active, int? Offset)> ElementSegments => _elementSegments;

    internal readonly Dictionary<string, (byte Kind, uint Index)> _exports = new(StringComparer.Ordinal);
    internal readonly List<int?> _definedI32Functions = new();
    internal readonly List<(bool Active, int? Offset, long PayloadOffset, uint Size)> _dataSegments = new();
    internal readonly List<(bool Active, int? Offset)> _elementSegments = new();

    public int? GetExportedI32Function(string name)
    {
        if (!_exports.TryGetValue(name, out (byte Kind, uint Index) export) || export.Kind != 0)
            return null;

        if (export.Index < ImportedFunctionCount)
            return null;

        uint definedIndex = export.Index - ImportedFunctionCount;
        return definedIndex < _definedI32Functions.Count ? _definedI32Functions[(int)definedIndex] : null;
    }
}

internal sealed class WasmModuleInfoReader : WasmModuleReader
{
    private const byte FunctionImport = 0;
    private const byte GlobalImport = 3;
    private const byte I32Const = 0x41;
    private const byte GlobalGet = 0x23;
    private const byte End = 0x0b;

    private readonly WasmModuleInfo _info = new();

    public WasmModuleInfoReader(Stream stream) : base(stream)
    {
    }

    public WasmModuleInfo Read()
    {
        if (!Visit())
            throw new BadImageFormatException("The input is not a valid WebAssembly module.");
        return _info;
    }

    protected override bool VisitSection(Section section, out bool shouldStop)
    {
        shouldStop = false;
        try
        {
            switch (section)
            {
                case Section.Import:
                    ReadImports();
                    break;
                case Section.Function:
                    _info.DefinedFunctionCount = ReadULEB128();
                    break;
                case Section.Export:
                    ReadExports();
                    break;
                case Section.Element:
                    ReadElements();
                    break;
                case Section.Code:
                    ReadCode();
                    break;
                case Section.Data:
                    ReadDataSegments();
                    shouldStop = true;
                    break;
            }
        }
        catch (EndOfStreamException ex)
        {
            throw new BadImageFormatException($"Unexpected end of WebAssembly {section} section.", ex);
        }
        return true;
    }

    private void ReadImports()
    {
        uint count = ReadULEB128();
        for (uint i = 0; i < count; i++)
        {
            ReadName();
            ReadName();
            byte kind = ReadByte();
            switch (kind)
            {
                case FunctionImport:
                    ReadULEB128();
                    _info.ImportedFunctionCount++;
                    break;
                case 1:
                    ReadByte();
                    SkipLimits();
                    break;
                case 2:
                    SkipLimits();
                    break;
                case GlobalImport:
                    ReadByte();
                    ReadByte();
                    break;
                case 4:
                    ReadByte();
                    ReadULEB128();
                    break;
                default:
                    throw new BadImageFormatException($"Unsupported WebAssembly import kind {kind}.");
            }
        }
    }

    private void ReadExports()
    {
        uint count = ReadULEB128();
        for (uint i = 0; i < count; i++)
        {
            string name = ReadName();
            byte kind = ReadByte();
            uint index = ReadULEB128();
            _info._exports[name] = (kind, index);
        }
    }

    private void ReadElements()
    {
        uint count = ReadULEB128();
        for (uint i = 0; i < count; i++)
        {
            uint flags = ReadULEB128();
            if (flags > 7)
                throw new BadImageFormatException($"Unsupported WebAssembly element segment flags {flags}.");

            bool active = flags is 0 or 2 or 4 or 6;
            int? offset = null;
            if (flags is 2 or 6)
                ReadULEB128(); // table index
            if (active)
                offset = ReadConstExpression();

            if (flags is 1 or 2 or 3)
                ReadByte(); // elemkind
            else if (flags >= 4)
                ReadByte(); // reftype

            uint elementCount = ReadULEB128();
            for (uint element = 0; element < elementCount; element++)
            {
                if (flags < 4)
                {
                    ReadULEB128();
                }
                else
                {
                    byte opcode = ReadByte();
                    SkipInstructionOperand(opcode);
                    RequireEnd();
                }
            }
            _info._elementSegments.Add((active, offset));
        }
    }

    private void ReadDataSegments()
    {
        uint count = ReadULEB128();
        for (uint i = 0; i < count; i++)
        {
            uint flags = ReadULEB128();
            bool active = flags is 0 or 2;
            int? offset = null;
            if (flags == 2)
                ReadULEB128(); // memory index
            if (active)
                offset = ReadConstExpression();
            else if (flags != 1)
                throw new BadImageFormatException($"Unsupported WebAssembly data segment flags {flags}.");

            uint size = ReadULEB128();
            long payloadOffset = BaseStream.Position;
            if (size > BaseStream.Length - payloadOffset)
                throw new BadImageFormatException("WebAssembly data segment extends past the end of the file.");
            BaseStream.Seek(size, SeekOrigin.Current);
            _info._dataSegments.Add((active, offset, payloadOffset, size));
        }
    }

    private void ReadCode()
    {
        uint count = ReadULEB128();
        for (uint i = 0; i < count; i++)
        {
            uint bodySize = ReadULEB128();
            long bodyEnd = checked(BaseStream.Position + bodySize);
            if (bodyEnd > BaseStream.Length)
                throw new BadImageFormatException("WebAssembly function body extends past the end of the file.");
            uint localDeclarationCount = ReadULEB128();
            for (uint local = 0; local < localDeclarationCount; local++)
            {
                ReadULEB128();
                ReadByte();
            }

            int? value = null;
            if (BaseStream.Position < bodyEnd && ReadByte() == I32Const)
            {
                int candidate = ReadSLEB32();
                if (BaseStream.Position < bodyEnd && ReadByte() == End && BaseStream.Position == bodyEnd)
                    value = candidate;
            }
            _info._definedI32Functions.Add(value);
            BaseStream.Position = bodyEnd;
        }
    }

    private int? ReadConstExpression()
    {
        byte opcode = ReadByte();
        int? value = opcode == I32Const ? ReadSLEB32() : null;
        if (opcode != I32Const)
            SkipInstructionOperand(opcode);
        RequireEnd();
        return value;
    }

    private void SkipInstructionOperand(byte opcode)
    {
        switch (opcode)
        {
            case I32Const:
                ReadSLEB32();
                break;
            case GlobalGet:
            case 0xd2: // ref.func
                ReadULEB128();
                break;
            case 0xd0: // ref.null
                ReadByte();
                break;
            default:
                throw new BadImageFormatException($"Unsupported WebAssembly constant-expression opcode 0x{opcode:x2}.");
        }
    }

    private void SkipLimits()
    {
        uint flags = ReadULEB128();
        ReadULEB128();
        if ((flags & 1) != 0)
            ReadULEB128();
    }

    private string ReadName()
    {
        uint length = ReadULEB128();
        byte[] bytes = ReadBytes(checked((int)length));
        return Encoding.UTF8.GetString(bytes);
    }

    private int ReadSLEB32()
    {
        int value = 0;
        int shift = 0;
        byte current;
        do
        {
            current = ReadByte();
            value |= (current & 0x7f) << shift;
            shift += 7;
            if (shift > 35)
                throw new OverflowException();
        } while ((current & 0x80) != 0);

        if (shift < 32 && (current & 0x40) != 0)
            value |= -1 << shift;
        return value;
    }

    private void RequireEnd()
    {
        if (ReadByte() != End)
            throw new BadImageFormatException("WebAssembly constant expression is missing its end opcode.");
    }

    private byte ReadByte()
    {
        int value = BaseStream.ReadByte();
        if (value < 0)
            throw new EndOfStreamException();
        return (byte)value;
    }

    private byte[] ReadBytes(int count)
    {
        byte[] bytes = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = BaseStream.Read(bytes, offset, count - offset);
            if (read == 0)
                throw new EndOfStreamException();
            offset += read;
        }
        return bytes;
    }
}

internal static class WasmComponentFile
{
    public static void ReplaceFirstCoreModule(string componentPath, string modulePath, string outputPath)
    {
        byte[] component = File.ReadAllBytes(componentPath);
        byte[] module = File.ReadAllBytes(modulePath);
        if (component.Length < 8 ||
            component[0] != 0 || component[1] != 0x61 || component[2] != 0x73 || component[3] != 0x6d ||
            component[4] != 0x0d || component[5] != 0 || component[6] != 1 || component[7] != 0)
            throw new BadImageFormatException($"{componentPath} is not a WebAssembly component.");

        using FileStream output = File.Create(outputPath);
        output.Write(component, 0, 8);
        int position = 8;
        bool replaced = false;
        while (position < component.Length)
        {
            int sectionStart = position;
            byte sectionId = component[position++];
            uint sectionSize = ReadULEB128(component, ref position);
            int payloadStart = position;
            int sectionEnd = checked(payloadStart + (int)sectionSize);
            if (sectionEnd > component.Length)
                throw new BadImageFormatException("WebAssembly component section extends past the end of the file.");

            if (sectionId == 1 && !replaced)
            {
                output.WriteByte(sectionId);
                WriteULEB128(output, checked((uint)module.Length));
                output.Write(module, 0, module.Length);
                replaced = true;
            }
            else
            {
                output.Write(component, sectionStart, sectionEnd - sectionStart);
            }
            position = sectionEnd;
        }

        if (!replaced)
            throw new BadImageFormatException($"{componentPath} has no core-module section.");
    }

    private static uint ReadULEB128(byte[] data, ref int offset)
    {
        uint value = 0;
        int shift = 0;
        while (offset < data.Length)
        {
            byte current = data[offset++];
            value |= (uint)(current & 0x7f) << shift;
            if ((current & 0x80) == 0)
                return value;
            shift += 7;
            if (shift >= 35)
                throw new OverflowException();
        }
        throw new EndOfStreamException();
    }

    private static void WriteULEB128(Stream stream, uint value)
    {
        do
        {
            byte current = (byte)(value & 0x7f);
            value >>= 7;
            if (value != 0)
                current |= 0x80;
            stream.WriteByte(current);
        } while (value != 0);
    }
}

public static class WasiR2RComposition
{
    public static void InspectComposite(string path, out int functionCount, out int payloadSize)
    {
        WasmModuleInfo module = ReadModule(path);
        (bool Active, int? Offset, long PayloadOffset, uint Size)[] active =
            new List<(bool Active, int? Offset, long PayloadOffset, uint Size)>(module.DataSegments)
                .FindAll(segment => segment.Active)
                .ToArray();
        if (active.Length != 1)
            throw new BadImageFormatException($"Expected exactly one active WebCIL payload segment in {path}, found {active.Length}.");
        if (!module.Exports.TryGetValue("patchWebcilHeader", out (byte Kind, uint Index) patchExport) || patchExport.Kind != 0)
            throw new BadImageFormatException($"{path} is not a self-installing WebCIL image: patchWebcilHeader is missing.");
        functionCount = checked((int)module.DefinedFunctionCount);
        payloadSize = checked((int)active[0].Size);
    }

    public static void InspectHost(
        string path,
        out int imageBase,
        out int imageCapacity,
        out int tableBase,
        out int reservedTableStart)
    {
        WasmModuleInfo module = ReadModule(path);
        imageBase = GetRequiredI32Export(module, "wasi_r2r_image_base");
        imageCapacity = GetRequiredI32Export(module, "wasi_r2r_image_cap");
        tableBase = GetRequiredI32Export(module, "wasi_r2r_table_base");
        reservedTableStart = int.MaxValue;
        foreach ((bool Active, int? Offset) segment in module.ElementSegments)
        {
            if (segment.Active && segment.Offset.HasValue)
                reservedTableStart = Math.Min(reservedTableStart, segment.Offset.Value);
        }
        if (reservedTableStart == int.MaxValue)
            throw new BadImageFormatException($"{path} has no active element segment.");
    }

    public static void ExtractPassiveWebcilPayload(string inputPath, string outputPath)
    {
        using FileStream input = File.OpenRead(inputPath);
        WasmModuleInfo module = new WasmModuleInfoReader(input).Read();
        foreach ((bool Active, int? Offset, long PayloadOffset, uint Size) segment in module.DataSegments)
        {
            if (segment.Size < 4)
                continue;
            input.Position = segment.PayloadOffset;
            byte[] magic = ReadExactly(input, 4);
            if (magic[0] != 0x57 || magic[1] != 0x62 || magic[2] != 0x49 || magic[3] != 0x4c)
                continue;
            if (segment.Active)
                throw new BadImageFormatException($"{inputPath} is a code-carrying image, not a passive component forwarding stub.");

            input.Position = segment.PayloadOffset;
            using FileStream output = File.Create(outputPath);
            CopyExactly(input, output, segment.Size);
            return;
        }
        throw new BadImageFormatException($"{inputPath} does not contain a WebCIL payload.");
    }

    public static void ReplaceFirstCoreModule(string componentPath, string modulePath, string outputPath) =>
        WasmComponentFile.ReplaceFirstCoreModule(componentPath, modulePath, outputPath);

    private static WasmModuleInfo ReadModule(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return new WasmModuleInfoReader(stream).Read();
    }

    private static int GetRequiredI32Export(WasmModuleInfo module, string name) =>
        module.GetExportedI32Function(name)
        ?? throw new BadImageFormatException($"The host does not export constant i32 function '{name}'.");

    private static byte[] ReadExactly(Stream input, int count)
    {
        byte[] bytes = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = input.Read(bytes, offset, count - offset);
            if (read == 0)
                throw new EndOfStreamException();
            offset += read;
        }
        return bytes;
    }

    private static void CopyExactly(Stream input, Stream output, uint count)
    {
        byte[] buffer = new byte[81920];
        long remaining = count;
        while (remaining > 0)
        {
            int read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
            if (read == 0)
                throw new EndOfStreamException();
            output.Write(buffer, 0, read);
            remaining -= read;
        }
    }

}
