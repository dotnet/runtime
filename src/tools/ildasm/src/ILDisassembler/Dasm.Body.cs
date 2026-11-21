// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Metadata;
using System.Text;

namespace ILDisassembler;

// Method body disassembly: instructions, locals and EH clauses.
internal sealed partial class Dasm
{
    private const uint COR_ILEXCEPTION_CLAUSE_FILTER = 0x0001;
    private const uint COR_ILEXCEPTION_CLAUSE_FINALLY = 0x0002;
    private const uint COR_ILEXCEPTION_CLAUSE_FAULT = 0x0004;
    private const uint NEW_TRY_BLOCK = 0x80000000;
    private const uint PUT_INTO_CODE = 0x40000000;
    private const uint ERR_OUT_OF_CODE = 0x20000000;
    private const uint SEH_NEW_PUT_MASK = NEW_TRY_BLOCK | PUT_INTO_CODE | ERR_OUT_OF_CODE;

    private sealed class EHClause
    {
        public uint Flags;
        public uint TryOffset;
        public uint TryLength;
        public uint HandlerOffset;
        public uint HandlerLength;
        public uint ClassTokenOrFilterOffset;
        public bool IsFat;
    }

    // COR_ILMETHOD_DECODER
    private sealed class MethodBody
    {
        public uint Flags;
        public int MaxStack;
        public uint CodeSize;
        public int LocalVarSigTok;
        public byte[] Code = Array.Empty<byte>();
        public List<EHClause>? EH;
    }

    private const uint CorILMethod_InitLocals = 0x0010;

    private MethodBody? DecodeMethodBody(uint rva)
    {
        ReadOnlySpan<byte> data = SpanAtRva(rva);
        if (data.Length == 0)
        {
            return null;
        }

        MethodBody m = new();
        int p;
        byte b0 = data[0];
        if ((b0 & 0x3) == 0x2)
        {
            // tiny header
            m.CodeSize = (uint)(b0 >> 2);
            m.MaxStack = 8;
            m.Flags = 0x2;
            p = 1;
        }
        else if ((b0 & 0x3) == 0x3)
        {
            ushort flagsAndSize = (ushort)(data[0] | data[1] << 8);
            m.Flags = (uint)(flagsAndSize & 0x0FFF);
            int headerSize = (flagsAndSize >> 12) * 4;
            m.MaxStack = data[2] | data[3] << 8;
            m.CodeSize = ReadUInt32(data, 4);
            m.LocalVarSigTok = (int)ReadUInt32(data, 8);
            p = headerSize;
        }
        else
        {
            return null;
        }

        int codeLen = (int)Math.Min(m.CodeSize, (uint)Math.Max(0, data.Length - p));
        m.Code = data.Slice(p, codeLen).ToArray();

        if ((m.Flags & 0x8) != 0 && (b0 & 0x3) == 0x3)
        {
            // More sections follow the code, 4-byte aligned. Only the first EH section is used.
            int sect = (int)Math.Min(p + m.CodeSize, int.MaxValue - 8);
            while (true)
            {
                sect = (sect + 3) & ~3;
                if (sect >= data.Length)
                {
                    break;
                }

                byte kind = data[sect];
                bool fat = (kind & 0x40) != 0;
                bool more = (kind & 0x80) != 0;
                int dataSize = fat ? (data[sect + 1] | data[sect + 2] << 8 | data[sect + 3] << 16) : data[sect + 1];
                if ((kind & 0x3F) == 0x1)
                {
                    m.EH = new List<EHClause>();
                    int count = fat ? (dataSize - 4) / 24 : (dataSize - 4) / 12;
                    int c = sect + 4;
                    for (int i = 0; i < count; i++)
                    {
                        EHClause e = new() { IsFat = fat };
                        if (fat)
                        {
                            e.Flags = ReadUInt32(data, (int)c);
                            e.TryOffset = ReadUInt32(data, (int)c + 4);
                            e.TryLength = ReadUInt32(data, (int)c + 8);
                            e.HandlerOffset = ReadUInt32(data, (int)c + 12);
                            e.HandlerLength = ReadUInt32(data, (int)c + 16);
                            e.ClassTokenOrFilterOffset = ReadUInt32(data, (int)c + 20);
                            c += 24;
                        }
                        else
                        {
                            e.Flags = (uint)(data[c] | data[c + 1] << 8);
                            e.TryOffset = (uint)(data[c + 2] | data[c + 3] << 8);
                            e.TryLength = data[c + 4];
                            e.HandlerOffset = (uint)(data[c + 5] | data[c + 6] << 8);
                            e.HandlerLength = data[c + 7];
                            e.ClassTokenOrFilterOffset = ReadUInt32(data, (int)c + 8);
                            c += 12;
                        }

                        m.EH.Add(e);
                    }

                    break;
                }

                if (!more)
                {
                    break;
                }

                sect += dataSize;
            }
        }

        return m;
    }

    // ---------------------------------------------------------------- EH

    private List<EHClause> _ehInfo = new();

