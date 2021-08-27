using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WebAssemblyInfo
{
    class WasmReader
    {
        BinaryReader reader;

        public WasmReader(string path)
        {
            if (Program.Verbose)
                Console.WriteLine($"Reading wasm file: {path}");

            var stream = File.Open(path, FileMode.Open);
            reader = new BinaryReader(stream);
        }

        public void Parse()
        {
            ReadModule();
        }

        void ReadModule()
        {
            byte[] magicWasm = { 0x0, 0x61, 0x73, 0x6d };
            var magicBytes = reader.ReadBytes(4);

            for (int i = 0; i < magicWasm.Length; i++)
            {
                if (magicWasm[i] != magicBytes[i])
                    throw new FileLoadException("not wasm file, module magic is wrong");
            }

            var version = reader.ReadUInt32();
            if (Program.Verbose)
                Console.WriteLine($"WebAssembly binary format version: {version}");

            while (reader.BaseStream.Position < reader.BaseStream.Length)
                ReadSection();
        }

        enum SectionId
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
            DataCount
        }

        void ReadSection()
        {
            SectionId id = (SectionId)reader.ReadByte();
            UInt32 size = ReadU32();

            if (Program.Verbose)
                Console.Write($"Section: {id,9} size: {size,12}");

            var begin = reader.BaseStream.Position;

            switch (id)
            {
                case SectionId.Custom:
                    ReadCustomSection();
                    break;
                case SectionId.Type:
                    ReadTypeSection();
                    break;
                case SectionId.Function:
                    ReadFunctionSection();
                    break;
                case SectionId.Export:
                    ReadExportSection();
                    break;
                case SectionId.Import:
                    ReadImportSection();
                    break;
                default:
                    break;
            }

            if (Program.Verbose)
                Console.WriteLine();

            reader.BaseStream.Seek(begin + size, SeekOrigin.Begin);
        }

        void ReadCustomSection()
        {
            if (Program.Verbose)
                Console.Write($" name: {ReadString()}");
        }

        enum ExportDesc : Byte
        {
            FuncIdx = 0,
            TableIdx,
            MemIdx,
            GlobalIdx
        }

        struct Export
        {
            public string Name;
            public UInt32 Idx;
            public ExportDesc Desc;

            public override string ToString()
            {
                return $"(export \"{Name}\" ({Desc} {Idx}))";
            }
        }

        Export[]? exports;

        void ReadExportSection()
        {
            UInt32 count = ReadU32();

            if (Program.Verbose)
                Console.Write($" count: {count}");

            if (Program.Verbose2)
                Console.WriteLine();

            exports = new Export[count];

            for (int i = 0; i < count; i++)
            {
                exports[i].Name = ReadString();
                exports[i].Desc = (ExportDesc)reader.ReadByte();
                exports[i].Idx = ReadU32();

                if (Program.Verbose2)
                    Console.WriteLine($"  {exports[i]}");
            }
        }

        enum ImportDesc : Byte
        {
            TypeIdx = 0,
            TableIdx,
            MemIdx,
            GlobalIdx
        }

        struct Import
        {
            public string Module;
            public string Name;
            public UInt32 Idx;
            public ImportDesc Desc;

            public override string ToString()
            {
                return $"(import \"{Module}\" \"{Name}\" ({Desc} {Idx}))";
            }
        }

        Import[]? imports;

        void ReadImportSection()
        {
            UInt32 count = ReadU32();

            if (Program.Verbose)
                Console.Write($" count: {count}");

            if (Program.Verbose2)
                Console.WriteLine();

            imports = new Import[count];

            for (int i = 0; i < count; i++)
            {
                imports[i].Module = ReadString();
                imports[i].Name = ReadString();
                imports[i].Desc = (ImportDesc)reader.ReadByte();
                imports[i].Idx = ReadU32();

                if (Program.Verbose2)
                    Console.WriteLine($"  {imports[i]}");
            }
        }

        string ReadString()
        {
            return Encoding.UTF8.GetString(reader.ReadBytes((int)ReadU32()));
        }

        struct Function
        {
            public UInt32 TypeIdx;
        }

        Function[]? functions;

        public enum NumberType : Byte
        {
            i32 = 0x7f,
            i64 = 0x7e,
            f32 = 0x7d,
            f64 = 0x7c
        }

        public enum ReferenceType : Byte
        {
            FuncRef = 0x70,
            ExternRef = 0x6f,
        }

        [StructLayout(LayoutKind.Explicit)]
        struct ValueType
        {
            [FieldOffset(0)]
            public byte value;
            [FieldOffset(0)]
            public NumberType Number;
            [FieldOffset(0)]
            public ReferenceType Reference;

            [FieldOffset(1)]
            public bool IsRefenceType;

            public override string ToString()
            {
                return IsRefenceType ? Reference.ToString() : Number.ToString();
            }
        }

        struct ResultType
        {
            public ValueType[] Types;

            public override string ToString()
            {
                StringBuilder sb = new();

                for (var i = 0; i < Types.Length; i++)
                {
                    if (i > 0)
                        sb.Append(' ');

                    sb.Append(Types[i].ToString());
                }

                return sb.ToString();
            }
        }

        struct FunctionType
        {
            public ResultType Parameters;
            public ResultType Results;

            public override string ToString()
            {
                var results = Results.Types.Length == 0 ? "" : $" (result {Results.ToString()})";
                return $"(func (param {Parameters}){results})";
            }
        }

        FunctionType[]? functionTypes;

        void ReadTypeSection()
        {
            UInt32 count = ReadU32();

            if (Program.Verbose)
                Console.Write($" count: {count}");

            if (Program.Verbose2)
                Console.WriteLine();

            functionTypes = new FunctionType[count];
            for (int i = 0; i < count; i++)
            {
                var b = reader.ReadByte();
                if (b != 0x60)
                    throw new FileLoadException("Expected 0x60 for function type");

                ReadResultTypes(ref functionTypes[i].Parameters);
                ReadResultTypes(ref functionTypes[i].Results);

                if (Program.Verbose2)
                    Console.WriteLine($"  Function type[{i}]: {functionTypes[i]}");
            }
        }

        void ReadResultTypes(ref ResultType type)
        {
            UInt32 count = ReadU32();

            type.Types = new ValueType[count];

            for (int i = 0; i < count; i++)
            {
                var b = reader.ReadByte();

                type.Types[i].IsRefenceType = b <= 0x70;
                type.Types[i].value = b;
            }
        }

        void ReadFunctionSection()
        {
            UInt32 count = ReadU32();

            if (Program.Verbose)
                Console.Write($" count: {count}");

            functions = new Function[count];

            for (int i = 0; i < count; i++)
            {
                functions[i].TypeIdx = ReadU32();
            }
        }

        UInt32 ReadU32()
        {
            UInt32 value = 0;
            var offset = 0;
            do
            {
                var b = reader.ReadByte();
                value |= (UInt32)(b & 0x7f) << offset;

                if ((b & 0x80) == 0)
                    break;

                offset += 7;
            } while (true);

            return value;
        }
    }
}
