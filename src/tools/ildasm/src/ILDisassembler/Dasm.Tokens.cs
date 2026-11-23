// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ILDisassembler;

// Token pretty printing.
internal sealed partial class Dasm
{
    // IsGenericInst: whether a TypeSpec is a (possibly decorated) generic instantiation.
    private bool IsGenericInst(EntityHandle h)
    {
        if (h.Kind != HandleKind.TypeSpecification || !IsValid(h))
        {
            return false;
        }

        byte[] sig = Blob(_md.GetTypeSpecification((TypeSpecificationHandle)h).Signature);
        foreach (byte b in sig)
        {
            switch ((SignatureTypeCode)b)
            {
                case SignatureTypeCode.Pointer:
                case SignatureTypeCode.SZArray:
                case SignatureTypeCode.Array:
                case SignatureTypeCode.ByReference:
                    continue;
                default:
                    return (SignatureTypeCode)b == SignatureTypeCode.GenericTypeInstance;
            }
        }

        return false;
    }

    private static bool IsMdPrivateScope(MethodAttributes a) => (a & MethodAttributes.MemberAccessMask) == MethodAttributes.PrivateScope;

    private static bool IsFdPrivateScope(FieldAttributes a) => (a & FieldAttributes.FieldAccessMask) == FieldAttributes.PrivateScope;

    // Returns the full line (prefix + signature) and, in indent, the byte column just after the opening parenthesis.
    private string PrettyPrintMemberRef(string prefix, EntityHandle h, byte[]? instSig, out int indent)
    {
        EntityHandle savedVarOwner = _varOwner;
        string memberName;
        byte[] sig;
        EntityHandle cr;
        if (IsValid(h))
        {
            MemberReference mr = _md.GetMemberReference((MemberReferenceHandle)h);
            memberName = Str(mr.Name);
            sig = Blob(mr.Signature);
            cr = GetParent(h);
        }
        else
        {
            (memberName, sig, cr) = ("Invalid MemberRef record", Array.Empty<byte>(), default(TypeReferenceHandle));
        }

        memberName = MakeNameIfNone(memberName, h);
        MethodAttributes dwAttrs = (MethodAttributes)0xFFFF;
        MethodDefinitionHandle methodDef = default;
        StringBuilder curPos = new();
        if (cr.Kind == HandleKind.MethodDefinition)
        {
            methodDef = (MethodDefinitionHandle)cr;
            dwAttrs = _md.GetMethodDefinition(methodDef).Attributes;
            cr = GetParent(cr);
        }

        if (!cr.IsNil && cr != s_moduleType)
        {
            curPos.Append(PrettyPrintClass(cr)).Append("::");
            if (IsGenericInst(cr))
            {
                _varOwner = default;
            }
        }

        string namePart = !methodDef.IsNil && IsMdPrivateScope(dwAttrs) ? memberName + "$PST" + X8(Token(methodDef)) : memberName;
        curPos.Append(!methodDef.IsNil ? JUMPPT(ProperName(namePart), methodDef) : ProperName(namePart));

        if (instSig is not null)
        {
            curPos.Append(PrettyPrintSig(instSig, curPos.ToString()));
        }

        string cur = curPos.ToString();
        string tail = PrettyPrintSig(sig, cur, prefix, printTyArity: instSig is null);
        indent = ComputeIndent(tail, cur);
        _varOwner = savedVarOwner;
        return tail;
    }