    private bool EnumEHInfo(MethodBody method)
    {
        _ehInfo = new List<EHClause>();
        bool tryInCode = false;
        if (method.EH is null || method.EH.Count == 0)
        {
            return false;
        }

        foreach (EHClause src in method.EH)
        {
            _ehInfo.Add(new EHClause
            {
                Flags = src.Flags & ~SEH_NEW_PUT_MASK,
                TryOffset = src.TryOffset,
                TryLength = src.TryLength,
                HandlerOffset = src.HandlerOffset,
                HandlerLength = src.HandlerLength,
                ClassTokenOrFilterOffset = src.ClassTokenOrFilterOffset,
                IsFat = src.IsFat,
            });
        }

        uint codeSize = method.CodeSize;
        tryInCode = !_options.RawExceptionHandling;
        foreach (EHClause e in _ehInfo)
        {
            if (e.TryOffset >= codeSize || e.TryOffset + e.TryLength >= codeSize
                || e.HandlerOffset >= codeSize || e.HandlerOffset + e.HandlerLength > codeSize)
            {
                e.Flags |= ERR_OUT_OF_CODE;
                tryInCode = false;
            }
        }

        if (tryInCode)
        {
            // qsort is not stable and the comparison has no further tie breaker, so equal clauses keep an unspecified order.
            EHClause[] sorted = _ehInfo.ToArray();
            QSort(sorted, static (a, b) =>
            {
                int d = (int)(a.TryOffset - b.TryOffset);
                if (d != 0)
                {
                    return d;
                }

                d = (int)(a.TryLength - b.TryLength);
                if (d != 0)
                {
                    return d;
                }

                d = (int)(a.HandlerOffset - b.HandlerOffset);
                if (d != 0)
                {
                    return d;
                }

                return (int)(a.HandlerLength - b.HandlerLength);
            });
            _ehInfo = new List<EHClause>(sorted);

            uint wasTryOffset = 0xFFFFFFFF, wasTryLength = 0xFFFFFFFF, lastOffset = 0xFFFFFFFF;
            int newTryBlock = 0;
            for (int i = 0; i < _ehInfo.Count; i++)
            {
                EHClause e = _ehInfo[i];
                if (e.TryOffset != wasTryOffset || e.TryLength != wasTryLength)
                {
                    e.Flags |= NEW_TRY_BLOCK;
                    lastOffset = e.TryOffset + e.TryLength;
                    wasTryOffset = e.TryOffset;
                    wasTryLength = e.TryLength;
                    newTryBlock = i;
                }

                uint filterOrHandler = (e.Flags & COR_ILEXCEPTION_CLAUSE_FILTER) != 0 ? e.ClassTokenOrFilterOffset : e.HandlerOffset;
                if (filterOrHandler == lastOffset)
                {
                    if (i == newTryBlock || (_ehInfo[newTryBlock].Flags & PUT_INTO_CODE) != 0)
                    {
                        e.Flags |= PUT_INTO_CODE;
                    }
                    else
                    {
                        e.Flags |= NEW_TRY_BLOCK;
                    }

                    lastOffset = e.HandlerOffset + e.HandlerLength;
                }
                else
                {
                    e.Flags |= NEW_TRY_BLOCK;
                }
            }
        }
        else
        {
            foreach (EHClause e in _ehInfo)
            {
                e.Flags |= NEW_TRY_BLOCK;
            }
        }

        return tryInCode;
    }

    // Reproduces the element order of the C runtime qsort on arrays with equal keys as closely as practical:
    // an insertion sort is stable, which matches glibc's merge sort based qsort for the small arrays seen here.
    private static void QSort<T>(T[] a, Comparison<T> cmp)
    {
        for (int i = 1; i < a.Length; i++)
        {
            T x = a[i];
            int j = i - 1;
            while (j >= 0 && cmp(a[j], x) > 0)
            {
                a[j + 1] = a[j];
                j--;
            }

            a[j + 1] = x;
        }
    }

    private void DumpHexEHInfo(EHClause e)
    {
        StringBuilder sb = new();
        sb.Append(Indent).Append("// HEX:");
        e.Flags &= ~SEH_NEW_PUT_MASK;
        if (e.IsFat)
        {
            foreach (uint v in new[] { e.Flags, e.TryOffset, e.TryLength, e.HandlerOffset, e.HandlerLength, e.ClassTokenOrFilterOffset })
            {
                AppendHexBytes(sb, v, 4);
            }
        }
        else
        {
            AppendHexBytes(sb, e.Flags, 2);
            AppendHexBytes(sb, e.TryOffset, 2);
            AppendHexBytes(sb, e.TryLength, 1);
            AppendHexBytes(sb, e.HandlerOffset, 2);
            AppendHexBytes(sb, e.HandlerLength, 1);
            AppendHexBytes(sb, e.ClassTokenOrFilterOffset, 4);
        }

        PrintLine(COMMENT(sb.ToString()));

        static void AppendHexBytes(StringBuilder sb, uint v, int n)
        {
            for (int i = 0; i < n; i++)
            {
                sb.Append(' ').Append(((byte)(v >> (8 * i))).ToString("X2", CultureInfo.InvariantCulture));
            }
        }
    }

