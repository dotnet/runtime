// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.NET.WebAssembly.Webcil;

/// <summary>
/// Restores the file-level alignment of the Webcil payload in a Webcil-in-Wasm module after a tool such as
/// wasm-opt rewrote it. Rewriting re-encodes the module and shifts the payload segment, but tools that read
/// the payload straight from the file, such as R2RDump, require its file offset to stay aligned.
/// Only the trailing zero padding of passive data segment 0 changes; the contents of every segment are kept.
/// </summary>
public static class WebcilWasmPayloadAlignment
{
    /// <summary>The file alignment crossgen2 gives the Webcil payload segment.</summary>
    public const int PayloadFileAlignment = 16;

    private const byte DataSectionId = 11;
    private const uint PassiveSegment = 1;
    // payloadSize and tableSize
    private const int SizesLength = 8;
    private const int PaddedULEB128Length = 5;

    /// <summary>
    /// Gets the file offset of the Webcil payload, the contents of data segment 1.
    /// </summary>
    public static bool TryGetPayloadFileOffset(byte[] module, out long payloadOffset)
    {
        payloadOffset = 0;
        if (!TryReadLayout(module, out Layout layout))
            return false;

        payloadOffset = layout.PayloadOffset;
        return true;
    }

    /// <summary>
    /// Pads data segment 0 so that the Webcil payload starts at a <see cref="PayloadFileAlignment"/>-aligned
    /// file offset. Returns the module unchanged when the payload is already aligned.
    /// </summary>
    /// <exception cref="BadImageFormatException">The module is not a Webcil-in-Wasm module.</exception>
    public static byte[] AlignPayload(byte[] module)
    {
        if (!TryReadLayout(module, out Layout layout))
            throw new BadImageFormatException("The module does not have the Webcil-in-Wasm data segment layout.");

        if (layout.PayloadOffset % PayloadFileAlignment == 0)
            return module;

        for (int i = layout.Segment0Data + SizesLength; i < layout.Segment1Start; i++)
        {
            if (module[i] != 0)
                throw new BadImageFormatException("Data segment 0 has non-zero bytes after the Webcil sizes.");
        }

        // The rewritten section and segment 0 lengths use fixed-width encodings, so the padding does not
        // change the size of the bytes in front of the payload.
        int segmentCountLength = layout.Segment0Start - layout.SectionContent;
        int segment1HeaderLength = layout.PayloadOffset - layout.Segment1Start;
        long unpaddedPayloadOffset = layout.SectionStart + 1 + PaddedULEB128Length + segmentCountLength
            + 1 + PaddedULEB128Length + SizesLength + segment1HeaderLength;
        int padding = (int)((PayloadFileAlignment - unpaddedPayloadOffset % PayloadFileAlignment) % PayloadFileAlignment);
        int remainderLength = layout.SectionEnd - layout.Segment1Start;
        uint sectionLength = checked((uint)(segmentCountLength + 1 + PaddedULEB128Length + SizesLength + padding + remainderLength));

        using var output = new MemoryStream(module.Length + 2 * PaddedULEB128Length + PayloadFileAlignment);
        output.Write(module, 0, layout.SectionStart);
        output.WriteByte(DataSectionId);
        WritePaddedULEB128(output, sectionLength);
        output.Write(module, layout.SectionContent, segmentCountLength);
        output.WriteByte((byte)PassiveSegment);
        WritePaddedULEB128(output, (uint)(SizesLength + padding));
        output.Write(module, layout.Segment0Data, SizesLength);
        output.Write(new byte[padding], 0, padding);
        output.Write(module, layout.Segment1Start, remainderLength);
        output.Write(module, layout.SectionEnd, module.Length - layout.SectionEnd);

        byte[] aligned = output.ToArray();
        if (!TryReadLayout(aligned, out Layout alignedLayout) || alignedLayout.PayloadOffset % PayloadFileAlignment != 0)
            throw new InvalidOperationException("Padding data segment 0 did not align the Webcil payload.");

        return aligned;
    }

