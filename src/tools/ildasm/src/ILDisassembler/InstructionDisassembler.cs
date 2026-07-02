// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ILDisassembler;

/// <summary>
/// Disassembles IL instructions from a method body.
/// This is the core "switch on opcode" logic.
/// </summary>
public sealed class InstructionDisassembler
{
    private readonly MetadataReader _reader;

    public InstructionDisassembler(MetadataReader reader)
    {
        _reader = reader;
    }

    /// <summary>
    /// Disassembles a single instruction and advances the reader.
    /// </summary>
    public string DisassembleInstruction(ref BlobReader reader)
    {
        int opcode = reader.ReadByte();

        // Two-byte opcodes start with 0xFE
        if (opcode == 0xFE)
        {
            opcode = 0xFE00 + reader.ReadByte();
        }

        var ilOpcode = (ILOpCode)opcode;

        return ilOpcode switch
        {
            // ===== No operand instructions =====
            ILOpCode.Nop => "nop",
            ILOpCode.Break => "break",
            ILOpCode.Ret => "ret",
            ILOpCode.Ldnull => "ldnull",
            ILOpCode.Dup => "dup",
            ILOpCode.Pop => "pop",
            ILOpCode.Throw => "throw",
            ILOpCode.Rethrow => "rethrow",
            ILOpCode.Ldlen => "ldlen",
            ILOpCode.Ckfinite => "ckfinite",
            ILOpCode.Localloc => "localloc",
            ILOpCode.Endfilter => "endfilter",
            ILOpCode.Endfinally => "endfinally",
            ILOpCode.Arglist => "arglist",
            ILOpCode.Cpblk => "cpblk",
            ILOpCode.Initblk => "initblk",
            ILOpCode.Refanytype => "refanytype",

            // ===== Arithmetic =====
            ILOpCode.Add => "add",
            ILOpCode.Add_ovf => "add.ovf",
            ILOpCode.Add_ovf_un => "add.ovf.un",
            ILOpCode.Sub => "sub",
            ILOpCode.Sub_ovf => "sub.ovf",
            ILOpCode.Sub_ovf_un => "sub.ovf.un",
            ILOpCode.Mul => "mul",
            ILOpCode.Mul_ovf => "mul.ovf",
            ILOpCode.Mul_ovf_un => "mul.ovf.un",
            ILOpCode.Div => "div",
            ILOpCode.Div_un => "div.un",
            ILOpCode.Rem => "rem",
            ILOpCode.Rem_un => "rem.un",
            ILOpCode.Neg => "neg",

            // ===== Bitwise =====
            ILOpCode.And => "and",
            ILOpCode.Or => "or",
            ILOpCode.Xor => "xor",
            ILOpCode.Not => "not",
            ILOpCode.Shl => "shl",
            ILOpCode.Shr => "shr",
            ILOpCode.Shr_un => "shr.un",

            // ===== Comparison =====
            ILOpCode.Ceq => "ceq",
            ILOpCode.Cgt => "cgt",
            ILOpCode.Cgt_un => "cgt.un",
            ILOpCode.Clt => "clt",
            ILOpCode.Clt_un => "clt.un",

            // ===== Load constants =====
            ILOpCode.Ldc_i4_m1 => "ldc.i4.m1",
            ILOpCode.Ldc_i4_0 => "ldc.i4.0",
            ILOpCode.Ldc_i4_1 => "ldc.i4.1",
            ILOpCode.Ldc_i4_2 => "ldc.i4.2",
            ILOpCode.Ldc_i4_3 => "ldc.i4.3",
            ILOpCode.Ldc_i4_4 => "ldc.i4.4",
            ILOpCode.Ldc_i4_5 => "ldc.i4.5",
            ILOpCode.Ldc_i4_6 => "ldc.i4.6",
            ILOpCode.Ldc_i4_7 => "ldc.i4.7",
            ILOpCode.Ldc_i4_8 => "ldc.i4.8",
            ILOpCode.Ldc_i4_s => $"ldc.i4.s {reader.ReadSByte()}",
            ILOpCode.Ldc_i4 => $"ldc.i4 0x{reader.ReadInt32():x}",
            ILOpCode.Ldc_i8 => $"ldc.i8 0x{reader.ReadInt64():x}",
            ILOpCode.Ldc_r4 => $"ldc.r4 {reader.ReadSingle():G9}",
            ILOpCode.Ldc_r8 => $"ldc.r8 {reader.ReadDouble():G17}",

            // ===== Load/store locals (short form) =====
            ILOpCode.Ldloc_0 => "ldloc.0",
            ILOpCode.Ldloc_1 => "ldloc.1",
            ILOpCode.Ldloc_2 => "ldloc.2",
            ILOpCode.Ldloc_3 => "ldloc.3",
            ILOpCode.Stloc_0 => "stloc.0",
            ILOpCode.Stloc_1 => "stloc.1",
            ILOpCode.Stloc_2 => "stloc.2",
            ILOpCode.Stloc_3 => "stloc.3",
            ILOpCode.Ldloc_s => $"ldloc.s {reader.ReadByte()}",
            ILOpCode.Stloc_s => $"stloc.s {reader.ReadByte()}",
            ILOpCode.Ldloca_s => $"ldloca.s {reader.ReadByte()}",

            // ===== Load/store locals (long form) =====
            ILOpCode.Ldloc => $"ldloc {reader.ReadUInt16()}",
            ILOpCode.Stloc => $"stloc {reader.ReadUInt16()}",
            ILOpCode.Ldloca => $"ldloca {reader.ReadUInt16()}",

            // ===== Load/store args (short form) =====
            ILOpCode.Ldarg_0 => "ldarg.0",
            ILOpCode.Ldarg_1 => "ldarg.1",
            ILOpCode.Ldarg_2 => "ldarg.2",
            ILOpCode.Ldarg_3 => "ldarg.3",
            ILOpCode.Ldarg_s => $"ldarg.s {reader.ReadByte()}",
            ILOpCode.Starg_s => $"starg.s {reader.ReadByte()}",
            ILOpCode.Ldarga_s => $"ldarga.s {reader.ReadByte()}",

            // ===== Load/store args (long form) =====
            ILOpCode.Ldarg => $"ldarg {reader.ReadUInt16()}",
            ILOpCode.Starg => $"starg {reader.ReadUInt16()}",
            ILOpCode.Ldarga => $"ldarga {reader.ReadUInt16()}",

            // ===== Indirect load =====
            ILOpCode.Ldind_i1 => "ldind.i1",
            ILOpCode.Ldind_u1 => "ldind.u1",
            ILOpCode.Ldind_i2 => "ldind.i2",
            ILOpCode.Ldind_u2 => "ldind.u2",
            ILOpCode.Ldind_i4 => "ldind.i4",
            ILOpCode.Ldind_u4 => "ldind.u4",
            ILOpCode.Ldind_i8 => "ldind.i8",
            ILOpCode.Ldind_i => "ldind.i",
            ILOpCode.Ldind_r4 => "ldind.r4",
            ILOpCode.Ldind_r8 => "ldind.r8",
            ILOpCode.Ldind_ref => "ldind.ref",

            // ===== Indirect store =====
            ILOpCode.Stind_ref => "stind.ref",
            ILOpCode.Stind_i1 => "stind.i1",
            ILOpCode.Stind_i2 => "stind.i2",
            ILOpCode.Stind_i4 => "stind.i4",
            ILOpCode.Stind_i8 => "stind.i8",
            ILOpCode.Stind_r4 => "stind.r4",
            ILOpCode.Stind_r8 => "stind.r8",
            ILOpCode.Stind_i => "stind.i",

            // ===== Conversions =====
            ILOpCode.Conv_i1 => "conv.i1",
            ILOpCode.Conv_i2 => "conv.i2",
            ILOpCode.Conv_i4 => "conv.i4",
            ILOpCode.Conv_i8 => "conv.i8",
            ILOpCode.Conv_r4 => "conv.r4",
            ILOpCode.Conv_r8 => "conv.r8",
            ILOpCode.Conv_u1 => "conv.u1",
            ILOpCode.Conv_u2 => "conv.u2",
            ILOpCode.Conv_u4 => "conv.u4",
            ILOpCode.Conv_u8 => "conv.u8",
            ILOpCode.Conv_i => "conv.i",
            ILOpCode.Conv_u => "conv.u",
            ILOpCode.Conv_r_un => "conv.r.un",
            ILOpCode.Conv_ovf_i1 => "conv.ovf.i1",
            ILOpCode.Conv_ovf_i2 => "conv.ovf.i2",
            ILOpCode.Conv_ovf_i4 => "conv.ovf.i4",
            ILOpCode.Conv_ovf_i8 => "conv.ovf.i8",
            ILOpCode.Conv_ovf_u1 => "conv.ovf.u1",
            ILOpCode.Conv_ovf_u2 => "conv.ovf.u2",
            ILOpCode.Conv_ovf_u4 => "conv.ovf.u4",
            ILOpCode.Conv_ovf_u8 => "conv.ovf.u8",
            ILOpCode.Conv_ovf_i => "conv.ovf.i",
            ILOpCode.Conv_ovf_u => "conv.ovf.u",
            ILOpCode.Conv_ovf_i1_un => "conv.ovf.i1.un",
            ILOpCode.Conv_ovf_i2_un => "conv.ovf.i2.un",
            ILOpCode.Conv_ovf_i4_un => "conv.ovf.i4.un",
            ILOpCode.Conv_ovf_i8_un => "conv.ovf.i8.un",
            ILOpCode.Conv_ovf_u1_un => "conv.ovf.u1.un",
            ILOpCode.Conv_ovf_u2_un => "conv.ovf.u2.un",
            ILOpCode.Conv_ovf_u4_un => "conv.ovf.u4.un",
            ILOpCode.Conv_ovf_u8_un => "conv.ovf.u8.un",
            ILOpCode.Conv_ovf_i_un => "conv.ovf.i.un",
            ILOpCode.Conv_ovf_u_un => "conv.ovf.u.un",

            // ===== Array element load =====
            ILOpCode.Ldelem_i1 => "ldelem.i1",
            ILOpCode.Ldelem_u1 => "ldelem.u1",
            ILOpCode.Ldelem_i2 => "ldelem.i2",
            ILOpCode.Ldelem_u2 => "ldelem.u2",
            ILOpCode.Ldelem_i4 => "ldelem.i4",
            ILOpCode.Ldelem_u4 => "ldelem.u4",
            ILOpCode.Ldelem_i8 => "ldelem.i8",
            ILOpCode.Ldelem_i => "ldelem.i",
            ILOpCode.Ldelem_r4 => "ldelem.r4",
            ILOpCode.Ldelem_r8 => "ldelem.r8",
            ILOpCode.Ldelem_ref => "ldelem.ref",
            ILOpCode.Ldelem => $"ldelem {FormatToken(reader.ReadInt32())}",
            ILOpCode.Ldelema => $"ldelema {FormatToken(reader.ReadInt32())}",

            // ===== Array element store =====
            ILOpCode.Stelem_i => "stelem.i",
            ILOpCode.Stelem_i1 => "stelem.i1",
            ILOpCode.Stelem_i2 => "stelem.i2",
            ILOpCode.Stelem_i4 => "stelem.i4",
            ILOpCode.Stelem_i8 => "stelem.i8",
            ILOpCode.Stelem_r4 => "stelem.r4",
            ILOpCode.Stelem_r8 => "stelem.r8",
            ILOpCode.Stelem_ref => "stelem.ref",
            ILOpCode.Stelem => $"stelem {FormatToken(reader.ReadInt32())}",

            // ===== Branches (short form) =====
            ILOpCode.Br_s => FormatBranchShort("br.s", ref reader),
            ILOpCode.Brfalse_s => FormatBranchShort("brfalse.s", ref reader),
            ILOpCode.Brtrue_s => FormatBranchShort("brtrue.s", ref reader),
            ILOpCode.Beq_s => FormatBranchShort("beq.s", ref reader),
            ILOpCode.Bne_un_s => FormatBranchShort("bne.un.s", ref reader),
            ILOpCode.Bge_s => FormatBranchShort("bge.s", ref reader),
            ILOpCode.Bge_un_s => FormatBranchShort("bge.un.s", ref reader),
            ILOpCode.Bgt_s => FormatBranchShort("bgt.s", ref reader),
            ILOpCode.Bgt_un_s => FormatBranchShort("bgt.un.s", ref reader),
            ILOpCode.Ble_s => FormatBranchShort("ble.s", ref reader),
            ILOpCode.Ble_un_s => FormatBranchShort("ble.un.s", ref reader),
            ILOpCode.Blt_s => FormatBranchShort("blt.s", ref reader),
            ILOpCode.Blt_un_s => FormatBranchShort("blt.un.s", ref reader),
            ILOpCode.Leave_s => FormatBranchShort("leave.s", ref reader),

            // ===== Branches (long form) =====
            ILOpCode.Br => FormatBranchLong("br", ref reader),
            ILOpCode.Brfalse => FormatBranchLong("brfalse", ref reader),
            ILOpCode.Brtrue => FormatBranchLong("brtrue", ref reader),
            ILOpCode.Beq => FormatBranchLong("beq", ref reader),
            ILOpCode.Bne_un => FormatBranchLong("bne.un", ref reader),
            ILOpCode.Bge => FormatBranchLong("bge", ref reader),
            ILOpCode.Bge_un => FormatBranchLong("bge.un", ref reader),
            ILOpCode.Bgt => FormatBranchLong("bgt", ref reader),
            ILOpCode.Bgt_un => FormatBranchLong("bgt.un", ref reader),
            ILOpCode.Ble => FormatBranchLong("ble", ref reader),
            ILOpCode.Ble_un => FormatBranchLong("ble.un", ref reader),
            ILOpCode.Blt => FormatBranchLong("blt", ref reader),
            ILOpCode.Blt_un => FormatBranchLong("blt.un", ref reader),
            ILOpCode.Leave => FormatBranchLong("leave", ref reader),

            // ===== Switch =====
            ILOpCode.Switch => FormatSwitch(ref reader),

            // ===== Method calls =====
            ILOpCode.Call => $"call {FormatToken(reader.ReadInt32())}",
            ILOpCode.Callvirt => $"callvirt {FormatToken(reader.ReadInt32())}",
            ILOpCode.Calli => $"calli {FormatToken(reader.ReadInt32())}", // TODO: signature formatting
            ILOpCode.Jmp => $"jmp {FormatToken(reader.ReadInt32())}",
            ILOpCode.Newobj => $"newobj {FormatToken(reader.ReadInt32())}",
            ILOpCode.Ldftn => $"ldftn {FormatToken(reader.ReadInt32())}",
            ILOpCode.Ldvirtftn => $"ldvirtftn {FormatToken(reader.ReadInt32())}",

            // ===== Field access =====
            ILOpCode.Ldfld => $"ldfld {FormatToken(reader.ReadInt32())}",
            ILOpCode.Ldflda => $"ldflda {FormatToken(reader.ReadInt32())}",
            ILOpCode.Stfld => $"stfld {FormatToken(reader.ReadInt32())}",
            ILOpCode.Ldsfld => $"ldsfld {FormatToken(reader.ReadInt32())}",
            ILOpCode.Ldsflda => $"ldsflda {FormatToken(reader.ReadInt32())}",
            ILOpCode.Stsfld => $"stsfld {FormatToken(reader.ReadInt32())}",

            // ===== Type operations =====
            ILOpCode.Ldstr => $"ldstr {FormatString(reader.ReadInt32())}",
            ILOpCode.Newarr => $"newarr {FormatToken(reader.ReadInt32())}",
            ILOpCode.Castclass => $"castclass {FormatToken(reader.ReadInt32())}",
            ILOpCode.Isinst => $"isinst {FormatToken(reader.ReadInt32())}",
            ILOpCode.Box => $"box {FormatToken(reader.ReadInt32())}",
            ILOpCode.Unbox => $"unbox {FormatToken(reader.ReadInt32())}",
            ILOpCode.Unbox_any => $"unbox.any {FormatToken(reader.ReadInt32())}",
            ILOpCode.Ldtoken => $"ldtoken {FormatToken(reader.ReadInt32())}",
            ILOpCode.Ldobj => $"ldobj {FormatToken(reader.ReadInt32())}",
            ILOpCode.Stobj => $"stobj {FormatToken(reader.ReadInt32())}",
            ILOpCode.Cpobj => $"cpobj {FormatToken(reader.ReadInt32())}",
            ILOpCode.Initobj => $"initobj {FormatToken(reader.ReadInt32())}",
            ILOpCode.Sizeof => $"sizeof {FormatToken(reader.ReadInt32())}",
            ILOpCode.Mkrefany => $"mkrefany {FormatToken(reader.ReadInt32())}",
            ILOpCode.Refanyval => $"refanyval {FormatToken(reader.ReadInt32())}",

            // ===== Prefix instructions =====
            ILOpCode.Constrained => $"constrained. {FormatToken(reader.ReadInt32())}",
            ILOpCode.Unaligned => $"unaligned. {reader.ReadByte()}",
            ILOpCode.Volatile => "volatile.",
            ILOpCode.Tail => "tail.",
            ILOpCode.Readonly => "readonly.",

            // ===== Default - should not happen if all opcodes are handled =====
            _ => $"/* unknown opcode 0x{opcode:X2} */"
        };
    }