    private void DumpOneEHInfo(EHClause e)
    {
        uint flags = e.Flags;
        if ((flags & PUT_INTO_CODE) != 0)
        {
            return;
        }

        StringBuilder sb = new();
        if ((flags & NEW_TRY_BLOCK) != 0)
        {
            sb.Append(Indent).Append(KEYWORD(".try")).Append(' ').Append(EHLabel(e.TryOffset)).Append(' ');
            sb.Append(KEYWORD("to")).Append(' ').Append(EHLabel(e.TryOffset + e.TryLength)).Append(' ');
        }
        else
        {
            sb.Append(Indent).Append("                        ");
        }

        if ((flags & COR_ILEXCEPTION_CLAUSE_FILTER) != 0)
        {
            sb.Append(KEYWORD("filter")).Append(' ').Append(EHLabel(e.ClassTokenOrFilterOffset)).Append(' ');
        }
        else if ((flags & COR_ILEXCEPTION_CLAUSE_FAULT) != 0)
        {
            sb.Append(KEYWORD("fault "));
        }
        else if ((flags & COR_ILEXCEPTION_CLAUSE_FINALLY) != 0 || IsNilToken((int)e.ClassTokenOrFilterOffset))
        {
            sb.Append(KEYWORD("finally "));
        }
        else
        {
            sb.Append(KEYWORD("catch")).Append(' ').Append(PrettyPrintClass((int)e.ClassTokenOrFilterOffset)).Append(' ');
        }

        sb.Append(KEYWORD("handler")).Append(' ').Append(EHLabel(e.HandlerOffset));
        if (e.HandlerLength != 0xFFFFFFFF)
        {
            sb.Append(' ').Append(KEYWORD("to")).Append(' ').Append(EHLabel(e.HandlerOffset + e.HandlerLength));
        }

        PrintLine(sb);
        if (_options.ShowBytes)
        {
            DumpHexEHInfo(e);
        }
    }

    private void DumpEHInfo()
    {
        if (_ehInfo.Count == 0)
        {
            return;
        }

        PrintLine(COMMENT($"{Indent}// Exception count {_ehInfo.Count}"));
        foreach (EHClause e in _ehInfo)
        {
            DumpOneEHInfo(e);
        }
    }

    // ---------------------------------------------------------------- locals

    private void DumpLocals(MethodBody method)
    {
        if (method.LocalVarSigTok == 0)
        {
            return;
        }

        int tok = method.LocalVarSigTok;
        if (TypeFromToken(tok) != mdtSignature || !IsValidToken(tok))
        {
            PrintLine("Invalid record");
            return;
        }

        byte[] sig = GetStandaloneSig(tok);
        StringBuilder sb = new();
        sb.Append(Indent).Append(KEYWORD(".locals "));
        if (_options.ShowTokens)
        {
            sb.Append(COMMENT($"/*{tok:X8}*/ "));
        }

        if ((method.Flags & CorILMethod_InitLocals) != 0)
        {
            sb.Append(KEYWORD("init "));
        }

        int dwL = sb.Length;
        string tail = PrettyPrintSig(sig, null, sb.ToString(), new SigLabel { Prefix = "V", StartIndex = 0 });
        if (tail.Length < dwL + 3)
        {
            return;
        }

        int indent = Indent.Length + 9; // indent + ".locals ("
        string ltn = LTN();
        string gtn = GTN();
        int pComma = tail.IndexOf(',');
        while (pComma >= 0)
        {
            int i = 0, j = 0, k = 0;
            for (int pch = 0; pch < pComma; pch++)
            {
                char c = tail[pch];
                if (c == '\'')
                {
                    j = 1 - j;
                }
                else if (j == 0)
                {
                    if (c == '[')
                    {
                        i++;
                    }
                    else if (c == ']')
                    {
                        i--;
                    }
                    else if (string.CompareOrdinal(tail, pch, ltn, 0, ltn.Length) == 0)
                    {
                        k++;
                    }
                    else if (string.CompareOrdinal(tail, pch, gtn, 0, gtn.Length) == 0)
                    {
                        k--;
                    }
                }
            }

            pComma++;
            if (i == 0 && k == 0 && j == 0)
            {
                PrintLine(tail.Substring(0, pComma));
                tail = new string(' ', indent) + tail.Substring(pComma);
                pComma = tail.IndexOf(',');
            }
            else
            {
                pComma = tail.IndexOf(',', pComma);
            }
        }

        PrintLine(tail);
    }

    // ---------------------------------------------------------------- the instruction loop

    private bool _thisIsInstanceMethod;

    // Offsets where instructions start: branch and EH targets elsewhere have no IL_xxxx label and print as raw offsets.
    private HashSet<uint> _instrStarts = new();
    private uint _codeSize;

    private const int CEE_NO = 0x100 + 0x19;

    // opcode.def has no 'no.' prefix (0xFE 0x19), which ilasm accepts as an instruction with a byte operand.
    private static OpcodeInfo GetOpcodeInfo(int instr)
        => instr == CEE_NO ? new OpcodeInfo("no.", OperandType.ShortInlineI) : OpcodeTable.Opcodes[instr];

    private static bool IsUndefinedOpcode(int instr)
        => instr == CEE_COUNT || (instr != CEE_NO && OpcodeTable.Opcodes[instr].Name is "unused" or "illegal" or "endmac" or "codelabel");

    private static HashSet<uint> ComputeInstructionStarts(byte[] code, uint codeSize)
    {
        HashSet<uint> starts = new();
        uint pc = 0;
        while (pc < codeSize && pc < code.Length)
        {
            int instr = DecodeOpcode(code, (int)pc, out int len);
            starts.Add(pc);
            if (IsUndefinedOpcode(instr))
            {
                pc += instr == CEE_COUNT && len == 3 ? 1u : (uint)len;
                continue;
            }

            pc += (uint)len;
            switch (GetOpcodeInfo(instr).Type)
            {
                case OperandType.ShortInlineVar:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineBrTarget:
                    pc += 1;
                    break;
                case OperandType.InlineVar:
                    pc += 2;
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    pc += 8;
                    break;
                case OperandType.InlineSwitch:
                    pc += pc + 4 <= code.Length ? 4 + 4 * ReadUInt32(code, (int)pc) : 4;
                    break;
                case OperandType.InlinePhi:
                    pc += pc < code.Length ? 1 + 2u * code[pc] : 1;
                    break;
                case OperandType.InlineNone:
                    break;
                default:
                    pc += 4;
                    break;
            }
        }

        return starts;
    }

