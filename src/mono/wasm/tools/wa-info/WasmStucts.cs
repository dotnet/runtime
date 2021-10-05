using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace WebAssemblyInfo
{
    struct Instruction
    {
        public Opcode Opcode;

        public BlockType BlockType;

        public Instruction[] Block;
        public Instruction[] Block2;

        public UInt32 Idx;
        public UInt32 Idx2;
        public Int32 I32;
        public Int64 I64;
        public Single F32;
        public Double F64;

        public MemArg MemArg;

        public UInt32[] IdxArray;

        public string ToString(WasmReader? reader)
        {
            var opStr = Opcode.ToString().ToLower().Replace("_", ".");
            switch (Opcode)
            {
                case Opcode.Block:
                case Opcode.Loop:
                case Opcode.If:
                    var str = $"{opStr}\n{BlockToString(Block, reader)}";
                    return str + ((Block2 == null || Block2.Length < 1) ? "" : $"else\n{BlockToString(Block2, reader)}");
                case Opcode.Local_Get:
                case Opcode.Local_Set:
                case Opcode.Local_Tee:
                    return $"{opStr} ${Idx}";
                case Opcode.Global_Get:
                case Opcode.Global_Set:
                    return $"{opStr} {GlobalName(Idx, reader)}";
                case Opcode.Call:
                    return $"{opStr} {FunctionName(Idx, reader)}";
                case Opcode.Call_Indirect:
                    var table = Idx2 == 0 ? "" : $" table:{Idx2}";
                    return $"{opStr} {FunctionType(Idx, reader)}{table}";
                case Opcode.I32_Const:
                    return $"{opStr} {I32}";
                case Opcode.I64_Const:
                    return $"{opStr} {I64}";
                case Opcode.F32_Const:
                    return $"{opStr} {F32}";
                case Opcode.F64_Const:
                    return $"{opStr} {F64}";
                case Opcode.I32_Load:
                case Opcode.I64_Load:
                case Opcode.F32_Load:
                case Opcode.F64_Load:
                case Opcode.I32_Load8_S:
                case Opcode.I32_Load8_U:
                case Opcode.I32_Load16_S:
                case Opcode.I32_Load16_U:
                case Opcode.I64_Load8_S:
                case Opcode.I64_Load8_U:
                case Opcode.I64_Load16_S:
                case Opcode.I64_Load16_U:
                case Opcode.I64_Load32_S:
                case Opcode.I64_Load32_U:
                case Opcode.I32_Store:
                case Opcode.I64_Store:
                case Opcode.F32_Store:
                case Opcode.F64_Store:
                case Opcode.I32_Store8:
                case Opcode.I32_Store16:
                case Opcode.I64_Store8:
                case Opcode.I64_Store16:
                case Opcode.I64_Store32:
                    var offset = MemArg.Offset != 0 ? $" offset:{MemArg.Offset}" : null;
                    var align = MemArg.Align != 0 ? $" align:{MemArg.Align}" : null;

                    return $"{opStr}{offset}{align}";
                case Opcode.Nop:
                default:
                    return opStr;
            }
        }

        static string FunctionName(UInt32 idx, WasmReader? reader)
        {
            if (reader == null)
                return $"[{idx.ToString()}]";

            return reader.FunctionName(idx);
        }

        static string FunctionType(UInt32 idx, WasmReader? reader)
        {
            if (reader == null)
                return $"[{idx.ToString()}]";

            return reader.FunctionType(idx);
        }

        static string GlobalName(UInt32 idx, WasmReader? reader)
        {
            if (reader == null)
                return $"${idx.ToString()}";

            return $"${reader.GlobalName(idx)}";
        }

        public override string ToString()
        {
            return ToString(null);
        }

        static string BlockToString(Instruction[] instructions, WasmReader? reader)
        {
            if (instructions == null || instructions.Length < 1)
                return "";

            StringBuilder sb = new();

            foreach (var instruction in instructions)
                sb.AppendLine(instruction.ToString(reader).Indent(" "));

            return sb.ToString();
        }
    }

    struct MemArg
    {
        public UInt32 Align;
        public UInt32 Offset;
    }

    struct LocalsBlock
    {
        public UInt32 Count;
        public ValueType Type;

        public override string ToString()
        {
            return $"local {Count} {Type}";
        }
    }

    struct Code
    {
        public LocalsBlock[] Locals;
        public Instruction[] Instructions;
        public UInt32 Idx;
        public UInt32 Size;
        public long Offset;

        void ReadCode(WasmReader reader)
        {
            reader.Reader.BaseStream.Seek(Offset, SeekOrigin.Begin);

            if (Program.Verbose2)
                Console.WriteLine($"  code[{Idx}]: {Size} bytes");

            var vecSize = reader.ReadU32();
            Locals = new LocalsBlock[vecSize];

            if (Program.Verbose2)
                Console.WriteLine($"    locals blocks count {vecSize}");

            for (var j = 0; j < vecSize; j++)
            {
                Locals[j].Count = reader.ReadU32();
                reader.ReadValueType(ref Locals[j].Type);

                //Console.WriteLine($"    locals count: {funcsCode[i].Locals[j].Count} type: {funcsCode[i].Locals[j].Type}");
            }

            // read expr
            (Instructions, _) = reader.ReadBlock();

            if (Program.Verbose2)
                Console.WriteLine(ToString().Indent("    "));
        }

        public bool EnsureCodeReaded (WasmReader? reader)
        {
            if (Instructions == null)
            {
                if (reader == null)
                    return false;

                ReadCode(reader);
            }

            return true;
        }

        public string ToString(WasmReader? reader)
        {
            EnsureCodeReaded(reader);

            StringBuilder sb = new();

            foreach (LocalsBlock lb in Locals)
                sb.AppendLine(lb.ToString().Indent(" "));

            foreach (var instruction in Instructions)
                sb.AppendLine(instruction.ToString(reader).Indent(" "));

            return sb.ToString();
        }

        public override string ToString()
        {
            return ToString(null);
        }
    }

    enum BlockTypeKind
    {
        Empty,
        ValueType,
        TypeIdx
    }

    struct BlockType
    {
        public BlockTypeKind Kind;
        public ValueType ValueType;
        public UInt32 TypeIdx;
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

    struct Function
    {
        public UInt32 TypeIdx;
    }

    enum NumberType : Byte
    {
        i32 = 0x7f,
        i64 = 0x7e,
        f32 = 0x7d,
        f64 = 0x7c
    }

    enum ReferenceType : Byte
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

        public string ToString(string? name)
        {
            var results = Results.Types.Length == 0 ? "" : $" (result {Results})";
            var parameters = Parameters.Types.Length == 0 ? "" : $"(param { Parameters})";
            return $"(func {name}{parameters}{results})";
        }

        public override string ToString()
        {
            return ToString(null);
        }
    }

    enum CustomSubSectionId
    {
        ModuleName = 0,
        FunctionNames = 1,
        LocalNames = 2,
        // extended names
        LabelNames = 3,
        TypeNames = 4,
        TableNames = 5,
        MemoryNames = 6,
        GlobalNames = 7,
        ElemSegmentNames = 8,
        DataSegmentNames = 9,
    }
}
