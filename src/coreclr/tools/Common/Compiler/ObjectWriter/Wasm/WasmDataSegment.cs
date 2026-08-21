// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using ILCompiler.DependencyAnalysis;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.Text;

namespace ILCompiler.ObjectWriter
{
    internal enum WasmDataSegmentType : byte
    {
        Active = 0,  // (data list(byte) (active offset-expr))
        Passive = 1, // (data list(byte) passive)
        ActiveMemorySpecified = 2 // (data list(byte) (active memidx offset-expr))
    }

    internal interface IWasmDataSegment : IWasmEmittable
    {
        int HeaderSize { get; }
        int RawContentSize { get; }
        int Padding { get; set; }
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
            int length = DwarfHelper.WriteULEB128(headerBuffer, (ulong)type);
            if (type == WasmDataSegmentType.Active)
            {
                length += initExpr.Encode(headerBuffer.Slice(length));
            }
            else if (type != WasmDataSegmentType.Passive)
            {
                throw new NotSupportedException();
            }

            Debug.Assert(headerBuffer.Slice(length).Length == Relocation.WASM_PADDED_RELOC_SIZE_32);
            DwarfHelper.WritePaddedULEB128(headerBuffer.Slice(length), (ulong)contentSize);
            return headerBuffer.Length;
        }

        public static void EmitPadding(Stream outputFileStream, int padding)
        {
            if (padding == 0)
            {
                return;
            }

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

    internal sealed class WasmByteArrayDataSegment : IWasmDataSegment
    {
        private readonly byte[] _contents;
        private readonly WasmDataSegmentType _type;
        private readonly WasmInstructionGroup _initExpr;
        private bool _paddingSet;
        private int _padding;

        public WasmByteArrayDataSegment(
            byte[] contents,
            Utf8String name,
            WasmDataSegmentType type,
            WasmInstructionGroup initExpr)
        {
            Debug.Assert(contents is not null);
            Debug.Assert(!name.IsNull);
            Debug.Assert(type is WasmDataSegmentType.Active or WasmDataSegmentType.Passive);
            Debug.Assert((type == WasmDataSegmentType.Active) == (initExpr is not null));

            _contents = contents;
            _type = type;
            _initExpr = initExpr;
            Name = name;
        }

        public Utf8String Name { get; }
        public int HeaderSize => WasmDataSegmentEncoding.GetHeaderSize(_type, _initExpr);

        public int Padding
        {
            get
            {
                Debug.Assert(_paddingSet);
                return _padding;
            }
            set
            {
                _paddingSet = true;
                _padding = value;
            }
        }

        public int ContentSize => _contents.Length + Padding;
        public int RawContentSize => _contents.Length;

        public int EncodeSize() => HeaderSize + ContentSize;

        public int EmitToStream(Stream outputFileStream)
        {
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            int headerSize = WasmDataSegmentEncoding.EncodeHeader(
                headerBuffer,
                _type,
                _initExpr,
                ContentSize);
            Debug.Assert(headerSize == HeaderSize);
            outputFileStream.Write(headerBuffer);

            outputFileStream.Write(_contents);
            WasmDataSegmentEncoding.EmitPadding(outputFileStream, Padding);

            return headerSize + _contents.Length + Padding;
        }
    }

    internal sealed class WasmDataSegmentEmitter : SectionDataEmitter, IWasmDataSegment
    {
        private static readonly WasmInstructionGroup s_zeroOffset = new([I32.Const(0)]);
        private int _alignment = 1;
        private int _padding;

        public WasmDataSegmentEmitter(
            Stream contents,
            Utf8String name,
            int sectionIndex)
            : base(contents, name, sectionIndex)
        {
        }

        public int Alignment => _alignment;
        public int HeaderSize => WasmDataSegmentEncoding.GetHeaderSize(WasmDataSegmentType.Active, s_zeroOffset);
        public int RawContentSize => (int)ContentReadStream.Length;
        public int Padding
        {
            get => _padding;
            set => _padding = value;
        }

        public void UpdateAlignment(int alignment)
        {
            Debug.Assert(BitOperations.IsPow2(alignment));
            _alignment = Math.Max(_alignment, alignment);
        }

        public override int EncodeSize() => HeaderSize + RawContentSize + Padding;

        public override int EmitToStream(Stream outputFileStream)
        {
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            int headerSize = WasmDataSegmentEncoding.EncodeHeader(
                headerBuffer,
                WasmDataSegmentType.Active,
                s_zeroOffset,
                RawContentSize + Padding);
            Debug.Assert(headerSize == HeaderSize);
            outputFileStream.Write(headerBuffer);

            ContentReadStream.Position = 0;
            ContentReadStream.CopyTo(outputFileStream);
            WasmDataSegmentEncoding.EmitPadding(outputFileStream, Padding);

            return EncodeSize();
        }
    }
}
