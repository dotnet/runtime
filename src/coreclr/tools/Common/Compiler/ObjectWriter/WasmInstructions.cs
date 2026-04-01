// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.Wasm;

// This namespace implements encodings for certain Wasm expressions (instructions)
// which are used in the object writer.
// For now, these instructions are only used for constructing constant expressions
// to calculate placements for data segments based on imported globals.
namespace ILCompiler.ObjectWriter.WasmInstructions
{
    // Represents a Wasm function body in the code section.
    public class WasmFunctionBody : IWasmEncodable
    {
        public readonly WasmFuncType Signature;
        private readonly WasmInstructionGroup _body;
        private readonly byte[] _locals;

        private static readonly byte[] s_emptyLocals = new byte[1];

        public WasmFunctionBody(WasmFuncType signature, WasmExpr[] instructions) : this(signature, Array.Empty<WasmValueType>(), instructions)
        {
        }

        public WasmFunctionBody(WasmFuncType signature, WasmValueType[] locals, WasmExpr[] instructions)
        {
            Signature = signature;

            if (locals.Length == 0)
            {
                _locals = s_emptyLocals;
            }
            else
            {
                ArrayBufferWriter<byte> localWriter = new ArrayBufferWriter<byte>();

                WasmValueType currentLocalType = locals[0];
                ulong localChunks = 0;
                ulong localsOfCurrentType = 1;
                for (int i = 1; i < locals.Length; i++)
                {
                    if (locals[i] == currentLocalType)
                    {
                        localsOfCurrentType++;
                    }
                    else
                    {
                        WriteLocalType(localWriter, localsOfCurrentType, currentLocalType);
                        localChunks++;
                        localsOfCurrentType = 1;
                        currentLocalType = locals[i];
                    }
                }
                WriteLocalType(localWriter, localsOfCurrentType, currentLocalType);
                localChunks++;

                // Now we've written the local data, and know the count of locals chunks
                // Build the actual locals
                _locals = new byte[DwarfHelper.SizeOfULEB128(localChunks) + localWriter.WrittenSpan.Length];
                int pos = DwarfHelper.WriteULEB128(_locals.AsSpan(), localChunks);
                localWriter.WrittenSpan.CopyTo(_locals.AsSpan(pos));


                static void WriteLocalType(ArrayBufferWriter<byte> writer, ulong localCount, WasmValueType localType)
                {
                    DwarfHelper.WriteULEB128(writer, (ulong)localCount);
                    byte localTypeByte = (byte)localType;
                    writer.Write(new ReadOnlySpan<byte>(ref localTypeByte));
                }
            }

            _body = new WasmInstructionGroup(instructions);
        }

        private int BodyContentSize()
        {
            // local declarations + instruction group (instructions + end opcode)
            return _locals.Length + _body.EncodeSize();
        }

        public int EncodeSize()
        {
            return BodyContentSize();
        }

        public int Encode(Span<byte> buffer)
        {
            _locals.CopyTo(buffer);
            int pos = _locals.Length;
            pos += _body.Encode(buffer.Slice(pos));

            return pos;
        }

        public int EncodeRelocationCount()
        {
            return _body.EncodeRelocationCount();
        }

        public int EncodeRelocations(Span<Relocation> buffer)
        {
            int relocsEncoded = _body.EncodeRelocations(buffer);
            WasmExpr.OffsetRelocationsByOffset(buffer.Slice(0, relocsEncoded), _locals.Length);
            return relocsEncoded;
        }
    }
    public enum WasmExprKind
    {
        CallIndirect = 0x11,
        LocalGet = 0x20,
        LocalSet = 0x21,
        LocalTee = 0x22,
        GlobalGet = 0x23,
        GlobalSet = 0x24,
        I32Const = 0x41,
        I64Const = 0x42,
        I32Add = 0x6A,
        I32Sub = 0x6B,
        I32Load = 0x28,
        I64Load = 0x29,
        F32Load = 0x2A,
        F64Load = 0x2B,
        I32Store = 0x36,
        I64Store = 0x37,
        F32Store = 0x38,
        F64Store = 0x39,
        // Variable length instructions — not directly cast to a byte, instead the prefix byte is set in the upper 8 bits of the enum, and the lower 24 bits are the extended variable length opcode
        MemoryInit = unchecked((int)0xFC000008),
        V128Load = unchecked((int)0xFD00000A),
        V128Store = unchecked((int)0xFD000000),
    }

