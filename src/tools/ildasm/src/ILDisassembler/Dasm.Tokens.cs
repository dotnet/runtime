// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Text;

namespace ILDisassembler;

// Token pretty printing (PrettyPrintToken and friends in the native dis.cpp).
internal sealed partial class Dasm
{
    // .typedef aliases encoded by ilasm as ELEMENT_TYPE_TYPEDEF TypeSpecs.
    private sealed class TypeDefDescr
    {
        public string Name = "";
        public int TkTypeSpec;
        public int TkSelf;
        public byte[]? Sig;
    }

    private TypeDefDescr? FindTypedefFor(int tk)
    {
        foreach (TypeDefDescr td in _typedefs)
        {
            if (td.TkTypeSpec == tk)
            {
                return td;
            }
        }

        return null;
    }

    // IsGenericInst: whether a TypeSpec is a (possibly decorated) generic instantiation.
    private bool IsGenericInst(int tk)
    {
        if (TypeFromToken(tk) != mdtTypeSpec || !IsValidToken(tk))
        {
            return false;
        }

        byte[] sig = GetTypeSpecSig(tk);
        foreach (byte b in sig)
        {
            switch (b)
            {
                case ELEMENT_TYPE_PTR:
                case ELEMENT_TYPE_SZARRAY:
                case ELEMENT_TYPE_ARRAY:
                case ELEMENT_TYPE_BYREF:
                    continue;
                default:
                    return b == ELEMENT_TYPE_GENERICINST;
            }
        }

        return false;
    }

    private static bool IsMdPrivateScope(MethodAttributes a) => (a & MethodAttributes.MemberAccessMask) == MethodAttributes.PrivateScope;

    private static bool IsFdPrivateScope(FieldAttributes a) => (a & FieldAttributes.FieldAccessMask) == FieldAttributes.PrivateScope;

    // Returns the full line (prefix + signature) and, in indent, the byte column just after the opening parenthesis.
    private string PrettyPrintMemberRef(string prefix, int tk, byte[]? instSig, out int indent)
    {
        int savedVarOwner = _tkVarOwner;
        string memberName;
        byte[] sig;
        int cr;
        if (IsValidToken(tk))
        {
            (memberName, sig, cr) = GetMemberRefProps(tk);
        }
        else
        {
            (memberName, sig, cr) = ("Invalid MemberRef record", Array.Empty<byte>(), mdTypeRefNil);
        }

        memberName = MakeNameIfNone(memberName, tk);
        MethodAttributes dwAttrs = (MethodAttributes)0xFFFF;
        int tkMethodDef = 0;
        StringBuilder curPos = new();
        if (TypeFromToken(cr) == mdtMethodDef)
        {
            dwAttrs = GetMethodDefAttrs(cr);
            tkMethodDef = cr;
            cr = GetParentOfMethodDef(cr);
        }

        if (RidFromToken(cr) != 0 && cr != 0x02000001)
        {
            curPos.Append(PrettyPrintClass(cr)).Append("::");
            if (IsGenericInst(cr))
            {
                _tkVarOwner = 0;
            }
        }

        string namePart = tkMethodDef != 0 && IsMdPrivateScope(dwAttrs) ? memberName + "$PST" + X8(tkMethodDef) : memberName;
        curPos.Append(tkMethodDef != 0 ? JUMPPT(ProperName(namePart), tkMethodDef) : ProperName(namePart));

        if (instSig is not null)
        {
            curPos.Append(PrettyPrintSig(instSig, curPos.ToString()));
        }

        string cur = curPos.ToString();
        string tail = PrettyPrintSig(sig, cur, prefix, printTyArity: instSig is null);
        indent = ComputeIndent(tail, cur);
        _tkVarOwner = savedVarOwner;
        return tail;
    }

