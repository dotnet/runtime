// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace R2RDump
{
    /// <summary>
    /// Disassembler for WebAssembly bytecode.
    /// Decodes WASM binary instructions into WAT (WebAssembly Text) format.
    /// Based on the WebAssembly specification: https://webassembly.github.io/spec/core/
    /// </summary>
    internal sealed class WasmDisassembler
    {
        private readonly byte[] _code;
        private int _offset;
        private readonly int _baseOffset;
        private readonly int _endOffset;

        public WasmDisassembler(byte[] code, int offset, int length)
        {
            _code = code;
            _baseOffset = offset;
            _offset = offset;
            _endOffset = offset + length;
        }

        /// <summary>
        /// Disassemble all instructions in the function body and return the textual representation.
        /// </summary>
        public string Disassemble()
        {
            var sb = new StringBuilder();
            int indent = 0;

            while (_offset < _endOffset)
            {
                int instrOffset = _offset - _baseOffset;
                string instr = DecodeInstruction(ref indent);

                sb.Append($"    {instrOffset:X4}: ");
                if (indent > 0)
                {
                    sb.Append(' ', indent * 2);
                }
                sb.AppendLine(instr);
            }

            return sb.ToString();
        }

        private string DecodeInstruction(ref int indent)
        {
            byte opcode = ReadByte();

            switch (opcode)
            {
                // Control instructions
                case 0x00: return "unreachable";
                case 0x01: return "nop";
                case 0x02:
                {
                    string bt = ReadBlockType();
                    indent++;
                    return $"block{bt}";
                }
                case 0x03:
                {
                    string bt = ReadBlockType();
                    indent++;
                    return $"loop{bt}";
                }
                case 0x04:
                {
                    string bt = ReadBlockType();
                    indent++;
                    return $"if{bt}";
                }
                case 0x05:
                    return "else";
                case 0x08:
                    return $"throw {ReadU32()}";
                case 0x0A:
                    return "throw_ref";
                case 0x0B:
                    if (indent > 0) indent--;
                    return "end";
                case 0x0C: return $"br {ReadU32()}";
                case 0x0D: return $"br_if {ReadU32()}";
                case 0x0E:
                {
                    uint count = ReadU32();
                    var sb = new StringBuilder("br_table");
                    for (uint i = 0; i <= count; i++)
                    {
                        sb.Append($" {ReadU32()}");
                    }
                    return sb.ToString();
                }
                case 0x0F: return "return";
                case 0x10: return $"call {ReadU32()}";
                case 0x11:
                {
                    uint typeIdx = ReadU32();
                    uint tableIdx = ReadU32();
                    return $"call_indirect {tableIdx} (type {typeIdx})";
                }
                case 0x12: return $"return_call {ReadU32()}";
                case 0x13:
                {
                    uint typeIdx = ReadU32();
                    uint tableIdx = ReadU32();
                    return $"return_call_indirect {tableIdx} (type {typeIdx})";
                }
                case 0x14: return $"call_ref {ReadU32()}";
                case 0x15: return $"return_call_ref {ReadU32()}";

                // Parametric instructions
                case 0x1A: return "drop";
                case 0x1B: return "select";
                case 0x1C:
                {
                    uint count = ReadU32();
                    var sb = new StringBuilder("select");
                    for (uint i = 0; i < count; i++)
                    {
                        sb.Append($" {ValTypeName(ReadByte())}");
                    }
                    return sb.ToString();
                }

                // Variable instructions
                case 0x20: return $"local.get {ReadU32()}";
                case 0x21: return $"local.set {ReadU32()}";
                case 0x22: return $"local.tee {ReadU32()}";
                case 0x23: return $"global.get {ReadU32()}";
                case 0x24: return $"global.set {ReadU32()}";

                // Table instructions
                case 0x25: return $"table.get {ReadU32()}";
                case 0x26: return $"table.set {ReadU32()}";

                // Memory instructions
                case 0x28: return $"i32.load {ReadMemArg()}";
                case 0x29: return $"i64.load {ReadMemArg()}";
                case 0x2A: return $"f32.load {ReadMemArg()}";
                case 0x2B: return $"f64.load {ReadMemArg()}";
                case 0x2C: return $"i32.load8_s {ReadMemArg()}";
                case 0x2D: return $"i32.load8_u {ReadMemArg()}";
                case 0x2E: return $"i32.load16_s {ReadMemArg()}";
                case 0x2F: return $"i32.load16_u {ReadMemArg()}";
                case 0x30: return $"i64.load8_s {ReadMemArg()}";
                case 0x31: return $"i64.load8_u {ReadMemArg()}";
                case 0x32: return $"i64.load16_s {ReadMemArg()}";
                case 0x33: return $"i64.load16_u {ReadMemArg()}";
                case 0x34: return $"i64.load32_s {ReadMemArg()}";
                case 0x35: return $"i64.load32_u {ReadMemArg()}";
                case 0x36: return $"i32.store {ReadMemArg()}";
                case 0x37: return $"i64.store {ReadMemArg()}";
                case 0x38: return $"f32.store {ReadMemArg()}";
                case 0x39: return $"f64.store {ReadMemArg()}";
                case 0x3A: return $"i32.store8 {ReadMemArg()}";
                case 0x3B: return $"i32.store16 {ReadMemArg()}";
                case 0x3C: return $"i64.store8 {ReadMemArg()}";
                case 0x3D: return $"i64.store16 {ReadMemArg()}";
                case 0x3E: return $"i64.store32 {ReadMemArg()}";
                case 0x3F:
                {
                    uint memIdx = ReadU32();
                    return $"memory.size {memIdx}";
                }
                case 0x40:
                {
                    uint memIdx = ReadU32();
                    return $"memory.grow {memIdx}";
                }

                // Numeric instructions - constants
                case 0x41: return $"i32.const {ReadI32()}";
                case 0x42: return $"i64.const {ReadI64()}";
                case 0x43: return $"f32.const {ReadF32()}";
                case 0x44: return $"f64.const {ReadF64()}";

                // Numeric instructions - comparison (i32)
                case 0x45: return "i32.eqz";
                case 0x46: return "i32.eq";
                case 0x47: return "i32.ne";
                case 0x48: return "i32.lt_s";
                case 0x49: return "i32.lt_u";
                case 0x4A: return "i32.gt_s";
                case 0x4B: return "i32.gt_u";
                case 0x4C: return "i32.le_s";
                case 0x4D: return "i32.le_u";
                case 0x4E: return "i32.ge_s";
                case 0x4F: return "i32.ge_u";

                // Numeric instructions - comparison (i64)
                case 0x50: return "i64.eqz";
                case 0x51: return "i64.eq";
                case 0x52: return "i64.ne";
                case 0x53: return "i64.lt_s";
                case 0x54: return "i64.lt_u";
                case 0x55: return "i64.gt_s";
                case 0x56: return "i64.gt_u";
                case 0x57: return "i64.le_s";
                case 0x58: return "i64.le_u";
                case 0x59: return "i64.ge_s";
                case 0x5A: return "i64.ge_u";

                // Numeric instructions - comparison (f32)
                case 0x5B: return "f32.eq";
                case 0x5C: return "f32.ne";
                case 0x5D: return "f32.lt";
                case 0x5E: return "f32.gt";
                case 0x5F: return "f32.le";
                case 0x60: return "f32.ge";

                // Numeric instructions - comparison (f64)
                case 0x61: return "f64.eq";
                case 0x62: return "f64.ne";
                case 0x63: return "f64.lt";
                case 0x64: return "f64.gt";
                case 0x65: return "f64.le";
                case 0x66: return "f64.ge";

                // Numeric instructions - arithmetic (i32)
                case 0x67: return "i32.clz";
                case 0x68: return "i32.ctz";
                case 0x69: return "i32.popcnt";
                case 0x6A: return "i32.add";
                case 0x6B: return "i32.sub";
                case 0x6C: return "i32.mul";
                case 0x6D: return "i32.div_s";
                case 0x6E: return "i32.div_u";
                case 0x6F: return "i32.rem_s";
                case 0x70: return "i32.rem_u";
                case 0x71: return "i32.and";
                case 0x72: return "i32.or";
                case 0x73: return "i32.xor";
                case 0x74: return "i32.shl";
                case 0x75: return "i32.shr_s";
                case 0x76: return "i32.shr_u";
                case 0x77: return "i32.rotl";
                case 0x78: return "i32.rotr";

                // Numeric instructions - arithmetic (i64)
                case 0x79: return "i64.clz";
                case 0x7A: return "i64.ctz";
                case 0x7B: return "i64.popcnt";
                case 0x7C: return "i64.add";
                case 0x7D: return "i64.sub";
                case 0x7E: return "i64.mul";
                case 0x7F: return "i64.div_s";
                case 0x80: return "i64.div_u";
                case 0x81: return "i64.rem_s";
                case 0x82: return "i64.rem_u";
                case 0x83: return "i64.and";
                case 0x84: return "i64.or";
                case 0x85: return "i64.xor";
                case 0x86: return "i64.shl";
                case 0x87: return "i64.shr_s";
                case 0x88: return "i64.shr_u";
                case 0x89: return "i64.rotl";
                case 0x8A: return "i64.rotr";

                // Numeric instructions - arithmetic (f32)
                case 0x8B: return "f32.abs";
                case 0x8C: return "f32.neg";
                case 0x8D: return "f32.ceil";
                case 0x8E: return "f32.floor";
                case 0x8F: return "f32.trunc";
                case 0x90: return "f32.nearest";
                case 0x91: return "f32.sqrt";
                case 0x92: return "f32.add";
                case 0x93: return "f32.sub";
                case 0x94: return "f32.mul";
                case 0x95: return "f32.div";
                case 0x96: return "f32.min";
                case 0x97: return "f32.max";
                case 0x98: return "f32.copysign";

                // Numeric instructions - arithmetic (f64)
                case 0x99: return "f64.abs";
                case 0x9A: return "f64.neg";
                case 0x9B: return "f64.ceil";
                case 0x9C: return "f64.floor";
                case 0x9D: return "f64.trunc";
                case 0x9E: return "f64.nearest";
                case 0x9F: return "f64.sqrt";
                case 0xA0: return "f64.add";
                case 0xA1: return "f64.sub";
                case 0xA2: return "f64.mul";
                case 0xA3: return "f64.div";
                case 0xA4: return "f64.min";
                case 0xA5: return "f64.max";
                case 0xA6: return "f64.copysign";

                // Numeric instructions - conversions
                case 0xA7: return "i32.wrap_i64";
                case 0xA8: return "i32.trunc_f32_s";
                case 0xA9: return "i32.trunc_f32_u";
                case 0xAA: return "i32.trunc_f64_s";
                case 0xAB: return "i32.trunc_f64_u";
                case 0xAC: return "i64.extend_i32_s";
                case 0xAD: return "i64.extend_i32_u";
                case 0xAE: return "i64.trunc_f32_s";
                case 0xAF: return "i64.trunc_f32_u";
                case 0xB0: return "i64.trunc_f64_s";
                case 0xB1: return "i64.trunc_f64_u";
                case 0xB2: return "f32.convert_i32_s";
                case 0xB3: return "f32.convert_i32_u";
                case 0xB4: return "f32.convert_i64_s";
                case 0xB5: return "f32.convert_i64_u";
                case 0xB6: return "f32.demote_f64";
                case 0xB7: return "f64.convert_i32_s";
                case 0xB8: return "f64.convert_i32_u";
                case 0xB9: return "f64.convert_i64_s";
                case 0xBA: return "f64.convert_i64_u";
                case 0xBB: return "f64.promote_f32";
                case 0xBC: return "i32.reinterpret_f32";
                case 0xBD: return "i64.reinterpret_f64";
                case 0xBE: return "f32.reinterpret_i32";
                case 0xBF: return "f64.reinterpret_i64";

                // Sign extension instructions
                case 0xC0: return "i32.extend8_s";
                case 0xC1: return "i32.extend16_s";
                case 0xC2: return "i64.extend8_s";
                case 0xC3: return "i64.extend16_s";
                case 0xC4: return "i64.extend32_s";

                // Reference instructions
                case 0xD0: return $"ref.null {ReadHeapType()}";
                case 0xD1: return "ref.is_null";
                case 0xD2: return $"ref.func {ReadU32()}";
                case 0xD3: return "ref.eq";
                case 0xD4: return "ref.as_non_null";
                case 0xD5: return $"br_on_null {ReadU32()}";
                case 0xD6: return $"br_on_non_null {ReadU32()}";

                // GC instructions (0xFB prefix)
                case 0xFB: return DecodeFBPrefixed();

                // Saturating truncation and bulk memory (0xFC prefix)
                case 0xFC: return DecodeFCPrefixed();

                // SIMD (0xFD prefix)
                case 0xFD: return DecodeFDPrefixed();

                default:
                    return $"<unknown opcode 0x{opcode:X2}>";
            }
        }

        private string DecodeFCPrefixed()
        {
            uint subOpcode = ReadU32();
            switch (subOpcode)
            {
                case 0: return "i32.trunc_sat_f32_s";
                case 1: return "i32.trunc_sat_f32_u";
                case 2: return "i32.trunc_sat_f64_s";
                case 3: return "i32.trunc_sat_f64_u";
                case 4: return "i64.trunc_sat_f32_s";
                case 5: return "i64.trunc_sat_f32_u";
                case 6: return "i64.trunc_sat_f64_s";
                case 7: return "i64.trunc_sat_f64_u";
                case 8:
                {
                    uint dataIdx = ReadU32();
                    uint memIdx = ReadU32();
                    return $"memory.init {dataIdx} {memIdx}";
                }
                case 9: return $"data.drop {ReadU32()}";
                case 10:
                {
                    uint dstMem = ReadU32();
                    uint srcMem = ReadU32();
                    return $"memory.copy {dstMem} {srcMem}";
                }
                case 11: return $"memory.fill {ReadU32()}";
                case 12:
                {
                    uint elemIdx = ReadU32();
                    uint tableIdx = ReadU32();
                    return $"table.init {tableIdx} {elemIdx}";
                }
                case 13: return $"elem.drop {ReadU32()}";
                case 14:
                {
                    uint dstTable = ReadU32();
                    uint srcTable = ReadU32();
                    return $"table.copy {dstTable} {srcTable}";
                }
                case 15: return $"table.grow {ReadU32()}";
                case 16: return $"table.size {ReadU32()}";
                case 17: return $"table.fill {ReadU32()}";
                default:
                    return $"<unknown 0xFC sub-opcode {subOpcode}>";
            }
        }

        private string DecodeFBPrefixed()
        {
            uint subOpcode = ReadU32();
            switch (subOpcode)
            {
                case 0: return $"struct.new {ReadU32()}";
                case 1: return $"struct.new_default {ReadU32()}";
                case 2:
                {
                    uint typeIdx = ReadU32();
                    uint fieldIdx = ReadU32();
                    return $"struct.get {typeIdx} {fieldIdx}";
                }
                case 3:
                {
                    uint typeIdx = ReadU32();
                    uint fieldIdx = ReadU32();
                    return $"struct.get_s {typeIdx} {fieldIdx}";
                }
                case 4:
                {
                    uint typeIdx = ReadU32();
                    uint fieldIdx = ReadU32();
                    return $"struct.get_u {typeIdx} {fieldIdx}";
                }
                case 5:
                {
                    uint typeIdx = ReadU32();
                    uint fieldIdx = ReadU32();
                    return $"struct.set {typeIdx} {fieldIdx}";
                }
                case 6: return $"array.new {ReadU32()}";
                case 7: return $"array.new_default {ReadU32()}";
                case 8:
                {
                    uint typeIdx = ReadU32();
                    uint size = ReadU32();
                    return $"array.new_fixed {typeIdx} {size}";
                }
                case 9:
                {
                    uint typeIdx = ReadU32();
                    uint dataIdx = ReadU32();
                    return $"array.new_data {typeIdx} {dataIdx}";
                }
                case 10:
                {
                    uint typeIdx = ReadU32();
                    uint elemIdx = ReadU32();
                    return $"array.new_elem {typeIdx} {elemIdx}";
                }
                case 11: return $"array.get {ReadU32()}";
                case 12: return $"array.get_s {ReadU32()}";
                case 13: return $"array.get_u {ReadU32()}";
                case 14: return $"array.set {ReadU32()}";
                case 15: return "array.len";
                case 16: return $"array.fill {ReadU32()}";
                case 17:
                {
                    uint dstType = ReadU32();
                    uint srcType = ReadU32();
                    return $"array.copy {dstType} {srcType}";
                }
                case 18:
                {
                    uint typeIdx = ReadU32();
                    uint dataIdx = ReadU32();
                    return $"array.init_data {typeIdx} {dataIdx}";
                }
                case 19:
                {
                    uint typeIdx = ReadU32();
                    uint elemIdx = ReadU32();
                    return $"array.init_elem {typeIdx} {elemIdx}";
                }
                case 20: return $"ref.test (ref {ReadHeapType()})";
                case 21: return $"ref.test (ref null {ReadHeapType()})";
                case 22: return $"ref.cast (ref {ReadHeapType()})";
                case 23: return $"ref.cast (ref null {ReadHeapType()})";
                case 26: return "any.convert_extern";
                case 27: return "extern.convert_any";
                case 28: return $"ref.i31";
                case 29: return "i31.get_s";
                case 30: return "i31.get_u";
                default:
                    return $"<unknown 0xFB sub-opcode {subOpcode}>";
            }
        }

        private string DecodeFDPrefixed()
        {
            uint subOpcode = ReadU32();
            // SIMD instructions - just show the sub-opcode for now
            // Full SIMD decode would add hundreds of entries
            if (subOpcode == 0)
            {
                // v128.load memarg
                return $"v128.load {ReadMemArg()}";
            }
            if (subOpcode == 11)
            {
                // v128.store memarg
                return $"v128.store {ReadMemArg()}";
            }
            if (subOpcode == 12)
            {
                // v128.const - 16 bytes immediate
                var bytes = new byte[16];
                for (int i = 0; i < 16; i++)
                    bytes[i] = ReadByte();
                return $"v128.const 0x{BitConverter.ToString(bytes).Replace("-", "")}";
            }
            return $"<simd opcode 0xFD {subOpcode}>";
        }

        private byte ReadByte()
        {
            if (_offset >= _endOffset)
                return 0;
            return _code[_offset++];
        }

        private uint ReadU32()
        {
            uint result = 0;
            int shift = 0;
            byte b;
            do
            {
                b = ReadByte();
                result |= (uint)(b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return result;
        }

        private int ReadI32()
        {
            int result = 0;
            int shift = 0;
            byte b;
            do
            {
                b = ReadByte();
                result |= (int)(b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            // Sign extend
            if (shift < 32 && (b & 0x40) != 0)
                result |= -(1 << shift);
            return result;
        }

        private long ReadI64()
        {
            long result = 0;
            int shift = 0;
            byte b;
            do
            {
                b = ReadByte();
                result |= (long)(b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            // Sign extend
            if (shift < 64 && (b & 0x40) != 0)
                result |= -(1L << shift);
            return result;
        }

        private float ReadF32()
        {
            if (_offset + 4 > _endOffset)
            {
                _offset = _endOffset;
                return 0;
            }
            float val = BitConverter.ToSingle(_code, _offset);
            _offset += 4;
            return val;
        }

        private double ReadF64()
        {
            if (_offset + 8 > _endOffset)
            {
                _offset = _endOffset;
                return 0;
            }
            double val = BitConverter.ToDouble(_code, _offset);
            _offset += 8;
            return val;
        }

        private string ReadMemArg()
        {
            uint align = ReadU32();
            // Bit 6 indicates multi-memory (memory index follows)
            uint memIdx = 0;
            if ((align & 0x40) != 0)
            {
                align &= ~0x40u;
                memIdx = ReadU32();
            }
            uint offset = ReadU32();
            if (memIdx != 0)
                return $"align={1u << (int)align} offset={offset} mem={memIdx}";
            if (offset != 0)
                return $"align={1u << (int)align} offset={offset}";
            return $"align={1u << (int)align}";
        }

        private string ReadBlockType()
        {
            if (_offset >= _endOffset)
                return "";
            byte b = _code[_offset];
            if (b == 0x40)
            {
                _offset++;
                return "";
            }
            // Value type encoding uses single bytes 0x7F-0x70 range
            if (b >= 0x70 && b <= 0x7F)
            {
                _offset++;
                return $" (result {ValTypeName(b)})";
            }
            // Otherwise it's a type index as a signed LEB128
            int typeIdx = ReadI32();
            return $" (type {typeIdx})";
        }

        private string ReadHeapType()
        {
            byte b = _code[_offset];
            // Abstract heap types are encoded as single bytes
            switch (b)
            {
                case 0x73: _offset++; return "nofunc";
                case 0x72: _offset++; return "noextern";
                case 0x71: _offset++; return "none";
                case 0x70: _offset++; return "func";
                case 0x6F: _offset++; return "extern";
                case 0x6E: _offset++; return "any";
                case 0x6D: _offset++; return "eq";
                case 0x6C: _offset++; return "i31";
                case 0x6B: _offset++; return "struct";
                case 0x6A: _offset++; return "array";
                default:
                    // Type index as signed LEB128
                    return ReadI32().ToString();
            }
        }

        private static string ValTypeName(byte b)
        {
            return b switch
            {
                0x7F => "i32",
                0x7E => "i64",
                0x7D => "f32",
                0x7C => "f64",
                0x7B => "v128",
                0x70 => "funcref",
                0x6F => "externref",
                0x6E => "anyref",
                0x6D => "eqref",
                0x6C => "i31ref",
                0x6B => "structref",
                0x6A => "arrayref",
                _ => $"<type 0x{b:X2}>"
            };
        }
    }
}