    public static class WasmExprKindExtensions
    {
        public static bool IsConstExpr(this WasmExprKind kind)
        {
            return kind == WasmExprKind.I32Const || kind == WasmExprKind.I64Const;
        }

        public static bool IsBinaryExpr(this WasmExprKind kind)
        {
            switch (kind)
            {
                case WasmExprKind.I32Add:
                case WasmExprKind.I32Sub:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsLocalVarExpr(this WasmExprKind kind)
        {
            switch (kind)
            {
                case WasmExprKind.LocalGet:
                case WasmExprKind.LocalSet:
                case WasmExprKind.LocalTee:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsGlobalVarExpr(this WasmExprKind kind)
        {
            return kind == WasmExprKind.GlobalGet || kind == WasmExprKind.GlobalSet;
        }

        public static bool IsMemoryExpr(this WasmExprKind kind)
        {
            return kind == WasmExprKind.MemoryInit;
        }
        public static bool IsVariableLengthInstruction(this WasmExprKind kind)
        {
            return ((int)kind & 0xFF000000) != 0x00;
        }
    }

    // Represents a group of Wasm instructions (expressions) which 
    // form a complete expression ending with the 'end' opcode.
    public class WasmInstructionGroup : IWasmEncodable
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
#if DEBUG
                int encodeSizeExpected = expr.EncodeSize();
                int adjustSize = expr.Encode(buffer.Slice(pos, encodeSizeExpected));
                Debug.Assert(adjustSize == encodeSizeExpected);
                pos += adjustSize;
#else
                pos += expr.Encode(buffer.Slice(pos));
#endif
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

        public int EncodeRelocationCount()
        {
            int count = 0;
            foreach (var expr in _wasmExprs)
            {
                count += expr.EncodeRelocationCount();
            }
            return count;
        }
        public int EncodeRelocations(Span<Relocation> buffer)
        {
            int offset = 0;
            int totalRelocsEncoded = 0;
            foreach (var expr in _wasmExprs)
            {
                int relocsEncoded = expr.EncodeRelocations(buffer.Slice(totalRelocsEncoded));
                Debug.Assert(relocsEncoded == expr.EncodeRelocationCount());
                WasmExpr.OffsetRelocationsByOffset(buffer.Slice(totalRelocsEncoded, relocsEncoded), offset);

                totalRelocsEncoded += relocsEncoded;
                offset += expr.EncodeSize();
            }
            return totalRelocsEncoded;
        }
    }

    public abstract class WasmExpr : IWasmEncodable
    {
        WasmExprKind _kind;
        public WasmExpr(WasmExprKind kind)
        {
            _kind = kind;
        }

        public virtual int EncodeSize() => _kind.IsVariableLengthInstruction() ? 1 + (int)DwarfHelper.SizeOfULEB128(((uint)_kind) & 0xFFFFFF) : 1;
        public virtual int Encode(Span<byte> buffer)
        {
            if (_kind.IsVariableLengthInstruction())
            {
                buffer[0] = (byte)((uint)_kind >> 24);
                return 1 + DwarfHelper.WriteULEB128(buffer.Slice(1), ((uint)_kind) & 0xFFFFFF);
            }
            else
            {
                buffer[0] = (byte)_kind;
                return 1;
            }
        }
        public virtual int EncodeRelocationCount() => 0;
        public virtual int EncodeRelocations(Span<Relocation> buffer)
        {
            Debug.Assert(buffer.Length >= EncodeRelocationCount());
            return 0;
        }

        public static void OffsetRelocationsByOffset(Span<Relocation> buffer, int offset)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                Relocation r = buffer[i];
                buffer[i] = new Relocation(r.RelocType, r.Offset + offset, r.Target);
            }
        }
    }

    class WasmMemoryArgInstruction : WasmExpr
    {
        readonly uint _align;
        readonly ulong _offset;

        public WasmMemoryArgInstruction(WasmExprKind kind, uint align, ulong offset) : base(kind)
        {
            switch (align)
            {
                case 1: _align = 0; break;
                case 2: _align = 1; break;
                case 4: _align = 2; break;
                case 8: _align = 3; break;
                case 16: _align = 4; break;
                default:
                    throw new Exception();
            }
            _offset = offset;
        }

        public override int EncodeSize()
        {
            uint valSize = DwarfHelper.SizeOfULEB128(_align) + DwarfHelper.SizeOfULEB128(_offset);
            return base.EncodeSize() + (int)valSize;
        }