    private string PrettyPrintMethodDef(string prefix, int tk, byte[]? instSig, out int indent)
    {
        int savedVarOwner = _tkVarOwner;
        string memberName = MakeNameIfNone(GetNameOfMethodDef(tk), tk);
        MethodAttributes dwAttrs = GetMethodDefAttrs(tk);
        byte[] sig = GetSigOfMethodDef(tk);
        int cr = GetParentOfMethodDef(tk);
        StringBuilder curPos = new();
        if (RidFromToken(cr) != 0 && cr != 0x02000001)
        {
            curPos.Append(PrettyPrintClass(cr)).Append("::");
            if (IsGenericInst(cr))
            {
                _tkVarOwner = 0;
            }
        }

        string namePart = IsMdPrivateScope(dwAttrs) ? memberName + "$PST" + X8(tk) : memberName;
        curPos.Append(JUMPPT(ProperName(namePart), tk));

        if (instSig is not null)
        {
            curPos.Append(PrettyPrintSig(instSig, curPos.ToString()));
        }

        string cur = curPos.ToString();
        string tail = PrettyPrintSig(sig, cur, prefix, printTyArity: instSig is null);
        indent = ComputeIndent(tail, cur);
        _tkVarOwner = savedVarOwner;
        return tail;
    }

    private static int ComputeIndent(string tail, string cur)
    {
        int pos = tail.IndexOf(cur, StringComparison.Ordinal);
        if (pos < 0)
        {
            pos = 0;
        }

        return Encoding.UTF8.GetByteCount(tail.AsSpan(0, pos)) + Encoding.UTF8.GetByteCount(cur) + 1;
    }

    private static int ByteOffsetToCharIndex(string s, int byteOffset)
    {
        int bytes = 0;
        for (int i = 0; i < s.Length; i++)
        {
            if (bytes >= byteOffset)
            {
                return i;
            }

            int cp = s[i];
            if (char.IsHighSurrogate(s[i]) && i + 1 < s.Length)
            {
                bytes += 4;
                i++;
                continue;
            }

            bytes += cp < 0x80 ? 1 : cp < 0x800 ? 2 : 3;
        }

        return s.Length;
    }

    private void PrettyPrintTokenWithSplit(StringBuilder line, int tk)
    {
        string prefix = line.ToString();
        string tail;
        int indent;
        switch (TypeFromToken(tk))
        {
            case mdtMethodDef:
                tail = PrettyPrintMethodDef(prefix, tk, null, out indent);
                break;
            case mdtMemberRef:
                tail = PrettyPrintMemberRef(prefix, tk, null, out indent);
                break;
            case mdtMethodSpec:
            {
                int savedMVarOwner = _tkMVarOwner;
                _tkMVarOwner = 0;
                (int meth, byte[] inst) = GetMethodSpecProps(tk);
                tail = TypeFromToken(meth) == mdtMethodDef
                    ? PrettyPrintMethodDef(prefix, meth, inst, out indent)
                    : PrettyPrintMemberRef(prefix, meth, inst, out indent);
                _tkMVarOwner = savedMVarOwner;
                break;
            }

            default:
                return;
        }

        SplitSignatureByCommas(line, tail, tk, indent);
    }

