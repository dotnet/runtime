// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

// This namespace implements encodings for certain Wasm expressions (instructions)
// which are used in the object writer.
// For now, these instructions are only used for constructing constant expressions
// to calculate placements for data segments based on imported globals.
namespace ILCompiler.ObjectWriter.WasmInstructions
{
    public enum WasmExprKind
    {
        I32Const = 0x41,
        I64Const = 0x42,
        GlobalGet = 0x23,
        I32Add = 0x6A,
    }

    public static class WasmExprKindExtensions
    {
        public static bool IsConstExpr(this WasmExprKind kind)
        {
            return kind == WasmExprKind.I32Const || kind == WasmExprKind.I64Const;
        }

        public static bool IsBinaryExpr(this WasmExprKind kind)
        {
            return kind == WasmExprKind.I32Add;
        }

        public static bool IsGlobalVarExpr(this WasmExprKind kind)
        {
            return kind == WasmExprKind.GlobalGet;
        }
    }

    // Represents a group of Wasm instructions (expressions) which 
    // form a complete expression ending with the 'end' opcode.
    class WasmInstructionGroup : IWasmEncodable
    {
        readonly WasmExpr[] _wasmExprs;
        public WasmInstructionGroup(WasmExpr[] wasmExprs)
        {
            _wasmExprs = wasmExprs;
        }

        public int Encode(Span<byte> buffer)
        {
            int pos = 0;
            foreach (var expr in _wasmExprs)
            {
                pos += expr.Encode(buffer.Slice(pos));
            }
            buffer[pos++] = 0x0B; // end opcode
            return pos;
        }

        public int EncodeSize()
        {
            int size = 0;
            foreach (var expr in _wasmExprs)
            {
                size += expr.EncodeSize();
            }
            // plus one for the end opcode
            return size + 1;
        }
    }

    public abstract class WasmExpr : IWasmEncodable
    {
        WasmExprKind _kind;
        public WasmExpr(WasmExprKind kind)
        {
            _kind = kind;
        }

        public virtual int EncodeSize() => 1;
        public virtual int Encode(Span<byte> buffer)
        {
            buffer[0] = (byte)_kind;
            return 1;
        }
    }

    // Represents a constant expression (e.g., (i32.const <value>))
    class WasmConstExpr : WasmExpr
    {
        long ConstValue;

        public WasmConstExpr(WasmExprKind kind, long value) : base(kind)
        {
            if (kind == WasmExprKind.I32Const)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, int.MaxValue);
                ArgumentOutOfRangeException.ThrowIfLessThan(value, int.MinValue);
            }

            ConstValue = value;
        }

        public override int EncodeSize()
        {
            uint valSize = DwarfHelper.SizeOfSLEB128(ConstValue);
            return base.EncodeSize() + (int)valSize;
        }

        public override int Encode(Span<byte> buffer)
        {
            int pos = base.Encode(buffer);
            pos += DwarfHelper.WriteSLEB128(buffer.Slice(pos), ConstValue);

            return pos;
        }
    }

    // Represents a global variable expression (e.g., (global.get <index))
    class WasmGlobalVarExpr : WasmExpr
    {
        public readonly int GlobalIndex;
        public WasmGlobalVarExpr(WasmExprKind kind, int globalIndex) : base(kind)
        {
            Debug.Assert(globalIndex >= 0);
            Debug.Assert(kind.IsGlobalVarExpr());
            GlobalIndex = globalIndex;
        }

        public override int Encode(Span<byte> buffer)
        {
            int pos = base.Encode(buffer);
            pos += DwarfHelper.WriteULEB128(buffer.Slice(pos), (uint)GlobalIndex);
            return pos;
        }

        public override int EncodeSize()
        {
            return base.EncodeSize() + (int)DwarfHelper.SizeOfULEB128((uint)GlobalIndex);
        }
    }

    // Represents a binary expression (e.g., i32.add)
    class WasmBinaryExpr : WasmExpr
    {
        public WasmBinaryExpr(WasmExprKind kind) : base(kind)
        {
            Debug.Assert(kind.IsBinaryExpr());
        }

        // base class defaults are sufficient as the base class encodes just the opcode
    }

    // ************************************************
    // Simple DSL wrapper for creating Wasm expressions
    // ************************************************
    static class Global
    {
        public static WasmExpr Get(int index)
        {
            return new WasmGlobalVarExpr(WasmExprKind.GlobalGet, index);
        }
    }

    static class I32
    {
        public static WasmExpr Const(long value)
        {
            return new WasmConstExpr(WasmExprKind.I32Const, value);
        }

        public static WasmExpr Add => new WasmBinaryExpr(WasmExprKind.I32Add);
    }
}
