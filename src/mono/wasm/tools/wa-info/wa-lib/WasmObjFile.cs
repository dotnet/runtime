using System.Text;

namespace WebAssemblyInfo
{
    public partial class WasmReader : WasmReaderBase
    {
        enum LinkingSubsectionType
        {
            WasmSegmentInfo = 5,
            WasmInitFunctions = 6,
            WasmComdatInfo = 7,
            WasmSymbolTable = 8,
        };

        enum SymbolKind
        {
            Function = 0,
            Data = 1,
            Global = 2,
            Section = 3,
            Event = 4,
            Table = 5,
        }

        enum SymbolFlags : UInt32
        {
            Weak = 1,
            Local = 2,
            Hidden = 4,
            Undefined = 0x10,
            Exported = 0x20,
            ExplicitName = 0x40,
            NoStrip = 0x80,
            TLS = 0x100,
            Absolute = 0x200,
        }

        void ReadCustomLinkingSection(UInt32 size)
        {
            var start = Reader.BaseStream.Position;
            var version = ReadU32();
            if (version != 2)
            {
                if (Context.Verbose)
                    Console.WriteLine($"Custom linking section version {version} is not supported");
                return;
            }

            while (Reader.BaseStream.Position - start < size)
            {
                var subsectionType = (LinkingSubsectionType)Reader.ReadByte();
                var len = ReadU32();
                if (Context.Verbose) {
                    Console.WriteLine();
                    Console.WriteLine($"Reading custom linking sub section type: {subsectionType} length: {len}");
                }

                switch (subsectionType)
                {
                    case LinkingSubsectionType.WasmSegmentInfo:
                        var count = ReadU32();
                        for (var i = 0; i < count; i++)
                        {
                            var name = ReadString();
                            var alignment = ReadU32();
                            var flags = ReadU32();
                            if (Context.Verbose)
                                Console.WriteLine($"Segment {i} name: {name} alignment: {alignment} flags: {flags}");
                        }
                        break;
                    case LinkingSubsectionType.WasmInitFunctions:
                        count = ReadU32();
                        for (var i = 0; i < count; i++)
                        {
                            var priority = ReadU32();
                            var symbolIndex = ReadU32();
                            if (Context.Verbose)
                                Console.WriteLine($"Init function {i} priority: {priority} symbol index: {symbolIndex}");
                        }
                        break;
                    case LinkingSubsectionType.WasmComdatInfo:
                        count = ReadU32();
                        for (var i = 0; i < count; i++)
                        {
                            var name = ReadString();
                            var flags = ReadU32();
                            if (Context.Verbose)
                                Console.WriteLine($"COMDAT {i} name: {name} flags: {flags}");
                            var symbolsCount = ReadU32();
                            for (var j = 0; j < symbolsCount; j++)
                            {
                                var kind = (SymbolKind)Reader.ReadByte();
                                var symbolIndex = ReadU32();
                                if (Context.Verbose)
                                    Console.WriteLine($"  Symbol {j} kind: {kind} index: {symbolIndex}");
                            }
                        }
                        break;
                    case LinkingSubsectionType.WasmSymbolTable:
                        LinkingReadSymbolTable();
                        break;
                    default:
                        if (Context.Verbose)
                            Console.WriteLine($"Unknown custom linking sub section type: {subsectionType}");
                        Reader.BaseStream.Seek(len, SeekOrigin.Current);
                        break;
                }
            }
        }

        enum RelocationType : byte
        {
            FunctionIndexLEB = 0,
            TableIndexSLEB = 1,
            TableIndexI32 = 2,
            MemoryAddressLEB = 3,
            MemoryAddressSLEB = 4,
            MemoryAddressI32 = 5,
            TypeIndexLEB = 6,
            GlobalIndexLEB = 7,
            FunctionOffsetI32 = 8,
            SectionOffsetI32 = 9,
            EventIndexLEB = 10,
            GlobalIndexI32 = 13,
            MemoryAddressLEB64 = 14,
            MemoryAddressSLEB64 = 15,
            MemoryAddressI64 = 16,
            TableIndexSLEB64 = 18,
            TableIndexI64 = 19,
            TableNumberLEB = 20,
            FunctionOffsetI64 = 22,
            MemoryAddressLocRelI32 = 23,
            TableIndexRelSLEB = 24,
            FunctionIndexI32 = 26,
        };