    // Prints all but the last segment of a long member signature, one parameter per line, leaving the rest in line.
    private void SplitSignatureByCommas(StringBuilder line, string tail, int tk, int indent)
    {
        if (indent < Encoding.UTF8.GetByteCount(tail))
        {
            int start = ByteOffsetToCharIndex(tail, indent);
            int pComma = tail.IndexOf(',', start);
            while (pComma >= 0)
            {
                int i = 0, j = 0, k = 0, l = 0;
                string ltn = LTN();
                string gtn = GTN();
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
                        else if (c == '(')
                        {
                            k++;
                        }
                        else if (c == ')')
                        {
                            k--;
                        }
                        else if (string.CompareOrdinal(tail, pch, ltn, 0, ltn.Length) == 0)
                        {
                            l++;
                        }
                        else if (string.CompareOrdinal(tail, pch, gtn, 0, gtn.Length) == 0)
                        {
                            l--;
                        }
                    }
                }

                pComma++;
                // The native counters are unsigned: a negative parenthesis depth never allows a split.
                if (i == 0 && j == 0 && k >= 0 && k <= 1 && l == 0)
                {
                    PrintLine(tail.Substring(0, pComma));
                    string ind = Indent;
                    int pad = indent - Encoding.UTF8.GetByteCount(ind);
                    tail = ind + new string(' ', Math.Max(0, pad)) + tail.Substring(pComma);
                    pComma = tail.IndexOf(',', 0);
                }
                else
                {
                    pComma = tail.IndexOf(',', pComma);
                }
            }
        }

        line.Clear();
        line.Append(tail);
        if (_options.ShowTokens)
        {
            line.Append(COMMENT($" /* {tk:X8} */"));
        }
    }

    // PrettyPrintToken: appends the formatted token to line (which may emit preceding lines for long signatures).
    private void PrettyPrintToken(StringBuilder line, int tk)
    {
        if (!IsValidToken(tk))
        {
            line.Append(ERRORMSG($"<invalid token 0x{tk:x8}>"));
            return;
        }

        TypeDefDescr? alias = FindTypedefFor(tk);
        if (alias is not null)
        {
            line.Append(JUMPPT(ProperName(alias.Name), alias.TkSelf));
            return;
        }

        switch (TypeFromToken(tk))
        {
            case mdtTypeDef:
            case mdtTypeRef:
            case mdtTypeSpec:
            case mdtAssemblyRef:
            case mdtAssembly:
            case mdtModuleRef:
            case mdtModule:
                PrettyPrintClass(line, tk);
                break;

            case mdtMethodDef:
            case mdtMemberRef:
            case mdtMethodSpec:
                PrettyPrintTokenWithSplit(line, tk);
                break;

            case mdtFieldDef:
            {
                string memberName = MakeNameIfNone(GetNameOfFieldDef(tk), tk);
                FieldAttributes attrs = GetFieldDefAttrs(tk);
                byte[] sig = GetSigOfFieldDef(tk);
                int cr = GetParentOfFieldDef(tk);
                StringBuilder curPos = new();
                if (cr != 0x02000001 && RidFromToken(cr) != 0)
                {
                    curPos.Append(PrettyPrintClass(cr)).Append("::");
                }

                string namePart = IsFdPrivateScope(attrs) ? memberName + "$PST" + X8(tk) : memberName;
                curPos.Append(JUMPPT(ProperName(namePart), tk));
                string s = PrettyPrintSig(sig, curPos.ToString(), line.ToString());
                line.Clear();
                line.Append(s);
                if (_options.ShowTokens)
                {
                    line.Append(COMMENT($" /* {tk:X8} */"));
                }

                break;
            }

            case mdtString:
            {
                byte[]? bytes = GetUserStringBytes(tk);
                if (bytes is not null)
                {
                    DumpUnicodeString(line, bytes);
                }
                else
                {
                    line.Clear();
                    line.Append(ERRORMSG($"INVALID TOKEN: 0x{tk:X8}"));
                }

                if (_options.ShowTokens)
                {
                    line.Append(COMMENT($" /* {tk:X8} */"));
                }

                break;
            }

            default:
                line.Append(ERRORMSG($"<unknown token type 0x{(byte)((uint)tk >> 24):x2}>"));
                break;
        }
    }

    // The UTF-16 bytes of a user string, without the trailing flag byte.
    private byte[]? GetUserStringBytes(int tk)
    {
        if (!IsValidToken(tk))
        {
            return null;
        }

        string s = _md.GetUserString(System.Reflection.Metadata.Ecma335.MetadataTokens.UserStringHandle(RidFromToken(tk)));
        byte[] b = new byte[s.Length * 2];
        for (int i = 0; i < s.Length; i++)
        {
            b[2 * i] = (byte)s[i];
            b[2 * i + 1] = (byte)(s[i] >> 8);
        }

        return b;
    }

    // Whether a MemberRef refers to a field (used to pick the 'field' or 'method' keyword).
    private bool IsFieldMemberRef(int tk)
    {
        byte[] sig = GetMemberRefProps(tk).Sig;
        return sig.Length > 0 && IsCallConv(sig[0], IMAGE_CEE_CS_CALLCONV_FIELD);
    }
}
