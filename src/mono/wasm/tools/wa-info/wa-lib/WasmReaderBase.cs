// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using NameMap = System.Collections.Generic.Dictionary<uint, string>;

namespace WebAssemblyInfo
{
    public abstract class WasmReaderBase
    {
        public WasmContext Context;

        public readonly BinaryReader Reader;
        public uint Version { get; private set; }
        public string Path { get; private set; }

        protected readonly long Length;
        protected readonly Stack<long> EndOfModulePositions = new();

        public WasmReaderBase(WasmContext context, string path)
        {
            Context = context;

            if (Context.Verbose)
                Console.WriteLine($"Reading wasm file: {path}");

            Path = path;
            var stream = File.Open(Path, FileMode.Open);
            Length = stream.Length;
            Reader = new BinaryReader(stream);
            EndOfModulePositions.Push(Length);
        }

        public WasmReaderBase(WasmContext context, Stream stream, long len)
        {
            Context = context;
            Length =  len;
            Path = "[embedded]";
            Reader = new BinaryReader(stream);
            EndOfModulePositions.Push(stream.Position + len);
        }

        public void Parse()
        {
            ReadModule();
            PostParse();
        }

        protected virtual void PostParse() { }

        protected byte[] MagicWasm = { 0x0, 0x61, 0x73, 0x6d };

        protected bool InWitComponent;

        protected virtual void ReadModule()
        {
            var magicBytes = Reader.ReadBytes(4);

            for (int i = 0; i < MagicWasm.Length; i++)
            {
                if (MagicWasm[i] != magicBytes[i])
                    throw new FileLoadException("not wasm file, module magic is wrong");
            }

            Version = Reader.ReadUInt32();
            if (Context.Verbose)
                Console.WriteLine($"WebAssembly binary format version: 0x{Version:x8}");

            switch (Version)
            {
                case 0x1000d:
                    InWitComponent = true;
                    goto case 1;
                case 1:
                    ReadWasmModule();
                    break;
                default:
                    throw new FileLoadException($"Unsupported WebAssembly binary format version: 0x{Version:x8}");
            }
        }

        private void ReadWasmModule()
        {
            while (Reader.BaseStream.Position < EndOfModulePositions.Peek())
                ReadSection();
        }

        protected enum SectionId
        {
            Custom = 0,
            Type,
            Import,
            Function,
            Table,
            Memory,
            Global,
            Export,
            Start,
            Element,
            Code,
            Data,
            DataCount,
            Tag,
            // Wit Component sections
            WitCoreCustom = 0x1000,
            WitCoreModule,
            WitCoreInstance,
            WitCoreType,
            WitComponent,
            WitInstance,
            WitAlias,
            WitType,
            WitCanon,
            WitStart,
            WitImport,
            WitExport,
            WitValue,
        }

        protected struct SectionInfo
        {
            public SectionId id;
            public uint size;
            public long offset;
            public long begin;
        }
        protected List<SectionInfo> sections = new();
        protected Dictionary<SectionId, List<SectionInfo>> sectionsById = new();

        protected abstract void ReadSection(SectionInfo section);

        private SectionId ReadSectionId()
        {
            var id = (int)Reader.ReadByte();
            if (InWitComponent)
                id += 0x1000;

            return (SectionId)id;
        }

        private void ReadSection()
        {
            var section = new SectionInfo() { offset=Reader.BaseStream.Position, id = ReadSectionId(), size = ReadU32(), begin = Reader.BaseStream.Position };
            sections.Add(section);
            if (!sectionsById.ContainsKey(section.id))
                sectionsById[section.id] = new List<SectionInfo>();

            sectionsById[section.id].Add(section);

            if (Context.Verbose)
                Console.Write($"Reading section: {section.id,9} size: {section.size,12}");

            ReadSection(section);

            if (Context.Verbose)
                Console.WriteLine();

            Reader.BaseStream.Seek(section.begin + section.size, SeekOrigin.Begin);
        }

        public uint ReadU32()
        {
            uint value = 0;
            var offset = 0;
            do
            {
                var b = Reader.ReadByte();
                value |= (uint)(b & 0x7f) << offset;

                if ((b & 0x80) == 0)
                    break;

                offset += 7;
            } while (true);

            return value;
        }

        protected int ReadI32()
        {
            int value = 0;
            var offset = 0;
            byte b;

            do
            {
                b = Reader.ReadByte();
                value |= (int)(b & 0x7f) << offset;

                if ((b & 0x80) == 0)
                    break;

                offset += 7;
            } while (true);

            if (offset < 32 && (b & 0x40) == 0x40)
                value |= (~(int)0 << offset);

            return value;
        }

        protected long ReadI64()
        {
            long value = 0;
            var offset = 0;
            byte b;

            do
            {
                b = Reader.ReadByte();
                value |= (long)(b & 0x7f) << offset;

                if ((b & 0x80) == 0)
                    break;

                offset += 7;
            } while (true);

            if (offset < 64 && (b & 0x40) == 0x40)
                value |= (~(long)0 << offset);

            return value;
        }
    }
}
