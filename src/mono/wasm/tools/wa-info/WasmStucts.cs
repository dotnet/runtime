using System;
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
    }

    struct Code
    {
        public LocalsBlock[] Locals;
        public Instruction[] Instructions;
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

        public override string ToString()
        {
            var results = Results.Types.Length == 0 ? "" : $" (result {Results.ToString()})";
            return $"(func (param {Parameters}){results})";
        }
    }
}
