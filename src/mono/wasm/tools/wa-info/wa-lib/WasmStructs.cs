﻿using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace WebAssemblyInfo
{
    public struct Instruction
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

        public PrefixOpcode PrefixOpcode;
        public SIMDOpcode SIMDOpcode;
        public MTOpcode MTOpcode;
        public byte[] SIMDImmByteArray;
        public byte SIMDImmLaneIdx;

        public string ToString(WasmReader? reader)
        {
            var prefix = reader != null && reader.Context.PrintOffsets ? $"0x{Offset:x8}: " : null;
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
                    return opStr + (reader != null && reader.Context.ShowConstLoad ? $" {I32}" : "");
                case Opcode.I64_Const:
                    return opStr + (reader != null && reader.Context.ShowConstLoad ? $" {I64}" : "");
                case Opcode.F32_Const:
                    return opStr + (reader != null && reader.Context.ShowConstLoad ? $" {F32}" : "");
                case Opcode.F64_Const:
                    return opStr + (reader != null && reader.Context.ShowConstLoad ? $" {F64}" : "");
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

                    switch (SIMDOpcode)
                    {
                        case SIMDOpcode.V128_Const:
                        case SIMDOpcode.I8x16_Shuffle:
                            var sb = new StringBuilder(" 0x");
                            for (var i = 15; i >= 0; i--)
                                sb.Append($"{SIMDImmByteArray[i]:x2}");
                            optional = sb.ToString();
                            break;
                        case SIMDOpcode.V128_Load8_Lane:
                        case SIMDOpcode.V128_Load16_Lane:
                        case SIMDOpcode.V128_Load32_Lane:
                        case SIMDOpcode.V128_Load64_Lane:
                        case SIMDOpcode.V128_Store8_Lane:
                        case SIMDOpcode.V128_Store16_Lane:
                        case SIMDOpcode.V128_Store32_Lane:
                        case SIMDOpcode.V128_Store64_Lane:
                        case SIMDOpcode.I8x16_Extract_Lane_S:
                        case SIMDOpcode.I8x16_Extract_Lane_U:
                        case SIMDOpcode.I8x16_Replace_Lane:
                        case SIMDOpcode.I16x8_Extract_Lane_S:
                        case SIMDOpcode.I16x8_Extract_Lane_U:
                        case SIMDOpcode.I16x8_Replace_Lane:
                        case SIMDOpcode.I32x4_Extract_Lane:
                        case SIMDOpcode.I32x4_Replace_Lane:
                        case SIMDOpcode.I64x2_Extract_Lane:
                        case SIMDOpcode.I64x2_Replace_Lane:
                        case SIMDOpcode.F32x4_Extract_Lane:
                        case SIMDOpcode.F32x4_Replace_Lane:
                        case SIMDOpcode.F64x2_Extract_Lane:
                        case SIMDOpcode.F64x2_Replace_Lane:
                            optional = $" {SIMDImmLaneIdx}";
                            break;
                    }

                    return $"{opStr}{offset}{align}{optional}    [SIMD]";
                case Opcode.MTPrefix:
                    opStr = prefix + MTOpcode.ToString().ToLower().Replace("_", ".");
                    offset = MemArg.Offset != 0 ? $" offset:{MemArg.Offset}" : null;
                    align = MemArg.Align != 0 ? $" align:{MemArg.Align}" : null;

                    return $"{opStr}{offset}{align}    [MT]";
                case Opcode.Prefix:
                    opStr = prefix + PrefixOpcode.ToString().ToLower().Replace("_", ".");

                    return $"{opStr}    [PF]";
                case Opcode.Nop:
                default:
                    return opStr;
            }
        }

        static string? FunctionName(UInt32 idx, WasmReader? reader)
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

    public struct TableType
    {
        public ReferenceType RefType;
        public UInt32 Min;
        public UInt32 Max;
    }

    public struct Element
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
    public enum ElementFlag
    {
        PassiveOrDeclarative = 1,
        ExplicitIndex = 2,
        TypeAndExpressions = 4
    }

    public struct Data
    {
        public DataMode Mode;
        public Instruction[] Expression;
        public UInt32 MemIdx;
        public byte[] Content;
    }

    public struct Global
    {
        public ValueType Type;
        public Mutability Mutability;
        public Instruction[] Expression;
    }

    public struct Memory
    {
        public UInt32 Min;
        public UInt32 Max;
    }

    public enum Mutability
    {
        Const = 0,
        Var = 1,
    }

    public struct MemArg
    {
        public UInt32 Align;
        public UInt32 Offset;
    }

    public struct LocalsBlock
    {
        public UInt32 Count;
        public ValueType Type;

        public string ToString(int idx)
        {
            string? varName = idx == -1 ? null : $" ${idx}";
            string? count = Count == 1 ? null : $" {Count}";
            return $"local{varName}{count} {Type}";
        }

        public override string ToString()
        {
            return ToString(-1);
        }
    }

    public struct Code
    {
        public LocalsBlock[] Locals;
        public Instruction[] Instructions;
        public UInt32 Idx;
        public UInt32 Size;
        public long Offset;

        void ReadCode(WasmReader reader)
        {
            reader.Reader.BaseStream.Seek(Offset, SeekOrigin.Begin);

            if (reader.Context.Verbose2)
                Console.WriteLine($"  code[{Idx}]: {Size} bytes");

            var vecSize = reader.ReadU32();
            Locals = new LocalsBlock[vecSize];

            if (reader.Context.Verbose2)
                Console.WriteLine($"    locals blocks count {vecSize}");

            for (var j = 0; j < vecSize; j++)
            {
                Locals[j].Count = reader.ReadU32();
                reader.ReadValueType(ref Locals[j].Type);

                // Console.WriteLine($"    locals {j} count: {Locals[j].Count} type: {Locals[j].Type}");
            }

            // read expr
            (Instructions, _) = reader.ReadBlock();

            if (reader.Context.Verbose2)
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

    public enum BlockTypeKind
    {
        Empty,
        ValueType,
        TypeIdx
    }

    public struct BlockType
    {
        public BlockTypeKind Kind;
        public ValueType ValueType;
        public UInt32 TypeIdx;
    }

    public enum ExportDesc : Byte
    {
        FuncIdx = 0,
        TableIdx,
        MemIdx,
        GlobalIdx
    }

    public struct Export
    {
        public string Name;
        public UInt32 Idx;
        public ExportDesc Desc;

        public override string ToString()
        {
            return $"(export \"{Name}\" ({Desc} {Idx}))";
        }
    }

    public enum ImportDesc : Byte
    {
        TypeIdx = 0,
        TableIdx,
        MemIdx,
        GlobalIdx
    }

    public struct Import
    {
        public string Module;
        public string Name;
        public UInt32 Idx;
        public UInt32 Min;
        public UInt32 Max;
        public ImportDesc Desc;

        public override string ToString()
        {
            return $"(import \"{Module}\" \"{Name}\" ({Desc} {Idx}))";
        }
    }

    public struct Function
    {
        public UInt32 TypeIdx;
    }

    public enum WitExternDescriptionKind : Byte
    {
        CoreModule = 0,
        Function,
        Value,
        Type,
        Component,
        Instance,
    }

    public enum WitTypeBound : byte {
        Eq = 0,
        Sub,
    }

    public enum WitValueBound : byte {
        Eq = 0,
        Type,
    }

    public enum WitPrimaryValueType : byte {
        String = 0x73,
        Char,
        F64,
        F32,
        U64,
        S64,
        U32,
        S32,
        U16,
        S16,
        U8,
        S8,
        Bool,
    }

    public enum WitValueTypeKind {
        Type,
        PrimaryValueType,
    }

    public struct WitValueType {
        public WitValueTypeKind Kind;
        public UInt32 TypeIdx;
        public WitPrimaryValueType PrimaryValueType;

        override public string ToString()
        {
            return Kind == WitValueTypeKind.Type ? $"typeidx: {TypeIdx}" : $"primaryvaluetype: {PrimaryValueType}";
        }
    }

    public struct WitExternDescription {
        public WitExternDescriptionKind Kind;
        public WitTypeBound TypeBound;
        public WitValueBound ValueBound;
        public WitValueType ValueType;
        public UInt32 Idx;
    }

    public struct WitImport
    {
        public string Name;
        public UInt32 Length;

        public WitExternDescription ExternDescription;

        override public string ToString()
        {
            var tail = "";
            switch(ExternDescription.Kind)
            {
                case WitExternDescriptionKind.CoreModule:
                case WitExternDescriptionKind.Function:
                case WitExternDescriptionKind.Component:
                case WitExternDescriptionKind.Instance:
                    tail = $" typeidx: {ExternDescription.Idx}";
                    break;
                case WitExternDescriptionKind.Type:
                    tail =  ExternDescription.TypeBound == WitTypeBound.Eq ? $" typebound eq: {ExternDescription.Idx}" : $" typebound sub resource";
                    break;
                case WitExternDescriptionKind.Value:
                    tail = ExternDescription.ValueBound == WitValueBound.Eq ? $" valuebound eq: {ExternDescription.Idx}" : $" valuebound t: {ExternDescription.ValueType}";
                    break;
            }

            return $"import name: \"{Name}\" externdesc kind: {ExternDescription.Kind}{tail}";
        }
    }

    public enum WitCoreSort {
        Function = 0,
        Table,
        Memory,
        Global,
        Type = 0x10,
        Module,
        Instance,
    }

    public enum WitSort {
        CoreSort = 0,
        Function,
        Value,
        Type,
        Component,
        Instance,
    }

    struct WitExport
    {
        public string Name;
        public UInt32 Length;
        public UInt32 SortIdx;
        public WitSort Sort;
        public WitCoreSort CoreSort;
        public WitExternDescription ExternDescription;

        override public string ToString()
        {
            return $"export name: \"{Name}\" sort: {Sort} sortidx: {SortIdx} externdesc kind: {ExternDescription.Kind} typeidx: {ExternDescription.Idx}";
        }
    }

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
    public struct ValueType : IComparable<ValueType>
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

    public struct ResultType : IComparable<ResultType>
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

        public string ToString(int startIdx)
        {
            StringBuilder sb = new();

            for (var i = 0; i < Types.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                if (startIdx >= 0)
                {
                    sb.Append('$');
                    sb.Append(startIdx++.ToString());
                    sb.Append(' ');
                }

                sb.Append(Types[i].ToString());
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return ToString(-1);
        }

    }

    public struct FunctionType : IComparable<FunctionType>
    {
        public ResultType Parameters;
        public ResultType Results;

        public string ToString(string? name, bool displayVars = false)
        {
            var results = Results.Types.Length == 0 ? "" : $" (result {Results})";
            var parameters = Parameters.Types.Length == 0 ? "" : $"(param {Parameters.ToString(displayVars ? 0 : -1)})";
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