        public override int Encode(Span<byte> buffer)
        {
            int pos = base.Encode(buffer);
            pos += DwarfHelper.WriteULEB128(buffer.Slice(pos), _align);
            pos += DwarfHelper.WriteULEB128(buffer.Slice(pos), _offset);
            return pos;
        }
    }

    // Represents a constant expression (e.g., (i32.const <value>))
    class WasmConstExpr : WasmExpr
    {
        readonly long ConstValue;

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

    sealed class WasmIndirectCallInstruction : WasmExpr
    {
        ISymbolNode _type;
        uint _tableIndex;

        public WasmIndirectCallInstruction(WasmExprKind kind, ISymbolNode type, uint tableIndex) : base(kind)
        {
            _type = type;
            _tableIndex = tableIndex;
        }

        public override int EncodeSize()
        {
            uint tableSize = DwarfHelper.SizeOfULEB128(_tableIndex);
            return base.EncodeSize() + Relocation.GetSize(RelocType.WASM_TYPE_INDEX_LEB) + (int)tableSize;
        }

        public override int Encode(Span<byte> buffer)
        {
            int pos = base.Encode(buffer);
            int relocSize = Relocation.GetSize(RelocType.WASM_TYPE_INDEX_LEB);
            DwarfHelper.WritePaddedULEB128(buffer.Slice(pos, relocSize), 0);
            pos += relocSize;
            pos += DwarfHelper.WriteULEB128(buffer.Slice(pos), _tableIndex);

            return pos;
        }

        public override int EncodeRelocationCount() => 1;
        public override int EncodeRelocations(Span<Relocation> buffer)
        {
            buffer[0] = new Relocation(RelocType.WASM_TYPE_INDEX_LEB, base.EncodeSize(), _type);
            return 1;
        }
    }

    sealed class WasmLEBConstantReloc : WasmExpr
    {
        readonly ISymbolNode _symbol;
        readonly RelocType _relocType;

        public WasmLEBConstantReloc(WasmExprKind kind, ISymbolNode symbol, RelocType relocType) : base(kind)
        {
            _symbol = symbol;
            _relocType = relocType;
        }
        public override int EncodeSize() => base.EncodeSize() + Relocation.GetSize(_relocType);
        public override int Encode(Span<byte> buffer)
        {
            int pos = base.Encode(buffer);
            int relocSize = Relocation.GetSize(_relocType);
            switch (_relocType)
            {
                case RelocType.WASM_FUNCTION_INDEX_LEB:
                case RelocType.WASM_MEMORY_ADDR_LEB:
                case RelocType.WASM_TYPE_INDEX_LEB:
                case RelocType.WASM_GLOBAL_INDEX_LEB:
                    DwarfHelper.WritePaddedULEB128(buffer.Slice(pos, relocSize), 0);
                    break;

                case RelocType.WASM_TABLE_INDEX_SLEB:
                case RelocType.WASM_MEMORY_ADDR_REL_SLEB:
                    DwarfHelper.WritePaddedSLEB128(buffer.Slice(pos, relocSize), 0);
                    break;

                default:
                    throw new Exception($"Unknown WASM reloc type : {_relocType}");
            }

            pos += relocSize;
            return pos;
        }

        public override int EncodeRelocationCount() => 1;
        public override int EncodeRelocations(Span<Relocation> buffer)
        {
            buffer[0] = new Relocation(_relocType, base.EncodeSize(), _symbol);
            return 1;
        }
    }

    // Represents a local variable expression (e.g., (local.get <index>))
    class WasmLocalVarExpr : WasmExpr
    {
        public readonly int LocalIndex;
        public WasmLocalVarExpr(WasmExprKind kind, int localIndex) : base(kind)
        {
            Debug.Assert(localIndex >= 0);
            Debug.Assert(kind.IsLocalVarExpr());
            LocalIndex = localIndex;
        }

        public override int Encode(Span<byte> buffer)
        {
            int pos = base.Encode(buffer);
            pos += DwarfHelper.WriteULEB128(buffer.Slice(pos), (uint)LocalIndex);

            return pos;
        }

