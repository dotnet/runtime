// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace SuperPmi.Mcs;

// Raw IL printing for -ildump. All the instructions of a method are printed
// on one line (only unknown opcodes end a line), short branch targets add the offset as an unsigned byte,
// and a few names are off (0x30 bgt.s, 0x3D bgt, 0xA3 stelem, leave.s as leave): the format of -ildump.
internal static partial class ILDumper
{
    private enum Operand : byte
    {
        None,
        Int8,
        Int16,
        Int32,
        Int64,
        Float32,
        Float64,
        Token,
        ShortBranch,
        Branch,
        Switch,
    }

    private static readonly (string Name, Operand Operand)[] s_opcodes = BuildTable(
    [
        (0x00, "nop", Operand.None),
        (0x01, "break", Operand.None),
        (0x02, "ldarg.0", Operand.None),
        (0x03, "ldarg.1", Operand.None),
        (0x04, "ldarg.2", Operand.None),
        (0x05, "ldarg.3", Operand.None),
        (0x06, "ldloc.0", Operand.None),
        (0x07, "ldloc.1", Operand.None),
        (0x08, "ldloc.2", Operand.None),
        (0x09, "ldloc.3", Operand.None),
        (0x0A, "stloc.0", Operand.None),
        (0x0B, "stloc.1", Operand.None),
        (0x0C, "stloc.2", Operand.None),
        (0x0D, "stloc.3", Operand.None),
        (0x0E, "ldarg.s", Operand.Int8),
        (0x0F, "ldarga.s", Operand.Int8),
        (0x10, "starg.s", Operand.Int8),
        (0x11, "ldloc.s", Operand.Int8),
        (0x12, "ldloca.s", Operand.Int8),
        (0x13, "stloc.s", Operand.Int8),
        (0x14, "ldnull", Operand.None),
        (0x15, "ldc.i4.m1", Operand.None),
        (0x16, "ldc.i4.0", Operand.None),
        (0x17, "ldc.i4.1", Operand.None),
        (0x18, "ldc.i4.2", Operand.None),
        (0x19, "ldc.i4.3", Operand.None),
        (0x1A, "ldc.i4.4", Operand.None),
        (0x1B, "ldc.i4.5", Operand.None),
        (0x1C, "ldc.i4.6", Operand.None),
        (0x1D, "ldc.i4.7", Operand.None),
        (0x1E, "ldc.i4.8", Operand.None),
        (0x1F, "ldc.i4.s", Operand.Int8),
        (0x20, "ldc.i4", Operand.Int32),
        (0x21, "ldc.i8", Operand.Int64),
        (0x22, "ldc.r4", Operand.Float32),
        (0x23, "ldc.r8", Operand.Float64),
        (0x25, "dup", Operand.None),
        (0x26, "pop", Operand.None),
        (0x27, "jmp", Operand.Token),
        (0x28, "call", Operand.Token),
        (0x29, "calli", Operand.Token),
        (0x2A, "ret", Operand.None),
        (0x2B, "br.s", Operand.ShortBranch),
        (0x2C, "brfalse.s", Operand.ShortBranch),
        (0x2D, "brtrue.s", Operand.ShortBranch),
        (0x2E, "beq.s", Operand.ShortBranch),
        (0x2F, "bgt.s", Operand.ShortBranch),
        (0x30, "bgt.s", Operand.ShortBranch),
        (0x31, "ble.s", Operand.ShortBranch),
        (0x32, "blt.s", Operand.ShortBranch),
        (0x33, "bne.un.s", Operand.ShortBranch),
        (0x34, "bge.un.s", Operand.ShortBranch),
        (0x35, "bgt.un.s", Operand.ShortBranch),
        (0x36, "ble.un.s", Operand.ShortBranch),
        (0x37, "blt.un.s", Operand.ShortBranch),
        (0x38, "br", Operand.Branch),
        (0x39, "brfalse", Operand.Branch),
        (0x3A, "brtrue", Operand.Branch),
        (0x3B, "beq", Operand.Branch),
        (0x3C, "bgt", Operand.Branch),
        (0x3D, "bgt", Operand.Branch),
        (0x3E, "ble", Operand.Branch),
        (0x3F, "blt", Operand.Branch),
        (0x40, "bne.un", Operand.Branch),
        (0x41, "bge.un", Operand.Branch),
        (0x42, "bgt.un", Operand.Branch),
        (0x43, "ble.un", Operand.Branch),
        (0x44, "blt.un", Operand.Branch),
        (0x45, "switch", Operand.Switch),
        (0x46, "ldind.i1", Operand.None),
        (0x47, "ldind.u1", Operand.None),
        (0x48, "ldind.i2", Operand.None),
        (0x49, "ldind.u2", Operand.None),
        (0x4A, "ldind.i4", Operand.None),
        (0x4B, "ldind.u4", Operand.None),
        (0x4C, "ldind.i8", Operand.None),
        (0x4D, "ldind.u8", Operand.None),
        (0x4E, "ldind.r4", Operand.None),
        (0x4F, "ldind.r8", Operand.None),
        (0x50, "ldind.ref", Operand.None),
        (0x51, "stind.ref", Operand.None),
        (0x52, "stind.i1", Operand.None),
        (0x53, "stind.i2", Operand.None),
        (0x54, "stind.i4", Operand.None),
        (0x55, "stind.i8", Operand.None),
        (0x56, "stind.r4", Operand.None),
        (0x57, "stind.r8", Operand.None),
        (0x58, "add", Operand.None),
        (0x59, "sub", Operand.None),
        (0x5A, "mul", Operand.None),
        (0x5B, "div", Operand.None),
        (0x5C, "div.un", Operand.None),
        (0x5D, "rem", Operand.None),
        (0x5E, "rem.un", Operand.None),
        (0x5F, "and", Operand.None),
        (0x60, "or", Operand.None),
        (0x61, "xor", Operand.None),
        (0x62, "shl", Operand.None),
        (0x63, "shr", Operand.None),
        (0x64, "shr.un", Operand.None),
        (0x65, "neg", Operand.None),
        (0x66, "not", Operand.None),
        (0x67, "conv.i1", Operand.None),
        (0x68, "conv.i2", Operand.None),
        (0x69, "conv.i4", Operand.None),
        (0x6A, "conv.i8", Operand.None),
        (0x6B, "conv.r4", Operand.None),
        (0x6C, "conv.r8", Operand.None),
        (0x6D, "conv.u4", Operand.None),
        (0x6E, "conv.u8", Operand.None),
        (0x6F, "callvirt", Operand.Token),
        (0x70, "cpobj", Operand.Token),
        (0x71, "ldobj", Operand.Token),
        (0x72, "ldstr", Operand.Token),
        (0x73, "newobj", Operand.Token),
        (0x74, "castclass", Operand.Token),
        (0x75, "isinst", Operand.Token),
        (0x76, "conv.r.un", Operand.None),
        (0x79, "unbox", Operand.Token),
        (0x7A, "throw", Operand.None),
        (0x7B, "ldfld", Operand.Token),
        (0x7C, "ldflda", Operand.Token),
        (0x7D, "stfld", Operand.Token),
        (0x7E, "ldsfld", Operand.Token),
        (0x7F, "ldsflda", Operand.Token),
        (0x80, "stsfld", Operand.Token),
        (0x81, "stobj", Operand.Token),
        (0x82, "conv.ovf.i1.un", Operand.None),
        (0x83, "conv.ovf.i2.un", Operand.None),
        (0x84, "conv.ovf.i4.un", Operand.None),
        (0x85, "conv.ovf.i8.un", Operand.None),
        (0x86, "conv.ovf.u1.un", Operand.None),
        (0x87, "conv.ovf.u2.un", Operand.None),
        (0x88, "conv.ovf.u4.un", Operand.None),
        (0x89, "conv.ovf.u8.un", Operand.None),
        (0x8A, "conv.ovf.i.un", Operand.None),
        (0x8B, "conv.ovf.u.un", Operand.None),
        (0x8C, "box", Operand.Token),
        (0x8D, "newarr", Operand.Token),
        (0x8E, "ldlen", Operand.None),
        (0x8F, "ldelema", Operand.Token),
        (0x90, "ldelem.i1", Operand.None),
        (0x91, "ldelem.u1", Operand.None),
        (0x92, "ldelem.i2", Operand.None),
        (0x93, "ldelem.u2", Operand.None),
        (0x94, "ldelem.i4", Operand.None),
        (0x95, "ldelem.u4", Operand.None),
        (0x96, "ldelem.i8", Operand.None),
        (0x97, "ldelem.i", Operand.None),
        (0x98, "ldelem.r4", Operand.None),
        (0x99, "ldelem.r8", Operand.None),
        (0x9A, "ldelem.ref", Operand.None),
        (0x9B, "stelem.i", Operand.None),
        (0x9C, "stelem.i1", Operand.None),
        (0x9D, "stelem.i2", Operand.None),
        (0x9E, "stelem.i4", Operand.None),
        (0x9F, "stelem.i8", Operand.None),
        (0xA0, "stelem.r4", Operand.None),
        (0xA1, "stelem.r8", Operand.None),
        (0xA2, "stelem.ref", Operand.None),
        (0xA3, "stelem", Operand.Token),
        (0xA4, "stelem", Operand.Token),
        (0xA5, "unbox.any", Operand.Token),
        (0xB3, "conv.ovf.i1", Operand.None),
        (0xB4, "conv.ovf.u1", Operand.None),
        (0xB5, "conv.ovf.i2", Operand.None),
        (0xB6, "conv.ovf.u2", Operand.None),
        (0xB7, "conv.ovf.i4", Operand.None),
        (0xB8, "conv.ovf.u4", Operand.None),
        (0xB9, "conv.ovf.i8", Operand.None),
        (0xBA, "conv.ovf.u8", Operand.None),
        (0xC2, "refanyval", Operand.Token),
        (0xC3, "ckfinite", Operand.None),
        (0xC6, "mkrefany", Operand.Token),
        (0xD0, "ldtoken", Operand.Token),
        (0xD1, "conv.u2", Operand.None),
        (0xD2, "conv.u1", Operand.None),
        (0xD3, "conv.i", Operand.None),
        (0xD4, "conv.ovf.i", Operand.None),
        (0xD5, "conv.ovf.u", Operand.None),
        (0xD6, "add.ovf", Operand.None),
        (0xD7, "add.ovf.un", Operand.None),
        (0xD8, "mul.ovf", Operand.None),
        (0xD9, "mul.ovf.un", Operand.None),
        (0xDA, "sub.ovf", Operand.None),
        (0xDB, "sub.ovf.un", Operand.None),
        (0xDC, "endfinally", Operand.None),
        (0xDD, "leave", Operand.Int32),
        (0xDE, "leave", Operand.Int8),
        (0xDF, "stind.i", Operand.None),
        (0xE0, "conv.u", Operand.None),
    ]);