    private string PrettyPrintMethodDef(string prefix, MethodDefinitionHandle h, byte[]? instSig, out int indent)
    {
        EntityHandle savedVarOwner = _varOwner;
        MethodDefinition md = _md.GetMethodDefinition(h);
        string memberName = MakeNameIfNone(Str(md.Name), h);
        MethodAttributes dwAttrs = md.Attributes;
        byte[] sig = Blob(md.Signature);
        EntityHandle cr = GetParent(h);
        StringBuilder curPos = new();
        if (!cr.IsNil && cr != s_moduleType)
        {
            curPos.Append(PrettyPrintClass(cr)).Append("::");
            if (IsGenericInst(cr))
            {
                _varOwner = default;
            }
        }

        string namePart = IsMdPrivateScope(dwAttrs) ? memberName + "$PST" + X8(Token(h)) : memberName;
        curPos.Append(JUMPPT(ProperName(namePart), h));

        if (instSig is not null)
        {
            curPos.Append(PrettyPrintSig(instSig, curPos.ToString()));
        }

        string cur = curPos.ToString();
        string tail = PrettyPrintSig(sig, cur, prefix, printTyArity: instSig is null);
        indent = ComputeIndent(tail, cur);
        _varOwner = savedVarOwner;
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

    private void PrettyPrintTokenWithSplit(StringBuilder line, EntityHandle h)
    {
        string prefix = line.ToString();
        string tail;
        int indent;
        switch (h.Kind)
        {
            case HandleKind.MethodDefinition:
                tail = PrettyPrintMethodDef(prefix, (MethodDefinitionHandle)h, null, out indent);
                break;
            case HandleKind.MemberReference:
                tail = PrettyPrintMemberRef(prefix, h, null, out indent);
                break;
            case HandleKind.MethodSpecification:
            {
                EntityHandle savedMVarOwner = _mvarOwner;
                _mvarOwner = default;
                MethodSpecification ms = _md.GetMethodSpecification((MethodSpecificationHandle)h);
                byte[] inst = Blob(ms.Signature);
                EntityHandle method = NilAsDefault(ms.Method);
                tail = method.Kind == HandleKind.MethodDefinition
                    ? PrettyPrintMethodDef(prefix, (MethodDefinitionHandle)method, inst, out indent)
                    : PrettyPrintMemberRef(prefix, method, inst, out indent);
                _mvarOwner = savedMVarOwner;
                break;
            }

            default:
                return;
        }

        SplitSignatureByCommas(line, tail, h, indent);
    }

    // Prints all but the last segment of a long member signature, one parameter per line, leaving the rest in line.
    private void SplitSignatureByCommas(StringBuilder line, string tail, EntityHandle h, int indent)
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
                    if (c == '\\' && j != 0)
                    {
                        pch++; // skip the escaped character in a quoted name
                    }
                    else if (c == '\'')
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
                // The counters are unsigned: a negative parenthesis depth never allows a split.
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
            line.Append(COMMENT($" /* {Token(h):X8} */"));
        }
    }

    // PrettyPrintToken: appends the formatted token to line (which may emit preceding lines for long signatures).
    private void PrettyPrintToken(StringBuilder line, EntityHandle h)
    {
        if (!IsValid(h))
        {
            line.Append(ERRORMSG($"<invalid token 0x{Token(h):x8}>"));
            return;
        }

        switch (h.Kind)
        {
            case HandleKind.TypeDefinition:
            case HandleKind.TypeReference:
            case HandleKind.TypeSpecification:
            case HandleKind.AssemblyReference:
            case HandleKind.AssemblyDefinition:
            case HandleKind.ModuleReference:
            case HandleKind.ModuleDefinition:
                PrettyPrintClass(line, h);
                break;

            case HandleKind.MethodDefinition:
            case HandleKind.MemberReference:
            case HandleKind.MethodSpecification:
                PrettyPrintTokenWithSplit(line, h);
                break;

            case HandleKind.FieldDefinition:
            {
                FieldDefinition fd = _md.GetFieldDefinition((FieldDefinitionHandle)h);
                string memberName = MakeNameIfNone(Str(fd.Name), h);
                EntityHandle cr = GetParent(h);
                StringBuilder curPos = new();
                if (cr != s_moduleType && !cr.IsNil)
                {
                    curPos.Append(PrettyPrintClass(cr)).Append("::");
                }

                string namePart = IsFdPrivateScope(fd.Attributes) ? memberName + "$PST" + X8(Token(h)) : memberName;
                curPos.Append(JUMPPT(ProperName(namePart), h));
                string s = PrettyPrintSig(Blob(fd.Signature), curPos.ToString(), line.ToString());
                line.Clear();
                line.Append(s);
                if (_options.ShowTokens)
                {
                    line.Append(COMMENT($" /* {Token(h):X8} */"));
                }

                break;
            }

            default:
                line.Append(ERRORMSG($"<unknown token type 0x{(byte)h.Kind:x2}>"));
                break;
        }
    }

    // A user string (ldstr).
    private void PrettyPrintUserString(StringBuilder line, UserStringHandle h)
    {
        string s = _md.GetUserString(h);
        byte[] bytes = new byte[s.Length * 2];
        for (int i = 0; i < s.Length; i++)
        {
            bytes[2 * i] = (byte)s[i];
            bytes[2 * i + 1] = (byte)(s[i] >> 8);
        }

        DumpUnicodeString(line, bytes);
        if (_options.ShowTokens)
        {
            line.Append(COMMENT($" /* {Token(h):X8} */"));
        }
    }

    // Whether a MemberRef refers to a field (used to pick the 'field' or 'method' keyword).
    private bool IsFieldMemberRef(MemberReferenceHandle h)
    {
        BlobReader sig = _md.GetBlobReader(_md.GetMemberReference(h).Signature);
        return sig.Length > 0 && sig.ReadSignatureHeader().Kind == SignatureKind.Field;
    }
}
