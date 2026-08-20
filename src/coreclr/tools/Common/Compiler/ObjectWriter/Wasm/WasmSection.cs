// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Internal.Text;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Interface for section-like objects that can be emitted to a wasm module stream.
    /// </summary>
    internal interface IWasmEmittable
    {
        int EmitToStream(Stream outputFileStream);
        int EncodeSize();
    }

    /// <summary>
    /// Interface for types that represent a WebAssembly section.
    /// </summary>
    internal interface IWasmSection : IWasmEmittable
    {
        WasmSectionType Type { get; }
    }

    /// <summary>
    /// A read-only view of a SectionWriter's Stream.
    /// The section should be written to via the SectionWriter, and SectionDataEmitter implementations
    /// handle copying the data to the output stream and adding any required headers and padding.
    /// </summary>
    internal abstract class SectionDataEmitter : IWasmEmittable
    {
        public SectionDataEmitter(Stream stream, Utf8String name, int sectionIndex)
        {
            ContentReadStream = stream;
            SectionName = name;
            SectionIndex = sectionIndex;
        }

        public int SectionIndex { get; }
        public Utf8String SectionName { get; }
        public Stream ContentReadStream { get; set; }

        public abstract int EmitToStream(Stream outputFileStream);
        public abstract int EncodeSize();
    }

    /// <summary>
    /// The base class for WebAssembly sections that are not composed of subsections.
    /// </summary>
    internal class WasmSection : SectionDataEmitter, IWasmSection
    {
        public WasmSectionType Type { get; }

        protected virtual int ContentPrefixSize => 0;

        protected virtual int EncodeContentPrefix(Span<byte> destination) => 0;

        public virtual int HeaderSize
        {
            get
            {
                uint sizeEncodeLength = DwarfHelper.SizeOfULEB128((ulong)ContentSize);
                return 1 + (int)sizeEncodeLength;
            }
        }

        public virtual int ContentSize => (int)ContentReadStream.Length + ContentPrefixSize;

        public override int EncodeSize()
        {
            return HeaderSize + ContentSize;
        }

        protected virtual int EncodeHeader(Span<byte> headerBuffer)
        {
            ulong contentSize = (ulong)ContentSize;
            uint encodeLength = DwarfHelper.SizeOfULEB128(contentSize);

            // Section header consists of:
            // 1 byte: section type
            // ULEB128: size of section
            headerBuffer[0] = (byte)Type;
            DwarfHelper.WriteULEB128(headerBuffer.Slice(1), contentSize);

            return 1 + (int)encodeLength;
        }

        public override int EmitToStream(Stream outputFileStream)
        {
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            EncodeHeader(headerBuffer);

            outputFileStream.Write(headerBuffer);

            if (ContentPrefixSize > 0)
            {
                Span<byte> contentPrefix = stackalloc byte[ContentPrefixSize];
                int encodedSize = EncodeContentPrefix(contentPrefix);
                Debug.Assert(encodedSize == contentPrefix.Length);
                outputFileStream.Write(contentPrefix);
            }

            ContentReadStream.Position = 0;
            ContentReadStream.CopyTo(outputFileStream);

            return HeaderSize + ContentSize;
        }

        public WasmSection(WasmSectionType type, Stream stream, Utf8String name, int sectionIndex) : base(stream, name, sectionIndex)
        {
            Type = type;
        }
    }

    internal abstract class WasmVectorSection : WasmSection
    {
        public int EntryCount { get; protected set; }

        protected override int ContentPrefixSize => (int)DwarfHelper.SizeOfULEB128((ulong)EntryCount);

        protected override int EncodeContentPrefix(Span<byte> destination) =>
            DwarfHelper.WriteULEB128(destination, (ulong)EntryCount);

        protected void CompleteEntry()
        {
            EntryCount++;
        }

        protected WasmVectorSection(WasmSectionType type, Stream stream, Utf8String name, int sectionIndex)
            : base(type, stream, name, sectionIndex)
        {
        }
    }

    // ObjectWriter writes directly to the section writer for the code and type sections without going through
    // the WasmSection abstraction.
    internal sealed class WasmExternallyCountedSection : WasmVectorSection
    {
        private bool _entryCountSet;

        public WasmExternallyCountedSection(WasmSectionType type, Stream stream, Utf8String name, int sectionIndex)
            : base(type, stream, name, sectionIndex)
        {
        }

        public override int EmitToStream(Stream outputFileStream)
        {
            Debug.Assert(_entryCountSet);
            return base.EmitToStream(outputFileStream);
        }

        public void SetEntryCount(int entryCount)
        {
            _entryCountSet = true;
            EntryCount = entryCount;
        }
    }

    internal abstract class WasmSection<TEntry> : WasmVectorSection
    {
        public void WriteEntry(SectionWriter writer, TEntry entry)
        {
            Debug.Assert(writer.SectionIndex == SectionIndex);
            WriteEntryCore(writer, entry);
            CompleteEntry();
        }

        protected abstract void WriteEntryCore(SectionWriter writer, TEntry entry);

        protected static void WriteEncodable<TEncodable>(SectionWriter writer, TEncodable entry)
            where TEncodable : IWasmEncodable
        {
            int encodeSize = entry.EncodeSize();
            int bytesWritten = entry.Encode(writer.Buffer.GetSpan(encodeSize));
            Debug.Assert(bytesWritten == encodeSize);
            writer.Buffer.Advance(bytesWritten);
        }

        protected WasmSection(WasmSectionType type, Stream stream, Utf8String name, int sectionIndex)
            : base(type, stream, name, sectionIndex)
        {
        }
    }

    internal sealed class WasmImportSection : WasmSection<WasmImport>
    {
        public WasmImportSection(Stream stream, Utf8String name, int sectionIndex)
            : base(WasmSectionType.Import, stream, name, sectionIndex)
        {
        }

        protected override void WriteEntryCore(SectionWriter writer, WasmImport entry)
        {
            writer.EmitSymbolDefinition(new Utf8String(entry.Name));
            writer.WriteUtf8WithLength(entry.Module);
            writer.WriteUtf8WithLength(entry.Name);
            writer.WriteByte((byte)entry.Kind);
            WriteEncodable(writer, entry);
        }
    }

    internal sealed class WasmFunctionSection : WasmSection<int>
    {
        public WasmFunctionSection(Stream stream, Utf8String name, int sectionIndex)
            : base(WasmSectionType.Function, stream, name, sectionIndex)
        {
        }

        protected override void WriteEntryCore(SectionWriter writer, int typeIndex)
        {
            writer.WriteULEB128((ulong)typeIndex);
        }
    }

    internal sealed class WasmGlobalSection : WasmSection<WasmGlobal>
    {
        public WasmGlobalSection(Stream stream, Utf8String name, int sectionIndex)
            : base(WasmSectionType.Global, stream, name, sectionIndex)
        {
        }

        protected override void WriteEntryCore(SectionWriter writer, WasmGlobal entry)
        {
            writer.EmitSymbolDefinition(new Utf8String(entry.Name));
            WriteEncodable(writer, entry);
        }
    }

    internal readonly struct WasmExport
    {
        public string Name { get; }
        public WasmExportKind Kind { get; }
        public int Index { get; }

        public WasmExport(string name, WasmExportKind kind, int index)
        {
            Name = name;
            Kind = kind;
            Index = index;
        }
    }

    internal sealed class WasmExportSection : WasmSection<WasmExport>
    {
        public WasmExportSection(Stream stream, Utf8String name, int sectionIndex)
            : base(WasmSectionType.Export, stream, name, sectionIndex)
        {
        }

        protected override void WriteEntryCore(SectionWriter writer, WasmExport entry)
        {
            writer.WriteUtf8WithLength(entry.Name);
            writer.WriteByte((byte)entry.Kind);
            writer.WriteULEB128((ulong)entry.Index);
        }
    }

    internal sealed class WasmElementSection : WasmSection<ReadOnlyMemory<int>>
    {
        public WasmElementSection(Stream stream, Utf8String name, int sectionIndex)
            : base(WasmSectionType.Element, stream, name, sectionIndex)
        {
        }

        protected override void WriteEntryCore(SectionWriter writer, ReadOnlyMemory<int> entry)
        {
            ReadOnlySpan<int> functionIndices = entry.Span;

            writer.WriteByte(1); // Passive element segment
            writer.WriteByte(0); // element type: ref func
            writer.WriteULEB128((ulong)functionIndices.Length);
            foreach (int functionIndex in functionIndices)
            {
                writer.WriteULEB128((ulong)functionIndex);
            }
        }
    }
}