    private static readonly (string Name, Operand Operand)[] s_fePrefixedOpcodes = BuildTable(
    [
        (0x00, "arglist", Operand.None),
        (0x01, "ceq", Operand.None),
        (0x02, "cgt", Operand.None),
        (0x03, "cgt.un", Operand.None),
        (0x04, "clt", Operand.None),
        (0x05, "clt.un", Operand.None),
        (0x06, "ldftn", Operand.Token),
        (0x07, "ldvirtftn", Operand.Token),
        (0x09, "ldarg", Operand.Int16),
        (0x0A, "ldarga", Operand.Int16),
        (0x0B, "starg", Operand.Int16),
        (0x0C, "ldloc", Operand.Int16),
        (0x0D, "ldloca", Operand.Int16),
        (0x0E, "stloc", Operand.Int16),
        (0x0F, "localloc", Operand.None),
        (0x11, "endfilter", Operand.None),
        (0x12, "unaligned.", Operand.Int8),
        (0x13, "volatile.", Operand.None),
        (0x14, "tail.", Operand.None),
        (0x15, "initobj", Operand.Token),
        (0x16, "constrained.", Operand.Token),
        (0x17, "cpblk", Operand.None),
        (0x18, "initblk", Operand.None),
        (0x19, "no.", Operand.None),
        (0x1A, "rethrow", Operand.None),
        (0x1C, "sizeof", Operand.Token),
        (0x1D, "refanytype", Operand.None),
    ]);