    private static string FormatBranchShort(string mnemonic, ref BlobReader reader)
    {
        // Per ECMA-335 §III.1.7.1, branch offsets are relative to the beginning of the next instruction
        sbyte delta = reader.ReadSByte();
        int target = reader.Offset + delta;
        return $"{mnemonic} IL_{target:X4}";
    }

    private static string FormatBranchLong(string mnemonic, ref BlobReader reader)
    {
        // Per ECMA-335 §III.1.7.1, branch offsets are relative to the beginning of the next instruction
        int delta = reader.ReadInt32();
        int target = reader.Offset + delta;
        return $"{mnemonic} IL_{target:X4}";
    }

    private static string FormatSwitch(ref BlobReader reader)
    {
        // Per ECMA-335 §III.1.7.1, switch offsets are relative to the end of the switch instruction
        uint count = reader.ReadUInt32();

        // Read all the offsets first, then calculate targets based on position after the switch instruction
        int[] deltas = new int[count];
        for (uint i = 0; i < count; i++)
        {
            deltas[i] = reader.ReadInt32();
        }

        // reader.Offset is now at the end of the switch instruction (the "next instruction")
        int afterSwitch = reader.Offset;

        var sb = new StringBuilder();
        sb.Append("switch (");

        for (uint i = 0; i < count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }
            int target = afterSwitch + deltas[i];
            sb.Append($"IL_{target:X4}");
        }

