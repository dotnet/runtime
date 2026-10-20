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
    public IReadOnlyList<WasmElementSegmentInfo> ElementSegments => _elementSegments;

    internal readonly Dictionary<string, (byte Kind, uint Index)> _exports = new(StringComparer.Ordinal);
    internal readonly List<int?> _definedI32Functions = new();
    internal readonly List<(bool Active, int? Offset, long PayloadOffset, uint Size)> _dataSegments = new();
    internal readonly List<WasmElementSegmentInfo> _elementSegments = new();

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

internal readonly struct WasmElementSegmentInfo
{
    public WasmElementSegmentInfo(bool active, uint tableIndex, int? offset, uint elementCount)
    {
        Active = active;
        TableIndex = tableIndex;
        Offset = offset;
        ElementCount = elementCount;
    }

    public bool Active { get; }
    public uint TableIndex { get; }
    public int? Offset { get; }
    public uint ElementCount { get; }
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
            uint tableIndex = 0;
            int? offset = null;
            if (flags is 2 or 6)
                tableIndex = ReadULEB128();
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
            _info._elementSegments.Add(new WasmElementSegmentInfo(active, tableIndex, offset, elementCount));
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

internal static class WasmConstantGlobalFolder
{
    private const byte ImportSectionId = 2;
    private const byte GlobalSectionId = 6;
    private const byte ElementSectionId = 9;
    private const byte DataSectionId = 11;
    private const byte I32 = 0x7f;
    private const byte I32Const = 0x41;
    private const byte I64Const = 0x42;
    private const byte F32Const = 0x43;
    private const byte F64Const = 0x44;
    private const byte GlobalGet = 0x23;
    private const byte RefNull = 0xd0;
    private const byte RefFunc = 0xd2;
    private const byte End = 0x0b;

    public static void Fold(string inputPath, string outputPath)
    {
        byte[] module = File.ReadAllBytes(inputPath);
        if (module.Length < 8 ||
            module[0] != 0 || module[1] != 0x61 || module[2] != 0x73 || module[3] != 0x6d ||
            module[4] != 1 || module[5] != 0 || module[6] != 0 || module[7] != 0)
            throw new BadImageFormatException($"{inputPath} is not a WebAssembly core module.");

        var folder = new Folder(module);
        using FileStream output = File.Create(outputPath);
        output.Write(module, 0, 8);
        int position = 8;
        while (position < module.Length)
        {
            int sectionStart = position;
            byte sectionId = module[position++];
            uint sectionSize = ReadULEB128(module, ref position);
            int payloadStart = position;
            int sectionEnd = checked(payloadStart + (int)sectionSize);
            if (sectionEnd > module.Length)
                throw new BadImageFormatException("WebAssembly section extends past the end of the file.");

            byte[]? rewritten = sectionId switch
            {
                ImportSectionId => folder.ReadImports(payloadStart),
                GlobalSectionId => folder.RewriteGlobals(payloadStart),
                ElementSectionId => folder.RewriteElements(payloadStart),
                DataSectionId => folder.RewriteData(payloadStart),
                _ => null,
            };

            if (rewritten is null)
            {
                output.Write(module, sectionStart, sectionEnd - sectionStart);
            }
            else
            {
                output.WriteByte(sectionId);
                WriteULEB128(output, checked((uint)rewritten.Length));
                output.Write(rewritten, 0, rewritten.Length);
            }
            position = sectionEnd;
        }
    }

    private sealed class Folder(byte[] module)
    {
        private readonly Dictionary<uint, int> _constants = new();
        private uint _importedGlobals;
        private uint _globalCount;

        public byte[]? ReadImports(int position)
        {
            uint count = ReadULEB128(module, ref position);
            for (uint i = 0; i < count; i++)
            {
                SkipName(ref position);
                SkipName(ref position);
                byte kind = module[position++];
                switch (kind)
                {
                    case 0:
                        ReadULEB128(module, ref position);
                        break;
                    case 1:
                        SkipValueType(ref position);
                        SkipLimits(ref position);
                        break;
                    case 2:
                        SkipLimits(ref position);
                        break;
                    case 3:
                        SkipValueType(ref position);
                        position++;
                        _importedGlobals++;
                        break;
                    case 4:
                        position++;
                        ReadULEB128(module, ref position);
                        break;
                    default:
                        throw new BadImageFormatException($"Unsupported WebAssembly import kind {kind}.");
                }
            }
            _globalCount = _importedGlobals;
            return null;
        }

        public byte[] RewriteGlobals(int position)
        {
            using var output = new MemoryStream();
            uint count = ReadULEB128(module, ref position);
            WriteULEB128(output, count);
            for (uint i = 0; i < count; i++)
            {
                int typeStart = position;
                byte valueType = module[position];
                SkipValueType(ref position);
                bool mutable = module[position++] != 0;
                output.Write(module, typeStart, position - typeStart);

                int? value = RewriteExpression(ref position, output);
                if (valueType == I32 && !mutable && value.HasValue)
                    _constants[_globalCount] = value.Value;
                _globalCount++;
            }
            return output.ToArray();
        }

        public byte[] RewriteElements(int position)
        {
            using var output = new MemoryStream();
            uint count = ReadULEB128(module, ref position);
            WriteULEB128(output, count);
            for (uint i = 0; i < count; i++)
            {
                int flagsStart = position;
                uint flags = ReadULEB128(module, ref position);
                if (flags > 7)
                    throw new BadImageFormatException($"Unsupported WebAssembly element segment flags {flags}.");
                bool passiveOrDeclarative = (flags & 1) != 0;
                bool explicitTable = (flags & 2) != 0;
                bool usesExpressions = (flags & 4) != 0;

                if (explicitTable && !passiveOrDeclarative)
                    ReadULEB128(module, ref position);
                output.Write(module, flagsStart, position - flagsStart);

                if (!passiveOrDeclarative)
                    RewriteExpression(ref position, output);

                int itemsStart = position;
                if (passiveOrDeclarative || explicitTable)
                {
                    if (usesExpressions)
                        SkipValueType(ref position);
                    else
                        position++;
                }

                uint itemCount = ReadULEB128(module, ref position);
                output.Write(module, itemsStart, position - itemsStart);
                for (uint item = 0; item < itemCount; item++)
                {
                    if (usesExpressions)
                    {
                        RewriteExpression(ref position, output);
                    }
                    else
                    {
                        int indexStart = position;
                        ReadULEB128(module, ref position);
                        output.Write(module, indexStart, position - indexStart);
                    }
                }
            }
            return output.ToArray();
        }

        public byte[] RewriteData(int position)
        {
            using var output = new MemoryStream();
            uint count = ReadULEB128(module, ref position);
            WriteULEB128(output, count);
            for (uint i = 0; i < count; i++)
            {
                int flagsStart = position;
                uint flags = ReadULEB128(module, ref position);
                if (flags > 2)
                    throw new BadImageFormatException($"Unsupported WebAssembly data segment flags {flags}.");
                if (flags == 2)
                    ReadULEB128(module, ref position);
                output.Write(module, flagsStart, position - flagsStart);

                if (flags != 1)
                    RewriteExpression(ref position, output);

                int bytesStart = position;
                uint length = ReadULEB128(module, ref position);
                position = checked(position + (int)length);
                output.Write(module, bytesStart, position - bytesStart);
            }
            return output.ToArray();
        }

        // Copies a constant expression, folding a lone global.get of a known constant global. Returns the
        // expression's i32 value when it is a constant after folding.
        private int? RewriteExpression(ref int position, Stream output)
        {
            int start = position;
            if (module[position] == GlobalGet)
            {
                int index = position + 1;
                uint global = ReadULEB128(module, ref index);
                if (module[index] == End && _constants.TryGetValue(global, out int folded))
                {
                    position = index + 1;
                    output.WriteByte(I32Const);
                    WriteSLEB128(output, folded);
                    output.WriteByte(End);
                    return folded;
                }
            }

            int? value = null;
            bool single = true;
            while (true)
            {
                byte opcode = module[position++];
                if (opcode == End)
                    break;

                switch (opcode)
                {
                    case I32Const:
                        int constant = ReadSLEB128(module, ref position);
                        value = single ? constant : null;
                        break;
                    case I64Const:
                        SkipLEB128(ref position);
                        value = null;
                        break;
                    case F32Const:
                        position += 4;
                        value = null;
                        break;
                    case F64Const:
                        position += 8;
                        value = null;
                        break;
                    case GlobalGet:
                        uint global = ReadULEB128(module, ref position);
                        if (global >= _importedGlobals)
                            throw new InvalidOperationException(
                                $"Constant expression at offset {start} reads module-defined global {global}, which cannot be folded to a constant.");
                        value = null;
                        break;
                    case RefNull:
                        SkipLEB128(ref position);
                        value = null;
                        break;
                    case RefFunc:
                        ReadULEB128(module, ref position);
                        value = null;
                        break;
                    case 0x6a: // i32.add
                    case 0x6b: // i32.sub
                    case 0x6c: // i32.mul
                    case 0x7c: // i64.add
                    case 0x7d: // i64.sub
                    case 0x7e: // i64.mul
                        value = null;
                        break;
                    default:
                        throw new NotSupportedException(
                            $"Unsupported opcode 0x{opcode:x2} in the constant expression at offset {start}.");
                }
                single = false;
            }

            output.Write(module, start, position - start);
            return value;
        }

        private void SkipName(ref int position)
        {
            uint length = ReadULEB128(module, ref position);
            position = checked(position + (int)length);
        }

        private void SkipValueType(ref int position)
        {
            byte type = module[position++];
            // (ref ht) and (ref null ht) carry a heap type.
            if (type == 0x63 || type == 0x64)
                SkipLEB128(ref position);
        }

        private void SkipLimits(ref int position)
        {
            byte flags = module[position++];
            SkipLEB128(ref position);
            if ((flags & 1) != 0)
                SkipLEB128(ref position);
        }

        private void SkipLEB128(ref int position)
        {
            while ((module[position++] & 0x80) != 0)
            {
            }
        }
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

    private static int ReadSLEB128(byte[] data, ref int offset)
    {
        int value = 0;
        int shift = 0;
        byte current;
        do
        {
            if (offset >= data.Length)
                throw new EndOfStreamException();
            current = data[offset++];
            value |= (current & 0x7f) << shift;
            shift += 7;
        } while ((current & 0x80) != 0 && shift < 35);

        if (shift < 32 && (current & 0x40) != 0)
            value |= -1 << shift;
        return value;
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

    private static void WriteSLEB128(Stream stream, int value)
    {
        bool more = true;
        while (more)
        {
            byte current = (byte)(value & 0x7f);
            value >>= 7;
            if ((value == 0 && (current & 0x40) == 0) || (value == -1 && (current & 0x40) != 0))
                more = false;
            else
                current |= 0x80;
            stream.WriteByte(current);
        }
    }
}

public static class WasiR2RComposition
{
    // Every self-installing image defines getWebcilSize and patchWebcilHeader; a component forwarding
    // stub defines nothing else.
    private const uint ComponentStubFunctionCount = 2;

    public static void InspectComposite(
        string path,
        out int functionCount,
        out int tableSlotCount,
        out int payloadSize)
    {
        WasmModuleInfo module = ReadModule(path);
        functionCount = checked((int)module.DefinedFunctionCount);
        WasmElementSegmentInfo? activeElementSegment = null;
        foreach (WasmElementSegmentInfo segment in module.ElementSegments)
        {
            if (!segment.Active)
                continue;
            if (segment.TableIndex != 0)
                throw new BadImageFormatException(
                    $"{path} has an active element segment for table {segment.TableIndex}; expected table 0.");
            if (activeElementSegment is not null)
                throw new BadImageFormatException($"{path} has multiple active element segments.");
            activeElementSegment = segment;
        }
        if (activeElementSegment is null)
            throw new BadImageFormatException($"{path} has no active element segment.");

        tableSlotCount = checked((int)activeElementSegment.Value.ElementCount);
        payloadSize = checked((int)GetSelfInstallingPayloadSegment(module, path).Size);
    }

    public static void InspectHost(
        string path,
        out int imageBase,
        out int imageCapacity,
        out int tableBase,
        out int reservedTableStart,
        out int compositeNameBase,
        out int compositeNameCapacity)
    {
        WasmModuleInfo module = ReadModule(path);
        imageBase = GetRequiredI32Export(module, "wasi_r2r_image_base");
        imageCapacity = GetRequiredI32Export(module, "wasi_r2r_image_cap");
        tableBase = GetRequiredI32Export(module, "wasi_r2r_table_base");
        compositeNameBase = GetRequiredI32Export(module, "wasi_r2r_composite_name_base");
        compositeNameCapacity = GetRequiredI32Export(module, "wasi_r2r_composite_name_cap");
        reservedTableStart = int.MaxValue;
        foreach (WasmElementSegmentInfo segment in module.ElementSegments)
        {
            if (segment.Active && segment.Offset.HasValue)
                reservedTableStart = Math.Min(reservedTableStart, segment.Offset.Value);
        }
        if (reservedTableStart == int.MaxValue)
            throw new BadImageFormatException($"{path} has no active element segment.");
    }

    public static void ExtractComponentStubPayload(string inputPath, string outputPath)
    {
        using FileStream input = File.OpenRead(inputPath);
        WasmModuleInfo module = new WasmModuleInfoReader(input).Read();
        (bool Active, int? Offset, long PayloadOffset, uint Size) segment = GetSelfInstallingPayloadSegment(module, inputPath);
        if (module.DefinedFunctionCount != ComponentStubFunctionCount)
            throw new BadImageFormatException($"{inputPath} is a code-carrying image, not a component forwarding stub.");

        input.Position = segment.PayloadOffset;
        byte[] magic = ReadExactly(input, 4);
        if (magic[0] != 0x57 || magic[1] != 0x62 || magic[2] != 0x49 || magic[3] != 0x4c)
            throw new BadImageFormatException($"{inputPath} does not contain a WebCIL payload.");

        input.Position = segment.PayloadOffset;
        using FileStream output = File.Create(outputPath);
        CopyExactly(input, output, segment.Size);
    }

    public static void ReplaceFirstCoreModule(string componentPath, string modulePath, string outputPath) =>
        WasmComponentFile.ReplaceFirstCoreModule(componentPath, modulePath, outputPath);

    /// <summary>
    /// Rewrites constant expressions that read an immutable, constant-initialized i32 global defined in the
    /// module into the constant itself. Merging turns the composite's imported base globals into module-defined
    /// globals, and reading those in a constant expression requires a proposal engines don't enable by default.
    /// </summary>
    public static void FoldConstantGlobalReads(string inputPath, string outputPath) =>
        WasmConstantGlobalFolder.Fold(inputPath, outputPath);

    private static WasmModuleInfo ReadModule(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return new WasmModuleInfoReader(stream).Read();
    }

    private static (bool Active, int? Offset, long PayloadOffset, uint Size) GetSelfInstallingPayloadSegment(WasmModuleInfo module, string path)
    {
        (bool Active, int? Offset, long PayloadOffset, uint Size)[] active =
            new List<(bool Active, int? Offset, long PayloadOffset, uint Size)>(module.DataSegments)
                .FindAll(segment => segment.Active)
                .ToArray();
        if (active.Length != 1)
            throw new BadImageFormatException($"Expected exactly one active WebCIL payload segment in {path}, found {active.Length}.");
        if (!module.Exports.TryGetValue("patchWebcilHeader", out (byte Kind, uint Index) patchExport) || patchExport.Kind != 0)
            throw new BadImageFormatException($"{path} is not a self-installing WebCIL image: patchWebcilHeader is missing.");
        return active[0];
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
