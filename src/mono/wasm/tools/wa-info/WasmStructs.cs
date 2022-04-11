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

        public bool TryDelegate;

        public UInt32 Idx;
        public UInt32 Idx2;
        public Int32 I32;
        public Int64 I64;
        public Single F32;
        public Double F64;

        public MemArg MemArg;

        public UInt32[] IdxArray;

        public long Offset;

        public SIMDOpcode SIMDOpcode;
        public byte[] SIMDImmByteArray;
        public byte[] SIMDImmLaneIdxArray;
        public byte SIMDImmLaneIdx;

        public string ToString(WasmReader? reader)
        {
            var prefix = Program.PrintOffsets ? $"0x{Offset:x8}: " : null;
            var opStr = prefix + Opcode.ToString().ToLower().Replace("_", ".");
            switch (Opcode)
            {
                case Opcode.Block:
                case Opcode.Loop:
                case Opcode.If:
                case Opcode.Try:
                    var str = $"{opStr}\n{BlockToString(Block, reader)}";
                    str += ((Block2 == null || Block2.Length < 1) ? "" : $"else\n{BlockToString(Block2, reader)}");
                    if (Opcode == Opcode.Try && TryDelegate)
                        str += $"\n{prefix}{Opcode.Delegate.ToString().ToLower().Replace("_", ".")} {Idx}";

                    return str;
                case Opcode.Local_Get:
                case Opcode.Local_Set:
                case Opcode.Local_Tee:
                case Opcode.Rethrow:
                    return $"{opStr} ${Idx}";
                case Opcode.Global_Get:
                case Opcode.Global_Set:
                    return $"{opStr} {GlobalName(Idx, reader)}";
                case Opcode.Call:
                    return $"{opStr} {FunctionName(Idx, reader)}";
                case Opcode.Call_Indirect:
                    var table = Idx2 == 0 ? "" : $" table:{Idx2}";
                    return $"{opStr} {FunctionType(Idx, reader)}{table}";
                case Opcode.Catch:
                case Opcode.Throw:
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
                case Opcode.SIMDPrefix:
                    opStr = prefix + SIMDOpcode.ToString().ToLower().Replace("_", ".");
                    offset = MemArg.Offset != 0 ? $" offset:{MemArg.Offset}" : null;
                    align = MemArg.Align != 0 ? $" align:{MemArg.Align}" : null;
                    string? optional = null;

                    switch(SIMDOpcode)
                    {
                        case SIMDOpcode.V128_Const:
                            var sb = new StringBuilder(" 0x");
                            for (var i = 15; i >= 0; i--)
                                sb.Append($"{SIMDImmByteArray[i]:x2}");
                            optional = sb.ToString();
                            break;
                    }

                    return $"{opStr}{offset}{align}{optional}    [SIMD]";
                case Opcode.Nop:
                default:
                    return opStr;
            }
        }

        static string FunctionName(UInt32 idx, WasmReader? reader)
        {
            if (reader == null)
                return $"[{idx.ToString()}]";

            return reader.GetFunctionName(idx, false);
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

    struct TableType
    {
        public ReferenceType RefType;
        public UInt32 Min;
        public UInt32 Max;
    }

    struct Element
    {
        public ElementFlag Flags;
        public UInt32 TableIdx;
        public ReferenceType RefType;
        public byte Kind;
        public UInt32[] Indices;
        public Instruction[] Expression;
        public Instruction[][] Expressions;

        public bool HasTableIdx
        {
            get
            {
                return (Flags & ElementFlag.ExplicitIndex) == ElementFlag.ExplicitIndex && (Flags & ElementFlag.PassiveOrDeclarative) != ElementFlag.PassiveOrDeclarative;
            }
        }

        public bool HasExpression
        {
            get
            {
                return ((Flags & ElementFlag.PassiveOrDeclarative) != ElementFlag.PassiveOrDeclarative);
            }
        }

        public bool HasExpressions
        {
            get
            {
                return (Flags & ElementFlag.TypeAndExpressions) == ElementFlag.TypeAndExpressions;
            }
        }

        public bool HasRefType
        {
            get
            {
                return ((Flags & ElementFlag.PassiveOrDeclarative) == ElementFlag.PassiveOrDeclarative || (Flags & ElementFlag.ExplicitIndex) == ElementFlag.ExplicitIndex
                    && (Flags & ElementFlag.TypeAndExpressions) == ElementFlag.TypeAndExpressions);
            }
        }

        public bool HasElemKind
        {
            get
            {
                return ((Flags & ElementFlag.PassiveOrDeclarative) == ElementFlag.PassiveOrDeclarative || (Flags & ElementFlag.ExplicitIndex) == ElementFlag.ExplicitIndex
                    && (Flags & ElementFlag.TypeAndExpressions) != ElementFlag.TypeAndExpressions);
            }
        }
    }

    [Flags]
    enum ElementFlag
    {
        PassiveOrDeclarative = 1,
        ExplicitIndex = 2,
        TypeAndExpressions = 4
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

        public string ToString(int idx)
        {
            string varName = idx == -1 ? null : $" ${idx}";
            string count = Count == 1 ? null : $" {Count}";
            return $"local{varName}{count} {Type}";
        }

        public override string ToString()
        {
            return ToString(-1);
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

                // Console.WriteLine($"    locals {j} count: {Locals[j].Count} type: {Locals[j].Type}");
            }

            // read expr
            (Instructions, _) = reader.ReadBlock();

            if (Program.Verbose2)
                Console.WriteLine(ToString().Indent("    "));
        }

        public bool EnsureCodeReaded(WasmReader? reader)
        {
            if (Instructions == null)
            {
                if (reader == null)
                    return false;

                ReadCode(reader);
            }

            return true;
        }

        public string ToString(WasmReader? reader, int startIdx)
        {
            EnsureCodeReaded(reader);

            StringBuilder sb = new();

            foreach (LocalsBlock lb in Locals)
                sb.AppendLine(lb.ToString(startIdx++).Indent(" "));

            foreach (var instruction in Instructions)
                sb.AppendLine(instruction.ToString(reader).Indent(" "));

            return sb.ToString();
        }

        public override string ToString()
        {
            return ToString(null, 0);
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
    struct ValueType : IComparable<ValueType>
    {
        [FieldOffset(0)]
        public byte value;
        [FieldOffset(0)]
        public NumberType Number;
        [FieldOffset(0)]
        public ReferenceType Reference;

        [FieldOffset(1)]
        public bool IsRefenceType;

        [FieldOffset(2)]
        public bool IsVectorType;

        public int CompareTo(ValueType other)
        {
            return IsRefenceType ? Reference.CompareTo(other.Reference) : Number.CompareTo(other.Number);
        }

        public override string ToString()
        {
            if (IsRefenceType)
                return Reference.ToString();

            if (IsVectorType)
                return "v128";

            return Number.ToString();
        }
    }

    struct ResultType : IComparable<ResultType>
    {
        public ValueType[] Types;

        public int CompareTo(ResultType other)
        {
            if (Types.Length != other.Types.Length)
                return Types.Length - other.Types.Length;

            for (int i = 0; i < Types.Length; i++)
            {
                int cmp = Types[i].CompareTo(other.Types[i]);
                if (cmp != 0)
                    return cmp;
            }

            return 0;
        }

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

    struct FunctionType : IComparable<FunctionType>
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

        public int CompareTo(FunctionType other)
        {
            var cmpParameters = Parameters.CompareTo(other.Parameters);
            var cmpResults = Results.CompareTo(other.Results);

            if (cmpParameters == 0 && cmpResults == 0)
                return 0;

            return cmpParameters != 0 ? cmpParameters : cmpResults;
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