        sb.Append(')');
        return sb.ToString();
    }

    private static string FormatToken(int token)
    {
        // TODO: Resolve TypeDef tokens to Namespace.TypeName
        // TODO: Resolve TypeRef tokens to [AssemblyName]Namespace.TypeName
        // TODO: Resolve TypeSpec tokens by decoding generic instantiation signatures
        // TODO: Resolve MethodDef tokens to ReturnType Namespace.Type::MethodName(ParamTypes)
        // TODO: Resolve MemberRef tokens to Type::MemberName with signature
        // TODO: Resolve MethodSpec tokens by decoding generic method instantiation
        // TODO: Resolve FieldDef tokens to FieldType Namespace.Type::FieldName
        // TODO: Resolve StandaloneSignature tokens for calli instruction
        // TODO: Use SignatureDecoder to decode type signatures
        // TODO: Handle primitive types, arrays, pointers, byrefs
        // TODO: Handle generic type arguments (T, !0, !!0)
        // TODO: Handle custom modifiers (modreq/modopt)
        // TODO: Support --tokens option to append raw token values
        var handle = MetadataTokens.EntityHandle(token);
        return $"/* {handle.Kind} 0x{token:X8} */";
    }

    private string FormatString(int token)
    {
        var handle = MetadataTokens.UserStringHandle(token);
        var str = _reader.GetUserString(handle);
        // Escape special characters
        return $"\"{EscapeString(str)}\"";
    }

    private static string EscapeString(string str)
    {
        var sb = new StringBuilder(str.Length);
        foreach (char c in str)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\0': sb.Append("\\0"); break;
                default:
                    if (c < 32 || c > 126)
                    {
                        sb.Append($"\\u{(int)c:X4}");
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }
}