    /// <summary>
    /// Aligns the Webcil payload of the module at <paramref name="path"/> in place.
    /// </summary>
    public static void AlignPayload(string path)
    {
        byte[] module = File.ReadAllBytes(path);
        byte[] aligned = AlignPayload(module);
        if (!ReferenceEquals(aligned, module))
            File.WriteAllBytes(path, aligned);
    }

    private struct Layout
    {
        public int SectionStart;
        public int SectionContent;
        public int SectionEnd;
        public int Segment0Start;
        public int Segment0Data;
        public int Segment1Start;
        public int PayloadOffset;
    }

    private static bool TryReadLayout(byte[] module, out Layout layout)
    {
        layout = default;
        try
        {
            if (module.Length < 8 || module[0] != 0 || module[1] != 0x61 || module[2] != 0x73 || module[3] != 0x6d ||
                module[4] != 1 || module[5] != 0 || module[6] != 0 || module[7] != 0)
                return false;

            int position = 8;
            while (position < module.Length)
            {
                int sectionStart = position;
                byte sectionId = module[position++];
                uint sectionLength = ReadULEB128(module, ref position);
                int sectionContent = position;
                int sectionEnd = checked(sectionContent + (int)sectionLength);
                if (sectionEnd > module.Length)
                    return false;

                if (sectionId == DataSectionId)
                {
                    layout.SectionStart = sectionStart;
                    layout.SectionContent = sectionContent;
                    layout.SectionEnd = sectionEnd;
                    return TryReadDataSegments(module, sectionContent, sectionEnd, ref layout);
                }

                position = sectionEnd;
            }
        }
        catch (Exception ex) when (ex is IndexOutOfRangeException or OverflowException)
        {
        }

        return false;
    }

    private static bool TryReadDataSegments(byte[] module, int position, int sectionEnd, ref Layout layout)
    {
        if (ReadULEB128(module, ref position) < 2)
            return false;

        layout.Segment0Start = position;
        if (ReadULEB128(module, ref position) != PassiveSegment)
            return false;

        uint segment0Length = ReadULEB128(module, ref position);
        if (segment0Length < SizesLength)
            return false;

        layout.Segment0Data = position;
        position = checked(position + (int)segment0Length);
        layout.Segment1Start = position;

        uint segment1Flags = ReadULEB128(module, ref position);
        if (segment1Flags == 2)
            ReadULEB128(module, ref position); // memory index
        else if (segment1Flags != 0 && segment1Flags != PassiveSegment)
            return false;
        if (segment1Flags != PassiveSegment && !TrySkipConstantExpression(module, ref position))
            return false;

        uint payloadLength = ReadULEB128(module, ref position);
        layout.PayloadOffset = position;
        return position + (long)payloadLength <= sectionEnd;
    }

    private static bool TrySkipConstantExpression(byte[] module, ref int position)
    {
        while (true)
        {
            byte opcode = module[position++];
            switch (opcode)
            {
                case 0x0b: // end
                    return true;
                case 0x23: // global.get
                case 0x41: // i32.const
                case 0x42: // i64.const
                    ReadULEB128(module, ref position);
                    break;
                case 0x6a: // i32.add
                case 0x6b: // i32.sub
                case 0x6c: // i32.mul
                    break;
                default:
                    return false;
            }
        }
    }

    // Also skips signed LEB128 operands, which share the continuation-bit encoding.
    private static uint ReadULEB128(byte[] module, ref int position)
    {
        uint value = 0;
        int shift = 0;
        byte current;
        do
        {
            if (shift >= 70)
                throw new OverflowException();
            current = module[position++];
            if (shift < 32)
                value |= (uint)(current & 0x7f) << shift;
            shift += 7;
        } while ((current & 0x80) != 0);

        return value;
    }

    private static void WritePaddedULEB128(Stream stream, uint value)
    {
        for (int i = 0; i < PaddedULEB128Length - 1; i++)
        {
            stream.WriteByte((byte)((value & 0x7f) | 0x80));
            value >>= 7;
        }

        stream.WriteByte((byte)(value & 0x7f));
    }
}