        void ReadCustomRelocSection(UInt32 size, string name)
        {
            if (Context.Verbose) {
                Console.WriteLine();
                Console.WriteLine($"Reading custom reloc section: {name}");
            }

            var start = Reader.BaseStream.Position;
            var sectionIndex = ReadU32();
            var count = ReadU32();
            if (Context.Verbose2)
                Console.WriteLine($"Section index: {sectionIndex} count: {count}");

            for (var i = 0; i < count; i++) {
                var type = (RelocationType)Reader.ReadByte();
                var offset = ReadU32();
                var index = ReadU32();
                int addend = 0;
                switch(type) {
                    case RelocationType.MemoryAddressLEB:
                    case RelocationType.MemoryAddressSLEB:
                    case RelocationType.MemoryAddressI32:
                    case RelocationType.MemoryAddressLEB64:
                    case RelocationType.MemoryAddressSLEB64:
                    case RelocationType.MemoryAddressI64:
                    case RelocationType.MemoryAddressLocRelI32:
                    case RelocationType.FunctionOffsetI32:
                    case RelocationType.FunctionOffsetI64:
                    case RelocationType.SectionOffsetI32:
                        addend = (int)ReadI32();
                        break;
                }
                if (Context.Verbose2) {
                    var addendStr = addend != 0 ? $" addend: {addend}" : "";
                    Console.WriteLine($"  type: {type} offset: {offset} index: {index}{addendStr}");
                }
            }
        }

        private void LinkingReadSymbolTable()
        {
            var count = ReadU32();
            if (Context.Verbose)
                Console.WriteLine($"Symbol table count: {count}");
            for (var i = 0; i < count; i++)
            {
                var kind = (SymbolKind)Reader.ReadByte();
                var flags = ReadU32();
                switch (kind)
                {
                    case SymbolKind.Function:
                    case SymbolKind.Global:
                    case SymbolKind.Event:
                    case SymbolKind.Table:
                        var index = ReadU32();
                        string? name = null;
                        if ((flags & (UInt32)SymbolFlags.Undefined) != 0 && (flags & (UInt32)SymbolFlags.ExplicitName) == 0)
                        {
                            if (imports != null)
                                name = imports[index].Name;
                        }
                        else
                        {
                            name = ReadString();
                        }
                        if (Context.Verbose)
                            Console.WriteLine($"Symbol {i} kind: {kind} flags: {SymbolFlagsToString(flags)} index: {index} name: {name}");
                        break;
                    case SymbolKind.Data:
                        name = ReadString();
                        if (Context.Verbose)
                            Console.WriteLine($"Symbol {i} kind: {kind} flags: {SymbolFlagsToString(flags)} name: {name}");

                        if ((flags & (UInt32)SymbolFlags.Undefined) == 0)
                        {
                            index = ReadU32();
                            var offset = ReadU32();
                            var dataSize = ReadU32();
                            if (Context.Verbose)
                                Console.WriteLine($"  index: {index} offset: {offset} dataSize: {dataSize}");
                        }
                        break;
                    case SymbolKind.Section:
                        var sectionIndex = ReadU32();
                        if (Context.Verbose)
                            Console.WriteLine($"Symbol {i} kind: {kind} section index: {sectionIndex}");
                        break;
                    default:
                        if (Context.Verbose)
                            Console.WriteLine($"Unknown symbol kind: {kind}");
                        break;
                }
            }
        }

        string SymbolFlagsToString(UInt32 flags)
        {
            var sb = new StringBuilder();
            foreach (var flag in Enum.GetValues(typeof(SymbolFlags)))
            {
                if ((flags & (UInt32)flag) != 0)
                {
                    var prefix = sb.Length == 0 ? "" : ", ";
                    sb.Append($"{prefix}{flag}");
                }
            }

            return sb.ToString();
        }
    }
}