    private static (string, Operand)[] BuildTable((byte Opcode, string Name, Operand Operand)[] entries)
    {
        var table = new (string, Operand)[256];
        foreach ((byte opcode, string name, Operand operand) in entries)
            table[opcode] = (name, operand);
        return table;
    }

    public static void DumpILToConsoleBare(TextWriter output, byte[] ilCode, int len)
    {
        for (int i = 0; i < len; i++)
        {
            output.Write($"IL_{i:x4}: ");

            (string name, Operand operand) = s_opcodes[ilCode[i]];
            if (ilCode[i] == 0xFE)
            {
                i++;
                (name, operand) = s_fePrefixedOpcodes[ilCode[i]];
                if (name == null)
                {
                    Logger.LogError($"unknown ilCode 0xfe{ilCode[i],2:x} at offset {i} in MethodGen::PrettyPrint");
                    output.Write('\n');
                    continue;
                }
            }
            else if (name == null)
            {
                Logger.LogError($"unknown ilCode 0x{ilCode[i]:x2} at offset {i} in MethodGen::PrettyPrint");
                output.Write('\n');
                continue;
            }

            WriteInstruction(output, name, operand, ilCode, ref i);
        }
    }

    // Writes the instruction at 'i' and advances 'i' to its last byte.
    private static void WriteInstruction(TextWriter output, string name, Operand operand, byte[] ilCode, ref int i)
    {
        int at = i;
        switch (operand)
        {
            case Operand.None:
                output.Write(name);
                break;
            case Operand.Int8:
                output.Write($"{name} 0x{Hex(ilCode, at, 1)}");
                i += 1;
                break;
            case Operand.Int16:
                output.Write($"{name} 0x{Hex(ilCode, at, 2)}");
                i += 2;
                break;
            case Operand.Int32:
                output.Write($"{name} 0x{Hex(ilCode, at, 4)}");
                i += 4;
                break;
            case Operand.Int64:
                output.Write($"{name} 0x{Hex(ilCode, at, 8)}");
                i += 8;
                break;
            case Operand.Float32:
                output.Write($"{name} float32(0x{Hex(ilCode, at, 4)})");
                i += 4;
                break;
            case Operand.Float64:
                output.Write($"{name} float64(0x{Hex(ilCode, at, 8)})");
                i += 8;
                break;
            case Operand.Token:
                output.Write($"{name} <0x{Hex(ilCode, at, 4)}>");
                i += 4;
                break;
            case Operand.ShortBranch:
                output.Write($"{name} IL_{at + 2 + ilCode[at + 1]:x4}");
                i += 1;
                break;
            case Operand.Branch:
                output.Write($"{name} IL_{at + 5 + Int32At(ilCode, at):x4}");
                i += 4;
                break;
            case Operand.Switch:
                output.Write($"{name} (0x{Hex(ilCode, at, 4)})");
                int count = Int32At(ilCode, at);
                i += 4;
                for (int j = 0; j < count; j++)
                {
                    output.Write($" <0x{Hex(ilCode, i, 4)}>");
                    i += 4;
                }
                break;
        }
    }

    private static int Int32At(byte[] ilCode, int at)
        => (ilCode[at + 4] << 24) | (ilCode[at + 3] << 16) | (ilCode[at + 2] << 8) | ilCode[at + 1];

    // The 'count' operand bytes after 'at', most significant first.
    private static string Hex(byte[] ilCode, int at, int count)
    {
        Span<byte> bytes = stackalloc byte[count];
        for (int k = 1; k <= count; k++)
            bytes[count - k] = ilCode[at + k];
        return Convert.ToHexStringLower(bytes);
    }
}