        public override int EncodeSize()
        {
            return base.EncodeSize() + (int)DwarfHelper.SizeOfULEB128((uint)LocalIndex);
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

    // Represents a memory.init expression.
    // Binary encoding: 0xFC prefix + u32(8) sub-opcode + u32(dataSegmentIndex) + u32(memoryIndex)
    class WasmMemoryInitExpr : WasmExpr
    {
        public readonly int DataSegmentIndex;
        public readonly int MemoryIndex;

        public WasmMemoryInitExpr(int dataSegmentIndex, int memoryIndex = 0) : base(WasmExprKind.MemoryInit)
        {
            Debug.Assert(dataSegmentIndex >= 0);
            Debug.Assert(memoryIndex >= 0);
            DataSegmentIndex = dataSegmentIndex;
            MemoryIndex = memoryIndex;
        }

        public override int Encode(Span<byte> buffer)
        {
            int pos = base.Encode(buffer);
            pos += DwarfHelper.WriteULEB128(buffer.Slice(pos), (uint)DataSegmentIndex);
            pos += DwarfHelper.WriteULEB128(buffer.Slice(pos), (uint)MemoryIndex);

            return pos;
        }

        public override int EncodeSize()
        {
            return base.EncodeSize()
                + (int)DwarfHelper.SizeOfULEB128((uint)DataSegmentIndex)
                + (int)DwarfHelper.SizeOfULEB128((uint)MemoryIndex);
        }
    }

    // ************************************************
    // Simple DSL wrapper for creating Wasm expressions
    // ************************************************
    static class Local
    {
        public static WasmExpr Get(int index)
        {
            return new WasmLocalVarExpr(WasmExprKind.LocalGet, index);
        }
        public static WasmExpr Set(int index)
        {
            return new WasmLocalVarExpr(WasmExprKind.LocalSet, index);
        }
        public static WasmExpr Tee(int index)
        {
            return new WasmLocalVarExpr(WasmExprKind.LocalTee, index);
        }
    }

    static class Global
    {
        public static WasmExpr Get(int index)
        {
            return new WasmGlobalVarExpr(WasmExprKind.GlobalGet, index);
        }
        public static WasmExpr Set(int index)
        {
            return new WasmGlobalVarExpr(WasmExprKind.GlobalSet, index);
        }
    }

    static class I32
    {
        public static WasmExpr Const(long value)
        {
            return new WasmConstExpr(WasmExprKind.I32Const, value);
        }
        public static WasmExpr ConstRVA(ISymbolNode symbolNode)
        {
            return new WasmLEBConstantReloc(WasmExprKind.I32Const, symbolNode, RelocType.WASM_MEMORY_ADDR_REL_SLEB);
        }

        public static WasmExpr Add => new WasmBinaryExpr(WasmExprKind.I32Add);
        public static WasmExpr Sub => new WasmBinaryExpr(WasmExprKind.I32Sub);
        public static WasmExpr Load(ulong offset) => new WasmMemoryArgInstruction(WasmExprKind.I32Load, 4, offset);
        public static WasmExpr Store(ulong offset) => new WasmMemoryArgInstruction(WasmExprKind.I32Store, 4, offset);
    }

    static class I64
    {
        public static WasmExpr Load(ulong offset) => new WasmMemoryArgInstruction(WasmExprKind.I64Load, 8, offset);
        public static WasmExpr Store(ulong offset) => new WasmMemoryArgInstruction(WasmExprKind.I64Store, 8, offset);
    }

    static class F32
    {
        public static WasmExpr Load(ulong offset) => new WasmMemoryArgInstruction(WasmExprKind.F32Load, 4, offset);
        public static WasmExpr Store(ulong offset) => new WasmMemoryArgInstruction(WasmExprKind.F32Store, 4, offset);
    }

    static class F64
    {
        public static WasmExpr Load(ulong offset) => new WasmMemoryArgInstruction(WasmExprKind.F64Load, 8, offset);
        public static WasmExpr Store(ulong offset) => new WasmMemoryArgInstruction(WasmExprKind.F64Store, 8, offset);
    }

    static class V128
    {
        public static WasmExpr Load(ulong offset) => new WasmMemoryArgInstruction(WasmExprKind.V128Load, 16, offset);
        public static WasmExpr Store(ulong offset) => new WasmMemoryArgInstruction(WasmExprKind.V128Store, 16, offset);
    }

    static class Memory
    {
        public static WasmExpr Init(int dataSegmentIndex, int memoryIndex = 0)
        {
            return new WasmMemoryInitExpr(dataSegmentIndex, memoryIndex);
        }
    }
    static class ControlFlow
    {
        public static WasmExpr CallIndirect(ISymbolNode funcType, uint tableIndex) => new WasmIndirectCallInstruction(WasmExprKind.CallIndirect, funcType, tableIndex);
    }
}
