// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using ILCompiler.DependencyAnalysis;
using ILCompiler.ObjectWriter.WasmInstructions;

namespace ILCompiler.ObjectWriter
{
    internal enum WasmDataSegmentType : byte
    {
        Active = 0,  // (data list(byte) (active offset-expr))
        Passive = 1, // (data list(byte) passive)
        ActiveMemorySpecified = 2 // (data list(byte) (active memidx offset-expr))
    }

    /// <summary>
    /// Interface for a data segment in a WASM data section.
    /// These members help ensure the Data section can properly align the segment content in the WASM module.
    /// </summary>
    internal interface IWasmDataSegment : IWasmEmittable
    {
        /// <summary>
        /// The size of the header of the segment.
        /// </summary>
        int HeaderSize { get; }

        /// <summary>
        /// The size of the content of the segment, excluding any padding.
        /// </summary>
        int RawContentSize { get; }

        /// <summary>
        /// The required alignment of the segment content in the WASM module.
        /// </summary>
        int Alignment { get; }

        /// <summary>
        /// Sets the padding after the content of the segment to ensure that following segments are aligned properly.
        /// The padding should be included in IWasmEmittable.EncodeSize() and IWasmEmittable.EmitToStream(), but not RawContentSize.
        /// </summary>
        void SetPadding(int value);
    }

    internal static class WasmDataSegmentEncoding
    {
        public static int GetHeaderSize(
            WasmDataSegmentType type,
            WasmInstructionGroup initExpr)
        {
            return type switch
            {
                WasmDataSegmentType.Active =>
                    (int)DwarfHelper.SizeOfULEB128((ulong)type) +
                    initExpr.EncodeSize() +
                    Relocation.WASM_PADDED_RELOC_SIZE_32,
                WasmDataSegmentType.Passive =>
                    (int)DwarfHelper.SizeOfULEB128((ulong)type) +
                    Relocation.WASM_PADDED_RELOC_SIZE_32,
                _ => throw new NotSupportedException(),
            };
        }

        public static int EncodeHeader(
            Span<byte> headerBuffer,
            WasmDataSegmentType type,
            WasmInstructionGroup initExpr,
            int contentSize)
        {
            Debug.Assert(type is not WasmDataSegmentType.ActiveMemorySpecified,
                "ActiveMemorySpecified isn't implemented yet and probably shouldn't be needed here.");
            int length = DwarfHelper.WriteULEB128(headerBuffer, (ulong)type);
            if (type == WasmDataSegmentType.Active)
            {
                length += initExpr.Encode(headerBuffer.Slice(length));
            }

            Debug.Assert(headerBuffer.Slice(length).Length == Relocation.WASM_PADDED_RELOC_SIZE_32);
            DwarfHelper.WritePaddedULEB128(headerBuffer.Slice(length), (ulong)contentSize);
            return headerBuffer.Length;
        }

        public static void EmitPadding(Stream outputFileStream, int padding)
        {
            if (padding == 0)
                return;

            Span<byte> paddingBytes = stackalloc byte[Math.Min(padding, 256)];
            paddingBytes.Clear();
            while (padding > 0)
            {
                int paddingSize = Math.Min(padding, paddingBytes.Length);
                outputFileStream.Write(paddingBytes.Slice(0, paddingSize));
                padding -= paddingSize;
            }
        }
    }
}