    // The label of an EH boundary: IL_xxxx at an instruction start or at the end of the code (which gets a
    // label before the raw clauses), else the plain offset, which ilasm accepts too.
    private string EHLabel(uint offset)
        => _instrStarts.Contains(offset) || offset == _codeSize ? "IL_" + offset.ToString("x4", CultureInfo.InvariantCulture) : offset.ToString(CultureInfo.InvariantCulture);

    private const int CEE_COUNT = 294;
    private const int CEE_RET = 0x2A;
    private const int CEE_THROW = 0x7A;
    private const int CEE_LDSTR = 0x72;
    private const int CEE_LDARG_S = 0x0E;
    private const int CEE_LDARGA_S = 0x0F;
    private const int CEE_STARG_S = 0x10;
    private const int CEE_LDLOC_S = 0x11;
    private const int CEE_LDLOCA_S = 0x12;
    private const int CEE_STLOC_S = 0x13;
    private const int CEE_LDARG = 0x100 + 0x09;
    private const int CEE_LDARGA = 0x100 + 0x0A;
    private const int CEE_STARG = 0x100 + 0x0B;
    private const int CEE_LDLOC = 0x100 + 0x0C;
    private const int CEE_LDLOCA = 0x100 + 0x0D;
    private const int CEE_STLOC = 0x100 + 0x0E;

    private static int DecodeOpcode(byte[] code, int pc, out int len)
    {
        len = 1;
        int op = code[pc];
        switch (op)
        {
            case 0xFE:
                op = (pc + 1 < code.Length ? code[pc + 1] : 0) + 256;
                if (op >= CEE_COUNT)
                {
                    op = CEE_COUNT;
                }

                len = 2;
                break;
            case 0xFF:
            case 0xFD:
            case 0xFC:
            case 0xFB:
            case 0xFA:
            case 0xF9:
            case 0xF8:
                len = 3;
                return CEE_COUNT;
        }

        return op;
    }

    private bool Disassemble(MethodBody method, int funcToken, ParamDescriptor[]? argNames, int ulArgs)
    {
        byte[] code = method.Code;
        PrintLine($"{Indent}// Code size       {method.CodeSize} (0x{method.CodeSize:x})");
        if (method.CodeSize == 0)
        {
            return true;
        }

        PrintLine($"{Indent}{KEYWORD(".maxstack")}  {method.MaxStack}");

        DumpLocals(method);

        bool fNeedNewLine = false;
        uint pc = 0;
        _codeSize = method.CodeSize;
        _instrStarts = ComputeInstructionStarts(code, method.CodeSize);
        bool fTryInCode = EnumEHInfo(method);
        if (fTryInCode && !StructuredEHRoundTrips(method))
        {
            // The nested form would not reassemble to the same clauses (dotnet/runtime#4873): list them raw instead.
            _options.RawExceptionHandling = true;
            try
            {
                fTryInCode = EnumEHInfo(method);
            }
            finally
            {
                _options.RawExceptionHandling = _rawEHOption;
            }
        }

        uint codeSize = method.CodeSize;
        List<SequencePoint>? sequencePoints = GetSequencePoints(funcToken);
        int spIndex = 0;

        while (pc < codeSize)
        {
            if (pc >= code.Length)
            {
                // The body claims more code than the image holds.
                throw new IndexOutOfRangeException();
            }

            int instr = DecodeOpcode(code, (int)pc, out int len);
            if (IsUndefinedOpcode(instr))
            {
                // Undefined opcodes have no unambiguous mnemonic: emit their bytes so that the IL reassembles
                // exactly.
                if (instr == CEE_COUNT && len == 3)
                {
                    // A reserved prefix byte: only that byte is undefined.
                    len = 1;
                }

                if (fNeedNewLine)
                {
                    fNeedNewLine = false;
                    PrintLine("");
                }

                if (fTryInCode)
                {
                    DumpEHRegionsAt(pc);
                }

                StringBuilder raw = new();
                raw.Append(Indent).Append("IL_").Append(pc.ToString("x4", CultureInfo.InvariantCulture)).Append(":  ");
                for (int i = 0; i < len && pc + i < code.Length; i++)
                {
                    raw.Append(i == 0 ? "" : " ").Append(KEYWORD(".emitbyte")).Append(" 0x").Append(code[pc + i].ToString("x2", CultureInfo.InvariantCulture));
                }

                PrintLine(raw);
                pc += (uint)len;
                continue;
            }

            if (fNeedNewLine)
            {
                fNeedNewLine = false;
                PrintLine("");
            }

            // Sequence points that are not at an instruction start are skipped rather than stalling the rest.
            while (sequencePoints is not null && spIndex < sequencePoints.Count && sequencePoints[spIndex].Offset <= pc)
            {
                if (sequencePoints[spIndex].Offset == pc)
                {
                    DumpSourceLine(sequencePoints[spIndex]);
                }

                spIndex++;
            }

            if (fTryInCode)
            {
                DumpEHRegionsAt(pc);
            }

            StringBuilder sb = new();
            sb.Append(Indent).Append("IL_").Append(pc.ToString("x4", CultureInfo.InvariantCulture)).Append(":  ");
            if (_options.ShowBytes)
            {
                sb.Append(CommentPrefix).Append("/* ");
                int i;
                for (i = 0; i < len; i++)
                {
                    sb.Append(code[pc + i].ToString("X2", CultureInfo.InvariantCulture));
                }

                for (; i < 2; i++)
                {
                    sb.Append("  ");
                }

                sb.Append(" | ");
            }

            pc += (uint)len;
            len = 0;

            OpcodeInfo info = GetOpcodeInfo(instr);
            string name = KEYWORD(info.Name);

            void Pad()
            {
                if (len < 16)
                {
                    sb.Append(' ', 16 - len);
                }

                sb.Append(" */").Append(CommentPostfix).Append(' ');
            }

            string B(uint off) => code[off].ToString("X2", CultureInfo.InvariantCulture);

            switch (info.Type)
            {
                case OperandType.InlineNone:
                    if (_options.ShowBytes)
                    {
                        Pad();
                    }

                    sb.Append(name);
                    if (instr == CEE_RET || instr == CEE_THROW)
                    {
                        fNeedNewLine = true;
                    }

                    break;

                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                {
                    byte ch = code[pc];
                    int sh = info.Type == OperandType.ShortInlineVar ? ch : (sbyte)ch;
                    if (_options.ShowBytes)
                    {
                        sb.Append(B(pc)).Append(' ');
                        len += 3;
                        Pad();
                    }

                    switch (instr)
                    {
                        case CEE_LDARG_S:
                        case CEE_LDARGA_S:
                        case CEE_STARG_S:
                            AppendArg(sb, name, ch, argNames, ulArgs);
                            break;
                        case CEE_LDLOC_S:
                        case CEE_LDLOCA_S:
                        case CEE_STLOC_S:
                            sb.Append(Pad10(name)).Append(" V_").Append(ch.ToString(CultureInfo.InvariantCulture));
                            break;
                        default:
                            sb.Append(Pad10(name)).Append(' ').Append(sh.ToString(CultureInfo.InvariantCulture));
                            break;
                    }

                    pc++;
                    break;
                }

                case OperandType.InlineVar:
                {
                    if (_options.ShowBytes)
                    {
                        sb.Append(B(pc + 1)).Append(B(pc)).Append(' ');
                        len += 5;
                        Pad();
                    }

                    ushort v = (ushort)(code[pc] | code[pc + 1] << 8);
                    switch (instr)
                    {
                        case CEE_LDARGA:
                        case CEE_LDARG:
                        case CEE_STARG:
                            AppendArg(sb, name, v, argNames, ulArgs);
                            break;
                        case CEE_LDLOCA:
                        case CEE_LDLOC:
                        case CEE_STLOC:
                            sb.Append(Pad10(name)).Append(" V_").Append(v.ToString(CultureInfo.InvariantCulture));
                            break;
                        default:
                            sb.Append(Pad10(name)).Append(' ').Append(v.ToString(CultureInfo.InvariantCulture));
                            break;
                    }

                    pc += 2;
                    break;
                }

                case OperandType.InlineI:
                case OperandType.InlineRVA:
                {
                    uint v = ReadUInt32(code, (int)pc);
                    if (_options.ShowBytes)
                    {
                        sb.Append(B(pc + 3)).Append(B(pc + 2)).Append(B(pc + 1)).Append(B(pc)).Append(' ');
                        len += 9;
                        Pad();
                    }

                    sb.Append(Pad10(name)).Append(" 0x").Append(v.ToString("x", CultureInfo.InvariantCulture));
                    pc += 4;
                    break;
                }

                case OperandType.InlineI8:
                {
                    ulong v = BitConverter.ToUInt64(code, (int)pc);
                    if (_options.ShowBytes)
                    {
                        for (int i = 7; i >= 0; i--)
                        {
                            sb.Append(B(pc + (uint)i));
                        }

                        len += 16;
                        Pad();
                    }

                    sb.Append(Pad10(name)).Append(" 0x").Append(v.ToString("x", CultureInfo.InvariantCulture));
                    pc += 8;
                    break;
                }

                case OperandType.ShortInlineR:
                {
                    int v = BitConverter.ToInt32(code, (int)pc);
                    float f = BitConverter.Int32BitsToSingle(v);
                    if (_options.ShowBytes)
                    {
                        sb.Append(B(pc + 3)).Append(B(pc + 2)).Append(B(pc + 1)).Append(B(pc)).Append(' ');
                        len += 9;
                        Pad();
                    }

                    string szf = f == 0.0f ? ((v >> 24) == 0 ? "0.0" : "-0.0") : FormatG(f, 8);
                    float fd = (float)Atof(szf);
                    if (BitConverter.SingleToInt32Bits(fd) == v && !IsSpecialNumber(szf))
                    {
                        sb.Append(Pad10(name)).Append(' ').Append(szf);
                    }
                    else
                    {
                        sb.Append(Pad10(name)).Append(" (").Append(B(pc)).Append(' ').Append(B(pc + 1)).Append(' ').Append(B(pc + 2)).Append(' ').Append(B(pc + 3)).Append(')');
                    }

                    pc += 4;
                    break;
                }

                case OperandType.InlineR:
                {
                    long v = BitConverter.ToInt64(code, (int)pc);
                    double d = BitConverter.Int64BitsToDouble(v);
                    if (_options.ShowBytes)
                    {
                        for (int i = 7; i >= 0; i--)
                        {
                            sb.Append(B(pc + (uint)i));
                        }

                        len += 16;
                        Pad();
                    }

                    string szf = d == 0.0 ? ((v >> 56) == 0 ? "0.0" : "-0.0") : FormatG(d, 17);
                    double df = Atof(szf);
                    if (BitConverter.DoubleToInt64Bits(df) == v && !IsSpecialNumber(szf))
                    {
                        sb.Append(Pad10(name)).Append(' ').Append(szf);
                    }
                    else
                    {
                        sb.Append(Pad10(name)).Append(" (");
                        for (int i = 0; i < 8; i++)
                        {
                            if (i != 0)
                            {
                                sb.Append(' ');
                            }

                            sb.Append(B(pc + (uint)i));
                        }

                        sb.Append(')');
                    }

                    pc += 8;
                    break;
                }

                case OperandType.ShortInlineBrTarget:
                {
                    sbyte offset = (sbyte)code[pc];
                    long dest = pc + 1 + offset;
                    if (_options.ShowBytes)
                    {
                        sb.Append(B(pc)).Append(' ');
                        len += 3;
                        Pad();
                    }

                    pc++;
                    AppendBranchTarget(sb, name, dest, offset);
                    fNeedNewLine = true;
                    break;
                }

                case OperandType.InlineBrTarget:
                {
                    int offset = BitConverter.ToInt32(code, (int)pc);
                    long dest = pc + 4 + offset;
                    if (_options.ShowBytes)
                    {
                        sb.Append(B(pc + 3)).Append(B(pc + 2)).Append(B(pc + 1)).Append(B(pc)).Append(' ');
                        len += 9;
                        Pad();
                    }

                    pc += 4;
                    AppendBranchTarget(sb, name, dest, offset);
                    fNeedNewLine = true;
                    break;
                }

                case OperandType.InlineSwitch:
                {
                    uint cases = ReadUInt32(code, (int)pc);
                    if (_options.ShowBytes)
                    {
                        sb.Append(B(pc + 3)).Append(B(pc + 2)).Append(B(pc + 1)).Append(B(pc)).Append(' ');
                        len += 9;
                        Pad();
                    }

                    sb.Append(Pad10(name)).Append(cases != 0 ? " ( " : " ( )");
                    PrintLine(sb);
                    pc += 4;
                    uint next = pc + 4 * cases;
                    for (uint i = 0; i < cases; i++)
                    {
                        int offset = BitConverter.ToInt32(code, (int)pc);
                        long dest = next + offset;
                        sb.Clear();
                        sb.Append(Indent).Append("          ");
                        if (_options.ShowBytes)
                        {
                            sb.Append("/*      | ").Append(B(pc + 3)).Append(B(pc + 2)).Append(B(pc + 1)).Append(B(pc)).Append(' ');
                            len = 9;
                            Pad();
                        }

                        sb.Append("            ").Append(dest >= 0 && _instrStarts.Contains((uint)dest)
                            ? "IL_" + ((uint)dest).ToString("x4", CultureInfo.InvariantCulture)
                            : offset.ToString(CultureInfo.InvariantCulture)).Append(i == cases - 1 ? ")" : ",");
                        pc += 4;
                        PrintLine(sb);
                    }

                    continue;
                }

                case OperandType.InlinePhi:
                {
                    int cases = code[pc];
                    if (_options.ShowBytes)
                    {
                        sb.Append(cases.ToString("X2", CultureInfo.InvariantCulture));
                        len += 2;
                        for (int i = cases * 2; i > 0; i--)
                        {
                            sb.Append(B(pc + (uint)i));
                            len += 2;
                        }

                        Pad();
                    }

                    sb.Append(Pad10(name));
                    for (int i = 0; i < cases; i++)
                    {
                        sb.Append(' ').Append(((short)(code[pc + 1 + 2 * i] | code[pc + 2 + 2 * i] << 8)).ToString(CultureInfo.InvariantCulture));
                    }

                    pc += (uint)(2 * cases + 1);
                    break;
                }

                case OperandType.InlineString:
                case OperandType.InlineField:
                case OperandType.InlineType:
                case OperandType.InlineTok:
                case OperandType.InlineMethod:
                {
                    int tk = BitConverter.ToInt32(code, (int)pc);
                    int tkType = TypeFromToken(tk);
                    if (instr == CEE_LDSTR && tkType != mdtString)
                    {
                        // Backwards compatible ldstr instruction: the operand is the RVA of a zero terminated UTF-16 string.
                        if (_options.ShowBytes)
                        {
                            sb.Append(B(pc + 3)).Append(B(pc + 2)).Append(B(pc + 1)).Append(B(pc)).Append(' ');
                            len += 9;
                            Pad();
                        }

                        byte[]? str = GetBytesAtRva((uint)tk, -1);
                        if (str is null)
                        {
                            PrintLine($"// Error: no section header for RVA 0x{tk:x}, defaulting to empty string");
                        }

                        sb.Append(Pad10(name)).Append(' ').Append(ConvToLiteral(str ?? new byte[2]));
                        pc += 4;
                        break;
                    }

                    if (_options.ShowBytes)
                    {
                        sb.Append('(').Append(B(pc + 3)).Append(')').Append(B(pc + 2)).Append(B(pc + 1)).Append(B(pc)).Append(' ');
                        len += 11;
                        Pad();
                    }

                    pc += 4;
                    sb.Append(Pad10(name)).Append(' ');
                    if (((uint)tk & 0xFF000000) == 0)
                    {
                        sb.Append(tk == 0 ? "0" : "0x" + tk.ToString("x", CultureInfo.InvariantCulture)).Append(' ');
                        break;
                    }

                    if (!IsValidToken(tk))
                    {
                        sb.Append(ERRORMSG($" [ERROR: INVALID TOKEN 0x{tk:X8}] "));
                        break;
                    }

                    if (info.Type == OperandType.InlineTok)
                    {
                        switch (tkType)
                        {
                            case mdtMethodDef:
                            case mdtMethodSpec:
                                sb.Append(KEYWORD("method "));
                                break;
                            case mdtFieldDef:
                                sb.Append(KEYWORD("field "));
                                break;
                            case mdtMemberRef:
                                sb.Append(KEYWORD(IsFieldMemberRef(tk) ? "field " : "method "));
                                break;
                        }
                    }

                    PrettyPrintToken(sb, tk);
                    break;
                }

                case OperandType.InlineSig:
                {
                    if (_options.ShowBytes)
                    {
                        sb.Append(B(pc + 3)).Append(B(pc + 2)).Append(B(pc + 1)).Append(B(pc)).Append(' ');
                        len += 9;
                        Pad();
                    }

                    int tk = BitConverter.ToInt32(code, (int)pc);
                    pc += 4;
                    if (TypeFromToken(tk) == mdtSignature && IsValidToken(tk))
                    {
                        sb.Append(Pad10(name)).Append(' ').Append(PrettyPrintSig(GetStandaloneSig(tk), ""));
                        if (_options.ShowTokens)
                        {
                            sb.Append(COMMENT($" /*{tk:X8}*/"));
                        }
                    }
                    else if (TypeFromToken(tk) == mdtSignature)
                    {
                        sb.Clear();
                        sb.Append(COMMENT($"// ERROR: Invalid {tk:X8} record"));
                    }
                    else
                    {
                        sb.Append(ERRORMSG($"{Pad10(name)} 0x{tk:X8} // ERROR: invalid token type"));
                    }

                    break;
                }

                default:
                    sb.Append($"ERROR: Unknown type 0x{(int)info.Type:X} of instruction 0x{instr:X}");
                    PrintLine(sb);
                    return false;
            }

            PrintLine(sb);
        }

        // Close anything that may still be open and dump the EH regions that could not be put in the code.
        if (_ehInfo.Count != 0)
        {
            if (fTryInCode)
            {
                foreach (EHClause e in _ehInfo)
                {
                    uint theEnd = e.HandlerOffset + e.HandlerLength;
                    if ((e.Flags & PUT_INTO_CODE) != 0 && pc == theEnd)
                    {
                        PopIndent();
                        PrintLine($"{Indent}{UNSCOPE()}  {COMMENT("// end handler")}");
                        if (_options.ShowBytes)
                        {
                            DumpHexEHInfo(e);
                        }

                        e.TryOffset = 0xFF000000;
                        e.HandlerOffset = 0xFF000000;
                    }
                }
            }

            bool more = false;
            foreach (EHClause e in _ehInfo)
            {
                if (!(e.TryOffset == 0xFF000000 && e.HandlerOffset == 0xFF000000))
                {
                    more = true;
                    break;
                }
            }

            if (more)
            {
                PrintLine($"{Indent}IL_{codeSize:x4}:  ");
                DumpEHInfo();
            }
        }

        return true;
    }

    private void DumpEHRegionsAt(uint pc)
    {
        for (int i = 0; i < _ehInfo.Count; i++)
        {
            EHClause e = _ehInfo[i];
            uint theEnd = e.HandlerOffset + e.HandlerLength;
            if ((e.Flags & PUT_INTO_CODE) != 0 && pc == theEnd)
            {
                PopIndent();
                PrintLine($"{Indent}{UNSCOPE()}  {COMMENT("// end handler")}");
                if (_options.ShowBytes)
                {
                    DumpHexEHInfo(e);
                }

                e.TryOffset = 0xFF000000;
                e.HandlerOffset = 0xFF000000;
            }
        }

        for (int i = 0; i < _ehInfo.Count; i++)
        {
            EHClause e = _ehInfo[i];
            if ((e.Flags & PUT_INTO_CODE) == 0)
            {
                continue;
            }

            if ((e.Flags & NEW_TRY_BLOCK) != 0)
            {
                if (pc == e.TryOffset + e.TryLength)
                {
                    PopIndent();
                    PrintLine($"{Indent}{UNSCOPE()}  {COMMENT("// end .try")}");
                }

                if (pc == e.TryOffset)
                {
                    PrintLine(Indent + KEYWORD(".try"));
                    PrintLine(Indent + SCOPE());
                    PushIndent();
                }
            }

            uint flags = e.Flags;
            if ((flags & COR_ILEXCEPTION_CLAUSE_FILTER) != 0 && pc == e.ClassTokenOrFilterOffset)
            {
                PrintLine(Indent + KEYWORD("filter"));
                PrintLine(Indent + SCOPE());
                PushIndent();
            }

            if (pc == e.HandlerOffset)
            {
                if ((flags & COR_ILEXCEPTION_CLAUSE_FILTER) != 0)
                {
                    PopIndent();
                    PrintLine($"{Indent}{UNSCOPE()}  {COMMENT("// end filter")}");
                    PrintLine($"{Indent}{SCOPE()}  {COMMENT("// handler")}");
                    PushIndent();
                }
                else
                {
                    if ((flags & COR_ILEXCEPTION_CLAUSE_FAULT) != 0)
                    {
                        PrintLine(Indent + KEYWORD("fault"));
                    }
                    else if ((flags & COR_ILEXCEPTION_CLAUSE_FINALLY) != 0 || IsNilToken((int)e.ClassTokenOrFilterOffset))
                    {
                        PrintLine(Indent + KEYWORD("finally"));
                    }
                    else
                    {
                        PrintLine($"{Indent}{KEYWORD("catch")} {PrettyPrintClass((int)e.ClassTokenOrFilterOffset)} ");
                    }

                    PrintLine(Indent + SCOPE());
                    PushIndent();
                }
            }
        }
    }

    // A branch to an instruction start uses its label; any other target (dotnet/runtime#38506) the raw relative offset.
    private void AppendBranchTarget(StringBuilder sb, string name, long dest, int offset)
    {
        sb.Append(Pad10(name)).Append(' ');
        if (dest >= 0 && _instrStarts.Contains((uint)dest))
        {
            sb.Append("IL_").Append(((uint)dest).ToString("x4", CultureInfo.InvariantCulture));
        }
        else
        {
            sb.Append(offset.ToString(CultureInfo.InvariantCulture));
        }
    }

    // Whether the nested .try/handler form reproduces the clause table: every boundary of a clause put in the code
    // must be an instruction start (or the end of the code), and ilasm, which adds a clause when its handler block
    // closes, must get the clauses in their original order.
    private bool StructuredEHRoundTrips(MethodBody method)
    {
        List<(EHClause Clause, int Index)> inCode = new();
        List<EHClause> raw = new();
        for (int i = 0; i < _ehInfo.Count; i++)
        {
            EHClause e = _ehInfo[i];
            if ((e.Flags & PUT_INTO_CODE) == 0)
            {
                raw.Add(e);
                continue;
            }

            bool Boundary(uint o) => _instrStarts.Contains(o) || o == method.CodeSize;
            if (!Boundary(e.TryOffset) || !Boundary(e.TryOffset + e.TryLength) || !Boundary(e.HandlerOffset)
                || !Boundary(e.HandlerOffset + e.HandlerLength)
                || ((e.Flags & COR_ILEXCEPTION_CLAUSE_FILTER) != 0 && !Boundary(e.ClassTokenOrFilterOffset)))
            {
                return false;
            }

            inCode.Add((e, i));
        }

        // Handlers close in order of their end offset. Braces closing at the same offset are matched by ilasm with the
        // innermost open blocks first, whatever order the comments suggest: the block opened last closes first.
        static uint OpenOffset(EHClause e) => (e.Flags & COR_ILEXCEPTION_CLAUSE_FILTER) != 0 ? e.ClassTokenOrFilterOffset : e.HandlerOffset;
        inCode.Sort((a, b) =>
        {
            int c = (a.Clause.HandlerOffset + a.Clause.HandlerLength).CompareTo(b.Clause.HandlerOffset + b.Clause.HandlerLength);
            if (c != 0)
            {
                return c;
            }

            c = OpenOffset(b.Clause).CompareTo(OpenOffset(a.Clause));
            return c != 0 ? c : b.Index.CompareTo(a.Index);
        });

        List<EHClause> emitted = new(inCode.Count + raw.Count);
        foreach ((EHClause c, _) in inCode)
        {
            emitted.Add(c);
        }

        emitted.AddRange(raw);
        List<EHClause> original = method.EH!;
        for (int i = 0; i < original.Count; i++)
        {
            EHClause a = original[i];
            EHClause b = emitted[i];
            uint mask = ~SEH_NEW_PUT_MASK & 0x7;
            uint classOrFilterA = a.ClassTokenOrFilterOffset;
            uint classOrFilterB = b.ClassTokenOrFilterOffset;
            if ((a.Flags & mask) != (b.Flags & mask) || a.TryOffset != b.TryOffset || a.TryLength != b.TryLength
                || a.HandlerOffset != b.HandlerOffset || a.HandlerLength != b.HandlerLength || classOrFilterA != classOrFilterB)
            {
                return false;
            }
        }

        return true;
    }

    private void AppendArg(StringBuilder sb, string name, int ch, ParamDescriptor[]? argNames, int ulArgs)
    {
        if (_thisIsInstanceMethod && ch == 0)
        {
            sb.Append(Pad10(name)).Append(' ').Append(ch.ToString(CultureInfo.InvariantCulture));
        }
        else if (argNames is not null)
        {
            int ch1 = _thisIsInstanceMethod ? ch - 1 : ch;
            if (_thisIsInstanceMethod && ch1 < 0)
            {
                ch1 = (name.Length > 0 && ch1 < 0) ? int.MaxValue : ch1;
            }

            if (ch1 < ulArgs)
            {
                sb.Append(Pad10(name)).Append(' ').Append(argNames[ch1].Name is { } n ? ProperLocalName(n) : "(null)");
            }
            else
            {
                sb.Append(ERRORMSG($"{Pad10(name)} {ch} // ERROR: invalid arg index (>={ulArgs})"));
            }
        }
        else
        {
            sb.Append(Pad10(name)).Append(" A_").Append(ch.ToString(CultureInfo.InvariantCulture));
        }
    }

    // ConvToLiteral: quoted string for the old style ldstr operand, or "" if it holds characters it cannot print.
    private static string ConvToLiteral(byte[] utf16z)
    {
        StringBuilder sb = new();
        sb.Append('"');
        for (int i = 0; i + 1 < utf16z.Length && i < 0xFFFF * 2; i += 2)
        {
            char c = (char)(utf16z[i] | utf16z[i + 1] << 8);
            if (c == 0)
            {
                break;
            }

            if (c > 255)
            {
                return "";
            }

            if (!IsPrint(c))
            {
                if (c == '\n')
                {
                    sb.Append("\\n");
                    continue;
                }

                return "";
            }

            if (c == '"')
            {
                sb.Append("\\\"");
            }
            else if (c == '\\')
            {
                sb.Append("\\\\");
            }
            else
            {
                sb.Append(c);
            }
        }

        sb.Append('"');
        return sb.ToString();
    }
}
